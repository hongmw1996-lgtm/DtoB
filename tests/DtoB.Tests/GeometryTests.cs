using DtoB.Core;
using DtoB.Geometry;
using DtoB.Revit.Core;
using Xunit;

namespace DtoB.Tests;
public class GeometryTests
{
    [Theory][InlineData(304.8, 1)][InlineData(0, 0)][InlineData(-609.6, -2)][InlineData(3000, 9.84251968503937)]
    public void ConversionRoundTripsAtAdapterBoundary(double mm, double feet)
    { Assert.Equal(feet, RevitUnits.ToInternalFeet(mm), 12); Assert.Equal(mm, RevitUnits.FromInternalFeet(RevitUnits.ToInternalFeet(mm)), 10); }
    [Fact] public void CounterclockwiseRotationThenTranslationUsesMillimeters()
    {
        var p = Transform3.Placement(new(100, 200, 300), Math.PI / 2, new(1, 1, 1)).Apply(new(10, 0, 2));
        Assert.Equal(100, p.X, 10); Assert.Equal(210, p.Y, 10); Assert.Equal(302, p.Z, 10);
    }
    [Fact] public void MirroringAndNestedCompositionHaveExplicitOrder()
    {
        var inner = Transform3.Placement(new(10, 0, 0), 0, new(-1, 1, 1));
        var outer = Transform3.Placement(new(100, 50, 0), Math.PI / 2, new(1, 1, 1));
        var point = new Point3(2, 3, 4); var p = inner.Then(outer).Apply(point);
        Assert.Equal(97, p.X, 10); Assert.Equal(58, p.Y, 10); Assert.Equal(4, p.Z, 10);
        var expected = outer.Apply(inner.Apply(point)); Assert.Equal(expected.X, p.X, 10); Assert.Equal(expected.Y, p.Y, 10);
    }
    [Fact] public void NonfiniteGeometryAndHiddenInvalidToleranceAreRejected()
    {
        Assert.Throws<InvalidDataException>(() => new Point3(double.NaN, 0, 0).Validate());
        Assert.Throws<InvalidDataException>(() => RevitUnits.ToInternalFeet(double.PositiveInfinity));
        Assert.Throws<InvalidDataException>(() => new GeometryTolerance(0, 0.1).Validate());
        Assert.Throws<InvalidDataException>(() => new Transform3([1]).Validate());
        new GeometryTolerance(0.01, 0.001).Validate(); // Explicit test profile; not a production default.
    }
}
