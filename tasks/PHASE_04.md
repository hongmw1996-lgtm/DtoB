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

# PHASE 04 — CAD Data Engine

## 1. 목적

PHASE 03 Native Text DXF extraction의 DtoB-owned RawDrawingSnapshot을 분석 가능한 normalized CAD IR로 변환한다. Reader Adapter를 다시 구현하거나 vendor/WPF data를 분석 Source of Truth로 삼지 않는다.

```text
RawDrawingSnapshot -> unit/frame + OCS/WCS normalization
-> BLOCK base point / local + nested INSERT composition / effective styles
-> cleanup / analysis structures -> CAD IR
```

기존 IDrawingReader -> DrawingReadResult -> CadDocument는 normalized CAD IR 경계이며 ADR-006/008의 mm/coordinate/schema 의미를 유지한다. PHASE05+가 architectural meaning을 인식한다. 이 문서는 계획 갱신이며 현재 PHASE04 구현은 승인되지 않았다.

## 2. 전제조건 / 가정

- PHASE03 extraction gate 통과와 source-traceable snapshot/completeness evidence.
- Raw native units/source degree angles/source frames, original lexical Handles와 definition/INSERT source facts/provenance 보존. PHASE03에서 INSERT matrix는 계산하지 않는다.
- Unknown/unitless/invalid/unsupported 사실의 정책을 명시적으로 결정; mm 가정 금지.
- Required CAD IR coverage가 v1 contract에 없으면 ADR/schema review 후 처리; 검증을 약화하거나 unknown fields를 추가하지 않는다.

## 3. 지원 범위

Raw primitive/annotation/table/definition facts의 typed mapping, normalized mm coordinates, OCS/WCS/definition/occurrence transform, effective layer/color/style 정책, 원본 identity/provenance 유지, deterministic minimal cleanup, analysis spatial index와 CAD IR JSON. Source bytes를 다시 decode하는 별도 reader나 semantic recognizer는 이 phase의 주 기능이 아니다.

## 4. 아직 지원하지 않는 범위

AI/semantic recognition, dynamic/complex3D/Xref resolving, CAD repair, vendor object downstream coupling, MINSERT cell expansion. MINSERT unsupported source metadata/diagnostics를 보존하며 ADR-007 ID 변경 없이 임의 확장하지 않는다.

## 5. Subphase 상세

### 04A — Primitive Mapping & Coverage

Raw geometry를 지원되는 typed IR로 map한다. LINE/POLYLINE/ARC/CIRCLE 외 POINT/SOLID/ELLIPSE/SPLINE/HATCH coverage는 기존 strict CAD IR v1의 한계를 먼저 검토하고 필요한 ADR 없이 schema를 확장하지 않는다. 원본 raw fact/Handle을 보존하고 지원하지 않는 경우 CadUnsupported + diagnostic으로 명시한다.

### 04B — Annotation Mapping

Raw TEXT/MTEXT/DIMENSION과 지원되는 annotation evidence를 map한다. Stored source facts와 derived measurement를 혼동하지 않는다. LEADER/MLEADER/TABLE은 실제 raw coverage와 schema 결정 이후 처리한다.

### 04C — Layer / Style Policy

Source raw layer/color/linetype/lineweight와 normalized 표현을 분리한다. Missing/unverified/default facts를 채워 넣어 완전한 추출처럼 표시하지 않는다.

### 04D — Definition / Occurrence Assembly

Raw DXF definitions/INSERT/attributes/source-degree rotation/scale/normal/reference facts를 분석용 occurrence 구조로 조립한다. PHASE04가 source default resolution 근거, local 및 nested transform composition을 처음 수행한다. +Z column-vector placement는 T(insertion)*R*S*T(-blockBase)로 독립 검증하며 base point를 rotation/scale 후 임의 단순 차감하지 않는다. 일반 extrusion/OCS 경로는 별도 verified convention/test 필요. Definition-local geometry와 accumulated occurrence transform을 보존·검증한다. MINSERT raw source는 unsupported이며 synthetic cell occurrence ID 생성 금지.

### 04E — Effective Data Policy

Raw entity layer, insert layer, owner block/root INSERT 및 occurrence-context ByLayer/ByBlock inheritance를 근거와 함께 결정한다. Unresolved 값은 explicit policy/error이지 guessed value가 아니다.

### 04F — Unit / Frame / OCS-WCS Normalization

Raw native coordinates와 DXF degree angles를 명시적으로 mm/radians로 변환하고 source scale/evidence를 보존한다. Unitless/unknown은 resolve 전 supported CadDocument로 만들지 않는다. Frame conversion, block base point, mirror/scale/composition을 caller-supplied tolerance로 검증한다. Non-+Z source data는 raw snapshot에 남기고 normalized v1이 지원하지 않으면 diagnostic으로 처리한다. SourceToModel이 unit scaling을 반복하지 않는다.

### 04G — Cleanup

