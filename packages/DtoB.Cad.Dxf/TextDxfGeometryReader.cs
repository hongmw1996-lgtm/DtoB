using System.Globalization;
using DtoB.Cad.Raw;

namespace DtoB.Cad.Dxf;

/// <summary>Maps immutable03B evidence into native factual geometry. No file access,
/// normalization, cleanup, identity graph, occurrence placement or rendering.</summary>
public sealed partial class TextDxfGeometryReader
{
    private readonly DxfGeometryLimits limits;
    public TextDxfGeometryReader(DxfGeometryLimits? limits = null)
    { this.limits = limits ?? new(); this.limits.Validate(); }
    public DxfGeometryReadResult Read(DxfStructuralReadResult evidence, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        return new Operation(evidence, limits, cancellationToken).Run();
    }

    private sealed partial class Operation(DxfStructuralReadResult evidence, DxfGeometryLimits limits, CancellationToken token)
    {
        private IReadOnlyList<DxfRawPair> P => evidence.LexicalEvidence.Pairs;
        private DxfGeometryLimits Limits => limits;
        private CancellationToken Token => token;
        private readonly List<RawGeometryRecord> geometry = [];
        private readonly List<DxfGeometryDiagnostic> diagnostics = [];
        private int totalVertices;
        private static readonly HashSet<string> UnsupportedFamilies = new(StringComparer.Ordinal)
            { "ELLIPSE", "SPLINE", "HATCH", "3DSOLID", "REGION", "MESH", "HELIX", "ACAD_PROXY_ENTITY" };

        public DxfGeometryReadResult Run()
        {
            try
            {
                token.ThrowIfCancellationRequested();
                if (!evidence.StructureValidated || evidence.VersionAcceptance != DxfStructuralVersionAcceptance.Supported ||
                    evidence.Status is not (DxfStructuralStatus.Succeeded or DxfStructuralStatus.Partial))
                {
                    Add(new(DxfGeometryDiagnosticCode.StructuralInputRejected, DxfStructuralDiagnosticSeverity.Error,
                        "Geometry mapping requires validated AC1032 structural evidence."));
                    return Result(evidence.Status switch
                    {
                        DxfStructuralStatus.Canceled => DxfGeometryStatus.Canceled,
                        DxfStructuralStatus.IoFailure => DxfGeometryStatus.IoFailure,
                        DxfStructuralStatus.UnsupportedVersion => DxfGeometryStatus.UnsupportedVersion,
                        DxfStructuralStatus.UnsupportedInput => DxfGeometryStatus.UnsupportedInput,
                        DxfStructuralStatus.ResourceLimitExceeded => DxfGeometryStatus.ResourceLimitExceeded,
                        _ => DxfGeometryStatus.InvalidInput
                    }, false);
                }
                Guard(P.Count > limits.MaxSourcePairs, "source pairs");
                Guard(evidence.Records.Count > limits.MaxStructuralRecords, "structural records");
                for (var i = 0; i < evidence.Records.Count; i++)
                {
                    token.ThrowIfCancellationRequested(); var record = evidence.Records[i];
                    if (record.Disposition != DxfDisposition.DeferredTo03C &&
                        !(record.Scope is "BLOCKS" or "ENTITIES" && UnsupportedFamilies.Contains(record.RecordType))) continue;
                    Guard(geometry.Count >= limits.MaxGeometryRecords, "geometry records", record.Source);
                    if (record.Source.StartPairIndex < 0 || record.Source.EndPairIndex >= P.Count ||
                        record.Source.EndPairIndex < record.Source.StartPairIndex ||
                        P[record.Source.StartPairIndex].GroupCode != 0 || P[record.Source.StartPairIndex].RawTextValue != record.RecordType)
                    {
                        Add(new(DxfGeometryDiagnosticCode.InvalidSourceRecord, DxfStructuralDiagnosticSeverity.Error,
                            "Structural record range/type does not match lexical evidence.", record.Source));
                        return Result(DxfGeometryStatus.InvalidInput, false);
                    }
                    Guard(record.Source.EndPairIndex - record.Source.StartPairIndex + 1 > limits.MaxFieldsPerRecord, "geometry record fields", record.Source);
                    geometry.Add(new Mapping(this, i, record).Map());
                }
                var complete = geometry.All(r => r.Disposition == DxfDisposition.Supported);
                var mappedIndices = geometry.Select(g => g.StructuralRecordIndex).ToHashSet();
                var effectiveIncomplete = false;
                for (var i = 0; i < evidence.Records.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    if (!mappedIndices.Contains(i) && evidence.Records[i].Disposition is DxfDisposition.Unsupported or DxfDisposition.Invalid or DxfDisposition.DeferredTo03C or DxfDisposition.DeferredTo03D)
                        effectiveIncomplete = true;
                }
                return Result(!complete || effectiveIncomplete ? DxfGeometryStatus.Partial : DxfGeometryStatus.Succeeded, complete);
            }
            catch (Stop) { return Result(DxfGeometryStatus.ResourceLimitExceeded, false, false); }
            catch (OperationCanceledException)
            {
                Terminal(DxfGeometryDiagnosticCode.Canceled, "Geometry mapping canceled.");
                return Result(DxfGeometryStatus.Canceled, false, false);
            }
        }

