using DtoB.Cad.Raw;

namespace DtoB.Cad.Dxf;

/// <summary>Pure same-document source validation. No file/project access, name resolution or geometry interpretation.</summary>
public sealed partial class TextDxfIdentityGraphReader
{
    private readonly DxfIdentityGraphLimits limits;
    public TextDxfIdentityGraphReader(DxfIdentityGraphLimits? limits = null)
    { this.limits = limits ?? new(); this.limits.Validate(); }
    public DxfIdentityGraphReadResult Read(DxfBlockAnnotationReadResult evidence, DxfDocumentProvenance? provenance = null, CancellationToken cancellationToken = default)
    { ArgumentNullException.ThrowIfNull(evidence); return new Operation(evidence, provenance, limits, cancellationToken).Run(); }
    private sealed partial class Operation(DxfBlockAnnotationReadResult evidence, DxfDocumentProvenance? document, DxfIdentityGraphLimits limits, CancellationToken token)
    {
        private DxfStructuralReadResult S => evidence.StructuralEvidence;
        private IReadOnlyList<DxfRawPair> P => evidence.LexicalEvidence.Pairs;
        private readonly List<(DxfGraphRecordLocator Locator, DxfStructuralRecord Record)> source = [];
        private readonly List<DxfIdentityNode> nodes = [];
        private readonly List<DxfGraphRecordLocator> nonIdentity = [];
        private readonly List<DxfReferenceEdge> edges = [];
        private readonly List<DxfOwnershipEdge> owners = [];
        private readonly List<DxfHandleConflict> conflicts = [];
        private readonly List<DxfOwnerCycle> cycles = [];
        private readonly List<DxfGraphDiagnostic> diagnostics = [];
        private readonly Dictionary<string, List<int>> lookup = new(StringComparer.Ordinal);
        private readonly Dictionary<DxfGraphRecordLocator, int> nodeLookup = [];
        private bool errors;
        private bool unvalidatedScope;
        private int traversalWork;
        public DxfIdentityGraphReadResult Run()
        {
            try
            {
                token.ThrowIfCancellationRequested();
                if (evidence.Status is not (DxfBlockAnnotationStatus.Succeeded or DxfBlockAnnotationStatus.Partial) || !S.StructureValidated)
                {
                    Emit(DxfGraphDiagnosticCode.InputRejected, DxfStructuralDiagnosticSeverity.Error, "03E requires validated preceding source evidence.");
                    return Result(Enum.TryParse<DxfGraphStatus>(evidence.Status.ToString(), out var rejected) && rejected is not (DxfGraphStatus.Succeeded or DxfGraphStatus.Partial)
                        ? rejected : DxfGraphStatus.InvalidInput, false, false, false);
                }
                Guard(S.Records.Count > limits.MaxRecords - S.Tables.Count || P.Count > limits.MaxSourcePairs, "input records/pairs");
                foreach (var section in S.Sections)
                {
                    token.ThrowIfCancellationRequested();
                    if (section.Name is "HEADER" or "CLASSES" or "TABLES" or "BLOCKS" or "ENTITIES" or "OBJECTS") continue;
                    unvalidatedScope = true;
                    Emit(DxfGraphDiagnosticCode.UnvalidatedIdentityScope, DxfStructuralDiagnosticSeverity.Warning,
                        "Unknown section has no reviewed object-identity profile; graph coverage incomplete.", section.Source);
                }
                for (var i = 0; i < S.Records.Count; i++) { token.ThrowIfCancellationRequested(); source.Add((new(DxfGraphRecordKind.StructuralRecord, i), S.Records[i])); }
                for (var i = 0; i < S.Tables.Count; i++) { token.ThrowIfCancellationRequested(); source.Add((new(DxfGraphRecordKind.TableHeader, i), S.Tables[i].HeaderRecord)); }
                source.Sort((a, b) => a.Record.Source.StartPairIndex.CompareTo(b.Record.Source.StartPairIndex));
                CollectNodes(); ValidateDuplicates(); CollectEdges(); ValidateOwners();
                var referenceComplete = edges.All(e => e.Resolution is DxfReferenceResolution.Resolved or DxfReferenceResolution.Null or DxfReferenceResolution.NotApplicable);
                var identitiesComplete = !unvalidatedScope && nodes.All(n => n.Validity == DxfIdentityValidity.Valid);
                var graphValid = !errors && identitiesComplete;
                // Semantic mapping status remains on unchanged03D evidence; not promoted by a clean graph.
                return Result(graphValid && referenceComplete ? DxfGraphStatus.Succeeded : DxfGraphStatus.Partial, graphValid, identitiesComplete, referenceComplete);
            }
            catch (Stop) { return Result(DxfGraphStatus.ResourceLimitExceeded, false, false, false, false); }
            catch (OperationCanceledException)
            { Terminal(DxfGraphDiagnosticCode.Canceled, "03E validation canceled."); return Result(DxfGraphStatus.Canceled, false, false, false, false); }
        }
        private void CollectNodes()
        {
            var blockMembership = new Dictionary<int, int>();
            for (var i = 0; i < evidence.BlockDefinitions.Count; i++)
            {
                token.ThrowIfCancellationRequested(); var b = evidence.BlockDefinitions[i]; blockMembership[b.BeginRecordIndex] = i;
                if (b.EndRecordIndex.HasValue) blockMembership[b.EndRecordIndex.Value] = i;
                foreach (var c in b.Children) { token.ThrowIfCancellationRequested(); blockMembership[c.StructuralRecordIndex] = i; }
            }
            foreach (var (locator, r) in source)
            {
                token.ThrowIfCancellationRequested(); var identityScope = r.Scope is "BLOCKS" or "ENTITIES" or "OBJECTS" || r.Scope.StartsWith("TABLES/", StringComparison.Ordinal);
                var required = identityScope && (r.References.Any(r => r.Role == DxfReferenceRole.Owner) || r.ContextMarkers.Any(m => m.GroupCode == 100 && m.Application == null && m.ControlPath.Length == 0 &&
                    m.Subclass is "AcDbEntity" or "AcDbSymbolTable" or "AcDbSymbolTableRecord") ||
                    r.Scope == "OBJECTS" && r.RecordType is "DICTIONARY" or "LAYOUT" or "MATERIAL" or "VISUALSTYLE");
                var indices = new List<int>(); string? subclass = null, app = null; var control = "";
                var markers = r.ContextMarkers.ToDictionary(m => m.PairIndex);
                for (var i = r.Source.StartPairIndex; i <= r.Source.EndPairIndex; i++)
                {
                    token.ThrowIfCancellationRequested();
                    if (markers.TryGetValue(i, out var m)) { subclass = m.Subclass; app = m.Application; control = m.ControlPath; continue; }
                    if (!identityScope || app != null || control.Length != 0) continue;
                    var identityRole = r.RecordType == "DIMSTYLE" ? P[i].GroupCode == 105 && subclass is null or "AcDbDimStyleTableRecord" : P[i].GroupCode == 5 && subclass == null;
                    if (identityRole) indices.Add(i);
                }
                if (!required && indices.Count == 0) { nonIdentity.Add(locator); continue; }
                Guard(nodes.Count >= limits.MaxNodes, "identity nodes", r.Source);
                var validity = indices.Count == 0 ? DxfIdentityValidity.MissingRequired : indices.Count > 1 ? DxfIdentityValidity.MultipleFields : DxfIdentityValidity.Valid;
                var original = indices.Count == 1 ? P[indices[0]].RawTextValue : null;
                var canonical = original == null ? null : Canonical(original);
                if (validity == DxfIdentityValidity.Valid && (canonical == null || canonical == "0")) { validity = DxfIdentityValidity.Malformed; canonical = null; }
                var span = indices.Count == 0 ? r.Source : Span(indices[0], indices[^1]);
                if (validity != DxfIdentityValidity.Valid)
                    Emit(validity == DxfIdentityValidity.MissingRequired ? DxfGraphDiagnosticCode.MissingRequiredHandle : validity == DxfIdentityValidity.MultipleFields ? DxfGraphDiagnosticCode.MultipleHandleFields : DxfGraphDiagnosticCode.MalformedHandle,
                        DxfStructuralDiagnosticSeverity.Error, "Object identity source field is missing, malformed or conflicting; no replacement.", span, locator);
                var fact = new DxfSourceFact(DxfFactKind.Text, indices.Count == 0 ? DxfFactPresence.Absent : DxfFactPresence.Present,
                    validity == DxfIdentityValidity.Valid ? DxfFactValidity.Valid : DxfFactValidity.Invalid, original, null, null, indices, span);
                var section = r.Scope.Split('/')[0]; var table = section == "TABLES" ? r.Scope["TABLES/".Length..] : null;
                var n = new DxfIdentityNode(locator, r, section, table,
                    locator.Kind == DxfGraphRecordKind.StructuralRecord && blockMembership.TryGetValue(locator.Index, out var block) ? block : null,
                    fact, canonical, validity, document == null ? null : new(document, original, validity));
                nodeLookup.Add(locator, nodes.Count); nodes.Add(n);
                if (canonical != null)
                { if (!lookup.TryGetValue(canonical, out var members)) { members = []; lookup.Add(canonical, members); } members.Add(nodes.Count - 1); }
            }
        }
        private void ValidateDuplicates()
        {
            var seen = new HashSet<string>(StringComparer.Ordinal); var conflictMembers = 0;
            for (var i = 0; i < nodes.Count; i++)
            {
                token.ThrowIfCancellationRequested(); var key = nodes[i].CanonicalHandle;
                if (key == null || !seen.Add(key) || lookup[key].Count < 2) continue;
                var members = lookup[key]; Guard(conflicts.Count >= limits.MaxDuplicateGroups || members.Count > limits.MaxConflictMembers - conflictMembers, "duplicate evidence", nodes[i].OriginalHandle.Source);
                conflictMembers += members.Count; var locators = new List<DxfGraphRecordLocator>();
                foreach (var j in members)
                {
                    token.ThrowIfCancellationRequested(); var n = nodes[j]; locators.Add(n.Locator);
                    nodes[j] = n with { Validity = DxfIdentityValidity.Ambiguous, FullIdentity = document == null ? null : new(document, n.OriginalHandle.Text, DxfIdentityValidity.Ambiguous) };
                    Emit(DxfGraphDiagnosticCode.DuplicateHandle, DxfStructuralDiagnosticSeverity.Error, "Duplicate canonical Handle " + key + "; every conflicting location retained.", n.OriginalHandle.Source, n.Locator);
                }
                conflicts.Add(new(key, locators));
            }
        }
        private void CollectEdges()
        {
            foreach (var (locator, r) in source)
            {
                token.ThrowIfCancellationRequested();
                foreach (var reference in r.References)
                {
                    token.ThrowIfCancellationRequested(); Guard(edges.Count >= limits.MaxReferences, "contextual references", r.Source);
                    var raw = P[reference.PairIndex].RawTextValue; var canonical = Canonical(raw); DxfGraphRecordLocator? target = null;
                    var state = canonical == null ? DxfReferenceResolution.Malformed : canonical == "0" ? DxfReferenceResolution.Null :
                        !lookup.TryGetValue(canonical, out var matches) ? DxfReferenceResolution.Unresolved : matches.Count != 1 ? DxfReferenceResolution.Ambiguous : DxfReferenceResolution.Resolved;
                    if (state == DxfReferenceResolution.Resolved) target = nodes[lookup[canonical!][0]].Locator;
                    var from = nodeLookup.TryGetValue(locator, out var ni) ? nodes[ni].OriginalHandle.Text : null;
                    edges.Add(new(locator, from, reference.GroupCode, raw, canonical, Span(reference.PairIndex, reference.PairIndex), reference, Pointer(reference.GroupCode), state, target));
                    if (state is DxfReferenceResolution.Resolved or DxfReferenceResolution.Null) continue;
                    var code = state == DxfReferenceResolution.Malformed ? DxfGraphDiagnosticCode.MalformedReference : state == DxfReferenceResolution.Ambiguous ? DxfGraphDiagnosticCode.AmbiguousReference : reference.Role switch
                    {
                        DxfReferenceRole.Owner => DxfGraphDiagnosticCode.UnresolvedOwner, DxfReferenceRole.Reactor => DxfGraphDiagnosticCode.UnresolvedReactor,
                        DxfReferenceRole.ExtensionDictionary => DxfGraphDiagnosticCode.UnresolvedExtensionDictionary, DxfReferenceRole.XData => DxfGraphDiagnosticCode.UnresolvedXData, _ => DxfGraphDiagnosticCode.UnresolvedPointer
                    };
                    Emit(code, reference.Role == DxfReferenceRole.Owner ? DxfStructuralDiagnosticSeverity.Error : DxfStructuralDiagnosticSeverity.Warning,
                        "Contextual reference " + reference.Role + " has " + state + " target; raw evidence retained.", Span(reference.PairIndex, reference.PairIndex), locator);
                }
            }
        }
        private string? Canonical(string raw)
        {
            var first = -1;
            for (var i = 0; i < raw.Length; i++)
            {
                token.ThrowIfCancellationRequested(); var c = raw[i];
                if (!(c is >= '0' and <= '9' or >= 'A' and <= 'F' or >= 'a' and <= 'f')) return null;
                if (first < 0 && c != '0') first = i;
            }
            return raw.Length == 0 ? null : first < 0 ? "0" : raw[first..].ToUpperInvariant();
        }
        private static DxfPointerKind Pointer(int code) => code switch
        { >= 330 and <= 339 => DxfPointerKind.SoftPointer, >= 340 and <= 349 or >= 390 and <= 399 or 480 or 481 => DxfPointerKind.HardPointer,
            >= 350 and <= 359 => DxfPointerKind.SoftOwner, >= 360 and <= 369 => DxfPointerKind.HardOwner, 1005 => DxfPointerKind.XDataHandle, _ => DxfPointerKind.Other };
        private DxfSourceSpan Span(int start, int end) => new(start, end, P[start].CodeLineNumber, P[end].ValueLineNumber);
        private DxfIdentityGraphReadResult Result(DxfGraphStatus status, bool valid, bool identity, bool references, bool checkToken = true)
        {
            if (checkToken) token.ThrowIfCancellationRequested();
            return new(evidence, document, status, nodes, nonIdentity, edges, owners, conflicts, cycles, diagnostics, valid, identity, references, checkToken ? token : CancellationToken.None);
        }
        private void Guard(bool exceeded, string dimension, DxfSourceSpan? span = null)
        { token.ThrowIfCancellationRequested(); if (!exceeded) return; Terminal(DxfGraphDiagnosticCode.ResourceLimitExceeded, "03E budget exceeded: " + dimension + ".", span); throw new Stop(); }
        private void Emit(DxfGraphDiagnosticCode code, DxfStructuralDiagnosticSeverity severity, string message, DxfSourceSpan? span = null, DxfGraphRecordLocator? locator = null)
        {
            token.ThrowIfCancellationRequested();
            if (diagnostics.Count >= limits.MaxStoredDiagnostics - 1) { Terminal(DxfGraphDiagnosticCode.DiagnosticLimitExceeded, "03E diagnostic budget exhausted; validation incomplete."); throw new Stop(); }
            if (severity == DxfStructuralDiagnosticSeverity.Error) errors = true; diagnostics.Add(new(code, severity, message, span, locator));
        }
        private void Terminal(DxfGraphDiagnosticCode code, string message, DxfSourceSpan? span = null) => diagnostics.Add(new(code, DxfStructuralDiagnosticSeverity.Error, message, span));
        private sealed class Stop : Exception;
    }
}
