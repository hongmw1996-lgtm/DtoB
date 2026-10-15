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

# DtoB
## 2D CAD → Revit BIM Automation Platform
### Final Master Development Plan

> Project Name: **DtoB**  
> Meaning: **Drawing to BIM**  
> Version: v1.0  
> Primary Goal: Controlled Text DXF → Native DXF Extraction → CAD IR → DtoB Analysis → BIM Preview → Revit Native BIM → RVT
> Development Tools: Codex + Antigravity  
> Primary Target: Autodesk Revit 2025 series  
> Platform: Windows 11

---

> **2026-10-14 owner Native DXF direction:** DtoB V1 uses Native Text DXF extraction. Users externally Save As/Export DWG to Text DXF; DtoB directly decodes no DWG and selects no CAD SDK. PHASE03 is Native DXF Data Extraction Foundation; PHASE04 alone owns unit/OCS-WCS/block transform/effective-style normalization into CAD IR; PHASE05+ owns recognition. ACadSharp is NOT SELECTED (historical ACADSHARP_CAPABILITY_FAIL); preserve its report/evidence, no patch/integration/continued SDK review. ADR015 native amendment preserves the raw boundary; ADR016 Native DXF Reader is Accepted. Current work is final pre-implementation housekeeping only, then STOP before03A; Antigravity APPROVED_FOR_NATIVE_DXF_IMPLEMENTATION. Core mm/CAD IRv1/projectv2/source IDs remain unchanged. Sections14–16 retain completed milestone history; existing PHASE02 DWG registrations do not imply V1 decoding support.

# 0. 프로젝트 한 줄 정의

> **DtoB는 2D Text DXF 도면의 Layer, Color, Geometry, Block, Text, Dimension, 평면·단면·입면 정보를 해석하고, 사용자가 검수 가능한 BIM 데이터로 변환한 뒤 Revit Add-in을 통해 Native Revit 객체를 자동 생성하는 독립형 BIM 자동화 프로그램이다.**

---

# 1. DtoB V1의 가장 중요한 목표

DtoB V1의 첫 번째 성공 기준은 거대한 AI 시스템이 아니다.

가장 먼저 성공해야 하는 것은 아래의 **전체 End-to-End Pipeline**이다.

```text
정리된 Text DXF (DWG 사용자는 외부 CAD에서 Save As/Export DXF)
    ↓
DtoB Desktop에 DXF 등록
    ↓
RawDrawingSnapshot으로 원시 사실 추출
    ↓
Factual QA + 최소 diagnostic Viewer
    ↓
단위/frame/OCS-WCS 정규화 → CAD IR
    ↓
Layer / Color / Block / Geometry 분석
    ↓
Wall / Door / Window / Column / Floor / Grid 인식
    ↓
1F / 2F / Section / Elevation 구분
    ↓
3D BIM Logical Model 생성
    ↓
DtoB 3D BIM Preview
    ↓
Revit Family / Type 매칭
    ↓
DtoB Revit Add-in
    ↓
Native Revit Element 생성
    ↓
RVT 저장
```

**단순한 도면 하나라도 이 전체 흐름을 안정적으로 끝까지 통과시키는 것**이 DtoB의 첫 Milestone이다.

---

# 2. 초기 개발 철학

## 2.1 처음부터 모든 CAD를 처리하지 않는다

초기 MVP에서는 도면이 매우 명확하게 정리되어 있다고 가정한다.

예:

```text
Wall      → A-WALL
Door      → A-DOOR
Window    → A-WINDOW
Floor     → A-FLOOR
Column    → S-COLUMN
Grid      → A-GRID
Text      → A-TEXT
Dimension → A-DIM
Level     → A-LEVEL
Section   → A-SECTION
```

또한 다음을 전제로 한다.

- Layer가 객체별로 명확히 분리되어 있음
- Color가 정리되어 있음
- Unit은 mm
- Block Name이 명확함
- 평면/단면/입면이 구분 가능한 상태
- Xref는 초기 MVP에서 제외
- Dynamic Block은 초기 MVP에서 제외
- 곡선 Wall 및 비정형 Wall 제외
- 잘못 작성된 CAD 복구 기능 제외

---

## 2.2 초기 목표는 인식률보다 전체 Pipeline 성공

초기부터 아래를 목표로 하지 않는다.

```text
모든 회사의 CAD 지원
Layer 0 혼용 지원
잘못된 CAD 자동 복구
복잡한 Nested Block
AI가 모든 의미 자동 추론
실제 모든 건축 도면 대응
```

먼저:

```text
정리된 Text DXF
→ Native DXF extraction / CAD IR
→ 정확한 분석
→ 정확한 BIM IR
→ 정확한 Revit 생성
```

을 성공시킨다.

---

# 3. 제품 구성

DtoB는 두 개의 프로그램 구성으로 개발한다.

```text
DtoB Desktop Application
+
DtoB Revit Add-in
```

---

# 4. DtoB Desktop Application 역할

DtoB Desktop은 전체 프로젝트의 중심 프로그램이다.

담당 기능:

- Project 생성/저장/열기
- Text DXF 등록 (DWG는 사용자 외부 export)
- 원시 CAD factual extraction / QA / 최소 diagnostic Viewer
- Layer/Block/Text 분석
- 객체 인식
- 평면/단면/입면 분석
- BIM Logical Model 생성
- 2D Overlay
- 3D BIM Preview
- 사용자 Review
- Revit Family Mapping
- Generate BIM 명령
- QA 결과 확인

