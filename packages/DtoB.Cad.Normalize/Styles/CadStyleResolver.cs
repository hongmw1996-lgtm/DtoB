using DtoB.Cad.Normalize.Occurrences;
using DtoB.Cad.Normalize.Scalars;
using DtoB.Cad.Raw;
using DtoB.Cad.V2;

namespace DtoB.Cad.Normalize.Styles;

public static class CadStyleResolver
{
    public static CadStyleResult Resolve(RawDrawingSnapshot snapshot,CadOccurrenceExpansionResult input,
        CadStyleLimits? limits=null,CancellationToken cancellationToken=default)=>new Session(snapshot,input,limits??new(),cancellationToken).Run();

    private sealed class Session(RawDrawingSnapshot snapshot,CadOccurrenceExpansionResult input,CadStyleLimits limits,CancellationToken token)
    {
        private readonly Dictionary<int,CadDeclaredStyle> declarations=[];
        private readonly Dictionary<string,List<CadStyleCatalogEntry>> layers=new(StringComparer.OrdinalIgnoreCase),linetypes=new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int,CadStyleCatalogEntry> layerRecords=[];
        private readonly List<CadStyleCatalogEntry> orderedLayers=[],orderedLinetypes=[];
        private readonly Dictionary<string,CadEffectiveStyle> contexts=new(StringComparer.Ordinal);
        private readonly List<CadEffectiveStyle> roots=[],orderedContexts=[],entities=[];
        private readonly List<CadStyleDiagnostic> diagnostics=[];
        private CadStyleEntityKey? current;
        private long allocated;
        private sealed class Stop : Exception;
        internal CadStyleResult Run()
        {
            try
            {
                token.ThrowIfCancellationRequested(); Validate();
                foreach(var row in snapshot.Records)
                {
                    token.ThrowIfCancellationRequested(); current=new(new(row.StructuralRecordIndex),null);
                    if(RawStyleAccess.Entity(row))declarations.Add(row.StructuralRecordIndex,new(new(row.StructuralRecordIndex),RawStyleAccess.Facts(snapshot,row,"AcDbEntity",token)));
                    if(row.SourceRecord.Scope is not ("TABLES/LAYER" or "TABLES/LTYPE"))continue;
                    var isLayer=row.SourceRecord.Scope=="TABLES/LAYER";
                    var entry=new CadStyleCatalogEntry(new(row.StructuralRecordIndex),RawStyleAccess.Facts(snapshot,row,isLayer?"AcDbLayerTableRecord":"AcDbLinetypeTableRecord",token),isLayer);
                    (isLayer?orderedLayers:orderedLinetypes).Add(entry);
                    if(isLayer)layerRecords.Add(row.StructuralRecordIndex,entry);
                    if(isLayer)
                        foreach(var (fact,channel) in new[]{(entry.Lineweight,"Layer.Lineweight"),(entry.Transparency,"Layer.Transparency"),(entry.ColorName,"Layer.ColorName")})
                            if(fact.Presence==DxfFactPresence.Present)Add(CadStyleDiagnosticCode.UnsupportedStyleChannel,channel,[fact]);
                    if(entry.Name.Validity!=DxfFactValidity.Valid){Add(isLayer?CadStyleDiagnosticCode.InvalidLayerReference:CadStyleDiagnosticCode.InvalidLinetype,"CatalogName",[entry.Name]);continue;}
                    var map=isLayer?layers:linetypes;
                    if(!map.TryGetValue(entry.Name.Text!,out var entries))map.Add(entry.Name.Text!,entries=[]);
                    entries.Add(entry);
                }
                foreach(var row in snapshot.Records)
                {
                    token.ThrowIfCancellationRequested();
                    if(row.SourceSection=="ENTITIES"&&row.BlockAnnotation?.Payload is not RawInsert&&declarations.TryGetValue(row.StructuralRecordIndex,out var source))
                        roots.Add(Project(source,null,null));
                }
                foreach(var occurrence in input.Expansion!.Occurrences)
                {
                    token.ThrowIfCancellationRequested();
                    CadEffectiveStyle? parent=null;
                    if(occurrence.Value.ParentOccurrenceId is {} parentId)
                        CadContract.Require(contexts.TryGetValue(parentId.Value,out parent),"Parent context must precede child.");
                    var context=Project(declarations[occurrence.Value.InsertSource.Index],occurrence.Value.Id,parent);
                    contexts.Add(occurrence.Value.Id.Value,context); orderedContexts.Add(context);
                    foreach(var child in occurrence.Definition.ChildSources)
                    {
                        token.ThrowIfCancellationRequested();
                        if(snapshot.Records[child.Index].BlockAnnotation?.Payload is RawInsert)continue;
                        if(declarations.TryGetValue(child.Index,out var source))entities.Add(Project(source,occurrence.Value.Id,context));
                    }
                }
                token.ThrowIfCancellationRequested();
                var artifact=new CadStyleArtifact(input,declarations.OrderBy(p=>p.Key).Select(p=>p.Value).ToArray(),orderedLayers,orderedLinetypes,roots,orderedContexts,entities,limits);
                token.ThrowIfCancellationRequested();
                return new(diagnostics.Count==0&&input.Status==CadExpansionStatus.Complete?CadStyleStatus.Complete:CadStyleStatus.Partial,artifact,diagnostics,allocated);
            }
            catch(OperationCanceledException){Terminal(CadStyleDiagnosticCode.Cancelled,"Cancellation");return new(CadStyleStatus.Cancelled,null,diagnostics,allocated);}
            catch(Stop){return new(CadStyleStatus.ResourceLimitExceeded,null,diagnostics,allocated);}
            catch(Exception e) when(e is InvalidDataException or ArgumentException or FormatException or KeyNotFoundException)
            {Terminal(CadStyleDiagnosticCode.InvalidInput,"AuthorityOrOccurrenceContext");return new(CadStyleStatus.InvalidInput,null,diagnostics,allocated);}
        }
        private void Validate()
        {
            CadContract.Require(input.Expansion is not null&&input.Status is CadExpansionStatus.Complete or CadExpansionStatus.Partial,"Published occurrence input required.");
            var doc=RawScalarAccess.HeaderUnits(snapshot).Source.Document; var source=input.Expansion!.Sources;
            CadContract.Require(doc.DrawingId==source.Source.DrawingId&&doc.RevisionId==source.Source.RevisionId&&doc.FileHash==source.Source.FileHash,"Paired source authority required.");
            CadContract.Require(snapshot.Records.Count==source.Records.Count,"Paired source registry required.");
            for(var i=0;i<snapshot.Records.Count;i++)
            {
                token.ThrowIfCancellationRequested(); var row=snapshot.Records[i]; var s=row.SourceRecord.Source; var record=source.Records[i]; var locator=record.Locator;
                CadContract.Require(row.StructuralRecordIndex==i&&locator.Domain==CadSourceRecordDomain.StructuralRecord&&locator.RecordIndex==i
                    &&locator.FirstPair==s.StartPairIndex&&locator.LastPair==s.EndPairIndex&&locator.FirstPhysicalLine==s.StartLine&&locator.LastPhysicalLine==s.EndLine
                    &&record.OriginalHandle==row.Identity?.OriginalHandle.Text&&record.CanonicalHandle==row.Identity?.CanonicalHandle,"Source pairing mismatch.");
            }
        }
        private void Terminal(CadStyleDiagnosticCode code,string channel)=>diagnostics.Add(new(code,current,channel,[]));
        private void Fatal(string channel){Terminal(CadStyleDiagnosticCode.ResourceLimitExceeded,channel);throw new Stop();}
        private void Add(CadStyleDiagnosticCode code,string channel,IReadOnlyList<CadStyleFact> facts)
        {if(diagnostics.Count>=limits.MaxDiagnostics-1)Fatal("Diagnostics");diagnostics.Add(new(code,current,channel,facts));}
        private CadStyleCatalogEntry? Lookup(Dictionary<string,List<CadStyleCatalogEntry>> map,CadStyleFact name,string channel,
            CadStyleDiagnosticCode missing,CadStyleDiagnosticCode ambiguous)
        {
            if(!map.TryGetValue(name.Text!,out var matches)){Add(missing,channel,[name]);return null;}
            if(matches.Count!=1){Add(ambiguous,channel,[name,..matches.Select(e=>e.Name)]);return null;}
            return matches[0];
        }
        private CadEffectiveStyle Project(CadDeclaredStyle source,CadId? occurrence,CadEffectiveStyle? parent)
        {
            token.ThrowIfCancellationRequested(); current=new(source.Source,occurrence);
            if(allocated>=limits.MaxProjections)Fatal("Projections"); allocated++;
            var layer=Layer(source,parent); var color=Color(source,layer,parent); var linetype=Linetype(source,layer,parent);
            foreach(var (fact,channel) in new[]{(source.Lineweight,"Lineweight"),(source.Transparency,"Transparency"),(source.ColorName,"ColorName")})
                if(fact.Presence==DxfFactPresence.Present)Add(CadStyleDiagnosticCode.UnsupportedStyleChannel,channel,[fact]);
            if(source.SourceVisible is null)Add(CadStyleDiagnosticCode.InvalidVisibility,"Visibility",[source.Visibility]);
            return new(current,source,layer,color,linetype);
        }
        private CadStyleProperty<string> Layer(CadDeclaredStyle d,CadEffectiveStyle? parent)
        {
            if(d.Layer.Validity!=DxfFactValidity.Valid)
            {Add(CadStyleDiagnosticCode.InvalidLayerReference,"Layer",[d.Layer]);return new(d.Layer.Presence==DxfFactPresence.Absent?CadResolutionState.Absent:CadResolutionState.Unsupported,null,"DXF.Layer.InvalidOrAbsent",[d.Layer]);}
            if(d.Layer.Text=="0"&&parent is not null)
                return new(parent.Layer.Value is null?CadResolutionState.Unsupported:CadResolutionState.Inherited,parent.Layer.Value,"DXF.Layer.ZeroFromInsert",[d.Layer],parent.Layer.CatalogRecord,parent.Key);
            var entry=Lookup(layers,d.Layer,"Layer",CadStyleDiagnosticCode.MissingLayerDefinition,CadStyleDiagnosticCode.AmbiguousLayerDefinition);
            return new(entry is null?CadResolutionState.Unsupported:CadResolutionState.Known,entry?.Name.Text,"DXF.Layer.Explicit",entry is null?[d.Layer]:[d.Layer,entry.Name],entry?.Source);
        }
        private CadStyleProperty<T> ByBlock<T>(CadStyleProperty<T>? parent,CadStyleEntityKey? key,string channel,CadStyleFact fact) where T:class
        {
            if(parent?.Value is null)Add(CadStyleDiagnosticCode.UnresolvedByBlockContext,channel,[fact]);
            return new(parent?.Value is null?CadResolutionState.Unsupported:CadResolutionState.Inherited,parent?.Value,"DXF."+channel+".ByBlock",[fact],parent?.CatalogRecord,key);
        }
        private CadStyleProperty<CadColor> Color(CadDeclaredStyle d,CadStyleProperty<string> layer,CadEffectiveStyle? parent)
        {
            var evidence=new[]{d.Aci,d.TrueColor,d.ColorName};
            if(d.ColorMode is CadDeclaredColorMode.Aci or CadDeclaredColorMode.Rgb)
                return new(CadResolutionState.Known,new(d.ColorMode==CadDeclaredColorMode.Rgb?CadColorKind.Rgb:CadColorKind.Aci,
                    d.ColorMode==CadDeclaredColorMode.Rgb?d.TrueColor.Integer!.Value:d.Aci.Integer!.Value),"DXF.Color."+d.ColorMode,evidence);
            if(d.ColorMode==CadDeclaredColorMode.ByBlock)return ByBlock(parent?.Color,parent?.Key,"Color",d.Aci);
            if(d.ColorMode is CadDeclaredColorMode.Invalid or CadDeclaredColorMode.Unsupported)
            {Add(CadStyleDiagnosticCode.InvalidColor,"Color",evidence);return new(CadResolutionState.Unsupported,null,"DXF.Color.InvalidOrUnsupported",evidence);}
            var l=layer.CatalogRecord is {} record?layerRecords[record.Index]:null;
            CadColor? value=null;
            if(l is not null)
            {
                if(l.TrueColor.Presence==DxfFactPresence.Present)
                {if(l.TrueColor.Validity==DxfFactValidity.Valid&&l.TrueColor.Integer is >=0 and <=0xFFFFFF)value=new(CadColorKind.Rgb,l.TrueColor.Integer.Value);}
                else if(l.ColorName.Presence==DxfFactPresence.Absent&&l.Aci.Validity==DxfFactValidity.Valid&&l.Aci.Integer is >=-255 and <=255 and not 0)
                    value=new(CadColorKind.Aci,Math.Abs(l.Aci.Integer.Value));
            }
            if(value is null)Add(CadStyleDiagnosticCode.InvalidColor,"Color.ByLayer",l is null?evidence:[..evidence,l.Aci,l.TrueColor,l.ColorName]);
            return new(value is null?CadResolutionState.Unsupported:d.ColorMode==CadDeclaredColorMode.Absent?CadResolutionState.Defaulted:CadResolutionState.Inherited,
                value,d.ColorMode==CadDeclaredColorMode.Absent?"DXF.Color.AbsentByLayer":"DXF.Color.ByLayer",l is null?evidence:[..evidence,l.Aci,l.TrueColor,l.ColorName],l?.Source,
                layer.SupportingContext);
        }
        private CadStyleProperty<string> Linetype(CadDeclaredStyle d,CadStyleProperty<string> layer,CadEffectiveStyle? parent)
        {
            if(d.LinetypeMode==CadDeclaredLinetypeMode.ByBlock)return ByBlock(parent?.Linetype,parent?.Key,"Linetype",d.Linetype);
            if(d.LinetypeMode==CadDeclaredLinetypeMode.Invalid)
            {Add(CadStyleDiagnosticCode.InvalidLinetype,"Linetype",[d.Linetype]);return new(CadResolutionState.Unsupported,null,"DXF.Linetype.Invalid",[d.Linetype]);}
            var fromLayer=d.LinetypeMode!=CadDeclaredLinetypeMode.Explicit;
            var l=fromLayer&&layer.CatalogRecord is {} record?layerRecords[record.Index]:null;
            var name=fromLayer?l?.Linetype:d.Linetype;
            if(name is null||name.Validity!=DxfFactValidity.Valid||StringComparer.OrdinalIgnoreCase.Equals(name.Text,"BYLAYER")||StringComparer.OrdinalIgnoreCase.Equals(name.Text,"BYBLOCK"))
            {Add(CadStyleDiagnosticCode.InvalidLinetype,"Linetype.Source",name is null?[d.Linetype]:[d.Linetype,name]);return new(CadResolutionState.Unsupported,null,"DXF.Linetype.Unresolved",name is null?[d.Linetype]:[d.Linetype,name],l?.Source);}
            var entry=Lookup(linetypes,name,"Linetype",CadStyleDiagnosticCode.MissingLinetypeDefinition,CadStyleDiagnosticCode.AmbiguousLinetypeDefinition);
            return new(entry is null?CadResolutionState.Unsupported:!fromLayer?CadResolutionState.Known:d.LinetypeMode==CadDeclaredLinetypeMode.Absent?CadResolutionState.Defaulted:CadResolutionState.Inherited,
                entry?.Name.Text,!fromLayer?"DXF.Linetype.Explicit":d.LinetypeMode==CadDeclaredLinetypeMode.Absent?"DXF.Linetype.AbsentByLayer":"DXF.Linetype.ByLayer",
                entry is null?[d.Linetype,name]:[d.Linetype,name,entry.Name],entry?.Source,fromLayer?layer.SupportingContext:null);
        }
    }
}
