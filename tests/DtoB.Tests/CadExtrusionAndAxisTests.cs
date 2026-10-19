using DtoB.Cad.Normalize.Coordinates;
using DtoB.Cad.V2;
using Xunit;
using static DtoB.Tests.Cad04CCoordinateFixture;

namespace DtoB.Tests;

public sealed class CadExtrusionNormalizerTests
{
    [Fact] public void WhollyAbsentHasTraceablePositiveZDefaultWithoutPresentEvidence()
    {
        var r=CadExtrusionNormalizer.Normalize(Input()); Vector(r.Normal,0,0,1);
        Assert.Equal(CadExtrusionNormalizer.DefaultPositiveZRule,r.DefaultRule);
        Assert.Empty(r.Input.Extrusion.X.PairIndices); Assert.Equal(DtoB.Cad.Raw.DxfFactPresence.Absent,r.Input.Extrusion.X.Presence);
    }
    [Theory][InlineData(0,0,1,0,0,1)][InlineData(0,0,-1,0,0,-1)][InlineData(0,2,0,0,1,0)]
    [InlineData(0,1e-300,0,0,1,0)][InlineData(0,double.Epsilon,0,0,1,0)][InlineData(0,double.MaxValue,0,0,1,0)]
    [InlineData(double.MaxValue,double.MaxValue,double.MaxValue,0.5773502691896258,0.5773502691896258,0.5773502691896258)]
    public void NonzeroNormalsAreRobustlyNormalizedWithoutMagnitudeEpsilon(double x,double y,double z,double nx,double ny,double nz)
    {
        var r=CadExtrusionNormalizer.Normalize(Input(normal:Triple(x,y,z))); Assert.Equal(CadCoordinateStatus.Normalized,r.Status);
        Vector(r.Normal,nx,ny,nz); Assert.Null(r.DefaultRule);
        Assert.Equal(y,r.Input.Extrusion.Y.Value);
    }
    [Theory][InlineData(0,0,0)][InlineData(double.NaN,1,0)][InlineData(1,double.PositiveInfinity,0)][InlineData(1,0,double.NegativeInfinity)]
    public void InvalidNormalNeverGetsRepaired(double x,double y,double z)
    {
        var r=CadExtrusionNormalizer.Normalize(Input(normal:Triple(x,y,z)));
        Assert.Equal(CadCoordinateStatus.Invalid,r.Status); Assert.Null(r.Normal); Assert.Null(r.DefaultRule);
        Assert.Equal(CadCoordinateDiagnosticCode.InvalidExtrusionNormal,Assert.Single(r.Diagnostics).Code);
    }
    [Fact] public void PartialOrUnmappedNormalIsNotTheWhollyAbsentDefault()
    {
        var partial=new DtoB.Cad.Raw.RawCoordinateFacts(Fact(0),Absent,Fact(1));
        Assert.Equal(CadCoordinateStatus.Invalid,CadExtrusionNormalizer.Normalize(Input(normal:partial)).Status);
        var unmapped=CadCoordinateInput.FromRaw(Cad04BScalarFixture.Document,CadCoordinateRole.CircleCenterOcs,Triple(1,2,3));
        Assert.Equal(CadCoordinateStatus.Invalid,CadExtrusionNormalizer.Normalize(unmapped).Status);
    }
}
public sealed class CadArbitraryAxisTests
{
    [Theory][InlineData(0,0,1,1,0,0,0,1,0)][InlineData(0,0,-1,-1,0,0,0,1,0)]
    [InlineData(0,1,0,-1,0,0,0,0,1)][InlineData(1,0,0,0,1,0,0,0,1)]
    [InlineData(0,2,0,-1,0,0,0,0,1)]
    public void AxesMatchIndependentCrossProductOrder(double nx,double ny,double nz,double xx,double xy,double xz,double yx,double yy,double yz)
    {
        var f=Frame(nx,ny,nz); Vector(f.Ax,xx,xy,xz); Vector(f.Ay,yx,yy,yz);
        var n=CadExtrusionNormalizer.Normalize(Input(normal:Triple(nx,ny,nz))).Normal!; Vector(f.N,n.X,n.Y,n.Z);
        // Test-local determinant, independent of the production cross/dot helpers.
        var determinant=f.Ax.X*(f.Ay.Y*f.N.Z-f.Ay.Z*f.N.Y)-f.Ay.X*(f.Ax.Y*f.N.Z-f.Ax.Z*f.N.Y)+f.N.X*(f.Ax.Y*f.Ay.Z-f.Ax.Z*f.Ay.Y);
        Assert.True(Math.Abs(determinant-1)<=Policy.MatrixCoefficientTolerance);
    }
    [Fact] public void ObliqueBasisUsesHandCalculatedConstants()
    {
        var f=Frame(1,1,1);
        Vector(f.Ax,-0.7071067811865475,0.7071067811865475,0);
        Vector(f.Ay,-0.4082482904638631,-0.4082482904638631,0.8164965809277261);
        Vector(f.N,0.5773502691896258,0.5773502691896258,0.5773502691896258);
    }
    [Theory][InlineData(true)][InlineData(false)]
    public void ThresholdIsStrictForEachComponentAndDoesNotUsePolicy(bool xAxis)
    {
        var threshold=1d/64;
        foreach(var value in new[]{Math.BitDecrement(threshold),threshold,Math.BitIncrement(threshold)})
        {
            var n=new CadVector3(xAxis?value:0,xAxis?0:value,Math.Sqrt(1-value*value));
            Assert.Equal(value<threshold,CadArbitraryAxis.UsesWorldYAxis(n));
            var f=Frame(n.X,n.Y,n.Z);
            // Full path basis follows the normalized N, not the unnormalized source magnitude.
            if(CadArbitraryAxis.UsesWorldYAxis(f.N)) Assert.True(f.Ax.X>0);
            else if(xAxis) Vector(f.Ax,0,1,0); else Vector(f.Ax,-1,0,0);
        }
        Assert.False(CadArbitraryAxis.UsesWorldYAxis(new(threshold,threshold,Math.Sqrt(1-2*threshold*threshold))));
    }
    [Fact] public void TooStrictExplicitComparisonPolicyRefusesFrameWithoutRepair()
    {
        var normal=CadExtrusionNormalizer.Normalize(Input(normal:Triple(1,1,1)));
        var r=CadArbitraryAxis.Create(normal,new CadNumericPolicy("deliberately-too-strict",new(1e-8,1e-10,1e-30)));
        Assert.Equal(CadCoordinateStatus.Invalid,r.Status); Assert.Null(r.Frame);
        Assert.Equal(CadCoordinateDiagnosticCode.InvalidOcsFrame,Assert.Single(r.Diagnostics).Code);
    }
}
