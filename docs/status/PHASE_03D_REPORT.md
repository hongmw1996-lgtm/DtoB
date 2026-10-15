# PHASE 03D — Native Text DXF Block / INSERT / Annotation Raw Extraction

Status: **PASS_WITH_KNOWN_LIMITATIONS**. Implementation and local verification
are ready for owner and independent Antigravity review. This is not an
independent review verdict, whole PHASE03 completion, or authorization for03E.
Date: 2026-10-14. Branch: `phase/03-cad-data-extraction`;
unchanged HEAD `142100deb62e8eb3692ded84a95962548925731f`.
No commit or push performed.

## 1. Exact scope and entry evidence

Authorized03D only: consume DeferredTo03D BLOCK/ENDBLK/INSERT/TEXT/MTEXT/
DIMENSION and separately fixture-gated ATTRIB/ATTDEF/attribute SEQEND. Preserve
native source facts and definition membership. Predecessor review verdicts
PHASE_03A_PASS, PHASE_03B_PASS and PHASE_03C_PASS read before implementation.
Baseline Core/full Release gate and **356/356** tests passed before changes.
Independent source scan found93 deferred records; original hash verified.
No earlier03D semantic mapper or CAD SDK dependency was present.

## 2. Contracts and changed files

Added, without changing project references or dependencies:

- `packages/DtoB.Cad.Raw/DxfRawBlockAnnotationContracts.cs`
- `packages/DtoB.Cad.Dxf/DxfBlockAnnotationLimits.cs`
- `packages/DtoB.Cad.Dxf/TextDxfBlockAnnotationReader.cs`
- `packages/DtoB.Cad.Dxf/TextDxfBlockAnnotationReader.Sequences.cs`
- `tests/DtoB.Tests/TextDxfBlockAnnotationReaderTests.cs`
- Twelve DXFs and README under `datasets/controlled/phase03d/`.
- This report; append-only03D evidence in `PHASE_03_CHECKLIST.md`.

Contracts: RawBlockBegin/End, RawInsert, RawText, RawMText/RawTextChunk,
RawDimension, RawAttrib/AttDef, RawSequenceEnd, RawBlockAnnotationRecord,
RawBlockDefinition/Child, RawAttributeSequence, diagnostics/status and separate
DxfBlockAnnotationReadResult. All collection publication is a defensive
read-only copy. Existing DxfSourceFact/RawCoordinateFacts reused.

## 3. Additive result architecture and decisions

Result references unchanged03C geometry, which references03B structural and03A
lexical evidence. Geometry/structural/lexical objects are not mutated. Existing
geometry children are referenced by result-local index; no duplicate payloads.
New record dispositions overlay03C dispositions only for DeferredTo03D.
Original333-record inventory remains the accounting basis.

Simplest choices: separate result rather than replacing prior models; ordered
containment locators rather than global resolution; raw chunks rather than a
decoder; explicit unsupported subtype coverage rather than guessing fields.
These follow accepted ADR-015/ADR-016. No major schema/unit/coordinate/stable-ID
decision changed and no new ADR was required. No final RawDrawingSnapshot.

## 4. BLOCK begin mapping

Map source name2, alternate3, base10/20/30, flags70, layer8, xref1 and
description4 in eligible contexts. Handles/owner references remain in original
lexical/structural evidence, unchanged. Missing baseZ stays absent; explicit
empty xref remains Present/Valid/empty. BLOCK_RECORD table entries remain
distinct from BLOCK begin records and RawBlockDefinition.

## 5. BLOCK / ENDBLK boundaries

Per actual BLOCKS section, track BLOCK → ordered children → ENDBLK. Complete
range is inclusive from BLOCK code0 to final ENDBLK source pair. Nested BLOCK,
missing ENDBLK, unexpected ENDBLK and children outside a definition produce
located deterministic errors and incomplete/InvalidInput result. Invalid
delimiter facts do not produce a Supported definition. Incomplete definitions
retain their observed range and null end locator, never an invented terminator.
Following records remain preserved.

## 6. Child membership

Measured12 definitions,44 children, maximum9 children per definition. Existing
geometry remains89 records, including32 within BLOCKS. Child locators reference
source structural records and, when present, existing geometry/new annotation
results. LINE/LWPOLYLINE/ARC/POINT/SOLID/INSERT/MTEXT and unknown source records
retain original order and scope. Unsupported child facts remain visible through
their original disposition. Definition coverage validates delimiters and
containment, not successful interpretation of every possible child entity.

## 7. INSERT raw mapping

