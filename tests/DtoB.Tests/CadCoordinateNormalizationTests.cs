using DtoB.Cad.Normalize.Coordinates;
using DtoB.Cad.Raw;
using Xunit;
using static DtoB.Tests.Cad04CCoordinateFixture;

namespace DtoB.Tests;

public sealed class CadOcsPointNormalizationTests
{
    [Theory][InlineData(0,0,1,2,3,4)][InlineData(0,0,-1,-2,3,-4)]
    [InlineData(0,1,0,-2,4,3)][InlineData(1,0,0,4,2,3)][InlineData(0,2,0,-2,4,3)]
    public void HandPointOracles(double nx,double ny,double nz,double x,double y,double z) =>
        Point(CadCoordinateNormalizer.NormalizePoint(Input(normal:Triple(nx,ny,nz)),Units(),Policy),x,y,z);
    [Fact] public void ObliqueCoordinateOracleWasDerivedIndependently()
    {
        // Ax=(-1/sqrt2,1/sqrt2,0), Ay=(-1/sqrt6,-1/sqrt6,2/sqrt6), N=(1,1,1)/sqrt3.
        Point(CadCoordinateNormalizer.NormalizePoint(Input(normal:Triple(1,1,1)),Units(),Policy),
            -0.32955735700618094,2.4988697677400094,4.758890819541682);
    }
    [Theory][InlineData(1,25.4)][InlineData(2,304.8)][InlineData(4,1)][InlineData(5,10)][InlineData(6,1000)]
    public void CoordinatesUseExisting04BScaleExactlyOnce(int code,double scale)
    {
        var units=Units(code);
        var r=CadCoordinateNormalizer.NormalizePoint(Input(point:Triple(-2,3,-4),normal:Triple(0,1,0)),units,Policy);
        Point(r,2*scale,-4*scale,3*scale); Assert.Same(units,r.Units);
        Assert.Equal(scale,r.Units!.Decision.SourceToMillimeters);
    }
    [Theory][InlineData(double.NaN)][InlineData(double.PositiveInfinity)][InlineData(double.NegativeInfinity)]
    public void NonfiniteSourceNeverGetsClamped(double value)
    {
        var r=CadCoordinateNormalizer.NormalizePoint(Input(point:Triple(value,2,3)),Units(),Policy);
        Assert.Equal(CadCoordinateStatus.Invalid,r.Status); Assert.Null(r.PointMm);
        Assert.Equal(CadCoordinateDiagnosticCode.NonFiniteSourceCoordinate,Assert.Single(r.Diagnostics).Code);
    }
    [Theory][InlineData(true)][InlineData(false)]
    public void ScalingAndBasisOverflowAreExplicitFailures(bool scale)
    {
        var r=CadCoordinateNormalizer.NormalizePoint(Input(point:Triple(-double.MaxValue,-double.MaxValue,double.MaxValue),normal:Triple(1,1,1)),Units(scale?6:4),Policy);
        Assert.Equal(CadCoordinateStatus.Invalid,r.Status); Assert.Null(r.PointMm);
        Assert.Equal(CadCoordinateDiagnosticCode.NonFiniteNormalizedCoordinate,Assert.Single(r.Diagnostics).Code);
        Assert.Equal(scale?"UnitScale":"Basis",r.Diagnostics[0].Field);
    }
    [Fact] public void MissingZDoesNotGloballyFlattenAThreeDimensionalPoint()
    {
        var r=CadCoordinateNormalizer.NormalizePoint(Input(point:new(Fact(2),Fact(3),Absent)),Units(),Policy);
        Assert.Equal(CadCoordinateDiagnosticCode.MissingRequiredCoordinateFact,Assert.Single(r.Diagnostics).Code); Assert.Equal("Z",r.Diagnostics[0].Field);
    }
    [Theory][InlineData(0,CadCoordinateDiagnosticCode.NeedsExplicitUnits)][InlineData(3,CadCoordinateDiagnosticCode.UnsupportedUnits)]
    [InlineData(-1,CadCoordinateDiagnosticCode.InvalidUnits)]
    public void UnitRefusalsDoNotBecomeGuessedCoordinates(int code,CadCoordinateDiagnosticCode expected)
    { var r=CadCoordinateNormalizer.NormalizePoint(Input(),Units(code),Policy); Assert.Null(r.PointMm); Assert.Equal(expected,Assert.Single(r.Diagnostics).Code); }
    [Theory][InlineData("duplicate")][InlineData("missing-span")][InlineData("partial")]
    public void MalformedCoordinateEvidenceIsNotRepaired(string reason)
    {
        var x=reason=="partial"?Absent:reason=="duplicate"?
            new DxfSourceFact(DxfFactKind.Real,DxfFactPresence.Present,DxfFactValidity.Invalid,null,null,null,[3,4],new(3,4,7,10)):
            new(DxfFactKind.Real,DxfFactPresence.Present,DxfFactValidity.Valid,null,null,2,[3],null);
        var r=CadCoordinateNormalizer.NormalizePoint(Input(point:new(x,Fact(3,5),Fact(4,6))),Units(),Policy);
        Assert.Equal(CadCoordinateStatus.Invalid,r.Status); Assert.Null(r.PointMm);
        Assert.Equal(reason=="partial"?CadCoordinateDiagnosticCode.MissingRequiredCoordinateFact:CadCoordinateDiagnosticCode.InvalidSourceFact,Assert.Single(r.Diagnostics).Code);
        Assert.Equal(x.SourcePairIndices,r.Input.Coordinates.X.PairIndices);
    }
}

