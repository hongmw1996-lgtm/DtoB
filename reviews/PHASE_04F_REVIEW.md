# PHASE 04F — OCCURRENCE STYLE / LAYER INHERITANCE
## INDEPENDENT IMPLEMENTATION & ARCHITECTURAL REVIEW

- **Review Date**: 2026-10-16
- **Reviewer**: Antigravity (Independent Architecture & QA Gatekeeper)
- **Subject Commit / Target**: PHASE 04F Implementation on `phase/04-cad-normalization`
- **Reviewed Package**: `packages/DtoB.Cad.Normalize/Styles/`
- **Reviewed Tests**: `tests/DtoB.Tests/` (70 new tests, 1258 / 1258 solution PASS)
- **Authoritative Baseline**:
  - `ADR-018`: CAD IR Normalization Boundary
  - `ADR-019`: Coordinate Normalization Transforms
  - `ADR-020`: Occurrence Identity and Expansion
  - `PHASE_04E_REVIEW.md`: `PHASE_04E_PASS`
- **Final Verdict**: **`PHASE_04F_PASS`**

---

## 1. Executive Summary & Verdict

PHASE 04F 구현물(`packages/DtoB.Cad.Normalize/Styles/` 및 관련 단위/회귀 테스트)에 대한 독립적인 아키텍처 및 무결성 감사를 완료하였습니다.

Codex가 구현한 `CadStyleResolver`, `RawStyleAccess`, `StyleContracts.cs`는 ADR-018, ADR-019, ADR-020에 정의된 스타일 상속 및 채널 해석 규격을 완벽하게 충족하며, 04E의 Occurrence 전개 결과 및 불변 Source Registry를 바탕으로 완벽히 결정론적이고 부분적(Partial) 복원력을 갖춘 유효 CAD 스타일 투영을 생성합니다.

### 핵심 판정 요약
1. **04F 영역 무결성 (Strict Scope Enforcement)**:
   - 지원 채널(Layer, ACI/TrueColor, Linetype)에 대한 상속/해석 로직만을 구현하였으며, 04G 소관인 점/곡선 변환(`Transform3`, `Apply`, `Point3`), 불지 곡선 분해, CadDocument 조립, 렌더링, BIM/Revit 연계 코드가 단 한 줄도 포함되지 않았습니다.
2. **Raw Contextual Fact 접근 및 서브클래스 격리**:
   - 기존 파서/스냅샷을 수정하거나 재파싱하지 않고, 보존된 `LexicalEvidence.Pairs` 및 `ContextMarkers`를 활용하여 `AcDbEntity` 및 적합한 심볼 테이블 서브클래스 범위에서만 스타일 팩트를 안전하게 추출합니다.
   - XDATA(1001), 제어 그룹(102), 이종 서브클래스, 특히 MTEXT 내부 배경색 420 및 테이블 충돌을 완벽히 차단하며, 중복 공통 코드는 First-wins가 아닌 `Invalid`로 엄정 처리합니다.
3. **Layer 0 및 ByLayer / ByBlock 상속 수학**:
   - 블록 내부의 Layer 0 요소는 부모 INSERT의 '유효 레이어(Effective Layer)'를 즉시 상속합니다.
   - ByLayer는 유효 레이어 치환 후의 레이어 테이블을 조회하며, 부재(Absent, Defaulted)와 명시적 ByLayer(Inherited)를 엄격히 구분합니다.
   - ByBlock은 부모 INSERT의 유효 채널 값을 유한 조상 체인을 통해 전달하며, 루트 ByBlock 또는 미해결 부모에 대해 임의의 화이트/Continuous 기본값을 날조하지 않고 명시적 `UnresolvedByBlockContext` 진단과 함께 `Unsupported`로 처리합니다.
4. **TrueColor 우선권 및 색상 모드 체계**:
   - 유효 범위(0..0xFFFFFF)의 420 TrueColor는 동시 존재하는 62(ACI, ByLayer, ByBlock 포함)에 항상 우선하며, 420=0(Black) 역시 유효한 색상으로 수용됩니다. 420이 손상/중복/범위 초과된 경우 62로의 묵시적 폴백을 금지하고 `Invalid`로 격리합니다.
5. **공유 정의 격리 (Shared-Definition Isolation)**:
   - 동일 블록 정의 내부의 엔터티 선언(`CadDeclaredStyle`)은 불변으로 재사용되며, 서로 다른 Occurrence 컨텍스트(예: Red/Blue, MINSERT 셀)에 종속된 스타일 결과는 오직 `(SourceRecordRef, OccurrenceId?)` 복합 키를 가진 `CadEffectiveStyle` 투영 인스턴스에만 독립적으로 보관됩니다.
