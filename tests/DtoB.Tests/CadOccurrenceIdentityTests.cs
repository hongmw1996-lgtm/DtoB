using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using DtoB.Cad.Normalize.Occurrences;
using DtoB.Cad.V2;
using DtoB.Geometry;
using Xunit;
using static DtoB.Tests.Cad04EOccurrenceFixture;

namespace DtoB.Tests;

public sealed class CadOccurrencePathTests
{
    [Fact] public async Task OrdinaryNestedAndCellStepsHaveIndependentTypedOracle()
    {
        var result=Complete(Expand(await Snapshot(Block("B","A",Insert("D","B",columns:2))+Block("C","B"),Insert("A","A"))));
        var paths=result.Occurrences.Select(o=>o.Value.Path.Steps.Select(s=>(
            result.Sources.Resolve(s.InsertSource).CanonicalHandle!,result.Sources.Resolve(s.DefinitionSource).CanonicalHandle!,s.Row,s.Column)).ToArray()).ToArray();
        Assert.Equal(new[]{("A","B",(int?)null,(int?)null)},paths[0]);
        Assert.Equal(new[]{("A","B",(int?)null,(int?)null),("D","C",(int?)0,(int?)0)},paths[1]);
        Assert.Equal(new[]{("A","B",(int?)null,(int?)null),("D","C",(int?)0,(int?)1)},paths[2]);
    }
    [Fact] public void PathsOwnInputAndNeverExposeMutableCollections()
    {
        var steps=new[]{new CadOccurrenceStep(new(0),new(1),null,null)}; var path=new CadOccurrencePath(steps);
        steps[0]=new(new(2),new(3),0,0);
        Assert.Equal(0,path.Steps[0].InsertSource.Index);
        Assert.Throws<NotSupportedException>(()=>((IList<CadOccurrenceStep>)path.Steps).Clear());
        Assert.Throws<InvalidDataException>(()=>new CadOccurrenceStep(new(0),new(1),0,null));
    }
    [Fact] public void TypedTupleFramingDisambiguatesHandlesAndCellIndices()
    {
        var r=CadOccurrenceIdentityTests.Registry("A","BC","AB","C","D");
        CadOccurrenceStep S(int i,int? row=null,int? col=null)=>new(new(i),new(4),row,col);
        string Id(params CadOccurrenceStep[] steps)=>CadOccurrenceIdentity.Occurrence(r,new(steps)).Value;
        Assert.NotEqual(Id(S(0),S(1)),Id(S(2),S(3)));
        Assert.NotEqual(Id(S(0,1,23)),Id(S(0,12,3)));
        Assert.NotEqual(Id(S(0)),Id(S(0,0,0)));
    }
}
public sealed class CadOccurrenceIdentityTests
{
    internal static CadProvenanceRegistry Registry(params string?[] handles)=>new(Cad04BScalarFixture.Document,
        handles.Select((h,i)=>new CadSourceRecord(new(CadSourceRecordDomain.StructuralRecord,i,i,i,2*i+1,2*i+2),h,h)).ToArray(),[]);
    [Fact] public void HandFramedBytesIndependentlyMatchCad2OccurrenceId()
    {
        // Independent byte writer: no CadId.Create/CadIdentityField/production path encoder in the oracle.
        static byte[] Field(byte tag,byte[] payload)
        { var b=new byte[5+payload.Length]; b[0]=tag; BinaryPrimitives.WriteUInt32BigEndian(b.AsSpan(1),checked((uint)payload.Length)); payload.CopyTo(b,5); return b; }
        static byte[] Text(byte tag,string value)=>Field(tag,Encoding.UTF8.GetBytes(value));
        static byte[] Tuple(params byte[][] fields)
        { using var b=new MemoryStream(); var n=new byte[4]; BinaryPrimitives.WriteUInt32BigEndian(n,checked((uint)fields.Length)); b.Write(n); foreach(var f in fields)b.Write(f); return Field(6,b.ToArray()); }
        var authority=Tuple(Text(2,new string('1',32)),Text(2,new string('2',32)),Text(3,new string('A',64)),Text(4,"D"),
            Tuple(Tuple(Text(1,"insert"),Text(4,"A"),Text(4,"B"),Field(0,[]),Field(0,[])),
                Tuple(Text(1,"cell"),Text(4,"D"),Text(4,"C"),Text(5,"1"),Text(5,"2"))));
        var bytes=Tuple(Text(1,"dtob/cad2/id/v1"),Text(1,"occurrence"),authority);
        var expected="cad2:"+Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var path=new CadOccurrencePath([new(new(0),new(1),null,null),new(new(2),new(3),1,2)]);
        Assert.Equal(expected,CadOccurrenceIdentity.Occurrence(Registry("A","B","D","C"),path).Value);
    }
    [Fact] public void SourceDomainsAndHandlelessFallbackLocatorsAreSeparated()
    {
        var registry=Registry(null,null,"A");
        Assert.NotEqual(CadOccurrenceIdentity.Source(registry,new(0),CadIdentityDomain.Definition).Value,
            CadOccurrenceIdentity.Source(registry,new(0),CadIdentityDomain.SourceInsert).Value);
        Assert.NotEqual(CadOccurrenceIdentity.Occurrence(registry,new([new(new(0),new(2),null,null)])).Value,
            CadOccurrenceIdentity.Occurrence(registry,new([new(new(1),new(2),null,null)])).Value);
        Assert.Throws<InvalidDataException>(()=>CadOccurrenceIdentity.Source(registry,new(0),CadIdentityDomain.Leaf));
    }
    [Fact] public async Task RepeatExecutionPreservesIdsButDistinctParentsAndCellsDoNotCollide()
    {
        var s=await Snapshot(Block("B","A",Insert("D","B",columns:2))+Block("C","B"),Insert("A","A")+Insert("E","A"));
        var a=Complete(Expand(s)); var b=Complete(Expand(s));
        Assert.Equal(a.Occurrences.Select(o=>o.Value.Id.Value),b.Occurrences.Select(o=>o.Value.Id.Value));
        Assert.Equal(6,a.Occurrences.Select(o=>o.Value.Id.Value).Distinct().Count());
        Assert.All(a.Occurrences.Where(o=>o.Grid.IsArray),o=>Assert.Equal("D",a.Sources.Resolve(o.Value.InsertSource).CanonicalHandle));
    }
    [Fact] public async Task AmbiguousSourceHandleCannotBePublishedAsAValidatedCad2Identity()
    {
        var valid=await Snapshot(Block("B","B"),Insert("A","B"));
        // The real PHASE03 assembler already rejects duplicate handles. Deliberately inject an
        // invalid view into an owned snapshot to verify the normalization boundary also refuses it.
        var rows=valid.Records.Select(row=>row.Identity?.CanonicalHandle=="A"
            ? new DtoB.Cad.Raw.RawDrawingRecordView(row.StructuralRecordIndex,row.SourceRecord,row.SemanticDisposition,
                row.GeometryRecordIndex,row.Geometry,row.BlockAnnotationRecordIndex,row.BlockAnnotation,row.IdentityNodeIndex,
                row.Identity with { Validity=DtoB.Cad.Raw.DxfIdentityValidity.Ambiguous },row.BlockDefinitionIndex) : row).ToArray();
        var s=new DtoB.Cad.Raw.RawDrawingSnapshot(valid.IdentityEvidence,rows,valid.Diagnostics,valid.AssemblyDiagnostics,valid.Completeness);
        var r=Expand(s); Assert.Equal(CadExpansionStatus.InvalidInput,r.Status); Assert.Null(r.Expansion);
        Assert.Equal(0,r.TotalOccurrencesAllocated); Assert.Equal("SourceIdentity",Assert.Single(r.Diagnostics).Detail);
    }
    [Fact] public void OriginalHandleSpellingIsPreservedButOnlyCanonicalHandleEntersIdentity()
    {
        var canonical=Registry("A","B");
        var padded=new CadProvenanceRegistry(canonical.Source,[new(canonical.Records[0].Locator,"000a","A"),canonical.Records[1]],[]);
        var path=new CadOccurrencePath([new(new(0),new(1),null,null)]);
        Assert.Equal(CadOccurrenceIdentity.Occurrence(canonical,path).Value,CadOccurrenceIdentity.Occurrence(padded,path).Value);
        Assert.Equal("000a",padded.Resolve(new CadSourceRecordRef(0)).OriginalHandle);
    }
    [Fact] public void IdentityInputsContainNoTransformFloatsOrMachinePaths()
    {
        var registry=Registry("A","B"); var path=new CadOccurrencePath([new(new(0),new(1),null,null)]);
        var id=CadOccurrenceIdentity.Occurrence(registry,path); var definition=CadOccurrenceIdentity.Source(registry,new(1),CadIdentityDomain.Definition);
        var identity=CadTransform.FromTransform3(Transform3.Identity);
        var changed=CadTransform.FromTransform3(Transform3.Placement(new(123.456,78,0),0.123,new(2,3,1)));
        var before=new CadBlockOccurrence(id,new(0),definition,null,path,identity,identity,null);
        var after=new CadBlockOccurrence(CadOccurrenceIdentity.Occurrence(registry,path),new(0),definition,null,path,changed,changed,null);
        Assert.Equal(before.Id.Value,after.Id.Value);
        Assert.NotEqual(before.LocalTransform.Values,after.LocalTransform.Values);
        Assert.All(typeof(CadOccurrenceIdentity).GetMethods().Where(m=>m.DeclaringType==typeof(CadOccurrenceIdentity)),m=>
            Assert.DoesNotContain(m.GetParameters(),p=>new[]{typeof(double),typeof(CadTransform),typeof(Transform3)}.Contains(p.ParameterType)));
    }
}