public sealed class CadOcsVectorNormalizationTests
{
    [Theory][InlineData(false)][InlineData(true)]
    public void VectorPathHasExplicitDimensionalSemanticsAndNoTranslation(bool dimensional)
    {
        var role=dimensional?CadCoordinateRole.OcsDisplacementVector:CadCoordinateRole.OcsDirectionVector;
        var input=Input(role,normal:Triple(0,1,0));
        var r=CadCoordinateNormalizer.NormalizeVector(input,Units(5),Policy);
        Assert.Equal(CadCoordinateStatus.Normalized,r.Status); Assert.Null(r.PointMm);
        Vector(r.Vector,dimensional?-20:-2,dimensional?40:4,dimensional?30:3);
        Assert.Equal(CadCoordinateKind.Vector,CadCoordinateRoles.Describe(role)!.Kind);
        Assert.Equal(CadCoordinateStatus.Unsupported,CadCoordinateNormalizer.NormalizePoint(input,Units(),Policy).Status);
    }
    [Fact] public void PointAndVectorApisRemainDistinctDespiteTranslationFreeBasis()
    {
        var f=Frame(0,1,0); var p=f.PointToWcs(new(2,3,4)); var v=f.VectorToWcs(new(2,3,4));
        Assert.Equal((-2d,4d,3d),(p.X,p.Y,p.Z)); Vector(v,-2,4,3);
        Assert.NotEqual(typeof(DtoB.Geometry.Point3),typeof(CadVector3));
        Assert.DoesNotContain(typeof(CadOcsFrame).GetProperties(),p => p.Name.Contains("Translation",StringComparison.Ordinal));
    }
    [Fact] public void DimensionlessDirectionDoesNotRequireDrawingUnitResolution()
    { Vector(CadCoordinateNormalizer.NormalizeVector(Input(CadCoordinateRole.OcsDirectionVector,normal:Triple(0,1,0)),null,Policy).Vector,-2,4,3); }
}

public sealed class CadWcsCoordinateNormalizationTests
{
    [Theory][InlineData(CadCoordinateRole.LineStartWcs)][InlineData(CadCoordinateRole.LineEndWcs)]
    [InlineData(CadCoordinateRole.PointPositionWcs)][InlineData(CadCoordinateRole.MTextInsertionWcs)]
    [InlineData(CadCoordinateRole.DimensionDefinitionWcs)][InlineData(CadCoordinateRole.DimensionFirstExtensionWcs)]
    [InlineData(CadCoordinateRole.DimensionSecondExtensionWcs)]
    public void WcsCoordinatesStayWcsRegardlessOfExtrusion(CadCoordinateRole role)
    {
        var r=CadCoordinateNormalizer.NormalizePoint(Input(role,normal:Triple(0,1,0)),Units(5),Policy);
        Point(r,20,30,40); Assert.Null(r.Frame); Assert.Empty(r.DefaultRules);
        Assert.Equal(CadCoordinateSpace.Wcs,r.Semantics!.Space); Assert.Equal(CadCoordinateKind.Point,r.Semantics.Kind);
    }
    [Fact] public void InvalidUnusedExtrusionCannotRotateOrInvalidateWcsCoordinates()
    { Point(CadCoordinateNormalizer.NormalizePoint(Input(CadCoordinateRole.PointPositionWcs,normal:Triple(0,0,0)),Units(),Policy),2,3,4); }
    [Fact] public void MTextDirectionIsWcsDimensionlessFactNotLengthOrOrientationReconstruction()
    {
        var r=CadCoordinateNormalizer.NormalizeVector(Input(CadCoordinateRole.MTextDirectionWcs,Triple(2,3,4),Triple(0,1,0)),Units(6),Policy);
        Vector(r.Vector,2,3,4); Assert.Null(r.Frame);
    }
    [Fact] public void OptionalAbsentDirectionIsNotInvented()
    {
        var r=CadCoordinateNormalizer.NormalizeVector(Input(CadCoordinateRole.MTextDirectionWcs,AbsentTriple),null,Policy);
        Assert.Equal(CadCoordinateStatus.Absent,r.Status); Assert.Null(r.Vector);
    }
}

