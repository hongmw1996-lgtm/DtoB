using System.Text.Json;
using DtoB.Cad.Dxf;
using DtoB.Cad.Raw;
using Xunit;

namespace DtoB.Tests;

public partial class ExtractionQaCampaignTests
{
    public static IEnumerable<object[]> RejectedSources()
    {
        // Bounded, named source edits. No random sampling and no fabricated parser results.
        yield return new object[] { "structure/no-section", "03B", "0\nPOINT\n10\n1\n20\n2\n0\nEOF\n", "InvalidTransition" };
        yield return new object[] { "structure/duplicate-eof", "03B", Doc(Point) + "0\nEOF\n", "ContentAfterEof" };
        yield return new object[] { "structure/unclosed-control", "03B", Doc(Point + "102\n{ACAD_REACTORS\n"), "UnclosedControlGroup" };
        yield return new object[] { "structure/unclosed-xdata", "03B", Doc(Point + "1001\nAPP\n1002\n{\n"), "UnclosedControlGroup" };
        yield return new object[] { "geometry/solid-missing-y", "03C", Doc("0\nSOLID\n5\nA\n330\n0\n10\n1\n20\n2\n11\n1\n21\n2\n12\n1\n22\n2\n13\n1\n"), "MissingRequiredField" };
        foreach (var (name, extra, diagnostic) in new[] {
            ("line-duplicate", "10\n1\n10\n2\n20\n1\n11\n1\n21\n1\n", "DuplicateField"),
            ("line-nonfinite", "10\nNaN\n20\n1\n11\n1\n21\n1\n", "InvalidNumericValue"),
            ("line-missing", "10\n1\n11\n1\n21\n1\n", "MissingRequiredField") })
            yield return new object[] { "geometry/" + name, "03C", Doc("0\nLINE\n5\nA\n330\n0\n" + extra), diagnostic };
        foreach (var radius in new[] { "0", "-1", "NaN" })
            yield return new object[] { "geometry/circle-radius-" + radius, "03C", Doc("0\nCIRCLE\n5\nA\n330\n0\n10\n0\n20\n0\n40\n" + radius + "\n"), radius == "NaN" ? "InvalidNumericValue" : "InvalidRadius" };
        yield return new object[] { "annotation/insert-no-target", "03D", Doc("0\nINSERT\n5\nA\n330\n0\n10\n1\n20\n2\n"), "MissingRequiredField" };
        yield return new object[] { "annotation/text-no-content", "03D", Doc("0\nTEXT\n5\nA\n330\n0\n10\n1\n20\n2\n40\n3\n"), "MissingRequiredField" };
        foreach (var (name, chunks, diagnostic) in new[] { ("no-terminal", "3\na\n", "MissingRequiredField"), ("two-terminals", "1\na\n1\nb\n", "InvalidMTextChunks"), ("after-terminal", "1\na\n3\nb\n", "InvalidMTextChunks") })
            yield return new object[] { "annotation/mtext-" + name, "03D", Doc("0\nMTEXT\n5\nA\n330\n0\n10\n1\n20\n2\n40\n3\n" + chunks), diagnostic };
        yield return new object[] { "graph/duplicate", "03E", Doc(ObjectEntity + ObjectEntity, "OBJECTS"), "DuplicateHandle" };
        yield return new object[] { "graph/unresolved-owner", "03E", Doc(ObjectEntity.Replace("330\n0", "330\nFF"), "OBJECTS"), "UnresolvedOwner" };
        yield return new object[] { "graph/unresolved-reactor", "03E", Doc(ObjectEntity + "102\n{ACAD_REACTORS\n330\nFF\n102\n}\n", "OBJECTS"), "UnresolvedReactor" };
        yield return new object[] { "graph/self-owner", "03E", Doc(ObjectEntity.Replace("330\n0", "330\nA"), "OBJECTS"), "SelfOwner" };
        yield return new object[] { "graph/two-node-cycle", "03E", Doc("0\nDICTIONARY\n5\nA\n330\nB\n0\nDICTIONARY\n5\nB\n330\nA\n", "OBJECTS"), "OwnerCycle" };
    }
    [Theory, MemberData(nameof(RejectedSources))]
    public async Task NamedSourceCorruptionsNeverCrossSnapshotGateAndDiagnosticsRepeat(string id, string stage, string text, string expectedCode)
    {
        var results = new List<string>();
        for (var repeat = 0; repeat < 2; repeat++)
        {
            var a = await Lex(text); var b = new TextDxfStructuralReader().Read(a); var c = new TextDxfGeometryReader().Read(b);
            var d = new TextDxfBlockAnnotationReader().Read(c); var e = new TextDxfIdentityGraphReader().Read(d);
            var codes = stage switch { "03B" => b.Diagnostics.Select(x => x.Code.ToString()), "03C" => c.Diagnostics.Select(x => x.Code.ToString()), "03D" => d.Diagnostics.Select(x => x.Code.ToString()), _ => e.Diagnostics.Select(x => x.Code.ToString()) };
            Assert.Contains(expectedCode, codes); var r = new TextDxfRawDrawingSnapshotAssembler().Assemble(e);
            Assert.Null(r.Snapshot); Assert.False(r.Completeness.SnapshotAssemblyComplete); Assert.Equal(RawSnapshotAssemblyStatus.InvalidInput, r.Status);
            results.Add(JsonSerializer.Serialize(e));
        }
        Assert.Equal(results[0], results[1]);
        Evidence("corruption-" + id.Replace('/', '-') + ".dxf", text);
        Evidence("corruption-" + id.Replace('/', '-') + ".json", JsonSerializer.Serialize(new { CaseId = id, Stage = stage, ExpectedCode = expectedCode, Repeats = 2, Snapshot = "refused", EvidenceHash = JsonHash(results[0]) }));
    }
    [Theory]
    [InlineData("ELLIPSE")][InlineData("SPLINE")][InlineData("HATCH")][InlineData("3DSOLID")][InlineData("ACAD_PROXY_ENTITY")]
    public async Task UnsupportedEntitiesRemainSourceEvidenceWithNoApproximatePayload(string type)
    {
        var s = Snapshot(await Lex(Doc("0\n" + type + "\n5\nA\n330\n0\n10\n1\n20\n2\n"))).Snapshot!;
        var view = Assert.Single(s.Records); Assert.Equal(DxfDisposition.Unsupported, view.SemanticDisposition);
        Assert.Null(view.Geometry!.Payload); Assert.False(s.Completeness.TypedMappingComplete); Assert.True(s.Completeness.SourceEvidenceComplete);
        Assert.NotEmpty(view.Geometry.UnmappedPairIndices); Assert.NotEmpty(s.Diagnostics);
    }
}