6. **자원 한도 및 예외/취소 정책**:
   - 단일 투영 카운터(`MaxProjections = 250,000`)로 루트, 컨텍스트, 자식 투영을 선점형(No-refund)으로 제한하며, 진단 한도(`MaxDiagnostics = 5,000`) 도달 시 마지막 슬롯을 `ResourceLimitExceeded` 터미널 슬롯으로 보존합니다. 취소 발생 시 부분 아티팩트 발행을 차단(`Artifact = null`)합니다.
7. **통제된 실 도면 사실성 확인**:
   - `DtoB_TEST_2STORY_R2018.dxf`의 158개 선언, 12 레이어, 21 선종, 97개 루트, 17개 INSERT 컨텍스트, 26개 블록 자식(총 140개 투영) 구성을 정직하게 입증하였으며, 실 도면의 12개 레이어 선가중치(370) 보존으로 인한 `Partial` 상태를 투명하게 보고하였습니다.
8. **회귀 및 안정성 검증**:
   - 04F 신규 테스트 70개를 포함하여 전체 **1258 / 1258 PASS** (0 failed, 0 skipped, 5.8s)를 달성하였으며, 04A~04E 이전 단계 증거 다이제스트 및 04F 유효 스타일 다이제스트 `7A448C95EC2F67DCA6C65C3D60B4982E79312814C8C1FD8E5A62C55C611FFC01`가 문화권(`Invariant`, `fr-FR`, `ko-KR`) 및 독립 프로세스 전반에서 100% 일치합니다.

**최종 판정**: **`PHASE_04F_PASS`** (BLOCKER: 0, HIGH: 0, MEDIUM: 0, LOW: 0)

---

## 2. 34개 세부 검토 항목 감사 결과

### 2.1 Scope Audit (영역 감사)
- 허용 범위인 유효 레이어, Layer 0 상속, ACI/TrueColor 모드, ByLayer/ByBlock, 선종 식별, 투영 컨텍스트, 프로비넌스, 부분 해결, 결정론적 진단, 한도 집행만을 순수 인메모리로 구현하였습니다.
- 금지 범위인 기하 변환(`Transform3`, `Apply`, `Point3`, `Line`, `Arc`, `Circle`, `Polyline`), 지오메트리 평탄화, 불지 곡선 변환, 바운딩 박스, 텍스트/치수 렌더링, 최종 `CadDocument` 조립, BIM/Revit 연계가 완전히 배제되었습니다.

### 2.2 Raw Contextual-Style Access (원시 컨텍스트 스타일 접근)
- `RawStyleAccess.cs`는 `LexicalEvidence.Pairs`와 `ContextMarkers`를 단 1회 스캔하여 그룹 2, 6, 8, 60, 62, 70, 370, 420, 430, 440 팩트를 수집합니다.
- 제어 블록(`{APP ... }`), XDATA(1001), 이종 서브클래스 범위를 엄격히 제외하며, 중복 출현하는 공통 코드는 First-wins가 아닌 `Validity = Invalid`로 판정합니다.

### 2.3 Supported / Deferred Channel Matrix (지원 및 유예 채널 매트릭스)
- **지원 채널**: Layer (이름/카탈로그 레코드), Color (ACI 1~255 및 24비트 TrueColor RGB), Linetype (이름 및 LTYPE 레코드 참조).
- **유예 채널**: Lineweight (370), Transparency (440), Color-book Name (430).
  - 유예 채널 팩트는 원본 그대로 보존되며, 존재 시 `UnsupportedStyleChannel` 진단을 남기고 `Unsupported` 상태로 기록됩니다. 임의의 선가중치 수치 해석이나 투명도 알파 합성을 날조하지 않습니다.

### 2.4 Declared-Style Immutability (선언 스타일 불변성)
- `CadDeclaredStyle`은 소스 구조 행마다 1회 생성되어 불변으로 유지됩니다.
- 유효 스타일 해석 과정에서 선언 값이 덮어써지지 않으며, 동일 선언 객체가 복수의 Occurrence 투영에서 안전하게 공유됩니다.

