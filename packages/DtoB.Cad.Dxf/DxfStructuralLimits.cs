namespace DtoB.Cad.Dxf;

/// <summary>Development caps: controlled input has 6 sections, 9 tables and fewer than
/// 400 child records; largest child record has 131 pairs. Variables/table headers
/// are included in the 593 measured work records. No occurrence expansion.</summary>
public sealed record DxfStructuralLimits
{
    public int MaxSections { get; init; } = 32;
    public int MaxTables { get; init; } = 32;
    public int MaxRecords { get; init; } = 4_096;
    public int MaxFieldsPerRecord { get; init; } = 512;
    public int MaxReferenceOccurrences { get; init; } = 16_384;
    public int MaxControlGroupDepth { get; init; } = 16;
    public int MaxStoredDiagnostics { get; init; } = 5_000;
    public int MaxPairCount { get; init; } = 250_000;
    public void Validate()
    {
        if (MaxSections <= 0 || MaxTables <= 0 || MaxRecords <= 0 || MaxFieldsPerRecord <= 0 ||
            MaxReferenceOccurrences <= 0 || MaxControlGroupDepth <= 0 || MaxStoredDiagnostics < 2 || MaxPairCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(DxfStructuralLimits), "Positive limits and at least two diagnostic slots are required.");
    }
}
