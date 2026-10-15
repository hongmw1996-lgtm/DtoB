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

# DtoB V1 — Updated Phase Index

이 파일들은 `DtoB_FINAL_MASTER_PLAN.md`를 기준으로 다시 작성한 PHASE 00~16 실행 문서입니다.

| Phase | 기능 |
|---|---|
| 00 | Architecture & Technical Foundation |
| 01 | DtoB Desktop Shell |
| 02 | DWG Upload & Drawing Management |
| 03 | Native DXF Data Extraction Foundation |
| 04 | CAD Data Engine |
| 05 | Drawing Region & View Detection |
| 06 | Grid / Wall / Column Recognition |
| 07 | Door / Window / Floor / Room Recognition |
| 08 | Text / Dimension / Metadata Intelligence |
| 09 | Plan + Section + Elevation → 3D Logical Model |
| 10 | BIM IR & 3D Preview |
| 11 | Review Workbench |
| 12 | Revit Add-in Foundation |
| 13 | Revit Family & Type Catalog |
| 14 | Family / Type Matching |
| 15 | Revit Native BIM Generation |
| 16 | QA, Regression & DtoB V1 Completion |

## 핵심 원칙

2026-10-14 current owner direction: **DtoB V1 uses Native Text DXF extraction.**

```text
DWG -> user-controlled external Save As/Export DXF
PHASE03: Managed Text DXF -> lexical pairs -> section/object parser
         -> DtoB-owned raw facts / RawDrawingSnapshot -> factual QA
         -> secondary minimal diagnostic Viewer
PHASE04: snapshot -> explicit unit/default/radian/OCS-WCS resolution
         -> local + nested BLOCK/INSERT transforms / effective styles
         -> cleanup / analysis structures -> normalized CAD IR
PHASE05+: recognition -> BIM IR -> Revit Adapter
```

03A DXF Lexical / Group-Code Reader; 03B Sections / Header / Tables;
03C Geometry Entity Extraction; 03D Blocks / INSERT / Annotation Extraction;
03E Handle / Owner / Source Provenance; 03F RawDrawingSnapshot Assembly;
03G Extraction QA / Deterministic Validation; 03H Minimal Diagnostic Viewer.
No PHASE03 final/local/nested INSERT matrix, mm/OCS conversion or effective style calculation.
Initial implementation fixture/profile: controlled AC1032 Text DXF; other versions/encodings require real fixture gates. DirectDWG/BinaryDXF/UTF16/DWF/automatic conversion/CAD SDK are not V1.

PHASE00/01/02 independently PASSed/merged (v0.1/v0.2/v0.3). PHASE02 historical name/registration behavior is retained; existing DWG assets remain project metadata, not supported native reader input. Current branch phase/03-cad-data-extraction; old phase/03-dwg-viewer name is historical.
ADR015 native raw boundary amendment; ADR016 Native DXF Reader **Accepted**; ADR017 lifecycle reserved. Core mm/CAD IRv1/projectv2/source IDs unchanged. MINSERT expansion unsupported with original fields/diagnostic, no cells.
ACadSharp **NOT SELECTED**; historical ACADSHARP_CAPABILITY_FAIL report/evidence unchanged. No patch/fork/integration/continued SDK review/ODA/RealDWG escalation. Prior APPROVED_FOR_CAPABILITY_SPIKE does not approve native implementation.
Current authorization: final pre-implementation housekeeping only, following APPROVED_FOR_NATIVE_DXF_IMPLEMENTATION, then **STOP before03A**. No parser/projects/Viewer/PHASE04 implementation/SDK install/commit/push.
- Phase = 사용자에게 실제로 추가되는 기능
- Subphase = 그 기능을 만드는 기술 단위
- 각 Phase 종료 시 실행 가능한 DtoB 유지
- 초기 V1은 Controlled CAD만 지원
- Layer / Color / Block / Geometry를 우선 활용
- 복잡한 AI/현업 예외는 V1 성공 이후 확장
## Final Native Text DXF pre-implementation housekeeping — 2026-10-14

Antigravity verdict: **APPROVED_FOR_NATIVE_DXF_IMPLEMENTATION**. Issues1–3 incorporated; **ADR-016 Accepted**, accepted ADR001–015 unchanged. Ready for subsequent PHASE03A; current turn STOP before parser/projects/Viewer/PHASE04 implementation, no commit/push.

Official profile: **Text DXF, AC1032, strict UTF-8**, ASCII subset naturally supported, optional UTF-8 BOM, **CRLF / LF**. Valid Korean/other non-ASCII UTF-8 is allowed, not forbidden by format terminology; actual acceptance tests remain required.

Authoritative source identity begins at **imported managed Text DXF**: **DrawingId + RevisionId + FileHash + Original DXF Handle**. OriginalHandle is the handle written in imported DXF, not an upstream DWG handle unless independently proven. DtoB guarantees no semantic/structural/identity equivalence and assumes no responsibility for third-party export changes to handles, anonymous blocks, dimensions, proxy/custom objects, unsupported records or database structure. DtoB remains responsible for extracting that managed DXF faithfully. No DWG/DXF reconciliation in PHASE03 or PHASE04.

Initial configurable/injectable development limits: physical source line **64 KiB (65,536 UTF-8 bytes)**, combined control nesting **16**, stored diagnostics **5,000** (reserve terminal slot), raw pairs **250,000**, input **16 MiB (16,777,216 bytes)**. Enforce before allocation; exceed -> **ResourceLimitExceeded**, never truncated-success. Smaller deterministic test profiles and later measured revisions permitted; no performance claim. Full semantics: ADR016 and checklist§20. Status: docs/status/PHASE_03_NATIVE_DXF_HOUSEKEEPING_REPORT.md.