public sealed class CadElevationNormalizationTests
{
    [Theory][InlineData(5,4,-2,5,3)][InlineData(0,4,-2,0,3)][InlineData(-7,4,-2,-7,3)]
    [InlineData(5,5,-20,50,30)]
    public async Task RealLwPolylineElevationIsInsertedOnceBeforeBasis(double elevation,int units,double x,double y,double z)
    {
        var text=Entity("LWPOLYLINE",$"90\n1\n38\n{elevation.ToString("R",System.Globalization.CultureInfo.InvariantCulture)}\n10\n2\n20\n3\n210\n0\n220\n1\n230\n0\n");
        text=text.Replace("70\n4\n0\nENDSEC",$"70\n{units}\n0\nENDSEC",StringComparison.Ordinal);
        var s=await Cad04BScalarFixture.Snapshot(text); var input=RawCoordinateAccess.Record(s,Assert.Single(s.Records),CadCoordinateRole.LwPolylineVertexOcs);
        var unit=DtoB.Cad.Normalize.Scalars.CadUnitNormalizer.Resolve(DtoB.Cad.Normalize.Scalars.RawScalarAccess.HeaderUnits(s));
        var r=CadCoordinateNormalizer.NormalizePoint(input,unit,Policy); Point(r,x,y,z);
        Assert.Equal(elevation,input.Elevation.Value); Assert.False(input.Coordinates.Z.Mapped);
        Assert.Equal(0,input.Source.VertexOrdinal); Assert.Empty(r.DefaultRules);
    }
    [Fact] public async Task AbsentElevationHasItsOwnRuleAndDoesNotFabricateSourceZ()
    {
        var s=await Cad04BScalarFixture.Snapshot(Entity("LWPOLYLINE","90\n1\n10\n2\n20\n3\n"));
        var input=RawCoordinateAccess.Record(s,Assert.Single(s.Records),CadCoordinateRole.LwPolylineVertexOcs);
        var r=CadCoordinateNormalizer.NormalizePoint(input,DtoB.Cad.Normalize.Scalars.CadUnitNormalizer.Resolve(DtoB.Cad.Normalize.Scalars.RawScalarAccess.HeaderUnits(s)),Policy);
        Point(r,2,3,0); Assert.Contains(CadCoordinateNormalizer.ElevationDefaultZeroRule,r.DefaultRules);
        Assert.Contains(CadExtrusionNormalizer.DefaultPositiveZRule,r.DefaultRules);
        Assert.Empty(input.Elevation.PairIndices); Assert.False(input.Coordinates.Z.Mapped);
    }
    [Fact] public void DuplicateElevationSourcesAreRejectedRatherThanAdded()
    {
        var r=CadCoordinateNormalizer.NormalizePoint(Input(CadCoordinateRole.LwPolylineVertexOcs,elevation:Fact(5)),Units(),Policy);
        Assert.Equal(CadCoordinateDiagnosticCode.ConflictingCoordinateFacts,Assert.Single(r.Diagnostics).Code);
        r=CadCoordinateNormalizer.NormalizePoint(Input(elevation:Fact(5)),Units(),Policy);
        Assert.Equal(CadCoordinateDiagnosticCode.ConflictingCoordinateFacts,Assert.Single(r.Diagnostics).Code);
    }
}
