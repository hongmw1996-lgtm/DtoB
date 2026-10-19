using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace DtoB.Cad.V2;

public enum CadIdentityDomain { Source, Definition, SourceInsert, Occurrence, Leaf, DerivedComponent }

/// <summary>Immutable typed fields for the ADR-020 framing primitive, not an occurrence expander.</summary>
public sealed class CadIdentityField
{
    // Frozen encoding: 0=null, 1=text, 2=GUID, 3=hash, 4=handle, 5=index, 6=tuple.
    private readonly byte tag;
    private readonly byte[] payload;
    private static readonly UTF8Encoding Utf8 = new(false, true);
    private CadIdentityField(byte tag, byte[] payload) { this.tag = tag; this.payload = payload; }
    private static CadIdentityField Scalar(byte tag, string value) => new(tag, Utf8.GetBytes(value));
    public static CadIdentityField Null() => new(0, []);
    public static CadIdentityField Text(string value) => Scalar(1, CadContract.Required(value));
    public static CadIdentityField GuidValue(Guid value)
    { CadContract.Require(value != Guid.Empty, "Identity GUID required."); return Scalar(2, value.ToString("N", CultureInfo.InvariantCulture)); }
    public static CadIdentityField FileHash(string value) => Scalar(3, CadContract.Hash(value));
    public static CadIdentityField Handle(string value)
    { CadContract.Require(CadContract.CanonicalHandle(value) == value, "Canonical identity handle required."); return Scalar(4, value); }
    public static CadIdentityField Index(int value) => Scalar(5, CadContract.Index(value).ToString(CultureInfo.InvariantCulture));
    public static CadIdentityField Tuple(IReadOnlyList<CadIdentityField> fields)
    {
        var owned = CadContract.Copy(fields);
        using var bytes = new MemoryStream(); WriteUInt(bytes, owned.Count);
        foreach (var field in owned) field.Write(bytes);
        return new(6, bytes.ToArray());
    }
    private static void WriteUInt(Stream output, int value)
    { Span<byte> length = stackalloc byte[4]; BinaryPrimitives.WriteUInt32BigEndian(length, checked((uint)value)); output.Write(length); }
    internal void Write(Stream output) { output.WriteByte(tag); WriteUInt(output, payload.Length); output.Write(payload); }
}

public sealed class CadId
{
    public string Value { get; }
    public CadId(string value)
    {
        CadContract.Require(value is not null && value.Length == 69 && value.StartsWith("cad2:", StringComparison.Ordinal)
            && value.AsSpan(5).ToArray().All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f'), "cad2 lowercase SHA-256 identity required.");
        Value = value!;
    }
    public static CadId Create(CadIdentityDomain domain, IReadOnlyList<CadIdentityField> fields)
    {
        CadContract.EnumValue(domain);
        var name = domain switch
        {
            CadIdentityDomain.Source => "source", CadIdentityDomain.Definition => "definition",
            CadIdentityDomain.SourceInsert => "source-insert", CadIdentityDomain.Occurrence => "occurrence",
            CadIdentityDomain.Leaf => "leaf", CadIdentityDomain.DerivedComponent => "derived-component",
            _ => throw new InvalidDataException("Unknown ID domain.")
        };
        using var bytes = new MemoryStream();
        CadIdentityField.Tuple([CadIdentityField.Text("dtob/cad2/id/v1"), CadIdentityField.Text(name), CadIdentityField.Tuple(fields)]).Write(bytes);
        return new("cad2:" + Convert.ToHexString(SHA256.HashData(bytes.ToArray())).ToLowerInvariant());
    }
    public override string ToString() => Value;
}

public sealed class CadOccurrenceStep
{
    public CadSourceRecordRef InsertSource { get; }
    public CadSourceRecordRef DefinitionSource { get; }
    public int? Row { get; }
    public int? Column { get; }
    public CadOccurrenceStep(CadSourceRecordRef insertSource, CadSourceRecordRef definitionSource, int? row, int? column)
    {
        InsertSource = CadContract.Required(insertSource); DefinitionSource = CadContract.Required(definitionSource);
        CadContract.Require((row is null) == (column is null) && (row is null || row >= 0 && column >= 0), "Both nonnegative cell indices or neither required.");
        Row = row; Column = column;
    }
}
public sealed class CadOccurrencePath(IReadOnlyList<CadOccurrenceStep> steps)
{
    /// <summary>Outer-to-inner source order. Construction does not expand any INSERT.</summary>
    public IReadOnlyList<CadOccurrenceStep> Steps { get; } = CadContract.Copy(steps);
}
