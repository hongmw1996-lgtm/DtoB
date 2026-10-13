namespace DtoB.Project;
public enum Detection { HeaderDetected, PendingReader, NotApplicable }
public sealed record DrawingFile(Guid DrawingId,Guid RevisionId,string OriginalFileName,string SourceFilePath,string ManagedFilePath,string FileExtension,long FileSize,string FileHash,Detection VersionDetection,string? DetectedDwgVersion,Detection UnitDetection,DateTimeOffset ImportedAt,string Validation)
{
    public void Validate(Guid projectId)
    {
        if(DrawingId==Guid.Empty || RevisionId==Guid.Empty || string.IsNullOrWhiteSpace(OriginalFileName) || OriginalFileName!=Path.GetFileName(OriginalFileName) || string.IsNullOrWhiteSpace(SourceFilePath) || !Path.IsPathFullyQualified(SourceFilePath) || SourceFilePath!=Path.GetFullPath(SourceFilePath) || FileExtension is not (".dwg" or ".dxf") || FileSize<=0 || FileHash==null || FileHash.Length!=64 || FileHash.Any(c=>!Uri.IsHexDigit(c)||char.IsLower(c)) || ImportedAt==default || ImportedAt.Offset!=TimeSpan.Zero || UnitDetection!=Detection.PendingReader || Validation!="BasicHeaderOnly")throw new InvalidDataException("Invalid drawing revision metadata.");
        if(ManagedFilePath!=AssetPaths.Relative(projectId,DrawingId,RevisionId,FileExtension))throw new InvalidDataException("Invalid managed relative path.");
        if(FileExtension==".dwg" ? VersionDetection!=Detection.HeaderDetected || !DrawingFileService.DwgCodes.Contains(DetectedDwgVersion??"") : VersionDetection!=Detection.NotApplicable || DetectedDwgVersion!=null)throw new InvalidDataException("Invalid version detection evidence.");
    }
}
public static class AssetPaths
{
    public static string Relative(Guid project,Guid drawing,Guid revision,string extension)=>$"{project:N}.dtob-assets/drawings/{drawing:N}/{revision:N}{extension}";
    public static string Resolve(string manifest,Guid project,DrawingFile drawing)
    {
        drawing.Validate(project);var root=Path.GetDirectoryName(Path.GetFullPath(manifest))!;var full=Path.GetFullPath(Path.Combine(root,drawing.ManagedFilePath));
        RejectReparse(full,root);return full;
    }
    public static void RejectReparse(string path,string projectDirectory)
    {
        var root=Path.TrimEndingDirectorySeparator(Path.GetFullPath(projectDirectory));
        var full=Path.GetFullPath(path);
        if(!full.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Managed path escapes project.");
        // The opened project directory is the boundary, including when reached via a junction.
        // Check the manifest itself or every asset component below it, never its ancestors.
        for(var current=full;!StringComparer.OrdinalIgnoreCase.Equals(current,root);current=Path.GetDirectoryName(current)!)
        {
            try{if((File.GetAttributes(current)&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("Reparse paths are unsupported for project storage.");}
            catch(FileNotFoundException){}catch(DirectoryNotFoundException){}
        }
    }
}
public enum Availability { Ready, ManagedMissing, ManagedUnreadable }
public enum SourceAvailability { SourceUnchecked, SourcePresent, SourceMissing, SourceUnreadable }
public enum Integrity { NotVerified, Verified, HashMismatch }
public sealed record FileProbe(bool Exists,bool Readable,long Length,long LastWriteTicks);
public sealed record DrawingStatus(Availability Availability,SourceAvailability SourceStatus,bool SizeChanged,FileProbe Probe,Integrity Integrity=Integrity.NotVerified,DateTimeOffset? VerifiedAt=null)
{
    public bool SourceMissing=>SourceStatus==SourceAvailability.SourceMissing;
    public string Display=>$"{Availability} · {SourceStatus}"+(SizeChanged?" · SizeChanged (unverified)":"")+$" · ReaderPending · Integrity: {Integrity}"+(VerifiedAt.HasValue?$" at {VerifiedAt:O}":"");
}
public sealed record Registration(DrawingFile Drawing,bool Changed,string Message);
public sealed partial class ProjectSession
{
    private readonly Dictionary<Guid,DrawingStatus> runtime=[];
    public IReadOnlyDictionary<Guid,DrawingStatus> Runtime=>runtime;
    private void ResetRuntime(){generation++;runtime.Clear();}
    public void RefreshDrawings()
    {
        if(Project==null || CurrentFilePath==null)return;
        foreach(var d in Project.Drawings!)
        {
            var status=files.Probe(CurrentFilePath,Project.ProjectId,d);
            if(runtime.TryGetValue(d.DrawingId,out var old))
            {
                status=status with {SourceStatus=old.SourceStatus};
                if(status.Probe==old.Probe&&status.Availability==Availability.Ready)status=status with {Integrity=old.Integrity,VerifiedAt=old.VerifiedAt};
            }
            runtime[d.DrawingId]=status;
        }
        foreach(var id in runtime.Keys.Except(Project.Drawings!.Select(d=>d.DrawingId)).ToArray())runtime.Remove(id);
    }
    public async Task<Registration> AddDrawingAsync(string source,Guid? replace=null,CancellationToken cancellationToken=default)
    {
        if(Project==null || CurrentFilePath==null)throw new InvalidOperationException("Save the project before adding a drawing.");
        var version=generation;var candidate=await files.RegisterAsync(Project,CurrentFilePath,source,replace,cancellationToken);cancellationToken.ThrowIfCancellationRequested();
        if(version!=generation)throw new InvalidOperationException("Project changed during drawing registration.");
        if(candidate.Changed){var next=Project with {Drawings=Project.Drawings!.Where(d=>d.DrawingId!=candidate.Drawing.DrawingId).Append(candidate.Drawing).ToArray()};next.Validate();Project=next;IsDirty=true;generation++;runtime.Remove(candidate.Drawing.DrawingId);RefreshDrawings();}return candidate;
    }
    public void RemoveDrawing(Guid id){if(Project==null)throw new InvalidOperationException("No project.");if(!Project.Drawings!.Any(d=>d.DrawingId==id))throw new InvalidDataException("Drawing not found.");Project=Project with {Drawings=Project.Drawings!.Where(d=>d.DrawingId!=id).ToArray()};IsDirty=true;generation++;runtime.Remove(id);}
    public async Task<bool> RefreshSourceStatusAsync(Guid id,CancellationToken cancellationToken=default)
    {
        if(Project==null || CurrentFilePath==null)throw new InvalidOperationException("No saved project.");
        var drawing=Project.Drawings!.Single(d=>d.DrawingId==id);var version=generation;var manifest=CurrentFilePath;
        RefreshDrawings();runtime[id]=runtime[id] with {SourceStatus=SourceAvailability.SourceUnchecked};
        var source=await files.ProbeSourceAsync(drawing.SourceFilePath,cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if(version!=generation || CurrentFilePath!=manifest)return false;
        runtime[id]=runtime[id] with {SourceStatus=source};return true;
    }
    public async Task<bool> VerifyIntegrityAsync(Guid id,CancellationToken cancellationToken=default)
    {
        if(Project==null || CurrentFilePath==null)throw new InvalidOperationException("No saved project.");var drawing=Project.Drawings!.Single(d=>d.DrawingId==id);var version=generation;var manifest=CurrentFilePath;
        RefreshDrawings();runtime[id]=runtime[id] with {Integrity=Integrity.NotVerified,VerifiedAt=null};
        var verified=await files.VerifyAsync(manifest,Project.ProjectId,drawing,cancellationToken);cancellationToken.ThrowIfCancellationRequested();
        if(version!=generation || CurrentFilePath!=manifest)return false;
        RefreshDrawings();if(runtime[id].Probe!=verified.Probe)throw new IOException("Drawing changed during integrity verification.");
        runtime[id]=runtime[id] with {Integrity=verified.Integrity,VerifiedAt=DateTimeOffset.UtcNow};return true;
    }
}

