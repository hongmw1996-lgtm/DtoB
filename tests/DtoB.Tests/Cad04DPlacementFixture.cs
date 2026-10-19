using System.Globalization;
using System.Text;
using DtoB.Cad.Dxf;
using DtoB.Cad.Normalize.Placement;
using DtoB.Cad.Raw;
using DtoB.Geometry;
using Xunit;

namespace DtoB.Tests;

internal static class Cad04DPlacementFixture
{
    internal static DtoB.Cad.V2.CadNumericPolicy Policy => Cad04CCoordinateFixture.Policy;
    internal static string Number(double n) => n.ToString("R",CultureInfo.InvariantCulture);
    internal static string Point(int x,Point3 p) => $"{x}\n{Number(p.X)}\n{x+10}\n{Number(p.Y)}\n{x+20}\n{Number(p.Z)}\n";
    internal static string Block(string name = "B",Point3? blockBase = null,string handle = "B",string flags = "0") =>
        $"0\nBLOCK\n5\n{handle}\n330\n0\n2\n{name}\n70\n{flags}\n"+Point(10,blockBase ?? new(0,0,0))+$"0\nENDBLK\n5\n{handle}0\n330\n0\n";
    internal static string Document(string blocks,string insert,string target = "B",int units = 4,string tables = "") =>
        Cad04BScalarFixture.Dxf("0\nINSERT\n5\nA\n330\n0\n2\n"+target+"\n"+insert,
            tables+"0\nSECTION\n2\nBLOCKS\n"+blocks+"0\nENDSEC\n",$"70\n{units}\n");
    internal static string Insert(Point3? insertion = null,Point3? scale = null,double? rotation = null,Point3? normal = null,string extra = "") =>
        Point(10,insertion ?? new(0,0,0))+(scale is {} s ? $"41\n{Number(s.X)}\n42\n{Number(s.Y)}\n43\n{Number(s.Z)}\n" : "")
        +(rotation.HasValue ? $"50\n{Number(rotation.Value)}\n" : "")+(normal is {} n ? Point(210,n) : "")+extra;
    internal static async Task<RawDrawingSnapshot> Snapshot(Point3? blockBase = null,Point3? insertion = null,
        Point3? scale = null,double? rotation = null,Point3? normal = null,int units = 4) =>
        await Cad04BScalarFixture.Snapshot(Document(Block(blockBase:blockBase),Insert(insertion,scale,rotation,normal),units:units));
    internal static RawDrawingRecordView InsertRow(RawDrawingSnapshot s) => s.Records.Single(r=>r.SourceSection=="ENTITIES" && r.BlockAnnotation?.Payload is RawInsert);
    internal static CadLocalPlacementResult Result(RawDrawingSnapshot s) => CadInsertLocalTransform.Create(s,InsertRow(s),Policy);
    internal static CadLocalPlacement Placement(RawDrawingSnapshot s)
    { var r=Result(s); Assert.Equal(CadPlacementStatus.Normalized,r.Status); return Assert.IsType<CadLocalPlacement>(r.Placement); }
    internal static async Task AssertSnapshotRejected(string text)
    {
        using var stream=new MemoryStream(Encoding.UTF8.GetBytes(text));
        var lex=await new TextDxfLexicalReader().ReadAsync(stream); var structural=new TextDxfStructuralReader().Read(lex);
        var annotations=new TextDxfBlockAnnotationReader().Read(new TextDxfGeometryReader().Read(structural));
        var graph=new TextDxfIdentityGraphReader().Read(annotations);
        var result=new TextDxfRawDrawingSnapshotAssembler().Assemble(graph);
        Assert.Equal(RawSnapshotAssemblyStatus.InvalidInput,result.Status); Assert.Null(result.Snapshot);
    }
    internal static void ExpectedPoint(CadLocalPlacement p,Point3 input,Point3 expected)
    {
        var result=CadInsertLocalTransform.ApplyPoint(p,input); Assert.True(result.Succeeded); Assert.NotNull(result.PointMm);
        var a=result.PointMm.Value; Assert.True(Math.Abs(a.X-expected.X)<=Policy.LengthMm,$"X {a.X:R} != {expected.X:R}");
        Assert.True(Math.Abs(a.Y-expected.Y)<=Policy.LengthMm,$"Y {a.Y:R} != {expected.Y:R}"); Assert.True(Math.Abs(a.Z-expected.Z)<=Policy.LengthMm);
    }
    internal static void ExpectedVector(CadLocalPlacement p,Point3 input,Point3 expected)
    {
        var r=CadInsertLocalTransform.ApplyVector(p,new(input.X,input.Y,input.Z)); Assert.True(r.Succeeded); Assert.Null(r.PointMm);
        Cad04CCoordinateFixture.Vector(r.Vector,expected.X,expected.Y,expected.Z);
    }
}
