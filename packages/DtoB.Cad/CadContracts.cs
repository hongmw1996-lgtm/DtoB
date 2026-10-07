using System.Text.Json;
using System.Text.Json.Serialization;
using DtoB.Core;
using DtoB.Geometry;

namespace DtoB.Cad;

public sealed record CadLayer(string Name, string Color, string Linetype, double LineweightMm);
public sealed record CadBlock(string Id, string Name, string SourceHandle, CadEntity[] Entities);
public sealed record CadDocument(int SchemaVersion, Guid DrawingId, Guid RevisionId, string SourceFile,
    string SourceUnit, Transform3 SourceToModel, CadLayer[] Layers, CadBlock[] Blocks, CadEntity[] Entities, Diagnostic[] Diagnostics)
{
    public void Validate()
    {
        Contract.Version(SchemaVersion);
        Contract.Require(DrawingId != Guid.Empty && RevisionId != Guid.Empty && !string.IsNullOrWhiteSpace(SourceFile), "Document identity/file required.");
        Contract.Require(SourceUnit == "mm", "PHASE 00 CAD fixtures support declared mm source units only.");
        Contract.Require(SourceToModel != null && Layers != null && Blocks != null && Entities != null && Diagnostics != null, "CAD transform and all arrays required.");
        Contract.Require(Layers.All(l => l != null) && Blocks.All(b => b != null && b.Entities != null) && Entities.All(e => e != null) && Diagnostics.All(d => d != null), "CAD arrays cannot contain null entries.");
        SourceToModel.Validate();
        Contract.Require(Layers.Select(l => l.Name).Distinct().Count() == Layers.Length, "Duplicate CAD layers.");
        foreach (var layer in Layers) { Contract.Require(!string.IsNullOrWhiteSpace(layer.Name), "Layer name required."); Contract.Finite(layer.LineweightMm); }
        Contract.Require(Blocks.Select(b => b.Id).Distinct().Count() == Blocks.Length, "Duplicate block IDs.");
        var all = Entities.Concat(Blocks.SelectMany(b => b.Entities)).ToArray();
        Contract.Require(all.Select(e => e.Id).Distinct().Count() == all.Length, "Duplicate CAD occurrence IDs.");
        foreach (var block in Blocks) Contract.Require(!string.IsNullOrWhiteSpace(block.Id) && !string.IsNullOrWhiteSpace(block.SourceHandle), "Block identity required.");
        foreach (var entity in all)
        {
            entity.Validate();
            Contract.Require(entity.Id == CadIdentity.Create(DrawingId, entity.SourceHandle, entity.InsertionPath), "CAD ID must preserve source identity/path.");
            Contract.Require(Layers.Any(l => l.Name == entity.RawLayer) && Layers.Any(l => l.Name == entity.EffectiveLayer), "Unknown entity layer.");
            if (entity is CadInsert insert) Contract.Require(Blocks.Any(b => b.Id == insert.BlockId), "Unknown block reference.");
            if (entity is CadUnsupported unsupported)
                Contract.Require(Diagnostics.Any(d => d.Code == "UNSUPPORTED_ENTITY" && d.SourceHandle == unsupported.SourceHandle && d.Message == unsupported.Reason), "Unsupported entity requires source-linked diagnostic.");
        }
    }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$kind")]
[JsonDerivedType(typeof(CadPrimitive), "primitive")]
[JsonDerivedType(typeof(CadInsert), "insert")]
[JsonDerivedType(typeof(CadText), "text")]
[JsonDerivedType(typeof(CadDimension), "dimension")]
[JsonDerivedType(typeof(CadUnsupported), "unsupported")]
public abstract record CadEntity
{
    public required string Id { get; init; }
    public required string SourceHandle { get; init; }
    public required string RawLayer { get; init; }
    public required string EffectiveLayer { get; init; }
    public required string RawColor { get; init; }
    public required string EffectiveColor { get; init; }
    public string Linetype { get; init; } = "Continuous";
    public double LineweightMm { get; init; }
    public string? OwnerBlockId { get; init; }
    public string? InsertLayer { get; init; }
    public string[] InsertionPath { get; init; } = [];
    public Transform3 Transform { get; init; } = Transform3.Identity;
    public Dictionary<string, JsonElement> RawProperties { get; init; } = [];
    public virtual void Validate()
    {
        Contract.Require(!string.IsNullOrWhiteSpace(Id) && !string.IsNullOrWhiteSpace(SourceHandle), "CAD identity required.");
        Contract.Require(Transform != null && InsertionPath != null && RawProperties != null, "CAD transform/path/raw properties required.");
        if (this is not CadUnsupported && RawProperties.TryGetValue("extrusionNormal", out var normal))
            Contract.Require(normal.ValueKind == JsonValueKind.Array && normal.GetArrayLength() == 3 && normal[0].GetDouble() == 0 && normal[1].GetDouble() == 0 && normal[2].GetDouble() == 1, "Non-+Z normal must be CadUnsupported with diagnostic.");
        Contract.Finite(LineweightMm); Transform.Validate();
    }
}
public enum CadPrimitiveType { Line, Polyline, Arc, Circle }
public sealed record CadPrimitive : CadEntity
{
    public required CadPrimitiveType EntityType { get; init; }
    public required Point3[] Points { get; init; }
    public bool Closed { get; init; }
    public double? RadiusMm { get; init; }
    public double? StartAngleRadians { get; init; }
    public double? EndAngleRadians { get; init; }
    public override void Validate()
    {
        base.Validate(); Contract.Require(Points != null, "Primitive points required."); foreach (var p in Points) p.Validate();
        Contract.Require(Points.Length >= (EntityType is CadPrimitiveType.Line or CadPrimitiveType.Polyline ? 2 : 1), "Insufficient primitive points.");
        if (EntityType is CadPrimitiveType.Arc or CadPrimitiveType.Circle) { Contract.Require(Points.Length == 1, "Arc/circle Points[0] must be the sole definition-frame center."); Contract.Require(RadiusMm.HasValue, "Radius required."); Contract.Positive(RadiusMm!.Value); }
        if (EntityType == CadPrimitiveType.Arc) { Contract.Require(StartAngleRadians.HasValue && EndAngleRadians.HasValue, "Arc angles required."); Contract.Finite(StartAngleRadians!.Value, EndAngleRadians!.Value); }
    }
}
public sealed record CadInsert : CadEntity
{
    public required string BlockId { get; init; }
    public Dictionary<string, string> Attributes { get; init; } = [];
}
public sealed record CadText : CadEntity
{
    public required string Content { get; init; }
    public required Point3 Insertion { get; init; }
    public required double HeightMm { get; init; }
    public double RotationRadians { get; init; }
    public bool Multiline { get; init; }
    public override void Validate() { base.Validate(); Insertion.Validate(); Contract.Positive(HeightMm); Contract.Finite(RotationRadians); }
}
public sealed record CadDimension : CadEntity
{
    public required Point3[] DefiningPoints { get; init; }
    public required double MeasurementMm { get; init; }
    public required string DisplayText { get; init; }
    public override void Validate() { base.Validate(); Contract.Require(DefiningPoints != null && DefiningPoints.Length >= 2, "Dimension defining points required."); foreach (var p in DefiningPoints) p.Validate(); Contract.Finite(MeasurementMm); }
}
public sealed record CadUnsupported : CadEntity
{
    public required string OriginalType { get; init; }
    public required string Reason { get; init; }
    public override void Validate() { base.Validate(); Contract.Require(!string.IsNullOrWhiteSpace(OriginalType) && !string.IsNullOrWhiteSpace(Reason), "Unsupported type/reason required."); }
}

public sealed record DrawingReadResult(CadDocument? Document, Diagnostic[] Diagnostics);
public interface IDrawingReader
{
    Task<DrawingReadResult> ReadAsync(string path, CancellationToken cancellationToken = default);
}
