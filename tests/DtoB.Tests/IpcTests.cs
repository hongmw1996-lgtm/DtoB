using System.IO.Pipes;
using DtoB.Ipc;
using Xunit;

namespace DtoB.Tests;
public class IpcTests
{
    private static HostIdentity Host() => new(Environment.ProcessId, "Revit", "2025", "synthetic-test-host");
    private sealed class NonPumpingContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback callback, object? state) { /* Revit UI is blocked during synchronous shutdown. */ }
    }
    // Intentional blocking reproduces IExternalApplication.OnShutdown; bounded to avoid a stuck suite.
#pragma warning disable xUnit1031
    [Fact] public void RevitStyleSynchronousShutdownDoesNotDependOnUiMessagePump()
    {
        var prior = SynchronizationContext.Current;
        try
        {
            SynchronizationContext.SetSynchronizationContext(new NonPumpingContext());
            var server = new PingServer(Host(), _ => { }, TimeSpan.FromSeconds(1)); server.Start();
            Assert.True(server.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(3)), "Shutdown captured the UI synchronization context.");
        }
        finally { SynchronizationContext.SetSynchronizationContext(prior); }
    }
#pragma warning restore xUnit1031
    [Fact] public async Task PingPongReconnectShutdownAndRestart()
    {
        var host = Host(); var log = new System.Collections.Concurrent.ConcurrentQueue<string>();
        await using (var server = new PingServer(host, log.Enqueue, TimeSpan.FromSeconds(1)))
        {
            server.Start(); var client = new PingClient();
            var a = await client.PingAsync(host.ProcessId, TimeSpan.FromSeconds(3));
            var b = await client.PingAsync(host.ProcessId, TimeSpan.FromSeconds(3));
            Assert.Equal(IpcStatus.Pong, a.Status); Assert.NotEqual(a.RequestId, b.RequestId); Assert.Equal(host, a.Host);
        }
        await Assert.ThrowsAsync<TimeoutException>(() => new PingClient().PingAsync(host.ProcessId, TimeSpan.FromMilliseconds(100)));
        await using var restarted = new PingServer(host, log.Enqueue, TimeSpan.FromSeconds(1)); restarted.Start();
        Assert.Equal(IpcStatus.Pong, (await new PingClient().PingAsync(host.ProcessId, TimeSpan.FromSeconds(3))).Status);
    }
    [Theory][InlineData(2, IpcCommand.Ping)][InlineData(1, (IpcCommand)99)]
    public async Task VersionAndUnsupportedCommandsProduceExplicitErrors(int version, IpcCommand command)
    {
        var host = Host(); await using var server = new PingServer(host, _ => { }, TimeSpan.FromSeconds(2)); server.Start();
        using var token = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        using var client = new NamedPipeClientStream(".", PipeProtocol.Endpoint(host.ProcessId), PipeDirection.InOut, PipeOptions.Asynchronous);
        await client.ConnectAsync(token.Token); var request = new PingRequest(version, Guid.NewGuid(), command);
        await PipeProtocol.WriteAsync(client, request, token.Token);
        var response = await PipeProtocol.ReadAsync<PingResponse>(client, token.Token);
        Assert.Equal(IpcStatus.Error, response.Status); Assert.Equal(request.RequestId, response.RequestId); Assert.NotEqual(IpcError.None, response.Error);
    }
    [Fact] public async Task ClientCancellationIsNotReportedAsTimeout()
    {
        using var token = new CancellationTokenSource(); token.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new PingClient().PingAsync(Host().ProcessId, TimeSpan.FromSeconds(3), token.Token));
    }
    [Fact] public async Task OversizeAndTruncatedFramesAreRejected()
    {
        using var invalid = new MemoryStream(BitConverter.GetBytes(PipeProtocol.MaxFrameBytes + 1));
        await Assert.ThrowsAsync<InvalidDataException>(() => PipeProtocol.ReadAsync<PingRequest>(invalid, default));
        using var truncated = new MemoryStream([1, 0]);
        await Assert.ThrowsAsync<EndOfStreamException>(() => PipeProtocol.ReadAsync<PingRequest>(truncated, default));
    }
    [Fact] public async Task IdleClientTimesOutAndServerAcceptsNextClient()
    {
        var host = Host(); var log = new System.Collections.Concurrent.ConcurrentQueue<string>();
        await using var server = new PingServer(host, log.Enqueue, TimeSpan.FromMilliseconds(100)); server.Start();
        using (var client = new NamedPipeClientStream(".", PipeProtocol.Endpoint(host.ProcessId), PipeDirection.InOut, PipeOptions.Asynchronous))
        {
            using var token = new CancellationTokenSource(TimeSpan.FromSeconds(3)); await client.ConnectAsync(token.Token);
            byte[] buffer = new byte[1]; Assert.Equal(0, await client.ReadAsync(buffer, token.Token));
        }
        Assert.Equal(IpcStatus.Pong, (await new PingClient().PingAsync(host.ProcessId, TimeSpan.FromSeconds(3))).Status);
        Assert.Contains(log, s => s.Contains("IPC request failed"));
    }
    [Fact] public async Task MalformedClientDoesNotKillListener()
    {
        var host = Host(); var log = new System.Collections.Concurrent.ConcurrentQueue<string>();
        await using var server = new PingServer(host, log.Enqueue, TimeSpan.FromSeconds(1)); server.Start();
        using (var client = new NamedPipeClientStream(".", PipeProtocol.Endpoint(host.ProcessId), PipeDirection.InOut, PipeOptions.Asynchronous))
        {
            using var token = new CancellationTokenSource(TimeSpan.FromSeconds(3)); await client.ConnectAsync(token.Token);
            await client.WriteAsync(BitConverter.GetBytes(-1), token.Token);
            byte[] buffer = new byte[1]; Assert.Equal(0, await client.ReadAsync(buffer, token.Token));
        }
        Assert.Equal(IpcStatus.Pong, (await new PingClient().PingAsync(host.ProcessId, TimeSpan.FromSeconds(3))).Status);
        Assert.Contains(log, s => s.Contains("Invalid IPC frame size"));
    }
    [Theory][InlineData("correlation")][InlineData("version")][InlineData("host")]
    public async Task ClientRejectsForgedResponse(string mismatch)
    {
        var host = Host();
        using var token = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var server = new NamedPipeServerStream(PipeProtocol.Endpoint(host.ProcessId), PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        var responder = Task.Run(async () =>
        {
            await server.WaitForConnectionAsync(token.Token);
            var request = await PipeProtocol.ReadAsync<PingRequest>(server, token.Token);
            var response = new PingResponse(mismatch == "version" ? 2 : 1, mismatch == "correlation" ? Guid.NewGuid() : request.RequestId, IpcStatus.Pong,
                mismatch == "host" ? host with { ProcessId = host.ProcessId + 1 } : host);
            await PipeProtocol.WriteAsync(server, response, token.Token);
        });
        await Assert.ThrowsAsync<InvalidDataException>(() => new PingClient().PingAsync(host.ProcessId, TimeSpan.FromSeconds(3)));
        await responder;
    }

    [Fact] public async Task IdleConnectionDoesNotBlockSecondClient()
    {
        var host=Host();await using var server=new PingServer(host,_=>{},TimeSpan.FromSeconds(3));server.Start();
        using var idle=new NamedPipeClientStream(".",PipeProtocol.Endpoint(host.ProcessId),PipeDirection.InOut,PipeOptions.Asynchronous);
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(3));await idle.ConnectAsync(deadline.Token);
        Assert.Equal(IpcStatus.Pong,(await new PingClient().PingAsync(host.ProcessId,TimeSpan.FromSeconds(1))).Status);
    }
    [Fact] public async Task CreationFailureRecoversAndFaultedShutdownIsSafe()
    {
        var host=Host();int calls=0;var log=new System.Collections.Concurrent.ConcurrentQueue<string>();
        await using var server=new PingServer(host,log.Enqueue,TimeSpan.FromSeconds(1),()=>
        {
            if(Interlocked.Increment(ref calls)==2)throw new IOException("injected creation failure");
            return new NamedPipeServerStream(PipeProtocol.Endpoint(host.ProcessId),PipeDirection.InOut,4,PipeTransmissionMode.Byte,PipeOptions.Asynchronous);
        });server.Start();
        Assert.Equal(IpcStatus.Pong,(await new PingClient().PingAsync(host.ProcessId,TimeSpan.FromSeconds(3))).Status);
        await Task.Delay(200);Assert.Equal(ListenerState.Running,server.State);
        Assert.Contains(log,l=>l.Contains("creation failed"));
    }
    [Fact] public async Task NonexistentProcessFailsFast()
    { var timer=System.Diagnostics.Stopwatch.StartNew(); await Assert.ThrowsAsync<HostNotRunningException>(()=>new PingClient().PingAsync(int.MaxValue,TimeSpan.FromSeconds(5))); Assert.True(timer.Elapsed<TimeSpan.FromSeconds(1)); }
    [Fact] public async Task OsProcessIdentityRejectsImpersonation()
    {
        using var live=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("powershell.exe", "-NoProfile -NonInteractive -Command Start-Sleep -Seconds 10") { UseShellExecute=false,CreateNoWindow=true });
        var other=live!.Id;
        using var server=new NamedPipeServerStream(PipeProtocol.Endpoint(other),PipeDirection.InOut,1,PipeTransmissionMode.Byte,PipeOptions.Asynchronous);
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var accept=server.WaitForConnectionAsync(deadline.Token);
        await Assert.ThrowsAsync<InvalidDataException>(()=>new PingClient().PingAsync(other,TimeSpan.FromSeconds(3)));
        await accept; live.Kill();await live.WaitForExitAsync();
    }

    [Fact] public async Task FiftyConnectAndDropCyclesPreserveListener()
    {
        var host=Host();await using var server=new PingServer(host,_=>{},TimeSpan.FromSeconds(1));server.Start();
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(10));
        for(int i=0;i<50;i++)
        {
            using var client=new NamedPipeClientStream(".",PipeProtocol.Endpoint(host.ProcessId),PipeDirection.InOut,PipeOptions.Asynchronous);
            await client.ConnectAsync(deadline.Token);
        }
        Assert.Equal(IpcStatus.Pong,(await new PingClient().PingAsync(host.ProcessId,TimeSpan.FromSeconds(3))).Status);
        Assert.Equal(ListenerState.Running,server.State);
    }
    [Fact] public async Task ExhaustedCreationRecoveryFaultsAndShutsDownSafely()
    {
        var host=Host();int count=0;
        var server=new PingServer(host,_=>{},TimeSpan.FromSeconds(1),()=>
        {
            if(Interlocked.Increment(ref count)>1)throw new IOException("forced unrecoverable fault");
            return new NamedPipeServerStream(PipeProtocol.Endpoint(host.ProcessId),PipeDirection.InOut,4,PipeTransmissionMode.Byte,PipeOptions.Asynchronous);
        });server.Start();
        var deadline=System.Diagnostics.Stopwatch.StartNew();
        while(server.State!=ListenerState.Faulted&&deadline.Elapsed<TimeSpan.FromSeconds(3))await Task.Delay(20);
        Assert.Equal(ListenerState.Faulted,server.State);await server.DisposeAsync();
    }
    [Fact] public async Task PingSurvivesFourIdleClients()
    {
        var host=Host();await using var server=new PingServer(host,_=>{},TimeSpan.FromSeconds(3));server.Start();
        var idle=new List<NamedPipeClientStream>();
        try
        {
            using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(3));
            for(int i=0;i<4;i++){var client=new NamedPipeClientStream(".",PipeProtocol.Endpoint(host.ProcessId),PipeDirection.InOut,PipeOptions.Asynchronous);idle.Add(client);await client.ConnectAsync(deadline.Token);}
            Assert.Equal(IpcStatus.Pong,(await new PingClient().PingAsync(host.ProcessId,TimeSpan.FromSeconds(2))).Status);
        }
        finally{foreach(var client in idle)client.Dispose();}
    }

    [Fact] public async Task UnexpectedWorkerExitIsSupervisedAndRestarted()
    {
        var host=Host();int calls=0;var log=new System.Collections.Concurrent.ConcurrentQueue<string>();
        await using var server=new PingServer(host,log.Enqueue,TimeSpan.FromSeconds(1),()=>
        {
            if(Interlocked.Increment(ref calls)==2)throw new InvalidOperationException("unexpected injected worker exit");
            return new NamedPipeServerStream(PipeProtocol.Endpoint(host.ProcessId),PipeDirection.InOut,4,PipeTransmissionMode.Byte,PipeOptions.Asynchronous);
        });server.Start();await Task.Delay(200);
        Assert.Equal(ListenerState.Running,server.State);Assert.Contains(log,l=>l.Contains("unexpected exit"));
        Assert.Equal(IpcStatus.Pong,(await new PingClient().PingAsync(host.ProcessId,TimeSpan.FromSeconds(3))).Status);
    }
}
