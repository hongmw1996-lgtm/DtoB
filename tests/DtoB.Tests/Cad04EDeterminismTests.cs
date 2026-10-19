using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using DtoB.Cad.Normalize.Occurrences;
using DtoB.Cad.Raw;
using Xunit;
using static DtoB.Tests.Cad04EOccurrenceFixture;

namespace DtoB.Tests;

public sealed class Cad04EDeterminismTests
{
    internal static object Projection(CadOccurrenceExpansionResult r)
    {
        var a=r.Expansion;
        return new
        {
            r.Status,r.TotalOccurrencesAllocated,Document=a?.Sources.Source,a?.Options,a?.NumericPolicyId,
            Definitions=a?.Definitions.Select(d=>new{d.Id,d.Source,d.Base.Name,d.Base.BaseMm,d.ChildSources}),
            Occurrences=a?.Occurrences.Select(o=>new
            {
                o.Value.Id,o.Value.ParentOccurrenceId,o.Value.DefinitionId,o.SourceInsertId,o.Depth,o.SourceOrderOrdinal,o.CellOrdinal,
                Source=a.Sources.Resolve(o.Value.InsertSource),o.Value.Path,
                Local=o.Value.LocalTransform.Values,Accumulated=o.Value.AccumulatedTransform.Values,
                o.LocalOrientationSign,o.AccumulatedOrientationSign,o.Grid
            }),
            Diagnostics=r.Diagnostics.Select(d=>new{d.Code,d.Source,d.Path,d.Detail,d.Requested,d.Limit,d.Placement})
        };
    }
    [Fact] public async Task FixedOccurrenceExpansionEvidenceMatchesAcrossCulturesProcessesAndWorkingDirectories()
    {
        var old=CultureInfo.CurrentCulture; var oldUi=CultureInfo.CurrentUICulture; var bytes=new List<byte[]>();
        try
        {
            foreach(var culture in new[]{"","fr-FR","ko-KR"})
            {
                CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo(culture); CultureInfo.CurrentUICulture=CultureInfo.GetCultureInfo(culture);
                var blocks=Block("B","A",Insert("D","B",new(1000,2000,0),new(-2,3,1),90,new(0,1,0),2,2,200,100))
                    +Block("C","B",Insert("E","C",new(1,2,0))+Insert("F","MISSING"))
                    +Block("10","C",Insert("11","A"));
                var r=Expand(await Snapshot(blocks,Insert("A","A",new(100,0,0),angle:90)+Insert("12","A")));
                Assert.Equal(CadExpansionStatus.Partial,r.Status); Assert.Equal(18,r.TotalOccurrencesAllocated);
                Assert.Equal(16,r.Diagnostics.Count); Assert.Equal(18,r.Expansion!.Occurrences.Count);
                bytes.Add(JsonSerializer.SerializeToUtf8Bytes(Projection(r)));
            }
        }
        finally { CultureInfo.CurrentCulture=old; CultureInfo.CurrentUICulture=oldUi; }
        Assert.Equal(bytes[0],bytes[1]); Assert.Equal(bytes[0],bytes[2]);
        var directory=Environment.GetEnvironmentVariable("DTOB_04E_EVIDENCE_DIRECTORY");
        if(directory is not null)
        {
            Directory.CreateDirectory(directory); File.WriteAllBytes(Path.Combine(directory,"occurrence-expansion-evidence.json"),bytes[0]);
            File.WriteAllText(Path.Combine(directory,"occurrence-expansion-evidence.sha256"),Convert.ToHexString(SHA256.HashData(bytes[0]))+"\n");
        }
    }
}
public sealed class Cad04EControlledSourceTests
{
    [Fact] public async Task OfficialSourceHasSeventeenRootsEightUnreachableNestedInsertsAndNoArrays()
    {
        var s=await Cad04BScalarFixture.Controlled(); var before=JsonSerializer.Serialize(s);
        var inserts=s.Records.Where(x=>x.BlockAnnotation?.Payload is RawInsert).ToArray();
        Assert.Equal(25,inserts.Length); Assert.Equal(17,inserts.Count(x=>x.SourceSection=="ENTITIES"));
        Assert.Equal(8,inserts.Count(x=>x.SourceSection=="BLOCKS"));
        Assert.All(inserts,x=>Assert.False(((RawInsert)x.BlockAnnotation!.Payload!).OccurrenceExpansionUnsupported));
        var r=Expand(s); var a=Complete(r);
        Assert.Equal(17,r.TotalOccurrencesAllocated); Assert.Equal(17,a.Occurrences.Count); Assert.Equal(3,a.Definitions.Count);
        Assert.All(a.Occurrences,o=> { Assert.Equal(1,o.Depth); Assert.Null(o.Value.ParentOccurrenceId); });
        Assert.Equal(new[]{"B9","BB","BD","BF","C3","C7","CA","CC","CE","109","10B","10D","10F","113","117","119","11B"},
            a.Occurrences.Select(o=>a.Sources.Resolve(o.Value.InsertSource).CanonicalHandle));
        var door=Assert.Single(a.Occurrences,o=>a.Sources.Resolve(o.Value.InsertSource).CanonicalHandle=="C3");
        Assert.Equal("D_SINGLE_900",door.Definition.Base.Name.Value);
        // Source bytes C3 pair3760: insertion(4000,2550,0), R90, unit scale/base0.
        Point(door,new(1,0,0),new(4000,2551,0));
        var db=Assert.Single(inserts,x=>x.Identity?.CanonicalHandle=="DB");
        Assert.Equal(130,db.StructuralRecordIndex); Assert.Equal(2971,db.SourceRecord.Source.StartPairIndex);
        Assert.Equal("_ARCHTICK",((RawInsert)db.BlockAnnotation!.Payload!).TargetName.Text);
        Assert.DoesNotContain(a.Occurrences,o=>a.Sources.Resolve(o.Value.InsertSource).CanonicalHandle=="DB");
        Assert.Equal(before,JsonSerializer.Serialize(s));
        var directory=Environment.GetEnvironmentVariable("DTOB_04E_EVIDENCE_DIRECTORY");
        if(directory is not null)
        {
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory,"controlled-occurrence-evidence.json"),JsonSerializer.SerializeToUtf8Bytes(new
            {
                RawInsertCount=inserts.Length,TopLevel=17,BlockScope=8,ArrayCount=0,ReachableNested=0,
                NotTraversed="DIMENSION graphic-block references are not root INSERTs",
                SnapshotBeforeSha256=Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(before))),
                SourceInsertInventory=inserts.Select(x=>new{x.StructuralRecordIndex,x.SourceSection,x.Identity!.CanonicalHandle,x.SourceRecord.Source,
                    Target=((RawInsert)x.BlockAnnotation!.Payload!).TargetName.Text}),Expansion=Cad04EDeterminismTests.Projection(r)
            }));
        }
    }
}
