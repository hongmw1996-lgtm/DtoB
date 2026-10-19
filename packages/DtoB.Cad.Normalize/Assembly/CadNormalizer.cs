using System.Globalization;
using DtoB.Cad.Normalize.Occurrences;
using DtoB.Cad.Normalize.Placement;
using DtoB.Cad.Normalize.Scalars;
using DtoB.Cad.Normalize.Styles;
using DtoB.Cad.Raw;
using DtoB.Cad.V2;
using V2Document = DtoB.Cad.V2.CadDocument;
using V2Entity = DtoB.Cad.V2.CadEntity;
using V2Layer = DtoB.Cad.V2.CadLayer;

namespace DtoB.Cad.Normalize.Assembly;

/// <summary>Pure synchronous composition. The caller owns scheduling and the paired immutable Raw snapshot.</summary>
public sealed partial class CadNormalizer(CadNumericPolicy numericPolicy) : ICadNormalizer
{
    private readonly CadNumericPolicy policy = CadContract.Required(numericPolicy);
    public CadNormalizationResult Normalize(RawDrawingSnapshot snapshot, CadNormalizationOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);ArgumentNullException.ThrowIfNull(options);
        var session = new Session(snapshot,options,policy,cancellationToken);
        return session.Run();
    }

    private sealed partial class Session(RawDrawingSnapshot raw, CadNormalizationOptions options, CadNumericPolicy policy, CancellationToken token)
    {
        private readonly List<CadDiagnostic> diagnostics=[];
        private readonly List<CadDiagnostic> rawDiagnostics=[];
        private readonly List<CadGeometry> geometry=[];
        private readonly List<V2Entity> entities=[];
        private readonly List<CadStyle> styles=[];
        private readonly List<CadBlockDefinition> definitions=[];
        private readonly List<CadBlockOccurrence> occurrences=[];
        private readonly List<CadModelSpaceRecord> model=[];
        private readonly List<V2Layer> layers=[];
        private readonly CadMaterializationBudget budget=new(options.Limits.MaxDerivedGeometry);
        private readonly Dictionary<int,V2Entity> sourceEntities=[];
        private readonly Dictionary<int,CadNormalizationState> states=[];
        private readonly Dictionary<int,List<CadId>> objects=[];
        private readonly Dictionary<(int,int),CadSourceFactRef> styleFacts=[];
        private readonly Dictionary<int,CadSourceFactRef> recordFacts=[];
        private readonly Dictionary<(int,string?),CadEffectiveStyle> projections=[];
        private readonly Dictionary<int,bool> modelSources=[];
        private readonly HashSet<string> modelOccurrences=new(StringComparer.Ordinal);
        private CadProvenanceRegistry sources=null!;
        private CadUnitResolution units=null!;
        private CadStyleArtifact styleInput=null!;
        private bool partial;
        private sealed class Refusal(CadNormalizationStatus status) : Exception { internal CadNormalizationStatus Status { get; }=status; }
        internal CadNormalizationResult Run()
        {
            try
            {
                token.ThrowIfCancellationRequested();
                if(!StringComparer.Ordinal.Equals(options.ProfileId,policy.ProfileId))Abort(CadNormalizationStatus.InvalidNormalization,"CAD_NUMERIC_PROFILE_MISMATCH");
                units=CadUnitNormalizer.Resolve(RawScalarAccess.HeaderUnits(raw));
                if(units.Status!=CadUnitStatus.Resolved) Abort(CadNormalizationStatus.InvalidNormalization,"CAD_UNITS_UNRESOLVED");
                var expansion=CadOccurrenceExpander.Expand(raw,options,policy,token);
                foreach(var d in expansion.Diagnostics)
                    Add(new("OCCURRENCE_"+d.Code,CadSeverity.Warning,CadStage.Occurrences,d.Source,null,d.Path,
                        [d.Detail,d.Requested?.ToString(CultureInfo.InvariantCulture)??"",d.Limit?.ToString(CultureInfo.InvariantCulture)??""]));
                if(expansion.Expansion is null) Abort(Map(expansion.Status),"CAD_OCCURRENCE_REFUSED");
                var resolved=CadStyleResolver.Resolve(raw,expansion,new CadStyleLimits(maxDiagnostics:options.Limits.MaxDiagnostics),token);
                foreach(var d in resolved.Diagnostics) Add(new("STYLE_"+d.Code,CadSeverity.Warning,CadStage.Styles,d.Key?.Source,null,null,[d.Channel]));
                if(resolved.Artifact is null) Abort(resolved.Status==CadStyleStatus.Cancelled?CadNormalizationStatus.Cancelled:
                    resolved.Status==CadStyleStatus.ResourceLimitExceeded?CadNormalizationStatus.ResourceLimitExceeded:CadNormalizationStatus.InvalidNormalization,"CAD_STYLE_REFUSED");
                styleInput=resolved.Artifact!;BuildSources(expansion.Expansion!.Sources);
                partial=expansion.Status==CadExpansionStatus.Partial||resolved.Status==CadStyleStatus.Partial;
                foreach(var p in styleInput.Roots.Concat(styleInput.OccurrenceContexts).Concat(styleInput.Entities))
                {token.ThrowIfCancellationRequested();projections.Add((p.Key.Source.Index,p.Key.OccurrenceId?.Value),p);}
                foreach(var row in raw.Records)
                {
                    token.ThrowIfCancellationRequested();
                    var i=row.StructuralRecordIndex;states[i]=CadNormalizationState.NotApplicable;
                    if(row.SemanticDisposition==DxfDisposition.Unsupported)
                    {states[i]=CadNormalizationState.UnsupportedNormalization;Issue("RAW_UNSUPPORTED",i,row.SourceRecord.RecordType);}
                    if(row.SourceSection is "ENTITIES" or "BLOCKS")
                    {
                        if(row.SourceRecord.RecordType is not ("BLOCK" or "ENDBLK" or "SEQEND")) BuildEntity(row);
                    }
                }
                BuildDefinitions(expansion.Expansion);
                foreach(var item in expansion.Expansion.Occurrences)
                {
                    token.ThrowIfCancellationRequested();var v=item.Value;
                    if(!IsModelSource(raw.Records[v.Path.Steps[0].InsertSource.Index])) continue;
                    foreach(var (matrix,sign) in new[]{(v.LocalTransform,item.LocalOrientationSign),(v.AccumulatedTransform,item.AccumulatedOrientationSign)})
                    {
                        var actual=CadOrientationValidation.Sign(matrix);
                        if(actual!=0&&actual!=sign) Abort(CadNormalizationStatus.InvalidNormalization,"CAD_ORIENTATION_INVARIANT",v.InsertSource);
                        if(actual==0) Issue("CAD_SINGULAR_STORED_TRANSFORM",v.InsertSource.Index);
                    }
                    var context=RequiredProjection(v.InsertSource,v.Id);
                    var style=EffectiveStyle(context,"insert-style");
                    occurrences.Add(new(v.Id,v.InsertSource,v.DefinitionId,v.ParentOccurrenceId,v.Path,v.LocalTransform,v.AccumulatedTransform,style));
                    modelOccurrences.Add(v.Id.Value);
                    Track(v.InsertSource.Index,v.Id);
                }
                foreach(var p in styleInput.Entities)
                {
                    token.ThrowIfCancellationRequested();
                    if(!modelOccurrences.Contains(p.Key.OccurrenceId!.Value))continue;
                    if(!sourceEntities.TryGetValue(p.Key.Source.Index,out var template)) Abort(CadNormalizationStatus.InvalidNormalization,"CAD_TEMPLATE_MISSING",p.Key.Source);
                    if(template!.Kind==CadEntityKind.Insert) continue;
                    Reserve();var id=CadAssemblyIdentity.Leaf(template.Id,p.Key.OccurrenceId!);
                    var leaf=new V2Entity(id,template.Source,template.Kind,template.Disposition,template.GeometryId,EffectiveStyle(p,"effective-style"),template.Annotation,template.Evidence);
                    entities.Add(leaf);model.Add(new(id,p.Key.OccurrenceId));Track(template.Source.Index,id);
                }
                BuildLayers();
                foreach(var d in raw.Diagnostics)
                {
                    token.ThrowIfCancellationRequested();
                    // Compact reference preserves the exact original diagnostic in the paired Raw snapshot, without localized text in canonical data.
                    var original=raw.ResolveDiagnostic(d);
                    var (code,severity,span)=original switch
                    {
                        DxfStructuralDiagnostic x=>(x.Code.ToString(),(CadSeverity)(int)x.Severity,x.Source),
                        DxfGeometryDiagnostic x=>(x.Code.ToString(),(CadSeverity)(int)x.Severity,x.Source),
                        DxfBlockAnnotationDiagnostic x=>(x.Code.ToString(),(CadSeverity)(int)x.Severity,x.Source),
                        DxfGraphDiagnostic x=>(x.Code.ToString(),(CadSeverity)(int)x.Severity,x.Source),
                        RawSnapshotAssemblyDiagnostic x=>(x.Code.ToString(),CadSeverity.Error,x.Source),
                        DxfLexicalDiagnostic x=>(x.Code.ToString(),CadSeverity.Error,(DxfSourceSpan?)null),
                        _=>throw new InvalidDataException("Unknown Raw diagnostic variant.")
                    };
                    var record=span is null?null:raw.Records.FirstOrDefault(r=>r.SourceRecord.Source.StartPairIndex<=span.StartPairIndex&&r.SourceRecord.Source.EndPairIndex>=span.EndPairIndex);
                    Add(new("RAW_"+code,severity,CadStage.Contract,record is null?null:new(record.StructuralRecordIndex),null,null,
                        [d.Stage.ToString(),d.Index.ToString(CultureInfo.InvariantCulture)]),true);
                }
                var ledger=new List<CadRecordOutcome>();
                foreach(var row in raw.Records)
                {token.ThrowIfCancellationRequested();var i=row.StructuralRecordIndex;ledger.Add(new(new(i),states[i],objects.GetValueOrDefault(i)??[]));}
                partial|=ledger.Any(e=>e.State is not (CadNormalizationState.Normalized or CadNormalizationState.NotApplicable));
                token.ThrowIfCancellationRequested();
                var document=new V2Document(2,"dtob-normalize-04g-v1",options.ProfileId,CadConventions.Canonical,units.Decision,CadTransform.Identity,
                    sources,layers,definitions,entities,occurrences,model,geometry,styles,ledger,diagnostics,rawDiagnostics,
                    new(partial?CadNormalizationState.PartiallyNormalized:CadNormalizationState.Normalized,sources.Records.Count,ledger.Count));
                token.ThrowIfCancellationRequested();document.ValidateStructure();token.ThrowIfCancellationRequested();
                return new(partial?CadNormalizationStatus.PartiallyNormalized:CadNormalizationStatus.Normalized,document,diagnostics);
            }
            catch(OperationCanceledException)
            {Terminal("CAD_CANCELLED");return new(CadNormalizationStatus.Cancelled,null,diagnostics);}
            catch(Refusal r) {return new(r.Status,null,diagnostics);}
            catch(Exception e) when(e is InvalidDataException or FormatException or ArgumentException or OverflowException)
            {Terminal("CAD_INVALID_INPUT_OR_CONTRACT");return new(CadNormalizationStatus.InvalidNormalization,null,diagnostics);}
        }
        private static CadNormalizationStatus Map(CadExpansionStatus s)=>s==CadExpansionStatus.Cancelled?CadNormalizationStatus.Cancelled:
            s==CadExpansionStatus.ResourceLimitExceeded?CadNormalizationStatus.ResourceLimitExceeded:CadNormalizationStatus.InvalidNormalization;
        private void Reserve()
        {token.ThrowIfCancellationRequested();if(!budget.TryReserve()) Abort(CadNormalizationStatus.ResourceLimitExceeded,"CAD_GEOMETRY_LIMIT");}
        private void Track(int source,CadId id)
        {if(!objects.TryGetValue(source,out var ids)) objects.Add(source,ids=[]);ids.Add(id);}
        private void Add(CadDiagnostic diagnostic,bool upstream=false)
        {
            token.ThrowIfCancellationRequested();
            if(diagnostics.Count+rawDiagnostics.Count>=options.Limits.MaxDiagnostics-1) Abort(CadNormalizationStatus.ResourceLimitExceeded,"CAD_DIAGNOSTIC_LIMIT");
            (upstream?rawDiagnostics:diagnostics).Add(diagnostic);
        }
        private void Terminal(string code,CadSourceRecordRef? source=null)
        {
            while(diagnostics.Count+rawDiagnostics.Count>=options.Limits.MaxDiagnostics)
            {if(rawDiagnostics.Count>0)rawDiagnostics.RemoveAt(rawDiagnostics.Count-1);else diagnostics.RemoveAt(diagnostics.Count-1);}
            diagnostics.Add(new(code,CadSeverity.Error,CadStage.Assembly,source,null,null,[budget.Allocated.ToString(CultureInfo.InvariantCulture)]));
        }
        private void Abort(CadNormalizationStatus status,string code,CadSourceRecordRef? source=null)
        {Terminal(code,source);throw new Refusal(status);}
        private void Issue(string code,int source,params string[] args)
        {partial=true;Add(new(code,CadSeverity.Warning,CadStage.Geometry,new(source),null,null,args));}
        private CadEffectiveStyle RequiredProjection(CadSourceRecordRef source,CadId? occurrence)
        {if(!projections.TryGetValue((source.Index,occurrence?.Value),out var p))Abort(CadNormalizationStatus.InvalidNormalization,"CAD_STYLE_PROJECTION_MISSING",source);return p!;}
        private void BuildSources(CadProvenanceRegistry input)
        {
            var facts=new List<CadSourceFact>();
            var styleBySource=styleInput.Declarations.SelectMany(d=>new[]{d.Layer,d.Aci,d.TrueColor,d.ColorName,d.Linetype,d.Lineweight,d.Transparency,d.Visibility})
                .Concat(styleInput.Layers.Concat(styleInput.Linetypes).SelectMany(d=>new[]{d.Name,d.Aci,d.TrueColor,d.ColorName,d.Linetype,d.Flags,d.Lineweight,d.Transparency}))
                .GroupBy(f=>f.Source.Index).ToDictionary(g=>g.Key,g=>g.GroupBy(f=>f.GroupCode).Select(x=>x.First()).OrderBy(f=>f.GroupCode).ToArray());
            foreach(var row in raw.Records)
            {
                token.ThrowIfCancellationRequested();var source=new CadSourceRecordRef(row.StructuralRecordIndex);var entries=new SortedDictionary<string,CadSourceFact>(StringComparer.Ordinal);
                var span=row.SourceRecord.Source;entries.Add("record",new(source,"record",CadFactPresence.Present,Enumerable.Range(span.StartPairIndex,span.EndPairIndex-span.StartPairIndex+1).ToArray(),span.StartLine,span.EndLine));
                foreach(var (prefix,fields) in new[]{("structural",row.SourceRecord.Fields),("geometry",row.Geometry?.Fields??[]),("annotation",row.BlockAnnotation?.Fields??[])})
                    foreach(var (field,ordinal) in fields.Select((f,n)=>(f,n)))
                    {
                        token.ThrowIfCancellationRequested();var f=field.Fact;var role=prefix+"."+field.Name+"."+ordinal.ToString("D6",CultureInfo.InvariantCulture);
                        entries.Add(role,new(source,role,f.Presence==DxfFactPresence.Absent?CadFactPresence.Absent:CadFactPresence.Present,f.SourcePairIndices,f.Source?.StartLine,f.Source?.EndLine));
                    }
                if(styleBySource.TryGetValue(source.Index,out var sf))foreach(var f in sf)
                {var role="style."+f.GroupCode.ToString("D3",CultureInfo.InvariantCulture);entries.Add(role,new(source,role,f.Presence==DxfFactPresence.Absent?CadFactPresence.Absent:CadFactPresence.Present,f.PairIndices,f.FirstLine,f.LastLine));}
                foreach(var pair in entries)
                {
                    token.ThrowIfCancellationRequested();var reference=new CadSourceFactRef(facts.Count);
                    if(pair.Key=="record")recordFacts.Add(source.Index,reference);
                    if(pair.Key.StartsWith("style.",StringComparison.Ordinal))styleFacts.Add((source.Index,int.Parse(pair.Key.AsSpan(6),CultureInfo.InvariantCulture)),reference);
                    facts.Add(pair.Value);
                }
            }
            sources=new(input.Source,input.Records,facts);
        }
        private void BuildDefinitions(CadOccurrenceExpansion expansion)
        {
            var reached=expansion.Definitions.ToDictionary(d=>d.Source.Index);
            foreach(var b in raw.BlockDefinitions)
            {
                token.ThrowIfCancellationRequested();var row=raw.Records[b.BeginRecordIndex];var source=new CadSourceRecordRef(b.BeginRecordIndex);
                if(row.BlockAnnotation?.Payload is not RawBlockBegin block) {Issue("CAD_DEFINITION_UNAVAILABLE",source.Index);continue;}
                var records=new List<RawDrawingRecordView>();
                foreach(var candidate in raw.Records)
                {
                    token.ThrowIfCancellationRequested();
                    if(candidate.TableScope=="BLOCK_RECORD"&&candidate.SourceRecord.Fields.Any(f=>f.Name=="Name"&&StringComparer.OrdinalIgnoreCase.Equals(f.Fact.Text,block.Name.Text)))records.Add(candidate);
                }
                var record=records.Count==1?records[0]:null;
                var blockUnits=record is null?null:CadUnitNormalizer.CheckBlock(RawScalarAccess.Record(raw,record,CadScalarRole.BlockInsertionUnits,token),units);
                var baseResult=reached.TryGetValue(source.Index,out var d)?d.Base:CadBlockBaseNormalizer.Normalize(new(sources.Source,row),block,units,blockUnits);
                if(baseResult.BaseMm is null || records.Count>1 || b.EndRecordIndex is null || !string.IsNullOrEmpty(block.XrefPath.Text) || (block.Flags.Integer.GetValueOrDefault()&124)!=0)
                {states[source.Index]=CadNormalizationState.UnsupportedNormalization;Issue("CAD_DEFINITION_REJECTED",source.Index);continue;}
                var children=new List<CadId>();foreach(var c in b.Children)
                {token.ThrowIfCancellationRequested();if(sourceEntities.TryGetValue(c.StructuralRecordIndex,out var e))children.Add(e.Id);}
                var id=d?.Id??CadOccurrenceIdentity.Source(sources,source,CadIdentityDomain.Definition);
                definitions.Add(new(id,block.Name.Text!,source,record is null?null:new(record.StructuralRecordIndex),baseResult.BaseMm.Value,children));
                states[source.Index]=CadNormalizationState.Normalized;Track(source.Index,id);
            }
        }
    }
}
