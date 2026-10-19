using DtoB.Geometry;

namespace DtoB.Cad.V2;

/// <summary>Immutable matrix storage only. No placement, composition, conversion or bounds engine.</summary>
public sealed class CadTransform
{
    private readonly double[] matrix;
    public IReadOnlyList<double> Values { get; }
    public CadTransform(IReadOnlyList<double> values)
    {
        matrix = CadContract.Copy(values).Select(CadContract.Finite).ToArray();
        new Transform3(matrix).ValidateStructure();
        Values = Array.AsReadOnly(matrix);
    }
    public static CadTransform FromTransform3(Transform3 source) => new(CadContract.Required(source).Matrix);
    public Transform3 ToTransform3() => new((double[])matrix.Clone());
    public static CadTransform Identity => new([1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1]);
}
