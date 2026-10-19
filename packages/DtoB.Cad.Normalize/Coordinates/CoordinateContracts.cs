using DtoB.Cad.Raw;
using DtoB.Cad.V2;
using DtoB.Cad.Normalize.Scalars;
using DtoB.Geometry;

namespace DtoB.Cad.Normalize.Coordinates;

public enum CadCoordinateRole
{
    LineStartWcs, LineEndWcs, PointPositionWcs, LwPolylineVertexOcs, CircleCenterOcs, ArcCenterOcs,
    SolidFirstOcs, SolidSecondOcs, SolidThirdOcs, SolidFourthOcs, TextInsertionOcs, TextAlignmentOcs,
    InsertInsertionOcs, MTextInsertionWcs, MTextDirectionWcs, DimensionDefinitionWcs,
    DimensionFirstExtensionWcs, DimensionSecondExtensionWcs, DimensionTextMidpointOcs,
    OcsDisplacementVector, OcsDirectionVector
}
public enum CadCoordinateSpace { Wcs, Ocs }
public enum CadCoordinateKind { Point, Vector }
public enum CadCoordinateStatus { Normalized, Absent, Unsupported, Invalid }
public enum CadCoordinateDiagnosticCode
{
    UnsupportedCoordinateRole, MissingRequiredCoordinateFact, InvalidSourceFact,
    NonFiniteSourceCoordinate, NonFiniteNormalizedCoordinate, InvalidExtrusionNormal,
    InvalidOcsFrame, NeedsExplicitUnits, UnsupportedUnits, InvalidUnits, MismatchedSourceAuthority, ConflictingCoordinateFacts
}

public sealed class CadCoordinateRoleInfo(CadCoordinateKind kind, CadCoordinateSpace space,
    bool dimensional, bool separateElevation, bool optional)
{
    public CadCoordinateKind Kind { get; } = kind;
    public CadCoordinateSpace Space { get; } = space;
    public bool Dimensional { get; } = dimensional;
    public bool SeparateElevation { get; } = separateElevation;
    public bool Optional { get; } = optional;
}
public static class CadCoordinateRoles
{
    public static CadCoordinateRoleInfo? Describe(CadCoordinateRole role)
    {
        if (!Enum.IsDefined(role)) return null;
        var vector = role is CadCoordinateRole.MTextDirectionWcs or CadCoordinateRole.OcsDisplacementVector or CadCoordinateRole.OcsDirectionVector;
        var wcs = role is CadCoordinateRole.LineStartWcs or CadCoordinateRole.LineEndWcs or CadCoordinateRole.PointPositionWcs
            or CadCoordinateRole.MTextInsertionWcs or CadCoordinateRole.MTextDirectionWcs or CadCoordinateRole.DimensionDefinitionWcs
            or CadCoordinateRole.DimensionFirstExtensionWcs or CadCoordinateRole.DimensionSecondExtensionWcs;
        return new(vector ? CadCoordinateKind.Vector : CadCoordinateKind.Point, wcs ? CadCoordinateSpace.Wcs : CadCoordinateSpace.Ocs,
            role is not (CadCoordinateRole.MTextDirectionWcs or CadCoordinateRole.OcsDirectionVector),
            role == CadCoordinateRole.LwPolylineVertexOcs,
            role is CadCoordinateRole.TextAlignmentOcs or CadCoordinateRole.DimensionTextMidpointOcs or CadCoordinateRole.MTextDirectionWcs);
    }
}

