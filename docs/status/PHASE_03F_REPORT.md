# PHASE03F — RawDrawingSnapshot Assembly

Status: **PASS_WITH_KNOWN_LIMITATIONS** — ready for owner and independent
Antigravity03F review. Local verification is not independent approval or whole
PHASE03 completion. Date2026-10-14; branch `phase/03-cad-data-extraction`,
unchanged HEAD `142100deb62e8eb3692ded84a95962548925731f`. No commit/push.

## 1. Exact scope and baseline

Only immutable in-memory RawDrawingSnapshot assembly from03E→03A evidence.
Prior independent verdicts03A/03B/03C/03D/03E PASS confirmed, including the new
03E review. Its closing Viewer wording does not authorize03H. Baseline Core/full
Release and **522/522** tests passed before changes. Controlled DXF hash unchanged,
no production SDK dependency and no existing final Snapshot model.
Prior reports/reviews/checklist/ADR015/016/tasks03+04/master/architecture/
instructions and current Raw/Dxf contracts inspected. Historical gates remain
history; current owner authorization is03F only.

## 2. Added files/contracts

- `packages/DtoB.Cad.Raw/RawDrawingSnapshot.cs`
- `packages/DtoB.Cad.Dxf/RawDrawingSnapshotAssemblyLimits.cs`
- `packages/DtoB.Cad.Dxf/TextDxfRawDrawingSnapshotAssembler.cs`
- `tests/DtoB.Tests/RawDrawingSnapshotAssemblerTests.cs`
- Two actual Text DXFs + README under `datasets/controlled/phase03f/`.
- This report and append-only03F checklist evidence.

Contracts: RawDrawingSnapshot, RawDrawingRecordView, RawSnapshotCompleteness,
stage/index diagnostic references, assembly diagnostics, operation/coverage
enums and RawDrawingSnapshotAssemblyResult. No old contract/project/ADR change.

## 3. Snapshot architecture and decisions

Assembler accepts DxfIdentityGraphReadResult and optional limits/token, no
filename/stream/Project. Snapshot references that exact03E instance and forwards
03D/03C/03B/03A, header/sections/tables/geometry/definitions/annotations/identity/
references/ownership/effective semantic counts. Only final record associations,
diagnostic locators and completeness are newly owned.

Selected reference composition over graph cloning/re-parsing; source-order
association indexes over a second semantic model; stage/index diagnostic references
over rewritten messages. These implement accepted ADR015/016 without changing
stable IDs, project persistence, units, coordinate or CAD/BIM schema. No new ADR
required. No normalized CadDocument/IDrawingReader implementation.

## 4. Predecessor ownership/reference policy

Existing immutable result and payload collections remain owned by03A–03E and
are exposed as exact references. No source read/tokenization, geometry mapping,
handle canonicalization/resolution or graph rebuild in03F. No duplicate source
record or geometry clone. Snapshot-owned lists are defensive frozen copies.
Forwarding properties and record payload references are JsonIgnore in deterministic
QA serialization: IdentityEvidence appears once as the upstream root; final views
serialize small association indices. Existing upstream serializer behavior remains
unchanged; no new persisted/export schema or deserialization promise.

## 5. RawDrawingSnapshot contract

IdentityEvidence is the source-of-truth root. Runtime forwarding properties expose
the exact document provenance, Header, Sections, Tables, Geometry, BlockAnnotations,
BlockDefinitions, IdentityNodes, References, Ownership and semantic counts. Other
inventories such as HEADER variables, attribute sequences, owner roots/cycles and
table headers remain reachable through predecessor evidence without duplicate lists.
Snapshot Records, Diagnostics, AssemblyDiagnostics and Completeness are immutable.
This artifact is raw extraction input for future PHASE04, not .dtob/CAD IR/BIM IR,
rendering data, source-ID replacement or semantic building truth.

## 6. Final record association model

Exactly one view per structural child, ordered by StructuralEvidence.Records index.
View retains original source object, current semantic disposition and optional
geometry/03D/identity references plus result-local indices and source block
definition index. Section/table/type/source span remain reachable from that record.
No type sorting or synthetic source objects.

