using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DtoB.Cad.Dxf;
using DtoB.Cad.Raw;
using Xunit;

namespace DtoB.Tests;

public class TextDxfBlockAnnotationReaderTests
{
    private const string SourceHash = "2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC";
    private const string Header = "0\nSECTION\n2\nHEADER\n9\n$ACADVER\n1\nAC1032\n0\nENDSEC\n";
    private const string Insert = "0\nINSERT\n2\nTARGET\n10\n1\n20\n2\n";
    private const string Text = "0\nTEXT\n1\nabc\n10\n1\n20\n2\n40\n3\n";
    private const string Block = "0\nBLOCK\n2\nB\n10\n0\n20\n0\n";
    private const string Attrib = "0\nATTRIB\n1\nvalue\n2\nTAG\n10\n1\n20\n2\n40\n3\n";
    private static string Root()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "DtoB.sln"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidDataException("Repository root not found.");
    }
    private static string Doc(string entities, string? blocks = null) => Header +
        (blocks == null ? "" : "0\nSECTION\n2\nBLOCKS\n" + blocks + "0\nENDSEC\n") +
        "0\nSECTION\n2\nENTITIES\n" + entities + "0\nENDSEC\n0\nEOF\n";
    private static string FixtureText(string name) => File.ReadAllText(Path.Combine(Root(), "datasets/controlled/phase03d", name + ".dxf"), new UTF8Encoding(false, true));
    private static async Task<DxfGeometryReadResult> Geometry(string text)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text), false);
        return new TextDxfGeometryReader().Read(new TextDxfStructuralReader().Read(await new TextDxfLexicalReader().ReadAsync(stream)));
    }
    private static async Task<DxfBlockAnnotationReadResult> Read(string text, DxfBlockAnnotationLimits? limits = null) =>
        new TextDxfBlockAnnotationReader(limits).Read(await Geometry(text));
    private static Task<DxfBlockAnnotationReadResult> Fixture(string name) => Read(FixtureText(name));
    private static void Accounting(RawBlockAnnotationRecord r) => Assert.Equal(
        Enumerable.Range(r.Source.StartPairIndex, r.Source.EndPairIndex - r.Source.StartPairIndex + 1),
        r.Fields.SelectMany(f => f.Fact.SourcePairIndices).Concat(r.UnmappedPairIndices).OrderBy(i => i));
    private static void Absent(DxfSourceFact f)
    { Assert.Equal(DxfFactPresence.Absent, f.Presence); Assert.Equal(DxfFactValidity.NotApplicable, f.Validity); Assert.Null(f.Real); Assert.Null(f.Integer); Assert.Null(f.Text); Assert.Empty(f.SourcePairIndices); }
    private sealed record Pair(int Index, int Code, string Text);
    private sealed record SourceRecord(string Type, string Section, int Start, int End);
    private static (Pair[] Pairs, SourceRecord[] Records) Scan(string path)
    {
        // BCL-only source oracle; no production reader grouping, contexts or facts.
        var lines = File.ReadAllLines(path, new UTF8Encoding(false, true));
        var pairs = Enumerable.Range(0, lines.Length / 2).Select(i => new Pair(i, int.Parse(lines[2 * i], CultureInfo.InvariantCulture), lines[2 * i + 1])).ToArray();
        var zero = pairs.Where(p => p.Code == 0).ToArray(); var records = new List<SourceRecord>(); var section = "";
        for (var i = 0; i < zero.Length - 1; i++)
        {
            var p = zero[i];
            if (p.Text == "SECTION") section = pairs[p.Index + 1].Text;
            else if (p.Text == "ENDSEC") section = "";
            else records.Add(new(p.Text, section, p.Index, zero[i + 1].Index - 1));
        }
        return (pairs, records.ToArray());
    }
    private static void Match(DxfSourceFact f, int code, Pair[] raw)
    {
        var occurrences = raw.Where(p => p.Code == code).ToArray();
        if (occurrences.Length == 0) { Absent(f); return; }
        var p = Assert.Single(occurrences); Assert.Equal(DxfFactValidity.Valid, f.Validity);
        Assert.Equal(p.Index, Assert.Single(f.SourcePairIndices)); Assert.Equal(2 * p.Index + 1, f.Source!.StartLine); Assert.Equal(2 * p.Index + 2, f.Source.EndLine);
        if (f.Kind == DxfFactKind.Text) Assert.Equal(p.Text, f.Text);
        else if (f.Kind == DxfFactKind.Integer) Assert.Equal(int.Parse(p.Text, CultureInfo.InvariantCulture), f.Integer);
        else Assert.Equal(double.Parse(p.Text, CultureInfo.InvariantCulture), f.Real);
    }

    [Fact]
    public async Task ControlledIndependentCodeZeroAuditAll93RecordsAndEveryField()
    {
        var path = Path.Combine(Root(), "datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf");
        Assert.Equal(SourceHash, Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))));
        var (pairs, source) = Scan(path); var types = new[] { "BLOCK", "ENDBLK", "INSERT", "MTEXT", "TEXT", "DIMENSION" };
        var expected = source.Where(r => types.Contains(r.Type)).ToArray();
        Assert.Equal(new[] { 12, 12, 25, 4, 36, 4 }, types.Select(t => expected.Count(r => r.Type == t)));
        Assert.Equal(8, expected.Count(r => r.Type == "INSERT" && r.Section == "BLOCKS"));
        Assert.Equal(17, expected.Count(r => r.Type == "INSERT" && r.Section == "ENTITIES"));
        using var stream = File.OpenRead(path);
        var geometry = new TextDxfGeometryReader().Read(new TextDxfStructuralReader().Read(await new TextDxfLexicalReader().ReadAsync(stream)));
        var before = JsonSerializer.Serialize(geometry); var result = new TextDxfBlockAnnotationReader().Read(geometry);
        Assert.Equal(before, JsonSerializer.Serialize(geometry)); Assert.Same(geometry, result.GeometryEvidence);
        Assert.Equal(DxfBlockAnnotationStatus.Partial, result.Status); Assert.True(result.MappingComplete); Assert.Empty(result.Diagnostics);
        Assert.Equal(93, result.Records.Count); Assert.Equal(333, result.EffectiveRecordDispositionCounts.Sum(c => c.Count));
        Assert.Equal(281, result.EffectiveRecordDispositionCounts.Single(c => c.Disposition == DxfDisposition.Supported).Count);
        Assert.Equal(52, result.EffectiveRecordDispositionCounts.Single(c => c.Disposition == DxfDisposition.Unsupported).Count);
        Assert.DoesNotContain(result.EffectiveRecordDispositionCounts, c => c.Disposition is DxfDisposition.Invalid or DxfDisposition.DeferredTo03D);
        for (var n = 0; n < expected.Length; n++)
        {
            var r = result.Records[n]; var e = expected[n]; var raw = pairs.Skip(e.Start).Take(e.End - e.Start + 1).ToArray();
            Assert.Equal(e.Type, r.EntityType); Assert.Equal(e.Start, r.Source.StartPairIndex); Assert.Equal(e.End, r.Source.EndPairIndex);
            Assert.Equal(e.Section, r.SourceRecord.Scope); Assert.Same(result.StructuralEvidence.Records[r.StructuralRecordIndex], r.SourceRecord);
            Accounting(r); Assert.Equal(DxfDisposition.Supported, r.Disposition);
            // Every present mapped scalar independently checked against original physical lines.
            foreach (var f in r.Fields.Select(f => f.Fact).Where(f => f.Presence == DxfFactPresence.Present))
                Match(f, pairs[Assert.Single(f.SourcePairIndices)].Code, raw);
            if (r.Payload is RawInsert insert)
            {
                Match(insert.TargetName, 2, raw); Match(insert.Insertion.X, 10, raw); Match(insert.Insertion.Y, 20, raw); Match(insert.Insertion.Z, 30, raw);
                Match(insert.Scale.X, 41, raw); Match(insert.Scale.Y, 42, raw); Match(insert.Scale.Z, 43, raw); Match(insert.RotationDegrees, 50, raw);
                Match(insert.Extrusion.X, 210, raw); Match(insert.Extrusion.Y, 220, raw); Match(insert.Extrusion.Z, 230, raw);
                Match(insert.Columns, 70, raw); Match(insert.Rows, 71, raw); Match(insert.ColumnSpacing, 44, raw); Match(insert.RowSpacing, 45, raw); Match(insert.AttributesFollow, 66, raw);
                Assert.False(insert.OccurrenceExpansionUnsupported);
                Assert.Contains(raw, p => p.Code == 5); Assert.Contains(raw, p => p.Code == 330);
                if (e.Section == "BLOCKS") Assert.Single(result.BlockDefinitions, b => b.Children.Any(c => c.StructuralRecordIndex == r.StructuralRecordIndex));
            }
            if (r.Payload is RawBlockBegin b)
            { Match(b.Name, 2, raw); Match(b.AlternateName, 3, raw); Match(b.BasePoint.X, 10, raw); Match(b.BasePoint.Y, 20, raw); Match(b.BasePoint.Z, 30, raw); Match(b.Flags, 70, raw); Match(b.XrefPath, 1, raw); Match(b.Description, 4, raw); }
            if (r.Payload is RawText text) Match(text.Content, 1, raw);
            if (r.Payload is RawMText mt)
            { Assert.Equal(raw.Where(p => p.Code is 1 or 3).Select(p => p.Text), mt.Chunks.Select(c => c.Content.Text)); Assert.Equal(raw.Where(p => p.Code is 1 or 3).Select(p => p.Index), mt.Chunks.Select(c => Assert.Single(c.Content.SourcePairIndices))); }
            if (r.Payload is RawDimension d)
            {
                Assert.Equal(32, d.TypeFlags.Integer); Assert.True(d.SubtypeProfileSupported); Absent(d.SourceMeasurement);
                Assert.Contains(raw, p => p.Code == 100 && p.Text == "AcDbAlignedDimension"); Assert.Contains(raw, p => p.Code == 100 && p.Text == "AcDbRotatedDimension");
                Match(d.FirstExtensionPoint!.X, 13, raw); Match(d.FirstExtensionPoint.Y, 23, raw); Match(d.SecondExtensionPoint!.X, 14, raw); Match(d.SecondExtensionPoint.Y, 24, raw);
            }
        }
        var names = result.Records.Select(r => r.Payload).OfType<RawBlockBegin>().Select(b => b.Name.Text);
        Assert.Equal(new[] { "*Model_Space", "*Paper_Space", "_ARCHTICK", "_CLOSEDFILLED", "_CLOSEDBLANK", "COLUMN_400", "D_SINGLE_900", "W_SINGLE_1200", "*D1", "*D2", "*D3", "*D4" }, names);
        Assert.Equal(12, result.BlockDefinitions.Count);
        // Independent delimiter matching and all child indices/ranges, including old geometry references.
        foreach (var b in result.BlockDefinitions)
        {
            var begin = result.StructuralEvidence.Records[b.BeginRecordIndex]; var end = result.StructuralEvidence.Records[b.EndRecordIndex!.Value];
            var independentChildren = source.Where(r => r.Start > begin.Source.StartPairIndex && r.End < end.Source.StartPairIndex).ToArray();
            Assert.Equal(begin.Source.StartPairIndex, b.Source.StartPairIndex); Assert.Equal(end.Source.EndPairIndex, b.Source.EndPairIndex);
            Assert.Equal(independentChildren.Select(c => c.Start), b.Children.Select(c => result.StructuralEvidence.Records[c.StructuralRecordIndex].Source.StartPairIndex));
            foreach (var c in b.Children)
            {
                if (c.GeometryRecordIndex.HasValue) Assert.Same(result.StructuralEvidence.Records[c.StructuralRecordIndex], geometry.Geometry[c.GeometryRecordIndex.Value].SourceRecord);
                if (c.AnnotationRecordIndex.HasValue) Assert.Same(result.StructuralEvidence.Records[c.StructuralRecordIndex], result.Records[c.AnnotationRecordIndex.Value].SourceRecord);
            }
        }
        Assert.Equal(44, result.BlockDefinitions.Sum(b => b.Children.Count)); Assert.Equal(89, geometry.Geometry.Count);
        // Hand-reviewed pair/line goldens, independent of production indexing.
        var first = Assert.IsType<RawBlockBegin>(result.Records.Single(r => r.Source.StartPairIndex == 2662).Payload);
        Assert.Equal(2668, Assert.Single(first.Name.SourcePairIndices)); Assert.Equal(5337, first.Name.Source!.StartLine); Assert.Equal("", first.XrefPath.Text);
        var dim = Assert.IsType<RawDimension>(result.Records.Single(r => r.Source.StartPairIndex == 3915).Payload);
        Assert.Equal(3930, Assert.Single(dim.TypeFlags.SourcePairIndices)); Assert.Equal("<>", dim.TextOverride.Text); Assert.Equal(8000, dim.SecondExtensionPoint!.X.Real);
    }

    [Theory]
    [InlineData("blocks-nested")][InlineData("insert-array")][InlineData("text-alignment")][InlineData("mtext-chunks")]
    [InlineData("attributes")][InlineData("attributes-malformed")][InlineData("dimension-aligned")][InlineData("dimension-unsupported")]
    [InlineData("duplicate-nonfinite")][InlineData("foreign-contexts")][InlineData("block-malformed")][InlineData("legacy-seqend")]
    public async Task HandReviewedFixturesUseRealFullPipelineAndExactAccounting(string name)
    { var r = await Fixture(name); Assert.NotEmpty(r.Records); Assert.All(r.Records, Accounting); Assert.True(r.StructuralEvidence.StructureValidated); }

    [Fact]
    public async Task NonZeroBaseNestedInsertNativeFactsAndNoGeometryClones()
    {
        var r = await Fixture("blocks-nested"); var b = Assert.IsType<RawBlockBegin>(r.Records[0].Payload);
        Assert.Equal(12.5, b.BasePoint.X.Real); Assert.Equal(-30, b.BasePoint.Y.Real); Assert.Equal(7, b.BasePoint.Z.Real);
        var inserts = r.Records.Select(x => x.Payload).OfType<RawInsert>().ToArray(); Assert.Equal(2, inserts.Length);
        Assert.All(inserts, i => { Assert.Equal("NONZERO", i.TargetName.Text); Assert.Equal(100, i.Insertion.X.Real); Assert.Equal(200, i.Insertion.Y.Real); Assert.Equal(300, i.Insertion.Z.Real); Assert.Equal(2, i.Scale.X.Real); Assert.Equal(3, i.Scale.Y.Real); Assert.Equal(4, i.Scale.Z.Real); Assert.Equal(45, i.RotationDegrees.Real); Assert.Equal(1, i.Extrusion.Y.Real); });
        Assert.Single(r.GeometryEvidence.Geometry); Assert.Equal(2, r.BlockDefinitions.Count); Assert.Equal(2, r.BlockDefinitions[0].Children.Count);
        Assert.Equal("*D99", Assert.IsType<RawBlockBegin>(r.Records.Single(x => x.EntityType == "BLOCK" && x != r.Records[0]).Payload).Name.Text);
    }
    [Fact]
    public async Task MissingInsertDefaultsAndZRemainAbsent()
    {
        var r = await Read(Doc(Insert)); var i = Assert.IsType<RawInsert>(Assert.Single(r.Records).Payload);
        foreach (var f in new[] { i.Insertion.Z, i.Scale.X, i.Scale.Y, i.Scale.Z, i.RotationDegrees, i.Columns, i.Rows, i.ColumnSpacing, i.RowSpacing, i.AttributesFollow, i.Extrusion.X, i.Extrusion.Y, i.Extrusion.Z }) Absent(f);
    }
    [Theory][InlineData("70\n2\n")][InlineData("71\n3\n")][InlineData("100\nAcDbMInsertBlock\n")]
    public async Task ArrayRetainedOnceUnsupportedExpansionAndFollowingEntitySurvives(string array)
    {
        var r = await Read(Doc(Insert + array + Text)); Assert.Equal(2, r.Records.Count);
        Assert.True(Assert.IsType<RawInsert>(r.Records[0].Payload).OccurrenceExpansionUnsupported); Assert.Equal(DxfDisposition.Unsupported, r.Records[0].Disposition);
        Assert.IsType<RawText>(r.Records[1].Payload); Assert.Empty(r.GeometryEvidence.Geometry); Assert.False(r.MappingComplete);
        Assert.Contains(r.Diagnostics, d => d.Code == DxfBlockAnnotationDiagnosticCode.UnsupportedOccurrenceExpansion && d.Source == r.Records[0].Source);
    }
    [Fact]
    public async Task TextRawUnicodeEscapesAlignmentAndOptionalAbsence()
    {
        var r = await Fixture("text-alignment"); var t = Assert.IsType<RawText>(Assert.Single(r.Records).Payload);
        Assert.Equal("한글 %%d \\U+0041", t.Content.Text); Assert.Equal(4, t.Alignment.X.Real); Assert.Equal(5, t.Alignment.Y.Real); Assert.Equal(6, t.Alignment.Z.Real);
        Assert.Equal(90, t.RotationDegrees.Real); Assert.Equal("RAW_STYLE", t.Style.Text); Assert.Equal(2, t.HorizontalJustification.Integer); Assert.Equal(3, t.VerticalJustification.Integer); Assert.Equal(.5, t.Thickness.Real);
        var minimal = Assert.IsType<RawText>(Assert.Single((await Read(Doc(Text))).Records).Payload);
        foreach (var f in new[] { minimal.Insertion.Z, minimal.Alignment.X, minimal.Alignment.Y, minimal.Alignment.Z, minimal.RotationDegrees, minimal.Style, minimal.HorizontalJustification, minimal.VerticalJustification, minimal.Thickness }) Absent(f);
    }
    [Fact]
    public async Task MTextOrderedChunksFormattingSpansAndPlacementStayRaw()
    {
        var r = await Fixture("mtext-chunks"); var t = Assert.IsType<RawMText>(Assert.Single(r.Records).Payload);
        Assert.Equal(new[] { 3, 3, 1 }, t.Chunks.Select(c => c.GroupCode)); Assert.Equal(new[] { "{\\fArial;첫째\\P", "둘째 %%d ", "끝}" }, t.Chunks.Select(c => c.Content.Text));
        Assert.Equal(t.Chunks.Select(c => c.Content.Source!.StartPairIndex).OrderBy(i => i), t.Chunks.Select(c => c.Content.Source!.StartPairIndex));
        Assert.Equal(1, t.Direction.X.Real); Assert.Equal(0, t.Direction.Y.Real); Assert.Equal(50, t.RectangleWidth.Real); Assert.Equal(5, t.Attachment.Integer);
        Assert.Equal(1, t.DrawingDirection.Integer); Assert.Equal(2, t.SpacingStyle.Integer); Assert.Equal(1.5, t.SpacingFactor.Real);
    }
    [Theory][InlineData("3\na\n", DxfBlockAnnotationDiagnosticCode.MissingRequiredField)]
    [InlineData("1\na\n1\nb\n", DxfBlockAnnotationDiagnosticCode.InvalidMTextChunks)]
    [InlineData("1\na\n3\nb\n", DxfBlockAnnotationDiagnosticCode.InvalidMTextChunks)]
    public async Task MalformedMTextRetainsAllChunksAndLocatedDiagnostics(string chunks, DxfBlockAnnotationDiagnosticCode code)
    {
        var r = await Read(Doc("0\nMTEXT\n10\n1\n20\n2\n40\n3\n" + chunks));
        Assert.False(r.MappingComplete); Assert.Equal(DxfDisposition.Invalid, Assert.Single(r.Records).Disposition); Accounting(r.Records[0]);
        Assert.Contains(r.Diagnostics, d => d.Code == code && d.Source != null);
    }
    [Fact]
    public async Task AlignedDimensionOnlySourceMeasurementAndEmptyOverrideNoDerivedMeasurement()
    {
        var r = await Fixture("dimension-aligned"); var d = Assert.IsType<RawDimension>(Assert.Single(r.Records).Payload);
        Assert.Equal(33, d.TypeFlags.Integer); Assert.True(d.SubtypeProfileSupported); Assert.Equal("", d.TextOverride.Text); Assert.Equal(123.456, d.SourceMeasurement.Real);
        Assert.Equal(1, d.FirstExtensionPoint!.X.Real); Assert.Equal(4, d.SecondExtensionPoint!.Y.Real); Assert.Equal(22.5, d.RotationDegrees!.Real);
        var text = FixtureText("dimension-aligned").Replace("42\n123.456\n", "").Replace("1\n\n", "");
        d = Assert.IsType<RawDimension>(Assert.Single((await Read(text)).Records).Payload); Absent(d.SourceMeasurement); Absent(d.TextOverride);
    }
    [Fact]
    public async Task UnsupportedDimensionHasCommonCoverageAndSubtypePairsUnmapped()
    {
        var r = await Fixture("dimension-unsupported"); var record = Assert.Single(r.Records); var d = Assert.IsType<RawDimension>(record.Payload);
        Assert.False(d.SubtypeProfileSupported); Assert.Equal(DxfDisposition.Unsupported, record.Disposition); Assert.Null(d.FirstExtensionPoint); Assert.Null(d.SecondExtensionPoint); Assert.Null(d.RotationDegrees);
        Assert.Equal(123.456, d.SourceMeasurement.Real); Assert.Contains(r.Diagnostics, x => x.Code == DxfBlockAnnotationDiagnosticCode.UnsupportedDimensionSubtype);
        Assert.Contains(record.UnmappedPairIndices, i => r.LexicalEvidence.Pairs[i].GroupCode == 13); Accounting(record);
    }
    [Fact]
    public async Task AttributeSequenceAndAttDefHaveSeparateValuesAndSourceAssociations()
    {
        var r = await Fixture("attributes"); var seq = Assert.Single(r.AttributeSequences);
        var attribute = Assert.IsType<RawAttrib>(r.Records.Single(x => x.EntityType == "ATTRIB").Payload);
        var definition = Assert.IsType<RawAttDef>(r.Records.Single(x => x.EntityType == "ATTDEF").Payload);
        Assert.Equal("explicit value", attribute.Text.Content.Text); Assert.Equal("default only", definition.Text.Content.Text); Assert.Equal("TAG", attribute.Tag.Text); Assert.Equal("Prompt?", definition.Prompt.Text);
        Assert.Equal(1, attribute.Flags.Integer); Assert.Equal(30, attribute.Text.RotationDegrees.Real); Assert.Equal("ATTR_STYLE", attribute.Text.Style.Text);
        Assert.Equal(r.Records.Single(x => x.EntityType == "ATTRIB").StructuralRecordIndex, Assert.Single(seq.AttributeRecordIndices)); Assert.NotNull(seq.EndRecordIndex);
        Assert.Equal("InsertAttributes", Assert.IsType<RawSequenceEnd>(r.Records.Single(x => x.EntityType == "SEQEND").Payload).Role); Assert.True(r.MappingComplete);
    }
    [Fact]
    public async Task MalformedAttributeSequenceDoesNotSwallowFollowingText()
    {
        var r = await Fixture("attributes-malformed"); Assert.Equal(DxfBlockAnnotationStatus.InvalidInput, r.Status); Assert.False(r.MappingComplete);
        Assert.Null(Assert.Single(r.AttributeSequences).EndRecordIndex); Assert.IsType<RawText>(r.Records.Last().Payload);
        Assert.Contains(r.Diagnostics, d => d.Code == DxfBlockAnnotationDiagnosticCode.MissingAttributeSequenceEnd && d.Source != null);
    }
    [Theory][InlineData("orphan")][InlineData("end")][InlineData("zero")]
    public async Task AttributeSequencesRequireExplicitActiveContext(string scenario)
    {
        var body = scenario == "orphan" ? Attrib : Insert + "66\n1\n" + Attrib;
        if (scenario == "zero") body = Insert + "66\n0\n" + Attrib + "0\nSEQEND\n";
        var r = await Read(Doc(body)); Assert.False(r.MappingComplete); Assert.Equal(DxfBlockAnnotationStatus.InvalidInput, r.Status);
        Assert.Contains(r.Diagnostics, d => d.Source != null && d.Code is DxfBlockAnnotationDiagnosticCode.OrphanAttribute or DxfBlockAnnotationDiagnosticCode.MissingAttributeSequenceEnd);
    }
    [Fact]
    public async Task LegacyAndUnassignedSeqendNeverBecomeInsertAttributes()
    {
        var r = await Fixture("legacy-seqend"); Assert.Empty(r.AttributeSequences); Assert.DoesNotContain(r.Records, x => x.Payload is RawSequenceEnd);
        Assert.Contains(r.Diagnostics, d => d.Code == DxfBlockAnnotationDiagnosticCode.LegacySequenceGated);
        Assert.Contains(r.Diagnostics, d => d.Code == DxfBlockAnnotationDiagnosticCode.UnassignedSequenceEnd);
        Assert.Contains(r.Records, x => x.EntityType == "SEQEND" && x.Disposition == DxfDisposition.DeferredTo03D); Assert.IsType<RawText>(r.Records.Last().Payload);
    }
    [Theory][InlineData("nested", DxfBlockAnnotationDiagnosticCode.NestedBlock)]
    [InlineData("missing", DxfBlockAnnotationDiagnosticCode.MissingBlockEnd)]
    [InlineData("unexpected", DxfBlockAnnotationDiagnosticCode.UnexpectedBlockEnd)]
    [InlineData("outside", DxfBlockAnnotationDiagnosticCode.ChildOutsideBlock)]
    public async Task MalformedBlockBoundariesAreLocatedAndIncomplete(string scenario, DxfBlockAnnotationDiagnosticCode code)
    {
        var body = scenario switch { "nested" => Block + Block + "0\nENDBLK\n", "missing" => Block + Insert, "unexpected" => "0\nENDBLK\n", _ => Insert };
        var r = await Read(Doc(Text, body)); Assert.Equal(DxfBlockAnnotationStatus.InvalidInput, r.Status); Assert.False(r.MappingComplete);
        Assert.Contains(r.Diagnostics, d => d.Code == code && d.Source != null); Assert.IsType<RawText>(r.Records.Last().Payload);
    }
    [Theory][InlineData("NaN")][InlineData("Infinity")][InlineData("-Infinity")][InlineData("1e9999")][InlineData("bad")]
    public async Task NonfiniteAnnotationValueRetainsRawEvidenceWithoutCoercion(string number)
    {
        var r = await Read(Doc(Text.Replace("40\n3\n", "40\n" + number + "\n"))); var t = Assert.IsType<RawText>(Assert.Single(r.Records).Payload);
        Assert.Equal(DxfFactValidity.Invalid, t.Height.Validity); Assert.Null(t.Height.Real); Assert.Equal(number, r.LexicalEvidence.Pairs[Assert.Single(t.Height.SourcePairIndices)].RawTextValue);
        Assert.Contains(r.Diagnostics, d => d.Code == DxfBlockAnnotationDiagnosticCode.InvalidNumericValue && d.Source == t.Height.Source);
    }
    [Theory][InlineData("1\ndef\n", "Content")][InlineData("40\n4\n", "Height")][InlineData("10\n5\n", "Insertion.X")]
    public async Task DuplicateSingletonHasAllOccurrencesNoWinner(string extra, string field)
    {
        var r = await Read(Doc(Text + extra)); var f = Assert.Single(r.Records).Fields.Single(x => x.Name == field).Fact;
        Assert.Equal(2, f.SourcePairIndices.Count); Assert.Equal(DxfFactValidity.Invalid, f.Validity); Assert.Null(f.Text); Assert.Null(f.Real); Accounting(r.Records[0]);
    }
    [Fact]
    public async Task ForeignSubclassControlAndXdataCannotPopulateInsertFields()
    {
        var r = await Fixture("foreign-contexts"); var i = Assert.IsType<RawInsert>(Assert.Single(r.Records).Payload);
        Assert.Equal("RIGHT", i.TargetName.Text); Assert.Equal(1, i.Insertion.X.Real); Absent(i.Scale.X); Absent(i.Scale.Y);
        Assert.Contains(r.Records[0].UnmappedPairIndices, n => r.LexicalEvidence.Pairs[n].RawTextValue == "WRONG"); Accounting(r.Records[0]);
        var missing = await Read(Doc("0\nTEXT\n100\nForeign\n1\nwrong\n100\nAcDbText\n10\n1\n20\n2\n40\n3\n"));
        Assert.Equal(DxfFactValidity.Invalid, Assert.IsType<RawText>(Assert.Single(missing.Records).Payload).Content.Validity);
    }
    [Fact]
    public async Task InvariantCultureAndImmutablePublication()
    {
        var previous = CultureInfo.CurrentCulture;
        try { CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR"); var r = await Read(Doc(Text.Replace("40\n3\n", "40\n3.5\n"))); Assert.Equal(3.5, Assert.IsType<RawText>(Assert.Single(r.Records).Payload).Height.Real);
            Assert.Throws<NotSupportedException>(() => ((IList<RawBlockAnnotationRecord>)r.Records).Clear()); Assert.Throws<NotSupportedException>(() => ((IList<DxfNamedFact>)r.Records[0].Fields).Clear()); }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    public static IEnumerable<object[]> Budgets()
    {
        foreach (var kind in new[] { "definitions", "children", "inserts", "annotations", "chunks", "text", "attributes", "mapped", "fields", "structural", "pairs" }) yield return [kind];
    }
    [Theory][MemberData(nameof(Budgets))]
    public async Task ExactLimitSucceedsFirstOverNeverTruncatedSuccess(string kind)
    {
        var text = kind switch
        {
            "definitions" => Doc(Text, Block + "0\nENDBLK\n" + Block + "0\nENDBLK\n"),
            "children" => Doc(Text, Block + Insert + Insert + "0\nENDBLK\n"),
            "inserts" => Doc(Insert + Insert), "annotations" => Doc(Text + Text), "chunks" => FixtureText("mtext-chunks"),
            "attributes" => Doc(Insert + "66\n1\n" + Attrib + Attrib + "0\nSEQEND\n"), "mapped" => Doc(Insert + Text), _ => Doc(Text)
        };
        var g = await Geometry(text); var pairCount = g.StructuralEvidence.LexicalEvidence.Pairs.Count;
        var chars = g.StructuralEvidence.LexicalEvidence.Pairs.Where(p => (p.GroupCode is 1 or 3 or 7 or 8) && p.PairIndex >= g.StructuralEvidence.Records[0].Source.StartPairIndex).Sum(p => p.RawTextValue.Length);
        var count = kind switch { "chunks" => 3, "text" => chars, "fields" => g.StructuralEvidence.Records[0].Source.EndPairIndex - g.StructuralEvidence.Records[0].Source.StartPairIndex + 1, "structural" => g.StructuralEvidence.Records.Count, "pairs" => pairCount, _ => 2 };
        DxfBlockAnnotationLimits L(int n) => kind switch
        {
            "definitions" => new() { MaxDefinitions = n }, "children" => new() { MaxChildrenPerBlock = n }, "inserts" => new() { MaxInserts = n },
            "annotations" => new() { MaxAnnotations = n }, "chunks" => new() { MaxMTextChunks = n }, "text" => new() { MaxRawAnnotationCharacters = n },
            "attributes" => new() { MaxAttributeSequenceLength = n }, "mapped" => new() { MaxMappedRecords = n }, "fields" => new() { MaxFieldsPerRecord = n },
            "structural" => new() { MaxStructuralRecords = n }, _ => new() { MaxSourcePairs = n }
        };
        Assert.True(new TextDxfBlockAnnotationReader(L(count)).Read(g).MappingComplete);
        // A one-record fixture cannot set a positive first-over structural limit; add another record.
        if (kind == "structural") { text = Doc(Text + Text); g = await Geometry(text); count = 2; }
        var failed = new TextDxfBlockAnnotationReader(L(count - 1)).Read(g);
        Assert.Equal(DxfBlockAnnotationStatus.ResourceLimitExceeded, failed.Status); Assert.False(failed.MappingComplete);
        Assert.Contains(failed.Diagnostics, d => d.Code == DxfBlockAnnotationDiagnosticCode.ResourceLimitExceeded);
    }
    [Fact]
    public async Task DiagnosticLimitReservesTerminalAndCancellationNeverComplete()
    {
        var exact = new TextDxfBlockAnnotationReader(new() { MaxStoredDiagnostics = 2 }).Read(await Geometry(Doc(Text + "1\nx\n")));
        Assert.Equal(DxfBlockAnnotationStatus.Partial, exact.Status); Assert.Single(exact.Diagnostics);
        var g = await Geometry(Doc(Text + "1\nx\n40\n4\n10\n5\n"));
        var r = new TextDxfBlockAnnotationReader(new() { MaxStoredDiagnostics = 2 }).Read(g);
        Assert.Equal(2, r.Diagnostics.Count); Assert.Equal(DxfBlockAnnotationStatus.ResourceLimitExceeded, r.Status); Assert.False(r.MappingComplete);
        Assert.Equal(DxfBlockAnnotationDiagnosticCode.DiagnosticLimitExceeded, r.Diagnostics.Last().Code);
        using var c = new CancellationTokenSource(); c.Cancel(); r = new TextDxfBlockAnnotationReader().Read(g, c.Token);
        Assert.Equal(DxfBlockAnnotationStatus.Canceled, r.Status); Assert.False(r.MappingComplete); Assert.Empty(r.Records);
        Assert.Throws<OperationCanceledException>(() => new DxfBlockAnnotationReadResult(g, DxfBlockAnnotationStatus.Partial, [], [], [], [], true, c.Token));
    }
    [Fact]
    public async Task ExplicitDefaultsArePresentAndDifferentFromAbsent()
    {
        var r = await Read(Doc(Insert + "30\n0\n41\n1\n42\n1\n43\n1\n50\n0\n70\n1\n71\n1\n44\n0\n45\n0\n210\n0\n220\n0\n230\n1\n", Block + "0\nENDBLK\n"));
        var i = Assert.IsType<RawInsert>(r.Records.Single(x => x.EntityType == "INSERT").Payload);
        Assert.Equal(DxfFactPresence.Present, i.Insertion.Z.Presence); Assert.Equal(0, i.Insertion.Z.Real); Assert.Equal(1, i.Scale.Z.Real); Assert.Equal(0, i.RotationDegrees.Real);
        Assert.Equal(1, i.Columns.Integer); Assert.Equal(1, i.Rows.Integer); Assert.Equal(0, i.ColumnSpacing.Real); Assert.Equal(1, i.Extrusion.Z.Real);
        Absent(Assert.IsType<RawBlockBegin>(r.Records[0].Payload).BasePoint.Z);
    }
    [Fact]
    public async Task ArrayFixturePreservesRowsColumnsSpacingAndSourceHandle()
    {
        var r = await Fixture("insert-array"); var a = r.Records[0]; var i = Assert.IsType<RawInsert>(a.Payload);
        Assert.Equal(2, i.Columns.Integer); Assert.Equal(3, i.Rows.Integer); Assert.Equal(10, i.ColumnSpacing.Real); Assert.Equal(20, i.RowSpacing.Real);
        Assert.Equal("I1", r.LexicalEvidence.Pairs[a.Source.StartPairIndex + 1].RawTextValue); Assert.Equal(2, r.Records.Count);
    }
    [Fact]
    public async Task InvalidAttributeDoesNotAdvertiseValidSequenceAndNoDefaultCopied()
    {
        var r = await Read(Doc(Insert + "66\n1\n" + Attrib.Replace("1\nvalue\n", "") + "0\nSEQEND\n"));
        Assert.Equal(DxfDisposition.Invalid, Assert.Single(r.AttributeSequences).Disposition); Assert.False(r.MappingComplete);
        Assert.Equal(DxfFactValidity.Invalid, Assert.IsType<RawAttrib>(r.Records.Single(x => x.EntityType == "ATTRIB").Payload).Text.Content.Validity);
    }
    [Theory][InlineData(0)][InlineData(2)]
    public async Task AttributeSequenceCannotCrossBlocksSectionBoundary(int following)
    {
        var r = await Read(Doc(following == 0 ? "0\nSEQEND\n" : Text, Block + Insert + "66\n1\n" + Attrib + "0\nENDBLK\n"));
        Assert.Null(Assert.Single(r.AttributeSequences).EndRecordIndex); Assert.False(r.MappingComplete);
        Assert.DoesNotContain(r.Records, x => x.Payload is RawSequenceEnd);
    }
    [Fact]
    public async Task RejectedVersionProducesNoMappedPublication()
    {
        var r = await Read(Doc(Text).Replace("AC1032", "AC1024"));
        Assert.Equal(DxfBlockAnnotationStatus.UnsupportedVersion, r.Status); Assert.False(r.MappingComplete); Assert.Empty(r.Records);
        Assert.Equal(DxfBlockAnnotationDiagnosticCode.InputRejected, Assert.Single(r.Diagnostics).Code);
    }
    [Fact]
    public async Task AnnotationTextBudgetCountsDuplicateAndForeignRawStrings()
    {
        var g = await Geometry(Doc(Text + "1\ndefgh\n"));
        var r = new TextDxfBlockAnnotationReader(new() { MaxRawAnnotationCharacters = 7 }).Read(g);
        Assert.Equal(DxfBlockAnnotationStatus.ResourceLimitExceeded, r.Status); Assert.False(r.MappingComplete);
        g = await Geometry(Doc(Text + "100\nForeign\n3\nlong foreign value\n"));
        r = new TextDxfBlockAnnotationReader(new() { MaxRawAnnotationCharacters = 4 }).Read(g);
        Assert.Equal(DxfBlockAnnotationStatus.ResourceLimitExceeded, r.Status);
    }
    [Fact]
    public async Task FreezeCancellationDuringEnumerationDoesNotPublishSuccess()
    {
        var g = await Geometry(Doc(Text)); var r = new TextDxfBlockAnnotationReader().Read(g); using var c = new CancellationTokenSource();
        IEnumerable<RawBlockAnnotationRecord> CancelingRecords()
        { yield return r.Records[0]; c.Cancel(); yield return r.Records[0]; }
        Assert.Throws<OperationCanceledException>(() => new DxfBlockAnnotationReadResult(g, DxfBlockAnnotationStatus.Succeeded, CancelingRecords(), [], [], [], true, c.Token));
    }
    [Theory][InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)][InlineData(5)]
    [InlineData(6)][InlineData(7)][InlineData(8)][InlineData(9)][InlineData(10)][InlineData(11)]
    public void InvalidLimitsAreRejectedBeforeReading(int index)
    {
        var l = index switch
        {
            0 => new DxfBlockAnnotationLimits { MaxDefinitions = 0 }, 1 => new() { MaxChildrenPerBlock = 0 }, 2 => new() { MaxInserts = 0 },
            3 => new() { MaxAnnotations = 0 }, 4 => new() { MaxMTextChunks = 0 }, 5 => new() { MaxRawAnnotationCharacters = 0 },
            6 => new() { MaxAttributeSequenceLength = 0 }, 7 => new() { MaxMappedRecords = 0 }, 8 => new() { MaxStoredDiagnostics = 1 },
            9 => new() { MaxFieldsPerRecord = 0 }, 10 => new() { MaxStructuralRecords = 0 }, _ => new() { MaxSourcePairs = 0 }
        };
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextDxfBlockAnnotationReader(l));
    }
    [Fact]
    public async Task IndependentFreshFileStreamsAreDeterministicAndPredecessorsUnchanged()
    {
        var path = Path.Combine(Root(), "datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf"); var serialized = new List<string>();
        for (var i = 0; i < 3; i++)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var lex = await new TextDxfLexicalReader().ReadAsync(stream); var structure = new TextDxfStructuralReader().Read(lex); var geometry = new TextDxfGeometryReader().Read(structure);
            var old = JsonSerializer.Serialize(geometry); var r = new TextDxfBlockAnnotationReader().Read(geometry);
            Assert.Equal(old, JsonSerializer.Serialize(geometry)); serialized.Add(JsonSerializer.Serialize(r));
        }
        Assert.Equal(serialized[0], serialized[1]); Assert.Equal(serialized[0], serialized[2]);
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(serialized[0])));
        var evidence = Environment.GetEnvironmentVariable("DTOB_03D_EVIDENCE_DIRECTORY");
        if (evidence != null) File.WriteAllText(Path.Combine(evidence, "determinism.sha256"), digest + "\n");
    }
}
