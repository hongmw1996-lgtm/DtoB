# PHASE 04 — CAD Normalization Engine / CAD IR Foundation Architecture Plan Independent Review

## Executive Summary

- **Review Target**: PHASE 04 Architecture & Implementation Plan (`docs/plans/PHASE_04_IMPLEMENTATION_PLAN.md`, `docs/status/PHASE_04_PLANNING_REPORT.md`, `tasks/PHASE_04.md`)
- **Review Date**: 2026-10-15
- **Review Mode**: Independent Architecture Gate & Design Review
- **Reviewer**: Architecture / Adversarial Review Owner (Antigravity)
- **Status of Codex**: `REVIEW_REQUIRED` (Planning only, 0 production code changes)
- **Final Verdict**: **`PHASE_04_PLAN_PASS`**

DtoB 시스템의 텍스트 DXF 파이프라인(Phase 03)에서 추출된 불변의 사실 증거(`RawDrawingSnapshot`)를 입력으로 받아, 기하학적·공간적 의미를 명확히 정규화하고 발생 계통(Occurrence Lineage)과 유효 속성을 확립하는 **PHASE 04 CAD Normalization Engine / CAD IR Foundation** 아키텍처 계획서에 대해 독립적이고 엄격한 아키텍처 감사를 수행하였습니다.

PHASE 04는 특정 저작 도구(Revit)에 결합된 변환기가 아니라, 향후 도면 인식(PHASE 05+ Drawing Intelligence), BIM IR 합성, 그리고 다양한 다운스트림 어댑터(Revit, IFC, Speckle, Web 3D, Knowledge Graph 등)의 공통 기반이 될 **Revit 독립적인 순수 CAD 2D/3D 공간 의미 모델(CAD IR)**을 확립하는 가장 핵심적인 관문입니다.

독립 감사 결과 요약:
1. **Revit 완전 배제 및 파이프라인 위치 검증**: CAD IR 모델과 정규화 엔진 내에 Revit API, Revit XYZ, ElementId, BuiltInCategory, 패밀리/파라미터, 내부 피트 단위 등의 오염이 전혀 없으며, `RawDrawingSnapshot` $\rightarrow$ `CAD Normalization Engine` $\rightarrow$ `CAD IR` $\rightarrow$ `Recognition (PHASE 05+)` $\rightarrow$ `BIM IR` $\rightarrow$ `Revit/IFC Adapters`의 대원칙이 완벽히 준수되었습니다.
2. **원시 증거(Raw)와 정규화(Normalized)의 엄격한 분리**: Raw 패키지(`DtoB.Cad.Raw`, `DtoB.Cad.Dxf`)는 단 1바이트의 수정도 없이 불변으로 유지되며, Raw 데이터("DXF가 기록한 원본 텍스트/그룹코드 사실")와 정규화 데이터("해당 사실의 WCS mm/라디안 공간적 의미")의 경계가 확고하게 분리되었습니다.
3. **엄밀한 수학적 변환 정의**: OCS $\rightarrow$ WCS Arbitrary Axis Algorithm(AAA), 블록 베이스 포인트 보정($T(-b)$), 회전, 비균일/음수 스케일, MINSERT 행/열 간격 배치 수식, LWPOLYLINE Bulge 수식 등이 Autodesk DXF 공식 사양과 일치하도록 수학적으로 완전히 명시되었습니다.
4. **결정론적 식별자 및 계통 추적성**: 난수 GUID나 타임스탬프를 배제하고 도메인 분리 SHA-256 해시 기반의 결정론적 ID를 정의하였으며, 모든 정규화된 기하 및 엔티티가 원본 DrawingId, RevisionId, FileHash, Handle, 물리 행, 페어 인덱스까지 도달할 수 있는 역추적 구조를 설계하였습니다.
5. **체계적인 점진적 서브페이즈(04A~04H) 및 ADR 전략**: 8단계 서브페이즈가 선행 의존성 없이 명확한 입출력과 테스트 게이트를 갖추고 있으며, 주요 설계 변경을 위한 ADR-018, ADR-019, ADR-020의 범위가 명확하게 식별되었습니다.

계획서에서 식별된 1건의 HIGH 이슈(Core의 `Contract.Version` 제약 및 Analysis 인터페이스 결합도)와 3건의 MEDIUM 이슈는 04A 착수 전 승인될 ADR-018 및 ADR-020을 통해 명문화하여 통제할 수 있는 설계 과제이므로, 본 계획서는 프로덕션 설계를 진행하기에 충분히 견고하고 성숙한 것으로 판단하여 **`PHASE_04_PLAN_PASS`**를 부여합니다.

---

## 1. Core Architectural Questions Independent Audit (Questions 1 ~ 30)

