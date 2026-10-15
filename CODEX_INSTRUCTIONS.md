> **Current status — 2026-10-15 whole-PHASE03 final gate.** Native Text DXF
> extraction 03A–03F, deterministic QA 03G and the diagnostic-only Viewer 03H
> are implemented; each has its final independent `PHASE_03A_PASS` through
> `PHASE_03H_PASS`. See [final release gate](docs/status/PHASE_03_FINAL_GATE.md) for the current
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

# Codex Instructions — PHASE 03 Native DXF Design Gate

**DtoB V1 uses Native Text DXF extraction.** Antigravity verdict is APPROVED_FOR_NATIVE_DXF_IMPLEMENTATION. Current owner request is final pre-implementation document housekeeping only, then STOP before03A. No Native DXF parser/raw production projects/Viewer/PHASE04 code, SDK install, commit or push.

Read master/index/AGENTS/architecture/tasks03+04/checklist/ADR015 native amendment and AcceptedADR016, previous review/housekeeping/spike before work. Preserve original controlled DXF and prior reports/review/spike/evidence. Current branch phase/03-cad-data-extraction. Old APPROVED_FOR_CAPABILITY_SPIKE is historical and does not approve Native DXF implementation.

## Input and boundaries

- Users export DWG to Text DXF externally; no directDWG/BinaryDXF/UTF16/nonstandard encodings/DWF/automatic conversion/cloud/host automation or CAD SDK V1 dependency.
- Initial controlled AC1032 text profile only; new versions/content encodings need actual fixtures/tests, no filename inference or silent fallback.
- PHASE03: strict text -> original ordered group-code/code-value-line evidence -> sections/tables/raw geometry/annotations/BLOCK/INSERT/handle-owner facts -> owned RawDrawingSnapshot/source QA. No parser exception becomes valid empty data.
- PHASE04: source unit/default resolution, mm/radian/OCS-WCS, local/final/nested block transforms, effective styles, normalization/cleanup/analysis structures -> existing IDrawingReader/CadDocument/CAD IR.
- PHASE05+: recognition/architectural semantics; BIM IR domain truth, Revit output adapter.

No PHASE03 transformation/measurement/ByLayer-ByBlock calculation. MINSERT source handle/array/raw insertion fields preserved with unsupported expansion diagnostic and continuation, no cells/IDs/flatten/Explode. Raw facts keep spelling/presence/absent/invalid/source location, no fabricated source defaults. Viewer consumes snapshot only, supported native-frame display/local definition view/raw INSERT markers; no second DXF parser or transform engine.

Proposed DtoB.Cad.Raw and DtoB.Cad.Dxf are pure net8 with no vendor/WPF/Revit dependency; do not create this turn. Core mm/CAD IRv1/BIM/source IDs/projectv2 and accepted ADR001–014 preserved. ADR015 amendment accepted raw-boundary clarification under owner direction; **ADR016 Accepted** for Native Text DXF. ADR017 lifecycle remains reserved if justified.

## Historical backend disposition

ACadSharp **NOT SELECTED**, historical ACADSHARP_CAPABILITY_FAIL/report/evidence unchanged. No patch/fork/repair/integration/continued backend review or ODA/RealDWG install. Historical spike may retain isolated reference; no production project may reference it.

## Read authority / safety

ManagedFilePath resolved against actual CurrentFilePath with PHASE02 containment/reparse rules; no SourceFilePath fallback/IO. Explicit selected extraction only may read/hash with strict immutable lease. Ordinary Open/Refresh/metadata selection must stay no-content/no-hash/no-sourceIO. Native loops/async reads observe cancellation; dispose via normal owner finally and reject stale success AND error. No source/managed bytes or persisted session/dirty/IDs/hash mutation.

Design explicit unsupported format/version/encoding and raw record dispositions; contextual HEADER/code105/reactor/owner rules, repeated pairs, finite invariant numbers, malformed sequences, measured finite limits/diagnostic budgets and deterministic source-linked QA. Independent historical DXF group-code evidence is a reference, not native parser proof or forced semantic counts. All claimed families/version/encoding cases require reviewed real text fixtures.

## Verification / stop

For this final documentation-housekeeping task run FoundationDocumentTests/affected dependency checks, inspect document diff/UTF8/ADR sequence and git diff --check. Confirm no production project/package/code or historical spike/report/input changes. Record actual design checks/limitations in docs/status/PHASE_03_NATIVE_DXF_HOUSEKEEPING_REPORT.md, then stop before03A. No new native test/build compatibility claim.

Future implementation requires separate approval and stepwise03A–03H fixtures/regressions/Core/full Release/all tests/actual extraction/diagnostic/read-authority/hash evidence/final report. Revit host checks only if IPC/integration actually changes. Never use prior SDK tests to claim Native DXF success.
## Final Native Text DXF pre-implementation housekeeping — 2026-10-14

Antigravity verdict: **APPROVED_FOR_NATIVE_DXF_IMPLEMENTATION**. Issues1–3 incorporated; **ADR-016 Accepted**, accepted ADR001–015 unchanged. Ready for subsequent PHASE03A; current turn STOP before parser/projects/Viewer/PHASE04 implementation, no commit/push.

Official profile: **Text DXF, AC1032, strict UTF-8**, ASCII subset naturally supported, optional UTF-8 BOM, **CRLF / LF**. Valid Korean/other non-ASCII UTF-8 is allowed, not forbidden by format terminology; actual acceptance tests remain required.

Authoritative source identity begins at **imported managed Text DXF**: **DrawingId + RevisionId + FileHash + Original DXF Handle**. OriginalHandle is the handle written in imported DXF, not an upstream DWG handle unless independently proven. DtoB guarantees no semantic/structural/identity equivalence and assumes no responsibility for third-party export changes to handles, anonymous blocks, dimensions, proxy/custom objects, unsupported records or database structure. DtoB remains responsible for extracting that managed DXF faithfully. No DWG/DXF reconciliation in PHASE03 or PHASE04.

Initial configurable/injectable development limits: physical source line **64 KiB (65,536 UTF-8 bytes)**, combined control nesting **16**, stored diagnostics **5,000** (reserve terminal slot), raw pairs **250,000**, input **16 MiB (16,777,216 bytes)**. Enforce before allocation; exceed -> **ResourceLimitExceeded**, never truncated-success. Smaller deterministic test profiles and later measured revisions permitted; no performance claim. Full semantics: ADR016 and checklist§20. Status: docs/status/PHASE_03_NATIVE_DXF_HOUSEKEEPING_REPORT.md.
