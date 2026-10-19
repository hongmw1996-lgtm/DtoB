using DtoB.Geometry;

namespace DtoB.Cad.V2;

public sealed class CadAnnotation
{
    public CadAnnotationKind Kind { get; }
    public string? Content { get; }
    public IReadOnlyList<Point3> AnchorsMm { get; }
    public double? SourceMeasurementMm { get; }
    public double? HeightMm { get; }
    public double? RotationRadians { get; }
    public CadAnnotation(CadAnnotationKind kind, string? content, IReadOnlyList<Point3> anchorsMm,
        double? sourceMeasurementMm, double? heightMm, double? rotationRadians)
    {
        Kind = CadContract.EnumValue(kind); Content = content; AnchorsMm = CadContract.Copy(anchorsMm);
        foreach (var p in AnchorsMm) p.ValidateStructure();
        SourceMeasurementMm = sourceMeasurementMm is { } m ? CadContract.Finite(m) : null;
        HeightMm = heightMm is { } h ? CadContract.Positive(h) : null;
        RotationRadians = rotationRadians is { } a ? CadContract.Finite(a) : null;
    }
}

public sealed class CadEntity(CadId id, CadSourceRecordRef source, CadEntityKind kind,
    CadNormalizationState disposition, CadId? geometryId, CadId? styleId, CadAnnotation? annotation,
    IReadOnlyList<CadSourceFactRef> evidence)
{
    public CadId Id { get; } = CadContract.Required(id);
    public CadSourceRecordRef Source { get; } = CadContract.Required(source);
    public CadEntityKind Kind { get; } = CadContract.EnumValue(kind);
    public CadNormalizationState Disposition { get; } = CadContract.EnumValue(disposition);
    public CadId? GeometryId { get; } = geometryId;
    public CadId? StyleId { get; } = styleId;
    public CadAnnotation? Annotation { get; } = annotation;
    public IReadOnlyList<CadSourceFactRef> Evidence { get; } = CadContract.Copy(evidence);
}

public sealed class CadBlockDefinition(CadId id, string name, CadSourceRecordRef blockSource,
    CadSourceRecordRef? blockRecordSource, Point3 basePointMm, IReadOnlyList<CadId> childEntityIds)
{
    public CadId Id { get; } = CadContract.Required(id);
    public string Name { get; } = CadContract.Text(name);
    public CadSourceRecordRef BlockSource { get; } = CadContract.Required(blockSource);
    public CadSourceRecordRef? BlockRecordSource { get; } = blockRecordSource;
    public Point3 BasePointMm { get; } = Checked(basePointMm);
    public IReadOnlyList<CadId> ChildEntityIds { get; } = CadContract.Copy(childEntityIds);
    private static Point3 Checked(Point3 p) { p.ValidateStructure(); return p; }
}
public sealed class CadBlockOccurrence(CadId id, CadSourceRecordRef insertSource, CadId definitionId,
    CadId? parentOccurrenceId, CadOccurrencePath path, CadTransform localTransform, CadTransform accumulatedTransform,
    CadId? styleId)
{
    public CadId Id { get; } = CadContract.Required(id);
    public CadSourceRecordRef InsertSource { get; } = CadContract.Required(insertSource);
    public CadId DefinitionId { get; } = CadContract.Required(definitionId);
    public CadId? ParentOccurrenceId { get; } = parentOccurrenceId;
    public CadOccurrencePath Path { get; } = CadContract.Required(path);
    public CadTransform LocalTransform { get; } = CadContract.Required(localTransform);
    public CadTransform AccumulatedTransform { get; } = CadContract.Required(accumulatedTransform);
    public CadId? StyleId { get; } = styleId;
}
public sealed class CadModelSpaceRecord(CadId entityId, CadId? occurrenceId)
{
    public CadId EntityId { get; } = CadContract.Required(entityId);
    public CadId? OccurrenceId { get; } = occurrenceId;
}
public sealed class CadDiagnostic(string code, CadSeverity severity, CadStage stage, CadSourceRecordRef? source,
    CadSourceFactRef? fact, CadOccurrencePath? path, IReadOnlyList<string> arguments)
{
    public string Code { get; } = CadContract.Text(code);
    public CadSeverity Severity { get; } = CadContract.EnumValue(severity);
    public CadStage Stage { get; } = CadContract.EnumValue(stage);
    public CadSourceRecordRef? Source { get; } = source;
    public CadSourceFactRef? Fact { get; } = fact;
    public CadOccurrencePath? Path { get; } = path;
    public IReadOnlyList<string> Arguments { get; } = CadContract.Copy(arguments);
}
public sealed class CadRecordOutcome(CadSourceRecordRef source, CadNormalizationState state, IReadOnlyList<CadId> objectIds)
{
    public CadSourceRecordRef Source { get; } = CadContract.Required(source);
    public CadNormalizationState State { get; } = CadContract.EnumValue(state);
    public IReadOnlyList<CadId> ObjectIds { get; } = CadContract.Copy(objectIds);
}
public sealed class CadCompleteness
{
    public CadNormalizationState State { get; }
    public int SourceRecordCount { get; }
    public int AccountedRecordCount { get; }
    public CadCompleteness(CadNormalizationState state, int sourceRecordCount, int accountedRecordCount)
    {
        State = CadContract.EnumValue(state); SourceRecordCount = CadContract.Index(sourceRecordCount);
        AccountedRecordCount = CadContract.Index(accountedRecordCount);
        CadContract.Require(state is CadNormalizationState.Normalized or CadNormalizationState.PartiallyNormalized or CadNormalizationState.UnsupportedNormalization
            && accountedRecordCount <= sourceRecordCount, "Invalid document completeness.");
    }
}

