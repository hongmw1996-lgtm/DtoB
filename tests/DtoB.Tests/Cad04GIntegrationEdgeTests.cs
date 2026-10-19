using DtoB.Cad.Normalize;
using DtoB.Cad.Normalize.Assembly;
using DtoB.Cad.V2;
using DtoB.Geometry;
using Xunit;

namespace DtoB.Tests;

public sealed class Cad04GIntegrationEdgeTests
{
    [Fact] public async Task LeafEntriesConsumeGlobalBudgetWithoutCloningGeometry()
    {
        var f=await Cad04FStyleFixture.Run(Cad04FStyleFixture.Insert("A")+Cad04FStyleFixture.Insert("D"),Cad04FStyleFixture.Block(Cad04GFixture.Line()));
        var d=Cad04GFixture.Document(Cad04GFixture.Normalize(f.Snapshot,budget:3));Assert.Single(d.Geometry);Assert.Equal(2,d.ModelSpace.Count);
        var refused=Cad04GFixture.Normalize(f.Snapshot,budget:2);Assert.Equal(CadNormalizationStatus.ResourceLimitExceeded,refused.Status);Assert.Null(refused.Document);
    }
    [Fact] public async Task PaperSpaceRootsAndTheirOccurrenceLeavesStayOutOfModelSpace()
    {
        var f=await Cad04FStyleFixture.Run(Cad04GFixture.Line("F")+Cad04GFixture.Line("E").Replace("100\nAcDbEntity\n","100\nAcDbEntity\n67\n1\n",StringComparison.Ordinal)
            +Cad04FStyleFixture.Insert("A",extra:"67\n1\n"),Cad04FStyleFixture.Block(Cad04GFixture.Line("C")));
        var d=Cad04GFixture.Document(Cad04GFixture.Normalize(f.Snapshot));Assert.Empty(d.Occurrences);Assert.Single(d.ModelSpace);
        var e=Assert.Single(d.Entities,x=>x.Id.Value==d.ModelSpace[0].EntityId.Value);Assert.Equal("F",d.Provenance.Resolve(e.Source).CanonicalHandle);
        Assert.Contains(d.Diagnostics,x=>x.Code=="CAD_NON_MODEL_SPACE_RETAINED");Assert.Equal(3,d.Geometry.Count);
    }
    [Fact] public async Task NestedOccurrencesDoNotDuplicateInsertGeometryAndBoundsUseAccumulatedMatrix()
    {
        var blocks=Cad04FStyleFixture.Block(Cad04FStyleFixture.Insert("D",target:"C"),"B","B")+Cad04FStyleFixture.Block(Cad04GFixture.Line(),"C","C");
        var f=await Cad04FStyleFixture.Run(Cad04EOccurrenceFixture.Insert("A","B",new(100,10,0),angle:90,extra:"8\n0\n"),blocks);
        var d=Cad04GFixture.Document(Cad04GFixture.Normalize(f.Snapshot));Assert.Equal(2,d.Occurrences.Count);Assert.Single(d.ModelSpace);Assert.Single(d.Geometry);
        var q=new CadBoundsQuery(d);var bounds=q.Occurrence(d.Occurrences[0].Id).Bounds!;
        Cad04GFixture.Point(new(99,10,0),bounds.Min);Cad04GFixture.Point(new(100,11,0),bounds.Max);
        Assert.Equal(CadBoundsState.Unavailable,q.Definition(d.Definitions.Single(x=>x.Name=="B").Id).State);
    }
    [Fact] public async Task MTextAndDimensionExposeOnlyFactualAnnotationChannels()
    {
        var m=await Cad04GFixture.Root(Cad04GFixture.Entity("MTEXT","10\n2\n20\n3\n30\n4\n11\n1\n21\n0\n31\n0\n40\n2\n1\nhello\n"));
        var annotation=Assert.Single(m.Entities).Annotation!;Assert.Equal(CadAnnotationKind.MText,annotation.Kind);Assert.Equal("hello",annotation.Content);Assert.Equal(new Point3(2,3,4),Assert.Single(annotation.AnchorsMm));
        Assert.Null(annotation.RotationRadians);Assert.Contains(m.Diagnostics,x=>x.Code=="CAD_MTEXT_DIRECTION_DEFERRED");
        var d=await Cad04GFixture.Root(Cad04GFixture.Entity("DIMENSION","10\n0\n20\n0\n30\n0\n11\n2\n21\n3\n31\n0\n70\n0\n13\n0\n23\n0\n33\n0\n14\n4\n24\n0\n34\n0\n"));
        var dimension=Assert.Single(d.Entities).Annotation!;Assert.Equal(CadAnnotationKind.Dimension,dimension.Kind);Assert.Null(dimension.SourceMeasurementMm);Assert.Empty(d.Geometry);Assert.Empty(d.Occurrences);
    }
    [Fact] public async Task AttributeDefinitionsRemainExplicitUnsupportedSourceEntities()
    {
        var d=await Cad04GFixture.Root(Cad04GFixture.Entity("ATTDEF","10\n0\n20\n0\n30\n0\n40\n2\n1\nvalue\n2\nTAG\n70\n0\n"));
        Assert.Equal(CadEntityKind.Unsupported,Assert.Single(d.Entities).Kind);Assert.Empty(d.Geometry);Assert.Contains(d.Diagnostics,x=>x.Code=="CAD_ENTITY_UNSUPPORTED");
    }
    [Fact] public async Task SimplePointLineSolidAndPolylineBoundsHaveIndependentLiteralExtrema()
    {
        var p=await Cad04GFixture.Root(Cad04GFixture.Entity("POINT","10\n2\n20\n-3\n30\n4\n"));
        var pb=new CadBoundsQuery(p).GeometryLocal(p.Geometry[0].Id).Bounds!;Assert.Equal(new Point3(2,-3,4),pb.Min);Assert.Equal(pb.Min,pb.Max);
        var l=await Cad04GFixture.Root(Cad04GFixture.Line());var lb=new CadBoundsQuery(l).GeometryLocal(l.Geometry[0].Id).Bounds!;Assert.Equal(new Point3(0,0,0),lb.Min);Assert.Equal(new Point3(1,1,0),lb.Max);
        var poly=await Cad04GFixture.Root(Cad04GFixture.Entity("LWPOLYLINE","90\n3\n70\n1\n10\n-2\n20\n0\n10\n3\n20\n0\n10\n0\n20\n4\n"));
        var pl=new CadBoundsQuery(poly).GeometryLocal(poly.Geometry.OfType<CadPolyline>().Single().Id).Bounds!;Assert.Equal(new Point3(-2,0,0),pl.Min);Assert.Equal(new Point3(3,4,0),pl.Max);
        var solid=await Cad04GFixture.Root(Cad04GFixture.Entity("SOLID","10\n0\n20\n0\n30\n0\n11\n2\n21\n0\n31\n0\n12\n0\n22\n3\n32\n0\n13\n0\n23\n3\n33\n0\n"));
        var sb=new CadBoundsQuery(solid).GeometryLocal(solid.Geometry[0].Id).Bounds!;Assert.Equal(new Point3(0,0,0),sb.Min);Assert.Equal(new Point3(2,3,0),sb.Max);
    }
    [Fact] public async Task InvalidUnitsRefuseAndMissingStylesDoNotEraseValidGeometry()
    {
        var unitless=await Cad04BScalarFixture.Snapshot(Cad04BScalarFixture.Dxf(Cad04GFixture.Line(),units:"70\n0\n"));
        var r=Cad04GFixture.Normalize(unitless);Assert.Equal(CadNormalizationStatus.InvalidNormalization,r.Status);Assert.Null(r.Document);
        var noCatalog=await Cad04BScalarFixture.Snapshot(Cad04BScalarFixture.Dxf(Cad04GFixture.Line()));
        var d=Cad04GFixture.Document(Cad04GFixture.Normalize(noCatalog));Assert.Single(d.Geometry);Assert.Equal(CadNormalizationState.PartiallyNormalized,d.Completeness.State);
        Assert.Contains(d.Styles,s=>s.Color.State==CadResolutionState.Unsupported);
    }
    [Fact] public async Task BoundsCancellationDoesNotPublishPartialQuery()
    {var d=await Cad04GFixture.Root(Cad04GFixture.Line());Assert.Throws<OperationCanceledException>(()=>new CadBoundsQuery(d).Model(new(true)));}
    [Fact] public async Task AmbiguousLayersAreAllWithheldInsteadOfFirstWins()
    {
        var f=await Cad04FStyleFixture.Run(Cad04GFixture.Line(),layers:[Cad04FStyleFixture.Layer("0",1),Cad04FStyleFixture.Layer("0",5)]);
        var d=Cad04GFixture.Document(Cad04GFixture.Normalize(f.Snapshot));Assert.Empty(d.Layers);Assert.Single(d.Geometry);
        Assert.Equal(2,d.Diagnostics.Count(x=>x.Code=="CAD_LAYER_CATALOG_REJECTED"));
    }
    [Fact] public async Task NumericProfileMismatchRefusesUntraceablePolicy()
    {
        var f=await Cad04FStyleFixture.Run(Cad04GFixture.Line());
        var r=new CadNormalizer(Cad04DPlacementFixture.Policy).Normalize(f.Snapshot,new("different",CadNormalizationLimits.Development));
        Assert.Null(r.Document);Assert.Equal("CAD_NUMERIC_PROFILE_MISMATCH",Assert.Single(r.Diagnostics).Code);
    }
    [Fact] public async Task ConcurrentCancellationRefusesWholeComposition()
    {
        var f=await Cad04FStyleFixture.Run(Cad04EOccurrenceFixture.Insert("A","B",rows:100,columns:100,extra:"8\n0\n"),Cad04FStyleFixture.Block(Cad04GFixture.Line()));
        using var cts=new CancellationTokenSource();var worker=Task.Run(()=>Cad04GFixture.Normalize(f.Snapshot,token:cts.Token));
        cts.CancelAfter(1);var r=await worker;Assert.Equal(CadNormalizationStatus.Cancelled,r.Status);Assert.Null(r.Document);
        // Scheduling is deliberately not a throughput/timing oracle; entry and in-loop checks enforce no publication.
    }
}
