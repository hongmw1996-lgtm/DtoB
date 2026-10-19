namespace DtoB.Cad.V2;

public enum CadColorKind { Aci, Rgb }
public sealed class CadColor
{
    public CadColorKind Kind { get; }
    public int Value { get; }
    public CadColor(CadColorKind kind, int value)
    {
        Kind = CadContract.EnumValue(kind); Value = value;
        CadContract.Require(kind == CadColorKind.Aci ? value is >= 1 and <= 255 : value is >= 0 and <= 0xFFFFFF, "Invalid effective color.");
    }
}

/// <summary>Explicit state/value/evidence. All nullable members are serialized explicitly, never omitted.</summary>
public sealed class CadResolvedProperty<T> where T : class
{
    public CadPropertyMode SourceMode { get; }
    public CadResolutionState State { get; }
    public T? Value { get; }
    public string? RuleId { get; }
    public IReadOnlyList<CadSourceFactRef> Evidence { get; }
    public CadResolvedProperty(CadPropertyMode sourceMode, CadResolutionState state, T? value,
        string? ruleId, IReadOnlyList<CadSourceFactRef> evidence)
    {
        CadContract.Require(typeof(T) == typeof(string) || typeof(T) == typeof(CadColor), "Only immutable CAD name/color values are supported.");
        SourceMode = CadContract.EnumValue(sourceMode); State = CadContract.EnumValue(state); Value = value; RuleId = ruleId;
        Evidence = CadContract.Copy(evidence);
        CadContract.Require((state is CadResolutionState.Known or CadResolutionState.Inherited or CadResolutionState.Defaulted) == (value is not null), "Resolution state/value mismatch.");
        if (state is CadResolutionState.Inherited or CadResolutionState.Defaulted) CadContract.Text(ruleId!);
        if (value is string name) CadContract.Text(name);
    }
}
public sealed class CadStyle(CadId id, CadResolvedProperty<string> layer, CadResolvedProperty<CadColor> color,
    CadResolvedProperty<string> linetype)
{
    public CadId Id { get; } = CadContract.Required(id);
    public CadResolvedProperty<string> Layer { get; } = CadContract.Required(layer);
    public CadResolvedProperty<CadColor> Color { get; } = CadContract.Required(color);
    public CadResolvedProperty<string> Linetype { get; } = CadContract.Required(linetype);
}
public sealed class CadLayer(string name, CadSourceRecordRef source, CadId? styleId, bool? isOff, bool? isFrozen)
{
    public string Name { get; } = CadContract.Text(name);
    public CadSourceRecordRef Source { get; } = CadContract.Required(source);
    public CadId? StyleId { get; } = styleId;
    public bool? IsOff { get; } = isOff;
    public bool? IsFrozen { get; } = isFrozen;
}
