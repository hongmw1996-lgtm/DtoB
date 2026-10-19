# PHASE 04E — NESTED INSERT / MINSERT OCCURRENCE EXPANSION
## INDEPENDENT IMPLEMENTATION & ARCHITECTURAL REVIEW

- **Review Date**: 2026-10-16
- **Reviewer**: Antigravity (Independent Architecture & QA Gatekeeper)
- **Subject Commit / Target**: PHASE 04E Implementation on `phase/04-cad-normalization`
- **Reviewed Package**: `packages/DtoB.Cad.Normalize/Occurrences/` & `packages/DtoB.Cad.Normalize/Placement/CadInsertLocalTransform.cs`
- **Reviewed Tests**: `tests/DtoB.Cad.Normalize.Tests/Occurrences/` (60 new tests, 1188 / 1188 solution PASS)
- **Authoritative Baseline**:
  - `ADR-018`: CAD IR Normalization Boundary
  - `ADR-019`: Coordinate Normalization Transforms
  - `ADR-020`: Occurrence Identity and Expansion
  - `PHASE_04D_REVIEW.md`: `PHASE_04D_PASS`
- **Final Verdict**: **`PHASE_04E_PASS`**

---

## 1. Executive Summary & Verdict

PHASE 04E 구현에 대한 독립적인 아키텍처 및 구현 검증을 완료하였습니다.

Codex가 구현한 `CadOccurrenceExpander`, `CadOccurrenceIdentity`, `CadInsertLocalTransform.CreateArrayTemplate` 및 관련 계약 모델은 ADR-018, ADR-019, ADR-020에 명시된 Occurrence 전개(Expansion) 아키텍처를 완벽하게 준수하고 있습니다.

### 핵심 판정 요약
1. **04D Seam 무결성**: 기존 04D `CadInsertLocalTransform.Create`의 연산 로직과 바이트 무결성이 100% 보존되었으며, 04E MINSERT 지원을 위한 팩토리(`CreateArrayTemplate`)는 내부 템플릿 생성에만 한정적으로 결합되었습니다.
2. **Transform 합성 순서 및 Spacing 독립성**:
   - 누적 변환 합성 $M_{\text{child}} = M_{\text{parent}} \cdot L_{\text{child}}$ (Right-associative column-vector semantics, `local.Then(parentAccumulated)`)가 정확히 검증되었습니다.
   - MINSERT 격자 변환 $L_{rc} = T(A \cdot R \cdot d) \cdot L_{\text{zero}}$에서 격자 간격 $d$에 Block Scale $S$가 곱해지지 않음(스케일 분리)을 수학적으로 확인하였습니다.
3. **결정론적 Occurrence Identity**:
   - `CadId.Create` (Domain: `Occurrence`)를 재사용하며, Authority, 최내각 INSERT 토큰, 경로 스텝 튜플(Block Handle, Insert Handle, Row, Column)만을 결정론적 바이너리 프레이밍으로 해싱하여 생성합니다.
   - 변환 행렬, 좌표, 타임스탬프, 인스턴스 주소 등의 비결정론적 요소가 일절 포함되지 않습니다.
4. **자원 한도 및 예외 정책**:
   - 깊이 한도(32), 배열 셀 한도(10,000), 전역 Occurrence 예산(100,000)이 엄격하게 시행됩니다.
   - 생성 실패 시 슬롯을 반환하지 않는 "No-Refund" 규칙이 엄수되어 DoS 및 무한 루프 공격을 원천 차단합니다.
   - 진단(Diagnostic) 한도 도달 시 `LimitExhausted` 터미널 슬롯을 보장합니다.
5. **Controlled Source 사실성 검증**:
   - 공식 fixture `DtoB_TEST_2STORY_R2018.dxf`의 25개 INSERT 중 ENTITIES 루트 17개, 미참조 블록 내부 8개(0 MINSERT)의 구성을 정확히 진단하였으며, 중첩 및 MINSERT는 합성 fixture를 통해 정직하고 엄밀하게 검증되었습니다.
