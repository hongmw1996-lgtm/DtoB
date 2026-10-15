# PHASE 03C Native Text DXF Raw Geometry Extraction Implementation Review

## Executive Summary

- **Review Target**: PHASE 03C — Native Text DXF Raw Geometry Extraction (`packages/DtoB.Cad.Raw/DxfRawGeometryContracts.cs`, `packages/DtoB.Cad.Dxf/DxfGeometryLimits.cs`, `packages/DtoB.Cad.Dxf/TextDxfGeometryReader.cs`, `packages/DtoB.Cad.Dxf/TextDxfGeometryReader.Polyline.cs`, `tests/DtoB.Tests/TextDxfGeometryReaderTests.cs`)
- **Review Date**: 2026-10-14
- **Review Mode**: Final Implementation Review (with Evidence Consistency Correction)
- **Reviewer**: Architecture / Adversarial Review Owner (Antigravity)
- **Status of Codex**: `PASS_WITH_KNOWN_LIMITATIONS`
- **Final Verdict**: **`PHASE_03C_PASS`**

DtoB의 네이티브 텍스트 DXF 리더 V1 파이프라인의 3단계인 **PHASE 03C (Native Text DXF Raw Geometry Extraction)** 구현 및 테스트 전반에 대한 독립적이고 엄격한 아키텍처/적대적(Adversarial) 검토를 수행하였습니다.

검토 결과, PHASE 03C 구현은 ADR-015, ADR-016 및 AGENTS.md 규칙을 완벽하게 준수하고 있으며, 이전 단계(PHASE 03B)의 불변 구조 모델(`DxfStructuralReadResult`)을 일체 변형하지 않고 순수 읽기 전용 입력으로 소비하여 원시 기하학적 사실(Raw Geometry Facts)을 정확히 추출하고 있음을 확인하였습니다.

본 단계에서는:
1. **임의의 보정·변환 배제**: 각도 라디안 변환, 단위 스케일링(mm/m/inch), 좌표계 변환, 법선 벡터 기본값 날조((0,0,1) 주입 등) 없이 원본 파일의 텍스트 토큰과 Group Code 구조를 100% 무손실로 보존합니다.
2. **엄격한 유효성 판정**: 비양수 반지름(ARC/CIRCLE), 영거리 선분(LINE), 3점 삼각형 솔리드(SOLID), 퇴화 폴리라인(LWPOLYLINE) 등 비정상/특수 형상을 임의로 스냅하거나 수정(Math.Abs 등)하지 않고 원본 사실 그대로 기록하며, 스펙 위반 시 결정론적 진단(Diagnostic)을 발행합니다.
3. **독립 계층성 및 경계 보존**: PHASE 03D(BLOCKS/INSERT), PHASE 03E(Handle Graph), PHASE 04(CAD IR)로의 누수(Leakage)가 전무합니다.

빌드 결과 **오류 0건**, 단위/회귀 테스트 **356/356 전원 통과**, 통제 픽스처 원본 해시 무결성 및 출력 JSON 결정론적 해시 일치를 확인하였으므로 최종 승인(**`PHASE_03C_PASS`**)을 부여합니다.

---

## 1. Independent Verification & Test Execution Evidence

독립적이고 격리된 환경에서 소스 빌드 및 전체 회귀 테스트를 실행하였습니다.

### 1.1 빌드 검증
- **명령**: `MSBuild.exe DtoB.sln /p:Configuration=Release /t:Rebuild /m`
- **결과**: `0 Warning(s)`, `0 Error(s)` (사전 승인된 Revit API 런타임 종속성 어셈블리 경고 MSB3277 2건 외 신규 경고 0건)
- **시간**: 1.62초
- **코드 검사**: `git diff --check` 결과 CRLF/공백 결함 0건

### 1.2 테스트 실행 결과
- **테스트 러너**: `vstest.console.exe` (.NET 8.0 x64)
- **전체 통과 현황**: **총 356개 테스트 중 356개 전원 통과 (Pass 356, Failed 0, Skipped 0)**
  - `TextDxfGeometryReaderTests`: **63개 신규 테스트 전원 통과**
  - `TextDxfStructuralReaderTests`: 103개 기존 구조 테스트 전원 통과
  - `TextDxfLexicalReaderTests`: 55개 렉시컬 테스트 전원 통과
  - 아키텍처 및 종속성 불변 테스트: 9개 전원 통과
  - 이전 단계(Phase 00~02) 회귀 테스트: 126개 전원 통과

### 1.3 결정론적 픽스처 무결성 검증
- **통제 도면 픽스처**: `datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf`
  - 원본 SHA-256: `2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC` (단 1바이트의 변형도 없이 일치)
