using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using DtoB.Cad.Raw;
using DtoB.Desktop.Diagnostics;

namespace DtoB.Desktop;

public partial class DiagnosticViewerWindow : Window
{
    private readonly DiagnosticRequest request;
    private readonly Func<bool> contextCurrent;
    private readonly DiagnosticRequestGate gate = new();
    private CancellationTokenSource? cancellation;
    private RawDrawingSnapshot? snapshot;
    private bool closed;
    public DiagnosticViewerWindow(DiagnosticRequest request, Func<bool> contextCurrent)
    {
        InitializeComponent(); this.request = request; this.contextCurrent = contextCurrent;
        Viewport.RecordSelected += SelectRecord; Viewport.CameraChanged += ShowCamera;
        Closed += (_, _) => { closed = true; InvalidateRequest(); };
    }
    public void InvalidateRequest()
    {
        gate.Next(); cancellation?.Cancel(); ClearScene();
        if (!closed) { State.Text = "Project/drawing changed. Close Viewer and reopen for the current selection."; RunButton.IsEnabled = false; CancelButton.IsEnabled = false; }
    }
    private void ClearScene()
    {
        snapshot = null; Viewport.Scene = null; Viewport.BaseMarker = null; Viewport.SelectedRecordIndex = null; Viewport.InvalidateVisual();
        Records.ItemsSource = null; Blocks.ItemsSource = null; Summary.Text = ""; Inspector.Text = ""; ModeNotice.Text = "";
        Diagnostics.ItemsSource = null; CameraStatus.Text = "";
        EmptyState.Text = "No successful extraction. No scene published."; EmptyState.Visibility = Visibility.Visible;
    }
    private async void Run_Click(object sender, RoutedEventArgs e)
    {
        if (cancellation != null || !contextCurrent()) { if (!contextCurrent()) InvalidateRequest(); return; }
        var epoch = gate.Next(); using var source = new CancellationTokenSource(); cancellation = source;
        ClearScene(); Diagnostics.ItemsSource = null; RunButton.IsEnabled = false; CancelButton.IsEnabled = true; State.Text = "Reading verified managed Text DXF…";
        try
        {
            var result = await new DiagnosticExtraction().ReadAsync(request, source.Token);
            if (closed || !gate.IsCurrent(epoch) || !contextCurrent()) return; // Applies to success AND failure.
            Diagnostics.ItemsSource = result.Issues;
            State.Text = $"{result.Stage} · {result.Status}";
            if (result.Snapshot is not { } value) { State.Text += " · No Snapshot/scene published."; return; }
            snapshot = value; Summary.Text = RawDiagnosticScene.Summary(value);
            Records.ItemsSource = value.Records.Select(r => new RecordChoice(r)).ToArray();
            Blocks.ItemsSource = RawDiagnosticScene.Blocks(value); ShowScene(RawDiagnosticViewMode.Entities);
        }
        catch (Exception ex)
        {
            // Last UI boundary: unexpected projection failures never become a valid empty scene.
            if (closed || !gate.IsCurrent(epoch) || !contextCurrent()) return;
            ClearScene(); State.Text = "Viewer failure · " + ex.Message;
            Diagnostics.ItemsSource = new[] { new DiagnosticIssue("Viewer", ex.GetType().Name, ex.Message, "") };
            System.Diagnostics.Trace.WriteLine(ex);
        }
        finally { cancellation = null; if (!closed) { RunButton.IsEnabled = contextCurrent(); CancelButton.IsEnabled = false; } }
    }
    private void ShowScene(RawDiagnosticViewMode mode, int? index = null)
    {
        if (snapshot == null) return;
        var scene = RawDiagnosticScene.Build(snapshot, mode, index); Viewport.Scene = scene; Viewport.BaseMarker = null;
        if (scene.BlockBasePoint is { } point && index.HasValue)
            Viewport.BaseMarker = new(snapshot.Records[snapshot.BlockDefinitions[index.Value].BeginRecordIndex], "BLOCK_BASE", [point], "Raw base point marker; no compensation applied.");
        ModeNotice.Text = scene.Notice + $"  {scene.Items.Count} diagnostic records.";
        EmptyState.Text = "No renderable child XY facts (empty or unrepresentable display bounds).";
        EmptyState.Visibility = scene.Bounds == null ? Visibility.Visible : Visibility.Collapsed;
        // Summary/notice can change viewport height. Fit after WPF completes that layout.
        _ = Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(() =>
        { if (!closed && snapshot != null && ReferenceEquals(Viewport.Scene, scene)) Viewport.Fit(); }));
        ShowSelection();
    }
    private void ShowCamera() => CameraStatus.Text = FormattableString.Invariant($"View only · Zoom {Viewport.Camera.Scale:G5} · Pan {Viewport.Camera.OffsetX:F1},{Viewport.Camera.OffsetY:F1}px · wheel: zoom; drag: pan; click: inspect");
    private sealed record RecordChoice(RawDrawingRecordView Record)
    {
        public override string ToString() => $"#{Record.StructuralRecordIndex} {Record.SourceSection} / {Record.SourceRecord.RecordType} · {Record.SemanticDisposition} · {Record.Identity?.OriginalHandle.Text}";
    }
    private void SelectRecord(int index)
    {
        if (Records.ItemsSource is not RecordChoice[] choices) return;
        Records.SelectedItem = choices.Single(r => r.Record.StructuralRecordIndex == index); Records.ScrollIntoView(Records.SelectedItem);
    }
    private void Record_Changed(object sender, SelectionChangedEventArgs e)
        => ShowSelection();
    private void ShowSelection()
    {
        if (snapshot == null || Records.SelectedItem is not RecordChoice choice) return;
        Viewport.SelectedRecordIndex = choice.Record.StructuralRecordIndex; Viewport.InvalidateVisual();
        var item = Viewport.Scene?.Items.FirstOrDefault(i => i.RecordIndex == choice.Record.StructuralRecordIndex)
            ?? (Viewport.BaseMarker?.RecordIndex == choice.Record.StructuralRecordIndex ? Viewport.BaseMarker : null);
        Inspector.Text = (item?.Notice ?? "No primitive in current spatial scope; source evidence remains inspectable.") + "\n" + RawDiagnosticScene.Inspect(snapshot, choice.Record);
    }
    private void Entities_Click(object sender, RoutedEventArgs e) { Blocks.SelectedItem = null; ShowScene(RawDiagnosticViewMode.Entities); }
    private void Block_Changed(object sender, SelectionChangedEventArgs e) { if (Blocks.SelectedItem is DiagnosticBlockChoice b) ShowScene(RawDiagnosticViewMode.BlockDefinition, b.Index); }
    private void Fit_Click(object sender, RoutedEventArgs e) => Viewport.Fit();
    private void Reset_Click(object sender, RoutedEventArgs e) => Viewport.Reset();
    private void Cancel_Click(object sender, RoutedEventArgs e) => cancellation?.Cancel();
}
