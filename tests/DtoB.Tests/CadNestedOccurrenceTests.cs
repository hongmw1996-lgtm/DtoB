using DtoB.Cad.Normalize.Occurrences;
using DtoB.Cad.V2;
using Xunit;
using static DtoB.Tests.Cad04EOccurrenceFixture;

namespace DtoB.Tests;

public sealed class CadNestedOccurrenceTests
{
    [Fact] public async Task RootIsLocalAndHasNoSyntheticSourceParent()
    {
        var r=Expand(await Snapshot(Block("B","B"),Insert("A","B",new(100,200,300)))); var e=Complete(r); var o=Assert.Single(e.Occurrences);
        Assert.Null(o.Value.ParentOccurrenceId); Assert.Equal(1,o.Depth); Assert.Equal(o.Value.LocalTransform.Values,o.Value.AccumulatedTransform.Values);
        Point(o,new(1,2,3),new(101,202,303)); Assert.Equal(1,r.TotalOccurrencesAllocated);
    }
    [Fact] public async Task TwoLevelsUseParentTimesLocalAndDifferFromReversedOrder()
    {
        var e=Complete(Expand(await Nested())); Assert.Equal(2,e.Occurrences.Count);
        var root=e.Occurrences[0]; var child=e.Occurrences[1];
        Point(child,new(1,0,0),new(100,12,0)); Assert.Equal(root.Value.Id.Value,child.Value.ParentOccurrenceId!.Value);
        // Wrong Lchild*Mparent would give (210,1,0), not (100,12,0).
        var wrong=root.Value.AccumulatedTransform.ToTransform3().Then(child.Value.LocalTransform.ToTransform3()).Apply(new(1,0,0));
        Assert.True(Math.Abs(wrong.X-210)<Cad04DPlacementFixture.Policy.LengthMm); Assert.True(Math.Abs(wrong.Y-1)<Cad04DPlacementFixture.Policy.LengthMm);
    }
    [Fact] public async Task ThreeLevelsHaveIndependentNonuniformNegativeRotationOracle()
    {
        var s=await Snapshot(Block("B","A",Insert("D","B",new(10,0,0),new(2,3,1)))
            +Block("C","B",Insert("E","C",new(0,5,0),new(-1,1,1)))+Block("F","C"),Insert("A","A",new(100,0,0),angle:90));
        var e=Complete(Expand(s)); Assert.Equal(3,e.Occurrences.Count);
        // C:(1,2)->(-1,7); B:(-1,7)->(8,21); A:R90+(100,0)->(79,8).
        Point(e.Occurrences[2],new(1,2,0),new(79,8,0)); Assert.Equal(3,e.Occurrences[2].Depth);
    }
    [Fact] public async Task SharedTargetIsNotGloballyVisitedAndDefinitionsAreReferenceShared()
    {
        var s=await Snapshot(Block("B","A",Insert("D","C"))+Block("C","B",Insert("E","C"))+Block("F","C"),
            Insert("A","A")+Insert("AA","B")); var e=Complete(Expand(s)); Assert.Equal(4,e.Occurrences.Count);
        var left=e.Occurrences[1]; var right=e.Occurrences[3]; Assert.Same(left.Definition,right.Definition);
        Assert.NotEqual(left.Value.Id.Value,right.Value.Id.Value); Assert.Equal(left.Value.DefinitionId.Value,right.Value.DefinitionId.Value);
        Assert.Equal(new[]{"A","D","AA","E"},e.Occurrences.Select(o=>o.SourcePlacement.Scale.Source.CanonicalHandle));
    }
}
public sealed class CadOccurrenceOrientationTests
{
    [Theory][InlineData(1,1,1)][InlineData(1,-1,-1)][InlineData(-1,-1,1)][InlineData(-1,1,-1)]
    public async Task AccumulatedSignIsParentTimesLocalWithoutWindingChanges(int parent,int child,int expected)
    {
        var s=await Snapshot(Block("B","A",Insert("D","B",scale:new(1,child,1)))+Block("C","B"),Insert("A","A",scale:new(parent,1,1)));
        var e=Complete(Expand(s)); Assert.Equal(child,e.Occurrences[1].LocalOrientationSign); Assert.Equal(expected,e.Occurrences[1].AccumulatedOrientationSign);
    }
}
public sealed class CadOccurrenceCycleTests
{
    [Fact] public async Task DirectCycleRejectsChildAndKeepsIndependentSibling()
    {
        var s=await Snapshot(Block("B","B",Insert("C","B"))+Block("D","D"),Insert("A","B")+Insert("E","D"));
        var r=Expand(s); Assert.Equal(CadExpansionStatus.Partial,r.Status); Assert.Equal(2,r.Expansion!.Occurrences.Count);
        Assert.Equal(CadOccurrenceDiagnosticCode.Cycle,Assert.Single(r.Diagnostics).Code); Assert.Equal(2,r.Diagnostics[0].Path.Steps.Count);
        Assert.Equal(2,r.TotalOccurrencesAllocated);
    }
    [Fact] public async Task IndirectCycleIsDetectedByActiveDefinitionPathBeforeDepthLimit()
    {
        var s=await Snapshot(Block("B","A",Insert("E","B"))+Block("C","B",Insert("F","C"))+Block("D","C",Insert("AA","A")),Insert("A","A"));
        var r=Expand(s); Assert.Equal(CadExpansionStatus.Partial,r.Status); Assert.Equal(3,r.Expansion!.Occurrences.Count);
        Assert.Equal(CadOccurrenceDiagnosticCode.Cycle,Assert.Single(r.Diagnostics).Code); Assert.Equal(4,r.Diagnostics[0].Path.Steps.Count);
    }
    [Fact] public async Task CycleThroughCellsRetainsDistinctCellPaths()
    {
        var s=await Snapshot(Block("B","B",Insert("C","B",columns:2,columnSpacing:100)),Insert("A","B"));
        var r=Expand(s); Assert.Equal(CadExpansionStatus.Partial,r.Status); Assert.Single(r.Expansion!.Occurrences); Assert.Equal(2,r.Diagnostics.Count);
        Assert.Equal(new int?[]{0,1},r.Diagnostics.Select(d=>d.Path.Steps[^1].Column));
    }
    [Fact] public async Task MissingChildDoesNotDestroyAncestorsOrGoodSibling()
    {
        var s=await Snapshot(Block("B","B",Insert("C","MISSING")+Insert("D","GOOD"))+Block("E","GOOD"),Insert("A","B"));
        var r=Expand(s); Assert.Equal(CadExpansionStatus.Partial,r.Status); Assert.Equal(2,r.Expansion!.Occurrences.Count);
        Assert.Equal(DtoB.Cad.Normalize.Placement.CadPlacementDiagnosticCode.MissingBlockDefinition,Assert.Single(r.Diagnostics).Placement.Single().Code);
    }
}
