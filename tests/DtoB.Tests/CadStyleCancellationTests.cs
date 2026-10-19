using System.Text.Json;
using DtoB.Cad.Normalize.Styles;
using Xunit;
using static DtoB.Tests.Cad04FStyleFixture;

namespace DtoB.Tests;

[Collection("04E resource")]
public sealed class CadStyleCancellationTests
{
    [Fact] public async Task ConcurrentCancellationDoesNotPublishAStylePrefix()
    {
        var c=await Run(Insert(extra:"70\n100\n71\n100\n"),Block(Line("E")+Line("F")));
        using var cts=new CancellationTokenSource();using var started=new ManualResetEventSlim();
        var task=Task.Run(()=>{started.Set();return CadStyleResolver.Resolve(c.Snapshot,c.Expansion,cancellationToken:cts.Token);});
        Assert.True(started.Wait(TimeSpan.FromSeconds(10)));cts.CancelAfter(TimeSpan.FromMilliseconds(10));
        var r=await task.WaitAsync(TimeSpan.FromSeconds(30));Assert.Equal(CadStyleStatus.Cancelled,r.Status);Assert.Null(r.Artifact);
        Assert.Equal(CadStyleDiagnosticCode.Cancelled,r.Diagnostics[^1].Code);
        var directory=Environment.GetEnvironmentVariable("DTOB_04F_EVIDENCE_DIRECTORY");
        if(directory is not null){Directory.CreateDirectory(directory);File.WriteAllBytes(Path.Combine(directory,"style-cancellation-evidence.json"),JsonSerializer.SerializeToUtf8Bytes(new{r.Status,r.ProjectionsAllocated,ArtifactPublished=r.Artifact is not null}));}
    }
}
