using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using DtoB.Cad.Normalize;
using DtoB.Cad.Normalize.Coordinates;
using DtoB.Cad.Raw;
using Xunit;
using static DtoB.Tests.Cad04CCoordinateFixture;

namespace DtoB.Tests;

public sealed class Cad04CScopeArchitectureTests
{
    [Fact] public void CoordinateServicesContainNoPlacementExpansionGeometryAssemblyOrVendorCode()
    {
        var directory=Path.Combine(CadV2ContractFixture.Root(),"packages/DtoB.Cad.Normalize/Coordinates");
        foreach(var file in Directory.GetFiles(directory,"*.cs"))
        {
            var text=File.ReadAllText(file);
            Assert.DoesNotMatch(@"\b(Transform3|CadTransform|RawBlockBegin|CadDocument|CadCircle|CadArc|CadPolyline|CadBlockOccurrence|Autodesk\.Revit|RevitAPI(?:UI)?|ElementId|BuiltInCategory|FamilySymbol|WallType)\b",text);
            Assert.DoesNotMatch(@"\.BasePoint\b|\.Scale\b|\.RotationDegrees\b|\.Bulge\b|\.(Then|Apply)\(|Math\.(Sin|Cos|Tan|Atan)|\b(File|FileStream|Directory|Task)\.|\b(ByLayer|ByBlock|MINSERT)\b",text);
        }
        Assert.Equal(typeof(DtoB.Cad.Normalize.Assembly.CadNormalizer), Assert.Single(typeof(ICadNormalizer).Assembly.GetTypes(), t => t.IsClass && typeof(ICadNormalizer).IsAssignableFrom(t)));
        Assert.DoesNotContain(typeof(CadCoordinateNormalizer).Assembly.GetReferencedAssemblies(),a=>new[]{"DtoB.Cad.Dxf","DtoB.Desktop","PresentationFramework","RevitAPI","RevitAPIUI"}.Contains(a.Name));
    }
    [Fact] public void PublishedCoordinateStateIsOwnedImmutableAndCompact()
    {
        foreach(var type in typeof(CadCoordinateInput).Assembly.GetExportedTypes().Where(t=>t.Namespace=="DtoB.Cad.Normalize.Coordinates" && !t.IsEnum))
        foreach(var p in type.GetProperties(BindingFlags.Public|BindingFlags.Instance))
        {
            Assert.Null(p.SetMethod); Assert.False(p.PropertyType.IsArray);
            Assert.DoesNotContain(p.PropertyType,new[]{typeof(RawDrawingSnapshot),typeof(RawCoordinateFacts),typeof(DxfSourceFact)});
        }
    }
}

public sealed class Cad04CDeterminismTests
{
    [Fact] public void FixedCoordinateEvidenceIsCultureProcessAndCwdIndependent()
    {
        var before=CultureInfo.CurrentCulture; var bytes=new List<byte[]>();
        try
        {
            foreach(var culture in new[]{"","fr-FR","ko-KR"})
            {
                CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo(culture);
                var point=CadCoordinateNormalizer.NormalizePoint(Input(normal:Triple(0,2,0)),Units(5),Policy);
                Point(point,-20,40,30); Vector(point.Frame!.Ax,-1,0,0); Vector(point.Frame.Ay,0,0,1); Vector(point.Frame.N,0,1,0);
                var vector=CadCoordinateNormalizer.NormalizeVector(Input(CadCoordinateRole.OcsDirectionVector,normal:Triple(0,1,0)),null,Policy);
                Vector(vector.Vector,-2,4,3);
                var invalid=CadCoordinateNormalizer.NormalizePoint(Input(point:Triple(double.MaxValue,1,2)),Units(6),Policy);
                Assert.Equal(CadCoordinateDiagnosticCode.NonFiniteNormalizedCoordinate,Assert.Single(invalid.Diagnostics).Code);
                var oblique=CadCoordinateNormalizer.NormalizePoint(Input(normal:Triple(1,1,1)),Units(),Policy);
                Point(oblique,-0.32955735700618094,2.4988697677400094,4.758890819541682);
                var defaulted=CadCoordinateNormalizer.NormalizePoint(Input(),Units(),Policy);
                var d=CadCoordinateRoles.Describe(point.Input.Source.Role)!;
                var evidence=new
                {
                    point.Status,Scale=point.Units!.Decision.SourceToMillimeters,point.Input.Source.Role,d.Space,d.Kind,d.Dimensional,
                    Input=new[]{point.Input.Coordinates.X.Value,point.Input.Coordinates.Y.Value,point.Input.Coordinates.Z.Value},
                    SourceNormal=new[]{point.Input.Extrusion.X.Value,point.Input.Extrusion.Y.Value,point.Input.Extrusion.Z.Value},
                    Normal=point.Frame.N,point.Frame.Ax,point.Frame.Ay,point.Frame.N,point.PointMm,
                    VectorInput=new[]{2,3,4},VectorOutput=vector.Vector,point.PolicyId,
                    UnitSourcePairs=point.Units.Source.PairIndices,
                    SourcePairs=new[]{point.Input.Coordinates.X.PairIndices,point.Input.Coordinates.Y.PairIndices,point.Input.Coordinates.Z.PairIndices},
                    NormalSourcePairs=new[]{point.Input.Extrusion.X.PairIndices,point.Input.Extrusion.Y.PairIndices,point.Input.Extrusion.Z.PairIndices},
                    Codes=invalid.Diagnostics.Select(x=>new{x.Code,x.Field}),
                    Oblique=new{oblique.PointMm,oblique.Frame!.Ax,oblique.Frame.Ay,oblique.Frame.N},
                    Defaulted=new{defaulted.DefaultRules,defaulted.PointMm,defaulted.Input.Extrusion.X.PairIndices}
                };
                bytes.Add(JsonSerializer.SerializeToUtf8Bytes(evidence));
            }
        }
        finally{CultureInfo.CurrentCulture=before;}
        Assert.Equal(bytes[0],bytes[1]); Assert.Equal(bytes[0],bytes[2]);
        var directory=Environment.GetEnvironmentVariable("DTOB_04C_EVIDENCE_DIRECTORY");
        if(directory is not null)
        {
            Directory.CreateDirectory(directory); File.WriteAllBytes(Path.Combine(directory,"coordinate-evidence.json"),bytes[0]);
            File.WriteAllText(Path.Combine(directory,"coordinate-evidence.sha256"),Convert.ToHexString(SHA256.HashData(bytes[0]))+"\n");
        }
    }
}
