using DtoB.Cad.Normalize.Coordinates;
using DtoB.Cad.Normalize.Occurrences;
using DtoB.Cad.Normalize.Scalars;
using DtoB.Cad.Raw;
using DtoB.Cad.V2;
using DtoB.Geometry;
using V2Entity = DtoB.Cad.V2.CadEntity;

namespace DtoB.Cad.Normalize.Assembly;

public sealed partial class CadNormalizer
{
    private sealed partial class Session
    {
        private void BuildEntity(RawDrawingRecordView row)
        {
            var i=row.StructuralRecordIndex;var source=new CadSourceRecordRef(i);var isInsert=row.BlockAnnotation?.Payload is RawInsert;
            var id=isInsert?CadOccurrenceIdentity.Source(sources,source,CadIdentityDomain.SourceInsert):CadAssemblyIdentity.Source(sources,source);
            var declared=styleInput.Declarations.FirstOrDefault(d=>d.Source.Index==i);
            CadId? style=null;
            if(declared is not null) style=row.SourceSection=="ENTITIES"&&!isInsert?EffectiveStyle(RequiredProjection(source,null),"effective-style"):DeclaredStyle(declared);
            var kind=isInsert?CadEntityKind.Insert:row.Geometry?.Payload is not null?CadEntityKind.Primitive:
                row.BlockAnnotation?.Payload is RawText or RawMText or RawDimension?CadEntityKind.Annotation:CadEntityKind.Unsupported;
            var disposition=kind==CadEntityKind.Unsupported?CadNormalizationState.UnsupportedNormalization:CadNormalizationState.Normalized;
            if(row.SourceSection=="ENTITIES"&&!isInsert&&declared is not null)
            {
                var effective=RequiredProjection(source,null);
                if(effective.Layer.Value is null||effective.Color.Value is null||effective.Linetype.Value is null)disposition=CadNormalizationState.PartiallyNormalized;
            }
            CadGeometry? payload=null;CadAnnotation? annotation=null;
            var before=diagnostics.Count;
            try
            {
                if(kind==CadEntityKind.Primitive)payload=BuildGeometry(row,id);
                if(kind==CadEntityKind.Annotation)annotation=BuildAnnotation(row);
                if(kind==CadEntityKind.Unsupported)Issue("CAD_ENTITY_UNSUPPORTED",i,row.SourceRecord.RecordType);
                if(isInsert && !styleInput.Input.Expansion!.Occurrences.Any(o=>o.Value.InsertSource.Index==i))
                {disposition=CadNormalizationState.PartiallyNormalized;Issue("CAD_INSERT_NOT_MATERIALIZED",i);}
            }
            catch(InvalidDataException)
            {disposition=CadNormalizationState.InvalidNormalization;Issue("CAD_INVALID_GEOMETRY_OR_ANNOTATION",i);}
            if(disposition==CadNormalizationState.Normalized&&diagnostics.Count>before)disposition=CadNormalizationState.PartiallyNormalized;
            if(declared is not null&&(declared.Lineweight.Presence==DxfFactPresence.Present||declared.Transparency.Presence==DxfFactPresence.Present||declared.ColorName.Presence==DxfFactPresence.Present))
            {Issue("CAD_DECLARED_STYLE_CHANNEL_DEFERRED",i);if(disposition==CadNormalizationState.Normalized)disposition=CadNormalizationState.PartiallyNormalized;}
            var entity=new V2Entity(id,source,kind,disposition,payload?.Id,style,annotation,[recordFacts[i]]);
            sourceEntities.Add(i,entity);entities.Add(entity);Track(i,id);states[i]=disposition;
            if(row.SemanticDisposition==DxfDisposition.Unsupported)states[i]=CadNormalizationState.UnsupportedNormalization;
            if(row.SourceSection=="ENTITIES"&&!isInsert&&IsModelSource(row))model.Add(new(id,null));
        }
        private bool IsModelSource(RawDrawingRecordView row)
        {
            if(modelSources.TryGetValue(row.StructuralRecordIndex,out var known))return known;
            var markers=row.SourceRecord.ContextMarkers.ToDictionary(m=>m.PairIndex);string? subclass=null,application=null;var control="";var values=new List<string>();
            for(var i=row.SourceRecord.Source.StartPairIndex;i<=row.SourceRecord.Source.EndPairIndex;i++)
            {
                token.ThrowIfCancellationRequested();if(markers.TryGetValue(i,out var marker)){subclass=marker.Subclass;application=marker.Application;control=marker.ControlPath;continue;}
                var pair=raw.LexicalEvidence.Pairs[i];
                if(application is null&&control.Length==0&&(subclass is null or "AcDbEntity")&&pair.GroupCode==67)values.Add(pair.RawTextValue);
            }
            var model=values.Count==0||values.Count==1&&int.TryParse(values[0],System.Globalization.NumberStyles.Integer,System.Globalization.CultureInfo.InvariantCulture,out var space)&&space==0;
            if(!model)Issue("CAD_NON_MODEL_SPACE_RETAINED",row.StructuralRecordIndex);
            modelSources.Add(row.StructuralRecordIndex,model);return model;
        }
        private CadCoordinateResult PointResult(RawDrawingRecordView row,CadCoordinateRole role,int vertex=0)
        {
            token.ThrowIfCancellationRequested();var result=CadCoordinateNormalizer.NormalizePoint(RawCoordinateAccess.Record(raw,row,role,vertex,token),units,policy);
            CadContract.Require(result.PointMm is not null,"Coordinate normalization refused.");return result;
        }
        private Point3 Point(RawDrawingRecordView row,CadCoordinateRole role,int vertex=0)=>PointResult(row,role,vertex).PointMm!.Value;
        private double Length(RawDrawingRecordView row,CadScalarRole role)
        {var result=CadScalarLengthNormalizer.Normalize(RawScalarAccess.Record(raw,row,role,token),units);CadContract.Require(result.Value is not null,"Scalar normalization refused.");return result.Value!.Value;}
        private double? OptionalScalar(RawDrawingRecordView row,CadScalarRole role,bool angle)
        {
            var fact=RawScalarAccess.Record(raw,row,role,token);var r=angle?CadAngleNormalizer.Normalize(fact):CadScalarLengthNormalizer.Normalize(fact,units);
            if(r.Status is not (CadScalarStatus.Normalized or CadScalarStatus.Absent))Issue("CAD_ANNOTATION_CHANNEL_UNSUPPORTED",row.StructuralRecordIndex,role.ToString());
            return r.Value;
        }
        private static Point3 P(CadVector3 v)=>new(v.X,v.Y,v.Z);
        private static double Hypot(double a,double b)
        {a=Math.Abs(a);b=Math.Abs(b);var max=Math.Max(a,b);return max==0?0:max*Math.Sqrt(1+Math.Pow(Math.Min(a,b)/max,2));}
        private static Point3 Sub(Point3 a,Point3 b)=>new(a.X-b.X,a.Y-b.Y,a.Z-b.Z);
        private static Point3 Cross(Point3 a,Point3 b)=>new(a.Y*b.Z-a.Z*b.Y,a.Z*b.X-a.X*b.Z,a.X*b.Y-a.Y*b.X);
        private static double Dot(Point3 a,Point3 b)=>a.X*b.X+a.Y*b.Y+a.Z*b.Z;
        private static double Norm(Point3 a)=>Hypot(Hypot(a.X,a.Y),a.Z);
        private void Publish(CadGeometry g) {token.ThrowIfCancellationRequested();geometry.Add(g);Track(g.Source.Index,g.Id);}
        private CadGeometry BuildGeometry(RawDrawingRecordView row,CadId entity)
        {
            var source=new CadSourceRecordRef(row.StructuralRecordIndex);var id=CadAssemblyIdentity.Component(entity,"geometry");
            CadSourceFactRef[] evidence=[recordFacts[source.Index]];
            Reserve(); // Before any payload construction; failed source geometry still consumes admission.
            CadGeometry g;
            switch(row.Geometry!.Payload)
            {
                case RawLine:
                    var a=Point(row,CadCoordinateRole.LineStartWcs);var b=Point(row,CadCoordinateRole.LineEndWcs);
                    CadContract.Require(Norm(Sub(a,b))>policy.LengthMm,"Degenerate line.");g=new CadLine(id,source,evidence,a,b);break;
                case RawPoint p:
                    g=new CadPoint(id,source,evidence,Point(row,CadCoordinateRole.PointPositionWcs));
                    if(p.AngleDegrees.Presence==DxfFactPresence.Present)Issue("CAD_POINT_DISPLAY_ANGLE_DEFERRED",source.Index);break;
                case RawCircle:
                    var c=PointResult(row,CadCoordinateRole.CircleCenterOcs);g=new CadCircle(id,source,evidence,new(c.PointMm!.Value,P(c.Frame!.Ax),P(c.Frame.Ay)),Length(row,CadScalarRole.CircleRadius));break;
                case RawArc:
                    var center=PointResult(row,CadCoordinateRole.ArcCenterOcs);
                    var angles=CadAngleNormalizer.NormalizeArc(RawScalarAccess.Record(raw,row,CadScalarRole.ArcStartAngle,token),RawScalarAccess.Record(raw,row,CadScalarRole.ArcEndAngle,token));
                    CadContract.Require(angles.Status==CadScalarStatus.Normalized,"Unsupported arc interval.");
                    g=new CadArc(id,source,evidence,new(center.PointMm!.Value,P(center.Frame!.Ax),P(center.Frame.Ay)),Length(row,CadScalarRole.ArcRadius),angles.StartRadians!.Value,angles.SweepRadians!.Value);break;
                case RawLwPolyline poly:
                    var segments=new List<CadGeometry>();var n=poly.Vertices.Count;CadContract.Require(n>=2,"Insufficient polyline vertices.");
                    var flags=poly.Flags.Presence==DxfFactPresence.Absent?0:Integer(poly.Flags);
                    CadContract.Require(flags>=0&&(flags&~129)==0,"Unsupported polyline flags.");var closed=(flags&1)!=0;
                    if(poly.ConstantWidth.Presence==DxfFactPresence.Present||poly.Vertices.Any(v=>v.StartWidth.Presence==DxfFactPresence.Present||v.EndWidth.Presence==DxfFactPresence.Present))Issue("CAD_POLYLINE_WIDTH_DEFERRED",source.Index);
                    var count=closed?n:n-1;
                    for(var j=0;j<count;j++)
                    {
                        token.ThrowIfCancellationRequested();Reserve();var next=(j+1)%n;
                        var start=PointResult(row,CadCoordinateRole.LwPolylineVertexOcs,j);var end=PointResult(row,CadCoordinateRole.LwPolylineVertexOcs,next);
                        var vertex=poly.Vertices[j];var bulge=vertex.Bulge.Presence==DxfFactPresence.Absent?0:Real(vertex.Bulge);
                        var sid=CadAssemblyIdentity.Component(entity,"segment",j);
                        segments.Add(CadBulgeGeometry.Create(sid,source,evidence,start.PointMm!.Value,end.PointMm!.Value,P(start.Frame!.Ax),P(start.Frame.Ay),bulge,policy));
                    }
                    g=new CadPolyline(id,source,evidence,segments.Select(s=>s.Id).ToArray(),closed);
                    // Commit segment payloads only after the entire source polyline succeeds. Reservations are never refunded.
                    foreach(var segment in segments)Publish(segment);break;
                case RawSolid:
                    var p1=Point(row,CadCoordinateRole.SolidFirstOcs);var p2=Point(row,CadCoordinateRole.SolidSecondOcs);
                    var p3=Point(row,CadCoordinateRole.SolidThirdOcs);var p4=Point(row,CadCoordinateRole.SolidFourthOcs);
                    var normal=Cross(Sub(p2,p1),Sub(p3,p1));var length=Norm(normal);
                    CadContract.Require(double.IsFinite(length)&&length>0,"Degenerate solid.");
                    var unit=new Point3(normal.X/length,normal.Y/length,normal.Z/length);
                    CadContract.Require(Math.Abs(Dot(Sub(p4,p1),unit))<=policy.LengthMm,"Nonplanar solid.");
                    g=new CadSolid(id,source,evidence,p3==p4?[p1,p2,p3]:[p1,p2,p4,p3]);break;
                default:throw new InvalidDataException("Unsupported source geometry.");
            }
            if(row.Geometry.Thickness is {} thick&&thick.Presence==DxfFactPresence.Present&&(!thick.Real.HasValue||thick.Real.Value!=0))Issue("CAD_THICKNESS_DEFERRED",source.Index);
            Publish(g);return g;
        }
        private static double Real(DxfSourceFact f)
        {CadContract.Require(f.Kind==DxfFactKind.Real&&f.Validity==DxfFactValidity.Valid&&f.Real.HasValue,"Invalid real fact.");return CadContract.Finite(f.Real!.Value);}
        private static int Integer(DxfSourceFact f)
        {CadContract.Require(f.Kind==DxfFactKind.Integer&&f.Validity==DxfFactValidity.Valid&&f.Integer.HasValue,"Invalid integer fact.");return f.Integer!.Value;}
        private CadAnnotation BuildAnnotation(RawDrawingRecordView row)
        {
            var i=row.StructuralRecordIndex;Issue("CAD_ANNOTATION_ANCHORS_ONLY",i);
            switch(row.BlockAnnotation!.Payload)
            {
                case RawText t:
                    var anchors=new List<Point3>{Point(row,CadCoordinateRole.TextInsertionOcs)};
                    var alignment=CadCoordinateNormalizer.NormalizePoint(RawCoordinateAccess.Record(raw,row,CadCoordinateRole.TextAlignmentOcs,cancellationToken:token),units,policy);
                    if(alignment.PointMm is {} p)anchors.Add(p);else if(alignment.Status!=CadCoordinateStatus.Absent)Issue("CAD_TEXT_ALIGNMENT_UNAVAILABLE",i);
                    return new(CadAnnotationKind.Text,t.Content.Text,anchors,null,OptionalScalar(row,CadScalarRole.TextHeight,false),OptionalScalar(row,CadScalarRole.TextRotation,true));
                case RawMText m:
                    if(m.Direction.X.Presence==DxfFactPresence.Present||m.Direction.Y.Presence==DxfFactPresence.Present||m.Direction.Z.Presence==DxfFactPresence.Present)Issue("CAD_MTEXT_DIRECTION_DEFERRED",i);
                    return new(CadAnnotationKind.MText,string.Concat(m.Chunks.Select(c=>c.Content.Text)),[Point(row,CadCoordinateRole.MTextInsertionWcs)],null,
                        OptionalScalar(row,CadScalarRole.TextHeight,false),OptionalScalar(row,CadScalarRole.MTextRotation,true));
                case RawDimension d:
                    CadContract.Require(d.SubtypeProfileSupported,"Unsupported dimension subtype.");
                    var points=new List<Point3>();
                    foreach(var role in new[]{CadCoordinateRole.DimensionDefinitionWcs,CadCoordinateRole.DimensionFirstExtensionWcs,CadCoordinateRole.DimensionSecondExtensionWcs,CadCoordinateRole.DimensionTextMidpointOcs})
                    {
                        token.ThrowIfCancellationRequested();var r=CadCoordinateNormalizer.NormalizePoint(RawCoordinateAccess.Record(raw,row,role,cancellationToken:token),units,policy);
                        if(r.PointMm is {} point)points.Add(point);else if(r.Status!=CadCoordinateStatus.Absent)Issue("CAD_DIMENSION_ANCHOR_UNAVAILABLE",i,role.ToString());
                    }
                    return new(CadAnnotationKind.Dimension,d.TextOverride.Text,points,OptionalScalar(row,CadScalarRole.LinearDimensionMeasurement,false),null,OptionalScalar(row,CadScalarRole.LinearDimensionRotation,true));
                default:throw new InvalidDataException("Unsupported annotation.");
            }
        }
    }
}