### Q1. CAD IR Position in the Full DtoB Pipeline
- **평가**: **승인 (APPROVED)**
- **분석**:
  - 제안된 파이프라인: `Text DXF` $\rightarrow$ `RawDrawingSnapshot` $\rightarrow$ `CAD Normalization Engine` $\rightarrow$ `CAD IR (CadDocument)` $\rightarrow$ `Drawing Intelligence / Recognition (PHASE 05+)` $\rightarrow$ `BIM IR` $\rightarrow$ `Adapters (Revit, IFC, Speckle, Web, KG)`.
  - CAD IR 내부에는 Revit 종속적인 개념(`Autodesk.Revit.DB.XYZ`, `ElementId`, `BuiltInCategory`, `FamilySymbol`, `WallType`, 내부 피트 단위)이 완전히 배제되어 있습니다.
  - CAD IR은 순수하게 정규화된 2D/3D 기하, 레이어/스타일, 블록 인스턴스 발생 계통, 텍스트/치수 앵커만을 보존하는 도메인 중립적 CAD 모델입니다.

### Q2. Raw vs Normalized Responsibility Separation
- **평가**: **승인 (APPROVED)**
- **분석**:
  - **Raw (PHASE 03)**: "DXF가 실제로 기술한 내용 (What DXF said)". 원본 그룹 코드 철자, 90도 각도, OCS 좌표, 미해석 INSERT 앵커, 미지원 엔티티 등의 원시 증거를 불변으로 보존.
  - **Normalized (PHASE 04)**: "그 원본 사실이 공간적으로 의미하는 바 (What that source fact means spatially)". 정규화된 WCS mm 좌표, $\pi/2$ 라디안 각도, 베이스 포인트가 보정된 발생 인스턴스 행렬, 합성된 선/호 세그먼트, 계통이 확립된 유효 스타일.
  - Raw 계약은 절대 수정되지 않으며, 정규화가 실패하거나 미지원되더라도 Raw 사실을 변조하거나 감추지 않고 Ledger/Diagnostic에 정직하게 기록합니다.

### Q3. CAD v2 Strategy & Coexistence
- **평가**: **조건부 승인 (APPROVED WITH HIGH FINDING H-01)**
- **분석**:
  - Codex는 기존 `DtoB.Cad` 패키지 내부에 `DtoB.Cad.V2` 네임스페이스를 신설하여 additive v2 DTO를 추가하고, 변환 로직은 `DtoB.Cad.Normalize` 패키지로 분리하는 방안을 제안하였습니다.
  - 이는 기존 v1을 급격히 파괴하여 BIM, Analysis, 회귀 테스트 전반이 깨지는 위험을 방지하는 실용적인 접근입니다.
  - 다만, `DtoB.Core.Contract.Version`이 `major == 1`만을 허용하도록 하드코딩되어 있으므로, V2 직렬화/역직렬화 및 검증 시 Core의 정적 유틸리티와의 충돌을 피하기 위해 `CadIrJson` v2 전용 코덱을 엄격히 분리해야 합니다 (상세 내용은 Finding H-01 참조).

### Q4. Package Dependency Direction
- **평가**: **승인 (APPROVED)**
- **분석**:
  - 의존성 흐름: `DtoB.Cad.Raw` $\leftarrow$ `DtoB.Cad.Normalize` $\rightarrow$ `DtoB.Cad (V2)` $\rightarrow$ `DtoB.Geometry` / `DtoB.Core`.
  - `DtoB.Cad.Raw`와 `DtoB.Cad.Dxf`는 `DtoB.Cad.Normalize`를 절대 참조하지 않습니다.
  - `DtoB.Cad.Normalize`는 Revit, WPF, Desktop, UI를 일절 참조하지 않는 순수 .NET 8 클래스 라이브러리로 구성됩니다.
  - 가장 작고 깨끗한 단방향 의존성 그래프가 수립되었습니다.

### Q5. Canonical Coordinate Model
- **평가**: **승인 (APPROVED)**
- **분석**:
  - 길이 단위: 밀리미터 (mm)
  - 각도 단위: 라디안 (radians)
  - 좌표계: 오른손 좌표계 (Right-handed XYZ), $+Z$ up, 반시계 방향(CCW) 양수 회전.
  - 기존 `DtoB.Geometry`의 `Point3`, `Segment3`, `Transform3`, `GeometryTolerance`를 온전히 재사용하며, 중복된 벡터/행렬 타입을 추가로 생성하지 않습니다.
  - `Transform3`는 열벡터에 작용하는 행-우선(Row-major) $4\times4$ 아핀 행렬이며, `inner.Then(outer)`는 `outer * inner`의 합성 의미를 가집니다.

