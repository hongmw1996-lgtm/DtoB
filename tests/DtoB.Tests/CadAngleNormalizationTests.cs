using DtoB.Cad.Normalize.Scalars;
using DtoB.Cad.Raw;
using Xunit;
using static DtoB.Tests.Cad04BScalarFixture;

namespace DtoB.Tests;

public sealed class CadAngleNormalizationTests
{
    [Theory][InlineData(0,0)][InlineData(90,Math.PI/2)][InlineData(180,Math.PI)]
    [InlineData(270,3*Math.PI/2)][InlineData(360,0)][InlineData(-90,3*Math.PI/2)]
    [InlineData(720000090,Math.PI/2)][InlineData(-720000090,3*Math.PI/2)]
    public void DegreeOracleIsHandAuthored(double degrees,double expected) => Assert.Equal(expected,CadAngleNormalizer.Normalize(Angle(degrees)).Value);
    [Theory][InlineData(-0.0)][InlineData(Math.Tau)][InlineData(-Math.Tau)][InlineData(1000000*Math.Tau)][InlineData(-1000000*Math.Tau)]
    public void WrappedFiniteValuesAlwaysStayInsideHalfOpenRange(double value)
    { var r = CadAngleNormalizer.WrapPositiveRadians(value); Assert.True(r >= 0 && r < Math.Tau); if (value == 0 || value == Math.Tau || value == -Math.Tau) Assert.Equal(0,BitConverter.DoubleToInt64Bits(r)); }
    [Fact] public void AdjacentBoundaryValuesAreNotEpsilonSnapped()
    {
        Assert.Equal(Math.BitDecrement(Math.Tau),CadAngleNormalizer.WrapPositiveRadians(Math.BitDecrement(Math.Tau)));
        Assert.Equal(Math.BitIncrement(Math.Tau)-Math.Tau,CadAngleNormalizer.WrapPositiveRadians(Math.BitIncrement(Math.Tau)));
        Assert.Equal(Math.BitDecrement(Math.Tau),CadAngleNormalizer.WrapPositiveRadians(-double.Epsilon));
        Assert.Equal(double.Epsilon,CadAngleNormalizer.WrapPositiveRadians(double.Epsilon));
    }
    [Theory][InlineData(double.NaN)][InlineData(double.PositiveInfinity)][InlineData(double.NegativeInfinity)]
    public void NonfiniteAnglesAreNotClamped(double value)
    { var r = CadAngleNormalizer.Normalize(Angle(value)); Assert.Equal(CadScalarStatus.Invalid,r.Status); Assert.Equal(CadScalarDiagnosticCode.NonFiniteSourceValue,Assert.Single(r.Diagnostics).Code); }
    [Fact] public void ExtremeFiniteDegreesDoNotOverflowAndSignedStyleStaysSigned()
    {
        var a = CadAngleNormalizer.Normalize(Angle(double.MaxValue)); Assert.Equal(CadScalarStatus.Normalized,a.Status); Assert.True(a.Value >= 0 && a.Value < Math.Tau);
        Assert.Equal(-Math.PI/2,CadAngleNormalizer.Normalize(Angle(-90,CadScalarRole.StyleObliqueAngle)).Value);
        Assert.Equal(4*Math.PI,CadAngleNormalizer.Normalize(Angle(720,CadScalarRole.StyleObliqueAngle)).Value);
    }
    [Theory][InlineData(CadScalarRole.InsertRotation)][InlineData(CadScalarRole.TextRotation)]
    public void OnlyApprovedAbsentRotationsHaveTraceableZeroDefault(CadScalarRole role)
    { var r = CadAngleNormalizer.Normalize(Fact(role,Absent(DxfFactKind.Real))); Assert.Equal(0,r.Value); Assert.NotNull(r.DefaultRule); Assert.Empty(r.Source.PairIndices); }
    [Theory][InlineData(CadScalarRole.MTextRotation)][InlineData(CadScalarRole.PointDisplayAngle)]
    [InlineData(CadScalarRole.LinearDimensionRotation)][InlineData(CadScalarRole.StyleObliqueAngle)]
    public void OtherAbsentRolesRemainAbsent(CadScalarRole role) => Assert.Equal(CadScalarStatus.Absent,CadAngleNormalizer.Normalize(Fact(role,Absent(DxfFactKind.Real))).Status);
    [Fact] public void WrongKindCannotTriggerDefault() => Assert.Equal(CadScalarStatus.Invalid,
        CadAngleNormalizer.Normalize(Fact(CadScalarRole.TextRotation,Absent(DxfFactKind.Integer))).Status);
}