### 2.5 Occurrence-Context Ordering (발생 컨텍스트 순서)
- 04E의 깊이 우선 탐색(DFS) 순서를 엄격히 준수합니다.
- 부모 INSERT 컨텍스트가 반드시 자식 INSERT 및 자식 엔터티보다 먼저 해석 및 캐시(`contexts`)되도록 강제(`CadContract.Require(contexts.TryGetValue(parentId.Value, out parent))`)하여, 미해결 부모 상태를 읽는 레이스 컨디션을 원천 방지하였습니다.

### 2.6 Layer Lookup (레이어 카탈로그 조회)
- `StringComparer.OrdinalIgnoreCase`를 사용하여 불변 사전에 색인합니다.
- 원본 선언 철자를 보존하면서 고유 매칭된 카탈로그의 정규 철자를 유효 이름으로 채택합니다.
- 대소문자 충돌(Duplicate/Collision) 발생 시 First-wins를 배제하고 `AmbiguousLayerDefinition`으로 처리하며, 선행 공백 트리밍이나 터키어 문화권 폴딩에 취약하지 않습니다.

### 2.7 Layer 0 Semantics (Layer 0 상속 규격)
- 블록 정의 내부 엔터티의 선언 레이어가 정확히 `"0"`인 경우, 포함하는 INSERT의 유효 레이어를 상속(`DXF.Layer.ZeroFromInsert`)합니다.
- 블록 정의 내부의 0이 아닌 고유 레이어 엔터티는 부모 INSERT와 무관하게 자신의 레이어를 유지합니다.
- 모델 공간(Root)의 Layer 0은 가공의 부모가 아닌 실제 카탈로그 Layer 0 엔터티를 참조합니다.

### 2.8 Nested Layer 0 (중첩 Layer 0 체인)
- 루트 A(A-INST) → 중첩 D(0) → 엔터티 E(0) 구조에서 D와 E 모두 A-INST로 정상 분해됩니다.
- D가 C-INST를 선언하면 E는 직전 부모 D의 유효 레이어인 C-INST를 정확히 상속합니다. 임의의 조상 탐색 없이 직전 부모 컨텍스트를 사용합니다.

### 2.9 ByLayer (ByLayer 해결 규격)
- 레이어 치환(Layer 0 substitution)이 완료된 후의 '유효 레이어'를 기준으로 ByLayer 색상 및 선종을 조회합니다.
- 부재(Absent, 62/6 생략)는 `DXF.Color.AbsentByLayer` / `Defaulted`로, 명시적 `256`/`"BYLAYER"`는 `DXF.Color.ByLayer` / `Inherited`로 명확히 분리하여 원본 의도를 왜곡 없이 보존합니다.

### 2.10 ByBlock (ByBlock 해결 규격)
- 엔터티의 62=0 또는 Linetype `"BYBLOCK"`은 포함하는 INSERT의 이미 해결된 유효 채널 값을 상속(`DXF.Color.ByBlock`)합니다.
- 엔터티의 고유 레이어가 0이 아니더라도 부모로부터의 ByBlock 색상 상속은 독립적으로 유지됩니다.

### 2.11 Nested Mixed Inheritance (중첩 혼합 상속 오라클)
- A (명시적 ACI 1) → D (레이어 C-INST, Color ByLayer, 레이어 색상 ACI 5) → E (Layer 0, Color ByBlock) 구조에서:
  - E의 유효 레이어는 C-INST, 유효 색상은 A의 1이 아닌 D의 유효 색상인 ACI 5로 정확히 해석됨을 핸드 오라클 검증으로 확인하였습니다.

### 2.12 Color Modes (색상 모드 체계)
- `CadDeclaredColorMode`: Aci(1~255), Rgb(0..0xFFFFFF), ByLayer(256), ByBlock(0), Absent, Invalid, Unsupported로 완벽히 분리됩니다.
- 0과 256은 일반 팔레트 인덱스가 아닌 모드로 격리되며, ACI를 임의의 RGB로 근사하거나 Revit 재료로 변환하지 않습니다.

### 2.13 TrueColor Precedence (TrueColor 우선권)
- 그룹 420이 유효 정수(0..0xFFFFFF)로 존재하면 동시 존재하는 그룹 62(ACI, ByLayer, ByBlock)에 무조건 우선합니다.
- 420=0(Black)은 부재가 아닌 유효한 검은색으로 해석됩니다.
- 420이 중복/손상/범위 초과된 경우 62로의 무단 폴백을 금지하고 `Invalid`로 거부하며, 62/420/430 원본 팩트는 프로비넌스 증거로 보존됩니다.

