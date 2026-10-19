using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using DtoB.Cad.Normalize.Occurrences;
using DtoB.Cad.Normalize.Styles;
using DtoB.Cad.V2;
using Xunit;
using static DtoB.Tests.Cad04FStyleFixture;

namespace DtoB.Tests;

public sealed class Cad04FDeterminismTests
{
    internal static object Projection(CadStyleResult result)
    {
        var a=result.Artifact!;
        return new {result.Status,result.ProjectionsAllocated,a.Limits,a.Sources.Source,Records=a.Sources.Records,
            a.Declarations,a.Layers,a.Linetypes,a.Roots,a.OccurrenceContexts,a.Entities,
            Occurrences=a.Input.Expansion!.Occurrences.Select(o=>new{o.Value.Id,o.Value.InsertSource,o.Value.ParentOccurrenceId,o.Value.DefinitionId,o.Value.Path,o.Depth}),result.Diagnostics};
    }
    [Fact] public async Task FixedEffectiveStyleEvidenceMatchesAcrossCulturesProcessesAndWorkingDirectories()
    {
        var previous=CultureInfo.CurrentCulture;var previousUi=CultureInfo.CurrentUICulture;var bytes=new List<byte[]>();
        try
        {
            foreach(var culture in new[]{"","fr-FR","ko-KR"})
            {
                CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo(culture);CultureInfo.CurrentUICulture=CultureInfo.GetCultureInfo(culture);
                var c=await Run(Insert(target:"A",color:1,linetype:"DASHED")+Insert("10","A","C-INST",5,"CONTINUOUS")
                    +Line("F0",color:0,linetype:"BYBLOCK")+Line("F1",layer:"missing",color:3,linetype:"DASHED"),
                    Block(Insert("D","B","0",0,"BYBLOCK")+Line("E",color:256,extra:"370\n-1\n"),"A","B")+Block(Line("F",color:0,linetype:"BYBLOCK"),"B","C"));
                Assert.Equal(10,c.Result.ProjectionsAllocated);Assert.Equal(4,c.Artifact.OccurrenceContexts.Count);Assert.Equal(4,c.Artifact.Entities.Count);
                Assert.Equal(new[]{1,5},c.Artifact.Entities.Where(p=>c.Artifact.Sources.Resolve(p.Key.Source).CanonicalHandle=="F").Select(p=>p.Color.Value!.Value));
                Assert.Equal(CadStyleStatus.Partial,c.Result.Status);
                bytes.Add(JsonSerializer.SerializeToUtf8Bytes(Projection(c.Result)));
            }
        }
        finally {CultureInfo.CurrentCulture=previous;CultureInfo.CurrentUICulture=previousUi;}
        Assert.Equal(bytes[0],bytes[1]);Assert.Equal(bytes[0],bytes[2]);
        Write("effective-style-evidence.json",bytes[0]);
    }
    internal static void Write(string name,byte[] bytes)
    {
        var directory=Environment.GetEnvironmentVariable("DTOB_04F_EVIDENCE_DIRECTORY");
        if(directory is null)return;
        Directory.CreateDirectory(directory);File.WriteAllBytes(Path.Combine(directory,name),bytes);
        File.WriteAllText(Path.Combine(directory,name+".sha256"),Convert.ToHexString(SHA256.HashData(bytes))+"\n");
    }
}
public sealed class Cad04FControlledSourceTests
{
    [Fact] public async Task RealDoorSourceStyleTraceUsesActualPairsAndReportsUnreachableByBlockHonestly()
    {
        var snapshot=await Cad04BScalarFixture.Controlled();var input=Cad04EOccurrenceFixture.Expand(snapshot);
        var before=JsonSerializer.Serialize(snapshot);var occurrenceBefore=JsonSerializer.Serialize(input);
        var result=CadStyleResolver.Resolve(snapshot,input);var a=Assert.IsType<CadStyleArtifact>(result.Artifact);
        Assert.Equal(12,a.Layers.Count);Assert.Equal(17,a.OccurrenceContexts.Count);Assert.Equal(26,a.Entities.Count);Assert.Equal(97,a.Roots.Count);
        Assert.Equal(140,result.ProjectionsAllocated);
        var c3=Assert.Single(a.OccurrenceContexts,p=>a.Sources.Resolve(p.Key.Source).CanonicalHandle=="C3");
        var line=Assert.Single(a.Entities,p=>a.Sources.Resolve(p.Key.Source).CanonicalHandle=="9C"&&p.Key.OccurrenceId!.Value==c3.Key.OccurrenceId!.Value);
        Assert.Equal("A-DOOR",line.Declared.Layer.Text);Assert.Equal("A-DOOR",line.Layer.Value);
        Assert.Equal(CadDeclaredColorMode.Absent,line.Declared.ColorMode);Assert.Equal(CadColorKind.Aci,line.Color.Value!.Kind);Assert.Equal(3,line.Color.Value.Value);
        Assert.Equal("Continuous",line.Linetype.Value);Assert.Equal(new[]{2860},line.Declared.Layer.PairIndices);
        Assert.Contains(line.Color.Evidence,f=>f.GroupCode==62&&f.Integer==3&&f.PairIndices.SequenceEqual(new[]{1164}));
        Assert.Equal(2856,a.Sources.Resolve(line.Key.Source).Locator.FirstPair);Assert.Equal(new[]{3764},c3.Declared.Layer.PairIndices);
        Assert.All(a.Entities,p=>Assert.NotEqual("0",p.Declared.Layer.Text));
        Assert.DoesNotContain(a.Entities,p=>p.Declared.ColorMode==CadDeclaredColorMode.ByBlock);
        Assert.Equal(3,a.Declarations.Count(d=>d.ColorMode==CadDeclaredColorMode.ByBlock));
        Assert.DoesNotContain(a.Roots,p=>p.Declared.ColorMode!=CadDeclaredColorMode.Absent);
        Assert.Equal(before,JsonSerializer.Serialize(snapshot));Assert.Equal(occurrenceBefore,JsonSerializer.Serialize(input));
        Cad04FDeterminismTests.Write("controlled-style-evidence.json",JsonSerializer.SerializeToUtf8Bytes(Cad04FDeterminismTests.Projection(result)));
    }
}
