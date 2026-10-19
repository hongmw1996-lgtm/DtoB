using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using DtoB.Cad.Normalize;
using DtoB.Cad.Normalize.Assembly;
using DtoB.Cad.Raw;
using DtoB.Cad.V2;
using Xunit;

namespace DtoB.Tests;

public sealed class Cad04HReleaseTests
{
    internal static void ValidateCoverage(IEnumerable<string> disk,IEnumerable<string> paths)
    {
        var a=disk.Order(StringComparer.Ordinal).ToArray(); var b=paths.Order(StringComparer.Ordinal).ToArray();
        if(b.Distinct(StringComparer.OrdinalIgnoreCase).Count()!=b.Length || !a.SequenceEqual(b))
            throw new InvalidDataException("Fixture omission, duplicate or unreviewed input.");
    }
    [Theory][InlineData("omission")][InlineData("duplicate")][InlineData("case-duplicate")]
    public void ManifestCoverageRejectsSilentLossAndDuplicatePaths(string mutation)
    {
        var original=Cad04HCorpus.Manifest().Select(f=>f.Path).ToArray();
        var altered=mutation=="omission"?original.Skip(1):original.Append(mutation=="duplicate"?original[0]:original[0].ToUpperInvariant());
        Assert.Throws<InvalidDataException>(()=>ValidateCoverage(original,altered));
    }
    [Fact] public void CorpusManifestProtectsEveryInputAndExplainsEveryExclusion()
    {
        var manifest=Cad04HCorpus.Manifest(); Assert.Equal(manifest.Length,manifest.Select(f=>f.Key).Distinct().Count());
        foreach(var f in manifest)
        {
            Assert.False(Path.IsPathRooted(f.Path));Assert.DoesNotContain("..",f.Path,StringComparison.Ordinal);
            var bytes=File.ReadAllBytes(Cad04HCorpus.PathOf(f.Path));Assert.Equal(f.Bytes,bytes.LongLength);Assert.Equal(f.SHA256,Cad04HCorpus.Hash(bytes));
            Assert.NotEmpty(f.InputProfile);Assert.NotEmpty(f.Coverage);Assert.NotEmpty(f.ExpectedState);
            if(!f.CadGolden)Assert.NotEmpty(f.Exclusion);
        }
        var dir=Cad04HCorpus.PathOf("datasets/controlled/phase04/goldens");
        var goldens=JsonNode.Parse(File.ReadAllText(Path.Combine(dir,"manifest.json")))!.AsArray();
        Assert.Equal(manifest.Where(f=>f.CadGolden).Select(f=>f.Key).Order(),goldens.Select(g=>g!["Key"]!.GetValue<string>()).Order());
        var expected=new List<string>{"manifest.json"};
        foreach(var g in goldens)
        {
            expected.Add(g!["SummaryFile"]!.GetValue<string>());
            if(g["CanonicalFile"] is { } file)expected.Add(file.GetValue<string>());
        }
        ValidateCoverage(Directory.GetFiles(dir).Select(Path.GetFileName).Select(p=>p!),expected);
    }
    public static IEnumerable<object[]> Goldens()=>Cad04HCorpus.Cases.Where(f=>f.CadGolden).Select(f=>new object[]{f.Key});
    [Theory,MemberData(nameof(Goldens))]
    public async Task ReleaseGoldensAreStrictAndCannotBeRegeneratedByTheAssertion(string key)
    {
        var f=Cad04HCorpus.Cases.Single(f=>f.Key==key);var (raw,r)=await Cad04HCorpus.Read(f);var d=Cad04GFixture.Document(r);
        Cad04HCorpus.SourceAndMathOracle(f,raw,d);var bytes=Cad04HCorpus.Triple(d);
        var dir=Cad04HCorpus.PathOf("datasets/controlled/phase04/goldens");
        var manifest=JsonNode.Parse(File.ReadAllText(Path.Combine(dir,"manifest.json")))!.AsArray();
        var entry=manifest.Single(e=>e!["Key"]!.GetValue<string>()==key)!;
        Assert.Equal(f.Path,entry["SourcePath"]!.GetValue<string>());Assert.Equal(f.SHA256,entry["SourceSHA256"]!.GetValue<string>());
        Assert.Equal(entry["SHA256"]!.GetValue<string>(),Cad04HCorpus.Hash(bytes));
        Assert.Equal(entry["Bytes"]!.GetValue<int>(),bytes.Length);
        var summaryFile=Path.Combine(dir,key+".summary.json");
        Assert.Equal(entry["SummarySHA256"]!.GetValue<string>(),Cad04HCorpus.Hash(File.ReadAllBytes(summaryFile)));
        Assert.Equal(File.ReadAllText(summaryFile),Cad04HCorpus.Summary(d,bytes));
        if(entry["CanonicalFile"] is { } file)Assert.Equal(File.ReadAllBytes(Path.Combine(dir,file.GetValue<string>())),bytes);
    }
    [Fact] public async Task NonsemanticContextIndexConstructionOrderCannotChangeCanonicalOutput()
    {
        var raw=await Cad04BScalarFixture.Controlled();var changed=0;
        var records=raw.Records.Select(row=>
        {
            var s=row.SourceRecord;if(s.ContextMarkers.Count>1)changed++;
            // Context markers carry explicit pair keys. Their index construction enumeration is
            // nonsemantic; lexical pair/record/child/vertex/diagnostic order stays untouched.
            var copy=new DxfStructuralRecord(s.RecordType,s.Scope,s.Source,s.Disposition,s.Fields,
                s.UnmappedPairIndices,s.ContextMarkers.Reverse(),s.References);
            return new RawDrawingRecordView(row.StructuralRecordIndex,copy,row.SemanticDisposition,row.GeometryRecordIndex,row.Geometry,
                row.BlockAnnotationRecordIndex,row.BlockAnnotation,row.IdentityNodeIndex,row.Identity,row.BlockDefinitionIndex);
        }).ToArray();
        Assert.True(changed>0);
        var equivalent=new RawDrawingSnapshot(raw.IdentityEvidence,records,raw.Diagnostics,raw.AssemblyDiagnostics,raw.Completeness);
        Assert.Equal(raw.LexicalEvidence.Pairs,equivalent.LexicalEvidence.Pairs);
        var a=Cad04HCorpus.Triple(Cad04GFixture.Document(Cad04GFixture.Normalize(raw)));
        var b=Cad04HCorpus.Triple(Cad04GFixture.Document(Cad04GFixture.Normalize(equivalent)));
        Assert.Equal(a,b);Assert.Equal(Cad04HCorpus.ControlledHash,Cad04HCorpus.Hash(b));
    }
    [Theory]
    [InlineData("unknown")][InlineData("missing")][InlineData("duplicate")][InlineData("schema")]
    [InlineData("enum")][InlineData("id")][InlineData("reference")][InlineData("matrix")][InlineData("list")]
    public async Task StrictCodecRejectsMutationsOfActualNormalizedDocument(string mutation)
    {
        var f=Cad04HCorpus.Cases.Single(f=>f.Key=="nested");var (_,r)=await Cad04HCorpus.Read(f);
        var text=CadIrJson.Serialize(Cad04GFixture.Document(r));var node=JsonNode.Parse(text)!;
        switch(mutation)
        {
            case "unknown":node["provenance"]!["source"]!["extra"]=1;break;
            case "missing":node.AsObject().Remove("geometry");break;
            case "duplicate":text=text.Replace("\"schemaVersion\": 2","\"schemaVersion\": 2, \"schemaVersion\": 2",StringComparison.Ordinal);Assert.NotEqual(text,CadIrJson.Serialize(r.Document!));break;
            case "schema":node["schemaVersion"]=3;break;
            case "enum":node["completeness"]!["state"]="FutureState";break;
            case "id":node["entities"]![0]!["id"]="cad2:leaf:invalid";break;
            case "reference":node["modelSpace"]![0]!["entityId"]="cad2:"+new string('a',64);break;
            case "matrix":node["sourceToModel"]=new JsonArray(1,2,3);break;
            case "list":node["definitions"]![0]!["childEntityIds"]=new JsonArray((JsonNode?)null);break;
        }
        Assert.Throws<InvalidDataException>(()=>CadIrJson.Deserialize(mutation=="duplicate"?text:node.ToJsonString()));
    }
    [Theory][InlineData("profile")][InlineData("authority")][InlineData("geometry-budget")][InlineData("diagnostics")][InlineData("cancel")]
    public async Task WholePipelineRefusalPublishesNoDocument(string condition)
    {
        var f=Cad04HCorpus.Cases.Single(f=>f.Key=="minsert");var (raw,_)=await Cad04HCorpus.Read(f);
        var policy=Cad04DPlacementFixture.Policy;var options=new CadNormalizationOptions(condition=="profile"?"wrong":policy.ProfileId,
            new(32,10000,100000,condition=="geometry-budget"?1:250000,condition=="diagnostics"?1:5000));
        if(condition=="authority")raw=new(raw.IdentityEvidence,raw.Records,raw.Diagnostics,raw.AssemblyDiagnostics,raw.Completeness with{SourceEvidenceComplete=false});
        var r=new CadNormalizer(policy).Normalize(raw,options,new(condition=="cancel"));Assert.Null(r.Document);
        Assert.Equal(condition=="cancel"?CadNormalizationStatus.Cancelled:condition is "geometry-budget" or "diagnostics"?CadNormalizationStatus.ResourceLimitExceeded:CadNormalizationStatus.InvalidNormalization,r.Status);
        Assert.NotEmpty(r.Diagnostics);
    }
    [Theory][InlineData(10000,true)][InlineData(10001,false)]
    public async Task ExactMinsertCellLimitAndLimitPlusOneReachFinalNormalizer(int cells,bool allowed)
    {
        // Retain a valid style catalog so diagnostic exhaustion cannot mask the cell boundary.
        var source=File.ReadAllText(Cad04HCorpus.PathOf("datasets/controlled/phase04/minsert.dxf"));
        var changed=source.Replace("70\n3\n71\n2\n","70\n"+cells.ToString(System.Globalization.CultureInfo.InvariantCulture)+"\n71\n1\n",StringComparison.Ordinal);
        Assert.NotEqual(source,changed);var raw=await Cad04BScalarFixture.Snapshot(changed);
        var before=JsonSerializer.SerializeToUtf8Bytes(raw);var result=Cad04GFixture.Normalize(raw);
        Assert.Equal(before,JsonSerializer.SerializeToUtf8Bytes(raw));
        if(allowed)Assert.Equal(cells,Cad04GFixture.Document(result).Occurrences.Count);
        else {Assert.Null(result.Document);Assert.Equal(CadNormalizationStatus.ResourceLimitExceeded,result.Status);}
    }
}
