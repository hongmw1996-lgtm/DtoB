using DtoB.Cad.Normalize.Assembly;
using DtoB.Cad.V2;
using DtoB.Geometry;
using Xunit;

namespace DtoB.Tests;

public sealed class Cad04GBulgeTests
{
    [Theory][InlineData(0)][InlineData(1)][InlineData(-1)][InlineData(2)][InlineData(-2)][InlineData(0.5)][InlineData(-0.5)][InlineData(1e-6)]
    public void IndependentChordOraclesPreserveSignedSweepAndBothEndpoints(double b)
    {
        var g=CadBulgeGeometry.Create(CadV2ContractFixture.Id("segment"),new(0),[],new(0,0,0),new(2,0,0),new(1,0,0),new(0,1,0),b,Cad04DPlacementFixture.Policy);
        if(b==0){Assert.IsType<CadLine>(g);return;}
        var a=Assert.IsType<CadArc>(g);Cad04GFixture.Near((1/b-b)/2,a.Plane.OriginMm.Y);Assert.Equal(1,a.Plane.OriginMm.X);
        Cad04GFixture.Near((Math.Abs(b)+1/Math.Abs(b))/2,a.RadiusMm);Assert.Equal(4*Math.Atan(b),a.SweepRadians);
        if(Math.Abs(b)>=0.5)
        {
            Point3 At(double t)=>new(a.Plane.OriginMm.X+a.RadiusMm*Math.Cos(t),a.Plane.OriginMm.Y+a.RadiusMm*Math.Sin(t),0);
            Cad04GFixture.Point(new(0,0,0),At(a.StartRadians));Cad04GFixture.Point(new(2,0,0),At(a.StartRadians+a.SweepRadians));
        }
    }
    [Fact] public void CoincidentChordAndNonfiniteBulgeAreRefused()
    {
        CadGeometry Run(double b)=>CadBulgeGeometry.Create(CadV2ContractFixture.Id("bad"),new(0),[],new(1,1,0),new(1,1,0),new(1,0,0),new(0,1,0),b,Cad04DPlacementFixture.Policy);
        Assert.Throws<InvalidDataException>(()=>Run(1));Assert.Throws<InvalidDataException>(()=>Run(double.PositiveInfinity));
    }
    [Fact] public void ExtremelyTinyBulgeCannotBecomeAnInaccurateArcOrFabricatedLine()
    {
        Assert.Throws<InvalidDataException>(()=>CadBulgeGeometry.Create(CadV2ContractFixture.Id("tiny"),new(0),[],new(0,0,0),new(2,0,0),new(1,0,0),new(0,1,0),1e-300,Cad04DPlacementFixture.Policy));
    }
    [Fact] public async Task ClosedPolylineEmitsExactlyOneClosingSegmentAndRetainsWidthsAsDeferred()
    {
        var d=await Cad04GFixture.Root(Cad04GFixture.Entity("LWPOLYLINE","90\n3\n70\n1\n43\n2\n10\n0\n20\n0\n42\n1\n10\n2\n20\n0\n10\n2\n20\n2\n"));
        var p=Assert.Single(d.Geometry.OfType<CadPolyline>());Assert.True(p.Closed);Assert.Equal(3,p.SegmentIds.Count);Assert.Equal(4,d.Geometry.Count);
        var a=Assert.IsType<CadArc>(d.Geometry.Single(g=>g.Id.Value==p.SegmentIds[0].Value));Assert.Equal(Math.PI,a.SweepRadians);
        var closing=Assert.IsType<CadLine>(d.Geometry.Single(g=>g.Id.Value==p.SegmentIds[2].Value));Assert.Equal(new Point3(2,2,0),closing.StartMm);Assert.Equal(new Point3(0,0,0),closing.EndMm);
        Assert.Contains(d.Diagnostics,x=>x.Code=="CAD_POLYLINE_WIDTH_DEFERRED");
    }
}

