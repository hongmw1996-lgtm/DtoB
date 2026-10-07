using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Win32.SafeHandles;

namespace DtoB.Ipc;
public enum IpcCommand { Ping }
public enum IpcStatus { Pong, Error }
public enum IpcError { None, UnsupportedVersion, InvalidRequest, UnsupportedCommand }
public enum ListenerState { Stopped, Running, Faulted }
public sealed record HostIdentity(int ProcessId, string Product, string Version, string Build);
public sealed record PingRequest(int ProtocolVersion, Guid RequestId, IpcCommand Command);
public sealed record PingResponse(int ProtocolVersion, Guid RequestId, IpcStatus Status, HostIdentity Host, IpcError Error = IpcError.None);
public sealed class HostNotRunningException(int pid) : IOException($"Revit process {pid} is not running.");
public static class PipeProtocol
{
    public const int Version=1;
    public const int MaxFrameBytes=16*1024;
    private static readonly JsonSerializerOptions Json=new() { Converters={new JsonStringEnumConverter()}, UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow };
    public static string Endpoint(int pid)=>pid>0?$"dtob-revit-{pid}":throw new ArgumentOutOfRangeException(nameof(pid));
    public static async Task WriteAsync<T>(Stream stream,T message,CancellationToken token)
    {
        var payload=JsonSerializer.SerializeToUtf8Bytes(message,Json);
        if(payload.Length>MaxFrameBytes) throw new InvalidDataException("IPC frame too large.");
        byte[] header=new byte[4];BinaryPrimitives.WriteInt32LittleEndian(header,payload.Length);
        await stream.WriteAsync(header,token);await stream.WriteAsync(payload,token);await stream.FlushAsync(token);
    }
    public static async Task<T> ReadAsync<T>(Stream stream,CancellationToken token)
    {
        byte[] header=new byte[4];await stream.ReadExactlyAsync(header,token);
        int size=BinaryPrimitives.ReadInt32LittleEndian(header);
        if(size is <=0 or >MaxFrameBytes)throw new InvalidDataException("Invalid IPC frame size.");
        byte[] payload=new byte[size];await stream.ReadExactlyAsync(payload,token);
        return JsonSerializer.Deserialize<T>(payload,Json)??throw new InvalidDataException("Null IPC message.");
    }
}
public sealed class PingClient
{
    [DllImport("kernel32.dll",SetLastError=true)]
    [return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetNamedPipeServerProcessId(SafePipeHandle pipe,out uint processId);
    public async Task<PingResponse> PingAsync(int pid,TimeSpan timeout,CancellationToken token=default)
    {
        token.ThrowIfCancellationRequested();
        try { using var process=Process.GetProcessById(pid);if(process.HasExited)throw new HostNotRunningException(pid); }
        catch(ArgumentException){throw new HostNotRunningException(pid);}
        using var deadline=CancellationTokenSource.CreateLinkedTokenSource(token);deadline.CancelAfter(timeout);
        try
        {
            using var pipe=new NamedPipeClientStream(".",PipeProtocol.Endpoint(pid),PipeDirection.InOut,PipeOptions.Asynchronous|PipeOptions.CurrentUserOnly);
            using(var connect=CancellationTokenSource.CreateLinkedTokenSource(deadline.Token))
            { connect.CancelAfter(TimeSpan.FromSeconds(1));await pipe.ConnectAsync(connect.Token); }
            if(!GetNamedPipeServerProcessId(pipe.SafePipeHandle,out uint actualPid)||actualPid!=pid)
                throw new InvalidDataException("OS pipe server process identity mismatch.");
            var request=new PingRequest(PipeProtocol.Version,Guid.NewGuid(),IpcCommand.Ping);
            await PipeProtocol.WriteAsync(pipe,request,deadline.Token);
            var response=await PipeProtocol.ReadAsync<PingResponse>(pipe,deadline.Token);
            if(response.ProtocolVersion!=PipeProtocol.Version||response.RequestId!=request.RequestId||response.Host==null||response.Host.ProcessId!=pid||response.Host.Product!="Revit")
                throw new InvalidDataException("IPC response version, correlation or Revit host mismatch.");
            if(response.Status!=IpcStatus.Pong||response.Error!=IpcError.None)throw new InvalidDataException($"IPC response error: {response.Error}.");
            return response;
        }
        catch(OperationCanceledException)when(!token.IsCancellationRequested)
        {throw new TimeoutException($"Revit process {pid} endpoint unavailable or did not respond before timeout.");}
    }
}
public sealed class PingServer:IAsyncDisposable
{
    private readonly HostIdentity host;
    private readonly Action<string> log;
    private readonly TimeSpan requestTimeout;
    private readonly Func<NamedPipeServerStream> factory;
    private readonly CancellationTokenSource stopping=new();
    private Task? loop;
    private int faulted;
    public ListenerState State=>Volatile.Read(ref faulted)!=0?ListenerState.Faulted:loop==null||loop.IsCompleted?ListenerState.Stopped:ListenerState.Running;
    public PingServer(HostIdentity host,Action<string> log,TimeSpan requestTimeout,Func<NamedPipeServerStream>? pipeFactory=null)
    {this.host=host;this.log=log;this.requestTimeout=requestTimeout;factory=pipeFactory??CreatePipe;}
    private NamedPipeServerStream CreatePipe()=>new(PipeProtocol.Endpoint(host.ProcessId),PipeDirection.InOut,4,PipeTransmissionMode.Byte,PipeOptions.Asynchronous|PipeOptions.CurrentUserOnly);
    public void Start()
    {
        if(loop!=null)throw new InvalidOperationException("Server already started.");
        var initial=factory();loop=Task.WhenAll(Enumerable.Range(0,4).Select(i=>Task.Run(()=>RunAsync(i==0?initial:null))));
    }
    private async Task RunAsync(NamedPipeServerStream? initial)
    {
        int failures=0;
        try
        {
            while(!stopping.IsCancellationRequested)
            {
                NamedPipeServerStream pipe;
                try {pipe=initial??factory();initial=null;failures=0;}
                catch(Exception ex)when(ex is IOException or UnauthorizedAccessException)
                {
                    log($"IPC listener creation failed ({++failures}/3): {ex.Message}");
                    if(failures>=3){Interlocked.Exchange(ref faulted,1);log("IPC listener FAULTED");return;}
                    await Task.Delay(TimeSpan.FromMilliseconds(100),stopping.Token);continue;
                }
                using(pipe)
                {
                    await pipe.WaitForConnectionAsync(stopping.Token);
                    using var deadline=CancellationTokenSource.CreateLinkedTokenSource(stopping.Token);deadline.CancelAfter(requestTimeout);
                    try
                    {
                        var request=await PipeProtocol.ReadAsync<PingRequest>(pipe,deadline.Token);
                        var error=request.ProtocolVersion!=PipeProtocol.Version?IpcError.UnsupportedVersion:request.RequestId==Guid.Empty?IpcError.InvalidRequest:request.Command!=IpcCommand.Ping?IpcError.UnsupportedCommand:IpcError.None;
                        var response=new PingResponse(PipeProtocol.Version,request.RequestId,error==IpcError.None?IpcStatus.Pong:IpcStatus.Error,host,error);
                        await PipeProtocol.WriteAsync(pipe,response,deadline.Token);log($"{response.Status} request={request.RequestId} pid={host.ProcessId} error={error}");
                    }
                    catch(Exception ex)when(ex is IOException or InvalidDataException or JsonException or OperationCanceledException)
                    {log($"IPC request failed: {ex.GetType().Name}: {ex.Message}");}
                }
            }
        }
        catch(OperationCanceledException)when(stopping.IsCancellationRequested){}
        catch(Exception ex){Interlocked.Exchange(ref faulted,1);log($"IPC listener FAULTED: {ex}");}
        finally{initial?.Dispose();}
    }
    public async ValueTask DisposeAsync()
    {
        await stopping.CancelAsync().ConfigureAwait(false);
        if(loop!=null)await loop.ConfigureAwait(false);
        stopping.Dispose();
    }
}