6. **영역 격리 (Strict Scope Preservation)**:
   - 04F 소관인 Layer 0 / ByLayer / ByBlock 색상 및 선형 상속, 지오메트리 변환 투영, 불지 곡선 분해, CadDocument 어셈블리, BIM/Revit 의존성이 일절 도입되지 않았습니다.

**최종 판정**: **`PHASE_04E_PASS`** (BLOCKER: 0, HIGH: 0, MEDIUM: 0, LOW: 0)

---

## 2. 34개 세부 항목 감사 매트릭스 (Comprehensive Review Audit)

### 2.1 Scope & Architecture Boundary
1. **Scope Boundary Audit**:
   - 04E 소관(Root INSERT, Nested INSERT, MINSERT cells, Occurrence Path, Deterministic CadId, Accumulated Transform, Resource Limits, Active-path Cycle Detection)만을 구현하였으며, 04F 소관(색상/선형 상속, 지오메트리 변환 적용, 어셈블리) 및 PHASE 05+ 인식이 완벽히 배제되었습니다.
2. **04D Transform Seam Audit**:
   - `CadInsertLocalTransform.cs`의 수정 내역은 `internal static CreateArrayTemplate` 추가 및 공통 로직을 위한 `CreateCore` 분할뿐이며, `arrayTemplate: false`일 때의 공개 `Create` 메서드는 04D와 100% 동일한 연산 경로를 수행합니다. 04D 증거 해시 `02045C202CACF9DA66406827E297435CCCB7448D1D0A38C7B52E7FCAAA3A0EE5`가 완전하게 유지됩니다.
3. **Definition vs. Occurrence Separation**:
   - `CadBlockDefinition`은 불변의 도메인 Source of Truth로 읽기 전용 공유되며, 지오메트리를 복제하지 않고 경로와 누적 변환을 담은 `CadExpandedOccurrence` 인스턴스만을 생성합니다.
4. **Root INSERT Semantics**:
   - ENTITIES 섹션의 루트 INSERT에 대해 Depth=0, Parent=null, LocalTransform == AccumulatedTransform, Path 길이 1인 정형 Occurrence가 생성됩니다.
5. **Revit Independence**:
   - 어떠한 Revit API(`Autodesk.Revit.*`) 의존성도 참조하지 않으며 순수 C# 수치 및 정규화 계약으로만 동작합니다.

### 2.2 Transform Mathematics & Composition
6. **Nested Transform Composition Order**:
   - `local.ToTransform3().Then(parent.AccumulatedTransform.ToTransform3())`는 내부적으로 `b * a` ($M_{\text{parent}} \cdot L_{\text{child}}$)를 연산합니다. 점 $p$에 대해 $p' = M_{\text{parent}} \cdot (L_{\text{child}} \cdot p)$가 적용되는 Column-Vector 규칙을 완벽하게 만족합니다.
7. **Local vs. Accumulated Transform Semantics**:
   - `LocalTransform`은 직전 부모 기준의 상대 변환을 유지하고, `AccumulatedTransform`은 월드(WCS) 기준의 복합 변환 행렬을 정확히 보존합니다.
8. **Orientation & Reflection Consistency**:
   - 부모 변환과 자식 변환의 행렬식 부호 곱을 통해 음수 스케일에 의한 반전(Reflection / Handedness change)이 누적 행렬에 정확히 반영됩니다.

### 2.3 Identity, Path & Handles
9. **Occurrence Path Typed Semantics**:
   - `CadOccurrencePath`는 루트부터 리프까지의 `CadOccurrenceStep` 배열을 불변 리스트로 캡슐화하며, 각 스텝은 `(DefinitionHandle, InsertHandle, RowIndex, ColumnIndex)`의 튜플로 고유성을 가집니다.
10. **Occurrence Identity Determinism**:
    - `CadOccurrenceIdentity.CreateId`는 `CadIdentityDomain.Occurrence`를 사용하고, Authority, 최내각 INSERT 토큰, 경로 스텝들의 Handle 바이트 및 그리드 인덱스를 정규 순서로 해싱합니다.
