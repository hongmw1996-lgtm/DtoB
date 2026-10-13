using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DtoB.Project;
using DtoB.Desktop;
using Xunit;
namespace DtoB.Tests;
public sealed class DrawingManagementTests:IDisposable
{
    private readonly string root=Path.Combine(Path.GetTempPath(),"DtoB-drawings-"+Guid.NewGuid());
    public DrawingManagementTests()=>Directory.CreateDirectory(root);
    private string PathTo(string name)=>Path.Combine(root,name);
    private string Source(string name="도면.dwg",string suffix="one") {var p=PathTo(name);Directory.CreateDirectory(Path.GetDirectoryName(p)!);File.WriteAllText(p,name.EndsWith(".dxf",StringComparison.OrdinalIgnoreCase)?"0\nSECTION\n2\nHEADER\n0\nENDSEC\n0\nEOF\n"+suffix:"AC1024"+new string(' ',64)+suffix,Encoding.ASCII);return p;}
    private ProjectSession Session(DrawingFileService? files=null,IProjectStore? store=null){var s=new ProjectSession(store??new ProjectStore(),files);s.New("Drawings","0.3.0");s.Save(PathTo("project.dtob"));return s;}
    private string Managed(ProjectSession s,DrawingFile d)=>AssetPaths.Resolve(s.CurrentFilePath!,s.Project!.ProjectId,d);
    public void Dispose()=>Directory.Delete(root,true);
    private sealed class SpyFiles:AssetFiles
    {
        public int Reads;public bool ForbidReads,FailPublish,FailFlush,FailDelete;public long? ManagedSize;public List<string> Probes=[];
        public override Stream OpenRead(string path){Reads++;if(ForbidReads)throw new InvalidOperationException("Content read forbidden.");return base.OpenRead(path);}
        protected override Stream OpenAccessProbe(string path)=>new NoContentReadStream(base.OpenAccessProbe(path));
        public override FileProbe Probe(string path){Probes.Add(path);var p=base.Probe(path);return ManagedSize.HasValue&&path.Contains(".dtob-assets")?p with {Length=ManagedSize.Value}:p;}
        public override void Publish(string temporary,string target){if(FailPublish)throw new IOException("Injected publish failure");base.Publish(temporary,target);}
        public override void Flush(Stream stream){if(FailFlush)throw new IOException("Injected flush failure");base.Flush(stream);}
        public override void Delete(string path){if(FailDelete)throw new IOException("Injected cleanup failure");base.Delete(path);}
    }
    // Exercises the production metadata probe while rejecting every content-read API.
    private sealed class NoContentReadStream(Stream inner):Stream
    {
        public override bool CanRead=>inner.CanRead;
        public override bool CanSeek=>inner.CanSeek;
        public override bool CanWrite=>false;
        public override long Length=>inner.Length;
        public override long Position {get=>inner.Position;set=>inner.Position=value;}
        private static InvalidOperationException Forbidden()=>new("Availability probe must never read drawing content.");
        public override int Read(byte[] buffer,int offset,int count)=>throw Forbidden();
        public override int Read(Span<byte> buffer)=>throw Forbidden();
        public override int ReadByte()=>throw Forbidden();
        public override Task<int> ReadAsync(byte[] buffer,int offset,int count,CancellationToken cancellationToken)=>throw Forbidden();
        public override ValueTask<int> ReadAsync(Memory<byte> buffer,CancellationToken cancellationToken=default)=>throw Forbidden();
        public override long Seek(long offset,SeekOrigin origin)=>inner.Seek(offset,origin);
        public override void Flush()=>inner.Flush();
        public override void SetLength(long value)=>throw new NotSupportedException();
        public override void Write(byte[] buffer,int offset,int count)=>throw new NotSupportedException();
        protected override void Dispose(bool disposing){if(disposing)inner.Dispose();base.Dispose(disposing);}
    }
    private sealed class SpyHasher:IContentHasher
    {
        public int Calls;public bool Forbid;public TaskCompletionSource? Entered,Release;
        public async Task<Fingerprint> HashAsync(Stream input,Stream? output,CancellationToken token){Calls++;if(Forbid)throw new InvalidOperationException("Hash forbidden.");if(Entered!=null){Entered.TrySetResult();await Release!.Task.WaitAsync(token);}return await new ContentHasher().HashAsync(input,output,token);}
    }
    [Fact] public void LegacyMigrationIsCleanAndOnlyExplicitSaveWritesV2()
    {
        var path=PathTo("legacy.dtob");var id=Guid.NewGuid();var created=DateTimeOffset.UtcNow;var json=JsonSerializer.Serialize(new {schemaVersion=1,projectId=id,name="Legacy",createdAt=created,modifiedAt=created,dtoBVersion="0.2.0",lastKnownPath="informational"});File.WriteAllText(path,json);
        var s=Session();s.Open(path);Assert.False(s.IsDirty);Assert.Equal(id,s.Project!.ProjectId);Assert.Equal(created,s.Project.CreatedAt);Assert.Empty(s.Project.Drawings!);Assert.Equal(json,File.ReadAllText(path));s.Save(path);Assert.Contains("\"schemaVersion\": 2",File.ReadAllText(path));Assert.Contains("\"drawings\": []",File.ReadAllText(path));
        Assert.Throws<InvalidDataException>(()=>LocalJson.Read<ProjectFile>(path)); // strict old state/version-1 reader cannot accept v2
    }
    [Theory][InlineData(".dwg")][InlineData(".dxf")]
    public async Task RegistrationPreservesBytesAndIdentityThroughReload(string extension)
    {
        var source=Source("도면"+extension);var original=File.ReadAllBytes(source);var s=Session();var d=(await s.AddDrawingAsync(source)).Drawing;Assert.NotEqual(d.DrawingId,d.RevisionId);Assert.True(s.IsDirty);Assert.Equal(Convert.ToHexString(SHA256.HashData(original)),d.FileHash);Assert.Equal(original,File.ReadAllBytes(Managed(s,d)));Assert.Equal(original,File.ReadAllBytes(source));Assert.Equal(Detection.PendingReader,d.UnitDetection);s.Save(s.CurrentFilePath!);var facts=JsonSerializer.Serialize(d);s.Close();s.Open(PathTo("project.dtob"));Assert.Equal(facts,JsonSerializer.Serialize(s.Project!.Drawings![0]));Assert.False(s.IsDirty);Assert.Equal(Integrity.NotVerified,s.Runtime[d.DrawingId].Integrity);
    }
    [Theory][InlineData("bad.txt","AC1024 payload")][InlineData("bad.dwg","")][InlineData("bad.dwg","invalid contents")][InlineData("bad.dwg","AC1024")][InlineData("bad.dxf","AutoCAD Binary DXF\r\n\u001a\0")][InlineData("bad.dxf","nonsense")]
    public async Task InvalidImportPreservesSessionAndCleansTemp(string name,string contents)
    {
        var s=Session();var p=s.Project;File.WriteAllText(PathTo(name),contents);await Assert.ThrowsAsync<InvalidDataException>(()=>s.AddDrawingAsync(PathTo(name)));Assert.Same(p,s.Project);Assert.False(s.IsDirty);Assert.Empty(Directory.GetFiles(root,"*.tmp",SearchOption.AllDirectories));
    }
    [Fact] public async Task MissingAndLockedSourcesAreExpectedFailures()
    {
        var s=Session();await Assert.ThrowsAsync<FileNotFoundException>(()=>s.AddDrawingAsync(PathTo("missing.dwg")));var src=Source();using(var locked=new FileStream(src,FileMode.Open,FileAccess.Read,FileShare.None))await Assert.ThrowsAsync<IOException>(()=>s.AddDrawingAsync(src));Assert.Empty(s.Project!.Drawings!);Assert.False(s.IsDirty);
    }
    [Fact] public async Task DuplicatesUseHashAndSourcePathWithoutSilentNewDrawings()
    {
        var s=Session();var source=Source();var first=(await s.AddDrawingAsync(source)).Drawing;s.Save(s.CurrentFilePath!);var same=await s.AddDrawingAsync(source.ToUpperInvariant());Assert.False(same.Changed);Assert.False(s.IsDirty);
        var other=PathTo("same-bytes.dwg");File.Copy(source,other);Assert.False((await s.AddDrawingAsync(other)).Changed);Assert.Single(s.Project!.Drawings!);
        Source(suffix:"changed");await Assert.ThrowsAsync<InvalidDataException>(()=>s.AddDrawingAsync(source));Assert.False(s.IsDirty);
        var distinct=Source("other/도면.dwg","different");var added=await s.AddDrawingAsync(distinct);Assert.NotEqual(first.DrawingId,added.Drawing.DrawingId);Assert.Equal(first.OriginalFileName,added.Drawing.OriginalFileName);
    }
    [Fact] public async Task ReplacePreservesLogicalIdChangesRevisionAndRetainsOldBytes()
    {
        var s=Session();var d=(await s.AddDrawingAsync(Source())).Drawing;s.Save(s.CurrentFilePath!);var oldPath=Managed(s,d);var oldBytes=File.ReadAllBytes(oldPath);Assert.False((await s.AddDrawingAsync(d.SourceFilePath,d.DrawingId)).Changed);Assert.False(s.IsDirty);
        var replacement=Source("replacement.dwg","changed");var next=(await s.AddDrawingAsync(replacement,d.DrawingId)).Drawing;Assert.Equal(d.DrawingId,next.DrawingId);Assert.NotEqual(d.RevisionId,next.RevisionId);Assert.NotEqual(d.FileHash,next.FileHash);Assert.Equal(oldBytes,File.ReadAllBytes(oldPath));Assert.True(s.IsDirty);
        var second=(await s.AddDrawingAsync(Source("another.dwg","other"))).Drawing;await Assert.ThrowsAsync<InvalidDataException>(()=>s.AddDrawingAsync(second.SourceFilePath,next.DrawingId));
    }
    [Fact] public async Task RemoveAndDiscardKeepRevisionFiles()
    {
        var s=Session();var d=(await s.AddDrawingAsync(Source())).Drawing;s.Save(s.CurrentFilePath!);var path=Managed(s,d);s.RemoveDrawing(d.DrawingId);Assert.Empty(s.Project!.Drawings!);Assert.True(File.Exists(path));Assert.True(File.Exists(d.SourceFilePath));Assert.True(s.IsDirty);s.Open(s.CurrentFilePath!);Assert.Equal(d.DrawingId,Assert.Single(s.Project!.Drawings!).DrawingId);Assert.False(s.IsDirty);
    }
    [Theory][InlineData(70L)][InlineData(8_000_000_000L)]
    public async Task ActualOpenAndViewModelRefreshNeverReadOrHashManagedAssets(long size)
    {
        var io=new SpyFiles();var hash=new SpyHasher();var files=new DrawingFileService(io,hash);var s=Session(files);await s.AddDrawingAsync(Source());s.Save(s.CurrentFilePath!);var path=s.CurrentFilePath!;var d=s.Project!.Drawings![0];
        io.Reads=hash.Calls=0;io.ForbidReads=hash.Forbid=true;io.ManagedSize=size;io.Probes.Clear();s.Open(path);var vm=new ShellViewModel(s);vm.SelectedDrawing=s.Project!.Drawings![0];vm.Refresh();vm.Refresh();Assert.Equal(0,io.Reads);Assert.Equal(0,hash.Calls);Assert.Equal(3,io.Probes.Count);Assert.All(io.Probes,p=>Assert.Equal(Managed(s,d),p));Assert.Equal(Integrity.NotVerified,s.Runtime[d.DrawingId].Integrity);Assert.False(s.IsDirty);
    }
    [Fact] public async Task MissingSourceAndManagedAreDetectedWithoutHashing()
    {
        var io=new SpyFiles();var hash=new SpyHasher();var s=Session(new(io,hash));var d=(await s.AddDrawingAsync(Source())).Drawing;s.Save(s.CurrentFilePath!);File.Delete(d.SourceFilePath);io.ForbidReads=hash.Forbid=true;io.Reads=hash.Calls=0;s.Open(s.CurrentFilePath!);Assert.Equal(SourceAvailability.SourceUnchecked,s.Runtime[d.DrawingId].SourceStatus);Assert.True(await s.RefreshSourceStatusAsync(d.DrawingId));Assert.True(s.Runtime[d.DrawingId].SourceMissing);Assert.Equal(Availability.Ready,s.Runtime[d.DrawingId].Availability);File.Delete(Managed(s,d));s.RefreshDrawings();Assert.Equal(Availability.ManagedMissing,s.Runtime[d.DrawingId].Availability);Assert.Equal(0,io.Reads);Assert.Equal(0,hash.Calls);Assert.False(s.IsDirty);
    }
    [Fact] public async Task ExplicitSha256IsTheOnlySourceOfHashMismatch()
    {
        var io=new SpyFiles();var hash=new SpyHasher();var s=Session(new(io,hash));var d=(await s.AddDrawingAsync(Source())).Drawing;s.Save(s.CurrentFilePath!);io.Reads=hash.Calls=0;Assert.True(await s.VerifyIntegrityAsync(d.DrawingId));Assert.Equal(1,hash.Calls);Assert.Equal(1,io.Reads);Assert.Equal(Integrity.Verified,s.Runtime[d.DrawingId].Integrity);
        var path=Managed(s,d);var bytes=File.ReadAllBytes(path);bytes[^1]^=1;File.WriteAllBytes(path,bytes);s.RefreshDrawings();Assert.Equal(Integrity.NotVerified,s.Runtime[d.DrawingId].Integrity);Assert.True(await s.VerifyIntegrityAsync(d.DrawingId));Assert.Equal(Integrity.HashMismatch,s.Runtime[d.DrawingId].Integrity);Assert.False(s.IsDirty);s.Open(s.CurrentFilePath!);Assert.Equal(Integrity.NotVerified,s.Runtime[d.DrawingId].Integrity);
    }
    [Fact] public async Task CancellationAndStaleVerifyNeverPublishVerdicts()
    {
        var hash=new SpyHasher();var s=Session(new(contentHasher:hash));var d=(await s.AddDrawingAsync(Source())).Drawing;s.Save(s.CurrentFilePath!);
        hash.Entered=new(TaskCreationOptions.RunContinuationsAsynchronously);hash.Release=new(TaskCreationOptions.RunContinuationsAsynchronously);using var ct=new CancellationTokenSource();var pending=s.VerifyIntegrityAsync(d.DrawingId,ct.Token);await hash.Entered.Task;Assert.False(pending.IsCompleted);ct.Cancel();await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>pending);Assert.Equal(Integrity.NotVerified,s.Runtime[d.DrawingId].Integrity);
        hash.Entered=new(TaskCreationOptions.RunContinuationsAsynchronously);hash.Release=new(TaskCreationOptions.RunContinuationsAsynchronously);pending=s.VerifyIntegrityAsync(d.DrawingId);await hash.Entered.Task;s.Open(s.CurrentFilePath!);hash.Release.SetResult();Assert.False(await pending);Assert.Equal(Integrity.NotVerified,s.Runtime[d.DrawingId].Integrity);Assert.False(s.IsDirty);
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task FailedPublishOrFlushPreservesAddAndReplaceSession(bool flush)
    {
        var io=new SpyFiles();var s=Session(new(io));var d=(await s.AddDrawingAsync(Source())).Drawing;s.Save(s.CurrentFilePath!);var project=s.Project;var bytes=File.ReadAllBytes(Managed(s,d));io.FailFlush=flush;io.FailPublish=!flush;
        await Assert.ThrowsAsync<IOException>(()=>s.AddDrawingAsync(Source("new.dwg","new")));await Assert.ThrowsAsync<IOException>(()=>s.AddDrawingAsync(Source("replace.dwg","replace"),d.DrawingId));Assert.Same(project,s.Project);Assert.False(s.IsDirty);Assert.Equal(bytes,File.ReadAllBytes(Managed(s,d)));Assert.Empty(Directory.GetFiles(root,"*.tmp",SearchOption.AllDirectories));
    }
    private sealed class FailingStore:IProjectStore {public bool Fail;private readonly ProjectStore inner=new();public ProjectFile Load(string path)=>inner.Load(path);public void Save(string path,ProjectFile p){if(Fail)throw new IOException("Manifest save failed");inner.Save(path,p);}}
    [Fact] public async Task SaveAsCopiesAssetsAndManifestFailureRetainsOriginalStateAndRecent()
    {
        var store=new FailingStore();var io=new SpyFiles();var s=Session(new(io),store);var d=(await s.AddDrawingAsync(Source())).Drawing;s.Save(s.CurrentFilePath!);var original=s.CurrentFilePath!;var bytes=File.ReadAllBytes(original);var p=s.Project;var recent=new RecentStore(PathTo("recent.json"));recent.Record(original,p!.Name);var recentBytes=File.ReadAllBytes(PathTo("recent.json"));Directory.CreateDirectory(PathTo("destination"));var target=PathTo("destination/new.dtob");
        io.FailPublish=true;await Assert.ThrowsAsync<IOException>(()=>s.SaveAsync(target));io.FailPublish=false;store.Fail=true;await Assert.ThrowsAsync<IOException>(()=>s.SaveAsync(target));Assert.Equal(original,s.CurrentFilePath);Assert.Same(p,s.Project);Assert.False(s.IsDirty);Assert.Equal(bytes,File.ReadAllBytes(original));Assert.Equal(recentBytes,File.ReadAllBytes(PathTo("recent.json")));Assert.False(File.Exists(target));store.Fail=false;await s.SaveAsync(target);Assert.Equal(Path.GetFullPath(target),s.CurrentFilePath);Assert.Equal(d.FileHash,Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Managed(s,d)))));Assert.Equal(bytes,File.ReadAllBytes(original));Assert.Empty(Directory.GetFiles(root,"*.tmp",SearchOption.AllDirectories));
    }
    [Fact] public async Task UnsafePathsAndReparseDestinationsAreRejected()
    {
        var s=Session();var d=(await s.AddDrawingAsync(Source())).Drawing;foreach(var path in new[]{"../outside.dwg",PathTo("outside.dwg"),"C:/outside.dwg",d.ManagedFilePath.Replace('/', '\\')})Assert.Throws<InvalidDataException>(()=>AssetPaths.Resolve(s.CurrentFilePath!,s.Project!.ProjectId,d with {ManagedFilePath=path}));
        s.Save(s.CurrentFilePath!);var assets=PathTo($"{s.Project!.ProjectId:N}.dtob-assets");Directory.Move(assets,PathTo("original-assets"));Directory.CreateDirectory(PathTo("outside"));TestJunction.Create(assets,PathTo("outside"));try{await Assert.ThrowsAsync<InvalidDataException>(()=>s.AddDrawingAsync(Source("next.dwg","next")));}finally{Directory.Delete(assets);Directory.Move(PathTo("original-assets"),assets);}
        await Assert.ThrowsAsync<InvalidDataException>(()=>s.SaveAsync(Source("external.dwg")));Assert.True(File.Exists(d.SourceFilePath));
    }
    [Fact] public async Task MissingCollectionUnknownFieldsAndDuplicateDrawingInvariantsFail()
    {
        var s=Session();var d=(await s.AddDrawingAsync(Source())).Drawing;var p=s.Project!;Assert.Throws<InvalidDataException>(()=>(p with {Drawings=null}).Validate());Assert.Throws<InvalidDataException>(()=>(p with {Drawings=[d,d]}).Validate());Assert.Throws<InvalidDataException>(()=>(p with {Drawings=[d with {FileHash="bad"}]}).Validate());Assert.Throws<InvalidDataException>(()=>(p with {Drawings=[d with {RevisionId=Guid.Empty}]}).Validate());
        var path=PathTo("invalid.dtob");File.WriteAllText(path,JsonSerializer.Serialize(p,LocalJson.Options).Replace("\"schemaVersion\": 2","\"schemaVersion\": 3"));await Assert.ThrowsAsync<InvalidDataException>(()=>Task.Run(()=>s.Open(path)));Assert.Same(p,s.Project);
    }
    [Fact] public async Task CancelledRegistrationCleansTempAndKeepsDirtyState()
    {
        var hash=new SpyHasher {Entered=new(TaskCreationOptions.RunContinuationsAsynchronously),Release=new(TaskCreationOptions.RunContinuationsAsynchronously)};
        var s=Session(new(contentHasher:hash));s.Rename("Unsaved rename");var before=s.Project;using var cancel=new CancellationTokenSource();var pending=s.AddDrawingAsync(Source(),cancellationToken:cancel.Token);await hash.Entered.Task;cancel.Cancel();await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>pending);Assert.Same(before,s.Project);Assert.True(s.IsDirty);Assert.Empty(Directory.GetFiles(root,"*.tmp",SearchOption.AllDirectories));
    }
    [Fact] public async Task MovedProjectPairAndMissingSizeWarningStayCleanWithoutHashing()
    {
        var io=new SpyFiles();var hash=new SpyHasher();var s=Session(new(io,hash));var d=(await s.AddDrawingAsync(Source())).Drawing;s.Save(s.CurrentFilePath!);var json=File.ReadAllBytes(s.CurrentFilePath!);Directory.CreateDirectory(PathTo("moved"));var newPath=PathTo("moved/project-renamed.dtob");File.WriteAllBytes(newPath,json);Directory.Move(PathTo($"{s.Project!.ProjectId:N}.dtob-assets"),PathTo($"moved/{s.Project.ProjectId:N}.dtob-assets"));io.ForbidReads=hash.Forbid=true;s.Open(newPath);Assert.Equal(Availability.Ready,s.Runtime[d.DrawingId].Availability);Assert.Equal(d.ManagedFilePath,s.Project!.Drawings![0].ManagedFilePath);Assert.False(s.IsDirty);io.ManagedSize=d.FileSize+1;s.RefreshDrawings();Assert.True(s.Runtime[d.DrawingId].SizeChanged);Assert.Equal(Integrity.NotVerified,s.Runtime[d.DrawingId].Integrity);Assert.False(s.IsDirty);
    }
    [Fact] public async Task UnreadableManagedAssetAndMissingVerifyDoNotHash()
    {
        var io=new SpyFiles();var hash=new SpyHasher();var s=Session(new(io,hash));var d=(await s.AddDrawingAsync(Source())).Drawing;s.Save(s.CurrentFilePath!);var path=Managed(s,d);io.Reads=hash.Calls=0;
        using(var locked=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.None)){s.RefreshDrawings();Assert.Equal(Availability.ManagedUnreadable,s.Runtime[d.DrawingId].Availability);await Assert.ThrowsAsync<IOException>(()=>s.VerifyIntegrityAsync(d.DrawingId));}
        File.Delete(path);await Assert.ThrowsAsync<IOException>(()=>s.VerifyIntegrityAsync(d.DrawingId));Assert.Equal(Integrity.NotVerified,s.Runtime[d.DrawingId].Integrity);Assert.Equal(0,io.Reads);Assert.Equal(0,hash.Calls);Assert.False(s.IsDirty);
    }
    [Theory][InlineData(".dwg")][InlineData(".dxf")]
    public async Task ActualControlledFixtureRegistersWithoutInterpretingUnits(string extension)
    {
        var source=PathTo("Controlled"+extension);File.Copy(Path.Combine(AppContext.BaseDirectory,"fixtures","phase02","DtoB_TEST_2STORY_R2018"+extension),source);var s=Session();var d=(await s.AddDrawingAsync(source)).Drawing;Assert.Equal(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(source))),d.FileHash);Assert.Equal(Detection.PendingReader,d.UnitDetection);if(extension==".dwg")Assert.Equal("AC1024",d.DetectedDwgVersion);Assert.True(await s.VerifyIntegrityAsync(d.DrawingId));Assert.Equal(Integrity.Verified,s.Runtime[d.DrawingId].Integrity);
    }
    [Theory][InlineData("missing")][InlineData("null")][InlineData("unknown")]
    public void StrictV2EnvelopeFailuresPreservePreviousSession(string kind)
    {
        var s=Session();var p=s.Project;
        var json=System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(p,LocalJson.Options))!.AsObject();
        if(kind=="missing")Assert.True(json.Remove("drawings"));
        else if(kind=="null")json["drawings"]=null;
        else json["future"]=true;
        File.WriteAllText(PathTo("invalid.dtob"),json.ToJsonString());Assert.Throws<InvalidDataException>(()=>s.Open(PathTo("invalid.dtob")));Assert.Same(p,s.Project);Assert.False(s.IsDirty);
    }
    [Fact] public async Task CleanupFailureIsReportedWithoutHidingRegistrationFailure()
    {
        var io=new SpyFiles {FailFlush=true,FailDelete=true};var warnings=new List<string>();var s=Session(new(io,cleanupWarning:warnings.Add));var previous=s.Project;
        var error=await Assert.ThrowsAsync<IOException>(()=>s.AddDrawingAsync(Source()));
        Assert.Equal("Injected flush failure",error.Message);Assert.Contains("cleanup failed",Assert.Single(warnings));Assert.Same(previous,s.Project);Assert.False(s.IsDirty);
    }
    [Fact] public async Task OrdinarySaveDoesNotHashAndKnownMismatchPreventsPublishing()
    {
        var io=new SpyFiles();var hash=new SpyHasher();var s=Session(new(io,hash));var d=(await s.AddDrawingAsync(Source())).Drawing;s.Save(s.CurrentFilePath!);var path=s.CurrentFilePath!;
        io.ForbidReads=hash.Forbid=true;io.Reads=hash.Calls=0;s.Rename("Metadata edit");await s.SaveAsync(path);Assert.Equal(0,io.Reads);Assert.Equal(0,hash.Calls);
        io.ForbidReads=hash.Forbid=false;var managed=Managed(s,d);var bytes=File.ReadAllBytes(managed);bytes[^1]^=1;File.WriteAllBytes(managed,bytes);await s.VerifyIntegrityAsync(d.DrawingId);var manifest=File.ReadAllBytes(path);var previous=s.Project;
        await Assert.ThrowsAsync<InvalidDataException>(()=>s.SaveAsync(path));Assert.Equal(manifest,File.ReadAllBytes(path));Assert.Same(previous,s.Project);Assert.False(s.IsDirty);
    }
}

