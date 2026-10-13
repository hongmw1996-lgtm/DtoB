using System.Diagnostics;
using System.Text;
using DtoB.Desktop;
using DtoB.Project;
using Xunit;

namespace DtoB.Tests;

// Directory junctions do not require symbolic-link privilege or Developer Mode.
internal static class TestJunction
{
    public static void Create(string link,string target)
    {
        static string Quote(string value)=>"'"+value.Replace("'","''")+"'";
        var start=new ProcessStartInfo("powershell.exe") {UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
        foreach(var argument in new[]{"-NoProfile","-NonInteractive","-Command","$ErrorActionPreference='Stop'; New-Item -ItemType Junction -Path "+Quote(link)+" -Target "+Quote(target)+" | Out-Null"})start.ArgumentList.Add(argument);
        using var process=Process.Start(start)!;
        var output=process.StandardOutput.ReadToEnd();var error=process.StandardError.ReadToEnd();process.WaitForExit();
        Assert.True(process.ExitCode==0,output+error);
        Assert.True((File.GetAttributes(link)&FileAttributes.ReparsePoint)!=0);
    }
}

public sealed class Phase02ReviewFixTests:IDisposable
{
    private readonly string root=Path.Combine(Path.GetTempPath(),"DtoB-review-"+Guid.NewGuid());
    public Phase02ReviewFixTests()=>Directory.CreateDirectory(root);
    private string At(string name)=>Path.Combine(root,name);
    public void Dispose()=>Directory.Delete(root,true);
    private string Source(string name="source.dwg") {var path=At(name);File.WriteAllText(path,"AC1024"+new string(' ',70)+name,Encoding.ASCII);return path;}
    private ProjectSession Session(DrawingFileService? files=null)
    {
        var session=new ProjectSession(new ProjectStore(),files);session.New("Review fixes","0.3.0");session.Save(At("project.dtob"));return session;
    }
    private sealed class ProbeFiles:AssetFiles
    {
        public string? SourcePath;public bool AllowSource,ForbidContent;public int SourceProbes,Reads;public List<string> Probes=[];
        public FileProbe? ManagedOverride,SourceOverride;public ManualResetEventSlim? Release;public TaskCompletionSource? Entered;
        public override FileProbe Probe(string path)
        {
            Probes.Add(path);
            if(StringComparer.OrdinalIgnoreCase.Equals(path,SourcePath))
            {
                SourceProbes++;if(!AllowSource)throw new InvalidOperationException("Source IO is forbidden in normal project use.");
                Entered?.TrySetResult();Release?.Wait();return SourceOverride??new(false,false,0,0);
            }
            return ManagedOverride??base.Probe(path);
        }
        public override Stream OpenRead(string path){Reads++;if(ForbidContent)throw new InvalidOperationException("Content IO forbidden.");return base.OpenRead(path);}
    }
    private sealed class NoHash:IContentHasher
    {
        public int Calls;
        public Task<Fingerprint> HashAsync(Stream input,Stream? output,CancellationToken cancellationToken){Calls++;throw new InvalidOperationException("Hash forbidden.");}
    }
    private async Task<(ProjectSession Session,DrawingFile Drawing)> Fixture(ProbeFiles io,NoHash hash,bool unc=false)
    {
        var original=Session();var drawing=(await original.AddDrawingAsync(Source())).Drawing;original.Save(original.CurrentFilePath!);
        if(unc)
        {
            drawing=drawing with {SourceFilePath=@"\\unreachable-dtob-test.invalid\share\source.dwg"};
            new ProjectStore().Save(original.CurrentFilePath!,original.Project! with {Drawings=[drawing]});
        }
        io.SourcePath=drawing.SourceFilePath;io.ForbidContent=true;
        return (new ProjectSession(new ProjectStore(),new DrawingFileService(io,hash)),drawing);
    }

