using DtoB.Bim;
using DtoB.Cad;
using DtoB.Core;

namespace DtoB.Analysis;

public sealed record AnalysisResult(BimDocument? Model, Diagnostic[] Diagnostics);
public interface IAnalysisEngine
{
    Task<AnalysisResult> AnalyzeAsync(CadDocument document, CancellationToken cancellationToken = default);
}
// Deliberately explicit: the foundation provides contracts, not object recognition.
public sealed class FoundationAnalysisEngine : IAnalysisEngine
{
    public Task<AnalysisResult> AnalyzeAsync(CadDocument document, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); document.Validate();
        return Task.FromResult(new AnalysisResult(null, [new("ANALYSIS_NOT_IMPLEMENTED", DiagnosticSeverity.Warning,
            "Semantic recognition belongs to subsequent phases; no BIM model was produced.")]));
    }
}
