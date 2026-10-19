# PHASE 04 — ADR-018 / ADR-019 / ADR-020 Independent Architecture Freeze Review

## Executive Summary

- **Review Target**:
  - `docs/adr/ADR-018-CAD-IR-Normalization-Boundary.md` (Proposed)
  - `docs/adr/ADR-019-Coordinate-Normalization-Transforms.md` (Proposed)
  - `docs/adr/ADR-020-Occurrence-Identity-and-Expansion.md` (Proposed)
  - `docs/plans/PHASE_04_IMPLEMENTATION_PLAN.md` (Amended)
  - `docs/status/PHASE_04_ADR_GATE_REPORT.md`
- **Review Date**: 2026-10-15
- **Review Mode**: Independent Architecture Freeze & ADR Audit (Zero production code modifications)
- **Reviewer**: Architecture / Adversarial Review Owner (Antigravity)
- **Status of Codex**: `ADR_REVIEW_REQUIRED` (All three ADRs Proposed, not Accepted)
- **Final Verdict**: **`PHASE_04_ADR_PASS`**

PHASE 04 아키텍처 계획 승인(`PHASE_04_PLAN_PASS`) 후속으로 작성된 3건의 Proposed ADR(ADR-018, ADR-019, ADR-020)과 갱신된 구현 계획서에 대해 엄격하고 독립적인 아키텍처 동결(Freeze) 감사를 수행하였습니다.

본 검토는 세 ADR이 제안하는 정규화 경계, 수학적 변환 공식, 불변성 보장, 식별자 및 전역 리소스 거버넌스가 실제 DtoB 코드베이스(`DtoB.Core`, `DtoB.Geometry`, `DtoB.Cad`, `DtoB.Cad.Raw`, `DtoB.Cad.Dxf`, `DtoB.Analysis`), Autodesk DXF 표준 사양, 그리고 선행 리뷰에서 도출된 5대 과제(H-01, M-01, M-02, M-03, L-01)를 아키텍처 수준에서 완벽하게 해소하고 동결할 수 있는지를 독립적으로 검증하였습니다.

독립 감사 결과 요약:
1. **ADR-018 (정규화 경계 및 CAD v2)**:
   - Core의 레거시 `Contract.Version(1)` 및 `IrJson` 검증 경로를 우회하고, CAD v2 전용의 독립 코덱(`CadIrJson`)과 검증 규칙(`CadContract`)을 확립함으로써 이전 리뷰의 **H-01** 과제를 완벽히 해결하였습니다.
   - Raw 데이터의 완전 불변성, Revit으로부터의 철저한 독립성, 그리고 Analysis v1을 변경하지 않고 향후 PHASE 05 진입 시 명시적으로 마이그레이션하는 2단계 전환 전략이 명문화되었습니다.
2. **ADR-019 (좌표 정규화 및 변환 수학)**:
   - `DtoB.Geometry.Transform3`의 실제 구현(행-우선 저장, 열벡터 작용, `inner.Then(outer) = outer * inner`)과 100% 일치하는 수학적 정의를 수립하였습니다.
   - 가변 `double[]` 배열 노출을 원천 차단하는 방어적 복사(Defensive Copy) 불변 래퍼 정책을 확립하여 **M-01**을 해결하였습니다.
   - OCS $\rightarrow$ WCS Arbitrary Axis Algorithm(AAA), 블록 베이스 포인트 보정($T(-b)$)을 포함한 $L = A \cdot T(i) \cdot R_z(\theta) \cdot S \cdot T(-b)$ 변환 합성식, 그리고 비균일 스케일된 원/호에 대한 엄밀한 해석적 경계 상자(AABB) 극점 수식 및 손계산 오라클을 정립하여 **M-02**를 해결하였습니다.