    [Fact] public async Task JunctionParentAllowsEmptySaveOpenRegistrationAndSaveAs()
    {
        Directory.CreateDirectory(At("physical"));TestJunction.Create(At("link"),At("physical"));
        try
        {
            var s=new ProjectSession(new ProjectStore());s.New("Junction parent","0.3.0");
            var manifest=At("link/project.dtob");s.Save(manifest);s.Open(manifest);Assert.Empty(s.Project!.Drawings!);Assert.False(s.IsDirty);
            var drawing=(await s.AddDrawingAsync(Source())).Drawing;s.Save(manifest);s.Open(manifest);
            Assert.Equal(Availability.Ready,s.Runtime[drawing.DrawingId].Availability);
            Directory.CreateDirectory(At("link/destination"));await s.SaveAsync(At("link/destination/copy.dtob"));s.Open(s.CurrentFilePath!);
            Assert.Equal(drawing,Assert.Single(s.Project!.Drawings!));Assert.Equal(Availability.Ready,s.Runtime[drawing.DrawingId].Availability);Assert.False(s.IsDirty);
        }
        finally{Directory.Delete(At("link"));}
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task ReparseWithinAssetTreeIsRejectedWithoutWritingOutside(bool nested)
    {
        var s=Session();var drawing=(await s.AddDrawingAsync(Source())).Drawing;s.Save(s.CurrentFilePath!);
        var assets=At($"{s.Project!.ProjectId:N}.dtob-assets");var link=nested?Path.Combine(assets,"drawings",drawing.DrawingId.ToString("N")):assets;
        Directory.Move(link,At("preserved"));Directory.CreateDirectory(At("outside"));TestJunction.Create(link,At("outside"));
        try
        {
            Assert.Throws<InvalidDataException>(()=>AssetPaths.Resolve(s.CurrentFilePath!,s.Project.ProjectId,drawing));
            s.Open(s.CurrentFilePath!);Assert.Equal(Availability.ManagedUnreadable,s.Runtime[drawing.DrawingId].Availability);
            await Assert.ThrowsAsync<InvalidDataException>(()=>s.AddDrawingAsync(Source("replace.dwg"),drawing.DrawingId));
            Assert.Empty(Directory.GetFileSystemEntries(At("outside")));Assert.False(s.IsDirty);
        }
        finally{Directory.Delete(link);Directory.Move(At("preserved"),link);}
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task OpenRefreshAndSelectionNeverProbeLocalOrUncSource(bool unc)
    {
        var io=new ProbeFiles();var hash=new NoHash();var (s,d)=await Fixture(io,hash,unc);s.Open(At("project.dtob"));
        var vm=new ShellViewModel(s);vm.Refresh();vm.SelectedDrawing=s.Project!.Drawings![0];vm.Refresh();
        Assert.False(vm.CanRevealSource);Assert.True(vm.CanRevealManaged);Assert.Contains("SourceUnchecked",vm.DrawingMetadata);Assert.Contains("SourceUnchecked",vm.DrawingSummary);
        Assert.All(vm.DrawingRows,row=>Assert.Contains("SourceUnchecked",row.StatusText));Assert.Equal(0,io.SourceProbes);Assert.Equal(0,io.Reads);Assert.Equal(0,hash.Calls);
        Assert.All(io.Probes,path=>Assert.Equal(AssetPaths.Resolve(s.CurrentFilePath!,s.Project!.ProjectId,d),path));Assert.False(s.IsDirty);
    }

    [Fact] public async Task MissingManagedAssetNeverFallsBackToPresentSource()
    {
        var io=new ProbeFiles();var hash=new NoHash();var(s,d)=await Fixture(io,hash);Assert.True(File.Exists(d.SourceFilePath));
        var p=new ProjectStore().Load(At("project.dtob"));File.Delete(AssetPaths.Resolve(At("project.dtob"),p.ProjectId,d));s.Open(At("project.dtob"));
        Assert.Equal(Availability.ManagedMissing,s.Runtime[d.DrawingId].Availability);await Assert.ThrowsAsync<IOException>(()=>s.VerifyIntegrityAsync(d.DrawingId));
        Assert.Equal(0,io.SourceProbes);Assert.Equal(0,io.Reads);Assert.Equal(0,hash.Calls);
    }

    [Theory][InlineData(true,true,SourceAvailability.SourcePresent)][InlineData(false,false,SourceAvailability.SourceMissing)][InlineData(true,false,SourceAvailability.SourceUnreadable)]
    public async Task ExplicitSourceCheckPublishesOnlyRuntimeStatus(bool exists,bool readable,SourceAvailability expected)
    {
        var io=new ProbeFiles();var hash=new NoHash();var(s,d)=await Fixture(io,hash,true);s.Open(At("project.dtob"));var project=s.Project;var bytes=File.ReadAllBytes(s.CurrentFilePath!);
        io.AllowSource=true;io.SourceOverride=new(exists,readable,0,0);Assert.True(await s.RefreshSourceStatusAsync(d.DrawingId));
        var vm=new ShellViewModel(s);vm.SelectedDrawing=d;vm.Refresh();Assert.Equal(expected,s.Runtime[d.DrawingId].SourceStatus);Assert.Equal(expected==SourceAvailability.SourcePresent,vm.CanRevealSource);
        Assert.Contains(expected.ToString(),vm.DrawingSummary);Assert.Equal(1,io.SourceProbes);Assert.Equal(0,io.Reads);Assert.Equal(0,hash.Calls);Assert.Same(project,s.Project);Assert.False(s.IsDirty);Assert.Equal(bytes,File.ReadAllBytes(s.CurrentFilePath!));
        io.AllowSource=false;s.Open(s.CurrentFilePath!);Assert.Equal(SourceAvailability.SourceUnchecked,s.Runtime[d.DrawingId].SourceStatus);Assert.Equal(1,io.SourceProbes);
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task PendingSourceCheckDoesNotBlockCallerAndRejectsCancelledOrStaleResult(bool cancel)
    {
        var io=new ProbeFiles();var hash=new NoHash();var(s,d)=await Fixture(io,hash,true);s.Open(At("project.dtob"));
        using var release=new ManualResetEventSlim();using var ct=new CancellationTokenSource();io.Release=release;io.Entered=new(TaskCreationOptions.RunContinuationsAsynchronously);io.AllowSource=true;io.SourceOverride=new(true,true,0,0);
        var pending=s.RefreshSourceStatusAsync(d.DrawingId,ct.Token);
        try
        {
            await io.Entered.Task.WaitAsync(TimeSpan.FromSeconds(15));Assert.False(pending.IsCompleted);
            if(cancel){ct.Cancel();await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>pending);}
            else{s.Open(s.CurrentFilePath!);release.Set();Assert.False(await pending);}
            Assert.Equal(SourceAvailability.SourceUnchecked,s.Runtime[d.DrawingId].SourceStatus);Assert.False(s.IsDirty);
        }
        finally{release.Set();}
    }

    [Theory][InlineData(false,false,0L,"ManagedMissing")][InlineData(true,false,0L,"ManagedUnreadable")][InlineData(true,true,8_000_000_000L,"SizeChanged (unverified)")]
    public async Task RowsAndSummaryExposeProblemsWithoutSelectionOrContentReads(bool exists,bool readable,long size,string expected)
    {
        var io=new ProbeFiles();var hash=new NoHash();var(s,d)=await Fixture(io,hash);io.ManagedOverride=new(exists,readable,size,0);s.Open(At("project.dtob"));
        var vm=new ShellViewModel(s);Assert.Null(vm.SelectedDrawing);Assert.Contains(expected,Assert.Single(vm.DrawingRows).StatusText);Assert.Contains(expected,vm.DrawingSummary);
        vm.Refresh();Assert.Null(vm.SelectedDrawing);Assert.Contains(expected,Assert.Single(vm.DrawingRows).StatusText);Assert.Contains(expected,vm.DrawingSummary);
        Assert.Equal(0,io.SourceProbes);Assert.Equal(0,io.Reads);Assert.Equal(0,hash.Calls);Assert.Equal(Integrity.NotVerified,s.Runtime[d.DrawingId].Integrity);Assert.False(s.IsDirty);
    }

    [Fact] public async Task RefreshRestoresSelectionAfterItemsSourceBindingClearsIt()
    {
        var s=Session();var d=(await s.AddDrawingAsync(Source())).Drawing;var vm=new ShellViewModel(s);vm.SelectedDrawing=d;
        vm.PropertyChanged+=(_,e)=>{if(e.PropertyName==nameof(vm.DrawingRows))vm.SelectedDrawing=null;};
        vm.Refresh();Assert.Equal(d,vm.SelectedDrawing);Assert.Contains(d.DrawingId.ToString(),vm.DrawingMetadata);
        var replacement=(await s.AddDrawingAsync(Source("replacement.dwg"),d.DrawingId)).Drawing;vm.Refresh();Assert.Equal(replacement,vm.SelectedDrawing);
        s.RemoveDrawing(d.DrawingId);vm.Refresh();Assert.Null(vm.SelectedDrawing);
    }
}
