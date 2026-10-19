using DtoB.Cad.Normalize.Coordinates;
using DtoB.Cad.Normalize.Scalars;
using DtoB.Cad.Raw;
using DtoB.Cad.V2;
using DtoB.Geometry;

namespace DtoB.Cad.Normalize.Placement;

public enum CadPlacementStatus { Normalized, Unsupported, Invalid }
public enum CadPlacementDiagnosticCode
{
    MissingBlockDefinition, AmbiguousBlockDefinition, InvalidBlockDefinition, UnsupportedDefinition,
    InvalidBlockBase, InvalidInsertScale, InvalidInsertRotation, InvalidInsertAnchor,
    UnsupportedInsertProfile, SourceAuthorityMismatch, NonFiniteTransform, NonFiniteTransformedValue,
    UnsupportedBlockUnits, UnresolvedUnits
}
public sealed class CadPlacementSource
{
    public CadSourceDocument Document { get; }
    public CadSourceLocator Record { get; }
    public string? OriginalHandle { get; }
    public string? CanonicalHandle { get; }
    internal CadPlacementSource(CadSourceDocument document, RawDrawingRecordView row)
    {
        Document = document; var s = row.SourceRecord.Source;
        Record = new(CadSourceRecordDomain.StructuralRecord,row.StructuralRecordIndex,s.StartPairIndex,s.EndPairIndex,s.StartLine,s.EndLine);
        OriginalHandle = row.Identity?.OriginalHandle.Text; CanonicalHandle = row.Identity?.CanonicalHandle;
    }
}
public sealed class CadPlacementDiagnostic(CadPlacementDiagnosticCode code, CadPlacementSource source, string field, IReadOnlyList<int>? pairs = null)
{
    public CadPlacementDiagnosticCode Code { get; } = code;
    public CadPlacementSource Source { get; } = source;
    public string Field { get; } = field;
    public IReadOnlyList<int> PairIndices { get; } = CadContract.Copy(pairs ?? []);
}
public sealed class CadPlacementNameFact
{
    public string? Value { get; }
    public IReadOnlyList<int> PairIndices { get; }
    public int? FirstLine { get; }
    public int? LastLine { get; }
    internal bool Valid { get; }
    internal CadPlacementNameFact(DxfSourceFact fact)
    {
        Value = fact.Text; PairIndices = CadContract.Copy(fact.SourcePairIndices);
        FirstLine = fact.Source?.StartLine; LastLine = fact.Source?.EndLine;
        Valid = fact.Kind == DxfFactKind.Text && fact.Presence == DxfFactPresence.Present && fact.Validity == DxfFactValidity.Valid
            && !string.IsNullOrWhiteSpace(Value) && fact.Real is null && fact.Integer is null && PairIndices.Count == 1
            && fact.Source is {} s && s.StartPairIndex == PairIndices[0] && s.EndPairIndex == s.StartPairIndex && s.StartLine > 0 && s.EndLine >= s.StartLine;
    }
}
public sealed class CadBlockBaseResult
{
    public CadPlacementStatus Status { get; }
    public CadPlacementSource Source { get; }
    public CadPlacementNameFact Name { get; }
    public CadCoordinateTriple Facts { get; }
    public Point3? BaseMm { get; }
    public CadUnitResolution Units { get; }
    public CadBlockUnitResolution? BlockUnits { get; }
    public IReadOnlyList<CadPlacementDiagnostic> Diagnostics { get; }
    internal CadBlockBaseResult(CadPlacementStatus status, CadPlacementSource source, CadPlacementNameFact name,
        CadCoordinateTriple facts, Point3? value, CadUnitResolution units, CadBlockUnitResolution? blockUnits, IReadOnlyList<CadPlacementDiagnostic> diagnostics)
    { Status = status; Source = source; Name = name; Facts = facts; BaseMm = value; Units = units; BlockUnits = blockUnits; Diagnostics = CadContract.Copy(diagnostics); }
}
public sealed class CadInsertScaleResult
{
    public CadPlacementStatus Status { get; }
    public CadPlacementSource Source { get; }
    public CadCoordinateTriple Facts { get; }
    public CadVector3? Scale { get; }
    public IReadOnlyList<string> DefaultRules { get; }
    public IReadOnlyList<CadPlacementDiagnostic> Diagnostics { get; }
    internal CadInsertScaleResult(CadPlacementStatus status, CadPlacementSource source, CadCoordinateTriple facts,
        CadVector3? value, IReadOnlyList<string> defaults, IReadOnlyList<CadPlacementDiagnostic> diagnostics)
    { Status = status; Source = source; Facts = facts; Scale = value; DefaultRules = CadContract.Copy(defaults); Diagnostics = CadContract.Copy(diagnostics); }
}
/// <summary>One definition-to-immediate-parent placement. Contains no source graph or child geometry.</summary>
public sealed class CadLocalPlacement
{
    public CadTransform Transform { get; }
    public int OrientationSign { get; }
    public CadPlacementNameFact Target { get; }
    public CadBlockBaseResult Block { get; }
    public CadCoordinateResult Anchor { get; }
    public CadScalarResult Rotation { get; }
    public CadInsertScaleResult Scale { get; }
    public string PolicyId { get; }
    public IReadOnlyList<string> DefaultRules { get; }
    internal CadLocalPlacement(CadTransform transform, CadPlacementNameFact target, CadBlockBaseResult block, CadCoordinateResult anchor,
        CadScalarResult rotation, CadInsertScaleResult scale, CadNumericPolicy policy)
    {
        Transform = transform; Target = target; Block = block; Anchor = anchor; Rotation = rotation; Scale = scale; PolicyId = policy.ProfileId;
        var s = scale.Scale!; OrientationSign = ((s.X < 0) ^ (s.Y < 0) ^ (s.Z < 0)) ? -1 : 1;
        DefaultRules = CadContract.Copy(scale.DefaultRules.Concat(rotation.DefaultRule is {} r ? new[]{r} : []).Concat(anchor.DefaultRules).ToArray());
    }
}
public sealed class CadLocalPlacementResult
{
    public CadPlacementStatus Status { get; }
    public CadLocalPlacement? Placement { get; }
    public IReadOnlyList<CadPlacementDiagnostic> Diagnostics { get; }
    internal CadLocalPlacementResult(CadPlacementStatus status, CadLocalPlacement? placement, IReadOnlyList<CadPlacementDiagnostic> diagnostics)
    { CadContract.Require((status == CadPlacementStatus.Normalized) == (placement is not null),"Placement status/value mismatch."); Status = status; Placement = placement; Diagnostics = CadContract.Copy(diagnostics); }
}
public sealed class CadPlacementApplicationResult
{
    public Point3? PointMm { get; }
    public CadVector3? Vector { get; }
    public IReadOnlyList<CadPlacementDiagnostic> Diagnostics { get; }
    public bool Succeeded => Diagnostics.Count == 0;
    internal CadPlacementApplicationResult(Point3? point, CadVector3? vector, IReadOnlyList<CadPlacementDiagnostic> diagnostics)
    { PointMm = point; Vector = vector; Diagnostics = CadContract.Copy(diagnostics); }
}
