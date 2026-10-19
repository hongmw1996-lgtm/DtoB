using DtoB.Cad.Raw;
using DtoB.Cad.V2;

namespace DtoB.Cad.Normalize.Styles;

/// <summary>Only retained, correctly scoped facts. No parsing source files or changing Raw mappings.</summary>
internal static class RawStyleAccess
{
    internal static Dictionary<int,List<DxfRawPair>> Read(RawDrawingSnapshot snapshot,RawDrawingRecordView row,string native,CancellationToken token)
    {
        var result=new Dictionary<int,List<DxfRawPair>>();
        var markers=row.SourceRecord.ContextMarkers.ToDictionary(m=>m.PairIndex);
        string? subclass=null,application=null; var control="";
        for(var i=row.SourceRecord.Source.StartPairIndex;i<=row.SourceRecord.Source.EndPairIndex;i++)
        {
            token.ThrowIfCancellationRequested();
            if(markers.TryGetValue(i,out var m)){subclass=m.Subclass; application=m.Application; control=m.ControlPath; continue;}
            if(application is not null||control.Length!=0)continue;
            var pair=snapshot.LexicalEvidence.Pairs[i];
            var eligible=subclass is null||subclass==native||(native!="AcDbEntity"&&subclass=="AcDbSymbolTableRecord"&&pair.GroupCode==2);
            if(!eligible||pair.GroupCode is not (2 or 6 or 8 or 60 or 62 or 70 or 370 or 420 or 430 or 440))continue;
            if(!result.TryGetValue(pair.GroupCode,out var list))result.Add(pair.GroupCode,list=[]);
            list.Add(pair);
        }
        return result;
    }
    internal static Func<int,bool,CadStyleFact> Facts(RawDrawingSnapshot snapshot,RawDrawingRecordView row,string native,CancellationToken token)
    {
        var pairs=Read(snapshot,row,native,token); var source=new CadSourceRecordRef(row.StructuralRecordIndex);
        return (code,integer)=>new(source,code,pairs.TryGetValue(code,out var list)?list:[],integer);
    }
    internal static bool Entity(RawDrawingRecordView row)=>row.SourceSection is "ENTITIES" or "BLOCKS"
        && row.SourceRecord.RecordType is not ("BLOCK" or "ENDBLK" or "SEQEND" or "VERTEX");
}
