using System.ComponentModel;
using DtoB.Project;
namespace DtoB.Desktop;
public sealed class ShellViewModel(ProjectSession session):INotifyPropertyChanged
{
    public ProjectSession Session {get;}=session;
    public event PropertyChangedEventHandler? PropertyChanged;
    public string ProjectName {get=>Session.Project?.Name??"";set{if(Session.Project!=null){Session.Rename(value);Refresh();}}}
    public string Metadata=>Session.Project is not { } p?"Create or open a DtoB project.":$"Project ID: {p.ProjectId}\nFile: {Session.CurrentFilePath??"Not saved"}\nCreated: {p.CreatedAt:O}\nModified: {p.ModifiedAt:O}\nDtoB version: {p.DtoBVersion}";
    public string WindowTitle=>"DtoB"+(Session.Project is { } p?" — "+p.Name+(Session.IsDirty?" *":""):"");
    public bool HasProject=>Session.Project!=null;
    public void Refresh(){foreach(var name in new[]{nameof(ProjectName),nameof(Metadata),nameof(WindowTitle),nameof(HasProject)})PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(name));}
}