public sealed class CadMTextAngleTests
{
    [Fact] public async Task ActualRetainedCode50IsAlreadyRadiansNotDegrees()
    {
        var s = await Snapshot(MText("50\n1.5707963267948966\n"));
        var f = RawScalarAccess.Record(s,Assert.Single(s.Records),CadScalarRole.MTextRotation);
        var r = CadAngleNormalizer.Normalize(f); Assert.Equal(Math.PI/2,r.Value);
        Assert.NotEqual(Math.PI*Math.PI/360,r.Value); Assert.Single(r.Source.PairIndices);
        Assert.Equal("1.5707963267948966",s.LexicalEvidence.Pairs[r.Source.PairIndices[0]].RawTextValue);
    }
    [Theory][InlineData("11\n1\n21\n0\n31\n0\n50\n1.5\n")]
    [InlineData("75\n1\n50\n1.5\n")][InlineData("50\n1\n50\n2\n")]
    [InlineData("100\nForeignSubclass\n50\n1.5\n")]
    public async Task AmbiguousDirectionColumnOrForeignRolesAreExplicitlyUnsupported(string fields)
    {
        var s = await Snapshot(MText(fields)); var r = CadAngleNormalizer.Normalize(RawScalarAccess.Record(s,Assert.Single(s.Records),CadScalarRole.MTextRotation));
        Assert.Equal(CadScalarStatus.Unsupported,r.Status); Assert.Equal(CadScalarDiagnosticCode.UnsupportedAngleRole,Assert.Single(r.Diagnostics).Code); Assert.Null(r.Value);
    }
    [Fact] public async Task MissingAndMalformedMTextAreDistinct()
    {
        foreach (var value in new[] {"", "50\nnot-a-number\n"})
        {
            var s = await Snapshot(MText(value));
            Assert.Equal(value.Length == 0 ? CadScalarStatus.Absent : CadScalarStatus.Invalid,
                CadAngleNormalizer.Normalize(RawScalarAccess.Record(s,Assert.Single(s.Records),CadScalarRole.MTextRotation)).Status);
        }
    }
    [Fact] public async Task ControlGroupCode50DoesNotBecomeRotation()
    {
        var s = await Snapshot(MText("102\n{PRIVATE\n50\n90\n102\n}\n50\n1.5707963267948966\n"));
        Assert.Equal(Math.PI/2,CadAngleNormalizer.Normalize(RawScalarAccess.Record(s,Assert.Single(s.Records),CadScalarRole.MTextRotation)).Value);
    }
}

public sealed class CadAngleRoleTests
{
    [Theory][InlineData(CadScalarRole.ArcStartAngle)][InlineData(CadScalarRole.ArcEndAngle)]
    [InlineData(CadScalarRole.InsertRotation)][InlineData(CadScalarRole.TextRotation)]
    [InlineData(CadScalarRole.LinearDimensionRotation)][InlineData(CadScalarRole.PointDisplayAngle)][InlineData(CadScalarRole.StyleObliqueAngle)]
    public void ReviewedDegreeRolesUseOneConversion(CadScalarRole role) => Assert.Equal(Math.PI/2,CadAngleNormalizer.Normalize(Angle(90,role)).Value);
    [Fact] public void IdenticalCode50NumericValueHasRoleSpecificMeaning()
    {
        Assert.Equal(Math.PI/2,CadAngleNormalizer.Normalize(Angle(90,CadScalarRole.TextRotation)).Value);
        Assert.Equal(90 % Math.Tau,CadAngleNormalizer.Normalize(Angle(90,CadScalarRole.MTextRotation)).Value);
        Assert.Equal(CadScalarStatus.Unsupported,CadAngleNormalizer.Normalize(Fact(CadScalarRole.TextHeight,Real(90))).Status);
    }
    [Theory][InlineData("TEXT")][InlineData("ATTDEF")]
    public async Task ActualTextPayloadDegreesAreNormalizedWithSourceRole(string type)
    {
        var s = await Snapshot(Dxf($"0\n{type}\n5\nA\n330\n0\n10\n0\n20\n0\n40\n2\n1\ntext\n2\ntag\n70\n0\n50\n90\n"));
        var row = Assert.Single(s.Records);
        Assert.Equal(Math.PI/2,CadAngleNormalizer.Normalize(RawScalarAccess.Record(s,row,CadScalarRole.TextRotation)).Value);
        Assert.Equal(2,CadScalarLengthNormalizer.Normalize(RawScalarAccess.Record(s,row,CadScalarRole.TextHeight),CadUnitNormalizer.Resolve(RawScalarAccess.HeaderUnits(s))).Value);
    }
    [Theory][InlineData(0,CadScalarStatus.Normalized)][InlineData(1,CadScalarStatus.Unsupported)][InlineData(2,CadScalarStatus.Unsupported)]
    public async Task DimensionProfileExplicitlyDefersAmbiguousRoles(int subtype,CadScalarStatus expected)
    {
        var s = await Snapshot(Dxf($"0\nDIMENSION\n5\nA\n330\n0\n70\n{subtype}\n10\n0\n20\n0\n13\n0\n23\n0\n14\n1\n24\n1\n50\n90\n42\n5\n"));
        var row = Assert.Single(s.Records);
        var r = CadAngleNormalizer.Normalize(RawScalarAccess.Record(s,row,CadScalarRole.LinearDimensionRotation));
        Assert.Equal(expected,r.Status); if (expected == CadScalarStatus.Normalized) Assert.Equal(Math.PI/2,r.Value);
    }
    [Fact] public async Task PointDisplayAndStyleObliqueStayMetadataScalars()
    {
        var s = await Snapshot(Dxf("0\nPOINT\n5\nA\n330\n0\n10\n123\n20\n456\n50\n90\n",
            "0\nSECTION\n2\nTABLES\n0\nTABLE\n2\nSTYLE\n70\n1\n0\nSTYLE\n5\nB\n330\n0\n2\nSimple\n70\n0\n50\n-90\n0\nENDTAB\n0\nENDSEC\n"));
        var point = s.Records.Single(r => r.SourceRecord.RecordType == "POINT");
        Assert.Equal(Math.PI/2,CadAngleNormalizer.Normalize(RawScalarAccess.Record(s,point,CadScalarRole.PointDisplayAngle)).Value);
        Assert.Equal(123,Assert.IsType<RawPoint>(point.Geometry!.Payload).Position.X.Real);
        Assert.Equal(-Math.PI/2,CadAngleNormalizer.Normalize(RawScalarAccess.Record(s,s.Records.Single(r => r.SourceRecord.RecordType == "STYLE"),CadScalarRole.StyleObliqueAngle)).Value);
    }
    [Fact] public async Task ActualControlledTypedAngleRolesAndUnknownRoleAreMappedExplicitly()
    {
        var s = await Controlled();
        var arc = s.Records.Single(r => r.SourceRecord.Source.StartPairIndex == 2868);
        Assert.Equal(Math.PI/2,CadAngleNormalizer.Normalize(RawScalarAccess.Record(s,arc,CadScalarRole.ArcEndAngle)).Value);
        var insert = s.Records.Single(r => r.SourceRecord.Source.StartPairIndex == 2971);
        Assert.Equal(Math.PI,CadAngleNormalizer.Normalize(RawScalarAccess.Record(s,insert,CadScalarRole.InsertRotation)).Value);
        Assert.Equal(CadScalarStatus.Unsupported,CadAngleNormalizer.Normalize(RawScalarAccess.Record(s,insert,CadScalarRole.MTextRotation)).Status);
        var point = s.Records.First(r => r.Geometry?.Payload is RawLine);
        Assert.Equal(CadScalarStatus.Unsupported,CadAngleNormalizer.Normalize(RawScalarAccess.Record(s,point,CadScalarRole.PointDisplayAngle)).Status);
    }
}

