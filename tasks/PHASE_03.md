> **Current status — 2026-10-15 whole-PHASE03 final gate.** Native Text DXF
> extraction 03A–03F, deterministic QA 03G and the diagnostic-only Viewer 03H
> are implemented; each has its final independent `PHASE_03A_PASS` through
> `PHASE_03H_PASS`. See [final release gate](../docs/status/PHASE_03_FINAL_GATE.md) for the current
> integrated build, tests, limitations and owner-acceptance status.
> ADR-015's additive raw boundary, Accepted ADR-016 and Accepted ADR-017
> together govern the implementation: Raw is runtime-only; Dxf references Raw;
> Desktop owns explicit managed-file hash/read lifecycle and WPF presentation.
> Managed AC1032 strict UTF-8 Text DXF is authoritative; DWG decoding and CAD SDKs
> are not production capabilities. Snapshot coverage remains Partial, including
> semantic Unsupported 52 (VPORT 1, APPID 3, OBJECTS 48); LOW-01 remains accepted.
> PHASE04, commit, push, PR and tag are **not authorized** by this gate.
>
> **Historical checkpoint notice:** earlier dated "current task", "STOP before03A",
> "proposed projects", "not implemented" and "ADR017 reserved" statements below
> record the earlier design/housekeeping authorization, superseded by the owner's
> subsequent 03A–03H instructions. They are not current implementation status.
> Earlier Proposed ADR-016 references are superseded by its Accepted decision;
> ADR-017 is now Accepted. Technical phase boundaries and future-phase requirements
> below remain in force. Original checkpoint text is retained without rewriting
> historical reports/reviews or changing accepted architectural decisions.

# PHASE 03 — Native DXF Data Extraction Foundation

## 1. Purpose / current authorization

**DtoB V1 uses Native Text DXF extraction.** This owner-directed turn is architecture/design revision only. STOP before03A; do not implement parser/raw production contracts/projects/Viewer, install SDKs, begin PHASE04, commit or push.

```text
DWG -> external user-controlled Save As/Export DXF
Managed Text DXF -> DXF lexical reader -> raw group-code records
-> section/object parser -> DtoB-owned raw facts -> RawDrawingSnapshot
-> factual QA -> minimal diagnostic Viewer
PHASE04: snapshot -> normalization / CAD IR
PHASE05+: recognition -> BIM IR -> Revit Adapter
```

No direct DWG V1 decoder. ACadSharp is NOT SELECTED; failed spike and previous SDK-oriented review remain historical, not current implementation approval. No patch/fork/integration/continued backend review/ODA/RealDWG escalation. See docs/status/PHASE_03_CHECKLIST.md (30-section proposal), ADR015 native amendment and **Accepted** ADR016-Native-DXF-Reader.

## 2. Assumptions / input profile

PHASE00/01/02 independent PASS/merged; existing project v2/managed revisions stay intact. First authoritative implementation fixture is unchanged datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf, actual **AC1032**. Other versions require real fixtures/tests and explicit profiles before support claims. Accepted **Text DXF / AC1032 / strict UTF-8** profile, ASCII subset naturally supported, optional UTF-8 BOM, CRLF / LF supported; valid Korean/non-ASCII UTF-8 allowed; controlled input is ASCII-only/noBOM/CRLF, so non-ASCII compatibility is not yet proven. Unsupported format/version/encoding has an explicit outcome, no guessed codepage or replacement decoding.

Existing PHASE02 DWG registrations remain valid project metadata but are ineligible for Native DXF extraction. Users export DXF externally; no automatic conversion/source fallback or manifest migration. New import UI preference and extraction eligibility integration are later reviewed work, not changes now.

## 3. Supported phase scope (planned, not implemented)

- Source-preserving raw code/value text, physical code/value lines, ordered pairs/ranges and typed scalar parse state.
- Explicit SECTION/ENDSEC/EOF and HEADER/CLASSES/TABLES/BLOCKS/ENTITIES/OBJECTS; every record disposition is SUPPORTED/IGNORED_WITH_DIAGNOSTIC/UNSUPPORTED/INVALID.
- Header ACADVER/INSUNITS/EXTMIN/EXTMAX and encoding/handle-seed evidence; units are declarations only.
- Tables LAYER/LTYPE/STYLE/DIMSTYLE/BLOCK_RECORD: original handles/owners/raw names/flags/color modes/references/source locations.
- Required initial geometry LINE/LWPOLYLINE/ARC/POINT/SOLID. CIRCLE planned simple mapping with separate real fixture gate; POLYLINE/VERTEX/SEQEND separate legacy family/subtype gate. ELLIPSE/SPLINE/HATCH later investigation/raw unsupported retention.
- BLOCK definition handle/owner/name/base point/flags/children/range versus INSERT handle/owner/target/position/scales/source-degree rotation/normal/arrays/attributes-follow/raw fields.
- TEXT/MTEXT raw content/chunks/control facts, DIMENSION raw defining/reference facts; ATTRIB/ATTDEF coverage investigation and sequence handling.
- Contextual handle/owner/reference/membership validation, located diagnostics, bounded safety/cancellation, deterministic owned snapshot/source QA.
- Secondary snapshot-only basic native-frame diagnostic display/navigation/layer/click/source-line inspection.

