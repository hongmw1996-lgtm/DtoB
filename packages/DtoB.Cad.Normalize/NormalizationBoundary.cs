using DtoB.Cad.Raw;
using DtoB.Cad.V2;
using V2Document = DtoB.Cad.V2.CadDocument;

namespace DtoB.Cad.Normalize;

/// <summary>Pure future normalization boundary. 04A deliberately supplies no engine implementation.</summary>
public interface ICadNormalizer
{
    CadNormalizationResult Normalize(RawDrawingSnapshot snapshot, CadNormalizationOptions options,
        CancellationToken cancellationToken = default);
}
public enum CadNormalizationStatus
{ Normalized, PartiallyNormalized, UnsupportedNormalization, InvalidNormalization, Cancelled, ResourceLimitExceeded, NotImplemented }

public sealed class CadNormalizationResult
{
    public CadNormalizationStatus Status { get; }
    public V2Document? Document { get; }
    public IReadOnlyList<CadDiagnostic> Diagnostics { get; }
    public CadNormalizationResult(CadNormalizationStatus status, V2Document? document, IReadOnlyList<CadDiagnostic> diagnostics)
    {
        Status = CadContract.EnumValue(status); Document = document; Diagnostics = CadContract.Copy(diagnostics);
        var published = status is CadNormalizationStatus.Normalized or CadNormalizationStatus.PartiallyNormalized;
        CadContract.Require(!published || document is not null, "Published result requires a document.");
        CadContract.Require(status is not (CadNormalizationStatus.InvalidNormalization or CadNormalizationStatus.Cancelled
            or CadNormalizationStatus.ResourceLimitExceeded or CadNormalizationStatus.NotImplemented) || document is null, "Refusal cannot publish a document.");
        CadContract.Require(document is not null || Diagnostics.Count > 0, "Refusal requires diagnostics.");
        if (published)
            CadContract.Require(document!.Completeness.State == (status == CadNormalizationStatus.Normalized
                ? CadNormalizationState.Normalized : CadNormalizationState.PartiallyNormalized), "Result/document status mismatch.");
        if (status == CadNormalizationStatus.UnsupportedNormalization && document is not null)
            CadContract.Require(document.Completeness.State == CadNormalizationState.UnsupportedNormalization, "Unsupported result/document status mismatch.");
    }
}

/// <summary>Configuration only. No occurrence accounting or resource enforcement algorithm in 04A.</summary>
public sealed class CadNormalizationLimits
{
    public int MaxDepth { get; }
    public int MaxCellsPerInsert { get; }
    public int MaxOccurrences { get; }
    public int MaxDerivedGeometry { get; }
    public int MaxDiagnostics { get; }
    public CadNormalizationLimits(int maxDepth, int maxCellsPerInsert, int maxOccurrences, int maxDerivedGeometry, int maxDiagnostics)
    {
        CadContract.Require(maxDepth > 0 && maxCellsPerInsert > 0 && maxOccurrences > 0 && maxDerivedGeometry > 0
            && maxDiagnostics >= 1, "Positive resource limits required; diagnostics includes terminal slot.");
        MaxDepth = maxDepth; MaxCellsPerInsert = maxCellsPerInsert; MaxOccurrences = maxOccurrences;
        MaxDerivedGeometry = maxDerivedGeometry; MaxDiagnostics = maxDiagnostics;
    }
    public static CadNormalizationLimits Development => new(32, 10_000, 100_000, 250_000, 5_000);
}
public sealed class CadNormalizationOptions(string profileId, CadNormalizationLimits limits)
{
    public string ProfileId { get; } = CadContract.Text(profileId);
    public CadNormalizationLimits Limits { get; } = CadContract.Required(limits);
}
