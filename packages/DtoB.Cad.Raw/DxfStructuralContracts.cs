namespace DtoB.Cad.Raw;

public enum DxfStructuralStatus { Succeeded, Partial, InvalidInput, UnsupportedInput, UnsupportedVersion, ResourceLimitExceeded, Canceled, IoFailure }
public enum DxfStructuralVersionAcceptance { NotValidated, Supported, Unsupported, Invalid }
public enum DxfDisposition { Supported, IgnoredWithDiagnostic, Unsupported, Invalid, DeferredTo03C, DeferredTo03D }
public enum DxfFactPresence { Absent, Present }
public enum DxfFactValidity { NotApplicable, Valid, Invalid }
public enum DxfFactKind { Text, Integer, Real }
public enum DxfReportedExtentsState { NotAvailable, SourceConfirmed, UnusableReportedExtents }
public enum DxfStructuralDiagnosticSeverity { Information, Warning, Error }
public enum DxfStructuralDiagnosticCode
{
    LexicalInputRejected, InvalidEvidence, MissingSectionName, NestedSection, UnexpectedEndSec,
    MissingEndSec, MissingEof, ContentAfterEof, DuplicateHeaderSection, InvalidTransition,
    MissingSections, UnknownSection, CommentsRetained, MalformedHeaderVariable,
    DuplicateHeaderVariable, InvalidHeaderValue, MissingAcadVersion, DuplicateAcadVersion,
    UnsupportedAcadVersion, UnusableReportedExtents, UnmappedHeaderVariables,
    NestedTable, UnexpectedEndTab, MissingEndTab, MissingTableName, UnknownTable,
    DeclaredTableCountMismatch, InvalidTableValue, DuplicateField, UnknownRecord,
    DeferredRecords, InvalidControlGroup, UnclosedControlGroup, StructuralLimitExceeded,
    DiagnosticLimitExceeded, Canceled
}

/// <summary>Inclusive indices into LexicalEvidence.Pairs; physical lines are one-based.</summary>
public sealed record DxfSourceSpan(int StartPairIndex, int EndPairIndex, int StartLine, int EndLine);
public sealed record DxfStructuralDiagnostic(DxfStructuralDiagnosticCode Code,
    DxfStructuralDiagnosticSeverity Severity, string Message, DxfSourceSpan? Source = null, int Count = 1);

/// <summary>A factual scalar, never a source default. Exactly one typed slot is used when valid.
/// SourcePairIndices references the original immutable lexical store, including duplicate occurrences.</summary>
public sealed class DxfSourceFact
{
    public DxfFactKind Kind { get; }
    public DxfFactPresence Presence { get; }
    public DxfFactValidity Validity { get; }
    public string? Text { get; }
    public int? Integer { get; }
    public double? Real { get; }
    public IReadOnlyList<int> SourcePairIndices { get; }
    public DxfSourceSpan? Source { get; }
    public DxfSourceFact(DxfFactKind kind, DxfFactPresence presence, DxfFactValidity validity,
        string? text, int? integer, double? real, IEnumerable<int> sourcePairIndices, DxfSourceSpan? source)
    {
        Kind = kind; Presence = presence; Validity = validity; Text = text; Integer = integer; Real = real;
        SourcePairIndices = Array.AsReadOnly(sourcePairIndices.ToArray()); Source = source;
    }
}

public sealed record DxfNamedFact(string Name, DxfSourceFact Fact);
public sealed record DxfHeaderVariable(string Name, DxfSourceSpan Source, DxfDisposition Disposition);
public sealed record DxfRawHeaderPoint(DxfSourceFact X, DxfSourceFact Y, DxfSourceFact Z);
public sealed class DxfHeaderFacts
{
    public IReadOnlyList<DxfHeaderVariable> Variables { get; }
    public DxfSourceFact AcadVersion { get; }
    public DxfSourceFact InsertionUnits { get; }
    public DxfSourceFact DwgCodePage { get; }
    public DxfSourceFact HandleSeed { get; }
    public DxfRawHeaderPoint ExtMin { get; }
    public DxfRawHeaderPoint ExtMax { get; }
    public DxfReportedExtentsState ReportedExtentsState { get; }
    public DxfHeaderFacts(IEnumerable<DxfHeaderVariable> variables, DxfSourceFact acadVersion,
        DxfSourceFact insertionUnits, DxfSourceFact dwgCodePage, DxfSourceFact handleSeed,
        DxfRawHeaderPoint extMin, DxfRawHeaderPoint extMax, DxfReportedExtentsState extentsState)
    {
        Variables = Array.AsReadOnly(variables.ToArray()); AcadVersion = acadVersion;
        InsertionUnits = insertionUnits; DwgCodePage = dwgCodePage; HandleSeed = handleSeed;
        ExtMin = extMin; ExtMax = extMax; ReportedExtentsState = extentsState;
    }
}

public sealed record DxfSectionRecord(string Name, DxfSourceSpan Source, DxfDisposition Disposition);
public sealed record DxfContextMarker(int PairIndex, int GroupCode, string? Subclass,
    string? Application, string ControlPath);