public sealed class Cad04GPrimitiveTests
{
    [Fact] public async Task PointStaysWcsAndSolidUsesBoundaryOneTwoFourThree()
    {
        var point=await Cad04GFixture.Root(Cad04GFixture.Entity("POINT","10\n2\n20\n3\n30\n4\n210\n0\n220\n1\n230\n0\n"));
        Assert.Equal(new Point3(2,3,4),Assert.Single(point.Geometry.OfType<CadPoint>()).PositionMm);
        var d=await Cad04GFixture.Root(Cad04GFixture.Entity("SOLID","10\n0\n20\n0\n30\n0\n11\n2\n21\n0\n31\n0\n12\n0\n22\n3\n32\n0\n13\n2\n23\n3\n33\n0\n"));
        Assert.Equal(new[]{new Point3(0,0,0),new(2,0,0),new(2,3,0),new(0,3,0)},Assert.Single(d.Geometry.OfType<CadSolid>()).BoundaryMm);
    }
    [Fact] public async Task TiltedCircleUses04CPlaneAnd04BRadius()
    {
        var d=await Cad04GFixture.Root(Cad04GFixture.Entity("CIRCLE","10\n2\n20\n3\n30\n4\n40\n5\n210\n0\n220\n1\n230\n0\n"));
        var c=Assert.Single(d.Geometry.OfType<CadCircle>());Assert.Equal(new Point3(-2,4,3),c.Plane.OriginMm);Assert.Equal(new Point3(-1,0,0),c.Plane.AxisU);Assert.Equal(new Point3(0,0,1),c.Plane.AxisV);Assert.Equal(5,c.RadiusMm);
    }
    [Fact] public async Task TextAnnotationRetainsFactsWithAnchorOnlyBounds()
    {
        var d=await Cad04GFixture.Root(Cad04GFixture.Entity("TEXT","10\n2\n20\n3\n30\n0\n40\n5\n50\n90\n1\nhello\n"));
        var a=Assert.Single(d.Entities).Annotation!;Assert.Equal("hello",a.Content);Assert.Equal(5,a.HeightMm);Assert.Equal(Math.PI/2,a.RotationRadians);Assert.Null(a.SourceMeasurementMm);
        Assert.Equal(CadBoundsState.AnchorOnly,new CadBoundsQuery(d).Model().State);
    }
    [Fact] public async Task NonplanarSolidWithholdsPayloadAndKeepsFailureLedger()
    {
        var d=await Cad04GFixture.Root(Cad04GFixture.Entity("SOLID","10\n0\n20\n0\n30\n0\n11\n2\n21\n0\n31\n0\n12\n0\n22\n3\n32\n0\n13\n2\n23\n3\n33\n1\n"));
        Assert.Empty(d.Geometry);Assert.Equal(CadNormalizationState.InvalidNormalization,Assert.Single(d.Entities).Disposition);Assert.Equal(CadBoundsState.Unavailable,new CadBoundsQuery(d).Model().State);
    }
}