### Q6. Unit Normalization ($INSUNITS)
- **평가**: **승인 (APPROVED)**
- **분석**:
  - 통제 도면($INSUNITS = 4$)은 mm 단위로서 scale factor 1.0으로 정밀 정규화됩니다.
  - 지원 코드: 1 (inch, 25.4), 2 (foot, 304.8), 4 (mm, 1.0), 5 (cm, 10.0), 6 (meter, 1000.0).
  - 0 (Unitless) 또는 미기재(absent)의 경우, 임의로 mm를 가정하거나 도면 크기/파일명에서 추측하지 않고 `NeedsExplicitUnits` 상태로 명시적 거부합니다.
  - 블록 정의 단위(BLOCK_RECORD code 70)가 도면 단위와 모순되는 경우, 검증되지 않은 자동 스케일링을 배제하고 안전하게 거부합니다.

### Q7. Angle Normalization & MTEXT Radians Exception
- **평가**: **승인 (APPROVED)**
- **분석**:
  - 도면 내 60분법 각도(degrees)는 오직 PHASE 04 정규화 단계에서만 라디안으로 변환됩니다.
  - 회전각 및 호 시작각은 $[0, 2\pi)$ 범위로 정규화되며, 호(ARC)는 항상 양의 반시계 방향 스윕(CCW sweep)을 저장합니다.
  - 중요한 DXF 예외: `MTEXT`의 회전각(Group Code 50)은 DXF 사양상 이미 라디안으로 저장되어 있음을 계획서가 정확히 짚어내었으며, 이를 degree로 오인하여 이중 변환하지 않도록 필드 역할 기반(Field-Role) 변환표를 수립하였습니다.

### Q8. OCS $\rightarrow$ WCS & Arbitrary Axis Algorithm (AAA)
- **평가**: **승인 (APPROVED)**
- **분석**:
  - Autodesk DXF 공식 사양서의 Arbitrary Axis Algorithm을 정확히 채택하였습니다:
    $|N_x| < 1/64$ 및 $|N_y| < 1/64$일 때 $A_x = \text{normalize}(\text{WorldY} \times N)$, 그 외의 경우 $A_x = \text{normalize}(\text{WorldZ} \times N)$, $A_y = N \times A_x$.
  - 기저 행렬 $A = [A_x \ A_y \ N]$을 열벡터로 구성하여 점 변환($p_{WCS} = A \cdot p_{OCS}$) 및 벡터 변환($v_{WCS} = A \cdot v_{OCS}$)을 수행합니다.
  - 엔티티별 좌표계 구분이 명확합니다:
    - WCS 필드: `LINE`, `POINT`, `MTEXT` 삽입점/방향, `DIMENSION` 정의점(10/20/30) 및 확장점(13/23/33, 14/24/34).
    - OCS 필드: `LWPOLYLINE` (x, y, elevation), `CIRCLE`, `ARC`, `SOLID`, `TEXT`, `INSERT`, `DIMENSION` 텍스트 중점(11/21/31).
  - Elevation은 OCS z축에 단 한 번만 더해진 후 기저 변환됩니다.

### Q9. BLOCK / INSERT Transform Mathematics
- **평가**: **승인 (APPROVED)**
- **분석**:
  - 변환 합성 수식:
    $$L = A \cdot T(i) \cdot R_z(\theta) \cdot S(s_x, s_y, s_z) \cdot T(-b)$$
    $$q_{\text{parent}} = L \cdot q$$
    $$M_{\text{child}} = M_{\text{parent}} \cdot L_{\text{child}} \quad (\text{코드상 } L_{\text{child}}.\text{Then}(M_{\text{parent}}))$$
    $$q_{\text{model}} = \text{SourceToModel} \cdot M_{\text{child}} \cdot q$$
  - 블록 베이스 포인트 보정($T(-b)$)이 스케일 $S$ 및 회전 $R_z$보다 먼저 점 좌표에 작용하도록 우측에 배치되어, 베이스 포인트를 원점으로 정렬한 후 변환이 이루어집니다.
  - INSERT의 OCS 삽입점 $i$와 회전각 $\theta$는 INSERT 자신의 OCS 상에 존재하므로, $T(i) \cdot R_z \cdot S \cdot T(-b)$ 전체에 OCS 기저 행렬 $A$가 좌측에서 곱해지는 수학적 순서가 DXF 규격과 완벽히 일치합니다.

### Q10. Nonuniform & Negative Scale Handling
- **평가**: **조건부 승인 (APPROVED WITH MEDIUM FINDING M-02)**
- **분석**:
  - 비균일 스케일($s_x \neq s_y$) 적용 시 원이나 호가 타원으로 왜곡되는 문제에 대해, 가짜 반경(Circle)으로 축소 왜곡하거나 임의의 타원 곡선으로 피팅 근사하지 않고, "로컬 원/호 기하 + 누적 아핀 변환 행렬"을 정직하게 보존하는 아키텍처 결정을 내렸습니다.
  - 음수 스케일(미러링) 시 winding 반전 처리가 필요하며, 외접 경계 상자(AABB) 계산에 대한 명확한 수학적 처리가 보완되어야 합니다.