MINSERT expansion remains UNSUPPORTED: preserve base handle/row-column-spacing/raw insertion facts, emit UnsupportedOccurrenceExpansion, continue safely. No synthetic cell IDs/flatten/Explode.

## 4. PHASE04 boundary / exclusions

PHASE03 does **not calculate final/local/nested INSERT transforms** or reproduce GetTransform. PHASE04 solely owns explicit source-unit/default resolution, mm/radian conversion, OCS->WCS, block/base-point/nested composition, ByLayer/ByBlock effective styles, normalization/cleanup/validation policy/analysis structures and normalized CAD IR. Preserve the existing IDrawingReader/DrawingReadResult/CadDocument contract and ADR006/008 semantics.

No recognition/semantic meanings/measured dimensions/OCR/AI/BIM/Revit generation, CAD repair/editing/source-ID replacement, automatic CAD host/conversion, SDK dependency, BinaryDXF/UTF16/nonstandard text/DWF/directDWG, Xref resolution/dynamic3D promises or AutoCAD rendering fidelity.

## 5. Official subphases

### 03A — DXF Lexical / Group-Code Reader

After implementation approval: strict text/version/encoding profile, raw spelling/line ranges/ordered pair store, checked invariant scalar parsing, explicit malformed/unsupported outcomes, injected resource limits and cooperative cancellation. No product parser this design turn.

### 03B — Sections / Header / Tables

Stateful delimiters/header variables, typed required tables and preserved unknown records; subclass/control context, missing/duplicate fields and unusable header extents. No silent ignored sections or false source defaults.

### 03C — Geometry Entity Extraction

Required source coordinates/vertex order/bulges/widths/normal/elevation/flags; separate acceptance for CIRCLE and legacy sequences, raw unsupported later families. No geometry normalization, approximation or zero-length pruning.

### 03D — Blocks / INSERT / Annotation Extraction

Distinct BLOCK_RECORD/BLOCK/ENDBLK/INSERT records and native placement parameters, raw TEXT/MTEXT/DIMENSION facts; investigate attributes/SEQEND. MINSERT unsupported expansion/no cells. No transformation/measurement/effective-style engine.

### 03E — Handle / Owner / Source Provenance

DrawingId/RevisionId/FileHash, original lexical/numeric handles, contextual owners/references, section/block/source pair and line ranges. Code105 DIMSTYLE and HEADER/control-group exceptions; no generated replacement source handles. Deferred bounded reference validation.

### 03F — RawDrawingSnapshot Assembly

Owned vendor/WPF/Revit-free Header/Tables/Blocks/Spaces/Entities/Objects/Diagnostics/SourceEvidence, explicit completeness/dispositions and immutable publication. Derived extraction/QA export only; no .dtob/schema/CAD IR migration.

### 03G — Extraction QA / Deterministic Validation

Actual controlled pair/section/scope/handle/layer/block/INSERT/geometry/annotation/reference/disposition counts and source-line samples, repeat deterministic output, malformed/unsupported/limit/cancel/lease/hash/session regressions. Previous independent spike inventory is reference only; no forced semantic counts.

### 03H — Minimal Diagnostic Viewer

Only after QA: display directly drawable native-frame source geometry, Zoom/Pan/Fit/Layer On-Off/click -> Handle/Type/Layer/raw/source-line Inspector. Separate local block-definition inspection and INSERT raw markers; no inserted/nested block transform or OCS/unit conversion. Explicit unrendered frames/annotations; no second parser, plot/SHX/high-fidelity gate.

## 6. User result / authority

Selected managed DXF -> explicit Extract -> source facts/counts/diagnostics and native-frame diagnostic view. File hash verified on strict FileAccess.Read/FileShare.Read same lease; no SourceFilePath fallback or project mutation. Ordinary Open/Refresh/metadata selection still zero content reads/hashes/sourceIO. Selected DWG yields an external-export instruction, not decode. Viewer/QA actions do not dirty the project.

