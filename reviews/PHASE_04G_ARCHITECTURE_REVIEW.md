# PHASE 04G — PRE-IMPLEMENTATION CONTRACT CLARIFICATION
## INDEPENDENT ARCHITECTURAL REVIEW

- **Review Date**: 2026-10-16
- **Reviewer**: Antigravity (Independent Architecture & QA Gatekeeper)
- **Subject Gate**: PHASE 04G Pre-Implementation Contract & Policy Clarification
- **Inspected Reports**:
  - `docs/status/PHASE_04G_REPORT.md` (Codex Pre-Implementation Gate Report)
  - `docs/status/PHASE_04F_REPORT.md` & `reviews/PHASE_04F_REVIEW.md`
  - `docs/status/PHASE_04E_REPORT.md` & `reviews/PHASE_04E_REVIEW.md`
  - `docs/status/PHASE_04A_REPORT.md` & `reviews/PHASE_04A_REVIEW.md`
- **Inspected ADRs**:
  - `ADR-018`: CAD IR Normalization Boundary
  - `ADR-019`: Coordinate Normalization Transforms
  - `ADR-020`: Occurrence Identity and Expansion
- **Inspected Frozen Contracts**:
  - `packages/DtoB.Cad/V2/` (`CadDocument.cs`, `CadIdentity.cs`, `CadTransform.cs`, `CadStyleContracts.cs`, `CadGeometryContracts.cs`, `CadProvenance.cs`, `CadIrJson.cs`)
  - `packages/DtoB.Cad.Normalize/Occurrences/`
  - `packages/DtoB.Cad.Normalize/Styles/`
- **Final Verdict**: **`PHASE_04G_ARCHITECTURE_PASS`**

---

## 1. Executive Summary & Verdict

PHASE 04G 구현 착수 전, Codex가 선제적으로 제기한 두 가지 핵심 계약/정책 질문(AG-04G-01: Orientation 부호의 직렬화 보존 여부, AG-04G-02: Occurrence 종속 유효 스타일 바인딩 모델) 및 부차적 경계(Bounds, Header Units)에 대한 독립적인 아키텍처 검토를 완료하였습니다.

### 핵심 판정 요약
1. **AG-04G-01: Orientation 부호 직렬화 불필요 확인**:
   - `CadBlockOccurrence`의 권위 있는 직렬화 안착(Placement) 데이터는 불변 아핀 변환 행렬인 `LocalTransform` 및 `AccumulatedTransform`입니다.
   - 04E 런타임의 `LocalOrientationSign` 및 `AccumulatedOrientationSign`은 정규화 단계의 수치 무결성 검증 및 QA 증거이며, `CadBlockOccurrence`에 별도 직렬화 멤버로 저장할 필요가 없습니다.
   - 어떠한 승인된 ADR도 방향 부호의 별도 직렬화 필드를 요구하지 않으며, 행렬식 부호 $\operatorname{sgn}(\det(M))$를 통한 수치적 동등성이 완벽히 보장됩니다.
2. **AG-04G-02: "Source-Template vs. Projected-Leaf" 엔터티 모델 확정**:
   - 지오메트리(`CadGeometry`)는 정의 단위로 단 1회 생성되어 완벽히 공유됩니다(No Geometry Duplication).
   - 블록 정의는 원본 팩트 선언 스타일을 담은 **Source/Template CadEntity**(`ChildEntityIds`)를 유지합니다.
   - 실제 모델 공간의 Occurrence 컨텍스트별 지오메트리는 동일한 `GeometryId`를 가리키면서 04F의 Occurrence별 유효 `StyleId`를 바인딩하는 **Projected-Leaf CadEntity**를 통해 구체화됩니다.
   - 투영 엔터티는 이미 동결 계약에 정의된 `CadIdentityDomain.Leaf` 도메인과 `(SourceEntityIdentity, OccurrenceIdentity)` 튜플 프레이밍을 통해 완벽히 결정론적인 식별자를 부여받습니다.
