using DtoB.Core;
using DtoB.Bim;
using DtoB.Cad;
using DtoB.Geometry;
using Xunit;
namespace DtoB.Tests;
public class ReviewRegressionTests
{
    [Theory]
    [InlineData("{\"schemaVersion\":2,\"future\":true}", "Unsupported schema major")]
    [InlineData("{\"schemaVersion\":1,\"future\":true}", "Invalid IR v1 JSON")]
    public void ExplicitEnvelopeFailures(string json, string message) => Assert.Contains(message, Assert.Throws<InvalidDataException>(() => IrJson.Deserialize<CadDocument>(json)).Message);
    [Fact] public void MissingArraysHaveTypedValidationError()
    {
        var cad = IrJson.Deserialize<CadDocument>("{\"schemaVersion\":1}");
        Assert.Throws<InvalidDataException>(cad.Validate);
        Assert.Throws<InvalidDataException>(() => IrJson.Deserialize<BimDocument>("{\"schemaVersion\":1}").Validate());
    }
    [Fact] public void ConfirmingPredictionCannotDiscardEvidence()
    {
        var wall = Assert.Single(ContractTests.Bim().Objects.OfType<BimWall>());
        Assert.Throws<InvalidDataException>(() => (wall with { IsSemanticPrediction=true, Status=ReviewStatus.Confirmed, Prediction=null }).Validate());
        (wall with { IsSemanticPrediction=false, Status=ReviewStatus.Confirmed, Prediction=null }).Validate();
    }
    [Fact] public void CanonicalHandlesAndConsistentSourceReference()
    {
        var id=Guid.NewGuid(); Assert.Equal(CadIdentity.Create(id,"ff",["ab"]),CadIdentity.Create(id,"FF",["AB"]));
        new SourceReference(id,Guid.NewGuid(),"ff",CadIdentity.Create(id,"FF")).Validate();
        Assert.Throws<InvalidDataException>(()=>new SourceReference(id,Guid.NewGuid(),"A",CadIdentity.Create(id,"B")).Validate());
    }
    [Fact] public void ToleranceControlsClosureAndDegeneracy()
    {
        Point3[] points=[new(0,0,0),new(10,0,0),new(10,10,0),new(0,0.001,0)];
        GeometryPredicates.ClosedBoundary(points,new(0.01,0.001));
        Assert.Throws<InvalidDataException>(()=>GeometryPredicates.ClosedBoundary(points,new(0.0001,0.001)));
        Assert.Throws<InvalidDataException>(()=>new Segment3(new(0,0,0),new(1e-9,0,0)).Validate(new(0.01,0.001)));
    }
    [Fact] public void NestedMirroredArcUsesDefinitionFrame()
    {
        var inner=Transform3.Placement(new(10,0,0),0,new(-1,1,1));
        var outer=Transform3.Placement(new(100,200,0),Math.PI/2,new(1,1,1));
        var transform=inner.Then(outer);
        var a=transform.Apply(new(5,0,0)); var b=transform.Apply(new(0,5,0));
        Assert.Equal(100,a.X,8);Assert.Equal(205,a.Y,8);Assert.Equal(95,b.X,8);Assert.Equal(210,b.Y,8);
    }
    [Fact] public void IndependentSnapshotsCompareByContent()
    { Assert.Equal(IrJson.Serialize(ContractTests.Cad()),IrJson.Serialize(ContractTests.Cad())); }

    [Fact] public void NestedFixturePreservesArcAndUnsupportedNormal()
    {
        var doc=IrJson.Deserialize<CadDocument>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"fixtures","nested-mirrored.json")));doc.Validate();
        var arc=Assert.Single(doc.Entities.OfType<CadPrimitive>(), p=>p.SourceHandle=="CA");
        Assert.Equal(new string[]{"A0","B0"},arc.InsertionPath);
        var start=doc.SourceToModel.Apply(arc.Transform.Apply(new(5,0,0)));
        Assert.Equal(new Point3(100,205,0),start);
        Assert.Equal(IrJson.Serialize(doc),IrJson.Serialize(IrJson.Deserialize<CadDocument>(IrJson.Serialize(doc))));
        Assert.Contains(doc.Diagnostics,d=>d.Code=="UNSUPPORTED_ENTITY"&&d.Message.Contains("normal"));
    }
    [Fact] public void OutOfOrderDiscriminatorIsExplicitError()
    {
        var json=IrJson.Serialize(ContractTests.Cad()).Replace("\"$kind\": \"primitive\",", "\"rawColor\": \"Red\", \"$kind\": \"primitive\",");
        Assert.Throws<InvalidDataException>(()=>IrJson.Deserialize<CadDocument>(json));
    }
}