Target2; insertion10/20/30; scale41/42/43; rotation50 in source degrees;
columns70/rows71/spacings44/45; attributes-follow66; normal210/220/230 and layer.
Every field has presence/validity/source indices/span; all original spellings
remain accessible. Missing scale/rotation/count/spacing/Z/normal fields are
absent, not1/0/identity defaults. Target names are not asserted to resolve.

## 8. Nested INSERT policy

Eight actual INSERTs inside BLOCKS are raw children with target and placement
facts, not occurrence expansion. Supplemental NONZERO base (12.5,-30,7) and
INSERT (100,200,300), scale (2,3,4), rotation45 degrees, normal (0,1,0) remain
unchanged native values. No recursive traversal, block-base compensation,
matrix, transformed child or world-position calculation.

## 9. MINSERT / array policy

Explicit rows/columns >1 or eligible AcDbMInsertBlock subclass yields
UnsupportedOccurrenceExpansion and Unsupported disposition. Preserve source
INSERT and all array facts; no cells, synthetic IDs or geometry clones. Tests
verify columns2/rows3/spacings10/20 and safe continuation to unrelated TEXT.

## 10. TEXT

Map verbatim1, insertion10/20/30, optional alignment11/21/31, height40,
rotation50, style7, horizontal72/vertical73, thickness39 and normal210/220/230
in the supported AcDbText profile. Unicode, escapes, formatting, empty strings
and exact source spans remain raw. No font loading, SHX, bounds, rendering,
label classification or numeric/text interpretation.

## 11. MTEXT

Keep each code3 continuation and final code1 separately in physical source
order with source fact/span. No concatenated or decoded content replaces source.
Map insertion10, direction11, nominal height40, rectangle width41, attachment71,
drawing direction72, spacing73/44, style7 and normal210. Duplicate final chunks,
continuation after final and missing final yield Invalid facts/record with
located diagnostics; all chunks retained. Rich-text/rendering semantics deferred.

## 12. DIMENSION and limited subtype profile

Independent inspection of all four controlled records: flags70=32 (base type0),
AcDbDimension plus AcDbAlignedDimension and AcDbRotatedDimension markers.
Common block2/style3/override1/flags70/definition10/text midpoint11/normal210
mapped. Only source-proven linear/rotated type0 and fixture-gated aligned type1
map extension points13/14 and rotation50 in eligible contexts.

Controlled four code42 values are absent: no calculated measurements. Aligned
fixture preserves explicit42=123.456 as source evidence, not normalized truth.
Unsupported angular fixture retains common fields, UnsupportedDimensionSubtype
diagnostic and Unsupported disposition. Subtype fields remain unmapped; nullable
extension/rotation properties mean **unmapped**, not verified absence. No
universal DIMENSION support, point-distance calculation or dimension rendering.

## 13. ATTRIB / ATTDEF evidence and limits

No actual controlled ATTRIB/ATTDEF records. Separate actual UTF-8 fixtures gate
minimal AcDbText plus AcDbAttribute/AcDbAttributeDefinition profiles. Preserve
tag2, explicit value/default1, ATTDEF prompt3, flags70, placement, height, style
and rotation. ATTRIB “explicit value” and ATTDEF “default only” stay distinct;
no default copying, font work or transformed placement. Additional attribute
subclass-specific settings remain unmapped, not claimed supported.

## 14. Attribute sequences and SEQEND roles

Only explicitly valid66=1 opens INSERT → ATTRIB... → SEQEND association within
the current BLOCKS/ENTITIES section. Relationship uses source locators. Invalid
attributes cannot advertise a valid sequence. Missing terminator, orphan
ATTRIB, zero-follow and interruptions produce located errors without swallowing
the following unrelated entity. No sequence crosses block/section boundaries.
Only reviewed active attribute terminator becomes RawSequenceEnd. Legacy
POLYLINE/VERTEX/SEQEND stays gated/deferred with diagnostic; unassigned SEQEND
is Unsupported. No generic attribute interpretation of every SEQEND.

## 15. Source presence, numeric and context policies

Absent != explicit empty/zero/default. Nullable unsupported properties !=
checked absence. Required field absence is Invalid with record location;
optional absence is NotApplicable without fabricated provenance. Invariant
integer/real parsing; finite values only. NaN/Infinity/overflow/malformed and
duplicate singleton occurrences retain all raw pairs; no winner/coercion.
102 controls,1001/1002 XDATA and foreign subclasses cannot populate facts.
Eligible wrong-subclass scalar values remain unmapped with a warning; required
fields still fail when no eligible source value exists. Each mapped field occurs
once in Fields; mapped indices + unmapped indices cover every source pair once.