---

# 5. DtoB Revit Add-in 역할

Revit Add-in은 Revit 전용 기능만 담당한다.

담당:

- DtoB Desktop과 연결
- Revit Template 정보 읽기
- Revit Family Catalog 추출
- Wall Type / Floor Type 추출
- Family / Type 정보 추출
- BIM IR 수신
- Native Revit Element 생성
- Parameter 작성
- 오류 수집
- RVT 저장

---

# 6. 왜 독립형 프로그램 + Revit Add-in 구조인가

DtoB의 핵심 기능 대부분은 Revit 자체 기능이 아니다.

예:

- Text DXF 등록 (DWG는 사용자 외부 export)
- 분석
- Viewer
- 검수
- 3D Preview
- Family Mapping

이를 모두 Revit Add-in 창 내부에 넣으면 장기적으로 Revit에 지나치게 종속된다.

독립형 구조로 만들면 향후:

```text
DWG → Revit
DWG → IFC
PDF → BIM
Image → BIM
Text → BIM
BIM → Knowledge Graph
```

으로 확장할 수 있다.

Revit은 DtoB의 여러 Output Adapter 중 하나가 된다.

---

# 7. 핵심 Architecture

```text
DWG -> user-controlled external Save As/Export DXF
Managed Text DXF bytes
 ↓
DtoB Native DXF lexical reader / ordered raw group-code evidence
 ↓
Section/object parser / typed DtoB-owned raw source facts
 ↓
IRawDrawingReader / RawDrawingReadResult / RawDrawingSnapshot
 ↓
PHASE04 CAD Data Engine (unit/radian/frame/OCS-WCS + block/nested transforms,
                       effective styles, cleanup / analysis structures)
 ↓
CAD IR (IDrawingReader / DrawingReadResult / CadDocument)
 ↓
Drawing Intelligence -> Semantic Objects -> Multi-view Resolver
 ↓
BIM IR -> Review -> Family Mapping -> Revit Adapter
 ↓
Native Revit Model -> RVT
```

No CAD SDK, direct DWG decode or automatic conversion in V1. Raw pair spelling/physical source lines and record ranges remain inspectable. PHASE03 extracts raw INSERT fields, never local/final/nested matrices. PHASE04 composes transforms with independent tests.

---
# 8. Architecture 핵심 원칙

## Rule 1 — DWG를 바로 Revit으로 보내지 않는다

잘못된 구조:

```text
DWG
→ Revit API
→ RVT
```

올바른 구조:

```text
Managed Text DXF
→ Native DXF lexical/section/raw facts → RawDrawingSnapshot (PHASE 03)
→ CAD Data Engine / normalized CAD IR (PHASE 04)
→ Drawing Intelligence (PHASE 05+)
→ BIM IR
→ Revit Adapter
→ RVT
```

---

## Rule 2 — CAD Geometry와 BIM 의미를 분리한다

```text
LINE
ARC
POLYLINE
BLOCK
```

은 CAD 객체다.

```text
Wall
Door
Window
Floor
Column
```

은 BIM 의미다.

따라서:

```text
CAD Primitive
↓
Semantic Interpretation
↓
BIM Object
```

구조를 유지한다.

---

# 9. 초기 객체 인식 전략

초기 MVP에서는 복잡한 AI보다 CAD 내부의 명확한 정보를 우선 사용한다.

우선순위:

```text
Layer
Color
Block Name
Geometry
Text
Dimension
```

초기에는 Layer가 강력한 신호다.

예:

```text
Layer = A-WALL
+
두 평행선
+
간격 200mm
=
Wall
```

---

# 10. Layer를 반드시 읽는 이유

Geometry만 사용하면 오류가 쉽게 발생한다.

예:

```text
Line A
Line B

Distance = 150mm
Parallel = true
```

Geometry만 보면 Wall처럼 보일 수 있다.

하지만:

```text
Line A Layer = A-FLOOR
Line B Layer = A-FURNITURE
```

라면 Wall이 아니다.

반대로:

```text
Line A Layer = A-WALL
Line B Layer = A-WALL
```

이면 Wall일 가능성이 매우 높다.

따라서 초기 DtoB에서도 최소한:

```text
Layer
+
Geometry
+
Block
```

을 함께 사용한다.

---

# 11. 인식 로직의 단계적 발전

## LEVEL 1 — Controlled CAD

이번 V1.

```text
명확한 Layer
명확한 Color
명확한 Block
정확한 Unit
정리된 Drawing
```

인식:

```text
Layer
+
간단한 Geometry
+
Block
```

목표:

> 단순한 2층 Text DXF를 RVT까지 완전히 변환 (DWG는 사용자 외부 export)

---

## LEVEL 2 — Semi-Structured CAD

V1 성공 이후.

추가:

- Layer Mapping
- 회사별 Naming
- Text Association
- Block Attribute
- Geometry Validation
- 기본 Conflict 처리

---

## LEVEL 3 — Unstructured CAD

후속 고도화.

```text
Geometry
+
Layer
+
Color
+
Block
+
Text
+
Dimension
+
Spatial Context
+
Plan / Section / Elevation
+
AI
```

추가:

- Confidence
- Negative Evidence
- Conflict Resolution
- Learning from Corrections

---

# 12. Phase 개발 원칙

앞으로 Phase는 기술 항목이 아니라:

