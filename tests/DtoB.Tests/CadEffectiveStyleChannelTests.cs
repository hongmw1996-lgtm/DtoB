using DtoB.Cad.Normalize.Styles;
using DtoB.Cad.V2;
using Xunit;
using static DtoB.Tests.Cad04FStyleFixture;

namespace DtoB.Tests;

public sealed class CadEffectiveColorTests
{
    [Theory] [InlineData(1)] [InlineData(255)]
    public async Task ExplicitAciIsFactualIndexWithoutPaletteApproximation(int aci)
    {var c=await Run(Line(color:aci));Assert.Equal(CadColorKind.Aci,c.Root("E").Color.Value!.Kind);Assert.Equal(aci,c.Root("E").Color.Value!.Value);}
    [Theory] [InlineData(-1)] [InlineData(257)] [InlineData(int.MinValue)]
    public async Task InvalidOrUnsupportedAciNeverBecomesPaletteValue(int aci)
    {var c=await Run(Line(color:aci));Assert.Equal(CadResolutionState.Unsupported,c.Root("E").Color.State);Assert.Contains(c.Result.Diagnostics,d=>d.Code==CadStyleDiagnosticCode.InvalidColor);}
    [Theory] [InlineData(1)] [InlineData(0)] [InlineData(256)]
    public async Task TrueColorPrecedesSimultaneousAciModesIncludingBlack(int aci)
    {var c=await Run(Line(color:aci,rgb:0));Assert.Equal(CadDeclaredColorMode.Rgb,c.Root("E").Declared.ColorMode);Assert.Equal(CadColorKind.Rgb,c.Root("E").Color.Value!.Kind);Assert.Equal(0,c.Root("E").Color.Value!.Value);}
    [Theory] [InlineData(-1)] [InlineData(0x1000000)]
    public async Task InvalidPresentTrueColorDoesNotFallBackToValidAci(int rgb)
    {var c=await Run(Line(color:1,rgb:rgb));Assert.Null(c.Root("E").Color.Value);}
    [Fact] public async Task LayerTrueColorOverridesAciAndPropagatesByBlock()
    {var c=await Run(Insert(),Block(Line(color:0)),layers:[Layer("0"),Layer("A-INST",1,rgb:0x123456)]);Assert.Equal(CadColorKind.Rgb,c.Child("E","A").Color.Value!.Kind);Assert.Equal(0x123456,c.Child("E","A").Color.Value!.Value);}
    [Fact] public async Task NegativeLayerAciRetainsOffAndFrozenWithoutDroppingStyle()
    {var c=await Run(Line(layer:"A-INST"),layers:[Layer("A-INST",-5,flags:1)]);Assert.Equal(5,c.Root("E").Color.Value!.Value);Assert.True(c.Artifact.Layers[0].IsOff);Assert.True(c.Artifact.Layers[0].IsFrozen);Assert.Single(c.Artifact.Roots);}
    [Theory] [InlineData("62\n1\n62\n2\n")] [InlineData("62\nno\n")] [InlineData("420\n1\n420\n2\n")]
    public async Task DuplicateOrMalformedCommonColorIsNotFirstWins(string extra)
    {var c=await Run(Line(extra:extra));Assert.Null(c.Root("E").Color.Value);Assert.Equal(CadDeclaredColorMode.Invalid,c.Root("E").Declared.ColorMode);}
    [Fact] public async Task ForeignNativeBackgroundControlsAndXdataCannotBecomeEntityTrueColor()
    {
        var c=await Run(Line(color:2,extra:"102\n{APP\n420\n16711680\n102\n}\n")+"100\nForeignSubclass\n420\n255\n1001\nAPP\n420\n65280\n");
        Assert.Equal(2,c.Root("E").Color.Value!.Value);Assert.Empty(c.Root("E").Declared.TrueColor.PairIndices);
    }
    [Fact] public async Task MtextBackgroundTrueColorIsNotItsEntityColor()
    {
        var c=await Run("0\nMTEXT\n5\nE\n330\n0\n100\nAcDbEntity\n8\n0\n62\n3\n100\nAcDbMText\n10\n0\n20\n0\n40\n2\n1\ntext\n420\n16711680\n");
        Assert.Equal(3,c.Root("E").Color.Value!.Value);Assert.Empty(c.Root("E").Declared.TrueColor.PairIndices);
    }
    [Fact] public async Task MissingLayerColorAndMalformedLayerRgbNeverInventAciSeven()
    {
        var missing=await Run(Line(),layers:[Layer("0").Replace("62\n7\n","",StringComparison.Ordinal)]);
        Assert.Null(missing.Root("E").Color.Value);Assert.Equal("0",missing.Root("E").Layer.Value);
        var invalid=await Run(Line(),layers:[Layer("0",1,rgb:-1)]);Assert.Null(invalid.Root("E").Color.Value);
    }
}
public sealed class CadEffectiveLinetypeTests
{
    [Theory] [InlineData(null,"CONTINUOUS")] [InlineData("bylayer","CONTINUOUS")] [InlineData("dashed","DASHED")]
    public async Task DeclaredNameAndDefaultModesResolveCatalogNameOnly(string? name,string expected)
    {
        var c=await Run(Line(linetype:name));Assert.Equal(expected,c.Root("E").Linetype.Value);Assert.NotNull(c.Root("E").Linetype.CatalogRecord);
        Assert.All(c.Artifact.Linetypes,t=>{Assert.Null(t.IsOff);Assert.Null(t.IsFrozen);});
    }
    [Fact] public async Task MissingExplicitLtypeIsNotSubstitutedWithContinuous()
    {var c=await Run(Line(linetype:"MISSING"));Assert.Null(c.Root("E").Linetype.Value);Assert.Contains(c.Result.Diagnostics,d=>d.Code==CadStyleDiagnosticCode.MissingLinetypeDefinition);}
    [Fact] public async Task CaseCollisionInLtypeCatalogIsAmbiguous()
    {var c=await Run(Line(linetype:"dashed"),types:["DASHED","dashed"]);Assert.Null(c.Root("E").Linetype.Value);Assert.Contains(c.Result.Diagnostics,d=>d.Code==CadStyleDiagnosticCode.AmbiguousLinetypeDefinition);}
    [Fact] public async Task LayerLtypeSelectsItsOwnNamedRecord()
    {var c=await Run(Line(layer:"A-INST"),layers:[Layer("A-INST",1,"DASHED")]);Assert.Equal("DASHED",c.Root("E").Linetype.Value);Assert.Contains(c.Root("E").Linetype.Evidence,f=>f.Source.Index==c.Artifact.Layers[0].Source.Index);}
    [Fact] public async Task DuplicateEntityLinetypeStaysUnsupported()
    {var c=await Run(Line(linetype:"DASHED",extra:"6\nCONTINUOUS\n"));Assert.Null(c.Root("E").Linetype.Value);}
    [Fact] public async Task MissingLayerLinetypeDoesNotDefaultToContinuous()
    {var c=await Run(Line(),layers:[Layer("0").Replace("6\nCONTINUOUS\n","",StringComparison.Ordinal)]);Assert.Null(c.Root("E").Linetype.Value);Assert.Equal(7,c.Root("E").Color.Value!.Value);}
}
public sealed class CadStylePartialResolutionTests
{
    [Fact] public async Task DeferredChannelsPreserveFactsAndValidChannelsWithoutFalseComplete()
    {
        var c=await Run(Line(color:3,extra:"370\n-1\n440\n33554559\n"));var p=c.Root("E");
        Assert.Equal(CadStyleStatus.Partial,c.Result.Status);Assert.Equal(3,p.Color.Value!.Value);Assert.Equal("0",p.Layer.Value);
        Assert.Equal(CadResolutionState.Unsupported,p.LineweightState);Assert.Equal(CadResolutionState.Unsupported,p.TransparencyState);
        Assert.Equal(-1,p.Declared.Lineweight.Integer);Assert.Equal(33554559,p.Declared.Transparency.Integer);
    }
    [Fact] public async Task ColorBookWithoutRgbIsUnsupportedRatherThanGuessed()
    {var c=await Run(Line(color:1,extra:"430\nBOOK$Red\n"));Assert.Null(c.Root("E").Color.Value);Assert.Equal(CadStyleStatus.Partial,c.Result.Status);}
    [Fact] public async Task InvalidLinetypeDoesNotDestroyValidColorAndLayer()
    {var c=await Run(Line(color:4,linetype:"missing"));Assert.Equal(4,c.Root("E").Color.Value!.Value);Assert.Equal("0",c.Root("E").Layer.Value);Assert.Null(c.Root("E").Linetype.Value);}
    [Fact] public async Task DeferredLayerChannelHasExplicitStateAndEvidence()
    {
        var c=await Run(Line(),layers:[Layer("0",extra:"370\n-3\n440\n33554559\n")]);
        Assert.Equal(CadResolutionState.Unsupported,c.Artifact.Layers[0].LineweightState);
        Assert.Equal(CadResolutionState.Unsupported,c.Artifact.Layers[0].TransparencyState);
        Assert.Equal(CadStyleStatus.Partial,c.Result.Status);Assert.Equal(7,c.Root("E").Color.Value!.Value);
    }
    [Theory] [InlineData(null,true)] [InlineData(0,true)] [InlineData(1,false)] [InlineData(2,null)]
    public async Task VisibilityIsPreservedAndNeverFiltersProjection(int? visibility,bool? expected)
    {var c=await Run(Line(extra:visibility.HasValue?$"60\n{visibility}\n":""));Assert.Single(c.Artifact.Roots);Assert.Equal(expected,c.Root("E").Declared.SourceVisible);}
}
