using DtoB.Cad.Raw;
using DtoB.Cad.V2;
using DtoB.Cad.Normalize.Scalars;

namespace DtoB.Cad.Normalize.Coordinates;

/// <summary>Projects one actual field/vertex from one published snapshot; never expands entities.</summary>
public static class RawCoordinateAccess
{
    public static CadCoordinateInput Record(RawDrawingSnapshot snapshot, RawDrawingRecordView row,
        CadCoordinateRole role, int vertexOrdinal = 0, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var doc = RawScalarAccess.HeaderUnits(snapshot).Source.Document;
        CadContract.Require(row.StructuralRecordIndex >= 0 && row.StructuralRecordIndex < snapshot.Records.Count
            && ReferenceEquals(snapshot.Records[row.StructuralRecordIndex],row),"Coordinate row must belong to snapshot.");
        var span = row.SourceRecord.Source;
        var locator = new CadSourceLocator(CadSourceRecordDomain.StructuralRecord,row.StructuralRecordIndex,
            span.StartPairIndex,span.EndPairIndex,span.StartLine,span.EndLine);
        var g = row.Geometry?.Payload; var a = row.BlockAnnotation?.Payload;
        var text = a switch { RawText t => t, RawAttrib t => t.Text, RawAttDef t => t.Text, _ => null };
        RawCoordinateFacts? point = role switch
        {
            CadCoordinateRole.LineStartWcs when g is RawLine line => line.Start,
            CadCoordinateRole.LineEndWcs when g is RawLine line => line.End,
            CadCoordinateRole.PointPositionWcs when g is RawPoint p => p.Position,
            CadCoordinateRole.CircleCenterOcs when g is RawCircle c => c.Center,
            CadCoordinateRole.ArcCenterOcs when g is RawArc arc => arc.Center,
            CadCoordinateRole.SolidFirstOcs when g is RawSolid s => s.First,
            CadCoordinateRole.SolidSecondOcs when g is RawSolid s => s.Second,
            CadCoordinateRole.SolidThirdOcs when g is RawSolid s => s.Third,
            CadCoordinateRole.SolidFourthOcs when g is RawSolid s => s.Fourth,
            CadCoordinateRole.TextInsertionOcs when text is not null => text.Insertion,
            CadCoordinateRole.TextAlignmentOcs when text is not null => text.Alignment,
            CadCoordinateRole.InsertInsertionOcs when a is RawInsert insert => insert.Insertion,
            CadCoordinateRole.MTextInsertionWcs when a is RawMText mt => mt.Insertion,
            CadCoordinateRole.MTextDirectionWcs when a is RawMText mt => mt.Direction,
            CadCoordinateRole.DimensionDefinitionWcs when a is RawDimension d && Supported(d) => d.DefinitionPoint,
            CadCoordinateRole.DimensionFirstExtensionWcs when a is RawDimension d && Supported(d) => d.FirstExtensionPoint,
            CadCoordinateRole.DimensionSecondExtensionWcs when a is RawDimension d && Supported(d) => d.SecondExtensionPoint,
            CadCoordinateRole.DimensionTextMidpointOcs when a is RawDimension d && Supported(d) => d.TextMidpoint,
            _ => null
        };
        var extrusion = row.Geometry?.Extrusion ?? a switch
        {
            RawInsert insert => insert.Extrusion, RawMText mt => mt.Extrusion, RawDimension d => d.Extrusion,
            _ => text?.Extrusion
        };
        var source = new CadCoordinateEvidence(doc,role,locator,row.Identity?.OriginalHandle.Text,row.Identity?.CanonicalHandle,
            role == CadCoordinateRole.LwPolylineVertexOcs ? vertexOrdinal : null);
        if (role == CadCoordinateRole.LwPolylineVertexOcs && g is RawLwPolyline poly
            && vertexOrdinal >= 0 && vertexOrdinal < poly.Vertices.Count)
        {
            var vertex = poly.Vertices[vertexOrdinal];
            return new(source,new(vertex.X,vertex.Y),extrusion,poly.Elevation,true);
        }
        return new(source,new(point),extrusion,null,point is not null);
    }
    private static bool Supported(RawDimension d) => d.SubtypeProfileSupported && d.TypeFlags.Validity == DxfFactValidity.Valid
        && d.TypeFlags.Integer is int flags && (flags & 7) is 0 or 1;
}