### Q11. Nested INSERT Occurrence Model
- **평가**: **승인 (APPROVED)**
- **분석**:
  - 블록 정의(Definition)는 공유되고, 각 인스턴스는 발생 인스턴스(Occurrence)로 보존됩니다.
  - 형상(Geometry)을 익명으로 평탄화(Flattening)하여 소스 식별성을 잃어버리는 일이 없도록, 최상위 INSERT부터 말단까지의 계통 경로(Typed Occurrence Path)와 로컬/누적 행렬을 각각 관리합니다.
  - 재귀적 순환 참조(Direct/Indirect Block Cycle)를 방어하기 위한 활성 경로(Active-Path) 주기 감지 및 최대 깊이 제한(Depth limit = 32)이 계획되었습니다.

### Q12. MINSERT Array Expansion
- **평가**: **승인 (APPROVED)**
- **분석**:
  - 행/열 순서: 행 우선(Row-major, 0-based), 순서 번호 $r \times \text{cols} + c$.
  - 변환 수식:
    $$d(r, c) = (c \cdot \text{colSpacingMm}, \ r \cdot \text{rowSpacingMm}, \ 0)$$
    $$L_{rc} = A \cdot T(i) \cdot R_z(\theta) \cdot T(d(r, c)) \cdot S \cdot T(-b)$$
  - AutoCAD DXF 사양에 따라 행/열 간격 $d(r, c)$는 회전 각도 $\theta$를 따라 회전하지만, 블록 자체의 스케일 $S$에는 곱해지지 않는다는 사실을 정확히 반영하였습니다.
  - 가짜 DXF 핸들을 만들지 않고 실제 소스 INSERT 핸들과 $(r, c)$ 튜플로 식별성을 부여합니다.

### Q13. Normalized Identities
- **평가**: **승인 (APPROVED)**
- **분석**:
  - 난수 GUID, 머신 경로, 타임스탬프, 부동소수점 기하 해시를 일절 사용하지 않습니다.
  - 도메인 분리 SHA-256 디제스트 기반의 정규화 ID(`cad2:<digest>`) 체계를 수립하였습니다.
  - 튜플 구성: DrawingId + RevisionId + FileHash + Canonical Handle + Typed Path (INSERT 체인 및 row/col).
  - 다회 실행 시 100% 동일한 ID가 재현되는 결정론적 안정성을 확보하였습니다.

### Q14. Provenance & Traceability
- **평가**: **승인 (APPROVED)**
- **분석**:
  - 모든 정규화된 기하 및 엔티티가 원본 `DrawingId`, `RevisionId`, `FileHash`, `OriginalHandle`, `CanonicalHandle`, `SourceRecordRef`, `SourceFactRef`를 참조합니다.
  - 메모리 폭발을 방지하기 위해 거대한 Raw 객체를 매 인스턴스마다 복제하지 않고, 문서 단위의 컴팩트 레지스트리(Registry)와 인덱스 참조 방식을 채택하였습니다.

### Q15. Style Resolution (Layer / Color / Linetype)
- **평가**: **승인 (APPROVED)**
- **분석**:
  - 레이어 0 규칙: 블록 정의 내부의 레이어 0 엔티티는 해당 블록을 참조한 부모 INSERT의 유효 레이어를 상속받음.
  - ByLayer / ByBlock 규칙: 명시적 값 $\rightarrow$ ByLayer $\rightarrow$ ByBlock 계통을 재귀적으로 해결하며, 최상위에서 미해소된 ByBlock은 임의의 ACI 7(흰색)로 날조하지 않고 `Unsupported/Unresolved` 상태로 정직하게 기록합니다.
  - 색상 우선순위: 420(TrueColor RGB)이 62(ACI)보다 우선함을 명시하였습니다.

### Q16. LWPOLYLINE Bulge Mathematics
- **평가**: **승인 (APPROVED)**
- **분석**:
  - 수식: 스윕각 $\alpha = 4 \arctan(b)$, 반경 $R = \text{chord} \cdot (|b| + 1/|b|) / 4$, 중심 오프셋 $h = \text{chord} \cdot (1/b - b) / 4$.
  - 수학적으로 완전 무결하며, $b=0$ (직선), $b>0$ (CCW 호), $b<0$ (CW 호), $b=1$ (반원), 극소 Bulge 등에 대한 분기 처리가 올바르게 설계되었습니다.
  - 불필요한 다각형 테셀레이션(Tessellation)을 하지 않고 엄밀한 호 세그먼트로 변환합니다.

