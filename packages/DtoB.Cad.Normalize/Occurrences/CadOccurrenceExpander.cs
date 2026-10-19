using DtoB.Cad.Normalize.Placement;
using DtoB.Cad.Normalize.Scalars;
using DtoB.Cad.Raw;
using DtoB.Cad.V2;
using DtoB.Geometry;

namespace DtoB.Cad.Normalize.Occurrences;

public static class CadOccurrenceExpander
{
    public static CadOccurrenceExpansionResult Expand(RawDrawingSnapshot snapshot, CadNormalizationOptions options,
        CadNumericPolicy numericPolicy, CancellationToken cancellationToken = default) =>
        new Session(snapshot,options,numericPolicy,cancellationToken).Run();

    private sealed class Session(RawDrawingSnapshot snapshot, CadNormalizationOptions options, CadNumericPolicy policy, CancellationToken token)
    {
        private readonly List<CadExpandedOccurrence> occurrences = [];
        private readonly List<CadOccurrenceDiagnostic> diagnostics = [];
        private readonly Dictionary<int,CadOccurrenceDefinition> definitions = [];
        private readonly Dictionary<int,RawBlockDefinition> rawDefinitions = [];
        private readonly Dictionary<int,CadLocalPlacementResult> placements = [];
        private readonly Dictionary<int,CadOccurrenceGrid?> grids = [];
        private CadProvenanceRegistry sources = null!;
        private CadUnitResolution units = null!;
        private long allocated;
        private CadSourceRecordRef? current;
        private CadOccurrencePath currentPath = new([]);
        private sealed class Stop(CadExpansionStatus status) : Exception { public CadExpansionStatus Status { get; } = status; }
        private sealed record Candidate(RawDrawingRecordView Row, CadExpandedOccurrence? Parent, CadLocalPlacement Placement,
            CadOccurrenceGrid Grid, int? RowIndex, int? ColumnIndex);

