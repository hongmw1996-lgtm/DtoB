using DtoB.Cad.Normalize.Scalars;
using DtoB.Cad.V2;
using DtoB.Geometry;
using V2Document=DtoB.Cad.V2.CadDocument;
using V2Entity = DtoB.Cad.V2.CadEntity;

namespace DtoB.Cad.Normalize.Assembly;

public enum CadBoundsState { Exact, Partial, AnchorOnly, Unavailable }
public sealed record CadBounds(Point3 Min,Point3 Max);
public sealed record CadBoundsResult(CadBoundsState State,CadBounds? Bounds);

/// <summary>Query-only analytic extrema. Definition means direct local children; occurrence includes descendants in model coordinates.</summary>
public sealed class CadBoundsQuery
{
    private readonly V2Document document;
    private readonly Dictionary<string,CadGeometry> geometry;
    private readonly Dictionary<string,V2Entity> entities;
    private readonly Dictionary<string,CadBlockOccurrence> occurrences;
    public CadBoundsQuery(V2Document document)
    {
        this.document=CadContract.Required(document);
        geometry=document.Geometry.ToDictionary(g=>g.Id.Value,StringComparer.Ordinal);
        entities=document.Entities.ToDictionary(e=>e.Id.Value,StringComparer.Ordinal);
        occurrences=document.Occurrences.ToDictionary(o=>o.Id.Value,StringComparer.Ordinal);
    }
    public CadBoundsResult GeometryLocal(CadId id,CancellationToken token=default)=>Geometry(id,CadTransform.Identity,new HashSet<string>(StringComparer.Ordinal),token);
    public CadBoundsResult GeometryTransformed(CadId id,CadTransform transform,CancellationToken token=default)=>Geometry(id,transform,new HashSet<string>(StringComparer.Ordinal),token);
    public CadBoundsResult Definition(CadId id,CancellationToken token=default)
    {
        var d=document.Definitions.Single(d=>d.Id.Value==id.Value);var results=new List<CadBoundsResult>();
        foreach(var child in d.ChildEntityIds){token.ThrowIfCancellationRequested();results.Add(Entity(entities[child.Value],CadTransform.Identity,token));}
        return Union(results);
    }
    public CadBoundsResult Occurrence(CadId id,CancellationToken token=default)
    {
        CadContract.Require(occurrences.ContainsKey(id.Value),"Unknown occurrence.");var admitted=new HashSet<string>(StringComparer.Ordinal){id.Value};
        foreach(var o in document.Occurrences)
        {token.ThrowIfCancellationRequested();if(o.ParentOccurrenceId is {} parent&&admitted.Contains(parent.Value))admitted.Add(o.Id.Value);}
        return ModelRecords(document.ModelSpace.Where(m=>m.OccurrenceId is {} o&&admitted.Contains(o.Value)),token);
    }
    public CadBoundsResult Model(CancellationToken token=default)
    {
        var result=ModelRecords(document.ModelSpace,token);
        return result.State==CadBoundsState.Exact&&document.Completeness.State!=CadNormalizationState.Normalized?result with{State=CadBoundsState.Partial}:result;
    }
    private CadBoundsResult ModelRecords(IEnumerable<CadModelSpaceRecord> records,CancellationToken token)
    {
        var results=new List<CadBoundsResult>();
        foreach(var m in records)
        {
            token.ThrowIfCancellationRequested();var transform=m.OccurrenceId is {} o
                ?CadTransform.FromTransform3(occurrences[o.Value].AccumulatedTransform.ToTransform3().Then(document.SourceToModel.ToTransform3())):document.SourceToModel;
            results.Add(Entity(entities[m.EntityId.Value],transform,token));
        }
        return Union(results);
    }
    private CadBoundsResult Entity(V2Entity entity,CadTransform transform,CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if(entity.GeometryId is {} id)
        {
            var result=Geometry(id,transform,new HashSet<string>(StringComparer.Ordinal),token);
            return result.State==CadBoundsState.Exact&&entity.Disposition!=CadNormalizationState.Normalized?result with{State=CadBoundsState.Partial}:result;
        }
        if(entity.Annotation is {} a&&a.AnchorsMm.Count>0)
            return new(CadBoundsState.AnchorOnly,Points(a.AnchorsMm.Select(transform.ToTransform3().Apply),token));
        return new(CadBoundsState.Unavailable,null);
    }
    private CadBoundsResult Geometry(CadId id,CadTransform transform,HashSet<string> active,CancellationToken token)
    {
        token.ThrowIfCancellationRequested();CadContract.Require(active.Add(id.Value),"Cyclic geometry query.");
        try
        {
            var g=geometry[id.Value];var t=transform.ToTransform3();CadBounds? bounds;
            switch(g)
            {
                case CadPoint p:bounds=Points([t.Apply(p.PositionMm)],token);break;
                case CadLine l:bounds=Points([t.Apply(l.StartMm),t.Apply(l.EndMm)],token);break;
                case CadSolid s:bounds=Points(s.BoundaryMm.Select(t.Apply),token);break;
                case CadPolyline p:
                    var children=new List<CadBoundsResult>();foreach(var segment in p.SegmentIds){token.ThrowIfCancellationRequested();children.Add(Geometry(segment,transform,active,token));}return Union(children);
                case CadCircle c:bounds=Curve(c.Plane,c.RadiusMm,0,Math.Tau,transform,true,token);break;
                case CadArc a:bounds=Curve(a.Plane,a.RadiusMm,a.StartRadians,a.SweepRadians,transform,false,token);break;
                default:return new(CadBoundsState.Unavailable,null);
            }
            return new(bounds is null?CadBoundsState.Unavailable:CadBoundsState.Exact,bounds);
        }
        catch(InvalidDataException){return new(CadBoundsState.Unavailable,null);}
        finally{active.Remove(id.Value);}
    }
    private static CadBounds? Points(IEnumerable<Point3> points,CancellationToken token)
    {
        CadBounds? result=null;
        foreach(var p in points)
        {
            token.ThrowIfCancellationRequested();p.ValidateStructure();
            result=result is null?new(p,p):Merge(result,new(p,p));
        }
        return result;
    }
    private static CadBounds Merge(CadBounds a,CadBounds b)=>new(new(Math.Min(a.Min.X,b.Min.X),Math.Min(a.Min.Y,b.Min.Y),Math.Min(a.Min.Z,b.Min.Z)),
        new(Math.Max(a.Max.X,b.Max.X),Math.Max(a.Max.Y,b.Max.Y),Math.Max(a.Max.Z,b.Max.Z)));
    private static CadBoundsResult Union(IEnumerable<CadBoundsResult> values)
    {
        CadBounds? bounds=null;var exact=true;var onlyAnchors=true;
        foreach(var v in values)
        {exact&=v.State==CadBoundsState.Exact;onlyAnchors&=v.State==CadBoundsState.AnchorOnly;if(v.Bounds is {} b)bounds=bounds is null?b:Merge(bounds,b);}
        return new(bounds is null?CadBoundsState.Unavailable:exact?CadBoundsState.Exact:onlyAnchors?CadBoundsState.AnchorOnly:CadBoundsState.Partial,bounds);
    }
    private static CadBounds Curve(CadPlane plane,double radius,double start,double sweep,CadTransform transform,bool full,CancellationToken token)
    {
        var c=transform.ToTransform3().Apply(plane.OriginMm);var m=transform.Values;
        double[] Vector(Point3 p)=>[m[0]*p.X+m[1]*p.Y+m[2]*p.Z,m[4]*p.X+m[5]*p.Y+m[6]*p.Z,m[8]*p.X+m[9]*p.Y+m[10]*p.Z];
        var u=Vector(plane.AxisU);var v=Vector(plane.AxisV);double[] center=[c.X,c.Y,c.Z];var min=new double[3];var max=new double[3];
        for(var j=0;j<3;j++)
        {
            token.ThrowIfCancellationRequested();var a=u[j]*radius;var b=v[j]*radius;CadContract.Finite(a);CadContract.Finite(b);
            if(full)
            {var amplitude=Hypot(a,b);min[j]=center[j]-amplitude;max[j]=center[j]+amplitude;}
            else
            {
                var candidates=new List<double>{start,start+sweep};var angle=Math.Atan2(b,a);
                if(Inside(angle,start,sweep))candidates.Add(angle);if(Inside(angle+Math.PI,start,sweep))candidates.Add(angle+Math.PI);
                var values=candidates.Select(t=>center[j]+a*Math.Cos(t)+b*Math.Sin(t)).ToArray();min[j]=values.Min();max[j]=values.Max();
            }
            CadContract.Finite(min[j]);CadContract.Finite(max[j]);
        }
        return new(new(min[0],min[1],min[2]),new(max[0],max[1],max[2]));
    }
    private static bool Inside(double angle,double start,double sweep)=>Math.Abs(sweep)>=Math.Tau||
        (sweep>=0?CadAngleNormalizer.WrapPositiveRadians(angle-start):CadAngleNormalizer.WrapPositiveRadians(start-angle))<=Math.Abs(sweep);
    private static double Hypot(double a,double b)
    {a=Math.Abs(a);b=Math.Abs(b);var max=Math.Max(a,b);return max==0?0:max*Math.Sqrt(1+Math.Pow(Math.Min(a,b)/max,2));}
}