### Q17. Normalized Geometry Types
- **평가**: **승인 (APPROVED)**
- **분석**:
  - `DtoB.Geometry`의 기본 타입(`Point3`, `Segment3`)을 기초로 하되, CAD 전용의 평면, 선, 호, 원, 폴리라인 세그먼트, SOLID 평면 다각형을 정규화 타입으로 정의합니다.
  - 솔리드(SOLID) 엔티티의 DXF 정점 순서(1, 2, 4, 3의 나비 넥타이 꼬임 구조)를 올바른 평면 사각형/삼각형 순서로 재배열하는 디테일이 반영되었습니다.

### Q18. Degenerate Geometry Policy
- **평가**: **승인 (APPROVED)**
- **분석**:
  - 길이 0인 선, 반경 0인 원, 동일 정점으로 붕괴된 폴리라인 세그먼트 등을 조용히 삭제(Silent deletion)하지 않습니다.
  - Raw 관점에서는 유효하지만 정규화 관점에서는 붕괴된 형상으로 분류하여 Ledger에 `InvalidNormalization` 또는 `Degenerate` 상태와 진단 경고를 남깁니다.

### Q19. Error & Partial Document Policy
- **평가**: **승인 (APPROVED)**
- **분석**:
  - 부분 정규화(Partial Normalization) 지원: 하나의 불량 엔티티나 미지원 스타일 때문에 도면 전체의 유효한 기하까지 모두 거부되는 과도한 엄격성을 탈피하였습니다.
  - 단, 도면 전체의 전제 조건(단위 모순, 스키마 불일치, 전역 리소스 초과, 취소) 발생 시에는 명시적으로 문서를 발행하지 않고 거부합니다.
  - 정규화 상태를 `Normalized`, `PartiallyNormalized`, `UnsupportedNormalization`, `InvalidNormalization`으로 다차원 구분합니다.

### Q20. Central Numerical Policy & Tolerances
- **평가**: **승인 (APPROVED)**
- **분석**:
  - 세 가지 공차의 엄격한 분리:
    1. 소스 유효성 검사 (Finite, 유효 범위)
    2. 정규화 수치 비교 공차 (`GeometryTolerance`, 길이 mm, 각도 rad, 무차원 행렬 공차)
    3. 미래 인식 공차 (PHASE 05+의 벽체 결합, 스냅 공차 $\rightarrow$ PHASE 04에 절대 개입 금지)
  - 소스 좌표를 임의로 정수 스냅하거나 양자화(Quantization)하지 않습니다.

### Q21. Determinism
- **평가**: **승인 (APPROVED)**
- **분석**:
  - Invariant, `fr-FR`, `ko-KR` 3대 로케일 및 임의의 작업 디렉터리, 프로세스 환경에서 byte-identical 출력을 요구합니다.
  - 해시 맵의 비결정적 순회를 배제하고, 소스 레코드 인덱스 및 서수 기반의 엄격한 순서 정렬을 강제합니다.

### Q22. CAD Document Structure for Recognition
- **평가**: **승인 (APPROVED)**
- **분석**:
  - PHASE 05 인식 엔진이 필요로 하는 모델 공간 가시 기하 목록, 레이어별 기하 인덱스, 블록 정의 및 인스턴스 쿼리, 텍스트/치수 앵커 위치를 효율적으로 제공합니다.
  - 도면 인식용 구조를 지원하되, 벽(Wall), 문(Door), 기둥(Column)과 같은 BIM 의미론적 타입을 CAD IR에 일절 혼입하지 않았습니다.

### Q23. Downstream PHASE 05 Compatibility
- **평가**: **조건부 승인 (APPROVED WITH HIGH FINDING H-01)**
- **분석**:
  - 현재 `services/DtoB.Analysis`는 v1 `CadDocument`를 인수로 받는 더미 인터페이스(`IAnalysisEngine`)로 되어 있습니다.
  - PHASE 05 착수 시 v2 `CadDocument`를 소비하도록 Analysis의 서명을 변경하는 명시적 마이그레이션 경계가 설정되었습니다.

### Q24. Subphase Decomposition (04A ~ 04H)
- **평가**: **승인 (APPROVED)**
- **분석**:
  - 04A (Contracts & Boundary) $\rightarrow$ 04B (Units/Angles/Policy) $\rightarrow$ 04C (OCS/WCS) $\rightarrow$ 04D (Transform Engine) $\rightarrow$ 04E (Nested/MINSERT) $\rightarrow$ 04F (Styles/Provenance) $\rightarrow$ 04G (Geometry/Assembly) $\rightarrow$ 04H (Deterministic QA).
  - 단계 간 의존성이 자연스럽고, 각 서브페이즈마다 명확한 입출력, 제외 범위, 테스트 기준, ADR 요구사항이 정의되어 있습니다.