## 7. Required tests (future implementation)

Controlled AC1032 source pairs/line goldens; strict encoding/version, repeated/absent/raw fields; structural/sequence/control contexts; source handles/code5-code105/owners/reactors/references; core geometry and annotation raw samples; optional real file gates; MINSERT metadata/diagnostic/following record/no cells; every record disposition/count; malformed/nonfinite/limit/cancel/dispose/stale behavior; immutable bytes/hash/session; Open/Refresh no read/hash/sourceIO; all prior regression/dependency checks. PHASE04 owns later coordinate transform tests, not an extra engine in PHASE03.

## 8. Exit Criteria (future implementation)

- [ ] Actual controlled AC1032 Text DXF read with exact raw text/line evidence and tested version/encoding profile.
- [ ] All section/record scopes accounted once; header/tables/definitions/spaces/raw objects and dispositions inspectable.
- [ ] Required raw geometry/TEXT/MTEXT/DIMENSION facts and original identity/provenance preserved; optional support separately fixture-proven.
- [ ] BLOCK definitions versus INSERT source facts; MINSERT retention/unsupported/safe continuation, no transformed occurrences/cell IDs.
- [ ] Original coordinates/units/angle units/default absence; no PHASE03 matrix/mm/OCS/effective-style computation.
- [ ] Owned deterministic RawDrawingSnapshot, factual QA/completeness and bounded invalid/unsupported cases; no valid-empty success on errors.
- [ ] Read-only verified managed-only extraction/cancellation/stale publication, unchanged bytes/session, no Open/Refresh content/hash/sourceIO.
- [ ] Minimal snapshot-only native-frame diagnostic Viewer with explicit limits; no CAD-fidelity requirement.
- [ ] Measured limits, Core/full Release/all tests/actual workflow/hash/diff/report and independent review evidence.

## 9. Execution / review / gate

Read master/index/AGENTS/architecture/instructions/current checklist/native ADR proposal and historical review/spike before work. Latest owner authorizes **final documentation housekeeping only**, following APPROVED_FOR_NATIVE_DXF_IMPLEMENTATION, then STOP before03A. Older APPROVED_FOR_CAPABILITY_SPIKE does not approve native implementation. No Native DXF Reader production project/code or SDK installation this turn.

Future approved implementation proceeds subphase-by-subphase with fixtures; final PHASE_03_REPORT requires actual evidence. Final implementation status PASS/PASS_WITH_KNOWN_LIMITATIONS/REVIEW_REQUIRED/BLOCKED. Architecture is APPROVED_FOR_NATIVE_DXF_IMPLEMENTATION after housekeeping; phase implementation remains incomplete, no phase PASS. Do not start PHASE04 or commit/push without explicit instruction.
## Final Native Text DXF pre-implementation housekeeping — 2026-10-14

Antigravity verdict: **APPROVED_FOR_NATIVE_DXF_IMPLEMENTATION**. Issues1–3 incorporated; **ADR-016 Accepted**, accepted ADR001–015 unchanged. Ready for subsequent PHASE03A; current turn STOP before parser/projects/Viewer/PHASE04 implementation, no commit/push.

Official profile: **Text DXF, AC1032, strict UTF-8**, ASCII subset naturally supported, optional UTF-8 BOM, **CRLF / LF**. Valid Korean/other non-ASCII UTF-8 is allowed, not forbidden by format terminology; actual acceptance tests remain required.

Authoritative source identity begins at **imported managed Text DXF**: **DrawingId + RevisionId + FileHash + Original DXF Handle**. OriginalHandle is the handle written in imported DXF, not an upstream DWG handle unless independently proven. DtoB guarantees no semantic/structural/identity equivalence and assumes no responsibility for third-party export changes to handles, anonymous blocks, dimensions, proxy/custom objects, unsupported records or database structure. DtoB remains responsible for extracting that managed DXF faithfully. No DWG/DXF reconciliation in PHASE03 or PHASE04.

Initial configurable/injectable development limits: physical source line **64 KiB (65,536 UTF-8 bytes)**, combined control nesting **16**, stored diagnostics **5,000** (reserve terminal slot), raw pairs **250,000**, input **16 MiB (16,777,216 bytes)**. Enforce before allocation; exceed -> **ResourceLimitExceeded**, never truncated-success. Smaller deterministic test profiles and later measured revisions permitted; no performance claim. Full semantics: ADR016 and checklist§20. Status: docs/status/PHASE_03_NATIVE_DXF_HOUSEKEEPING_REPORT.md.
