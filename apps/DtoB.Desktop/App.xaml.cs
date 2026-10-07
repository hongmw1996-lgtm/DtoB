using System.IO;
using System.Windows;
using DtoB.Project;
namespace DtoB.Desktop;
public partial class App:Application
{
    private FileLogger? logger;private bool fatal;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException+=(_,args)=>{args.Handled=true;Fatal(args.Exception);};
        AppDomain.CurrentDomain.UnhandledException+=(_,args)=>{logger?.Write(LogLevel.Fatal,"Unhandled process error",args.ExceptionObject as Exception);};
        try
        {
            if(e.Args.Length!=0)
            {
                if(!(e.Args.Length==4&&e.Args[0]=="--verify-pid"&&int.TryParse(e.Args[1],out var pid)&&pid>0&&e.Args[2]=="--evidence"&&!string.IsNullOrWhiteSpace(e.Args[3])))
                {MessageBox.Show("Usage: --verify-pid <positive PID> --evidence <file>","DtoB verification arguments");Shutdown(2);return;}
                var connection=new ConnectionWindow();MainWindow=connection;connection.Loaded+=async(_,_)=>await connection.VerifyAsync(int.Parse(e.Args[1]),e.Args[3]);connection.Show();return;
            }
            var root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"DtoB");Directory.CreateDirectory(root);
            var notices=new List<string>();logger=new FileLogger(Path.Combine(root,"desktop-logs"),message=>{if(MainWindow!=null)Dispatcher.Invoke(()=>MessageBox.Show(message,"DtoB logging warning"));else notices.Add(message);});
            var store=new SettingsStore(Path.Combine(root,"settings.json"));var settings=AppSettings.Defaults(root);
            if(File.Exists(Path.Combine(root,"settings.json")))try{settings=store.Load();}catch(Exception ex) when(LocalFileErrors.IsExpected(ex)){notices.Add("Settings could not be loaded; defaults are in use. Original file retained. "+ex.Message);logger.Write(LogLevel.Warning,"Settings load failed",ex);}
            logger.Minimum=settings.LogLevel;
            var recent=new RecentStore(Path.Combine(root,"recent-projects.json"));if(File.Exists(Path.Combine(root,"recent-projects.json")))try{recent.Load();}catch(Exception ex) when(LocalFileErrors.IsExpected(ex)){notices.Add("Recent projects could not be loaded. Original file retained. "+ex.Message);logger.Write(LogLevel.Warning,"Recent load failed",ex);}
            var window=new MainWindow(new ShellViewModel(new ProjectSession(new ProjectStore())),recent,store,settings,logger);MainWindow=window;window.Show();logger.Write(LogLevel.Info,"Desktop started.");foreach(var notice in notices)MessageBox.Show(window,notice,"DtoB warning",MessageBoxButton.OK,MessageBoxImage.Warning);
        }
        catch(Exception ex){Fatal(ex);}
    }
    private void Fatal(Exception ex){if(fatal)return;fatal=true;logger?.Write(LogLevel.Fatal,"Desktop fatal error",ex);MessageBox.Show("DtoB must close because of an unexpected error. Unsaved changes may be lost.\n"+ex.Message,"DtoB fatal error",MessageBoxButton.OK,MessageBoxImage.Error);Shutdown(1);}
}