/// <summary>
/// Immutable contract snapshot, constructed explicitly; not a spatial assembler.
/// Collections retain source/DFS/row-major order supplied by the future engine. No dictionary-based ordering.
/// All JSON members are required, including explicit nulls for nullable facts; null means unavailable, never zero/default.
/// </summary>
public sealed class CadDocument
{
    public int SchemaVersion { get; }
    public string AlgorithmVersion { get; }
    public string ProfileId { get; }
    public CadConventions Conventions { get; }
    public CadUnitDecision Units { get; }
    public CadTransform SourceToModel { get; }
    public CadProvenanceRegistry Provenance { get; }
    public IReadOnlyList<CadLayer> Layers { get; }
    public IReadOnlyList<CadBlockDefinition> Definitions { get; }
    public IReadOnlyList<CadEntity> Entities { get; }
    public IReadOnlyList<CadBlockOccurrence> Occurrences { get; }
    public IReadOnlyList<CadModelSpaceRecord> ModelSpace { get; }
    public IReadOnlyList<CadGeometry> Geometry { get; }
    public IReadOnlyList<CadStyle> Styles { get; }
    public IReadOnlyList<CadRecordOutcome> Ledger { get; }
    public IReadOnlyList<CadDiagnostic> Diagnostics { get; }
    public IReadOnlyList<CadDiagnostic> RawDiagnostics { get; }
    public CadCompleteness Completeness { get; }
    public CadDocument(int schemaVersion, string algorithmVersion, string profileId, CadConventions conventions,
        CadUnitDecision units, CadTransform sourceToModel, CadProvenanceRegistry provenance,
        IReadOnlyList<CadLayer> layers, IReadOnlyList<CadBlockDefinition> definitions, IReadOnlyList<CadEntity> entities,
        IReadOnlyList<CadBlockOccurrence> occurrences, IReadOnlyList<CadModelSpaceRecord> modelSpace,
        IReadOnlyList<CadGeometry> geometry, IReadOnlyList<CadStyle> styles, IReadOnlyList<CadRecordOutcome> ledger,
        IReadOnlyList<CadDiagnostic> diagnostics, IReadOnlyList<CadDiagnostic> rawDiagnostics, CadCompleteness completeness)
    {
        SchemaVersion = CadContract.Version(schemaVersion); AlgorithmVersion = CadContract.Text(algorithmVersion);
        ProfileId = CadContract.Text(profileId); Conventions = CadContract.Required(conventions);
        Units = CadContract.Required(units); SourceToModel = CadContract.Required(sourceToModel); Provenance = CadContract.Required(provenance);
        Layers = CadContract.Copy(layers); Definitions = CadContract.Copy(definitions); Entities = CadContract.Copy(entities);
        Occurrences = CadContract.Copy(occurrences); ModelSpace = CadContract.Copy(modelSpace); Geometry = CadContract.Copy(geometry);
        Styles = CadContract.Copy(styles); Ledger = CadContract.Copy(ledger); Diagnostics = CadContract.Copy(diagnostics);
        RawDiagnostics = CadContract.Copy(rawDiagnostics); Completeness = CadContract.Required(completeness);
        ValidateStructure();
    }

