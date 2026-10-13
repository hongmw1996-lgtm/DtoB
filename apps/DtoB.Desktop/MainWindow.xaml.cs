using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using DtoB.Project;
namespace DtoB.Desktop;
public partial class MainWindow:Window
{
    private readonly ShellViewModel vm;private readonly RecentStore recent;private readonly SettingsStore settingsStore;private AppSettings settings;private readonly FileLogger logger;
    private CancellationTokenSource? operation;private bool closingAllowed;
    public MainWindow(ShellViewModel vm,RecentStore recent,SettingsStore settingsStore,AppSettings settings,FileLogger logger)
    {InitializeComponent();this.vm=vm;this.recent=recent;this.settingsStore=settingsStore;this.settings=settings;this.logger=logger;DataContext=vm;Refresh();Closing+=OnClosing;}
    private void Refresh(){vm.Refresh();RecentList.ItemsSource=recent.Entries.ToArray();}
    private void Error(string action,Exception ex){logger.Write(LogLevel.Error,action,ex);Status.Text=action+": "+ex.Message;MessageBox.Show(this,Status.Text,"DtoB error",MessageBoxButton.OK,MessageBoxImage.Error);}
    private async Task<bool> RunAsync(string action,Func<CancellationToken,Task> work)
    {
        if(operation!=null)return false;using var cancellation=new CancellationTokenSource();operation=cancellation;Commands.IsEnabled=DrawingCommands.IsEnabled=AssetCommands.IsEnabled=RecentList.IsEnabled=false;NameInput.IsReadOnly=true;CancelOperation.Visibility=Visibility.Visible;Status.Text=action+"…";
        try{await work(cancellation.Token);return true;}
        catch(OperationCanceledException){Status.Text="Operation cancelled; previous project state retained.";logger.Write(LogLevel.Info,"Operation cancelled.");return false;}
        catch(Exception ex) when(LocalFileErrors.IsExpected(ex)){Error(action,ex);return false;}
        finally{operation=null;Commands.IsEnabled=DrawingCommands.IsEnabled=AssetCommands.IsEnabled=RecentList.IsEnabled=true;NameInput.IsReadOnly=false;CancelOperation.Visibility=Visibility.Collapsed;Refresh();}
    }
    private void RecordRecent(){try{recent.Record(vm.Session.CurrentFilePath!,vm.Session.Project!.Name);}catch(Exception ex) when(LocalFileErrors.IsExpected(ex)){Error("Project succeeded, but recent state could not be saved",ex);}}
    private async Task<bool> SaveAsync(bool saveAs=false)
    {
        if(vm.Session.Project==null)return false;
        NameInput.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateSource();
        if(System.Windows.Controls.Validation.GetHasError(NameInput)){MessageBox.Show(this,"Enter a nonempty project name before saving.","Invalid project name");return false;}
        var path=saveAs?null:vm.Session.CurrentFilePath;
        if(path==null){var dialog=new SaveFileDialog{Filter="DtoB project (*.dtob)|*.dtob",DefaultExt=".dtob",AddExtension=true,InitialDirectory=settings.WorkingDirectory,FileName="Project.dtob"};if(dialog.ShowDialog(this)!=true)return false;path=dialog.FileName;}
        return await RunAsync("Save",async ct=>{await vm.Session.SaveAsync(path,ct);RecordRecent();Status.Text="Project saved.";logger.Write(LogLevel.Info,"Project saved.");});
    }
    private async Task<bool> CanLeaveAsync()
    {
        if(operation!=null)return false;
        NameInput.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateSource();var invalid=System.Windows.Controls.Validation.GetHasError(NameInput);
        if(!vm.Session.IsDirty&&!invalid)return true;
        var answer=MessageBox.Show(this,"Save changes before continuing?\nYes = Save, No = Discard, Cancel = stay.","Unsaved project",MessageBoxButton.YesNoCancel,MessageBoxImage.Question);
        if(answer==MessageBoxResult.No)return true;if(answer!=MessageBoxResult.Yes || invalid)return false;return await SaveAsync()&&!vm.Session.IsDirty;
    }
    private async void OnClosing(object? sender,CancelEventArgs e)
    {
        if(closingAllowed){logger.Write(LogLevel.Info,"Desktop closed.");return;}
        e.Cancel=true;
        if(await CanLeaveAsync())
        {
            closingAllowed=true;
            // CanLeaveAsync may complete synchronously for a clean project.
            // Re-entering Close inside WPF's Closing event is prohibited.
            _ = Dispatcher.BeginInvoke(new Action(Close));
        }
    }
    private async void New_Click(object sender,RoutedEventArgs e){if(await CanLeaveAsync()){vm.Session.New("Untitled Project","0.3.0");Refresh();Status.Text="New project · save before adding drawings.";}}
    private async Task OpenPathAsync(string path){if(!await CanLeaveAsync())return;await RunAsync("Open failed",_=>{vm.Session.Open(path);RecordRecent();Status.Text="Project opened · "+vm.DrawingSummary;logger.Write(LogLevel.Info,"Project opened.");return Task.CompletedTask;});}
    private async void Open_Click(object sender,RoutedEventArgs e){var dialog=new OpenFileDialog{Filter="DtoB project (*.dtob)|*.dtob",InitialDirectory=settings.WorkingDirectory};if(dialog.ShowDialog(this)==true)await OpenPathAsync(dialog.FileName);}
    private async void Recent_DoubleClick(object sender,MouseButtonEventArgs e){if(operation==null&&RecentList.SelectedItem is RecentEntry entry)await OpenPathAsync(entry.Path);}
    private async void Recent_KeyDown(object sender,KeyEventArgs e){if(e.Key==Key.Enter&&operation==null&&RecentList.SelectedItem is RecentEntry entry)await OpenPathAsync(entry.Path);}
    private async void Save_Click(object sender,RoutedEventArgs e)=>await SaveAsync();
    private async void SaveAs_Click(object sender,RoutedEventArgs e)=>await SaveAsync(true);
    private async void Close_Click(object sender,RoutedEventArgs e){if(await CanLeaveAsync()){vm.Session.Close();Refresh();Status.Text="Project closed.";}}
    private void Connection_Click(object sender,RoutedEventArgs e)=>new ConnectionWindow{Owner=this}.Show();
    private async void Settings_Click(object sender,RoutedEventArgs e)
    {var dialog=new SettingsWindow(settings){Owner=this};if(dialog.ShowDialog()!=true)return;await RunAsync("Settings save failed",_=>{settingsStore.Save(dialog.Value!);settings=dialog.Value!;logger.Minimum=settings.LogLevel;Status.Text="Settings saved.";return Task.CompletedTask;});}
    private string[]? PickDrawings(bool multiple){var dialog=new OpenFileDialog{Filter="CAD drawings (*.dwg;*.dxf)|*.dwg;*.dxf",InitialDirectory=settings.WorkingDirectory,Multiselect=multiple};return dialog.ShowDialog(this)==true?dialog.FileNames:null;}
    private async Task<bool> EnsureSavedAsync(){if(vm.Session.Project==null){Status.Text="Create or open a project first.";return false;}return vm.Session.CurrentFilePath!=null||await SaveAsync();}
    private async Task RegisterAsync(string[] paths,Guid? replace=null)
    {
        foreach(var path in paths)
        {
            var ok=await RunAsync(replace.HasValue?"Replace drawing":"Add drawing",async ct=>{var result=await vm.Session.AddDrawingAsync(path,replace,ct);Refresh();vm.SelectedDrawing=result.Drawing;Status.Text=result.Message;logger.Write(LogLevel.Info,result.Message);});
            if(!ok){Status.Text+=" · Import stopped; any remaining selected files were not processed.";break;}
        }
    }
    private async void Add_Click(object sender,RoutedEventArgs e){if(!await EnsureSavedAsync())return;var paths=PickDrawings(true);if(paths!=null)await RegisterAsync(paths);}
    private async void Replace_Click(object sender,RoutedEventArgs e){if(vm.SelectedDrawing is not { } d){Status.Text="Select a drawing to replace.";return;}var paths=PickDrawings(false);if(paths!=null)await RegisterAsync(paths,d.DrawingId);}
    private void Remove_Click(object sender,RoutedEventArgs e){if(vm.SelectedDrawing is not { } d)return;if(MessageBox.Show(this,"Remove this drawing registration? Source and managed files are retained.","Remove drawing",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;vm.Session.RemoveDrawing(d.DrawingId);Refresh();Status.Text="Drawing registration removed; files retained.";logger.Write(LogLevel.Info,"Drawing registration removed.");}
    private async void Verify_Click(object sender,RoutedEventArgs e){if(vm.SelectedDrawing is not { } d)return;await RunAsync("Verify integrity",async ct=>{var applied=await vm.Session.VerifyIntegrityAsync(d.DrawingId,ct);Refresh();Status.Text=applied?vm.Session.Runtime[d.DrawingId].Display:"Stale verification result discarded.";logger.Write(LogLevel.Info,"Explicit integrity verification: "+Status.Text);});}
    private void Refresh_Click(object sender,RoutedEventArgs e){Refresh();Status.Text="Availability refreshed · "+vm.DrawingSummary;}
    private async void CheckSource_Click(object sender,RoutedEventArgs e)
    {
        if(vm.SelectedDrawing is not { } d){Status.Text="Select a drawing to check its source.";return;}
        await RunAsync("Check source",async ct=>{var applied=await vm.Session.RefreshSourceStatusAsync(d.DrawingId,ct);Refresh();Status.Text=applied?vm.DrawingSummary:"Stale source-status result discarded.";});
    }
    private void Cancel_Click(object sender,RoutedEventArgs e)=>operation?.Cancel();
    private async Task RevealAsync(bool managed)
    {
        if(vm.SelectedDrawing is not { } d || vm.Session.CurrentFilePath==null)return;
        await RunAsync("Reveal location",async ct=>{var path=managed?AssetPaths.Resolve(vm.Session.CurrentFilePath,vm.Session.Project!.ProjectId,d):d.SourceFilePath;if(!await Task.Run(()=>System.IO.File.Exists(path),ct).WaitAsync(ct))throw new System.IO.FileNotFoundException("Drawing file is missing; location cannot be selected.");var start=new ProcessStartInfo("explorer.exe"){UseShellExecute=true};start.ArgumentList.Add("/select,"+path);Process.Start(start);Status.Text="Drawing location opened.";});
    }
    private async void RevealSource_Click(object sender,RoutedEventArgs e)=>await RevealAsync(false);
    private async void RevealManaged_Click(object sender,RoutedEventArgs e)=>await RevealAsync(true);
    private void Drawing_DragOver(object sender,DragEventArgs e){e.Effects=operation==null&&e.Data.GetDataPresent(DataFormats.FileDrop)?DragDropEffects.Copy:DragDropEffects.None;e.Handled=true;}
    private async void Drawing_Drop(object sender,DragEventArgs e){if(operation==null&&e.Data.GetData(DataFormats.FileDrop) is string[] paths&&await EnsureSavedAsync())await RegisterAsync(paths);}
}
