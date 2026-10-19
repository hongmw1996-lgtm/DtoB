using DtoB.Cad.Normalize.Occurrences;
using DtoB.Cad.Raw;
using DtoB.Cad.V2;

namespace DtoB.Cad.Normalize.Styles;

public enum CadStyleStatus { Complete, Partial, InvalidInput, Cancelled, ResourceLimitExceeded }
public enum CadDeclaredColorMode { Aci, Rgb, ByLayer, ByBlock, Absent, Invalid, Unsupported }
public enum CadDeclaredLinetypeMode { Explicit, ByLayer, ByBlock, Absent, Invalid }
public enum CadStyleDiagnosticCode { MissingLayerDefinition, AmbiguousLayerDefinition, InvalidLayerReference,
    InvalidColor, InvalidLinetype, MissingLinetypeDefinition, AmbiguousLinetypeDefinition, UnresolvedByBlockContext,
    UnsupportedStyleChannel, InvalidVisibility, InvalidInput, Cancelled, ResourceLimitExceeded }

/// <summary>Owned contextual source scalar; pair/line references address the unchanged paired Raw snapshot.</summary>
public sealed class CadStyleFact
{
    public CadSourceRecordRef Source { get; }
    public int GroupCode { get; }
    public DxfFactPresence Presence { get; }
    public DxfFactValidity Validity { get; }
    public string? Text { get; }
    public int? Integer { get; }
    public IReadOnlyList<int> PairIndices { get; }
    public int? FirstLine { get; }
    public int? LastLine { get; }
    internal CadStyleFact(CadSourceRecordRef source,int code,IReadOnlyList<DxfRawPair> pairs,bool integer)
    {
        Source=source; GroupCode=code; Presence=pairs.Count==0?DxfFactPresence.Absent:DxfFactPresence.Present;
        Text=pairs.Count==1?pairs[0].RawTextValue:null;
        var valid=pairs.Count==1 && (integer?int.TryParse(Text,System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture,out _):!string.IsNullOrWhiteSpace(Text));
        Validity=pairs.Count==0?DxfFactValidity.NotApplicable:valid?DxfFactValidity.Valid:DxfFactValidity.Invalid;
        Integer=valid&&integer?int.Parse(Text!,System.Globalization.CultureInfo.InvariantCulture):null;
        PairIndices=CadContract.Copy(pairs.Select(p=>p.PairIndex).ToArray());
        FirstLine=pairs.Count==0?null:pairs[0].CodeLineNumber; LastLine=pairs.Count==0?null:pairs[^1].ValueLineNumber;
    }
}
public sealed class CadDeclaredStyle
{
    public CadSourceRecordRef Source { get; }
    public CadStyleFact Layer { get; }
    public CadStyleFact Aci { get; }
    public CadStyleFact TrueColor { get; }
    public CadStyleFact ColorName { get; }
    public CadStyleFact Linetype { get; }
    public CadStyleFact Lineweight { get; }
    public CadStyleFact Transparency { get; }
    public CadStyleFact Visibility { get; }
    public bool? SourceVisible { get; }
    public string VisibilityRule { get; }
    public CadDeclaredColorMode ColorMode { get; }
    public CadDeclaredLinetypeMode LinetypeMode { get; }
    internal CadDeclaredStyle(CadSourceRecordRef source,Func<int,bool,CadStyleFact> f)
    {
        Source=source; Layer=f(8,false); Aci=f(62,true); TrueColor=f(420,true); ColorName=f(430,false);
        Linetype=f(6,false); Lineweight=f(370,true); Transparency=f(440,true); Visibility=f(60,true);
        SourceVisible=Visibility.Presence==DxfFactPresence.Absent?true:Visibility.Validity==DxfFactValidity.Valid&&Visibility.Integer is 0 or 1?Visibility.Integer==0:null;
        VisibilityRule=Visibility.Presence==DxfFactPresence.Absent?"DXF.Visibility.AbsentVisible":"DXF.Visibility.Explicit";
        ColorMode=TrueColor.Presence==DxfFactPresence.Present
            ? TrueColor.Validity==DxfFactValidity.Valid&&TrueColor.Integer is >=0 and <=0xFFFFFF?CadDeclaredColorMode.Rgb:CadDeclaredColorMode.Invalid
            : ColorName.Presence==DxfFactPresence.Present?CadDeclaredColorMode.Unsupported
            : Aci.Presence==DxfFactPresence.Absent?CadDeclaredColorMode.Absent
            : Aci.Validity!=DxfFactValidity.Valid?CadDeclaredColorMode.Invalid
            : Aci.Integer switch {0=>CadDeclaredColorMode.ByBlock,256=>CadDeclaredColorMode.ByLayer,>=1 and <=255=>CadDeclaredColorMode.Aci,_=>CadDeclaredColorMode.Invalid};
        LinetypeMode=Linetype.Presence==DxfFactPresence.Absent?CadDeclaredLinetypeMode.Absent
            : Linetype.Validity!=DxfFactValidity.Valid?CadDeclaredLinetypeMode.Invalid
            : StringComparer.OrdinalIgnoreCase.Equals(Linetype.Text,"BYLAYER")?CadDeclaredLinetypeMode.ByLayer
            : StringComparer.OrdinalIgnoreCase.Equals(Linetype.Text,"BYBLOCK")?CadDeclaredLinetypeMode.ByBlock:CadDeclaredLinetypeMode.Explicit;
    }
}
public sealed class CadStyleCatalogEntry
{
    public CadSourceRecordRef Source { get; }
    public CadStyleFact Name { get; }
    public CadStyleFact Aci { get; }
    public CadStyleFact TrueColor { get; }
    public CadStyleFact ColorName { get; }
    public CadStyleFact Linetype { get; }
    public CadStyleFact Flags { get; }
    public CadStyleFact Lineweight { get; }
    public CadStyleFact Transparency { get; }
    public bool? IsOff { get; }
    public bool? IsFrozen { get; }
    public CadResolutionState LineweightState { get; }
    public CadResolutionState TransparencyState { get; }
    internal CadStyleCatalogEntry(CadSourceRecordRef source,Func<int,bool,CadStyleFact> f,bool isLayer)
    {
        Source=source; Name=f(2,false); Aci=f(62,true); TrueColor=f(420,true); ColorName=f(430,false); Linetype=f(6,false); Flags=f(70,true);
        Lineweight=f(370,true); Transparency=f(440,true);
        LineweightState=Lineweight.Presence==DxfFactPresence.Present?CadResolutionState.Unsupported:CadResolutionState.Absent;
        TransparencyState=Transparency.Presence==DxfFactPresence.Present?CadResolutionState.Unsupported:CadResolutionState.Absent;
        IsOff=isLayer&&Aci.Validity==DxfFactValidity.Valid&&Aci.Integer is >=-255 and <=255 and not 0?Aci.Integer<0:null;
        IsFrozen=isLayer&&Flags.Validity==DxfFactValidity.Valid&&Flags.Integer>=0?(Flags.Integer.Value&1)!=0:null;
    }
}
/// <summary>Typed composite reference, not another released ID/hash or fabricated DXF handle.</summary>
public sealed class CadStyleEntityKey(CadSourceRecordRef source,CadId? occurrenceId)
{ public CadSourceRecordRef Source { get; }=source; public CadId? OccurrenceId { get; }=occurrenceId; }
public sealed class CadStyleProperty<T> where T:class
{
    public CadResolutionState State { get; }
    public T? Value { get; }
    public string RuleId { get; }
    public IReadOnlyList<CadStyleFact> Evidence { get; }
    public CadSourceRecordRef? CatalogRecord { get; }
    public CadStyleEntityKey? SupportingContext { get; }
    internal CadStyleProperty(CadResolutionState state,T? value,string rule,IReadOnlyList<CadStyleFact> evidence,
        CadSourceRecordRef? catalog=null,CadStyleEntityKey? context=null)
    {
        CadContract.Require((state is CadResolutionState.Known or CadResolutionState.Inherited or CadResolutionState.Defaulted)==(value is not null),"Style state/value mismatch.");
        State=state; Value=value; RuleId=rule; Evidence=CadContract.Copy(evidence); CatalogRecord=catalog; SupportingContext=context;
    }
}
public sealed class CadEffectiveStyle(CadStyleEntityKey key,CadDeclaredStyle declared,CadStyleProperty<string> layer,
    CadStyleProperty<CadColor> color,CadStyleProperty<string> linetype)
{
    public CadStyleEntityKey Key { get; }=key;
    public CadDeclaredStyle Declared { get; }=declared;
    public CadStyleProperty<string> Layer { get; }=layer;
    public CadStyleProperty<CadColor> Color { get; }=color;
    public CadStyleProperty<string> Linetype { get; }=linetype;
    public CadResolutionState LineweightState { get; }=declared.Lineweight.Presence==DxfFactPresence.Present?CadResolutionState.Unsupported:CadResolutionState.Absent;
    public CadResolutionState TransparencyState { get; }=declared.Transparency.Presence==DxfFactPresence.Present?CadResolutionState.Unsupported:CadResolutionState.Absent;
}
public sealed class CadStyleDiagnostic(CadStyleDiagnosticCode code,CadStyleEntityKey? key,string channel,IReadOnlyList<CadStyleFact> evidence)
{
    public CadStyleDiagnosticCode Code { get; }=code;
    public CadStyleEntityKey? Key { get; }=key;
    public string Channel { get; }=channel;
    public IReadOnlyList<CadStyleFact> Evidence { get; }=CadContract.Copy(evidence);
}
public sealed class CadStyleLimits
{
    public int MaxProjections { get; }
    public int MaxDiagnostics { get; }
    public CadStyleLimits(int maxProjections=250_000,int maxDiagnostics=5_000)
    { CadContract.Require(maxProjections>0&&maxDiagnostics>=1,"Positive style budgets required."); MaxProjections=maxProjections; MaxDiagnostics=maxDiagnostics; }
}
public sealed class CadStyleArtifact(CadOccurrenceExpansionResult input,IReadOnlyList<CadDeclaredStyle> declarations,
    IReadOnlyList<CadStyleCatalogEntry> layers,IReadOnlyList<CadStyleCatalogEntry> linetypes,IReadOnlyList<CadEffectiveStyle> roots,
    IReadOnlyList<CadEffectiveStyle> contexts,IReadOnlyList<CadEffectiveStyle> entities,CadStyleLimits limits)
{
    public CadStyleLimits Limits { get; }=limits;
    public CadOccurrenceExpansionResult Input { get; }=input;
    public CadProvenanceRegistry Sources { get; }=input.Expansion!.Sources;
    public IReadOnlyList<CadDeclaredStyle> Declarations { get; }=CadContract.Copy(declarations);
    public IReadOnlyList<CadStyleCatalogEntry> Layers { get; }=CadContract.Copy(layers);
    public IReadOnlyList<CadStyleCatalogEntry> Linetypes { get; }=CadContract.Copy(linetypes);
    public IReadOnlyList<CadEffectiveStyle> Roots { get; }=CadContract.Copy(roots);
    public IReadOnlyList<CadEffectiveStyle> OccurrenceContexts { get; }=CadContract.Copy(contexts);
    public IReadOnlyList<CadEffectiveStyle> Entities { get; }=CadContract.Copy(entities);
}
public sealed class CadStyleResult
{
    public CadStyleStatus Status { get; }
    public CadStyleArtifact? Artifact { get; }
    public IReadOnlyList<CadStyleDiagnostic> Diagnostics { get; }
    public long ProjectionsAllocated { get; }
    internal CadStyleResult(CadStyleStatus status,CadStyleArtifact? artifact,IReadOnlyList<CadStyleDiagnostic> diagnostics,long allocated)
    {
        CadContract.Require((status is CadStyleStatus.Complete or CadStyleStatus.Partial)==(artifact is not null),"Style publication/status mismatch.");
        Status=status; Artifact=artifact; Diagnostics=CadContract.Copy(diagnostics); ProjectionsAllocated=allocated;
    }
}