11. **Transform / Coordinate Non-leaking Identity**:
    - 좌표, 변환 행렬의 float 값, 시스템 타임스탬프, 메모리 포인터가 ID 생성 해시에 일절 포함되지 않아 기계 간 완전한 결정론을 보장합니다.
12. **DWG/DXF Handle Preservation**:
    - 블록 정의 Handle과 INSERT 엔터티 Handle이 소실 없이 `CadRawHandle` 형태로 보존되어 완벽한 역추적성을 제공합니다.

### 2.4 MINSERT Cell Mathematics & Semantics
13. **MINSERT Entity Mapping**:
    - `IsArray == true`인 레코드에 대해 단일 INSERT가 아닌 $R \times C$개의 독립적인 `CadExpandedOccurrence`로 정확히 전개됩니다.
14. **Row / Column Indexing Order**:
    - `r = 0 .. RowCount-1`, `c = 0 .. ColumnCount-1`의 순서로 정규화된 0-based 인덱스를 부여합니다.
15. **Cell Local Transform Formula**:
    - 기준 셀(0,0) 변환 $L_{\text{zero}} = T(\text{anchorWcs}) \cdot A \cdot R \cdot S \cdot T(-b)$에 대해, 셀 $(r,c)$의 변환은 $L_{rc} = T(A \cdot R \cdot d) \cdot L_{\text{zero}}$로 도출됩니다.
16. **Spacing vs. Scale Independence (Critical)**:
    - 격자 변위 벡터 $d = (c \cdot \Delta x, r \cdot \Delta y, 0)$는 블록 스케일 $S$의 영향을 받지 않고 오직 회전 $R$과 OCS 축 $A$에 의해서만 WCS 변위로 투영됩니다. 소스 코드에서 `Transform3.Placement(new(0,0,0), rotation, new(1,1,1))`를 통해 스케일이 1로 고정됨을 확인하였습니다.
17. **Tilted OCS MINSERT Handling**:
    - Extrusion이 Z축과 다른 경사 OCS의 경우에도 Autodesk Arbitrary Axis Algorithm으로 계산된 축 변환 $A$가 Spacing 변위에 일관되게 적용됩니다.

### 2.5 Resource Bounds & Security Policy
18. **Depth Limit Policy**:
    - 중첩 깊이가 32를 초과하는 즉시 `CadOccurrenceDiagnosticCode.DepthLimitExceeded` 치명적(Fatal) 오류를 기록하고 확장을 중단합니다.
19. **Per-Array Cell Limit Policy**:
    - 단일 MINSERT의 $R \times C$가 10,000을 초과할 경우 셀 전개 전에 즉각 차단하고 `CellLimitExceeded` 치명적 오류를 기록합니다. 정수 오버플로를 방지하기 위해 `checked((long)rows * cols)` 연산을 사용합니다.
20. **Session Occurrence Budget**:
    - 단일 세션 전체에서 누적 생성 개수가 100,000개를 초과할 수 없으며, 100,001번째 요청 시 `SessionBudgetExceeded` 치명적 오류와 함께 차단됩니다.
21. **No-Refund Token Policy**:
    - Occurrence 생성을 시도할 때 슬롯 카운터를 먼저 선점(Reservation)하며, 이후 자식 평가 실패나 오류가 발생하더라도 카운터를 차감(Refund)하지 않습니다.
22. **Diagnostic Exhaustion Guarantee**:
    - 진단 메시지가 최대 한도(`MaxDiagnostics - 1`)에 도달하면 마지막 슬롯을 `DiagnosticLimitExhausted` 코드로 예약하여 진단 누락 여부를 호출자에게 명확히 전달합니다.

### 2.6 Cycle Detection & Graph Traversal
23. **Active-Path Cycle Detection**:
    - 단순 전역 방문 집합이 아닌 현재 탐색 중인 조상 경로(`activeAncestors`) 상의 블록 정의 인덱스 중복만을 검사합니다.
