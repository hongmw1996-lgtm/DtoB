using DtoB.Cad.Normalize.Placement;
using DtoB.Cad.Normalize.Scalars;
using DtoB.Cad.Raw;
using DtoB.Cad.V2;

namespace DtoB.Cad.Normalize.Occurrences;

public enum CadExpansionStatus { Complete, Partial, InvalidInput, Cancelled, ResourceLimitExceeded }
public enum CadOccurrenceDiagnosticCode { PlacementRejected, InvalidArrayFacts, UnsupportedArrayAttributes, Cycle,
    NonFiniteCellTransform, NonFiniteAccumulatedTransform, InvalidAuthorityOrUnits, ResourceLimitExceeded, Cancelled }

public sealed class CadOccurrenceIntegerFact
{
    public int? Value { get; }
    public DxfFactPresence Presence { get; }
    public DxfFactValidity Validity { get; }
    public IReadOnlyList<int> PairIndices { get; }
    public int? FirstLine { get; }
    public int? LastLine { get; }
    internal int? Resolved { get; }
    internal CadOccurrenceIntegerFact(DxfSourceFact fact)
    {
        Value = fact.Integer; Presence = fact.Presence; Validity = fact.Validity; PairIndices = CadContract.Copy(fact.SourcePairIndices);
        FirstLine = fact.Source?.StartLine; LastLine = fact.Source?.EndLine;
        var shape = fact.Kind == DxfFactKind.Integer && fact.Real is null && fact.Text is null;
        if (shape && Presence == DxfFactPresence.Absent && Validity == DxfFactValidity.NotApplicable && Value is null && PairIndices.Count == 0 && fact.Source is null) Resolved = 1;
        else if (shape && Presence == DxfFactPresence.Present && Validity == DxfFactValidity.Valid && Value >= 1 && PairIndices.Count == 1
            && fact.Source is {} span && span.StartPairIndex == PairIndices[0] && span.EndPairIndex == span.StartPairIndex && span.StartLine > 0 && span.EndLine >= span.StartLine) Resolved = Value;
    }
}
public sealed class CadOccurrenceGrid
{
    public bool IsArray { get; }
    public CadOccurrenceIntegerFact RowsSource { get; }
    public CadOccurrenceIntegerFact ColumnsSource { get; }
    public int Rows { get; }
    public int Columns { get; }
    public double RowSpacingMm { get; }
    public double ColumnSpacingMm { get; }
    public CadScalarResult RowSpacingSource { get; }
    public CadScalarResult ColumnSpacingSource { get; }
    public IReadOnlyList<string> DefaultRules { get; }
    internal CadOccurrenceGrid(bool array, CadOccurrenceIntegerFact rows, CadOccurrenceIntegerFact columns,
        CadScalarResult rowSpacing, CadScalarResult columnSpacing)
    {
        IsArray = array; RowsSource = rows; ColumnsSource = columns; Rows = rows.Resolved!.Value; Columns = columns.Resolved!.Value;
        RowSpacingSource = rowSpacing; ColumnSpacingSource = columnSpacing;
        RowSpacingMm = rowSpacing.Value ?? 0; ColumnSpacingMm = columnSpacing.Value ?? 0;
        var rules = new List<string>();
        if (rows.Presence == DxfFactPresence.Absent) rules.Add("DXF.INSERT.Rows.DefaultOne");
        if (columns.Presence == DxfFactPresence.Absent) rules.Add("DXF.INSERT.Columns.DefaultOne");
        if (rowSpacing.Status == CadScalarStatus.Absent) rules.Add("DXF.INSERT.RowSpacing.DefaultZero");
        if (columnSpacing.Status == CadScalarStatus.Absent) rules.Add("DXF.INSERT.ColumnSpacing.DefaultZero");
        DefaultRules = CadContract.Copy(rules);
    }
}
/// <summary>Shared occurrence catalog metadata; ordered source children, no extracted geometry.</summary>
public sealed class CadOccurrenceDefinition
{
    public CadId Id { get; }
    public CadSourceRecordRef Source { get; }
    public CadBlockBaseResult Base { get; }
    public IReadOnlyList<CadSourceRecordRef> ChildSources { get; }
    internal CadOccurrenceDefinition(CadId id, CadSourceRecordRef source, CadBlockBaseResult block, IReadOnlyList<CadSourceRecordRef> children)
    { Id = id; Source = source; Base = block; ChildSources = CadContract.Copy(children); }
}
public sealed class CadExpandedOccurrence
{
    public CadBlockOccurrence Value { get; }
    public CadOccurrenceDefinition Definition { get; }
    public CadLocalPlacement SourcePlacement { get; }
    public CadOccurrenceGrid Grid { get; }
    public CadId SourceInsertId { get; }
    public int Depth { get; }
    public int SourceOrderOrdinal { get; }
    public int? CellOrdinal { get; }
    public int LocalOrientationSign { get; }
    public int AccumulatedOrientationSign { get; }
    internal CadExpandedOccurrence(CadBlockOccurrence value, CadOccurrenceDefinition definition, CadLocalPlacement placement,
        CadOccurrenceGrid grid, CadId sourceId, int depth, int ordinal, int? cellOrdinal, int accumulatedSign)
    {
        Value = value; Definition = definition; SourcePlacement = placement; Grid = grid; SourceInsertId = sourceId;
        Depth = depth; SourceOrderOrdinal = ordinal; CellOrdinal = cellOrdinal;
        LocalOrientationSign = placement.OrientationSign; AccumulatedOrientationSign = accumulatedSign;
    }
}
public sealed class CadOccurrenceDiagnostic(CadOccurrenceDiagnosticCode code, CadSourceRecordRef? source, CadOccurrencePath path,
    string detail, long? requested = null, long? limit = null, IReadOnlyList<CadPlacementDiagnostic>? placement = null)
{
    public CadOccurrenceDiagnosticCode Code { get; } = code;
    public CadSourceRecordRef? Source { get; } = source;
    public CadOccurrencePath Path { get; } = path;
    public string Detail { get; } = detail;
    public long? Requested { get; } = requested;
    public long? Limit { get; } = limit;
    public IReadOnlyList<CadPlacementDiagnostic> Placement { get; } = CadContract.Copy(placement ?? []);
}
public sealed class CadOccurrenceExpansion
{
    public CadProvenanceRegistry Sources { get; }
    public IReadOnlyList<CadOccurrenceDefinition> Definitions { get; }
    public IReadOnlyList<CadExpandedOccurrence> Occurrences { get; }
    public CadNormalizationOptions Options { get; }
    public string NumericPolicyId { get; }
    internal CadOccurrenceExpansion(CadProvenanceRegistry sources, IReadOnlyList<CadOccurrenceDefinition> definitions,
        IReadOnlyList<CadExpandedOccurrence> occurrences, CadNormalizationOptions options, string numericPolicyId)
    { Sources = sources; Definitions = CadContract.Copy(definitions); Occurrences = CadContract.Copy(occurrences); Options = options; NumericPolicyId = numericPolicyId; }
}
public sealed class CadOccurrenceExpansionResult
{
    public CadExpansionStatus Status { get; }
    public CadOccurrenceExpansion? Expansion { get; }
    public long TotalOccurrencesAllocated { get; }
    public IReadOnlyList<CadOccurrenceDiagnostic> Diagnostics { get; }
    internal CadOccurrenceExpansionResult(CadExpansionStatus status, CadOccurrenceExpansion? expansion, long total,
        IReadOnlyList<CadOccurrenceDiagnostic> diagnostics)
    {
        CadContract.Require((status is CadExpansionStatus.Complete or CadExpansionStatus.Partial) == (expansion is not null),"Expansion status/publication mismatch.");
        Status = status; Expansion = expansion; TotalOccurrencesAllocated = total; Diagnostics = CadContract.Copy(diagnostics);
    }
}
