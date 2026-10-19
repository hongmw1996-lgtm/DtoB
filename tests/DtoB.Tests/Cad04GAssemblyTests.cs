using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using DtoB.Cad.Normalize;
using DtoB.Cad.Normalize.Assembly;
using DtoB.Cad.Raw;
using DtoB.Cad.V2;
using DtoB.Geometry;
using Xunit;
using V2Document=DtoB.Cad.V2.CadDocument;

namespace DtoB.Tests;

internal static class Cad04GFixture
{
    internal static CadNormalizationResult Normalize(RawDrawingSnapshot raw,int budget=250000,int diagnostics=5000,CancellationToken token=default)=>
        new CadNormalizer(Cad04DPlacementFixture.Policy).Normalize(raw,new(Cad04DPlacementFixture.Policy.ProfileId,new(32,10000,100000,budget,diagnostics)),token);
    internal static V2Document Document(CadNormalizationResult r)
    {Assert.True(r.Document is not null,string.Join(",",r.Diagnostics.Select(d=>d.Code)));return r.Document!;}
    internal static string Line(string handle="E",string layer="0",int? color=null)=>Cad04FStyleFixture.Line(handle,layer,color,extra:"").Replace("11\n1\n21\n1\n","30\n0\n11\n1\n21\n1\n31\n0\n",StringComparison.Ordinal);
    internal static string Entity(string type,string fields,string handle="E")=>$"0\n{type}\n5\n{handle}\n330\n0\n8\n0\n"+fields;
    internal static async Task<V2Document> Root(string entity)
    {var f=await Cad04FStyleFixture.Run(entity);return Document(Normalize(f.Snapshot));}
    internal static void Near(double expected,double actual)=>Assert.True(Math.Abs(expected-actual)<=1e-8,$"Expected {expected:R}; actual {actual:R}");
    internal static void Point(Point3 expected,Point3 actual){Near(expected.X,actual.X);Near(expected.Y,actual.Y);Near(expected.Z,actual.Z);}
}