### Q25. ADR Strategy
- **평가**: **조건부 승인 (APPROVED WITH MEDIUM FINDING M-03)**
- **분석**:
  - ADR-018 (CAD IR Normalization Boundary), ADR-019 (Coordinate Normalization Transforms), ADR-020 (Occurrence Identity & Expansion) 3건의 ADR이 식별되었습니다.
  - 무분별한 ADR 남발 없이 핵심적인 아키텍처 경계, 기하 수학, 식별자 정책에 집중되어 있습니다.

### Q26. Test Fixture Strategy
- **평가**: **승인 (APPROVED)**
- **분석**:
  - 독립 수작업 계산 골든(Hand-calculated Goldens)을 사용하며, 자체 구현 코드로 계산한 값을 오라클로 사용하는 자가 오라클(Self-oracle)을 철저히 배제합니다.
  - 기존 86개 통제 픽스처 해시 및 QA 테스트의 무결성을 깨뜨리지 않도록, `phase04` 신규 픽스처 매니페스트를 분리하고 합집합(Union) 커버리지를 검증하는 안전한 확장 전략을 수립하였습니다.

### Q27. Resource & Expansion Safety
- **평가**: **승인 (APPROVED)**
- **분석**:
  - 중첩 깊이 상한: 32
  - MINSERT 단일 엔티티 셀 상한: 10,000
  - 총 발생 인스턴스 상한: 100,000
  - 파생 기하 상한: 250,000
  - 진단 레코드 상한: 5,000
  - 악의적인 폭탄 도면에 의한 메모리/CPU 고갈을 차단합니다.

### Q28. Cancellation Responsiveness
- **평가**: **승인 (APPROVED)**
- **분석**:
  - 단계 간 전환 시점뿐만 아니라, 엔티티 순회, 블록 트리 전개, MINSERT 격자 생성, 배치 배치 단위 등 CPU 루프 내부에서 `CancellationToken`을 주기적으로 확인하도록 설계되었습니다.

### Q29. Hard-Gate No Revit Leakage
- **평가**: **승인 (APPROVED)**
- **분석**:
  - 코드베이스 및 제안 계획서 전수 검사 결과, Revit 어셈블리 참조, 네임스페이스 참조, Revit 고유 타입이 전무함을 확인하였습니다.

### Q30. Measurable Exit Criteria
- **평가**: **승인 (APPROVED)**
- **분석**:
  - 13개 항목의 명확하고 측정 가능한 최종 수용 기준(Acceptance Criteria)이 정의되어 있어, 구현 완료 시 객관적 증거 기반으로 합격 여부를 판정할 수 있습니다.

---

## 2. Special Architecture Review — CAD IR $\rightarrow$ BIM $\rightarrow$ Revit

본 독립 검토는 DtoB의 장기 비전 및 다중 어댑터 전략에 따라 다음 원칙을 엄격하게 재확인합니다:

```text
[Managed Text DXF]
        ↓ (Phase 03 Native Extraction)
[RawDrawingSnapshot] (불변의 소스 증거)
        ↓ (Phase 04 Normalization Engine)
[CAD IR / CadDocument] (중립적 2D/3D CAD 공간 모델)
        ↓ (Phase 05+ Drawing Intelligence)
[BIM IR / BimDocument] (의미론적 건축 논리 모델: Wall, Door, Slab...)
        ↓ (Phase 08+ Downstream Output Adapters)
  ┌───────────────┬───────────────┬───────────────┐
[Revit Adapter]  [IFC Adapter]  [Speckle Adapter] [Web/KG...]
```

- **절대 불가 사항**: CAD IR에서 Revit API를 직접 호출하여 Revit 요소를 즉시 생성하는 지름길(Shortcut) 구현은 DtoB의 아키텍처 규칙 위반입니다.
- PHASE 04의 목표는 **어떤 특정 BIM 소프트웨어에도 종속되지 않는 고품질의 순수 CAD 기하/계통 정규화 모델**을 완성하는 것입니다. 본 계획서는 이 원칙을 정확하게 고수하고 있습니다.

---

## 3. Findings & Required Plan Adjustments

### [Finding H-01] Core `Contract.Version` 제약 및 Analysis 어셈블리의 CAD v1 강결합
- **Severity**: **HIGH**
- **Plan section**: Section 6 (Package and dependency design), Section 7 (Proposed CAD IR model), Section 23 (PHASE05 compatibility)
- **Problem**:
  1. `packages/DtoB.Core/Contracts.cs`의 `Contract.Version(int version)`은 오직 `version == 1`만을 허용하도록 하드코딩되어 있습니다 (`Require(version == 1, $"Unsupported schema major version {version}.");`).
  2. `IrJson.Deserialize<T>` 역시 이 `Contract.Version(major)`를 호출하므로, `schemaVersion: 2`인 JSON을 역직렬화하면 즉시 예외가 발생합니다.
  3. 현재 `services/DtoB.Analysis/AnalysisBoundary.cs`의 `IAnalysisEngine.AnalyzeAsync(CadDocument document, ...)`는 v1 `DtoB.Cad.CadDocument`를 직접 파라미터로 참조하고 있습니다.
