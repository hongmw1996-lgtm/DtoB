namespace DtoB.Cad.Dxf;

public sealed record DxfReaderLimits
{
    public int MaxPhysicalLineBytes { get; init; } = 65_536;
    // Reserved for 03B. 03A does not interpret control/XDATA nesting.
    public int MaxControlGroupDepth { get; init; } = 16;
    public int MaxStoredDiagnostics { get; init; } = 5_000;
    public int MaxPairCount { get; init; } = 250_000;
    public long MaxInputBytes { get; init; } = 16_777_216;

    public void Validate()
    {
        if (MaxPhysicalLineBytes <= 0 || MaxControlGroupDepth <= 0 ||
            MaxStoredDiagnostics < 2 || MaxPairCount <= 0 || MaxInputBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(DxfReaderLimits),
                "Limits must be positive; diagnostics requires an ordinary and a terminal slot.");
    }
}
