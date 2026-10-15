using DtoB.Cad.Raw;

namespace DtoB.Cad.Dxf;

/// <summary>Consumes only 03A's immutable evidence. No streams, paths, re-tokenizing or CAD geometry.
/// Synchronous CPU work; a future integration caller schedules it off the UI thread.</summary>
public sealed partial class TextDxfStructuralReader
{
    private readonly DxfStructuralLimits limits;
    public TextDxfStructuralReader(DxfStructuralLimits? limits = null)
    {
        this.limits = limits ?? new(); this.limits.Validate();
    }
    public DxfStructuralReadResult Read(DxfLexicalReadResult evidence, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        return new Operation(evidence, limits, cancellationToken).Run();
    }

    private sealed partial class Operation(DxfLexicalReadResult evidence, DxfStructuralLimits limits, CancellationToken token)
    {
        private IReadOnlyList<DxfRawPair> P => evidence.Pairs;
        private readonly List<DxfSectionRecord> sections = [];
        private readonly List<DxfTableRecord> tables = [];
        private readonly List<DxfStructuralRecord> records = [];
        private readonly List<DxfStructuralDiagnostic> diagnostics = [];
        private DxfHeaderFacts? header;
        private DxfStructuralVersionAcceptance version;
        private bool invalid;
        private bool partial;
        private bool structureValidated;
        private int referenceCount;
        private int recordWorkCount;
        private int errorCount;
        private readonly HashSet<int> comments = [];

        private static readonly HashSet<string> KnownSections = new(StringComparer.Ordinal)
            { "HEADER", "CLASSES", "TABLES", "BLOCKS", "ENTITIES", "OBJECTS" };
        private static readonly HashSet<string> KnownTables = new(StringComparer.Ordinal)
            { "LAYER", "LTYPE", "STYLE", "DIMSTYLE", "BLOCK_RECORD" };

        public DxfStructuralReadResult Run()
        {
            try
            {
                token.ThrowIfCancellationRequested();
                if (evidence.Status != DxfLexicalStatus.Succeeded)
                {
                    Add(DxfStructuralDiagnosticCode.LexicalInputRejected, DxfStructuralDiagnosticSeverity.Error,
                        "Structural parsing requires successful lexical evidence.");
                    return Result(evidence.Status switch
                    {
                        DxfLexicalStatus.Canceled => DxfStructuralStatus.Canceled,
                        DxfLexicalStatus.IoFailure => DxfStructuralStatus.IoFailure,
                        DxfLexicalStatus.ResourceLimitExceeded => DxfStructuralStatus.ResourceLimitExceeded,
                        DxfLexicalStatus.UnsupportedEncoding or DxfLexicalStatus.UnsupportedInput => DxfStructuralStatus.UnsupportedInput,
                        _ => DxfStructuralStatus.InvalidInput
                    });
                }
                Guard(P.Count > limits.MaxPairCount, "pair count");
                // Public evidence constructors cannot bypass the contiguous source-index contract.
                for (var i = 0; i < P.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    if (P[i].PairIndex != i || P[i].CodeLineNumber != i * 2 + 1 || P[i].ValueLineNumber != i * 2 + 2)
                        Fail(DxfStructuralDiagnosticCode.InvalidEvidence, "Lexical evidence indices/physical lines are not contiguous.", i);
                }
                ParseSections();
                ParseHeader();
                ValidateVersion();
                // Unsupported profiles expose structural/header evidence only; no assumed table field map.
                if (version == DxfStructuralVersionAcceptance.Supported)
                {
                    foreach (var section in sections)
                    {
                        token.ThrowIfCancellationRequested();
                        if (section.Name == "TABLES") ParseTables(section);
                        else if (section.Name != "HEADER") PartitionSection(section);
                    }
                }
                if (comments.Count > 0)
                    Add(DxfStructuralDiagnosticCode.CommentsRetained, DxfStructuralDiagnosticSeverity.Information,
                        "Code 999 comments retained and ignored for structural mapping.", Span(comments.Min(), comments.Min()), comments.Count);
                var deferred = records.Count(r => r.Disposition is DxfDisposition.DeferredTo03C or DxfDisposition.DeferredTo03D);
                if (deferred > 0)
                    Add(DxfStructuralDiagnosticCode.DeferredRecords, DxfStructuralDiagnosticSeverity.Information,
                        "Raw geometry/annotation/block boundaries retained; mapping belongs to later subphases.", null, deferred);
                structureValidated = !invalid && version == DxfStructuralVersionAcceptance.Supported;
                return Result(invalid ? DxfStructuralStatus.InvalidInput : version == DxfStructuralVersionAcceptance.Unsupported
                    ? DxfStructuralStatus.UnsupportedVersion : partial ? DxfStructuralStatus.Partial : DxfStructuralStatus.Succeeded);
            }
            catch (Stop stop) { structureValidated = false; return Result(stop.Status, checkCancellation: false); }
            catch (OperationCanceledException)
            {
                structureValidated = false;
                Terminal(DxfStructuralDiagnosticCode.Canceled, "Structural parsing canceled.");
                return Result(DxfStructuralStatus.Canceled, checkCancellation: false);
            }
        }

