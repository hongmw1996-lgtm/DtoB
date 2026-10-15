using System.Globalization;
using System.Text;
using System.Text.Json;
using DtoB.Cad.Dxf;
using DtoB.Cad.Raw;
using Xunit;

namespace DtoB.Tests;

public partial class ExtractionQaCampaignTests
{
    public sealed record LimitCase(string Stage, string Property, string Text, int RequiredBudget);
    private const string TextEntity = "0\nTEXT\n1\nabc\n10\n1\n20\n2\n40\n3\n";
    private const string InsertEntity = "0\nINSERT\n2\nB\n10\n1\n20\n2\n";
    private const string BlockEntity = "0\nBLOCK\n2\nB\n10\n0\n20\n0\n";
    private const string ObjectEntity = "0\nDICTIONARY\n5\nA\n330\n0\n";
    private static int Pairs(string s) => s.Count(c => c == '\n') / 2;
    private static string Blocks(string body) => Header + "0\nSECTION\n2\nBLOCKS\n" + body + "0\nENDSEC\n0\nEOF\n";
    public static IEnumerable<object[]> LimitCases()
    {
        var cases = new List<LimitCase>();
        void Add(string stage, string property, string text, int budget) => cases.Add(new(stage, property, text, budget));
        Add("03A", "MaxInputBytes", "0\nx\n", 4);
        Add("03A", "MaxPhysicalLineBytes", "1\n한글\n", 6);
        Add("03A", "MaxPairCount", "1\na\n1\nb\n1\nc\n", 3);
        Add("03A", "MaxStoredDiagnostics", "x\na\ny\nb\n", 3); // Two ordinary + reserved terminal slot.
        Add("03B", "MaxSections", Doc(Point), 2);
        Add("03B", "MaxTables", Doc("0\nTABLE\n2\nLAYER\n0\nENDTAB\n0\nTABLE\n2\nSTYLE\n0\nENDTAB\n", "TABLES"), 2);
        Add("03B", "MaxRecords", Doc(Point + Point), 3); // One HEADER variable + two children.
        Add("03B", "MaxFieldsPerRecord", Doc(Point), 6);
        Add("03B", "MaxReferenceOccurrences", Doc(Point + Point), 2);
        Add("03B", "MaxControlGroupDepth", Doc(Point + "102\n{A\n102\n{B\n102\n}\n102\n}\n"), 2);
        Add("03B", "MaxPairCount", Doc(Point), Pairs(Doc(Point)));
        Add("03B", "MaxStoredDiagnostics", Doc("0\nFOO\n0\nBAR\n"), 3);
        Add("03C", "MaxGeometryRecords", Doc(Point + Point), 2);
        Add("03C", "MaxVerticesPerPolyline", Doc("0\nLWPOLYLINE\n90\n3\n10\n0\n20\n0\n10\n1\n20\n1\n10\n2\n20\n2\n"), 3);
        Add("03C", "MaxTotalVertices", Doc(Point + Point), 2);
        Add("03C", "MaxFieldsPerRecord", Doc(Point), 6);
        Add("03C", "MaxStructuralRecords", Doc(Point + Point), 2);
        Add("03C", "MaxSourcePairs", Doc(Point), Pairs(Doc(Point)));
        Add("03C", "MaxStoredDiagnostics", Doc("0\nLINE\n"), 5); // Four missing XY components + reserved slot.
        Add("03D", "MaxDefinitions", Blocks(BlockEntity + "0\nENDBLK\n" + BlockEntity + "0\nENDBLK\n"), 2);
        Add("03D", "MaxChildrenPerBlock", Blocks(BlockEntity + Point + Point + "0\nENDBLK\n"), 2);
        Add("03D", "MaxInserts", Doc(InsertEntity + InsertEntity), 2);
        Add("03D", "MaxAnnotations", Doc(TextEntity + TextEntity), 2);
        Add("03D", "MaxMTextChunks", Doc("0\nMTEXT\n3\na\n3\nb\n1\nc\n10\n1\n20\n2\n40\n3\n"), 3);
        Add("03D", "MaxRawAnnotationCharacters", Doc(TextEntity), 3);
        Add("03D", "MaxAttributeSequenceLength", Doc(InsertEntity + "66\n1\n0\nATTRIB\n1\na\n2\nT\n10\n1\n20\n2\n40\n3\n0\nATTRIB\n1\nb\n2\nT\n10\n1\n20\n2\n40\n3\n0\nSEQEND\n"), 2);
        Add("03D", "MaxMappedRecords", Doc(TextEntity + TextEntity), 2);
        Add("03D", "MaxFieldsPerRecord", Doc(TextEntity), 5);
        Add("03D", "MaxStructuralRecords", Doc(TextEntity + TextEntity), 2);
        Add("03D", "MaxSourcePairs", Doc(TextEntity), Pairs(Doc(TextEntity)));
        Add("03D", "MaxStoredDiagnostics", Doc("0\nTEXT\n"), 5);
        Add("03E", "MaxRecords", Doc(ObjectEntity + ObjectEntity.Replace("5\nA", "5\nB"), "OBJECTS"), 2);
        Add("03E", "MaxNodes", Doc(ObjectEntity + ObjectEntity.Replace("5\nA", "5\nB"), "OBJECTS"), 2);
        Add("03E", "MaxReferences", Doc(ObjectEntity + ObjectEntity.Replace("5\nA", "5\nB"), "OBJECTS"), 2);
        var duplicates = ObjectEntity + ObjectEntity + ObjectEntity.Replace("5\nA", "5\nB") + ObjectEntity.Replace("5\nA", "5\nB");
        Add("03E", "MaxDuplicateGroups", Doc(duplicates, "OBJECTS"), 2);
        Add("03E", "MaxConflictMembers", Doc(duplicates, "OBJECTS"), 4);
        Add("03E", "MaxTraversalWork", Doc(ObjectEntity, "OBJECTS"), 5); // edge, node, owner index, DFS node, DFS pop.
        Add("03E", "MaxSourcePairs", Doc(ObjectEntity, "OBJECTS"), Pairs(Doc(ObjectEntity, "OBJECTS")));
        Add("03E", "MaxStoredDiagnostics", Doc(ObjectEntity.Replace("5\nA", "5\nG") + ObjectEntity.Replace("5\nA", "5\nH"), "OBJECTS"), 3);
        // Controlled oracle counts: 333 children +9 table headers, 333 associations, 59 diagnostic refs.
        Add("03F", "MaxSourceLocations", "CONTROLLED", 342);
        Add("03F", "MaxChildAssociations", "CONTROLLED", 333);
        Add("03F", "MaxDiagnosticReferences", "CONTROLLED", 59);
        return cases.Select(c => new object[] { c.Stage, c.Property, c.Text, c.RequiredBudget });
    }
    private static T Limits<T>(string property, int value) where T : new()
    {
        var limits = new T(); var p = typeof(T).GetProperty(property)!;
        // Test-only construction of init properties; no production hook or dynamic production config.
        p.SetValue(limits, Convert.ChangeType(value, p.PropertyType, CultureInfo.InvariantCulture)); return limits;
    }
    [Theory, MemberData(nameof(LimitCases))]
    public async Task EveryEnforcedLimitHasBelowExactFirstOverEvidence(string stage, string property, string text, int required)
    {
        var lex = text == "CONTROLLED" ? (await FreshControlled()).LexicalEvidence : await Lex(text);
        var b = new TextDxfStructuralReader().Read(lex); var c = new TextDxfGeometryReader().Read(b);
        var d = new TextDxfBlockAnnotationReader().Read(c); var e = new TextDxfIdentityGraphReader().Read(d);
        var outcomes = new List<object>();
        foreach (var offset in new[] { 1, 0, -1 })
        {
            var budget = required + offset; string status; bool complete;
            switch (stage)
            {
                case "03A":
                    using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(text), false))
                    { var r = await new TextDxfLexicalReader(Limits<DxfReaderLimits>(property, budget)).ReadAsync(stream); status = r.Status.ToString(); complete = r.Status == DxfLexicalStatus.Succeeded; }
                    break;
                case "03B": { var r = new TextDxfStructuralReader(Limits<DxfStructuralLimits>(property, budget)).Read(lex); status = r.Status.ToString(); complete = r.StructureValidated; break; }
                case "03C": { var r = new TextDxfGeometryReader(Limits<DxfGeometryLimits>(property, budget)).Read(b); status = r.Status.ToString(); complete = r.GeometryMappingComplete; break; }
                case "03D": { var r = new TextDxfBlockAnnotationReader(Limits<DxfBlockAnnotationLimits>(property, budget)).Read(c); status = r.Status.ToString(); complete = r.MappingComplete; break; }
                case "03E": { var r = new TextDxfIdentityGraphReader(Limits<DxfIdentityGraphLimits>(property, budget)).Read(d); status = r.Status.ToString(); complete = r.GraphValidated; break; }
                default: { var r = new TextDxfRawDrawingSnapshotAssembler(Limits<RawDrawingSnapshotAssemblyLimits>(property, budget)).Assemble(e); status = r.Status.ToString(); complete = r.Completeness.SnapshotAssemblyComplete; if (offset == -1) Assert.Null(r.Snapshot); break; }
            }
            outcomes.Add(new { Budget = budget, Relation = offset == 1 ? "below" : offset == 0 ? "exact" : "first-over", Status = status, Complete = complete });
            if (offset == -1) { Assert.Equal("ResourceLimitExceeded", status); Assert.False(complete); }
            else Assert.NotEqual("ResourceLimitExceeded", status);
        }
        Evidence("limit-" + stage + "-" + property + ".json", JsonSerializer.Serialize(outcomes));
    }
    [Fact]
    public void LimitMatrixHasAnEntryForEveryEnforcedConfigurationProperty()
    {
        var cases = LimitCases().Select(a => (Stage: (string)a[0], Property: (string)a[1])).ToArray();
        foreach (var (stage, type) in new[] { ("03A", typeof(DxfReaderLimits)), ("03B", typeof(DxfStructuralLimits)), ("03C", typeof(DxfGeometryLimits)), ("03D", typeof(DxfBlockAnnotationLimits)), ("03E", typeof(DxfIdentityGraphLimits)), ("03F", typeof(RawDrawingSnapshotAssemblyLimits)) })
            foreach (var p in type.GetProperties())
                if (!(stage == "03A" && p.Name == "MaxControlGroupDepth")) Assert.Contains((stage, p.Name), cases);
        Assert.DoesNotContain(("03A", "MaxControlGroupDepth"), cases); // Explicitly reserved; actual depth boundary tested in03B.
    }
}
