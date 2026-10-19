using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DtoB.Cad.Normalize;
using DtoB.Cad.Normalize.Assembly;
using DtoB.Cad.Raw;
using DtoB.Cad.V2;
using DtoB.Geometry;
using Xunit;

namespace DtoB.Tests;

internal sealed record Cad04HSpec(string Key, string Path, string SHA256, long Bytes,
    string InputProfile, string Coverage, string ExpectedState, bool CadGolden,
    bool BoundsGolden, bool DiagnosticGolden, string Exclusion);

internal static class Cad04HCorpus
{
    internal const string ControlledHash = "8A5E078ADB9990C7127B7E626DEC1333071F481FA4A4B7F91CD17D23505F4F24";
    internal static string Root => CadV2ContractFixture.Root();
    internal static string PathOf(string path) => Path.Combine(Root, path);
    internal static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    internal static Cad04HSpec[] Manifest() => JsonSerializer.Deserialize<Cad04HSpec[]>(File.ReadAllText(PathOf("datasets/controlled/phase04/manifest.json")))!;
    internal static IEnumerable<Cad04HSpec> Cases => Manifest().Where(f => !f.Key.StartsWith("legacy-", StringComparison.Ordinal) || f.CadGolden);
    internal static async Task<(RawDrawingSnapshot Raw, CadNormalizationResult Result)> Read(Cad04HSpec f)
    {
        var before = File.ReadAllBytes(PathOf(f.Path));
        Assert.Equal(f.Bytes, before.LongLength); Assert.Equal(f.SHA256, Hash(before));
        var raw = await Cad04BScalarFixture.Snapshot(before);
        var rawBefore = JsonSerializer.SerializeToUtf8Bytes(raw);
        var result = Cad04GFixture.Normalize(raw);
        Assert.Equal(rawBefore, JsonSerializer.SerializeToUtf8Bytes(raw));
        Assert.Equal(before, File.ReadAllBytes(PathOf(f.Path)));
        return (raw, result);
    }
    internal static byte[] Triple(CadDocument d)
    {
        d.ValidateStructure(); var a = CadIrJson.SerializeUtf8(d);
        var second = CadIrJson.Deserialize(Encoding.UTF8.GetString(a)); second.ValidateStructure();
        var b = CadIrJson.SerializeUtf8(second);
        var third = CadIrJson.Deserialize(Encoding.UTF8.GetString(b)); third.ValidateStructure();
        Assert.Equal(a, b); Assert.Equal(b, CadIrJson.SerializeUtf8(third)); return a;
    }
    // Fingerprints preserve complete ordered registry/diagnostic projections, not invented ID domains.
    internal static string Summary(CadDocument d, byte[] bytes) => JsonSerializer.Serialize(new
    {
        Sha256 = Hash(bytes), Bytes = bytes.Length, State = d.Completeness.State.ToString(),
        Sources = d.Provenance.Records.Count, Facts = d.Provenance.Facts.Count,
        Layers = d.Layers.Count, Styles = d.Styles.Count, Geometry = d.Geometry.Count,
        Entities = d.Entities.Count, Definitions = d.Definitions.Count, Occurrences = d.Occurrences.Count,
        ModelSpace = d.ModelSpace.Count, Annotations = d.Entities.Count(e => e.Annotation is not null),
        Ledger = d.Ledger.GroupBy(l => l.State).Select(g => new { State = g.Key.ToString(), Count = g.Count() }),
        Bounds = new CadBoundsQuery(d).Model(),
        SourceReferences = Fingerprint(d.Provenance.Records), FactReferences = Fingerprint(d.Provenance.Facts),
        LayerReferences = Fingerprint(d.Layers), AnnotationReferences = Fingerprint(d.Entities.Where(e => e.Annotation is not null).Select(e => new { e.Id, e.Source, e.Annotation })),
        Diagnostics = Fingerprint(d.Diagnostics), RawDiagnostics = Fingerprint(d.RawDiagnostics),
        Ids = new { Style = d.Styles.Select(x => x.Id.Value), Geometry = d.Geometry.Select(x => x.Id.Value),
            Entity = d.Entities.Select(x => x.Id.Value), Definition = d.Definitions.Select(x => x.Id.Value), Occurrence = d.Occurrences.Select(x => x.Id.Value) }
    }) + "\n";
    private static string Fingerprint<T>(T value) => Hash(JsonSerializer.SerializeToUtf8Bytes(value));