Four bounded association arrays join prior indexes. Conflicting typed payloads,
duplicate associations, foreign source object instances, invalid locators, missing
identity inventory and inconsistent child/definition scope are refused. Graph
table headers use their distinct locator domain, not fabricated child records.
03D authoritative semantic counts are exposed unchanged; a bounded association
tally verifies agreement, not a new classification pass. Existing BLOCK definitions
and ordered child locators remain exact references and containment only.

## 7. Completeness/coverage axes

Summary independently exposes LexicalSucceeded, StructureValidated,
GeometryMappingComplete, BlockAnnotationMappingComplete, IdentityCoverageComplete,
ReferenceResolutionComplete, GraphValidated, SourceEvidenceComplete,
TypedMappingComplete, SnapshotAssemblyComplete; Coverage is Complete/Partial.

SourceEvidenceComplete requires successful lexical evidence, validated structure
and reviewed section scopes; unknown sections make it false. TypedMappingComplete
requires completed typed stage coverage, authoritative semantic child accounting
without Unsupported/Invalid/Deferred records, supported table coverage and no
unmapped HEADER variables. It is not normalization/unit validity. Controlled source
is preserved completely while typed compatibility remains **false**, including52
unsupported children and retained unmapped metadata. No single “complete” flag.

## 8. Operation status versus source coverage

Operation statuses: Succeeded/InvalidInput/ResourceLimitExceeded/Canceled.
Controlled operation Succeeded and SnapshotAssemblyComplete=true coexist with
Coverage=Partial/TypedMappingComplete=false. Graph success never promotes52
Unsupported semantic records. Explicit Unsupported geometry/annotations can also
be assembled when source stages finish safely and graph prerequisites pass;
their incomplete typed flags and raw dispositions remain visible. Tests cover
Unsupported ELLIPSE and MINSERT without fabricated geometry/cells/defaults.

## 9. Source/document provenance

Exact caller DxfDocumentProvenance object forwarded, or null when absent. No
DrawingId/RevisionId/FileHash discovery/generation/hash re-read/Project access or
machine paths. FullIdentity remains supplied document tuple + Original DXF Handle;
CanonicalHandle remains same-file metadata.000a stays original000a even when
canonicalA. Indices remain QA locators, never released stable IDs. SourceFact →
pair index → original physical lines/raw text chain remains intact.

## 10. Diagnostics and stage ordering

Global order is03A,03B,03C,03D,03E,03F; within a stage, original result order.
Views store only stage and index. ResolveDiagnostic returns the exact existing
immutable diagnostic of that stage, not a copied/localized message. Assembly
diagnostics are separately owned. No timestamp/path/exception text or message sort.
Controlled snapshot has59 original03B diagnostic references, no new assembly
diagnostic. Original03B deferred aggregate remains attributed to03B rather than
rewritten as current coverage; current coverage is the summary/03D inventory.
Supplemental fixtures verify03C Unsupported and03D foreign-field warning views.

## 11. Unsupported/unknown content retention

All52 unsupported child records remain final views with source type/range/raw
pairs/disposition and identity/reference access when present. Unsupported tables/
metadata remain source-addressable. No primitive fabrication or source disappearance.
Unknown sections retain prior evidence but current strict graph/coverage precondition
refuses completed Snapshot; failure result retains InputEvidence and reports false
SourceEvidenceComplete. No unsupported content promoted by successful assembly.

## 12. Controlled count domains

| Domain | Measured/reviewed count |
| --- | ---: |
| Source bytes / physical lines / lexical pairs |96621 /17474 /8737|
| Sections / HEADER variables / tables |6 /251 /9|
| Structural semantic children |333|
| Semantic Supported / Unsupported |281 /52|
| Deferred03C / Deferred03D / Invalid |0 /0 /0|
| Geometry /03D records / definitions |89 /93 /12|
| Identity-inspected locations (333 children +9 headers) |342|
| Identity nodes / nonidentity CLASS locations |327 /15|
| Identity-associated child views / header nodes |318 /9|
| Reference edges / resolved / null |458 /438 /20|
| Owner edges / roots / cycles |317 /10 /0|

