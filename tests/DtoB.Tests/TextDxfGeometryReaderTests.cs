using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DtoB.Cad.Dxf;
using DtoB.Cad.Raw;
using Xunit;

namespace DtoB.Tests;

public class TextDxfGeometryReaderTests
{
    private const string Hash = "2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC";
    private const string Header = "0\nSECTION\n2\nHEADER\n9\n$ACADVER\n1\nAC1032\n0\nENDSEC\n";
    private static string Doc(string entities) => Header + "0\nSECTION\n2\nENTITIES\n" + entities + "0\nENDSEC\n0\nEOF\n";
    private static string Root()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "DtoB.sln"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidDataException("Repository root not found.");
    }
    private static async Task<DxfStructuralReadResult> Structure(string text)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text), false);
        return new TextDxfStructuralReader().Read(await new TextDxfLexicalReader().ReadAsync(stream));
    }
    private static async Task<DxfGeometryReadResult> Read(string text, DxfGeometryLimits? limits = null) => new TextDxfGeometryReader(limits).Read(await Structure(text));
    private static DxfSourceFact Field(RawGeometryRecord r, string name) => r.Fields.Single(f => f.Name == name).Fact;
    private static void Accounting(RawGeometryRecord r)
    {
        Assert.Equal(Enumerable.Range(r.Source.StartPairIndex, r.Source.EndPairIndex - r.Source.StartPairIndex + 1),
            r.Fields.SelectMany(f => f.Fact.SourcePairIndices).Concat(r.UnmappedPairIndices).OrderBy(i => i));
    }
    private static void SourceFact(DxfGeometryReadResult result, DxfSourceFact fact, int index, string text)
    {
        Assert.Equal(DxfFactPresence.Present, fact.Presence); Assert.Equal(DxfFactValidity.Valid, fact.Validity);
        Assert.Equal(index, Assert.Single(fact.SourcePairIndices)); Assert.Equal(index * 2 + 1, fact.Source!.StartLine);
        Assert.Equal(index * 2 + 2, fact.Source.EndLine); Assert.Equal(text, result.StructuralEvidence.LexicalEvidence.Pairs[index].RawTextValue);
    }

    [Fact]
    public async Task ActualControlledGeometryMatchesIndependentRawInventoryEveryPolylineAndAllPairAccounting()
    {
        var path = Path.Combine(Root(), "datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf");
        Assert.Equal(Hash, Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))));
        // Independent BCL/code0 scan, no production geometry/context/grouping helpers.
        var lines = File.ReadAllLines(path, new UTF8Encoding(false, true));
        var raw = Enumerable.Range(0, lines.Length / 2).Select(i => (Index: i, Code: int.Parse(lines[i * 2], CultureInfo.InvariantCulture), Text: lines[i * 2 + 1])).ToArray();
        var markers = raw.Where(p => p.Code == 0).ToArray();
        var types = new[] { "LINE", "LWPOLYLINE", "ARC", "POINT", "SOLID" };
        var independent = new List<(string Type, int Start, int End)>();
        for (var i = 0; i < markers.Length - 1; i++)
            if (types.Contains(markers[i].Text)) independent.Add((markers[i].Text, markers[i].Index, markers[i + 1].Index - 1));
        var counts = independent.GroupBy(r => r.Type).ToDictionary(g => g.Key, g => g.Count());
        Assert.Equal(59, counts["LINE"]); Assert.Equal(16, counts["LWPOLYLINE"]); Assert.Equal(1, counts["ARC"]);
        Assert.Equal(12, counts["POINT"]); Assert.Equal(1, counts["SOLID"]); Assert.Equal(89, independent.Count);
        using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var structure = new TextDxfStructuralReader().Read(await new TextDxfLexicalReader().ReadAsync(input)); input.Dispose();
        var beforeStructure = JsonSerializer.Serialize(structure);
        var result = new TextDxfGeometryReader().Read(structure);
        Assert.Equal(DxfGeometryStatus.Partial, result.Status); Assert.True(result.GeometryMappingComplete);
        Assert.Same(structure, result.StructuralEvidence); Assert.Empty(result.Diagnostics);
        Assert.Equal(independent.Count, result.Geometry.Count);
        Assert.Equal(independent.Select(r => r.Type), result.Geometry.Select(r => r.EntityType));
        Assert.Equal(independent.Select(r => r.Start), result.Geometry.Select(r => r.Source.StartPairIndex));
        Assert.Equal(independent.Select(r => r.End), result.Geometry.Select(r => r.Source.EndPairIndex));
        Assert.All(result.Geometry, r =>
        {
            Assert.Equal(DxfDisposition.Supported, r.Disposition); Accounting(r);
            Assert.Same(structure.Records[r.StructuralRecordIndex], r.SourceRecord);
            foreach (var f in r.Fields.Select(f => f.Fact).Where(f => f.Presence == DxfFactPresence.Present))
            {
                var i = Assert.Single(f.SourcePairIndices); SourceFact(result, f, i, raw[i].Text);
                if (f.Kind == DxfFactKind.Real) Assert.Equal(double.Parse(raw[i].Text, CultureInfo.InvariantCulture), f.Real);
                else if (f.Kind == DxfFactKind.Integer) Assert.Equal(int.Parse(raw[i].Text, CultureInfo.InvariantCulture), f.Integer);
                else Assert.Equal(raw[i].Text, f.Text);
            }
        });
        var totalVertices = 0;
        foreach (var record in result.Geometry.Where(r => r.EntityType == "LWPOLYLINE"))
        {
            var poly = Assert.IsType<RawLwPolyline>(record.Payload);
            var source = raw.Skip(record.Source.StartPairIndex).Take(record.Source.EndPairIndex - record.Source.StartPairIndex + 1).ToArray();
            var xs = source.Where(p => p.Code == 10).ToArray(); var ys = source.Where(p => p.Code == 20).ToArray();
            Assert.Equal(xs.Length, ys.Length); Assert.Equal(xs.Length, poly.ActualVertexCount);
            Assert.Equal(int.Parse(source.Single(p => p.Code == 90).Text, CultureInfo.InvariantCulture), poly.DeclaredVertexCount.Integer);
            Assert.Equal(xs.Length, poly.DeclaredVertexCount.Integer);
            Assert.Equal(int.Parse(source.Single(p => p.Code == 70).Text, CultureInfo.InvariantCulture), poly.Flags.Integer);
            Assert.Equal(xs.Select(p => p.Index), poly.Vertices.Select(v => Assert.Single(v.X.SourcePairIndices)));
            Assert.Equal(ys.Select(p => p.Index), poly.Vertices.Select(v => Assert.Single(v.Y.SourcePairIndices)));
            for (var v = 0; v < xs.Length; v++)
            {
                Assert.Equal(v, poly.Vertices[v].Ordinal);
                Assert.Equal(double.Parse(xs[v].Text, CultureInfo.InvariantCulture), poly.Vertices[v].X.Real);
                Assert.Equal(double.Parse(ys[v].Text, CultureInfo.InvariantCulture), poly.Vertices[v].Y.Real);
                var vertexEnd = v + 1 < xs.Length ? xs[v + 1].Index - 1 : record.Source.EndPairIndex;
                foreach (var (code, fact) in new[] { (40, poly.Vertices[v].StartWidth), (41, poly.Vertices[v].EndWidth), (42, poly.Vertices[v].Bulge), (91, poly.Vertices[v].Identifier) })
                {
                    var expected = source.Where(p => p.Index >= xs[v].Index && p.Index <= vertexEnd && p.Code == code).ToArray();
                    Assert.Equal(expected.Length == 0 ? DxfFactPresence.Absent : DxfFactPresence.Present, fact.Presence);
                    Assert.Equal(expected.Select(p => p.Index), fact.SourcePairIndices);
                }
            }
            foreach (var (name, code) in new[] { ("Elevation", 38), ("ConstantWidth", 43), ("Thickness", 39), ("Extrusion.X", 210), ("Extrusion.Y", 220), ("Extrusion.Z", 230) })
                Assert.Equal(source.Any(p => p.Code == code) ? DxfFactPresence.Present : DxfFactPresence.Absent, Field(record, name).Presence);
            totalVertices += xs.Length;
        }
        Assert.Equal(61, totalVertices); Assert.Equal(4, result.Geometry.Where(r => r.Payload is RawLwPolyline).Max(r => ((RawLwPolyline)r.Payload!).ActualVertexCount));
        // Reviewed exact source-line/range goldens, every core family.
        var lineRecord = result.Geometry.First(r => r.EntityType == "LINE"); Assert.Equal(2856, lineRecord.Source.StartPairIndex); Assert.Equal(2867, lineRecord.Source.EndPairIndex);
        var line = Assert.IsType<RawLine>(lineRecord.Payload); SourceFact(result, line.Start.X, 2862, "0.0"); SourceFact(result, line.End.X, 2865, "900.0");
        var polyRecord = result.Geometry.First(r => r.EntityType == "LWPOLYLINE"); Assert.Equal(2713, polyRecord.Source.StartPairIndex); Assert.Equal(2727, polyRecord.Source.EndPairIndex);
        var firstPoly = Assert.IsType<RawLwPolyline>(polyRecord.Payload); SourceFact(result, firstPoly.ConstantWidth, 2723, "0.15");
        SourceFact(result, firstPoly.Vertices[0].X, 2724, "-0.5"); SourceFact(result, firstPoly.Vertices[0].Y, 2725, "-0.5");
        SourceFact(result, firstPoly.Vertices[1].X, 2726, "0.5"); SourceFact(result, firstPoly.Vertices[1].Y, 2727, "0.5");
        var triangleRecord = result.Geometry.Single(r => r.Source.StartPairIndex == 2786);
        Assert.Equal(2801, triangleRecord.Source.EndPairIndex); var triangle = (RawLwPolyline)triangleRecord.Payload!;
        SourceFact(result, triangle.Vertices[0].Y, 2797, "0.16439898372402537"); SourceFact(result, triangle.Vertices[2].Y, 2801, "-0.16439898372402537"); Assert.Equal(3, triangle.ActualVertexCount);
        var rectangleRecord = result.Geometry.Single(r => r.Source.StartPairIndex == 2821);
        Assert.Equal(2836, rectangleRecord.Source.EndPairIndex); var rectangle = (RawLwPolyline)rectangleRecord.Payload!;
        SourceFact(result, rectangle.Vertices[0].X, 2829, "-200.0"); SourceFact(result, rectangle.Vertices[3].Y, 2836, "200.0"); Assert.Equal(4, rectangle.ActualVertexCount);
        var arcRecord = result.Geometry.Single(r => r.EntityType == "ARC"); Assert.Equal(2868, arcRecord.Source.StartPairIndex); Assert.Equal(2880, arcRecord.Source.EndPairIndex);
        var arc = Assert.IsType<RawArc>(arcRecord.Payload); SourceFact(result, arc.Radius, 2877, "900.0"); SourceFact(result, arc.StartAngleDegrees, 2879, "0.0"); SourceFact(result, arc.EndAngleDegrees, 2880, "90.0"); Assert.Equal(90, arc.EndAngleDegrees.Real);
        var pointRecord = result.Geometry.First(r => r.EntityType == "POINT"); Assert.Equal(3021, pointRecord.Source.StartPairIndex); Assert.Equal(3029, pointRecord.Source.EndPairIndex);
        var point = Assert.IsType<RawPoint>(pointRecord.Payload); SourceFact(result, point.Position.Y, 3028, "-1300.0");
        var solidRecord = result.Geometry.Single(r => r.EntityType == "SOLID"); Assert.Equal(2747, solidRecord.Source.StartPairIndex); Assert.Equal(2766, solidRecord.Source.EndPairIndex);
        var solid = Assert.IsType<RawSolid>(solidRecord.Payload); SourceFact(result, solid.Third.Y, 2762, "-0.16439898372402537"); SourceFact(result, solid.Fourth.Y, 2765, "-0.16439898372402537"); Assert.Equal(solid.Third.Y.Real, solid.Fourth.Y.Real);
        Assert.Equal(beforeStructure, JsonSerializer.Serialize(structure)); // Evidence/dispositions never mutated.
        var effective = result.EffectiveRecordDispositionCounts.ToDictionary(c => c.Disposition, c => c.Count);
        Assert.Equal(structure.Records.Count, effective.Values.Sum());
        Assert.Equal(structure.RecordDispositionCounts.Single(c => c.Disposition == DxfDisposition.Supported).Count + independent.Count, effective[DxfDisposition.Supported]);
        Assert.False(effective.ContainsKey(DxfDisposition.DeferredTo03C)); Assert.False(effective.ContainsKey(DxfDisposition.Invalid));
        Assert.Equal(52, effective[DxfDisposition.Unsupported]); Assert.Equal(93, effective[DxfDisposition.DeferredTo03D]);
        using var again = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var second = new TextDxfGeometryReader().Read(new TextDxfStructuralReader().Read(await new TextDxfLexicalReader().ReadAsync(again)));
        var json = JsonSerializer.Serialize(result); Assert.Equal(json, JsonSerializer.Serialize(second));
        Assert.Equal(Hash, Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))));
        var dir = Path.Combine(Root(), "artifacts/phase03/03c-geometry-20261014"); Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "controlled-geometry.json"), json);
        File.WriteAllText(Path.Combine(dir, "controlled-qa.json"), JsonSerializer.Serialize(new
        {
            hash = Hash, independentCounts = counts, mappedCounts = result.Geometry.GroupBy(r => r.EntityType).ToDictionary(g => g.Key, g => g.Count()),
            geometryCount = result.Geometry.Count, polylineVertices = totalVertices, maxVertices = 4,
            coordinateSlots = counts["LINE"] * 2 + totalVertices + counts["ARC"] + counts["POINT"] + counts["SOLID"] * 4,
            effectiveCounts = result.EffectiveRecordDispositionCounts.ToDictionary(c => c.Disposition.ToString(), c => c.Count),
            status = result.Status.ToString(), complete = result.GeometryMappingComplete, diagnostics = result.Diagnostics.Count,
            geometrySha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)))
        }, new JsonSerializerOptions { WriteIndented = true }));
    }

    [Fact]
    public async Task LineDefaultsStayAbsentAndExplicitZeroAndZeroLengthRemainSourceFacts()
    {
        var r = await Read(Doc("0\nLINE\n10\n-0.0\n20\n1e+20\n11\n-0.0\n21\n1e+20\n")); var record = Assert.Single(r.Geometry);
        Assert.Equal(DxfDisposition.Supported, record.Disposition); var line = Assert.IsType<RawLine>(record.Payload);
        Assert.Equal(line.Start.X.Real, line.End.X.Real); Assert.Equal(line.Start.Y.Real, line.End.Y.Real);
        Assert.Equal(DxfFactPresence.Absent, line.Start.Z.Presence); Assert.Null(line.Start.Z.Real);
        Assert.Equal(DxfFactPresence.Absent, record.Thickness!.Presence); Assert.Equal(DxfFactPresence.Absent, record.Extrusion!.Z.Presence);
        Assert.Equal("-0.0", r.StructuralEvidence.LexicalEvidence.Pairs[line.Start.X.SourcePairIndices[0]].RawTextValue); Accounting(record);
        var explicitZero = await Read(Doc("0\nLINE\n10\n0\n20\n0\n30\n0\n11\n0\n21\n0\n31\n0\n39\n0\n210\n0\n220\n0\n230\n1\n"));
        Assert.Equal(DxfFactPresence.Present, ((RawLine)explicitZero.Geometry[0].Payload!).Start.Z.Presence);
        Assert.Equal(0, explicitZero.Geometry[0].Thickness!.Real); Assert.Equal(1, explicitZero.Geometry[0].Extrusion!.Z.Real);
    }
    [Theory]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("1e999")]
    [InlineData("broken")]
    public async Task NonfiniteRequiredCoordinateIsInvalidWithoutCoercion(string value)
    {
        var r = await Read(Doc("0\nLINE\n10\n" + value + "\n20\n0\n11\n2\n21\n3\n")); var e = Assert.Single(r.Geometry);
        Assert.Equal(DxfGeometryStatus.Partial, r.Status); Assert.False(r.GeometryMappingComplete); Assert.Equal(DxfDisposition.Invalid, e.Disposition);
        var f = ((RawLine)e.Payload!).Start.X; Assert.Equal(DxfFactValidity.Invalid, f.Validity); Assert.Null(f.Real);
        Assert.Equal(value, r.StructuralEvidence.LexicalEvidence.Pairs[Assert.Single(f.SourcePairIndices)].RawTextValue);
        Assert.Contains(r.Diagnostics, d => d.Code == DxfGeometryDiagnosticCode.InvalidNumericValue && d.Source == f.Source); Accounting(e);
    }
    [Fact]
    public async Task RequiredMissingAndDuplicateCoordinatesHaveLocatedInvalidFacts()
    {
        var r = await Read(Doc("0\nLINE\n10\n1\n10\n2\n11\n3\n21\n4\n")); var e = r.Geometry[0]; var p = (RawLine)e.Payload!;
        Assert.Equal(DxfFactValidity.Invalid, p.Start.X.Validity); Assert.Equal(2, p.Start.X.SourcePairIndices.Count); Assert.Null(p.Start.X.Real);
        Assert.Equal(DxfFactPresence.Absent, p.Start.Y.Presence); Assert.Equal(DxfFactValidity.Invalid, p.Start.Y.Validity); Assert.Null(p.Start.Y.Real);
        Assert.Contains(r.Diagnostics, d => d.Code == DxfGeometryDiagnosticCode.MissingRequiredField && d.Source == e.Source);
        Assert.Contains(r.Diagnostics, d => d.Code == DxfGeometryDiagnosticCode.DuplicateField); Accounting(e);
    }
    [Fact]
    public async Task PolylineWidthsBulgeElevationNormalAndClosedFlagAreRawOnly()
    {
        var r = await Read(Doc("0\nLWPOLYLINE\n90\n2\n70\n1\n38\n12.5\n39\n0\n43\n99\n210\n1\n220\n2\n230\n3\n10\n1\n20\n2\n40\n3\n41\n4\n42\n-0.5\n91\n17\n10\n5\n20\n6\n"));
        var e = r.Geometry[0]; var p = Assert.IsType<RawLwPolyline>(e.Payload); Assert.Equal(DxfDisposition.Supported, e.Disposition);
        Assert.Equal(2, p.ActualVertexCount); Assert.Equal(1, p.Flags.Integer); Assert.Equal(12.5, p.Elevation.Real); Assert.Equal(99, p.ConstantWidth.Real);
        Assert.Equal(3, p.Vertices[0].StartWidth.Real); Assert.Equal(4, p.Vertices[0].EndWidth.Real); Assert.Equal(-0.5, p.Vertices[0].Bulge.Real); Assert.Equal(17, p.Vertices[0].Identifier.Integer);
        Assert.Equal(DxfFactPresence.Absent, p.Vertices[1].Bulge.Presence); Assert.Null(p.Vertices[1].Bulge.Real);
        Assert.Equal(3, e.Extrusion!.Z.Real); Assert.Equal(5, p.Vertices[1].X.Real); Accounting(e);
    }
    [Theory]
    [InlineData("90\n2\n70\n0\n10\n1\n20\n2\n", DxfGeometryDiagnosticCode.VertexCountMismatch)]
    [InlineData("90\n1\n10\n1\n", DxfGeometryDiagnosticCode.MissingRequiredField)]
    [InlineData("90\n1\n20\n2\n10\n1\n20\n2\n", DxfGeometryDiagnosticCode.InvalidVertexOrder)]
    [InlineData("90\n1\n10\n1\n40\n2\n20\n3\n", DxfGeometryDiagnosticCode.InvalidVertexOrder)]
    [InlineData("90\n1\n10\n1\n20\n2\n20\n3\n", DxfGeometryDiagnosticCode.DuplicateField)]
    [InlineData("90\n1\n10\n1\n20\n2\n42\n1\n42\n2\n", DxfGeometryDiagnosticCode.DuplicateField)]
    [InlineData("90\n0\n", DxfGeometryDiagnosticCode.InvalidVertexCount)]
    [InlineData("90\n2147483648\n10\n1\n20\n2\n", DxfGeometryDiagnosticCode.InvalidNumericValue)]
    public async Task MalformedPolylineIsDiagnosedAndAllPairsRemain(string payload, DxfGeometryDiagnosticCode code)
    {
        var r = await Read(Doc("0\nLWPOLYLINE\n" + payload)); var e = r.Geometry[0];
        Assert.Equal(DxfDisposition.Invalid, e.Disposition); Assert.False(r.GeometryMappingComplete);
        Assert.Contains(r.Diagnostics, d => d.Code == code && d.Source != null); Accounting(e);
    }
    [Theory]
    [InlineData("-1", DxfGeometryDiagnosticCode.InvalidRadius)]
    [InlineData("0", DxfGeometryDiagnosticCode.InvalidRadius)]
    [InlineData("NaN", DxfGeometryDiagnosticCode.InvalidNumericValue)]
    public async Task ArcInvalidRadiusNeverBecomesAbsoluteValue(string radius, DxfGeometryDiagnosticCode code)
    {
        var r = await Read(Doc("0\nARC\n10\n1\n20\n2\n40\n" + radius + "\n50\n720\n51\n-90\n")); var a = (RawArc)r.Geometry[0].Payload!;
        Assert.Equal(DxfDisposition.Invalid, r.Geometry[0].Disposition); Assert.Null(a.Radius.Real); Assert.Equal(DxfFactValidity.Invalid, a.Radius.Validity);
        Assert.Equal(720, a.StartAngleDegrees.Real); Assert.Equal(-90, a.EndAngleDegrees.Real); Assert.Contains(r.Diagnostics, d => d.Code == code); Accounting(r.Geometry[0]);
    }
    [Fact]
    public async Task ArcAnglesAndPointUcsAngleRemainUnwrappedDegrees()
    {
        var r = await Read(Doc("0\nARC\n10\n1\n20\n2\n40\n3\n50\n720\n51\n-90\n0\nPOINT\n10\n4\n20\n5\n50\n450\n"));
        var arc = (RawArc)r.Geometry[0].Payload!; Assert.Equal(720, arc.StartAngleDegrees.Real); Assert.Equal(-90, arc.EndAngleDegrees.Real);
        var point = (RawPoint)r.Geometry[1].Payload!; Assert.Equal(450, point.AngleDegrees.Real); Assert.Equal(DxfFactPresence.Absent, point.Position.Z.Presence);
    }
    [Fact]
    public async Task DegenerateSolidPreservesAllFourSlotsWithoutCleanup()
    {
        var r = await Read(Doc("0\nSOLID\n10\n1\n20\n2\n11\n1\n21\n2\n12\n1\n22\n2\n13\n1\n23\n2\n"));
        var e = r.Geometry[0]; var solid = (RawSolid)e.Payload!; Assert.Equal(DxfDisposition.Supported, e.Disposition);
        Assert.Equal(solid.First.X.Real, solid.Fourth.X.Real); Assert.NotEqual(solid.Third.X.SourcePairIndices[0], solid.Fourth.X.SourcePairIndices[0]); Accounting(e);
    }
    [Fact]
    public async Task ControlsXDataAndForeignSubclassCannotSupplyGeometryFacts()
    {
        var r = await Read(Doc("0\nLINE\n100\nAcDbEntity\n8\nLayer\n102\n{PRIVATE\n10\n999\n102\n}\n100\nAcDbLine\n10\n1\n20\n2\n11\n3\n21\n4\n1001\nAPP\n10\n888\n"));
        Assert.Equal(DxfDisposition.Supported, r.Geometry[0].Disposition); Assert.Equal(1, ((RawLine)r.Geometry[0].Payload!).Start.X.Real); Assert.Equal("Layer", r.Geometry[0].Layer!.Text); Accounting(r.Geometry[0]);
        var foreign = await Read(Doc("0\nLINE\n100\nForeign\n10\n1\n20\n2\n11\n3\n21\n4\n"));
        Assert.Equal(DxfDisposition.Invalid, foreign.Geometry[0].Disposition); Assert.Contains(foreign.Diagnostics, d => d.Code == DxfGeometryDiagnosticCode.ForeignFieldContext);
    }
    [Theory]
    [InlineData("ELLIPSE")]
    [InlineData("SPLINE")]
    [InlineData("HATCH")]
    [InlineData("3DSOLID")]
    [InlineData("ACAD_PROXY_ENTITY")]
    public async Task UnsupportedGeometryIsRetainedWithoutApproximation(string type)
    {
        var r = await Read(Doc("0\n" + type + "\n10\n1\n20\n2\n")); var e = Assert.Single(r.Geometry);
        Assert.Null(e.Payload); Assert.Equal(DxfDisposition.Unsupported, e.Disposition); Assert.False(r.GeometryMappingComplete);
        Assert.Contains(r.Diagnostics, d => d.Code == DxfGeometryDiagnosticCode.UnsupportedGeometry); Accounting(e);
    }
    [Fact]
    public async Task LegacyPolylineSequenceIsExplicitlyGatedAndDoesNotParseAsLightweight()
    {
        var r = await Read(Doc("0\nPOLYLINE\n70\n8\n0\nVERTEX\n10\n1\n20\n2\n0\nSEQEND\n"));
        Assert.Equal(2, r.Geometry.Count); Assert.All(r.Geometry, e => { Assert.Null(e.Payload); Assert.Equal(DxfDisposition.DeferredTo03C, e.Disposition); Accounting(e); });
        Assert.Equal(2, r.Diagnostics.Count(d => d.Code == DxfGeometryDiagnosticCode.LegacyPolylineGated));
        Assert.Equal(DxfDisposition.DeferredTo03D, r.StructuralEvidence.Records[^1].Disposition);
    }

    [Theory]
    [InlineData("records")]
    [InlineData("perPolyline")]
    [InlineData("totalVertices")]
    [InlineData("fields")]
    [InlineData("sourcePairs")]
    [InlineData("structuralRecords")]
    public async Task InjectedLimitsRejectFirstExcessWithNoTruncatedSuccess(string dimension)
    {
        var text = Doc("0\nLWPOLYLINE\n90\n2\n70\n0\n10\n1\n20\n2\n10\n3\n20\n4\n0\nPOINT\n10\n0\n20\n0\n");
        var limits = dimension switch
        {
            "records" => new DxfGeometryLimits { MaxGeometryRecords = 1 },
            "perPolyline" => new DxfGeometryLimits { MaxVerticesPerPolyline = 1 },
            "totalVertices" => new DxfGeometryLimits { MaxTotalVertices = 1 },
            "fields" => new DxfGeometryLimits { MaxFieldsPerRecord = 2 },
            "sourcePairs" => new DxfGeometryLimits { MaxSourcePairs = 1 },
            _ => new DxfGeometryLimits { MaxStructuralRecords = 1 }
        };
        var r = await Read(text, limits); Assert.Equal(DxfGeometryStatus.ResourceLimitExceeded, r.Status); Assert.False(r.GeometryMappingComplete);
        Assert.Contains(r.Diagnostics, d => d.Code == DxfGeometryDiagnosticCode.GeometryLimitExceeded);
    }
    [Fact]
    public async Task ExactLimitsDiagnosticReservedSlotAndConfigurationAreEnforced()
    {
        var structure = await Structure(Doc("0\nLWPOLYLINE\n90\n2\n70\n0\n10\n1\n20\n2\n10\n3\n20\n4\n"));
        var r = new TextDxfGeometryReader(new() { MaxGeometryRecords = 1, MaxVerticesPerPolyline = 2, MaxTotalVertices = 2,
            MaxFieldsPerRecord = 7, MaxStructuralRecords = 1, MaxSourcePairs = structure.LexicalEvidence.Pairs.Count }).Read(structure);
        Assert.Equal(DxfGeometryStatus.Succeeded, r.Status);
        var refused = await Read(Doc("0\nLINE\n"), new() { MaxStoredDiagnostics = 3 });
        Assert.Equal(DxfGeometryStatus.ResourceLimitExceeded, refused.Status); Assert.Equal(3, refused.Diagnostics.Count);
        Assert.Equal(DxfGeometryDiagnosticCode.DiagnosticLimitExceeded, refused.Diagnostics[^1].Code);
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextDxfGeometryReader(new() { MaxTotalVertices = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextDxfGeometryReader(new() { MaxStoredDiagnostics = 1 }));
    }
    [Fact]
    public async Task CancellationAndImmutableGeometryPublicationAreVerified()
    {
        var structure = await Structure(Doc("0\nLWPOLYLINE\n90\n1\n10\n1\n20\n2\n"));
        using var cts = new CancellationTokenSource(); cts.Cancel();
        var canceled = new TextDxfGeometryReader().Read(structure, cts.Token); Assert.Equal(DxfGeometryStatus.Canceled, canceled.Status); Assert.False(canceled.GeometryMappingComplete);
        var r = new TextDxfGeometryReader().Read(structure); var e = r.Geometry[0]; var p = (RawLwPolyline)e.Payload!;
        Assert.Equal(DxfFactPresence.Absent, p.Flags.Presence);
        Assert.Throws<NotSupportedException>(() => ((IList<RawGeometryRecord>)r.Geometry).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<RawLwVertex>)p.Vertices).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<DxfNamedFact>)e.Fields).Clear());
        using var during = new CancellationTokenSource();
        IEnumerable<RawGeometryRecord> Canceling()
        { yield return e; during.Cancel(); yield return e; }
        Assert.Throws<OperationCanceledException>(() => new DxfGeometryReadResult(structure, DxfGeometryStatus.Succeeded, Canceling(), [], true, during.Token));
    }
    [Theory]
    [InlineData("0\nEOF\n", DxfGeometryStatus.InvalidInput)]
    [InlineData("0\nSECTION\n2\nHEADER\n9\n$ACADVER\n1\nAC1024\n0\nENDSEC\n0\nEOF\n", DxfGeometryStatus.UnsupportedVersion)]
    public async Task InvalidStructuralEvidenceCannotBecomeGeometrySuccess(string text, DxfGeometryStatus status)
    {
        var r = await Read(text); Assert.Equal(status, r.Status); Assert.Empty(r.Geometry); Assert.False(r.GeometryMappingComplete);
        Assert.Contains(r.Diagnostics, d => d.Code == DxfGeometryDiagnosticCode.StructuralInputRejected);
    }

    [Fact]
    public async Task ActualTextCircleGateVerifiesIndependentSourcePairsAndNativeValues()
    {
        var path = Path.Combine(Root(), "datasets/controlled/phase03c/circle-reviewed.dxf");
        var bytes = File.ReadAllBytes(path); var source = File.ReadAllLines(path, new UTF8Encoding(false, true));
        Assert.Equal("10", source[24]); Assert.Equal("-1.5", source[25]); Assert.Equal("40", source[30]); Assert.Equal("2.5", source[31]);
        var r = await Read(File.ReadAllText(path)); var e = Assert.Single(r.Geometry); var circle = Assert.IsType<RawCircle>(e.Payload);
        Assert.Equal(DxfDisposition.Supported, e.Disposition); Assert.True(r.GeometryMappingComplete); Assert.Empty(r.Diagnostics);
        SourceFact(r, circle.Center.X, 12, "-1.5"); SourceFact(r, circle.Center.Y, 13, "2.25"); SourceFact(r, circle.Center.Z, 14, "-3.5"); SourceFact(r, circle.Radius, 15, "2.5");
        SourceFact(r, e.Thickness!, 16, "0.125"); SourceFact(r, e.Extrusion!.Z, 19, "1.0"); Assert.Equal(0.1, e.Extrusion!.X.Real); Assert.Equal(0.2, e.Extrusion!.Y.Real);
        Assert.Equal(-3.5, circle.Center.Z.Real); Assert.Equal(2.5, circle.Radius.Real); Accounting(e);
        Assert.Equal(bytes, File.ReadAllBytes(path));
    }
    [Theory]
    [InlineData("circle-reviewed", DxfDisposition.Supported)]
    [InlineData("lwpolyline-full", DxfDisposition.Supported)]
    [InlineData("lwpolyline-count-mismatch", DxfDisposition.Invalid)]
    [InlineData("lwpolyline-missing-y", DxfDisposition.Invalid)]
    [InlineData("lwpolyline-invalid-order", DxfDisposition.Invalid)]
    [InlineData("line-native-3d", DxfDisposition.Supported)]
    [InlineData("line-optional-z", DxfDisposition.Supported)]
    [InlineData("line-zero-length", DxfDisposition.Supported)]
    [InlineData("line-missing-y", DxfDisposition.Invalid)]
    [InlineData("line-nonfinite", DxfDisposition.Invalid)]
    [InlineData("line-duplicate-x", DxfDisposition.Invalid)]
    [InlineData("solid-degenerate", DxfDisposition.Supported)]
    [InlineData("arc-native-degrees", DxfDisposition.Supported)]
    [InlineData("point-angle", DxfDisposition.Supported)]
    [InlineData("unsupported-ellipse", DxfDisposition.Unsupported)]
    [InlineData("legacy-gated", DxfDisposition.DeferredTo03C)]
    public async Task ReviewedSupplementalFilesExerciseActualReaderPipeline(string name, DxfDisposition disposition)
    {
        var path = Path.Combine(Root(), "datasets/controlled/phase03c", name + ".dxf");
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var structural = new TextDxfStructuralReader().Read(await new TextDxfLexicalReader().ReadAsync(stream));
        var r = new TextDxfGeometryReader().Read(structural); Assert.NotEmpty(r.Geometry); Assert.All(r.Geometry, e => { Assert.Equal(disposition, e.Disposition); Accounting(e); });
        if (name == "line-native-3d")
        {
            Assert.Equal(1, structural.Header!.InsertionUnits.Integer); var p = (RawLine)r.Geometry[0].Payload!;
            Assert.Equal(1, p.Start.X.Real); Assert.Equal(3, p.Start.Z.Real); Assert.Equal(6, p.End.Z.Real); Assert.Equal(0.4, r.Geometry[0].Extrusion!.Z.Real);
        }
        if (name == "lwpolyline-full")
        {
            var p = (RawLwPolyline)r.Geometry[0].Payload!; Assert.Equal(2, p.ActualVertexCount); Assert.Equal(1, p.Flags.Integer);
            Assert.Equal(12.5, p.ConstantWidth.Real); Assert.Equal(-0.5, p.Vertices[0].Bulge.Real); Assert.Equal(17, p.Vertices[0].Identifier.Integer);
            Assert.Equal(DxfFactPresence.Absent, p.Vertices[1].StartWidth.Presence);
        }
    }
    [Fact]
    public async Task TotalVertexBudgetIncludesLineEndpointsPointAndSolidSlots()
    {
        var text = Doc("0\nLINE\n10\n1\n20\n2\n11\n3\n21\n4\n0\nPOINT\n10\n0\n20\n0\n");
        var accepted = await Read(text, new() { MaxTotalVertices = 3 }); Assert.Equal(DxfGeometryStatus.Succeeded, accepted.Status);
        var refused = await Read(text, new() { MaxTotalVertices = 2 }); Assert.Equal(DxfGeometryStatus.ResourceLimitExceeded, refused.Status); Assert.False(refused.GeometryMappingComplete);
    }
    [Fact]
    public async Task UnmappedUnsupportedFieldsAreNotFalselyReportedAbsent()
    {
        var r = await Read(Doc("0\nELLIPSE\n8\nLayerPresent\n39\n5\n210\n1\n")); var e = Assert.Single(r.Geometry);
        Assert.Null(e.Layer); Assert.Null(e.Thickness); Assert.Null(e.Extrusion); Assert.Empty(e.Fields);
        Assert.Equal(DxfDisposition.Unsupported, e.Disposition); Accounting(e);
        Assert.Contains(r.StructuralEvidence.LexicalEvidence.Pairs, p => p.RawTextValue == "LayerPresent");
    }
    [Theory]
    [InlineData("CIRCLE", "0")]
    [InlineData("CIRCLE", "-1")]
    [InlineData("CIRCLE", "Infinity")]
    [InlineData("ARC", "1e999")]
    public async Task RadialProfilesRejectInvalidRadiusWithoutReplacingSource(string type, string radius)
    {
        var r = await Read(Doc("0\n" + type + "\n10\n1\n20\n2\n40\n" + radius + "\n50\n0\n51\n90\n"));
        var fact = Field(r.Geometry[0], "Radius"); Assert.Equal(DxfFactValidity.Invalid, fact.Validity); Assert.Null(fact.Real);
        Assert.Equal(radius, r.StructuralEvidence.LexicalEvidence.Pairs[Assert.Single(fact.SourcePairIndices)].RawTextValue); Accounting(r.Geometry[0]);
    }
    [Fact]
    public async Task InvalidOptionalValueAndMalformedVertexIdentifierAreNeverReclassifiedAsAbsent()
    {
        var r = await Read(Doc("0\nLWPOLYLINE\n90\n1\n39\nNaN\n10\n1\n20\n2\n91\n2147483648\n")); var e = r.Geometry[0];
        Assert.Equal(DxfDisposition.Invalid, e.Disposition); Assert.Equal(DxfFactPresence.Present, e.Thickness!.Presence); Assert.Equal(DxfFactValidity.Invalid, e.Thickness!.Validity);
        var id = ((RawLwPolyline)e.Payload!).Vertices[0].Identifier; Assert.Equal(DxfFactValidity.Invalid, id.Validity); Assert.Null(id.Integer); Accounting(e);
    }
    [Fact]
    public async Task GeometryDefaultsAndBoundedFailureInventoryAreExplicit()
    {
        var limits = new DxfGeometryLimits(); Assert.Equal(512, limits.MaxGeometryRecords); Assert.Equal(64, limits.MaxVerticesPerPolyline);
        Assert.Equal(1024, limits.MaxTotalVertices); Assert.Equal(512, limits.MaxFieldsPerRecord); Assert.Equal(5000, limits.MaxStoredDiagnostics);
        Assert.Equal(4096, limits.MaxStructuralRecords); Assert.Equal(250000, limits.MaxSourcePairs);
        var r = await Read(Doc("0\nPOINT\n10\n1\n20\n2\n0\nPOINT\n10\n3\n20\n4\n"), new() { MaxStructuralRecords = 1 });
        Assert.Equal(DxfGeometryStatus.ResourceLimitExceeded, r.Status); Assert.False(r.EffectiveInventoryAvailable);
        Assert.Empty(r.EffectiveRecordDispositionCounts); Assert.False(r.GeometryMappingComplete);
    }
}
