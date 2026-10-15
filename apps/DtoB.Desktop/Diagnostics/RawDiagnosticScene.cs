using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using DtoB.Cad.Raw;

namespace DtoB.Desktop.Diagnostics;

public enum RawDiagnosticViewMode { Entities, BlockDefinition }
public sealed record DisplayXY(double X, double Y);
public sealed record DisplayBounds(double MinX, double MinY, double MaxX, double MaxY);
public sealed record DiagnosticBlockChoice(int Index, string Name)
{
    public override string ToString() => Name;
}
public sealed class RawDiagnosticSceneItem
{
    [JsonIgnore] public RawDrawingRecordView Record { get; }
    public int RecordIndex => Record.StructuralRecordIndex;
    public string Kind { get; }
    public IReadOnlyList<DisplayXY> Points { get; }
    public double? Radius { get; }
    public double? StartDegrees { get; }
    public double? EndDegrees { get; }
    public bool Closed { get; }
    public string Notice { get; }
    public RawDiagnosticSceneItem(RawDrawingRecordView record, string kind, IEnumerable<DisplayXY> points,
        string notice = "", bool closed = false, double? radius = null, double? start = null, double? end = null)
    { Record = record; Kind = kind; Points = Array.AsReadOnly(points.ToArray()); Notice = notice; Closed = closed; Radius = radius; StartDegrees = start; EndDegrees = end; }
}
// SCREEN presentation only. No normalized units, occurrence placement or downstream consumers.
public sealed class RawDiagnosticScene
{
    public RawDiagnosticViewMode Mode { get; }
    public int? BlockIndex { get; }
    public IReadOnlyList<RawDiagnosticSceneItem> Items { get; }
    public DisplayXY? BlockBasePoint { get; }
    public DisplayBounds? Bounds { get; }
    public string Notice => (Mode == RawDiagnosticViewMode.Entities ? "ENTITIES source XY only." : "Raw block-definition coordinates — no occurrence transform applied.") +
        "  2D XY: Z/extrusion/thickness are not spatially transformed. INSERT/annotations are anchors only; bulges are chords.";
    private RawDiagnosticScene(RawDiagnosticViewMode mode, int? blockIndex, IEnumerable<RawDiagnosticSceneItem> items, DisplayXY? basePoint)
    {
        Mode = mode; BlockIndex = blockIndex; Items = Array.AsReadOnly(items.ToArray()); BlockBasePoint = basePoint;
        var points = new List<DisplayXY>();
        foreach (var item in Items)
        {
            if (item.Radius is { } radius && item.Points.Count > 0)
            {
                var c = item.Points[0];
                // A finite center alone is not a drawable circle bound if center±radius overflows.
                if (!double.IsFinite(c.X - radius) || !double.IsFinite(c.X + radius) || !double.IsFinite(c.Y - radius) || !double.IsFinite(c.Y + radius)) continue;
                points.Add(new(c.X - radius, c.Y - radius)); points.Add(new(c.X + radius, c.Y + radius));
            }
            points.AddRange(item.Points);
        }
        if (points.Count > 0)
        {
            var minX = points.Min(p => p.X); var minY = points.Min(p => p.Y); var maxX = points.Max(p => p.X); var maxY = points.Max(p => p.Y);
            if (double.IsFinite(maxX - minX) && double.IsFinite(maxY - minY)) Bounds = new(minX, minY, maxX, maxY);
        }
    }
    public static IReadOnlyList<DiagnosticBlockChoice> Blocks(RawDrawingSnapshot s) => Array.AsReadOnly(s.BlockDefinitions.Select((b, i) =>
        new DiagnosticBlockChoice(i, ((RawBlockBegin)s.Records[b.BeginRecordIndex].BlockAnnotation!.Payload!).Name.Text ?? "(unnamed)")).ToArray());
    public static RawDiagnosticScene Build(RawDrawingSnapshot snapshot, RawDiagnosticViewMode mode, int? blockIndex = null)
    {
        IEnumerable<RawDrawingRecordView> records; DisplayXY? basePoint = null;
        if (mode == RawDiagnosticViewMode.Entities) records = snapshot.Records.Where(r => r.SourceSection == "ENTITIES");
        else
        {
            if (blockIndex == null || blockIndex < 0 || blockIndex >= snapshot.BlockDefinitions.Count) return new(mode, blockIndex, [], null);
            var block = snapshot.BlockDefinitions[blockIndex.Value];
            records = block.Children.Select(child => snapshot.Records[child.StructuralRecordIndex]);
            basePoint = XY(((RawBlockBegin)snapshot.Records[block.BeginRecordIndex].BlockAnnotation!.Payload!).BasePoint);
        }
        return new(mode, blockIndex, records.Select(Project).OfType<RawDiagnosticSceneItem>(), basePoint);
    }
    private static DisplayXY? XY(RawCoordinateFacts p) => p.X.Validity == DxfFactValidity.Valid && p.Y.Validity == DxfFactValidity.Valid &&
        p.X.Real is { } x && p.Y.Real is { } y && double.IsFinite(x) && double.IsFinite(y) ? new(x, y) : null;
    private static RawDiagnosticSceneItem? Project(RawDrawingRecordView r)
    {
        if (r.SemanticDisposition != DxfDisposition.Supported) return null;
        RawDiagnosticSceneItem? Item(string kind, IEnumerable<RawCoordinateFacts> positions, string notice = "", bool closed = false, double? radius = null, double? start = null, double? end = null)
        { var points = positions.Select(XY).ToArray(); return points.Length > 0 && points.All(p => p != null) ? new(r, kind, points.Select(p => p!), notice, closed, radius, start, end) : null; }
        switch (r.Geometry?.Payload)
        {
            case RawLine p: return Item("LINE", [p.Start, p.End]);
            case RawPoint p: return Item("POINT", [p.Position]);
            case RawSolid p: return Item("SOLID", [p.First, p.Second, p.Third, p.Fourth], "Raw corner-slot order; no topology interpretation.", true);
            case RawCircle p when p.Radius.Real is > 0: return Item("CIRCLE", [p.Center], radius: p.Radius.Real);
            case RawArc p when p.Radius.Real is > 0: return Item("ARC", [p.Center], "Source degrees; view-only arc presentation.", radius: p.Radius.Real, start: p.StartAngleDegrees.Real, end: p.EndAngleDegrees.Real);
            case RawLwPolyline p:
                var hasBulge = p.Vertices.Any(v => v.Bulge.Real is { } b && b != 0);
                return Item("LWPOLYLINE", p.Vertices.Select(v => new RawCoordinateFacts(v.X, v.Y, p.Elevation)), hasBulge ? "NONZERO BULGE: chord display only; curved geometry not interpreted in03H." : "Raw vertex sequence.", (p.Flags.Integer.GetValueOrDefault() & 1) != 0);
        }
        switch (r.BlockAnnotation?.Payload)
        {
            case RawInsert p: return Item("INSERT", [p.Insertion], "Anchor only: " + p.TargetName.Text + "; no occurrence expansion.");
            case RawText p: return Item("TEXT", [p.Insertion], "Anchor only; raw content in Inspector.");
            case RawMText p: return Item("MTEXT", [p.Insertion], "Anchor only; raw chunks in Inspector.");
            case RawDimension p: return Item("DIMENSION", [p.DefinitionPoint], "Diagnostic definition point only; no measurement/graphics calculation.");
        }
        return null;
    }
    public static string Summary(RawDrawingSnapshot s)
    {
        int Count(DxfDisposition d) => s.SemanticDispositionCounts.Where(x => x.Disposition == d).Sum(x => x.Count);
        var c = s.Completeness;
        return FormattableString.Invariant($"{s.LexicalEvidence.SourceBytesConsumed} bytes · {s.LexicalEvidence.PhysicalLineCount} lines · {s.LexicalEvidence.Pairs.Count} pairs · {s.Sections.Count} sections · {s.Records.Count} children\nSupported {Count(DxfDisposition.Supported)} · Unsupported {Count(DxfDisposition.Unsupported)} · Coverage {c.Coverage} · Geometry {s.Geometry.Count} ·03D {s.BlockAnnotations.Count} · Definitions {s.BlockDefinitions.Count}\nIdentity {s.IdentityNodes.Count} · References {s.References.Count} · Owners {s.Ownership.Count}\nSourceEvidenceComplete={c.SourceEvidenceComplete}; TypedMappingComplete={c.TypedMappingComplete}; GraphValidated={c.GraphValidated}; IdentityCoverageComplete={c.IdentityCoverageComplete}; ReferenceResolutionComplete={c.ReferenceResolutionComplete}; SnapshotAssemblyComplete={c.SnapshotAssemblyComplete}");
    }
    public static string Inspect(RawDrawingSnapshot s, RawDrawingRecordView r)
    {
        var text = new StringBuilder();
        text.AppendLine($"Record {r.StructuralRecordIndex} · {r.SourceSection} · {r.SourceRecord.RecordType} · {r.SemanticDisposition}");
        text.AppendLine($"OriginalHandle: {r.Identity?.OriginalHandle.Text ?? "(not applicable)"} · CanonicalHandle: {r.Identity?.CanonicalHandle ?? "(none)"}");
        text.AppendLine($"Source pairs {r.SourceRecord.Source.StartPairIndex}..{r.SourceRecord.Source.EndPairIndex}; physical lines {r.SourceRecord.Source.StartLine}..{r.SourceRecord.Source.EndLine}");
        text.AppendLine("Native values only; absent means absent. Canvas uses XY; Z/extrusion/thickness are not transformed.");
        var fields = r.SourceRecord.Fields.Concat(r.Geometry?.Fields ?? []).Concat(r.BlockAnnotation?.Fields ?? []);
        foreach (var f in fields)
        {
            var fact = f.Fact;
            text.AppendLine($"{f.Name}: {fact.Presence}/{fact.Validity}; text={fact.Text}; integer={fact.Integer?.ToString(CultureInfo.InvariantCulture)}; real={fact.Real?.ToString("R", CultureInfo.InvariantCulture)}; pairs=[{string.Join(",", fact.SourcePairIndices)}]");
        }
        foreach (var edge in s.References.Where(e => e.SourceLocator.Kind == DxfGraphRecordKind.StructuralRecord && e.SourceLocator.Index == r.StructuralRecordIndex))
            text.AppendLine($"Reference {edge.ContextEvidence.Role}: {edge.OriginalTargetHandle} / {edge.Resolution}; pair {edge.Source.StartPairIndex}");
        var relevant = s.Diagnostics.Select(s.ResolveDiagnostic).Where(d => d switch
        { DxfStructuralDiagnostic x => Overlaps(x.Source), DxfGeometryDiagnostic x => Overlaps(x.Source), DxfBlockAnnotationDiagnostic x => Overlaps(x.Source), DxfGraphDiagnostic x => Overlaps(x.Source), _ => false }).ToArray();
        text.AppendLine($"Located diagnostics: {relevant.Length}");
        foreach (var d in relevant) text.AppendLine(d.ToString());
        text.AppendLine("Payload → SourceFact → pair → code/value physical lines → raw source (all mapped AND unmapped pairs):");
        for (var i = r.SourceRecord.Source.StartPairIndex; i <= r.SourceRecord.Source.EndPairIndex; i++)
        {
            var p = s.LexicalEvidence.Pairs[i];
            var names = fields.Where(f => f.Fact.SourcePairIndices.Contains(i)).Select(f => f.Name);
            text.AppendLine($"pair {i} [{string.Join(",", names)}] lines {p.CodeLineNumber}/{p.ValueLineNumber} code '{p.RawCodeText}' → '{p.RawTextValue}'");
        }
        return text.ToString();
        bool Overlaps(DxfSourceSpan? span) => span != null && span.StartPairIndex <= r.SourceRecord.Source.EndPairIndex && span.EndPairIndex >= r.SourceRecord.Source.StartPairIndex;
    }
}

