using DtoB.Cad.Normalize;
using System.Security.Cryptography;
using System.Text;
using DtoB.Cad.Dxf;
using DtoB.Cad.Normalize.Occurrences;
using DtoB.Cad.Raw;
using DtoB.Geometry;
using Xunit;

namespace DtoB.Tests;

internal static class Cad04EOccurrenceFixture
{
    internal static string Insert(string handle,string target,Point3? point = null,Point3? scale = null,double? angle = null,
        Point3? normal = null,int? rows = null,int? columns = null,double? rowSpacing = null,double? columnSpacing = null,string extra = "") =>
        $"0\nINSERT\n5\n{handle}\n330\n0\n2\n{target}\n"+Cad04DPlacementFixture.Insert(point,scale,angle,normal)
        +(rows.HasValue?$"71\n{rows.Value}\n":"")+(columns.HasValue?$"70\n{columns.Value}\n":"")
        +(rowSpacing.HasValue?$"45\n{Cad04DPlacementFixture.Number(rowSpacing.Value)}\n":"")
        +(columnSpacing.HasValue?$"44\n{Cad04DPlacementFixture.Number(columnSpacing.Value)}\n":"")+extra;
    internal static string Block(string handle,string name,string children = "",Point3? point = null) =>
        Cad04DPlacementFixture.Block(name,point,handle).Replace("0\nENDBLK\n",children+"0\nENDBLK\n",StringComparison.Ordinal);
    internal static async Task<RawDrawingSnapshot> Snapshot(string blocks,string roots,int units = 4)
    {
        var bytes=Encoding.UTF8.GetBytes(Cad04BScalarFixture.Dxf(roots,"0\nSECTION\n2\nBLOCKS\n"+blocks+"0\nENDSEC\n",$"70\n{units}\n"));
        using var stream=new MemoryStream(bytes);
        var lexical=await new TextDxfLexicalReader().ReadAsync(stream);
        var structure=new TextDxfStructuralReader().Read(lexical);
        var geometry=new TextDxfGeometryReader().Read(structure);
        // Test-only source capacity: depth33 needs33 definitions; production PHASE03 defaults stay unchanged.
        var annotations=new TextDxfBlockAnnotationReader(new(){MaxDefinitions=64}).Read(geometry);
        var doc=Cad04BScalarFixture.Document;
        var graph=new TextDxfIdentityGraphReader().Read(annotations,new(doc.DrawingId.ToString(),doc.RevisionId.ToString(),Convert.ToHexString(SHA256.HashData(bytes))));
        var result=new TextDxfRawDrawingSnapshotAssembler().Assemble(graph);
        Assert.Equal(RawSnapshotAssemblyStatus.Succeeded,result.Status); return Assert.IsType<RawDrawingSnapshot>(result.Snapshot);
    }
    internal static CadNormalizationOptions Options(int depth = 32,int cells = 10_000,int total = 100_000,int diagnostics = 5_000) =>
        new("04E-analytic",new(depth,cells,total,250_000,diagnostics));
    internal static CadOccurrenceExpansionResult Expand(RawDrawingSnapshot snapshot,CadNormalizationOptions? options = null,CancellationToken token = default) =>
        CadOccurrenceExpander.Expand(snapshot,options ?? Options(),Cad04DPlacementFixture.Policy,token);
    internal static CadOccurrenceExpansion Complete(CadOccurrenceExpansionResult result)
    { Assert.Equal(CadExpansionStatus.Complete,result.Status); return Assert.IsType<CadOccurrenceExpansion>(result.Expansion); }
    internal static void Point(CadExpandedOccurrence occurrence,Point3 input,Point3 expected)
    {
        var p=occurrence.Value.AccumulatedTransform.ToTransform3().Apply(input); var e=Cad04DPlacementFixture.Policy.LengthMm;
        Assert.True(Math.Abs(p.X-expected.X)<=e,$"X {p.X:R} != {expected.X:R}"); Assert.True(Math.Abs(p.Y-expected.Y)<=e,$"Y {p.Y:R} != {expected.Y:R}"); Assert.True(Math.Abs(p.Z-expected.Z)<=e);
    }
    internal static async Task<RawDrawingSnapshot> Nested() => await Snapshot(
        Block("B","A",Insert("D","B",new(10,0,0),new(2,1,1)))+Block("C","B"),Insert("A","A",new(100,0,0),angle:90));
}