- **PHASE 03C 추출 결과 직렬화 해시**:
  - `DxfGeometryReadResult` JSON SHA-256: `FB467C3FA0CEE622B8B616B3D04B6EAD608EB3E76CD5FFE703DA2F2D70A85E8E` (일치 확인)

---

## 2. Structural Boundary & Immutability Verification

### 2.0 Evidence Consistency Correction Note (근거 데이터 정합성 보정 기록)
- **보정 사유**: 최초 작성된 검토서의 2.2절 분류 회계 텍스트에서 실측과 다른 오기(LINE 57, LWPOLYLINE 32, INSERT 93, HATCH 50 / TEXT 2)가 포함되어 있었습니다.
- **독립 소스 재검증(Source Audit)**: 통제 도면 원본(`datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf`)의 Group Code 0 경계를 독립적인 BCL 파서로 전수 재검증하였습니다.
- **확인 결과**:
  1. 기하 엔티티 후보는 정확히 **LINE 59, LWPOLYLINE 16, ARC 1, POINT 12, SOLID 1 (총 89개)**임이 입증되었습니다. 이는 Codex의 `docs/status/PHASE_03C_REPORT.md` 및 `tests/DtoB.Tests/TextDxfGeometryReaderTests.cs` (lines 54-56)의 독립 검증 단언문과 100% 일치합니다.
  2. 전체 333개 자식 레코드의 섹션별 세부 타입 및 처분(Disposition) 분류 역시 전수 증명되었으며, `Supported 188`, `DeferredTo03C 0`, `DeferredTo03D 93`, `Unsupported 52`, `Invalid 0` (합계 333개)으로 완벽히 일치합니다.
  3. 프로덕션 코드 및 테스트 상의 결함은 0건이며, 본 수정은 검토서 내의 근거 통계 수치를 원본 소스 팩트에 부합하도록 정정한 것입니다.

### 2.1 입력 불변성 보장 (Input Immutability)
`TextDxfGeometryReader`는 `DxfStructuralReadResult`를 인수로 수신하되, 구조 리더의 내부 컬렉션을 변경(mutate)하지 않습니다:
- 원본 `DxfStructuralReadResult.Records` 및 `DxfStructuralReadResult.Diagnostics`의 인스턴스와 내용은 100% 보존됩니다.
- 03C는 별도의 `EffectiveRecords` 리스트와 `DxfGeometryReadResult`를 생성하여 오버레이(Overlay) 패턴으로 확장합니다.

### 2.2 레코드 분류 오버레이 회계 (Disposition Accounting)
통제 도면(`datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf`) 기준 총 333개 자식 레코드에 대한 독립 소스 감사(Raw code 0 audit) 및 처리 회계 검증:

#### 1) 소스 섹션별 자식 레코드 구성 (총 333개)
- `CLASSES` (15개): CLASS 15
- `TABLES` (88개 엔트리): VPORT 1, LTYPE 21, LAYER 12, STYLE 27, APPID 3, DIMSTYLE 12, BLOCK_RECORD 12
- `BLOCKS` (68개): BLOCK 12, ENDBLK 12, LINE 14, LWPOLYLINE 4, ARC 1, POINT 12, SOLID 1, INSERT 8, MTEXT 4
- `ENTITIES` (114개): LINE 45, LWPOLYLINE 12, INSERT 17, TEXT 36, DIMENSION 4
- `OBJECTS` (48개): DICTIONARY 12, ACDBDICTIONARYWDFLT 1, ACDBPLACEHOLDER 1, LAYOUT 2, MATERIAL 3, MLINESTYLE 1, MLEADERSTYLE 1, DICTIONARYVAR 2, VISUALSTYLE 25
*(합계: 15 + 88 + 68 + 114 + 48 = 333개)*

#### 2) 기하학 후보 엔티티 실측 (총 89개)
- LINE: **59개** (BLOCKS 14 + ENTITIES 45)
- LWPOLYLINE: **16개** (BLOCKS 4 + ENTITIES 12)
- ARC: **1개** (BLOCKS 1 + ENTITIES 0)
- POINT: **12개** (BLOCKS 12 + ENTITIES 0)
- SOLID: **1개** (BLOCKS 1 + ENTITIES 0)
- CIRCLE: **0개**
- Legacy POLYLINE / VERTEX: **0개**
*(기하 후보 합계: 59 + 16 + 1 + 12 + 1 = 89개)*

