using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using DtoB.Cad.Normalize;
using DtoB.Cad.Normalize.Placement;
using DtoB.Cad.Raw;
using Xunit;
using static DtoB.Tests.Cad04DPlacementFixture;

namespace DtoB.Tests;

public sealed class Cad04DScopeArchitectureTests
{
    [Fact] public void PlacementSourceHasNoChildTraversalArraysStylesGeometryAssemblyOrVendorIntegration()
    {
        var directory=Path.Combine(CadV2ContractFixture.Root(),"packages/DtoB.Cad.Normalize/Placement");
        foreach(var file in Directory.GetFiles(directory,"*.cs"))
        {
            var text=File.ReadAllText(file);
            Assert.DoesNotMatch(@"\.Children\b|\.(ColumnSpacing|RowSpacing)\b|\b(CadDocument|CadBlockOccurrence|CadOccurrencePath|CadCircle|CadArc|CadPolyline|ByLayer|ByBlock|Autodesk\.Revit|RevitAPI(?:UI)?|ElementId|BuiltInCategory|FamilySymbol|WallType|ACadSharp)\b",text);
            Assert.DoesNotMatch(@"\b(Expand|Traverse|ResolveStyle|NormalizeCoordinates)\s*\(|\b(File|FileStream|Directory|Task)\.|\.ParentTransform\b",text);
        }
        Assert.Equal(typeof(DtoB.Cad.Normalize.Assembly.CadNormalizer), Assert.Single(typeof(ICadNormalizer).Assembly.GetTypes(), t => t.IsClass && typeof(ICadNormalizer).IsAssignableFrom(t)));
        Assert.DoesNotContain(typeof(CadInsertLocalTransform).Assembly.GetReferencedAssemblies(),a=>new[]{"DtoB.Cad.Dxf","DtoB.Desktop","PresentationFramework","RevitAPI"}.Contains(a.Name));
    }
    [Fact] public void PublishedPlacementPropertiesAreImmutableAndDoNotRetainRawGraphs()
    {
        foreach(var type in typeof(CadLocalPlacement).Assembly.GetExportedTypes().Where(t=>t.Namespace=="DtoB.Cad.Normalize.Placement" && !t.IsEnum))
        foreach(var property in type.GetProperties(BindingFlags.Public|BindingFlags.Instance))
        {
            Assert.Null(property.SetMethod); Assert.False(property.PropertyType.IsArray);
            Assert.DoesNotContain(property.PropertyType,new[]{typeof(RawDrawingSnapshot),typeof(RawBlockDefinition),typeof(RawInsert),typeof(RawBlockBegin),typeof(DxfSourceFact)});
        }
    }
}
public sealed class Cad04DDeterminismTests
{
    [Fact] public async Task FixedLocalTransformEvidenceMatchesAcrossCulturesProcessesAndWorkingDirectories()
    {
        var cultureBefore=CultureInfo.CurrentCulture; var bytes=new List<byte[]>();
        try
        {
            foreach(var culture in new[]{"","fr-FR","ko-KR"})
            {
                CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo(culture);
                var p=Placement(await Snapshot(new(100,50,0),new(1000,2000,0),new(2,3,1),90,new(0,1,0)));
                ExpectedPoint(p,new(110,70,0),new(-940,0,2020)); ExpectedVector(p,new(10,20,0),new(60,0,20));
                var defaulted=Placement(await Snapshot());
                var missing=Result(await Cad04BScalarFixture.Snapshot(Document("",Insert())));
                Assert.Equal(CadPlacementDiagnosticCode.MissingBlockDefinition,Assert.Single(missing.Diagnostics).Code);
                var frame=p.Anchor.Frame!;
                var evidence=new
                {
                    UnitScale=p.Block.Units.Decision.SourceToMillimeters,p.Block.Source.Document,
                    Definition=new{p.Block.Source.OriginalHandle,p.Block.Name.Value,p.Block.BaseMm,p.Block.Facts},
                    Insert=new{p.Scale.Source.OriginalHandle,p.Target,p.Anchor.Input.Coordinates,p.Anchor.PointMm},
                    frame.Ax,frame.Ay,frame.N,RotationRadians=p.Rotation.Value,Scale=p.Scale.Scale,LocalMatrix16=p.Transform.Values,p.OrientationSign,
                    PointInput=new[]{110,70,0},PointOutput=CadInsertLocalTransform.ApplyPoint(p,new(110,70,0)).PointMm,
                    VectorInput=new[]{10,20,0},VectorOutput=CadInsertLocalTransform.ApplyVector(p,new(10,20,0)).Vector,
                    DefaultRules=defaulted.DefaultRules,p.PolicyId,Codes=missing.Diagnostics.Select(d=>new{d.Code,d.Field,d.PairIndices})
                };
                bytes.Add(JsonSerializer.SerializeToUtf8Bytes(evidence));
            }
        }
        finally{CultureInfo.CurrentCulture=cultureBefore;}
        Assert.Equal(bytes[0],bytes[1]); Assert.Equal(bytes[0],bytes[2]);
        var directory=Environment.GetEnvironmentVariable("DTOB_04D_EVIDENCE_DIRECTORY");
        if(directory is not null)
        {
            Directory.CreateDirectory(directory); File.WriteAllBytes(Path.Combine(directory,"local-transform-evidence.json"),bytes[0]);
            File.WriteAllText(Path.Combine(directory,"local-transform-evidence.sha256"),Convert.ToHexString(SHA256.HashData(bytes[0]))+"\n");
        }
    }
}
