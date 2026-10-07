using System.Diagnostics;
namespace DtoB.Project;
public enum LogLevel { Info, Warning, Error, Fatal }
public sealed record AppSettings(int SchemaVersion,string WorkingDirectory,string CacheDirectory,string RevitTarget,LogLevel LogLevel)
{
    public static AppSettings Defaults(string root)=>new(1,Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),Path.Combine(root,"cache"),"2025",LogLevel.Info);
    public void Validate(){if(SchemaVersion!=1 || string.IsNullOrWhiteSpace(WorkingDirectory) || !Path.IsPathFullyQualified(WorkingDirectory) || string.IsNullOrWhiteSpace(CacheDirectory) || !Path.IsPathFullyQualified(CacheDirectory) || RevitTarget!="2025" || !Enum.IsDefined(LogLevel))throw new InvalidDataException("Invalid settings. Use absolute directories and Revit target 2025.");}
}
public sealed class SettingsStore(string path)
{
    public AppSettings Load(){var s=LocalJson.Read<AppSettings>(path);s.Validate();return s;}
    public void Save(AppSettings value){value.Validate();LocalJson.Write(path,value);}
}
public sealed record RecentEntry(string Path,string Name,DateTimeOffset OpenedAt)
{
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsMissing=>!File.Exists(Path);
    [System.Text.Json.Serialization.JsonIgnore]
    public string Display=>$"{Name} — {Path}"+(IsMissing?" [File missing]":"");
}
public sealed record RecentFile(int SchemaVersion,RecentEntry[] Entries);
public sealed class RecentStore(string path)
{
    public IReadOnlyList<RecentEntry> Entries {get;private set;}=[];
    private static string Normalize(string value)=>System.IO.Path.GetFullPath(value.Replace('/',System.IO.Path.DirectorySeparatorChar)).TrimEnd(System.IO.Path.DirectorySeparatorChar);
    public void Load()
    {
        var data=LocalJson.Read<RecentFile>(path);
        if(data.Entries==null || data.Entries.Any(e=>e==null || string.IsNullOrWhiteSpace(e.Name) || string.IsNullOrWhiteSpace(e.Path) || !System.IO.Path.IsPathFullyQualified(e.Path) || e.OpenedAt==default))throw new InvalidDataException("Invalid recent-project state.");
        Entries=data.Entries.OrderByDescending(e=>e.OpenedAt).DistinctBy(e=>Normalize(e.Path),StringComparer.OrdinalIgnoreCase).Take(10).ToArray();
    }
    public void Record(string projectPath,string name)
    {
        var actual=Normalize(projectPath);var next=new[]{new RecentEntry(actual,name,DateTimeOffset.UtcNow)}.Concat(Entries.Where(e=>!StringComparer.OrdinalIgnoreCase.Equals(Normalize(e.Path),actual))).Take(10).ToArray();
        LocalJson.Write(path,new RecentFile(1,next));Entries=next;
    }
}
public sealed class FileLogger
{
    private readonly string file;private readonly Action<string> fallback;private readonly object gate=new();
    public LogLevel Minimum {get;set;}=LogLevel.Info;
    public FileLogger(string directory,Action<string> fallback)
    {
        this.fallback=fallback;file=Path.Combine(directory,$"desktop-{DateTime.UtcNow:yyyyMMddTHHmmssfffffff}-{Environment.ProcessId}.log");
        try{Directory.CreateDirectory(directory);File.WriteAllText(file,""); foreach(var old in new DirectoryInfo(directory).GetFiles("desktop-*.log").OrderByDescending(f=>f.Name,StringComparer.Ordinal).Skip(10))try{old.Delete();}catch(Exception ex) when(ex is IOException or UnauthorizedAccessException){Fallback("Log retention failed: "+ex.Message);}}
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException){Fallback("Log initialization failed: "+ex.Message);}
    }
    private void Fallback(string message){Trace.WriteLine(message);fallback(message);}
    public void Write(LogLevel level,string message,Exception? exception=null)
    {
        if(level<Minimum)return;
        try{lock(gate)File.AppendAllText(file,$"{DateTimeOffset.UtcNow:O} PID={Environment.ProcessId} {level.ToString().ToUpperInvariant()} {message} {exception}\n");}
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException){Fallback("Log write failed: "+ex.Message);}
    }
}