> **사용자에게 실제로 추가되는 기능 단위**

로 구성한다.

각 Phase 내부에서 기술 개발은 Subphase / Task로 나눈다.

---

# 13. DtoB V1 Phase 전체 구조

```text
PHASE 00  Architecture
PHASE 01  DtoB Desktop Shell
PHASE 02  Project / DWG Upload
PHASE 03  Native DXF Data Extraction Foundation
PHASE 04  CAD Data Engine
PHASE 05  Drawing Region / View Detection
PHASE 06  Grid / Wall / Column Recognition
PHASE 07  Door / Window / Opening / Room / Floor
PHASE 08  Text / Dimension / Metadata Intelligence
PHASE 09  Plan + Section + Elevation → 3D
PHASE 10  BIM IR + 3D Preview
PHASE 11  Review Workbench
PHASE 12  Revit Add-in Foundation
PHASE 13  Revit Family Catalog
PHASE 14  Family / Type Matching
PHASE 15  Revit BIM Generation
PHASE 16  QA / V1 Completion
```

---

# 14. PHASE 00 — Architecture

## 목적

실제 구현을 시작하기 전
DtoB 전체 기술 방향을 확정한다.

---

## 00A — 제품 형태 확정

```text
DtoB Desktop
+
DtoB Revit Add-in
```

---

## 00B — Desktop Framework 결정

우선 검토 후보:

```text
C#
.NET
WPF
WebView2
```

분석 Engine은 필요 시 Python을 별도 Process 또는 Service 형태로 사용할 수 있게 설계한다.

---

## 00C — Revit Stack

```text
C#
Revit API
Visual Studio
```

Revit Adapter는 CAD/BIM Core와 분리한다.

---

## 00D — 데이터 구조

```text
DWG
→ CAD IR
→ BIM IR
→ Revit
```

최소 Schema를 먼저 정의한다.

---

## 00E — Desktop ↔ Revit 통신 PoC

본격 기능 개발 전에 연결 가능성만 확인한다.

예:

```text
DtoB Desktop
→ PING

DtoB Revit Add-in
→ PONG
```

---

## PHASE 00 Exit Criteria

- Desktop Framework 확정
- Revit Add-in 구조 확정
- 통신 방식 확정
- CAD Reader 방향 확정
- CAD IR 최소 Schema
- BIM IR 최소 Schema
- Repository 구조 확정
- Test Strategy 확정

---

# 15. PHASE 01 — DtoB Desktop Shell

## 목적

실제로 실행 가능한 DtoB 프로그램을 만든다.

---

## 01A — Application Shell

```text
DtoB.exe
```

실행.

---

## 01B — Main UI

```text
┌──────────────────────────────────────────┐
│ DtoB                                     │
├──────────────────────────────────────────┤
│                                          │
│             + New Project                │
│                                          │
│             Recent Projects              │
│                                          │
└──────────────────────────────────────────┘
```

---

## 01C — Project Management

- New
- Open
- Save
- Save As
- Close
- Recent Projects

---

## 01D — Settings

- Working Folder
- Revit Version
- Cache
- Language
- Log Level

---

## 01E — Logging

```text
INFO
WARNING
ERROR
FATAL
```

---

## Exit Criteria

```text
DtoB 실행
→ Project 생성
→ 저장
→ 종료
→ 다시 열기
```

가능.

---

# 16. PHASE 02 — Project / DWG Upload

## 목적

실제 CAD 파일을 DtoB 프로젝트 안으로 가져온다.

---

## 02A — Upload

- File Dialog
- Drag & Drop

---

## 02B — Validation

- Extension
- File Size
- Hash
- Version
- Unit
- Corruption

---

## 02C — Drawing Management

```text
Project
 └── Drawings
     ├── Architectural.dwg
     ├── Section.dwg
     └── Elevation.dwg
```

---

## 02D — Version 구조

향후:

```text
Drawing V1
Drawing V2
```

를 지원할 수 있도록 ID와 Metadata를 설계한다.

---

## Exit Criteria

사용자가 DtoB에서 DWG를 추가/삭제/관리할 수 있다.

---

# 17. PHASE 03 — Native DXF Data Extraction Foundation

## 목적 / 현재 Gate

**DtoB V1 uses Native Text DXF extraction.** First authoritative fixture: controlled AC1032 Text DXF. Native lexical/section/entity parser extracts factual source data into owned RawDrawingSnapshot without ACadSharp/ODA/RealDWG/AutoCAD/Revit/WPF dependency. Direct DWG is not V1; users externally export Text DXF. Other versions/encodings require explicit fixture-proven profiles.

Current owner authorization is final pre-implementation documentation housekeeping only; no parser/projects/Viewer/PHASE04 implementation, SDK install, commit or push. Architecture verdict APPROVED_FOR_NATIVE_DXF_IMPLEMENTATION; STOP before03A in this housekeeping turn. Historical ACadSharp spike failed and is NOT SELECTED; no repair/patch/fork/continued backend review/escalation. Prior review/spike/housekeeping evidence is preserved unchanged.

## 03A — DXF Lexical / Group-Code Reader

Strict supported text/profile decoding, raw code/value text, ordered pairs/code-value physical lines/source ranges, checked scalar parsing and located diagnostics. No irreversible text trimming/default fabrication; bounded resources/cancellation. Accepted Text DXF / AC1032 / strict UTF-8 profile naturally supports the ASCII subset and valid Korean/non-ASCII UTF-8, optional UTF-8 BOM, CRLF / LF; actual controlled ASCII-only fixture does not prove multilingual support.

