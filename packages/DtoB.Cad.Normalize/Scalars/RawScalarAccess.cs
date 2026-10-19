using System.Globalization;
using DtoB.Cad.Raw;
using DtoB.Cad.V2;

namespace DtoB.Cad.Normalize.Scalars;

/// <summary>Read-only field-role projection from published source evidence. No spatial normalization.</summary>
public static class RawScalarAccess
{
    private static CadSourceDocument Document(RawDrawingSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var c = snapshot.Completeness;
        CadContract.Require(c.SnapshotAssemblyComplete && c.LexicalSucceeded && c.StructureValidated
            && c.GraphValidated && c.ReferenceResolutionComplete && c.SourceEvidenceComplete, "Published source evidence required.");
        var p = snapshot.DocumentProvenance;
        CadContract.Require(p is not null, "Document provenance required.");
        return new(Guid.Parse(p!.DrawingId!), Guid.Parse(p.RevisionId!), p.FileHash!);
    }
    public static CadScalarFact HeaderUnits(RawDrawingSnapshot snapshot) =>
        new(Document(snapshot), CadScalarRole.HeaderInsertionUnits, snapshot.Header?.InsertionUnits);

    public static CadScalarFact Record(RawDrawingSnapshot snapshot, RawDrawingRecordView record,
        CadScalarRole role, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var doc = Document(snapshot);
        CadContract.Require(record.StructuralRecordIndex >= 0 && record.StructuralRecordIndex < snapshot.Records.Count
            && ReferenceEquals(snapshot.Records[record.StructuralRecordIndex], record), "Record must belong to this snapshot.");
        var source = record.SourceRecord; var span = source.Source;
        var locator = new CadSourceLocator(CadSourceRecordDomain.StructuralRecord, record.StructuralRecordIndex,
            span.StartPairIndex, span.EndPairIndex, span.StartLine, span.EndLine);
        var g = record.Geometry?.Payload; var a = record.BlockAnnotation?.Payload;
        var text = a switch { RawText t => t, RawAttrib t => t.Text, RawAttDef t => t.Text, _ => null };
        DxfSourceFact? fact = role switch
        {
            CadScalarRole.BlockInsertionUnits when source.Scope == "TABLES/BLOCK_RECORD" => Field(source, "Flags"),
            CadScalarRole.StyleObliqueAngle when source.Scope == "TABLES/STYLE" => Field(source, "ObliqueAngle"),
            CadScalarRole.ArcStartAngle when g is RawArc arc => arc.StartAngleDegrees,
            CadScalarRole.ArcEndAngle when g is RawArc arc => arc.EndAngleDegrees,
            CadScalarRole.ArcRadius when g is RawArc arc => arc.Radius,
            CadScalarRole.CircleRadius when g is RawCircle circle => circle.Radius,
            CadScalarRole.PointDisplayAngle when g is RawPoint point => point.AngleDegrees,
            CadScalarRole.PolylineElevation when g is RawLwPolyline poly => poly.Elevation,
            CadScalarRole.PolylineConstantWidth when g is RawLwPolyline poly => poly.ConstantWidth,
            CadScalarRole.InsertRotation when a is RawInsert insert => insert.RotationDegrees,
            CadScalarRole.InsertColumnSpacing when a is RawInsert insert => insert.ColumnSpacing,
            CadScalarRole.InsertRowSpacing when a is RawInsert insert => insert.RowSpacing,
            CadScalarRole.TextRotation when text is not null => text.RotationDegrees,
            CadScalarRole.TextHeight when text is not null => text.Height,
            CadScalarRole.TextHeight when a is RawMText mt => mt.Height,
            CadScalarRole.MTextWidth when a is RawMText mt => mt.RectangleWidth,
            CadScalarRole.LinearDimensionRotation when a is RawDimension d && IsLinear(d) => d.RotationDegrees,
            CadScalarRole.LinearDimensionMeasurement when a is RawDimension d && IsLinear(d) => d.SourceMeasurement,
            _ => null
        };
        var supported = fact is not null;
        if (role == CadScalarRole.MTextRotation && a is RawMText)
            (fact, supported) = MText(snapshot, source, cancellationToken);
        return new(doc, role, fact, locator, record.Identity?.OriginalHandle.Text,
            record.Identity?.CanonicalHandle, supported);
    }
    private static bool IsLinear(RawDimension d) => d.SubtypeProfileSupported
        && d.TypeFlags.Validity == DxfFactValidity.Valid && d.TypeFlags.Integer is int flags && (flags & 7) == 0;
    private static DxfSourceFact? Field(DxfStructuralRecord record, string name) =>
        record.Fields.SingleOrDefault(f => f.Name == name)?.Fact;

    private static (DxfSourceFact Fact, bool Supported) MText(RawDrawingSnapshot snapshot,
        DxfStructuralRecord record, CancellationToken token)
    {
        // MTEXT has no typed rotation in Raw. Inspect retained lexical/context facts only.
        // Direction precedence and column heights are deliberately outside this scalar profile.
        var markers = record.ContextMarkers.ToDictionary(m => m.PairIndex);
        string? subclass = null, application = null; var control = "";
        var rotations = new List<DxfRawPair>(); var ambiguous = false;
        for (var i = record.Source.StartPairIndex; i <= record.Source.EndPairIndex; i++)
        {
            token.ThrowIfCancellationRequested();
            if (markers.TryGetValue(i, out var marker))
            { subclass = marker.Subclass; application = marker.Application; control = marker.ControlPath; continue; }
            var pair = snapshot.LexicalEvidence.Pairs[i];
            if (application is not null || control.Length != 0) continue;
            var main = subclass is null or "AcDbMText";
            if (pair.GroupCode == 50) { rotations.Add(pair); if (!main) ambiguous = true; }
            if (main && pair.GroupCode is 11 or 21 or 31 or 75 or 76 or 78 or 79 or 48 or 49) ambiguous = true;
        }
        var first = rotations.FirstOrDefault(); var last = rotations.LastOrDefault();
        var valid = rotations.Count == 1 && double.TryParse(first!.RawTextValue, NumberStyles.Float,
            CultureInfo.InvariantCulture, out var value) && double.IsFinite(value);
        double? parsed = valid ? double.Parse(first!.RawTextValue, CultureInfo.InvariantCulture) : null;
        return (new(DxfFactKind.Real, first is null ? DxfFactPresence.Absent : DxfFactPresence.Present,
            first is null ? DxfFactValidity.NotApplicable : valid ? DxfFactValidity.Valid : DxfFactValidity.Invalid,
            null, null, parsed, rotations.Select(p => p.PairIndex), first is null ? null :
                new(first.PairIndex, last!.PairIndex, first.CodeLineNumber, last.ValueLineNumber)), !ambiguous && rotations.Count <= 1);
    }
}