/// <summary>Owned fact/value/evidence, including genuine absence. No upstream graph retained.</summary>
public sealed class CadCoordinateFact
{
    public bool Mapped { get; }
    public DxfFactPresence? Presence { get; }
    public DxfFactValidity? Validity { get; }
    public double? Value { get; }
    public IReadOnlyList<int> PairIndices { get; }
    public int? FirstLine { get; }
    public int? LastLine { get; }
    internal bool Absent { get; }
    internal bool Valid { get; }
    internal CadCoordinateFact(DxfSourceFact? fact)
    {
        Mapped = fact is not null; Presence = fact?.Presence; Validity = fact?.Validity; Value = fact?.Real;
        PairIndices = CadContract.Copy(fact?.SourcePairIndices ?? []); FirstLine = fact?.Source?.StartLine; LastLine = fact?.Source?.EndLine;
        var realShape = fact is not null && fact.Kind == DxfFactKind.Real && fact.Text is null && fact.Integer is null;
        Absent = realShape && Presence == DxfFactPresence.Absent && Validity == DxfFactValidity.NotApplicable
            && Value is null && PairIndices.Count == 0 && fact!.Source is null;
        Valid = realShape && Presence == DxfFactPresence.Present && Validity == DxfFactValidity.Valid && Value.HasValue
            && PairIndices.Count == 1 && PairIndices[0] >= 0 && fact!.Source is { } span
            && span.StartPairIndex == PairIndices[0] && span.EndPairIndex == span.StartPairIndex
            && span.StartLine > 0 && span.EndLine >= span.StartLine;
    }
}
public sealed class CadCoordinateTriple
{
    public CadCoordinateFact X { get; }
    public CadCoordinateFact Y { get; }
    public CadCoordinateFact Z { get; }
    internal CadCoordinateTriple(RawCoordinateFacts? raw)
    { X = new(raw?.X); Y = new(raw?.Y); Z = new(raw?.Z); }
    internal CadCoordinateTriple(DxfSourceFact x, DxfSourceFact y)
    { X = new(x); Y = new(y); Z = new(null); }
    internal bool AllAbsent => X.Absent && Y.Absent && Z.Absent;
}
public sealed class CadCoordinateEvidence
{
    public CadSourceDocument Document { get; }
    public CadCoordinateRole Role { get; }
    public CadSourceLocator? Record { get; }
    public string? OriginalHandle { get; }
    public string? CanonicalHandle { get; }
    public int? VertexOrdinal { get; }
    internal CadCoordinateEvidence(CadSourceDocument document, CadCoordinateRole role, CadSourceLocator? record,
        string? originalHandle, string? canonicalHandle, int? vertexOrdinal)
    { Document = CadContract.Required(document); Role = role; Record = record; OriginalHandle = originalHandle; CanonicalHandle = canonicalHandle; VertexOrdinal = vertexOrdinal; }
}
public sealed class CadCoordinateInput
{
    public CadCoordinateEvidence Source { get; }
    public CadCoordinateTriple Coordinates { get; }
    public CadCoordinateTriple Extrusion { get; }
    public CadCoordinateFact Elevation { get; }
    public bool RoleSupported { get; }
    internal CadCoordinateInput(CadCoordinateEvidence source, CadCoordinateTriple coordinates, RawCoordinateFacts? extrusion,
        DxfSourceFact? elevation, bool supported)
    { Source = source; Coordinates = coordinates; Extrusion = new(extrusion); Elevation = new(elevation); RoleSupported = supported; }
    public static CadCoordinateInput FromRaw(CadSourceDocument document, CadCoordinateRole role, RawCoordinateFacts coordinates,
        RawCoordinateFacts? extrusion = null, DxfSourceFact? elevation = null) =>
        new(new(document,role,null,null,null,null),new(coordinates),extrusion,elevation,true);
}
public sealed class CadCoordinateDiagnostic(CadCoordinateDiagnosticCode code, CadCoordinateEvidence source, string field)
{
    public CadCoordinateDiagnosticCode Code { get; } = code;
    public CadCoordinateEvidence Source { get; } = source;
    public string Field { get; } = field;
}
public sealed class CadVector3
{
    public double X { get; }
    public double Y { get; }
    public double Z { get; }
    public CadVector3(double x, double y, double z)
    { X = CadContract.Finite(x); Y = CadContract.Finite(y); Z = CadContract.Finite(z); }
}
public sealed class CadNormalizedNormal
{
    public CadCoordinateStatus Status { get; }
    public CadVector3? Normal { get; }
    public CadCoordinateInput Input { get; }
    public string? DefaultRule { get; }
    public IReadOnlyList<CadCoordinateDiagnostic> Diagnostics { get; }
    internal CadNormalizedNormal(CadCoordinateStatus status, CadVector3? normal, CadCoordinateInput input,
        string? rule, IReadOnlyList<CadCoordinateDiagnostic> diagnostics)
    { Status = status; Normal = normal; Input = input; DefaultRule = rule; Diagnostics = CadContract.Copy(diagnostics); }
}
public sealed class CadOcsFrame
{
    public CadVector3 Ax { get; }
    public CadVector3 Ay { get; }
    public CadVector3 N { get; }
    public CadNormalizedNormal NormalEvidence { get; }
    public string PolicyId { get; }
    internal CadOcsFrame(CadVector3 ax, CadVector3 ay, CadNormalizedNormal normal, CadNumericPolicy policy)
    { Ax = ax; Ay = ay; N = normal.Normal!; NormalEvidence = normal; PolicyId = policy.ProfileId; }
    public Point3 PointToWcs(Point3 point)
    { point.ValidateStructure(); var v = VectorToWcs(new(point.X,point.Y,point.Z)); return new(v.X,v.Y,v.Z); }
    public CadVector3 VectorToWcs(CadVector3 vector) => new(
        Ax.X*vector.X+Ay.X*vector.Y+N.X*vector.Z,
        Ax.Y*vector.X+Ay.Y*vector.Y+N.Y*vector.Z,
        Ax.Z*vector.X+Ay.Z*vector.Y+N.Z*vector.Z);
}
public sealed class CadOcsFrameResult
{
    public CadCoordinateStatus Status { get; }
    public CadOcsFrame? Frame { get; }
    public CadNormalizedNormal Normal { get; }
    public IReadOnlyList<CadCoordinateDiagnostic> Diagnostics { get; }
    internal CadOcsFrameResult(CadCoordinateStatus status, CadOcsFrame? frame, CadNormalizedNormal normal,
        IReadOnlyList<CadCoordinateDiagnostic> diagnostics)
    {
        CadContract.Require((status == CadCoordinateStatus.Normalized) == (frame is not null),"Frame status/value mismatch.");
        Status = status; Frame = frame; Normal = normal; Diagnostics = CadContract.Copy(diagnostics);
    }
}
public sealed class CadCoordinateResult
{
    public CadCoordinateStatus Status { get; }
    public CadCoordinateRoleInfo? Semantics { get; }
    public Point3? PointMm { get; }
    public CadVector3? Vector { get; }
    public CadCoordinateInput Input { get; }
    public CadUnitResolution? Units { get; }
    public CadOcsFrame? Frame { get; }
    public string PolicyId { get; }
    public IReadOnlyList<string> DefaultRules { get; }
    public IReadOnlyList<CadCoordinateDiagnostic> Diagnostics { get; }
    internal CadCoordinateResult(CadCoordinateStatus status, CadCoordinateInput input, CadUnitResolution? units,
        CadNumericPolicy policy, Point3? point, CadVector3? vector, CadOcsFrame? frame,
        IReadOnlyList<string> defaults, IReadOnlyList<CadCoordinateDiagnostic> diagnostics)
    {
        CadContract.Require(status == CadCoordinateStatus.Normalized ? (point is null) != (vector is null) : point is null && vector is null,"Coordinate status/value mismatch.");
        Status = status; Semantics = CadCoordinateRoles.Describe(input.Source.Role); Input = input; Units = units; Frame = frame; PolicyId = policy.ProfileId;
        PointMm = point; Vector = vector; DefaultRules = CadContract.Copy(defaults); Diagnostics = CadContract.Copy(diagnostics);
    }
}
