using System.Collections;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DtoB.Cad.Dxf;
using DtoB.Cad.Raw;
using Xunit;

namespace DtoB.Tests;

// QA only. Expected counts/line goldens are reviewed source facts, never production defaults.
public partial class ExtractionQaCampaignTests
{
    private const string SourceHash = "2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC";
    private const string SnapshotHash = "4B91C83791F89DEC9BDC633C832001F2D1E537E7F0330BEE0666067CAE9A310C";
    private const string Header = "0\nSECTION\n2\nHEADER\n9\n$ACADVER\n1\nAC1032\n0\nENDSEC\n";
    private const string Point = "0\nPOINT\n5\nA\n330\n0\n10\n1.5\n20\n1e+20\n30\n-0.0\n";
    private static string Doc(string body, string section = "ENTITIES") => Header + "0\nSECTION\n2\n" + section + "\n" + body + "0\nENDSEC\n0\nEOF\n";
    private static string Root()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d != null && !File.Exists(Path.Combine(d.FullName, "DtoB.sln"))) d = d.Parent;
        return d?.FullName ?? throw new InvalidDataException("Repository not found.");
    }
    private static string Controlled => Path.Combine(Root(), "datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf");
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    private static string JsonHash(object value) => Hash(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value)));
    private static async Task<DxfLexicalReadResult> Lex(string text)
    {
        using var s = new MemoryStream(Encoding.UTF8.GetBytes(text), false);
        return await new TextDxfLexicalReader().ReadAsync(s);
    }
    private static DxfIdentityGraphReadResult Graph(DxfLexicalReadResult lex) => new TextDxfIdentityGraphReader().Read(
        new TextDxfBlockAnnotationReader().Read(new TextDxfGeometryReader().Read(new TextDxfStructuralReader().Read(lex))),
        new DxfDocumentProvenance("drawing-fixed", "revision-fixed", SourceHash));
    private static RawDrawingSnapshotAssemblyResult Snapshot(DxfLexicalReadResult lex) => new TextDxfRawDrawingSnapshotAssembler().Assemble(Graph(lex));
    private static async Task<RawDrawingSnapshot> FreshControlled()
    {
        using var s = new FileStream(Controlled, FileMode.Open, FileAccess.Read, FileShare.Read);
        var result = Snapshot(await new TextDxfLexicalReader().ReadAsync(s));
        Assert.Equal(RawSnapshotAssemblyStatus.Succeeded, result.Status);
        return Assert.IsType<RawDrawingSnapshot>(result.Snapshot);
    }
    private static void Evidence(string name, string content)
    {
        var directory = Environment.GetEnvironmentVariable("DTOB_03G_EVIDENCE_DIRECTORY");
        if (directory != null) { Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory, name), content, new UTF8Encoding(false)); }
    }
    public sealed record FixtureSpec(string FixtureId, string Path, string Purpose, string ExpectedStage,
        string ExpectedStatus, string[] ExpectedDiagnosticCodes, string ExpectedPreservationBehavior, string OriginalSHA256);
    private static FixtureSpec[] Manifest() => JsonSerializer.Deserialize<FixtureSpec[]>(
        File.ReadAllText(Path.Combine(Root(), "datasets/controlled/phase03g/manifest.json")))!;
    public static IEnumerable<object[]> Fixtures() => Manifest().Select(f => new object[] { f.FixtureId });

    [Theory, MemberData(nameof(Fixtures))]
    public async Task ManifestCaseUsesReadOnlySourceAndReviewedStageExpectation(string fixtureId)
    {
        var f = Manifest().Single(x => x.FixtureId == fixtureId); var path = Path.Combine(Root(), f.Path);
        var before = File.ReadAllBytes(path); Assert.Equal(f.OriginalSHA256, Hash(before));
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        Assert.False(stream.CanWrite);
        var a = await new TextDxfLexicalReader().ReadAsync(stream);
        object stage = a;
        if (f.ExpectedStage != "03A")
        {
            var b = new TextDxfStructuralReader().Read(a); stage = b;
            if (f.ExpectedStage != "03B")
            {
                var c = new TextDxfGeometryReader().Read(b); stage = c;
                if (f.ExpectedStage != "03C")
                {
                    var d = new TextDxfBlockAnnotationReader().Read(c); stage = d;
                    if (f.ExpectedStage != "03D")
                    {
                        var e = new TextDxfIdentityGraphReader().Read(d); stage = e;
                        if (f.ExpectedStage == "03F") stage = new TextDxfRawDrawingSnapshotAssembler().Assemble(e);
                    }
                }
            }
        }
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(stage, stage.GetType(), new JsonSerializerOptions { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } }));
        Assert.Equal(f.ExpectedStatus, json.RootElement.GetProperty("Status").GetString());
        var codes = json.RootElement.GetProperty("Diagnostics").EnumerateArray().Select(x => x.GetProperty("Code").GetString()).ToArray();
        foreach (var code in f.ExpectedDiagnosticCodes) Assert.Contains(code, codes);
        Assert.True(stream.CanRead); Assert.Equal(before, File.ReadAllBytes(path));
        if (a.Status == DxfLexicalStatus.Succeeded)
        {
            // Independent BCL physical-line oracle; no production grouping helpers.
            using var reader = new StringReader(new UTF8Encoding(false, true).GetString(before).TrimStart('\uFEFF'));
            foreach (var pair in a.Pairs)
            {
                Assert.Equal(reader.ReadLine(), pair.RawCodeText); Assert.Equal(reader.ReadLine(), pair.RawTextValue);
                Assert.Equal(pair.PairIndex * 2 + 1, pair.CodeLineNumber); Assert.Equal(pair.PairIndex * 2 + 2, pair.ValueLineNumber);
            }
            Assert.Null(reader.ReadLine());
        }
    }
    [Fact]
    public void ManifestCoversEveryReviewedDiskFixtureAndHashesAreUniqueByPath()
    {
        var manifest = Manifest(); Assert.Equal(manifest.Length, manifest.Select(f => f.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        var actual = Directory.GetFiles(Path.Combine(Root(), "datasets/controlled"), "*", SearchOption.AllDirectories)
            .Where(p => p.EndsWith(".dxf", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
            .Select(p => Path.GetRelativePath(Root(), p).Replace('\\', '/')).Order().ToArray();
        Assert.Equal(86, manifest.Length);
        Assert.Equal("A3C7CBD16E94498A0CDE094664DFEFA520DC3182DA4899DB92EAE6C1649B7B36", Hash(File.ReadAllBytes(Path.Combine(Root(), "datasets/controlled/phase03g/manifest.json"))));
        Cad04HReleaseTests.ValidateCoverage(actual, Cad04HCorpus.Manifest().Select(f => f.Path));
        Assert.Equal(manifest.Select(f => f.Path).Order(), Cad04HCorpus.Manifest().Where(f => f.Key.StartsWith("legacy-", StringComparison.Ordinal)).Select(f => f.Path).Order());
        Assert.All(manifest, f => { Assert.NotEmpty(f.Purpose); Assert.NotEmpty(f.ExpectedPreservationBehavior); Assert.Equal(64, f.OriginalSHA256.Length); });
    }

    [Theory]
    [InlineData("")][InlineData("fr-FR")][InlineData("ko-KR")]
    public async Task FreshFullPipelinesHaveIdenticalOrderAndAuthoritativeHashAcrossCultures(string culture)
    {
        var prior = CultureInfo.CurrentCulture; var priorUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture); CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            var serializations = new List<string>();
            for (var i = 0; i < 3; i++)
            {
                var s = await FreshControlled(); serializations.Add(JsonSerializer.Serialize(s));
                Assert.Equal(SnapshotHash, JsonHash(s));
                Assert.Equal(Enumerable.Range(0, 8737), s.LexicalEvidence.Pairs.Select(p => p.PairIndex));
                Assert.Equal(Enumerable.Range(0, 333), s.Records.Select(r => r.StructuralRecordIndex));
                Assert.Equal(s.Diagnostics.OrderBy(d => d.Stage).ThenBy(d => d.Index), s.Diagnostics);
                // Interleave unrelated corrupt input to detect retained mutable parser state.
                Assert.Null(Snapshot(await Lex("0\nEOF\n")).Snapshot);
            }
            Assert.All(serializations, x => Assert.Equal(serializations[0], x));
            var numeric = Snapshot(await Lex(Doc(Point))).Snapshot!;
            var p = Assert.IsType<RawPoint>(Assert.Single(numeric.Geometry).Payload);
            Assert.Equal(1.5, p.Position.X.Real); Assert.Equal(1e20, p.Position.Y.Real);
            Assert.Equal(long.MinValue, BitConverter.DoubleToInt64Bits(p.Position.Z.Real!.Value));
            Assert.Equal(SourceHash, Hash(File.ReadAllBytes(Controlled)));
            Evidence("controlled-snapshot.json", serializations[0]);
            Evidence("determinism-" + (culture.Length == 0 ? "invariant" : culture) + ".json", JsonSerializer.Serialize(new { Culture = culture, Runs = 3, InterleavedInvalid = true, SnapshotHash, SourceHash }));
        }
        finally { CultureInfo.CurrentCulture = prior; CultureInfo.CurrentUICulture = priorUi; }
    }

    [Fact]
    public async Task ControlledIndependentSourceInventoryAndAdditionalExactGoldensReachSnapshot()
    {
        var bytes = File.ReadAllBytes(Controlled); Assert.Equal(96621, bytes.Length); Assert.Equal(SourceHash, Hash(bytes));
        var lines = File.ReadAllLines(Controlled, new UTF8Encoding(false, true)); Assert.Equal(17474, lines.Length);
        var starts = Enumerable.Range(0, lines.Length / 2).Where(i => int.Parse(lines[2 * i], CultureInfo.InvariantCulture) == 0).ToArray();
        Assert.DoesNotContain(starts, i => lines[2 * i + 1] == "HATCH");
        Assert.Equal(4, starts.Count(i => lines[2 * i + 1] == "DIMENSION"));
        var s = await FreshControlled();
        var unsupported = s.Records.Where(r => r.SemanticDisposition == DxfDisposition.Unsupported).ToArray();
        Assert.Equal(52, unsupported.Length); Assert.Equal(48, unsupported.Count(r => r.SourceSection == "OBJECTS"));
        Assert.Single(unsupported, r => r.SourceRecord.RecordType == "VPORT"); Assert.Equal(3, unsupported.Count(r => r.SourceRecord.RecordType == "APPID"));
        var objectInventory = new Dictionary<string, int> { ["DICTIONARY"] = 12, ["ACDBDICTIONARYWDFLT"] = 1, ["ACDBPLACEHOLDER"] = 1, ["LAYOUT"] = 2, ["MATERIAL"] = 3, ["MLINESTYLE"] = 1, ["MLEADERSTYLE"] = 1, ["DICTIONARYVAR"] = 2, ["VISUALSTYLE"] = 25 };
        Assert.Equal(objectInventory.OrderBy(x => x.Key), unsupported.Where(r => r.SourceSection == "OBJECTS").GroupBy(r => r.SourceRecord.RecordType).ToDictionary(g => g.Key, g => g.Count()).OrderBy(x => x.Key));
        foreach (var r in s.Records)
        {
            Assert.Contains(r.SourceRecord.Source.StartPairIndex, starts);
            for (var i = r.SourceRecord.Source.StartPairIndex; i <= r.SourceRecord.Source.EndPairIndex; i++)
                Assert.Equal(lines[2 * i + 1], s.LexicalEvidence.Pairs[i].RawTextValue);
        }
        void Fact(DxfSourceFact f, int pair, string raw)
        { Assert.Equal(pair, Assert.Single(f.SourcePairIndices)); Assert.Equal(raw, lines[pair * 2 + 1]); Assert.Equal(raw, s.LexicalEvidence.Pairs[pair].RawTextValue); Assert.Equal(pair * 2 + 2, f.Source!.EndLine); }
        Fact(s.Header!.InsertionUnits, 455, "4"); Fact(s.Header.DwgCodePage, 7, "ANSI_1252");
        var point = Assert.IsType<RawPoint>(s.Records.Single(r => r.SourceRecord.Source.StartPairIndex == 3021).Geometry!.Payload); Fact(point.Position.Y, 3028, "-1300.0");
        var solid = Assert.IsType<RawSolid>(s.Records.Single(r => r.SourceRecord.Source.StartPairIndex == 2747).Geometry!.Payload);
        Fact(solid.Third.Y, 2762, "-0.16439898372402537"); Fact(solid.Fourth.Y, 2765, "-0.16439898372402537");
        var mtext = Assert.IsType<RawMText>(s.Records.Single(r => r.SourceRecord.Source.StartPairIndex == 3008).BlockAnnotation!.Payload);
        Fact(Assert.Single(mtext.Chunks).Content, 3019, "800000");
        foreach (var name in new[] { "COLUMN_400", "D_SINGLE_900", "W_SINGLE_1200", "*D1", "*D2", "*D3", "*D4" })
            Assert.Contains(s.BlockAnnotations, r => r.Payload is RawBlockBegin b && b.Name.Text == name);
        Assert.All(s.Records.Where(r => r.SourceRecord.RecordType == "DIMENSION"), r => Assert.Equal(DxfDisposition.Supported, r.SemanticDisposition));
        Fact(s.IdentityNodes.Single(n => n.SourceRecord.Source.StartPairIndex == 682).OriginalHandle, 684, "8");
        Fact(s.IdentityNodes.Single(n => n.SourceRecord.Source.StartPairIndex == 2662).OriginalHandle, 2663, "18");
        Fact(s.IdentityNodes.Single(n => n.SourceRecord.Source.StartPairIndex == 2856).OriginalHandle, 2857, "9C");
        Assert.DoesNotContain(s.IdentityNodes, n => n.OriginalHandle.SourcePairIndices.Contains(277));
    }

    private static void Frozen(object list)
    {
        var collection = Assert.IsAssignableFrom<IList>(list); Assert.True(collection.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => collection.Clear());
    }
    [Fact]
    public async Task PublishedCollectionsAndCallerListsCannotAlterFrozenSnapshot()
    {
        var s = await FreshControlled(); var before = JsonSerializer.Serialize(s);
        foreach (var list in new object[] { s.LexicalEvidence.Pairs, s.StructuralEvidence.Records, s.Geometry, s.BlockAnnotations,
            s.BlockDefinitions[0].Children, s.IdentityNodes, s.References, s.Ownership, s.Records, s.Diagnostics }) Frozen(list);
        Frozen(s.BlockAnnotations.Select(r => r.Payload).OfType<RawMText>().First().Chunks);
        var records = s.Records.ToList(); var diagnostics = s.Diagnostics.ToList();
        var copy = new RawDrawingSnapshot(s.IdentityEvidence, records, diagnostics, s.AssemblyDiagnostics, s.Completeness);
        records.Clear(); diagnostics.Clear(); Assert.Equal(333, copy.Records.Count); Assert.Equal(59, copy.Diagnostics.Count);
        Assert.Same(s.Completeness, copy.Completeness); Assert.Equal(before, JsonSerializer.Serialize(s));
    }

    [Fact]
    public async Task WarningOnlyUnresolvedPointerRemainsKnownV1SnapshotRefusalPolicy()
    {
        var g = Graph(await Lex(Doc("0\nDICTIONARY\n5\nA\n330\n0\n340\nFF\n", "OBJECTS")));
        Assert.True(g.GraphValidated); Assert.True(g.IdentityCoverageComplete); Assert.False(g.ReferenceResolutionComplete);
        var r = new TextDxfRawDrawingSnapshotAssembler().Assemble(g);
        Assert.Null(r.Snapshot); Assert.Equal(RawSnapshotAssemblyStatus.InvalidInput, r.Status); Assert.False(r.Completeness.SnapshotAssemblyComplete);
    }
}
