using DtoB.Cad.V2;
using V2Document = DtoB.Cad.V2.CadDocument;

namespace DtoB.Tests;

/// <summary>Hand-authored contract data, not output from a normalization engine or geometry conversion.</summary>
internal static class CadV2ContractFixture
{
    internal static readonly Guid Drawing = Guid.Parse("11111111-2222-3333-4444-555555555555");
    internal static readonly Guid Revision = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    internal const string Hash = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
    internal static CadId Id(string name, CadIdentityDomain domain = CadIdentityDomain.Source) => CadId.Create(domain, [CadIdentityField.Text(name)]);
    internal static CadSourceRecordRef Record(int index) => new(index);
    internal static CadSourceFactRef Fact(int index) => new(index);
    internal static CadProvenanceRegistry Registry() => new(new(Drawing, Revision, Hash),
        [new(new(CadSourceRecordDomain.StructuralRecord, 0, 0, 4, 1, 10), "000a", "A"),
         new(new(CadSourceRecordDomain.StructuralRecord, 1, 5, 7, 11, 16), "b", "B")],
        [new(Record(0), "Start.X", CadFactPresence.Present, [1], 3, 4),
         new(Record(0), "Start.Z", CadFactPresence.Absent, [], null, null)]);
    internal static CadDiagnostic Diagnostic(string code = "CONTRACT_UNSUPPORTED") => new(code, CadSeverity.Warning,
        CadStage.Contract, Record(1), null, null, ["synthetic contract fixture"]);
    internal static V2Document Document(IReadOnlyList<CadGeometry>? geometry = null,
        IReadOnlyList<CadEntity>? entities = null, IReadOnlyList<CadDiagnostic>? diagnostics = null)
    {
        geometry ??= [new CadLine(Id("line-geometry", CadIdentityDomain.Leaf), Record(0), [Fact(0)], new(1.25, -0d, 0), new(4, 5, 0))];
        entities ??= [new(Id("line"), Record(0), CadEntityKind.Primitive, CadNormalizationState.Normalized, geometry[0].Id, null, null, [Fact(0)]),
            new(Id("unsupported"), Record(1), CadEntityKind.Unsupported, CadNormalizationState.UnsupportedNormalization, null, null, null, [])];
        return new(2, "04a-contract-fixture", "contract-only", CadConventions.Canonical,
            new(CadUnitDecisionKind.Unresolved, null, null, null, null, null), CadTransform.Identity, Registry(), [], [], entities,
            [], [new(entities[0].Id, null)], geometry, [],
            [new(Record(0), CadNormalizationState.Normalized, [entities[0].Id, geometry[0].Id]),
             new(Record(1), CadNormalizationState.UnsupportedNormalization, [entities[1].Id])],
            diagnostics ?? [Diagnostic()], [], new(CadNormalizationState.PartiallyNormalized, 2, 2));
    }
    internal static string Root()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DtoB.sln"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidDataException("Repository root unavailable.");
    }
}
