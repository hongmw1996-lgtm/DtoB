using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DtoB.Cad.Raw;
using DtoB.Cad.Dxf;
using Xunit;

namespace DtoB.Tests;

public class TextDxfIdentityGraphReaderTests
{
    private const string Hash = "2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC";
    private const string Header = "0\nSECTION\n2\nHEADER\n9\n$ACADVER\n1\nAC1032\n0\nENDSEC\n";
    private static string Obj(string handle, string owner = "0") => "0\nDICTIONARY\n5\n" + handle + "\n330\n" + owner + "\n";
    private static string Doc(string objects) => Header + "0\nSECTION\n2\nOBJECTS\n" + objects + "0\nENDSEC\n0\nEOF\n";
    private static string Root()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory); while (d != null && !File.Exists(Path.Combine(d.FullName, "DtoB.sln"))) d = d.Parent;
        return d?.FullName ?? throw new InvalidDataException("Repository root not found.");
    }
    private static string FixtureText(string name) => File.ReadAllText(Path.Combine(Root(), "datasets/controlled/phase03e", name + ".dxf"), new UTF8Encoding(false, true));
    private static DxfDocumentProvenance Document() => new("drawing-fixed", "revision-fixed", Hash);
    private static async Task<DxfBlockAnnotationReadResult> Prior(string text)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text), false);
        return new TextDxfBlockAnnotationReader().Read(new TextDxfGeometryReader().Read(new TextDxfStructuralReader().Read(await new TextDxfLexicalReader().ReadAsync(stream))));
    }
    private static async Task<DxfIdentityGraphReadResult> Read(string text, DxfIdentityGraphLimits? limits = null, DxfDocumentProvenance? document = null) => new TextDxfIdentityGraphReader(limits).Read(await Prior(text), document);
    private static Task<DxfIdentityGraphReadResult> Fixture(string name) => Read(FixtureText(name));
    private sealed record Pair(int Index, int Code, string Value);
    private sealed record Reference(Pair Pair, string Role);
    private sealed record Record(string Type, string Section, string? Table, int Start, int End, Pair[] Handles, Reference[] References);
    private static Record[] IndependentScan(string text)
    {
        // Independent physical-line scanner; no production scope/context/graph helpers.
        var lines = text.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
        var p = Enumerable.Range(0, lines.Length / 2).Select(i => new Pair(i, int.Parse(lines[2 * i], CultureInfo.InvariantCulture), lines[2 * i + 1])).ToArray();
        var zero = p.Where(x => x.Code == 0).ToArray(); var section = ""; string? table = null; var records = new List<Record>();
        for (var z = 0; z < zero.Length - 1; z++)
        {
            var start = zero[z].Index; var end = zero[z + 1].Index - 1; var type = zero[z].Value;
            if (type == "SECTION") { section = p[start + 1].Value; continue; }
            if (type == "ENDSEC") { section = ""; table = null; continue; }
            if (type == "ENDTAB") { table = null; continue; }
            if (type == "TABLE") table = p[start + 1].Value;
            if (section is not ("TABLES" or "BLOCKS" or "ENTITIES" or "OBJECTS")) continue;
            var handles = new List<Pair>(); var refs = new List<Reference>(); var controls = new List<string>(); string? app = null, subclass = null;
            for (var i = start + 1; i <= end; i++)
            {
                var pair = p[i];
                if (pair.Code == 100 && controls.Count == 0 && app == null) subclass = pair.Value;
                if (pair.Code == 1001 && controls.Count == 0) app = pair.Value;
                if (pair.Code is 102 or 1002)
                { if (pair.Value.StartsWith('{')) controls.Add(pair.Value); else if (pair.Value == "}" && controls.Count > 0) controls.RemoveAt(controls.Count - 1); }
                if (controls.Count == 0 && app == null && (type == "DIMSTYLE" ? pair.Code == 105 : pair.Code == 5)) handles.Add(pair);
                if (pair.Code is >= 330 and <= 369 or >= 390 and <= 399 or 480 or 481 or 1005)
                {
                    var role = app != null ? "XData" : controls.Contains("{ACAD_REACTORS") ? "Reactor" : controls.Contains("{ACAD_XDICTIONARY") ? "ExtensionDictionary" :
                        controls.Count == 0 && pair.Code == 330 && subclass is null or "AcDbSymbolTable" or "AcDbSymbolTableRecord" ? "Owner" : "Other";
                    refs.Add(new(pair, role));
                }
            }
            records.Add(new(type, section, table, start, end, handles.ToArray(), refs.ToArray()));
        }
        return records.ToArray();
    }
    private static string Key(string raw) { var s = raw.ToUpperInvariant().TrimStart('0'); return s.Length == 0 ? "0" : s; }

    [Fact]
    public async Task ControlledIndependentInventoryEveryNodeEdgeAndOwnerChain()
    {
        var path = Path.Combine(Root(), "datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf");
        Assert.Equal(Hash, Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))));
        var raw = IndependentScan(File.ReadAllText(path)); Assert.Equal(327, raw.Length); Assert.All(raw, r => Assert.Single(r.Handles));
        var sourceLookup = raw.ToDictionary(r => Key(r.Handles[0].Value)); Assert.Equal(327, sourceLookup.Count);
        using var stream = File.OpenRead(path);
        var d = new TextDxfBlockAnnotationReader().Read(new TextDxfGeometryReader().Read(new TextDxfStructuralReader().Read(await new TextDxfLexicalReader().ReadAsync(stream))));
        var before = JsonSerializer.Serialize(d); var r = new TextDxfIdentityGraphReader().Read(d, Document());
        Assert.Equal(before, JsonSerializer.Serialize(d)); Assert.Same(d, r.AnnotationEvidence);
        Assert.Equal(DxfGraphStatus.Succeeded, r.Status); Assert.True(r.GraphValidated); Assert.True(r.IdentityCoverageComplete); Assert.True(r.ReferenceResolutionComplete);
        Assert.Empty(r.Diagnostics); Assert.Empty(r.DuplicateGroups); Assert.Empty(r.OwnerCycles); Assert.Equal(327, r.Nodes.Count); Assert.Equal(15, r.NonIdentityRecords.Count);
        Assert.Equal(9, r.Nodes.Count(n => n.Locator.Kind == DxfGraphRecordKind.TableHeader));
        Assert.Equal(281, d.EffectiveRecordDispositionCounts.Single(c => c.Disposition == DxfDisposition.Supported).Count);
        Assert.Equal(52, d.EffectiveRecordDispositionCounts.Single(c => c.Disposition == DxfDisposition.Unsupported).Count);
        Assert.Equal(DxfBlockAnnotationStatus.Partial, d.Status);
        for (var i = 0; i < raw.Length; i++)
        {
            var n = r.Nodes[i]; var s = raw[i]; var h = s.Handles[0];
            Assert.Equal(s.Type, n.SourceRecord.RecordType); Assert.Equal(s.Section, n.SourceSection); Assert.Equal(s.Table, n.TableScope);
            Assert.Equal(s.Start, n.SourceRecord.Source.StartPairIndex); Assert.Equal(s.End, n.SourceRecord.Source.EndPairIndex);
            Assert.Equal(h.Value, n.OriginalHandle.Text); Assert.Equal(Key(h.Value), n.CanonicalHandle); Assert.Equal(h.Index, Assert.Single(n.OriginalHandle.SourcePairIndices));
            Assert.Equal(2 * h.Index + 1, n.OriginalHandle.Source!.StartLine); Assert.Equal(2 * h.Index + 2, n.OriginalHandle.Source.EndLine);
            Assert.Equal(DxfIdentityValidity.Valid, n.Validity); Assert.Equal(Document().DrawingId, n.FullIdentity!.Document.DrawingId); Assert.Equal(h.Value, n.FullIdentity.OriginalHandle);
            if (n.SourceSection == "BLOCKS") Assert.NotNull(n.BlockDefinitionIndex); else Assert.Null(n.BlockDefinitionIndex);
        }
        var independentRefs = raw.SelectMany(x => x.References.Select(e => (Record: x, Ref: e))).ToArray(); Assert.Equal(458, independentRefs.Length); Assert.Equal(458, r.References.Count);
        var nulls = 0; var resolvedOwners = 0;
        for (var i = 0; i < independentRefs.Length; i++)
        {
            var (s, e) = independentRefs[i]; var actual = r.References[i]; var targetKey = Key(e.Pair.Value);
            Assert.Equal(e.Pair.Index, actual.Source.StartPairIndex); Assert.Equal(e.Pair.Code, actual.GroupCode); Assert.Equal(e.Pair.Value, actual.OriginalTargetHandle);
            Assert.Equal(e.Role, actual.ContextEvidence.Role.ToString()); Assert.Equal(s.Handles[0].Value, actual.SourceHandle);
            if (targetKey == "0") { Assert.Equal(DxfReferenceResolution.Null, actual.Resolution); Assert.Null(actual.TargetLocator); nulls++; }
            else
            {
                Assert.Equal(DxfReferenceResolution.Resolved, actual.Resolution); var target = r.Nodes.Single(n => n.Locator == actual.TargetLocator);
                Assert.Equal(sourceLookup[targetKey].Start, target.SourceRecord.Source.StartPairIndex); if (e.Role == "Owner") resolvedOwners++;
            }
        }
        Assert.Equal(20, nulls); Assert.Equal(317, resolvedOwners); Assert.Equal(327, r.References.Count(e => e.ContextEvidence.Role == DxfReferenceRole.Owner));
        Assert.Equal(30, r.References.Count(e => e.ContextEvidence.Role == DxfReferenceRole.Reactor)); Assert.Equal(101, r.References.Count(e => e.ContextEvidence.Role == DxfReferenceRole.Other));
        Assert.DoesNotContain(r.References, e => e.ContextEvidence.Role is DxfReferenceRole.ExtensionDictionary or DxfReferenceRole.XData);
        Assert.Empty(r.UnresolvedReferenceIndices); Assert.Empty(r.AmbiguousReferenceIndices); Assert.Equal(317, r.Ownership.Count); Assert.Equal(10, r.OwnerRoots.Count);
        // Independent owner chains, excluding arbitrary pointer edges, detect cycles/root count.
        var roots = new HashSet<string>();
        foreach (var s in raw)
        {
            var active = new HashSet<string>(); var current = s;
            while (true)
            {
                var key = Key(current.Handles[0].Value); Assert.True(active.Add(key)); var owner = Assert.Single(current.References, e => e.Role == "Owner");
                if (Key(owner.Pair.Value) == "0") { roots.Add(key); break; } current = sourceLookup[Key(owner.Pair.Value)];
            }
        }
        Assert.Equal(10, roots.Count);
        var evidence = Environment.GetEnvironmentVariable("DTOB_03E_EVIDENCE_DIRECTORY");
        if (evidence != null) File.WriteAllText(Path.Combine(evidence, "controlled-graph.json"), JsonSerializer.Serialize(r));
    }
    [Fact]
    public async Task HandReviewedCrossSectionSourceGoldensAndHeaderDimstyleExceptions()
    {
        var path = Path.Combine(Root(), "datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf"); var r = await Read(File.ReadAllText(path));
        foreach (var (start, type, handle, pair, code) in new[] { (682, "TABLE", "8", 684, 5), (1700, "DIMSTYLE", "2B", 1701, 105), (2662, "BLOCK", "18", 2663, 5), (2856, "LINE", "9C", 2857, 5), (4905, "DICTIONARY", "A", 4906, 5), (5454, "VISUALSTYLE", "5A", 5455, 5) })
        {
            var n = r.Nodes.Single(n => n.SourceRecord.Source.StartPairIndex == start); Assert.Equal(type, n.SourceRecord.RecordType); Assert.Equal(handle, n.OriginalHandle.Text); Assert.Equal(handle, n.CanonicalHandle);
            Assert.Equal(pair, Assert.Single(n.OriginalHandle.SourcePairIndices)); Assert.Equal(code, r.LexicalEvidence.Pairs[pair].GroupCode); Assert.Equal(2 * pair + 1, n.OriginalHandle.Source!.StartLine);
        }
        var reactor = r.References.Single(e => e.Source.StartPairIndex == 5187); Assert.Equal(DxfReferenceRole.Reactor, reactor.ContextEvidence.Role); Assert.Equal("E", reactor.OriginalTargetHandle);
        Assert.Equal(4953, r.Nodes.Single(n => n.Locator == reactor.TargetLocator).SourceRecord.Source.StartPairIndex);
        Assert.Equal(DxfReferenceRole.Reactor, r.References.Single(e => e.Source.StartPairIndex == 5457).ContextEvidence.Role);
        Assert.Equal(DxfReferenceRole.Owner, r.References.Single(e => e.Source.StartPairIndex == 5459).ContextEvidence.Role);
        Assert.DoesNotContain(r.Nodes, n => n.SourceSection == "HEADER"); Assert.DoesNotContain(r.Nodes, n => n.SourceRecord.RecordType == "CLASS");
        var dim = r.Nodes.Single(n => n.SourceRecord.Source.StartPairIndex == 1700);
        Assert.DoesNotContain(dim.OriginalHandle.SourcePairIndices, i => r.LexicalEvidence.Pairs[i].GroupCode == 5);
    }
    [Theory]
    [InlineData("duplicate-same")][InlineData("duplicate-cross")][InlineData("duplicate-case")][InlineData("duplicate-zero")]
    public async Task DuplicatesRetainEveryRawNodeAndRejectUniqueIdentity(string name)
    {
        var r = await Read(FixtureText(name), document: Document()); var group = Assert.Single(r.DuplicateGroups);
        Assert.Equal("A", group.CanonicalHandle); Assert.Equal(2, group.Records.Count); Assert.Equal(2, r.Nodes.Count); Assert.False(r.GraphValidated); Assert.False(r.IdentityCoverageComplete);
        Assert.All(r.Nodes, n => { Assert.Equal(DxfIdentityValidity.Ambiguous, n.Validity); Assert.Equal(DxfIdentityValidity.Ambiguous, n.FullIdentity!.Validity); });
        Assert.Equal(2, r.Diagnostics.Count(d => d.Code == DxfGraphDiagnosticCode.DuplicateHandle));
        Assert.Equal(r.Nodes.Select(n => n.Locator), group.Records);
    }
    [Theory][InlineData("A", "A")][InlineData("a", "A")][InlineData("000A", "A")][InlineData("00001f", "1F")]
    [InlineData("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF")]
    public async Task StringCanonicalHandlesNoIntegerWidthOrSourceRewrite(string raw, string canonical)
    {
        var r = await Read(Doc(Obj(raw)), document: Document()); var n = Assert.Single(r.Nodes);
        Assert.Equal(raw, n.OriginalHandle.Text); Assert.Equal(canonical, n.CanonicalHandle); Assert.Equal(raw, n.FullIdentity!.OriginalHandle); Assert.True(r.GraphValidated);
    }
    [Theory][InlineData("")][InlineData("G")][InlineData("0")][InlineData("000")][InlineData(" A")][InlineData("A ")][InlineData("-A")][InlineData("0xA")]
    public async Task InvalidObjectHandleHasNoCanonicalOrValidFullIdentity(string raw)
    {
        var r = await Read(Doc(Obj(raw)), document: Document()); var n = Assert.Single(r.Nodes);
        Assert.Equal(raw, n.OriginalHandle.Text); Assert.Null(n.CanonicalHandle); Assert.Equal(DxfIdentityValidity.Malformed, n.Validity); Assert.Equal(n.Validity, n.FullIdentity!.Validity);
        Assert.False(r.GraphValidated); Assert.Contains(r.Diagnostics, d => d.Code == DxfGraphDiagnosticCode.MalformedHandle && d.Source == n.OriginalHandle.Source);
    }
    [Fact]
    public async Task MissingRequiredHandleVersusNonApplicableRecordsNoManufacturedIdentity()
    {
        var r = await Fixture("missing-required"); var missing = r.Nodes.Single(n => n.Validity == DxfIdentityValidity.MissingRequired);
        Assert.Null(missing.CanonicalHandle); Assert.Null(missing.OriginalHandle.Text); Assert.Equal(DxfFactPresence.Absent, missing.OriginalHandle.Presence); Assert.False(r.GraphValidated);
        r = await Read(Header + "0\nSECTION\n2\nCLASSES\n0\nCLASS\n1\nRAW\n5\nA\n0\nENDSEC\n0\nEOF\n");
        Assert.Empty(r.Nodes); Assert.Single(r.NonIdentityRecords); Assert.DoesNotContain(r.Diagnostics, d => d.Code == DxfGraphDiagnosticCode.MissingRequiredHandle);
    }
    [Fact]
    public async Task MultipleIdentityFieldsAndForeignControlFieldsCannotSelectWinner()
    {
        var r = await Read(Doc(Obj("A") + "5\nB\n")); Assert.Equal(DxfIdentityValidity.MultipleFields, Assert.Single(r.Nodes).Validity); Assert.Null(r.Nodes[0].CanonicalHandle); Assert.Equal(2, r.Nodes[0].OriginalHandle.SourcePairIndices.Count);
        r = await Read(Doc(Obj("A") + "102\n{APP\n5\nB\n102\n}\n100\nForeign\n5\nC\n1001\nAPP\n5\nD\n"));
        Assert.Equal("A", Assert.Single(r.Nodes).OriginalHandle.Text); Assert.Single(r.Nodes[0].OriginalHandle.SourcePairIndices);
    }
    [Fact]
    public async Task Dimstyle105AndCode5LegacySettingHaveDifferentIdentityRoles()
    {
        var r = await Fixture("identity-context"); var n = r.Nodes.Single(n => n.SourceRecord.RecordType == "DIMSTYLE");
        Assert.Equal("B", n.OriginalHandle.Text); Assert.Equal(105, r.LexicalEvidence.Pairs[Assert.Single(n.OriginalHandle.SourcePairIndices)].GroupCode); Assert.Equal("DIMSTYLE", n.TableScope);
        Assert.Contains(r.LexicalEvidence.Pairs, p => p.RawTextValue == "LEGACY-NOT-HANDLE"); Assert.True(r.GraphValidated);
        var seed = await Read(Doc(Obj("A")).Replace("0\nENDSEC\n0\nSECTION", "9\n$HANDSEED\n5\nA\n0\nENDSEC\n0\nSECTION"));
        Assert.Single(seed.Nodes); Assert.Empty(seed.DuplicateGroups);
    }
    [Fact]
    public async Task OwnerRootNullAndResolvedParentRelations()
    {
        var r = await Read(Doc(Obj("A") + Obj("B", "A"))); Assert.True(r.GraphValidated); Assert.Single(r.Ownership); var root = Assert.Single(r.OwnerRoots);
        Assert.Equal("A", r.Nodes.Single(n => n.Locator == root).OriginalHandle.Text); Assert.Equal(root, r.Ownership[0].Owner);
        r = await Read(Doc("0\nCUSTOM\n5\nA\n")); Assert.True(r.GraphValidated); Assert.Single(r.OwnerRoots);
    }
    [Fact]
    public async Task UnresolvedOwnerIsErrorAndDoesNotBecomeRoot()
    {
        var r = await Fixture("unresolved-owner"); Assert.False(r.GraphValidated); Assert.True(r.IdentityCoverageComplete); Assert.False(r.ReferenceResolutionComplete); Assert.Empty(r.OwnerRoots);
        Assert.Contains(r.Diagnostics, d => d.Code == DxfGraphDiagnosticCode.UnresolvedOwner && d.Severity == DxfStructuralDiagnosticSeverity.Error); Assert.Single(r.UnresolvedReferenceIndices);
    }
    [Theory][InlineData("reactor", DxfGraphDiagnosticCode.UnresolvedReactor)][InlineData("dictionary", DxfGraphDiagnosticCode.UnresolvedExtensionDictionary)]
    [InlineData("xdata", DxfGraphDiagnosticCode.UnresolvedXData)][InlineData("pointer", DxfGraphDiagnosticCode.UnresolvedPointer)]
    public async Task UnresolvedNonOwnersAreLocatedWarningsIncompleteResolutionNotFatalOwnerGraph(string role, DxfGraphDiagnosticCode code)
    {
        var extra = role switch { "reactor" => "102\n{ACAD_REACTORS\n330\nFF\n102\n}\n", "dictionary" => "102\n{ACAD_XDICTIONARY\n330\nFF\n102\n}\n", "xdata" => "1001\nAPP\n1005\nFF\n", _ => "340\nFF\n" };
        var r = await Read(Doc(Obj("A") + extra)); Assert.True(r.GraphValidated); Assert.False(r.ReferenceResolutionComplete); Assert.Equal(DxfGraphStatus.Partial, r.Status);
        Assert.Contains(r.Diagnostics, d => d.Code == code && d.Severity == DxfStructuralDiagnosticSeverity.Warning && d.Source != null); Assert.Empty(r.Ownership);
    }
    [Theory][InlineData("")][InlineData("G")][InlineData(" A")]
    public async Task MalformedTargetIsNotNullAndCannotResolve(string target)
    {
        var r = await Read(Doc(Obj("A") + "340\n" + target + "\n")); var e = r.References.Single(x => x.GroupCode == 340);
        Assert.Equal(target, e.OriginalTargetHandle); Assert.Null(e.CanonicalTargetHandle); Assert.Equal(DxfReferenceResolution.Malformed, e.Resolution); Assert.False(r.ReferenceResolutionComplete);
    }
    [Fact]
    public async Task AmbiguousTargetKeepsAllConflictsNoFirstOrLastResolution()
    {
        var r = await Fixture("ambiguous-target"); var e = r.References.Single(x => x.OriginalTargetHandle == "A");
        Assert.Equal(DxfReferenceResolution.Ambiguous, e.Resolution); Assert.Null(e.TargetLocator); Assert.Single(r.AmbiguousReferenceIndices); Assert.Empty(r.Ownership); Assert.False(r.GraphValidated);
    }
    [Fact]
    public async Task ReactorXDictionaryXDataRetainRolesAndNeverCreateOwnership()
    {
        var r = await Fixture("contextual-references"); Assert.True(r.GraphValidated); Assert.Empty(r.Ownership);
        Assert.Equal(1, r.References.Count(e => e.ContextEvidence.Role == DxfReferenceRole.Reactor)); Assert.Equal(2, r.References.Count(e => e.ContextEvidence.Role == DxfReferenceRole.ExtensionDictionary)); Assert.Equal(2, r.References.Count(e => e.ContextEvidence.Role == DxfReferenceRole.XData));
        Assert.All(r.References.Where(e => e.OriginalTargetHandle == "B"), e => Assert.Equal(DxfReferenceResolution.Resolved, e.Resolution));
        var xdict = r.References.Single(e => e.GroupCode == 360); Assert.Equal(DxfPointerKind.HardOwner, xdict.PointerKind); Assert.Equal(DxfReferenceRole.ExtensionDictionary, xdict.ContextEvidence.Role);
    }
    [Theory][InlineData(330, DxfPointerKind.SoftPointer)][InlineData(339, DxfPointerKind.SoftPointer)][InlineData(340, DxfPointerKind.HardPointer)][InlineData(350, DxfPointerKind.SoftOwner)]
    [InlineData(360, DxfPointerKind.HardOwner)][InlineData(390, DxfPointerKind.HardPointer)][InlineData(480, DxfPointerKind.HardPointer)][InlineData(1005, DxfPointerKind.XDataHandle)]
    public async Task GroupCodePointerClassDoesNotChangeContextualRole(int code, DxfPointerKind expected)
    {
        var extra = code == 1005 ? "1001\nAPP\n1005\nB\n" : "100\nForeign\n" + code + "\nB\n";
        var r = await Read(Doc(Obj("A") + extra + Obj("B"))); var e = r.References.Single(e => e.GroupCode == code && e.OriginalTargetHandle == "B");
        Assert.Equal(expected, e.PointerKind); Assert.NotEqual(DxfReferenceRole.Owner, e.ContextEvidence.Role); Assert.Empty(r.Ownership);
    }
    [Fact]
    public async Task NullTargetsRawZeroPreservedNoFalseUnresolved()
    {
        var r = await Fixture("null-reference"); Assert.True(r.GraphValidated); Assert.True(r.ReferenceResolutionComplete); Assert.Empty(r.Diagnostics);
        var e = r.References.Single(x => x.GroupCode == 340); Assert.Equal("0000", e.OriginalTargetHandle); Assert.Equal("0", e.CanonicalTargetHandle); Assert.Equal(DxfReferenceResolution.Null, e.Resolution);
    }
    [Theory][InlineData("self-owner", 1)][InlineData("owner-cycle", 2)]
    public async Task OwnerCyclesRetainExactParticipantsAndEdges(string fixture, int count)
    {
        var r = await Fixture(fixture); Assert.False(r.GraphValidated); Assert.Equal(count, Assert.Single(r.OwnerCycles).Records.Count); Assert.Equal(count, r.Ownership.Count);
        Assert.Equal(count, r.Diagnostics.Count(d => d.Code is DxfGraphDiagnosticCode.SelfOwner or DxfGraphDiagnosticCode.OwnerCycle));
        Assert.All(r.OwnerCycles[0].Records, locator => Assert.Contains(r.Diagnostics, d => d.Record == locator && d.Source == r.Nodes.Single(n => n.Locator == locator).OriginalHandle.Source));
    }
    [Fact]
    public async Task PointerCyclesNotOwnerCyclesAndCompetingOwnersNeverSelectOne()
    {
        var r = await Fixture("pointer-cycle"); Assert.True(r.GraphValidated); Assert.Empty(r.OwnerCycles); Assert.Empty(r.Ownership);
        r = await Fixture("multiple-owners"); Assert.False(r.GraphValidated); Assert.Equal(2, r.Ownership.Count); Assert.Contains(r.Diagnostics, d => d.Code == DxfGraphDiagnosticCode.MultipleOwners);
    }
    [Fact]
    public async Task MissingRequiredOwnerDetectedOnlyForReviewedEntityProfile()
    {
        var text = Header + "0\nSECTION\n2\nENTITIES\n0\nPOINT\n5\nA\n100\nAcDbEntity\n100\nAcDbPoint\n10\n1\n20\n2\n0\nENDSEC\n0\nEOF\n";
        var r = await Read(text); Assert.False(r.GraphValidated); Assert.Empty(r.OwnerRoots); Assert.Contains(r.Diagnostics, d => d.Code == DxfGraphDiagnosticCode.MissingRequiredOwner);
        Assert.DoesNotContain((await Fixture("root")).Diagnostics, d => d.Code == DxfGraphDiagnosticCode.MissingRequiredOwner);
    }
    [Fact]
    public async Task UnsupportedSemanticObjectsParticipateWithoutPromotionAndInsertNamesNotHandleEdges()
    {
        var r = await Fixture("unsupported-target"); Assert.True(r.GraphValidated); Assert.Equal(2, r.Nodes.Count); Assert.All(r.Nodes, n => Assert.Equal(DxfDisposition.Unsupported, n.SourceRecord.Disposition));
        Assert.Equal(2, r.AnnotationEvidence.EffectiveRecordDispositionCounts.Single(c => c.Disposition == DxfDisposition.Unsupported).Count);
        var text = Header + "0\nSECTION\n2\nENTITIES\n0\nINSERT\n5\nA\n2\nB\n10\n1\n20\n2\n0\nENDSEC\n0\nSECTION\n2\nOBJECTS\n" + Obj("B") + "0\nENDSEC\n0\nEOF\n";
        r = await Read(text); Assert.DoesNotContain(r.References, e => e.GroupCode == 2); Assert.DoesNotContain(r.Ownership, e => r.Nodes.Single(n => n.Locator == e.Child).OriginalHandle.Text == "A");
    }
    [Fact]
    public async Task FixedDocumentTupleAbsentContextAndImmutableResults()
    {
        var r = await Read(Doc(Obj("000a")), document: Document()); var n = Assert.Single(r.Nodes);
        Assert.Equal("drawing-fixed", n.FullIdentity!.Document.DrawingId); Assert.Equal("revision-fixed", n.FullIdentity.Document.RevisionId); Assert.Equal(Hash, n.FullIdentity.Document.FileHash); Assert.Equal("000a", n.FullIdentity.OriginalHandle); Assert.Equal("A", n.CanonicalHandle);
        Assert.Null(Assert.Single((await Read(Doc(Obj("A")))).Nodes).FullIdentity);
        Assert.Throws<NotSupportedException>(() => ((IList<DxfIdentityNode>)r.Nodes).Clear()); Assert.Throws<NotSupportedException>(() => ((IList<DxfReferenceEdge>)r.References).Clear());
        Assert.Throws<ArgumentException>(() => new DxfDocumentProvenance("d", "r", "invalid"));
    }
    [Theory][InlineData("records")][InlineData("nodes")][InlineData("references")][InlineData("duplicates")][InlineData("members")][InlineData("pairs")]
    public async Task ExactAndFirstOverBudgetsNeverTruncateSuccess(string kind)
    {
        var text = kind is "duplicates" or "members" ? Doc(Obj("A") + Obj("A") + Obj("B") + Obj("B")) : Doc(Obj("A") + Obj("B")); var d = await Prior(text);
        var exact = kind switch { "duplicates" => 2, "members" => 4, "pairs" => d.LexicalEvidence.Pairs.Count, _ => 2 };
        DxfIdentityGraphLimits Limits(int v) => kind switch { "records" => new() { MaxRecords = v }, "nodes" => new() { MaxNodes = v }, "references" => new() { MaxReferences = v }, "duplicates" => new() { MaxDuplicateGroups = v }, "members" => new() { MaxConflictMembers = v }, _ => new() { MaxSourcePairs = v } };
        Assert.NotEqual(DxfGraphStatus.ResourceLimitExceeded, new TextDxfIdentityGraphReader(Limits(exact)).Read(d).Status);
        var failed = new TextDxfIdentityGraphReader(Limits(exact - 1)).Read(d); Assert.Equal(DxfGraphStatus.ResourceLimitExceeded, failed.Status); Assert.False(failed.GraphValidated); Assert.False(failed.IdentityCoverageComplete); Assert.False(failed.ReferenceResolutionComplete);
        Assert.Contains(failed.Diagnostics, x => x.Code == DxfGraphDiagnosticCode.ResourceLimitExceeded);
    }
    [Fact]
    public async Task DiagnosticAndTraversalBudgetsAreBoundedIncludingCycles()
    {
        var d = await Prior(Doc(Obj("G") + Obj("H") + Obj("I"))); var r = new TextDxfIdentityGraphReader(new() { MaxStoredDiagnostics = 2 }).Read(d);
        Assert.Equal(2, r.Diagnostics.Count); Assert.Equal(DxfGraphDiagnosticCode.DiagnosticLimitExceeded, r.Diagnostics[^1].Code); Assert.Equal(DxfGraphStatus.ResourceLimitExceeded, r.Status);
        d = await Prior(Doc(Obj("A"))); var work = 1;
        while (new TextDxfIdentityGraphReader(new() { MaxTraversalWork = work }).Read(d).Status == DxfGraphStatus.ResourceLimitExceeded) { work++; Assert.True(work < 20); }
        Assert.Equal(DxfGraphStatus.Succeeded, new TextDxfIdentityGraphReader(new() { MaxTraversalWork = work }).Read(d).Status);
        Assert.Equal(DxfGraphStatus.ResourceLimitExceeded, new TextDxfIdentityGraphReader(new() { MaxTraversalWork = work - 1 }).Read(d).Status);
        r = new TextDxfIdentityGraphReader(new() { MaxTraversalWork = 3 }).Read(await Prior(FixtureText("owner-cycle"))); Assert.False(r.GraphValidated); Assert.Equal(DxfGraphStatus.ResourceLimitExceeded, r.Status);
    }
    [Fact]
    public async Task CancellationAndFreezeCannotPublishValidatedGraph()
    {
        var d = await Prior(Doc(Obj("A"))); using var c = new CancellationTokenSource(); c.Cancel(); var r = new TextDxfIdentityGraphReader().Read(d, cancellationToken: c.Token);
        Assert.Equal(DxfGraphStatus.Canceled, r.Status); Assert.False(r.GraphValidated); Assert.False(r.IdentityCoverageComplete); Assert.False(r.ReferenceResolutionComplete);
        Assert.Throws<OperationCanceledException>(() => new DxfIdentityGraphReadResult(d, null, DxfGraphStatus.Succeeded, [], [], [], [], [], [], [], true, true, true, c.Token));
    }
    [Fact]
    public async Task CancellationDuringFreezeEnumerationAndDisposedInputStream()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(Doc(Obj("A"))));
        var lexical = await new TextDxfLexicalReader().ReadAsync(stream); stream.Dispose();
        var prior = new TextDxfBlockAnnotationReader().Read(new TextDxfGeometryReader().Read(new TextDxfStructuralReader().Read(lexical)));
        var graph = new TextDxfIdentityGraphReader().Read(prior, Document()); Assert.True(graph.GraphValidated);
        using var c = new CancellationTokenSource();
        IEnumerable<DxfIdentityNode> CancelingNodes() { yield return graph.Nodes[0]; c.Cancel(); yield return graph.Nodes[0]; }
        Assert.Throws<OperationCanceledException>(() => new DxfIdentityGraphReadResult(prior, Document(), DxfGraphStatus.Succeeded, CancelingNodes(), [], [], [], [], [], [], true, true, true, c.Token));
    }
    [Theory][InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)][InlineData(5)][InlineData(6)][InlineData(7)]
    public void InvalidGraphLimitsRejectedBeforeEvidence(int index)
    {
        var l = index switch { 0 => new DxfIdentityGraphLimits { MaxRecords = 0 }, 1 => new() { MaxNodes = 0 }, 2 => new() { MaxReferences = 0 },
            3 => new() { MaxDuplicateGroups = 0 }, 4 => new() { MaxConflictMembers = 0 }, 5 => new() { MaxTraversalWork = 0 }, 6 => new() { MaxStoredDiagnostics = 1 }, _ => new() { MaxSourcePairs = 0 } };
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextDxfIdentityGraphReader(l));
    }
    [Fact]
    public async Task RejectedPredecessorAndUnsupportedVersionCannotValidateGraph()
    {
        var r = await Read(Doc(Obj("A")).Replace("AC1032", "AC1024")); Assert.Equal(DxfGraphStatus.UnsupportedVersion, r.Status); Assert.False(r.GraphValidated); Assert.Empty(r.Nodes);
        var malformed = Header + "0\nSECTION\n2\nBLOCKS\n0\nBLOCK\n2\nMISSING_END\n10\n0\n20\n0\n0\nENDSEC\n0\nEOF\n";
        r = await Read(malformed); Assert.Equal(DxfGraphStatus.InvalidInput, r.Status); Assert.False(r.GraphValidated); Assert.Empty(r.Nodes);
    }
    [Fact]
    public async Task OwnerRoleInKnownObjectScopeRequiresIdentityWithoutGlobalClassRequirement()
    {
        var r = await Read(Doc("0\nCUSTOM_OBJECT\n330\n0\n")); Assert.Equal(DxfIdentityValidity.MissingRequired, Assert.Single(r.Nodes).Validity); Assert.False(r.GraphValidated);
        r = await Read(Header + "0\nSECTION\n2\nENTITIES\n0\nPOINT\n10\n1\n20\n2\n0\nENDSEC\n0\nEOF\n");
        Assert.Empty(r.Nodes); Assert.Single(r.NonIdentityRecords); Assert.DoesNotContain(r.Diagnostics, d => d.Code == DxfGraphDiagnosticCode.MissingRequiredHandle);
    }
    [Fact]
    public async Task UnknownSectionCannotAdvertiseWholeGraphIdentityCoverage()
    {
        var r = await Read(Doc(Obj("A")).Replace("0\nEOF\n", "0\nSECTION\n2\nVENDOR\n0\nCUSTOM\n5\nB\n0\nENDSEC\n0\nEOF\n"));
        Assert.False(r.GraphValidated); Assert.False(r.IdentityCoverageComplete); Assert.Equal(DxfGraphStatus.Partial, r.Status);
        Assert.Contains(r.Diagnostics, d => d.Code == DxfGraphDiagnosticCode.UnvalidatedIdentityScope && d.Source != null);
    }
    [Fact]
    public async Task NonIdentityClassOwnerEvidenceRetainedWithoutFabricatingHandleOrOwnership()
    {
        var r = await Read(Doc(Obj("A")).Replace("0\nEOF\n", "0\nSECTION\n2\nCLASSES\n0\nCLASS\n1\nRAW\n330\nA\n0\nENDSEC\n0\nEOF\n"));
        Assert.Single(r.Nodes); Assert.Single(r.NonIdentityRecords); Assert.False(r.GraphValidated); Assert.Empty(r.Ownership);
        Assert.Contains(r.References, e => e.ContextEvidence.Role == DxfReferenceRole.Owner && e.SourceHandle == null && e.Resolution == DxfReferenceResolution.Resolved);
        Assert.Contains(r.Diagnostics, d => d.Code == DxfGraphDiagnosticCode.OwnershipSourceUnavailable);
    }
    [Fact]
    public async Task IndependentFreshStreamsDeterministicGraphAndNoPredecessorMutation()
    {
        var path = Path.Combine(Root(), "datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf"); var results = new List<string>();
        for (var i = 0; i < 3; i++)
        {
            using var s = File.OpenRead(path); var d = new TextDxfBlockAnnotationReader().Read(new TextDxfGeometryReader().Read(new TextDxfStructuralReader().Read(await new TextDxfLexicalReader().ReadAsync(s))));
            var before = JsonSerializer.Serialize(d); results.Add(JsonSerializer.Serialize(new TextDxfIdentityGraphReader().Read(d, Document()))); Assert.Equal(before, JsonSerializer.Serialize(d));
        }
        Assert.Equal(results[0], results[1]); Assert.Equal(results[0], results[2]); var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(results[0])));
        var evidence = Environment.GetEnvironmentVariable("DTOB_03E_EVIDENCE_DIRECTORY"); if (evidence != null) File.WriteAllText(Path.Combine(evidence, "determinism.sha256"), digest + "\n");
    }
    [Theory][InlineData("duplicate-same")][InlineData("duplicate-cross")][InlineData("duplicate-case")][InlineData("duplicate-zero")]
    [InlineData("malformed")][InlineData("missing-required")][InlineData("root")][InlineData("unresolved-owner")][InlineData("unresolved-pointer")]
    [InlineData("ambiguous-target")][InlineData("self-owner")][InlineData("owner-cycle")][InlineData("pointer-cycle")][InlineData("contextual-references")]
    [InlineData("null-reference")][InlineData("unsupported-target")][InlineData("identity-context")][InlineData("multiple-owners")]
    public async Task ActualSupplementalFilesExerciseFullPipelineAndPreserveSource(string name)
    {
        var text = FixtureText(name); var d = await Prior(text); var before = JsonSerializer.Serialize(d); var r = new TextDxfIdentityGraphReader().Read(d);
        Assert.Equal(before, JsonSerializer.Serialize(d)); Assert.NotEmpty(r.Nodes); Assert.True(r.StructuralEvidence.StructureValidated);
        Assert.All(r.Nodes, n => Assert.Same(n.Locator.Kind == DxfGraphRecordKind.TableHeader ? d.StructuralEvidence.Tables[n.Locator.Index].HeaderRecord : d.StructuralEvidence.Records[n.Locator.Index], n.SourceRecord));
    }
}
