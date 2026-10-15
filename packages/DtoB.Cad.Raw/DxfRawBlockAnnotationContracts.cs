using System.Text.Json.Serialization;

namespace DtoB.Cad.Raw;

public enum DxfBlockAnnotationStatus { Succeeded, Partial, InvalidInput, UnsupportedInput, UnsupportedVersion, ResourceLimitExceeded, Canceled, IoFailure }
public enum DxfBlockAnnotationDiagnosticCode
{
    InputRejected, InvalidSourceRecord, MissingRequiredField, DuplicateField, InvalidNumericValue,
    ForeignFieldContext, NestedBlock, UnexpectedBlockEnd, MissingBlockEnd, ChildOutsideBlock,
    UnsupportedOccurrenceExpansion, UnsupportedDimensionSubtype, InvalidMTextChunks,
    MissingAttributeSequenceEnd, OrphanAttribute, UnassignedSequenceEnd, LegacySequenceGated,
    ResourceLimitExceeded, DiagnosticLimitExceeded, Canceled
}
public sealed record DxfBlockAnnotationDiagnostic(DxfBlockAnnotationDiagnosticCode Code,
    DxfStructuralDiagnosticSeverity Severity, string Message, DxfSourceSpan? Source = null);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$raw03d")]
[JsonDerivedType(typeof(RawBlockBegin), "BLOCK")]
[JsonDerivedType(typeof(RawBlockEnd), "ENDBLK")]
[JsonDerivedType(typeof(RawInsert), "INSERT")]
[JsonDerivedType(typeof(RawText), "TEXT")]
[JsonDerivedType(typeof(RawMText), "MTEXT")]
[JsonDerivedType(typeof(RawDimension), "DIMENSION")]
[JsonDerivedType(typeof(RawAttrib), "ATTRIB")]
[JsonDerivedType(typeof(RawAttDef), "ATTDEF")]
[JsonDerivedType(typeof(RawSequenceEnd), "SEQEND")]
public abstract record RawBlockAnnotationPayload;
public sealed record RawBlockBegin(DxfSourceFact Name, DxfSourceFact AlternateName,
    RawCoordinateFacts BasePoint, DxfSourceFact Flags, DxfSourceFact XrefPath, DxfSourceFact Description) : RawBlockAnnotationPayload;
public sealed record RawBlockEnd : RawBlockAnnotationPayload;
public sealed record RawInsert(DxfSourceFact TargetName, RawCoordinateFacts Insertion,
    RawCoordinateFacts Scale, DxfSourceFact RotationDegrees, DxfSourceFact Columns, DxfSourceFact Rows,
    DxfSourceFact ColumnSpacing, DxfSourceFact RowSpacing, DxfSourceFact AttributesFollow,
    RawCoordinateFacts Extrusion, bool OccurrenceExpansionUnsupported) : RawBlockAnnotationPayload;
public sealed record RawText(DxfSourceFact Content, RawCoordinateFacts Insertion,
    RawCoordinateFacts Alignment, DxfSourceFact Height, DxfSourceFact RotationDegrees, DxfSourceFact Style,
    DxfSourceFact HorizontalJustification, DxfSourceFact VerticalJustification,
    DxfSourceFact Thickness, RawCoordinateFacts Extrusion) : RawBlockAnnotationPayload;
