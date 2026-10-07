using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using DtoB.Project;
namespace DtoB.Desktop;
public partial class MainWindow:Window
{
    private readonly ShellViewModel vm;private readonly RecentStore recent;private readonly SettingsStore settingsStore;private AppSettings settings;private readonly FileLogger logger;
    public MainWindow(ShellViewModel vm,RecentStore recent,SettingsStore settingsStore,AppSettings settings,FileLogger logger)
    {InitializeComponent();this.vm=vm;this.recent=recent;this.settingsStore=settingsStore;this.settings=settings;this.logger=logger;DataContext=vm;Refresh();Closing+=OnClosing;}
    private void Refresh(){vm.Refresh();RecentList.ItemsSource=recent.Entries.ToArray();}
    private void Error(string operation,Exception ex){logger.Write(LogLevel.Error,operation,ex);Status.Text=operation+": "+ex.Message;MessageBox.Show(this,Status.Text,"DtoB error",MessageBoxButton.OK,MessageBoxImage.Error);}
    private void Run(string operation,Action action){try{action();Refresh();}catch(Exception ex) when(LocalFileErrors.IsExpected(ex)){Error(operation,ex);}}
    private void RecordRecent(){try{recent.Record(vm.Session.CurrentFilePath!,vm.Session.Project!.Name);}catch(Exception ex) when(LocalFileErrors.IsExpected(ex)){Error("Project succeeded, but recent state could not be saved",ex);}}
    private bool Save(bool saveAs=false)
    {
        if(vm.Session.Project==null)return false;
        NameInput.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateSource();
        if(System.Windows.Controls.Validation.GetHasError(NameInput)){MessageBox.Show(this,"Enter a nonempty project name before saving.","Invalid project name");return false;}
        try
        {
            var path=saveAs?null:vm.Session.CurrentFilePath;
            if(path==null){var dialog=new SaveFileDialog{Filter="DtoB project (*.dtob)|*.dtob",DefaultExt=".dtob",AddExtension=true,InitialDirectory=settings.WorkingDirectory,FileName="Project.dtob"};if(dialog.ShowDialog(this)!=true)return false;path=dialog.FileName;}
            vm.Session.Save(path);RecordRecent();Refresh();Status.Text="Project saved.";logger.Write(LogLevel.Info,"Project saved.");return true;
        }
        catch(Exception ex) when(LocalFileErrors.IsExpected(ex)){Error("Save failed",ex);return false;}
    }
    private bool CanLeave()
    {
        NameInput.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateSource();
        var invalidName=System.Windows.Controls.Validation.GetHasError(NameInput);
        if(!vm.Session.IsDirty && !invalidName)return true;
        var answer=MessageBox.Show(this,"Save changes before continuing?\nYes = Save, No = Discard, Cancel = stay.","Unsaved project",MessageBoxButton.YesNoCancel,MessageBoxImage.Question);
        var choice=answer==MessageBoxResult.Yes?UnsavedChoice.Save:answer==MessageBoxResult.No?UnsavedChoice.Discard:UnsavedChoice.Cancel;
        if(invalidName)return choice==UnsavedChoice.Discard;
        return vm.Session.CanLeave(choice,()=>Save());
    }
    private void OnClosing(object? sender,CancelEventArgs e){if(!CanLeave())e.Cancel=true;else logger.Write(LogLevel.Info,"Desktop closed.");}
    private void New_Click(object sender,RoutedEventArgs e){if(!CanLeave())return;Run("New project",()=>{vm.Session.New("Untitled Project","0.2.0");Status.Text="New project · edit its name and save.";});}
    private void OpenPath(string path){if(!CanLeave())return;Run("Open failed",()=>{vm.Session.Open(path);RecordRecent();Status.Text="Project opened.";logger.Write(LogLevel.Info,"Project opened.");});}
    private void Open_Click(object sender,RoutedEventArgs e){var dialog=new OpenFileDialog{Filter="DtoB project (*.dtob)|*.dtob",InitialDirectory=settings.WorkingDirectory};if(dialog.ShowDialog(this)==true)OpenPath(dialog.FileName);}
    private void Recent_DoubleClick(object sender,MouseButtonEventArgs e){if(RecentList.SelectedItem is RecentEntry entry)OpenPath(entry.Path);}
    private void Recent_KeyDown(object sender,KeyEventArgs e){if(e.Key==Key.Enter&&RecentList.SelectedItem is RecentEntry entry)OpenPath(entry.Path);}
    private void Save_Click(object sender,RoutedEventArgs e)=>Save();
    private void SaveAs_Click(object sender,RoutedEventArgs e)=>Save(true);
    private void Close_Click(object sender,RoutedEventArgs e){if(CanLeave()){vm.Session.Close();Refresh();Status.Text="Project closed.";}}
    private void Connection_Click(object sender,RoutedEventArgs e)=>new ConnectionWindow{Owner=this}.Show();
    private void Settings_Click(object sender,RoutedEventArgs e)
    {
        var dialog=new SettingsWindow(settings){Owner=this};if(dialog.ShowDialog()!=true)return;
        Run("Settings save failed",()=>{settingsStore.Save(dialog.Value!);settings=dialog.Value!;logger.Minimum=settings.LogLevel;Status.Text="Settings saved.";});
    }
}
