namespace DtoB.Cad.Dxf;

/// <summary>Controlled93 records/12 definitions/max9 children/25 INSERT/44 annotations,
/// max1 MTEXT chunk and974 annotation text chars. Attribute sequences use separate reviewed fixtures.</summary>
public sealed record DxfBlockAnnotationLimits
{
    public int MaxDefinitions { get; init; } = 32;
    public int MaxChildrenPerBlock { get; init; } = 64;
    public int MaxInserts { get; init; } = 256;
    public int MaxAnnotations { get; init; } = 256;
    public int MaxMTextChunks { get; init; } = 32;
    public int MaxRawAnnotationCharacters { get; init; } = 16_384;
    public int MaxAttributeSequenceLength { get; init; } = 64;
    public int MaxMappedRecords { get; init; } = 512;
    public int MaxStoredDiagnostics { get; init; } = 5_000;
    public int MaxFieldsPerRecord { get; init; } = 512;
    public int MaxStructuralRecords { get; init; } = 4_096;
    public int MaxSourcePairs { get; init; } = 250_000;
    public void Validate()
    {
        if (MaxDefinitions <= 0 || MaxChildrenPerBlock <= 0 || MaxInserts <= 0 || MaxAnnotations <= 0 ||
            MaxMTextChunks <= 0 || MaxRawAnnotationCharacters <= 0 || MaxAttributeSequenceLength <= 0 ||
            MaxMappedRecords <= 0 || MaxStoredDiagnostics < 2 || MaxFieldsPerRecord <= 0 || MaxStructuralRecords <= 0 || MaxSourcePairs <= 0)
            throw new ArgumentOutOfRangeException(nameof(DxfBlockAnnotationLimits), "Positive limits and at least two diagnostic slots are required.");
    }
}