public enum DxfReferenceRole { Owner, Reactor, ExtensionDictionary, XData, Other }
public sealed record DxfRawReference(int PairIndex, int GroupCode, DxfReferenceRole Role,
    string? Subclass, string? Application, string ControlPath);

/// <summary>Raw record only. Deferred geometry/annotation records contain no typed geometry.</summary>
public sealed class DxfStructuralRecord
{
    public string RecordType { get; }
    public string Scope { get; }
    public DxfSourceSpan Source { get; }
    public DxfDisposition Disposition { get; }
    public IReadOnlyList<DxfNamedFact> Fields { get; }
    public IReadOnlyList<int> UnmappedPairIndices { get; }
    public IReadOnlyList<DxfContextMarker> ContextMarkers { get; }
    public IReadOnlyList<DxfRawReference> References { get; }
    public DxfStructuralRecord(string recordType, string scope, DxfSourceSpan source,
        DxfDisposition disposition, IEnumerable<DxfNamedFact> fields, IEnumerable<int> unmapped,
        IEnumerable<DxfContextMarker> markers, IEnumerable<DxfRawReference> references)
    {
        RecordType = recordType; Scope = scope; Source = source; Disposition = disposition;
        Fields = Array.AsReadOnly(fields.ToArray()); UnmappedPairIndices = Array.AsReadOnly(unmapped.ToArray());
        ContextMarkers = Array.AsReadOnly(markers.ToArray()); References = Array.AsReadOnly(references.ToArray());
    }
}

public sealed class DxfTableRecord
{
    public string Name { get; }
    public DxfSourceSpan Source { get; }
    public DxfSourceSpan HeaderSource { get; }
    public DxfDisposition Disposition { get; }
    public DxfSourceFact DeclaredEntryCount { get; }
    public int ActualEntryCount => Entries.Count;
    public IReadOnlyList<DxfStructuralRecord> Entries { get; }
    public DxfStructuralRecord HeaderRecord { get; }
    public DxfTableRecord(string name, DxfSourceSpan source, DxfSourceSpan headerSource,
        DxfDisposition disposition, DxfSourceFact declaredCount,
        IEnumerable<DxfStructuralRecord> entries, DxfStructuralRecord headerRecord)
    {
        Name = name; Source = source; HeaderSource = headerSource; Disposition = disposition;
        DeclaredEntryCount = declaredCount; Entries = Array.AsReadOnly(entries.ToArray()); HeaderRecord = headerRecord;
    }
}

public sealed record DxfDispositionCount(DxfDisposition Disposition, int Count);
public sealed class DxfStructuralReadResult
{
    public DxfLexicalReadResult LexicalEvidence { get; }
    public DxfStructuralStatus Status { get; }
    public DxfStructuralVersionAcceptance VersionAcceptance { get; }
    public IReadOnlyList<DxfSectionRecord> Sections { get; }
    public DxfHeaderFacts? Header { get; }
    public IReadOnlyList<DxfTableRecord> Tables { get; }
    public IReadOnlyList<DxfStructuralRecord> Records { get; }
    public IReadOnlyList<DxfStructuralDiagnostic> Diagnostics { get; }
    public IReadOnlyList<DxfDispositionCount> RecordDispositionCounts { get; }
    public bool StructureValidated { get; }
    public bool MappingComplete => Status == DxfStructuralStatus.Succeeded;
    public DxfStructuralReadResult(DxfLexicalReadResult evidence, DxfStructuralStatus status,
        DxfStructuralVersionAcceptance version, IEnumerable<DxfSectionRecord> sections,
        DxfHeaderFacts? header, IEnumerable<DxfTableRecord> tables, IEnumerable<DxfStructuralRecord> records,
        IEnumerable<DxfStructuralDiagnostic> diagnostics, bool structureValidated, CancellationToken cancellationToken = default)
    {
        LexicalEvidence = evidence; Status = status; VersionAcceptance = version; Header = header;
        Sections = Freeze(sections, cancellationToken); Tables = Freeze(tables, cancellationToken);
        Records = Freeze(records, cancellationToken); Diagnostics = Freeze(diagnostics, cancellationToken);
        var counts = new Dictionary<DxfDisposition, int>();
        foreach (var record in Records)
        { cancellationToken.ThrowIfCancellationRequested(); counts[record.Disposition] = counts.GetValueOrDefault(record.Disposition) + 1; }
        RecordDispositionCounts = Freeze(counts.OrderBy(c => c.Key).Select(c => new DxfDispositionCount(c.Key, c.Value)), cancellationToken);
        StructureValidated = structureValidated;
        cancellationToken.ThrowIfCancellationRequested();
    }
    private static IReadOnlyList<T> Freeze<T>(IEnumerable<T> source, CancellationToken token)
    {
        var values = new List<T>();
        foreach (var value in source) { token.ThrowIfCancellationRequested(); values.Add(value); }
        token.ThrowIfCancellationRequested();
        return Array.AsReadOnly(values.ToArray());
    }
}