## 03B — Sections / Header / Tables

Explicit SECTION/ENDSEC/EOF, HEADER/CLASSES/TABLES/BLOCKS/ENTITIES/OBJECTS, TABLE/ENDTAB and unknown dispositions. Minimum ACADVER/INSUNITS/EXTMIN/EXTMAX; LAYER/LTYPE/STYLE/DIMSTYLE/BLOCK_RECORD raw names/flags/colors/handles/refs. Header declarations/extents are evidence, never automatic normalization or reliable bounds when invalid/unusable.

## 03C — Geometry Entity Extraction

Required LINE/LWPOLYLINE/ARC/POINT/SOLID source coordinates/order/bulges/widths/normal/elevation; CIRCLE simple planned mapping only after real fixture test; separate POLYLINE/VERTEX/SEQEND legacy sequence/subtype coverage. ELLIPSE/SPLINE/HATCH initially raw unsupported/later investigation, no line approximation or repair.

## 03D — Blocks / INSERT / Annotation Extraction

BLOCK_RECORD/BLOCK/ENDBLK identities/base point/flags/children/source ranges versus INSERT raw target/position/scale/source-degree rotation/normal/arrays/attribute-follow. TEXT/MTEXT original content/chunks/control facts; DIMENSION raw defining points/text/style/block refs, no Measurement calculation; ATTRIB/ATTDEF investigation. No PHASE03 local/final/nested INSERT transform.

MINSERT expansion remains V1 UNSUPPORTED: original handle + row/column/spacing/raw insertion facts, source-linked UnsupportedOccurrenceExpansion and safe continuation. No cells/IDs/flatten/Explode or ADR007 change.

## 03E — Handle / Owner / Source Provenance

DrawingId/RevisionId/FileHash -> original lexical/numeric DXF handle; contextual owner/reference/membership, section/object/pair/line provenance. DIMSTYLE105 and HEADER/control/reactor exceptions explicit. Missing/malformed/duplicate handles diagnosed, never replaced. Bounded deferred reference validation, not occurrence expansion.

## 03F — RawDrawingSnapshot Assembly

Owned Header/Tables/Blocks/Spaces/Entities/Objects/Diagnostics/SourceEvidence and completeness/disposition counts; no vendor objects. Derived extraction/QA export, not .dtob/project truth, normalized CAD IR or Revit data. ADR015 boundary/native amendment and AcceptedADR016; Core mm/CAD IRv1/projectv2 retained.

## 03G — Extraction QA / Deterministic Validation

Raw pairs/section/record/type-by-source-scope/handle/layer/block/INSERT/text/geometry/reference/disposition counts, hand-reviewed source-line goldens, unchanged original hash, repeat deterministic output. Prior independent DXF whole-file inventory is reference only, not ModelSpace/semantic truth or a forced output. Malformed/unknown/resource/cancel/stale/immutable-read and prior regressions required.

## 03H — Minimal Diagnostic Viewer

Secondary snapshot-only basic native-frame display/Zoom/Pan/Fit/Layer On-Off/click -> Handle/Type/Layer/raw/source line. Local block-definition view and raw INSERT markers avoid PHASE04 transformation leakage. Unsupported frames/rendering explicit. No second DXF parser, OCS/mm/effective-style engine, AutoCAD graphics/SHX/plot/dimension fidelity requirement.

## Exit Criteria

Actual controlled AC1032 source-preserving parse; all record dispositions/counts; required factual geometry/annotation/table/block/space data with original handles/owners/line evidence; explicit absent/invalid/default/unsupported facts; no PHASE03 transformation/normalization; MINSERT safe metadata/diagnostic/no cells; owned deterministic snapshot/QA; strict managed-only verified read with unchanged project/bytes and zero normal Open/Refresh content/hash/sourceIO; measured bounded limits/lifecycle; minimal diagnostic inspection; builds/tests/manual/hash/diff/report/independent review.

Native implementation compatibility is unverified until implemented/tests pass. Full checklist/spec are authoritative detailed requirements. Current design revision is not a phase PASS and no PHASE04 begins.

---
# 18. PHASE 04 — CAD Data Engine

## 목적과 경계

```text
RawDrawingSnapshot
-> units/frame/OCS-WCS normalization
-> effective-data policy -> cleanup / analysis structures
-> normalized CAD IR
```

PHASE 04는 PHASE 03 Native DXF raw 사실을 분석 가능한 mm CAD IR로 구조화한다. Source file을 별도 vendor reader로 중복 해석하지 않고 RawDrawingSnapshot을 소비한다. Existing IDrawingReader -> DrawingReadResult -> CadDocument 및 ADR-006/008의 normalized mm/strict v1 semantics 유지. Unknown units는 명시적 resolve 전 valid normalized IR로 둔갑시키지 않는다.

## 04A — Primitive Mapping & Coverage

Raw primitives를 검증된 typed IR로 map한다. CAD IR v1 coverage 밖의 POINT/SOLID/ELLIPSE/SPLINE/HATCH는 unsupported 정책 또는 별도 ADR/schema decision 이후 처리한다. Strict schema를 silently 확장하지 않는다.

## 04B — Annotation Mapping

Raw TEXT/MTEXT/DIMENSION과 supported annotation facts를 map하고 stored/derived evidence를 분리한다. LEADER/MLEADER/TABLE은 실제 raw coverage/schema review 후 처리한다.

