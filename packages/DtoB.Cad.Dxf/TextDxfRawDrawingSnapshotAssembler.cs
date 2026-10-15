using System.Diagnostics.CodeAnalysis;
using DtoB.Cad.Raw;

namespace DtoB.Cad.Dxf;

/// <summary>Immutable evidence composition only. No parsing, graph rebuilding, I/O or normalization.</summary>
public sealed class TextDxfRawDrawingSnapshotAssembler
{
    private readonly RawDrawingSnapshotAssemblyLimits limits;
    public TextDxfRawDrawingSnapshotAssembler(RawDrawingSnapshotAssemblyLimits? limits = null)
    { this.limits = limits ?? new(); this.limits.Validate(); }
    public RawDrawingSnapshotAssemblyResult Assemble(DxfIdentityGraphReadResult evidence, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evidence); return new Operation(evidence, limits, cancellationToken).Run();
    }
    private sealed class Operation(DxfIdentityGraphReadResult graph, RawDrawingSnapshotAssemblyLimits limits, CancellationToken token)
    {
        private DxfBlockAnnotationReadResult D => graph.AnnotationEvidence;
        private DxfGeometryReadResult C => D.GeometryEvidence;
        private DxfStructuralReadResult B => C.StructuralEvidence;
        private DxfLexicalReadResult A => B.LexicalEvidence;
        private readonly List<RawSnapshotAssemblyDiagnostic> diagnostics = [];
        private RawSnapshotCompleteness summary = new(false, false, false, false, false, false, false, false, false, false);
        public RawDrawingSnapshotAssemblyResult Run()
        {
            try
            {
                token.ThrowIfCancellationRequested();
                if (B.Records.Count > limits.MaxChildAssociations || B.Records.Count > limits.MaxSourceLocations - B.Tables.Count) Budget("source/child locations");
                summary = Summary(false);
                if (!summary.LexicalSucceeded || !summary.StructureValidated || B.VersionAcceptance != DxfStructuralVersionAcceptance.Supported ||
                    B.Status is not (DxfStructuralStatus.Succeeded or DxfStructuralStatus.Partial) ||
                    C.Status is not (DxfGeometryStatus.Succeeded or DxfGeometryStatus.Partial) ||
                    D.Status is not (DxfBlockAnnotationStatus.Succeeded or DxfBlockAnnotationStatus.Partial) || !D.EffectiveInventoryAvailable ||
                    graph.Status is not (DxfGraphStatus.Succeeded or DxfGraphStatus.Partial) || !summary.GraphValidated ||
                    !summary.IdentityCoverageComplete || !summary.ReferenceResolutionComplete)
                    Refuse(RawSnapshotDiagnosticCode.PredecessorRejected, "V1 snapshot requires successful source stages and complete validated identity/reference coverage.");
                var unsupportedGeometry = false; var unsupportedAnnotations = false;
                foreach (var r in C.Geometry)
                {
                    token.ThrowIfCancellationRequested();
                    if (r.Disposition is DxfDisposition.Invalid or DxfDisposition.DeferredTo03C or DxfDisposition.DeferredTo03D)
                        Refuse(RawSnapshotDiagnosticCode.PredecessorRejected, "Invalid or unfinished geometry mapping cannot be published.", r.Source);
                    if (r.Disposition == DxfDisposition.Unsupported) unsupportedGeometry = true;
                }
                foreach (var r in D.Records)
                {
                    token.ThrowIfCancellationRequested();
                    if (r.Disposition is DxfDisposition.Invalid or DxfDisposition.DeferredTo03C or DxfDisposition.DeferredTo03D)
                        Refuse(RawSnapshotDiagnosticCode.PredecessorRejected, "Invalid or unfinished block/annotation mapping cannot be published.", r.Source);
                    if (r.Disposition == DxfDisposition.Unsupported) unsupportedAnnotations = true;
                }
                // A completed Unsupported profile is raw coverage, not a failed assembly.
                if (!C.GeometryMappingComplete && !unsupportedGeometry || !D.MappingComplete && !unsupportedAnnotations)
                    Refuse(RawSnapshotDiagnosticCode.PredecessorRejected, "Mapping completeness is missing without explicit unsupported coverage.");
                if (C.Geometry.Count > B.Records.Count || D.Records.Count > B.Records.Count ||
                    graph.Nodes.Count > B.Records.Count + B.Tables.Count || graph.NonIdentityRecords.Count != B.Records.Count + B.Tables.Count - graph.Nodes.Count)
                    Refuse(RawSnapshotDiagnosticCode.InconsistentInventory, "Association/index counts exceed or disagree with their source domain.");
                var geometry = new int?[B.Records.Count]; var annotations = new int?[B.Records.Count];
                var identity = new int?[B.Records.Count]; var blocks = new int?[B.Records.Count];
                for (var i = 0; i < C.Geometry.Count; i++)
                {
                    token.ThrowIfCancellationRequested(); var r = C.Geometry[i]; CheckSource(r.StructuralRecordIndex, r.SourceRecord);
                    if (geometry[r.StructuralRecordIndex].HasValue) Refuse(RawSnapshotDiagnosticCode.ConflictingAssociation, "Duplicate geometry association.", r.Source);
                    geometry[r.StructuralRecordIndex] = i;
                }
                for (var i = 0; i < D.Records.Count; i++)
                {
                    token.ThrowIfCancellationRequested(); var r = D.Records[i]; CheckSource(r.StructuralRecordIndex, r.SourceRecord);
                    if (annotations[r.StructuralRecordIndex].HasValue || geometry[r.StructuralRecordIndex].HasValue)
                        Refuse(RawSnapshotDiagnosticCode.ConflictingAssociation, "Conflicting typed record associations.", r.Source);
                    annotations[r.StructuralRecordIndex] = i;
                }
                var inspected = new HashSet<DxfGraphRecordLocator>();
                for (var i = 0; i < graph.Nodes.Count; i++)
                {
                    token.ThrowIfCancellationRequested(); var n = graph.Nodes[i]; CheckLocator(n.Locator, n.SourceRecord);
                    if (!inspected.Add(n.Locator)) Refuse(RawSnapshotDiagnosticCode.ConflictingAssociation, "Duplicate identity source locator.", n.SourceRecord.Source);
                    if (n.Locator.Kind == DxfGraphRecordKind.StructuralRecord) identity[n.Locator.Index] = i;
                }
                foreach (var locator in graph.NonIdentityRecords)
                {
                    token.ThrowIfCancellationRequested(); CheckLocator(locator);
                    if (!inspected.Add(locator)) Refuse(RawSnapshotDiagnosticCode.ConflictingAssociation, "Identity/nonidentity source locator conflict.");
                }
                if (inspected.Count != B.Records.Count + B.Tables.Count) Refuse(RawSnapshotDiagnosticCode.InconsistentInventory, "Identity source inventory does not account for every child/table header.");
                for (var i = 0; i < D.BlockDefinitions.Count; i++)
                {
                    token.ThrowIfCancellationRequested(); var b = D.BlockDefinitions[i]; Membership(b.BeginRecordIndex, i);
                    if (!b.EndRecordIndex.HasValue) Refuse(RawSnapshotDiagnosticCode.InvalidAssociation, "Incomplete block definition cannot be published.", b.Source);
                    Membership(b.EndRecordIndex.Value, i);
                    foreach (var child in b.Children)
                    {
                        token.ThrowIfCancellationRequested(); Membership(child.StructuralRecordIndex, i);
                        if (child.GeometryRecordIndex != geometry[child.StructuralRecordIndex] || child.AnnotationRecordIndex != annotations[child.StructuralRecordIndex])
                            Refuse(RawSnapshotDiagnosticCode.InvalidAssociation, "Block child typed locator does not match preceding evidence.", b.Source);
                    }
                }
                void Membership(int index, int block)
                {
                    CheckIndex(index);
                    if (blocks[index].HasValue) Refuse(RawSnapshotDiagnosticCode.ConflictingAssociation, "Source record belongs to competing definitions.");
                    blocks[index] = block;
                }
                var records = new List<RawDrawingRecordView>(); var counts = new Dictionary<DxfDisposition, int>();
                for (var i = 0; i < B.Records.Count; i++)
                {
                    token.ThrowIfCancellationRequested(); var source = B.Records[i]; var g = geometry[i].HasValue ? C.Geometry[geometry[i]!.Value] : null;
                    var d = annotations[i].HasValue ? D.Records[annotations[i]!.Value] : null; var n = identity[i].HasValue ? graph.Nodes[identity[i]!.Value] : null;
                    if (n != null && n.BlockDefinitionIndex != blocks[i]) Refuse(RawSnapshotDiagnosticCode.InvalidAssociation, "Identity/definition source scope disagrees.", source.Source);
                    var disposition = d?.Disposition ?? g?.Disposition ?? source.Disposition;
                    counts[disposition] = counts.GetValueOrDefault(disposition) + 1;
                    records.Add(new(i, source, disposition, geometry[i], g, annotations[i], d, identity[i], n, blocks[i]));
                }
                if (counts.Count != D.EffectiveRecordDispositionCounts.Count) Refuse(RawSnapshotDiagnosticCode.InconsistentInventory, "Final semantic count domains disagree.");
                foreach (var c in D.EffectiveRecordDispositionCounts)
                { token.ThrowIfCancellationRequested(); if (counts.GetValueOrDefault(c.Disposition) != c.Count) Refuse(RawSnapshotDiagnosticCode.InconsistentInventory, "Final semantic associations disagree with03D authoritative inventory."); }
                var refs = new List<RawSnapshotDiagnosticReference>();
                void Stage(RawSnapshotDiagnosticStage stage, int count)
                {
                    for (var i = 0; i < count; i++)
                    { token.ThrowIfCancellationRequested(); if (refs.Count >= limits.MaxDiagnosticReferences) Budget("diagnostic references"); refs.Add(new(stage, i)); }
                }
                Stage(RawSnapshotDiagnosticStage.Lexical03A, A.Diagnostics.Count); Stage(RawSnapshotDiagnosticStage.Structural03B, B.Diagnostics.Count);
                Stage(RawSnapshotDiagnosticStage.Geometry03C, C.Diagnostics.Count); Stage(RawSnapshotDiagnosticStage.BlockAnnotation03D, D.Diagnostics.Count);
                Stage(RawSnapshotDiagnosticStage.Identity03E, graph.Diagnostics.Count); Stage(RawSnapshotDiagnosticStage.Assembly03F, diagnostics.Count);
                summary = Summary(true);
                var snapshot = new RawDrawingSnapshot(graph, records, refs, diagnostics, summary, token);
                token.ThrowIfCancellationRequested(); return new(graph, RawSnapshotAssemblyStatus.Succeeded, snapshot, summary, diagnostics);
            }
            catch (Refusal) { return Failure(RawSnapshotAssemblyStatus.InvalidInput); }
            catch (Limit) { return Failure(RawSnapshotAssemblyStatus.ResourceLimitExceeded); }
            catch (OperationCanceledException)
            { diagnostics.Add(new(RawSnapshotDiagnosticCode.Canceled, "Snapshot assembly canceled; no snapshot published.")); return Failure(RawSnapshotAssemblyStatus.Canceled); }
        }
        private RawSnapshotCompleteness Summary(bool complete)
        {
            token.ThrowIfCancellationRequested();
            var sourceComplete = A.Status == DxfLexicalStatus.Succeeded && B.StructureValidated;
            foreach (var section in B.Sections)
            { token.ThrowIfCancellationRequested(); if (section.Name is not ("HEADER" or "CLASSES" or "TABLES" or "BLOCKS" or "ENTITIES" or "OBJECTS")) sourceComplete = false; }
            var typed = D.EffectiveInventoryAvailable && C.GeometryMappingComplete && D.MappingComplete;
            var count = 0;
            foreach (var c in D.EffectiveRecordDispositionCounts)
            { token.ThrowIfCancellationRequested(); count += c.Count; if (c.Disposition is not (DxfDisposition.Supported or DxfDisposition.IgnoredWithDiagnostic)) typed = false; }
            if (count != B.Records.Count) typed = false;
            foreach (var table in B.Tables)
            { token.ThrowIfCancellationRequested(); if (table.Disposition is not (DxfDisposition.Supported or DxfDisposition.IgnoredWithDiagnostic)) typed = false; }
            if (B.Header != null)
                foreach (var variable in B.Header.Variables)
                { token.ThrowIfCancellationRequested(); if (variable.Disposition != DxfDisposition.Supported) typed = false; }
            return new(A.Status == DxfLexicalStatus.Succeeded, B.StructureValidated, C.GeometryMappingComplete, D.MappingComplete,
                graph.IdentityCoverageComplete, graph.ReferenceResolutionComplete, graph.GraphValidated, sourceComplete, typed, complete);
        }
        private void CheckIndex(int index)
        { token.ThrowIfCancellationRequested(); if (index < 0 || index >= B.Records.Count) Refuse(RawSnapshotDiagnosticCode.InvalidAssociation, "Child locator is outside structural inventory."); }
        private void CheckSource(int index, DxfStructuralRecord source)
        { CheckIndex(index); if (!ReferenceEquals(B.Records[index], source)) Refuse(RawSnapshotDiagnosticCode.InvalidAssociation, "Typed record is not linked to the exact source instance."); }
        private void CheckLocator(DxfGraphRecordLocator locator, DxfStructuralRecord? source = null)
        {
            token.ThrowIfCancellationRequested();
            if (locator.Kind == DxfGraphRecordKind.StructuralRecord) { CheckIndex(locator.Index); if (source != null) CheckSource(locator.Index, source); }
            else if (locator.Kind == DxfGraphRecordKind.TableHeader && locator.Index >= 0 && locator.Index < B.Tables.Count)
            { if (source != null && !ReferenceEquals(source, B.Tables[locator.Index].HeaderRecord)) Refuse(RawSnapshotDiagnosticCode.InvalidAssociation, "Identity header source instance mismatch."); }
            else Refuse(RawSnapshotDiagnosticCode.InvalidAssociation, "Identity locator is outside source inventory.");
        }
        [DoesNotReturn]
        private void Refuse(RawSnapshotDiagnosticCode code, string message, DxfSourceSpan? source = null)
        { diagnostics.Add(new(code, message, source)); throw new Refusal(); }
        [DoesNotReturn]
        private void Budget(string dimension)
        { diagnostics.Add(new(RawSnapshotDiagnosticCode.ResourceLimitExceeded, "Snapshot budget exceeded: " + dimension + ".")); throw new Limit(); }
        private RawDrawingSnapshotAssemblyResult Failure(RawSnapshotAssemblyStatus status) => new(graph, status, null, summary with { SnapshotAssemblyComplete = false }, diagnostics);
        private sealed class Refusal : Exception;
        private sealed class Limit : Exception;
    }
}
