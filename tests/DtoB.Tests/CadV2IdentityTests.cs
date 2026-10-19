using System.Globalization;
using DtoB.Cad.V2;
using Xunit;

namespace DtoB.Tests;

public sealed class CadV2IdentityTests
{
    [Fact] public void FrozenFramingMatchesIndependentlyEnumeratedBytes()
    {
        // Tuple(3): Text("dtob/cad2/id/v1"), Text("source"), Tuple(1): Text("A").
        // Independently enumerated BE frames: 06 00000032 00000003,
        // 01 0000000f <15 UTF8 bytes>, 01 00000006 <source>, 06 0000000a 00000001 01 00000001 41.
        Assert.Equal("cad2:1c249a29d20262678537453687e3bc3428b736b403bb41db13c2e6061fc036b6",
            CadId.Create(CadIdentityDomain.Source, [CadIdentityField.Text("A")]).Value);
    }
    [Fact] public void PrimitivePreservesDomainTupleTypeAndNullBoundaries()
    {
        var a = CadId.Create(CadIdentityDomain.Source, [CadIdentityField.Text("ab"), CadIdentityField.Text("c")]);
        var b = CadId.Create(CadIdentityDomain.Source, [CadIdentityField.Text("a"), CadIdentityField.Text("bc")]);
        Assert.NotEqual(a.Value, b.Value);
        Assert.NotEqual(a.Value, CadId.Create(CadIdentityDomain.Leaf, [CadIdentityField.Text("ab"), CadIdentityField.Text("c")]).Value);
        Assert.NotEqual(CadId.Create(CadIdentityDomain.Source, [CadIdentityField.Index(1)]).Value,
            CadId.Create(CadIdentityDomain.Source, [CadIdentityField.Text("1")]).Value);
        Assert.NotEqual(CadId.Create(CadIdentityDomain.Source, [CadIdentityField.Null()]).Value,
            CadId.Create(CadIdentityDomain.Source, [CadIdentityField.Text("")]).Value);
        Assert.NotEqual(CadId.Create(CadIdentityDomain.Source, [CadIdentityField.Tuple([CadIdentityField.Text("a")])]).Value,
            CadId.Create(CadIdentityDomain.Source, [CadIdentityField.Text("a")]).Value);
    }
    [Theory] [InlineData("")] [InlineData("fr-FR")] [InlineData("ko-KR")]
    public void TypedSourceTupleIsCultureIndependent(string culture)
    {
        static CadId Make() => CadId.Create(CadIdentityDomain.Source, [CadIdentityField.GuidValue(CadV2ContractFixture.Drawing),
            CadIdentityField.GuidValue(CadV2ContractFixture.Revision), CadIdentityField.FileHash(CadV2ContractFixture.Hash),
            CadIdentityField.Handle("A"), CadIdentityField.Index(1234)]);
        var expected = Make().Value; var old = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture); var actual = Make().Value; Assert.Equal(expected, actual);
            var output = Environment.GetEnvironmentVariable("DTOB_04A_EVIDENCE_DIRECTORY");
            if (output is not null)
            {
                Directory.CreateDirectory(output);
                File.WriteAllText(Path.Combine(output, "identity-" + (culture.Length == 0 ? "invariant" : culture) + ".txt"), actual);
            }
        }
        finally {CultureInfo.CurrentCulture = old;}
    }
    [Fact] public void NestedTupleOwnsInputAndDifferentCellContextDoesNotChangeOriginalSourceHandle()
    {
        var parts = new List<CadIdentityField> { CadIdentityField.Handle("A"), CadIdentityField.Index(0) };
        var field = CadIdentityField.Tuple(parts); var before = CadId.Create(CadIdentityDomain.Occurrence, [field]);
        parts.Clear(); Assert.Equal(before.Value, CadId.Create(CadIdentityDomain.Occurrence, [field]).Value);
        Assert.NotEqual(before.Value, CadId.Create(CadIdentityDomain.Occurrence, [CadIdentityField.Tuple([CadIdentityField.Handle("A"), CadIdentityField.Index(1)])]).Value);
        Assert.Equal("000a", CadV2ContractFixture.Registry().Records[0].OriginalHandle);
    }
    [Fact] public void InvalidIdentityComponentsAreRejected()
    {
        Assert.Throws<InvalidDataException>(() => new CadId("cad2:wrong"));
        Assert.Throws<InvalidDataException>(() => CadIdentityField.GuidValue(Guid.Empty));
        Assert.Throws<InvalidDataException>(() => CadIdentityField.Index(-1));
        Assert.Throws<InvalidDataException>(() => CadIdentityField.Handle("00a"));
        Assert.Throws<InvalidDataException>(() => CadIdentityField.FileHash("short"));
        Assert.Throws<InvalidDataException>(() => CadId.Create((CadIdentityDomain)999, []));
    }
}