### 2.14 MTEXT / Subclass Collision Filtering (MTEXT 및 서브클래스 충돌 필터링)
- `AcDbMText` 고유 배경색 그룹 420 및 확장 서브클래스 62/420 코드가 공통 엔터티 색상으로 오인되지 않도록 `ContextMarkers` 서브클래스 검사를 통해 원천 차단하였습니다.

### 2.15 Layer Off / Frozen Semantics (레이어 Off / Frozen 메타데이터)
- 음수 레이어 ACI(예: -5)는 절대값(5)을 팔레트 인덱스로 사용하되 `IsOff = true` 플래그를 분리 보존합니다.
- 플래그 70의 1번 비트는 `IsFrozen = true`로 분리 보존됩니다.
- 엔터티 ACI에는 음수 인덱스가 허용되지 않으며(`Invalid`), LTYPE 엔터티에는 Off/Frozen 개념이 도입되지 않음(`null`)을 확인하였습니다.

### 2.16 Linetype Resolution (선종 해석)
- 선종은 오직 정규화된 이름과 고유 LTYPE 카탈로그 레코드 참조만으로 해석됩니다.
- 대시 지오메트리 전개, 패턴 스케일 계산, 폰트/셰이프 로딩 등의 렌더링 행위는 철저히 배제되었습니다.

### 2.17 Deferred Lineweight / Transparency / Color-Name (유예 채널 처리)
- 370, 440, 430은 원본 정수/텍스트 팩트가 완벽히 보존되나, 유효 값 조립은 보류되고 `UnsupportedStyleChannel` 진단과 함께 `Unsupported` 상태로 기록됩니다.

### 2.18 Partial-Channel Behavior (채널 독립적 부분 복원력)
- 레이어/선종 카탈로그가 누락되거나 오류가 발생하더라도 독립적인 명시적 색상 채널은 온전히 `Known`으로 해결됩니다.
- 하나의 채널 실패가 다른 유효 채널 결과를 파괴하지 않는 다채널 독립성을 보장합니다.

### 2.19 Complete vs. Partial Semantics (완료 및 부분 상태 의미론)
- `Complete`는 지원 스타일 채널의 무결한 전개 완료를 의미하며, 유예 채널(370 등) 진단이 발생하거나 04E가 Partial이었던 경우 최종 상태는 정직하게 `Partial`로 전이됩니다.

### 2.20 Provenance / Rule Chain (프로비넌스 및 인과 추적 체인)
- 모든 유효 스타일 속성은 불변 인과 규칙(`RuleId`), 소스 팩트(`Evidence`), 참조 카탈로그 레코드(`CatalogRecord`), 부모 투영 키(`SupportingContext`)를 유지하여 완벽한 역추적성을 제공합니다.

### 2.21 Shared-Definition Isolation (공유 정의 격리 검증)
- 단일 블록 B 내부 엔터티 E(Layer 0, ByBlock)가 서로 다른 INSERT A(A-INST, ACI 1) 및 D(C-INST, ACI 5)로 인스턴스화될 때, 원본 E의 선언과 블록 정의는 불변으로 공유되면서 각 투영 인스턴스는 완벽히 격리된 유효 스타일(A-INST/1 vs C-INST/5)을 가집니다.

### 2.22 Resource / Cancellation Behavior (자원 한도 및 취소 처리)
- 단일 투영 카운터(`MaxProjections = 250,000`) 및 진단 카운터(`MaxDiagnostics = 5,000`)를 엄격히 집행하며, 선점 후 환불 불가(No-refund) 원칙을 적용합니다.
- 취소 시 부분 아티팩트 발행을 금지하고 즉시 `Cancelled` 상태와 `Artifact = null`을 반환합니다.

### 2.23 Controlled-Source Factual Inventory (통제된 소스 사실 목록)
- 공식 DXF(`DtoB_TEST_2STORY_R2018.dxf`):
  - 선언: 158개
  - 레이어 테이블: 12개
  - 선종 테이블: 21개
  - 루트 투영: 97개
  - INSERT 컨텍스트: 17개
  - 블록 자식 투영: 26개
  - **총 투영 수**: 140개
- 위 수치가 실제 스냅샷 팩트와 100% 일치함을 독립적으로 검증하였습니다.