// Camera values are UI pixels, never source units or normalization output.
public sealed class DiagnosticCamera
{
    public double Scale { get; private set; } = 1;
    public double OffsetX { get; private set; }
    public double OffsetY { get; private set; }
    public DisplayXY Project(DisplayXY p) => new(p.X * Scale + OffsetX, -p.Y * Scale + OffsetY);
    public bool Fit(DisplayBounds? bounds, double width, double height)
    {
        Reset(); if (bounds == null || width <= 48 || height <= 48) return false;
        var dx = bounds.MaxX - bounds.MinX; var dy = bounds.MaxY - bounds.MinY;
        Scale = Math.Min(dx > 0 ? (width - 48) / dx : double.MaxValue, dy > 0 ? (height - 48) / dy : double.MaxValue);
        if (Scale == double.MaxValue) Scale = 1;
        if (!double.IsFinite(Scale) || Scale <= 0) { Reset(); return false; }
        OffsetX = width / 2 - (bounds.MinX + dx / 2) * Scale;
        OffsetY = height / 2 + (bounds.MinY + dy / 2) * Scale;
        if (!double.IsFinite(OffsetX) || !double.IsFinite(OffsetY)) { Reset(); return false; }
        return true;
    }
    public void Reset() { Scale = 1; OffsetX = OffsetY = 0; }
    public void Pan(double x, double y) { if (double.IsFinite(OffsetX + x) && double.IsFinite(OffsetY + y)) { OffsetX += x; OffsetY += y; } }
    public void Zoom(double factor, double x, double y)
    {
        var scale = Scale * factor; var ox = x + (OffsetX - x) * factor; var oy = y + (OffsetY - y) * factor;
        if (factor > 0 && double.IsFinite(scale) && scale > 0 && double.IsFinite(ox) && double.IsFinite(oy)) { Scale = scale; OffsetX = ox; OffsetY = oy; }
    }
}
