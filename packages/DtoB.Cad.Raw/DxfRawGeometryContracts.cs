using System.Text.Json.Serialization;

namespace DtoB.Cad.Raw;

public enum DxfGeometryStatus { Succeeded, Partial, InvalidInput, UnsupportedInput, UnsupportedVersion, ResourceLimitExceeded, Canceled, IoFailure }
public enum DxfGeometryDiagnosticCode
{
    StructuralInputRejected, InvalidSourceRecord, MissingRequiredField, InvalidNumericValue,
    DuplicateField, InvalidRadius, InvalidVertexOrder, VertexCountMismatch, InvalidVertexCount,
    ForeignFieldContext, LegacyPolylineGated, UnsupportedGeometry, GeometryLimitExceeded,
    DiagnosticLimitExceeded, Canceled
}
public sealed record DxfGeometryDiagnostic(DxfGeometryDiagnosticCode Code,
    DxfStructuralDiagnosticSeverity Severity, string Message, DxfSourceSpan? Source = null);

/// <summary>Native source-component facts, never mm/WCS or inferred default coordinates.</summary>
public sealed record RawCoordinateFacts(DxfSourceFact X, DxfSourceFact Y, DxfSourceFact Z);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$geometry")]
[JsonDerivedType(typeof(RawLine), "LINE")]
[JsonDerivedType(typeof(RawLwPolyline), "LWPOLYLINE")]
[JsonDerivedType(typeof(RawArc), "ARC")]
[JsonDerivedType(typeof(RawPoint), "POINT")]
[JsonDerivedType(typeof(RawSolid), "SOLID")]
[JsonDerivedType(typeof(RawCircle), "CIRCLE")]
public abstract record RawGeometryPayload;
public sealed record RawLine(RawCoordinateFacts Start, RawCoordinateFacts End) : RawGeometryPayload;
public sealed record RawArc(RawCoordinateFacts Center, DxfSourceFact Radius,
    DxfSourceFact StartAngleDegrees, DxfSourceFact EndAngleDegrees) : RawGeometryPayload;
public sealed record RawCircle(RawCoordinateFacts Center, DxfSourceFact Radius) : RawGeometryPayload;
public sealed record RawPoint(RawCoordinateFacts Position, DxfSourceFact AngleDegrees) : RawGeometryPayload;
public sealed record RawSolid(RawCoordinateFacts First, RawCoordinateFacts Second,
    RawCoordinateFacts Third, RawCoordinateFacts Fourth) : RawGeometryPayload;
public sealed record RawLwVertex(int Ordinal, DxfSourceSpan Source, DxfSourceFact X, DxfSourceFact Y,
    DxfSourceFact StartWidth, DxfSourceFact EndWidth, DxfSourceFact Bulge, DxfSourceFact Identifier);
public sealed record RawLwPolyline : RawGeometryPayload
{
    public DxfSourceFact DeclaredVertexCount { get; }
    public DxfSourceFact Flags { get; }
    public DxfSourceFact Elevation { get; }
    public DxfSourceFact ConstantWidth { get; }
    public IReadOnlyList<RawLwVertex> Vertices { get; }
    public int ActualVertexCount => Vertices.Count;
    public RawLwPolyline(DxfSourceFact count, DxfSourceFact flags, DxfSourceFact elevation,
        DxfSourceFact constantWidth, IEnumerable<RawLwVertex> vertices)
    {
        DeclaredVertexCount = count; Flags = flags; Elevation = elevation; ConstantWidth = constantWidth;
        Vertices = Array.AsReadOnly(vertices.ToArray());
    }
}

