using DtoB.Cad.Normalize.Styles;
using DtoB.Cad.V2;
using Xunit;
using static DtoB.Tests.Cad04FStyleFixture;

namespace DtoB.Tests;

public sealed class CadLayerResolutionTests
{
    [Fact] public async Task ModelSpaceLayerZeroIsTheActualLayerWithoutFakeAncestry()
    {var c=await Run(Line());var p=c.Root("E");Assert.Equal("0",p.Layer.Value);Assert.Equal(CadResolutionState.Known,p.Layer.State);Assert.Null(p.Key.OccurrenceId);Assert.Null(p.Layer.SupportingContext);}
    [Theory] [InlineData("A-INST")] [InlineData("a-inst")]
    public async Task ModelSpaceExplicitNameUsesOrdinalIgnoreCaseCatalog(string name)
    {var c=await Run(Line(layer:name));Assert.Equal("A-INST",c.Root("E").Layer.Value);}
    [Fact] public async Task NonzeroDefinitionEntityRetainsItsOwnLayer()
    {var c=await Run(Insert(),Block(Line(layer:"B-ENTITY")));Assert.Equal("B-ENTITY",c.Child("E","A").Layer.Value);Assert.Equal(3,c.Child("E","A").Color.Value!.Value);}
    [Fact] public async Task LayerZeroInBlockInheritsContainingInsertEffectiveLayerBeforeByLayer()
    {var c=await Run(Insert(),Block(Line()));var p=c.Child("E","A");Assert.Equal("A-INST",p.Layer.Value);Assert.Equal(1,p.Color.Value!.Value);Assert.Equal(CadResolutionState.Inherited,p.Layer.State);Assert.Equal("DXF.Layer.ZeroFromInsert",p.Layer.RuleId);}
    [Fact] public async Task NestedLayerZeroUsesAlreadyResolvedImmediateContext()
    {
        var c=await Run(Insert(target:"A"),Block(Insert("D","B","0"),"A","B")+Block(Line(),"B","C"));
        Assert.Equal("A-INST",c.Context("D").Layer.Value);Assert.Equal("A-INST",c.Child("E","D").Layer.Value);
    }
    [Fact] public async Task MissingLayerIsExplicitAndIndependentExplicitColorSurvives()
    {var c=await Run(Line(layer:"MISSING",color:2));Assert.Equal(CadStyleStatus.Partial,c.Result.Status);Assert.Null(c.Root("E").Layer.Value);Assert.Equal(2,c.Root("E").Color.Value!.Value);Assert.Contains(c.Result.Diagnostics,d=>d.Code==CadStyleDiagnosticCode.MissingLayerDefinition);}
    [Fact] public async Task CaseCollisionIsAmbiguousAndDoesNotPickFirst()
    {var c=await Run(Line(layer:"a"),layers:[Layer("A",1),Layer("a",5)]);Assert.Null(c.Root("E").Layer.Value);Assert.Contains(c.Result.Diagnostics,d=>d.Code==CadStyleDiagnosticCode.AmbiguousLayerDefinition);}
    [Fact] public async Task NamesAreNotSilentlyTrimmedAndLayerZeroIsNotInvented()
    {var c=await Run(Line(layer:" A-INST"));Assert.Null(c.Root("E").Layer.Value);var zero=await Run(Line(),layers:[]);Assert.Null(zero.Root("E").Layer.Value);}
    [Fact] public async Task AbsentLayerRemainsAbsentRatherThanDefaultingToZero()
    {var c=await Run(Line().Replace("8\n0\n","",StringComparison.Ordinal));Assert.Equal(CadResolutionState.Absent,c.Root("E").Layer.State);Assert.Null(c.Root("E").Layer.Value);}
    [Theory] [InlineData("i",true)] [InlineData("ı",false)]
    public async Task SymbolComparisonDoesNotUseTurkishCultureFolding(string name,bool matched)
    {
        var previous=System.Globalization.CultureInfo.CurrentCulture;
        try {System.Globalization.CultureInfo.CurrentCulture=System.Globalization.CultureInfo.GetCultureInfo("tr-TR");var c=await Run(Line(layer:name),layers:[Layer("I")]);Assert.Equal(matched,c.Root("E").Layer.Value is not null);}
        finally {System.Globalization.CultureInfo.CurrentCulture=previous;}
    }
}
public sealed class CadByLayerResolutionTests
{
    [Theory] [InlineData(null,CadResolutionState.Defaulted)] [InlineData(256,CadResolutionState.Inherited)]
    public async Task DefaultAndExplicitByLayerRetainDifferentSourceFacts(int? aci,CadResolutionState state)
    {var c=await Run(Line(layer:"A-INST",color:aci));Assert.Equal(1,c.Root("E").Color.Value!.Value);Assert.Equal(state,c.Root("E").Color.State);}
    [Fact] public async Task NonzeroNestedInsertOverridesInheritedLayerContextForItsChildren()
    {
        var c=await Run(Insert(target:"A"),Block(Insert("D","B","C-INST",256),"A","B")+Block(Line(color:256),"B","C"));
        Assert.Equal("C-INST",c.Context("D").Layer.Value);Assert.Equal(5,c.Child("E","D").Color.Value!.Value);
    }
}
public sealed class CadByBlockResolutionTests
{
    [Fact] public async Task ExplicitParentChannelCanBeInheritedWithoutOverwritingChildLayer()
    {var c=await Run(Insert(color:2,linetype:"DASHED"),Block(Line(layer:"B-ENTITY",color:0,linetype:"BYBLOCK")));var p=c.Child("E","A");Assert.Equal("B-ENTITY",p.Layer.Value);Assert.Equal(2,p.Color.Value!.Value);Assert.Equal("DASHED",p.Linetype.Value);}
    [Fact] public async Task NestedChainPropagatesResolvedColorAndLinetypeThroughEachRealInsert()
    {
        // A explicitACI2/DASHED -> D ByBlock -> E ByBlock. E's nonzero layer cannot replace explicit parent channels.
        var c=await Run(Insert(target:"A",color:2,linetype:"DASHED"),Block(Insert("D","B","0",0,"BYBLOCK"),"A","B")+Block(Line(layer:"B-ENTITY",color:0,linetype:"BYBLOCK"),"B","C"));
        var p=c.Child("E","D");Assert.Equal(2,p.Color.Value!.Value);Assert.Equal("DASHED",p.Linetype.Value);Assert.Equal(c.Context("D").Key.OccurrenceId!.Value,p.Color.SupportingContext!.OccurrenceId!.Value);
    }
    [Fact] public async Task RootByBlockStaysUnresolvedUnderAcceptedPolicyWithoutWhiteOrContinuousFallback()
    {var c=await Run(Line(color:0,linetype:"BYBLOCK"));Assert.Null(c.Root("E").Color.Value);Assert.Null(c.Root("E").Linetype.Value);Assert.Equal(2,c.Result.Diagnostics.Count(d=>d.Code==CadStyleDiagnosticCode.UnresolvedByBlockContext));}
    [Fact] public async Task NestedByLayerThenByBlockUsesIntermediateInsertLayer()
    {
        var c=await Run(Insert(target:"A",color:1),Block(Insert("D","B","C-INST",256),"A","B")+Block(Line(color:0),"B","C"));
        Assert.Equal(5,c.Child("E","D").Color.Value!.Value);Assert.Equal("C-INST",c.Child("E","D").Layer.Value);
    }
}
