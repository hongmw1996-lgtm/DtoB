using System.Reflection;
using DtoB.Cad.Normalize;
using DtoB.Cad.Raw;
using DtoB.Cad.V2;
using Xunit;

namespace DtoB.Tests;

public sealed class CadNormalizationBoundaryTests
{
    [Fact] public void BoundaryAcceptsRawAndTokenButDeliberatelyProvidesNoFakeEngine()
    {
        var method = typeof(ICadNormalizer).GetMethod("Normalize")!; var p = method.GetParameters();
        Assert.Equal(typeof(RawDrawingSnapshot), p[0].ParameterType); Assert.Equal(typeof(CadNormalizationOptions), p[1].ParameterType);
        Assert.Equal(typeof(CancellationToken), p[2].ParameterType); Assert.True(p[2].HasDefaultValue);
        Assert.Equal(typeof(CadNormalizationResult), method.ReturnType);
        Assert.Equal(typeof(DtoB.Cad.Normalize.Assembly.CadNormalizer), Assert.Single(typeof(ICadNormalizer).Assembly.GetTypes(), t => t.IsClass && typeof(ICadNormalizer).IsAssignableFrom(t)));
        Assert.Equal(typeof(DtoB.Cad.V2.CadDocument), typeof(CadNormalizationResult).GetProperty("Document")!.PropertyType);
    }
    [Theory]
    [InlineData(CadNormalizationStatus.UnsupportedNormalization)] [InlineData(CadNormalizationStatus.InvalidNormalization)]
    [InlineData(CadNormalizationStatus.Cancelled)] [InlineData(CadNormalizationStatus.ResourceLimitExceeded)]
    [InlineData(CadNormalizationStatus.NotImplemented)]
    public void StructuredWholeDocumentRefusalKeepsDiagnostics(CadNormalizationStatus state)
    {
        var d = CadV2ContractFixture.Diagnostic("REFUSED"); var source = new List<CadDiagnostic> { d };
        var result = new CadNormalizationResult(state, null, source); source.Clear();
        Assert.Equal(state, result.Status); Assert.Null(result.Document); Assert.Single(result.Diagnostics);
        Assert.Throws<NotSupportedException>(() => ((IList<CadDiagnostic>)result.Diagnostics).Clear());
    }
    [Fact] public void ResultCannotPublishFakeOrContradictorySuccess()
    {
        var doc = CadV2ContractFixture.Document(); var d = new[] { CadV2ContractFixture.Diagnostic() };
        Assert.Throws<InvalidDataException>(() => new CadNormalizationResult(CadNormalizationStatus.Normalized, null, d));
        Assert.Throws<InvalidDataException>(() => new CadNormalizationResult(CadNormalizationStatus.Normalized, doc, d));
        Assert.Throws<InvalidDataException>(() => new CadNormalizationResult(CadNormalizationStatus.NotImplemented, doc, d));
        Assert.Throws<InvalidDataException>(() => new CadNormalizationResult(CadNormalizationStatus.ResourceLimitExceeded, doc, d));
        Assert.Throws<InvalidDataException>(() => new CadNormalizationResult(CadNormalizationStatus.UnsupportedNormalization, doc, d));
        Assert.Throws<InvalidDataException>(() => new CadNormalizationResult(CadNormalizationStatus.InvalidNormalization, null, []));
        Assert.Same(doc, new CadNormalizationResult(CadNormalizationStatus.PartiallyNormalized, doc, d).Document);
        Assert.Throws<InvalidDataException>(() => new CadCompleteness(CadNormalizationState.InvalidNormalization, 0, 0));
        Assert.Throws<InvalidDataException>(() => new CadCompleteness(CadNormalizationState.NotApplicable, 0, 0));
    }
    [Fact] public void OptionsContainOnlyImmutableValidatedConfigurationWithoutResourceCounters()
    {
        var limits = CadNormalizationLimits.Development; var options = new CadNormalizationOptions("contract", limits);
        Assert.Same(limits, options.Limits); Assert.Equal(32, limits.MaxDepth); Assert.Equal(10_000, limits.MaxCellsPerInsert);
        Assert.Equal(100_000, limits.MaxOccurrences); Assert.Equal(250_000, limits.MaxDerivedGeometry); Assert.Equal(5_000, limits.MaxDiagnostics);
        Assert.All(typeof(CadNormalizationLimits).GetProperties(), p => Assert.Null(p.SetMethod));
        Assert.DoesNotContain(typeof(CadNormalizationLimits).GetProperties(), p => p.Name == "TotalOccurrencesAllocated");
        Assert.Throws<InvalidDataException>(() => new CadNormalizationLimits(0, 1, 1, 1, 1));
        Assert.Throws<InvalidDataException>(() => new CadNormalizationLimits(1, 0, 1, 1, 1));
        Assert.Throws<InvalidDataException>(() => new CadNormalizationLimits(1, 1, 0, 1, 1));
        Assert.Throws<InvalidDataException>(() => new CadNormalizationLimits(1, 1, 1, 0, 1));
        Assert.Throws<InvalidDataException>(() => new CadNormalizationLimits(1, 1, 1, 1, 0));
    }
}
