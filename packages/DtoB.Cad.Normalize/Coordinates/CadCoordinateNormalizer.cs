using DtoB.Cad.V2;
using DtoB.Cad.Normalize.Scalars;
using DtoB.Geometry;

namespace DtoB.Cad.Normalize.Coordinates;

public static class CadCoordinateNormalizer
{
    public const string ElevationDefaultZeroRule = "DXF.LWPOLYLINE.Elevation.DefaultZero";
    public static CadCoordinateResult NormalizePoint(CadCoordinateInput input, CadUnitResolution units, CadNumericPolicy policy) =>
        Normalize(input,units,policy,CadCoordinateKind.Point);
    public static CadCoordinateResult NormalizeVector(CadCoordinateInput input, CadUnitResolution? units, CadNumericPolicy policy) =>
        Normalize(input,units,policy,CadCoordinateKind.Vector);

    private static CadCoordinateResult Normalize(CadCoordinateInput input, CadUnitResolution? units, CadNumericPolicy policy, CadCoordinateKind kind)
    {
        ArgumentNullException.ThrowIfNull(input); ArgumentNullException.ThrowIfNull(policy);
        CadCoordinateResult Fail(CadCoordinateDiagnosticCode code, string field, CadCoordinateStatus status = CadCoordinateStatus.Invalid) =>
            new(status,input,units,policy,null,null,null,[],[new(code,input.Source,field)]);
        var info = CadCoordinateRoles.Describe(input.Source.Role);
        if (!input.RoleSupported || info is null || info.Kind != kind)
            return Fail(CadCoordinateDiagnosticCode.UnsupportedCoordinateRole,"Role",CadCoordinateStatus.Unsupported);
        if (units is not null && (units.Source.Document.DrawingId != input.Source.Document.DrawingId
            || units.Source.Document.RevisionId != input.Source.Document.RevisionId || units.Source.Document.FileHash != input.Source.Document.FileHash))
            return Fail(CadCoordinateDiagnosticCode.MismatchedSourceAuthority,"Units");
        if (info.Dimensional && units?.Status != CadUnitStatus.Resolved)
            return Fail(units?.Status switch { CadUnitStatus.UnsupportedUnits => CadCoordinateDiagnosticCode.UnsupportedUnits,
                CadUnitStatus.InvalidUnits => CadCoordinateDiagnosticCode.InvalidUnits, _ => CadCoordinateDiagnosticCode.NeedsExplicitUnits },"Units",
                units?.Status == CadUnitStatus.InvalidUnits ? CadCoordinateStatus.Invalid : CadCoordinateStatus.Unsupported);
        var xyz = input.Coordinates;
        if (info.Optional && xyz.AllAbsent) return new(CadCoordinateStatus.Absent,input,units,policy,null,null,null,[],[]);
        var defaults = new List<string>();
        CadCoordinateDiagnosticCode? Check(CadCoordinateFact fact) => !fact.Valid
            ? fact.Absent || !fact.Mapped ? CadCoordinateDiagnosticCode.MissingRequiredCoordinateFact : CadCoordinateDiagnosticCode.InvalidSourceFact
            : !double.IsFinite(fact.Value!.Value) ? CadCoordinateDiagnosticCode.NonFiniteSourceCoordinate : null;
        if (Check(xyz.X) is {} errorX) return Fail(errorX,"X");
        if (Check(xyz.Y) is {} errorY) return Fail(errorY,"Y");
        double z;
        if (info.SeparateElevation)
        {
            if (xyz.Z.Mapped && !xyz.Z.Absent) return Fail(CadCoordinateDiagnosticCode.ConflictingCoordinateFacts,"Z/Elevation");
            if (input.Elevation.Absent) { z = 0; defaults.Add(ElevationDefaultZeroRule); }
            else if (Check(input.Elevation) is {} errorElevation) return Fail(errorElevation,"Elevation");
            else z = input.Elevation.Value!.Value;
        }
        else
        {
            if (input.Elevation.Mapped && !input.Elevation.Absent) return Fail(CadCoordinateDiagnosticCode.ConflictingCoordinateFacts,"Elevation");
            if (Check(xyz.Z) is {} errorZ) return Fail(errorZ,"Z");
            z = xyz.Z.Value!.Value;
        }
        var x = xyz.X.Value!.Value; var y = xyz.Y.Value!.Value;
        if (info.Dimensional)
        {
            var scale = units!.Decision.SourceToMillimeters!.Value;
            x *= scale; y *= scale; z *= scale;
        }
        if (!double.IsFinite(x) || !double.IsFinite(y) || !double.IsFinite(z))
            return Fail(CadCoordinateDiagnosticCode.NonFiniteNormalizedCoordinate,"UnitScale");
        if (!info.Dimensional && x == 0 && y == 0 && z == 0)
            return Fail(CadCoordinateDiagnosticCode.InvalidSourceFact,"Direction");
        CadOcsFrame? frame = null;
        if (info.Space == CadCoordinateSpace.Ocs)
        {
            var framed = CadArbitraryAxis.Create(CadExtrusionNormalizer.Normalize(input),policy);
            if (framed.Status != CadCoordinateStatus.Normalized)
                return new(framed.Status,input,units,policy,null,null,null,defaults,framed.Diagnostics);
            frame = framed.Frame!;
            if (frame.NormalEvidence.DefaultRule is {} rule) defaults.Add(rule);
        }
        try
        {
            var vector = new CadVector3(x,y,z);
            if (kind == CadCoordinateKind.Point)
            {
                var point = new Point3(vector.X,vector.Y,vector.Z);
                if (frame is not null) point = frame.PointToWcs(point);
                return new(CadCoordinateStatus.Normalized,input,units,policy,point,null,frame,defaults,[]);
            }
            if (frame is not null) vector = frame.VectorToWcs(vector);
            return new(CadCoordinateStatus.Normalized,input,units,policy,null,vector,frame,defaults,[]);
        }
        catch (InvalidDataException) { return Fail(CadCoordinateDiagnosticCode.NonFiniteNormalizedCoordinate,"Basis"); }
    }
}
