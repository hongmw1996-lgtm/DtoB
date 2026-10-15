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

# START HERE

Authoritative plans: DtoB_FINAL_MASTER_PLAN.md and PHASE_INDEX.md. Read AGENTS.md, ARCHITECTURE.md, CODEX_INSTRUCTIONS.md, tasks/PHASE_03.md, tasks/PHASE_04.md, the 30-section docs/status/PHASE_03_CHECKLIST.md and ADR015 native amendment/AcceptedADR016. Older removed MASTER_PLAN.md/FILE_INDEX.md are not alternate authority.

## Current task

**DtoB V1 uses Native Text DXF extraction.** PHASE03 is Native DXF Data Extraction Foundation: source-preserving lexical/group-code/section/table/entity parsing -> owned RawDrawingSnapshot/source QA -> secondary minimal diagnostic Viewer. DWG users externally Save As/Export Text DXF; no direct DWG V1 requirement or automated conversion/SDK selection. First controlled implementation profile AC1032; other versions/encoding cases need real fixtures/tests.

PHASE04 solely owns unit/default/radian/OCS-WCS resolution, block/base-point/local/nested transforms, effective styles, normalization/cleanup/analysis CAD IR. PHASE03 retains raw INSERT facts and source lines, no matrices. MINSERT expansion unsupported with original metadata/diagnostic/safe continuation and no cells/IDs. Raw snapshot is derived source evidence, not project or normalized BIM truth. Actual managed DXF is authoritative; SourceFilePath never fallback.

Antigravity verdict is APPROVED_FOR_NATIVE_DXF_IMPLEMENTATION; owner currently authorizes **final document housekeeping only, then STOP before03A**. No Native DXF parser/raw production projects/Viewer/PHASE04 code, SDK install, commit or push. ADR016-Native-DXF-Reader is Accepted. Start03A only on subsequent owner direction; architecture approval is recorded, no implementation this turn.

## Historical evidence / disposition

PHASE00/01/02 completed/reviewed/merged; historical PHASE02 DWG assets remain registrations, not supported extraction inputs. Current branch phase/03-cad-data-extraction. Prior Antigravity APPROVED_FOR_CAPABILITY_SPIKE and housekeeping report are history, not approval of the native implementation proposal.

ACadSharp **NOT SELECTED**, ACADSHARP_CAPABILITY_FAIL. Preserve docs/status/PHASE_03_ACADSHARP_SPIKE_REPORT.md, isolated spike and all evidence; no repair/patch/fork/integration/continued backend review or ODA/RealDWG escalation. Existing independent DXF group-code QA is reference only; native reader must prove its own controlled/source-line/unsupported/malformed/lifecycle/limit tests.

## Workflow

Design -> owner/Antigravity architecture review -> separately approved stepwise03A–03H -> fixture/source QA/tests/build/manual extraction/hash evidence -> final independent implementation review -> fixes/gate. No phase PASS from a design document or SDK/API inspection. Preserve prior reports, accepted ADR numbers, original fixtures and managed-byte/session authority.

Latest housekeeping checks/report: docs/status/PHASE_03_NATIVE_DXF_HOUSEKEEPING_REPORT.md. Earlier Native DXF design report remains historical. Normal Open/Refresh/selection stays no asset-content/hash/sourceIO; explicit Extract only may read/hash selected managed DXF without dirty mutation. Build guidance docs/PHASE_00_BUILD.md includes existing VisualStudio fallback when dotnet.exe is absent.
## Final Native Text DXF pre-implementation housekeeping — 2026-10-14

Antigravity verdict: **APPROVED_FOR_NATIVE_DXF_IMPLEMENTATION**. Issues1–3 incorporated; **ADR-016 Accepted**, accepted ADR001–015 unchanged. Ready for subsequent PHASE03A; current turn STOP before parser/projects/Viewer/PHASE04 implementation, no commit/push.

Official profile: **Text DXF, AC1032, strict UTF-8**, ASCII subset naturally supported, optional UTF-8 BOM, **CRLF / LF**. Valid Korean/other non-ASCII UTF-8 is allowed, not forbidden by format terminology; actual acceptance tests remain required.

Authoritative source identity begins at **imported managed Text DXF**: **DrawingId + RevisionId + FileHash + Original DXF Handle**. OriginalHandle is the handle written in imported DXF, not an upstream DWG handle unless independently proven. DtoB guarantees no semantic/structural/identity equivalence and assumes no responsibility for third-party export changes to handles, anonymous blocks, dimensions, proxy/custom objects, unsupported records or database structure. DtoB remains responsible for extracting that managed DXF faithfully. No DWG/DXF reconciliation in PHASE03 or PHASE04.

Initial configurable/injectable development limits: physical source line **64 KiB (65,536 UTF-8 bytes)**, combined control nesting **16**, stored diagnostics **5,000** (reserve terminal slot), raw pairs **250,000**, input **16 MiB (16,777,216 bytes)**. Enforce before allocation; exceed -> **ResourceLimitExceeded**, never truncated-success. Smaller deterministic test profiles and later measured revisions permitted; no performance claim. Full semantics: ADR016 and checklist§20. Status: docs/status/PHASE_03_NATIVE_DXF_HOUSEKEEPING_REPORT.md.