3. **ADR-020 (발생 인스턴스 식별자 및 전개)**:
   - 공유 정의와 발생 인스턴스(Occurrence)의 엄격한 분리, 계통 경로(Typed Occurrence Path), 그리고 난수/머신 경로를 배제한 결정론적 바이너리 길이 프레이밍 SHA-256 식별자(`cad2:<digest>`) 체계를 수립하였습니다.
   - MINSERT 격자 간격이 회전각을 따르되 자체 스케일에는 곱해지지 않는다는 DXF 사양을 완벽히 수식화하였습니다.
   - 단일 세션 전역에서 단조 증가하며 절대 리셋/환불되지 않는 `TotalOccurrencesAllocated` ($\le 100,000$) 카운터 정책을 명시하여 **L-01**을 해결하였습니다.
4. **거버넌스 및 선행 승인 순서 (M-03)**:
   - 04A 착수 전 ADR-018 수락, 04B 착수 전 ADR-018/019 수락 등 서브페이즈별 필수 선행 ADR 게이트를 확정하여 **M-03**을 해결하였습니다.
5. **프로덕션 코드 0바이트 불변 확인**:
   - `DtoB.Cad.Normalize` 프로젝트는 아직 생성되지 않았으며, 프로덕션 패키지 및 테스트 코드에 대한 무단 수정이 일절 발생하지 않았음을 확인하였습니다.

모든 아키텍처적 위험과 결합도가 완전히 통제되었으므로, 제안된 ADR-018, ADR-019, ADR-020의 내용을 최종 아키텍처로 동결 승인(**`PHASE_04_ADR_PASS`**)합니다.

---

## 1. Review Target 1 — ADR-018: CAD IR Normalization Boundary

### 1.1 CAD IR 위치 및 파이프라인 정합성
- **검증 결과**: **적합 (SATISFIED)**
- **분석**:
  - `RawDrawingSnapshot` $\rightarrow$ `Normalize` $\rightarrow$ `CAD v2` $\rightarrow$ `Drawing Intelligence (PHASE 05+)` $\rightarrow$ `BIM IR` $\rightarrow$ `Output Adapters (Revit/IFC/Speckle/Web)` 파이프라인의 위치가 명확히 확립되었습니다.
  - CAD IR은 Revit DTO가 아니며, BIM 의미 모델도 아니고, Raw 스냅샷을 대체하거나 왜곡하는 계층도 아닙니다.
  - CAD IR에서 Revit API를 호출하거나 Revit 요소를 직접 생성하는 지름길(Shortcut)은 아키텍처 원칙에 따라 엄격히 금지됩니다.

### 1.2 CAD v2 Version Ownership (Finding H-01 완전 종결)
- **검증 결과**: **적합 (CLOSED_BY_ARCHITECTURE)**
- **분석**:
  - 실제 레거시 `DtoB.Core.Contract.Version(int version)`은 오직 `version == 1`만을 허용하도록 하드코딩되어 있으며, Core의 `IrJson.Deserialize<T>` 또한 이 레거시 검증에 종속되어 있습니다.
  - ADR-018은 Core의 코드를 임의로 완화하거나 수정하지 않고, **`DtoB.Cad.V2` 네임스페이스 내부에 CAD v2 전용의 독립 직렬화/역직렬화 코덱인 `CadIrJson`과 `CadContract`를 신설**하도록 결정하였습니다.
  - `CadIrJson`은 정수형 `schemaVersion`이 정확히 2임을 검증하며, 레거시 Core 검증 경로를 일절 호출하지 않습니다. 미지원 버전, 누락/비정수 버전, 미인식 필드는 엄격히 거부(Strict rejection)합니다.
  - 이로써 레거시 BIM v1, CAD v1, Project 스키마의 무결성을 100% 보존하면서 CAD v2의 독립적인 스키마 소유권을 완벽하게 확립하였습니다.

### 1.3 CAD v1 / v2 공존 및 마이그레이션 전략
- **검증 결과**: **적합 (SATISFIED)**
- **분석**:
  - ADR-018은 CAD v1과 CAD v2를 영구적인 병렬 도메인 모델로 유지하지 않는다는 원칙을 천명하였습니다.
  - **PHASE 04**: CAD v2를 additive 방식으로 도입하며, Analysis-v1 경계는 건드리지 않고 온전히 보존.
  - **PHASE 05**: 인식 엔진 구현 시작 시점에 Analysis 입력/검증/테스트를 CAD v2로 공식 마이그레이션.
  - **향후 단계**: v1 소비처 및 호환성 영향도를 검토한 후 별도의 마이그레이션 게이트를 통해 v1을 공식 폐기(Deprecation/Removal).
  - 임시 변환기(v1 $\leftrightarrow$ v2 lossy converter)나 무단 JSON 업그레이드를 배제하고, 유효한 Raw 스냅샷으로부터 v2를 직접 생성하는 원칙을 고수하였습니다.