## 04C — Layer / Style Policy

Original layer/color/linetype/lineweight evidence와 normalized representation을 보존한다. Missing/default source facts를 fabricated values로 채우지 않는다.

## 04D — Definition / Occurrence Assembly

Raw DXF block/INSERT/attribute/source-degree rotation/scale/mirror/normal/reference facts에서 analysis occurrence structure를 조립한다. PHASE04가 explicit default/radian resolution과 local/final/nested transform composition을 처음 수행하고 definition-local geometry/accumulated transforms를 검증한다. MINSERT metadata/unsupported diagnostic 유지; synthetic cell ID 금지.

## 04E — Effective Data Policy

Raw layer/INSERT layer/owner block/root INSERT와 ByLayer/ByBlock occurrence-context inheritance를 explicit하게 결정한다. Unresolved 값은 알려진 값처럼 표시하지 않는다.

## 04F — Coordinate Normalization

Source units/degree angles와 OCS/WCS/definition/model frames를 explicit scale/tolerance로 mm/radians에 정규화한다. Raw source-default absence는 provenance를 가진 explicit policy로 resolve한다. Source evidence와 original Handle 유지, double scaling/implicit recentering 금지. Core geometry convention/schema는 ADR 없이 바꾸지 않는다.

## 04G — Geometry Cleanup

Zero-length/exact duplicate 등 최소 deterministic cleanup. Raw snapshot을 수정하지 않고 lineage/exclusion 이유를 유지한다.

## 04H — Analysis Structures / Spatial Index

Normalized point/line/bounds/layer 질의 제공. Diagnostic Viewer pick data는 분석 truth가 아니다.

## 04I — CAD IR Validation / Serialization

Strict schema/structure/geometry/ownership 검증과 JSON roundtrip, raw->normalized trace. Major schema change는 명시적 ADR 필요.

## Exit Criteria

Validated raw snapshot 소비; explicit units/frame normalization/effective data; source Handle/layer/block/annotation trace; supported CAD IR coverage/unsupported errors; cleanup/analysis structures; strict CAD IR serialization. PHASE 05+가 drawing intelligence/semantic recognition을 수행한다. 현재 Native DXF design revision에서는 PHASE04를 시작하지 않는다.

---

# 19. PHASE 05 — Drawing Region / View Detection

## 목적

한 DWG 안의 평면/단면/입면 영역을 구분한다.

---

## 05A — Region Detection

예:

```text
1F PLAN
2F PLAN
SECTION A-A
FRONT ELEVATION
```

---

## 05B — Title Detection

- 1F FLOOR PLAN
- 2F FLOOR PLAN
- SECTION A-A
- FRONT ELEVATION

---

## 05C — View Classification

```text
PLAN
SECTION
ELEVATION
DETAIL
SCHEDULE
UNKNOWN
```

---

## 05D — Orientation

초기에는 정방향 도면을 전제로 한다.

향후 회전 도면 지원.

---

## 05E — Manual Correction

잘못 인식된 Region을 사용자가 수정 가능.

---

## Exit Criteria

Viewer에서 각 영역이 Label과 Boundary로 표시.

---

# 20. PHASE 06 — Grid / Wall / Column Recognition

---

# 20.1 PHASE 06A — Grid

초기 입력 규칙:

```text
Layer = A-GRID
```

분석:

- Grid Line
- Grid Text
- Grid Intersection

출력:

```text
A
B
C

1
2
3
```

---

# 20.2 PHASE 06B — Wall

초기에는 Layer를 강하게 사용한다.

---

## 06B-1 Candidate Filter

```text
Layer = A-WALL
```

---

## 06B-2 Parallel Boundary Detection

A-WALL Layer의 두 평행선을 분석.

---

## 06B-3 Thickness

예:

```text
150mm
200mm
```

---

## 06B-4 Centerline

두 Wall Boundary의 중심선 생성.

---

## 06B-5 Junction

```text
L
T
X
```

기본 연결 처리.

---

## 06B-6 Wall Segment

BIM 변환을 위한 Wall Segment 생성.

---

## Wall Candidate 예

```json
{
  "category": "Wall",
  "thickness": 200,
  "centerline": [],
  "source_entities": []
}
```

---

# 20.3 PHASE 06C — Column

초기:

```text
Layer = S-COLUMN
Block = COLUMN_400
```

활용.

추출:

- Position
- Rotation
- Size

---

## Exit Criteria

Viewer에:

```text
Grid
Wall
Column
```

Overlay 가능.

---

# 21. PHASE 07 — Door / Window / Opening / Room / Floor

---

## 07A — Door

초기:

```text
Layer = A-DOOR
Block = D_SINGLE_900
```

추출:

- Position
- Rotation
- Width
- Host Wall Candidate

---

## 07B — Window

초기:

```text
Layer = A-WINDOW
Block = W_SINGLE_1200
```

추출:

- Position
- Rotation
- Width
- Host Wall

---

## 07C — Opening

Door/Window 외 개구부 확장 가능 구조만 준비.

---

## 07D — Room

초기에는 Closed Boundary 기반.

---

## 07E — Floor

초기:

```text
Layer = A-FLOOR
```

Boundary 활용.

---

## Exit Criteria

2D Plan의 주요 건축 객체가 모두 BIM Candidate 상태로 존재.

---