SourceEvidenceComplete=true; typed=false; graph/identity/reference coverage=true;
assembly=true. Counts verified against independently reviewed predecessor source
evidence, not expected values generated by this Snapshot. No new03G formal campaign.

## 13. Traceability goldens

Tests traverse Snapshot→predecessor→SourceFact→lexical pairs/physical lines:
ACADVER pair3=AC1032; A-WALL name1150; LINE start.X2862=0.0/end.X900;
LWP vertex2724/2725=-0.5/-0.5; ARC2879/2880=0.0/90.0 degrees;
COLUMN_400 block name; INSERT2977=_ARCHTICK/2979=-1300.0 with rotation180;
TEXT3431=1F FLOOR PLAN; DIMENSION3930=32 with absent measurement42;
DIMSTYLE105 handle1701=2B; DICTIONARY4906=A; Owner2664→17;
Reactor5187→E resolves to source start4953. Pair indices zero-based;
code/value lines=2*pair+1/2*pair+2. Raw units4/codepage/sentinel extents/HANDSEED
remain declarations, not repaired bounds or object identity.

## 14. Immutability

Every predecessor reference and serialization remains unchanged. Final views have
get-only associations; Snapshot-owned collections cannot mutate. Caller temporary
lists are copied before publication, no lazy mutable enumerator retained. Geometry,
block children, annotation content/chunks and graph objects are not cloned. JSON
forwarders are excluded to avoid duplicating entire predecessor roots. Direct freeze
tests complement genuine full-pipeline extraction proofs.

## 15. Predecessor failure policy / strict graph gate

Lexical must succeed, structure/version must be validated/supported, stage
operation statuses must be Succeeded/Partial, effective inventory available.
Current V1 requires **GraphValidated + IdentityCoverageComplete +
ReferenceResolutionComplete**, all true. Warning-only unresolved non-owner graph
coverage is therefore refused, not silently accepted as complete.

Canceled/resource/invalid predecessors, invalid typed geometry/03D records and
unfinished Deferred mappings are refused. Incomplete mapping flags without
explicit Unsupported coverage are refused. Completed intentional Unsupported
profiles are allowed, with their stage/typed flags still false. This distinguishes
raw unsupported coverage from parser/mapping failure. Missing block terminators
and conflicting associations refuse publication. Failures return Snapshot=null and
SnapshotAssemblyComplete=false; prior evidence axes retain their actual state.
No weaker graph policy or graph repair introduced.

## 16. Resource limits

Three finite injectable03F dimensions: source locations4096 (children + table headers, approved03E index domain), child associations4096 (approved upstream
record cap; also bounds four small index arrays) and diagnostic references30000
(six existing stage budgets of5000). No new parsing/edge/geometry budget or recursive
traversal. Bound before association allocation/diagnostic append; original upstream
safety caps remain active. Exact342/333/59 acceptance and first-over refusal tested.
Assembly error evidence is at most one refusal/resource message, or cancellation
message; no unbounded diagnostic generation or truncated-success.

## 17. Cancellation

Checks at input eligibility, stage coverage, index/source association loops,
identity/block inventory, diagnostic references, summary and freeze/materialization.
Pre-cancel returns Canceled with no Snapshot. Cancellation during frozen-list
enumeration throws before publication; assembler catches cancellation and resets
SnapshotAssemblyComplete=false. Prior graph flags may remain true because they
describe already completed immutable work, not successful03F publication.
No timed assertion, background/session coordinator, stream disposal or UI work.

## 18. Tests/results and fixtures

Final **558/558 PASS**, no failures/skips. New03F **36**; unchanged03E88,03D78,
03C63,03B103,03A55, architecture/dependency9 and PHASE00/01/02 regression126.
Controlled real full-pipeline Snapshot verifies all count/reference/traceability
domains. Tests also cover supported-only coverage, absence/provenance, diagnostics,
no payload clones, invalid/conflicting association inputs, failed stage matrix,
Unsupported preservation, limits, cancellation, immutable publication and determinism.
Owned-input negative tests exercise assembly integrity, not fake source extraction.

