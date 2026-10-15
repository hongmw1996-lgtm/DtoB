using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace DtoB.Desktop.Diagnostics;

// WPF pixels only. No source coordinate rewrite or PHASE04 transformation utility.
public sealed class DiagnosticViewport : FrameworkElement
{
    public DiagnosticCamera Camera { get; } = new();
    public RawDiagnosticScene? Scene { get; set; }
    public RawDiagnosticSceneItem? BaseMarker { get; set; }
    public int? SelectedRecordIndex { get; set; }
    public event Action<int>? RecordSelected;
    public event Action? CameraChanged;
    private readonly List<(RawDiagnosticSceneItem Item, Geometry Shape)> hits = [];
    private Point? drag;
    private bool moved;
    public DiagnosticViewport() { ClipToBounds = true; Focusable = true; Cursor = Cursors.Cross; }
    public bool Fit()
    {
        var fitted = Camera.Fit(Scene?.Bounds, ActualWidth, ActualHeight); InvalidateVisual(); CameraChanged?.Invoke(); return fitted;
    }
    public void Reset() { Camera.Reset(); InvalidateVisual(); CameraChanged?.Invoke(); }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc); dc.DrawRectangle(Brushes.WhiteSmoke, null, new Rect(RenderSize)); hits.Clear();
        if (Scene?.Bounds == null) return;
        foreach (var item in Scene.Items) Draw(dc, item);
        if (BaseMarker != null) Draw(dc, BaseMarker);
    }
    private void Draw(DrawingContext dc, RawDiagnosticSceneItem item)
    {
        var points = item.Points.Select(p => Camera.Project(p)).ToArray();
        // Refuse unrepresentable screen arithmetic; this is not a source tolerance.
        // Finite UI clipping guard in pixels, never a geometric tolerance or source exclusion.
        if (points.Any(p => !double.IsFinite(p.X) || !double.IsFinite(p.Y) || Math.Abs(p.X) > 10_000_000 || Math.Abs(p.Y) > 10_000_000)) return;
        Point P(DisplayXY p) => new(p.X, p.Y);
        Geometry shape;
        if (item.Kind is "CIRCLE" or "ARC")
        {
            var radius = item.Radius!.Value * Camera.Scale;
            if (!double.IsFinite(radius) || radius <= 0 || radius > 10_000_000) return;
            if (item.Kind == "CIRCLE") shape = new EllipseGeometry(P(points[0]), radius, radius);
            else
            {
                if (item.StartDegrees == null || item.EndDegrees == null) return;
                // View-only source-degree to WPF screen presentation, never normalized domain data.
                var start = item.StartDegrees.Value % 360; var end = item.EndDegrees.Value % 360;
                var sweep = (end - start + 360) % 360;
                Point At(double degrees) { var a = degrees * Math.PI / 180; return new(points[0].X + radius * Math.Cos(a), points[0].Y - radius * Math.Sin(a)); }
                var path = new StreamGeometry(); using (var g = path.Open())
                { g.BeginFigure(At(start), false, false); g.ArcTo(At(end), new Size(radius, radius), 0, sweep > 180, SweepDirection.Counterclockwise, true, false); }
                shape = path;
            }
        }
        else if (points.Length == 1)
        {
            var p = P(points[0]); shape = new GeometryGroup { Children = { new LineGeometry(new(p.X - 5, p.Y), new(p.X + 5, p.Y)), new LineGeometry(new(p.X, p.Y - 5), new(p.X, p.Y + 5)) } };
        }
        else
        {
            var path = new StreamGeometry(); using (var g = path.Open())
            { g.BeginFigure(P(points[0]), false, item.Closed); g.PolyLineTo(points.Skip(1).Select(P).ToArray(), true, false); }
            shape = path;
        }
        shape.Freeze();
        var color = SelectedRecordIndex == item.RecordIndex ? Brushes.OrangeRed : item.Kind == "INSERT" ? Brushes.DarkViolet : item.Kind is "TEXT" or "MTEXT" or "DIMENSION" ? Brushes.DarkGreen : item.Kind == "BLOCK_BASE" ? Brushes.Red : Brushes.SteelBlue;
        dc.DrawGeometry(null, new Pen(color, SelectedRecordIndex == item.RecordIndex ? 3 : 1.5), shape); hits.Add((item, shape));
    }
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    { var p = e.GetPosition(this); Camera.Zoom(e.Delta > 0 ? 1.2 : 1 / 1.2, p.X, p.Y); InvalidateVisual(); CameraChanged?.Invoke(); e.Handled = true; }
    protected override void OnMouseDown(MouseButtonEventArgs e)
    { if (e.ChangedButton is MouseButton.Left or MouseButton.Middle) { Focus(); drag = e.GetPosition(this); moved = false; CaptureMouse(); e.Handled = true; } }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (drag is not { } previous) return;
        var p = e.GetPosition(this); var delta = p - previous;
        if (delta.Length > 0) { moved = true; Camera.Pan(delta.X, delta.Y); drag = p; InvalidateVisual(); CameraChanged?.Invoke(); }
    }
    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        if (drag == null) return; var p = e.GetPosition(this); drag = null; ReleaseMouseCapture();
        if (!moved && e.ChangedButton == MouseButton.Left)
        {
            var hit = hits.AsEnumerable().Reverse().FirstOrDefault(h => h.Shape.StrokeContains(new Pen(Brushes.Black, 12), p));
            if (hit.Item != null) RecordSelected?.Invoke(hit.Item.RecordIndex);
        }
        e.Handled = true;
    }
    protected override void OnLostMouseCapture(MouseEventArgs e) { drag = null; base.OnLostMouseCapture(e); }
}
