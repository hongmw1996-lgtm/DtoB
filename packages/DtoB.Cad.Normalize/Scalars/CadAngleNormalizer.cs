using DtoB.Cad.Raw;
using DtoB.Cad.V2;

namespace DtoB.Cad.Normalize.Scalars;

public static class CadAngleNormalizer
{
    public static double WrapPositiveRadians(double value)
    {
        CadContract.Finite(value);
        var remainder = value % Math.Tau;
        if (remainder == 0) return 0;
        if (remainder > 0) return remainder;
        var wrapped = remainder + Math.Tau;
        // Directed range repair for an IEEE addition rounding UP to the excluded endpoint;
        // no epsilon/snap. Keep the negative-side approach to the boundary nonzero.
        return wrapped == Math.Tau ? Math.BitDecrement(Math.Tau) : wrapped;
    }
    public static CadScalarResult Normalize(CadScalarFact fact)
    {
        CadContract.Required(fact); var role = fact.Source.Role;
        if (!fact.RoleSupported || role is not (CadScalarRole.ArcStartAngle or CadScalarRole.ArcEndAngle
            or CadScalarRole.InsertRotation or CadScalarRole.TextRotation or CadScalarRole.MTextRotation
            or CadScalarRole.LinearDimensionRotation or CadScalarRole.PointDisplayAngle or CadScalarRole.StyleObliqueAngle))
            return CadScalarResult.Fail(fact, CadScalarStatus.Unsupported, CadScalarDiagnosticCode.UnsupportedAngleRole);
        if (fact.IsAbsent && fact.Kind == DxfFactKind.Real)
        {
            if (role is CadScalarRole.InsertRotation or CadScalarRole.TextRotation)
                return new(CadScalarStatus.Normalized, 0, fact.Source, [], role == CadScalarRole.InsertRotation ? "DXF.INSERT.Rotation.DefaultZero" : "DXF.TEXT.Rotation.DefaultZero");
            return CadScalarResult.Fail(fact, CadScalarStatus.Absent, CadScalarDiagnosticCode.MissingSourceFact);
        }
        if (!fact.IsValid(DxfFactKind.Real)) return CadScalarResult.Fail(fact, CadScalarStatus.Invalid, CadScalarDiagnosticCode.InvalidSourceFact);
        var value = fact.Real!.Value;
        if (!CadNumericPolicy.IsFiniteSource(value)) return CadScalarResult.Fail(fact, CadScalarStatus.Invalid, CadScalarDiagnosticCode.NonFiniteSourceValue);
        var signed = role == CadScalarRole.StyleObliqueAngle;
        var radians = role == CadScalarRole.MTextRotation ? value : (signed ? value : value % 360d) * (Math.PI / 180d);
        if (!double.IsFinite(radians)) return CadScalarResult.Fail(fact, CadScalarStatus.Invalid, CadScalarDiagnosticCode.NonFiniteNormalizedValue);
        return new(CadScalarStatus.Normalized, signed ? radians : WrapPositiveRadians(radians), fact.Source, []);
    }
    public static CadArcScalars NormalizeArc(CadScalarFact start, CadScalarFact end)
    {
        CadContract.Required(start); CadContract.Required(end);
        var a = Normalize(start); var b = Normalize(end);
        var evidence = new[] {start.Source, end.Source};
        if (start.Source.Role != CadScalarRole.ArcStartAngle || end.Source.Role != CadScalarRole.ArcEndAngle
            || start.Source.Document.DrawingId != end.Source.Document.DrawingId || start.Source.Document.RevisionId != end.Source.Document.RevisionId
            || start.Source.Document.FileHash != end.Source.Document.FileHash || !SameRecord(start.Source.Record, end.Source.Record))
            return new(CadScalarStatus.Invalid, null, null, evidence, [new(CadScalarDiagnosticCode.MismatchedArcSources, start.Source, [])]);
        if (a.Status != CadScalarStatus.Normalized || b.Status != CadScalarStatus.Normalized)
            return new(CadScalarStatus.Invalid, null, null, evidence, a.Diagnostics.Concat(b.Diagnostics).ToArray());
        var sweep = WrapPositiveRadians(b.Value!.Value - a.Value!.Value);
        return sweep == 0
            ? new(CadScalarStatus.Unsupported, null, null, evidence, [new(CadScalarDiagnosticCode.AmbiguousArcSweep, start.Source, [])])
            : new(CadScalarStatus.Normalized, a.Value, sweep, evidence, []);
    }
    private static bool SameRecord(CadSourceLocator? a, CadSourceLocator? b) => a is null ? b is null : b is not null
        && a.Domain == b.Domain && a.RecordIndex == b.RecordIndex && a.FirstPair == b.FirstPair && a.LastPair == b.LastPair
        && a.FirstPhysicalLine == b.FirstPhysicalLine && a.LastPhysicalLine == b.LastPhysicalLine;
}
public sealed class CadArcScalars
{
    public CadScalarStatus Status { get; }
    public double? StartRadians { get; }
    public double? SweepRadians { get; }
    public IReadOnlyList<CadScalarEvidence> Sources { get; }
    public IReadOnlyList<CadScalarDiagnostic> Diagnostics { get; }
    internal CadArcScalars(CadScalarStatus status, double? startRadians, double? sweepRadians,
        IReadOnlyList<CadScalarEvidence> sources, IReadOnlyList<CadScalarDiagnostic> diagnostics)
    { Status = status; StartRadians = startRadians; SweepRadians = sweepRadians; Sources = CadContract.Copy(sources); Diagnostics = CadContract.Copy(diagnostics); }
}