# 22. PHASE 08 — Text / Dimension / Metadata Intelligence

초기 MVP에서는 보조 역할부터 시작.

---

## 08A — Text Reading

예:

```text
1F
2F
D1
W1
ROOM A
ROOM B
```

---

## 08B — Dimension Reading

예:

```text
8000
6000
3000
900
1200
```

---

## 08C — Level Text

```text
1F +0
2F +3000
RF +6000
```

---

## 08D — Object Association

예:

```text
W1 1200x1500
↓
Window Candidate
```

---

## 08E — Metadata 보완

Layer / Block만으로 부족한 정보는
Text / Dimension을 통해 보완.

---

# 23. PHASE 09 — Plan + Section + Elevation → 3D

DtoB 핵심 R&D Phase.

여러 Subphase로 나눈다.

---

## 09A — Level Recognition

단면/입면에서:

```text
1F +0
2F +3000
RF +6000
```

추출.

---

## 09B — Level Structure

```text
Level 1 = 0
Level 2 = 3000
Roof = 6000
```

---

## 09C — Section Marker

평면의:

```text
A-A
```

와 SECTION A-A 연결.

---

## 09D — Coordinate Alignment

초기 Anchor:

```text
Grid
+
Section Marker
```

---

## 09E — Object Correspondence

예:

```text
Plan W1
↔
Elevation W1
```

---

## 09F — Vertical Information

추출:

- Wall Height
- Door Height
- Window Height
- Window Sill
- Slab Height
- Level

---

## 09G — 3D Assembly

Plan:

```text
X
Y
Width
```

Section/Elevation:

```text
Z
Height
Sill
```

결합.

---

## Exit Criteria

Revit 없이도 DtoB 내부에
완전한 3D Logical BIM Model이 존재.

---

# 24. PHASE 10 — BIM IR + 3D Preview

## 목적

Revit으로 보내기 전 DtoB 안에서 BIM 결과 확인.

---

## BIM IR 예

```json
{
  "id": "wall-001",
  "category": "Wall",
  "level": "1F",
  "geometry": {
    "centerline": [],
    "height": 3000,
    "thickness": 200
  },
  "source": []
}
```

---

## 3D Viewer 기능

- Orbit
- Pan
- Zoom
- Level Toggle
- Category Toggle
- Object Select
- Property

---

## Exit Criteria

DtoB 자체가 기본 BIM Viewer 역할 수행.

---

# 25. PHASE 11 — Review Workbench

Review는 각 Phase에서 최소 기능을 먼저 넣는다.

PHASE 11에서 통합 완성한다.

---

## 화면 구성

```text
Project Tree
2D CAD Viewer
3D BIM Viewer
Object Table
Property
Source
Issues
```

---

## 사용자 수정

- Category
- Position
- Geometry
- Level
- Height
- Thickness
- Family Mapping
- Ignore
- Confirm

---

# 26. PHASE 12 — Revit Add-in Foundation

## 목적

Revit에서 DtoB Connector 실행.

---

## Ribbon 예

```text
DtoB
 ├─ Connect
 ├─ Scan Families
 └─ Generate BIM
```

---

## 통신

```text
DtoB Desktop
↔
DtoB Revit Add-in
```

---

## Exit Criteria

Desktop ↔ Revit 프로젝트 정보 및 명령 송수신 성공.

---

# 27. PHASE 13 — Revit Family Catalog

## 목적

Revit Template의 사용 가능한 BIM Type 정보를 DtoB로 가져온다.

---

## 수집 대상

- Wall Types
- Floor Types
- Family
- Family Type
- Category
- Width
- Height
- Parameters
- Host

---

## DtoB UI 예

```text
Template:
DtoB_Test_Template.rte

Wall Types      12
Doors            8
Windows          6
Columns          5
Floors           4
```

---

# 28. PHASE 14 — Family / Type Matching

초기 Controlled MVP에서는 Matching을 단순하게 한다.

예:

```text
DtoB Object

Door
Width = 900
Height = 2100
```

Revit:

```text
D_SINGLE
900x2100
```

자동 매칭.

---

## Manual Override

사용자가 Mapping 수정 가능.

---

# 29. PHASE 15 — Revit BIM Generation

## 생성 순서

```text
1. Level
2. Grid
3. Wall
4. Column
5. Floor
6. Door
7. Window
8. Room
9. Parameter
```

---

## Native Revit 원칙

결과는 가능한 한:

```text
Native Revit Wall
Native Revit Floor
Native Revit FamilyInstance
```

이어야 한다.

DirectShape / Mesh로 끝내지 않는다.

---

## Traceability

가능하면 다음 Parameter 기록.

```text
DTB_SourceDrawing
DTB_SourceHandle
DTB_BimIrId
DTB_GeneratedAt
```

---

# 30. PHASE 16 — QA / V1 Completion

## 비교 항목

- Wall Count
- Door Count
- Window Count
- Column Count
- Floor Count
- Level Count

---

## Geometry QA

- Wall centerline deviation
- Door position deviation
- Window position deviation
- Column centroid deviation

---

## Failure Report 예

```text
Door D-03 생성 실패

Reason:
Host Wall을 찾지 못함.

Source:
Layer: A-DOOR
Handle: 31FA
```

---

# 31. DtoB V1 공식 성공 조건