3. **스키마 및 코덱 불변 (Strict Zero Schema Change)**:
   - `SchemaVersion = 2`가 유지되며, `schemaVersion3`이나 `CadIrJson` 엄격 모드 완화, 미공개 사이드카 레지스트리 도입은 일절 허용되지 않습니다.
   - Bounds는 스키마 멤버가 아닌 최종 레지스트리 기반의 분석적 쿼리(Analytic Query)로 제공됩니다.
   - Header Units는 이미 계약상 `Evidence`가 nullable이므로 허위 `SourceRecordRef` 날조 없이 무결하게 성립합니다.
4. **04G 구현 즉시 재개 승인**:
   - 기존 04A~04F의 모든 구현 및 다이제스트를 수정할 필요 없이, 동결된 CAD v2 계약 내에서 100% 무결하게 04G 구현을 진행할 수 있음을 확인하였습니다.

**최종 판정**: **`PHASE_04G_ARCHITECTURE_PASS`** (BLOCKER: 0, HIGH: 0, MEDIUM: 0, LOW: 0)

---

## 2. 세부 아키텍처 검토 및 결정 매트릭스

### 2.1 AG-04G-01 Orientation Decision (방향 부호 직렬화 결정)
- **결정**: `CadBlockOccurrence`에 별도의 `localOrientationSign` 또는 `accumulatedOrientationSign` 직렬화 필드를 추가하지 않는다.
- **근거**:
  - 이전 04G 지침의 "Occurrence orientation sign 보존" 문구는 구현 참고용 서술이었을 뿐, ADR-019나 ADR-020 어디에도 직렬화 DTO 멤버 추가를 명시한 승인된 요구사항이 존재하지 않습니다.
  - 04E 런타임 결과물인 `CadExpandedOccurrence`는 정규화 파이프라인 내부 QA 증거로서 해당 부호를 유지하며, 04G 조립/검증 시 이를 적극 활용할 수 있습니다.
  - 그러나 영속화되는 CAD v2 `CadBlockOccurrence`의 Source of Truth는 $4 \times 4$ 아핀 행렬인 `LocalTransform`과 `AccumulatedTransform`입니다.

### 2.2 Transform-as-Persisted-Truth Analysis (변환 행렬의 권위성 분석)
- ADR-019에 따라 3차원 아핀 변환 행렬 $M = \begin{bmatrix} A_{3 \times 3} & \mathbf{t} \\ \mathbf{0}^T & 1 \end{bmatrix}$은 이동(Translation), 회전(Rotation), 스케일(Scale), 전단(Shear) 및 반전(Reflection/Handedness)을 수학적으로 완전하게 표현합니다.
- 선형 변환 행렬 $A$의 행렬식 부호 $\operatorname{sgn}(\det(A)) \in \{+1, -1\}$는 기하학적 패리티(Orientation parity)를 유일하게 결정합니다.
- 변환 행렬과 독립된 스케일 부호 필드를 직렬화 스키마에 중복 배치하는 것은 수치적 다중 진실(Multiple Sources of Truth)을 초래하여 역직렬화 시 불일치 위험을 유발합니다. 따라서 아핀 행렬 단일 저장이 아키텍처상 가장 우수하고 정합합니다.

### 2.3 Runtime Orientation-Sign Status (런타임 방향 부호의 위상)
- `CadExpandedOccurrence.LocalOrientationSign` 및 `AccumulatedOrientationSign`은 런타임 수치 일관성 검증용으로 유지됩니다.
- 04G 어셈블러는 Occurrence 노드를 빌드할 때 행렬식 부호와 04E의 누적 방향 부호가 일치하는지 단언(Assert/Validate)할 수 있습니다.
- 직렬화 모델(`CadBlockOccurrence`)로 전이될 때 이 부호는 영속화되지 않으며, 이는 04E 아키텍처 의도와 완벽히 합치합니다.