- **Impact**:
  - Core 라이브러리를 수정하지 않고 v2를 도입하려 할 때, `CadIrJson` v2 코덱이 Core의 `Contract.Version`을 호출하면 스키마 버전 오류로 파손됩니다.
  - Analysis 프로젝트가 v1 타입을 강결합하고 있어, 향후 PHASE 05 진입 시 컴파일 에러가 발생하거나 불필요한 v1 $\leftrightarrow$ v2 변환 어댑터 작성이 강제될 위험이 있습니다.
- **Recommended Architecture**:
  1. `ADR-018`에서 CAD IR V2 전용 버전 검증 규칙(`Contract.CadVersion(major)` 또는 `CadContract.Version(major)`)을 `DtoB.Cad` 또는 `DtoB.Cad.V2` 내부에 독립적으로 정의하고, Core의 `Contract.Version(1)`을 호출하지 않도록 명시합니다.
  2. `services/DtoB.Analysis`의 경우, 현재는 더미 구현(`ANALYSIS_NOT_IMPLEMENTED`) 상태이므로 04A 단계에서는 건드리지 않되, PHASE 05 시작 시 v2 `CadDocument`로 타입을 교체하는 계획을 ADR-018에 공식 명문화합니다.
- **Required Plan Change**:
  - `docs/plans/PHASE_04_IMPLEMENTATION_PLAN.md` 섹션 7 및 섹션 20(04A)에 Core의 `Contract.Version` 회피 및 V2 전용 검증 로직 수립을 명시하십시오.
- **Required Future Test**:
  - `CadIrJsonV2Tests`: `schemaVersion: 2` 문서가 Core 버전 검사기에 의해 차단되지 않고 완벽하게 직렬화/역직렬화됨을 입증하는 테스트.

---

### [Finding M-01] `Transform3` 내부 행렬 배열의 가변성(Mutability)에 따른 방어적 복사 강제
- **Severity**: **MEDIUM**
- **Plan section**: Section 7 (Proposed CAD IR model), Section 12 (BLOCK/INSERT transform mathematics)
- **Problem**:
  - `DtoB.Geometry.Transform3`는 `public sealed record Transform3(double[] Matrix)`로 정의되어 있어, `Matrix` 속성을 통해 내부 `double[16]` 배열의 원소를 외부에서 임의 수정할 수 있습니다.
- **Impact**:
  - 블록 정의나 공유된 발생 인스턴스의 `Transform` 행렬이 다중 스레드나 후속 파이프라인에서 변조될 경우, 도면 전체의 기하가 오염될 위험이 있습니다.
- **Recommended Architecture**:
  - `CadBlockOccurrence`, `CadEntity`, `RawDiagnosticScene` 등 정규화 DTO를 생성하거나 반환할 때, `new Transform3((double[])source.Matrix.Clone())` 형태의 엄격한 방어적 복사(Defensive Copy) 정책을 아키텍처 규칙으로 확립하십시오.
- **Required Plan Change**:
  - 섹션 7에 "모든 DTO 생성 경계에서 `Transform3` 행렬 배열의 방어적 복사 의무화" 문구를 보강하십시오.
- **Required Future Test**:
  - `TransformImmutabilityTests`: 반환된 발생 인스턴스의 행렬 원소를 외부에서 수정하더라도 다른 인스턴스나 원본 정의의 변환 행렬에 영향을 주지 않음을 입증하는 테스트.

---

### [Finding M-02] 비균일 스케일된 원/호의 공간 바운즈(Bounding Box) 계산 공식 보완
- **Severity**: **MEDIUM**
- **Plan section**: Section 10 (Nonuniform / negative scale), Section 16 (Normalized geometry strategy)
- **Problem**:
  - 계획서에서는 비균일 스케일된 원/호를 타원으로 근사하지 않고 아핀 행렬을 유지한다고 명시하였으나, 이 경우 해당 엔티티의 2D/3D 축 정렬 경계 상자(AABB)를 계산하는 구체적인 수식이 계획서에 누락되어 있습니다.
- **Impact**:
  - 비균일 스케일된 도면 요소의 Fit Bounds 계산이나 공간 인덱싱 시 경계 상자가 부정확하게 계산되어 도면이 잘리거나 과대 여백이 발생할 수 있습니다.
