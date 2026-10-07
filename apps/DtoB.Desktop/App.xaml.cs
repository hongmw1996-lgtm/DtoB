using System.Windows;

namespace DtoB.Desktop;
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var window = new MainWindow(); MainWindow = window; window.Show();
        // Reproducible host verification uses the same UI/client path as the PING button.
        if (e.Args.Length == 4 && e.Args[0] == "--verify-pid" && int.TryParse(e.Args[1], out var pid) && e.Args[2] == "--evidence")
            window.Loaded += async (_, _) => await window.VerifyAsync(pid, e.Args[3]);
    }
}
