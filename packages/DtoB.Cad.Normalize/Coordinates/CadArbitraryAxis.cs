using DtoB.Cad.V2;

namespace DtoB.Cad.Normalize.Coordinates;

internal static class CoordinateVectorMath
{
    internal static CadVector3? Unit(double x, double y, double z)
    {
        if (!double.IsFinite(x) || !double.IsFinite(y) || !double.IsFinite(z)) return null;
        var maximum = Math.Max(Math.Abs(x),Math.Max(Math.Abs(y),Math.Abs(z)));
        if (maximum == 0) return null;
        x /= maximum; y /= maximum; z /= maximum;
        var length = Math.Sqrt(x*x+y*y+z*z);
        return new(x/length,y/length,z/length);
    }
    internal static CadVector3 Cross(CadVector3 a, CadVector3 b) => new(a.Y*b.Z-a.Z*b.Y,a.Z*b.X-a.X*b.Z,a.X*b.Y-a.Y*b.X);
    internal static double Dot(CadVector3 a, CadVector3 b) => a.X*b.X+a.Y*b.Y+a.Z*b.Z;
}
public static class CadExtrusionNormalizer
{
    public const string DefaultPositiveZRule = "DXF.OCS.Extrusion.DefaultPositiveZ";
    public static CadNormalizedNormal Normalize(CadCoordinateInput input)
    {
        ArgumentNullException.ThrowIfNull(input); var n = input.Extrusion;
        CadNormalizedNormal Fail(string field) => new(CadCoordinateStatus.Invalid,null,input,null,
            [new(CadCoordinateDiagnosticCode.InvalidExtrusionNormal,input.Source,field)]);
        if (!input.RoleSupported || CadCoordinateRoles.Describe(input.Source.Role)?.Space != CadCoordinateSpace.Ocs)
            return new(CadCoordinateStatus.Unsupported,null,input,null,[new(CadCoordinateDiagnosticCode.UnsupportedCoordinateRole,input.Source,"Extrusion")]);
        if (n.AllAbsent) return new(CadCoordinateStatus.Normalized,new(0,0,1),input,DefaultPositiveZRule,[]);
        if (!n.X.Valid || !n.Y.Valid || !n.Z.Valid) return Fail("Extrusion");
        var value = CoordinateVectorMath.Unit(n.X.Value!.Value,n.Y.Value!.Value,n.Z.Value!.Value);
        return value is null ? Fail("Extrusion") : new(CadCoordinateStatus.Normalized,value,input,null,[]);
    }
}
public static class CadArbitraryAxis
{
    // Specification constant; never adjusted by numerical/recognition tolerance.
    public static bool UsesWorldYAxis(CadVector3 normalizedNormal) => Math.Abs(normalizedNormal.X) < 1d/64d && Math.Abs(normalizedNormal.Y) < 1d/64d;
    public static CadOcsFrameResult Create(CadNormalizedNormal normal, CadNumericPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(normal); ArgumentNullException.ThrowIfNull(policy);
        if (normal.Status != CadCoordinateStatus.Normalized) return new(normal.Status,null,normal,normal.Diagnostics);
        var n = normal.Normal!;
        var cross = CoordinateVectorMath.Cross(UsesWorldYAxis(n) ? new(0,1,0) : new(0,0,1),n);
        var ax = CoordinateVectorMath.Unit(cross.X,cross.Y,cross.Z)!;
        cross = CoordinateVectorMath.Cross(n,ax);
        var ay = CoordinateVectorMath.Unit(cross.X,cross.Y,cross.Z)!;
        var handed = CoordinateVectorMath.Cross(ax,ay);
        bool Close(double value) => policy.IsNearZeroMatrixCoefficient(value);
        if (!Close(CoordinateVectorMath.Dot(ax,ax)-1) || !Close(CoordinateVectorMath.Dot(ay,ay)-1) || !Close(CoordinateVectorMath.Dot(n,n)-1)
            || !Close(CoordinateVectorMath.Dot(ax,ay)) || !Close(CoordinateVectorMath.Dot(ax,n)) || !Close(CoordinateVectorMath.Dot(ay,n))
            || !Close(handed.X-n.X) || !Close(handed.Y-n.Y) || !Close(handed.Z-n.Z))
            return new(CadCoordinateStatus.Invalid,null,normal,[new(CadCoordinateDiagnosticCode.InvalidOcsFrame,normal.Input.Source,"Frame")]);
        return new(CadCoordinateStatus.Normalized,new(ax,ay,normal,policy),normal,[]);
    }
}