## 16. Measured effective dispositions

| Evidence | Supported | Deferred03C | Deferred03D | Unsupported | Invalid | Total |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Unchanged03C |188|0|93|52|0|333|
| New03D overlay |281|0|0|52|0|333|

All93 controlled03D records Supported, MappingComplete=true for03D;
EffectiveInventoryAvailable=true; zero new03D diagnostics. Overall status
**Partial** due to52 existing unsupported records. Original03B/03C dispositions
and diagnostics are retained through evidence references. This is not complete
snapshot or reader/normalization support.

## 17. Finite injectable resource limits

| Guard | Default | Controlled measurement |
| --- | ---: | ---: |
| Definitions |32|12|
| Children per definition |64|9 maximum|
| INSERT records |256|25|
| Annotation records |256|44|
| MTEXT chunks per record |32|1 maximum|
| Total raw annotation text characters (codes1/3/7/8) |16384|974|
| Attribute sequence length |64|not exercised; fixtures|
| Mapped03D records |512|93|
| Stored diagnostics |5000|0 new|
| Fields/source pairs per record |512|existing03B bounded source|
| Structural records |4096|333|
| Lexical source pairs |250000|8737|

Guards precede append/allocation for growing collections. Raw annotation text
budget includes duplicate/foreign/control values, not just successful mappings.
Diagnostic cap reserves one terminal slot; exhausted budget stops with
ResourceLimitExceeded/MappingComplete=false. Exact limits and first-over limits
tested; no truncated-success. Oversize structural evidence disables unbounded
effective-inventory assembly. Defaults remain injectable; no performance claim.

## 18. Cancellation and publication

Token checks in deferred traversal, context/field mapping, annotation text/chunks,
geometry locator collection, block/attribute membership, pair accounting,
disposition overlay and result freeze. Bounded child copies occur inside checked
operations with final token checks before publication. Cancellation produces
Canceled, reserved diagnostic and MappingComplete=false; previously completed
partial facts may remain evidence. Tests cover pre-cancel and cancellation
during freeze enumeration. No asynchronous session/reader lifecycle added.

## 19. Controlled inventory

| Type | BLOCKS | ENTITIES | Total03D |
| --- | ---: | ---: | ---: |
| BLOCK |12|0|12|
| ENDBLK |12|0|12|
| INSERT |8|17|25|
| MTEXT |4|0|4|
| TEXT |0|36|36|
| DIMENSION |0|4|4|
| Total |36|57|93|

Twelve BLOCK + twelve ENDBLK delimiters = twelve definitions, not24.
BLOCKS contains68 total source records:24 delimiters +32 geometry +8 INSERT +4
MTEXT. Complete source retains333 records,8737 pairs,17474 lines,96621 bytes.

## 20. Independent QA and source goldens

BCL File.ReadAllLines/code0 scan provides independent source ranges/inventory,
section membership and typed value oracle. Production03D output is not used
as its own source truth. Every25 INSERT checked for target, placement, every
scale/rotation/normal/count/spacing/follow presence/value and source handle/owner
evidence. All BLOCK pairs/names/base points/child order, TEXT strings, MTEXT
chunk order and four DIMENSION source profiles checked against physical lines.

Full independent names: *Model_Space, *Paper_Space, _ARCHTICK, _CLOSEDFILLED,
_CLOSEDBLANK, COLUMN_400, D_SINGLE_900, W_SINGLE_1200, *D1, *D2, *D3, *D4.
Selected zero-based pair/one-based line goldens: BLOCK start2662; name2668
(line5337), empty xref2674; ENDBLK2675..2680. First DIMENSION3915..3941;
flags3930=32, text3932="<>", second-extensionX3937=8000. All93 records have
exact pair accounting. Original89 geometry payloads remain referenced unchanged.
Independent scanner evidence: `independent-records.json` in the evidence folder.

## 21. Supplemental fixtures

Twelve actual hand-authored DXFs listed in `datasets/controlled/phase03d/README.md`.
They cover nonzero-base/nested/anonymous blocks, scales/rotation/normal, arrays,
text alignment, Unicode/formatting chunks, attributes and malformed boundaries,
aligned/unsupported dimensions, duplicate/nonfinite fields, foreign/control/XDATA
fields and legacy/unassigned terminators. Additional inline Text DXFs exercise
absence/explicit defaults, source version rejection, budgets and malformed
MTEXT. Every extraction fixture passes the actual03A→03B→03C→03D pipeline.

