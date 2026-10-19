using DtoB.Cad.Normalize.Coordinates;
using DtoB.Cad.Normalize.Scalars;
using DtoB.Cad.Raw;
using DtoB.Cad.V2;
using DtoB.Geometry;

namespace DtoB.Cad.Normalize.Placement;

public static class CadBlockBaseNormalizer
{
    public static CadBlockBaseResult Normalize(CadPlacementSource source, RawBlockBegin block, CadUnitResolution units,
        CadBlockUnitResolution? blockUnits = null)
    {
        ArgumentNullException.ThrowIfNull(source); ArgumentNullException.ThrowIfNull(block); ArgumentNullException.ThrowIfNull(units);
        var facts = new CadCoordinateTriple(block.BasePoint); var name = new CadPlacementNameFact(block.Name);
        CadBlockBaseResult Fail(CadPlacementDiagnosticCode code, string field, CadPlacementStatus status = CadPlacementStatus.Invalid) =>
            new(status,source,name,facts,null,units,blockUnits,[new(code,source,field)]);
        if (!SameDocument(source.Document,units.Source.Document)) return Fail(CadPlacementDiagnosticCode.SourceAuthorityMismatch,"Units");
        if (units.Status != CadUnitStatus.Resolved) return Fail(CadPlacementDiagnosticCode.UnresolvedUnits,"Units",CadPlacementStatus.Unsupported);
        if (blockUnits is not null && !SameDocument(source.Document,blockUnits.Source.Document)) return Fail(CadPlacementDiagnosticCode.SourceAuthorityMismatch,"BlockUnits");
        if (blockUnits?.Status is CadBlockUnitStatus.InvalidUnits or CadBlockUnitStatus.UnsupportedUnits)
            return Fail(CadPlacementDiagnosticCode.UnsupportedBlockUnits,"BlockUnits",CadPlacementStatus.Unsupported);
        if (!name.Valid) return Fail(CadPlacementDiagnosticCode.InvalidBlockDefinition,"Name");
        if (!facts.X.Valid || !facts.Y.Valid || !facts.Z.Valid) return Fail(CadPlacementDiagnosticCode.InvalidBlockBase,"Base");
        var scale = units.Decision.SourceToMillimeters!.Value;
        var p = new Point3(facts.X.Value!.Value*scale,facts.Y.Value!.Value*scale,facts.Z.Value!.Value*scale);
        if (!double.IsFinite(p.X) || !double.IsFinite(p.Y) || !double.IsFinite(p.Z)) return Fail(CadPlacementDiagnosticCode.InvalidBlockBase,"Base");
        return new(CadPlacementStatus.Normalized,source,name,facts,p,units,blockUnits,[]);
    }
    internal static bool SameDocument(CadSourceDocument a, CadSourceDocument b) => a.DrawingId == b.DrawingId && a.RevisionId == b.RevisionId && a.FileHash == b.FileHash;
}
