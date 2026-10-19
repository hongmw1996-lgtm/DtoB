using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using DtoB.Cad.Normalize;
using DtoB.Cad.Normalize.Assembly;
using DtoB.Cad.V2;
using Xunit;

namespace DtoB.Tests;

public sealed class Cad04GEvidenceTests
{
    [Fact] public async Task ControlledNormalizedDocumentIsByteIdenticalAcrossCultures()
    {
        var previous=CultureInfo.CurrentCulture;var previousUi=CultureInfo.CurrentUICulture;byte[]? first=null;string? firstBounds=null;
        try
        {
            foreach(var name in new[]{"","fr-FR","ko-KR"})
            {
                CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo(name);CultureInfo.CurrentUICulture=CultureInfo.GetCultureInfo(name);
                var d=Cad04GFixture.Document(Cad04GFixture.Normalize(await Cad04BScalarFixture.Controlled()));
                Assert.Equal(333,d.Provenance.Records.Count);Assert.Equal(17,d.Occurrences.Count);Assert.Equal(123,d.ModelSpace.Count);
                var line=Assert.Single(d.Geometry.OfType<CadLine>(),g=>d.Provenance.Resolve(g.Source).CanonicalHandle=="9C");Assert.Equal(900,line.EndMm.X);
                var bytes=CadIrJson.SerializeUtf8(d);var bounds=System.Text.Json.JsonSerializer.Serialize(new CadBoundsQuery(d).Model());
                if(first is null){first=bytes;firstBounds=bounds;}else{Assert.Equal(first,bytes);Assert.Equal(firstBounds,bounds);}
                var dir=Environment.GetEnvironmentVariable("DTOB_04G_EVIDENCE_DIRECTORY");
                if(dir is not null){Directory.CreateDirectory(dir);File.WriteAllBytes(Path.Combine(dir,"normalized-"+(name.Length==0?"invariant":name)+".json"),bytes);File.WriteAllText(Path.Combine(dir,"normalized-bounds.json"),bounds);}
            }
        }
        finally{CultureInfo.CurrentCulture=previous;CultureInfo.CurrentUICulture=previousUi;}
    }
    [Fact] public async Task HeaderExtentsCannotAlterGeometryDerivedModelBounds()
    {
        var bytes=await File.ReadAllBytesAsync(Path.Combine(CadV2ContractFixture.Root(),"datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf"));
        var text=Encoding.UTF8.GetString(bytes);var lines=text.Replace("\r\n","\n",StringComparison.Ordinal).Split('\n');
        var count=0;
        for(var j=0;j+1<lines.Length;j+=2)
        {
            if(lines[j].Trim()=="9"&&lines[j+1] is "$EXTMIN" or "$EXTMAX")
            {
                var sign=lines[j+1]=="$EXTMIN"?"-":"";
                for(var k=j+2;k+1<lines.Length&&lines[k].Trim()!="9";k+=2)if(lines[k].Trim() is "10" or "20" or "30"){lines[k+1]=sign+"123456789";count++;}
            }
        }
        Assert.Equal(6,count);
        var original=Cad04GFixture.Document(Cad04GFixture.Normalize(await Cad04BScalarFixture.Snapshot(bytes)));
        var changed=Cad04GFixture.Document(Cad04GFixture.Normalize(await Cad04BScalarFixture.Snapshot(string.Join('\n',lines))));
        Assert.NotEqual(original.Provenance.Source.FileHash,changed.Provenance.Source.FileHash);
        Assert.Equal(new CadBoundsQuery(original).Model(),new CadBoundsQuery(changed).Model());
    }
    [Fact] public void LeafIdMatchesIndependentBinaryWriterAndContainsOnlySourceAndOccurrence()
    {
        var source=new CadId("cad2:"+new string('a',64));var occurrence=new CadId("cad2:"+new string('b',64));
        byte[] Field(byte tag,byte[] payload){using var s=new MemoryStream();s.WriteByte(tag);Span<byte> n=stackalloc byte[4];BinaryPrimitives.WriteUInt32BigEndian(n,(uint)payload.Length);s.Write(n);s.Write(payload);return s.ToArray();}
        byte[] Text(string s)=>Field(1,Encoding.UTF8.GetBytes(s));
        byte[] Tuple(params byte[][] fields){using var s=new MemoryStream();Span<byte> n=stackalloc byte[4];BinaryPrimitives.WriteUInt32BigEndian(n,(uint)fields.Length);s.Write(n);foreach(var f in fields)s.Write(f);return Field(6,s.ToArray());}
        var frame=Tuple(Text("dtob/cad2/id/v1"),Text("leaf"),Tuple(Text(source.Value),Text(occurrence.Value)));
        Assert.Equal("cad2:"+Convert.ToHexString(SHA256.HashData(frame)).ToLowerInvariant(),CadAssemblyIdentity.Leaf(source,occurrence).Value);
        Assert.NotEqual(CadAssemblyIdentity.Leaf(source,occurrence).Value,CadAssemblyIdentity.Leaf(occurrence,source).Value);
    }
    [Fact] public void AssemblyHasNoIoVendorRecognitionOrSchemaChanges()
    {
        var dir=Path.Combine(CadV2ContractFixture.Root(),"packages/DtoB.Cad.Normalize/Assembly");
        var code=string.Join('\n',Directory.GetFiles(dir,"*.cs").Select(File.ReadAllText));
        foreach(var banned in new[]{"File.","Directory.","FileStream","Autodesk.Revit","PresentationFramework","WallCandidate","DoorCandidate","WindowCandidate","RoomCandidate","RevitAPI","DtoB.Cad.Dxf","Task.Run"})Assert.DoesNotContain(banned,code,StringComparison.Ordinal);
        Assert.Equal(typeof(CadNormalizer),Assert.Single(typeof(ICadNormalizer).Assembly.GetTypes(),t=>t.IsClass&&typeof(ICadNormalizer).IsAssignableFrom(t)));
        Assert.Equal(2,CadContract.SchemaMajor);Assert.Null(typeof(CadBlockOccurrence).GetProperty("LocalOrientationSign"));Assert.Null(typeof(CadDocument).GetProperty("Bounds"));
    }
}
