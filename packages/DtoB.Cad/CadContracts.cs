using System.Text.Json;
using System.Text.Json.Serialization;
using DtoB.Core;
using DtoB.Geometry;

namespace DtoB.Cad;

public sealed record CadLayer(string Name, string Color, string Linetype, double LineweightMm);
public sealed record CadBlock(string Id, string Name, string SourceHandle, CadEntity[] Entities);
public sealed record CadDocument(int SchemaVersion, Guid DrawingId, Guid RevisionId, string SourceFile,
    string SourceUnit, Transform3 SourceToModel, CadLayer[] Layers, CadBlock[] Blocks, CadEntity[] Entities, Diagnostic[] Diagnostics, double SourceToMillimetersScale = 1)
{
    public Point3 NormalizeSourcePoint(Point3 source)
    { ValidateStructure(); source.ValidateStructure(); var result=new Point3(source.X*SourceToMillimetersScale,source.Y*SourceToMillimetersScale,source.Z*SourceToMillimetersScale); result.ValidateStructure(); return result; }
    public void ValidateGeometry(GeometryTolerance tolerance)
    {
        ValidateStructure(); tolerance.ValidateStructure();
        Contract.Require(tolerance.MatrixCoefficientTolerance.HasValue, "CAD transform validation requires explicit matrix coefficient tolerance.");
        foreach (var entity in Entities)
        {
            var composed=Transform3.Identity;
            var prefix=new List<string>();
            foreach(var handle in entity.InsertionPath)
            {
                var id=CadIdentity.Create(DrawingId,handle,prefix);
                var insert=Entities.OfType<CadInsert>().SingleOrDefault(i=>i.Id==id);
                Contract.Require(insert!=null,"Insertion path must resolve to INSERT occurrences.");
                composed=insert.Placement.Then(composed); prefix.Add(handle);
            }
            if(entity is CadInsert own) composed=own.Placement.Then(composed);
            for(int i=0;i<16;i++)
            {
                // Translation has mm dimension; linear matrix coefficients are dimensionless.
                double allowed=i is 3 or 7 or 11?tolerance.LengthMm:tolerance.MatrixCoefficientTolerance.Value;
                Contract.Require(Math.Abs(entity.Transform.Matrix[i]-composed.Matrix[i])<=allowed,"Accumulated transform does not match INSERT placement composition.");
            }
        }
    }
    public void ValidateStructure()
    {
        Contract.Version(SchemaVersion);
        Contract.Require(DrawingId != Guid.Empty && RevisionId != Guid.Empty && !string.IsNullOrWhiteSpace(SourceFile), "Document identity/file required.");
        var scale = SourceUnit switch { "mm" => 1d, "cm" => 10d, "m" => 1000d, "in" => 25.4d, "ft" => 304.8d, _ => throw new InvalidDataException("Unknown/unitless source drawing unit must be resolved explicitly.") };
        Contract.Require(SourceToMillimetersScale == scale, "Source conversion scale must explicitly match original unit.");
        Contract.Require(SourceToModel != null && Layers != null && Blocks != null && Entities != null && Diagnostics != null, "CAD transform and all arrays required.");
        Contract.Require(Layers.All(l => l != null) && Blocks.All(b => b != null && b.Entities != null && b.Entities.All(e => e != null)) && Entities.All(e => e != null) && Diagnostics.All(d => d != null), "CAD arrays cannot contain null entries.");
        SourceToModel.ValidateStructure();
        Contract.Require(Layers.Select(l => l.Name).Distinct().Count() == Layers.Length, "Duplicate CAD layers.");
        foreach (var layer in Layers) { Contract.Require(!string.IsNullOrWhiteSpace(layer.Name), "Layer name required."); Contract.Finite(layer.LineweightMm); }
        Contract.Require(Blocks.Select(b => b.Id).Distinct().Count() == Blocks.Length, "Duplicate block IDs.");
        var all = Entities.Concat(Blocks.SelectMany(b => b.Entities)).ToArray();
        Contract.Require(all.Select(e => e.Id).Distinct().Count() == all.Length, "Duplicate CAD occurrence IDs.");
        foreach (var block in Blocks)
        {
            Contract.Require(!string.IsNullOrWhiteSpace(block.Id) && !string.IsNullOrWhiteSpace(block.SourceHandle), "Block identity required.");
            foreach (var definition in block.Entities)
                Contract.Require(definition != null && definition.OwnerBlockId == block.Id && definition.InsertionPath != null && definition.InsertionPath.Length == 0 && definition.Transform != null && definition.Transform.Matrix.SequenceEqual(Transform3.Identity.Matrix), "Block definition requires matching owner, empty path and identity transform.");
        }
        foreach (var entity in all)
        {
            entity.ValidateStructure();
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
    public virtual void ValidateStructure()
    {
        Contract.Require(!string.IsNullOrWhiteSpace(Id) && !string.IsNullOrWhiteSpace(SourceHandle), "CAD identity required.");
        Contract.Require(Transform != null && InsertionPath != null && RawProperties != null, "CAD transform/path/raw properties required.");
        if (this is not CadUnsupported && RawProperties.TryGetValue("extrusionNormal", out var normal))
            Contract.Require(normal.ValueKind == JsonValueKind.Array && normal.GetArrayLength() == 3 && normal[0].ValueKind == JsonValueKind.Number && normal[1].ValueKind == JsonValueKind.Number && normal[2].ValueKind == JsonValueKind.Number && normal[0].TryGetDouble(out double nx) && normal[1].TryGetDouble(out double ny) && normal[2].TryGetDouble(out double nz) && nx == 0 && ny == 0 && nz == 1, "Non-+Z normal must be CadUnsupported with diagnostic.");
        Contract.Finite(LineweightMm); Transform.ValidateStructure();
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
    public override void ValidateStructure()
    {
        base.ValidateStructure(); Contract.Require(Points != null, "Primitive points required."); foreach (var p in Points) p.ValidateStructure();
        Contract.Require(Points.Length >= (EntityType is CadPrimitiveType.Line or CadPrimitiveType.Polyline ? 2 : 1), "Insufficient primitive points.");
        if (EntityType is CadPrimitiveType.Arc or CadPrimitiveType.Circle) { Contract.Require(Points.Length == 1, "Arc/circle Points[0] must be the sole definition-frame center."); Contract.Require(RadiusMm.HasValue, "Radius required."); Contract.Positive(RadiusMm!.Value); }
        if (EntityType == CadPrimitiveType.Arc) { Contract.Require(StartAngleRadians.HasValue && EndAngleRadians.HasValue, "Arc angles required."); Contract.Finite(StartAngleRadians!.Value, EndAngleRadians!.Value); }
    }
}
public sealed record CadInsert : CadEntity
{
    public required string BlockId { get; init; }
    public Transform3 Placement { get; init; } = Transform3.Identity;
    public override void ValidateStructure() { base.ValidateStructure(); Contract.Require(Placement != null, "INSERT local placement required."); Placement.ValidateStructure(); }
    public Dictionary<string, string> Attributes { get; init; } = [];
}
public sealed record CadText : CadEntity
{
    public required string Content { get; init; }
    public required Point3 Insertion { get; init; }
    public required double HeightMm { get; init; }
    public double RotationRadians { get; init; }
    public bool Multiline { get; init; }
    public override void ValidateStructure() { base.ValidateStructure(); Insertion.ValidateStructure(); Contract.Positive(HeightMm); Contract.Finite(RotationRadians); }
}
public sealed record CadDimension : CadEntity
{
    public required Point3[] DefiningPoints { get; init; }
    public required double MeasurementMm { get; init; }
    public required string DisplayText { get; init; }
    public override void ValidateStructure() { base.ValidateStructure(); Contract.Require(DefiningPoints != null && DefiningPoints.Length >= 2, "Dimension defining points required."); foreach (var p in DefiningPoints) p.ValidateStructure(); Contract.Finite(MeasurementMm); }
}
public sealed record CadUnsupported : CadEntity
{
    public required string OriginalType { get; init; }
    public required string Reason { get; init; }
    public override void ValidateStructure() { base.ValidateStructure(); Contract.Require(!string.IsNullOrWhiteSpace(OriginalType) && !string.IsNullOrWhiteSpace(Reason), "Unsupported type/reason required."); }
}

public sealed record DrawingReadResult(CadDocument? Document, Diagnostic[] Diagnostics);
public interface IDrawingReader
{
    Task<DrawingReadResult> ReadAsync(string path, CancellationToken cancellationToken = default);
}
