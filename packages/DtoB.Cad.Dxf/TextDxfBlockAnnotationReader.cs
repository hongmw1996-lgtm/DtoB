using System.Globalization;
using DtoB.Cad.Raw;

namespace DtoB.Cad.Dxf;

/// <summary>Source facts and containment only. No occurrence expansion, matrix,
/// geometry remapping, measurement, name/handle graph, rendering or source I/O.</summary>
public sealed partial class TextDxfBlockAnnotationReader
{
    private readonly DxfBlockAnnotationLimits limits;
    public TextDxfBlockAnnotationReader(DxfBlockAnnotationLimits? limits = null)
    { this.limits = limits ?? new(); this.limits.Validate(); }
    public DxfBlockAnnotationReadResult Read(DxfGeometryReadResult evidence, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evidence); return new Operation(evidence, limits, cancellationToken).Run();
    }
    private sealed partial class Operation(DxfGeometryReadResult evidence, DxfBlockAnnotationLimits limits, CancellationToken token)
    {
        private DxfStructuralReadResult S => evidence.StructuralEvidence;
        private IReadOnlyList<DxfRawPair> P => S.LexicalEvidence.Pairs;
        private readonly List<RawBlockAnnotationRecord> records = [];
        private readonly List<RawBlockDefinition> blocks = [];
        private readonly List<RawAttributeSequence> sequences = [];
        private readonly List<DxfBlockAnnotationDiagnostic> diagnostics = [];
        private readonly HashSet<int> invalidRecords = [];
        private readonly Dictionary<int, int> recordLookup = [];
        private int insertCount, annotationCount, textCharacters;
        private bool brokenSequence;
        private CancellationToken Token => token;
        private DxfBlockAnnotationLimits Limits => limits;
        public DxfBlockAnnotationReadResult Run()
        {
            try
            {
                token.ThrowIfCancellationRequested();
                if (evidence.Status is not (DxfGeometryStatus.Succeeded or DxfGeometryStatus.Partial) || !S.StructureValidated)
                {
                    Emit(DxfBlockAnnotationDiagnosticCode.InputRejected, DxfStructuralDiagnosticSeverity.Error, "03D requires validated preceding phase evidence.");
                    return Result(Enum.TryParse<DxfBlockAnnotationStatus>(evidence.Status.ToString(), out var status) && status is not (DxfBlockAnnotationStatus.Succeeded or DxfBlockAnnotationStatus.Partial)
                        ? status : DxfBlockAnnotationStatus.InvalidInput, false);
                }
                Guard(S.Records.Count > limits.MaxStructuralRecords || P.Count > limits.MaxSourcePairs, "input evidence");
                for (var i = 0; i < S.Records.Count; i++)
                {
                    token.ThrowIfCancellationRequested(); var r = S.Records[i];
                    if (r.Disposition != DxfDisposition.DeferredTo03D) continue;
                    Guard(records.Count >= limits.MaxMappedRecords, "mapped03D records", r.Source);
                    Guard(r.Source.EndPairIndex - r.Source.StartPairIndex + 1 > limits.MaxFieldsPerRecord, "record fields", r.Source);
                    if (r.Source.StartPairIndex < 0 || r.Source.EndPairIndex >= P.Count || r.Source.EndPairIndex < r.Source.StartPairIndex ||
                        P[r.Source.StartPairIndex].GroupCode != 0 || P[r.Source.StartPairIndex].RawTextValue != r.RecordType)
                    {
                        Emit(DxfBlockAnnotationDiagnosticCode.InvalidSourceRecord, DxfStructuralDiagnosticSeverity.Error, "Record range/type does not match lexical evidence.", r.Source);
                        return Result(DxfBlockAnnotationStatus.InvalidInput, false);
                    }
                    if (r.RecordType == "INSERT") Guard(++insertCount > limits.MaxInserts, "INSERT count", r.Source);
                    if (r.RecordType is "TEXT" or "MTEXT" or "DIMENSION" or "ATTRIB" or "ATTDEF")
                    {
                        Guard(++annotationCount > limits.MaxAnnotations, "annotation count", r.Source);
                        for (var j = r.Source.StartPairIndex; j <= r.Source.EndPairIndex; j++)
                        {
                            token.ThrowIfCancellationRequested();
                            if (P[j].GroupCode is not (1 or 3 or 7 or 8)) continue;
                            Guard(P[j].RawTextValue.Length > limits.MaxRawAnnotationCharacters - textCharacters, "raw annotation text characters", Span(j, j));
                            textCharacters += P[j].RawTextValue.Length;
                        }
                    }
                    recordLookup.Add(i, records.Count); records.Add(new Mapping(this, i, r).Map());
                }
                BuildBlocks(); BuildAttributeSequences();
                for (var i = 0; i < records.Count; i++)
                {
                    token.ThrowIfCancellationRequested(); var r = records[i];
                    if (invalidRecords.Contains(r.StructuralRecordIndex)) records[i] = new(r.StructuralRecordIndex, r.SourceRecord,
                        DxfDisposition.Invalid, r.Layer, r.Payload, r.Fields, r.UnmappedPairIndices);
                }
                var complete = records.All(r => r.Disposition == DxfDisposition.Supported) && !brokenSequence;
                var replacement = evidence.Geometry.ToDictionary(r => r.StructuralRecordIndex, r => r.Disposition);
                foreach (var r in records) { token.ThrowIfCancellationRequested(); replacement[r.StructuralRecordIndex] = r.Disposition; }
                var partial = false;
                for (var i = 0; i < S.Records.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    if (replacement.GetValueOrDefault(i, S.Records[i].Disposition) is DxfDisposition.Invalid or DxfDisposition.Unsupported or DxfDisposition.DeferredTo03C or DxfDisposition.DeferredTo03D) partial = true;
                }
                return Result(brokenSequence ? DxfBlockAnnotationStatus.InvalidInput : partial ? DxfBlockAnnotationStatus.Partial : DxfBlockAnnotationStatus.Succeeded, complete);
            }
            catch (Stop) { return Result(DxfBlockAnnotationStatus.ResourceLimitExceeded, false, false); }
            catch (OperationCanceledException)
            { Terminal(DxfBlockAnnotationDiagnosticCode.Canceled, "03D mapping canceled."); return Result(DxfBlockAnnotationStatus.Canceled, false, false); }
        }
        private DxfBlockAnnotationReadResult Result(DxfBlockAnnotationStatus status, bool complete, bool checkToken = true)
        {
            if (checkToken) token.ThrowIfCancellationRequested();
            var result = new DxfBlockAnnotationReadResult(evidence, status, records, blocks, sequences, diagnostics,
                complete, checkToken ? token : CancellationToken.None, limits.MaxStructuralRecords);
            if (checkToken) token.ThrowIfCancellationRequested(); return result;
        }
        private DxfSourceSpan Span(int start, int end) => new(start, end, P[start].CodeLineNumber, P[end].ValueLineNumber);
        private void Guard(bool exceeded, string dimension, DxfSourceSpan? source = null)
        {
            token.ThrowIfCancellationRequested(); if (!exceeded) return;
            Terminal(DxfBlockAnnotationDiagnosticCode.ResourceLimitExceeded, "03D budget exceeded: " + dimension + ".", source); throw new Stop();
        }
        private void Emit(DxfBlockAnnotationDiagnosticCode code, DxfStructuralDiagnosticSeverity severity, string message, DxfSourceSpan? source = null)
        {
            token.ThrowIfCancellationRequested();
            if (diagnostics.Count >= limits.MaxStoredDiagnostics - 1)
            { Terminal(DxfBlockAnnotationDiagnosticCode.DiagnosticLimitExceeded, "03D diagnostic budget exhausted; result incomplete."); throw new Stop(); }
            diagnostics.Add(new(code, severity, message, source));
        }
        private void Terminal(DxfBlockAnnotationDiagnosticCode code, string message, DxfSourceSpan? source = null) => diagnostics.Add(new(code, DxfStructuralDiagnosticSeverity.Error, message, source));
        private sealed class Stop : Exception;

        private sealed class Mapping(Operation owner, int index, DxfStructuralRecord source)
        {
            private readonly List<DxfNamedFact> fields = [];
            private readonly HashSet<int> mapped = [];
            private readonly List<(int Index, string? Subclass)> eligible = [];
            private bool invalid;
            private IReadOnlyList<DxfRawPair> P => owner.P;
            private CancellationToken Token => owner.Token;
            public RawBlockAnnotationRecord Map()
            {
                if (source.RecordType == "SEQEND") return Finish(DxfDisposition.DeferredTo03D, null, null);
                var markers = source.ContextMarkers.ToDictionary(m => m.PairIndex);
                string? subclass = null; string? app = null; var control = "";
                for (var i = source.Source.StartPairIndex; i <= source.Source.EndPairIndex; i++)
                {
                    Token.ThrowIfCancellationRequested();
                    if (markers.TryGetValue(i, out var m)) { subclass = m.Subclass; app = m.Application; control = m.ControlPath; continue; }
                    if (app == null && control.Length == 0 && P[i].GroupCode is not (999 or 100 or 102 or 1001 or 1002)) eligible.Add((i, subclass));
                }
                var layer = F("Layer", 8, DxfFactKind.Text, contexts: ["AcDbEntity"]);
                RawBlockAnnotationPayload? payload = source.RecordType switch
                {
                    "BLOCK" => new RawBlockBegin(F("Name", 2, DxfFactKind.Text, true, ["AcDbBlockBegin"]),
                        F("AlternateName", 3, DxfFactKind.Text, contexts: ["AcDbBlockBegin"]), Point("BasePoint", 10, true, ["AcDbBlockBegin"]),
                        F("Flags", 70, DxfFactKind.Integer, contexts: ["AcDbBlockBegin"]), F("XrefPath", 1, DxfFactKind.Text, contexts: ["AcDbBlockBegin"]),
                        F("Description", 4, DxfFactKind.Text, contexts: ["AcDbBlockBegin"])),
                    "ENDBLK" => new RawBlockEnd(),
                    "INSERT" => Insert(),
                    "TEXT" => Text(),
                    "MTEXT" => MText(),
                    "DIMENSION" => Dimension(),
                    "ATTRIB" => new RawAttrib(Text(), F("Tag", 2, DxfFactKind.Text, true, ["AcDbAttribute"]), F("AttributeFlags", 70, DxfFactKind.Integer, contexts: ["AcDbAttribute"])),
                    "ATTDEF" => new RawAttDef(Text(), F("Tag", 2, DxfFactKind.Text, true, ["AcDbAttributeDefinition"]),
                        F("Prompt", 3, DxfFactKind.Text, contexts: ["AcDbAttributeDefinition"]), F("AttributeFlags", 70, DxfFactKind.Integer, contexts: ["AcDbAttributeDefinition"])),
                    _ => null
                };
                var unsupported = payload is RawInsert { OccurrenceExpansionUnsupported: true } or RawDimension { SubtypeProfileSupported: false };
                return Finish(invalid ? DxfDisposition.Invalid : unsupported || payload == null ? DxfDisposition.Unsupported : DxfDisposition.Supported, layer, payload);
            }
            private DxfSourceFact F(string name, int code, DxfFactKind kind = DxfFactKind.Real,
                bool required = false, string[]? contexts = null)
            {
                Token.ThrowIfCancellationRequested(); var indices = new List<int>();
                foreach (var e in eligible)
                {
                    Token.ThrowIfCancellationRequested(); if (P[e.Index].GroupCode != code) continue;
                    if (e.Subclass == null || contexts == null || contexts.Contains(e.Subclass)) indices.Add(e.Index);
                    else owner.Emit(DxfBlockAnnotationDiagnosticCode.ForeignFieldContext, DxfStructuralDiagnosticSeverity.Warning,
                        "Field outside supported03D subclass context retained unmapped: " + name + ".", owner.Span(e.Index, e.Index));
                }
                return Fact(name, indices, kind, required);
            }
            private DxfSourceFact Fact(string name, IReadOnlyList<int> indices, DxfFactKind kind, bool required, bool forceInvalid = false)
            {
                Token.ThrowIfCancellationRequested(); foreach (var i in indices) { Token.ThrowIfCancellationRequested(); mapped.Add(i); }
                var span = indices.Count == 0 ? required ? source.Source : null : owner.Span(indices[0], indices[^1]);
                var valid = !forceInvalid; string? text = null; int? integer = null; double? real = null;
                if (indices.Count == 0)
                { if (required) { valid = false; Error(DxfBlockAnnotationDiagnosticCode.MissingRequiredField, "Required03D field absent: " + name + ".", source.Source); } }
                else if (indices.Count > 1)
                { valid = false; Error(DxfBlockAnnotationDiagnosticCode.DuplicateField, "Singleton03D field has multiple source occurrences: " + name + ".", span!); }
                else
                {
                    var raw = P[indices[0]].RawTextValue;
                    if (kind == DxfFactKind.Text)
                    {
                        text = raw;
                    }
                    else if (kind == DxfFactKind.Integer)
                    { var parsed = int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n); valid &= parsed; if (parsed) integer = n; }
                    else
                    { var parsed = double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) && double.IsFinite(n); valid &= parsed; if (parsed) real = n; }
                    if (!valid && kind != DxfFactKind.Text) Error(DxfBlockAnnotationDiagnosticCode.InvalidNumericValue, "Invalid/nonfinite invariant03D numeric field: " + name + ".", span!);
                }
                var f = new DxfSourceFact(kind, indices.Count == 0 ? DxfFactPresence.Absent : DxfFactPresence.Present,
                    !valid ? DxfFactValidity.Invalid : indices.Count == 0 ? DxfFactValidity.NotApplicable : DxfFactValidity.Valid,
                    text, valid ? integer : null, valid ? real : null, indices, span);
                fields.Add(new(name, f)); return f;
            }
            private RawCoordinateFacts Point(string name, int x, bool required = false, string[]? contexts = null) =>
                new(F(name + ".X", x, required: required, contexts: contexts), F(name + ".Y", x + 10, required: required, contexts: contexts), F(name + ".Z", x + 20, contexts: contexts));
            private RawInsert Insert()
            {
                string[] c = ["AcDbBlockReference", "AcDbMInsertBlock"];
                var target = F("TargetName", 2, DxfFactKind.Text, true, c); var insertion = Point("Insertion", 10, true, c);
                // Scale uses41/42/43, not coordinate offsets.
                var scale = new RawCoordinateFacts(F("Scale.X", 41, contexts: c), F("Scale.Y", 42, contexts: c), F("Scale.Z", 43, contexts: c));
                var rotation = F("RotationDegrees", 50, contexts: c); var columns = F("Columns", 70, DxfFactKind.Integer, contexts: c);
                var rows = F("Rows", 71, DxfFactKind.Integer, contexts: c); var colSpacing = F("ColumnSpacing", 44, contexts: c); var rowSpacing = F("RowSpacing", 45, contexts: c);
                var follow = F("AttributesFollow", 66, DxfFactKind.Integer, contexts: c); var normal = Point("Extrusion", 210, contexts: c);
                var array = columns.Integer > 1 || rows.Integer > 1 || source.ContextMarkers.Any(m => m.GroupCode == 100 && m.Subclass == "AcDbMInsertBlock" && m.Application == null && m.ControlPath.Length == 0);
                if (array) owner.Emit(DxfBlockAnnotationDiagnosticCode.UnsupportedOccurrenceExpansion, DxfStructuralDiagnosticSeverity.Warning,
                    "Array INSERT retained; occurrence expansion is unsupported. No cells or transformed children.", source.Source);
                return new(target, insertion, scale, rotation, columns, rows, colSpacing, rowSpacing, follow, normal, array);
            }
            private RawText Text()
            {
                string[] c = ["AcDbText"];
                return new(F("Content", 1, DxfFactKind.Text, true, c), Point("Insertion", 10, true, c), Point("Alignment", 11, contexts: c),
                    F("Height", 40, required: true, contexts: c), F("RotationDegrees", 50, contexts: c), F("Style", 7, DxfFactKind.Text, contexts: c),
                    F("HorizontalJustification", 72, DxfFactKind.Integer, contexts: c), F("VerticalJustification", 73, DxfFactKind.Integer, contexts: c),
                    F("Thickness", 39, contexts: c), Point("Extrusion", 210, contexts: c));
            }
            private RawMText MText()
            {
                string[] c = ["AcDbMText"]; var chunks = new List<RawTextChunk>(); var finalSeen = false;
                var candidates = eligible.Where(e => P[e.Index].GroupCode is 1 or 3).ToArray();
                var finalCount = candidates.Count(e => P[e.Index].GroupCode == 1 && (e.Subclass == null || c.Contains(e.Subclass)));
                foreach (var e in candidates)
                {
                    Token.ThrowIfCancellationRequested();
                    if (e.Subclass != null && !c.Contains(e.Subclass)) { Error(DxfBlockAnnotationDiagnosticCode.ForeignFieldContext, "MTEXT chunk in foreign subclass retained unmapped.", owner.Span(e.Index, e.Index)); continue; }
                    owner.Guard(chunks.Count >= owner.Limits.MaxMTextChunks, "MTEXT chunks", source.Source);
                    var code = P[e.Index].GroupCode; var bad = code == 1 && finalCount > 1 || code == 3 && finalSeen;
                    if (bad) Error(DxfBlockAnnotationDiagnosticCode.InvalidMTextChunks, "MTEXT final chunk duplicate or continuation after final chunk.", owner.Span(e.Index, e.Index));
                    if (code == 1) finalSeen = true;
                    chunks.Add(new(code, Fact("Chunks[" + chunks.Count + "]", [e.Index], DxfFactKind.Text, true, bad)));
                }
                if (!finalSeen) Error(DxfBlockAnnotationDiagnosticCode.MissingRequiredField, "MTEXT final code1 chunk absent.", source.Source);
                return new(chunks, Point("Insertion", 10, true, c), Point("Direction", 11, contexts: c), F("Height", 40, required: true, contexts: c),
                    F("RectangleWidth", 41, contexts: c), F("Attachment", 71, DxfFactKind.Integer, contexts: c), F("DrawingDirection", 72, DxfFactKind.Integer, contexts: c),
                    F("SpacingStyle", 73, DxfFactKind.Integer, contexts: c), F("SpacingFactor", 44, contexts: c), F("Style", 7, DxfFactKind.Text, contexts: c), Point("Extrusion", 210, contexts: c));
            }
            private RawDimension Dimension()
            {
                string[] common = ["AcDbDimension"]; string[] linear = ["AcDbAlignedDimension", "AcDbRotatedDimension"];
                var flags = F("TypeFlags", 70, DxfFactKind.Integer, true, common);
                var subtype = flags.Integer.HasValue ? flags.Integer.Value & 7 : -1;
                var supported = subtype is 0 or 1;
                if (!supported) owner.Emit(DxfBlockAnnotationDiagnosticCode.UnsupportedDimensionSubtype, DxfStructuralDiagnosticSeverity.Warning,
                    "DIMENSION common facts retained; only fixture-verified linear/aligned subtype fields mapped.", source.Source);
                return new(F("BlockName", 2, DxfFactKind.Text, contexts: common), F("Style", 3, DxfFactKind.Text, contexts: common), F("TextOverride", 1, DxfFactKind.Text, contexts: common), flags,
                    Point("DefinitionPoint", 10, true, common), Point("TextMidpoint", 11, contexts: common),
                    supported ? Point("FirstExtensionPoint", 13, true, linear) : null, supported ? Point("SecondExtensionPoint", 14, true, linear) : null,
                    supported ? F("RotationDegrees", 50, contexts: linear) : null, F("SourceMeasurement", 42, contexts: common), Point("Extrusion", 210, contexts: common), supported);
            }
            private void Error(DxfBlockAnnotationDiagnosticCode code, string message, DxfSourceSpan sourceSpan)
            { invalid = true; owner.Emit(code, DxfStructuralDiagnosticSeverity.Error, message, sourceSpan); }
            private RawBlockAnnotationRecord Finish(DxfDisposition disposition, DxfSourceFact? layer, RawBlockAnnotationPayload? payload)
            {
                var unmapped = new List<int>(); for (var i = source.Source.StartPairIndex; i <= source.Source.EndPairIndex; i++)
                { Token.ThrowIfCancellationRequested(); if (!mapped.Contains(i)) unmapped.Add(i); }
                return new(index, source, disposition, layer, payload, fields, unmapped);
            }
        }
    }
}
