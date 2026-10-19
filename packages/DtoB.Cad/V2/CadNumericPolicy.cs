using DtoB.Geometry;

namespace DtoB.Cad.V2;

/// <summary>Explicit normalization comparison policy, never snapping or recognition tolerance.</summary>
public sealed class CadNumericPolicy
{
    private readonly GeometryTolerance tolerance;
    public string ProfileId { get; }
    public double LengthMm => tolerance.LengthMm;
    public double AngleRadians => tolerance.AngleRadians;
    public double MatrixCoefficientTolerance => tolerance.MatrixCoefficientTolerance!.Value;
    public CadNumericPolicy(string profileId, GeometryTolerance tolerance)
    {
        ProfileId = CadContract.Text(profileId); CadContract.Required(tolerance).ValidateStructure();
        CadContract.Require(tolerance.MatrixCoefficientTolerance.HasValue, "Explicit dimensionless comparison tolerance required.");
        this.tolerance = new(tolerance.LengthMm, tolerance.AngleRadians, tolerance.MatrixCoefficientTolerance);
    }
    public static bool IsFiniteSource(double value) => double.IsFinite(value);
    public static bool IsExactZero(double value) { CadContract.Finite(value); return value == 0; }
    public bool IsNearZeroLengthMm(double value) => Math.Abs(CadContract.Finite(value)) <= LengthMm;
    public bool IsNearZeroAngleRadians(double value) => Math.Abs(CadContract.Finite(value)) <= AngleRadians;
    public bool IsNearZeroMatrixCoefficient(double value) => Math.Abs(CadContract.Finite(value)) <= MatrixCoefficientTolerance;
}
