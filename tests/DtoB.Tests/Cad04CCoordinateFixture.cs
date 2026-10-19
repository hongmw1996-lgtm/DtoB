using DtoB.Cad.Normalize.Coordinates;
using DtoB.Cad.Normalize.Scalars;
using DtoB.Cad.Raw;
using DtoB.Cad.V2;
using DtoB.Geometry;
using Xunit;

namespace DtoB.Tests;

internal static class Cad04CCoordinateFixture
{
    internal static CadNumericPolicy Policy => new("04C-analytic-mm-rad-coefficient",new(1e-8,1e-10,1e-12));
    internal static DxfSourceFact Fact(double value,int pair = 3) => new(DxfFactKind.Real,DxfFactPresence.Present,
        DxfFactValidity.Valid,null,null,value,[pair],new(pair,pair,pair*2+1,pair*2+2));
    internal static DxfSourceFact Absent => Cad04BScalarFixture.Absent(DxfFactKind.Real);
    internal static RawCoordinateFacts Triple(double x,double y,double z) => new(Fact(x,3),Fact(y,4),Fact(z,5));
    internal static RawCoordinateFacts AbsentTriple => new(Absent,Absent,Absent);
    internal static CadCoordinateInput Input(CadCoordinateRole role = CadCoordinateRole.CircleCenterOcs,
        RawCoordinateFacts? point = null,RawCoordinateFacts? normal = null,DxfSourceFact? elevation = null) =>
        CadCoordinateInput.FromRaw(Cad04BScalarFixture.Document,role,point ?? Triple(2,3,4),normal ?? AbsentTriple,elevation);
    internal static CadUnitResolution Units(int code = 4) => Cad04BScalarFixture.Units(code);
    internal static CadOcsFrame Frame(double x,double y,double z)
    {
        var r=CadArbitraryAxis.Create(CadExtrusionNormalizer.Normalize(Input(normal:Triple(x,y,z))),Policy);
        Assert.Equal(CadCoordinateStatus.Normalized,r.Status); return Assert.IsType<CadOcsFrame>(r.Frame);
    }
    internal static void Vector(CadVector3? value,double x,double y,double z)
    {
        Assert.NotNull(value); Assert.True(Math.Abs(value.X-x)<=Policy.MatrixCoefficientTolerance,$"X {value.X:R} != {x:R}");
        Assert.True(Math.Abs(value.Y-y)<=Policy.MatrixCoefficientTolerance,$"Y {value.Y:R} != {y:R}");
        Assert.True(Math.Abs(value.Z-z)<=Policy.MatrixCoefficientTolerance,$"Z {value.Z:R} != {z:R}");
    }
    internal static void Point(CadCoordinateResult r,double x,double y,double z)
    {
        Assert.Equal(CadCoordinateStatus.Normalized,r.Status); Assert.NotNull(r.PointMm); Assert.Null(r.Vector);
        var p=r.PointMm.Value; Assert.True(Math.Abs(p.X-x)<=Policy.LengthMm); Assert.True(Math.Abs(p.Y-y)<=Policy.LengthMm); Assert.True(Math.Abs(p.Z-z)<=Policy.LengthMm);
    }
    internal static string Entity(string type,string fields) => Cad04BScalarFixture.Dxf($"0\n{type}\n5\na\n330\n0\n"+fields);
}