/// <summary>Exact local line/arc interpretation. Bulge is tan(signed sweep/4), not source degrees.</summary>
public static class CadBulgeGeometry
{
    public static CadGeometry Create(CadId id,CadSourceRecordRef source,IReadOnlyList<CadSourceFactRef> evidence,
        Point3 start,Point3 end,Point3 axisU,Point3 axisV,double bulge,CadNumericPolicy policy)
    {
        start.ValidateStructure();end.ValidateStructure();CadContract.Finite(bulge);
        var dx=end.X-start.X;var dy=end.Y-start.Y;var dz=end.Z-start.Z;
        var max=Math.Max(Math.Abs(dx),Math.Max(Math.Abs(dy),Math.Abs(dz)));
        var length=max==0?0:max*Math.Sqrt(Math.Pow(dx/max,2)+Math.Pow(dy/max,2)+Math.Pow(dz/max,2));
        CadContract.Require(double.IsFinite(length)&&length>policy.LengthMm,"Degenerate bulge chord.");
        if(bulge==0)return new CadLine(id,source,evidence,start,end);
        var x=dx*axisU.X+dy*axisU.Y+dz*axisU.Z;var y=dx*axisV.X+dy*axisV.Y+dz*axisV.Z;
        var quarter=length/4;var b=Math.Abs(bulge);
        var offset=quarter/bulge-quarter*bulge;var radius=quarter*b+quarter/b;
        CadContract.Require(double.IsFinite(offset)&&double.IsFinite(radius)&&radius>0,"Unrepresentable bulge radius.");
        var left=new Point3((-y*axisU.X+x*axisV.X)/length,(-y*axisU.Y+x*axisV.Y)/length,(-y*axisU.Z+x*axisV.Z)/length);
        var center=new Point3(start.X/2+end.X/2+left.X*offset,start.Y/2+end.Y/2+left.Y*offset,start.Z/2+end.Z/2+left.Z*offset);
        center.ValidateStructure();var sx=start.X-center.X;var sy=start.Y-center.Y;var sz=start.Z-center.Z;
        var angle=CadAngleNormalizer.WrapPositiveRadians(Math.Atan2(sx*axisV.X+sy*axisV.Y+sz*axisV.Z,sx*axisU.X+sy*axisU.Y+sz*axisU.Z));
        var sweep=4*Math.Atan(bulge);
        CadContract.Require(Math.Abs(sweep)<Math.Tau,"Bulge sweep rounded to full circle.");
        // Refuse double representations that cannot reproduce factual endpoints within the caller's length policy.
        // Tiny bulges may require huge center/radius and lose the chord through cancellation. Never replace them with a straight segment.
        Point3 At(double t)=>new(center.X+radius*(axisU.X*Math.Cos(t)+axisV.X*Math.Sin(t)),
            center.Y+radius*(axisU.Y*Math.Cos(t)+axisV.Y*Math.Sin(t)),center.Z+radius*(axisU.Z*Math.Cos(t)+axisV.Z*Math.Sin(t)));
        bool Matches(Point3 expected,Point3 actual)=>double.IsFinite(actual.X)&&double.IsFinite(actual.Y)&&double.IsFinite(actual.Z)
            &&Math.Abs(expected.X-actual.X)<=policy.LengthMm&&Math.Abs(expected.Y-actual.Y)<=policy.LengthMm&&Math.Abs(expected.Z-actual.Z)<=policy.LengthMm;
        CadContract.Require(Matches(start,At(angle))&&Matches(end,At(angle+sweep)),"Bulge endpoint precision unavailable.");
        return new CadArc(id,source,evidence,new(center,axisU,axisV),radius,angle,sweep);
    }
}