24. **Multi-Parent Shared Block Support (DAG)**:
    - 서로 다른 브랜치에서 동일한 블록 정의를 재사용하는 DAG(유향 비순환 그래프) 구조를 정상 지원합니다.
25. **Iterative Traversal (Stack Safety)**:
    - 재귀 호출 대신 명시적인 `Stack<ExpansionFrame>`을 사용한 반복적 DFS를 채택하여 스택 오버플로 취약점을 원천 방지하였습니다.

### 2.7 Failure & Cancellation Policy
26. **Deterministic Failure Semantics**:
    - 치명적 오류 발생 시 상태는 즉시 `Failed`로 전이되며, 결정론적 진단 목록이 포함된 결과 객체를 반환합니다.
27. **Cancellation Semantics**:
    - 모든 주요 루프와 경계에서 `cancellationToken.ThrowIfCancellationRequested()`를 호출하며, 취소 시 부분 아티팩트를 남기지 않고 즉각 탈출합니다.

### 2.8 Controlled-Source Honesty & QA Verification
28. **Real Controlled-Source Honesty**:
    - 통제된 실 도면 `DtoB_TEST_2STORY_R2018.dxf`에는 17개의 루트 INSERT와 8개의 DIMENSION 내부 미참조 INSERT가 존재하며 MINSERT는 0개임을 정확히 명시하였습니다.
29. **Synthetic Fixture Exhaustiveness**:
    - Cyclic DAG, 33-level Deep Nesting, $101 \times 101$ MINSERT Over-budget, Tilted OCS MINSERT, Non-uniform Scale Reflection 등 모든 코너 케이스를 합성 테스트로 정밀 검증하였습니다.
30. **Invariant Culture & Digest Stability**:
    - `InvariantCulture`, `fr-FR`, `ko-KR` 환경에서 04E 증거 해시 `143684A78B37AA4962345A28FC8C268E24CB4ECDCDF2EBD410B66EC99A1B626B`가 100% 일치함을 확인하였습니다.
31. **Regression Suite Integrity**:
    - 전체 1188개 테스트 중 실패 0, 건너뜀 0으로 100% 통과하였습니다 (실행 시간 약 5.7초).
32. **Protected Codebase Immutability**:
    - `Cad.Raw`, `Cad.Dxf`, `Core`, `Geometry`, `Analysis`, `Desktop`, `Revit` 및 86개 테스트 fixture의 바이트 변조가 전무합니다.

### 2.9 Next Phase Readiness
33. **04F Boundary Readiness**:
    - 04E의 출력(`CadOccurrenceExpansionResult`)은 04F의 지오메트리 변환 및 속성 상속 엔진이 요구하는 모든 컨텍스트(Path, AccumulatedTransform, RawHandles, Definition)를 완벽하게 제공합니다.
34. **No Premature Feature Leak**:
    - 지오메트리 점 변환이나 색상 파싱 등의 04F 영역 코드가 단 한 줄도 사전 작성되지 않았습니다.

---

## 3. Findings & Defect Categorization

- **BLOCKER**: 0건
- **HIGH**: 0건
- **MEDIUM**: 0건
- **LOW**: 0건

---

## 4. Final Review Verdict

```text
================================================================================
FINAL VERDICT: PHASE_04E_PASS
================================================================================
- 04D Seam Integrity: PRESERVED (04D Evidence Hash: 02045C202CAC...)
- Transform Composition: RIGHT-ASSOCIATIVE COLUMN-VECTOR VERIFIED
- MINSERT Spacing: SCALE-INDEPENDENT VERIFIED
- Occurrence Identity: DETERMINISTIC CAD2 CANONICAL VERIFIED
- Resource Bounds & No-Refund: 32 Depth / 10k Cells / 100k Budget VERIFIED
- Active-Path Cycle Detection: DAG SUPPORTED & CYCLE TRAPPED
- Controlled Fixture Honesty: AUDITED & CONFIRMED
- Solution Regression: 1188 / 1188 PASS (0 failed, 0 skipped)
================================================================================
```