Zero-length/exact duplicate/minimal geometry issues를 deterministic하게 탐지한다. Raw snapshot은 수정하지 않고 lineage/exclusion reasons를 유지하며 자동 수선은 제한한다.

### 04H — Analysis Structures / Spatial Index

Normalized point/line/bounds 질의와 layer 조건을 지원한다. Diagnostic Viewer pick index를 분석 truth로 재사용하지 않는다.

### 04I — CAD IR Validation / Serialization

Supported CadDocument의 structure/geometry/reference validation과 strict versioned JSON roundtrip. Raw -> normalized lineage를 유지하고 schema major 변경은 ADR 필요.

## 6. 사용자 결과

Normalized data와 raw evidence를 연결한 Analysis Inspector, 지원/미지원/정규화 실패 목록과 count를 확인할 수 있다. Reader/Vendor 객체는 UI/Analysis로 노출되지 않는다.

## 7. 필수 테스트

Raw-to-mm unit/frame conversion, unknown units, OCS/WCS/definition/occurrence composition, mirrors/nonzero base points, raw/effective metadata, source handles/lineage, cleanup provenance, strict schema/roundtrip, spatial query, MINSERT unsupported/no synthetic IDs, prior regression/dependency boundaries.

## 8. Exit Criteria

- [ ] 검증된 PHASE03 raw snapshot 소비, vendor/source fallback 없음.
- [ ] Source unit/frame evidence에서 explicit mm/OCS-WCS normalization.
- [ ] Layers/style/raw-vs-effective policy와 original Handle/ownership trace 유지.
- [ ] Definitions/지원 INSERT occurrence 정확한 normalized composition; MINSERT diagnostic 유지.
- [ ] 지원 geometry/annotation CAD IR coverage와 unsupported/normalization failure 명시.
- [ ] Raw truth를 변경하지 않는 deterministic cleanup/analysis structures.
- [ ] Strict CAD IR validation/serialization 및 raw/diagnostic source-key 연결.

## 9. Codex 실행 지시

Current work is PHASE03 Native DXF architecture/design revision only, following Antigravity APPROVED_FOR_NATIVE_DXF_IMPLEMENTATION; current turn stops before03A. PHASE04 구현은 하지 않는다. PHASE03 phase gate와 별도 owner 지시 이후 master/index/ADR-015/006/007/008/AGENTS/spec를 읽고 계획·ADR·검증 후 subphase별로 구현한다. Normalized 계약/schema 변경을 silently 수행하지 않는다. Final PHASE_04_REPORT와 independent review, no commit/push without explicit instruction.

## 10. Antigravity Review Focus

Raw/normalized 경계, unknown-unit/default fabrication, source trace, coordinate/double scaling/tolerances, effective metadata, cleanup lineage, strict schema/coverage, dependency/regression/semantic scope. Review: reviews/PHASE_04_REVIEW.md.

## 11. Phase Gate

PASS / PASS_WITH_KNOWN_LIMITATIONS / REVIEW_REQUIRED / BLOCKED. Later phase는 independent gate와 별도 owner 지시 이후 진행한다.
## Final Native Text DXF pre-implementation housekeeping — 2026-10-14

Antigravity verdict: **APPROVED_FOR_NATIVE_DXF_IMPLEMENTATION**. Issues1–3 incorporated; **ADR-016 Accepted**, accepted ADR001–015 unchanged. Ready for subsequent PHASE03A; current turn STOP before parser/projects/Viewer/PHASE04 implementation, no commit/push.

Official profile: **Text DXF, AC1032, strict UTF-8**, ASCII subset naturally supported, optional UTF-8 BOM, **CRLF / LF**. Valid Korean/other non-ASCII UTF-8 is allowed, not forbidden by format terminology; actual acceptance tests remain required.

Authoritative source identity begins at **imported managed Text DXF**: **DrawingId + RevisionId + FileHash + Original DXF Handle**. OriginalHandle is the handle written in imported DXF, not an upstream DWG handle unless independently proven. DtoB guarantees no semantic/structural/identity equivalence and assumes no responsibility for third-party export changes to handles, anonymous blocks, dimensions, proxy/custom objects, unsupported records or database structure. DtoB remains responsible for extracting that managed DXF faithfully. No DWG/DXF reconciliation in PHASE03 or PHASE04.

Initial configurable/injectable development limits: physical source line **64 KiB (65,536 UTF-8 bytes)**, combined control nesting **16**, stored diagnostics **5,000** (reserve terminal slot), raw pairs **250,000**, input **16 MiB (16,777,216 bytes)**. Enforce before allocation; exceed -> **ResourceLimitExceeded**, never truncated-success. Smaller deterministic test profiles and later measured revisions permitted; no performance claim. Full semantics: ADR016 and checklist§20. Status: docs/status/PHASE_03_NATIVE_DXF_HOUSEKEEPING_REPORT.md.
