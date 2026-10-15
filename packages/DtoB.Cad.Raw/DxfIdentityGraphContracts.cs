using System.Text.Json.Serialization;

namespace DtoB.Cad.Raw;

public enum DxfGraphStatus { Succeeded, Partial, InvalidInput, UnsupportedInput, UnsupportedVersion, ResourceLimitExceeded, Canceled, IoFailure }
public enum DxfGraphRecordKind { StructuralRecord, TableHeader }
/// <summary>Result-local QA locator, never a released object ID.</summary>
public sealed record DxfGraphRecordLocator(DxfGraphRecordKind Kind, int Index);
public enum DxfIdentityValidity { Valid, MissingRequired, Malformed, MultipleFields, Ambiguous }
public enum DxfReferenceResolution { Resolved, Unresolved, Ambiguous, Malformed, Null, NotApplicable }
public enum DxfPointerKind { SoftPointer, HardPointer, SoftOwner, HardOwner, XDataHandle, Other }
public enum DxfGraphDiagnosticCode
{
    InputRejected, MissingRequiredHandle, MalformedHandle, MultipleHandleFields, DuplicateHandle,
    UnresolvedOwner, UnresolvedReactor, UnresolvedExtensionDictionary, UnresolvedXData, UnresolvedPointer,
    MalformedReference, AmbiguousReference, MissingRequiredOwner, OwnershipSourceUnavailable, UnvalidatedIdentityScope, MultipleOwners, SelfOwner, OwnerCycle,
    ResourceLimitExceeded, DiagnosticLimitExceeded, Canceled
}
public sealed class DxfDocumentProvenance
{
    public string DrawingId { get; }
    public string RevisionId { get; }
    public string FileHash { get; }
    public DxfDocumentProvenance(string drawingId, string revisionId, string fileHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(drawingId); ArgumentException.ThrowIfNullOrWhiteSpace(revisionId);
        ArgumentNullException.ThrowIfNull(fileHash);
        if (fileHash.Length != 64 || !fileHash.All(c => c is >= '0' and <= '9' or >= 'A' and <= 'F' or >= 'a' and <= 'f'))
            throw new ArgumentException("Caller provenance requires a SHA256 hexadecimal value.", nameof(fileHash));
        DrawingId = drawingId; RevisionId = revisionId; FileHash = fileHash;
    }
}
/// <summary>Validity is explicit: ambiguous/malformed identities are never released as valid.</summary>
public sealed record DxfFullSourceIdentity(DxfDocumentProvenance Document, string? OriginalHandle, DxfIdentityValidity Validity);
public sealed record DxfIdentityNode(DxfGraphRecordLocator Locator, DxfStructuralRecord SourceRecord,
    string SourceSection, string? TableScope, int? BlockDefinitionIndex, DxfSourceFact OriginalHandle,
    string? CanonicalHandle, DxfIdentityValidity Validity, DxfFullSourceIdentity? FullIdentity);
public sealed record DxfReferenceEdge(DxfGraphRecordLocator SourceLocator, string? SourceHandle,
    int GroupCode, string OriginalTargetHandle, string? CanonicalTargetHandle, DxfSourceSpan Source,
    DxfRawReference ContextEvidence, DxfPointerKind PointerKind, DxfReferenceResolution Resolution,
    DxfGraphRecordLocator? TargetLocator);
/// <summary>Only uniquely identified endpoints of resolved Owner-role references form these edges.</summary>
public sealed record DxfOwnershipEdge(DxfGraphRecordLocator Child, DxfGraphRecordLocator Owner, int ReferenceEdgeIndex);
public sealed class DxfHandleConflict
{
    public string CanonicalHandle { get; }
    public IReadOnlyList<DxfGraphRecordLocator> Records { get; }
    public DxfHandleConflict(string canonical, IEnumerable<DxfGraphRecordLocator> records)
    { CanonicalHandle = canonical; Records = Array.AsReadOnly(records.ToArray()); }
}
public sealed class DxfOwnerCycle
{
    public IReadOnlyList<DxfGraphRecordLocator> Records { get; }
    public DxfOwnerCycle(IEnumerable<DxfGraphRecordLocator> records) => Records = Array.AsReadOnly(records.ToArray());
}
public sealed record DxfGraphDiagnostic(DxfGraphDiagnosticCode Code, DxfStructuralDiagnosticSeverity Severity,
    string Message, DxfSourceSpan? Source = null, DxfGraphRecordLocator? Record = null);

