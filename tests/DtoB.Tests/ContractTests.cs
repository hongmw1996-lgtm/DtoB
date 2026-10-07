using DtoB.Analysis;
using DtoB.Bim;
using DtoB.Cad;
using DtoB.Core;
using DtoB.Geometry;
using DtoB.Revit.Core;
using Xunit;

namespace DtoB.Tests;

public class ContractTests
{
    private static string Fixture(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", file));
    internal static CadDocument Cad() => IrJson.Deserialize<CadDocument>(Fixture("cad.json"));
    internal static BimDocument Bim() => IrJson.Deserialize<BimDocument>(Fixture("bim.json"));

    [Fact] public void CadRoundTripPreservesSourceUnknownRawDataAndAllEntityContracts()
    {
        var original = Cad(); original.ValidateStructure();
        var json = IrJson.Serialize(original); var copy = IrJson.Deserialize<CadDocument>(json); copy.ValidateStructure();
        Assert.Equal(json, IrJson.Serialize(copy));
        Assert.Contains(copy.Entities, e => e is CadText); Assert.Contains(copy.Entities, e => e is CadDimension);
        Assert.Contains(copy.Entities, e => e is CadInsert); Assert.Single(copy.Blocks);
        var unknown = Assert.Single(copy.Entities.OfType<CadUnsupported>());
        Assert.Equal("proxy", unknown.RawProperties["payload"].GetString());
        Assert.Equal("FF", unknown.SourceHandle);
        Assert.Contains(copy.Diagnostics, d => d.SourceHandle == "FF" && d.Code == "UNSUPPORTED_ENTITY");
        Assert.Equal(4, copy.Entities.OfType<CadPrimitive>().Count());
    }
    [Fact] public void UnsupportedCannotDisappearWithoutDiagnostic()
    { Assert.Throws<InvalidDataException>(() => (Cad() with { Diagnostics = [] }).ValidateStructure()); }
    [Fact] public void SchemaMajorAndMissingSourceAreRejected()
    {
        Assert.Throws<InvalidDataException>(() => (Cad() with { SchemaVersion = 2 }).ValidateStructure());
        Assert.Throws<InvalidDataException>(() => (Bim() with { SchemaVersion = 2 }).ValidateStructure());
        Assert.Throws<InvalidDataException>(() => (Cad() with { DrawingId = Guid.Empty }).ValidateStructure());
    }
    [Fact] public void OccurrenceIdentityIsUnambiguous()
    {
        var drawing = Guid.NewGuid();
        Assert.NotEqual(CadIdentity.Create(drawing, "AB"), CadIdentity.Create(drawing, "a", ["b"]));
        Assert.NotEqual(CadIdentity.Create(drawing, "A", ["11"]), CadIdentity.Create(drawing, "A", ["12"]));
    }
    [Fact] public void BimRoundTripIncludesAllEightTypesAndManualObjectWithoutPrediction()
    {
        var original = Bim(); original.ValidateStructure(); var json = IrJson.Serialize(original);
        var copy = IrJson.Deserialize<BimDocument>(json); copy.ValidateStructure(); Assert.Equal(json, IrJson.Serialize(copy));
        Assert.Equal(8, copy.Objects.Select(o => o.GetType()).Distinct().Count());
        Assert.Contains(copy.Objects, o => o.Provenance.Origin == ProvenanceOrigin.Manual && o.Prediction == null && o.Status == ReviewStatus.Confirmed);
        Assert.All(copy.Objects, o => Assert.NotNull(o.Provenance));
    }
    [Fact] public void PredictedResultRequiresEvidenceAndConfidenceButConfirmedDomainDoesNot()
    {
        Assert.Throws<InvalidDataException>(() => new Prediction(double.NaN, ["layer"]).ValidateStructure());
        Assert.Throws<InvalidDataException>(() => new Prediction(1.1, ["layer"]).ValidateStructure());
        Assert.Throws<InvalidDataException>(() => new Prediction(0.9, []).ValidateStructure());
        var level = Assert.Single(Bim().Objects.OfType<BimLevel>());
        (level with { Prediction = null, Status = ReviewStatus.Confirmed }).ValidateStructure();
        Assert.Throws<InvalidDataException>(() => (level with { Prediction = null, Status = ReviewStatus.Suggested }).ValidateStructure());
        Assert.Throws<InvalidDataException>(() => new SemanticPrediction<BimLevel>(level, null!).ValidateStructure());
        new SemanticPrediction<BimLevel>(level, new(0.9, ["synthetic prediction"])).ValidateStructure();
        Assert.Throws<InvalidDataException>(() => (level with { Status = ReviewStatus.Confirmed, Prediction = new(0.9, []) }).ValidateStructure());
    }
    [Fact] public void InvalidHostLevelAndProvenanceAreRejected()
    {
        var doc = Bim(); var door = Assert.Single(doc.Objects.OfType<BimDoor>());
        Assert.Throws<InvalidDataException>(() => (doc with { Objects = doc.Objects.Select(o => o == door ? door with { HostWallId = Guid.NewGuid() } : o).ToArray() }).ValidateStructure());
        Assert.Throws<InvalidDataException>(() => (doc with { Objects = doc.Objects.Where(o => o is not BimLevel).ToArray() }).ValidateStructure());
        Assert.Throws<InvalidDataException>(() => (door with { Provenance = new(ProvenanceOrigin.SourceDrawing, "source", []) }).ValidateStructure());
    }
    [Fact] public async Task AnalysisFoundationReportsUnsupportedAndProducesNoModel()
    {
        var result = await new FoundationAnalysisEngine().AnalyzeAsync(Cad());
        Assert.Null(result.Model); Assert.Equal("ANALYSIS_NOT_IMPLEMENTED", Assert.Single(result.Diagnostics).Code);
    }
    [Fact] public void NoAutodeskDependencyLeaksIntoCoreOrAnalysis()
    {
        var assemblies = new[] { typeof(Contract).Assembly, typeof(Point3).Assembly, typeof(CadDocument).Assembly,
            typeof(BimDocument).Assembly, typeof(FoundationAnalysisEngine).Assembly, typeof(RevitUnits).Assembly, typeof(DtoB.Ipc.PingClient).Assembly };
        Assert.All(assemblies, a => Assert.DoesNotContain(a.GetReferencedAssemblies(), r => r.Name!.StartsWith("Revit") || r.Name.StartsWith("Autodesk")));
    }
}
