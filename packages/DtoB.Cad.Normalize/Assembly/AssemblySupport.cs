using System.Numerics;
using DtoB.Cad.V2;

namespace DtoB.Cad.Normalize.Assembly;

/// <summary>One session's monotonic admission count. Failed post-admission work is not refunded.</summary>
public sealed class CadMaterializationBudget(int maximum)
{
    private readonly int limit = maximum > 0 ? maximum : throw new ArgumentOutOfRangeException(nameof(maximum));
    public long Allocated { get; private set; }
    public bool TryReserve()
    {
        if (Allocated >= limit) return false;
        Allocated++;
        return true;
    }
}

public static class CadAssemblyIdentity
{
    public static CadId Source(CadProvenanceRegistry sources, CadSourceRecordRef reference)
    {
        var r = sources.Resolve(reference); var d = sources.Source;
        var token = r.CanonicalHandle is {} h ? CadIdentityField.Handle(h)
            : CadIdentityField.Tuple([CadIdentityField.Text("record"),CadIdentityField.Text(r.Locator.Domain.ToString()),CadIdentityField.Index(r.Locator.RecordIndex)]);
        return CadId.Create(CadIdentityDomain.Source,[CadIdentityField.GuidValue(d.DrawingId),CadIdentityField.GuidValue(d.RevisionId),CadIdentityField.FileHash(d.FileHash),token]);
    }
    public static CadId Component(CadId source, string role, int? ordinal = null) => CadId.Create(CadIdentityDomain.DerivedComponent,
        ordinal is {} n ? [CadIdentityField.Text(source.Value),CadIdentityField.Text(role),CadIdentityField.Index(n)]
            : [CadIdentityField.Text(source.Value),CadIdentityField.Text(role)]);
    public static CadId Leaf(CadId sourceEntity, CadId occurrence) => CadId.Create(CadIdentityDomain.Leaf,
        [CadIdentityField.Text(sourceEntity.Value),CadIdentityField.Text(occurrence.Value)]);
}

/// <summary>Exact sign of the determinant of stored finite IEEE doubles; no determinant magnitude overflow or tolerance guessing.</summary>
public static class CadOrientationValidation
{
    public static int Sign(CadTransform transform)
    {
        var m = transform.Values; var rows = new BigInteger[3][];
        for (var row = 0; row < 3; row++)
        {
            var values = Enumerable.Range(0,3).Select(c => Dyadic(m[row*4+c])).ToArray();
            var exponent = values.Min(v => v.Exponent);
            rows[row] = values.Select(v => v.Mantissa << (v.Exponent-exponent)).ToArray();
        }
        var a=rows[0];var b=rows[1];var c=rows[2];
        return (a[0]*(b[1]*c[2]-b[2]*c[1])-a[1]*(b[0]*c[2]-b[2]*c[0])+a[2]*(b[0]*c[1]-b[1]*c[0])).Sign;
    }
    private static (BigInteger Mantissa,int Exponent) Dyadic(double value)
    {
        var bits=BitConverter.DoubleToUInt64Bits(value);var e=(int)((bits>>52)&0x7ff);var fraction=bits&0xfffffffffffff;
        var mantissa=new BigInteger(e==0?fraction:fraction|0x10000000000000);
        return ((bits>>63)==0?mantissa:-mantissa,e==0?-1074:e-1023-52);
    }
}