### 2.24 Controlled-Source Style Proof (통제된 소스 스타일 증명)
- D_SINGLE_900 내부 LINE 9C (INSERT C3 발생 하위):
  - 선언 레이어: A-DOOR (페어 2860), 62/6 부재(ByLayer).
  - 유효 결과: 레이어 A-DOOR, 색상 ACI 3 (LAYER 8B 페어 1164 참조), 선종 Continuous (LTYPE 26 참조).
  - ByBlock 상속 주장이 아닌 정직한 ByLayer 증명임을 확인하였습니다.

### 2.25 Self-Oracle Review (테스트 오라클 검토)
- 70개 신규 테스트의 기대값은 `CadStyleResolver`의 결과로부터 추출한 자기 복제 오라클이 아니며, DXF 규격서 및 수기 계산에 기반하여 작성되었습니다.

### 2.26 Raw / Dxf Immutability (원시/DXF 불변성)
- `DtoB.Cad.Raw` 및 `DtoB.Cad.Dxf` 프로덕션 소스 및 86개 테스트 fixture의 해시 변조가 전무(0건)합니다.

### 2.27 Prior-Stage Stability (이전 단계 안정성)
- 04A Contract 다이제스트 (`3E49A200...`)
- 04B Scalar 다이제스트 (`568D2A7C...`)
- 04C Coordinate 다이제스트 (`81CA2010...`)
- 04D Placement 다이제스트 (`02045C20...`)
- 04E Occurrence 다이제스트 (`143684A7...`)
- 모든 선행 단계 증거 다이제스트가 변경 없이 유지됩니다.

### 2.28 Geometry Hard Boundary (기하 경계 엄수)
- Styles 프로덕션 코드베이스 내에 어떠한 지오메트리 변환 연산(`Transform3.Apply`, `Then`, `Point3` 생성)도 존재하지 않음을 확인하였습니다.

### 2.29 Revit Independence (Revit 비의존성)
- Revit API, WPF, DirectX, BIM 요소 식별자 등에 대한 어떠한 참조도 존재하지 않습니다.

### 2.30 Regression (회귀 검증)
- 전체 솔루션 단위 테스트 1258개 중 **1258 PASS / 0 FAIL / 0 SKIPPED** (실행 시간: 약 5.8초)를 기록하였습니다.

### 2.31 Build / Warnings (빌드 및 경고 게이트)
- Core 및 Solution Rebuild 0 Error / 0 Compiler Warning.
- 기 승인된 Revit 종속성 관련 MSB3277 경고 3건 외 신규 경고 0건입니다.

### 2.32 Determinism (결정론적 다이제스트)
- 04F 유효 스타일 직렬화 다이제스트 (53,380 bytes):
  ```text
  SHA256: 7A448C95EC2F67DCA6C65C3D60B4982E79312814C8C1FD8E5A62C55C611FFC01
  ```
- `InvariantCulture`, `fr-FR`, `ko-KR` 및 복수 프로세스/작업 디렉터리 환경에서 100% 동일함을 확인하였습니다.

### 2.33 04G Readiness (04G 준비 완료 상태)
- 04F의 출력(`CadStyleArtifact`)은 04G 지오메트리 조립 엔진이 요구하는 소스 참조, 발생 컨텍스트, 유효 레이어/색상/선종, 프로비넌스, 04E 누적 변환 행렬을 온전히 제공하며, 추가 인터페이스 수정 없이 즉시 결합 가능한 상태입니다.

### 2.34 Findings & Defect Categorization (결함 분류)
- **BLOCKER**: 0건
- **HIGH**: 0건
- **MEDIUM**: 0건
- **LOW**: 0건

---

## 3. Final Review Verdict

```text
================================================================================
FINAL VERDICT: PHASE_04F_PASS
================================================================================
- Scope Boundary: PRESERVED (Zero geometry transformations, pure style projection)
- Raw Contextual Fact Access: ISOLATED (Pre-subclass & AcDbEntity only, no collisions)
- Layer 0 / ByLayer / ByBlock: VERIFIED (Exact inheritance math & independent channels)
- TrueColor Precedence: VERIFIED (420 wins over 62, 0 is black, no invalid fallback)
- Shared-Definition Isolation: VERIFIED (Immutable declarations, occurrence-keyed projections)
- Resource Limits & Cancellation: VERIFIED (250k projections / 5k diagnostics terminal slot)
- Controlled Source Proof: CONFIRMED (140 projections, honest ByLayer & Partial status)
- Prior Evidence Stability: PRESERVED (04A/04B/04C/04D/04E digests unchanged)
- Solution Regression: 1258 / 1258 PASS (0 failed, 0 skipped)
================================================================================
```
