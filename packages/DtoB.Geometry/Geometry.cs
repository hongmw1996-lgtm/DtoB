using DtoB.Core;

namespace DtoB.Geometry;

public readonly record struct Point3(double X, double Y, double Z)
{
    public void ValidateStructure() => Contract.Finite(X, Y, Z);
}
public sealed record Segment3(Point3 Start, Point3 End)
{
    public void ValidateStructure() { Start.ValidateStructure(); End.ValidateStructure(); Contract.Require(Start != End, "Segment endpoints must differ."); }
    public void ValidateGeometry(GeometryTolerance tolerance)
    { tolerance.ValidateStructure(); Start.ValidateStructure(); End.ValidateStructure(); Contract.Require(GeometryPredicates.Distance(Start, End) > tolerance.LengthMm, "Segment is below length tolerance."); }
}
public sealed record GeometryTolerance(double LengthMm, double AngleRadians, double? MatrixCoefficientTolerance = null)
{
    public void ValidateStructure() { Contract.Positive(LengthMm, AngleRadians); if (MatrixCoefficientTolerance.HasValue) Contract.Positive(MatrixCoefficientTolerance.Value); }
}

// Row-major affine matrix; column-vector points. Then(next) means next(this(point)).
public sealed record Transform3(double[] Matrix)
{
    public static Transform3 Identity => new([1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1]);
    public void ValidateStructure()
    {
        Contract.Require(Matrix != null && Matrix.Length == 16, "Transform must contain 16 values.");
        Contract.Finite(Matrix);
        Contract.Require(Matrix[12] == 0 && Matrix[13] == 0 && Matrix[14] == 0 && Matrix[15] == 1, "Transform must be affine.");
    }
    public static Transform3 Placement(Point3 translationMm, double rotationRadians, Point3 scale)
    {
        translationMm.ValidateStructure(); scale.ValidateStructure(); Contract.Finite(rotationRadians);
        double c = Math.Cos(rotationRadians), s = Math.Sin(rotationRadians);
        return new([c * scale.X, -s * scale.Y, 0, translationMm.X, s * scale.X, c * scale.Y, 0, translationMm.Y,
            0, 0, scale.Z, translationMm.Z, 0, 0, 0, 1]);
    }
    public Point3 Apply(Point3 p)
    {
        ValidateStructure(); p.ValidateStructure(); var m = Matrix;
        var result = new Point3(m[0]*p.X + m[1]*p.Y + m[2]*p.Z + m[3],
            m[4]*p.X + m[5]*p.Y + m[6]*p.Z + m[7], m[8]*p.X + m[9]*p.Y + m[10]*p.Z + m[11]);
        result.ValidateStructure(); return result;
    }
    public Transform3 Then(Transform3 next)
    {
        ValidateStructure(); next.ValidateStructure(); var result = new double[16];
        for (int r = 0; r < 4; r++) for (int c = 0; c < 4; c++) for (int k = 0; k < 4; k++)
            result[r*4+c] += next.Matrix[r*4+k] * Matrix[k*4+c];
        var transform = new Transform3(result); transform.ValidateStructure(); return transform;
    }
}

public static class GeometryPredicates
{
    public static double Distance(Point3 a, Point3 b) => Math.Sqrt(Math.Pow(a.X-b.X,2)+Math.Pow(a.Y-b.Y,2)+Math.Pow(a.Z-b.Z,2));
    public static void ClosedBoundary(Point3[] points, GeometryTolerance tolerance)
    {
        tolerance.ValidateStructure(); Contract.Require(points != null && points.Length >= 4, "Boundary requires three vertices plus closure.");
        foreach (var p in points) p.ValidateStructure();
        Contract.Require(Distance(points[0], points[^1]) <= tolerance.LengthMm, "Boundary exceeds closure tolerance.");
        for (int i=1;i<points.Length;i++) new Segment3(points[i-1],points[i]).ValidateGeometry(tolerance);
    }
}
