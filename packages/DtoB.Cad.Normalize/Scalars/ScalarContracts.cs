using DtoB.Cad.Raw;
using DtoB.Cad.V2;

namespace DtoB.Cad.Normalize.Scalars;

public enum CadScalarRole
{
    HeaderInsertionUnits, BlockInsertionUnits,
    ArcStartAngle, ArcEndAngle, InsertRotation, TextRotation, MTextRotation, LinearDimensionRotation,
    PointDisplayAngle, StyleObliqueAngle,
    ArcRadius, CircleRadius, TextHeight, MTextWidth, InsertColumnSpacing, InsertRowSpacing,
    PolylineElevation, PolylineConstantWidth, LinearDimensionMeasurement
}
public enum CadScalarStatus { Normalized, Absent, Unsupported, Invalid }
public enum CadUnitStatus { Resolved, NeedsExplicitUnits, UnsupportedUnits, InvalidUnits }
public enum CadSourceLengthUnit { Inches = 1, Feet = 2, Millimeters = 4, Centimeters = 5, Meters = 6 }
public enum CadBlockUnitStatus { UsesDocumentUnit, MatchesDocumentUnit, UnsupportedUnits, InvalidUnits }
public enum CadScalarDiagnosticCode
{
    NeedsExplicitUnits, UnsupportedUnits, InvalidUnits, ContradictoryUnitOverride, ContradictoryBlockUnits,
    UnsupportedAngleRole, UnsupportedLengthRole, MissingSourceFact, InvalidSourceFact,
    NonFiniteSourceValue, NonFiniteNormalizedValue, InvalidLengthSign, AmbiguousArcSweep, MismatchedArcSources
}

/// <summary>Compact source authority/role/range. Header facts intentionally have no invented record index.</summary>
public sealed class CadScalarEvidence
{
    public CadSourceDocument Document { get; }
    public CadScalarRole Role { get; }
    public CadSourceLocator? Record { get; }
    public string? OriginalHandle { get; }
    public string? CanonicalHandle { get; }
    public IReadOnlyList<int> PairIndices { get; }
    public int? FirstPhysicalLine { get; }
    public int? LastPhysicalLine { get; }
    internal CadScalarEvidence(CadSourceDocument document, CadScalarRole role, DxfSourceFact? fact,
        CadSourceLocator? record, string? originalHandle, string? canonicalHandle)
    {
        Document = CadContract.Required(document); Role = CadContract.EnumValue(role); Record = record;
        OriginalHandle = originalHandle; CanonicalHandle = canonicalHandle;
        PairIndices = CadContract.Copy(fact?.SourcePairIndices ?? []);
        FirstPhysicalLine = fact?.Source?.StartLine; LastPhysicalLine = fact?.Source?.EndLine;
    }
}

