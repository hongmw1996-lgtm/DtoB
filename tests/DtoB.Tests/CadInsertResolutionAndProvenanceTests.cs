using System.Text.Json;
using DtoB.Cad.Normalize.Placement;
using DtoB.Cad.Normalize.Scalars;
using DtoB.Cad.Raw;
using DtoB.Cad.V2;
using DtoB.Geometry;
using Xunit;
using static DtoB.Tests.Cad04DPlacementFixture;

namespace DtoB.Tests;

public sealed class CadInsertDefinitionResolutionTests
{
    [Theory][InlineData("B")][InlineData("b")][InlineData("*D42")][InlineData("*A1")]
    public async Task OneFactualTargetResolvesWithoutAnonymousNameInference(string name)
    {
        var s=await Cad04BScalarFixture.Snapshot(Document(Block(name=="b"?"B":name),Insert(),name));
        var p=Placement(s); Assert.Equal(name,p.Target.Value); Assert.Equal(name=="b"?"B":name,p.Block.Name.Value);
    }
    [Fact] public async Task MissingDefinitionIsStructuredRefusal()
    {
        var r=Result(await Cad04BScalarFixture.Snapshot(Document("",Insert())));
        Assert.Equal(CadPlacementDiagnosticCode.MissingBlockDefinition,Assert.Single(r.Diagnostics).Code); Assert.Null(r.Placement);
    }
    [Fact] public async Task DuplicateTargetIncludingCaseCollisionDoesNotPickFirst()
    {
        var s=await Cad04BScalarFixture.Snapshot(Document(Block()+Block("b",handle:"C"),Insert()));
        Assert.Equal(CadPlacementDiagnosticCode.AmbiguousBlockDefinition,Assert.Single(Result(s).Diagnostics).Code);
    }
    [Theory][InlineData("-1",CadPlacementDiagnosticCode.InvalidBlockDefinition)][InlineData("4",CadPlacementDiagnosticCode.UnsupportedDefinition)]
    [InlineData("8",CadPlacementDiagnosticCode.UnsupportedDefinition)][InlineData("128",CadPlacementDiagnosticCode.UnsupportedDefinition)]
    public async Task InvalidOrExternalDefinitionProfileIsNotTreatedAsLocalContent(string flags,CadPlacementDiagnosticCode expected)
    { var r=Result(await Cad04BScalarFixture.Snapshot(Document(Block(flags:flags),Insert()))); Assert.Null(r.Placement); Assert.Equal(expected,Assert.Single(r.Diagnostics).Code); }
    [Fact] public async Task SelfReferencingChildIsNotTraversedOrComposed()
    {
        var child="0\nINSERT\n5\nF\n330\n0\n2\nB\n10\n1\n20\n2\n30\n3\n";
        var block=Block().Replace("0\nENDBLK\n",child+"0\nENDBLK\n",StringComparison.Ordinal);
        var s=await Cad04BScalarFixture.Snapshot(Document(block,Insert(new(100,200,300))));
        var root=Placement(s); ExpectedPoint(root,new(0,0,0),new(100,200,300));
        var childRow=s.Records.Single(r=>r.SourceSection=="BLOCKS" && r.BlockAnnotation?.Payload is RawInsert);
        var local=CadInsertLocalTransform.Create(s,childRow,Policy); Assert.Equal(CadPlacementStatus.Normalized,local.Status);
        ExpectedPoint(local.Placement!,new(0,0,0),new(1,2,3));
    }
    [Theory][InlineData("70\n2\n")][InlineData("71\n3\n")][InlineData("70\n0\n")][InlineData("71\n-1\n")]
    public async Task ArraysAndInvalidCountsDoNotCreateAnOriginCell(string count)
    {
        var r=Result(await Cad04BScalarFixture.Snapshot(Document(Block(),Insert(extra:count))));
        Assert.Null(r.Placement); Assert.Equal(CadPlacementDiagnosticCode.UnsupportedInsertProfile,Assert.Single(r.Diagnostics).Code);
    }
    [Theory][InlineData(4,true)][InlineData(0,true)][InlineData(1,false)][InlineData(3,false)]
    public async Task ActualBlockRecordUnitPolicyIsPreserved(int units,bool accepted)
    {
        var table=$"0\nSECTION\n2\nTABLES\n0\nTABLE\n2\nBLOCK_RECORD\n70\n1\n0\nBLOCK_RECORD\n5\nD\n330\n0\n2\nB\n70\n{units}\n0\nENDTAB\n0\nENDSEC\n";
        var r=Result(await Cad04BScalarFixture.Snapshot(Document(Block(),Insert(),tables:table)));
        Assert.Equal(accepted?CadPlacementStatus.Normalized:CadPlacementStatus.Unsupported,r.Status);
        if(accepted) Assert.NotNull(r.Placement!.Block.BlockUnits);
        else Assert.Equal(CadPlacementDiagnosticCode.UnsupportedBlockUnits,Assert.Single(r.Diagnostics).Code);
    }
    [Theory][InlineData(true)][InlineData(false)]
    public async Task ResolvedBlockRecordOwnerMustAgreeWithNamedDefinition(bool matching)
    {
        var table="0\nSECTION\n2\nTABLES\n0\nTABLE\n2\nBLOCK_RECORD\n70\n1\n0\nBLOCK_RECORD\n5\nD\n330\n0\n2\n"+(matching?"B":"OTHER")+"\n70\n4\n0\nENDTAB\n0\nENDSEC\n";
        var block=Block().Replace("330\n0\n2\nB\n","330\nD\n2\nB\n",StringComparison.Ordinal);
        var r=Result(await Cad04BScalarFixture.Snapshot(Document(block,Insert(),tables:table)));
        if(matching) Assert.Equal(CadPlacementStatus.Normalized,r.Status);
        else { Assert.Null(r.Placement); Assert.Equal(CadPlacementDiagnosticCode.InvalidBlockDefinition,Assert.Single(r.Diagnostics).Code); Assert.Equal("BlockRecordOwner",r.Diagnostics[0].Field); }
    }
}
public sealed class CadInsertProvenanceTests
{
    [Fact] public async Task PublishedPlacementIsImmutableAndRetainsDefaultsAndAllSourceFacts()
    {
        var s=await Snapshot(new(10,20,30),new(100,200,300)); var before=JsonSerializer.Serialize(s);
        var p=Placement(s); Assert.Equal("A",p.Scale.Source.OriginalHandle); Assert.Equal("B",p.Block.Source.OriginalHandle);
        Assert.Same(p.Anchor.Units,p.Block.Units); Assert.Equal(s.DocumentProvenance!.FileHash,p.Block.Source.Document.FileHash);
        Assert.Equal("DXF.INSERT.Rotation.DefaultZero",p.Rotation.DefaultRule);
        Assert.Contains("DXF.OCS.Extrusion.DefaultPositiveZ",p.DefaultRules); Assert.Equal(5,p.DefaultRules.Count);
        Assert.Single(p.Target.PairIndices); Assert.Single(p.Block.Name.PairIndices); Assert.Single(p.Anchor.Input.Coordinates.X.PairIndices);
        var saved=JsonSerializer.Serialize(p); var exported=p.Transform.ToTransform3(); exported.Matrix[3]=999;
        Assert.Equal(saved,JsonSerializer.Serialize(p)); Assert.Equal(before,JsonSerializer.Serialize(s));
        Assert.Throws<NotSupportedException>(()=>((IList<double>)p.Transform.Values)[0]=99);
        Assert.Throws<NotSupportedException>(()=>((IList<string>)p.DefaultRules)[0]="changed");
        var array=new double[]{1,0,0,1,0,1,0,2,0,0,1,3,0,0,0,1}; var value=new CadTransform(array); array[3]=999;
        Assert.Equal(1,value.Values[3]); ExpectedPoint(p,new(10,20,30),new(100,200,300));
    }
    [Fact] public async Task ForeignUnitsAndForeignRowsAndCancellationAreRejected()
    {
        var s=await Snapshot(); var row=InsertRow(s);
        var r=CadInsertLocalTransform.Create(s,row,Policy,Cad04BScalarFixture.Units());
        Assert.Equal(CadPlacementDiagnosticCode.SourceAuthorityMismatch,Assert.Single(r.Diagnostics).Code);
        var other=await Snapshot(insertion:new(1,2,3)); Assert.Throws<InvalidDataException>(()=>CadInsertLocalTransform.Create(s,InsertRow(other),Policy));
        using var c=new CancellationTokenSource(); c.Cancel(); Assert.Throws<OperationCanceledException>(()=>CadInsertLocalTransform.Create(s,row,Policy,cancellationToken:c.Token));
    }
    [Fact] public async Task ExplicitUnitsUseExistingDecisionWithoutImplicitMillimeterFallback()
    {
        var s=await Snapshot(units:0); Assert.Equal(CadPlacementDiagnosticCode.UnresolvedUnits,Assert.Single(Result(s).Diagnostics).Code);
        var units=CadUnitNormalizer.Resolve(RawScalarAccess.HeaderUnits(s),new(CadSourceLengthUnit.Centimeters,"owner","explicit unitless decision"));
        var r=CadInsertLocalTransform.Create(s,InsertRow(s),Policy,units); Assert.Equal(CadPlacementStatus.Normalized,r.Status);
        Assert.Same(units,r.Placement!.Block.Units);
    }
    [Fact] public async Task ControlledArchTickProducesOneLocalTransformWithoutExpansion()
    {
        var s=await Cad04BScalarFixture.Controlled(); var row=s.Records.Single(r=>r.SourceRecord.Source.StartPairIndex==2971);
        var before=JsonSerializer.Serialize(s); var result=CadInsertLocalTransform.Create(s,row,Policy);
        Assert.Equal(CadPlacementStatus.Normalized,result.Status); var p=result.Placement!;
        Assert.Equal("DB",p.Scale.Source.OriginalHandle); Assert.Equal("_ARCHTICK",p.Target.Value);
        Assert.Equal(new Point3(0,0,0),p.Block.BaseMm); Assert.Equal(new Point3(0,-1300,0),p.Anchor.PointMm);
        Assert.Equal(Math.PI,p.Rotation.Value); Cad04CCoordinateFixture.Vector(p.Scale.Scale,0.175,0.175,1);
        ExpectedPoint(p,new(0,0,0),new(0,-1300,0)); ExpectedPoint(p,new(1,0,0),new(-0.175,-1300,0));
        Assert.Equal(before,JsonSerializer.Serialize(s));
        var directory=Environment.GetEnvironmentVariable("DTOB_04D_EVIDENCE_DIRECTORY");
        if(directory is not null){Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory,"controlled-local-transform.json"),JsonSerializer.Serialize(p));}
    }
}