## 22. Final tests and verification results

Final **434/434 PASS**, zero failures/skips. New03D **78/78 PASS**. Unchanged
03C63/63,03B103/103,03A55/55; nine architecture/dependency additions and126
PHASE00/01/02 regressions pass (356 unchanged baseline tests in total).
All new03D cases are included in the final suite. Test categories: independent
inventory/all-field accounting, source locators/no clones, fixture gate, absence,
arrays, text/chunks/subtypes, attributes/malformed sequences, nonfinite/duplicates,
contexts, culture, immutability, guards, cancellation and repeated determinism.

Initial targeted run58 tests exposed two wrongly doubled backslashes in my
hand-authored fixtures; fixture spelling was corrected to reviewed literal DXF
escapes. This was fixture construction, not a reader decoding change. Evidence
of the initial failed run is retained separately; final434-test evidence supersedes
it. No prior test or assertion was weakened.

## 23. Determinism

Three fresh FileStream →03A→03B→03C→03D pipelines serialize identically,
including source evidence. Deterministic result SHA256:
`DB9E048EFCBEAD2B38F2F70A52621506D09AECDF35EBB6A2430563BC15F2B747`.
No timestamp, machine path, elapsed time or unstable exception text in results.
Predecessor geometry serialization checked unchanged before/after each read.

## 24. Build, dependency and diff verification

Core Release **PASS**. Full `DtoB.sln` Release/Rebuild **PASS** including Revit
adapter. Existing VisualStudio toolchain fallback used because dotnet.exe is
unavailable: VS2022 MSBuild/Roslyn, SDK8.0.416 files, VSTest runtime8.0.22.
No toolchain/security changes. Exact existing MSB3277 Revit warning identities
for Microsoft.VisualBasic/System.Drawing/WindowsBase pass the existing gate;
no new warning code accepted.

Raw remains pure runtime .NET8. Dxf references Raw only. No SDK/package/project
reference changes; no Core/Project/WPF/Revit references in these libraries.
Dependency tests unchanged and passing. No UI/IPC/Revit behavior changes, so
no new host or manual viewer verification applies to this raw-only phase.

Before implementation269 existing files were hashed. All269 unchanged prior
to the authorized checklist append; final comparison allows only that append.
Historical reviews/reports/tests/ADRs/source preserved. Inspected all new source
files and own changes, dependency boundary, limits, malformed-sequence recovery,
unknown-subtype coverage and cancellation. `git diff --check` passes; strict UTF-8
new-file checks pass. Intentional raw MTEXT trailing space retained in fixture.
Existing dirty-tree architecture changes are historical, not03D modifications.

Evidence folder: `artifacts/phase03/03d-raw-20261014/`:
baseline/build/TRX, `before.json`, `checklist-before.md`, independent scanner,
targeted initial TRX, `determinism.sha256`, `final/core-build.log`, `final/build.log`,
`final/tests.log`, `final/tests.trx`, final gate and diff/source audit logs.

## 25. Original controlled source hash

Before/after SHA256 of `datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf`:
`2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC`.
No source or imported/project data modifications, fallback file path or upstream
DWG reconciliation. Reader consumes supplied immutable evidence with no file I/O.

## 26. Known limitations, failure cases and readiness

Strict native AC1032 Text DXF profile inherited from03A/03B; no DWG/binary DXF,
older versions or vendor backend. Unsupported52 records remain explicit. INSERT
target resolution/global identity not verified here. MINSERT expansion unsupported;
legacy polyline sequences gated. Dimension profile limited to source-proven
linear/fixture-aligned; attributes limited to reviewed minimal profile. Other
source properties remain explicitly unmapped. No font/rich-text/annotation
geometry or defaults. Sequence-invalid results preserve evidence and cannot be
advertised complete. Resource/cancel/rejected version results are incomplete.
No performance/deployment/UI claims.

03D requested local exit checks satisfied with evidence. Ready for independent
Antigravity03D review; remaining whole-phase scope is reserved, not implemented.

## 27. Explicit scope stop and next step

No03E global handle/owner/pointer graph, provenance-ID assembly or source
reconciliation. No03F RawDrawingSnapshot. No Viewer/WPF rendering. No PHASE04
units→mm, degrees→radians, OCS/WCS, transforms/base compensation/occurrence
expansion, effective styles, cleanup, snapping, deduplication or CAD IR. No BIM,
semantic prediction, Revit generation, database or SDK. No commit/push.
Stop here for owner and independent Antigravity review; no next-phase execution.
