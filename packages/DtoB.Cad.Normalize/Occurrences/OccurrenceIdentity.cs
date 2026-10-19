using DtoB.Cad.V2;

namespace DtoB.Cad.Normalize.Occurrences;

/// <summary>Frozen ADR-020 occurrence tuples built exclusively with the existing cad2 framing primitive.</summary>
public static class CadOccurrenceIdentity
{
    private static CadIdentityField Token(CadSourceRecord record) => record.CanonicalHandle is {} handle
        ? CadIdentityField.Handle(handle)
        : CadIdentityField.Tuple([CadIdentityField.Text("record"),CadIdentityField.Text(record.Locator.Domain.ToString()),CadIdentityField.Index(record.Locator.RecordIndex)]);
    private static CadIdentityField[] Authority(CadSourceDocument doc) =>
        [CadIdentityField.GuidValue(doc.DrawingId),CadIdentityField.GuidValue(doc.RevisionId),CadIdentityField.FileHash(doc.FileHash)];
    public static CadId Source(CadProvenanceRegistry registry, CadSourceRecordRef record, CadIdentityDomain domain)
    {
        CadContract.Require(domain is CadIdentityDomain.SourceInsert or CadIdentityDomain.Definition,"Source or definition domain required.");
        return CadId.Create(domain,Authority(registry.Source).Append(Token(registry.Resolve(record))).ToArray());
    }
    public static CadId Occurrence(CadProvenanceRegistry registry, CadOccurrencePath path)
    {
        CadContract.Require(path.Steps.Count > 0,"Nonempty occurrence lineage required.");
        var steps = path.Steps.Select(step => CadIdentityField.Tuple([
            CadIdentityField.Text(step.Row is null ? "insert" : "cell"),Token(registry.Resolve(step.InsertSource)),Token(registry.Resolve(step.DefinitionSource)),
            step.Row is {} row ? CadIdentityField.Index(row) : CadIdentityField.Null(),
            step.Column is {} column ? CadIdentityField.Index(column) : CadIdentityField.Null()])).ToArray();
        return CadId.Create(CadIdentityDomain.Occurrence,Authority(registry.Source)
            .Append(Token(registry.Resolve(path.Steps[^1].InsertSource))).Append(CadIdentityField.Tuple(steps)).ToArray());
    }
}
