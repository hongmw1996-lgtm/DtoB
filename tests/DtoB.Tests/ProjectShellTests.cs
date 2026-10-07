using DtoB.Project;
using Xunit;
namespace DtoB.Tests;
public sealed class ProjectShellTests:IDisposable
{
    private readonly string root=Path.Combine(Path.GetTempPath(),"DtoB-shell-"+Guid.NewGuid());
    public ProjectShellTests()=>Directory.CreateDirectory(root);
    private string FilePath(string name)=>Path.Combine(root,name);
    public void Dispose()=>Directory.Delete(root,true);
    [Fact] public void CreateSaveReloadPreservesIdentityAndMetadata()
    {
        var session=new ProjectSession(new ProjectStore());session.New("Test","0.2.0");var id=session.Project!.ProjectId;var created=session.Project.CreatedAt;
        session.Save(FilePath("one.dtob"));var saved=session.Project;Assert.False(session.IsDirty);session.Close();session.Open(FilePath("one.dtob"));Assert.Equal(saved,session.Project);Assert.Equal(id,session.Project!.ProjectId);Assert.Equal(created,session.Project.CreatedAt);
    }
    [Fact] public void SaveAsPreservesIdentityAndOriginalFile()
    {
        var s=new ProjectSession(new ProjectStore());s.New("First","0.2.0");s.Save(FilePath("one.dtob"));var old=File.ReadAllBytes(FilePath("one.dtob"));var id=s.Project!.ProjectId;s.Rename("Second");s.Save(FilePath("two.dtob"));Assert.Equal(old,File.ReadAllBytes(FilePath("one.dtob")));Assert.Equal(id,s.Project!.ProjectId);Assert.Equal(FilePath("two.dtob"),s.CurrentFilePath);Assert.False(s.IsDirty);
    }
    [Fact] public void MovedFileUsesActualPathAndStaysClean()
    {
        var s=new ProjectSession(new ProjectStore());s.New("Moved","0.2.0");s.Save(FilePath("one.dtob"));var p=s.Project;File.Move(FilePath("one.dtob"),FilePath("moved.dtob"));s.Open(FilePath("moved.dtob"));Assert.Equal(p,s.Project);Assert.Equal(FilePath("moved.dtob"),s.CurrentFilePath);Assert.False(s.IsDirty);
    }
    [Theory][InlineData("{")][InlineData("{\"schemaVersion\":2}")][InlineData("{\"schemaVersion\":1}")][InlineData("null")][InlineData("{\"schemaVersion\":true}")][InlineData("{\"schemaVersion\":\"1\"}")]
    public void InvalidProjectCannotReplaceCurrentSession(string json)
    {
        var s=new ProjectSession(new ProjectStore());s.New("Keep","0.2.0");var p=s.Project;File.WriteAllText(FilePath("bad.dtob"),json);var error=Assert.Throws<InvalidDataException>(()=>s.Open(FilePath("bad.dtob")));Assert.True(LocalFileErrors.IsExpected(error));Assert.Equal(p,s.Project);Assert.True(s.IsDirty);
    }
    [Fact] public void NonexistentProjectFailsWithoutStateChange(){var s=new ProjectSession(new ProjectStore());s.New("Keep","0.2.0");Assert.Throws<FileNotFoundException>(()=>s.Open(FilePath("missing.dtob")));Assert.Equal("Keep",s.Project!.Name);}
    [Fact] public void FailedReplacePreservesPreviousFileAndSession()
    {
        var s=new ProjectSession(new ProjectStore());s.New("Before","0.2.0");s.Save(FilePath("one.dtob"));var bytes=File.ReadAllBytes(FilePath("one.dtob"));s.Rename("After");var p=s.Project;using(var locked=new FileStream(FilePath("one.dtob"),FileMode.Open,FileAccess.Read,FileShare.Read))Assert.Throws<IOException>(()=>s.Save(FilePath("one.dtob")));
        Assert.Equal(bytes,File.ReadAllBytes(FilePath("one.dtob")));Assert.Equal(p,s.Project);Assert.True(s.IsDirty);Assert.Equal(FilePath("one.dtob"),s.CurrentFilePath);Assert.Empty(Directory.GetFiles(root,"*.tmp"));
        Assert.Throws<DirectoryNotFoundException>(()=>s.Save(Path.Combine(root,"missing","save-as.dtob")));Assert.Equal(p,s.Project);Assert.Equal(FilePath("one.dtob"),s.CurrentFilePath);
    }
    [Fact] public void SettingsPersistAndCorruptionRetainsOriginal()
    {
        var store=new SettingsStore(FilePath("settings.json"));var s=AppSettings.Defaults(root) with {WorkingDirectory=root,LogLevel=LogLevel.Warning};store.Save(s);Assert.Equal(s,store.Load());File.WriteAllText(FilePath("settings.json"),"bad");Assert.Throws<InvalidDataException>(()=>store.Load());Assert.Equal("bad",File.ReadAllText(FilePath("settings.json")));
    }
    [Fact] public void RecentOrderingNormalizationLimitAndMissingArePersisted()
    {
        var r=new RecentStore(FilePath("recent.json"));for(var i=0;i<12;i++)r.Record(FilePath(i+".dtob"),i.ToString());r.Record(FilePath("11.dtob").ToUpperInvariant(),"Newest");Assert.Equal(10,r.Entries.Count);Assert.Equal("Newest",r.Entries[0].Name);Assert.True(r.Entries[0].IsMissing);Assert.Contains("File missing",r.Entries[0].Display);var loaded=new RecentStore(FilePath("recent.json"));loaded.Load();Assert.Equal(r.Entries,loaded.Entries);
    }
    [Theory][InlineData(UnsavedChoice.Cancel,false)][InlineData(UnsavedChoice.Discard,true)][InlineData(UnsavedChoice.Save,true)]
    public void UnsavedTransitionsRespectChoice(UnsavedChoice choice,bool expected)
    {
        var s=new ProjectSession(new ProjectStore());s.New("Dirty","0.2.0");var called=false;Assert.Equal(expected,s.CanLeave(choice,()=>{called=true;s.Save(FilePath("saved.dtob"));return true;}));Assert.Equal(choice==UnsavedChoice.Save,called);if(choice!=UnsavedChoice.Save)Assert.True(s.IsDirty);
    }
    [Fact] public void RecentFailureDoesNotUndoCompletedProjectSave()
    {
        var s=new ProjectSession(new ProjectStore());s.New("Saved","0.2.0");s.Save(FilePath("valid.dtob"));var r=new RecentStore(Path.Combine(root,"missing","recent.json"));Assert.Throws<DirectoryNotFoundException>(()=>r.Record(s.CurrentFilePath!,s.Project!.Name));Assert.False(s.IsDirty);Assert.Equal(FilePath("valid.dtob"),s.CurrentFilePath);Assert.Empty(r.Entries);
    }
    [Fact] public void CorruptRecentStateIsNotSilentlyOverwritten()
    {
        var path=FilePath("recent.json");File.WriteAllText(path,"{");var r=new RecentStore(path);Assert.Throws<InvalidDataException>(()=>r.Load());Assert.Equal("{",File.ReadAllText(path));Assert.Empty(r.Entries);
    }
    [Fact] public void CancelledOrFailedSaveBlocksTransition(){var s=new ProjectSession(new ProjectStore());s.New("Dirty","0.2.0");Assert.False(s.CanLeave(UnsavedChoice.Save,()=>false));Assert.True(s.IsDirty);}
    [Fact] public void LoggingFiltersFatalAndBoundsRetention()
    {
        var failures=new List<string>();for(var i=0;i<15;i++){var logger=new FileLogger(root,failures.Add){Minimum=LogLevel.Warning};logger.Write(LogLevel.Info,"filtered");logger.Write(LogLevel.Fatal,"crash",new InvalidOperationException("test fatal"));}
        var files=Directory.GetFiles(root,"desktop-*.log");Assert.Equal(10,files.Length);Assert.Empty(failures);Assert.All(files,f=>{var text=File.ReadAllText(f);Assert.Contains("FATAL",text);Assert.Contains("test fatal",text);Assert.DoesNotContain("filtered",text);});
    }
    [Fact] public void LoggingFailureUsesDirectFallback(){File.WriteAllText(FilePath("not-directory"),"x");var messages=new List<string>();var log=new FileLogger(FilePath("not-directory"),messages.Add);log.Write(LogLevel.Error,"test");Assert.Equal(2,messages.Count);}
    [Fact] public void CorruptLocalStateUsesRecoverableErrorPolicy()
    {
        var path=FilePath("bad.json");File.WriteAllText(path,"{");
        Assert.True(LocalFileErrors.IsExpected(Assert.Throws<InvalidDataException>(()=>new SettingsStore(path).Load())));
        Assert.True(LocalFileErrors.IsExpected(Assert.Throws<InvalidDataException>(()=>new RecentStore(path).Load())));
        Assert.False(LocalFileErrors.IsExpected(new InvalidOperationException("unexpected")));
        Assert.False(LocalFileErrors.IsExpected(new NullReferenceException("unexpected")));
    }
}
