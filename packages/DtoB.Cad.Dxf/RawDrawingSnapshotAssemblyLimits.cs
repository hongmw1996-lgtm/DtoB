namespace DtoB.Cad.Dxf;

/// <summary>Association arrays share the4096 upstream record cap; diagnostics budget is six stage caps.</summary>
public sealed record RawDrawingSnapshotAssemblyLimits
{
    public int MaxChildAssociations { get; init; } = 4_096;
    public int MaxSourceLocations { get; init; } = 4_096;
    public int MaxDiagnosticReferences { get; init; } = 30_000;
    public void Validate()
    {
        if (MaxChildAssociations <= 0 || MaxSourceLocations <= 0 || MaxDiagnosticReferences <= 0)
            throw new ArgumentOutOfRangeException(nameof(RawDrawingSnapshotAssemblyLimits), "Positive finite assembly limits required.");
    }
}