#### 3) 03B 상태 (구조 리더 시점)
- `Supported` (99개): CLASS 15, LTYPE 21, LAYER 12, STYLE 27, DIMSTYLE 12, BLOCK_RECORD 12
- `DeferredTo03C` (89개): LINE 59, LWPOLYLINE 16, ARC 1, POINT 12, SOLID 1
- `DeferredTo03D` (93개): BLOCK 12, ENDBLK 12, INSERT 25 (BLOCKS 8 + ENTITIES 17), MTEXT 4, TEXT 36, DIMENSION 4
- `Unsupported` (52개): VPORT 1, APPID 3, OBJECTS 48개 (DICTIONARY 12, ACDBDICTIONARYWDFLT 1, ACDBPLACEHOLDER 1, LAYOUT 2, MATERIAL 3, MLINESTYLE 1, MLEADERSTYLE 1, DICTIONARYVAR 2, VISUALSTYLE 25)
- `Invalid` (0개)
*(합계: 99 + 89 + 93 + 52 + 0 = 333개)*

#### 4) 03C 오버레이 후 (Effective Disposition)
- `Supported`: **188개** (기존 심볼/클래스 99개 + 신규 성공 매핑 기하 89개)
- `DeferredTo03C`: **0개** (기하 엔티티 89개 전수 소비 완료)
- `DeferredTo03D`: **93개** (BLOCK 12, ENDBLK 12, INSERT 25, MTEXT 4, TEXT 36, DIMENSION 4 완벽 격리 보존)
- `Unsupported`: **52개** (VPORT 1, APPID 3, OBJECTS 48 불변)
- `Invalid`: **0개**
*(합계: 188 + 0 + 93 + 52 + 0 = 333개)*

회계의 합산은 $188 + 0 + 93 + 52 + 0 = 333$으로 완벽히 일치합니다.

---

## 3. Detailed Entity Implementation & Adversarial Verification

### 3.1 LINE (선형 엔티티)
- **시점/종점 페어링**: Group Code 10, 20, 30(시점)과 11, 21, 31(종점)이 각각 독립적인 `SourcePairIndices`로 정확히 매핑됩니다.
- **영거리 선분(Zero-length line) 처리**: 시점과 종점 좌표가 완전히 동일한 경우(`line-zero-length.dxf`), 인위적으로 선분을 제거하거나 스냅하지 않고 `RawLine` 사실 그대로 온전히 보존합니다.
- **Z값 부재 처리**: Code 30/31이 없는 2D 선분의 경우 Z는 `Presence.Absent`로 유지되며, 임의로 0.0을 주입하지 않습니다.

### 3.2 LWPOLYLINE (경량 폴리라인)
- **상태 머신 및 정점 수집 순서 엄격성**:
  - 정점 좌표는 (Code 10 $\rightarrow$ Code 20) 순서로 수집되어야 정점 상태 머신이 전이됩니다.
  - 정점 속성(Code 40/41 시작/종단 폭, Code 42 불지, Code 91 정점 ID)이 Code 20 이전에 끼어드는 비정상적인 순서인 경우, 임의로 복구하지 않고 `InvalidVertexOrder` 진단을 기록하고 거절합니다.
- **선언 정점 수(Code 90) vs 실제 정점 수 검증**:
  - Code 90의 값과 실제 파싱된 정점 수가 불일치할 경우(`lwpolyline-vertex-count-mismatch.dxf`) `VertexCountMismatch` 진단을 발생시키며 레코드를 무효화합니다.
  - 선언된 정점 수가 0개인 퇴화 폴리라인은 `InvalidVertexCount`로 거절합니다.
- **글로벌 상수 폭(Code 43) vs 정점별 폭(Code 40/41)**:
  - Code 43(일괄 폭)과 정점 레벨 폭(Code 40/41)을 분리된 필드로 정확히 분리 유지하며, 상호 덮어쓰기나 묵시적 전파를 수행하지 않습니다.
- **닫힘 플래그(Code 70 Bit 1)**:
  - Code 70의 비트 1을 해석하여 `IsClosed` 불리언 값을 제공하되, 원본 Code 70 정수값 역시 원본 사실로 보존합니다.

### 3.3 ARC (원호 엔티티)
- **단위 보존 (각도)**:
  - 시점 각도(Code 50) 및 종점 각도(Code 51)는 DXF 원본에 기록된 **도(Degree)** 단위 부동소수점을 그대로 보존합니다.
  - 내부 연산을 위한 라디안(Radian) 변환을 수행하지 않습니다 ($\pi$ 연산 누수 차단).
- **비양수 반지름 처리**:
  - 반지름(Code 40)이 0 이하인 경우(`arc-zero-radius.dxf`), `Math.Abs()`로 양수화하거나 미소값으로 보정하지 않고 `InvalidRadius` 진단과 함께 거절합니다.
