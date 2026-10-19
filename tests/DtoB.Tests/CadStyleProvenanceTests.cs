using System.Reflection;
using System.Text.Json;
using DtoB.Cad.Normalize.Occurrences;
using DtoB.Cad.Normalize.Styles;
using DtoB.Cad.Raw;
using DtoB.Cad.V2;
using Xunit;
using static DtoB.Tests.Cad04FStyleFixture;

namespace DtoB.Tests;

public sealed class CadStyleOccurrenceIsolationTests
{
    [Fact] public async Task SameDefinitionAndEntityResolveRedAndBlueWithoutMutatingEitherSource()
    {
        var c=await Run(Insert("A",color:1)+Insert("D",layer:"C-INST",color:5),Block(Line(color:0)));
        var left=c.Child("E","A");var right=c.Child("E","D");
        Assert.Same(left.Declared,right.Declared);Assert.Equal(left.Key.Source.Index,right.Key.Source.Index);
        Assert.Equal("A-INST",left.Layer.Value);Assert.Equal(1,left.Color.Value!.Value);
        Assert.Equal("C-INST",right.Layer.Value);Assert.Equal(5,right.Color.Value!.Value);
        Assert.NotEqual(left.Key.OccurrenceId!.Value,right.Key.OccurrenceId!.Value);
        Assert.Same(c.Expansion.Expansion!.Occurrences[0].Definition,c.Expansion.Expansion.Occurrences[1].Definition);
        Assert.Equal("0",left.Declared.Layer.Text);Assert.Equal(0,left.Declared.Aci.Integer);
    }
    [Fact] public async Task ArrayCellsKeepExistingOccurrenceIdsAndShareSourceStyle()
    {
        var c=await Run(Insert(extra:"70\n2\n44\n100\n"),Block(Line(color:0)));
        Assert.Equal(2,c.Artifact.OccurrenceContexts.Count);Assert.Equal(2,c.Artifact.Entities.Count);
        Assert.Same(c.Artifact.Entities[0].Declared,c.Artifact.Entities[1].Declared);
        Assert.Equal(c.Expansion.Expansion!.Occurrences.Select(o=>o.Value.Id.Value),c.Artifact.OccurrenceContexts.Select(o=>o.Key.OccurrenceId!.Value));
        Assert.All(c.Expansion.Expansion.Occurrences,o=>Assert.Equal(1,o.Depth));
    }
}
public sealed class CadStyleProvenanceTests
{
    [Fact] public async Task CompactCausalKeysExplainByBlockAndLayerZeroThroughRealParents()
    {
        var c=await Run(Insert(target:"A",color:2),Block(Insert("D","B","0",0),"A","B")+Block(Line(color:0),"B","C"));
        var p=c.Child("E","D");var middle=c.Context("D");var root=c.Context("A");
        Assert.Equal("DXF.Color.ByBlock",p.Color.RuleId);Assert.Equal(middle.Key.Source.Index,p.Color.SupportingContext!.Source.Index);
        Assert.Equal(root.Key.Source.Index,middle.Color.SupportingContext!.Source.Index);Assert.Equal("DXF.Color.Aci",root.Color.RuleId);
        Assert.Equal(middle.Key.OccurrenceId!.Value,p.Layer.SupportingContext!.OccurrenceId!.Value);
        Assert.NotNull(root.Layer.CatalogRecord);Assert.Equal(c.Artifact.Sources.Source.FileHash,c.Snapshot.DocumentProvenance!.FileHash);
        foreach(var fact in p.Color.Evidence.Concat(p.Layer.Evidence))
        {
            var record=c.Artifact.Sources.Resolve(fact.Source);Assert.NotEmpty(fact.PairIndices);
            Assert.All(fact.PairIndices,i=>Assert.InRange(i,record.Locator.FirstPair,record.Locator.LastPair));
            Assert.InRange(fact.FirstLine!.Value,record.Locator.FirstPhysicalLine,record.Locator.LastPhysicalLine);
        }
    }
    [Fact] public async Task ByLayerEvidenceIncludesActualLayerAndLtypeRecordsAndAbsentDefaults()
    {
        var c=await Run(Line(layer:"A-INST"));var p=c.Root("E");
        Assert.Equal("DXF.Color.AbsentByLayer",p.Color.RuleId);Assert.Equal(DxfFactPresence.Absent,p.Declared.Aci.Presence);
        Assert.Empty(p.Declared.Aci.PairIndices);Assert.NotNull(p.Color.CatalogRecord);Assert.NotNull(p.Linetype.CatalogRecord);
        Assert.Contains(p.Color.Evidence,f=>f.Source.Index==p.Color.CatalogRecord!.Index&&f.GroupCode==62);
        Assert.Contains(p.Linetype.Evidence,f=>f.Source.Index==p.Linetype.CatalogRecord!.Index&&f.GroupCode==2);
    }
    [Fact] public async Task OutputsAreImmutableAndDoNotRetainRawOrMutateOccurrenceState()
    {
        var c=await Run(Insert(),Block(Line()));var before=JsonSerializer.Serialize(c.Expansion);var raw=JsonSerializer.Serialize(c.Snapshot);
        var result=CadStyleResolver.Resolve(c.Snapshot,c.Expansion);Assert.NotNull(result.Artifact);
        Assert.Equal(before,JsonSerializer.Serialize(c.Expansion));Assert.Equal(raw,JsonSerializer.Serialize(c.Snapshot));
        Assert.Same(c.Expansion,result.Artifact!.Input);Assert.Same(c.Expansion.Expansion!.Sources,result.Artifact.Sources);
        Assert.Throws<NotSupportedException>(()=>((IList<CadEffectiveStyle>)result.Artifact.Entities).Clear());
        Assert.Throws<NotSupportedException>(()=>((IList<int>)result.Artifact.Entities[0].Declared.Layer.PairIndices).Clear());
        foreach(var t in typeof(CadStyleResolver).Assembly.GetExportedTypes().Where(t=>t.Namespace=="DtoB.Cad.Normalize.Styles"&&!t.IsEnum))
        foreach(var p in t.GetProperties(BindingFlags.Public|BindingFlags.Instance))
        {Assert.Null(p.SetMethod);Assert.False(p.PropertyType.IsArray);Assert.DoesNotContain(p.PropertyType,new[]{typeof(RawDrawingSnapshot),typeof(RawInsert),typeof(DxfSourceFact),typeof(DxfRawPair)});}
    }
    [Fact] public async Task ForeignTableSubclassCannotOverrideLayerColor()
    {
        var c=await Run(Line(layer:"A-INST"),layers:[Layer("A-INST",2,extra:"100\nForeignSubclass\n62\n5\n420\n255\n")]);
        Assert.Equal(2,c.Root("E").Color.Value!.Value);Assert.Empty(c.Artifact.Layers[0].TrueColor.PairIndices);
    }
}
public sealed class CadStyleResourceTests
{
    [Theory] [InlineData(2,CadStyleStatus.ResourceLimitExceeded)] [InlineData(3,CadStyleStatus.Complete)]
    public async Task OneGlobalProjectionCounterIncludesContextAndChildren(int maximum,CadStyleStatus status)
    {
        var c=await Run(Insert(),Block(Line("E")+Line("F")),limits:new(maximum));
        Assert.Equal(status,c.Result.Status);Assert.Equal(maximum,c.Result.ProjectionsAllocated);
        if(status==CadStyleStatus.ResourceLimitExceeded)Assert.Null(c.Result.Artifact);
    }
    [Theory] [InlineData(1)] [InlineData(3)]
    public async Task DiagnosticExhaustionUsesTerminalSlotAndRefusesPrefix(int maximum)
    {
        var c=await Run(Line(layer:"missing"),limits:new(maxDiagnostics:maximum));
        Assert.Equal(CadStyleStatus.ResourceLimitExceeded,c.Result.Status);Assert.Null(c.Result.Artifact);
        Assert.Equal(maximum,c.Result.Diagnostics.Count);Assert.Equal(CadStyleDiagnosticCode.ResourceLimitExceeded,c.Result.Diagnostics[^1].Code);
    }
    [Fact] public async Task PreCancellationPublishesNoStyleArtifact()
    {
        var c=await Run(Line());using var cts=new CancellationTokenSource();cts.Cancel();
        var r=CadStyleResolver.Resolve(c.Snapshot,c.Expansion,cancellationToken:cts.Token);
        Assert.Equal(CadStyleStatus.Cancelled,r.Status);Assert.Null(r.Artifact);Assert.Equal(0,r.ProjectionsAllocated);
    }
    [Fact] public async Task DifferentSnapshotAuthorityIsRefusedWithoutReexpansion()
    {
        var a=await Run(Line(color:1));var b=await Run(Line(color:2));
        var r=CadStyleResolver.Resolve(a.Snapshot,b.Expansion);Assert.Equal(CadStyleStatus.InvalidInput,r.Status);Assert.Null(r.Artifact);
    }
    [Fact] public async Task PartialOccurrenceCoverageCannotBecomeCompleteStyleCoverage()
    {
        var c=await Run(Insert()+Insert("D","MISSING"),Block(Line()));
        Assert.Equal(CadExpansionStatus.Partial,c.Expansion.Status);Assert.Equal(CadStyleStatus.Partial,c.Result.Status);
        Assert.Single(c.Artifact.OccurrenceContexts);
    }
}
public sealed class Cad04FScopeArchitectureTests
{
    [Fact] public void StyleImplementationDoesNotExpandOccurrencesTransformGeometryOrAssembleDocuments()
    {
        foreach(var file in Directory.GetFiles(Path.Combine(CadV2ContractFixture.Root(),"packages/DtoB.Cad.Normalize/Styles"),"*.cs"))
        {
            var text=File.ReadAllText(file);
            Assert.DoesNotMatch(@"\b(CadOccurrenceExpander|CadOccurrenceIdentity|CadInsertLocalTransform|Transform3|Point3|CadDocument|CadLine|CadArc|CadCircle|CadPolyline|Bulge|Bounds|RevitAPI|Autodesk|ElementId|GraphicsStyle|BuiltInCategory|File|Directory|FileStream)\b",text);
            Assert.DoesNotMatch(@"\.Apply\s*\(|\.Then\s*\(|CadId\.Create|Guid\.NewGuid",text);
        }
        Assert.DoesNotContain(typeof(CadStyleResolver).Assembly.GetReferencedAssemblies(),a=>new[]{"DtoB.Cad.Dxf","DtoB.Bim","DtoB.Desktop","RevitAPI","PresentationFramework"}.Contains(a.Name));
    }
}
