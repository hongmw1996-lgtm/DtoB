using System.Text.Json;
using System.Text.Json.Serialization;
using DtoB.Core;
using DtoB.Geometry;

namespace DtoB.Bim;

public enum ReviewStatus { Suggested, NeedsReview, Confirmed, Ignored, Error }
public sealed record BimDocument(int SchemaVersion, BimObject[] Objects)
{
    public void ValidateGeometry(GeometryTolerance tolerance)
    {
        ValidateStructure(); tolerance.ValidateStructure();
        foreach (var obj in Objects)
        {
            if (obj is BimWall wall) wall.Baseline.ValidateGeometry(tolerance);
            if (obj is BimGrid grid) grid.Axis.ValidateGeometry(tolerance);
            if (obj is BimFloor floor) GeometryPredicates.ClosedBoundary(floor.Boundary, tolerance);
            if (obj is BimRoom room) GeometryPredicates.ClosedBoundary(room.Boundary, tolerance);
        }
    }
    public void ValidateStructure()
    {
        Contract.Version(SchemaVersion);
        Contract.Require(Objects != null && Objects.All(o => o != null), "BIM objects array required.");
        Contract.Require(Objects.Select(o => o.Id).Distinct().Count() == Objects.Length, "Duplicate BIM IDs.");
        foreach (var obj in Objects)
        {
            obj.ValidateStructure();
            if (obj.LevelId is Guid level) Contract.Require(Objects.Any(o => o.Id == level && o is BimLevel), "Unknown level reference.");
            if (obj is BimOpening opening) Contract.Require(Objects.Any(o => o.Id == opening.HostWallId && o is BimWall && o.LevelId == opening.LevelId), "Unknown or inconsistent wall host.");
        }
    }
}
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$category")]
[JsonDerivedType(typeof(BimLevel), "Level")]
[JsonDerivedType(typeof(BimGrid), "Grid")]
[JsonDerivedType(typeof(BimWall), "Wall")]
[JsonDerivedType(typeof(BimColumn), "Column")]
[JsonDerivedType(typeof(BimDoor), "Door")]
[JsonDerivedType(typeof(BimWindow), "Window")]
[JsonDerivedType(typeof(BimFloor), "Floor")]
[JsonDerivedType(typeof(BimRoom), "Room")]
public abstract record BimObject
{
    public required Guid Id { get; init; }
    public required Provenance Provenance { get; init; }
    public Prediction? Prediction { get; init; }
    public ReviewStatus Status { get; init; }
    public Guid? LevelId { get; init; }
    public virtual void ValidateStructure()
    {
        Contract.Require(Id != Guid.Empty, "BIM ID required.");
        Contract.Require(Provenance != null && !string.IsNullOrWhiteSpace(Provenance.Description), "BIM provenance required.");
        Contract.Require(Provenance!.Sources != null, "Provenance sources cannot be null.");
        if (Provenance!.Origin == ProvenanceOrigin.SourceDrawing) Contract.Require(Provenance.Sources.Length > 0, "Source-drawing provenance needs sources.");
        foreach (var s in Provenance.Sources!) { Contract.Require(s != null, "Null source reference."); s.ValidateStructure(); }
        Prediction?.ValidateStructure();
        // Suggested is a recognition result, whereas confirmed/manual domain objects may be non-predicted.
        Provenance.Confirmation?.ValidateStructure();
        if ((Provenance.Origin == ProvenanceOrigin.SourceDrawing && Provenance.Confirmation == null) || Status == ReviewStatus.Suggested)
            Contract.Require(Prediction != null, "Source-derived semantic result requires prediction or documented manual confirmation.");
        if (this is not BimLevel and not BimGrid) Contract.Require(LevelId.HasValue, "Level required.");
    }
}
public sealed record BimLevel : BimObject
{
    public required string Name { get; init; }
    public required double ElevationMm { get; init; }
    public override void ValidateStructure() { base.ValidateStructure(); Contract.Finite(ElevationMm); Contract.Require(!string.IsNullOrWhiteSpace(Name) && LevelId == null, "Level name required; level cannot reference a level."); }
}
public sealed record BimGrid : BimObject
{
    public required string Label { get; init; }
    public required Segment3 Axis { get; init; }
    public override void ValidateStructure() { base.ValidateStructure(); Contract.Require(Axis != null, "Grid axis required."); Axis.ValidateStructure(); Contract.Require(!string.IsNullOrWhiteSpace(Label), "Grid label required."); }
}
public sealed record BimWall : BimObject
{
    public required Segment3 Baseline { get; init; }
    public required double ThicknessMm { get; init; }
    public required double HeightMm { get; init; }
    public override void ValidateStructure() { base.ValidateStructure(); Contract.Require(Baseline != null, "Wall baseline required."); Baseline.ValidateStructure(); Contract.Positive(ThicknessMm, HeightMm); }
}
public sealed record BimColumn : BimObject
{
    public required Point3 Position { get; init; }
    public required double WidthMm { get; init; }
    public required double DepthMm { get; init; }
    public required double HeightMm { get; init; }
    public double RotationRadians { get; init; }
    public override void ValidateStructure() { base.ValidateStructure(); Position.ValidateStructure(); Contract.Positive(WidthMm, DepthMm, HeightMm); Contract.Finite(RotationRadians); }
}
public abstract record BimOpening : BimObject
{
    public required Point3 Position { get; init; }
    public required Guid HostWallId { get; init; }
    public required double WidthMm { get; init; }
    public required double HeightMm { get; init; }
    public double RotationRadians { get; init; }
    public override void ValidateStructure() { base.ValidateStructure(); Position.ValidateStructure(); Contract.Positive(WidthMm, HeightMm); Contract.Finite(RotationRadians); }
}
public sealed record BimDoor : BimOpening;
public sealed record BimWindow : BimOpening
{
    public required double SillMm { get; init; }
    public override void ValidateStructure() { base.ValidateStructure(); Contract.Finite(SillMm); Contract.Require(SillMm >= 0, "Sill must be nonnegative."); }
}
public sealed record BimFloor : BimObject
{
    public required Point3[] Boundary { get; init; }
    public required double ThicknessMm { get; init; }
    public override void ValidateStructure() { base.ValidateStructure(); BoundaryContract.Validate(Boundary); Contract.Positive(ThicknessMm); }
}
public sealed record BimRoom : BimObject
{
    public required string Name { get; init; }
    public required Point3[] Boundary { get; init; }
    public override void ValidateStructure() { base.ValidateStructure(); BoundaryContract.Validate(Boundary); Contract.Require(!string.IsNullOrWhiteSpace(Name), "Room name required."); }
}
internal static class BoundaryContract
{
    public static void Validate(Point3[] boundary)
    {
        Contract.Require(boundary != null && boundary.Length >= 4, "Boundary must contain at least three vertices plus closure; use document ValidateGeometry(tolerance) for geometry.");
        foreach (var p in boundary) p.ValidateStructure();
    }
}