- [ ] DtoB Desktop 실행
- [ ] Project 생성
- [ ] Text DXF Import (DWG는 사용자 external export)
- [ ] Raw CAD extraction / factual QA
- [ ] 최소 diagnostic CAD inspection
- [ ] Unit/frame-normalized CAD IR
- [ ] Layer 읽기
- [ ] Color 읽기
- [ ] Block 읽기
- [ ] Entity 읽기
- [ ] 1F / 2F / Section / Elevation 구분
- [ ] Grid 인식
- [ ] Wall 인식
- [ ] Column 인식
- [ ] Door 인식
- [ ] Window 인식
- [ ] Floor 인식
- [ ] Level 인식
- [ ] 3D Logical BIM 생성
- [ ] DtoB 3D Preview
- [ ] Review
- [ ] Revit 연결
- [ ] Revit Family Catalog
- [ ] Family Mapping
- [ ] Native Revit 생성
- [ ] RVT 저장
- [ ] QA Report

---

# 32. 최초 End-to-End Test Drawing

DtoB V1 최초 검증 입력은 AC1032 Text DXF이며 DWG 사용자는 외부 Save As/Export DXF를 수행한다. 검증 도면은
실제 복잡한 현장 도면이 아니라
**Controlled 2층 테스트 도면**을 사용한다.

---

## Building

```text
Size:
8000 x 6000 mm

Floors:
2

Floor-to-floor:
3000 mm
```

---

## Elements

```text
Exterior Wall = 200
Interior Wall = 150
Column = 400x400
Door = 900x2100
Window = 1200x1500
```

---

## Views

```text
1F FLOOR PLAN
2F FLOOR PLAN
SECTION A-A
FRONT ELEVATION
```

---

## Layers

```text
A-WALL
A-DOOR
A-WINDOW
A-FLOOR
S-COLUMN
A-GRID
A-TEXT
A-DIM
A-LEVEL
A-SECTION
```

---

## Blocks

```text
COLUMN_400
D_SINGLE_900
W_SINGLE_1200
```

---

# 33. 최초 테스트에서 의도적으로 제외

- Layer 0 혼용
- 잘못된 Layer
- Xref
- Dynamic Block
- Complex Nested Block
- Curve Wall
- Irregular Wall
- Overlapping Geometry
- Bad CAD
- Scan PDF
- Image Drawing
- Company-specific Layer Rule

---

# 34. 최초 Test 성공 정의

한 개의 Controlled 도면이 아래 전체 흐름을 통과한다.

```text
Text DXF (DWG -> user external export)
→ DtoB DXF Import
→ Native DXF Reader / RawDrawingSnapshot / factual QA / minimal diagnostic inspection
→ CAD Data Engine / normalized CAD IR
→ CAD Analysis
→ Object Recognition
→ BIM IR
→ 3D Preview
→ Revit Mapping
→ Revit Native Elements
→ RVT
```

이 단계가 안정적으로 성공하면
DtoB V1의 기술 기반이 성립한 것으로 본다.

---

# 35. Codex 역할

Codex = **Implementation Owner**

담당:

- Code
- Test
- Repository
- Desktop App
- CAD Engine
- Geometry
- BIM IR
- Viewer
- Revit Add-in
- Build
- CI
- Bug Fix

---

# 36. Antigravity 역할

Antigravity = **Architecture / Adversarial Review Owner**

담당:

- Architecture Review
- Code Review
- Geometry Edge Case
- CAD Edge Case
- Test Review
- Revit API Review
- Performance Review
- Failure Analysis
- Regression Risk

---

# 37. 개발 Workflow

```text
Phase Spec
    ↓
Codex
    ↓
Implementation
    ↓
Test
    ↓
PHASE_REPORT.md
    ↓
Antigravity
    ↓
PHASE_REVIEW.md
    ↓
Codex Fix
    ↓
Regression Test
    ↓
Phase PASS
    ↓
Next Phase
```

---

# 38. Phase 문서 표준 구조

앞으로 각 Phase는 다음 형식으로 작성한다.

```text
Purpose
Assumptions
Supported Scope
Not Supported Yet
Subphases
Implementation Tasks
UI Result
Test
Exit Criteria
Codex Instructions
Antigravity Review Focus
```

---

# 39. 개발 원칙

## 39.1 한 번에 한 Phase

다음 Phase를 미리 구현하지 않는다.

---

## 39.2 각 Phase가 끝날 때마다 실행 가능한 DtoB 유지

개발 도중에도 프로그램이 실행되어야 한다.

---

## 39.3 추출·분석 결과를 최소 진단 UI에서 검증

백엔드만 개발하고 UI를 마지막에 붙이지 않는다.

추출/분석 결과를 화면에서 검증하되 PHASE03 Native DXF에서는 source-pair/line factual QA가 우선이고 Viewer는 native-frame 최소 진단이다. INSERT/OCS/unit transformations는 PHASE04에만 둔다.

---

## 39.4 초기에는 AI보다 Deterministic 정보

초기 핵심:

```text
Layer
Color
Block
Geometry
```

---

## 39.5 Revit API는 Core에 침투하지 않는다

Revit은 Output Adapter다.

---

# 40. 초기 Repository 권장 구조