        private DxfGeometryReadResult Result(DxfGeometryStatus status, bool complete, bool checkToken = true)
        {
            if (checkToken) token.ThrowIfCancellationRequested();
            var result = new DxfGeometryReadResult(evidence, status, geometry, diagnostics, complete,
                checkToken ? token : CancellationToken.None, limits.MaxStructuralRecords);
            if (checkToken) token.ThrowIfCancellationRequested(); return result;
        }
        private void Guard(bool exceeded, string dimension, DxfSourceSpan? source = null)
        {
            token.ThrowIfCancellationRequested(); if (!exceeded) return;
            Terminal(DxfGeometryDiagnosticCode.GeometryLimitExceeded, "Geometry budget exceeded: " + dimension + ".", source);
            throw new Stop();
        }
        private void Add(DxfGeometryDiagnostic diagnostic)
        {
            token.ThrowIfCancellationRequested();
            if (diagnostics.Count >= limits.MaxStoredDiagnostics - 1)
            {
                Terminal(DxfGeometryDiagnosticCode.DiagnosticLimitExceeded, "Geometry diagnostic budget exhausted; result is incomplete.");
                throw new Stop();
            }
            diagnostics.Add(diagnostic);
        }
        private void Terminal(DxfGeometryDiagnosticCode code, string message, DxfSourceSpan? source = null) =>
            diagnostics.Add(new(code, DxfStructuralDiagnosticSeverity.Error, message, source));
        private DxfSourceSpan Span(int start, int end) => new(start, end, P[start].CodeLineNumber, P[end].ValueLineNumber);
        private sealed class Stop : Exception;

        private sealed partial class Mapping(Operation owner, int index, DxfStructuralRecord source)
        {
            private readonly List<DxfNamedFact> fields = [];
            private readonly HashSet<int> mapped = [];
            private readonly List<DxfGeometryDiagnostic> localDiagnostics = [];
            private readonly List<int> common = [];
            private readonly List<int> native = [];
            private bool invalid;
            private IReadOnlyList<DxfRawPair> P => owner.P;
            private CancellationToken Token => owner.Token;