public sealed class CadArcScalarSemanticsTests
{
    [Theory][InlineData(0,90,0,Math.PI/2)][InlineData(270,90,3*Math.PI/2,Math.PI)]
    [InlineData(-90,90,3*Math.PI/2,Math.PI)][InlineData(720,810,0,Math.PI/2)]
    public void StartAndPositiveCcwSweepAreIndependentScalarOracles(double start,double end,double expectedStart,double expectedSweep)
    {
        var r = CadAngleNormalizer.NormalizeArc(Angle(start,CadScalarRole.ArcStartAngle),Angle(end,CadScalarRole.ArcEndAngle));
        Assert.Equal(CadScalarStatus.Normalized,r.Status); Assert.Equal(expectedStart,r.StartRadians); Assert.Equal(expectedSweep,r.SweepRadians); Assert.Equal(2,r.Sources.Count);
    }
    [Theory][InlineData(0,0)][InlineData(0,360)][InlineData(90,-270)]
    public void EquivalentEndpointsNeverInventCircle(double start,double end)
    { var r = CadAngleNormalizer.NormalizeArc(Angle(start,CadScalarRole.ArcStartAngle),Angle(end,CadScalarRole.ArcEndAngle)); Assert.Equal(CadScalarStatus.Unsupported,r.Status); Assert.Equal(CadScalarDiagnosticCode.AmbiguousArcSweep,Assert.Single(r.Diagnostics).Code); }
    [Fact] public void MissingNonfiniteAndMismatchedSourceRefuse()
    {
        var start = Angle(0,CadScalarRole.ArcStartAngle);
        Assert.Equal(CadScalarStatus.Invalid,CadAngleNormalizer.NormalizeArc(start,Angle(double.NaN,CadScalarRole.ArcEndAngle)).Status);
        Assert.Equal(CadScalarStatus.Invalid,CadAngleNormalizer.NormalizeArc(start,Fact(CadScalarRole.ArcEndAngle,Absent(DxfFactKind.Real))).Status);
        Assert.Equal(CadScalarDiagnosticCode.MismatchedArcSources,Assert.Single(CadAngleNormalizer.NormalizeArc(start,Angle(90)).Diagnostics).Code);
    }
}
