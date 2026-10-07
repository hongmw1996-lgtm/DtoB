using System.IO;
using System.Text.Json;
using System.Windows;
using DtoB.Ipc;

namespace DtoB.Desktop;
public partial class MainWindow : Window
{
    private readonly CancellationTokenSource closing = new();
    public MainWindow() { InitializeComponent(); Closed += (_, _) => closing.Cancel(); }
    private async void Ping_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(ProcessId.Text, out var pid) || pid <= 0) { ConnectionStatus.Text = "Error · enter a positive Revit PID."; return; }
        await VerifyAsync(pid);
    }
    public async Task VerifyAsync(int pid, string? evidencePath = null)
    {
        ProcessId.Text = pid.ToString(); PingButton.IsEnabled = false; ConnectionStatus.Text = "Connecting…";
        try
        {
            var response = await new PingClient().PingAsync(pid, TimeSpan.FromSeconds(5), closing.Token);
            if (closing.IsCancellationRequested) return;
            ConnectionStatus.Text = $"Connected · PONG from Revit {response.Host.Version}\nBuild {response.Host.Build} · PID {pid}\nRequest {response.RequestId}";
            if (evidencePath != null) await File.WriteAllTextAsync(evidencePath, JsonSerializer.Serialize(new { utc = DateTimeOffset.UtcNow, desktopPid = Environment.ProcessId, response }, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (OperationCanceledException) when (closing.IsCancellationRequested) { }
        catch (Exception ex)
        {
            if (closing.IsCancellationRequested) return;
            ConnectionStatus.Text = $"Disconnected / error · {ex.Message}";
            if (evidencePath != null)
            {
                try { await File.WriteAllTextAsync(evidencePath, JsonSerializer.Serialize(new { utc = DateTimeOffset.UtcNow, error = ex.ToString() })); }
                catch (Exception writeError) when (writeError is IOException or UnauthorizedAccessException)
                { ConnectionStatus.Text += $"\nEvidence write failed: {writeError.Message}"; }
            }
        }
        finally { if (!closing.IsCancellationRequested) PingButton.IsEnabled = true; }
    }
}
