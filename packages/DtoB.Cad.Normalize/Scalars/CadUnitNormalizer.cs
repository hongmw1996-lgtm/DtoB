using DtoB.Cad.Raw;
using DtoB.Cad.V2;

namespace DtoB.Cad.Normalize.Scalars;

public static class CadUnitNormalizer
{
    // Decimal factors are specified exactly before conversion to binary double storage.
    private static decimal Factor(CadSourceLengthUnit unit) => unit switch
    {
        CadSourceLengthUnit.Inches => 25.4m, CadSourceLengthUnit.Feet => 304.8m,
        CadSourceLengthUnit.Millimeters => 1m, CadSourceLengthUnit.Centimeters => 10m,
        CadSourceLengthUnit.Meters => 1000m, _ => throw new InvalidDataException("Unsupported explicit unit.")
    };
    private static CadUnitStatus Classify(CadScalarFact fact)
    {
        if (!fact.RoleSupported || fact.Source.Role != CadScalarRole.HeaderInsertionUnits) return CadUnitStatus.InvalidUnits;
        if (fact.IsAbsent && fact.Kind == DxfFactKind.Integer) return CadUnitStatus.NeedsExplicitUnits;
        if (!fact.IsValid(DxfFactKind.Integer) || fact.Integer is < 0 or > 24) return CadUnitStatus.InvalidUnits;
        if (fact.Integer == 0) return CadUnitStatus.NeedsExplicitUnits;
        return Enum.IsDefined(typeof(CadSourceLengthUnit), fact.Integer!.Value) ? CadUnitStatus.Resolved : CadUnitStatus.UnsupportedUnits;
    }
    public static CadUnitResolution Resolve(CadScalarFact fact, CadExplicitUnitSelection? selection = null, CadSourceFactRef? registryEvidence = null)
    {
        CadContract.Required(fact); var status = Classify(fact);
        CadUnitResolution Fail(CadUnitStatus s, CadScalarDiagnosticCode code) => new(s, null,
            new(CadUnitDecisionKind.Unresolved, fact.Integer, null, null, null, registryEvidence), fact.Source, [new(code, fact.Source, [])]);
        if (status == CadUnitStatus.InvalidUnits) return Fail(status, CadScalarDiagnosticCode.InvalidUnits);
        if (status == CadUnitStatus.UnsupportedUnits) return Fail(status, CadScalarDiagnosticCode.UnsupportedUnits);
        if (status == CadUnitStatus.NeedsExplicitUnits && selection is null) return Fail(status, CadScalarDiagnosticCode.NeedsExplicitUnits);
        var unit = status == CadUnitStatus.Resolved ? (CadSourceLengthUnit)fact.Integer!.Value : selection!.Unit;
        if (selection is not null && status == CadUnitStatus.Resolved && selection.Unit != unit)
            return Fail(CadUnitStatus.InvalidUnits, CadScalarDiagnosticCode.ContradictoryUnitOverride);
        var isExplicit = status == CadUnitStatus.NeedsExplicitUnits;
        return new(CadUnitStatus.Resolved, unit, new(isExplicit ? CadUnitDecisionKind.Explicit : CadUnitDecisionKind.Declared,
            fact.Integer, (double)Factor(unit), isExplicit ? selection!.DecisionId : null, isExplicit ? selection!.Reason : null, registryEvidence), fact.Source, []);
    }
    public static CadBlockUnitResolution CheckBlock(CadScalarFact fact, CadUnitResolution document)
    {
        CadContract.Required(fact); CadContract.Required(document);
        CadBlockUnitResolution Fail(CadBlockUnitStatus status, CadScalarDiagnosticCode code) => new(status, fact.Source, [new(code, fact.Source, [])]);
        if (fact.Source.Role != CadScalarRole.BlockInsertionUnits || !fact.RoleSupported)
            return Fail(CadBlockUnitStatus.InvalidUnits, CadScalarDiagnosticCode.InvalidUnits);
        if (fact.Source.Document.DrawingId != document.Source.Document.DrawingId || fact.Source.Document.RevisionId != document.Source.Document.RevisionId
            || fact.Source.Document.FileHash != document.Source.Document.FileHash)
            return Fail(CadBlockUnitStatus.InvalidUnits, CadScalarDiagnosticCode.InvalidSourceFact);
        if (document.Status != CadUnitStatus.Resolved) return Fail(
            document.Status == CadUnitStatus.InvalidUnits ? CadBlockUnitStatus.InvalidUnits : CadBlockUnitStatus.UnsupportedUnits,
            document.Status switch { CadUnitStatus.InvalidUnits => CadScalarDiagnosticCode.InvalidUnits,
                CadUnitStatus.UnsupportedUnits => CadScalarDiagnosticCode.UnsupportedUnits, _ => CadScalarDiagnosticCode.NeedsExplicitUnits });
        if (fact.IsAbsent && fact.Kind == DxfFactKind.Integer) return new(CadBlockUnitStatus.UsesDocumentUnit, fact.Source, []);
        if (!fact.IsValid(DxfFactKind.Integer) || fact.Integer is < 0 or > 24) return Fail(CadBlockUnitStatus.InvalidUnits, CadScalarDiagnosticCode.InvalidUnits);
        if (fact.Integer == 0) return new(CadBlockUnitStatus.UsesDocumentUnit, fact.Source, []);
        if (!Enum.IsDefined(typeof(CadSourceLengthUnit), fact.Integer!.Value)) return Fail(CadBlockUnitStatus.UnsupportedUnits, CadScalarDiagnosticCode.UnsupportedUnits);
        return fact.Integer == (int)document.Unit!
            ? new(CadBlockUnitStatus.MatchesDocumentUnit, fact.Source, [])
            : Fail(CadBlockUnitStatus.UnsupportedUnits, CadScalarDiagnosticCode.ContradictoryBlockUnits);
    }
}