/// <summary>Owned factual scalar input; no retained Raw object/graph and no default or conversion.</summary>
public sealed class CadScalarFact
{
    private readonly bool validEvidence;
    private readonly bool absentEvidence;
    public CadScalarEvidence Source { get; }
    public DxfFactKind? Kind { get; }
    public DxfFactPresence? Presence { get; }
    public DxfFactValidity? Validity { get; }
    public int? Integer { get; }
    public double? Real { get; }
    public bool RoleSupported { get; }
    internal CadScalarFact(CadSourceDocument document, CadScalarRole role, DxfSourceFact? fact,
        CadSourceLocator? record = null, string? originalHandle = null, string? canonicalHandle = null, bool roleSupported = true)
    {
        Source = new(document, role, fact, record, originalHandle, canonicalHandle);
        Kind = fact?.Kind; Presence = fact?.Presence; Validity = fact?.Validity;
        Integer = fact?.Integer; Real = fact?.Real; RoleSupported = roleSupported && fact is not null;
        absentEvidence = fact is not null && fact.Text is null && fact.Integer is null && fact.Real is null && fact.Source is null;
        validEvidence = fact?.Text is null && fact?.Source is { } span && Source.PairIndices.Count == 1
            && Source.PairIndices[0] >= 0 && span.StartPairIndex == Source.PairIndices[0] && span.EndPairIndex == span.StartPairIndex
            && span.StartLine > 0 && span.EndLine >= span.StartLine;
    }
    public static CadScalarFact FromRaw(CadSourceDocument document, CadScalarRole role, DxfSourceFact fact) =>
        new(document, role, CadContract.Required(fact));
    internal bool IsAbsent => RoleSupported && absentEvidence && Presence == DxfFactPresence.Absent && Validity == DxfFactValidity.NotApplicable && Source.PairIndices.Count == 0;
    internal bool IsValid(DxfFactKind kind) => validEvidence && RoleSupported && Kind == kind && Presence == DxfFactPresence.Present
        && Validity == DxfFactValidity.Valid && Source.PairIndices.Count == 1
        && (kind == DxfFactKind.Integer ? Integer.HasValue && Real is null : Real.HasValue && Integer is null);
}
public sealed class CadScalarDiagnostic(CadScalarDiagnosticCode code, CadScalarEvidence source, IReadOnlyList<string> arguments)
{
    public CadScalarDiagnosticCode Code { get; } = CadContract.EnumValue(code);
    public CadScalarEvidence Source { get; } = CadContract.Required(source);
    public IReadOnlyList<string> Arguments { get; } = CadContract.Copy(arguments);
}
public sealed class CadScalarResult
{
    public CadScalarStatus Status { get; }
    public double? Value { get; }
    public CadScalarEvidence Source { get; }
    public string? DefaultRule { get; }
    public IReadOnlyList<CadScalarDiagnostic> Diagnostics { get; }
    internal CadScalarResult(CadScalarStatus status, double? value, CadScalarEvidence source,
        IReadOnlyList<CadScalarDiagnostic> diagnostics, string? defaultRule = null)
    {
        Status = status; Value = value is { } number ? CadContract.Finite(number) : null;
        Source = source; DefaultRule = defaultRule; Diagnostics = CadContract.Copy(diagnostics);
        CadContract.Require((status == CadScalarStatus.Normalized) == value.HasValue, "Scalar status/value mismatch.");
    }
    internal static CadScalarResult Fail(CadScalarFact fact, CadScalarStatus status, CadScalarDiagnosticCode code) =>
        new(status, null, fact.Source, [new(code, fact.Source, [])]);
}
public sealed class CadUnitResolution
{
    public CadUnitStatus Status { get; }
    public CadSourceLengthUnit? Unit { get; }
    public CadUnitDecision Decision { get; }
    public CadScalarEvidence Source { get; }
    public IReadOnlyList<CadScalarDiagnostic> Diagnostics { get; }
    internal CadUnitResolution(CadUnitStatus status, CadSourceLengthUnit? unit, CadUnitDecision decision,
        CadScalarEvidence source, IReadOnlyList<CadScalarDiagnostic> diagnostics)
    { Status = status; Unit = unit; Decision = decision; Source = source; Diagnostics = CadContract.Copy(diagnostics); }
}
public sealed class CadExplicitUnitSelection(CadSourceLengthUnit unit, string decisionId, string reason)
{
    public CadSourceLengthUnit Unit { get; } = CadContract.EnumValue(unit);
    public string DecisionId { get; } = CadContract.Text(decisionId);
    public string Reason { get; } = CadContract.Text(reason);
}
public sealed class CadBlockUnitResolution(CadBlockUnitStatus status, CadScalarEvidence source, IReadOnlyList<CadScalarDiagnostic> diagnostics)
{
    public CadBlockUnitStatus Status { get; } = CadContract.EnumValue(status);
    public CadScalarEvidence Source { get; } = CadContract.Required(source);
    public IReadOnlyList<CadScalarDiagnostic> Diagnostics { get; } = CadContract.Copy(diagnostics);
}