### 1.4 패키지 의존성 그래프
- **검증 결과**: **적합 (SATISFIED)**
- **분석**:
  - 실제 `.csproj` 의존성을 점검한 결과, 제안된 의존성 흐름(`DtoB.Cad.Raw` $\leftarrow$ `DtoB.Cad.Normalize` $\rightarrow$ `DtoB.Cad (V2)` $\rightarrow$ `Geometry` / `Core`)이 완벽한 단방향성을 가짐을 확인하였습니다.
  - 금지된 역참조(`Raw` $\rightarrow$ `Normalize`, `Dxf` $\rightarrow$ `Normalize`, `Normalize` $\rightarrow$ `Revit/WPF/Desktop`)가 일절 존재하지 않습니다.
  - 별도의 `DtoB.Cad.Ir` 패키지를 난립시키지 않고 기존 `DtoB.Cad` 내부에 `DtoB.Cad.V2` 네임스페이스로 격리 배치하는 방안이 가장 작고 깔끔한 의존성을 유지합니다.

### 1.5 부분 정규화 정책 (Partial Normalization)
- **검증 결과**: **적합 (SATISFIED)**
- **분석**:
  - 도면 내 단일 엔티티, 속성, 또는 블록 인스턴스의 정규화 실패로 인해 도면 전체의 정상 기하가 유실되지 않도록 `Normalized`, `PartiallyNormalized`, `UnsupportedNormalization`, `InvalidNormalization`의 4대 상태를 정의하였습니다.
  - 전체 문서 거부(`CadNormalizationResult.CadDocument == null`)는 필수 도면 단위(INSUNITS) 모순/부재, 스키마 비호환, 취소 요청, 또는 전역 리소스 한도 초과 등 **문서 전체의 전제 조건 결함에만 엄격히 한정**함으로써, Phase 03의 LOW-01과 같은 과도한 전체 거부가 정규화 계층에서 재발하지 않도록 합리적으로 설계되었습니다.

### 1.6 소스 역추적성 (Provenance)
- **검증 결과**: **적합 (SATISFIED)**
- **분석**:
  - 모든 정규화 요소는 `DrawingId`, `RevisionId`, `FileHash`, `OriginalHandle`, `CanonicalHandle`, `SourceRecordRef`, `SourceFactRef`를 보존합니다.
  - 매 객체마다 거대한 Raw 그래프를 복제하지 않고, 문서 단위의 컴팩트 레지스트리를 통해 단일 스냅샷 인덱스를 참조하도록 최적화되었습니다.

---

## 2. Review Target 2 — ADR-019: Coordinate Normalization / Transforms

### 2.1 표준 규약 및 `Transform3` 실제 코드 일치성
- **검증 결과**: **적합 (SATISFIED - ZERO MISMATCH)**
- **분석**:
  - 표준 규약: 길이 mm, 각도 라디안, 오른손 XYZ, $+Z$ up, 반시계(CCW) 양수 회전.
  - 실제 `packages/DtoB.Geometry/Geometry.cs`의 `Transform3` 코드를 전수 대조하였습니다:
    - 16개 double의 행-우선(Row-major) 저장 (인덱스 3, 7, 11이 평행이동 $X, Y, Z$, 마지막 행 `0, 0, 0, 1`).
    - 열벡터 점에 작용: `Apply(p)`는 행렬의 각 행과 점 벡터의 내적 + 평행이동.
    - 합성 순서: `Then(next)`는 `next.Matrix * this.Matrix`를 수행. 즉, `inner.Then(outer)`는 `outer * inner`로서 `inner` 적용 후 `outer` 적용.
  - ADR-019의 기술은 실제 C# 구현 코드와 단 1자의 오차도 없이 완벽히 일치합니다.

