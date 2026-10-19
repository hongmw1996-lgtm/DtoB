using DtoB.Cad.Normalize.Occurrences;
using DtoB.Cad.Normalize.Styles;
using DtoB.Cad.Raw;
using Xunit;

namespace DtoB.Tests;

internal sealed record Cad04FCase(RawDrawingSnapshot Snapshot,CadOccurrenceExpansionResult Expansion,CadStyleResult Result)
{
    internal CadStyleArtifact Artifact=>Assert.IsType<CadStyleArtifact>(Result.Artifact);
    internal CadEffectiveStyle Root(string handle)=>Assert.Single(Artifact.Roots,r=>Artifact.Sources.Resolve(r.Key.Source).CanonicalHandle==handle);
    internal CadEffectiveStyle Context(string handle)=>Assert.Single(Artifact.OccurrenceContexts,r=>Artifact.Sources.Resolve(r.Key.Source).CanonicalHandle==handle);
    internal CadEffectiveStyle Child(string handle,string insertHandle)=>Assert.Single(Artifact.Entities,r=>Artifact.Sources.Resolve(r.Key.Source).CanonicalHandle==handle
        &&Artifact.Input.Expansion!.Occurrences.Any(o=>o.Value.Id.Value==r.Key.OccurrenceId!.Value&&Artifact.Sources.Resolve(o.Value.InsertSource).CanonicalHandle==insertHandle));
}
internal static class Cad04FStyleFixture
{
    internal static string Common(string layer="0",int? color=null,string? linetype=null,int? rgb=null,string extra="")=>
        "8\n"+layer+"\n"+(color.HasValue?$"62\n{color.Value}\n":"")+(linetype is null?"":"6\n"+linetype+"\n")+(rgb.HasValue?$"420\n{rgb.Value}\n":"")+extra;
    internal static string Line(string handle="E",string layer="0",int? color=null,string? linetype=null,int? rgb=null,string extra="")=>
        $"0\nLINE\n5\n{handle}\n330\n0\n100\nAcDbEntity\n"+Common(layer,color,linetype,rgb,extra)+"100\nAcDbLine\n10\n0\n20\n0\n11\n1\n21\n1\n";
    internal static string Insert(string handle="A",string target="B",string layer="A-INST",int? color=null,string? linetype=null,int? rgb=null,string extra="")=>
        Cad04EOccurrenceFixture.Insert(handle,target,extra:Common(layer,color,linetype,rgb,extra));
    internal static string Block(string children,string name="B",string handle="B")=>Cad04EOccurrenceFixture.Block(handle,name,children);
    internal static string Layer(string name,int color=7,string linetype="CONTINUOUS",int flags=0,int? rgb=null,string extra="")=>
        "2\n"+name+"\n70\n"+flags+"\n62\n"+color+"\n6\n"+linetype+"\n"+(rgb.HasValue?$"420\n{rgb.Value}\n":"")+extra;
    internal static async Task<Cad04FCase> Run(string roots,string blocks="",string[]? layers=null,string[]? types=null,CadStyleLimits? limits=null)
    {
        layers??=[Layer("0"),Layer("A-INST",1),Layer("C-INST",5),Layer("B-ENTITY",3)];
        types??=["CONTINUOUS","DASHED"];
        var tables="0\nSECTION\n2\nTABLES\n0\nTABLE\n2\nLTYPE\n70\n"+types.Length+"\n";
        tables+=string.Concat(types.Select((name,i)=>$"0\nLTYPE\n5\n{(512+i):X}\n100\nAcDbSymbolTableRecord\n100\nAcDbLinetypeTableRecord\n2\n{name}\n70\n0\n3\nfixture\n72\n65\n73\n0\n40\n0\n"));
        tables+="0\nENDTAB\n0\nTABLE\n2\nLAYER\n70\n"+layers.Length+"\n";
        tables+=string.Concat(layers.Select((fields,i)=>$"0\nLAYER\n5\n{(256+i):X}\n100\nAcDbSymbolTableRecord\n100\nAcDbLayerTableRecord\n"+fields));
        tables+="0\nENDTAB\n0\nENDSEC\n";
        if(blocks.Length>0)tables+="0\nSECTION\n2\nBLOCKS\n"+blocks+"0\nENDSEC\n";
        var snapshot=await Cad04BScalarFixture.Snapshot(Cad04BScalarFixture.Dxf(roots,tables));
        var expansion=Cad04EOccurrenceFixture.Expand(snapshot);
        Assert.NotNull(expansion.Expansion);
        return new(snapshot,expansion,CadStyleResolver.Resolve(snapshot,expansion,limits));
    }
}
