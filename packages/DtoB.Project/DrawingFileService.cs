using System.Security.Cryptography;
using System.Text;
namespace DtoB.Project;

// Metadata/access probing has no content-read API usage. Separate from explicit hashing.
public interface IAssetFiles
{
    FileProbe Probe(string path);
    Stream OpenRead(string path);
    Stream CreateNew(string path);
    void CreateDirectory(string path);
    void Flush(Stream stream);
    void Publish(string temporary,string target);
    void Delete(string path);
}
public class AssetFiles:IAssetFiles
{
    protected virtual Stream OpenAccessProbe(string path)=>new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);
    public virtual FileProbe Probe(string path)
    {
        try{var info=new FileInfo(path);if(!info.Exists)return new(false,false,0,0);using var handle=OpenAccessProbe(path);return new(true,true,info.Length,info.LastWriteTimeUtc.Ticks);}
        catch(Exception ex) when(LocalFileErrors.IsExpected(ex)){return new(File.Exists(path),false,0,0);}
    }
    public virtual Stream OpenRead(string path)=>new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read,65536,FileOptions.Asynchronous|FileOptions.SequentialScan);
    public virtual Stream CreateNew(string path)=>new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None,65536,FileOptions.Asynchronous);
    public virtual void CreateDirectory(string path)=>Directory.CreateDirectory(path);
    public virtual void Flush(Stream stream){if(stream is FileStream file)file.Flush(true);else stream.Flush();}
    public virtual void Publish(string temporary,string target)=>File.Move(temporary,target,false);
    public virtual void Delete(string path)=>File.Delete(path);
}
public sealed record Fingerprint(long Length,string Hash,byte[] Header);
public interface IContentHasher {Task<Fingerprint> HashAsync(Stream input,Stream? output,CancellationToken cancellationToken);}
public sealed class ContentHasher:IContentHasher
{
    public Task<Fingerprint> HashAsync(Stream input,Stream? output,CancellationToken cancellationToken)=>Task.Run(async()=>
    {
        using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);var buffer=new byte[65536];using var header=new MemoryStream();long length=0;
        int read;while((read=await input.ReadAsync(buffer,cancellationToken).ConfigureAwait(false))!=0){cancellationToken.ThrowIfCancellationRequested();hash.AppendData(buffer,0,read);if(output!=null)await output.WriteAsync(buffer.AsMemory(0,read),cancellationToken).ConfigureAwait(false);if(header.Length<4096)header.Write(buffer,0,(int)Math.Min(read,4096-header.Length));length+=read;}
        cancellationToken.ThrowIfCancellationRequested();return new Fingerprint(length,Convert.ToHexString(hash.GetHashAndReset()),header.ToArray());
    },cancellationToken);
}
public sealed class DrawingFileService(IAssetFiles? assetFiles=null,IContentHasher? contentHasher=null,Action<string>? cleanupWarning=null)
{
    private readonly IAssetFiles io=assetFiles??new AssetFiles();private readonly IContentHasher hasher=contentHasher??new ContentHasher();
    public static IReadOnlySet<string> DwgCodes {get;}=new HashSet<string>(["AC1009","AC1012","AC1014","AC1015","AC1018","AC1021","AC1024","AC1027","AC1032"],StringComparer.Ordinal);
    private void Cleanup(string path){try{io.Delete(path);}catch(Exception ex) when(LocalFileErrors.IsExpected(ex)){System.Diagnostics.Trace.WriteLine(ex);cleanupWarning?.Invoke("Temporary drawing cleanup failed: "+ex.Message);}}
    public DrawingStatus Probe(string manifest,Guid project,DrawingFile drawing)
    {
        string path;
        try{path=AssetPaths.Resolve(manifest,project,drawing);}
        catch(Exception ex) when(LocalFileErrors.IsExpected(ex)){return new(Availability.ManagedUnreadable,SourceAvailability.SourceUnchecked,false,new(true,false,0,0));}
        var probe=io.Probe(path);var availability=!probe.Exists?Availability.ManagedMissing:!probe.Readable?Availability.ManagedUnreadable:Availability.Ready;
        return new(availability,SourceAvailability.SourceUnchecked,probe.Readable&&probe.Length!=drawing.FileSize,probe);
    }
    // Explicit user operation only. A cancelled wait cannot abort an already-running OS probe.
    public async Task<SourceAvailability> ProbeSourceAsync(string source,CancellationToken cancellationToken)
    {
        var probe=await Task.Run(()=>io.Probe(source),cancellationToken).WaitAsync(cancellationToken);
        return !probe.Exists?SourceAvailability.SourceMissing:probe.Readable?SourceAvailability.SourcePresent:SourceAvailability.SourceUnreadable;
    }
    private static string? ValidateHeader(string extension,Fingerprint content)
    {
        if(content.Length<=6)throw new InvalidDataException("Empty or truncated CAD file.");
        if(extension==".dwg"){var code=Encoding.ASCII.GetString(content.Header.AsSpan(0,6));if(!DwgCodes.Contains(code))throw new InvalidDataException("Unsupported or invalid DWG signature.");return code;}
        var bytes=content.Header.AsSpan();if(bytes.StartsWith(new byte[]{0xEF,0xBB,0xBF}))bytes=bytes[3..];
        if(bytes.IndexOf((byte)0)>=0)throw new InvalidDataException("Binary/UTF-16 DXF is unsupported in PHASE 02.");
        var lines=Encoding.ASCII.GetString(bytes).Split('\n').Select(l=>l.Trim()).Take(4).ToArray();
        if(lines.Length!=4 || lines[0]!="0" || lines[1]!="SECTION" || lines[2]!="2" || lines[3]!="HEADER")throw new InvalidDataException("Expected text DXF HEADER section; binary/other formats are unsupported.");return null;
    }
    public async Task<Registration> RegisterAsync(ProjectFile project,string manifest,string source,Guid? replace,CancellationToken cancellationToken)
    {
        project.Validate();source=Path.GetFullPath(source);var extension=Path.GetExtension(source).ToLowerInvariant();if(extension is not (".dwg" or ".dxf"))throw new InvalidDataException("Only DWG and text DXF drawings are supported.");
        var previous=replace.HasValue?project.Drawings!.SingleOrDefault(d=>d.DrawingId==replace):null;if(replace.HasValue&&previous==null)throw new InvalidDataException("Replacement drawing not found.");
        var id=previous?.DrawingId??Guid.NewGuid();var revision=Guid.NewGuid();
        var projectDirectory=Path.GetDirectoryName(Path.GetFullPath(manifest))!;
        var directory=Path.Combine(projectDirectory, $"{project.ProjectId:N}.dtob-assets","drawings",id.ToString("N"));AssetPaths.RejectReparse(directory,projectDirectory);io.CreateDirectory(directory);AssetPaths.RejectReparse(directory,projectDirectory);
        var temporary=Path.Combine(directory,"."+Guid.NewGuid().ToString("N")+".tmp");
        try
        {
            Fingerprint content;using(var input=io.OpenRead(source))using(var output=io.CreateNew(temporary)){content=await hasher.HashAsync(input,output,cancellationToken);await output.FlushAsync(cancellationToken);await Task.Run(()=>io.Flush(output),cancellationToken);}
            var code=ValidateHeader(extension,content);cancellationToken.ThrowIfCancellationRequested();
            var duplicate=project.Drawings!.FirstOrDefault(d=>d.FileHash==content.Hash);
            if(duplicate!=null){if(previous!=null&&duplicate.DrawingId!=previous.DrawingId)throw new InvalidDataException("Replacement duplicates another registered drawing.");return new(duplicate,false,"Same content already registered; no change.");}
            if(project.Drawings!.Any(d=>d.DrawingId!=id&&StringComparer.OrdinalIgnoreCase.Equals(d.SourceFilePath,source)))throw new InvalidDataException("This source path is registered with different contents. Use Replace Drawing.");
            var drawing=new DrawingFile(id,revision,Path.GetFileName(source),source,AssetPaths.Relative(project.ProjectId,id,revision,extension),extension,content.Length,content.Hash,extension==".dwg"?Detection.HeaderDetected:Detection.NotApplicable,code,Detection.PendingReader,DateTimeOffset.UtcNow,"BasicHeaderOnly");drawing.Validate(project.ProjectId);
            var target=AssetPaths.Resolve(manifest,project.ProjectId,drawing);cancellationToken.ThrowIfCancellationRequested();io.Publish(temporary,target);return new(drawing,true,previous==null?"Drawing registered.":"Drawing replaced; logical identity preserved.");
        }
        finally{Cleanup(temporary);}
    }
    public async Task<DrawingStatus> VerifyAsync(string manifest,Guid project,DrawingFile drawing,CancellationToken cancellationToken)
    {
        var before=Probe(manifest,project,drawing);if(before.Availability!=Availability.Ready)throw new IOException("Managed drawing is missing or unreadable.");
        Fingerprint fingerprint;using(var input=io.OpenRead(AssetPaths.Resolve(manifest,project,drawing)))fingerprint=await hasher.HashAsync(input,null,cancellationToken);
        var after=Probe(manifest,project,drawing);if(before.Probe!=after.Probe)throw new IOException("Managed drawing changed during verification.");
        return after with {Integrity=fingerprint.Hash==drawing.FileHash?Integrity.Verified:Integrity.HashMismatch};
    }
    public async Task CopyAssetsAsync(ProjectFile project,string sourceManifest,string targetManifest,CancellationToken cancellationToken)
    {
        foreach(var drawing in project.Drawings!)
        {
            cancellationToken.ThrowIfCancellationRequested();var source=AssetPaths.Resolve(sourceManifest,project.ProjectId,drawing);var target=AssetPaths.Resolve(targetManifest,project.ProjectId,drawing);
            if(io.Probe(target).Exists){using var existing=io.OpenRead(target);var fingerprint=await hasher.HashAsync(existing,null,cancellationToken);if(fingerprint.Hash!=drawing.FileHash)throw new InvalidDataException("Existing destination revision has different bytes; it will not be overwritten.");continue;}
            var directory=Path.GetDirectoryName(target)!;io.CreateDirectory(directory);AssetPaths.RejectReparse(directory,Path.GetDirectoryName(Path.GetFullPath(targetManifest))!);var temporary=Path.Combine(directory,"."+Guid.NewGuid().ToString("N")+".tmp");
            try{using(var input=io.OpenRead(source))using(var output=io.CreateNew(temporary)){var copied=await hasher.HashAsync(input,output,cancellationToken);if(copied.Hash!=drawing.FileHash || copied.Length!=drawing.FileSize)throw new InvalidDataException("Managed drawing content differs from its registered revision.");await output.FlushAsync(cancellationToken);await Task.Run(()=>io.Flush(output),cancellationToken);}cancellationToken.ThrowIfCancellationRequested();io.Publish(temporary,target);}
            finally{Cleanup(temporary);}
        }
    }
}

