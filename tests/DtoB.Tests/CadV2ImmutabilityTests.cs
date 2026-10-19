using DtoB.Cad.V2;
using DtoB.Geometry;
using Xunit;

namespace DtoB.Tests;

public sealed class CadV2ImmutabilityTests
{
    [Fact] public void DocumentAndNestedEvidenceOwnReadOnlyCollectionCopies()
    {
        var input = new List<CadSourceFactRef> { new(0) };
        var entity = new CadEntity(CadV2ContractFixture.Id("line"), new(0), CadEntityKind.Primitive,
            CadNormalizationState.Normalized, CadV2ContractFixture.Id("line-geometry", CadIdentityDomain.Leaf), null, null, input);
        input.Clear(); Assert.Single(entity.Evidence);
        var entities = new List<CadEntity> { entity, CadV2ContractFixture.Document().Entities[1] };
        var diagnostics = new List<CadDiagnostic> { CadV2ContractFixture.Diagnostic() };
        var doc = CadV2ContractFixture.Document(entities: entities, diagnostics: diagnostics);
        var before = CadIrJson.Serialize(doc); entities.Clear(); diagnostics.Clear();
        Assert.Equal(before, CadIrJson.Serialize(doc));
        Assert.Throws<NotSupportedException>(() => ((IList<CadEntity>)doc.Entities).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<CadSourceFactRef>)entity.Evidence)[0] = new(99));
    }
    [Fact] public void RegistryPathAndDiagnosticCollectionsAreDefensive()
    {
        var pairs = new[] { 1 }; var fact = new CadSourceFact(new(0), "Start.X", CadFactPresence.Present, pairs, 3, 4);
        var records = CadV2ContractFixture.Registry().Records.ToList(); var facts = new List<CadSourceFact> { fact };
        var registry = new CadProvenanceRegistry(CadV2ContractFixture.Registry().Source, records, facts);
        pairs[0] = 500; facts.Clear(); records.Clear(); Assert.Equal(1, registry.Facts[0].PairIndices[0]); Assert.Equal(2, registry.Records.Count);
        Assert.Throws<NotSupportedException>(() => ((IList<CadSourceRecord>)registry.Records).Clear());
        var steps = new List<CadOccurrenceStep> { new(new(0), new(1), 0, 0) }; var path = new CadOccurrencePath(steps); steps.Clear();
        Assert.Single(path.Steps); Assert.Throws<NotSupportedException>(() => ((IList<CadOccurrenceStep>)path.Steps).Clear());
        var args = new[] { "retained" }; var d = new CadDiagnostic("CODE", CadSeverity.Warning, CadStage.Contract, null, null, path, args);
        args[0] = "changed"; Assert.Equal("retained", d.Arguments[0]);
    }
    [Fact] public void GeometryAndStyleContractCollectionsCannotBeChangedExternally()
    {
        var points = new List<Point3> { new(0, 0, 0), new(1, 0, 0), new(0, 1, 0) };
        var solid = new CadSolid(CadV2ContractFixture.Id("solid"), new(0), [], points); points.Clear(); Assert.Equal(3, solid.BoundaryMm.Count);
        Assert.Throws<NotSupportedException>(() => ((IList<Point3>)solid.BoundaryMm).Clear());
        var evidence = new List<CadSourceFactRef> {new(0)};
        var color = new CadResolvedProperty<CadColor>(CadPropertyMode.ByLayer, CadResolutionState.Inherited, new(CadColorKind.Aci, 7), "rule", evidence);
        evidence.Clear(); Assert.Single(color.Evidence);
        Assert.Throws<InvalidDataException>(() => new CadResolvedProperty<List<string>>(CadPropertyMode.Explicit,
            CadResolutionState.Known, ["mutable"], null, []));
    }
}

public sealed class TransformImmutabilityTests
{
    [Fact] public void InputAndExportMutationsCannotChangeOccurrencesDefinitionOrPublishedState()
    {
        var input = Transform3.Identity.Matrix; var legacy = new Transform3(input);
        var a = CadTransform.FromTransform3(legacy); var b = CadTransform.FromTransform3(legacy);
        var path = new CadOccurrencePath([new(new(0), new(1), null, null)]);
        var first = new CadBlockOccurrence(CadV2ContractFixture.Id("one"), new(0), CadV2ContractFixture.Id("block"), null, path, a, a, null);
        var second = new CadBlockOccurrence(CadV2ContractFixture.Id("two"), new(0), CadV2ContractFixture.Id("block"), null, path, b, b, null);
        var definition = CadTransform.FromTransform3(legacy); var doc = CadV2ContractFixture.Document(); var before = CadIrJson.Serialize(doc);
        input[0] = 900; first.LocalTransform.ToTransform3().Matrix[0] = 400; first.AccumulatedTransform.ToTransform3().Matrix[3] = 700;
        Assert.Equal(1d, first.LocalTransform.Values[0]); Assert.Equal(1d, second.LocalTransform.Values[0]); Assert.Equal(1d, definition.Values[0]);
        Assert.Throws<NotSupportedException>(() => ((IList<double>)first.LocalTransform.Values)[0] = 600);
        doc.SourceToModel.ToTransform3().Matrix[0] = 3; Assert.Equal(before, CadIrJson.Serialize(doc));
        var copy = CadIrJson.Deserialize(before); copy.SourceToModel.ToTransform3().Matrix[0] = 4;
        Assert.Equal(before, CadIrJson.Serialize(copy));
    }
    [Theory] [InlineData(15)] [InlineData(17)]
    public void InvalidMatrixShapeCannotEnterValue(int size) => Assert.Throws<InvalidDataException>(() => new CadTransform(new double[size]));
    [Fact] public void NonfiniteAndNonaffineMatrixAreRejected()
    {
        var values = Transform3.Identity.Matrix; values[0] = double.NaN;
        Assert.Throws<InvalidDataException>(() => new CadTransform(values)); values[0] = 1; values[15] = 0;
        Assert.Throws<InvalidDataException>(() => new CadTransform(values));
    }
}
