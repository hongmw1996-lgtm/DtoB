using DtoB.Cad.Raw;
using DtoB.Cad.V2;
using DtoB.Cad.Normalize.Scalars;
using Xunit;
using static DtoB.Tests.Cad04BScalarFixture;

namespace DtoB.Tests;

public sealed class CadUnitNormalizationTests
{
    [Theory][InlineData(1,25.4)][InlineData(2,304.8)][InlineData(4,1)][InlineData(5,10)][InlineData(6,1000)]
    public void DeclaredUnitsHaveIndependentExactDecimalFactors(int code, double factor)
    {
        var r = Units(code); Assert.Equal(CadUnitStatus.Resolved, r.Status);
        Assert.Equal(code, (int)r.Unit!); Assert.Equal(code, r.Decision.SourceUnitCode);
        Assert.Equal(factor, r.Decision.SourceToMillimeters); Assert.Equal(CadUnitDecisionKind.Declared, r.Decision.Kind);
        Assert.Empty(r.Diagnostics);
    }
    [Theory][InlineData(0,CadUnitStatus.NeedsExplicitUnits)][InlineData(3,CadUnitStatus.UnsupportedUnits)]
    [InlineData(7,CadUnitStatus.UnsupportedUnits)][InlineData(24,CadUnitStatus.UnsupportedUnits)]
    [InlineData(-1,CadUnitStatus.InvalidUnits)][InlineData(25,CadUnitStatus.InvalidUnits)]
    public void UnresolvedNeverHasImplicitScale(int code, CadUnitStatus status)
    { var r = Units(code); Assert.Equal(status,r.Status); Assert.Null(r.Decision.SourceToMillimeters); Assert.Single(r.Diagnostics); }
    [Fact] public void AbsentNeedsExplicitUnitsAndSelectionRetainsAbsence()
    {
        var f = Fact(CadScalarRole.HeaderInsertionUnits, Absent(DxfFactKind.Integer));
        Assert.Equal(CadUnitStatus.NeedsExplicitUnits,CadUnitNormalizer.Resolve(f).Status);
        var r = CadUnitNormalizer.Resolve(f,new(CadSourceLengthUnit.Feet,"owner-1","explicit owner selection"));
        Assert.Equal(304.8,r.Decision.SourceToMillimeters); Assert.Null(r.Decision.SourceUnitCode);
        Assert.Equal(CadUnitDecisionKind.Explicit,r.Decision.Kind); Assert.Equal("owner-1",r.Decision.DecisionId);
        Assert.Empty(r.Source.PairIndices);
    }
    [Fact] public void ZeroSelectionPreservesZeroAndContradictoryOverrideRefuses()
    {
        var selection = new CadExplicitUnitSelection(CadSourceLengthUnit.Meters,"u1","owner");
        var r = CadUnitNormalizer.Resolve(Fact(CadScalarRole.HeaderInsertionUnits,Integer(0)),selection);
        Assert.Equal(0,r.Decision.SourceUnitCode); Assert.Equal(1000,r.Decision.SourceToMillimeters);
        r = CadUnitNormalizer.Resolve(Fact(CadScalarRole.HeaderInsertionUnits,Integer(4)),selection);
        Assert.Equal(CadUnitStatus.InvalidUnits,r.Status); Assert.Equal(CadScalarDiagnosticCode.ContradictoryUnitOverride,Assert.Single(r.Diagnostics).Code);
        Assert.Equal(CadUnitStatus.UnsupportedUnits,CadUnitNormalizer.Resolve(Fact(CadScalarRole.HeaderInsertionUnits,Integer(3)),selection).Status);
    }
    [Theory][InlineData("duplicate")][InlineData("invalid")][InlineData("wrong-kind")][InlineData("inconsistent-absent")][InlineData("missing-span")]
    public void MalformedSourceNeverGetsRepaired(string kind)
    {
        var f = kind switch
        {
            "duplicate" => new DxfSourceFact(DxfFactKind.Integer,DxfFactPresence.Present,DxfFactValidity.Invalid,null,null,null,[3,4],new(3,4,7,10)),
            "invalid" => new(DxfFactKind.Integer,DxfFactPresence.Present,DxfFactValidity.Invalid,null,null,null,[3],new(3,3,7,8)),
            "wrong-kind" => Real(4),
            "inconsistent-absent" => new(DxfFactKind.Integer,DxfFactPresence.Absent,DxfFactValidity.NotApplicable,null,4,null,[],null),
            _ => new(DxfFactKind.Integer,DxfFactPresence.Present,DxfFactValidity.Valid,null,4,null,[3],null)
        };
        Assert.Equal(CadUnitStatus.InvalidUnits,CadUnitNormalizer.Resolve(Fact(CadScalarRole.HeaderInsertionUnits,f)).Status);
    }
    [Theory][InlineData(4,CadBlockUnitStatus.MatchesDocumentUnit)][InlineData(0,CadBlockUnitStatus.UsesDocumentUnit)]
    [InlineData(1,CadBlockUnitStatus.UnsupportedUnits)][InlineData(3,CadBlockUnitStatus.UnsupportedUnits)][InlineData(-1,CadBlockUnitStatus.InvalidUnits)]
    public async Task ActualBlockRecordCode70MeansUnitsNotBlockFlags(int value,CadBlockUnitStatus expected)
    {
        var table = $"0\nSECTION\n2\nTABLES\n0\nTABLE\n2\nBLOCK_RECORD\n70\n1\n0\nBLOCK_RECORD\n5\nA\n330\n0\n2\nB\n70\n{value}\n0\nENDTAB\n0\nENDSEC\n";
        var s = await Snapshot(Dxf("",table)); var row = Assert.Single(s.Records);
        var r = CadUnitNormalizer.CheckBlock(RawScalarAccess.Record(s,row,CadScalarRole.BlockInsertionUnits),CadUnitNormalizer.Resolve(RawScalarAccess.HeaderUnits(s)));
        Assert.Equal(expected,r.Status); Assert.Equal(row.StructuralRecordIndex,r.Source.Record!.RecordIndex);
    }
    [Fact] public void AbsentBlockUsesDocumentUnitsWithoutLocalScaling() => Assert.Equal(CadBlockUnitStatus.UsesDocumentUnit,
        CadUnitNormalizer.CheckBlock(Fact(CadScalarRole.BlockInsertionUnits,Absent(DxfFactKind.Integer)),Units()).Status);
    [Fact] public void ForeignDocumentCannotSupplyUnitAuthority()
    {
        var foreign = new CadSourceDocument(Document.DrawingId,new("33333333-3333-3333-3333-333333333333"),Document.FileHash);
        Assert.Equal(CadBlockUnitStatus.InvalidUnits,CadUnitNormalizer.CheckBlock(CadScalarFact.FromRaw(foreign,CadScalarRole.BlockInsertionUnits,Integer(4)),Units()).Status);
        Assert.Equal(CadScalarStatus.Invalid,CadScalarLengthNormalizer.Normalize(CadScalarFact.FromRaw(foreign,CadScalarRole.CircleRadius,Real(1)),Units()).Status);
    }
    [Fact] public async Task OfficialControlledHeaderIsMillimetersWithoutSpatialConversion()
    {
        var s = await Controlled(); var r = CadUnitNormalizer.Resolve(RawScalarAccess.HeaderUnits(s));
        Assert.Equal(4,s.Header!.InsertionUnits.Integer); Assert.Equal(CadSourceLengthUnit.Millimeters,r.Unit);
        Assert.Equal(1,r.Decision.SourceToMillimeters); Assert.Equal(s.Header.InsertionUnits.SourcePairIndices,r.Source.PairIndices);
    }
}