### 2.2 Transform 불변성 래퍼 (Finding M-01 완전 종결)
- **검증 결과**: **적합 (CLOSED_BY_ARCHITECTURE)**
- **분석**:
  - 기존 `Transform3(double[] Matrix)`는 C# 레코드이지만 내부 `double[]`이 가변 배열이어서 외부 수정에 취약합니다.
  - ADR-019는 **CAD v2 변환 값 객체에서 16개 double의 방어적 복사본(Defensive Private Copy)을 보유하고, 내부 배열에 대한 참조를 절대 외부에 노출하지 않도록** 규정하였습니다.
  - 생성자, 역직렬화, 외부 프로젝션(Getter/Export) 등 모든 경계에서 복사본을 제공하도록 강제하였으며, 향후 `TransformImmutabilityTests`를 통해 원본 배열 변조 시에도 도면 및 인스턴스 행렬이 오염되지 않음을 검증하도록 못박았습니다.

### 2.3 $INSUNITS 및 각도 필드 역할
- **검증 결과**: **적합 (SATISFIED)**
- **분석**:
  - $INSUNITS$: 1(inch, 25.4), 2(foot, 304.8), 4(mm, 1.0), 5(cm, 10.0), 6(meter, 1000.0)을 지원하며, 0 또는 부재 시 `NeedsExplicitUnits`로 정직하게 거부합니다. 추측이나 파일명 기반 추론은 엄격히 금지됩니다.
  - 블록 단위(BLOCK_RECORD 70) 모순 시 임의 변환하지 않고 안전하게 거부합니다.
  - 각도 변환: ARC, INSERT, TEXT의 60분법 각도는 라디안으로 변환하되, **DXF 사양상 이미 라디안으로 저장되는 `MTEXT` Group Code 50은 이중 변환하지 않는다는 예외 규칙**을 필드 역할 변환표에 완벽히 명시하였습니다.

### 2.4 OCS $\rightarrow$ WCS Arbitrary Axis Algorithm (AAA) 및 Elevation
- **검증 결과**: **적합 (SATISFIED)**
- **분석**:
  - Autodesk DXF 공식 사양의 임계값 $|N_x| < 1/64$ 및 $|N_y| < 1/64$ 분기를 준수하며, 기저 $A = [A_x \ A_y \ N]$을 열벡터로 구성하여 오른손 정규 직교 기저를 확립합니다.
  - `LINE`/`POINT`/`MTEXT`는 WCS, `LWPOLYLINE`/`CIRCLE`/`ARC`/`SOLID`/`TEXT`/`INSERT`는 OCS를 적용하며, Elevation은 기저 변환 전 OCS $Z$축에 단 한 번만 더해져 이중 적용을 방지합니다.

### 2.5 BLOCK / INSERT 변환 합성 수학 및 손계산 오라클
- **검증 결과**: **적합 (MATHEMATICALLY VERIFIED)**
- **분석**:
  - 변환 합성 수식:
    $$L = A \cdot T(i) \cdot R_z(\theta) \cdot S \cdot T(-b)$$
    $$M_{\text{child}} = M_{\text{parent}} \cdot L_{\text{child}} \quad (\text{코드상 } L_{\text{child}}.\text{Then}(M_{\text{parent}}))$$
    $$q_{\text{model}} = \text{SourceToModel} \cdot M_{\text{child}} \cdot q$$
  - 점 $q$에 작용하는 순서: 베이스 포인트 원점 정렬($q - b$) $\rightarrow$ 블록 축 스케일($S$) $\rightarrow$ INSERT OCS 회전($R_z$) $\rightarrow$ INSERT OCS 평행이동($+i$) $\rightarrow$ 부모 WCS로 기저 회전($A$).
  - **독립 손계산 산술 검증**:
    1. $b=(10,20,0), q=(12,23,0), S=(2,3,1), R=90^\circ, i=(100,200,0), A=I$:
       $q-b=(2,3,0) \rightarrow S=(4,9,0) \rightarrow R_{90^\circ}=(-9,4,0) \rightarrow T(i)=(91,204,0)$. 정확함.
    2. 음수 스케일 $s_x = -2$:
       $q-b=(2,3,0) \rightarrow S=(-4,9,0) \rightarrow R_{90^\circ}=(-9,-4,0) \rightarrow T(i)=(91,196,0)$. 정확함.
    3. $q = b$일 때: $q-b = 0 \rightarrow$ 회전/스케일 무관하게 $A \cdot i$로 수렴. 정확함.
    4. 기울어진 법선 $N = +Y$ ($AAA$에 의해 $(x, y, z) \rightarrow (-x, z, y)$):
       첫 번째 결과 $(91, 204, 0)$이 $(-91, 0, 204)$로 변환. 부모 평행이동 $(10, 0, 0)$ 합성 시 $(-81, 0, 204)$. 정확함.