### 2.4 AG-04G-02 Style-Binding Decision (발생 종속 스타일 바인딩 결정)
- **결정**: "Source-Template Entity vs. Projected-Leaf Entity" 모델을 채택한다.
- **근거**:
  - 04F의 결과 동일한 블록 내 원본 엔터티 $E$가 발생 컨텍스트 $A$에서는 Red, $B$에서는 Blue로 해석될 수 있음을 증명하였습니다.
  - `CadEntity`는 단 1개의 `StyleId`를 가지므로, 단일 `CadEntity`에 상충하는 복수의 스타일을 부여할 수 없습니다.
  - 따라서 지오메트리(`CadGeometry`)는 1개로 유지하되, 모델 공간에 구체화되는 Occurrence별 엔터티 래퍼(Projected Leaf)를 분리합니다.

### 2.5 Source-Template Entity Semantics (소스 템플릿 엔터티 의미론)
- DXF BLOCKS 섹션에 선언된 원본 엔터티 1개를 나타냅니다.
- `CadBlockDefinition.ChildEntityIds`에 등록됩니다.
- 공유 지오메트리 `GeometryId = G1`을 소유/참조합니다.
- 원본 엔터티 레코드의 불변 소스 프로비넌스(`SourceRecordRef`)를 유지합니다.
- 발생 컨텍스트(Occurrence)에 의해 값이 변이(Mutation)되지 않습니다.
- `CadModelSpaceRecord`에 직접 등록되지 않습니다.

### 2.6 Projected-Leaf Entity Semantics (투영 리프 엔터티 의미론)
- 실제 모델 공간의 특정 Occurrence 컨텍스트 하위에서 인스턴스화된 엔터티를 나타냅니다.
- 템플릿 엔터티와 **완벽히 동일한 `GeometryId = G1`**을 참조합니다 (지오메트리 복제 금지).
- 04F에서 해석된 Occurrence 종속 유효 스타일인 `StyleId`를 참조합니다 (예: $EA$는 RedStyle, $EB$는 BlueStyle).
- 원본 엔터티의 소스 프로비넌스 및 발생 계보를 유지합니다.
- `CadModelSpaceRecord(EntityId, OccurrenceId)`에 명시적으로 등록됩니다.
- `CadBlockDefinition.ChildEntityIds`에는 **절대 등록되지 않습니다**.

### 2.7 Projected Entity Identity Framing (투영 엔터티 식별자 프레이밍)
- **도메인**: `packages/DtoB.Cad/V2/CadIdentity.cs`에 이미 동결되어 있는 **`CadIdentityDomain.Leaf`**를 사용합니다.
- **튜플 구성**:
  $$\text{LeafId} = \operatorname{CadId.Create}\left(\text{CadIdentityDomain.Leaf}, \left[\text{SourceEntityIdField}, \text{OccurrenceIdField}\right]\right)$$
- **결정론 및 비유출 원칙**:
  - 색상, 선종, 스타일 ID, 변환 행렬 수치, 월드 좌표, 바운딩 박스, 타임스탬프, 사전 순회 순서 등은 식별자 해싱 튜플에 **일절 포함되지 않습니다**.
  - 순수하게 소스 엔터티의 정적 식별자와 04E Occurrence 식별자의 결합만으로 생성되므로 완벽한 결정론과 역추적성을 보장합니다.

### 2.8 Template StyleId Policy (템플릿 엔터티 스타일 정책)
- `CadBlockDefinition`에 속한 Source-Template `CadEntity`의 `StyleId`는 **원본 소스에 선언된 사실적 스타일 상태(Declared/Unresolved Factual Style)**를 나타냅니다.
- 04F의 `CadDeclaredStyle`을 기반으로 `CadPropertyMode`(`Explicit`, `ByLayer`, `ByBlock`, `Absent`) 및 `CadResolutionState`를 충실히 반영한 카탈로그 엔터티를 가리킵니다.
- **금지 사항**: 첫 번째 Occurrence의 유효 스타일(예: Red)을 공유 템플릿의 `StyleId`로 임의 할당하는 것을 엄격히 금지합니다.

