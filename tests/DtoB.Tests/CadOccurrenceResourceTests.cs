using DtoB.Cad.Normalize.Occurrences;
using Xunit;
using static DtoB.Tests.Cad04EOccurrenceFixture;

namespace DtoB.Tests;

[CollectionDefinition("04E resource",DisableParallelization=true)] public sealed class Cad04EResourceCollection { }

public sealed class CadOccurrenceDepthTests
{
    [Theory] [InlineData(32,true)] [InlineData(33,false)]
    public async Task RootCountsAsOneAndDepthBoundaryIsExact(int depth,bool success)
    {
        var blocks=string.Concat(Enumerable.Range(1,depth).Select(i=>Block((256+i).ToString("X"),"B"+i,
            i<depth?Insert((512+i).ToString("X"),"B"+(i+1)):"")));
        var r=Expand(await Snapshot(blocks,Insert("A","B1")));
        Assert.Equal(32,r.TotalOccurrencesAllocated);
        if(success) { var a=Complete(r); Assert.Equal(32,a.Occurrences[^1].Depth); }
        else { Assert.Equal(CadExpansionStatus.ResourceLimitExceeded,r.Status); Assert.Null(r.Expansion); var d=Assert.Single(r.Diagnostics); Assert.Equal("Depth",d.Detail); Assert.Equal(33,d.Requested); }
    }
}
[Collection("04E resource")]
public sealed class CadOccurrenceResourceTests
{
    [Theory] [InlineData(99_999)] [InlineData(100_000)] [InlineData(100_001)]
    public async Task GlobalBoundaryIncludesAllRootsWithOneCounter(int requested)
    {
        var roots=string.Concat(Enumerable.Range(0,requested/10_000).Select(i=>Insert((256+i).ToString("X"),"B",rows:100,columns:100)));
        var rest=requested%10_000;
        if(rest>0)roots+=Insert("200","B",columns:rest);
        var r=Expand(await Snapshot(Block("B","B"),roots));
        Assert.Equal(Math.Min(requested,100_000),r.TotalOccurrencesAllocated);
        if(requested<=100_000)Assert.Equal(requested,Complete(r).Occurrences.Count);
        else { Assert.Equal(CadExpansionStatus.ResourceLimitExceeded,r.Status); Assert.Null(r.Expansion); var d=Assert.Single(r.Diagnostics); Assert.Equal("Occurrences",d.Detail); Assert.Equal(100_001,d.Requested); Assert.Equal(100_000,d.Limit); }
    }
    [Theory] [InlineData(100,100,true)] [InlineData(100,101,false)] [InlineData(int.MaxValue,int.MaxValue,false)]
    public async Task PerArrayProductIsCheckedBeforeAnyCellAllocation(int rows,int columns,bool success)
    {
        var r=Expand(await Snapshot(Block("B","B"),Insert("A","B",rows:rows,columns:columns)));
        if(success) { Assert.Equal(10_000,Complete(r).Occurrences.Count); Assert.Equal(10_000,r.TotalOccurrencesAllocated); }
        else { Assert.Equal(CadExpansionStatus.ResourceLimitExceeded,r.Status); Assert.Null(r.Expansion); Assert.Equal(0,r.TotalOccurrencesAllocated); Assert.Equal((long)rows*columns,Assert.Single(r.Diagnostics).Requested); }
    }
    [Fact] public async Task FailedCompositionReservationIsNotRefundedToLaterSibling()
    {
        var blocks=Block("B","A",Insert("D","B",scale:new(1e200,1,1)))+Block("C","B");
        var s=await Snapshot(blocks,Insert("A","A",scale:new(1e200,1,1))+Insert("E","B"));
        var partial=Expand(s,Options(total:3));
        Assert.Equal(CadExpansionStatus.Partial,partial.Status); Assert.Equal(3,partial.TotalOccurrencesAllocated);
        Assert.Equal(2,partial.Expansion!.Occurrences.Count); Assert.Equal(CadOccurrenceDiagnosticCode.NonFiniteAccumulatedTransform,Assert.Single(partial.Diagnostics).Code);
        var refused=Expand(s,Options(total:2));
        Assert.Equal(CadExpansionStatus.ResourceLimitExceeded,refused.Status); Assert.Null(refused.Expansion);
        Assert.Equal(2,refused.TotalOccurrencesAllocated); Assert.Equal(3,refused.Diagnostics[^1].Requested);
    }
    [Theory] [InlineData(0,1)] [InlineData(-1,1)] [InlineData(1,0)]
    public async Task InvalidCountsAreBranchFailuresWithoutInventingCells(int rows,int columns)
    {
        var r=Expand(await Snapshot(Block("B","B"),Insert("A","B",rows:rows,columns:columns)+Insert("C","B")));
        Assert.Equal(CadExpansionStatus.Partial,r.Status); Assert.Single(r.Expansion!.Occurrences); Assert.Equal(1,r.TotalOccurrencesAllocated);
        Assert.Equal(CadOccurrenceDiagnosticCode.InvalidArrayFacts,Assert.Single(r.Diagnostics).Code);
    }
    [Theory] [InlineData(1)] [InlineData(3)] [InlineData(5000)]
    public async Task DiagnosticCapReservesOneTerminalSlotAndRefusesPublication(int maximum)
    {
        var r=Expand(await Snapshot(Block("B","A",Insert("D","A",columns:maximum)),Insert("A","A")),Options(diagnostics:maximum));
        Assert.Equal(CadExpansionStatus.ResourceLimitExceeded,r.Status); Assert.Null(r.Expansion); Assert.Equal(maximum,r.Diagnostics.Count);
        Assert.Equal(CadOccurrenceDiagnosticCode.ResourceLimitExceeded,r.Diagnostics[^1].Code); Assert.Equal("Diagnostics",r.Diagnostics[^1].Detail);
        Assert.Equal(1,r.TotalOccurrencesAllocated);
    }
    [Fact] public async Task ArrayAttributesRemainExplicitlyUnsupported()
    {
        var r=Expand(await Snapshot(Block("B","B"),Insert("A","B",columns:2,extra:"66\n1\n")+"0\nATTRIB\n1\nvalue\n2\nTAG\n10\n1\n20\n2\n40\n3\n0\nSEQEND\n"));
        Assert.Equal(CadExpansionStatus.Partial,r.Status); Assert.Empty(r.Expansion!.Occurrences);
        Assert.Equal(CadOccurrenceDiagnosticCode.UnsupportedArrayAttributes,Assert.Single(r.Diagnostics).Code);
    }
    [Fact] public async Task NonFiniteDerivedCellKeepsPriorCellsAndLaterRootWithoutRefund()
    {
        var r=Expand(await Snapshot(Block("B","B"),Insert("A","B",columns:3,columnSpacing:1e308)+Insert("C","B")));
        Assert.Equal(CadExpansionStatus.Partial,r.Status); Assert.Equal(4,r.TotalOccurrencesAllocated);
        Assert.Equal(3,r.Expansion!.Occurrences.Count); var d=Assert.Single(r.Diagnostics);
        Assert.Equal(CadOccurrenceDiagnosticCode.NonFiniteCellTransform,d.Code); Assert.Equal(2,d.Path.Steps[^1].Column);
        Assert.Equal("C",r.Expansion.Sources.Resolve(r.Expansion.Occurrences[^1].Value.InsertSource).CanonicalHandle);
    }
    [Fact] public async Task OversizedArrayAfterValidRootStillRefusesEntirePublication()
    {
        var r=Expand(await Snapshot(Block("B","B"),Insert("A","B")+Insert("C","B",rows:100,columns:101)));
        Assert.Equal(CadExpansionStatus.ResourceLimitExceeded,r.Status); Assert.Null(r.Expansion); Assert.Equal(1,r.TotalOccurrencesAllocated);
        Assert.Equal("CellsPerInsert",Assert.Single(r.Diagnostics).Detail);
    }
    [Fact] public async Task FreshSessionStartsANewCounterRatherThanReusingPreviousCall()
    {
        var s=await Snapshot(Block("B","B"),Insert("A","B",columns:2));
        Assert.Equal(2,Expand(s,Options(total:2)).TotalOccurrencesAllocated);
        Assert.Equal(2,Expand(s,Options(total:2)).TotalOccurrencesAllocated);
    }
    [Fact] public async Task CancellationBeforeWorkPublishesNothing()
    {
        using var cts=new CancellationTokenSource(); cts.Cancel();
        var r=Expand(await Snapshot(Block("B","B"),Insert("A","B")),token:cts.Token);
        Assert.Equal(CadExpansionStatus.Cancelled,r.Status); Assert.Null(r.Expansion); Assert.Equal(0,r.TotalOccurrencesAllocated);
        Assert.Equal(CadOccurrenceDiagnosticCode.Cancelled,Assert.Single(r.Diagnostics).Code);
    }
    [Fact] public async Task ConcurrentCancellationPublishesNoArtifactAndRecordsObservedProgress()
    {
        var s=await Snapshot(Block("B","A",Insert("D","B",rows:100,columns:100))+Block("C","B"),Insert("A","A",rows:100,columns:100));
        using var cts=new CancellationTokenSource(); using var started=new ManualResetEventSlim();
        var task=Task.Run(()=> { started.Set(); return Expand(s,token:cts.Token); });
        Assert.True(started.Wait(TimeSpan.FromSeconds(10))); cts.CancelAfter(TimeSpan.FromMilliseconds(10));
        var r=await task.WaitAsync(TimeSpan.FromSeconds(30));
        Assert.Equal(CadExpansionStatus.Cancelled,r.Status); Assert.Null(r.Expansion); Assert.True(r.TotalOccurrencesAllocated<=100_000);
        Assert.Equal(CadOccurrenceDiagnosticCode.Cancelled,r.Diagnostics[^1].Code);
        // Scheduling delay requests cancellation; no fixed throughput/latency assertion is made.
        var directory=Environment.GetEnvironmentVariable("DTOB_04E_EVIDENCE_DIRECTORY");
        if(directory is not null)
        {
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory,"occurrence-cancellation-evidence.json"),System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new
            {r.Status,r.TotalOccurrencesAllocated,ArtifactPublished=r.Expansion is not null,r.Diagnostics.Count}));
        }
    }
}
[Collection("04E resource")]
public sealed class MinsertResourceExplosionTests
{
    [Fact] public async Task IndividuallyLegalNestedArraysExceedOneGlobalBudgetWithoutPartialArtifact()
    {
        var r=Expand(await Snapshot(Block("B","A",Insert("D","B",rows:100,columns:100))+Block("C","B"),Insert("A","A",rows:10,columns:10)));
        Assert.Equal(CadExpansionStatus.ResourceLimitExceeded,r.Status); Assert.Null(r.Expansion); Assert.Equal(100_000,r.TotalOccurrencesAllocated);
        var d=Assert.Single(r.Diagnostics); Assert.Equal("Occurrences",d.Detail); Assert.Equal(100_001,d.Requested);
        Assert.Equal(2,d.Path.Steps.Count); Assert.Equal(9,d.Path.Steps[0].Column); Assert.Equal(0,d.Path.Steps[0].Row);
    }
}