public sealed class RawGeometryRecord
{
    /// <summary>Index is a QA locator into structural Records, not a CAD identity.</summary>
    public int StructuralRecordIndex { get; }
    public DxfStructuralRecord SourceRecord { get; }
    public string EntityType => SourceRecord.RecordType;
    public string SourceSection => SourceRecord.Scope;
    public DxfSourceSpan Source => SourceRecord.Source;
    public DxfDisposition Disposition { get; }
    /// <summary>Null means this unsupported/gated record has no typed field mapping.
    /// In mapped profiles, an absent SourceFact instead means verified source absence.</summary>
    public DxfSourceFact? Layer { get; }
    public DxfSourceFact? Thickness { get; }
    public RawCoordinateFacts? Extrusion { get; }
    public RawGeometryPayload? Payload { get; }
    /// <summary>Each mapped fact appears once here; payload/common properties reference those facts.</summary>
    public IReadOnlyList<DxfNamedFact> Fields { get; }
    public IReadOnlyList<int> UnmappedPairIndices { get; }
    public IReadOnlyList<DxfGeometryDiagnostic> Diagnostics { get; }
    public RawGeometryRecord(int index, DxfStructuralRecord source, DxfDisposition disposition,
        DxfSourceFact? layer, DxfSourceFact? thickness, RawCoordinateFacts? extrusion,
        RawGeometryPayload? payload, IEnumerable<DxfNamedFact> fields, IEnumerable<int> unmapped,
        IEnumerable<DxfGeometryDiagnostic> diagnostics)
    {
        StructuralRecordIndex = index; SourceRecord = source; Disposition = disposition;
        Layer = layer; Thickness = thickness; Extrusion = extrusion; Payload = payload;
        Fields = Array.AsReadOnly(fields.ToArray()); UnmappedPairIndices = Array.AsReadOnly(unmapped.ToArray());
        Diagnostics = Array.AsReadOnly(diagnostics.ToArray());
    }
}

public sealed class DxfGeometryReadResult
{
    public DxfStructuralReadResult StructuralEvidence { get; }
    public DxfGeometryStatus Status { get; }
    public IReadOnlyList<RawGeometryRecord> Geometry { get; }
    public IReadOnlyList<DxfGeometryDiagnostic> Diagnostics { get; }
    public IReadOnlyList<DxfDispositionCount> GeometryDispositionCounts { get; }
    public IReadOnlyList<DxfDispositionCount> EffectiveRecordDispositionCounts { get; }
    public bool EffectiveInventoryAvailable { get; }
    public bool GeometryMappingComplete { get; }
    public DxfGeometryReadResult(DxfStructuralReadResult structural, DxfGeometryStatus status,
        IEnumerable<RawGeometryRecord> geometry, IEnumerable<DxfGeometryDiagnostic> diagnostics,
        bool complete, CancellationToken cancellationToken = default, int maxEffectiveRecords = 4_096)
    {
        StructuralEvidence = structural; Status = status;
        Geometry = Freeze(geometry, cancellationToken); Diagnostics = Freeze(diagnostics, cancellationToken);
        var replacement = new Dictionary<int, DxfDisposition>(); var geometryCounts = new Dictionary<DxfDisposition, int>();
        foreach (var record in Geometry)
        {
            cancellationToken.ThrowIfCancellationRequested(); replacement.Add(record.StructuralRecordIndex, record.Disposition);
            geometryCounts[record.Disposition] = geometryCounts.GetValueOrDefault(record.Disposition) + 1;
        }
        var effective = new Dictionary<DxfDisposition, int>();
        EffectiveInventoryAvailable = structural.Records.Count <= maxEffectiveRecords;
        for (var i = 0; EffectiveInventoryAvailable && i < structural.Records.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var disposition = replacement.GetValueOrDefault(i, structural.Records[i].Disposition);
            effective[disposition] = effective.GetValueOrDefault(disposition) + 1;
        }
        GeometryDispositionCounts = Freeze(geometryCounts.OrderBy(c => c.Key).Select(c => new DxfDispositionCount(c.Key, c.Value)), cancellationToken);
        EffectiveRecordDispositionCounts = Freeze(effective.OrderBy(c => c.Key).Select(c => new DxfDispositionCount(c.Key, c.Value)), cancellationToken);
        GeometryMappingComplete = complete && status is DxfGeometryStatus.Succeeded or DxfGeometryStatus.Partial;
        cancellationToken.ThrowIfCancellationRequested();
    }
    private static IReadOnlyList<T> Freeze<T>(IEnumerable<T> source, CancellationToken token)
    {
        var values = new List<T>();
        foreach (var value in source) { token.ThrowIfCancellationRequested(); values.Add(value); }
        token.ThrowIfCancellationRequested(); return Array.AsReadOnly(values.ToArray());
    }
}