public sealed class DxfIdentityGraphReadResult
{
    public DxfBlockAnnotationReadResult AnnotationEvidence { get; }
    [JsonIgnore] public DxfStructuralReadResult StructuralEvidence => AnnotationEvidence.StructuralEvidence;
    [JsonIgnore] public DxfLexicalReadResult LexicalEvidence => AnnotationEvidence.LexicalEvidence;
    public DxfDocumentProvenance? DocumentProvenance { get; }
    public DxfGraphStatus Status { get; }
    public IReadOnlyList<DxfIdentityNode> Nodes { get; }
    public IReadOnlyList<DxfGraphRecordLocator> NonIdentityRecords { get; }
    public IReadOnlyList<DxfReferenceEdge> References { get; }
    public IReadOnlyList<DxfOwnershipEdge> Ownership { get; }
    public IReadOnlyList<DxfHandleConflict> DuplicateGroups { get; }
    public IReadOnlyList<DxfOwnerCycle> OwnerCycles { get; }
    public IReadOnlyList<DxfGraphDiagnostic> Diagnostics { get; }
    public IReadOnlyList<int> UnresolvedReferenceIndices { get; }
    public IReadOnlyList<int> AmbiguousReferenceIndices { get; }
    public IReadOnlyList<DxfGraphRecordLocator> OwnerRoots { get; }
    public bool GraphValidated { get; }
    public bool IdentityCoverageComplete { get; }
    public bool ReferenceResolutionComplete { get; }
    public DxfIdentityGraphReadResult(DxfBlockAnnotationReadResult evidence, DxfDocumentProvenance? document,
        DxfGraphStatus status, IEnumerable<DxfIdentityNode> nodes, IEnumerable<DxfGraphRecordLocator> nonIdentity,
        IEnumerable<DxfReferenceEdge> references, IEnumerable<DxfOwnershipEdge> ownership,
        IEnumerable<DxfHandleConflict> duplicates, IEnumerable<DxfOwnerCycle> cycles,
        IEnumerable<DxfGraphDiagnostic> diagnostics, bool validated, bool identityComplete, bool referenceComplete,
        CancellationToken token = default)
    {
        AnnotationEvidence = evidence; DocumentProvenance = document; Status = status;
        Nodes = Freeze(nodes, token); NonIdentityRecords = Freeze(nonIdentity, token); References = Freeze(references, token);
        Ownership = Freeze(ownership, token); DuplicateGroups = Freeze(duplicates, token); OwnerCycles = Freeze(cycles, token); Diagnostics = Freeze(diagnostics, token);
        var unresolved = new List<int>(); var ambiguous = new List<int>();
        for (var i = 0; i < References.Count; i++)
        {
            token.ThrowIfCancellationRequested();
            if (References[i].Resolution == DxfReferenceResolution.Unresolved) unresolved.Add(i);
            if (References[i].Resolution == DxfReferenceResolution.Ambiguous) ambiguous.Add(i);
        }
        UnresolvedReferenceIndices = Freeze(unresolved, token); AmbiguousReferenceIndices = Freeze(ambiguous, token);
        var ownerFacts = new Dictionary<DxfGraphRecordLocator, List<DxfReferenceResolution>>();
        foreach (var edge in References)
        {
            token.ThrowIfCancellationRequested(); if (edge.ContextEvidence.Role != DxfReferenceRole.Owner) continue;
            if (!ownerFacts.TryGetValue(edge.SourceLocator, out var facts)) { facts = []; ownerFacts.Add(edge.SourceLocator, facts); } facts.Add(edge.Resolution);
        }
        var roots = new List<DxfGraphRecordLocator>();
        foreach (var node in Nodes)
        {
            token.ThrowIfCancellationRequested(); if (node.Validity != DxfIdentityValidity.Valid) continue;
            var facts = ownerFacts.GetValueOrDefault(node.Locator);
            var requiredOwner = node.SourceRecord.ContextMarkers.Any(m => m.GroupCode == 100 && m.Application == null && m.ControlPath.Length == 0 && m.Subclass == "AcDbEntity");
            if (facts == null && !requiredOwner || facts is { Count: 1 } && facts[0] == DxfReferenceResolution.Null) roots.Add(node.Locator);
        }
        OwnerRoots = Freeze(roots, token);
        var completeStatus = status is DxfGraphStatus.Succeeded or DxfGraphStatus.Partial;
        GraphValidated = validated && completeStatus; IdentityCoverageComplete = identityComplete && completeStatus;
        ReferenceResolutionComplete = referenceComplete && completeStatus; token.ThrowIfCancellationRequested();
    }
    private static IReadOnlyList<T> Freeze<T>(IEnumerable<T> source, CancellationToken token)
    {
        var copy = new List<T>(); foreach (var item in source) { token.ThrowIfCancellationRequested(); copy.Add(item); }
        token.ThrowIfCancellationRequested(); return Array.AsReadOnly(copy.ToArray());
    }
}