### 2.9 Root Entity Policy (루트 엔터티 정책)
- **동결된 단일 규칙**: **규칙 A(Direct Source CadEntity)를 적용**합니다.
- **근거**:
  - 모델 공간(Root)의 비-블록 엔터티는 블록 정의에 포함되지 않으며, 모델 공간 내에서 유일하고 확정적인 1개의 유효 스타일 컨텍스트를 가집니다.
  - 여기에 불필요한 템플릿/리프 이중화를 적용할 경우, 정의에 속하지 않는 고아(Orphan) 템플릿 엔터티가 `Entities`에 누적되어 식별자 낭비와 비일관성을 유발합니다.
  - 따라서 루트 엔터티는 단일 `CadEntity`(Domain: `Source`)로 생성되어 자신의 04F 유효 `StyleId`를 직접 갖고, `CadModelSpaceRecord(rootEntity.Id, null)`로 등록됩니다.

### 2.10 Definition.ChildEntityIds Semantics (블록 정의 하위 엔터티 의미론)
- `CadBlockDefinition.ChildEntityIds`는 오직 해당 블록 정의에 직속된 **Source-Template CadEntity ID 목록만을 보관**합니다.
- Occurrence 확장에 따라 동적으로 생성되는 Projected-Leaf Entity ID는 블록 정의에 절대 포함되지 않으며, 이를 통해 블록 정의의 순수성과 불변 공유성을 유지합니다.

### 2.11 ModelSpaceRecord Semantics (모델 공간 레코드 의미론)
- `CadModelSpaceRecord(CadId entityId, CadId? occurrenceId)`는 기존 형태 그대로 사용됩니다:
  - **루트 엔터티**: `(rootEntity.Id, null)`
  - **블록 발생 인스턴스**: `(projectedLeafEntity.Id, occurrence.Id)`
- `CadModelSpaceRecord`에 `StyleId` 필드를 추가하거나 병렬 바인딩 멤버를 신설할 필요가 전혀 없습니다.

### 2.12 Shared GeometryId Proof (지오메트리 공유성 증명)
- `CadDocument.ValidateStructure()`(라인 161~162)는 다음과 같이 검증합니다:
  ```csharp
  foreach (var entity in Entities)
  { Provenance.Resolve(entity.Source); Evidence(entity.Evidence); Optional(entity.GeometryId, geometryIds); Optional(entity.StyleId, styleIds); }
  ```
- 이 검증은 `GeometryId`가 `Geometry` 목록에 존재하는지만 확인할 뿐, 엔터티와 지오메트리 간 1:1 대응을 요구하지 않습니다.
- 따라서 $N$개의 Projected-Leaf `CadEntity`가 단 1개의 `CadGeometry`를 공유 참조하는 것은 코덱 및 검증 로직상 100% 합법적입니다.

### 2.13 Strict Codec Compatibility (엄격 코덱 호환성)
- `CadIrJson.cs`의 `JsonUnmappedMemberHandling.Disallow` 및 `ValidateShape` 검증을 완벽히 통과합니다.
- 알려지지 않은 JSON 멤버(`accumulatedOrientationSign`, `styleId` in ModelSpace 등)가 전혀 생성되지 않으므로 역직렬화 예외가 원천 방지됩니다.

### 2.14 Bounds Query Decision (바운딩 박스 쿼리 정책)
- `CadDocument`, `CadGeometry`, `CadBlockOccurrence` 어디에도 영속화된 `Bounds` 필드를 추가하지 않습니다.
- 바운딩 박스는 정규화 완료 후 `CadDocument`의 최종 레지스트리(`Geometry`, `Occurrences`, `ModelSpace`)를 순회하여 계산되는 **분석적 쿼리(Analytic Query)**로 제공됩니다.
- 신뢰할 수 없는 DXF 헤더 `$EXTMIN` / `$EXTMAX` 값은 지오메트리 바운드 연산에 일절 개입할 수 없습니다.

### 2.15 Header Evidence Decision (헤더 단위 증거 정책)
- `CadUnitDecision.Evidence`는 동결된 계약상 이미 `CadSourceFactRef?`로 nullable입니다 (`CadContract.cs` 라인 80).
- `CadDocument.ValidateStructure()`(라인 180) 역시 `if (Units.Evidence is not null)`로 검사하므로 `null` 증거가 합법적으로 허용됩니다.
- 향후 전용 헤더 로케이터 계약이 승인되기 전까지, 허위의 `SourceRecordRef`를 날조하지 않고 `Evidence = null`과 단위 결정 메타데이터(`DecisionId`, `Reason`, `SourceUnitCode`, `SourceToMillimeters`)를 조합하여 처리합니다.

