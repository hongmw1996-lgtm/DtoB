using Autodesk.Revit.UI;
using System.IO;
using DtoB.Ipc;

namespace DtoB.Revit2025;

public sealed class ConnectorApplication : IExternalApplication
{
    private PingServer? server;
    private readonly object logLock = new();
    private string logPath = "";
    public Result OnStartup(UIControlledApplication application)
    {
        try
        {
            var app = application.ControlledApplication;
            var host = new HostIdentity(Environment.ProcessId, "Revit", app.VersionNumber, app.VersionBuild);
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DtoB", "logs");
            Directory.CreateDirectory(directory); logPath = Path.Combine(directory, $"revit-{DateTime.UtcNow:yyyyMMddTHHmmssfff}-{host.ProcessId}.log");
            foreach (var old in new DirectoryInfo(directory).GetFiles("revit-*.log").OrderByDescending(f => f.LastWriteTimeUtc).Skip(20)) old.Delete();
            server = new PingServer(host, Log, TimeSpan.FromSeconds(3)); server.Start();
            Log($"STARTED Revit={host.Version} Build={host.Build} PID={host.ProcessId} endpoint={PipeProtocol.Endpoint(host.ProcessId)}");
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            Log($"STARTUP FAILED {ex}");
            TaskDialog.Show("DtoB connector startup failed", ex.Message);
            return Result.Failed;
        }
    }
    public Result OnShutdown(UIControlledApplication application)
    {
        try { server?.DisposeAsync().AsTask().GetAwaiter().GetResult(); Log("STOPPED"); return Result.Succeeded; }
        catch (Exception ex) { Log($"SHUTDOWN FAILED {ex}"); return Result.Failed; }
    }
    private void Log(string message)
    {
        lock (logLock)
        {
            try { File.AppendAllText(logPath, $"{DateTimeOffset.UtcNow:O} {message}{Environment.NewLine}"); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            { System.Diagnostics.Trace.TraceError($"DtoB log failure: {ex.Message}; original event: {message}"); }
        }
    }
}
