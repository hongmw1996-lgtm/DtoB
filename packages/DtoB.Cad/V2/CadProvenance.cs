namespace DtoB.Cad.V2;

public enum CadSourceRecordDomain { StructuralRecord, TableHeader }
public enum CadFactPresence { Present, Absent }

public sealed class CadSourceDocument
{
    public Guid DrawingId { get; }
    public Guid RevisionId { get; }
    public string FileHash { get; }
    public CadSourceDocument(Guid drawingId, Guid revisionId, string fileHash)
    {
        CadContract.Require(drawingId != Guid.Empty && revisionId != Guid.Empty, "Source GUIDs required.");
        DrawingId = drawingId; RevisionId = revisionId; FileHash = CadContract.Hash(fileHash);
    }
}

/// <summary>Index into the document registry, not a Raw object or a DXF handle.</summary>
public sealed class CadSourceRecordRef(int index)
{
    public int Index { get; } = CadContract.Index(index);
}
public sealed class CadSourceFactRef(int index)
{
    public int Index { get; } = CadContract.Index(index);
}

/// <summary>Inclusive pair range (zero-based) and physical line range (one-based).</summary>
public sealed class CadSourceLocator
{
    public CadSourceRecordDomain Domain { get; }
    public int RecordIndex { get; }
    public int FirstPair { get; }
    public int LastPair { get; }
    public int FirstPhysicalLine { get; }
    public int LastPhysicalLine { get; }
    public CadSourceLocator(CadSourceRecordDomain domain, int recordIndex, int firstPair, int lastPair,
        int firstPhysicalLine, int lastPhysicalLine)
    {
        Domain = CadContract.EnumValue(domain); RecordIndex = CadContract.Index(recordIndex);
        FirstPair = CadContract.Index(firstPair); LastPair = lastPair;
        FirstPhysicalLine = firstPhysicalLine; LastPhysicalLine = lastPhysicalLine;
        CadContract.Require(lastPair >= firstPair && firstPhysicalLine > 0 && lastPhysicalLine >= firstPhysicalLine,
            "Invalid source range.");
    }
}

public sealed class CadSourceRecord
{
    public CadSourceLocator Locator { get; }
    public string? OriginalHandle { get; }
    public string? CanonicalHandle { get; }
    public CadSourceRecord(CadSourceLocator locator, string? originalHandle, string? canonicalHandle)
    {
        Locator = CadContract.Required(locator); OriginalHandle = originalHandle; CanonicalHandle = canonicalHandle;
        CadContract.Require((originalHandle is null) == (canonicalHandle is null), "Both handle forms or neither required.");
        if (originalHandle is not null)
            CadContract.Require(CadContract.CanonicalHandle(originalHandle) == canonicalHandle, "Canonical handle disagrees with source.");
    }
}

public sealed class CadSourceFact
{
    public CadSourceRecordRef Record { get; }
    public string Role { get; }
    public CadFactPresence Presence { get; }
    public IReadOnlyList<int> PairIndices { get; }
    public int? FirstPhysicalLine { get; }
    public int? LastPhysicalLine { get; }
    public CadSourceFact(CadSourceRecordRef record, string role, CadFactPresence presence, IReadOnlyList<int> pairIndices,
        int? firstPhysicalLine, int? lastPhysicalLine)
    {
        Record = CadContract.Required(record); Role = CadContract.Text(role); Presence = CadContract.EnumValue(presence);
        PairIndices = CadContract.Copy(pairIndices); FirstPhysicalLine = firstPhysicalLine; LastPhysicalLine = lastPhysicalLine;
        CadContract.Require(PairIndices.All(p => p >= 0) && PairIndices.SequenceEqual(PairIndices.Distinct().Order()), "Fact pairs must be sorted unique indices.");
        CadContract.Require(presence == CadFactPresence.Absent
            ? PairIndices.Count == 0 && firstPhysicalLine is null && lastPhysicalLine is null
            : PairIndices.Count > 0 && firstPhysicalLine > 0 && lastPhysicalLine >= firstPhysicalLine,
            "Fact presence and evidence disagree.");
    }
}

/// <summary>Records in source pair/domain/index order; fact order is record then ordinal role/pairs.</summary>
public sealed class CadProvenanceRegistry
{
    public CadSourceDocument Source { get; }
    public IReadOnlyList<CadSourceRecord> Records { get; }
    public IReadOnlyList<CadSourceFact> Facts { get; }
    public CadProvenanceRegistry(CadSourceDocument source, IReadOnlyList<CadSourceRecord> records, IReadOnlyList<CadSourceFact> facts)
    {
        Source = CadContract.Required(source); Records = CadContract.Copy(records); Facts = CadContract.Copy(facts);
        var order = Records.Select(r => (r.Locator.FirstPair, r.Locator.Domain, r.Locator.RecordIndex)).ToArray();
        CadContract.Require(order.SequenceEqual(order.Order()), "Registry record source order required.");
        CadContract.Require(Records.Select(r => (r.Locator.Domain, r.Locator.RecordIndex)).Distinct().Count() == Records.Count, "Duplicate source locator.");
        string? previousRole = null; int previousRecord = -1;
        foreach (var fact in Facts)
        {
            var record = Resolve(fact.Record); var l = record.Locator;
            CadContract.Require(fact.Record.Index >= previousRecord &&
                (fact.Record.Index != previousRecord || StringComparer.Ordinal.Compare(previousRole, fact.Role) < 0), "Fact order/unique roles required.");
            previousRole = fact.Role; previousRecord = fact.Record.Index;
            CadContract.Require(fact.PairIndices.All(p => p >= l.FirstPair && p <= l.LastPair), "Fact outside record pairs.");
            if (fact.Presence == CadFactPresence.Present)
                CadContract.Require(fact.FirstPhysicalLine >= l.FirstPhysicalLine && fact.LastPhysicalLine <= l.LastPhysicalLine, "Fact outside physical range.");
        }
    }
    public CadSourceRecord Resolve(CadSourceRecordRef reference)
    { CadContract.Required(reference); CadContract.Require(reference.Index < Records.Count, "Unknown source record."); return Records[reference.Index]; }
    public CadSourceFact Resolve(CadSourceFactRef reference)
    { CadContract.Required(reference); CadContract.Require(reference.Index < Facts.Count, "Unknown source fact."); return Facts[reference.Index]; }
}
