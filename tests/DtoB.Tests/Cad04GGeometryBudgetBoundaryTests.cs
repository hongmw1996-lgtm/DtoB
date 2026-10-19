using System.Security.Cryptography;
using System.Text;
using DtoB.Cad.Dxf;
using DtoB.Cad.Normalize;
using DtoB.Cad.Raw;
using Xunit;

namespace DtoB.Tests;

public sealed class Cad04GGeometryBudgetBoundaryTests
{
    [Theory][InlineData(120,249999,true)][InlineData(121,250000,true)][InlineData(122,250001,false)]
    public async Task RealNormalizerHonorsFullDevelopmentBoundaryWithoutPublishingPrefix(int roots,int requested,bool allowed)
    {
        //511 shared POINT payloads +488*511 occurrence leaves +roots direct POINT payloads.
        //Style projections (488*512+roots) stay below their independent250000 cap.
        //Only existing TEST reader options are raised:633 source geometry rows and511 children; no production default changes.
        var children=new StringBuilder();for(var i=0;i<511;i++)children.Append(Cad04GFixture.Entity("POINT","10\n1\n20\n2\n30\n3\n",(0x1000+i).ToString("X",System.Globalization.CultureInfo.InvariantCulture)));
        var rootText=new StringBuilder(Cad04EOccurrenceFixture.Insert("A","B",columns:488,extra:"8\n0\n"));
        for(var i=0;i<roots;i++)rootText.Append(Cad04GFixture.Entity("POINT","10\n4\n20\n5\n30\n6\n",(0x2000+i).ToString("X",System.Globalization.CultureInfo.InvariantCulture)));
        var tables="0\nSECTION\n2\nTABLES\n0\nTABLE\n2\nLTYPE\n70\n1\n0\nLTYPE\n5\nF001\n2\nCONTINUOUS\n70\n0\n3\nfixture\n72\n65\n73\n0\n40\n0\n0\nENDTAB\n0\nTABLE\n2\nLAYER\n70\n1\n0\nLAYER\n5\nF002\n2\n0\n70\n0\n62\n7\n6\nCONTINUOUS\n0\nENDTAB\n0\nENDSEC\n";
        var block="0\nSECTION\n2\nBLOCKS\n"+Cad04EOccurrenceFixture.Block("B","B",children.ToString())+"0\nENDSEC\n";
        var bytes=Encoding.UTF8.GetBytes(Cad04BScalarFixture.Dxf(rootText.ToString(),tables+block));
        using var input=new MemoryStream(bytes);var lexical=await new TextDxfLexicalReader().ReadAsync(input);
        var structure=new TextDxfStructuralReader().Read(lexical);
        var geometry=new TextDxfGeometryReader(new(){MaxGeometryRecords=800}).Read(structure);
        var annotation=new TextDxfBlockAnnotationReader(new(){MaxChildrenPerBlock=600}).Read(geometry);
        var authority=Cad04BScalarFixture.Document;
        var graph=new TextDxfIdentityGraphReader().Read(annotation,new(authority.DrawingId.ToString(),authority.RevisionId.ToString(),Convert.ToHexString(SHA256.HashData(bytes))));
        var assembly=new TextDxfRawDrawingSnapshotAssembler().Assemble(graph);
        Assert.Equal(RawSnapshotAssemblyStatus.Succeeded,assembly.Status);Assert.NotNull(assembly.Snapshot);
        var result=Cad04GFixture.Normalize(assembly.Snapshot!);
        if(allowed)
        {
            var d=Cad04GFixture.Document(result);Assert.Equal(511+roots,d.Geometry.Count);Assert.Equal(488,d.Occurrences.Count);Assert.Equal(488*511+roots,d.ModelSpace.Count);
            Assert.Equal(requested,d.Geometry.Count+488*511);
        }
        else
        {Assert.Equal(CadNormalizationStatus.ResourceLimitExceeded,result.Status);Assert.Null(result.Document);Assert.Contains(result.Diagnostics,x=>x.Code=="CAD_GEOMETRY_LIMIT"&&x.Arguments[0]=="250000");}
    }
}