- **Recommended Architecture**:
  - 원점 중심 반경 $R$인 원에 아핀 변환 $M = \begin{bmatrix} a & b \\ c & d \end{bmatrix}$가 적용된 경우, 변환된 타원의 극점(Extrema) 공식:
    $$X_{\text{half}} = R \sqrt{a^2 + b^2}, \quad Y_{\text{half}} = R \sqrt{c^2 + d^2}$$
    이를 적용하여 변환된 중심점 $(X_c, Y_c) \pm (X_{\text{half}}, Y_{\text{half}})$로 엄밀한 AABB를 계산하는 수학 공식을 명시하십시오. (호의 경우 시작각/끝각 범위에 따른 극점 포함 여부 검사 추가).
- **Required Plan Change**:
  - 섹션 16에 아핀 변환된 곡선의 외접 바운즈 계산 수학 공식을 명시하십시오.
- **Required Future Test**:
  - `NonuniformCurveBoundsTests`: $X$축 2배, $Y$축 3배 스케일 및 45도 회전된 원의 수작업 계산 외접 사각형과 일치하는지 검증하는 테스트.

---

### [Finding M-03] ADR 사전 승인 순서 및 범위 명문화
- **Severity**: **MEDIUM**
- **Plan section**: Section 20 (Proposed subphases), Section 23 (ADR strategy)
- **Problem**:
  - 계획서에 제안된 ADR-018, ADR-019, ADR-020의 내용이 매우 방대하며, 서브페이즈 04A 시작 전에 3건이 모두 필요한지 여부가 서브페이즈 기술마다 약간 상이하게 서술되어 있습니다.
- **Impact**:
  - 구현자가 04A를 시작하면서 ADR 작성 범위와 승인 게이트를 혼동할 우려가 있습니다.
- **Recommended Architecture**:
  - 04A 착수 전: **ADR-018 (CAD IR Normalization Boundary)**을 반드시 작성하고 승인받아야 합니다.
  - 04B~04D 착수 전: **ADR-019 (Coordinate Normalization Transforms)**을 작성하고 승인받아야 합니다.
  - 04E 착수 전: **ADR-020 (Occurrence Identity & Expansion)**을 작성하고 승인받아야 합니다.
  - (단, 팀의 여건상 04A 시작 전에 ADR-018, 019, 020을 한꺼번에 정립하여 전체 아키텍처를 동결하는 것도 전적으로 허용됩니다.)
- **Required Plan Change**:
  - 섹션 20 및 23에 각 서브페이즈별 필수 선행 ADR 게이트를 명확히 고정하십시오.
- **Required Future Test**:
  - N/A (문서 거버넌스 사항)

---

### [Finding L-01] MINSERT 대량 셀 확장에 대한 글로벌 인스턴스 예산 가드
- **Severity**: **LOW**
- **Plan section**: Section 14 (MINSERT model), Section 27 (Resource / expansion safety)
- **Problem**:
  - 단일 MINSERT 엔티티의 행/열 제한(10,000) 외에도, 중첩된 블록 내부에 MINSERT가 다시 포함되거나 여러 MINSERT가 복합될 경우 전체 도면의 총 인스턴스 수가 지수적으로 급증할 수 있습니다.
- **Impact**:
  - 단일 엔티티 한도는 통과하지만 도면 전체의 총 인스턴스 수가 메모리 상한을 초과하여 OOM이 발생할 수 있습니다.
- **Recommended Architecture**:
  - 정규화 세션 전체를 통틀어 단일 카운터(`TotalOccurrencesAllocated`)를 유지하고, 100,000회를 초과하는 즉시 `ResourceLimitExceeded` 진단과 함께 안전하게 중단하도록 가드를 단일화하십시오.
- **Required Plan Change**:
  - 섹션 17 및 27의 리소스 가드 설명에 전역 누적 카운터 동작을 명시하십시오.
- **Required Future Test**:
  - `MinsertResourceExplosionTests`: 중첩 MINSERT 조합으로 전역 상한을 초과시키는 테스트 픽스처 거부 검증.

---

## 4. Final Verdict

```text
================================================================================
FINAL VERDICT: PHASE_04_PLAN_PASS
================================================================================
```

Codex가 작성한 `PHASE_04_IMPLEMENTATION_PLAN.md`는 도메인 원칙, 수학적 정합성, 소스 증거 추적성, 의존성 격리 측면에서 매우 높은 완성도를 갖춘 탁월한 아키텍처 계획입니다. 본 검토에서 제시된 1건의 HIGH 과제(Core 버전 제약 격리) 및 3건의 MEDIUM 과제는 향후 ADR 작성 및 04A~04H 서브페이즈 구현 과정에서 충분히 수용·반영될 수 있으므로, 본 아키텍처 계획에 대해 **최종 승인(PASS)**을 선언합니다.

*(주의: 본 판정은 아키텍처 계획에 대한 승인이며, 프로덕션 코드 구현이나 04A 착수를 즉시 승인하는 것이 아닙니다. 작업 소유자의 명시적 지시에 따라 선행 ADR 작성 및 단계별 승인을 거쳐 진행하십시오.)*
