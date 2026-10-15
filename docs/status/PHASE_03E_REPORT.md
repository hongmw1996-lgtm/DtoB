# PHASE03E — Native Text DXF Handle / Ownership / Source Provenance Validation

Status: **PASS_WITH_KNOWN_LIMITATIONS** — ready for owner and independent
Antigravity03E review. This is local implementation verification, not an
independent review verdict or whole-PHASE03 completion.
Date:2026-10-14. Branch `phase/03-cad-data-extraction`, unchanged HEAD
`142100deb62e8eb3692ded84a95962548925731f`. No commit/push.

## 1. Exact scope and baseline

Only03E source identity, canonical lookup/duplicate checks, contextual reference
resolution, Owner graph/cycle validation and neutral supplied provenance.
Consume immutable03D→03C→03B→03A evidence. No geometry reinterpretation.
Read prior reviews/reports, checklist, ADR015/016, tasks03/04, architecture,
instructions/master and current raw/reference contracts. Independent verdicts
PHASE_03A_PASS/03B_PASS/03C_PASS/03D_PASS confirmed. Pre-change Core/full Release
and434/434 tests passed. Original controlled source hash verified unchanged.
No previous03E implementation or production CAD SDK dependency.

## 2. Added files and contracts

- `packages/DtoB.Cad.Raw/DxfIdentityGraphContracts.cs`
- `packages/DtoB.Cad.Dxf/DxfIdentityGraphLimits.cs`
- `packages/DtoB.Cad.Dxf/TextDxfIdentityGraphReader.cs`
- `packages/DtoB.Cad.Dxf/TextDxfIdentityGraphReader.Owners.cs`
- `tests/DtoB.Tests/TextDxfIdentityGraphReaderTests.cs`
- Eighteen actual DXFs + README under `datasets/controlled/phase03e/`.
- This report; append-only03E execution evidence in PHASE_03_CHECKLIST.md.

Contracts: DxfGraphRecordLocator, DxfDocumentProvenance, DxfFullSourceIdentity,
DxfIdentityNode, DxfReferenceEdge/OwnershipEdge, DxfHandleConflict/OwnerCycle,
graph status/validity/resolution/pointer enums, diagnostics and separate
DxfIdentityGraphReadResult. No previous contracts/project references changed.

## 3. Graph architecture and decisions

Separate additive result references unchanged03D evidence and owns only new
immutable validation data. Result-local locators distinguish structural Records
indices from Tables header indices; neither is a released source ID. Source-order
merge includes table headers exactly once:333 child records +9 table headers
=342 source locations.15 CLASS declarations are nonidentity,327 object nodes.
Nodes retain original source objects/ranges and section/table/block containment.

Simplest alternatives: string canonicalization instead of width-limited integers;
neutral caller context instead of Project dependency; separate validation axes
instead of semantic promotion; iterative DFS instead of recursive owner traversal.
Existing ADR015/016 authorize this source boundary and identity tuple. No source-ID
policy, persisted schema, CAD/BIM IR, units or coordinate convention changed;
no new ADR required. Accepted ADRs preserved.

## 4. Original versus canonical Handle

OriginalHandle stores exact source spelling with source indices/span. Canonical
rule: nonempty ASCII hexadecimal digits only; uppercase A–F and strip leading
zeros; all-zero spelling becomes key0 for **references only**. Object identity0
is malformed and receives no canonical key. Whitespace/sign/0x/non-hex/empty
values are not trimmed or repaired. String processing avoids integer overflow;
no arbitrary UInt64 width limit added. A/a/000A all canonicalize toA while their
original spelling remains distinct. Every duplicate singleton source field stays
in OriginalHandle.SourcePairIndices; multiple fields have no selected value/key.