            public RawGeometryRecord Map()
            {
                if (source.RecordType is "POLYLINE" or "VERTEX")
                {
                    Warn(DxfGeometryDiagnosticCode.LegacyPolylineGated, "Legacy POLYLINE/VERTEX sequence/subtype mapping remains gated.", source.Source);
                    return Finish(DxfDisposition.DeferredTo03C, null, null, null, null);
                }
                if (UnsupportedFamilies.Contains(source.RecordType))
                {
                    Warn(DxfGeometryDiagnosticCode.UnsupportedGeometry, "Unsupported geometry retained without approximation.", source.Source);
                    return Finish(DxfDisposition.Unsupported, null, null, null, null);
                }
                CollectContext();
                var layer = Scalar("Layer", 8, DxfFactKind.Text, false, common);
                var thickness = Scalar("Thickness", 39);
                var extrusion = Point("Extrusion", 210, 220, 230, false);
                RawGeometryPayload? payload = source.RecordType switch
                {
                    "LINE" => new RawLine(Point("Start", 10, 20, 30), Point("End", 11, 21, 31)),
                    "ARC" => new RawArc(Point("Center", 10, 20, 30), Radius(), Scalar("StartAngleDegrees", 50, required: true), Scalar("EndAngleDegrees", 51, required: true)),
                    "CIRCLE" => new RawCircle(Point("Center", 10, 20, 30), Radius()),
                    "POINT" => new RawPoint(Point("Position", 10, 20, 30), Scalar("AngleDegrees", 50)),
                    "SOLID" => new RawSolid(Point("First", 10, 20, 30), Point("Second", 11, 21, 31), Point("Third", 12, 22, 32), Point("Fourth", 13, 23, 33)),
                    "LWPOLYLINE" => Polyline(),
                    _ => null
                };
                if (payload == null)
                    Warn(DxfGeometryDiagnosticCode.UnsupportedGeometry, "Geometry record has no implemented raw profile.", source.Source);
                return Finish(payload == null ? DxfDisposition.Unsupported : invalid ? DxfDisposition.Invalid : DxfDisposition.Supported,
                    layer, thickness, extrusion, payload);
            }

            private void CollectContext()
            {
                var markers = source.ContextMarkers.ToDictionary(m => m.PairIndex);
                string? subclass = null; string? application = null; var control = "";
                for (var i = source.Source.StartPairIndex; i <= source.Source.EndPairIndex; i++)
                {
                    Token.ThrowIfCancellationRequested();
                    if (markers.TryGetValue(i, out var marker))
                    { subclass = marker.Subclass; application = marker.Application; control = marker.ControlPath; continue; }
                    if (application != null || control.Length != 0 || P[i].GroupCode is 999 or 100 or 102 or 1001 or 1002) continue;
                    if (subclass is null or "AcDbEntity") common.Add(i);
                    var allowed = subclass == null || source.RecordType switch
                    {
                        "LINE" => subclass == "AcDbLine",
                        "LWPOLYLINE" => subclass == "AcDbPolyline",
                        "ARC" => subclass is "AcDbCircle" or "AcDbArc",
                        "CIRCLE" => subclass == "AcDbCircle",
                        "POINT" => subclass == "AcDbPoint",
                        "SOLID" => subclass == "AcDbTrace",
                        _ => false
                    };
                    // ARC center/radius are Circle-subclass facts; angles are Arc-subclass facts.
                    if (source.RecordType == "ARC" && subclass != null)
                        allowed = P[i].GroupCode is 50 or 51 ? subclass == "AcDbArc" : subclass == "AcDbCircle";
                    if (allowed) native.Add(i);
                    else if (P[i].GroupCode is >= 10 and <= 13 or >= 20 and <= 23 or >= 30 and <= 33 or 38 or 39 or >= 40 and <= 43 or 50 or 51 or 70 or 90 or 91 or 210 or 220 or 230)
                        Error(DxfGeometryDiagnosticCode.ForeignFieldContext, "Geometry field is outside its supported entity/subclass context; retained unmapped.", owner.Span(i, i));
                }
            }

            private DxfSourceFact Scalar(string name, int code, DxfFactKind kind = DxfFactKind.Real,
                bool required = false, IReadOnlyList<int>? candidates = null) =>
                Fact(name, (candidates ?? native).Where(i => P[i].GroupCode == code).ToArray(), kind, required);