public sealed record RawTextChunk(int GroupCode, DxfSourceFact Content);
public sealed record RawMText : RawBlockAnnotationPayload
{
    public IReadOnlyList<RawTextChunk> Chunks { get; }
    public RawCoordinateFacts Insertion { get; }
    public RawCoordinateFacts Direction { get; }
    public DxfSourceFact Height { get; }
    public DxfSourceFact RectangleWidth { get; }
    public DxfSourceFact Attachment { get; }
    public DxfSourceFact DrawingDirection { get; }
    public DxfSourceFact SpacingStyle { get; }
    public DxfSourceFact SpacingFactor { get; }
    public DxfSourceFact Style { get; }
    public RawCoordinateFacts Extrusion { get; }
    public RawMText(IEnumerable<RawTextChunk> chunks, RawCoordinateFacts insertion, RawCoordinateFacts direction,
        DxfSourceFact height, DxfSourceFact width, DxfSourceFact attachment, DxfSourceFact drawingDirection,
        DxfSourceFact spacingStyle, DxfSourceFact spacingFactor, DxfSourceFact style, RawCoordinateFacts extrusion)
    {
        Chunks = Array.AsReadOnly(chunks.ToArray()); Insertion = insertion; Direction = direction;
        Height = height; RectangleWidth = width; Attachment = attachment; DrawingDirection = drawingDirection;
        SpacingStyle = spacingStyle; SpacingFactor = spacingFactor; Style = style; Extrusion = extrusion;
    }
}
/// <summary>Null subtype fields mean unmapped coverage, not verified absence.
/// SourceMeasurement is an encoded source value, never a calculated distance.</summary>
public sealed record RawDimension(DxfSourceFact BlockName, DxfSourceFact Style, DxfSourceFact TextOverride,
    DxfSourceFact TypeFlags, RawCoordinateFacts DefinitionPoint, RawCoordinateFacts TextMidpoint,
    RawCoordinateFacts? FirstExtensionPoint, RawCoordinateFacts? SecondExtensionPoint,
    DxfSourceFact? RotationDegrees, DxfSourceFact SourceMeasurement, RawCoordinateFacts Extrusion,
    bool SubtypeProfileSupported) : RawBlockAnnotationPayload;
public sealed record RawAttrib(RawText Text, DxfSourceFact Tag, DxfSourceFact Flags) : RawBlockAnnotationPayload;
public sealed record RawAttDef(RawText Text, DxfSourceFact Tag, DxfSourceFact Prompt, DxfSourceFact Flags) : RawBlockAnnotationPayload;
public sealed record RawSequenceEnd(string Role) : RawBlockAnnotationPayload;

public sealed class RawBlockAnnotationRecord
{
    public int StructuralRecordIndex { get; }
    public DxfStructuralRecord SourceRecord { get; }
    public string EntityType => SourceRecord.RecordType;
    public DxfSourceSpan Source => SourceRecord.Source;
    public DxfDisposition Disposition { get; }
    /// <summary>Null means no mapped common profile; an absent fact means checked source absence.</summary>
    public DxfSourceFact? Layer { get; }
    public RawBlockAnnotationPayload? Payload { get; }
    public IReadOnlyList<DxfNamedFact> Fields { get; }
    public IReadOnlyList<int> UnmappedPairIndices { get; }
    public RawBlockAnnotationRecord(int index, DxfStructuralRecord source, DxfDisposition disposition,
        DxfSourceFact? layer, RawBlockAnnotationPayload? payload, IEnumerable<DxfNamedFact> fields, IEnumerable<int> unmapped)
    {
        StructuralRecordIndex = index; SourceRecord = source; Disposition = disposition; Layer = layer; Payload = payload;
        Fields = Array.AsReadOnly(fields.ToArray()); UnmappedPairIndices = Array.AsReadOnly(unmapped.ToArray());
    }
}
/// <summary>All indices are result-local locators, never CAD IDs. Referenced children are not cloned.</summary>
public sealed record RawBlockChild(int StructuralRecordIndex, int? GeometryRecordIndex, int? AnnotationRecordIndex);
public sealed class RawBlockDefinition
{
    public int BeginRecordIndex { get; }
    public int? EndRecordIndex { get; }
    public DxfSourceSpan Source { get; }
    public DxfDisposition Disposition { get; }
    public IReadOnlyList<RawBlockChild> Children { get; }
    public RawBlockDefinition(int begin, int? end, DxfSourceSpan source, DxfDisposition disposition, IEnumerable<RawBlockChild> children)
    { BeginRecordIndex = begin; EndRecordIndex = end; Source = source; Disposition = disposition; Children = Array.AsReadOnly(children.ToArray()); }
}
public sealed class RawAttributeSequence
{
    public int InsertRecordIndex { get; }
    public IReadOnlyList<int> AttributeRecordIndices { get; }
    public int? EndRecordIndex { get; }
    public DxfDisposition Disposition { get; }
    public RawAttributeSequence(int insert, IEnumerable<int> attributes, int? end, DxfDisposition disposition)
    { InsertRecordIndex = insert; AttributeRecordIndices = Array.AsReadOnly(attributes.ToArray()); EndRecordIndex = end; Disposition = disposition; }
}

