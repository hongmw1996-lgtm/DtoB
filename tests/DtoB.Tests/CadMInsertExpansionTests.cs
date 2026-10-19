using DtoB.Cad.Normalize.Occurrences;
using DtoB.Cad.Normalize.Placement;
using Xunit;
using static DtoB.Tests.Cad04EOccurrenceFixture;

namespace DtoB.Tests;

public sealed class CadMInsertExpansionTests
{
    [Fact] public async Task CellsAreRowMajorAndRealSourceHandleIsUnchanged()
    {
        var s=await Snapshot(Block("B","B"),Insert("A","B",rows:2,columns:3,rowSpacing:200,columnSpacing:100));
        var e=Complete(Expand(s)); Assert.Equal(6,e.Occurrences.Count);
        Assert.Equal(new[]{0,1,2,3,4,5},e.Occurrences.Select(o=>o.CellOrdinal!.Value));
        Assert.Equal(new int?[]{0,0,0,1,1,1},e.Occurrences.Select(o=>o.Value.Path.Steps[^1].Row));
        Assert.Equal(new int?[]{0,1,2,0,1,2},e.Occurrences.Select(o=>o.Value.Path.Steps[^1].Column));
        Assert.All(e.Occurrences,o=>Assert.Equal("A",o.SourcePlacement.Scale.Source.CanonicalHandle));
        Assert.Equal(6,e.Occurrences.Select(o=>o.Value.Id.Value).Distinct().Count());
        Assert.Equal(CadPlacementStatus.Unsupported,Cad04DPlacementFixture.Result(s).Status);
    }
    [Theory][InlineData(0,100,0)][InlineData(90,0,100)]
    public async Task SpacingRotatesButIsNotMultipliedByGeometryScale(double angle,double x,double y)
    {
        var s=await Snapshot(Block("B","B"),Insert("A","B",scale:new(2,3,1),angle:angle,columns:2,columnSpacing:100));
        var e=Complete(Expand(s)); Point(e.Occurrences[1],new(0,0,0),new(x,y,0));
    }
    [Fact] public async Task FullCellMatrixAndPointAreIndependentlyHandAuthored()
    {
        var s=await Snapshot(Block("B","B"),Insert("A","B",new(1000,2000,0),new(2,3,1),90,rows:2,columns:2,rowSpacing:200,columnSpacing:100));
        var o=Complete(Expand(s)).Occurrences[3]; Point(o,new(1,1,0),new(797,2102,0));
        var expected=new double[]{0,-3,0,800,2,0,0,2100,0,0,1,0,0,0,0,1};
        for(var i=0;i<16;i++) Assert.True(Math.Abs(expected[i]-o.Value.LocalTransform.Values[i])<=Cad04DPlacementFixture.Policy.MatrixCoefficientTolerance);
    }
    [Fact] public async Task NegativeScaleDoesNotReverseGridAndNegativeSpacingIsPreserved()
    {
        var s=await Snapshot(Block("B","B"),Insert("A","B",scale:new(-2,3,1),rows:2,columns:2,rowSpacing:-200,columnSpacing:100));
        var o=Complete(Expand(s)).Occurrences[3]; Point(o,new(1,1,0),new(98,-197,0)); Assert.Equal(-1,o.AccumulatedOrientationSign);
    }
    [Fact] public async Task TiltedBasisAndNonzeroAnchorApplyToRotatedSpacingOnce()
    {
        var s=await Snapshot(Block("B","B"),Insert("A","B",new(1000,2000,0),new(2,3,1),90,new(0,1,0),rows:2,columns:2,rowSpacing:200,columnSpacing:100));
        Point(Complete(Expand(s)).Occurrences[3],new(1,1,0),new(-797,0,2102));
    }
    [Fact] public async Task EachCellDescendsBeforeNextCellAndParentScaleAffectsChildGrid()
    {
        var s=await Snapshot(Block("B","A",Insert("D","B",columns:2,columnSpacing:100))+Block("C","B",Insert("E","C",new(1,0,0)))+Block("F","C"),
            Insert("A","A",new(100,0,0),new(2,1,1),90));
        var e=Complete(Expand(s)); Assert.Equal(new[]{"A","D","E","D","E"},e.Occurrences.Select(o=>o.SourcePlacement.Scale.Source.CanonicalHandle));
        Point(e.Occurrences[4],new(0,0,0),new(100,202,0)); Assert.Equal(3,e.Occurrences[4].Depth);
    }
    [Fact] public async Task ZeroSpacingKeepsCoincidentButDistinctCellsAndDefaultsRemainAbsent()
    {
        var s=await Snapshot(Block("B","B"),Insert("A","B",columns:2)); var e=Complete(Expand(s));
        Assert.Equal(e.Occurrences[0].Value.LocalTransform.Values,e.Occurrences[1].Value.LocalTransform.Values);
        Assert.NotEqual(e.Occurrences[0].Value.Id.Value,e.Occurrences[1].Value.Id.Value);
        Assert.Contains("DXF.INSERT.ColumnSpacing.DefaultZero",e.Occurrences[0].Grid.DefaultRules);
        Assert.Empty(e.Occurrences[0].Grid.ColumnSpacingSource.Source.PairIndices);
    }
    [Fact] public async Task CentimeterSpacingAndNonzeroBaseAreAppliedOnlyOnce()
    {
        var s=await Snapshot(Block("B","B",point:new(10,20,0)),Insert("A","B",new(100,200,0),new(2,3,1),columns:2,columnSpacing:100),5);
        Point(Complete(Expand(s)).Occurrences[1],new(100,200,0),new(2000,2000,0));
    }
}
