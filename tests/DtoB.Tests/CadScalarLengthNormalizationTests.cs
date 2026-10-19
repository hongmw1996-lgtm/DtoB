using System.Globalization;
using DtoB.Cad.Normalize.Scalars;
using DtoB.Cad.Raw;
using Xunit;
using static DtoB.Tests.Cad04BScalarFixture;

namespace DtoB.Tests;

public sealed class CadScalarLengthNormalizationTests
{
    [Theory][InlineData(2,1,50.8)][InlineData(-2,2,-609.6)][InlineData(0,6,0)]
    [InlineData(1e100,4,1e100)][InlineData(0.0001,5,0.001)]
    public void SignedLengthsAreConvertedOnceWithoutRounding(double source,int units,double expected)
    { var r = CadScalarLengthNormalizer.Normalize(Fact(CadScalarRole.InsertRowSpacing,Real(source)),Units(units)); Assert.Equal(CadScalarStatus.Normalized,r.Status); Assert.Equal(expected,r.Value); }
    [Theory][InlineData(double.NaN)][InlineData(double.PositiveInfinity)][InlineData(double.NegativeInfinity)]
    public void NonfiniteSourceIsExplicitFailure(double value)
    { var r = CadScalarLengthNormalizer.Normalize(Fact(CadScalarRole.PolylineElevation,Real(value)),Units()); Assert.Equal(CadScalarDiagnosticCode.NonFiniteSourceValue,Assert.Single(r.Diagnostics).Code); Assert.Null(r.Value); }
    [Fact] public void FiniteOverflowIsExplicitAndMaximumMmValueIsPreserved()
    {
        var fact = Fact(CadScalarRole.CircleRadius,Real(double.MaxValue));
        Assert.Equal(double.MaxValue,CadScalarLengthNormalizer.Normalize(fact,Units()).Value);
        Assert.Equal(CadScalarDiagnosticCode.NonFiniteNormalizedValue,Assert.Single(CadScalarLengthNormalizer.Normalize(fact,Units(6)).Diagnostics).Code);
    }
    [Theory][InlineData(CadScalarRole.CircleRadius,0)][InlineData(CadScalarRole.ArcRadius,-1)]
    [InlineData(CadScalarRole.TextHeight,0)][InlineData(CadScalarRole.MTextWidth,-1)]
    public void FieldSpecificSignRulesRejectIllegalLengths(CadScalarRole role,double value) =>
        Assert.Equal(CadScalarDiagnosticCode.InvalidLengthSign,Assert.Single(CadScalarLengthNormalizer.Normalize(Fact(role,Real(value)),Units()).Diagnostics).Code);
    [Fact] public void NoGlobalConversionOfOtherNumbersOrMissingUnitGuessing()
    {
        Assert.Equal(CadScalarStatus.Unsupported,CadScalarLengthNormalizer.Normalize(Angle(90),Units()).Status);
        Assert.Equal(CadScalarStatus.Unsupported,CadScalarLengthNormalizer.Normalize(Fact(CadScalarRole.CircleRadius,Real(2)),Units(0)).Status);
        Assert.Equal(CadScalarStatus.Absent,CadScalarLengthNormalizer.Normalize(Fact(CadScalarRole.CircleRadius,Absent(DxfFactKind.Real)),Units()).Status);
    }
    [Theory][InlineData("")][InlineData("fr-FR")][InlineData("ko-KR")]
    public void UnavailableUnitsPreserveRefusalReason(string culture)
    {
        // Culture does not change structured status/diagnostic identity.
        var old = CultureInfo.CurrentCulture; try
        {
            CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo(culture);
            var length = Fact(CadScalarRole.CircleRadius,Real(1));
            Assert.Equal(CadScalarDiagnosticCode.UnsupportedUnits,Assert.Single(CadScalarLengthNormalizer.Normalize(length,Units(3)).Diagnostics).Code);
            Assert.Equal(CadScalarDiagnosticCode.InvalidUnits,Assert.Single(CadScalarLengthNormalizer.Normalize(length,Units(-1)).Diagnostics).Code);
            Assert.Equal(CadBlockUnitStatus.InvalidUnits,CadUnitNormalizer.CheckBlock(Fact(CadScalarRole.BlockInsertionUnits,Integer(4)),Units(-1)).Status);
        }
        finally { CultureInfo.CurrentCulture=old; }
    }
    [Theory][InlineData("")][InlineData("fr-FR")][InlineData("ko-KR")]
    public void BinaryScalarResultDoesNotUseCurrentCulture(string culture)
    {
        var old = CultureInfo.CurrentCulture; try { CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            Assert.Equal(31.75,CadScalarLengthNormalizer.Normalize(Fact(CadScalarRole.CircleRadius,Real(1.25)),Units(1)).Value); }
        finally { CultureInfo.CurrentCulture = old; }
    }
}
