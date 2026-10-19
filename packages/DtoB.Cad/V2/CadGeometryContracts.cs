using System.Text.Json.Serialization;
using DtoB.Geometry;

namespace DtoB.Cad.V2;

/// <summary>Analytic payload contracts only; 04A does not generate or spatially validate geometry.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$kind")]
[JsonDerivedType(typeof(CadPoint), "point")]
[JsonDerivedType(typeof(CadLine), "line")]
[JsonDerivedType(typeof(CadCircle), "circle")]
[JsonDerivedType(typeof(CadArc), "arc")]
[JsonDerivedType(typeof(CadPolyline), "polyline")]
[JsonDerivedType(typeof(CadSolid), "solid")]
public abstract class CadGeometry
{
    public CadId Id { get; }
    public CadSourceRecordRef Source { get; }
    public IReadOnlyList<CadSourceFactRef> Evidence { get; }
    private protected CadGeometry(CadId id, CadSourceRecordRef source, IReadOnlyList<CadSourceFactRef> evidence)
    { Id = CadContract.Required(id); Source = CadContract.Required(source); Evidence = CadContract.Copy(evidence); }
    protected static Point3 Point(Point3 p) { p.ValidateStructure(); return new(p.X == 0 ? 0 : p.X, p.Y == 0 ? 0 : p.Y, p.Z == 0 ? 0 : p.Z); }
}
public sealed class CadPoint(CadId id, CadSourceRecordRef source, IReadOnlyList<CadSourceFactRef> evidence, Point3 positionMm)
    : CadGeometry(id, source, evidence)
{
    public Point3 PositionMm { get; } = Point(positionMm);
}
public sealed class CadLine(CadId id, CadSourceRecordRef source, IReadOnlyList<CadSourceFactRef> evidence, Point3 startMm, Point3 endMm)
    : CadGeometry(id, source, evidence)
{
    public Point3 StartMm { get; } = Point(startMm);
    public Point3 EndMm { get; } = Point(endMm);
}
public sealed class CadPlane(Point3 originMm, Point3 axisU, Point3 axisV)
{
    public Point3 OriginMm { get; } = Checked(originMm);
    public Point3 AxisU { get; } = Checked(axisU);
    public Point3 AxisV { get; } = Checked(axisV);
    private static Point3 Checked(Point3 p) { p.ValidateStructure(); return p; }
}
public sealed class CadCircle(CadId id, CadSourceRecordRef source, IReadOnlyList<CadSourceFactRef> evidence, CadPlane plane, double radiusMm)
    : CadGeometry(id, source, evidence)
{
    public CadPlane Plane { get; } = CadContract.Required(plane);
    public double RadiusMm { get; } = CadContract.Positive(radiusMm);
}
public sealed class CadArc(CadId id, CadSourceRecordRef source, IReadOnlyList<CadSourceFactRef> evidence, CadPlane plane,
    double radiusMm, double startRadians, double sweepRadians) : CadGeometry(id, source, evidence)
{
    public CadPlane Plane { get; } = CadContract.Required(plane);
    public double RadiusMm { get; } = CadContract.Positive(radiusMm);
    public double StartRadians { get; } = CadContract.Finite(startRadians);
    public double SweepRadians { get; } = CadContract.Finite(sweepRadians);
}
/// <summary>Ordered references to line/arc segments; no bulge conversion in the contract.</summary>
public sealed class CadPolyline(CadId id, CadSourceRecordRef source, IReadOnlyList<CadSourceFactRef> evidence,
    IReadOnlyList<CadId> segmentIds, bool closed) : CadGeometry(id, source, evidence)
{
    public IReadOnlyList<CadId> SegmentIds { get; } = CadContract.Copy(segmentIds);
    public bool Closed { get; } = closed;
}
public sealed class CadSolid(CadId id, CadSourceRecordRef source, IReadOnlyList<CadSourceFactRef> evidence,
    IReadOnlyList<Point3> boundaryMm) : CadGeometry(id, source, evidence)
{
    public IReadOnlyList<Point3> BoundaryMm { get; } = CadContract.Copy(boundaryMm).Select(Point).ToList().AsReadOnly();
}
