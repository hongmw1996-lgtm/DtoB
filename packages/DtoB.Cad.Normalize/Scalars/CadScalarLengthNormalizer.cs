using DtoB.Cad.Raw;
using DtoB.Cad.V2;

namespace DtoB.Cad.Normalize.Scalars;

public static class CadScalarLengthNormalizer
{
    public static CadScalarResult Normalize(CadScalarFact fact, CadUnitResolution units)
    {
        CadContract.Required(fact); CadContract.Required(units);
        var role = fact.Source.Role;
        if (role is not (CadScalarRole.ArcRadius or CadScalarRole.CircleRadius or CadScalarRole.TextHeight
            or CadScalarRole.MTextWidth or CadScalarRole.InsertColumnSpacing or CadScalarRole.InsertRowSpacing
            or CadScalarRole.PolylineElevation or CadScalarRole.PolylineConstantWidth or CadScalarRole.LinearDimensionMeasurement))
            return CadScalarResult.Fail(fact, CadScalarStatus.Unsupported, CadScalarDiagnosticCode.UnsupportedLengthRole);
        if (!fact.RoleSupported) return CadScalarResult.Fail(fact, CadScalarStatus.Unsupported, CadScalarDiagnosticCode.UnsupportedLengthRole);
        if (fact.IsAbsent && fact.Kind == DxfFactKind.Real) return CadScalarResult.Fail(fact, CadScalarStatus.Absent, CadScalarDiagnosticCode.MissingSourceFact);
        if (!fact.IsValid(DxfFactKind.Real)) return CadScalarResult.Fail(fact, CadScalarStatus.Invalid, CadScalarDiagnosticCode.InvalidSourceFact);
        var value = fact.Real!.Value;
        if (!CadNumericPolicy.IsFiniteSource(value)) return CadScalarResult.Fail(fact, CadScalarStatus.Invalid, CadScalarDiagnosticCode.NonFiniteSourceValue);
        if (units.Status != CadUnitStatus.Resolved) return CadScalarResult.Fail(fact,
            units.Status == CadUnitStatus.InvalidUnits ? CadScalarStatus.Invalid : CadScalarStatus.Unsupported,
            units.Status switch { CadUnitStatus.InvalidUnits => CadScalarDiagnosticCode.InvalidUnits,
                CadUnitStatus.UnsupportedUnits => CadScalarDiagnosticCode.UnsupportedUnits, _ => CadScalarDiagnosticCode.NeedsExplicitUnits });
        if (fact.Source.Document.DrawingId != units.Source.Document.DrawingId || fact.Source.Document.RevisionId != units.Source.Document.RevisionId
            || fact.Source.Document.FileHash != units.Source.Document.FileHash)
            return CadScalarResult.Fail(fact, CadScalarStatus.Invalid, CadScalarDiagnosticCode.InvalidSourceFact);
        var signed = role is CadScalarRole.InsertColumnSpacing or CadScalarRole.InsertRowSpacing or CadScalarRole.PolylineElevation;
        var strictlyPositive = role is CadScalarRole.ArcRadius or CadScalarRole.CircleRadius or CadScalarRole.TextHeight;
        if (!signed && (value < 0 || strictlyPositive && value == 0))
            return CadScalarResult.Fail(fact, CadScalarStatus.Invalid, CadScalarDiagnosticCode.InvalidLengthSign);
        var result = value * units.Decision.SourceToMillimeters!.Value;
        return !double.IsFinite(result)
            ? CadScalarResult.Fail(fact, CadScalarStatus.Invalid, CadScalarDiagnosticCode.NonFiniteNormalizedValue)
            : new(CadScalarStatus.Normalized, result, fact.Source, []);
    }
}