The selected profile preserves imported DXF identities, not upstream DWG export
equivalence. Code105 is DIMSTYLE identity; DIMSTYLE5 remains nonidentity setting.
HEADER HANDSEED5 and CLASS/unknown-scope/arbitrary foreign-subclass5 are not
automatically object identities. Source-role rules follow the accepted native
profile and [Autodesk numerical codes](https://help.autodesk.com/cloudhelp/2024/ENU/AutoCAD-DXF/files/GUID-3F0380A5-1C15-464D-BC66-2C5F094BCFB9.htm).

## 5. Node eligibility and missing-Handle profiles

Eligible source scopes: TABLES entries/headers, BLOCKS, ENTITIES, OBJECTS.
Normal5 is recognized in the common record prefix outside controls/XDATA/subclasses;
DIMSTYLE105 also accepts its reviewed AcDbDimStyleTableRecord context. Unsupported
object types with such identity fields are included.

Required-Handle evidence: Owner-role fact in an eligible object scope; eligible
AcDbEntity/AcDbSymbolTable/AcDbSymbolTableRecord marker; or reviewed OBJECTS type
DICTIONARY/LAYOUT/MATERIAL/VISUALSTYLE. Missing required fields yield an explicit
MissingRequired node/diagnostic, no invented handle. Present malformed, multiple
identity fields and ambiguous duplicates are separate validity states. Records
without applicable identity evidence/profile remain in NonIdentityRecords, not
globally diagnosed missing. Unknown sections explicitly make identity coverage
incomplete. Owner-role references from a nonidentity source retain their target
evidence but emit OwnershipSourceUnavailable; no silently validated owner graph.

## 6. Duplicate validation

Build same-file canonical index of all source nodes, including semantically
unsupported objects. Conflicts retain every source locator/span/raw spelling,
mark every member Ambiguous and emit one located diagnostic per member.
No first/last choice, merge, handle reassignment or deletion. Source-ordered
conflict groups/members are bounded. Ambiguous endpoints cannot be uniquely
identified or used as resolved ownership targets. Same-section/cross-section,
case and leading-zero fixtures all fail GraphValidated as required.

## 7. Reference roles and pointer kinds

Every03B contextual reference retained with source locator/raw source handle,
group code, original target spelling/canonical key, exact pair/line span and
the **same** contextual DxfRawReference evidence. Owner/Reactor/
ExtensionDictionary/XData/Other remain distinct. No first330 rule. Only Owner
role contributes ownership. Numeric class is separate:330–339 soft pointer,
340–349/390–399/480/481 hard pointer,350–359 soft owner,360–369 hard owner,
1005 XDATA handle. A hard-owner group in an extension dictionary remains an
ExtensionDictionary contextual role. No full AutoCAD object-model reconstruction.

Common-field/owner exclusions corroborated by
[Autodesk entity codes](https://help.autodesk.com/cloudhelp/2024/ENU/AutoCAD-DXF/files/GUID-3610039E-27D1-4E23-B6D3-7E60B22BB5BD.htm)
and [object codes](https://help.autodesk.com/cloudhelp/2024/ENU/AutoCAD-DXF/files/GUID-6939D69E-04CB-4F4C-87B2-67BC540FCF58.htm).
Fixtures, not documentation alone, prove the implemented profile.

## 8. Reference resolution and completeness

States: Resolved/Unresolved/Ambiguous/Malformed/Null/NotApplicable. Nonzero valid
canonical target resolves only to one current-document node. Duplicate target
is Ambiguous, with null TargetLocator; malformed/empty is Malformed.0/0000 is
Null, not false unresolved. Empty is not silently treated as null. No filename,
block/name, record-order, external-Xref or DWG resolution.

Unresolved Owner is Error; unresolved Reactor/ExtensionDictionary/XData/Other
are distinct located Warning diagnostics. Malformed/ambiguous references carry
role and analogous severity. Warning-only non-owner gaps produce Partial and
ReferenceResolutionComplete=false while the otherwise sound ownership/identity
graph may have GraphValidated=true. Status therefore never claims clean complete
resolution for those cases. Duplicate/malformed/missing identities and ownership
errors invalidate GraphValidated. IdentityCoverageComplete requires all applicable
nodes valid/unique and no unreviewed section. ReferenceResolutionComplete accepts
only resolved/null/not-applicable edges. Flags are false on rejection/resource/cancel.

## 9. Ownership graph and roots

Resolved Owner-role edges with uniquely valid source and target nodes form
child→owner relations. Both source reference and exact target locator retained.
Multiple Owner facts produce an error; all factual resolved edges remain for
audit, no selected parent. AcDbEntity profile requires an explicit Owner fact;
missing required Owner is an error. Non-entity reviewed root/no-owner profile is
allowed. Roots require valid identity and no required-owner failure, with either
no applicable Owner fact or one explicit null Owner. Unresolved/ambiguous or
competing parents do not masquerade as roots. Reactors/pointers never create owners.

## 10. Cycle/self-owner validation

Bounded iterative color/stack DFS on Owner adjacency only. No recursive stack,
repair or deletion. Retain exact cycle witness locators and located diagnostics
including original handles. Self-owner yields one-node witness; A→B→A yields
two-node witness and both original edges. Arbitrary pointer cycles are irrelevant
to Owner graph. Cycle witnesses follow deterministic DFS walk order; diagnostics
are emitted in deterministic validation-stage order, with source-ordered input
and conflict members. For competing-owner graphs these are failure witnesses,
not a promise to enumerate every combinatorial cycle.

## 11. Unsupported object identity policy

All48 actual OBJECTS (including DICTIONARY/LAYOUT/MATERIAL/VISUALSTYLE) participate
in source identity/reference validation despite remaining semantically Unsupported.
Other unsupported table entries are included when handle-bearing. Graph validation
never changes the existing333-record semantic inventory **Supported281 /
Unsupported52**, nor geometry/entity types. SourceRecord on nodes remains original
03B evidence; current semantic coverage is the unchanged03D overlay. INSERT code2
names are not Handle edges and no nested geometry is resolved/expanded.

## 12. Source-object provenance

Each node owns locator, type/source record, section, optional table scope and03D
definition locator, exact OriginalHandle fact and canonical lookup value,
identity validity and optional FullIdentity. Pair indices zero-based, physical
lines one-based; complete original source record span retained. Definition scope
is source containment, not occurrence ancestry. Every reference has its own
exact source span and original target/context. No machine-path provenance.

## 13. Document provenance dependency boundary

No compatible neutral identity contract existed in the pure Raw boundary;
existing CAD/Project identity models would introduce forbidden dependencies.
Small immutable DxfDocumentProvenance accepts opaque nonempty DrawingId/RevisionId
and caller-supplied64-hex SHA256 FileHash. It validates supplied syntax, preserves
values and knows nothing about acquisition/verification. No .dtob open, project
state/RevisionId discovery, ManagedFilePath/SourceFilePath access, hash re-read,
lease/epoch/stale-result coordination. Disposed-input-stream test proves the
validator can operate solely on supplied immutable evidence.

## 14. Full source identity tuple

Optional FullIdentity composes supplied document context + **Original DXF Handle**,
with explicit Valid/MissingRequired/Malformed/MultipleFields/Ambiguous validity.
Canonical key is separate; never replaces original spelling or accepts duplicate
identity. No context means FullIdentity=null, not guessed IDs/hash. FileHash authority
is the caller's responsibility; validator does not claim to authenticate it.
No new stable ID synthesis or DWG↔DXF reconciliation.

## 15. Controlled Handle/reference inventory

Independent source scan and final result agree:

| Identity scope | Nodes |
| --- | ---: |
| TABLES (88 entries +9 headers) |97|
| BLOCKS |68|
| ENTITIES |114|
| OBJECTS |48|
| Total identity-bearing records |327|

Valid327; malformed0; missing-required0; duplicate canonical groups0.15 CLASS
records are nonidentity; HEADER/delimiters/HANDSEED excluded.

| Context role | Occurrences | Resolved | Unresolved | Null | Ambiguous | Malformed |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Owner |327|317|0|10|0|0|
| Reactor |30|30|0|0|0|0|
| ExtensionDictionary |0|0|0|0|0|0|
| XData |0|0|0|0|0|0|
| Other |101|91|0|10|0|0|
| Total |458|438|0|20|0|0|

Owner edges317, roots10, self-owner0, cycle witnesses0. No new03E diagnostics.
Graph status Succeeded; GraphValidated/IdentityCoverageComplete/
ReferenceResolutionComplete=true.03D semantic status remains **Partial**.
458 equals03B historical record/table-header reference inventory because identical
scope is independently remeasured; HEADER metadata is excluded from object edges.

## 16. Independent QA

Independent BCL physical-line/code0 scanner creates handle/reference oracle,
including TABLE headers, sections/table names/control/XDATA/subclass roles.
No production grouping/graph helpers or vendor output supplies expected counts.
All327 source node ranges/handle spellings/canonical keys/pair-lines/scope are
compared. All458 reference codes/targets/roles/resolution target spans compared;
independent Owner chains verify roots and absence of cycles. PowerShell source
scan also preserved in ignored `independent-source.json` /
`independent-references.json`; production output separately `controlled-graph.json`.

## 17. Hand-reviewed source goldens

| Type | Start pair | Original handle | Handle pair/code | Code line |
| --- | ---: | --- | --- | ---: |
| TABLE/VPORT |682|8|684/5|1369|
| DIMSTYLE/Standard |1700|2B|1701/105|3403|
| BLOCK/*Model_Space |2662|18|2663/5|5327|
| LINE in BLOCKS |2856|9C|2857/5|5715|
| OBJECTS DICTIONARY |4905|A|4906/5|9813|
| OBJECTS VISUALSTYLE |5454|5A|5455/5|10911|

Source owner facts2664→17,2858→99 retained. Reactor pair5187→E resolves to
target source start4953. VISUALSTYLE5457→16 is Reactor,5459→16 is Owner, distinct
roles despite identical targets. DIMSTYLE5 nonidentity setting and HEADER
HANDSEED pair277 excluded. Controlled file contains no XDictionary references:
fixture proves330/360 XDictionary role, exact spans and target resolution instead
of claiming nonexistent controlled evidence.

## 18. Supplemental fixtures

Eighteen hand-authored actual Text DXFs documented in phase03e/README: same/cross/
case/zero duplicates; malformed/missing handles; root; unresolved owner/pointer;
ambiguous target; self/two-node Owner cycle; pointer cycle; reactor/XDictionary/
XDATA roles; null; unsupported target; DIMSTYLE context; competing owners.
Inline actual Text DXFs add long hex, empty/whitespace malformed values, identity
control exclusions, optional/missing-owner profile, unknown scopes, nonidentity
Owner source, supplied provenance, unsupported versions, resource/cancel cases.
All extraction proofs use03A→03B→03C→03D→03E; direct DTO creation only tests freeze.

## 19. Finite resource limits

| Guard | Default | Controlled |
| --- | ---: | ---: |
| Input records including headers |4096|342|
| Identity nodes |4096|327|
| Contextual edges |16384|458|
| Duplicate groups |2048 (half node cap)|0|
| Total conflict members |4096|0|
| Ownership traversal work |32768 (twice reference cap)|bounded, verified|
| Stored diagnostics |5000 incl. terminal|0|
| Source pairs |250000|8737|

Existing lexical line/input and structural field/context limits remain in force.
Growing records/nodes/edges/conflict lists guarded before append; derived
ownership/cycle evidence bounded by collected edges/nodes and metered DFS work.
Traversal budget includes reference inspection, owner assembly, adjacency, stack
visits and cycle-member evidence, not just depth. Diagnostic cap reserves one
terminal slot; exhaustion returns ResourceLimitExceeded, never silent eviction.
All finite configurations validated; injected exact/first-over budgets cover
dimensions and traversal. No memory/throughput guarantee or unbounded recursion.

## 20. Cancellation/materialization

Checks during record/node collection, source role/hex character scanning,
duplicates, edge collection/resolution, owner construction/DFS, provenance,
conflict evidence and result freeze. Cancellation and resource/refused input
return all validation/completeness flags false. Partial retained evidence cannot
advertise a clean graph. Frozen owned copies contain no lazy source callbacks,
vendor objects or stream. Pre-cancel and deterministic cancellation during freeze
tested; no timing-dependent cancellation/performance claim.

## 21. Tests and results

Final **522/522 PASS**, no failures/skips:

| Suite | Cases |
| --- | ---: |
| New03E |88|
| Unchanged03D |78|
| Unchanged03C |63|
| Unchanged03B |103|
| Unchanged03A |55|
| Existing architecture/dependency additions |9|
| PHASE00/01/02 regressions |126|

Baseline434/434 before changes. Initial dedicated03E75/75 passed; expanded safety/
coverage tests raised final03E to88 (intermediate520/520 is not final). No old
test changed or assertion weakened. Final full suite includes every new03E case.
Failures expected in malformed/resource/cycle fixtures were validated as explicit
results, not test failures. Tests verify exact source roles/identity/provenance,
cross-section resolution, semantic invariance, limits, cancellation/immutability
and independent deterministic pipelines.

## 22. Determinism

Three fresh read-only streams and complete03A→03E pipelines with fixed supplied
document context serialize identically. SHA256:
`A27ACDE11709AEB611B381377F9BD57295364BA410B2774E7E6FE685003C34A7`.
Node/edge order = physical source order; duplicate groups = first member order,
members source order. Diagnostics = deterministic stage order; cycle witnesses
= deterministic Owner DFS order. No timestamps/paths/durations/exception text.
03D and predecessor serialization remains identical before/after validation.

## 23. Build/dependency/diff verification

Core Release **PASS**, complete DtoB.sln Release/Rebuild **PASS** including Revit.
Existing VS2022 MSBuild/Roslyn + SDK8.0.416 files/VSTest .NET8.0.22 fallback used;
dotnet.exe unavailable as before, no toolchain/security change. Exact existing
Revit MSB3277 identity gate passes; no new warning accepted. No host/UI verification
needed or claimed: IPC/Revit/desktop integration untouched.

Raw remains pure runtime .NET8; Dxf references Raw only; no dependency/package/
project-file changes. Existing forbidden-dependency/architecture tests unchanged
and passing. No CAD SDK, WPF/Revit/Core geometry/Project coupling.
All289 existing file hashes unchanged before checklist append; final permits
only that append. Own new source reviewed for identity roles, duplicate/no-winner,
reference severity, root correctness, cycle-only Owner traversal, budgets and
unknown coverage. Added unknown-section/nonidentity-source tests during self-review.
`git diff --check` and new-file strict UTF-8/whitespace checks pass. Existing
unrelated dirty-tree changes preserved, historical reports/reviews not rewritten.

Evidence under `artifacts/phase03/03e-identity-20261014/`: before.json,
checklist-before.md, baseline gate/logs/TRX, independent source/reference audits,
targeted75-test build/log/TRX, controlled-graph.json, determinism.sha256,
final/core-build.log/build.log/tests.log/tests.trx, final-gate.log,
diff-check.log and final-audit.json. Final522 evidence supersedes intermediate520.

## 24. Original source hash

Before/after unchanged controlled96,621-byte DXF SHA256:
`2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC`.
Only QA/tests hash the fixture. Production03E neither reopens a file nor computes
FileHash. Caller-supplied hash preserved. No project/managed/source bytes changed.

## 25. Known limitations and review readiness

Native AC1032 Text DXF source profiles only; no broad DWG/binary/older version
support. Normal code5 identity maps common prefix, not arbitrary subclass text;
additional identity contexts need reviewed fixtures. Missing-handle requirements
are explicit, not universal across every unknown custom record. Unknown sections
make identity coverage incomplete. Contextual reference coverage follows03B ranges;
no arbitrary hex-looking string interpretation or complete AutoCAD object model.
Non-owner gaps are warnings with incomplete resolution. All52 semantic unsupported
records remain unsupported. Root/ownership/cycle witnesses describe source graph,
not model/paper-space semantics, rendering or occurrence assembly. Caller IDs/hash
are trusted supplied authority, not discovered/authenticated here. No performance,
deployment, project integration or final-snapshot claim.

Requested03E local checks satisfied with evidence; ready for independent
Antigravity03E review. Whole PHASE03 remains incomplete.

## 26. Explicit stop and next step

No03F RawDrawingSnapshot or final all-phase root assembly. No Viewer/graph UI,
WPF, PHASE04, mm/radian/OCS-WCS/Arbitrary Axis Algorithm, block base compensation,
INSERT/nested/MINSERT expansion, effective styles, CAD IR, cleanup/snapping/
deduplication. No Project persistence/read/session change, semantic recognition,
BIM/Revit generation, SDK, DWG↔DXF reconciliation, commit or push.
Stop for owner and independent Antigravity03E review only.