public sealed class DxfBlockAnnotationReadResult
{
    public DxfGeometryReadResult GeometryEvidence { get; }
    [JsonIgnore] public DxfStructuralReadResult StructuralEvidence => GeometryEvidence.StructuralEvidence;
    [JsonIgnore] public DxfLexicalReadResult LexicalEvidence => StructuralEvidence.LexicalEvidence;
    public DxfBlockAnnotationStatus Status { get; }
    public IReadOnlyList<RawBlockAnnotationRecord> Records { get; }
    public IReadOnlyList<RawBlockDefinition> BlockDefinitions { get; }
    public IReadOnlyList<RawAttributeSequence> AttributeSequences { get; }
    public IReadOnlyList<DxfBlockAnnotationDiagnostic> Diagnostics { get; }
    public IReadOnlyList<DxfDispositionCount> EffectiveRecordDispositionCounts { get; }
    public bool MappingComplete { get; }
    public bool EffectiveInventoryAvailable { get; }
    public DxfBlockAnnotationReadResult(DxfGeometryReadResult geometry, DxfBlockAnnotationStatus status,
        IEnumerable<RawBlockAnnotationRecord> records, IEnumerable<RawBlockDefinition> blocks,
        IEnumerable<RawAttributeSequence> sequences, IEnumerable<DxfBlockAnnotationDiagnostic> diagnostics,
        bool complete, CancellationToken token = default, int maxEffectiveRecords = 4_096)
    {
        GeometryEvidence = geometry; Status = status; Records = Freeze(records, token); BlockDefinitions = Freeze(blocks, token);
        AttributeSequences = Freeze(sequences, token); Diagnostics = Freeze(diagnostics, token);
        var counts = new Dictionary<DxfDisposition, int>(); EffectiveInventoryAvailable = StructuralEvidence.Records.Count <= maxEffectiveRecords;
        var replacement = new Dictionary<int, DxfDisposition>();
        if (EffectiveInventoryAvailable)
        {
            foreach (var r in geometry.Geometry) { token.ThrowIfCancellationRequested(); replacement.Add(r.StructuralRecordIndex, r.Disposition); }
            foreach (var r in Records) { token.ThrowIfCancellationRequested(); replacement[r.StructuralRecordIndex] = r.Disposition; }
        }
        for (var i = 0; EffectiveInventoryAvailable && i < StructuralEvidence.Records.Count; i++)
        {
            token.ThrowIfCancellationRequested(); var d = replacement.GetValueOrDefault(i, StructuralEvidence.Records[i].Disposition);
            counts[d] = counts.GetValueOrDefault(d) + 1;
        }
        EffectiveRecordDispositionCounts = Freeze(counts.OrderBy(c => c.Key).Select(c => new DxfDispositionCount(c.Key, c.Value)), token);
        MappingComplete = complete && status is DxfBlockAnnotationStatus.Succeeded or DxfBlockAnnotationStatus.Partial;
        token.ThrowIfCancellationRequested();
    }
    private static IReadOnlyList<T> Freeze<T>(IEnumerable<T> source, CancellationToken token)
    {
        var values = new List<T>(); foreach (var value in source) { token.ThrowIfCancellationRequested(); values.Add(value); }
        token.ThrowIfCancellationRequested(); return Array.AsReadOnly(values.ToArray());
    }
}