public sealed class Cad04GAnalyticBoundsTests
{
    private static CadBoundsQuery Query(CadGeometry g)=>new(CadV2ContractFixture.Document([g]));
    private static CadId Id=>CadV2ContractFixture.Id("curve");
    private static CadPlane Plane=>new(new(0,0,0),new(1,0,0),new(0,1,0));
    [Fact] public void UniformCircleBoundsAreAnalytic()
    {
        var c=new CadCircle(Id,new(0),[],Plane,2);var t=CadTransform.FromTransform3(Transform3.Placement(new(1,2,3),0,new(3,3,3)));
        var b=Query(c).GeometryTransformed(Id,t).Bounds!;Assert.Equal(new Point3(-5,-4,3),b.Min);Assert.Equal(new Point3(7,8,3),b.Max);
    }
    [Theory][InlineData(1)][InlineData(-1)] public void NearFullSignedArcIncludesEveryStationaryExtremum(int sign)
    {
        var a=new CadArc(Id,new(0),[],Plane,1,0,sign*Math.BitDecrement(Math.Tau));var b=Query(a).GeometryLocal(Id).Bounds!;
        Cad04GFixture.Point(new(-1,-1,0),b.Min);Cad04GFixture.Point(new(1,1,0),b.Max);
    }
    [Theory][InlineData(1)][InlineData(-1)]
    public void FullNonuniformRotatedCircleUsesSqrt26Amplitude(int mirror)
    {
        var c=new CadCircle(Id,new(0),[],Plane,2);
        var t=CadTransform.FromTransform3(Transform3.Placement(new(10,-4,3),Math.PI/4,new(2*mirror,3,1)));
        var result=Query(c).GeometryTransformed(Id,t);Assert.Equal(CadBoundsState.Exact,result.State);var b=result.Bounds!;
        Cad04GFixture.Point(new(10-Math.Sqrt(26),-4-Math.Sqrt(26),3),b.Min);Cad04GFixture.Point(new(10+Math.Sqrt(26),-4+Math.Sqrt(26),3),b.Max);
    }
    [Fact] public void PartialNonuniformArcUsesInteriorYExtremumWithoutFullCircleShortcut()
    {
        var a=new CadArc(Id,new(0),[],Plane,2,0,Math.PI/2);var t=CadTransform.FromTransform3(Transform3.Placement(new(10,-4,3),Math.PI/4,new(2,3,1)));
        var b=Query(a).GeometryTransformed(Id,t).Bounds!;
        Cad04GFixture.Point(new(10-3*Math.Sqrt(2),-4+2*Math.Sqrt(2),3),b.Min);Cad04GFixture.Point(new(10+2*Math.Sqrt(2),-4+Math.Sqrt(26),3),b.Max);
    }
    [Theory][InlineData(0,0.2)][InlineData(0.3,-0.2)]
    public void ArcWithoutStationaryExtremaUsesEndpoints(double start,double sweep)
    {
        var a=new CadArc(Id,new(0),[],Plane,1,start,sweep);var b=Query(a).GeometryLocal(Id).Bounds!;
        Cad04GFixture.Near(Math.Min(Math.Cos(start),Math.Cos(start+sweep)),b.Min.X);Cad04GFixture.Near(Math.Max(Math.Sin(start),Math.Sin(start+sweep)),b.Max.Y);
    }
    [Fact] public void WrappedArcIncludesZeroAndMirroredParameterRemainsSigned()
    {
        var a=new CadArc(Id,new(0),[],Plane,2,Math.PI*1.5,Math.PI);var t=CadTransform.FromTransform3(Transform3.Placement(new(0,0,0),0,new(-1,1,1)));
        var b=Query(a).GeometryTransformed(Id,t).Bounds!;Cad04GFixture.Point(new(-2,-2,0),b.Min);Cad04GFixture.Point(new(0,2,0),b.Max);
    }
    [Fact] public void TiltedCircleAndShearHaveAnalyticThreeDimensionalBounds()
    {
        var c=new CadCircle(Id,new(0),[],new(new(1,2,3),new(1,0,0),new(0,0,1)),2);
        var t=new CadTransform(new double[]{1,0,2,0,0,1,0,0,0,0,3,0,0,0,0,1});var b=Query(c).GeometryTransformed(Id,t).Bounds!;
        Cad04GFixture.Point(new(7-2*Math.Sqrt(5),2,3),b.Min);Cad04GFixture.Point(new(7+2*Math.Sqrt(5),2,15),b.Max);
    }
    [Fact] public async Task SharedGeometryOccurrenceQueriesUseDifferentWorldTransforms()
    {
        var f=await Cad04FStyleFixture.Run(Cad04EOccurrenceFixture.Insert("A","B",new(10,0,0),extra:"8\n0\n")+Cad04EOccurrenceFixture.Insert("D","B",new(20,0,0),extra:"8\n0\n"),Cad04FStyleFixture.Block(Cad04GFixture.Line()));
        var d=Cad04GFixture.Document(Cad04GFixture.Normalize(f.Snapshot));var q=new CadBoundsQuery(d);
        Assert.Equal(0,q.Definition(d.Definitions[0].Id).Bounds!.Min.X);Assert.Equal(10,q.Occurrence(d.Occurrences[0].Id).Bounds!.Min.X);Assert.Equal(20,q.Occurrence(d.Occurrences[1].Id).Bounds!.Min.X);
        Assert.Equal(21,q.Model().Bounds!.Max.X);Assert.Single(d.Geometry);
    }
}
