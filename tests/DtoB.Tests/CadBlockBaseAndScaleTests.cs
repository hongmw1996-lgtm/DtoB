using DtoB.Cad.Normalize.Placement;
using DtoB.Cad.Normalize.Scalars;
using DtoB.Cad.Raw;
using DtoB.Geometry;
using Xunit;
using static DtoB.Tests.Cad04DPlacementFixture;

namespace DtoB.Tests;

public sealed class CadBlockBaseNormalizationTests
{
    [Theory][InlineData(1,25.4)][InlineData(2,304.8)][InlineData(4,1)][InlineData(5,10)][InlineData(6,1000)]
    public async Task ActualNonzeroBaseUsesDocumentUnitOnceAndPreservesEvidence(int units,double scale)
    {
        var s=await Snapshot(new(10,20,30),units:units); var p=Placement(s);
        Assert.Equal(new Point3(10*scale,20*scale,30*scale),p.Block.BaseMm); Assert.Equal(10,p.Block.Facts.X.Value);
        Assert.Equal("B",p.Block.Source.OriginalHandle); Assert.Equal("B",p.Block.Source.CanonicalHandle);
        var pair=Assert.Single(p.Block.Facts.X.PairIndices); Assert.Equal("10",s.LexicalEvidence.Pairs[pair].RawTextValue);
        Assert.Equal(pair*2+1,p.Block.Facts.X.FirstLine); Assert.Equal(scale,p.Block.Units.Decision.SourceToMillimeters);
    }
    [Theory][InlineData("missing")][InlineData("malformed")][InlineData("nonfinite")]
    public async Task InvalidDefinitionBaseNeverFallsBackToOrigin(string kind)
    {
        var block=Block(blockBase:new(10,20,30));
        block=kind=="missing"?block.Replace("30\n30\n",""):
            kind=="malformed"?block.Replace("10\n10\n","10\nnot-a-number\n"):block.Replace("10\n10\n","10\nNaN\n");
        if(kind=="missing")
        {
            var r=Result(await Cad04BScalarFixture.Snapshot(Document(block,Insert()))); Assert.Null(r.Placement); Assert.Equal(CadPlacementStatus.Invalid,r.Status);
            Assert.Contains(r.Diagnostics,d=>d.Code is CadPlacementDiagnosticCode.InvalidBlockDefinition or CadPlacementDiagnosticCode.InvalidBlockBase);
        }
        else
        {
            await AssertSnapshotRejected(Document(block,Insert()));
            var s=await Snapshot(new(10,20,30)); var p=Placement(s);
            var raw=Assert.IsType<RawBlockBegin>(s.Records.Single(x=>x.BlockAnnotation?.Payload is RawBlockBegin).BlockAnnotation!.Payload);
            var fact=kind=="nonfinite"?Cad04CCoordinateFixture.Fact(double.NaN):
                new DxfSourceFact(DxfFactKind.Real,DxfFactPresence.Present,DxfFactValidity.Invalid,null,null,null,[3],new(3,3,7,8));
            var result=CadBlockBaseNormalizer.Normalize(p.Block.Source,raw with {BasePoint=new(fact,raw.BasePoint.Y,raw.BasePoint.Z)},p.Block.Units);
            Assert.Equal(CadPlacementDiagnosticCode.InvalidBlockBase,Assert.Single(result.Diagnostics).Code); Assert.Null(result.BaseMm);
        }
    }
    [Fact] public async Task NonMillimeterBaseAndAnchorDoNotScaleDimensionlessInsertScale()
    {
        var p=Placement(await Snapshot(new(10,20,30),new(100,200,300),new(2,3,1),units:5));
        ExpectedPoint(p,new(110,220,330),new(1020,2060,3030)); Assert.Equal(2,p.Scale.Scale!.X); Assert.Equal(3,p.Scale.Scale.Y);
    }
}
public sealed class CadInsertScaleTests
{
    [Fact] public async Task AbsentAxesDefaultIndividuallyWithNoInventedPresence()
    {
        var p=Placement(await Snapshot()); Assert.Equal(3,p.Scale.DefaultRules.Count);
        Cad04CCoordinateFixture.Vector(p.Scale.Scale,1,1,1);
        Assert.Equal(DxfFactPresence.Absent,p.Scale.Facts.X.Presence); Assert.Empty(p.Scale.Facts.X.PairIndices);
        Assert.Equal("DXF.INSERT.Scale.X.DefaultOne",p.Scale.DefaultRules[0]);
        var s=await Cad04BScalarFixture.Snapshot(Document(Block(),Insert(extra:"41\n2\n")));
        p=Placement(s); Cad04CCoordinateFixture.Vector(p.Scale.Scale,2,1,1); Assert.Equal(2,p.Scale.DefaultRules.Count);
    }
    [Theory][InlineData(2,2,2)][InlineData(2,3,4)][InlineData(-2,3,4)][InlineData(2,-3,4)][InlineData(2,3,-4)]
    [InlineData(1e-300,1,1)][InlineData(double.Epsilon,1,1)]
    public async Task ValidNonzeroScalesRemainDimensionlessIncludingTinyAndNegative(double x,double y,double z)
    {
        var p=Placement(await Snapshot(scale:new(x,y,z))); Cad04CCoordinateFixture.Vector(p.Scale.Scale,x,y,z);
        Assert.Equal(x,p.Transform.Values[0]); Assert.Empty(p.Scale.DefaultRules);
    }
    [Theory][InlineData(0,1,1)][InlineData(1,0,1)][InlineData(1,1,0)]
    [InlineData(double.NaN,1,1)][InlineData(1,double.PositiveInfinity,1)][InlineData(1,1,double.NegativeInfinity)]
    public async Task ZeroOrNonfiniteScaleIsNotRepaired(double x,double y,double z)
    {
        if(!double.IsFinite(x) || !double.IsFinite(y) || !double.IsFinite(z))
        {
            await AssertSnapshotRejected(Document(Block(),Insert(scale:new(x,y,z))));
            var valid=Placement(await Snapshot());
            var r=CadInsertScaleNormalizer.Normalize(valid.Scale.Source,Cad04CCoordinateFixture.Triple(x,y,z));
            Assert.Equal(CadPlacementDiagnosticCode.InvalidInsertScale,Assert.Single(r.Diagnostics).Code); Assert.Null(r.Scale);
        }
        else
        {
            var r=Result(await Snapshot(scale:new(x,y,z))); Assert.Equal(CadPlacementStatus.Invalid,r.Status); Assert.Null(r.Placement);
            Assert.Equal(CadPlacementDiagnosticCode.InvalidInsertScale,Assert.Single(r.Diagnostics).Code);
        }
    }
    [Fact] public async Task LargeFiniteScaleDoesNotRequireRepresentableDeterminantMagnitude()
    {
        var p=Placement(await Snapshot(scale:new(double.MaxValue,double.MaxValue,double.MaxValue)));
        Assert.Equal(1,p.OrientationSign); Assert.All(p.Transform.Values,x=>Assert.True(double.IsFinite(x)));
    }
}