            private DxfSourceFact Fact(string name, IReadOnlyList<int> indices, DxfFactKind kind, bool required)
            {
                Token.ThrowIfCancellationRequested(); foreach (var i in indices) { Token.ThrowIfCancellationRequested(); mapped.Add(i); }
                var span = indices.Count == 0 ? required ? source.Source : null : owner.Span(indices[0], indices[^1]);
                string? text = null; int? integer = null; double? real = null; var valid = true;
                if (indices.Count == 0)
                {
                    valid = !required;
                    if (required) Error(DxfGeometryDiagnosticCode.MissingRequiredField, "Required raw geometry field is absent: " + name + ".", source.Source);
                }
                else if (indices.Count != 1)
                { valid = false; Error(DxfGeometryDiagnosticCode.DuplicateField, "Raw singleton geometry field has multiple occurrences: " + name + ".", span!); }
                else
                {
                    var raw = P[indices[0]].RawTextValue;
                    if (kind == DxfFactKind.Text) text = raw;
                    else if (kind == DxfFactKind.Integer)
                    { valid = int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n); if (valid) integer = n; }
                    else
                    { valid = double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) && double.IsFinite(n); if (valid) real = n; }
                    if (!valid) Error(DxfGeometryDiagnosticCode.InvalidNumericValue, "Raw geometry scalar has invalid invariant syntax or is nonfinite: " + name + ".", span!);
                }
                var fact = new DxfSourceFact(kind, indices.Count == 0 ? DxfFactPresence.Absent : DxfFactPresence.Present,
                    !valid ? DxfFactValidity.Invalid : indices.Count == 0 ? DxfFactValidity.NotApplicable : DxfFactValidity.Valid,
                    text, integer, real, indices, span);
                fields.Add(new(name, fact)); return fact;
            }

            private RawCoordinateFacts Point(string name, int x, int y, int z, bool requiredXY = true)
            {
                if (requiredXY)
                {
                    owner.Guard(owner.totalVertices >= owner.Limits.MaxTotalVertices, "total geometry coordinate slots", source.Source);
                    owner.totalVertices++;
                }
                return new(Scalar(name + ".X", x, required: requiredXY), Scalar(name + ".Y", y, required: requiredXY), Scalar(name + ".Z", z));
            }
            private DxfSourceFact Radius()
            {
                var fact = Scalar("Radius", 40, required: true);
                if (fact.Real is <= 0) return Invalidate("Radius", fact, DxfGeometryDiagnosticCode.InvalidRadius,
                    "Source radius must be strictly positive; no absolute-value or repair.");
                return fact;
            }
            private DxfSourceFact Invalidate(string name, DxfSourceFact fact, DxfGeometryDiagnosticCode code, string message)
            {
                Error(code, message, fact.Source ?? source.Source);
                var replacement = new DxfSourceFact(fact.Kind, fact.Presence, DxfFactValidity.Invalid,
                    null, null, null, fact.SourcePairIndices, fact.Source);
                var i = fields.FindIndex(f => f.Name == name); fields[i] = new(name, replacement); return replacement;
            }
            private void Error(DxfGeometryDiagnosticCode code, string message, DxfSourceSpan span)
            { invalid = true; Emit(new(code, DxfStructuralDiagnosticSeverity.Error, message, span)); }
            private void Warn(DxfGeometryDiagnosticCode code, string message, DxfSourceSpan span) => Emit(new(code, DxfStructuralDiagnosticSeverity.Warning, message, span));
            private void Emit(DxfGeometryDiagnostic diagnostic) { owner.Add(diagnostic); localDiagnostics.Add(diagnostic); }
            private RawGeometryRecord Finish(DxfDisposition disposition, DxfSourceFact? layer,
                DxfSourceFact? thickness, RawCoordinateFacts? extrusion, RawGeometryPayload? payload)
            {
                var unmapped = new List<int>();
                for (var i = source.Source.StartPairIndex; i <= source.Source.EndPairIndex; i++)
                { Token.ThrowIfCancellationRequested(); if (!mapped.Contains(i)) unmapped.Add(i); }
                Token.ThrowIfCancellationRequested();
                return new(index, source, disposition, layer, thickness, extrusion, payload, fields, unmapped, localDiagnostics);
            }
        }
    }
}
