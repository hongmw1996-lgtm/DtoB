using System.Text.Json.Serialization;

namespace DtoB.Cad.Raw;

public enum RawSnapshotAssemblyStatus { Succeeded, InvalidInput, ResourceLimitExceeded, Canceled }
public enum RawSnapshotCoverage { Complete, Partial }
public enum RawSnapshotDiagnosticStage { Lexical03A, Structural03B, Geometry03C, BlockAnnotation03D, Identity03E, Assembly03F }
public enum RawSnapshotDiagnosticCode { PredecessorRejected, InvalidAssociation, ConflictingAssociation, InconsistentInventory, ResourceLimitExceeded, Canceled }
public sealed record RawSnapshotAssemblyDiagnostic(RawSnapshotDiagnosticCode Code, string Message, DxfSourceSpan? Source = null);
/// <summary>Index addresses the exact immutable diagnostic in its stage, never a rewritten message.</summary>
public sealed record RawSnapshotDiagnosticReference(RawSnapshotDiagnosticStage Stage, int Index);
public sealed record RawSnapshotCompleteness(bool LexicalSucceeded, bool StructureValidated,
    bool GeometryMappingComplete, bool BlockAnnotationMappingComplete, bool IdentityCoverageComplete,
    bool ReferenceResolutionComplete, bool GraphValidated, bool SourceEvidenceComplete,
    bool TypedMappingComplete, bool SnapshotAssemblyComplete)
{
    public RawSnapshotCoverage Coverage => SourceEvidenceComplete && TypedMappingComplete && GraphValidated &&
        IdentityCoverageComplete && ReferenceResolutionComplete ? RawSnapshotCoverage.Complete : RawSnapshotCoverage.Partial;
}
/// <summary>Exactly one view per structural child, in source order. All indices are QA locators.
/// Reference properties preserve upstream instances; serialization uses small indices instead of cloned data.</summary>
public sealed class RawDrawingRecordView
{
    public int StructuralRecordIndex { get; }
    public DxfDisposition SemanticDisposition { get; }
    public int? GeometryRecordIndex { get; }
    public int? BlockAnnotationRecordIndex { get; }
    public int? IdentityNodeIndex { get; }
    public int? BlockDefinitionIndex { get; }
    [JsonIgnore] public DxfStructuralRecord SourceRecord { get; }
    [JsonIgnore] public RawGeometryRecord? Geometry { get; }
    [JsonIgnore] public RawBlockAnnotationRecord? BlockAnnotation { get; }
    [JsonIgnore] public DxfIdentityNode? Identity { get; }
    [JsonIgnore] public string SourceSection => SourceRecord.Scope.Split('/')[0];
    [JsonIgnore] public string? TableScope => SourceRecord.Scope.StartsWith("TABLES/", StringComparison.Ordinal) ? SourceRecord.Scope[7..] : null;
    public RawDrawingRecordView(int index, DxfStructuralRecord source, DxfDisposition disposition,
        int? geometryIndex, RawGeometryRecord? geometry, int? annotationIndex, RawBlockAnnotationRecord? annotation,
        int? identityIndex, DxfIdentityNode? identity, int? block)
    {
        StructuralRecordIndex = index; SourceRecord = source; SemanticDisposition = disposition;
        GeometryRecordIndex = geometryIndex; Geometry = geometry; BlockAnnotationRecordIndex = annotationIndex;
        BlockAnnotation = annotation; IdentityNodeIndex = identityIndex; Identity = identity; BlockDefinitionIndex = block;
    }
}
/// <summary>In-memory raw artifact. No project persistence, normalized IR or rendering semantics.</summary>
public sealed class RawDrawingSnapshot
{
    public DxfIdentityGraphReadResult IdentityEvidence { get; }
    [JsonIgnore] public DxfBlockAnnotationReadResult BlockAnnotationEvidence => IdentityEvidence.AnnotationEvidence;
    [JsonIgnore] public DxfGeometryReadResult GeometryEvidence => BlockAnnotationEvidence.GeometryEvidence;
    [JsonIgnore] public DxfStructuralReadResult StructuralEvidence => GeometryEvidence.StructuralEvidence;
    [JsonIgnore] public DxfLexicalReadResult LexicalEvidence => StructuralEvidence.LexicalEvidence;
    [JsonIgnore] public DxfDocumentProvenance? DocumentProvenance => IdentityEvidence.DocumentProvenance;
    [JsonIgnore] public DxfHeaderFacts? Header => StructuralEvidence.Header;
    [JsonIgnore] public IReadOnlyList<DxfSectionRecord> Sections => StructuralEvidence.Sections;
    [JsonIgnore] public IReadOnlyList<DxfTableRecord> Tables => StructuralEvidence.Tables;
    [JsonIgnore] public IReadOnlyList<RawGeometryRecord> Geometry => GeometryEvidence.Geometry;
    [JsonIgnore] public IReadOnlyList<RawBlockAnnotationRecord> BlockAnnotations => BlockAnnotationEvidence.Records;
    [JsonIgnore] public IReadOnlyList<RawBlockDefinition> BlockDefinitions => BlockAnnotationEvidence.BlockDefinitions;
    [JsonIgnore] public IReadOnlyList<DxfIdentityNode> IdentityNodes => IdentityEvidence.Nodes;
    [JsonIgnore] public IReadOnlyList<DxfReferenceEdge> References => IdentityEvidence.References;
    [JsonIgnore] public IReadOnlyList<DxfOwnershipEdge> Ownership => IdentityEvidence.Ownership;
    [JsonIgnore] public IReadOnlyList<DxfDispositionCount> SemanticDispositionCounts => BlockAnnotationEvidence.EffectiveRecordDispositionCounts;
    public IReadOnlyList<RawDrawingRecordView> Records { get; }
    public IReadOnlyList<RawSnapshotDiagnosticReference> Diagnostics { get; }
    public IReadOnlyList<RawSnapshotAssemblyDiagnostic> AssemblyDiagnostics { get; }
    public RawSnapshotCompleteness Completeness { get; }
    public RawDrawingSnapshot(DxfIdentityGraphReadResult evidence, IEnumerable<RawDrawingRecordView> records,
        IEnumerable<RawSnapshotDiagnosticReference> diagnostics, IReadOnlyList<RawSnapshotAssemblyDiagnostic> assemblyDiagnostics,
        RawSnapshotCompleteness completeness, CancellationToken token = default)
    {
        IdentityEvidence = evidence; Records = Freeze(records, token); Diagnostics = Freeze(diagnostics, token);
        AssemblyDiagnostics = Freeze(assemblyDiagnostics, token); Completeness = completeness; token.ThrowIfCancellationRequested();
    }
    public object ResolveDiagnostic(RawSnapshotDiagnosticReference reference) => reference.Stage switch
    {
        RawSnapshotDiagnosticStage.Lexical03A => LexicalEvidence.Diagnostics[reference.Index],
        RawSnapshotDiagnosticStage.Structural03B => StructuralEvidence.Diagnostics[reference.Index],
        RawSnapshotDiagnosticStage.Geometry03C => GeometryEvidence.Diagnostics[reference.Index],
        RawSnapshotDiagnosticStage.BlockAnnotation03D => BlockAnnotationEvidence.Diagnostics[reference.Index],
        RawSnapshotDiagnosticStage.Identity03E => IdentityEvidence.Diagnostics[reference.Index],
        RawSnapshotDiagnosticStage.Assembly03F => AssemblyDiagnostics[reference.Index],
        _ => throw new ArgumentOutOfRangeException(nameof(reference))
    };
    private static IReadOnlyList<T> Freeze<T>(IEnumerable<T> source, CancellationToken token)
    {
        var copy = new List<T>(); foreach (var item in source) { token.ThrowIfCancellationRequested(); copy.Add(item); }
        token.ThrowIfCancellationRequested(); return Array.AsReadOnly(copy.ToArray());
    }
}
public sealed class RawDrawingSnapshotAssemblyResult
{
    [JsonIgnore] public DxfIdentityGraphReadResult InputEvidence { get; }
    public RawSnapshotAssemblyStatus Status { get; }
    public RawDrawingSnapshot? Snapshot { get; }
    public RawSnapshotCompleteness Completeness { get; }
    public IReadOnlyList<RawSnapshotAssemblyDiagnostic> Diagnostics { get; }
    public RawDrawingSnapshotAssemblyResult(DxfIdentityGraphReadResult input, RawSnapshotAssemblyStatus status,
        RawDrawingSnapshot? snapshot, RawSnapshotCompleteness completeness, IEnumerable<RawSnapshotAssemblyDiagnostic> diagnostics)
    { InputEvidence = input; Status = status; Snapshot = snapshot; Completeness = completeness; Diagnostics = Array.AsReadOnly(diagnostics.ToArray()); }
}