### 2.6 비균일 곡선 외접 바운즈 수학 (Finding M-02 완전 종결)
- **검증 결과**: **적합 (CLOSED_BY_ARCHITECTURE)**
- **분석**:
  - 비균일/음수 스케일 하에서 원을 가짜 원으로 축소 왜곡하지 않고 "로컬 원/호 + 아핀 행렬"을 유지합니다.
  - 전수 원(Circle)에 대해 2D 선형 행렬 $B = \begin{bmatrix} a & b \\ c & d \end{bmatrix}$ 적용 시 엄밀한 반경 폭(Half extents):
    $$X_{\text{half}} = R \sqrt{a^2 + b^2}, \quad Y_{\text{half}} = R \sqrt{c^2 + d^2}$$
  - 일반 3D/경사 평면: 기저 벡터 $u = B \cdot U, v = B \cdot V$에 대해 $j$축 반경 폭은 $R \sqrt{u_j^2 + v_j^2}$.
  - 호(Arc)의 경우 시작점, 끝점 및 도함수가 0이 되는 파라미터 $\theta = \text{atan2}(v_j, u_j) + k\pi$ 중 **실제 원본 스윕 각도 구간 내에 존재하는 극점만을 정밀 판정**하도록 수식화되었습니다.
  - ADR-019에 수록된 손계산 오라클($R=2, C=(10,-4,3), B=R_{45^\circ} \cdot S(2,3)$에 대해 $X_{\text{half}}=Y_{\text{half}}=\sqrt{26}$, 호 $[0, \pi/2]$ 구간에서의 실제 extrema 판정)이 수학적으로 완전 무결함을 검증하였습니다.

---

## 3. Review Target 3 — ADR-020: Occurrence Identity and Expansion

### 3.1 결정론적 cad2 식별자 체계
- **검증 결과**: **적합 (SATISFIED)**
- **분석**:
  - 난수 GUID, 머신 경로, 타임스탬프, 부동소수점 기하 해시를 배제하였습니다.
  - UTF-8 도메인 태그 `dtob/cad2/id/v1`로 시작하여 엔티티 도메인, DrawingId, RevisionId, FileHash, Canonical Handle, 계통 경로 튜플을 빅엔디안 바이너리 길이 프레이밍(Binary Length-framed Tuple)으로 인코딩한 후 SHA-256 디제스트를 생성하는 `cad2:<digest>` 체계를 수립하였습니다.
  - 서로 다른 도메인이나 계통 간의 해시 충돌 가능성이 원천 차단되었으며, 다회 실행 시 100% 동일한 ID가 재현됩니다.

### 3.2 MINSERT 변환 및 회전 간격 수식
- **검증 결과**: **적합 (MATHEMATICALLY VERIFIED)**
- **분석**:
  - 순서: 행 우선(Row-major, 0-based), 순서 번호 $r \times \text{cols} + c$.
  - 변환 수식:
    $$d(r, c) = (c \cdot \text{colSpacingMm}, \ r \cdot \text{rowSpacingMm}, \ 0)$$
    $$L_{rc} = A \cdot T(i) \cdot R_z(\theta) \cdot T(d(r, c)) \cdot S \cdot T(-b)$$
  - 간격 $d(r, c)$가 회전 $R_z(\theta)$의 영향을 받아 회전하지만, 블록 자체의 기하 스케일 $S$에는 곱해지지 않는다는 DXF 사양을 완벽히 만족합니다.
  - 가짜 DXF 핸들을 위조하지 않고 원본 INSERT 핸들과 $(r, c)$ 튜플로 식별성을 부여합니다.