        internal CadOccurrenceExpansionResult Run()
        {
            ArgumentNullException.ThrowIfNull(options); ArgumentNullException.ThrowIfNull(policy);
            var stack = new Stack<IEnumerator<Candidate>>();
            try
            {
                token.ThrowIfCancellationRequested();
                var header = RawScalarAccess.HeaderUnits(snapshot); units = CadUnitNormalizer.Resolve(header);
                if (units.Status != CadUnitStatus.Resolved) return Invalid("Units");
                var records = new List<CadSourceRecord>();
                foreach (var row in snapshot.Records)
                {
                    token.ThrowIfCancellationRequested(); var s = row.SourceRecord.Source;
                    current = new(row.StructuralRecordIndex);
                    if (row.Identity?.Validity is DxfIdentityValidity.Ambiguous or DxfIdentityValidity.Malformed or DxfIdentityValidity.MultipleFields)
                        return Invalid("SourceIdentity");
                    records.Add(new(new(CadSourceRecordDomain.StructuralRecord,row.StructuralRecordIndex,s.StartPairIndex,s.EndPairIndex,s.StartLine,s.EndLine),
                        row.Identity?.OriginalHandle.Text,row.Identity?.CanonicalHandle));
                }
                sources = new(header.Source.Document,records,[]);
                foreach (var definition in snapshot.BlockDefinitions) { token.ThrowIfCancellationRequested(); rawDefinitions.Add(definition.BeginRecordIndex,definition); }
                stack.Push(Roots().GetEnumerator());
                while (stack.Count > 0)
                {
                    token.ThrowIfCancellationRequested(); var iterator = stack.Peek();
                    if (!iterator.MoveNext()) { iterator.Dispose(); stack.Pop(); continue; }
                    var occurrence = Admit(iterator.Current);
                    if (occurrence is not null) stack.Push(Children(occurrence).GetEnumerator());
                }
                token.ThrowIfCancellationRequested();
                var artifact = new CadOccurrenceExpansion(sources,definitions.OrderBy(p=>p.Key).Select(p=>p.Value).ToArray(),occurrences,options,policy.ProfileId);
                token.ThrowIfCancellationRequested();
                return new(diagnostics.Count == 0 ? CadExpansionStatus.Complete : CadExpansionStatus.Partial,artifact,allocated,diagnostics);
            }
            catch (OperationCanceledException)
            { Terminal(CadOccurrenceDiagnosticCode.Cancelled,"Cancellation"); return new(CadExpansionStatus.Cancelled,null,allocated,diagnostics); }
            catch (Stop stop) { return new(stop.Status,null,allocated,diagnostics); }
            catch (Exception e) when (e is InvalidDataException or FormatException or ArgumentException)
            { return Invalid("Authority/SourceStructure"); }
            finally { while (stack.Count > 0) stack.Pop().Dispose(); }
        }
        private CadOccurrenceExpansionResult Invalid(string detail)
        { Terminal(CadOccurrenceDiagnosticCode.InvalidAuthorityOrUnits,detail); return new(CadExpansionStatus.InvalidInput,null,allocated,diagnostics); }
        private void Terminal(CadOccurrenceDiagnosticCode code, string detail, long? requested = null, long? limit = null)
        {
            // Ordinary diagnostics never consume the reserved terminal slot.
            diagnostics.Add(new(code,current,currentPath,detail,requested,limit));
        }
        private void Fatal(string detail, long requested, long limit)
        { Terminal(CadOccurrenceDiagnosticCode.ResourceLimitExceeded,detail,requested,limit); throw new Stop(CadExpansionStatus.ResourceLimitExceeded); }
        private void Branch(CadOccurrenceDiagnosticCode code, string detail, IReadOnlyList<CadPlacementDiagnostic>? placement = null)
        {
            if (diagnostics.Count >= options.Limits.MaxDiagnostics-1) Fatal("Diagnostics",diagnostics.Count+1,options.Limits.MaxDiagnostics);
            diagnostics.Add(new(code,current,currentPath,detail,placement:placement));
        }
        private IEnumerable<Candidate> Roots()
        {
            foreach (var row in snapshot.Records)
            {
                token.ThrowIfCancellationRequested();
                if (row.SourceSection != "ENTITIES" || row.BlockAnnotation?.Payload is not RawInsert) continue;
                foreach (var candidate in Candidates(row,null)) yield return candidate;
            }
        }
        private IEnumerable<Candidate> Children(CadExpandedOccurrence parent)
        {
            var definition = rawDefinitions[parent.Definition.Source.Index];
            foreach (var child in definition.Children)
            {
                token.ThrowIfCancellationRequested(); var row = snapshot.Records[child.StructuralRecordIndex];
                if (row.BlockAnnotation?.Payload is not RawInsert) continue;
                foreach (var candidate in Candidates(row,parent)) yield return candidate;
            }
        }
        private IEnumerable<Candidate> Candidates(RawDrawingRecordView row, CadExpandedOccurrence? parent)
        {
            token.ThrowIfCancellationRequested(); current = new(row.StructuralRecordIndex); currentPath = parent?.Value.Path ?? new([]);
            var raw = (RawInsert)row.BlockAnnotation!.Payload!;
            if (!grids.TryGetValue(row.StructuralRecordIndex,out var grid))
            {
                grid = Grid(row,raw); grids.Add(row.StructuralRecordIndex,grid);
            }
            if (grid is null) { Branch(CadOccurrenceDiagnosticCode.InvalidArrayFacts,"Counts/Spacing"); yield break; }
            var product = checked((long)grid.Rows*grid.Columns);
            if (grid.IsArray && product > options.Limits.MaxCellsPerInsert) Fatal("CellsPerInsert",product,options.Limits.MaxCellsPerInsert);
            if (grid.IsArray && raw.AttributesFollow.Integer == 1)
            { Branch(CadOccurrenceDiagnosticCode.UnsupportedArrayAttributes,"ArrayAttributes"); yield break; }
            if (!placements.TryGetValue(row.StructuralRecordIndex,out var result))
            {
                token.ThrowIfCancellationRequested();
                result = grid.IsArray ? CadInsertLocalTransform.CreateArrayTemplate(snapshot,row,policy,units,token)
                    : CadInsertLocalTransform.Create(snapshot,row,policy,units,token);
                placements.Add(row.StructuralRecordIndex,result);
            }
            if (result.Placement is not {} placement)
            { Branch(CadOccurrenceDiagnosticCode.PlacementRejected,"LocalPlacement",result.Diagnostics); yield break; }
            for (var r = 0; r < grid.Rows; r++)
            {
                token.ThrowIfCancellationRequested();
                for (var c = 0; c < grid.Columns; c++)
                {
                    token.ThrowIfCancellationRequested();
                    yield return new(row,parent,placement,grid,grid.IsArray ? r : null,grid.IsArray ? c : null);
                }
            }
        }
        private CadOccurrenceGrid? Grid(RawDrawingRecordView row, RawInsert raw)
        {
            var rows = new CadOccurrenceIntegerFact(raw.Rows); var columns = new CadOccurrenceIntegerFact(raw.Columns);
            if (rows.Resolved is null || columns.Resolved is null) return null;
            var rowSpacing = CadScalarLengthNormalizer.Normalize(RawScalarAccess.Record(snapshot,row,CadScalarRole.InsertRowSpacing,token),units);
            var columnSpacing = CadScalarLengthNormalizer.Normalize(RawScalarAccess.Record(snapshot,row,CadScalarRole.InsertColumnSpacing,token),units);
            if (rowSpacing.Status is not (CadScalarStatus.Normalized or CadScalarStatus.Absent)
                || columnSpacing.Status is not (CadScalarStatus.Normalized or CadScalarStatus.Absent)) return null;
            return new(raw.OccurrenceExpansionUnsupported || rows.Resolved > 1 || columns.Resolved > 1,rows,columns,rowSpacing,columnSpacing);
        }
        private CadExpandedOccurrence? Admit(Candidate candidate)
        {
            token.ThrowIfCancellationRequested(); var p = candidate.Placement; var parent = candidate.Parent;
            current = new(candidate.Row.StructuralRecordIndex); var definitionSource = new CadSourceRecordRef(p.Block.Source.Record.RecordIndex);
            var step = new CadOccurrenceStep(current,definitionSource,candidate.RowIndex,candidate.ColumnIndex);
            var previous = parent?.Value.Path.Steps ?? [];
            currentPath = new(previous.Append(step).ToArray());
            if (previous.Any(s=>s.DefinitionSource.Index == definitionSource.Index))
            { Branch(CadOccurrenceDiagnosticCode.Cycle,"ActiveDefinitionPath"); return null; }
            var depth = previous.Count+1;
            if (depth > options.Limits.MaxDepth) Fatal("Depth",depth,options.Limits.MaxDepth);
            if (allocated >= options.Limits.MaxOccurrences) Fatal("Occurrences",allocated+1,options.Limits.MaxOccurrences);
            // Admit one allocation slot before derived state; a later failed branch never refunds it.
            allocated++;
            CadTransform local;
            try
            {
                local = CellTransform(candidate);
            }
            catch (InvalidDataException) { Branch(CadOccurrenceDiagnosticCode.NonFiniteCellTransform,"Cell"); return null; }
            CadTransform accumulated;
            try
            {
                accumulated = parent is null ? local : CadTransform.FromTransform3(local.ToTransform3().Then(parent.Value.AccumulatedTransform.ToTransform3()));
            }
            catch (InvalidDataException) { Branch(CadOccurrenceDiagnosticCode.NonFiniteAccumulatedTransform,"Composition"); return null; }
            if (!definitions.TryGetValue(definitionSource.Index,out var definition))
            {
                var children = new List<CadSourceRecordRef>();
                foreach (var child in rawDefinitions[definitionSource.Index].Children) { token.ThrowIfCancellationRequested(); children.Add(new(child.StructuralRecordIndex)); }
                definition = new(CadOccurrenceIdentity.Source(sources,definitionSource,CadIdentityDomain.Definition),definitionSource,p.Block,children);
                definitions.Add(definitionSource.Index,definition);
            }
            var id = CadOccurrenceIdentity.Occurrence(sources,currentPath);
            var value = new CadBlockOccurrence(id,current,definition.Id,parent?.Value.Id,currentPath,local,accumulated,null);
            var occurrence = new CadExpandedOccurrence(value,definition,p,candidate.Grid,CadOccurrenceIdentity.Source(sources,current,CadIdentityDomain.SourceInsert),
                depth,occurrences.Count,candidate.RowIndex is {} r ? checked(r*candidate.Grid.Columns+candidate.ColumnIndex!.Value) : null,
                (parent?.AccumulatedOrientationSign ?? 1)*p.OrientationSign);
            occurrences.Add(occurrence); return occurrence;
        }
        private static CadTransform CellTransform(Candidate c)
        {
            if (!c.Grid.IsArray || c.RowIndex == 0 && c.ColumnIndex == 0) return c.Placement.Transform;
            var d = new Point3(c.ColumnIndex!.Value*c.Grid.ColumnSpacingMm,c.RowIndex!.Value*c.Grid.RowSpacingMm,0);
            d.ValidateStructure();
            // Lcell = T(A*R*d)*Lzero = T(anchor)*A*R*T(d)*S*T(-base). No S acts on d.
            var rotated = Transform3.Placement(new(0,0,0),c.Placement.Rotation.Value!.Value,new(1,1,1)).Apply(d);
            var v = c.Placement.Anchor.Frame!.VectorToWcs(new(rotated.X,rotated.Y,rotated.Z));
            var translation = Transform3.Placement(new(v.X,v.Y,v.Z),0,new(1,1,1));
            return CadTransform.FromTransform3(c.Placement.Transform.ToTransform3().Then(translation));
        }
    }
}