    internal static void SourceAndMathOracle(Cad04HSpec f, RawDrawingSnapshot raw, CadDocument d)
    {
        Assert.Equal(raw.Records.Count, d.Provenance.Records.Count);
        Assert.Equal(Enumerable.Range(0, raw.Records.Count), d.Ledger.Select(l => l.Source.Index));
        foreach (var row in raw.Records)
        {
            var record = d.Provenance.Records[row.StructuralRecordIndex];
            Assert.Equal(row.SourceRecord.Source.StartPairIndex, record.Locator.FirstPair);
            Assert.Equal(row.Identity?.CanonicalHandle, record.CanonicalHandle);
        }
        foreach(var definition in d.Definitions)
        {
            var source=raw.BlockDefinitions.Single(b=>b.BeginRecordIndex==definition.BlockSource.Index);
            var expected=source.Children.Select(c=>c.StructuralRecordIndex)
                .Where(i=>raw.Records[i].SourceRecord.RecordType is not ("BLOCK" or "ENDBLK" or "SEQEND"));
            Assert.Equal(expected,definition.ChildEntityIds.Select(id=>d.Entities.Single(e=>e.Id.Value==id.Value).Source.Index));
        }
        Assert.Equal(raw.Diagnostics.Select(x=>x.Stage.ToString()+":"+x.Index.ToString(CultureInfo.InvariantCulture)),
            d.RawDiagnostics.Select(x=>x.Arguments[0]+":"+x.Arguments[1]));
        if (f.Key.StartsWith("unit-", StringComparison.Ordinal))
        {
            var scale = f.Key switch { "unit-1" => 25.4, "unit-2" => 304.8, "unit-4" => 1, "unit-5" => 10, "unit-6" => 1000, _ => throw new InvalidDataException() };
            Assert.Equal(scale, d.Units.SourceToMillimeters);
            var line = Assert.Single(d.Geometry.OfType<CadLine>());
            Assert.Equal(new Point3(scale, 2 * scale, 3 * scale), line.EndMm);
        }
        if (f.Key.StartsWith("ocs-", StringComparison.Ordinal))
        {
            // Independent DXF AAA vector algebra; no call to coordinate/AAA production helpers.
            Assert.IsType<RawCircle>(Assert.Single(raw.Geometry).Payload);
            var n = Assert.Single(raw.Geometry).Extrusion!;
            var z = Unit(new(n.X.Real!.Value, n.Y.Real!.Value, n.Z.Real!.Value));
            var u = Unit(Cross(Math.Abs(z.X) < 1.0 / 64 && Math.Abs(z.Y) < 1.0 / 64 ? new(0, 1, 0) : new(0, 0, 1), z));
            var v = Cross(z, u); var c = Assert.Single(d.Geometry.OfType<CadCircle>());
            Cad04GFixture.Point(u, c.Plane.AxisU); Cad04GFixture.Point(v, c.Plane.AxisV);
            var center = new Point3(2*u.X+3*v.X+4*z.X, 2*u.Y+3*v.Y+4*z.Y, 2*u.Z+3*v.Z+4*z.Z);
            Cad04GFixture.Point(center, c.Plane.OriginMm); Assert.Equal(5, c.RadiusMm);
            var b = new CadBoundsQuery(d).Model().Bounds!;
            Cad04GFixture.Point(new(center.X-5*Math.Sqrt(u.X*u.X+v.X*v.X), center.Y-5*Math.Sqrt(u.Y*u.Y+v.Y*v.Y), center.Z-5*Math.Sqrt(u.Z*u.Z+v.Z*v.Z)), b.Min);
            Cad04GFixture.Point(new(center.X+5*Math.Sqrt(u.X*u.X+v.X*v.X), center.Y+5*Math.Sqrt(u.Y*u.Y+v.Y*v.Y), center.Z+5*Math.Sqrt(u.Z*u.Z+v.Z*v.Z)), b.Max);
        }
        if (f.Key.StartsWith("bulge-", StringComparison.Ordinal) && f.Key is not ("bulge-degenerate" or "bulge-closed-tilted"))
        {
            var b = double.Parse(f.Key[6..].Replace("minus", "-", StringComparison.Ordinal).Replace('p', '.'), CultureInfo.InvariantCulture);
            var p = Assert.Single(d.Geometry.OfType<CadPolyline>()); Assert.False(p.Closed);
            var segment = d.Geometry.Single(g => g.Id.Value == Assert.Single(p.SegmentIds).Value);
            if (b == 0) { var l = Assert.IsType<CadLine>(segment); Assert.Equal(new Point3(2,0,0), l.EndMm); }
            else
            {
                var a = Assert.IsType<CadArc>(segment); Cad04GFixture.Point(new(1,(1/b-b)/2,0), a.Plane.OriginMm);
                Cad04GFixture.Near((Math.Abs(b)+1/Math.Abs(b))/2, a.RadiusMm); Cad04GFixture.Near(4*Math.Atan(b), a.SweepRadians);
                Point3 At(double t) => new(a.Plane.OriginMm.X+a.RadiusMm*Math.Cos(t), a.Plane.OriginMm.Y+a.RadiusMm*Math.Sin(t), 0);
                Cad04GFixture.Point(new(0,0,0), At(a.StartRadians)); Cad04GFixture.Point(new(2,0,0), At(a.StartRadians+a.SweepRadians));
            }
        }
        if (f.Key == "shared-style")
        {
            var definition = Assert.Single(d.Definitions); var templateId = Assert.Single(definition.ChildEntityIds);
            var template = d.Entities.Single(e => e.Id.Value == templateId.Value); var geometry = Assert.Single(d.Geometry);
            Assert.Equal(geometry.Id.Value, template.GeometryId!.Value); Assert.Equal(2, d.ModelSpace.Count);
            Assert.Equal(2, d.ModelSpace.Select(m => m.EntityId.Value).Distinct().Count());
            var leaves = d.ModelSpace.Select(m => d.Entities.Single(e => e.Id.Value == m.EntityId.Value)).ToArray();
            Assert.All(leaves, e => Assert.Equal(geometry.Id.Value, e.GeometryId!.Value));
            Assert.DoesNotContain(d.ModelSpace, m => m.EntityId.Value == template.Id.Value);
            var colors = leaves.Select(e => d.Styles.Single(s => s.Id.Value == e.StyleId!.Value).Color).ToArray();
            Assert.Equal(1, colors[0].Value!.Value); Assert.Equal(255, colors[1].Value!.Value);
            Assert.NotEqual(leaves[0].StyleId!.Value, leaves[1].StyleId!.Value);
        }
        if (f.Key is "nested" or "minsert" or "block-base")
        {
            Assert.Single(d.Geometry); var query = new CadBoundsQuery(d);
            if (f.Key == "nested")
            {
                Assert.Equal(new[] {1,2}, d.Occurrences.Select(o => o.Path.Steps.Count)); Assert.Single(d.ModelSpace);
                Assert.Equal(d.Occurrences[0].Id.Value, d.Occurrences[1].ParentOccurrenceId!.Value);
                Cad04GFixture.Point(new(0,0,0), query.Model().Bounds!.Min); Cad04GFixture.Point(new(0,4,0), query.Model().Bounds!.Max);
                Assert.Equal(1, d.Styles.Single(s => s.Id.Value == d.Entities.Single(e => e.Id.Value == d.ModelSpace[0].EntityId.Value).StyleId!.Value).Color.Value!.Value);
            }
            if (f.Key == "minsert")
            {
                Assert.Equal(6, d.Occurrences.Count); Assert.Equal(6, d.ModelSpace.Count);
                for (var i=0; i<6; i++)
                {
                    var o=d.Occurrences[i]; var step=o.Path.Steps[^1]; Assert.Equal(i/3, step.Row); Assert.Equal(i%3, step.Column);
                    // R90*(column*10,row*20): spacing is outside own scale(2,3).
                    Cad04GFixture.Point(new(-(i/3)*20,(i%3)*10,0), o.AccumulatedTransform.ToTransform3().Apply(new(0,0,0)));
                }
                Cad04GFixture.Point(new(-20,0,0), query.Model().Bounds!.Min); Cad04GFixture.Point(new(0,24,0), query.Model().Bounds!.Max);
            }
            if (f.Key == "block-base")
            {
                var o=Assert.Single(d.Occurrences); Cad04GFixture.Point(new(0,0,0), o.LocalTransform.ToTransform3().Apply(new(3,4,0)));
                Cad04GFixture.Point(new(0,0,0), query.Model().Bounds!.Min); Cad04GFixture.Point(new(0,2,0), query.Model().Bounds!.Max);
            }
        }
        if (f.Key == "bulge-closed-tilted")
        {
            var p=Assert.Single(d.Geometry.OfType<CadPolyline>()); Assert.True(p.Closed); Assert.Equal(3,p.SegmentIds.Count);
            var close=Assert.IsType<CadLine>(d.Geometry.Single(g=>g.Id.Value==p.SegmentIds[^1].Value));
            Assert.Equal(new Point3(-2,0,2),close.StartMm); Assert.Equal(new Point3(0,0,0),close.EndMm);
        }
        if (f.Key.StartsWith("affine-", StringComparison.Ordinal))
        {
            var circle=Assert.Single(d.Geometry.OfType<CadCircle>()); var arc=Assert.Single(d.Geometry.OfType<CadArc>());
            Assert.Equal(2,circle.RadiusMm); Assert.Equal(Math.PI/2,arc.SweepRadians);
            var o=Assert.Single(d.Occurrences); var q=new CadBoundsQuery(d);
            var full=q.GeometryTransformed(circle.Id,o.AccumulatedTransform).Bounds!;
            Cad04GFixture.Point(new(10-Math.Sqrt(26),-4-Math.Sqrt(26),3),full.Min);
            Cad04GFixture.Point(new(10+Math.Sqrt(26),-4+Math.Sqrt(26),3),full.Max);
            var partial=q.GeometryTransformed(arc.Id,o.AccumulatedTransform).Bounds!;
            if(f.Key=="affine-positive")
            {
                Cad04GFixture.Point(new(10-3*Math.Sqrt(2),-4+2*Math.Sqrt(2),3),partial.Min);
                Cad04GFixture.Point(new(10+2*Math.Sqrt(2),-4+Math.Sqrt(26),3),partial.Max);
            }
            else
            {
                Cad04GFixture.Point(new(10-Math.Sqrt(26),-4-2*Math.Sqrt(2),3),partial.Min);
                Cad04GFixture.Point(new(10-2*Math.Sqrt(2),-4+3*Math.Sqrt(2),3),partial.Max);
            }
        }
        if (f.Key == "bulge-degenerate") { Assert.Empty(d.Geometry); Assert.Contains(d.Ledger,l=>l.State==CadNormalizationState.InvalidNormalization); }
        if (f.Key == "missing-catalog") { Assert.Single(d.Geometry); Assert.Equal(CadNormalizationState.PartiallyNormalized,d.Completeness.State); }
        if (f.Key == "cycle") Assert.Contains(d.Diagnostics,x=>x.Code.Contains("Cycle",StringComparison.Ordinal));
        if (f.Key.StartsWith("legacy-", StringComparison.Ordinal)) OfficialOracle(raw,d);
    }
    private static Point3 Unit(Point3 v) { var n=Math.Sqrt(v.X*v.X+v.Y*v.Y+v.Z*v.Z); return new(v.X/n,v.Y/n,v.Z/n); }
    private static Point3 Cross(Point3 a,Point3 b)=>new(a.Y*b.Z-a.Z*b.Y,a.Z*b.X-a.X*b.Z,a.X*b.Y-a.Y*b.X);
    private static void OfficialOracle(RawDrawingSnapshot raw,CadDocument d)
    {
        Assert.Equal(333,raw.Records.Count); Assert.Equal(8737,raw.LexicalEvidence.Pairs.Count);
        Assert.Equal(5535,d.Provenance.Facts.Count); Assert.Equal(12,d.Layers.Count); Assert.Equal(201,d.Styles.Count);
        Assert.Equal(12,d.Definitions.Count); Assert.Equal(17,d.Occurrences.Count); Assert.Equal(184,d.Entities.Count);
        Assert.Equal(123,d.ModelSpace.Count); Assert.Equal(149,d.Geometry.Count); Assert.Equal(44,d.Entities.Count(e=>e.Annotation is not null));
        Assert.Equal(117,d.Diagnostics.Count); Assert.Equal(59,d.RawDiagnostics.Count);
        Assert.Equal(new[]{117,65,52,99},new[]{CadNormalizationState.Normalized,CadNormalizationState.PartiallyNormalized,CadNormalizationState.UnsupportedNormalization,CadNormalizationState.NotApplicable}.Select(s=>d.Ledger.Count(l=>l.State==s)));
        var unsupported=raw.Records.Where(r=>r.SemanticDisposition==DxfDisposition.Unsupported).ToArray(); Assert.Equal(52,unsupported.Length);
        Assert.Single(unsupported,r=>r.SourceRecord.RecordType=="VPORT"); Assert.Equal(3,unsupported.Count(r=>r.SourceRecord.RecordType=="APPID")); Assert.Equal(48,unsupported.Count(r=>r.SourceSection=="OBJECTS"));
        foreach(var r in unsupported) Assert.Equal(CadNormalizationState.UnsupportedNormalization,d.Ledger[r.StructuralRecordIndex].State);
        Assert.Equal(89,raw.Geometry.Count(g=>g.Payload is not null));
        Assert.Equal(60,raw.Geometry.Select(g=>g.Payload).OfType<RawLwPolyline>().Sum(p=>p.Vertices.Count-((p.Flags.Integer.GetValueOrDefault()&1)==0?1:0)));
        Assert.Equal(raw.Records.Sum(r=>1+r.SourceRecord.Fields.Count+(r.Geometry?.Fields.Count??0)+(r.BlockAnnotation?.Fields.Count??0))+158*8+33*8,d.Provenance.Facts.Count);
        var line=Assert.Single(d.Geometry.OfType<CadLine>(),g=>d.Provenance.Resolve(g.Source).CanonicalHandle=="9C"); Assert.Equal(new Point3(900,0,0),line.EndMm);
        var arc=Assert.Single(d.Geometry.OfType<CadArc>(),g=>d.Provenance.Resolve(g.Source).CanonicalHandle=="9D"); Assert.Equal(900,arc.RadiusMm); Cad04GFixture.Near(Math.PI/2,arc.SweepRadians);
        var occurrence=Assert.Single(d.Occurrences,o=>d.Provenance.Resolve(o.InsertSource).CanonicalHandle=="C3");
        var template=Assert.Single(d.Entities,e=>e.GeometryId?.Value==line.Id.Value&&d.Definitions.Any(b=>b.ChildEntityIds.Any(c=>c.Value==e.Id.Value)));
        var leaf=Assert.Single(d.Entities,e=>e.Id.Value==CadAssemblyIdentity.Leaf(template.Id,occurrence.Id).Value);
        var style=Assert.Single(d.Styles,s=>s.Id.Value==leaf.StyleId!.Value); Assert.Equal("A-DOOR",style.Layer.Value); Assert.Equal(3,style.Color.Value!.Value); Assert.Equal("Continuous",style.Linetype.Value);
        var bounds=new CadBoundsQuery(d).Model(); Assert.Equal(CadBoundsState.Partial,bounds.State);
        Assert.Equal(new Point3(-1300.225,-10100,0),bounds.Bounds!.Min); Assert.Equal(new Point3(20700,8500,0),bounds.Bounds.Max);
        // Direct retained byte-pair witnesses, independent of bounds implementation and HEADER extents.
        foreach(var (pair,value) in new[]{(3954,"-1300.225"),(4178,"20700.0"),(4525,"-10100.0"),(4896,"8500.0")})
            Assert.Equal(value,raw.LexicalEvidence.Pairs[pair].RawTextValue);
        Assert.Equal(CadNormalizationState.PartiallyNormalized,d.Completeness.State);
    }
}

