using System.Text.Json;
using System.Text.Json.Serialization;
namespace DtoB.Project;

public sealed record ProjectFile(int SchemaVersion, Guid ProjectId, string Name, DateTimeOffset CreatedAt, DateTimeOffset ModifiedAt, string DtoBVersion, string? LastKnownPath = null, DrawingFile[]? Drawings = null)
{
    public static ProjectFile Create(string name, string version) => new(2, Guid.NewGuid(), name.Trim(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, version, Drawings: []);
    public void Validate()
    {
        if(SchemaVersion!=2 || ProjectId==Guid.Empty || string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(DtoBVersion) || CreatedAt==default || ModifiedAt<CreatedAt || Drawings==null)
            throw new InvalidDataException("Invalid project metadata or unsupported schema version.");
        foreach(var drawing in Drawings) {if(drawing==null)throw new InvalidDataException("Null drawing.");drawing.Validate(ProjectId);}
        if(Drawings.Select(d=>d.DrawingId).Distinct().Count()!=Drawings.Length || Drawings.Select(d=>d.RevisionId).Distinct().Count()!=Drawings.Length || Drawings.Select(d=>d.FileHash).Distinct().Count()!=Drawings.Length || Drawings.Select(d=>d.SourceFilePath).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=Drawings.Length)
            throw new InvalidDataException("Duplicate drawing identity, revision, source path or content.");
    }
}
public static class LocalJson
{
    public static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy=JsonNamingPolicy.CamelCase, WriteIndented=true, UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow, Converters={new JsonStringEnumConverter(allowIntegerValues:false)} };
    public static T Read<T>(string path)
    {
        try
        {
            using var document=JsonDocument.Parse(File.ReadAllText(path));
            if(document.RootElement.ValueKind!=JsonValueKind.Object || !document.RootElement.TryGetProperty("schemaVersion",out var v) || v.ValueKind!=JsonValueKind.Number || !v.TryGetInt32(out var version) || version!=1) throw new InvalidDataException("Missing or unsupported schemaVersion; expected 1.");
            return JsonSerializer.Deserialize<T>(document.RootElement,Options) ?? throw new InvalidDataException("Empty document.");
        }
        catch(JsonException ex){throw new InvalidDataException("Corrupted or invalid JSON document.",ex);}
    }
    public static void Write<T>(string path,T value)
    {
        path=Path.GetFullPath(path);
        var temporary=Path.Combine(Path.GetDirectoryName(path)!,"."+Path.GetFileName(path)+"."+Guid.NewGuid().ToString("N")+".tmp");
        try
        {
            using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None)) {JsonSerializer.Serialize(stream,value,Options);stream.Flush(true);}
            if(File.Exists(path)) File.Replace(temporary,path,null); else File.Move(temporary,path);
        }
        finally {try{if(File.Exists(temporary))File.Delete(temporary);}catch(IOException){}catch(UnauthorizedAccessException){}}
    }
}
public interface IProjectStore { ProjectFile Load(string path); void Save(string path,ProjectFile project); }
public sealed class ProjectStore : IProjectStore
{
    private sealed record LegacyProject(int SchemaVersion,Guid ProjectId,string Name,DateTimeOffset CreatedAt,DateTimeOffset ModifiedAt,string DtoBVersion,string? LastKnownPath=null);
    public ProjectFile Load(string path)
    {
        try
        {
            using var document=JsonDocument.Parse(File.ReadAllText(path));var root=document.RootElement;
            if(root.ValueKind!=JsonValueKind.Object || !root.TryGetProperty("schemaVersion",out var v) || v.ValueKind!=JsonValueKind.Number || !v.TryGetInt32(out var version))throw new InvalidDataException("Integer schemaVersion required.");
            ProjectFile p;
            if(version==1){var old=JsonSerializer.Deserialize<LegacyProject>(root,LocalJson.Options)??throw new InvalidDataException("Empty project.");p=new(2,old.ProjectId,old.Name,old.CreatedAt,old.ModifiedAt,old.DtoBVersion,old.LastKnownPath,[]);}
            else if(version==2)p=JsonSerializer.Deserialize<ProjectFile>(root,LocalJson.Options)??throw new InvalidDataException("Empty project.");
            else throw new InvalidDataException("Unsupported project schemaVersion.");
            p.Validate();return p;
        }
        catch(JsonException ex){throw new InvalidDataException("Corrupted or invalid project JSON.",ex);}
    }
    public void Save(string path,ProjectFile project){project.Validate();LocalJson.Write(path,project);}
}
public enum UnsavedChoice { Save, Discard, Cancel }
public sealed partial class ProjectSession(IProjectStore store,DrawingFileService? drawingFiles=null)
{
    private readonly DrawingFileService files=drawingFiles??new();
    private long generation;
    public ProjectFile? Project {get;private set;}
    public string? CurrentFilePath {get;private set;}
    public bool IsDirty {get;private set;}
    public void New(string name,string version){var p=ProjectFile.Create(name,version);p.Validate();Project=p;CurrentFilePath=null;IsDirty=true;ResetRuntime();}
    public void Open(string path){var actual=Path.GetFullPath(path);var p=store.Load(actual);Project=p;CurrentFilePath=actual;IsDirty=false;ResetRuntime();RefreshDrawings();}
    public void Rename(string name){if(Project==null)throw new InvalidOperationException("No project.");if(Project.Name==name)return;var p=Project with {Name=name};p.Validate();Project=p;IsDirty=true;generation++;}
    public void Save(string path)
    {
        if(Project==null)throw new InvalidOperationException("No project.");
        var actual=ValidateManifestPath(path);
        if(Project.Drawings!.Length>0 && !SameDirectory(actual,CurrentFilePath))throw new InvalidOperationException("Use asynchronous SaveAsync for asset relocation.");
        CommitSave(actual);
    }
    public async Task SaveAsync(string path,CancellationToken cancellationToken=default)
    {
        if(Project==null)throw new InvalidOperationException("No project.");var actual=ValidateManifestPath(path);var version=generation;var project=Project;
        if(project.Drawings!.Length>0 && !SameDirectory(actual,CurrentFilePath))await files.CopyAssetsAsync(project,CurrentFilePath!,actual,cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();if(generation!=version)throw new InvalidOperationException("Project changed during Save As.");CommitSave(actual);
    }
    private void CommitSave(string actual)
    {
        if(Project!.Drawings!.Length>0){foreach(var d in Project.Drawings){var status=files.Probe(actual,Project.ProjectId,d);if(status.Availability!=Availability.Ready || status.SizeChanged)throw new IOException("Required managed drawing is unavailable or has changed size.");if(Runtime.TryGetValue(d.DrawingId,out var r)&&r.Integrity==Integrity.HashMismatch)throw new InvalidDataException("Known managed drawing hash mismatch; replace or restore it before saving.");}}
        var p=Project with {ModifiedAt=DateTimeOffset.UtcNow,LastKnownPath=actual};store.Save(actual,p);Project=p;var moved=CurrentFilePath!=actual;CurrentFilePath=actual;IsDirty=false;generation++;if(moved)ResetRuntime();RefreshDrawings();
    }
    private static bool SameDirectory(string path,string? other)=>other!=null&&StringComparer.OrdinalIgnoreCase.Equals(Path.GetDirectoryName(path),Path.GetDirectoryName(other));
    private static string ValidateManifestPath(string path){var actual=Path.GetFullPath(path);if(!Path.GetExtension(actual).Equals(".dtob",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Project destination must be .dtob; CAD source files cannot be overwritten.");AssetPaths.RejectReparse(actual,Path.GetDirectoryName(actual)!);return actual;}
    public bool CanLeave(UnsavedChoice choice,Func<bool> save) => !IsDirty || choice switch {UnsavedChoice.Save=>save()&&!IsDirty,UnsavedChoice.Discard=>true,_=>false};
    public void Close(){Project=null;CurrentFilePath=null;IsDirty=false;ResetRuntime();}
}
