using System.Text.Json;
using System.Text.Json.Serialization;
namespace DtoB.Project;

public sealed record ProjectFile(int SchemaVersion, Guid ProjectId, string Name, DateTimeOffset CreatedAt, DateTimeOffset ModifiedAt, string DtoBVersion, string? LastKnownPath = null)
{
    public static ProjectFile Create(string name, string version) => new(1, Guid.NewGuid(), name.Trim(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, version);
    public void Validate()
    {
        if(SchemaVersion!=1 || ProjectId==Guid.Empty || string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(DtoBVersion) || CreatedAt==default || ModifiedAt<CreatedAt)
            throw new InvalidDataException("Invalid project metadata or unsupported schema version.");
    }
}
public static class LocalJson
{
    public static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy=JsonNamingPolicy.CamelCase, WriteIndented=true, UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow, Converters={new JsonStringEnumConverter()} };
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
    public ProjectFile Load(string path){var p=LocalJson.Read<ProjectFile>(path);p.Validate();return p;}
    public void Save(string path,ProjectFile project){project.Validate();LocalJson.Write(path,project);}
}
public enum UnsavedChoice { Save, Discard, Cancel }
public sealed class ProjectSession(IProjectStore store)
{
    public ProjectFile? Project {get;private set;}
    public string? CurrentFilePath {get;private set;}
    public bool IsDirty {get;private set;}
    public void New(string name,string version){var p=ProjectFile.Create(name,version);p.Validate();Project=p;CurrentFilePath=null;IsDirty=true;}
    public void Open(string path){var actual=Path.GetFullPath(path);var p=store.Load(actual);Project=p;CurrentFilePath=actual;IsDirty=false;}
    public void Rename(string name){if(Project==null)throw new InvalidOperationException("No project."); if(Project.Name==name)return;var p=Project with {Name=name};p.Validate();Project=p;IsDirty=true;}
    public void Save(string path)
    {
        if(Project==null)throw new InvalidOperationException("No project.");
        var actual=Path.GetFullPath(path);var p=Project with {ModifiedAt=DateTimeOffset.UtcNow,LastKnownPath=actual};
        store.Save(actual,p);Project=p;CurrentFilePath=actual;IsDirty=false;
    }
    public bool CanLeave(UnsavedChoice choice,Func<bool> save) => !IsDirty || choice switch {UnsavedChoice.Save=>save()&&!IsDirty,UnsavedChoice.Discard=>true,_=>false};
    public void Close(){Project=null;CurrentFilePath=null;IsDirty=false;}
}