public sealed class Cad04HCorpusTests
{
    public static IEnumerable<object[]> Cases()=>Cad04HCorpus.Cases.Select(f=>new object[]{f.Key});
    [Theory,MemberData(nameof(Cases))]
    public async Task PhysicalCorpusHasReviewedMathImmutableRawAndTripleRoundtrip(string key)
    {
        var f=Cad04HCorpus.Cases.Single(f=>f.Key==key); var (raw,r)=await Cad04HCorpus.Read(f);
        if(!f.CadGolden) { Assert.Null(r.Document); Assert.Equal(f.ExpectedState,r.Status.ToString()); return; }
        var d=Cad04GFixture.Document(r); Cad04HCorpus.SourceAndMathOracle(f,raw,d); Cad04HCorpus.Triple(d);
        Assert.Equal(f.Key.StartsWith("legacy-",StringComparison.Ordinal)?"PartiallyNormalized":f.ExpectedState,r.Status.ToString());
    }
    [Fact]
    public async Task CaptureCandidatesOnlyAfterOraclesRoundtripAndCultureEquality()
    {
        var prior=CultureInfo.CurrentCulture; var priorUi=CultureInfo.CurrentUICulture;
        try
        {
            foreach(var f in Cad04HCorpus.Cases.Where(f=>f.CadGolden))
            {
                byte[]? first=null; string? firstSummary=null;
                foreach(var culture in new[]{"","fr-FR","ko-KR"})
                {
                    CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo(culture); CultureInfo.CurrentUICulture=CultureInfo.GetCultureInfo(culture);
                    var (raw,r)=await Cad04HCorpus.Read(f);var d=Cad04GFixture.Document(r);
                    Cad04HCorpus.SourceAndMathOracle(f,raw,d); var bytes=Cad04HCorpus.Triple(d); var summary=Cad04HCorpus.Summary(d,bytes);
                    if(first is null){first=bytes;firstSummary=summary;}else{Assert.Equal(first,bytes);Assert.Equal(firstSummary,summary);}
                    if(f.Key.StartsWith("legacy-",StringComparison.Ordinal)){Assert.Equal(2233701,bytes.Length);Assert.Equal(Cad04HCorpus.ControlledHash,Cad04HCorpus.Hash(bytes));}
                    var output=Environment.GetEnvironmentVariable("DTOB_04H_EVIDENCE_DIRECTORY");
                    if(output is null)continue;
                    Assert.StartsWith(Path.GetFullPath(Cad04HCorpus.PathOf("artifacts"))+Path.DirectorySeparatorChar,Path.GetFullPath(output),StringComparison.OrdinalIgnoreCase);
                    var dir=Path.Combine(output,"candidates",culture.Length==0?"invariant":culture);Directory.CreateDirectory(dir);
                    File.WriteAllBytes(Path.Combine(dir,f.Key+".json"),bytes);File.WriteAllText(Path.Combine(dir,f.Key+".summary.json"),summary,new UTF8Encoding(false));
                }
            }
        }
        finally{CultureInfo.CurrentCulture=prior;CultureInfo.CurrentUICulture=priorUi;}
    }
}