### 3.3 재귀 순환 감지 및 전역 리소스 카운터 (Finding L-01 완전 종결)
- **검증 결과**: **적합 (CLOSED_BY_ARCHITECTURE)**
- **분석**:
  - 활성 블록 정의 참조 스택(Active-path reference stack)을 유지하여, 단순 정의 재사용과 순환 참조(Direct/Indirect Cycle)를 정확히 구별하고 순환 발생 시 해당 분기만을 안전하게 거부합니다.
  - 리소스 한도: 중첩 깊이 32, 단일 MINSERT 10,000 셀, 파생 기하 250,000, 진단 5,000.
  - **전역 단일 단조 증가 카운터 (`TotalOccurrencesAllocated <= 100,000`)**:
    - 정규화 세션 전체를 통틀어 단 하나의 카운터만 유지.
    - 루트 INSERT, 중첩 INSERT, 모든 MINSERT 셀을 포함하여 단 1회씩 누적 카운트.
    - 정의, 부모, 재귀 프레임 단위로 절대 리셋되지 않으며, 분기 거부 시에도 차감/환불(Refund)되지 않음.
    - 할당 전 검사를 수행하여 한도 초과 시 즉시 `ResourceLimitExceeded`를 발행하고 문서 발행을 거부.
  - 이로써 중첩 MINSERT 조합에 의한 악의적인 지수 메모리 폭발 취약점을 아키텍처 수준에서 완벽하게 봉쇄하였습니다.

### 3.4 취소 반응성 및 스타일 컨텍스트
- **검증 결과**: **적합 (SATISFIED)**
- **분석**:
  - 엔티티 순회, 재귀 전개, MINSERT 반복 루프 내부에서 `CancellationToken`을 주기적으로 확인하도록 설계되었습니다.
  - 레이어 0 상속 및 ByBlock 계통 해결을 위해 필요한 발생 인스턴스 체인 컨텍스트를 충실히 제공합니다.

---

## 4. Cross-ADR Consistency & Findings Closure Matrix

### 4.1 세 ADR 간 상호 정합성 (Cross-ADR Consistency)
- ADR-018(경계 및 스키마), ADR-019(좌표 및 변환), ADR-020(발생 및 식별자) 세 문서는 용어, 행렬 순서, 수치 정책, 스키마 major 2 버전, 에러 처리 등 모든 영역에서 완벽한 일치성을 보이고 있으며, 상호 모순되는 기술이 전무합니다.

### 4.2 선행 리뷰 과제(Findings) 종결 상태

| Finding ID | 이전 심각도 | 핵심 주제 | ADR 조치 내용 | 종결 판정 |
|---|---|---|---|---|
| **H-01** | **HIGH** | Core `Contract.Version` 제약 및 Analysis v1 결합 | ADR-018: CAD v2 전용 `CadIrJson`/`CadContract` 독립화, Core v1 검증 우회, Analysis v1 보존 및 PHASE 05 공식 마이그레이션 명시 | **CLOSED_BY_ARCHITECTURE** |
| **M-01** | **MEDIUM** | `Transform3` 가변성 결함 | ADR-019: CAD v2 변환 값 객체에서 16개 double의 방어적 복사 및 불변 래퍼 정책 강제, `TransformImmutabilityTests` 요구 | **CLOSED_BY_ARCHITECTURE** |
| **M-02** | **MEDIUM** | 비균일 스케일 곡선 바운즈 공식 누락 | ADR-019: 2D/3D 아핀 변환 곡선의 해석적 AABB 극점 수식 및 손계산 오라클 공식화, `NonuniformCurveBoundsTests` 요구 | **CLOSED_BY_ARCHITECTURE** |
| **M-03** | **MEDIUM** | ADR 선행 승인 순서 및 거버넌스 | ADR-018/019/020 및 계획서: 서브페이즈별 필수 선행 ADR 게이트 테이블 확정 | **CLOSED_BY_ARCHITECTURE** |
| **L-01** | **LOW** | 중첩 MINSERT 전역 인스턴스 폭발 | ADR-020: 세션 전역 단일 단조 증가 카운터(`TotalOccurrencesAllocated <= 100,000`, 리셋/환불 불가) 강제 | **CLOSED_BY_ARCHITECTURE** |