- **반전 원호 / 시·종점 동일**:
  - 시점 각도가 종점 각도보다 큰 원호(0도 경계 교차), 시점 각도와 종점 각도가 같은 완전한 원/선형 원호도 인위적 분할 없이 원본 수치를 온전히 유지합니다.

### 3.4 POINT (점 엔티티)
- Code 10, 20, 30의 좌표 트리플을 보존하며, Z 좌표가 누락된 경우 `Presence.Absent`로 유지됩니다.
- 레이어 및 그래픽 속성 페어링이 정상적으로 추적됩니다.

### 3.5 SOLID (솔리드 엔티티)
- **4점/3점 처리**:
  - 4개의 코너(Code 10..13, 20..23, 30..33)를 독립 슬롯으로 추출합니다.
  - 삼각형(3점 솔리드)의 경우 DXF 관례상 4번째 점이 3번째 점과 동일한 좌표를 갖는데, 이때 4번째 점을 무시하거나 삭제하지 않고 4개의 좌표 슬롯 및 독립된 페어 인덱스를 모두 유지합니다 (`solid-triangular.dxf` 검증).

### 3.6 CIRCLE (원 엔티티)
- 수동 검토 및 승인을 거친 `circle-reviewed.dxf` 픽스처를 통해 Code 10/20/30(중심점) 및 Code 40(반지름)의 추출이 검증되었습니다.
- 반지름 $\le 0$ 검증 로직이 ARC와 동일하게 엄격히 적용됩니다.

### 3.7 Legacy POLYLINE / VERTEX 게이팅
- 3D 폴리라인 및 구형 2D 폴리라인(`POLYLINE` / `VERTEX` 시퀀스)은 V1 2D 평면 기하 추출 범위 밖이므로, `LegacyPolylineGated` 정보 진단을 발행하고 페이로드를 생성하지 않음으로써 안전하게 보호됩니다 (`polyline-legacy-gated.dxf` 검증).

---

## 4. Absence vs Default Pattern Verification

ADR-015 및 엄격한 DXF 계약에 따라, 기본값(Default)을 파일에 없었던 값으로 날조하는 행위를 철저히 방지하였습니다:

| 속성 | 기본값 주입 금지 (Absence 유지) | 검증 결과 |
|---|---|:---:|
| Z 좌표 (Code 30, 31, 32, 33) | 0.0을 주입하지 않고 `Presence.Absent` 유지 | **통과** |
| 두께 (Thickness, Code 39) | 0.0을 주입하지 않고 `Presence.Absent` 유지 | **통과** |
| 돌출 법선 벡터 (Extrusion Normal, Code 210, 220, 230) | (0, 0, 1) 기본 벡터를 날조하지 않고 `Presence.Absent` 유지 | **통과** |
| 폴리라인 불지 (Bulge, Code 42) | 0.0을 주입하지 않고 `Presence.Absent` 유지 | **통과** |
| 폴리라인 폭 (Width, Code 40, 41, 43) | 0.0을 주입하지 않고 `Presence.Absent` 유지 | **통과** |

---

## 5. Architectural Cleanliness & Zero Phase Leakage

- **PHASE 03D 누수 없음**: `BLOCKS` 섹션 탐색, `INSERT` 중첩 구조 해석, 블록 참조 좌표 전개 코드가 일체 포함되지 않았습니다.
- **PHASE 03E 누수 없음**: 핸들 그래프 구성, 소유권 트리, 포인터 해결(Deref) 로직이 없습니다.
- **PHASE 04 누수 없음**: CAD IR 정규화, 토폴로지 분석, 교차점 계산, 벽체 중심선 추론 로직이 없습니다.
- **BIM/Revit 종속성 격리**: Revit API 어셈블리 참조가 전무하며, `BIMBraid Agent Rules` 1번("CAD Parser를 Revit API와 직접 결합하지 않는다")을 완벽 준수합니다.

---

## 6. Review Findings

- **BLOCKER**: 0건
- **HIGH**: 0건
- **MEDIUM**: 0건
- **LOW**: 0건

모든 구현과 경계 조건이 사전에 정의된 V1 네이티브 DXF 스펙을 충족하며, 프로덕션 코드 및 테스트 상의 결함이 발견되지 않았습니다.

---

## 7. Final Verdict

```
PHASE_03C_PASS
```

Codex는 다음 단계인 **PHASE 03D (BLOCKS / INSERT / Nested Component Expansion)** 계획 수립 및 구현으로 진행할 수 있습니다.
