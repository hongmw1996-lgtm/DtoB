using DtoB.Cad.Normalize.Placement;
using DtoB.Geometry;
using Xunit;
using static DtoB.Tests.Cad04DPlacementFixture;

namespace DtoB.Tests;

public sealed class CadInsertLocalTransformTests
{
    [Fact] public async Task IdentityHasIndependentSixteenCoefficientOracle()
    {
        var p=Placement(await Snapshot());
        Assert.Equal(new double[]{1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1},p.Transform.Values);
        ExpectedPoint(p,new(1,2,3),new(1,2,3)); ExpectedVector(p,new(1,2,3),new(1,2,3));
    }
    [Fact] public async Task TranslationAppliesToPointAndNotVector()
    {
        var p=Placement(await Snapshot(insertion:new(100,200,300)));
        ExpectedPoint(p,new(1,2,3),new(101,202,303)); ExpectedVector(p,new(1,2,3),new(1,2,3));
    }
    [Fact] public async Task NonzeroThreeDimensionalBaseIsSubtractedFirst()
    {
        var p=Placement(await Snapshot(new(10,20,30),new(100,200,300)));
        ExpectedPoint(p,new(10,20,30),new(100,200,300)); ExpectedPoint(p,new(11,22,33),new(101,202,303));
    }
    [Theory][InlineData(0,1,0)][InlineData(90,0,1)][InlineData(180,-1,0)][InlineData(270,0,-1)]
    public async Task PositiveCanonicalRotationIsLocalCcw(double degrees,double x,double y)
    {
        var p=Placement(await Snapshot(rotation:degrees)); ExpectedPoint(p,new(1,0,0),new(x,y,0));
        Assert.Equal(degrees==0?0:degrees==90?Math.PI/2:degrees==180?Math.PI:3*Math.PI/2,p.Rotation.Value);
    }
    [Fact] public async Task CompleteHistoricalMatrixIsHandDerived()
    {
        var p=Placement(await Snapshot(new(100,50,0),new(1000,2000,0),new(2,3,1),90));
        // R*S = [0,-3,0;2,0,0;0,0,1], t=i-(R*S)*b=(1150,1800,0).
        var expected=new double[]{0,-3,0,1150,2,0,0,1800,0,0,1,0,0,0,0,1};
        for(var i=0;i<16;i++) Assert.True(Math.Abs(expected[i]-p.Transform.Values[i])<=Policy.MatrixCoefficientTolerance,$"matrix[{i}]");
        ExpectedPoint(p,new(110,70,0),new(940,2020,0));
    }
    [Theory][InlineData("base")][InlineData("anchor")]
    public async Task IntermediateCompositionOverflowRefusesInsteadOfClamping(string field)
    {
        var snapshot=field=="base"?await Snapshot(new(double.MaxValue,0,0),scale:new(2,1,1)):
            await Snapshot(new(-double.MaxValue,0,0),new(double.MaxValue,0,0));
        var r=Result(snapshot); Assert.Null(r.Placement);
        Assert.Equal(CadPlacementDiagnosticCode.NonFiniteTransform,Assert.Single(r.Diagnostics).Code);
    }
    [Fact] public async Task PointAndVectorOutputOverflowAreStructuredFailures()
    {
        var p=Placement(await Snapshot(scale:new(double.MaxValue,1,1)));
        var point=CadInsertLocalTransform.ApplyPoint(p,new(2,0,0)); var vector=CadInsertLocalTransform.ApplyVector(p,new(2,0,0));
        Assert.False(point.Succeeded); Assert.False(vector.Succeeded); Assert.Null(point.PointMm); Assert.Null(vector.Vector);
        Assert.Equal(CadPlacementDiagnosticCode.NonFiniteTransformedValue,Assert.Single(point.Diagnostics).Code);
        Assert.Equal(CadPlacementDiagnosticCode.NonFiniteTransformedValue,Assert.Single(vector.Diagnostics).Code);
    }
}
public sealed class CadInsertTransformOrderTests
{
    [Fact] public async Task NonuniformScaleActsBeforeRotation()
    { var p=Placement(await Snapshot(scale:new(2,3,1),rotation:90)); ExpectedPoint(p,new(1,2,0),new(-6,2,0)); Assert.NotEqual(new Point3(-4,3,0),CadInsertLocalTransform.ApplyPoint(p,new(1,2,0)).PointMm); }
    [Fact] public async Task TranslationItselfIsNeitherScaledNorRotated()
    {
        var p=Placement(await Snapshot(insertion:new(100,200,300),scale:new(2,3,4),rotation:90));
        ExpectedPoint(p,new(0,0,0),new(100,200,300)); ExpectedPoint(p,new(1,2,3),new(94,202,312));
    }
    [Fact] public async Task HistoricalNonzeroBaseRegression()
    {
        var p=Placement(await Snapshot(new(100,50,0),new(1000,2000,0),new(2,3,1),90));
        // q-b=(10,20,0), S=(20,60,0), R90=(-60,20,0), +i=(940,2020,0).
        ExpectedPoint(p,new(110,70,0),new(940,2020,0));
        Assert.NotEqual(new Point3(790,2220,0),CadInsertLocalTransform.ApplyPoint(p,new(110,70,0)).PointMm);
    }
}
public sealed class CadInsertTiltedOcsTests
{
    [Fact] public async Task TiltedAnchorIsNotTransformedByASecondTime()
    {
        var p=Placement(await Snapshot(new(10,20,0),new(100,200,0),new(2,3,1),90,new(0,1,0)));
        Assert.Equal(new Point3(-100,0,200),p.Anchor.PointMm);
        // q-b=(2,3,0); S=(4,9,0); R=(-9,4,0); +i=(91,204,0); A=(-91,0,204).
        ExpectedPoint(p,new(12,23,0),new(-91,0,204)); ExpectedPoint(p,new(10,20,0),new(-100,0,200));
    }
    [Fact] public async Task BlockBaseMapsExactlyToInsertionAnchor()
    {
        var p=Placement(await Snapshot(new(10,20,30),new(100,200,0),new(-2,3,1),90,new(0,1,0)));
        var result=CadInsertLocalTransform.ApplyPoint(p,new(10,20,30)); Assert.Equal(p.Anchor.PointMm,result.PointMm);
        ExpectedPoint(p,new(12,23,30),new(-91,0,196)); ExpectedVector(p,new(2,3,0),new(9,0,-4));
    }
    [Fact] public async Task ObliqueLocalPlacementUsesIndependent04CConstantsOnce()
    {
        var p=Placement(await Snapshot(new(10,20,30),new(100,200,300),new(2,3,1),0,new(1,1,1)));
        // Relative q=(1,1,1) -> S=(2,3,1), add OCS insertion -> (102,203,301), then hand basis.
        var r2=Math.Sqrt(2); var r3=Math.Sqrt(3); var r6=Math.Sqrt(6);
        ExpectedPoint(p,new(11,21,31),new(-102/r2-203/r6+301/r3,102/r2-203/r6+301/r3,406/r6+301/r3));
        ExpectedPoint(p,new(10,20,30),p.Anchor.PointMm!.Value);
    }
}
public sealed class CadInsertMirrorTests
{
    [Theory][InlineData(-1,1,1,-1)][InlineData(1,-1,1,-1)][InlineData(1,1,-1,-1)]
    [InlineData(-1,-1,1,1)][InlineData(-1,-1,-1,-1)][InlineData(2,3,4,1)]
    public async Task ReflectionsPreservePointVectorAndOrientation(double x,double y,double z,int sign)
    {
        var p=Placement(await Snapshot(insertion:new(100,200,300),scale:new(x,y,z)));
        Assert.Equal(sign,p.OrientationSign); ExpectedPoint(p,new(1,2,3),new(100+x,200+2*y,300+3*z)); ExpectedVector(p,new(1,2,3),new(x,2*y,3*z));
        var m=p.Transform.Values; var determinant=m[0]*(m[5]*m[10]-m[6]*m[9])-m[1]*(m[4]*m[10]-m[6]*m[8])+m[2]*(m[4]*m[9]-m[5]*m[8]);
        Assert.Equal(sign,Math.Sign(determinant));
    }
    [Fact] public async Task TinyNegativeScaleRetainsSignWithoutDeterminantUnderflow()
    { var p=Placement(await Snapshot(scale:new(-1e-300,1e-300,1e-300))); Assert.Equal(-1,p.OrientationSign); Assert.Equal(-1e-300,p.Transform.Values[0]); }
}