---

## 5. ADR Acceptance Order & Subphase Prerequisites

구현 단계 착수 시 준수해야 할 필수 ADR 선행 승인 게이트는 다음과 같이 확정됩니다:

| Subphase | 구현 대상 내용 | 필수 선행 Accepted ADR |
|---|---|---|
| **04A** | V2 DTO 계약, `CadIrJson`, 레지스트리, 경계 스캐폴딩 | **ADR-018** |
| **04B** | 단위(INSUNITS), 각도 역할 변환, 수치 정책 | **ADR-018 + ADR-019** |
| **04C** | OCS / WCS / AAA 기저 및 좌표 변환 | **ADR-019** |
| **04D** | BLOCK 베이스 포인트 보정 및 INSERT 국소 변환 엔진 | **ADR-019** |
| **04E** | 중첩 INSERT 전개, MINSERT 격자, cad2 식별자, 전역 리소스 가드 | **ADR-019 + ADR-020** |
| **04F** | 레이어 0, ByLayer, ByBlock 발생 계통 스타일 해결 | **ADR-018 + ADR-020** (발생 컨텍스트) |
| **04G** | 기하 프리미티브 정규화, Bulge 호 세그먼트, 외접 바운즈, 문서 조립 | **ADR-018 + ADR-019 + ADR-020** |
| **04H** | 결정론적 QA, Invariant/fr-FR/ko-KR 일치성, 다중 프로세스 골든 검증 | **전체 아키텍처 (ADR-018/019/020)** |

*(참고: 04A 시작 전 3건의 ADR을 일괄 수락하는 것이 전체 파이프라인의 아키텍처 일관성을 유지하는 데 가장 이상적입니다.)*

---

## 6. Implementation Readiness & Strict Prohibitions

1. **프로덕션 소스 무단 수정 0건 확인**:
   - `packages/DtoB.Cad.Normalize` 디렉토리는 존재하지 않으며 생성되지 않았습니다.
   - `DtoB.Cad`, `DtoB.Cad.Raw`, `DtoB.Cad.Dxf`, `DtoB.Core`, `DtoB.Geometry`, `DtoB.Analysis`의 모든 기존 소스 파일 및 프로젝트 파일은 단 1바이트도 수정되지 않았습니다.
2. **ADR 상태 준수**:
   - 세 ADR 문서는 현재 모두 `Status: Proposed`로 정직하게 유지되고 있습니다.
3. **독립 검토자의 역할 한계 준수**:
   - 본 독립 검토자는 ADR을 직접 `Accepted`로 수정하지 않으며, 프로덕션 코드를 구현하거나 04A를 시작하지 않습니다.

---

## 7. Final Verdict

```text
================================================================================
FINAL VERDICT: PHASE_04_ADR_PASS
================================================================================
```

제안된 ADR-018, ADR-019, ADR-020은 도메인 격리, 수학적 엄밀성, 소스 증거 불변성, 식별자 결정론, 리소스 안전성 측면에서 최고의 아키텍처적 완성도를 입증하였습니다. 이에 따라 세 ADR의 아키텍처 동결(Freeze)을 **최종 승인(PASS)**합니다.

*(주의: 본 판정은 아키텍처 동결에 대한 독립 승인이며, ADR 상태를 'Accepted'로 변경하고 프로덕션 04A 구현을 착수하는 권한은 작업 소유자의 명시적 지시에 의해서만 행사되어야 합니다.)*