### 2.16 Schema / Version Decision (스키마 버전 불변 결정)
- `schemaVersion = 2`를 엄격히 유지합니다.
- `schemaVersion3` 승인 없음.
- 04A의 고정 증거 해시(`contract-*.json` 등)가 무효화되지 않습니다.

### 2.17 Compatibility with ADR-018 / 019 / 020
- **ADR-018**: 정규화 경계 내 불변 데이터 모델 및 소스 레지스트리 프로비넌스 완벽 준수.
- **ADR-019**: 4x4 아핀 행렬 기반의 좌표계 변환 및 불변 연산 체계 준수.
- **ADR-020**: 정의/발생 분리, 도메인 분리 `cad2:` 식별자 프레이밍, 자원 한도 집행 완벽 준수.

---

## 3. Findings & Defect Categorization

- **BLOCKER**: 0건
- **HIGH**: 0건
- **MEDIUM**: 0건
- **LOW**: 0건

*(참고: Codex가 구현 착수 전 선제적으로 계약 게이트를 작동하여 중단하였으므로, 프로덕션 결함은 존재하지 않으며 아키텍처 해석이 명확히 정립되었습니다.)*

---

## 4. Exact 04G Implementation Constraints (Codex 구현 지침)

Codex는 04G 구현 시 다음 제약 사항을 엄격히 준수해야 합니다:
1. `CadBlockOccurrence`에 어떠한 신규 필드도 추가하지 말 것 (`LocalTransform`, `AccumulatedTransform`만 영속화).
2. 04E `LocalOrientationSign` 및 `AccumulatedOrientationSign`은 조립 시 단언(Validation)용으로만 소비할 것.
3. 지오메트리(`CadGeometry`)를 Occurrence별로 복제하지 말고 단 1회 생성하여 공유할 것.
4. 블록 정의 자식 요소는 Source-Template `CadEntity`로 `BlockDefinition.ChildEntityIds`에 등록하고, 그 `StyleId`는 원본 선언 스타일을 반영할 것.
5. Occurrence별 구체화 요소는 Projected-Leaf `CadEntity`로 생성하고, `GeometryId`는 템플릿과 공유하며 `StyleId`는 04F 유효 스타일을 바인딩할 것.
6. Projected-Leaf ID는 `CadId.Create(CadIdentityDomain.Leaf, [SourceEntityId, OccurrenceId])`로 생성할 것.
7. 루트 엔터티는 단일 `CadEntity`로 생성하여 `ModelSpace`에 `(rootEntity.Id, null)`로 등록할 것 (불필요한 템플릿/리프 이중화 금지).
8. 바운딩 박스는 스키마 필드가 아닌 문서 분석 쿼리 메서드로 구현할 것.
9. 헤더 단위의 `Units.Evidence`는 허위 레코드를 날조하지 말고 `null`로 둘 것.
10. `CadIrJson` 엄격 모드 및 `SchemaVersion = 2`를 100% 유지할 것.

---

## 5. Final Review Verdict

```text
================================================================================
FINAL VERDICT: PHASE_04G_ARCHITECTURE_PASS
================================================================================
- AG-04G-01 Orientation: TRANSFORM-AS-PERSISTED-TRUTH CONFIRMED (No schema change)
- AG-04G-02 Style Binding: SOURCE-TEMPLATE VS PROJECTED-LEAF CONFIRMED (Zero geometry clone)
- Leaf Identity Framing: CadIdentityDomain.Leaf (Source + Occurrence) CONFIRMED
- Root Entity Policy: Direct Source CadEntity (Option A) FROZEN
- Bounds & Header Evidence: Query-only & Nullable Evidence CONFIRMED
- Strict Schema & Codec: SchemaVersion 2 PRESERVED
- Prior Stage Evidence: 04A-04F STABILITY PRESERVED
- Codex Implementation Gate: CLEARED TO PROCEED WITH 04G IMPLEMENTATION
================================================================================
```
