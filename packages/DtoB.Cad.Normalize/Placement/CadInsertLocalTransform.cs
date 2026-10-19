using DtoB.Cad.Normalize.Coordinates;
using DtoB.Cad.Normalize.Scalars;
using DtoB.Cad.Raw;
using DtoB.Cad.V2;
using DtoB.Geometry;

namespace DtoB.Cad.Normalize.Placement;

public static class CadInsertLocalTransform
{
    public static CadLocalPlacementResult Create(RawDrawingSnapshot snapshot, RawDrawingRecordView insertRow,
        CadNumericPolicy policy, CadUnitResolution? units = null, CancellationToken cancellationToken = default)
        => CreateCore(snapshot,insertRow,policy,units,cancellationToken,false);

    // 04E validates counts/budgets and owns cell expansion. Keep ordinary public refusal unchanged.
    internal static CadLocalPlacementResult CreateArrayTemplate(RawDrawingSnapshot snapshot, RawDrawingRecordView insertRow,
        CadNumericPolicy policy, CadUnitResolution units, CancellationToken cancellationToken)
        => CreateCore(snapshot,insertRow,policy,units,cancellationToken,true);

    private static CadLocalPlacementResult CreateCore(RawDrawingSnapshot snapshot, RawDrawingRecordView insertRow,
        CadNumericPolicy policy, CadUnitResolution? units, CancellationToken cancellationToken, bool arrayTemplate)
    {
        ArgumentNullException.ThrowIfNull(policy); cancellationToken.ThrowIfCancellationRequested();
        var header = RawScalarAccess.HeaderUnits(snapshot);
        CadContract.Require(insertRow.StructuralRecordIndex >= 0 && insertRow.StructuralRecordIndex < snapshot.Records.Count
            && ReferenceEquals(snapshot.Records[insertRow.StructuralRecordIndex],insertRow),"INSERT row must belong to snapshot.");
        var source = new CadPlacementSource(header.Source.Document,insertRow);
        CadLocalPlacementResult Fail(CadPlacementDiagnosticCode code, string field, CadPlacementStatus status = CadPlacementStatus.Invalid,
            IReadOnlyList<int>? pairs = null) => new(status,null,[new(code,source,field,pairs)]);
        if (insertRow.BlockAnnotation?.Payload is not RawInsert insert)
            return Fail(CadPlacementDiagnosticCode.UnsupportedInsertProfile,"Entity",CadPlacementStatus.Unsupported);
        units ??= CadUnitNormalizer.Resolve(header);
        if (!CadBlockBaseNormalizer.SameDocument(source.Document,units.Source.Document))
            return Fail(CadPlacementDiagnosticCode.SourceAuthorityMismatch,"Units");
        if (units.Status != CadUnitStatus.Resolved) return Fail(CadPlacementDiagnosticCode.UnresolvedUnits,"Units",CadPlacementStatus.Unsupported);
        if (!arrayTemplate && (insert.OccurrenceExpansionUnsupported || Count(insert.Columns) > 1 || Count(insert.Rows) > 1))
            return Fail(CadPlacementDiagnosticCode.UnsupportedInsertProfile,"Array",CadPlacementStatus.Unsupported);
        if (arrayTemplate ? Count(insert.Columns) is null or < 1 || Count(insert.Rows) is null or < 1
            : Count(insert.Columns) != 1 || Count(insert.Rows) != 1)
            return Fail(CadPlacementDiagnosticCode.UnsupportedInsertProfile,"ArrayCounts");
        var target = new CadPlacementNameFact(insert.TargetName);
        if (!target.Valid) return Fail(CadPlacementDiagnosticCode.InvalidBlockDefinition,"TargetName",pairs:target.PairIndices);
        var candidates = new List<(RawBlockDefinition Definition,RawDrawingRecordView Row,RawBlockBegin Block)>();
        foreach (var definition in snapshot.BlockDefinitions)
        {
            cancellationToken.ThrowIfCancellationRequested(); var row = snapshot.Records[definition.BeginRecordIndex];
            if (row.BlockAnnotation?.Payload is RawBlockBegin block && new CadPlacementNameFact(block.Name) is {Valid:true} name
                && StringComparer.OrdinalIgnoreCase.Equals(name.Value,target.Value)) candidates.Add((definition,row,block));
        }
        if (candidates.Count == 0) return Fail(CadPlacementDiagnosticCode.MissingBlockDefinition,"TargetName",pairs:target.PairIndices);
        if (candidates.Count != 1) return Fail(CadPlacementDiagnosticCode.AmbiguousBlockDefinition,"TargetName",pairs:target.PairIndices);
        var found = candidates[0]; var blockSource = new CadPlacementSource(source.Document,found.Row);
        CadLocalPlacementResult BlockFail(CadPlacementDiagnosticCode code, string field, CadPlacementStatus status = CadPlacementStatus.Invalid) =>
            new(status,null,[new(code,blockSource,field)]);
        if (found.Definition.Disposition != DxfDisposition.Supported || found.Definition.EndRecordIndex is null
            || !ValidInteger(found.Block.Flags) || found.Block.Flags.Integer < 0)
            return BlockFail(CadPlacementDiagnosticCode.InvalidBlockDefinition,"Definition");
        if ((found.Block.Flags.Integer!.Value & (4|8|16)) != 0 || !string.IsNullOrEmpty(found.Block.XrefPath.Text))
            return BlockFail(CadPlacementDiagnosticCode.UnsupportedDefinition,"ExternalReference",CadPlacementStatus.Unsupported);
        if (found.Block.Flags.Integer.Value > 127) return BlockFail(CadPlacementDiagnosticCode.UnsupportedDefinition,"Flags",CadPlacementStatus.Unsupported);
        var blockRecords = new List<RawDrawingRecordView>();
        foreach (var row in snapshot.Records)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (row.SourceRecord.Scope != "TABLES/BLOCK_RECORD") continue;
            var name = row.SourceRecord.Fields.SingleOrDefault(f=>f.Name == "Name")?.Fact;
            if (name is not null && new CadPlacementNameFact(name) is {Valid:true} named && StringComparer.OrdinalIgnoreCase.Equals(named.Value,target.Value)) blockRecords.Add(row);
        }
        if (blockRecords.Count > 1) return BlockFail(CadPlacementDiagnosticCode.AmbiguousBlockDefinition,"BlockRecord");
        // A resolved BLOCK owner, when present, must agree with the same named BLOCK_RECORD.
        // This inspects source identity only; it does not follow definition contents.
        foreach (var edge in snapshot.Ownership)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (edge.Child.Kind != DxfGraphRecordKind.StructuralRecord || edge.Child.Index != found.Row.StructuralRecordIndex) continue;
            if (edge.Owner.Kind != DxfGraphRecordKind.StructuralRecord || blockRecords.Count != 1
                || edge.Owner.Index != blockRecords[0].StructuralRecordIndex)
                return BlockFail(CadPlacementDiagnosticCode.InvalidBlockDefinition,"BlockRecordOwner");
        }
        CadBlockUnitResolution? blockUnits = blockRecords.Count == 0 ? null : CadUnitNormalizer.CheckBlock(
            RawScalarAccess.Record(snapshot,blockRecords[0],CadScalarRole.BlockInsertionUnits,cancellationToken),units);
        var baseResult = CadBlockBaseNormalizer.Normalize(blockSource,found.Block,units,blockUnits);
        if (baseResult.Status != CadPlacementStatus.Normalized) return new(baseResult.Status,null,baseResult.Diagnostics);
        var scales = CadInsertScaleNormalizer.Normalize(source,insert.Scale);
        if (scales.Status != CadPlacementStatus.Normalized) return new(scales.Status,null,scales.Diagnostics);
        var rotation = CadAngleNormalizer.Normalize(RawScalarAccess.Record(snapshot,insertRow,CadScalarRole.InsertRotation,cancellationToken));
        if (rotation.Status != CadScalarStatus.Normalized) return Fail(CadPlacementDiagnosticCode.InvalidInsertRotation,"Rotation",pairs:rotation.Source.PairIndices);
        var anchor = CadCoordinateNormalizer.NormalizePoint(RawCoordinateAccess.Record(snapshot,insertRow,CadCoordinateRole.InsertInsertionOcs,cancellationToken:cancellationToken),units,policy);
        if (anchor.Status != CadCoordinateStatus.Normalized) return Fail(CadPlacementDiagnosticCode.InvalidInsertAnchor,"Anchor/Frame");
        if (insertRow.SemanticDisposition != DxfDisposition.Supported
            && !(arrayTemplate && insert.OccurrenceExpansionUnsupported && insertRow.SemanticDisposition == DxfDisposition.Unsupported))
            return Fail(CadPlacementDiagnosticCode.UnsupportedInsertProfile,"SourceDisposition");
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var b = baseResult.BaseMm!.Value; var a = anchor.Frame!; var s = scales.Scale!;
            // Ai is already the 04C WCS anchor. A*T(i) = T(Ai)*A, so A is applied once.
            var basis = new Transform3([a.Ax.X,a.Ay.X,a.N.X,0,a.Ax.Y,a.Ay.Y,a.N.Y,0,a.Ax.Z,a.Ay.Z,a.N.Z,0,0,0,0,1]);
            var subtractBase = Transform3.Placement(new(-b.X,-b.Y,-b.Z),0,new(1,1,1));
            var rotateScale = Transform3.Placement(new(0,0,0),rotation.Value!.Value,new(s.X,s.Y,s.Z));
            var translateAnchor = Transform3.Placement(anchor.PointMm!.Value,0,new(1,1,1));
            var local = subtractBase.Then(rotateScale).Then(basis).Then(translateAnchor);
            var result = new CadLocalPlacement(CadTransform.FromTransform3(local),target,baseResult,anchor,rotation,scales,policy);
            return new(CadPlacementStatus.Normalized,result,[]);
        }
        catch (InvalidDataException) { return Fail(CadPlacementDiagnosticCode.NonFiniteTransform,"LocalMatrix"); }
    }
    private static bool ValidInteger(DxfSourceFact fact) => fact.Kind == DxfFactKind.Integer && fact.Presence == DxfFactPresence.Present
        && fact.Validity == DxfFactValidity.Valid && fact.Integer.HasValue && fact.Text is null && fact.Real is null
        && fact.SourcePairIndices.Count == 1 && fact.Source is {} span && span.StartPairIndex == fact.SourcePairIndices[0]
        && span.EndPairIndex == span.StartPairIndex && span.StartLine > 0 && span.EndLine >= span.StartLine;
    private static int? Count(DxfSourceFact fact) => fact.Kind == DxfFactKind.Integer && fact.Presence == DxfFactPresence.Absent
        && fact.Validity == DxfFactValidity.NotApplicable && fact.SourcePairIndices.Count == 0 && fact.Source is null
        && fact.Integer is null && fact.Real is null && fact.Text is null ? 1 : ValidInteger(fact) ? fact.Integer : null;

    public static CadPlacementApplicationResult ApplyPoint(CadLocalPlacement placement, Point3 point)
    {
        ArgumentNullException.ThrowIfNull(placement);
        try
        { var p = placement.Transform.ToTransform3().Apply(point); return new(new(CadContract.Finite(p.X),CadContract.Finite(p.Y),CadContract.Finite(p.Z)),null,[]); }
        catch (InvalidDataException) { return ApplicationFailure(placement,"Point"); }
    }
    public static CadPlacementApplicationResult ApplyVector(CadLocalPlacement placement, CadVector3 vector)
    {
        ArgumentNullException.ThrowIfNull(placement); ArgumentNullException.ThrowIfNull(vector);
        var m = placement.Transform.Values;
        try { return new(null,new(m[0]*vector.X+m[1]*vector.Y+m[2]*vector.Z,m[4]*vector.X+m[5]*vector.Y+m[6]*vector.Z,m[8]*vector.X+m[9]*vector.Y+m[10]*vector.Z),[]); }
        catch (InvalidDataException) { return ApplicationFailure(placement,"Vector"); }
    }
    private static CadPlacementApplicationResult ApplicationFailure(CadLocalPlacement placement, string field) =>
        new(null,null,[new(CadPlacementDiagnosticCode.NonFiniteTransformedValue,placement.Scale.Source,field)]);
}
