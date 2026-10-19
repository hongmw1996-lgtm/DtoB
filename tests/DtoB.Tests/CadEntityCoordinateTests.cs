using System.Text.Json;
using DtoB.Cad.Normalize.Coordinates;
using DtoB.Cad.Normalize.Scalars;
using DtoB.Cad.Raw;
using DtoB.Cad.V2;
using Xunit;
using static DtoB.Tests.Cad04CCoordinateFixture;

namespace DtoB.Tests;

public sealed class CadCoordinateRoleTests
{
    [Theory][InlineData(CadCoordinateRole.LineStartWcs,false)][InlineData(CadCoordinateRole.PointPositionWcs,false)]
    [InlineData(CadCoordinateRole.CircleCenterOcs,true)][InlineData(CadCoordinateRole.ArcCenterOcs,true)]
    [InlineData(CadCoordinateRole.TextInsertionOcs,true)][InlineData(CadCoordinateRole.InsertInsertionOcs,true)]
    [InlineData(CadCoordinateRole.DimensionDefinitionWcs,false)][InlineData(CadCoordinateRole.DimensionTextMidpointOcs,true)]
    public void SameGroup10FamilyDoesNotChooseCoordinateSpace(CadCoordinateRole role,bool ocs)
    { var d=CadCoordinateRoles.Describe(role)!; Assert.Equal(ocs?CadCoordinateSpace.Ocs:CadCoordinateSpace.Wcs,d.Space); Assert.Equal(CadCoordinateKind.Point,d.Kind); Assert.True(d.Dimensional); }
    [Fact] public void UnknownAndWrongEntityRolesAreExplicitlyUnsupported()
    {
        var r=CadCoordinateNormalizer.NormalizePoint(Input((CadCoordinateRole)999),Units(),Policy);
        Assert.Equal(CadCoordinateStatus.Unsupported,r.Status); Assert.Equal(CadCoordinateDiagnosticCode.UnsupportedCoordinateRole,Assert.Single(r.Diagnostics).Code);
    }
    [Fact] public void SourceAuthorityMustMatchUnitDecision()
    {
        var doc=new CadSourceDocument(Cad04BScalarFixture.Document.DrawingId,new("33333333-3333-3333-3333-333333333333"),Cad04BScalarFixture.Document.FileHash);
        var r=CadCoordinateNormalizer.NormalizePoint(CadCoordinateInput.FromRaw(doc,CadCoordinateRole.LineStartWcs,Triple(1,2,3)),Units(),Policy);
        Assert.Equal(CadCoordinateDiagnosticCode.MismatchedSourceAuthority,Assert.Single(r.Diagnostics).Code);
    }
}

