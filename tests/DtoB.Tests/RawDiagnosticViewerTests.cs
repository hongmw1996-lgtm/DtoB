using System.Collections;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DtoB.Cad.Dxf;
using DtoB.Cad.Raw;
using DtoB.Desktop.Diagnostics;
using DtoB.Project;
using Xunit;

namespace DtoB.Tests;

public class RawDiagnosticViewerTests
{
    private const string Hash = "2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC";
    private const string Header = "0\nSECTION\n2\nHEADER\n9\n$ACADVER\n1\nAC1032\n0\nENDSEC\n";
    private const string Point = "0\nPOINT\n5\nA\n330\n0\n10\n1\n20\n2\n";
    private static string Root()
    { var d = new DirectoryInfo(AppContext.BaseDirectory); while (d != null && !File.Exists(Path.Combine(d.FullName, "DtoB.sln"))) d = d.Parent; return d!.FullName; }
    private static string Controlled => Path.Combine(Root(), "datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf");
    private static string Doc(string entities) => Header + "0\nSECTION\n2\nENTITIES\n" + entities + "0\nENDSEC\n0\nEOF\n";
    private static async Task<RawDrawingSnapshot> Parse(string text)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text), false);
        var a = await new TextDxfLexicalReader().ReadAsync(stream);
        var b = new TextDxfStructuralReader().Read(a); var c = new TextDxfGeometryReader().Read(b); var d = new TextDxfBlockAnnotationReader().Read(c);
        var e = new TextDxfIdentityGraphReader().Read(d, new("drawing-fixed", "revision-fixed", Hash));
        return Assert.IsType<RawDrawingSnapshot>(new TextDxfRawDrawingSnapshotAssembler().Assemble(e).Snapshot);
    }
    private static Task<RawDrawingSnapshot> Snapshot() => Parse(File.ReadAllText(Controlled));
    private sealed class ProjectFixture : IDisposable
    {
        public string Directory { get; } = Path.Combine(Path.GetTempPath(), "DtoB-viewer-" + Guid.NewGuid().ToString("N"));
        public string Manifest => Path.Combine(Directory, "Project.dtob");
        public ProjectSession Session { get; } = new(new ProjectStore());
        public ProjectFixture() { System.IO.Directory.CreateDirectory(Directory); Session.New("Viewer QA", "0.3.0"); Session.Save(Manifest); }
        public async Task<DiagnosticRequest> Register(string source)
        { var result = await Session.AddDrawingAsync(source); Session.Save(Manifest); return new(Session.Project!.ProjectId, Manifest, result.Drawing); }
        public Task<DiagnosticRequest> ControlledRequest() => Register(Controlled);
        public async Task<DiagnosticRequest> TextRequest(string text)
        { var source = Path.Combine(Directory, "Source.dxf"); File.WriteAllText(source, text, new UTF8Encoding(false)); return await Register(source); }
        public void Dispose() => System.IO.Directory.Delete(Directory, true);
    }
    [Fact]
    public async Task ManagedControlledPipelinePreservesLeaseProvenanceProjectAndSource()
    {
        using var f = new ProjectFixture(); var r = await f.ControlledRequest(); var before = File.ReadAllBytes(f.Manifest);
        var source = File.ReadAllBytes(Controlled); var result = await new DiagnosticExtraction().ReadAsync(r);
        Assert.True(result.Succeeded); var s = result.Snapshot!;
        Assert.Equal(r.Drawing.DrawingId.ToString(), s.DocumentProvenance!.DrawingId); Assert.Equal(r.Drawing.RevisionId.ToString(), s.DocumentProvenance.RevisionId);
        Assert.Equal(Hash, s.DocumentProvenance.FileHash); Assert.False(f.Session.IsDirty); Assert.Equal(before, File.ReadAllBytes(f.Manifest)); Assert.Equal(source, File.ReadAllBytes(Controlled));
        Assert.NotEmpty(RawDiagnosticScene.Build(s, RawDiagnosticViewMode.Entities).Items);
    }
    [Fact]
    public async Task MissingManagedNeverReadsValidOriginalOrUncSource()
    {
        using var f = new ProjectFixture(); var r = await f.ControlledRequest(); var path = AssetPaths.Resolve(r.Manifest, r.ProjectId, r.Drawing); File.Delete(path);
        var seen = new List<string>();
        var loader = new DiagnosticExtraction(p => { seen.Add(p); return File.OpenRead(p); });
        foreach (var source in new[] { Path.GetFullPath(Controlled), @"\\unavailable\share\never-contact.dxf" })
        {
            var failed = await loader.ReadAsync(r with { Drawing = r.Drawing with { SourceFilePath = source } });
            Assert.False(failed.Succeeded); Assert.Equal("ManagedRead", failed.Stage); Assert.Equal("IoFailure", failed.Status);
        }
        Assert.Equal(new[] { path, path }, seen); Assert.True(File.Exists(Controlled));
    }
    [Fact]
    public async Task DwgRefusesBeforeAnyManagedStreamOpen()
    {
        using var f = new ProjectFixture(); var r = await f.ControlledRequest(); var calls = 0;
        var result = await new DiagnosticExtraction(_ => { calls++; throw new IOException(); }).ReadAsync(r with { Drawing = r.Drawing with { FileExtension = ".dwg" } });
        Assert.Equal(0, calls); Assert.Equal("UnsupportedInput", result.Status); Assert.Contains("externally", result.Issues[0].Message); Assert.Null(result.Snapshot);
    }
    private sealed class LeaseStream(byte[] bytes, CancellationTokenSource? cancel = null) : MemoryStream(bytes, false)
    {
        public int Rewinds { get; private set; }
        public bool Disposed { get; private set; }
        public override long Position { get => base.Position; set { if (value == 0) { Rewinds++; cancel?.Cancel(); } base.Position = value; } }
        protected override void Dispose(bool disposing) { Disposed = true; base.Dispose(disposing); }
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task HashAndParseShareSingleReadOnlyDisposedLeaseAndCancellationRefuses(bool cancelAfterHash)
    {
        using var f = new ProjectFixture(); var r = await f.ControlledRequest(); using var token = new CancellationTokenSource();
        var stream = new LeaseStream(File.ReadAllBytes(Controlled), cancelAfterHash ? token : null); var opens = 0;
        var result = await new DiagnosticExtraction(_ => { opens++; return stream; }).ReadAsync(r, token.Token);
        Assert.Equal(1, opens); Assert.Equal(1, stream.Rewinds); Assert.True(stream.Disposed);
        if (cancelAfterHash) { Assert.False(result.Succeeded); Assert.Contains(result.Status, new[] { "Canceled" }); }
        else Assert.True(result.Succeeded);
    }
    [Theory][InlineData("hash")][InlineData("bytes")][InlineData("lexical")][InlineData("structure")][InlineData("graph")][InlineData("low01")][InlineData("pre-cancel")]
    public async Task FailureStagesNeverPublishSuccessfulEmptyCanvas(string failure)
    {
        using var f = new ProjectFixture();
        var text = failure switch
        {
            "lexical" => Doc(Point + "bad\nvalue\n"), "structure" => Doc(Point).Replace("0\nEOF\n", ""),
            "graph" => Doc(Point + Point), "low01" => Doc(Point + "340\nFF\n"), _ => Doc(Point)
        };
        var request = await f.TextRequest(text);
        if (failure == "hash") request = request with { Drawing = request.Drawing with { FileHash = new string('0', 64) } };
        var result = await new DiagnosticExtraction(lexicalLimits: failure == "bytes" ? new() { MaxInputBytes = 16 } : null).ReadAsync(request, new(failure == "pre-cancel"));
        Assert.Null(result.Snapshot); Assert.False(result.Succeeded); Assert.NotEmpty(result.Issues);
        Assert.Equal(failure switch { "hash" => "Integrity", "bytes" or "lexical" => "03A", "structure" => "03B", "pre-cancel" => "Request", _ => "03F" }, result.Stage);
        if (failure == "bytes") Assert.Equal("ResourceLimitExceeded", result.Status);
        if (failure == "low01") Assert.Contains(result.Issues, x => x.Stage == "03E" && x.Code == "UnresolvedPointer");
    }
    [Fact]
    public async Task EntitiesScopeMatchesIndependentSourceInventoryNoBlockGeometryLeak()
    {
        var s = await Snapshot(); var scene = RawDiagnosticScene.Build(s, RawDiagnosticViewMode.Entities);
        Assert.Equal(114, scene.Items.Count); Assert.Equal(45, scene.Items.Count(i => i.Kind == "LINE")); Assert.Equal(12, scene.Items.Count(i => i.Kind == "LWPOLYLINE"));
        Assert.Equal(17, scene.Items.Count(i => i.Kind == "INSERT")); Assert.Equal(36, scene.Items.Count(i => i.Kind == "TEXT")); Assert.Equal(4, scene.Items.Count(i => i.Kind == "DIMENSION"));
        Assert.All(scene.Items, i => Assert.Equal("ENTITIES", i.Record.SourceSection));
        // Independent section/code0 scanner: no production grouping helpers.
        var lines = File.ReadAllLines(Controlled); var section = ""; var types = new List<string>();
        for (var i = 0; i < lines.Length; i += 2)
        { if (lines[i].Trim() == "0" && lines[i + 1] == "SECTION") section = lines[i + 3]; else if (lines[i].Trim() == "0" && lines[i + 1] == "ENDSEC") section = ""; else if (section == "ENTITIES" && lines[i].Trim() == "0") types.Add(lines[i + 1]); }
        Assert.Equal(types, scene.Items.Select(i => i.Kind)); Assert.Equal(57, scene.Items.Count(i => i.Record.Geometry != null));
    }
    [Fact]
    public async Task EveryBlockHasExactOrderedLocalChildrenNoOccurrenceExpansion()
    {
        var s = await Snapshot(); var blocks = RawDiagnosticScene.Blocks(s); Assert.Equal(12, blocks.Count); Assert.Equal(44, s.BlockDefinitions.Sum(b => b.Children.Count));
        foreach (var block in blocks)
        {
            var scene = RawDiagnosticScene.Build(s, RawDiagnosticViewMode.BlockDefinition, block.Index); var original = s.BlockDefinitions[block.Index];
            Assert.Equal(original.Children.Select(c => c.StructuralRecordIndex), scene.Items.Select(i => i.RecordIndex));
            Assert.All(scene.Items, i => Assert.Same(s.Records[i.RecordIndex], i.Record));
        }
        var fixture = File.ReadAllText(Path.Combine(Root(), "datasets/controlled/phase03f/blocks-native.dxf")); var native = await Parse(fixture);
        var choice = RawDiagnosticScene.Blocks(native).Single(b => b.Name == "NONZERO"); var local = RawDiagnosticScene.Build(native, RawDiagnosticViewMode.BlockDefinition, choice.Index);
        Assert.Equal(new DisplayXY(12.5, -30), local.BlockBasePoint); Assert.Equal(2, local.Items.Count);
        var insert = Assert.Single(local.Items, i => i.Kind == "INSERT"); Assert.Contains("no occurrence expansion", insert.Notice);
        var point = Assert.Single(local.Items, i => i.Kind == "POINT"); Assert.Equal(1, point.Points[0].X); // No subtraction of12.5 baseX.
    }
    [Theory][InlineData("LINE")][InlineData("POINT")][InlineData("SOLID")][InlineData("CIRCLE")][InlineData("ARC")][InlineData("LWPOLYLINE")]
    public async Task DiagnosticPrimitivesUseRawNativeFactsOnly(string type)
    {
        var body = type switch
        {
            "LINE" => "10\n1\n20\n2\n11\n3\n21\n4\n", "POINT" => "10\n1\n20\n2\n30\n99\n",
            "SOLID" => "10\n1\n20\n2\n11\n3\n21\n4\n12\n5\n22\n6\n13\n5\n23\n6\n",
            "CIRCLE" => "10\n1\n20\n2\n40\n3\n", "ARC" => "10\n1\n20\n2\n40\n3\n50\n720\n51\n-90\n",
            _ => "90\n2\n70\n1\n10\n1\n20\n2\n42\n0.5\n10\n3\n20\n4\n"
        };
        var s = await Parse(Doc("0\n" + type + "\n5\nA\n330\n0\n" + body)); var before = JsonSerializer.Serialize(s);
        var scene = RawDiagnosticScene.Build(s, RawDiagnosticViewMode.Entities); var item = Assert.Single(scene.Items);
        Assert.Equal(type, item.Kind); Assert.Equal(new DisplayXY(1, 2), item.Points[0]);
        if (type == "LWPOLYLINE") { Assert.Equal(2, item.Points.Count); Assert.True(item.Closed); Assert.Contains("NONZERO BULGE", item.Notice); }
        if (type == "SOLID") { Assert.Equal(4, item.Points.Count); Assert.Equal(item.Points[2], item.Points[3]); }
        if (type == "ARC") { Assert.Equal(720, item.StartDegrees); Assert.Equal(-90, item.EndDegrees); }
        if (type is "ARC" or "CIRCLE") Assert.Equal(3, item.Radius);
        Assert.Equal(before, JsonSerializer.Serialize(s));
    }
    [Theory][InlineData(0)][InlineData(1)]
    public async Task PolylineClosedFlagOnlyAndNoImplicitClosingVertex(int flags)
    {
        var s = await Parse(Doc($"0\nLWPOLYLINE\n5\nA\n330\n0\n90\n2\n70\n{flags}\n10\n1\n20\n2\n10\n3\n20\n4\n"));
        var item = Assert.Single(RawDiagnosticScene.Build(s, RawDiagnosticViewMode.Entities).Items); Assert.Equal(flags == 1, item.Closed); Assert.Equal(2, item.Points.Count);
    }
    [Fact]
    public async Task FitIgnoresHeaderSentinelsAndCameraIsOnlyScreenProjection()
    {
        var s = await Snapshot(); var before = JsonSerializer.Serialize(s); var scene = RawDiagnosticScene.Build(s, RawDiagnosticViewMode.Entities);
        Assert.NotNull(scene.Bounds); Assert.True(Math.Abs(scene.Bounds!.MaxX) < 1e10); Assert.Equal(1e20, s.Header!.ExtMin.X.Real);
        var camera = new DiagnosticCamera(); Assert.True(camera.Fit(scene.Bounds, 800, 600)); var p = camera.Project(new(0, 1)); var q = camera.Project(new(0, 2)); Assert.True(q.Y < p.Y);
        camera.Pan(12, 34); Assert.Equal(p.X + 12, camera.Project(new(0, 1)).X); camera.Zoom(2, 100, 100); var serialized = JsonSerializer.Serialize(camera);
        var second = new DiagnosticCamera(); second.Fit(scene.Bounds, 800, 600); second.Pan(12, 34); second.Zoom(2, 100, 100); Assert.Equal(serialized, JsonSerializer.Serialize(second));
        camera.Reset(); Assert.Equal(new DisplayXY(3, -4), camera.Project(new(3, 4))); Assert.Equal(before, JsonSerializer.Serialize(s));
        Assert.False(camera.Fit(null, 800, 600));
    }
    [Fact]
    public void FitRecomputesFromFinalViewportDimensionsRatherThanPreviousLayout()
    {
        var c = new DiagnosticCamera(); var bounds = new DisplayBounds(-100, -200, 500, 700);
        Assert.True(c.Fit(bounds, 800, 700)); var previous = c.Scale;
        Assert.True(c.Fit(bounds, 800, 400)); Assert.True(c.Scale < previous);
        foreach (var p in new[] { new DisplayXY(bounds.MinX, bounds.MinY), new DisplayXY(bounds.MaxX, bounds.MaxY) })
        { var screen = c.Project(p); Assert.InRange(screen.X, 24, 776); Assert.InRange(screen.Y, 24, 376); }
    }
    [Fact]
    public async Task OverflowingCircleDisplayBoundsDoNotFabricateCenterOnlyFit()
    {
        var s = await Parse(Doc("0\nCIRCLE\n5\nA\n330\n0\n10\n1e308\n20\n0\n40\n1e308\n"));
        var before = JsonSerializer.Serialize(s); var scene = RawDiagnosticScene.Build(s, RawDiagnosticViewMode.Entities);
        Assert.Single(scene.Items); Assert.Null(scene.Bounds); Assert.False(new DiagnosticCamera().Fit(scene.Bounds, 800, 600));
        Assert.Equal(before, JsonSerializer.Serialize(s)); // Facts remain inspectable; viewport shows unrepresentable empty state.
    }
    [Theory][InlineData("LINE")][InlineData("LWPOLYLINE")][InlineData("INSERT")][InlineData("TEXT")][InlineData("DIMENSION")]
    public async Task SelectedItemTracesPayloadFactsToPairsPhysicalLinesAndOriginalHandle(string kind)
    {
        var s = await Snapshot(); var item = RawDiagnosticScene.Build(s, RawDiagnosticViewMode.Entities).Items.First(i => i.Kind == kind);
        Assert.Same(s.Records[item.RecordIndex], item.Record); var trace = RawDiagnosticScene.Inspect(s, item.Record);
        var pair = s.LexicalEvidence.Pairs[item.Record.SourceRecord.Source.StartPairIndex];
        Assert.Contains($"pair {pair.PairIndex}", trace); Assert.Contains($"lines {pair.CodeLineNumber}/{pair.ValueLineNumber}", trace); Assert.Contains(pair.RawTextValue, trace);
        Assert.Contains("OriginalHandle:", trace); Assert.Contains("Reference Owner", trace); Assert.Contains("SourceFact", trace);
    }
    [Fact]
    public async Task SummaryUnsupportedAndSceneImmutabilityRemainHonest()
    {
        var s = await Snapshot(); var scene = RawDiagnosticScene.Build(s, RawDiagnosticViewMode.Entities); var summary = RawDiagnosticScene.Summary(s);
        foreach (var text in new[] { "96621 bytes", "8737 pairs", "333 children", "Supported 281", "Unsupported 52", "Coverage Partial", "Geometry 89", "03D 93", "Definitions 12", "Identity 327", "References 458", "Owners 317", "TypedMappingComplete=False" }) Assert.Contains(text, summary);
        var unsupported = s.Records.Where(r => r.SemanticDisposition == DxfDisposition.Unsupported).ToArray(); Assert.Equal(52, unsupported.Length);
        Assert.All(unsupported, r => { Assert.DoesNotContain(scene.Items, i => i.Record == r); Assert.Contains("Unsupported", RawDiagnosticScene.Inspect(s, r)); });
        Assert.Throws<NotSupportedException>(() => ((IList)scene.Items).Clear()); Assert.Throws<NotSupportedException>(() => ((IList)scene.Items[0].Points).Clear());
        var noGeometry = await Parse(Doc("0\nELLIPSE\n5\nA\n330\n0\n")); Assert.Null(RawDiagnosticScene.Build(noGeometry, RawDiagnosticViewMode.Entities).Bounds);
    }
    [Theory][InlineData("")][InlineData("fr-FR")][InlineData("ko-KR")]
    public async Task SceneModelDeterministicAcrossFreshSnapshotsAndCultures(string culture)
    {
        var previous = CultureInfo.CurrentCulture; try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture); var first = await Snapshot(); var second = await Snapshot();
            string Serialize(RawDrawingSnapshot s) => JsonSerializer.Serialize(new[] { RawDiagnosticScene.Build(s, RawDiagnosticViewMode.Entities) }.Concat(RawDiagnosticScene.Blocks(s).Select(b => RawDiagnosticScene.Build(s, RawDiagnosticViewMode.BlockDefinition, b.Index))));
            var json = Serialize(first); Assert.Equal(json, Serialize(second));
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
            Assert.Equal("9D179A0EFAC5EE803E8AEC2AC3C48E1AB0986F1C6159BCB1764E8071933963A7", hash);
            var directory = Environment.GetEnvironmentVariable("DTOB_03H_EVIDENCE_DIRECTORY");
            if (directory != null) { Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory, "scene-" + (culture.Length == 0 ? "invariant" : culture) + ".json"), json); File.WriteAllText(Path.Combine(directory, "scene-" + (culture.Length == 0 ? "invariant" : culture) + ".sha256"), hash); }
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }
    [Fact]
    public async Task ContextAndEpochDiscardStaleSuccessAndFailureWithoutSessionChanges()
    {
        using var f = new ProjectFixture(); var r = await f.ControlledRequest(); var gate = new DiagnosticRequestGate(); var first = gate.Next(); Assert.True(r.Matches(f.Session, r.Drawing));
        var second = gate.Next(); Assert.False(gate.IsCurrent(first)); Assert.True(gate.IsCurrent(second)); Assert.False(r.Matches(f.Session, null));
        Assert.False(r.Matches(f.Session, r.Drawing with { RevisionId = Guid.NewGuid() })); f.Session.Close(); Assert.False(r.Matches(f.Session, r.Drawing));
    }
    [Fact]
    public async Task ActualManagedLeaseDeniesConcurrentWriteAndReadLeavesNoChanges()
    {
        using var f = new ProjectFixture(); var r = await f.ControlledRequest(); var path = AssetPaths.Resolve(r.Manifest, r.ProjectId, r.Drawing); var bytes = File.ReadAllBytes(path);
        var result = await new DiagnosticExtraction(p =>
        {
            var stream = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.Read);
            Assert.Throws<IOException>(() => { using var write = new FileStream(p, FileMode.Open, FileAccess.Write, FileShare.ReadWrite); }); return stream;
        }).ReadAsync(r);
        Assert.True(result.Succeeded); Assert.Equal(bytes, File.ReadAllBytes(path));
    }
    [Fact]
    public async Task ManualWorkflowFixtureUsesExistingProjectSaveAndRegistrationApis()
    {
        using var f = new ProjectFixture(); await f.ControlledRequest();
        await f.Register(Path.Combine(Root(), "datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dwg"));
        Assert.Equal(2, f.Session.Project!.Drawings!.Length);
        var output = Environment.GetEnvironmentVariable("DTOB_03H_MANUAL_DIRECTORY");
        if (output != null) { Directory.CreateDirectory(output); await f.Session.SaveAsync(Path.Combine(output, "Viewer.dtob")); }
    }
}
