namespace DtoB.Cad.Dxf;

/// <summary>Derived from approved structural/reference caps. Controlled327 nodes/458 edges.</summary>
public sealed record DxfIdentityGraphLimits
{
    public int MaxRecords { get; init; } = 4_096;
    public int MaxNodes { get; init; } = 4_096;
    public int MaxReferences { get; init; } = 16_384;
    public int MaxDuplicateGroups { get; init; } = 2_048;
    public int MaxConflictMembers { get; init; } = 4_096;
    public int MaxTraversalWork { get; init; } = 32_768;
    public int MaxStoredDiagnostics { get; init; } = 5_000;
    public int MaxSourcePairs { get; init; } = 250_000;
    public void Validate()
    {
        if (MaxRecords <= 0 || MaxNodes <= 0 || MaxReferences <= 0 || MaxDuplicateGroups <= 0 ||
            MaxConflictMembers <= 0 || MaxTraversalWork <= 0 || MaxStoredDiagnostics < 2 || MaxSourcePairs <= 0)
            throw new ArgumentOutOfRangeException(nameof(DxfIdentityGraphLimits), "Positive budgets and a reserved terminal diagnostic slot required.");
    }
}