```text
DtoB/
│
├─ README.md
├─ START_HERE.md
├─ DtoB_FINAL_MASTER_PLAN.md
├─ AGENTS.md
│
├─ docs/
│
├─ tasks/
│   ├─ PHASE_00.md
│   ├─ PHASE_01.md
│   └─ ...
│
├─ apps/
│   └─ DtoB.Desktop/
│
├─ services/
│   └─ DtoB.Analysis/
│
├─ packages/
│   ├─ DtoB.Cad/
│   ├─ DtoB.Geometry/
│   └─ DtoB.Bim/
│
├─ revit/
│   ├─ DtoB.Revit.Core/
│   └─ DtoB.Revit2025/
│
├─ tests/
│
├─ datasets/
│   └─ controlled/
│
└─ reviews/
```

최종 구조는 PHASE 00에서 확정한다.

---

# 41. V1 이후 확장

## Stage 2 — Semi-Structured CAD

추가:

- 회사별 Layer Mapping
- 다양한 Layer Naming
- Block Variation
- Text Association 강화
- Dimension Validation

---

## Stage 3 — Unstructured CAD

추가:

- Geometry inference
- Negative Evidence
- Context Analysis
- AI
- Confidence
- Conflict Resolution
- User Correction Learning

---

## Stage 4 — Input Expansion

```text
PDF
Image
Scan
DXF
IFC
```

---

## Stage 5 — Output Expansion

```text
IFC
Speckle
Web BIM
Knowledge Graph
```

---

## Stage 6 — Text-to-BIM

예:

```text
"A축과 B축 사이에 200mm RC 벽을 만들고
Grid 1에서 1200 떨어진 위치에
900x2100 문을 설치해."
```

---

# 42. 장기 비전

DtoB의 최종 목표는 단순 CAD Converter가 아니다.

```text
Drawing
+
BIM
+
Construction Knowledge
+
AI
+
Ontology
```

를 연결하는 AEC Modeling Engine이다.

장기적으로:

```text
DWG → BIM
PDF → BIM
Image → BIM
Text → BIM
BIM → Knowledge Graph
Knowledge Graph → Automation
```

으로 발전한다.

---

# 43. 현재 실행 Gate 및 최초 Foundation 계획

2026-10-14 current owner direction: Native Text DXF reader architecture/proposal/ADRs를 갱신한 뒤 STOP before03A after final housekeeping; APPROVED_FOR_NATIVE_DXF_IMPLEMENTATION. Production parser/raw projects/Viewer/PHASE04/SDK install/commit/push 금지. ACadSharp NOT SELECTED; historical failed report/evidence preserved. ADR016 is Accepted.

아래 PHASE00 foundation 계획은 최초 개발 순서의 역사적 설명이며 완료된 phase를 다시 시작하라는 현재 지시가 아니다.

먼저 이 Final Master Plan을 기준으로
PHASE 00부터 상세 설계를 확정한다.

PHASE 00에서 반드시 결정할 것:

```text
Desktop 기술 Stack
Desktop UI Framework
Revit Add-in 기술 Stack
Desktop ↔ Revit 통신 방식
DWG Reader 후보
CAD IR 최소 Schema
BIM IR 최소 Schema
Repository 구조
Test 구조
```

그 후 `tasks/PHASE_00 ~ PHASE_16`을
이 Master Plan 기준으로 다시 작성하고
GitHub의 기존 Phase 문서를 한 번에 교체한다.

---

# 44. DtoB V1의 핵심 문장

> **DtoB V1의 목표는 복잡한 모든 CAD를 이해하는 것이 아니다.**
>
> **명확하게 정리된 2D CAD 도면 하나를 DtoB 독립 프로그램에서 분석하고, 3D BIM으로 검증한 뒤, Revit Add-in을 통해 Native Revit 모델로 끝까지 생성하는 것이다.**
>
> **이 첫 성공을 만든 뒤 DtoB의 인식 능력을 단계적으로 현실 세계 수준으로 확장한다.**

---

# END
## Final Native Text DXF pre-implementation housekeeping — 2026-10-14

Antigravity verdict: **APPROVED_FOR_NATIVE_DXF_IMPLEMENTATION**. Issues1–3 incorporated; **ADR-016 Accepted**, accepted ADR001–015 unchanged. Ready for subsequent PHASE03A; current turn STOP before parser/projects/Viewer/PHASE04 implementation, no commit/push.

Official profile: **Text DXF, AC1032, strict UTF-8**, ASCII subset naturally supported, optional UTF-8 BOM, **CRLF / LF**. Valid Korean/other non-ASCII UTF-8 is allowed, not forbidden by format terminology; actual acceptance tests remain required.

Authoritative source identity begins at **imported managed Text DXF**: **DrawingId + RevisionId + FileHash + Original DXF Handle**. OriginalHandle is the handle written in imported DXF, not an upstream DWG handle unless independently proven. DtoB guarantees no semantic/structural/identity equivalence and assumes no responsibility for third-party export changes to handles, anonymous blocks, dimensions, proxy/custom objects, unsupported records or database structure. DtoB remains responsible for extracting that managed DXF faithfully. No DWG/DXF reconciliation in PHASE03 or PHASE04.

Initial configurable/injectable development limits: physical source line **64 KiB (65,536 UTF-8 bytes)**, combined control nesting **16**, stored diagnostics **5,000** (reserve terminal slot), raw pairs **250,000**, input **16 MiB (16,777,216 bytes)**. Enforce before allocation; exceed -> **ResourceLimitExceeded**, never truncated-success. Smaller deterministic test profiles and later measured revisions permitted; no performance claim. Full semantics: ADR016 and checklist§20. Status: docs/status/PHASE_03_NATIVE_DXF_HOUSEKEEPING_REPORT.md.

# END
