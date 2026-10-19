using DtoB.Cad.Normalize.Styles;
using DtoB.Cad.Raw;
using DtoB.Cad.V2;

namespace DtoB.Cad.Normalize.Assembly;

public sealed partial class CadNormalizer
{
    private sealed partial class Session
    {
        private IReadOnlyList<CadSourceFactRef> Evidence<T>(CadEffectiveStyle owner,Func<CadEffectiveStyle,CadStyleProperty<T>> channel) where T:class
        {
            var refs=new List<CadSourceFactRef>();var seen=new HashSet<(int,string?)>();var p=owner;
            while(true)
            {
                token.ThrowIfCancellationRequested();
                CadContract.Require(seen.Add((p.Key.Source.Index,p.Key.OccurrenceId?.Value)),"Cyclic style evidence.");
                var property=channel(p);
                foreach(var fact in property.Evidence)
                    if(styleFacts.TryGetValue((fact.Source.Index,fact.GroupCode),out var r)&&!refs.Any(e=>e.Index==r.Index))refs.Add(r);
                if(property.SupportingContext is not {} parent)break;
                p=RequiredProjection(parent.Source,parent.OccurrenceId);
            }
            return refs.OrderBy(r=>r.Index).ToArray();
        }
        private static CadPropertyMode ColorMode(CadDeclaredStyle d)=>d.ColorMode switch
        {CadDeclaredColorMode.ByLayer=>CadPropertyMode.ByLayer,CadDeclaredColorMode.ByBlock=>CadPropertyMode.ByBlock,CadDeclaredColorMode.Absent=>CadPropertyMode.Absent,_=>CadPropertyMode.Explicit};
        private static CadPropertyMode LineMode(CadDeclaredStyle d)=>d.LinetypeMode switch
        {CadDeclaredLinetypeMode.ByLayer=>CadPropertyMode.ByLayer,CadDeclaredLinetypeMode.ByBlock=>CadPropertyMode.ByBlock,CadDeclaredLinetypeMode.Absent=>CadPropertyMode.Absent,_=>CadPropertyMode.Explicit};
        private CadId EffectiveStyle(CadEffectiveStyle p,string role)
        {
            var source=CadAssemblyIdentity.Source(sources,p.Key.Source);var id=CadAssemblyIdentity.Component(source,role);
            if(p.Key.OccurrenceId is {} o)id=CadAssemblyIdentity.Component(id,o.Value);
            // No value-only dedup: evidence and declaration modes are part of the contract.
            styles.Add(new(id,new(p.Declared.Layer.Presence==DxfFactPresence.Absent?CadPropertyMode.Absent:CadPropertyMode.Explicit,
                p.Layer.State,p.Layer.Value,p.Layer.RuleId,Evidence(p,x=>x.Layer)),
                new(ColorMode(p.Declared),p.Color.State,p.Color.Value,p.Color.RuleId,Evidence(p,x=>x.Color)),
                new(LineMode(p.Declared),p.Linetype.State,p.Linetype.Value,p.Linetype.RuleId,Evidence(p,x=>x.Linetype))));
            Track(p.Key.Source.Index,id);return id;
        }
        private CadId DeclaredStyle(CadDeclaredStyle d)
        {
            IReadOnlyList<CadSourceFactRef> E(params CadStyleFact[] fields)=>fields.Select(f=>styleFacts[(f.Source.Index,f.GroupCode)]).OrderBy(r=>r.Index).ToArray();
            var id=CadAssemblyIdentity.Component(CadAssemblyIdentity.Source(sources,d.Source),"declared-style");
            var layer=d.Layer.Validity==DxfFactValidity.Valid?d.Layer.Text:null;
            var color=d.ColorMode switch
            {CadDeclaredColorMode.Aci=>new CadColor(CadColorKind.Aci,d.Aci.Integer!.Value),CadDeclaredColorMode.Rgb=>new CadColor(CadColorKind.Rgb,d.TrueColor.Integer!.Value),_=>null};
            var line=d.LinetypeMode==CadDeclaredLinetypeMode.Explicit?d.Linetype.Text:null;
            styles.Add(new(id,new(d.Layer.Presence==DxfFactPresence.Absent?CadPropertyMode.Absent:CadPropertyMode.Explicit,
                layer is not null?CadResolutionState.Known:d.Layer.Presence==DxfFactPresence.Absent?CadResolutionState.Absent:CadResolutionState.Unsupported,layer,"CAD.Template.Declared",E(d.Layer)),
                new(ColorMode(d),color is not null?CadResolutionState.Known:d.ColorMode==CadDeclaredColorMode.Absent?CadResolutionState.Absent:CadResolutionState.Unsupported,color,"CAD.Template.Unresolved",E(d.Aci,d.TrueColor,d.ColorName)),
                new(LineMode(d),line is not null?CadResolutionState.Known:d.LinetypeMode==CadDeclaredLinetypeMode.Absent?CadResolutionState.Absent:CadResolutionState.Unsupported,line,"CAD.Template.Unresolved",E(d.Linetype))));
            Track(d.Source.Index,id);return id;
        }
        private void BuildLayers()
        {
            var names=styleInput.Layers.Where(l=>l.Name.Text is not null).GroupBy(l=>l.Name.Text!,StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g=>g.Key,g=>g.Count(),StringComparer.OrdinalIgnoreCase);
            foreach(var l in styleInput.Layers)
            {
                token.ThrowIfCancellationRequested();
                if(l.Name.Validity!=DxfFactValidity.Valid||l.Name.Text is null||names[l.Name.Text]!=1)
                {Issue("CAD_LAYER_CATALOG_REJECTED",l.Source.Index);states[l.Source.Index]=CadNormalizationState.UnsupportedNormalization;continue;}
                // Layer source facts and off/frozen are factual; no entity inheritance is invented for a catalog row.
                layers.Add(new(l.Name.Text,l.Source,null,l.IsOff,l.IsFrozen));
                if(l.LineweightState==CadResolutionState.Unsupported||l.TransparencyState==CadResolutionState.Unsupported)
                    states[l.Source.Index]=CadNormalizationState.PartiallyNormalized;
            }
        }
    }
}
