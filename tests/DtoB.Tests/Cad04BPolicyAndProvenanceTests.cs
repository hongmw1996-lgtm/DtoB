using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DtoB.Cad.Normalize;
using DtoB.Cad.Normalize.Scalars;
using DtoB.Cad.Raw;
using DtoB.Cad.V2;
using DtoB.Geometry;
using Xunit;
using static DtoB.Tests.Cad04BScalarFixture;

namespace DtoB.Tests;

public sealed class CadNumericPolicyTests
{
    [Fact] public void ExplicitDimensionalComparisonsAreInclusiveAndDoNotQuantize()
    {
        var p = new CadNumericPolicy("test-explicit-policy",new(0.001,1e-9,1e-12));
        Assert.Equal("test-explicit-policy",p.ProfileId);
        Assert.True(p.IsNearZeroLengthMm(0.001)); Assert.False(p.IsNearZeroLengthMm(Math.BitIncrement(0.001)));
        Assert.True(p.IsNearZeroAngleRadians(-1e-9)); Assert.False(p.IsNearZeroAngleRadians(1e-8));
        Assert.True(p.IsNearZeroMatrixCoefficient(1e-12)); Assert.False(p.IsNearZeroMatrixCoefficient(1e-11));
        Assert.True(CadNumericPolicy.IsExactZero(-0.0)); Assert.False(CadNumericPolicy.IsExactZero(double.Epsilon));
        var value = 0.0001; Assert.True(p.IsNearZeroLengthMm(value));
        Assert.Equal(value,CadScalarLengthNormalizer.Normalize(Fact(CadScalarRole.PolylineElevation,Real(value)),Units()).Value);
        Assert.All(typeof(CadNumericPolicy).GetProperties(),property => Assert.Null(property.SetMethod));
        Assert.DoesNotContain(typeof(CadNumericPolicy).GetMembers(),m => m.Name.Contains("Recognition",StringComparison.OrdinalIgnoreCase));
    }
    [Theory][InlineData(double.NaN)][InlineData(double.PositiveInfinity)][InlineData(double.NegativeInfinity)]
    public void SourceValidityIsSeparateFromComparison(double value)
    {
        Assert.False(CadNumericPolicy.IsFiniteSource(value));
        Assert.Throws<InvalidDataException>(() => CadNumericPolicy.IsExactZero(value));
        Assert.Throws<InvalidDataException>(() => new CadNumericPolicy("p",new(0.001,1e-9,1e-12)).IsNearZeroLengthMm(value));
    }
    [Fact] public void PolicyRequiresCallerSuppliedPositiveFiniteDimensions()
    {
        Assert.Throws<InvalidDataException>(() => new CadNumericPolicy("p",new(1,1)));
        Assert.Throws<InvalidDataException>(() => new CadNumericPolicy("p",new(0,1,1)));
        Assert.Throws<InvalidDataException>(() => new CadNumericPolicy("p",new(1,double.NaN,1)));
        Assert.Throws<InvalidDataException>(() => new CadNumericPolicy("p",new(1,1,-1)));
    }
}

public sealed class CadUnitProvenanceTests
{
    [Fact] public void DecisionsAndRefusalsRetainEvidenceAndDoNotMutateRaw()
    {
        var raw = Integer(4); var before = JsonSerializer.Serialize(raw); var reference = new CadSourceFactRef(7);
        var fact = Fact(CadScalarRole.HeaderInsertionUnits,raw); var result = CadUnitNormalizer.Resolve(fact,registryEvidence:reference);
        Assert.Same(reference,result.Decision.Evidence); Assert.Same(Document,result.Source.Document);
        Assert.Equal(CadScalarRole.HeaderInsertionUnits,result.Source.Role); Assert.Equal(new[] {3},result.Source.PairIndices);
        Assert.Equal(7,result.Source.FirstPhysicalLine); Assert.Equal(8,result.Source.LastPhysicalLine); Assert.Null(result.Source.Record);
        Assert.Equal(before,JsonSerializer.Serialize(raw));
        var refused = Units(3); Assert.Same(refused.Source,Assert.Single(refused.Diagnostics).Source);
        Assert.Equal(new[] {3},refused.Diagnostics[0].Source.PairIndices);
        Assert.Throws<NotSupportedException>(() => ((IList<int>)result.Source.PairIndices)[0] = 99);
    }
    [Fact] public async Task SnapshotAngleKeepsOriginalHandleRolePairAndLineWithoutGraphMutation()
    {
        var snapshot = await Snapshot(MText("50\n1.5707963267948966\n")); var before = JsonSerializer.Serialize(snapshot);
        var row = Assert.Single(snapshot.Records); var f = RawScalarAccess.Record(snapshot,row,CadScalarRole.MTextRotation);
        var r = CadAngleNormalizer.Normalize(f);
        Assert.Equal("a",r.Source.OriginalHandle); Assert.Equal("A",r.Source.CanonicalHandle);
        Assert.Equal(snapshot.DocumentProvenance!.FileHash,r.Source.Document.FileHash);
        var pair = Assert.Single(r.Source.PairIndices); Assert.Equal(2*pair+1,r.Source.FirstPhysicalLine); Assert.Equal(2*pair+2,r.Source.LastPhysicalLine);
        Assert.Equal(row.StructuralRecordIndex,r.Source.Record!.RecordIndex); Assert.Equal(CadScalarRole.MTextRotation,r.Source.Role);
        Assert.Equal(before,JsonSerializer.Serialize(snapshot));
        using var c = new CancellationTokenSource(); c.Cancel();
        Assert.Throws<OperationCanceledException>(() => RawScalarAccess.Record(snapshot,row,CadScalarRole.MTextRotation,c.Token));
        var other = await Snapshot(MText("50\n0\n"));
        Assert.Throws<InvalidDataException>(() => RawScalarAccess.Record(snapshot,other.Records[0],CadScalarRole.MTextRotation));
    }
    [Fact] public async Task ControlledRawAndAllPriorContractsRemainUntouchedAfterScalarUse()
    {
        var s = await Controlled(); var before = JsonSerializer.Serialize(s);
        var units = CadUnitNormalizer.Resolve(RawScalarAccess.HeaderUnits(s));
        var arc = s.Records.Single(r => r.SourceRecord.Source.StartPairIndex == 2868);
        // Original DXF pair 2877 is code40, text 900.0 (independent byte inspection).
        Assert.Equal(900,CadScalarLengthNormalizer.Normalize(RawScalarAccess.Record(s,arc,CadScalarRole.ArcRadius),units).Value);
        Assert.Equal(CadScalarStatus.Normalized,CadAngleNormalizer.NormalizeArc(RawScalarAccess.Record(s,arc,CadScalarRole.ArcStartAngle),RawScalarAccess.Record(s,arc,CadScalarRole.ArcEndAngle)).Status);
        Assert.Equal(before,JsonSerializer.Serialize(s));
    }
}