    public void ValidateStructure()
    {
        var objects = Entities.Select(x => x.Id).Concat(Definitions.Select(x => x.Id)).Concat(Occurrences.Select(x => x.Id))
            .Concat(Geometry.Select(x => x.Id)).Concat(Styles.Select(x => x.Id)).ToArray();
        CadContract.Require(objects.Select(x => x.Value).Distinct(StringComparer.Ordinal).Count() == objects.Length, "Duplicate CAD object ID.");
        var allIds = objects.Select(x => x.Value).ToHashSet(StringComparer.Ordinal);
        var entityIds = Entities.Select(x => x.Id.Value).ToHashSet(StringComparer.Ordinal);
        var geometryIds = Geometry.Select(x => x.Id.Value).ToHashSet(StringComparer.Ordinal);
        var definitionIds = Definitions.Select(x => x.Id.Value).ToHashSet(StringComparer.Ordinal);
        var occurrenceIds = Occurrences.Select(x => x.Id.Value).ToHashSet(StringComparer.Ordinal);
        var styleIds = Styles.Select(x => x.Id.Value).ToHashSet(StringComparer.Ordinal);
        void Optional(CadId? id, HashSet<string> set) { if (id is not null) CadContract.Require(set.Contains(id.Value), "Unknown CAD reference."); }
        void Evidence(IReadOnlyList<CadSourceFactRef> refs) { foreach (var r in refs) Provenance.Resolve(r); }
        void Path(CadOccurrencePath? path) { if (path is not null) foreach (var s in path.Steps) { Provenance.Resolve(s.InsertSource); Provenance.Resolve(s.DefinitionSource); } }
        foreach (var layer in Layers) { Provenance.Resolve(layer.Source); Optional(layer.StyleId, styleIds); }
        CadContract.Require(Layers.Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() == Layers.Count, "Ambiguous layer name.");
        foreach (var definition in Definitions)
        {
            Provenance.Resolve(definition.BlockSource); if (definition.BlockRecordSource is not null) Provenance.Resolve(definition.BlockRecordSource);
            foreach (var id in definition.ChildEntityIds) Optional(id, entityIds);
        }
        foreach (var entity in Entities)
        { Provenance.Resolve(entity.Source); Evidence(entity.Evidence); Optional(entity.GeometryId, geometryIds); Optional(entity.StyleId, styleIds); }
        foreach (var g in Geometry)
        {
            Provenance.Resolve(g.Source); Evidence(g.Evidence);
            if (g is CadPolyline p) foreach (var id in p.SegmentIds)
                CadContract.Require(Geometry.Any(s => s.Id.Value == id.Value && s is CadLine or CadArc), "Polyline references must be line/arc payloads.");
        }
        foreach (var occurrence in Occurrences)
        {
            Provenance.Resolve(occurrence.InsertSource); Optional(occurrence.DefinitionId, definitionIds);
            Optional(occurrence.ParentOccurrenceId, occurrenceIds); Optional(occurrence.StyleId, styleIds); Path(occurrence.Path);
            CadContract.Require(occurrence.Path.Steps.Count > 0 && occurrence.Path.Steps[^1].InsertSource.Index == occurrence.InsertSource.Index,
                "Occurrence must retain source INSERT path.");
        }
        foreach (var root in ModelSpace) { Optional(root.EntityId, entityIds); Optional(root.OccurrenceId, occurrenceIds); }
        foreach (var style in Styles) { Evidence(style.Layer.Evidence); Evidence(style.Color.Evidence); Evidence(style.Linetype.Evidence); }
        foreach (var d in Diagnostics.Concat(RawDiagnostics))
        { if (d.Source is not null) Provenance.Resolve(d.Source); if (d.Fact is not null) Provenance.Resolve(d.Fact); Path(d.Path); }
        if (Units.Evidence is not null) Provenance.Resolve(Units.Evidence);
        CadContract.Require(Completeness.SourceRecordCount == Provenance.Records.Count && Completeness.AccountedRecordCount == Ledger.Count
            && Ledger.Select(x => x.Source.Index).SequenceEqual(Enumerable.Range(0, Provenance.Records.Count)), "Every registry record needs one ordered ledger outcome.");
        foreach (var entry in Ledger) foreach (var id in entry.ObjectIds) Optional(id, allIds);
        if (Completeness.State == CadNormalizationState.Normalized)
            CadContract.Require(Ledger.All(x => x.State is CadNormalizationState.Normalized or CadNormalizationState.NotApplicable), "Complete document hides partial records.");
    }
}
