using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DtoB.Cad.Dxf;
using DtoB.Cad.Raw;
using Xunit;

namespace DtoB.Tests;

public class TextDxfStructuralReaderTests
{
    // Hand-written pair texts are source fixtures, not prebuilt parser results.
    private const string Header = "0\nSECTION\n2\nHEADER\n9\n$ACADVER\n1\nAC1032\n0\nENDSEC\n";
    private static string Doc(string body = "") => Header + body + "0\nEOF\n";
    private static string Section(string name, string body) => "0\nSECTION\n2\n" + name + "\n" + body + "0\nENDSEC\n";
    private static string Table(string name, string body, int count = 1) => Section("TABLES", "0\nTABLE\n2\n" + name + "\n70\n" + count + "\n" + body + "0\nENDTAB\n");
    private static async Task<DxfLexicalReadResult> Lex(string text)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text), false);
        return await new TextDxfLexicalReader().ReadAsync(stream);
    }
    private static async Task<DxfStructuralReadResult> Read(string text, DxfStructuralLimits? limits = null) => new TextDxfStructuralReader(limits).Read(await Lex(text));
    private static DxfSourceFact Field(DxfStructuralRecord entry, string name) => entry.Fields.Single(f => f.Name == name).Fact;
    private static string Root()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "DtoB.sln"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidDataException("Repository root not found.");
    }
    private static void Error(DxfStructuralReadResult r, DxfStructuralDiagnosticCode code)
    {
        Assert.Equal(DxfStructuralStatus.InvalidInput, r.Status);
        Assert.False(r.StructureValidated); Assert.False(r.MappingComplete);
        Assert.Contains(r.Diagnostics, d => d.Code == code);
    }

    [Fact]
    public async Task ControlledOriginalMatchesIndependentLineOracleAndGoldenLocations()
    {
        var path = Path.Combine(Root(), "datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf");
        const string hash = "2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC";
        Assert.Equal(hash, Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))));
        // Independent BCL line scan: no calls to any production structural helpers.
        var lines = File.ReadAllLines(path, new UTF8Encoding(false, true));
        var oracle = Enumerable.Range(0, lines.Length / 2).Select(i => (Code: int.Parse(lines[2 * i], CultureInfo.InvariantCulture), Text: lines[2 * i + 1], Index: i)).ToArray();
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var lex = await new TextDxfLexicalReader().ReadAsync(stream);
        stream.Dispose(); // 03B can consume evidence after its caller closes the original file.
        var r = new TextDxfStructuralReader().Read(lex);
        Directory.CreateDirectory(Path.Combine(Root(), "artifacts/phase03/03b-review-fixes-20261014"));
        File.WriteAllText(Path.Combine(Root(), "artifacts/phase03/03b-review-fixes-20261014/controlled-diagnostics.json"), JsonSerializer.Serialize(r.Diagnostics));
        Assert.Equal(DxfStructuralStatus.Partial, r.Status);
        Assert.True(r.StructureValidated); Assert.False(r.MappingComplete);
        Assert.Equal(DxfStructuralVersionAcceptance.Supported, r.VersionAcceptance);
        Assert.Same(lex, r.LexicalEvidence);
        Assert.Equal(new[] { "HEADER", "CLASSES", "TABLES", "BLOCKS", "ENTITIES", "OBJECTS" }, r.Sections.Select(s => s.Name));
        var sectionMarkers = oracle.Where(p => p.Code == 0 && p.Text == "SECTION").Select(p => p.Index).ToArray();
        Assert.Equal(new[] { 0, 557, 680, 2660, 3419, 4903 }, sectionMarkers);
        Assert.Equal(sectionMarkers, r.Sections.Select(s => s.Source.StartPairIndex));
        var h = r.Header!;
        AssertFact(h.AcadVersion, lex, 3, 8, "AC1032");
        AssertFact(h.DwgCodePage, lex, 7, 16, "ANSI_1252");
        AssertFact(h.InsertionUnits, lex, 455, 912, "4"); Assert.Equal(4, h.InsertionUnits.Integer);
        AssertFact(h.HandleSeed, lex, 277, 556, "161");
        AssertFact(h.ExtMin.X, lex, 17, 36, "1e+20"); AssertFact(h.ExtMax.X, lex, 21, 44, "-1e+20");
        AssertFact(h.ExtMin.Y, lex, 18, 38, "1e+20"); AssertFact(h.ExtMin.Z, lex, 19, 40, "1e+20");
        AssertFact(h.ExtMax.Y, lex, 22, 46, "-1e+20"); AssertFact(h.ExtMax.Z, lex, 23, 48, "-1e+20");
        Assert.Equal(1e20, h.ExtMin.X.Real); Assert.Equal(1e20, h.ExtMin.Y.Real); Assert.Equal(1e20, h.ExtMin.Z.Real);
        Assert.Equal(-1e20, h.ExtMax.X.Real); Assert.Equal(-1e20, h.ExtMax.Y.Real); Assert.Equal(-1e20, h.ExtMax.Z.Real);
        Assert.Equal(DxfReportedExtentsState.UnusableReportedExtents, h.ReportedExtentsState);
        Assert.Single(r.Diagnostics, d => d.Code == DxfStructuralDiagnosticCode.UnusableReportedExtents);
        Assert.Equal(oracle.Take(557).Count(p => p.Code == 9), h.Variables.Count);
        var variableMarkers = oracle.Take(557).Where(p => p.Code == 9).Select(p => p.Index).ToArray();
        Assert.Equal(variableMarkers, h.Variables.Select(v => v.Source.StartPairIndex));
        for (var i = 0; i < variableMarkers.Length; i++)
        {
            Assert.Equal((i + 1 < variableMarkers.Length ? variableMarkers[i + 1] : 556) - 1, h.Variables[i].Source.EndPairIndex);
            Assert.Equal(oracle[variableMarkers[i]].Text, h.Variables[i].Name);
        }
        Assert.Equal(9, r.Tables.Count);
        var independentCounts = new Dictionary<string, int>();
        var independentNames = new Dictionary<string, List<string>>();
        string? table = null;
        foreach (var p in oracle.Skip(682).Take(2660 - 682))
        {
            if (p.Code == 0 && p.Text == "TABLE") { table = oracle[p.Index + 1].Text; independentCounts[table] = 0; independentNames[table] = []; }
            else if (p.Code == 0 && p.Text == "ENDTAB") table = null;
            else if (table != null && p.Code == 0) independentCounts[table]++;
            else if (table != null && p.Code == 2 && p.Text != table) independentNames[table].Add(p.Text);
        }
        foreach (var t in r.Tables)
        {
            Assert.Equal(independentCounts[t.Name], t.ActualEntryCount);
            Assert.Equal("TABLE", oracle[t.Source.StartPairIndex].Text); Assert.Equal("ENDTAB", oracle[t.Source.EndPairIndex].Text);
            Assert.Equal(t.Source.StartPairIndex * 2 + 1, t.Source.StartLine);
        }
        Assert.Equal(12, independentCounts["BLOCK_RECORD"]); Assert.Equal(12, independentCounts["DIMSTYLE"]);
        Assert.Equal(21, independentCounts["LTYPE"]); Assert.Equal(27, independentCounts["STYLE"]);
        var layers = r.Tables.Single(t => t.Name == "LAYER");
        var names = layers.Entries.Select(e => Field(e, "Name").Text).ToArray();
        Assert.Equal(new[] { "0", "Defpoints", "A-WALL", "A-DOOR", "A-WINDOW", "A-FLOOR", "S-COLUMN", "A-GRID", "A-TEXT", "A-DIM", "A-LEVEL", "A-SECTION" }, names);
        Assert.Equal(independentNames["LAYER"], names);
        AssertFact(Field(layers.Entries.Single(e => Field(e, "Name").Text == "A-WALL"), "Name"), lex, 1150, 2302, "A-WALL");
        Assert.Equal(oracle.Count(p => p.Code == 0 && p.Text == "CLASS"), r.Records.Count(e => e.Scope == "CLASSES"));
        foreach (var scope in new[] { "BLOCKS", "ENTITIES", "OBJECTS" })
        {
            var s = r.Sections.Single(s => s.Name == scope);
            var markers = oracle.Where(p => p.Index > s.Source.StartPairIndex + 1 && p.Index < s.Source.EndPairIndex && p.Code == 0).ToArray();
            var actual = r.Records.Where(e => e.Scope == scope).ToArray();
            Assert.Equal(markers.Select(p => p.Index), actual.Select(e => e.Source.StartPairIndex));
            Assert.Equal(markers.Select(p => p.Text), actual.Select(e => e.RecordType));
            Assert.All(actual, e => Assert.NotEqual(DxfDisposition.Supported, e.Disposition));
        }
        Assert.All(r.Tables.Single(t => t.Name == "DIMSTYLE").Entries, e => Assert.All(Field(e, "RawHandle").SourcePairIndices, i => Assert.Equal(105, lex.Pairs[i].GroupCode)));
        var dimstyles = r.Tables.Single(t => t.Name == "DIMSTYLE").Entries;
        // Reviewed raw line goldens, corroborated by an independent code2/code69 source scan.
        var flagIndices = new[] { 1706, 1774, 1844, 1913, 1982, 2051, 2120, 2189, 2258, 2327, 2397, 2467 };
        var colorIndices = new[] { 1727, 1795, 1865, 1934, 2003, 2072, 2141, 2210, 2279, 2348, 2418, 2488 };
        for (var i = 0; i < dimstyles.Count; i++)
        {
            var entry = dimstyles[i];
            var rawEntry = oracle.Skip(entry.Source.StartPairIndex).Take(entry.Source.EndPairIndex - entry.Source.StartPairIndex + 1).ToArray();
            var namePair = rawEntry.Single(p => p.Code == 2);
            var fillModePair = rawEntry.Single(p => p.Code == 69);
            Assert.Equal(flagIndices[i], namePair.Index + 1); Assert.Equal(colorIndices[i], fillModePair.Index + 1);
            Assert.Equal(70, oracle[flagIndices[i]].Code); Assert.Equal(70, oracle[colorIndices[i]].Code);
            var flags = Field(entry, "Flags"); var color = Field(entry, "TextFillColor");
            AssertFact(flags, lex, flagIndices[i], flagIndices[i] * 2 + 2, oracle[flagIndices[i]].Text);
            AssertFact(color, lex, colorIndices[i], colorIndices[i] * 2 + 2, oracle[colorIndices[i]].Text);
            Assert.Equal(int.Parse(oracle[flagIndices[i]].Text, CultureInfo.InvariantCulture), flags.Integer);
            Assert.Equal(int.Parse(oracle[colorIndices[i]].Text, CultureInfo.InvariantCulture), color.Integer);
            Assert.Equal(DxfDisposition.Supported, entry.Disposition);
            Assert.NotEqual(flags.SourcePairIndices[0], color.SourcePairIndices[0]);
            foreach (var pair in rawEntry.Where(p => p.Code == 70))
                Assert.Single(entry.Fields.SelectMany(f => f.Fact.SourcePairIndices).Concat(entry.UnmappedPairIndices), index => index == pair.Index);
        }
        Assert.DoesNotContain(r.Diagnostics, d => d.Code == DxfStructuralDiagnosticCode.InvalidTableValue);
        var unmappedHeader = Assert.Single(r.Diagnostics, d => d.Code == DxfStructuralDiagnosticCode.UnmappedHeaderVariables);
        Assert.Equal(h.Variables.First(v => v.Disposition == DxfDisposition.IgnoredWithDiagnostic).Source, unmappedHeader.Source);
        // Every mapped or unmapped field references the one original store exactly once.
        foreach (var record in r.Records.Concat(r.Tables.Select(t => t.HeaderRecord)))
        {
            var all = record.UnmappedPairIndices.Concat(record.Fields.SelectMany(f => f.Fact.SourcePairIndices)).OrderBy(i => i);
            Assert.Equal(Enumerable.Range(record.Source.StartPairIndex, record.Source.EndPairIndex - record.Source.StartPairIndex + 1), all);
            var refs = oracle.Skip(record.Source.StartPairIndex).Take(record.Source.EndPairIndex - record.Source.StartPairIndex + 1)
                .Where(p => p.Code is >= 330 and <= 369 or >= 390 and <= 399 or 480 or 481 or 1005).Select(p => p.Index);
            Assert.Equal(refs, record.References.Select(reference => reference.PairIndex));
        }
        Assert.Equal(hash, Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))));
        using var secondStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var again = new TextDxfStructuralReader().Read(await new TextDxfLexicalReader().ReadAsync(secondStream));
        var json = JsonSerializer.Serialize(r); Assert.Equal(json, JsonSerializer.Serialize(again));
        var directory = Path.Combine(Root(), "artifacts/phase03/03b-review-fixes-20261014"); Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "controlled-structural.json"), json);
        File.WriteAllText(Path.Combine(directory, "controlled-independent-qa.json"), JsonSerializer.Serialize(new
        {
            hash, pairs = oracle.Length, sections = sectionMarkers, tableCounts = independentCounts,
            headerVariables = h.Variables.Count, records = r.Records.Count,
            dispositionCounts = r.RecordDispositionCounts.ToDictionary(c => c.Disposition.ToString(), c => c.Count), diagnostics = r.Diagnostics.Count,
            largestRecordPairs = r.Records.Max(e => e.Source.EndPairIndex - e.Source.StartPairIndex + 1),
            references = r.Records.Sum(e => e.References.Count) + r.Tables.Sum(t => t.HeaderRecord.References.Count),
            dimstyleFacts = dimstyles.Select(e => new { name = Field(e, "Name").Text, flags = Field(e, "Flags"), textFillColor = Field(e, "TextFillColor"), disposition = e.Disposition.ToString() }),
            structuralSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)))
        }, new JsonSerializerOptions { WriteIndented = true }));
    }
    private static void AssertFact(DxfSourceFact fact, DxfLexicalReadResult evidence, int index, int valueLine, string raw)
    {
        Assert.Equal(DxfFactPresence.Present, fact.Presence); Assert.Equal(DxfFactValidity.Valid, fact.Validity);
        Assert.Equal(index, Assert.Single(fact.SourcePairIndices)); Assert.Equal(index, fact.Source!.StartPairIndex);
        Assert.Equal(valueLine - 1, fact.Source.StartLine); Assert.Equal(valueLine, fact.Source.EndLine);
        Assert.Equal(raw, evidence.Pairs[index].RawTextValue);
    }

    [Fact]
    public async Task MinimalSupportedHeaderHasExplicitAbsentFactsAndNoSourceDefaults()
    {
        var r = await Read(Doc()); Assert.Equal(DxfStructuralStatus.Succeeded, r.Status);
        Assert.True(r.StructureValidated); Assert.True(r.MappingComplete);
        Assert.Equal(DxfVersionAcceptance.NotValidated, r.LexicalEvidence.VersionAcceptance);
        Assert.Equal(DxfStructuralVersionAcceptance.Supported, r.VersionAcceptance);
        foreach (var f in new[] { r.Header!.InsertionUnits, r.Header.ExtMin.Z, r.Header.HandleSeed })
        { Assert.Equal(DxfFactPresence.Absent, f.Presence); Assert.Equal(DxfFactValidity.NotApplicable, f.Validity); Assert.Null(f.Integer); Assert.Null(f.Real); Assert.Null(f.Text); Assert.Empty(f.SourcePairIndices); }
        Assert.Equal(DxfReportedExtentsState.NotAvailable, r.Header.ReportedExtentsState);
    }

    [Theory]
    [InlineData("0\nEOF\n", DxfStructuralDiagnosticCode.MissingSections)]
    [InlineData("0\nSECTION\n0\nENDSEC\n0\nEOF\n", DxfStructuralDiagnosticCode.MissingSectionName)]
    [InlineData("0\nSECTION\n", DxfStructuralDiagnosticCode.MissingSectionName)]
    [InlineData("0\nENDSEC\n0\nEOF\n", DxfStructuralDiagnosticCode.UnexpectedEndSec)]
    [InlineData("1\nnoise\n0\nEOF\n", DxfStructuralDiagnosticCode.InvalidTransition)]
    [InlineData("0\nSECTION\n2\nHEADER\n0\nSECTION\n2\nTABLES\n0\nEOF\n", DxfStructuralDiagnosticCode.NestedSection)]
    [InlineData("0\nSECTION\n2\nHEADER\n0\nEOF\n", DxfStructuralDiagnosticCode.MissingEndSec)]
    [InlineData("0\nSECTION\n2\nHEADER\n9\n$ACADVER\n1\nAC1032\n", DxfStructuralDiagnosticCode.MissingEndSec)]
    public async Task MalformedStructureIsNeverEmptySuccess(string text, DxfStructuralDiagnosticCode code) => Error(await Read(text), code);

    [Theory]
    [InlineData("missingEof", DxfStructuralDiagnosticCode.MissingEof)]
    [InlineData("afterEof", DxfStructuralDiagnosticCode.ContentAfterEof)]
    [InlineData("duplicateHeader", DxfStructuralDiagnosticCode.DuplicateHeaderSection)]
    [InlineData("headerValueOutsideVariable", DxfStructuralDiagnosticCode.MalformedHeaderVariable)]
    [InlineData("headerCode0", DxfStructuralDiagnosticCode.MalformedHeaderVariable)]
    public async Task HeaderAndTerminalTransitionsAreStrict(string variant, DxfStructuralDiagnosticCode code)
    {
        var text = variant switch
        {
            "missingEof" => Header,
            "afterEof" => Doc() + "1\nextra\n",
            "duplicateHeader" => Doc(Header),
            "headerValueOutsideVariable" => Section("HEADER", "70\n4\n9\n$ACADVER\n1\nAC1032\n") + "0\nEOF\n",
            _ => Header.Replace("0\nENDSEC", "0\nLINE\n0\nENDSEC") + "0\nEOF\n"
        };
        Error(await Read(text), code);
    }

    [Theory]
    [InlineData("AC1021")]
    [InlineData("AC1024")]
    [InlineData("AC1027")]
    [InlineData("AC1009")]
    public async Task ValidUnsupportedVersionsAreNeverAutomaticallyAccepted(string value)
    {
        var r = await Read(Doc().Replace("AC1032", value));
        Assert.Equal(DxfStructuralStatus.UnsupportedVersion, r.Status); Assert.False(r.StructureValidated);
        Assert.Equal(DxfStructuralVersionAcceptance.Unsupported, r.VersionAcceptance);
        Assert.Contains(r.Diagnostics, d => d.Code == DxfStructuralDiagnosticCode.UnsupportedAcadVersion);
    }
    [Theory]
    [InlineData("ac1032")]
    [InlineData("AC1032 ")]
    [InlineData("ACabcd")]
    [InlineData("")]
    public async Task InvalidVersionTextIsNotFilenameInferred(string value) => Error(await Read(Doc().Replace("AC1032", value)), DxfStructuralDiagnosticCode.InvalidHeaderValue);
    [Fact]
    public async Task MissingVersionFails() => Error(await Read(Section("ENTITIES", "") + "0\nEOF\n"), DxfStructuralDiagnosticCode.MissingAcadVersion);
    [Theory]
    [InlineData("AC1024")]
    [InlineData("AC1032")]
    public async Task DuplicateVersionNeverUsesLastValueWins(string second)
    {
        var r = await Read(Header.Replace("0\nENDSEC", "9\n$ACADVER\n1\n" + second + "\n0\nENDSEC") + "0\nEOF\n");
        Error(r, DxfStructuralDiagnosticCode.DuplicateAcadVersion);
        Assert.Equal(2, r.Header!.AcadVersion.SourcePairIndices.Count); Assert.Null(r.Header.AcadVersion.Text);
        Assert.Equal(DxfStructuralVersionAcceptance.Invalid, r.VersionAcceptance);
    }

    [Fact]
    public async Task UnknownHeaderRangesRepeatedFieldsAndMissingCoordinateStayFactual()
    {
        var text = Header.Replace("0\nENDSEC", "9\n$EXTMIN\n10\n-123.500\n20\n1e+20\n9\n$INSUNITS\n70\n0\n9\n$UNKNOWN\n10\n11\n10\n22\n9\n$HANDSEED\n5\n00ab\n0\nENDSEC") + "0\nEOF\n";
        var r = await Read(text); Assert.Equal(DxfStructuralStatus.Succeeded, r.Status);
        Assert.Equal(-123.5, r.Header!.ExtMin.X.Real); Assert.Equal(DxfFactPresence.Absent, r.Header.ExtMin.Z.Presence);
        Assert.Equal(0, r.Header.InsertionUnits.Integer); Assert.Equal("00ab", r.Header.HandleSeed.Text);
        Assert.Empty(r.Records); var v = r.Header.Variables.Single(v => v.Name == "$UNKNOWN");
        Assert.Equal(DxfDisposition.IgnoredWithDiagnostic, v.Disposition);
        Assert.Equal(new[] { "11", "22" }, r.LexicalEvidence.Pairs.Skip(v.Source.StartPairIndex + 1).Take(2).Select(p => p.RawTextValue));
        Assert.Contains(r.Diagnostics, d => d.Code == DxfStructuralDiagnosticCode.UnmappedHeaderVariables);
    }
    [Theory]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("1e999")]
    [InlineData("not-a-number")]
    public async Task NonfiniteHeaderValuesRetainEvidenceAndFail(string value)
    {
        var r = await Read(Header.Replace("0\nENDSEC", "9\n$EXTMIN\n10\n" + value + "\n0\nENDSEC") + "0\nEOF\n");
        Error(r, DxfStructuralDiagnosticCode.InvalidHeaderValue); Assert.Equal(DxfFactValidity.Invalid, r.Header!.ExtMin.X.Validity);
        Assert.Null(r.Header.ExtMin.X.Real); Assert.Equal(value, r.LexicalEvidence.Pairs[r.Header.ExtMin.X.SourcePairIndices[0]].RawTextValue);
    }
    [Theory]
    [InlineData("$INSUNITS", "70", "4")]
    [InlineData("$UNKNOWN", "1", "x")]
    public async Task DuplicateHeaderVariablesRetainEveryRange(string name, string code, string value)
    {
        var r = await Read(Header.Replace("0\nENDSEC", $"9\n{name}\n{code}\n{value}\n9\n{name}\n{code}\n{value}\n0\nENDSEC") + "0\nEOF\n");
        Error(r, DxfStructuralDiagnosticCode.DuplicateHeaderVariable); Assert.Equal(2, r.Header!.Variables.Count(v => v.Name == name));
    }

    [Fact]
    public async Task UnknownSectionAndCommentsRetainEvidenceWithBoundedClassification()
    {
        var r = await Read("999\nleading\n" + Doc(Section("ALIEN", "999\ncomment\n0\nTHING\n1\nraw\n")) + "999\ntrailing\n");
        Assert.Equal(DxfStructuralStatus.Partial, r.Status); Assert.True(r.StructureValidated);
        Assert.Contains(r.Sections, s => s.Name == "ALIEN" && s.Disposition == DxfDisposition.Unsupported);
        Assert.Single(r.Records); Assert.Equal("THING", r.Records[0].RecordType);
        Assert.Equal(3, Assert.Single(r.Diagnostics, d => d.Code == DxfStructuralDiagnosticCode.CommentsRetained).Count);
    }
    [Fact]
    public async Task SectionOrderIsNotFixed()
    {
        var r = await Read(Section("ENTITIES", "") + Header + "0\nEOF\n");
        Assert.Equal(DxfStructuralStatus.Succeeded, r.Status); Assert.Equal("ENTITIES", r.Sections[0].Name);
    }

    [Theory]
    [InlineData("0\nTABLE\n0\nENDTAB\n", DxfStructuralDiagnosticCode.MissingTableName)]
    [InlineData("0\nTABLE\n2\nLAYER\n0\nTABLE\n2\nSTYLE\n0\nENDTAB\n", DxfStructuralDiagnosticCode.NestedTable)]
    [InlineData("0\nTABLE\n2\nLAYER\n0\nLAYER\n2\nx\n", DxfStructuralDiagnosticCode.MissingEndTab)]
    [InlineData("0\nENDTAB\n", DxfStructuralDiagnosticCode.UnexpectedEndTab)]
    [InlineData("0\nTABLE\n2\nLAYER\n0\nENDTAB\n1\nextra\n", DxfStructuralDiagnosticCode.InvalidTransition)]
    public async Task MalformedTablesFail(string body, DxfStructuralDiagnosticCode code) => Error(await Read(Doc(Section("TABLES", body))), code);
    [Fact]
    public async Task UnknownTableRetainsHeadersEntriesAndRawFields()
    {
        var r = await Read(Doc(Table("CUSTOM", "0\nCUSTOM\n2\nname\n1\nraw\n", 2)));
        Assert.Equal(DxfStructuralStatus.Partial, r.Status); var t = Assert.Single(r.Tables);
        Assert.Equal(DxfDisposition.Unsupported, t.Disposition); Assert.Equal(2, t.DeclaredEntryCount.Integer); Assert.Equal(1, t.ActualEntryCount);
        Assert.Empty(t.Entries[0].Fields); Assert.Equal(3, t.Entries[0].UnmappedPairIndices.Count);
        Assert.Contains(r.Diagnostics, d => d.Code == DxfStructuralDiagnosticCode.UnknownTable);
        Assert.Contains(r.Diagnostics, d => d.Code == DxfStructuralDiagnosticCode.DeclaredTableCountMismatch);
    }

    [Fact]
    public async Task LayerFactsRespectReactorXDictionaryXDataAndSubclassOwnerContext()
    {
        const string body = "0\nLAYER\n5\n00Ab\n102\n{ACAD_REACTORS\n330\nREACTOR\n102\n}\n102\n{ACAD_XDICTIONARY\n330\nDICT\n102\n}\n330\nOWNER\n100\nAcDbSymbolTableRecord\n100\nAcDbLayerTableRecord\n2\n벽체\n70\n1\n62\n-7\n420\n123456\n6\nContinuous\n290\n0\n1001\nAPP\n1002\n{\n330\nXDATA\n1002\n}\n";
        var r = await Read(Doc(Table("LAYER", body))); Assert.Equal(DxfStructuralStatus.Succeeded, r.Status);
        var e = Assert.Single(r.Tables[0].Entries);
        Assert.Equal("00Ab", Field(e, "RawHandle").Text); Assert.Equal("OWNER", Field(e, "RawOwner").Text);
        Assert.Equal("벽체", Field(e, "Name").Text); Assert.Equal(-7, Field(e, "AciColor").Integer);
        Assert.Equal(123456, Field(e, "TrueColor").Integer); Assert.Equal(1, Field(e, "Flags").Integer); Assert.Equal(0, Field(e, "PlotFlag").Integer);
        Assert.Equal(new[] { DxfReferenceRole.Reactor, DxfReferenceRole.ExtensionDictionary, DxfReferenceRole.Owner, DxfReferenceRole.XData }, e.References.Select(v => v.Role));
        Assert.Contains(e.ContextMarkers, m => m.GroupCode == 100 && m.Subclass == "AcDbLayerTableRecord");
        Assert.Contains(e.ContextMarkers, m => m.GroupCode == 1001 && m.Application == "APP");
        Assert.Equal(4, e.References.Count); Assert.Single(Field(e, "RawOwner").SourcePairIndices);
        Assert.Equal(e.Source.EndPairIndex - e.Source.StartPairIndex + 1, e.UnmappedPairIndices.Count + e.Fields.Sum(f => f.Fact.SourcePairIndices.Count));
    }
    [Fact]
    public async Task DimstyleIdentity105DiffersFromCode5AndHandseed()
    {
        var text = Header.Replace("0\nENDSEC", "9\n$HANDSEED\n5\nSEED\n0\nENDSEC") + Table("DIMSTYLE", "0\nDIMSTYLE\n105\n00D1\n5\nLEGACY\n2\nDim\n70\n0\n40\n1.25\n999\nretained\n999\nagain\n") + "0\nEOF\n";
        var r = await Read(text); Assert.Equal(DxfStructuralStatus.Succeeded, r.Status); var e = r.Tables[0].Entries[0];
        Assert.Equal("SEED", r.Header!.HandleSeed.Text); Assert.Equal("00D1", Field(e, "RawHandle").Text);
        Assert.Equal(105, r.LexicalEvidence.Pairs[Field(e, "RawHandle").SourcePairIndices[0]].GroupCode);
        Assert.Equal("LEGACY", Field(e, "LegacyCode5Setting").Text); Assert.Equal(1.25, Field(e, "OverallScale").Real);
        Assert.Equal(2, r.Diagnostics.Single(d => d.Code == DxfStructuralDiagnosticCode.CommentsRetained).Count);
    }
    [Fact]
    public async Task LinetypeOrderedPatternAndStyleBlockClassFactsStayUnnormalized()
    {
        var r = await Read(Doc(
            Table("LTYPE", "0\nLTYPE\n5\nA\n2\nDash\n3\nDesc\n70\n0\n73\n2\n40\n12.5\n49\n5\n74\n0\n49\n-7.5\n74\n0\n") +
            Table("STYLE", "0\nSTYLE\n5\nB\n2\n한글스타일\n40\n0\n41\n2\n50\n30\n3\nfont.shx\n") +
            Table("BLOCK_RECORD", "0\nBLOCK_RECORD\n5\nC\n2\n*Model_Space\n70\n4\n340\nD\n280\n1\n281\n0\n") +
            Section("CLASSES", "0\nCLASS\n1\nCustom\n2\nCpp\n3\nApp\n90\n1\n91\n2\n280\n0\n281\n1\n")));
        Assert.Equal(DxfStructuralStatus.Succeeded, r.Status);
        Assert.Equal(new[] { "Pattern/49", "Pattern/74", "Pattern/49", "Pattern/74" }, r.Tables[0].Entries[0].Fields.Where(f => f.Name.StartsWith("Pattern/")).Select(f => f.Name));
        Assert.Equal(new double?[] { 5, -7.5 }, r.Tables[0].Entries[0].Fields.Where(f => f.Name == "Pattern/49").Select(f => f.Fact.Real));
        Assert.Equal("한글스타일", Field(r.Tables[1].Entries[0], "Name").Text); Assert.Equal(30, Field(r.Tables[1].Entries[0], "ObliqueAngle").Real);
        Assert.Equal("D", Field(r.Tables[2].Entries[0], "LayoutReference").Text); Assert.Equal(4, Field(r.Tables[2].Entries[0], "Flags").Integer);
        Assert.Equal("Custom", Field(r.Records.Single(e => e.Scope == "CLASSES"), "DxfName").Text);
    }
    [Fact]
    public async Task DuplicateTableSingletonFieldsAreNotOverwritten()
    {
        var r = await Read(Doc(Table("LAYER", "0\nLAYER\n2\nfirst\n2\nsecond\n62\n7\n62\n8\n")));
        Assert.Equal(DxfStructuralStatus.Partial, r.Status); Assert.True(r.StructureValidated);
        Assert.Contains(r.Diagnostics, d => d.Code == DxfStructuralDiagnosticCode.InvalidTableValue); var e = r.Tables[0].Entries[0];
        Assert.Equal(DxfDisposition.Invalid, e.Disposition); Assert.Null(Field(e, "Name").Text); Assert.Equal(2, Field(e, "Name").SourcePairIndices.Count);
    }
    [Fact]
    public async Task GeometryAnnotationsAndObjectsAreOnlyRawBoundaries()
    {
        var r = await Read(Doc(Section("ENTITIES", "0\nLINE\n10\nNaN\n0\nINSERT\n50\n90\n0\nHATCH\n1\nraw\n") + Section("OBJECTS", "0\nDICTIONARY\n330\n0\n")));
        Assert.Equal(DxfStructuralStatus.Partial, r.Status);
        Assert.Equal(new[] { DxfDisposition.DeferredTo03C, DxfDisposition.DeferredTo03D, DxfDisposition.Unsupported, DxfDisposition.Unsupported }, r.Records.Select(e => e.Disposition));
        Assert.All(r.Records, e => Assert.Empty(e.Fields)); Assert.Contains(r.LexicalEvidence.Pairs, p => p.RawTextValue == "NaN");
        Assert.Equal(4, r.RecordDispositionCounts.Sum(c => c.Count));
    }

    [Theory]
    [InlineData("102\n}\n", DxfStructuralDiagnosticCode.InvalidControlGroup)]
    [InlineData("102\n{ACAD_REACTORS\n330\nA\n", DxfStructuralDiagnosticCode.UnclosedControlGroup)]
    [InlineData("1002\n{\n1002\n}\n", DxfStructuralDiagnosticCode.InvalidControlGroup)]
    [InlineData("102\n{one\n1001\nAPP\n1002\n}\n", DxfStructuralDiagnosticCode.InvalidControlGroup)]
    public async Task MalformedControlGroupsFailWithoutOwnerGuessing(string control, DxfStructuralDiagnosticCode code) => Error(await Read(Doc(Table("LAYER", "0\nLAYER\n2\nx\n" + control))), code);

    [Theory]
    [InlineData("sections")]
    [InlineData("tables")]
    [InlineData("records")]
    [InlineData("fields")]
    [InlineData("references")]
    [InlineData("depth")]
    [InlineData("pairs")]
    public async Task InjectableStructuralLimitsRejectFirstExcess(string dimension)
    {
        var text = Doc(Table("LAYER", "0\nLAYER\n2\nx\n102\n{ACAD_REACTORS\n102\n{nested\n330\nA\n330\nB\n102\n}\n102\n}\n"));
        var limits = dimension switch
        {
            "sections" => new DxfStructuralLimits { MaxSections = 1 },
            "tables" => new DxfStructuralLimits { MaxTables = 1 },
            "records" => new DxfStructuralLimits { MaxRecords = 1 },
            "fields" => new DxfStructuralLimits { MaxFieldsPerRecord = 2 },
            "references" => new DxfStructuralLimits { MaxReferenceOccurrences = 1 },
            "depth" => new DxfStructuralLimits { MaxControlGroupDepth = 1 },
            _ => new DxfStructuralLimits { MaxPairCount = 6 }
        };
        if (dimension == "tables") text = Doc(Table("LAYER", "0\nLAYER\n2\nx\n") + Table("STYLE", "0\nSTYLE\n2\ny\n"));
        var r = await Read(text, limits); Assert.Equal(DxfStructuralStatus.ResourceLimitExceeded, r.Status);
        Assert.False(r.StructureValidated); Assert.False(r.MappingComplete); Assert.Contains(r.Diagnostics, d => d.Code == DxfStructuralDiagnosticCode.StructuralLimitExceeded);
    }
    [Fact]
    public async Task ExactStructuralLimitsAreAccepted()
    {
        var text = Doc(Table("LAYER", "0\nLAYER\n2\nx\n102\n{ACAD_REACTORS\n330\nA\n102\n}\n"));
        var lex = await Lex(text);
        var r = new TextDxfStructuralReader(new() { MaxSections = 2, MaxTables = 1, MaxRecords = 3,
            MaxFieldsPerRecord = 5, MaxReferenceOccurrences = 1, MaxControlGroupDepth = 1, MaxPairCount = lex.Pairs.Count }).Read(lex);
        Assert.Equal(DxfStructuralStatus.Succeeded, r.Status);
    }
    [Fact]
    public async Task DiagnosticLimitHasReservedTerminalSlotAndNoTruncatedSuccess()
    {
        var r = await Read(Doc(Section("ALIEN", "0\nONE\n0\nTWO\n0\nTHREE\n")), new() { MaxStoredDiagnostics = 3 });
        Assert.Equal(DxfStructuralStatus.ResourceLimitExceeded, r.Status); Assert.Equal(3, r.Diagnostics.Count);
        Assert.Equal(DxfStructuralDiagnosticCode.DiagnosticLimitExceeded, r.Diagnostics[^1].Code);
    }
    [Fact]
    public async Task CancellationNeverPublishesSuccessAndResultsAreFrozen()
    {
        var lex = await Lex(Doc(Table("LAYER", "0\nLAYER\n2\nx\n")));
        using var cts = new CancellationTokenSource(); cts.Cancel();
        var canceled = new TextDxfStructuralReader().Read(lex, cts.Token);
        Assert.Equal(DxfStructuralStatus.Canceled, canceled.Status); Assert.False(canceled.StructureValidated);
        Assert.Equal(DxfStructuralDiagnosticCode.Canceled, Assert.Single(canceled.Diagnostics).Code);
        var r = new TextDxfStructuralReader().Read(lex);
        Assert.Throws<NotSupportedException>(() => ((IList<DxfSectionRecord>)r.Sections).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<DxfStructuralRecord>)r.Tables[0].Entries).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<DxfNamedFact>)r.Tables[0].Entries[0].Fields).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<int>)Field(r.Tables[0].Entries[0], "Name").SourcePairIndices).Clear());
        Assert.Same(lex, r.LexicalEvidence);
    }
    [Fact]
    public async Task FailedLexicalEvidenceIsNeverParsedAsValidPartialStore()
    {
        var r = new TextDxfStructuralReader().Read(await Lex("not-a-code\nvalue\n"));
        Assert.Equal(DxfStructuralStatus.InvalidInput, r.Status); Assert.Empty(r.Sections);
        Assert.Equal(DxfStructuralVersionAcceptance.NotValidated, r.VersionAcceptance);
        Assert.Equal(DxfStructuralDiagnosticCode.LexicalInputRejected, Assert.Single(r.Diagnostics).Code);
    }
    [Fact]
    public async Task DefaultsAreFiniteAndInvalidConfigurationIsRejected()
    {
        var limits = new DxfStructuralLimits(); Assert.Equal(16, limits.MaxControlGroupDepth); Assert.Equal(5000, limits.MaxStoredDiagnostics);
        Assert.Equal(32, limits.MaxSections); Assert.Equal(32, limits.MaxTables); Assert.Equal(4096, limits.MaxRecords);
        Assert.Equal(512, limits.MaxFieldsPerRecord); Assert.Equal(16384, limits.MaxReferenceOccurrences); Assert.Equal(250000, limits.MaxPairCount);
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextDxfStructuralReader(limits with { MaxSections = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextDxfStructuralReader(limits with { MaxStoredDiagnostics = 1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextDxfStructuralReader(limits with { MaxRecords = -1 }));
        var lex = await Lex(Doc());
        var forged = new DxfLexicalReadResult(DxfLexicalStatus.Succeeded, lex.Pairs.Skip(1), [], 0, 0, false, DxfLineEndings.Lf);
        Error(new TextDxfStructuralReader().Read(forged), DxfStructuralDiagnosticCode.InvalidEvidence);
    }

    [Fact]
    public async Task CancellationDuringResultMaterializationStopsBeforePublication()
    {
        using var cts = new CancellationTokenSource();
        var lex = await Lex(Doc());
        IEnumerable<DxfSectionRecord> CancelingSections()
        {
            yield return new("HEADER", new(0, 4, 1, 10), DxfDisposition.Supported);
            cts.Cancel();
            yield return new("ENTITIES", new(5, 7, 11, 16), DxfDisposition.Supported);
        }
        Assert.Throws<OperationCanceledException>(() => new DxfStructuralReadResult(lex, DxfStructuralStatus.Succeeded,
            DxfStructuralVersionAcceptance.Supported, CancelingSections(), null, [], [], [], true, cts.Token));
    }
    [Theory]
    [InlineData("1\nstray\n")]
    [InlineData("0\nENDTAB\n")]
    [InlineData("0\nTABLE\n2\nLAYER\n")]
    public async Task KnownSectionInvalidTransitionsAreNotUnknownRecordSuccess(string body) =>
        Error(await Read(Doc(Section("ENTITIES", body))), DxfStructuralDiagnosticCode.InvalidTransition);

    [Fact]
    public async Task GenuineDuplicateDimstyleStandardFlagsAreInvalidWithoutLastWins()
    {
        var r = await Read(Doc(Table("DIMSTYLE", "0\nDIMSTYLE\n105\nA\n2\nD\n70\n16\n70\n0\n40\n1\n")));
        Assert.Equal(DxfStructuralStatus.Partial, r.Status); Assert.True(r.StructureValidated);
        var e = r.Tables[0].Entries[0]; var flags = Field(e, "Flags");
        Assert.Equal(DxfFactValidity.Invalid, flags.Validity); Assert.Null(flags.Integer);
        Assert.Equal(new[] { "16", "0" }, flags.SourcePairIndices.Select(i => r.LexicalEvidence.Pairs[i].RawTextValue));
        Assert.Equal("A", Field(e, "RawHandle").Text); Assert.Equal(DxfDisposition.Invalid, e.Disposition);
        Assert.Contains(r.Diagnostics, d => d.Code == DxfStructuralDiagnosticCode.InvalidTableValue);
    }

    [Theory]
    [InlineData("unsupported-version", DxfStructuralStatus.UnsupportedVersion, DxfStructuralDiagnosticCode.UnsupportedAcadVersion)]
    [InlineData("missing-version", DxfStructuralStatus.InvalidInput, DxfStructuralDiagnosticCode.MissingAcadVersion)]
    [InlineData("duplicate-version", DxfStructuralStatus.InvalidInput, DxfStructuralDiagnosticCode.DuplicateAcadVersion)]
    [InlineData("unknown-section", DxfStructuralStatus.Partial, DxfStructuralDiagnosticCode.UnknownSection)]
    [InlineData("nested-section", DxfStructuralStatus.InvalidInput, DxfStructuralDiagnosticCode.NestedSection)]
    [InlineData("missing-endsec", DxfStructuralStatus.InvalidInput, DxfStructuralDiagnosticCode.MissingEndSec)]
    [InlineData("missing-eof", DxfStructuralStatus.InvalidInput, DxfStructuralDiagnosticCode.MissingEof)]
    [InlineData("content-after-eof", DxfStructuralStatus.InvalidInput, DxfStructuralDiagnosticCode.ContentAfterEof)]
    [InlineData("malformed-header", DxfStructuralStatus.InvalidInput, DxfStructuralDiagnosticCode.MalformedHeaderVariable)]
    [InlineData("duplicate-header", DxfStructuralStatus.InvalidInput, DxfStructuralDiagnosticCode.DuplicateHeaderSection)]
    [InlineData("table-missing-name", DxfStructuralStatus.InvalidInput, DxfStructuralDiagnosticCode.MissingTableName)]
    [InlineData("nested-table", DxfStructuralStatus.InvalidInput, DxfStructuralDiagnosticCode.NestedTable)]
    [InlineData("missing-endtab", DxfStructuralStatus.InvalidInput, DxfStructuralDiagnosticCode.MissingEndTab)]
    [InlineData("unknown-table-count", DxfStructuralStatus.Partial, DxfStructuralDiagnosticCode.DeclaredTableCountMismatch)]
    public async Task HandReviewedFilesExerciseLexicalAndStructuralPipeline(string name, DxfStructuralStatus status, DxfStructuralDiagnosticCode code)
    {
        var path = Path.Combine(Root(), "datasets/controlled/phase03b", name + ".dxf");
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var lex = await new TextDxfLexicalReader().ReadAsync(stream); Assert.Equal(DxfLexicalStatus.Succeeded, lex.Status);
        var r = new TextDxfStructuralReader().Read(lex); Assert.Equal(status, r.Status); Assert.Contains(r.Diagnostics, d => d.Code == code);
    }
    [Theory]
    [InlineData("dimstyle-identity")]
    [InlineData("reactor-owner")]
    [InlineData("korean-layer-style")]
    public async Task HandReviewedSuccessfulContextFilesPreserveFactualValues(string name)
    {
        var r = await Read(File.ReadAllText(Path.Combine(Root(), "datasets/controlled/phase03b", name + ".dxf"), new UTF8Encoding(false, true)));
        Assert.Equal(DxfStructuralStatus.Succeeded, r.Status);
        var entry = r.Tables[0].Entries[0];
        if (name == "dimstyle-identity")
        { Assert.Equal("FF", r.Header!.HandleSeed.Text); Assert.Equal("00AB", Field(entry, "RawHandle").Text); Assert.Equal("LEGACY", Field(entry, "LegacyCode5Setting").Text); }
        else if (name == "reactor-owner")
        { Assert.Equal("C", Field(entry, "RawOwner").Text); Assert.Equal(DxfReferenceRole.Reactor, entry.References[0].Role); }
        else
        { Assert.Equal("벽체", Field(entry, "Name").Text); Assert.Equal("한글스타일", Field(r.Tables[1].Entries[0], "Name").Text); }
    }

    [Fact]
    public async Task UnknownSectionContentIsNeverAssumedToHaveKnownTableGrammar()
    {
        var r = await Read(Doc(Section("UNKNOWN", "0\nTABLE\n1\ncustom\n0\nENDTAB\n")));
        Assert.Equal(DxfStructuralStatus.Partial, r.Status);
        Assert.Equal(new[] { "TABLE", "ENDTAB" }, r.Records.Select(e => e.RecordType));
        Assert.All(r.Records, e => Assert.Equal(DxfDisposition.Unsupported, e.Disposition));
    }
    [Fact]
    public async Task MissingDimstyle105NeverFallsBackToLegacy5()
    {
        var r = await Read(Doc(Table("DIMSTYLE", "0\nDIMSTYLE\n5\nLEGACY\n2\nD\n")));
        var fact = Field(r.Tables[0].Entries[0], "RawHandle");
        Assert.Equal(DxfFactPresence.Absent, fact.Presence); Assert.Null(fact.Text);
        Assert.Equal("LEGACY", Field(r.Tables[0].Entries[0], "LegacyCode5Setting").Text);
    }
    [Theory]
    [InlineData("70\n2147483648\n")]
    [InlineData("40\nNaN\n")]
    public async Task InvalidTableScalarCannotFabricateDefaults(string fields)
    {
        var type = fields.StartsWith("70", StringComparison.Ordinal) ? "LAYER" : "STYLE";
        var r = await Read(Doc(Table(type, "0\n" + type + "\n2\nName\n" + fields)));
        Assert.Equal(DxfStructuralStatus.Partial, r.Status); Assert.True(r.StructureValidated);
        var fact = Field(r.Tables[0].Entries[0], type == "LAYER" ? "Flags" : "FixedHeight");
        Assert.Equal(DxfFactValidity.Invalid, fact.Validity); Assert.Null(fact.Integer); Assert.Null(fact.Real);
    }
    [Theory]
    [InlineData(DxfLexicalStatus.Canceled, DxfStructuralStatus.Canceled)]
    [InlineData(DxfLexicalStatus.IoFailure, DxfStructuralStatus.IoFailure)]
    [InlineData(DxfLexicalStatus.ResourceLimitExceeded, DxfStructuralStatus.ResourceLimitExceeded)]
    [InlineData(DxfLexicalStatus.UnsupportedEncoding, DxfStructuralStatus.UnsupportedInput)]
    [InlineData(DxfLexicalStatus.UnsupportedInput, DxfStructuralStatus.UnsupportedInput)]
    public void LexicalFailureKindsRemainExplicit(DxfLexicalStatus inputStatus, DxfStructuralStatus outputStatus)
    {
        var lex = new DxfLexicalReadResult(inputStatus, [], [], 0, 0, false, DxfLineEndings.None);
        var r = new TextDxfStructuralReader().Read(lex);
        Assert.Equal(outputStatus, r.Status); Assert.False(r.StructureValidated); Assert.Same(lex, r.LexicalEvidence);
    }
    [Fact]
    public async Task HeaderVariableWithoutAnyValueCannotBecomeCleanSuccess() =>
        Error(await Read(Header.Replace("0\nENDSEC", "9\n$UNKNOWN\n0\nENDSEC") + "0\nEOF\n"), DxfStructuralDiagnosticCode.MalformedHeaderVariable);
    [Fact]
    public async Task Combined102And1002DepthUsesOneBudget()
    {
        var text = Doc(Table("LAYER", "0\nLAYER\n2\nx\n1001\nAPP\n1002\n{\n102\n{INNER\n330\nA\n102\n}\n1002\n}\n"));
        var failure = await Read(text, new() { MaxControlGroupDepth = 1 });
        Assert.Equal(DxfStructuralStatus.ResourceLimitExceeded, failure.Status);
        var accepted = await Read(text, new() { MaxControlGroupDepth = 2 });
        Assert.Equal(DxfStructuralStatus.Succeeded, accepted.Status);
        Assert.Equal(DxfReferenceRole.XData, Assert.Single(accepted.Tables[0].Entries[0].References).Role);
        Assert.Equal(DxfFactPresence.Absent, Field(accepted.Tables[0].Entries[0], "RawOwner").Presence);
    }

    [Theory]
    [InlineData("dimstyle-distinct-70-roles", null)]
    [InlineData("dimstyle-duplicate-standard-flags", "Flags")]
    [InlineData("dimstyle-duplicate-text-fill-color", "TextFillColor")]
    public async Task DimstyleRoleFixturesSeparateValidFieldsAndGenuineDuplicates(string name, string? invalidField)
    {
        var path = Path.Combine(Root(), "datasets/controlled/phase03b", name + ".dxf");
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var lex = await new TextDxfLexicalReader().ReadAsync(stream);
        var r = new TextDxfStructuralReader().Read(lex); var entry = Assert.Single(r.Tables[0].Entries);
        Assert.Equal("00AB", Field(entry, "RawHandle").Text); Assert.Equal("LEGACY", Field(entry, "LegacyCode5Setting").Text);
        Assert.Equal(105, lex.Pairs[Assert.Single(Field(entry, "RawHandle").SourcePairIndices)].GroupCode);
        Assert.Equal(5, lex.Pairs[Assert.Single(Field(entry, "LegacyCode5Setting").SourcePairIndices)].GroupCode);
        Assert.Equal(invalidField == null ? DxfDisposition.Supported : DxfDisposition.Invalid, entry.Disposition);
        Assert.Equal(invalidField == null ? DxfStructuralStatus.Succeeded : DxfStructuralStatus.Partial, r.Status);
        foreach (var field in new[] { "Flags", "TextFillColor" })
        {
            var fact = Field(entry, field); Assert.Equal(DxfFactPresence.Present, fact.Presence);
            if (field == invalidField)
            {
                Assert.Equal(DxfFactValidity.Invalid, fact.Validity); Assert.Null(fact.Integer);
                Assert.Equal(field == "Flags" ? new[] { "16", "32" } : new[] { "7", "8" }, fact.SourcePairIndices.Select(i => lex.Pairs[i].RawTextValue));
                Assert.Contains(r.Diagnostics, d => d.Code == DxfStructuralDiagnosticCode.InvalidTableValue && d.Source == fact.Source);
            }
            else
            { Assert.Equal(DxfFactValidity.Valid, fact.Validity); Assert.Equal(field == "Flags" ? 16 : 7, fact.Integer); }
        }
        Assert.Equal(Enumerable.Range(entry.Source.StartPairIndex, entry.Source.EndPairIndex - entry.Source.StartPairIndex + 1),
            entry.Fields.SelectMany(f => f.Fact.SourcePairIndices).Concat(entry.UnmappedPairIndices).OrderBy(i => i));
        Assert.Empty(Field(entry, "Flags").SourcePairIndices.Intersect(Field(entry, "TextFillColor").SourcePairIndices));
        if (invalidField == null) Assert.DoesNotContain(r.Diagnostics, d => d.Code == DxfStructuralDiagnosticCode.InvalidTableValue);
    }

    [Theory]
    [InlineData("70\n16\n40\n1\n70\n7\n", false)]
    [InlineData("70\n16\n69\n2\n71\n0\n70\n7\n", false)]
    [InlineData("100\nForeignSubclass\n69\n2\n70\n7\n", false)]
    [InlineData("70\n16\n69\n2\n999\ncomment\n102\n{PRIVATE\n70\n123\n102\n}\n70\n7\n71\n0\n", true)]
    public async Task DimstyleFieldRolesRequireContextAndExcludeControlData(string payload, bool supported)
    {
        var r = await Read(Doc(Table("DIMSTYLE", "0\nDIMSTYLE\n105\nA\n100\nAcDbDimStyleTableRecord\n2\nD\n" + payload)));
        var e = r.Tables[0].Entries[0];
        if (supported)
        { Assert.Equal(DxfDisposition.Supported, e.Disposition); Assert.Equal(16, Field(e, "Flags").Integer); Assert.Equal(7, Field(e, "TextFillColor").Integer); }
        else
        {
            Assert.Equal(DxfDisposition.Invalid, e.Disposition);
            Assert.Contains(r.Diagnostics, d => d.Code == DxfStructuralDiagnosticCode.InvalidTableValue && d.Source?.StartPairIndex == e.Source.EndPairIndex);
        }
        Assert.Equal(Enumerable.Range(e.Source.StartPairIndex, e.Source.EndPairIndex - e.Source.StartPairIndex + 1),
            e.Fields.SelectMany(f => f.Fact.SourcePairIndices).Concat(e.UnmappedPairIndices).OrderBy(i => i));
    }

    [Fact]
    public async Task UnmappedHeaderAggregateHasFirstRepresentativeSourceWithoutDiagnosticExpansion()
    {
        var r = await Read(Header.Replace("0\nENDSEC", "9\n$ONE\n1\na\n9\n$TWO\n1\nb\n0\nENDSEC") + "0\nEOF\n");
        var diagnostic = Assert.Single(r.Diagnostics, d => d.Code == DxfStructuralDiagnosticCode.UnmappedHeaderVariables);
        Assert.NotNull(diagnostic.Source); Assert.Equal(2, diagnostic.Count);
        Assert.Equal(r.Header!.Variables.Single(v => v.Name == "$ONE").Source, diagnostic.Source);
        Assert.Equal(4, diagnostic.Source.StartPairIndex); Assert.Equal(9, diagnostic.Source.StartLine);
    }
}