public sealed class CadEntityCoordinateAccessTests
{
    [Theory][InlineData("LINE",CadCoordinateRole.LineStartWcs,false)][InlineData("POINT",CadCoordinateRole.PointPositionWcs,false)]
    [InlineData("CIRCLE",CadCoordinateRole.CircleCenterOcs,true)][InlineData("ARC",CadCoordinateRole.ArcCenterOcs,true)]
    [InlineData("SOLID",CadCoordinateRole.SolidFirstOcs,true)][InlineData("TEXT",CadCoordinateRole.TextInsertionOcs,true)]
    [InlineData("MTEXT",CadCoordinateRole.MTextInsertionWcs,false)][InlineData("INSERT",CadCoordinateRole.InsertInsertionOcs,true)]
    public async Task ActualTypedFieldsUseTheirSpecifiedCoordinateSpaces(string type,CadCoordinateRole role,bool ocs)
    {
        var text=Entity(type,"10\n2\n20\n3\n30\n4\n11\n5\n21\n6\n31\n7\n12\n8\n22\n9\n32\n10\n13\n11\n23\n12\n33\n13\n40\n2\n50\n0\n51\n90\n1\ntext\n2\nUNDEFINED\n210\n0\n220\n1\n230\n0\n");
        var s=await Cad04BScalarFixture.Snapshot(text); var row=Assert.Single(s.Records);
        var before=JsonSerializer.Serialize(s); var input=RawCoordinateAccess.Record(s,row,role);
        Assert.True(input.RoleSupported); Assert.Equal(2,input.Coordinates.X.Value); Assert.Equal(3,input.Coordinates.Y.Value); Assert.Equal(4,input.Coordinates.Z.Value);
        var result=CadCoordinateNormalizer.NormalizePoint(input,CadUnitNormalizer.Resolve(RawScalarAccess.HeaderUnits(s)),Policy);
        Point(result,ocs?-2:2,ocs?4:3,ocs?3:4);
        Assert.Equal("a",input.Source.OriginalHandle); Assert.Equal("A",input.Source.CanonicalHandle);
        Assert.Equal(row.StructuralRecordIndex,input.Source.Record!.RecordIndex);
        var pair=Assert.Single(input.Coordinates.X.PairIndices); Assert.Equal("2",s.LexicalEvidence.Pairs[pair].RawTextValue);
        Assert.Equal(2*pair+1,input.Coordinates.X.FirstLine); Assert.Equal(2*pair+2,input.Coordinates.X.LastLine);
        Assert.Equal(before,JsonSerializer.Serialize(s));
        Assert.False(RawCoordinateAccess.Record(s,row,(CadCoordinateRole)999).RoleSupported);
    }
    [Fact] public async Task RemainingSolidTextLineAndMTextFieldsPreserveTheirDistinctRoles()
    {
        foreach(var item in new[]{("LINE",CadCoordinateRole.LineEndWcs,5d,6d,7d),
            ("TEXT",CadCoordinateRole.TextAlignmentOcs,-5d,7d,6d),
            ("SOLID",CadCoordinateRole.SolidSecondOcs,-5d,7d,6d),
            ("SOLID",CadCoordinateRole.SolidThirdOcs,-8d,10d,9d),
            ("SOLID",CadCoordinateRole.SolidFourthOcs,-11d,13d,12d)})
        {
            var s=await Cad04BScalarFixture.Snapshot(Entity(item.Item1,"10\n2\n20\n3\n30\n4\n11\n5\n21\n6\n31\n7\n12\n8\n22\n9\n32\n10\n13\n11\n23\n12\n33\n13\n40\n2\n1\ntext\n210\n0\n220\n1\n230\n0\n"));
            Point(CadCoordinateNormalizer.NormalizePoint(RawCoordinateAccess.Record(s,Assert.Single(s.Records),item.Item2),CadUnitNormalizer.Resolve(RawScalarAccess.HeaderUnits(s)),Policy),item.Item3,item.Item4,item.Item5);
        }
    }
    [Theory][InlineData(0)][InlineData(1)]
    public async Task DimensionMixedCoordinatesAreNormalizedByFieldNotByEntity(int subtype)
    {
        var s=await Cad04BScalarFixture.Snapshot(Entity("DIMENSION",$"70\n{subtype}\n10\n2\n20\n3\n30\n4\n11\n2\n21\n3\n31\n4\n13\n5\n23\n6\n33\n7\n14\n8\n24\n9\n34\n10\n210\n0\n220\n1\n230\n0\n"));
        var row=Assert.Single(s.Records); var units=CadUnitNormalizer.Resolve(RawScalarAccess.HeaderUnits(s));
        foreach(var item in new[]{(CadCoordinateRole.DimensionDefinitionWcs,2d,3d,4d),(CadCoordinateRole.DimensionTextMidpointOcs,-2d,4d,3d),
            (CadCoordinateRole.DimensionFirstExtensionWcs,5d,6d,7d),(CadCoordinateRole.DimensionSecondExtensionWcs,8d,9d,10d)})
            Point(CadCoordinateNormalizer.NormalizePoint(RawCoordinateAccess.Record(s,row,item.Item1),units,Policy),item.Item2,item.Item3,item.Item4);
    }
    [Fact] public async Task UnsupportedDimensionSubtypeDoesNotPretendUnmappedFieldsAreAbsent()
    {
        var s=await Cad04BScalarFixture.Snapshot(Entity("DIMENSION","70\n2\n10\n2\n20\n3\n30\n4\n"));
        var input=RawCoordinateAccess.Record(s,Assert.Single(s.Records),CadCoordinateRole.DimensionFirstExtensionWcs);
        Assert.False(input.RoleSupported); Assert.False(input.Coordinates.X.Mapped);
        Assert.Equal(CadCoordinateStatus.Unsupported,CadCoordinateNormalizer.NormalizePoint(input,CadUnitNormalizer.Resolve(RawScalarAccess.HeaderUnits(s)),Policy).Status);
    }
    [Fact] public async Task InsertAnchorIgnoresScaleRotationAndDefinitionPlacement()
    {
        var s=await Cad04BScalarFixture.Snapshot(Entity("INSERT","2\nUNDEFINED\n10\n2\n20\n3\n30\n4\n41\n200\n42\n-300\n43\n400\n50\n90\n210\n0\n220\n1\n230\n0\n"));
        var input=RawCoordinateAccess.Record(s,Assert.Single(s.Records),CadCoordinateRole.InsertInsertionOcs);
        var r=CadCoordinateNormalizer.NormalizePoint(input,CadUnitNormalizer.Resolve(RawScalarAccess.HeaderUnits(s)),Policy);
        Point(r,-2,4,3); Assert.NotNull(r.Frame);
    }
    [Fact] public async Task ControlledSourceLineEndHasIndependentPairAndCoordinateOracle()
    {
        var s=await Cad04BScalarFixture.Controlled(); var row=s.Records.Single(r=>r.SourceRecord.Source.StartPairIndex==2856);
        var before=JsonSerializer.Serialize(s); var input=RawCoordinateAccess.Record(s,row,CadCoordinateRole.LineEndWcs);
        Assert.Equal(900,input.Coordinates.X.Value); Assert.Equal(0,input.Coordinates.Y.Value); Assert.Equal(0,input.Coordinates.Z.Value);
        Assert.Equal(2865,Assert.Single(input.Coordinates.X.PairIndices));
        var r=CadCoordinateNormalizer.NormalizePoint(input,CadUnitNormalizer.Resolve(RawScalarAccess.HeaderUnits(s)),Policy);
        Point(r,900,0,0); Assert.Null(r.Frame); Assert.Equal(before,JsonSerializer.Serialize(s));
        var directory=Environment.GetEnvironmentVariable("DTOB_04C_EVIDENCE_DIRECTORY");
        if(directory is not null) { Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory,"controlled-coordinate.json"),JsonSerializer.Serialize(r)); }
    }
    [Fact] public async Task RowOwnershipCancellationAndSourceCollectionsRemainProtected()
    {
        var s=await Cad04BScalarFixture.Snapshot(Entity("POINT","10\n2\n20\n3\n30\n4\n"));
        using var c=new CancellationTokenSource(); c.Cancel();
        Assert.Throws<OperationCanceledException>(()=>RawCoordinateAccess.Record(s,s.Records[0],CadCoordinateRole.PointPositionWcs,cancellationToken:c.Token));
        var other=await Cad04BScalarFixture.Snapshot(Entity("POINT","10\n0\n20\n0\n30\n0\n"));
        Assert.Throws<InvalidDataException>(()=>RawCoordinateAccess.Record(s,other.Records[0],CadCoordinateRole.PointPositionWcs));
        var input=RawCoordinateAccess.Record(s,s.Records[0],CadCoordinateRole.PointPositionWcs);
        Assert.Throws<NotSupportedException>(()=>((IList<int>)input.Coordinates.X.PairIndices)[0]=99);
    }
    [Fact] public async Task ActualMTextDirectionIsDimensionlessWcsWithNonunitExtrusionMetadata()
    {
        var s=await Cad04BScalarFixture.Snapshot(Entity("MTEXT","10\n2\n20\n3\n30\n4\n11\n2\n21\n3\n31\n4\n40\n2\n1\ntext\n210\n0\n220\n2\n230\n0\n"));
        var input=RawCoordinateAccess.Record(s,Assert.Single(s.Records),CadCoordinateRole.MTextDirectionWcs);
        var r=CadCoordinateNormalizer.NormalizeVector(input,CadUnitNormalizer.Resolve(RawScalarAccess.HeaderUnits(s)),Policy);
        Vector(r.Vector,2,3,4); Assert.Null(r.Frame);
        Assert.Single(input.Coordinates.X.PairIndices); Assert.Equal(2,input.Extrusion.Y.Value);
    }
    [Fact] public async Task LwVertexOrdinalsAreProjectedIndividuallyWithoutTopologyOrInventedCoordinates()
    {
        var s=await Cad04BScalarFixture.Snapshot(Entity("LWPOLYLINE","90\n2\n38\n5\n10\n2\n20\n3\n42\n0.5\n10\n7\n20\n8\n"));
        var row=Assert.Single(s.Records); var units=CadUnitNormalizer.Resolve(RawScalarAccess.HeaderUnits(s));
        var second=RawCoordinateAccess.Record(s,row,CadCoordinateRole.LwPolylineVertexOcs,1);
        Point(CadCoordinateNormalizer.NormalizePoint(second,units,Policy),7,8,5); Assert.Equal(1,second.Source.VertexOrdinal);
        Assert.False(RawCoordinateAccess.Record(s,row,CadCoordinateRole.LwPolylineVertexOcs,-1).RoleSupported);
        Assert.False(RawCoordinateAccess.Record(s,row,CadCoordinateRole.LwPolylineVertexOcs,2).RoleSupported);
        Assert.Equal(0.5,Assert.IsType<RawLwPolyline>(row.Geometry!.Payload).Vertices[0].Bulge.Real);
    }
    [Fact] public async Task OptionalAbsentTextAlignmentIsPreservedRatherThanLayoutReconstructed()
    {
        var s=await Cad04BScalarFixture.Snapshot(Entity("TEXT","10\n2\n20\n3\n30\n4\n40\n2\n1\ntext\n"));
        var input=RawCoordinateAccess.Record(s,Assert.Single(s.Records),CadCoordinateRole.TextAlignmentOcs);
        var r=CadCoordinateNormalizer.NormalizePoint(input,CadUnitNormalizer.Resolve(RawScalarAccess.HeaderUnits(s)),Policy);
        Assert.Equal(CadCoordinateStatus.Absent,r.Status); Assert.Null(r.PointMm); Assert.Empty(r.DefaultRules);
    }
}
