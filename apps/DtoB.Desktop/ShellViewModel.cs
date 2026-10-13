using System.ComponentModel;
using DtoB.Project;
namespace DtoB.Desktop;
public sealed record DrawingRow(DrawingFile Drawing,DrawingStatus? Status)
{
    public string OriginalFileName=>Drawing.OriginalFileName;
    public string StatusText=>Status?.Display??"Status unavailable";
}
public sealed class ShellViewModel(ProjectSession session):INotifyPropertyChanged
{
    public ProjectSession Session {get;}=session;
    private DrawingFile? selected;
    public DrawingFile? SelectedDrawing {get=>selected;set{selected=value;Notify(nameof(SelectedDrawing));Notify(nameof(DrawingMetadata));Notify(nameof(CanRevealSource));Notify(nameof(CanRevealManaged));}}
    public DrawingFile[] Drawings=>Session.Project?.Drawings??[];
    public DrawingRow[] DrawingRows=>Drawings.Select(d=>new DrawingRow(d,Session.Runtime.GetValueOrDefault(d.DrawingId))).ToArray();
    public string DrawingSummary
    {
        get
        {
            var states=Drawings.Select(d=>Session.Runtime.GetValueOrDefault(d.DrawingId)).OfType<DrawingStatus>().ToArray();
            var counts=states.GroupBy(s=>s.Availability).OrderBy(g=>g.Key).Select(g=>$"{g.Count()} {g.Key}").ToList();
            counts.AddRange(states.GroupBy(s=>s.SourceStatus).OrderBy(g=>g.Key).Select(g=>$"{g.Count()} {g.Key}"));
            if(states.Any(s=>s.SizeChanged))counts.Add($"{states.Count(s=>s.SizeChanged)} SizeChanged (unverified)");
            if(states.Any(s=>s.Integrity==Integrity.HashMismatch))counts.Add($"{states.Count(s=>s.Integrity==Integrity.HashMismatch)} HashMismatch");
            return $"{Drawings.Length} drawings"+(counts.Count==0?"":": "+string.Join(", ",counts));
        }
    }
    public bool CanRevealSource=>selected!=null&&Session.Runtime.TryGetValue(selected.DrawingId,out var r)&&r.SourceStatus==SourceAvailability.SourcePresent;
    public bool CanRevealManaged=>selected!=null&&Session.Runtime.TryGetValue(selected.DrawingId,out var r)&&r.Availability==Availability.Ready;
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify(string name)=>PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(name));
    public string ProjectName {get=>Session.Project?.Name??"";set{if(Session.Project!=null){Session.Rename(value);Refresh();}}}
    public string Metadata=>Session.Project is not { } p?"Create or open a DtoB project.":$"Project ID: {p.ProjectId}\nFile: {Session.CurrentFilePath??"Not saved"}\nCreated: {p.CreatedAt:O}\nModified: {p.ModifiedAt:O}\nDtoB version: {p.DtoBVersion} · Project schema: {p.SchemaVersion}";
    public string DrawingMetadata=>selected is not { } d?"Select a drawing to inspect metadata.":$"{d.OriginalFileName}\nDrawingId: {d.DrawingId}\nRevisionId: {d.RevisionId}\nSource: {d.SourceFilePath}\nManaged: {d.ManagedFilePath}\nSize: {d.FileSize:N0} bytes\nSHA-256: {d.FileHash}\nDWG format: {d.DetectedDwgVersion??d.VersionDetection.ToString()}\nUnit: {d.UnitDetection}\nImported: {d.ImportedAt:O}\nValidation: {d.Validation}\n{(Session.Runtime.TryGetValue(d.DrawingId,out var status)?status.Display:"Save project before importing")}";
    public string WindowTitle=>"DtoB"+(Session.Project is { } p?" — "+p.Name+(Session.IsDirty?" *":""):"");
    public bool HasProject=>Session.Project!=null;
    public void Refresh()
    {
        var selectedId=selected?.DrawingId;Session.RefreshDrawings();
        Notify(nameof(Drawings));Notify(nameof(DrawingRows));
        // ItemsSource replacement can send a null selection back through the WPF binding.
        SelectedDrawing=Drawings.FirstOrDefault(d=>d.DrawingId==selectedId);
        foreach(var name in new[]{nameof(ProjectName),nameof(Metadata),nameof(WindowTitle),nameof(HasProject),nameof(DrawingSummary)})Notify(name);
    }
}