Two03F actual source fixtures prove native circle/blocks/INSERT preservation with
valid graph prerequisites. Initial targeted32 run was31/32 because I reused a
narrow-stage fixture without valid03E ownership/handles. Dedicated03F fixtures
correct that test input; old fixtures/checks were not weakened or modified. Circle
adds explicit Owner0; INSERT handlesC1/C2 are valid unique hex. No geometry change.
Initial compile nullable-flow warning fixed with DoesNotReturn refusal annotation.
Initial/intermediate554/556 evidence is historical; final558 includes added invalid
geometry/annotation predecessor cases. Final suite covers all36 new cases.

## 19. Determinism

Three fresh FileAccess.Read/FileShare.Read streams→03A→03F with identical supplied
provenance serialize byte-identically. Streams are disposed before assembly,
demonstrating no source reopen requirement. Snapshot deterministic SHA256:
`4B91C83791F89DEC9BDC633C832001F2D1E537E7F0330BEE0666067CAE9A310C`.
No timestamps/paths/durations/random IDs/dictionary instability/exception strings.
This is an assembly regression check, not PHASE03G full validation campaign.

## 20. Build, dependency and diff verification

Core Release **PASS**, full DtoB.sln Release/Rebuild **PASS**, existing exact known
Revit MSB3277 warning-identity gate **PASS**. Existing VS2022 MSBuild/Roslyn +
SDK8.0.416 files/VSTest .NET8.0.22 fallback used; dotnet.exe unavailable as before,
no toolchain/security change. No UI/IPC/Revit integration changed; no new host claim.
Raw remains pure .NET8; Dxf references Raw only. No package/project/dependency
changes or weakened tests; no vendor/Core geometry/Project/WPF/Revit coupling.

All315 start-snapshot files unchanged before checklist append; final permits only
that append. Own code reviewed for exact instance references, record domains,
association uniqueness, Unsupported-vs-failure policy, summary axes, diagnostics,
bounded freeze/cancellation and no normalization. `git diff --check`, new-file
strict UTF-8/whitespace/source hash/protected-source audit PASS. Historical dirty
tree preserved; prior reports/reviews/ADRs/tests untouched.

Evidence: `artifacts/phase03/03f-snapshot-20261014/` baseline/logs/TRX, before.json,
checklist-before.md, initial/targeted build and32-case TRX/log, controlled-snapshot.json,
determinism.sha256, final/core-build.log/build.log/tests.log/tests.trx,
final-gate.log, diff-check.log and final-audit.json. Final authoritative test count558.

## 21. Original controlled source hash

Before/after96,621-byte original DXF SHA256 unchanged:
`2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC`.
Production03F never hashes/opens/reads source bytes or project state. Test QA hashing
is separate from the caller-supplied provenance forwarded unchanged.

## 22. Known limitations / readiness

Native AC1032 raw profiles only. Source-preserved does not mean typed-compatible,
normalization-ready or BIM-valid. All52 existing unsupported records remain visible.
Strict complete graph requirement refuses unresolved warning-only pointers. Invalid/
unfinished typed mappings refuse assembly; supported Unsupported profiles retain
partial stage/typed coverage. Unknown sections require future reviewed coverage.
Snapshot is in-memory with deterministic QA serialization, not persisted truth or
a versioned roundtrip schema. Caller IDs/hash remain supplied authority; no lease,
stale-result, GUI/deployment integration or performance claim. Full-stage diagnostic
messages/counts retain their historical attribution.

Requested03F local checks satisfied with evidence; ready for independent Antigravity
review. Full PHASE03 is not complete.

## 23. Explicit scope stop / next step

No03G formal extraction QA campaign,03H Viewer/WPF/Canvas/Viewport/zoom/pan/Fit/
font/text/block/graph rendering, or PHASE04. No mm/radian/OCS-WCS/Arbitrary Axis
Algorithm/base compensation/INSERT/nested/MINSERT expansion/effective styles/
topology/cleanup/snapping/deduplication/CAD IR/recognition. No project persistence,
DrawingRegistration/Revision/ManagedFilePath/Save/Open changes. No SDK/BIM/Revit
generation, IDrawingReader normalization, commit or push. Stop for owner and
independent Antigravity03F review only.
