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

# DtoB — Drawing to BIM

**DtoB V1 uses Native Text DXF extraction.** Users with DWG files Save As/Export Text DXF in their CAD application before importing. DtoB directly decodes no DWG and performs no automatic conversion.

```text
Managed Text DXF -> DtoB lexical / section / raw entity reader
-> RawDrawingSnapshot + source-line evidence / factual QA
-> PHASE04 normalization / CAD IR -> PHASE05+ recognition
-> BIM IR / review / family mapping -> Revit Adapter -> Native BIM -> RVT -> QA
```

PHASE00/01/02 completed, independently reviewed and merged (v0.1-phase00/v0.2-phase01/v0.3-phase02). Existing Desktop supports projects/settings/recent/logging, immutable drawing registration and isolated Revit PING. Historical PHASE02 DWG registration remains valid metadata; it is not Native DXF extraction support. No production reader/raw contracts/diagnostic Viewer exist yet.

Current **PHASE03 — Native DXF Data Extraction Foundation** is a design proposal. Initial authoritative fixture/profile: controlled AC1032 Text DXF; other versions/encoding content require actual tests. Raw units/source-degree angles/handles/owner/group-code spelling and lines remain source facts. PHASE04 alone calculates mm/OCS-WCS/local-nested INSERT transforms/effective styles; PHASE03 does not. MINSERT expansion unsupported, original metadata/diagnostics preserved, no cells. Core/CAD IRv1/projectv2 architecture unchanged.

ACadSharp **NOT SELECTED**; historical ACADSHARP_CAPABILITY_FAIL report/spike/evidence preserved. No patch/fork/integration/continued SDK review, ODA/RealDWG installation or production CAD SDK. ADR015 native amendment preserves the additive raw boundary; ADR016 Native DXF Reader is **Accepted**.

Antigravity verdict is APPROVED_FOR_NATIVE_DXF_IMPLEMENTATION. Current owner authorization is final pre-implementation housekeeping only, then **STOP before03A**. No parser/projects/Viewer/PHASE04 implementation, SDK installation, commit or push.

Read START_HERE.md, authoritative master/index, AGENTS.md, architecture/instructions, tasks03+04, docs/status/PHASE_03_CHECKLIST.md, ADR015/AcceptedADR016 and historical review/spike. Latest housekeeping result: docs/status/PHASE_03_NATIVE_DXF_HOUSEKEEPING_REPORT.md; earlier design report preserved as history. Build guidance: docs/PHASE_00_BUILD.md. Prior reports remain historical evidence, not current reader capability claims.
## Final Native Text DXF pre-implementation housekeeping — 2026-10-14

Antigravity verdict: **APPROVED_FOR_NATIVE_DXF_IMPLEMENTATION**. Issues1–3 incorporated; **ADR-016 Accepted**, accepted ADR001–015 unchanged. Ready for subsequent PHASE03A; current turn STOP before parser/projects/Viewer/PHASE04 implementation, no commit/push.

Official profile: **Text DXF, AC1032, strict UTF-8**, ASCII subset naturally supported, optional UTF-8 BOM, **CRLF / LF**. Valid Korean/other non-ASCII UTF-8 is allowed, not forbidden by format terminology; actual acceptance tests remain required.

Authoritative source identity begins at **imported managed Text DXF**: **DrawingId + RevisionId + FileHash + Original DXF Handle**. OriginalHandle is the handle written in imported DXF, not an upstream DWG handle unless independently proven. DtoB guarantees no semantic/structural/identity equivalence and assumes no responsibility for third-party export changes to handles, anonymous blocks, dimensions, proxy/custom objects, unsupported records or database structure. DtoB remains responsible for extracting that managed DXF faithfully. No DWG/DXF reconciliation in PHASE03 or PHASE04.

Initial configurable/injectable development limits: physical source line **64 KiB (65,536 UTF-8 bytes)**, combined control nesting **16**, stored diagnostics **5,000** (reserve terminal slot), raw pairs **250,000**, input **16 MiB (16,777,216 bytes)**. Enforce before allocation; exceed -> **ResourceLimitExceeded**, never truncated-success. Smaller deterministic test profiles and later measured revisions permitted; no performance claim. Full semantics: ADR016 and checklist§20. Status: docs/status/PHASE_03_NATIVE_DXF_HOUSEKEEPING_REPORT.md.