        private void ParseSections()
        {
            var i = 0; var eof = false;
            while (i < P.Count)
            {
                token.ThrowIfCancellationRequested();
                if (Comment(i)) { i++; continue; }
                if (eof) Fail(DxfStructuralDiagnosticCode.ContentAfterEof, "Meaningful content follows EOF.", i);
                if (Is(i, "EOF")) { eof = true; i++; continue; }
                if (Is(i, "ENDSEC")) Fail(DxfStructuralDiagnosticCode.UnexpectedEndSec, "ENDSEC outside SECTION.", i);
                if (!Is(i, "SECTION")) Fail(DxfStructuralDiagnosticCode.InvalidTransition, "Expected SECTION or EOF.", i);
                Guard(sections.Count >= limits.MaxSections, "section count", i);
                var start = i++;
                while (i < P.Count && Comment(i)) i++;
                if (i >= P.Count || P[i].GroupCode != 2 || string.IsNullOrWhiteSpace(P[i].RawTextValue))
                    Fail(DxfStructuralDiagnosticCode.MissingSectionName, "SECTION requires a code 2 name.", start);
                var name = P[i++].RawTextValue;
                if (name == "HEADER" && sections.Any(s => s.Name == "HEADER"))
                    Fail(DxfStructuralDiagnosticCode.DuplicateHeaderSection, "HEADER is a singleton section.", start);
                while (i < P.Count && !Is(i, "ENDSEC"))
                {
                    token.ThrowIfCancellationRequested(); Comment(i);
                    if (Is(i, "SECTION")) Fail(DxfStructuralDiagnosticCode.NestedSection, "SECTION cannot be nested.", i);
                    if (Is(i, "EOF")) Fail(DxfStructuralDiagnosticCode.MissingEndSec, "EOF reached before ENDSEC.", i);
                    i++;
                }
                if (i >= P.Count) Fail(DxfStructuralDiagnosticCode.MissingEndSec, "SECTION has no ENDSEC.", start);
                var disposition = KnownSections.Contains(name) ? DxfDisposition.Supported : DxfDisposition.Unsupported;
                sections.Add(new(name, Span(start, i), disposition));
                if (disposition == DxfDisposition.Unsupported)
                {
                    partial = true;
                    Add(DxfStructuralDiagnosticCode.UnknownSection, DxfStructuralDiagnosticSeverity.Warning,
                        "Unknown section retained without typed mapping.", Span(start, i));
                }
                i++;
            }
            if (!eof) Fail(DxfStructuralDiagnosticCode.MissingEof, "Terminal EOF is required.");
            if (sections.Count == 0) Fail(DxfStructuralDiagnosticCode.MissingSections, "EOF alone is not a valid drawing.");
        }

