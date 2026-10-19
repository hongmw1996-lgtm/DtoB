using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using DtoB.Cad.V2;
using Xunit;

namespace DtoB.Tests;

public sealed class CadIrJsonV2Tests
{
    [Fact] public void V2RoundtripIsDeterministicWhileLegacyMajorTwoStillFails()
    {
        var doc = CadV2ContractFixture.Document(); var bytes = CadIrJson.SerializeUtf8(doc);
        var copy = CadIrJson.Deserialize(CadIrJson.Serialize(doc));
        Assert.Equal(2, copy.SchemaVersion); Assert.Equal(bytes, CadIrJson.SerializeUtf8(copy));
        Assert.Throws<InvalidDataException>(() => DtoB.Core.Contract.Version(2));
        Assert.Throws<InvalidDataException>(() => DtoB.Core.IrJson.Deserialize<DtoB.Cad.CadDocument>(CadIrJson.Serialize(doc)));
        ContractTests.Cad().ValidateStructure(); ContractTests.Bim().ValidateStructure();
        Assert.False(bytes.Take(3).SequenceEqual(new byte[] { 0xEF, 0xBB, 0xBF }));
        Assert.EndsWith("\n", CadIrJson.Serialize(copy)); Assert.DoesNotContain("\r", CadIrJson.Serialize(copy));
    }
    [Theory]
    [InlineData(null)] [InlineData("1")] [InlineData("3")] [InlineData("2.0")] [InlineData("2.5")]
    [InlineData("\"2\"")] [InlineData("true")] [InlineData("null")]
    public void MissingMalformedAndUnsupportedMajorsAreRejected(string? version)
    {
        var json = JsonNode.Parse(CadIrJson.Serialize(CadV2ContractFixture.Document()))!.AsObject();
        if (version is null) json.Remove("schemaVersion"); else json["schemaVersion"] = JsonNode.Parse(version);
        Assert.Throws<InvalidDataException>(() => CadIrJson.Deserialize(json.ToJsonString()));
    }
    [Theory]
    [InlineData("{broken")] [InlineData("null")] [InlineData("[]")]
    public void InvalidEnvelopeIsRejected(string json) => Assert.Throws<InvalidDataException>(() => CadIrJson.Deserialize(json));

    [Theory]
    [InlineData("profileId")] [InlineData("conventions")] [InlineData("provenance")]
    [InlineData("geometry")] [InlineData("completeness")] [InlineData("sourceToModel")]
    public void RequiredRootMembersCannotDefaultSilently(string member)
    {
        var json = JsonNode.Parse(CadIrJson.Serialize(CadV2ContractFixture.Document()))!.AsObject(); json.Remove(member);
        Assert.Throws<InvalidDataException>(() => CadIrJson.Deserialize(json.ToJsonString()));
    }
    [Fact] public void NullableFactsRequireExplicitNullAndKeepMissingMeasurementDistinctFromZero()
    {
        var annotation = new CadAnnotation(CadAnnotationKind.Dimension, null, [], null, null, null);
        var doc = CadV2ContractFixture.Document();
        var entities = doc.Entities.ToArray(); entities[1] = new(entities[1].Id, entities[1].Source, CadEntityKind.Annotation,
            CadNormalizationState.UnsupportedNormalization, null, null, annotation, []);
        var json = CadIrJson.Serialize(CadV2ContractFixture.Document(entities: entities));
        Assert.Null(CadIrJson.Deserialize(json).Entities[1].Annotation!.SourceMeasurementMm);
        var node = JsonNode.Parse(json)!; node["entities"]![1]!["annotation"]!.AsObject().Remove("sourceMeasurementMm");
        Assert.Throws<InvalidDataException>(() => CadIrJson.Deserialize(node.ToJsonString()));
    }
    [Theory]
    [InlineData("unknown")] [InlineData("nested-unknown")] [InlineData("duplicate")]
    [InlineData("nested-duplicate")] [InlineData("bad-kind")] [InlineData("enum-number")]
    [InlineData("null-record")] [InlineData("missing-number")]
    public void UnknownDuplicateInvalidNestedShapeCannotEnterContract(string mutation)
    {
        var text = CadIrJson.Serialize(CadV2ContractFixture.Document()); var json = JsonNode.Parse(text)!;
        switch (mutation)
        {
            case "unknown": json["unknown"] = 1; break;
            case "nested-unknown": json["provenance"]!["source"]!["extra"] = 1; break;
            case "duplicate": text = text.Replace("\"schemaVersion\": 2", "\"schemaVersion\": 2, \"schemaVersion\": 2"); break;
            case "nested-duplicate": text = text.Replace("\"firstPair\": 0", "\"firstPair\": 0, \"firstPair\": 0"); break;
            case "bad-kind": json["geometry"]![0]!["$kind"] = "future-shape"; break;
            case "enum-number": json["conventions"]!["length"] = 0; break;
            case "null-record": json["provenance"]!["records"]![0] = null; break;
            case "missing-number": json["geometry"]![0]!["startMm"]!.AsObject().Remove("x"); break;
        }
        Assert.Throws<InvalidDataException>(() => CadIrJson.Deserialize(mutation.Contains("duplicate", StringComparison.Ordinal) ? text : json.ToJsonString()));
    }
    [Fact] public void EveryDeclaredGeometryPayloadHasStrictDeterministicRoundtrip()
    {
        var source = CadV2ContractFixture.Record(0); var plane = new CadPlane(new(0, 0, 0), new(1, 0, 0), new(0, 1, 0));
        var id = CadV2ContractFixture.Id("geometry", CadIdentityDomain.Leaf);
        var values = new CadGeometry[] {new CadPoint(id, source, [], new(1, 2, 3)),
            new CadLine(id, source, [], new(0, 0, 0), new(1, 2, 3)), new CadCircle(id, source, [], plane, 2),
            new CadArc(id, source, [], plane, 2, 0, 1.5), new CadPolyline(id, source, [], [], true),
            new CadSolid(id, source, [], [new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)])};
        foreach (var value in values)
        {
            var doc = CadV2ContractFixture.Document(geometry: [value]); var json = CadIrJson.Serialize(doc);
            Assert.Equal(json, CadIrJson.Serialize(CadIrJson.Deserialize(json)));
        }
    }
    [Theory] [InlineData("")] [InlineData("fr-FR")] [InlineData("ko-KR")]
    public void ContractDigestIsInvariantAcrossCulturesAndIndependentOfWorkingDirectory(string culture)
    {
        var expected = CadIrJson.SerializeUtf8(CadV2ContractFixture.Document());
        var prior = CultureInfo.CurrentCulture; var priorUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture); CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            var actual = CadIrJson.SerializeUtf8(CadV2ContractFixture.Document()); Assert.Equal(expected, actual);
            var output = Environment.GetEnvironmentVariable("DTOB_04A_EVIDENCE_DIRECTORY");
            if (output is not null)
            {
                Directory.CreateDirectory(output);
                File.WriteAllBytes(Path.Combine(output, "contract-" + (culture.Length == 0 ? "invariant" : culture) + ".json"), actual);
                File.WriteAllText(Path.Combine(output, "contract-" + (culture.Length == 0 ? "invariant" : culture) + ".sha256"), Convert.ToHexString(SHA256.HashData(actual)));
            }
        }
        finally { CultureInfo.CurrentCulture = prior; CultureInfo.CurrentUICulture = priorUi; }
        // Separate VSTest processes, launched by the gate in two CWDs, execute this same fixture.
    }
}