public sealed class Cad04BScopeArchitectureTests
{
    [Fact] public void ScalarImplementationHasNoSpatialEngineVendorOrIo()
    {
        var root = CadV2ContractFixture.Root();
        var files = Directory.GetFiles(Path.Combine(root,"packages/DtoB.Cad.Normalize/Scalars"),"*.cs");
        Assert.NotEmpty(files);
        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotMatch(@"\b(Autodesk\.Revit|RevitAPI(?:UI)?|ElementId|BuiltInCategory|FamilySymbol|WallType|Transform3|CadTransform|ArbitraryAxis|ByLayer|ByBlock)\b",text);
            Assert.DoesNotMatch(@"Math\.(Sin|Cos|Tan|Atan|Sqrt)|\b(File|FileStream|Directory|Task)\.|\.(Then|Apply)\(",text);
        }
        Assert.Equal(typeof(DtoB.Cad.Normalize.Assembly.CadNormalizer), Assert.Single(typeof(ICadNormalizer).Assembly.GetTypes(), t => t.IsClass && typeof(ICadNormalizer).IsAssignableFrom(t)));
        Assert.DoesNotContain(typeof(CadAngleNormalizer).Assembly.GetReferencedAssemblies(),a => new[] {"DtoB.Cad.Dxf","DtoB.Desktop","DtoB.Revit","PresentationFramework"}.Contains(a.Name));
        Assert.DoesNotContain(typeof(CadNumericPolicy).GetProperties(),p => p.PropertyType == typeof(Transform3));
    }
    [Fact] public void ScalarResultsContainOnlyOwnedCompactState()
    {
        foreach (var type in typeof(CadScalarFact).Assembly.GetExportedTypes().Where(t => t.Namespace == "DtoB.Cad.Normalize.Scalars" && !t.IsEnum))
        foreach (var property in type.GetProperties(BindingFlags.Public|BindingFlags.Instance))
        { Assert.Null(property.SetMethod); Assert.DoesNotContain("RawDrawingSnapshot",property.PropertyType.Name); Assert.DoesNotContain("DxfSourceFact",property.PropertyType.Name); }
    }
}

public sealed class Cad04BDeterminismTests
{
    [Fact] public void FixedScalarEvidenceIsCultureAndProcessIndependent()
    {
        var values = new List<byte[]>(); var previous = CultureInfo.CurrentCulture;
        try
        {
            foreach (var culture in new[] {"","fr-FR","ko-KR"})
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                var units = new[] {1,2,4,5,6,0,3,-1}.Select(code => Units(code)).ToArray();
                Assert.Equal(new double?[] {25.4,304.8,1,10,1000,null,null,null},units.Select(u => u.Decision.SourceToMillimeters));
                var angles = new[] {0d,90,180,270,360,-90}.Select(n => CadAngleNormalizer.Normalize(Angle(n))).ToArray();
                Assert.Equal(new double?[] {0,Math.PI/2,Math.PI,3*Math.PI/2,0,3*Math.PI/2},angles.Select(a => a.Value));
                var data = new { Units = units.Select(u => new {u.Status,u.Unit,u.Decision.Kind,u.Decision.SourceUnitCode,u.Decision.SourceToMillimeters,
                    Codes=u.Diagnostics.Select(d => d.Code),u.Source.Role,u.Source.PairIndices}),
                    Angles=angles.Select(a => new {a.Status,a.Value,a.Source.Role,a.Source.PairIndices}),
                    MText=CadAngleNormalizer.Normalize(Angle(Math.PI/2,CadScalarRole.MTextRotation)).Value,
                    Arc=CadAngleNormalizer.NormalizeArc(Angle(270,CadScalarRole.ArcStartAngle),Angle(90,CadScalarRole.ArcEndAngle)).SweepRadians };
                values.Add(JsonSerializer.SerializeToUtf8Bytes(data));
            }
        }
        finally { CultureInfo.CurrentCulture=previous; }
        Assert.Equal(values[0],values[1]); Assert.Equal(values[0],values[2]);
        var directory = Environment.GetEnvironmentVariable("DTOB_04B_EVIDENCE_DIRECTORY");
        if (directory is not null)
        {
            Directory.CreateDirectory(directory); File.WriteAllBytes(Path.Combine(directory,"scalar-contract-evidence.json"),values[0]);
            File.WriteAllText(Path.Combine(directory,"scalar-contract-evidence.sha256"),Convert.ToHexString(SHA256.HashData(values[0]))+"\n",Encoding.ASCII);
        }
    }
}
