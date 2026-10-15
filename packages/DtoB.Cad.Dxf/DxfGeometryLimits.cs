namespace DtoB.Cad.Dxf;

/// <summary>Development guards measured against89 core records,16 polylines,
/// 61 polyline vertices/max4 per polyline,196 total geometry coordinate slots.
/// Existing structural/lexical caps remain in force.</summary>
public sealed record DxfGeometryLimits
{
    public int MaxGeometryRecords { get; init; } = 512;
    public int MaxVerticesPerPolyline { get; init; } = 64;
    public int MaxTotalVertices { get; init; } = 1_024;
    public int MaxFieldsPerRecord { get; init; } = 512;
    public int MaxStoredDiagnostics { get; init; } = 5_000;
    public int MaxStructuralRecords { get; init; } = 4_096;
    public int MaxSourcePairs { get; init; } = 250_000;
    public void Validate()
    {
        if (MaxGeometryRecords <= 0 || MaxVerticesPerPolyline <= 0 || MaxTotalVertices <= 0 ||
            MaxFieldsPerRecord <= 0 || MaxStoredDiagnostics < 2 || MaxStructuralRecords <= 0 || MaxSourcePairs <= 0)
            throw new ArgumentOutOfRangeException(nameof(DxfGeometryLimits), "Positive limits and at least two diagnostic slots are required.");
    }
}