public sealed class Cad04GAssemblyTests
{
    [Fact] public async Task RootLineHasOneSourceEntityAndNoProjectedDuplicate()
    {
        var d=await Cad04GFixture.Root(Cad04GFixture.Line());var e=Assert.Single(d.Entities);Assert.Single(d.Geometry);var m=Assert.Single(d.ModelSpace);
        Assert.Equal(e.Id.Value,m.EntityId.Value);Assert.Null(m.OccurrenceId);Assert.Empty(d.Definitions);
        var g=Assert.IsType<CadLine>(d.Geometry[0]);Assert.Equal(new Point3(0,0,0),g.StartMm);Assert.Equal(new Point3(1,1,0),g.EndMm);
        Assert.Equal("E",d.Provenance.Resolve(e.Source).CanonicalHandle);Assert.Null(d.Units.Evidence);Assert.Equal(1,d.Units.SourceToMillimeters);
        Assert.Equal(d.Provenance.Records.Count,d.Ledger.Count);d.ValidateStructure();
    }
    [Fact] public async Task SharedSourceGeometryHasSeparateRedBlueLeavesAndDeclaredTemplate()
    {
        var child=Cad04GFixture.Line(color:0);
        var f=await Cad04FStyleFixture.Run(Cad04FStyleFixture.Insert("A",color:1)+Cad04FStyleFixture.Insert("D",layer:"C-INST",color:5),Cad04FStyleFixture.Block(child));
        var before=System.Text.Json.JsonSerializer.Serialize(f.Snapshot);var d=Cad04GFixture.Document(Cad04GFixture.Normalize(f.Snapshot));
        var definition=Assert.Single(d.Definitions);var template=Assert.Single(d.Entities,e=>e.Id.Value==Assert.Single(definition.ChildEntityIds).Value);
        var g=Assert.Single(d.Geometry);Assert.Equal(g.Id.Value,template.GeometryId!.Value);
        var declared=Assert.Single(d.Styles,s=>s.Id.Value==template.StyleId!.Value);Assert.Equal(CadPropertyMode.ByBlock,declared.Color.SourceMode);Assert.Null(declared.Color.Value);
        Assert.Equal(2,d.ModelSpace.Count);var colors=new List<int>();
        foreach(var m in d.ModelSpace)
        {
            var leaf=Assert.Single(d.Entities,e=>e.Id.Value==m.EntityId.Value);Assert.Equal(template.GeometryId.Value,leaf.GeometryId!.Value);
            Assert.Equal(CadAssemblyIdentity.Leaf(template.Id,m.OccurrenceId!).Value,leaf.Id.Value);Assert.NotEqual(template.Id.Value,leaf.Id.Value);
            Assert.Equal(template.Source.Index,leaf.Source.Index);colors.Add(Assert.Single(d.Styles,s=>s.Id.Value==leaf.StyleId!.Value).Color.Value!.Value);
        }
        Assert.Equal(new[]{1,5},colors);Assert.Equal(before,System.Text.Json.JsonSerializer.Serialize(f.Snapshot));
        var bytes=CadIrJson.SerializeUtf8(d);Assert.Equal(bytes,CadIrJson.SerializeUtf8(CadIrJson.Deserialize(Encoding.UTF8.GetString(bytes))));
    }
    [Fact] public async Task OfficialControlledSourceProducesStrictPartialDocumentAndSourceLinkedPrimitives()
    {
        var raw=await Cad04BScalarFixture.Controlled();var rawBefore=System.Text.Json.JsonSerializer.Serialize(raw);
        var d=Cad04GFixture.Document(Cad04GFixture.Normalize(raw));Assert.Equal(rawBefore,System.Text.Json.JsonSerializer.Serialize(raw));
        Assert.Equal("2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC",d.Provenance.Source.FileHash);
        Assert.Equal(CadNormalizationState.PartiallyNormalized,d.Completeness.State);Assert.Equal(333,d.Ledger.Count);
        Assert.Equal(52,raw.Records.Count(r=>r.SemanticDisposition==DxfDisposition.Unsupported));
        foreach(var r in raw.Records.Where(r=>r.SemanticDisposition==DxfDisposition.Unsupported))Assert.Equal(CadNormalizationState.UnsupportedNormalization,d.Ledger[r.StructuralRecordIndex].State);
        var line=Assert.Single(d.Geometry.OfType<CadLine>(),g=>d.Provenance.Resolve(g.Source).CanonicalHandle=="9C");
        Assert.Equal(new Point3(0,0,0),line.StartMm);Assert.Equal(new Point3(900,0,0),line.EndMm);
        var arc=Assert.Single(d.Geometry.OfType<CadArc>(),g=>d.Provenance.Resolve(g.Source).CanonicalHandle=="9D");
        Assert.Equal(new Point3(0,0,0),arc.Plane.OriginMm);Assert.Equal(900,arc.RadiusMm);Assert.Equal(0,arc.StartRadians);Cad04GFixture.Near(Math.PI/2,arc.SweepRadians);
        Assert.Equal(17,d.Occurrences.Count);Assert.Equal(12,d.Definitions.Count);Assert.Equal(12,d.Layers.Count);
        Assert.Equal(123,d.ModelSpace.Count); //97 real non-INSERT roots +26 source-child projections, independently inventoried in04F.
        // Independent input inventories:89 primitive sources,44 annotation sources,25 INSERTs;61 polyline vertices with one open2-vertex polyline =>60 segments.
        Assert.Equal(89,raw.Geometry.Count(g=>g.Payload is not null));Assert.Equal(60,raw.Geometry.Select(g=>g.Payload).OfType<RawLwPolyline>().Sum(p=>p.Vertices.Count-((p.Flags.Integer.GetValueOrDefault()&1)==0?1:0)));
        Assert.Equal(149,d.Geometry.Count);Assert.Equal(158+26,d.Entities.Count);Assert.Equal(158+17+26,d.Styles.Count);Assert.Equal(44,d.Entities.Count(e=>e.Annotation is not null));
        var expectedFacts=raw.Records.Sum(r=>1+r.SourceRecord.Fields.Count+(r.Geometry?.Fields.Count??0)+(r.BlockAnnotation?.Fields.Count??0))+158*8+(12+21)*8;
        Assert.Equal(expectedFacts,d.Provenance.Facts.Count);Assert.Equal(5535,d.Provenance.Facts.Count);
        Assert.NotEmpty(d.Geometry.OfType<CadPolyline>());Assert.NotEmpty(d.Geometry.OfType<CadPoint>());Assert.NotEmpty(d.Geometry.OfType<CadSolid>());
        // Literal original pair values independently inspected: POINT E1 and SOLID7B (repeated3/4 triangle).
        var point=Assert.Single(d.Geometry.OfType<CadPoint>(),g=>d.Provenance.Resolve(g.Source).CanonicalHandle=="E1");Assert.Equal(new Point3(0,-1300,0),point.PositionMm);
        var solid=Assert.Single(d.Geometry.OfType<CadSolid>(),g=>d.Provenance.Resolve(g.Source).CanonicalHandle=="7B");
        Assert.Equal(new[]{new Point3(-1,0.16439898372402537,0),new(0,0,0),new(-1,-0.16439898372402537,0)},solid.BoundaryMm);
        var poly=Assert.Single(d.Geometry.OfType<CadPolyline>(),g=>d.Provenance.Resolve(g.Source).CanonicalHandle=="77");Assert.False(poly.Closed);
        var segment=Assert.IsType<CadLine>(d.Geometry.Single(g=>g.Id.Value==Assert.Single(poly.SegmentIds).Value));Assert.Equal(new Point3(-0.5,-0.5,0),segment.StartMm);Assert.Equal(new Point3(0.5,0.5,0),segment.EndMm);
        var template=Assert.Single(d.Entities,e=>e.Source.Index==line.Source.Index&&d.Definitions.Any(b=>b.ChildEntityIds.Any(c=>c.Value==e.Id.Value)));
        Assert.Equal(line.Id.Value,template.GeometryId!.Value);
        var occurrence=Assert.Single(d.Occurrences,o=>d.Provenance.Resolve(o.InsertSource).CanonicalHandle=="C3");
        var leaf=Assert.Single(d.Entities,e=>e.Id.Value==CadAssemblyIdentity.Leaf(template.Id,occurrence.Id).Value);
        Assert.Equal(line.Id.Value,leaf.GeometryId!.Value);var style=Assert.Single(d.Styles,s=>s.Id.Value==leaf.StyleId!.Value);
        Assert.Equal("A-DOOR",style.Layer.Value);Assert.Equal(3,style.Color.Value!.Value);Assert.Equal("Continuous",style.Linetype.Value);
        var bytes=CadIrJson.SerializeUtf8(d);Assert.Equal(bytes,CadIrJson.SerializeUtf8(CadIrJson.Deserialize(Encoding.UTF8.GetString(bytes))));
        var bounds=new CadBoundsQuery(d).Model();Assert.NotNull(bounds.Bounds);Assert.Equal(CadBoundsState.Partial,bounds.State);
        var dir=Environment.GetEnvironmentVariable("DTOB_04G_EVIDENCE_DIRECTORY");
        if(dir is not null)
        {
            Directory.CreateDirectory(dir);File.WriteAllBytes(Path.Combine(dir,"controlled-normalized-cad-document.json"),bytes);
            File.WriteAllText(Path.Combine(dir,"controlled-summary.json"),System.Text.Json.JsonSerializer.Serialize(new{Source=d.Provenance.Records.Count,Facts=d.Provenance.Facts.Count,
                Layers=d.Layers.Count,Styles=d.Styles.Count,Geometry=d.Geometry.Count,Entities=d.Entities.Count,Definitions=d.Definitions.Count,Occurrences=d.Occurrences.Count,ModelSpace=d.ModelSpace.Count,
                Annotations=d.Entities.Count(e=>e.Annotation is not null),Diagnostics=d.Diagnostics.Count,RawDiagnostics=d.RawDiagnostics.Count,Bounds=bounds,Sha256=Convert.ToHexString(SHA256.HashData(bytes))},new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));
        }
    }
    [Fact] public async Task PreCancelledNormalizationPublishesNothing()
    {var f=await Cad04FStyleFixture.Run(Cad04GFixture.Line());var r=Cad04GFixture.Normalize(f.Snapshot,token:new(true));Assert.Equal(CadNormalizationStatus.Cancelled,r.Status);Assert.Null(r.Document);}
    [Theory][InlineData(249999)][InlineData(250000)][InlineData(250001)]
    public void GlobalBudgetExactBoundaryDoesNotRefundOrReset(int requested)
    {var b=new CadMaterializationBudget(250000);var accepted=0;for(var i=0;i<requested;i++)if(b.TryReserve())accepted++;Assert.Equal(Math.Min(requested,250000),accepted);Assert.Equal(accepted,b.Allocated);if(requested>=250000)Assert.False(b.TryReserve());}
    [Fact] public async Task SegmentExplosionRefusesWholeDocumentInsteadOfPrefix()
    {
        var f=await Cad04FStyleFixture.Run(Cad04GFixture.Entity("LWPOLYLINE","90\n3\n70\n1\n10\n0\n20\n0\n10\n2\n20\n0\n10\n2\n20\n2\n"));
        Assert.NotNull(Cad04GFixture.Normalize(f.Snapshot,budget:4).Document);
        var r=Cad04GFixture.Normalize(f.Snapshot,budget:3);Assert.Equal(CadNormalizationStatus.ResourceLimitExceeded,r.Status);Assert.Null(r.Document);Assert.Contains(r.Diagnostics,x=>x.Code=="CAD_GEOMETRY_LIMIT");
    }
    [Fact] public async Task FailedGeometryReservationIsNotRefundedForLaterRoot()
    {
        var f=await Cad04FStyleFixture.Run(Cad04GFixture.Entity("LINE","10\n0\n20\n0\n30\n0\n11\n0\n21\n0\n31\n0\n")+Cad04GFixture.Line("F"));
        var r=Cad04GFixture.Normalize(f.Snapshot,budget:1);Assert.Null(r.Document);Assert.Equal(CadNormalizationStatus.ResourceLimitExceeded,r.Status);
        var d=Cad04GFixture.Document(Cad04GFixture.Normalize(f.Snapshot,budget:2));Assert.Single(d.Geometry);Assert.Contains(d.Ledger,x=>x.State==CadNormalizationState.InvalidNormalization);
    }
    [Fact] public async Task DiagnosticCapIncludesReservedTerminalSlot()
    {
        var f=await Cad04FStyleFixture.Run(Cad04GFixture.Entity("POINT","10\n1\n20\n2\n30\n3\n50\n20\n"));
        var r=Cad04GFixture.Normalize(f.Snapshot,diagnostics:1);Assert.Null(r.Document);Assert.Equal(CadNormalizationStatus.ResourceLimitExceeded,r.Status);Assert.Single(r.Diagnostics);
    }
    [Theory][InlineData(1e200,1)][InlineData(-1e-300,-1)][InlineData(double.Epsilon,1)]
    public void OrientationSignAvoidsMagnitudeOverflowAndUnderflow(double s,int expected)
    {var m=CadTransform.FromTransform3(Transform3.Placement(new(0,0,0),0,new(s,Math.Abs(s),1)));Assert.Equal(expected,CadOrientationValidation.Sign(m));}
}
