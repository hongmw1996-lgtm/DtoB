using System.Security.Cryptography;
using System.Text;
using DtoB.Cad.Dxf;
using DtoB.Cad.Raw;
using DtoB.Cad.V2;
using DtoB.Cad.Normalize.Scalars;
using Xunit;

namespace DtoB.Tests;

internal static class Cad04BScalarFixture
{
    internal static readonly CadSourceDocument Document = new(new("11111111-1111-1111-1111-111111111111"),
        new("22222222-2222-2222-2222-222222222222"), new string('A', 64));
    internal static DxfSourceFact Real(double value) => new(DxfFactKind.Real, DxfFactPresence.Present,
        DxfFactValidity.Valid, null, null, value, [3], new(3, 3, 7, 8));
    internal static DxfSourceFact Integer(int value) => new(DxfFactKind.Integer, DxfFactPresence.Present,
        DxfFactValidity.Valid, null, value, null, [3], new(3, 3, 7, 8));
    internal static DxfSourceFact Absent(DxfFactKind kind) => new(kind, DxfFactPresence.Absent,
        DxfFactValidity.NotApplicable, null, null, null, [], null);
    internal static CadScalarFact Fact(CadScalarRole role, DxfSourceFact fact) => CadScalarFact.FromRaw(Document, role, fact);
    internal static CadScalarFact Angle(double value, CadScalarRole role = CadScalarRole.TextRotation) => Fact(role, Real(value));
    internal static CadUnitResolution Units(int value = 4) => CadUnitNormalizer.Resolve(Fact(CadScalarRole.HeaderInsertionUnits, Integer(value)));
    internal static Task<RawDrawingSnapshot> Snapshot(string text) => Snapshot(Encoding.UTF8.GetBytes(text));
    internal static async Task<RawDrawingSnapshot> Snapshot(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        var lexical = await new TextDxfLexicalReader().ReadAsync(stream);
        var structure = new TextDxfStructuralReader().Read(lexical);
        var geometry = new TextDxfGeometryReader().Read(structure);
        var annotations = new TextDxfBlockAnnotationReader().Read(geometry);
        var graph = new TextDxfIdentityGraphReader().Read(annotations,
            new(Document.DrawingId.ToString(), Document.RevisionId.ToString(), Convert.ToHexString(SHA256.HashData(stream.ToArray()))));
        var result = new TextDxfRawDrawingSnapshotAssembler().Assemble(graph);
        Assert.Equal(RawSnapshotAssemblyStatus.Succeeded, result.Status); return Assert.IsType<RawDrawingSnapshot>(result.Snapshot);
    }
    internal static string Dxf(string entities, string tables = "", string units = "70\n4\n") =>
        "0\nSECTION\n2\nHEADER\n9\n$ACADVER\n1\nAC1032\n9\n$INSUNITS\n" + units + "0\nENDSEC\n" + tables
        + "0\nSECTION\n2\nENTITIES\n" + entities + "0\nENDSEC\n0\nEOF\n";
    internal static string MText(string extra) => Dxf("0\nMTEXT\n5\na\n330\n0\n100\nAcDbEntity\n8\n0\n100\nAcDbMText\n10\n0\n20\n0\n40\n2\n1\ntext\n" + extra);
    internal static async Task<RawDrawingSnapshot> Controlled() => await Snapshot(await File.ReadAllBytesAsync(
        Path.Combine(CadV2ContractFixture.Root(), "datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf")));
}
