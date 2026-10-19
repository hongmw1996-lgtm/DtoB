using DtoB.Cad.Normalize.Coordinates;
using DtoB.Cad.Raw;

namespace DtoB.Cad.Normalize.Placement;

public static class CadInsertScaleNormalizer
{
    public static CadInsertScaleResult Normalize(CadPlacementSource source, RawCoordinateFacts raw)
    {
        ArgumentNullException.ThrowIfNull(source); ArgumentNullException.ThrowIfNull(raw);
        var facts = new CadCoordinateTriple(raw); var defaults = new List<string>(); var values = new List<double>();
        foreach (var (fact,axis) in new[]{(facts.X,"X"),(facts.Y,"Y"),(facts.Z,"Z")})
        {
            if (fact.Absent) { values.Add(1); defaults.Add("DXF.INSERT.Scale."+axis+".DefaultOne"); }
            else if (!fact.Valid || !double.IsFinite(fact.Value!.Value) || fact.Value == 0)
                return new(CadPlacementStatus.Invalid,source,facts,null,defaults,[new(CadPlacementDiagnosticCode.InvalidInsertScale,source,"Scale."+axis,fact.PairIndices)]);
            else values.Add(fact.Value!.Value);
        }
        return new(CadPlacementStatus.Normalized,source,facts,new(values[0],values[1],values[2]),defaults,[]);
    }
}