        private void ParseTables(DxfSectionRecord section)
        {
            var i = ContentStart(section); var end = section.Source.EndPairIndex;
            while (i < end)
            {
                token.ThrowIfCancellationRequested();
                if (Comment(i)) { i++; continue; }
                if (Is(i, "ENDTAB")) Fail(DxfStructuralDiagnosticCode.UnexpectedEndTab, "ENDTAB outside TABLE.", i);
                if (!Is(i, "TABLE")) Fail(DxfStructuralDiagnosticCode.InvalidTransition, "Expected TABLE within TABLES.", i);
                Guard(tables.Count >= limits.MaxTables, "table count", i);
                var start = i++;
                while (i < end && Comment(i)) i++;
                if (i >= end || P[i].GroupCode != 2 || string.IsNullOrWhiteSpace(P[i].RawTextValue))
                    Fail(DxfStructuralDiagnosticCode.MissingTableName, "TABLE requires a code 2 name.", start);
                var name = P[i++].RawTextValue;
                while (i < end && P[i].GroupCode != 0) { token.ThrowIfCancellationRequested(); i++; }
                var headerEnd = i - 1;
                var supported = KnownTables.Contains(name);
                var tableDisposition = supported ? DxfDisposition.Supported : DxfDisposition.Unsupported;
                var head = MakeRecord("TABLE", "TABLES/" + name, start, headerEnd, tableDisposition, tableHeader: true);
                var count = head.Fields.Single(f => f.Name == "DeclaredEntryCount").Fact;
                var entries = new List<DxfStructuralRecord>();
                while (i < end && !Is(i, "ENDTAB"))
                {
                    token.ThrowIfCancellationRequested();
                    if (Is(i, "TABLE")) Fail(DxfStructuralDiagnosticCode.NestedTable, "TABLE cannot be nested.", i);
                    if (P[i].GroupCode != 0) Fail(DxfStructuralDiagnosticCode.InvalidTransition, "Expected table record marker.", i);
                    var recordStart = i++;
                    while (i < end && P[i].GroupCode != 0) { token.ThrowIfCancellationRequested(); i++; }
                    var type = P[recordStart].RawTextValue;
                    var disposition = supported && type == name ? DxfDisposition.Supported : DxfDisposition.Unsupported;
                    var entry = MakeRecord(type, "TABLES/" + name, recordStart, i - 1, disposition);
                    entries.Add(entry); records.Add(entry);
                    if (disposition == DxfDisposition.Unsupported) UnknownRecord(entry);
                }
                if (i >= end) Fail(DxfStructuralDiagnosticCode.MissingEndTab, "TABLE has no ENDTAB.", start);
                // ENDTAB has its own structural context; extra non-comment fields are invalid.
                var close = i++;
                while (i < end && Comment(i)) i++;
                if (i < end && P[i].GroupCode != 0)
                    Fail(DxfStructuralDiagnosticCode.InvalidTransition, "Unexpected fields after ENDTAB.", i);
                tables.Add(new(name, Span(start, close), Span(start, headerEnd), tableDisposition, count, entries, head));
                if (!supported)
                {
                    partial = true;
                    Add(DxfStructuralDiagnosticCode.UnknownTable, DxfStructuralDiagnosticSeverity.Warning,
                        "Unknown table retained without typed entry mapping.", Span(start, close));
                }
                if (count.Integer.HasValue && count.Integer.Value != entries.Count)
                    Add(DxfStructuralDiagnosticCode.DeclaredTableCountMismatch, DxfStructuralDiagnosticSeverity.Warning,
                        "Declared table entry count differs from independently partitioned child count.", count.Source);
            }
        }

        private void PartitionSection(DxfSectionRecord section)
        {
            var i = ContentStart(section); var end = section.Source.EndPairIndex;
            while (i < end)
            {
                token.ThrowIfCancellationRequested();
                if (Comment(i)) { i++; continue; }
                if (KnownSections.Contains(section.Name) && P[i].GroupCode != 0)
                    Fail(DxfStructuralDiagnosticCode.InvalidTransition, "Known record section requires a code 0 record boundary.", i);
                if (KnownSections.Contains(section.Name) && (Is(i, "TABLE") || Is(i, "ENDTAB")))
                    Fail(DxfStructuralDiagnosticCode.InvalidTransition, "TABLE/ENDTAB belongs only in TABLES.", i);
                var start = i++; var type = P[start].GroupCode == 0 ? P[start].RawTextValue : "<unmapped>";
                while (i < end && P[i].GroupCode != 0) { token.ThrowIfCancellationRequested(); i++; }
                var disposition = section.Name == "CLASSES" && type == "CLASS" ? DxfDisposition.Supported :
                    section.Name is "ENTITIES" or "BLOCKS" ? type switch
                    {
                        "LINE" or "LWPOLYLINE" or "ARC" or "POINT" or "SOLID" or "CIRCLE" or "POLYLINE" or "VERTEX" => DxfDisposition.DeferredTo03C,
                        "BLOCK" or "ENDBLK" or "INSERT" or "TEXT" or "MTEXT" or "DIMENSION" or "ATTRIB" or "ATTDEF" or "SEQEND" => DxfDisposition.DeferredTo03D,
                        _ => DxfDisposition.Unsupported
                    } : DxfDisposition.Unsupported;
                var record = MakeRecord(type, section.Name, start, i - 1, disposition);
                records.Add(record);
                if (disposition != DxfDisposition.Supported) partial = true;
                if (disposition == DxfDisposition.Unsupported) UnknownRecord(record);
            }
        }

