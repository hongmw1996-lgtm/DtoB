using System.IO;
using System.Windows;
using DtoB.Project;
namespace DtoB.Desktop;
public partial class SettingsWindow:Window
{
    public AppSettings? Value {get;private set;}
    public SettingsWindow(AppSettings settings){InitializeComponent();Working.Text=settings.WorkingDirectory;Cache.Text=settings.CacheDirectory;Level.ItemsSource=Enum.GetValues<LogLevel>();Level.SelectedItem=settings.LogLevel;}
    private void Save_Click(object sender,RoutedEventArgs e)
    {
        try{var value=new AppSettings(1,Working.Text,Cache.Text,"2025",(LogLevel)Level.SelectedItem);value.Validate();Value=value;DialogResult=true;}
        catch(InvalidDataException ex){MessageBox.Show(this,ex.Message,"Invalid settings",MessageBoxButton.OK,MessageBoxImage.Warning);}
    }
}
