using DtoB.Cad.V2;
using Xunit;

namespace DtoB.Tests;

public sealed class CadV2ProvenanceTests
{
    [Fact] public void CompactRegistryResolvesDocumentHandleAndPhysicalEvidenceAfterRoundtrip()
    {
        var doc = CadIrJson.Deserialize(CadIrJson.Serialize(CadV2ContractFixture.Document()));
        var registry = doc.Provenance; var record = registry.Resolve(doc.Entities[0].Source);
        Assert.Equal(CadV2ContractFixture.Drawing, registry.Source.DrawingId); Assert.Equal(CadV2ContractFixture.Revision, registry.Source.RevisionId);
        Assert.Equal(CadV2ContractFixture.Hash, registry.Source.FileHash); Assert.Equal("000a", record.OriginalHandle); Assert.Equal("A", record.CanonicalHandle);
        Assert.Equal(0, record.Locator.RecordIndex); Assert.Equal(0, record.Locator.FirstPair); Assert.Equal(10, record.Locator.LastPhysicalLine);
        var fact = registry.Resolve(doc.Geometry[0].Evidence[0]); Assert.Equal("Start.X", fact.Role); Assert.Equal(new[] {1}, fact.PairIndices);
        Assert.Equal(3, fact.FirstPhysicalLine); Assert.Equal(4, fact.LastPhysicalLine);
        var absent = registry.Resolve(new CadSourceFactRef(1)); Assert.Equal(CadFactPresence.Absent, absent.Presence); Assert.Empty(absent.PairIndices);
        Assert.Null(absent.FirstPhysicalLine); Assert.Null(absent.LastPhysicalLine);
    }
    [Fact] public void InvalidSourceAuthorityHandlesRangesAndReferencesAreRejected()
    {
        Assert.Throws<InvalidDataException>(() => new CadSourceDocument(Guid.Empty, CadV2ContractFixture.Revision, CadV2ContractFixture.Hash));
        Assert.Throws<InvalidDataException>(() => new CadSourceDocument(CadV2ContractFixture.Drawing, Guid.Empty, CadV2ContractFixture.Hash));
        Assert.Throws<InvalidDataException>(() => new CadSourceDocument(CadV2ContractFixture.Drawing, CadV2ContractFixture.Revision, "not-a-hash"));
        Assert.Throws<InvalidDataException>(() => new CadSourceRecord(new(CadSourceRecordDomain.StructuralRecord, 0, 0, 1, 1, 4), "000a", "000A"));
        Assert.Throws<InvalidDataException>(() => new CadSourceRecord(new(CadSourceRecordDomain.StructuralRecord, 0, 0, 1, 1, 4), "A", null));
        Assert.Throws<InvalidDataException>(() => new CadSourceLocator(CadSourceRecordDomain.StructuralRecord, 0, 5, 4, 1, 2));
        Assert.Throws<InvalidDataException>(() => new CadSourceRecordRef(-1));
        Assert.Throws<InvalidDataException>(() => CadV2ContractFixture.Registry().Resolve(new CadSourceRecordRef(99)));
        Assert.Throws<InvalidDataException>(() => CadV2ContractFixture.Registry().Resolve(new CadSourceFactRef(99)));
    }
    [Theory] [InlineData("pairs")] [InlineData("lines")] [InlineData("presence")] [InlineData("order")] [InlineData("record")]
    public void BrokenEvidenceCannotEnterRegistry(string problem)
    {
        var r = CadV2ContractFixture.Registry();
        Assert.Throws<InvalidDataException>(() =>
        {
            var fact = problem switch
            {
                "pairs" => new CadSourceFact(new(0), "F", CadFactPresence.Present, [99], 3, 4),
                "lines" => new CadSourceFact(new(0), "F", CadFactPresence.Present, [1], 3, 100),
                "presence" => new CadSourceFact(new(0), "F", CadFactPresence.Absent, [1], null, null),
                "order" => new CadSourceFact(new(0), "F", CadFactPresence.Present, [2, 1], 3, 4),
                _ => new CadSourceFact(new(5), "F", CadFactPresence.Present, [1], 3, 4)
            };
            _ = new CadProvenanceRegistry(r.Source, r.Records, [fact]);
        });
    }
    [Fact] public void ReorderedOrDuplicateRegistryRecordsAreRejectedRatherThanRenumberingReferences()
    {
        var r = CadV2ContractFixture.Registry();
        Assert.Throws<InvalidDataException>(() => new CadProvenanceRegistry(r.Source, r.Records.Reverse().ToArray(), []));
        Assert.Throws<InvalidDataException>(() => new CadProvenanceRegistry(r.Source, [r.Records[0], r.Records[0]], []));
    }
    [Fact] public void PartialSiblingDataAndUnsupportedRecordRemainInContract()
    {
        var doc = CadIrJson.Deserialize(CadIrJson.Serialize(CadV2ContractFixture.Document()));
        Assert.Single(doc.Geometry); Assert.Equal(2, doc.Entities.Count); Assert.Equal(2, doc.Ledger.Count);
        Assert.Equal(CadNormalizationState.Normalized, doc.Ledger[0].State);
        Assert.Equal(CadNormalizationState.UnsupportedNormalization, doc.Ledger[1].State);
        Assert.Equal(CadNormalizationState.PartiallyNormalized, doc.Completeness.State); Assert.Single(doc.Diagnostics);
        var json = System.Text.Json.Nodes.JsonNode.Parse(CadIrJson.Serialize(doc))!;
        json["entities"]![1]!["kind"] = "insert";
        json["entities"]![1]!["disposition"] = "invalidNormalization";
        json["ledger"]![1]!["state"] = "invalidNormalization";
        json["diagnostics"]![0]!["code"] = "OCCURRENCE_REJECTED";
        var rejected = CadIrJson.Deserialize(json.ToJsonString());
        Assert.Single(rejected.Geometry); Assert.Empty(rejected.Occurrences);
        Assert.Equal(CadNormalizationState.InvalidNormalization, rejected.Ledger[1].State);
        Assert.Equal("OCCURRENCE_REJECTED", rejected.Diagnostics[0].Code);
    }
    [Fact] public void TypedCellProvenanceKeepsSourceIdentityInsteadOfInventingHandles()
    {
        var ordinary = new CadOccurrenceStep(new(0), new(1), null, null);
        var cell = new CadOccurrenceStep(new(0), new(1), 0, 0);
        Assert.Equal(ordinary.InsertSource.Index, cell.InsertSource.Index); Assert.Null(ordinary.Row); Assert.Equal(0, cell.Row);
        Assert.Throws<InvalidDataException>(() => new CadOccurrenceStep(new(0), new(1), 0, null));
        Assert.Throws<InvalidDataException>(() => new CadOccurrenceStep(new(0), new(1), -1, 0));
    }
    [Fact] public void OutOfRangeDocumentReferenceCannotHideInOtherwiseValidJson()
    {
        var text = CadIrJson.Serialize(CadV2ContractFixture.Document());
        var json = System.Text.Json.Nodes.JsonNode.Parse(text)!;
        json["entities"]![0]!["source"]!["index"] = 88;
        Assert.Throws<InvalidDataException>(() => CadIrJson.Deserialize(json.ToJsonString()));
    }
}