        private void UnknownRecord(DxfStructuralRecord record)
        {
            partial = true;
            Add(DxfStructuralDiagnosticCode.UnknownRecord, DxfStructuralDiagnosticSeverity.Warning,
                "Raw record retained; typed behavior is unsupported in 03B.", record.Source);
        }
        private int ContentStart(DxfSectionRecord section)
        {
            var i = section.Source.StartPairIndex + 1;
            while (Comment(i)) i++;
            return i + 1;
        }
        private bool Is(int i, string marker) => i < P.Count && P[i].GroupCode == 0 && P[i].RawTextValue == marker;
        private bool Comment(int i)
        {
            if (i >= P.Count || P[i].GroupCode != 999) return false;
            comments.Add(i); return true;
        }
        private DxfSourceSpan Span(int start, int end) => new(start, end, P[start].CodeLineNumber, P[end].ValueLineNumber);
        private void Guard(bool exceeded, string dimension, int? index = null)
        {
            token.ThrowIfCancellationRequested();
            if (!exceeded) return;
            Terminal(DxfStructuralDiagnosticCode.StructuralLimitExceeded, "Structural budget exceeded: " + dimension + ".", index);
            throw new Stop(DxfStructuralStatus.ResourceLimitExceeded);
        }
        private void Fail(DxfStructuralDiagnosticCode code, string message, int? index = null)
        {
            Add(code, DxfStructuralDiagnosticSeverity.Error, message, index.HasValue ? Span(index.Value, index.Value) : null);
            throw new Stop(DxfStructuralStatus.InvalidInput);
        }
        private void Add(DxfStructuralDiagnosticCode code, DxfStructuralDiagnosticSeverity severity,
            string message, DxfSourceSpan? source = null, int count = 1)
        {
            token.ThrowIfCancellationRequested();
            if (diagnostics.Count >= limits.MaxStoredDiagnostics - 1)
            {
                Terminal(DxfStructuralDiagnosticCode.DiagnosticLimitExceeded, "Diagnostic budget exhausted; structural result is incomplete.");
                throw new Stop(DxfStructuralStatus.ResourceLimitExceeded);
            }
            diagnostics.Add(new(code, severity, message, source, count));
            if (severity == DxfStructuralDiagnosticSeverity.Error)
            {
                errorCount++;
                // Bad table scalars do not invalidate safely established section/table boundaries.
                if (code == DxfStructuralDiagnosticCode.InvalidTableValue) partial = true;
                else invalid = true;
            }
        }
        private void Terminal(DxfStructuralDiagnosticCode code, string message, int? index = null) =>
            diagnostics.Add(new(code, DxfStructuralDiagnosticSeverity.Error, message, index.HasValue ? Span(index.Value, index.Value) : null));
        private DxfStructuralReadResult Result(DxfStructuralStatus status, bool checkCancellation = true)
        {
            if (checkCancellation) token.ThrowIfCancellationRequested();
            var result = new DxfStructuralReadResult(evidence, status, version, sections, header, tables, records, diagnostics, structureValidated,
                checkCancellation ? token : CancellationToken.None);
            if (checkCancellation) token.ThrowIfCancellationRequested();
            return result;
        }
        private sealed class Stop(DxfStructuralStatus status) : Exception { public DxfStructuralStatus Status { get; } = status; }
    }
}
