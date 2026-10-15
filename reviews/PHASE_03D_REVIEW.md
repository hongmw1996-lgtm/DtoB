# PHASE 03D Native Text DXF Block / INSERT / Annotation Raw Extraction Implementation Review

## Executive Summary

- **Review Target**: PHASE 03D — Native Text DXF Block / INSERT / Annotation Raw Extraction (`packages/DtoB.Cad.Raw/DxfRawBlockAnnotationContracts.cs`, `packages/DtoB.Cad.Dxf/DxfBlockAnnotationLimits.cs`, `packages/DtoB.Cad.Dxf/TextDxfBlockAnnotationReader.cs`, `packages/DtoB.Cad.Dxf/TextDxfBlockAnnotationReader.Sequences.cs`, `tests/DtoB.Tests/TextDxfBlockAnnotationReaderTests.cs`)
- **Review Date**: 2026-10-14
- **Review Mode**: Final Implementation Review
- **Reviewer**: Architecture / Adversarial Review Owner (Antigravity)
- **Status of Codex**: `PASS_WITH_KNOWN_LIMITATIONS`
- **Final Verdict**: **`PHASE_03D_PASS`**

DtoB의 네이티브 텍스트 DXF 파이프라인의 4번째 단계인 **PHASE 03D (BLOCK / INSERT / Annotation Raw Extraction)**에 대한 독립적이고 엄격한 아키텍처/적대적(Adversarial) 검토를 수행하였습니다.

PHASE 03D는 이전 단계(03A 렉시컬 $\rightarrow$ 03B 구조 $\rightarrow$ 03C 원시 기하)의 결과를 변경하지 않고 순수 읽기 전용 입력으로 소비하며, `DeferredTo03D`로 유예되었던 블록 정의(BLOCK/ENDBLK), 블록 참조(INSERT), 텍스트(TEXT), 다중 텍스트(MTEXT), 치수(DIMENSION) 및 부가 픽스처 게이트 속성(ATTRIB/ATTDEF/SEQEND)의 원본 사실(Raw Factual Evidence)을 무손실·무변환으로 추출하는 계층입니다.

독립적인 검증 결과:
1. **임의의 변환 및 전개 배제**: INSERT 블록 인스턴스 전개(Occurrence Expansion), 좌표 변환 행렬 계산, 월드 좌표(WCS) 계산, 블록 기준점 오프셋 보정, 단위/라디안 변환, 폰트 로딩/렌더링이 일체 존재하지 않음을 확인하였습니다.
2. **블록 정의 및 포함 관계(Containment) 무결성**: 12개 BLOCK 및 12개 ENDBLK가 정확히 12개의 논리적 블록 정의로 매핑되며, 총 44개 자식(기존 03C 기하 32개 + 신규 03D 레코드 12개)을 원본 기하 객체 복제(Clone) 없이 결과 로컬 인덱스로 완벽히 참조함을 검증하였습니다.
3. **DIMENSION Code 70 비트 마스킹 분리**: Code 70 값(32)을 전체 서브타입으로 오인하지 않고, 하위 3비트 마스킹(`flags & 7 == 0`)을 통해 기저 선형/회전 치수(Rotated Linear Type 0)와 블록 참조 플래그(Bit 32)를 정확히 분리 해석하며, Code 42 측정값 부재 시 임의의 거리를 계산하지 않고 원본 부재(`Absent`)를 엄격히 유지함을 확인하였습니다.
4. **MTEXT 및 속성 시퀀스 내결함성**: MTEXT의 분할 청크(Code 3)와 종단 청크(Code 1)를 물리적 순서대로 개별 보존하며, 잘못된 MTEXT/속성 시퀀스가 나타나더라도 이후의 독립 엔티티(TEXT 등)를 삼키지(swallow) 않고 안전하게 복원함을 확인하였습니다.
5. **빌드 및 회귀 무결성**: 솔루션 전체 Release 빌드 0 에러, **총 434/434 테스트 전원 통과 (신규 03D 78건)**, 통제 도면 해시 불변 및 결정론적 직렬화 해시(`DB9E048EFCBEAD2B38F2F70A52621506D09AECDF35EBB6A2430563BC15F2B747`) 일치를 확인하였습니다.

모든 아키텍처 및 적대적 기준을 완벽하게 만족하므로 최종 승인(**`PHASE_03D_PASS`**)을 부여합니다.

---

## 1. Independent Verification & Test Execution Evidence

- **Release 솔루션 빌드**: `MSBuild.exe DtoB.sln -restore -t:Rebuild -p:Configuration=Release -p:MSBuildEnableWorkloadResolver=false`
  - 결과: **0 Error(s)**, 0 Warning(s) (기존 승인된 Revit API 런타임 어셈블리 경고 MSB3277 외 신규 경고 0건)
- **전체 단위/통합/회귀 테스트**: **434개 테스트 중 434개 전원 통과 (Pass 434, Failed 0, Skipped 0)**
  - `TextDxfBlockAnnotationReaderTests`: **78개 신규 테스트 전원 통과**
  - `TextDxfGeometryReaderTests`: 63개 기하 리더 테스트 전원 통과
  - `TextDxfStructuralReaderTests`: 103개 구조 리더 테스트 전원 통과
  - `TextDxfLexicalReaderTests`: 55개 렉시컬 리더 테스트 전원 통과
  - 종속성 및 아키텍처 불변 테스트: 9개 전원 통과
  - Phase 00~02 회귀 테스트: 126개 전원 통과
- **코드 검사**: `git diff --check` 결과 CRLF/공백 결함 0건
- **통제 도면 픽스처 해시**: `datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf`
  - SHA-256: `2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC` (단 1바이트의 변형도 없이 일치)
- **결정론적 직렬화 결과 해시**:
  - `DxfBlockAnnotationReadResult` 직렬화 SHA-256: `DB9E048EFCBEAD2B38F2F70A52621506D09AECDF35EBB6A2430563BC15F2B747` (동일 파일 3회 독립 스트림 재실행 시 100% 일치)

---

## 2. Controlled Source Inventory Audit

통제 도면 원본 파일(`datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf`)의 Group Code 0 경계를 독립 파서로 감사한 실측 수치와 03D 추출 결과는 다음과 같습니다:

### 2.1 섹션별 03D 엔티티 실측 (총 93개)
- **BLOCKS 섹션 (36개)**:
  - `BLOCK`: 12개
  - `ENDBLK`: 12개
  - `INSERT`: 8개 (익명 블록 *D1, *D2, *D3, *D4 내부에 각 2개씩 `_ARCHTICK` 참조)
  - `MTEXT`: 4개 (*D1, *D2, *D3, *D4 내부에 각 1개씩 치수 텍스트)
- **ENTITIES 섹션 (57개)**:
  - `INSERT`: 17개 (`COLUMN_400` 8개, `D_SINGLE_900` 3개, `W_SINGLE_1200` 6개)
  - `TEXT`: 36개 (도면 주석, 실명, 그리드 버블 등)
  - `DIMENSION`: 4개 (*D1, *D2, *D3, *D4 블록을 참조하는 회전 선형 치수)
- **03D 대상 합계**: $36 + 57 = \mathbf{93\text{개}}$

### 2.2 전체 도면 자식 레코드 처분 오버레이 회계 (Effective Dispositions)
- 전체 자식 레코드 수: 333개
- 03D 오버레이 전: `Supported 188`, `DeferredTo03D 93`, `Unsupported 52`, `Invalid 0`
- **03D 오버레이 후 (Effective Disposition)**:
  - `Supported`: **281개** (기존 188개 + 신규 성공 매핑 93개)
  - `DeferredTo03D`: **0개** (전수 소비 완료)
  - `Unsupported`: **52개** (VPORT 1, APPID 3, OBJECTS 48 불변 유지)
  - `DeferredTo03C`: **0개**
  - `Invalid`: **0개**
- 합산 검증: $281 + 0 + 52 + 0 + 0 = \mathbf{333\text{개}}$
- 전체 상태는 52개의 미지원 레코드(OBJECTS 등)가 잔존하므로 `DxfBlockAnnotationStatus.Partial`이며, 03D 매핑 자체는 `MappingComplete = true`로 정확히 보고됩니다.

---

## 3. Detailed Component & Adversarial Analysis

### 3.1 BLOCK / ENDBLK 정의 및 포함 관계 (Containment)
- **정의 수 vs 경계 구분자**: 12개의 BLOCK과 12개의 ENDBLK가 쌍을 이루어 정확히 **12개의 `RawBlockDefinition`**을 형성하며, 24개의 정의로 오인되지 않습니다.
- **블록 이름 전수 검증**: `*Model_Space`, `*Paper_Space`, `_ARCHTICK`, `_CLOSEDFILLED`, `_CLOSEDBLANK`, `COLUMN_400`, `D_SINGLE_900`, `W_SINGLE_1200`, `*D1`, `*D2`, `*D3`, `*D4` (총 12개, 대소문자 및 아스타리스크 완전 보존).
- **자식 엔티티 회계 (Children Accounting)**:
  - 총 자식 수: **44개** (최대 자식 수 9개: *D1, *D2, *D3, *D4 각 9개).
  - 무복제 참조 원칙: 44개 자식 중 32개 기하 엔티티(LINE 14, LWPOLYLINE 4, ARC 1, POINT 12, SOLID 1)는 새로운 기하 페이로드를 생성하지 않고 `GeometryRecordIndex`를 통해 기존 03C 레코드를 참조합니다. 12개 주석/블록 엔티티(INSERT 8, MTEXT 4)는 `AnnotationRecordIndex`를 통해 참조됩니다.
- **경계 이상 내결함성**:
  - 중첩 BLOCK 출현 시 `NestedBlock` 에러 발행 및 이전 정의 미완료(End=null) 처리 (`block-malformed.dxf`).
  - 활성 BLOCK 없는 ENDBLK 출현 시 `UnexpectedBlockEnd` 에러 발행.
  - 섹션 종료 시 미완료 BLOCK은 `MissingBlockEnd` 에러 발행 및 종료점을 날조하지 않고 null 유지.

### 3.2 INSERT (블록 참조) 및 무변환(No Transform) 원칙
- **변환 연산 부재(No Transform Verification)**:
  - 소스 코드 전반에서 $T(\text{insertion}) \times R(\text{rotation}) \times S(\text{scale}) \times T(-\text{base})$ 행렬 계산, 삼각함수(`Sin`/`Cos`), WCS 좌표 변환, 정점 인스턴스 복제 로직이 전무함을 확인하였습니다.
  - 중첩 INSERT(예: `blocks-nested.dxf`의 NONZERO 블록 내 INSERT) 역시 기준점 오프셋 보정이나 좌표 변환 없이, 원본 부모 블록에 대한 소속과 순수 원시 배치 팩트(삽입점, 스케일, 회전각)만을 보존합니다.
- **부재(Absence) vs 기본값(Default)**:
  - 누락된 스케일(41/42/43)은 1.0으로 채우지 않고 `Presence.Absent` 유지.
  - 누락된 회전각(50)은 0.0으로 채우지 않고 `Presence.Absent` 유지 (원본 각도는 도 단위 유지).
  - 누락된 삽입점 Z(30), 배열 행/열 수(70/71), 간격(44/45), 돌출 법선(210/220/230) 모두 `Absent`로 유지.
- **MINSERT / 배열 처리**:
  - 행 또는 열 수가 2 이상이거나 `AcDbMInsertBlock` 서브클래스 마커가 있는 경우, 가상 인스턴스를 생성하지 않고 `UnsupportedOccurrenceExpansion` 경고 진단과 함께 `DxfDisposition.Unsupported`로 보존합니다 (`insert-array.dxf`).
  - 이후에 등장하는 독립 엔티티(TEXT 등)는 정상적으로 파싱됩니다.

### 3.3 TEXT (단일행 텍스트)
- **원본 문자열 무손실**: UTF-8 한글 문자열, DXF 제어 코드(`%%d`), 유니코드 이스케이프(`\U+0041`)를 디코딩/치환 없이 원문 그대로 보존합니다.
- **정렬점(Alignment Point, 11/21/31)**: 수평(72) 또는 수직(73) 정렬 모드가 지정되었더라도, 파일에 Code 11/21/31이 없으면 정렬점을 임의로 날조하지 않고 `Absent` 상태로 유지합니다 (`text-alignment.dxf` 검증).
- **폰트/경계 배제**: SHX/TTF 폰트 파일 조회, 텍스트 바운딩 박스 계산, 의미론적 레이블 추론 로직이 없습니다.

### 3.4 MTEXT (다중행 텍스트)
- **청크 보존(Chunks Preservation)**:
  - 250바이트 초과 시 분할되는 Code 3 연속 청크와 최종 Code 1 청크를 물리적 순서대로 `RawTextChunk` 목록에 개별 보존합니다.
  - 원본 청크들을 문자열 병합(Concatenation)하여 원본 증거를 덮어쓰지 않습니다.
- **청크 시퀀스 유효성**:
  - Code 1 이후에 Code 3이 등장하거나, Code 1이 중복 출현할 경우 `InvalidMTextChunks` 에러를 발생시키고 레코드를 무효화하되, 수집된 청크 데이터는 온전히 보존합니다.
  - 서식 태그(`{\fArial;...}`)를 해석하거나 제거하지 않고 원문 그대로 유지합니다.

### 3.5 DIMENSION (치수) — 임계 시맨틱 검증
- **Code 70 비트 마스킹 분리 (Type & Flags Separation)**:
  - 통제 도면의 4개 치수 레코드의 Code 70 값은 모두 **32**입니다.
  - 리더는 `flags.Integer.Value & 7` 마스킹을 수행하여 기저 치수 타입이 **0 (Rotated / Horizontal / Vertical Linear)**임을 정확히 식별하고, 비트 32(블록 참조 플래그)와 분리합니다. 32 전체를 미지의 서브타입으로 오분류하지 않습니다.
- **Code 42 (측정값) 계산 금지**:
  - 통제 도면의 4개 치수는 Code 42가 부재합니다. 정의점(10, 13, 14) 간의 기하 거리를 계산하여 Code 42에 주입하지 않고 `Presence.Absent`로 엄격히 유지합니다.
  - Code 42가 존재하는 경우(`dimension-aligned.dxf`), 원본 소스가 제공한 측정값 팩트로만 기록하며 정규화된 기하 진실로 승격하지 않습니다.
- **미지원 서브타입 격리**:
  - 각도 치수(Type 2) 등 미지원 서브타입(`dimension-unsupported.dxf`)은 공통 필드만 보존하고 서브타입 확장점(13, 14, 50)을 매핑하지 않으며(`null`), `UnsupportedDimensionSubtype` 진단과 함께 `DxfDisposition.Unsupported`로 유지됩니다.

### 3.6 ATTRIB / ATTDEF 및 속성 시퀀스 (Attribute Sequences)
- **별도 픽스처 게이팅**: 통제 도면에는 속성이 없으므로, 수동 작성된 정규 픽스처(`attributes.dxf`, `attributes-malformed.dxf`)를 통해 전체 파이프라인(03A $\rightarrow$ 03D)을 통과함을 검증하였습니다.
- **값 분리**: ATTRIB의 명시적 값(`explicit value`)과 ATTDEF의 기본값(`default only`)이 상호 복제되지 않고 엄격히 분리됩니다.
- **시퀀스 상태 머신 및 SEQEND 분기**:
  - INSERT의 `AttributesFollow`(Code 66)가 명시적으로 1인 경우에만 속성 시퀀스를 엽니다.
  - 시퀀스 도중 불량 엔티티가 출현하거나 SEQEND 없이 종료되면 `MissingAttributeSequenceEnd` 에러를 발행하고 시퀀스를 닫되, 후속 독립 엔티티(TEXT 등)는 정상 처리됩니다.
  - `legacy-seqend.dxf`: 구형 POLYLINE에 딸린 SEQEND는 `LegacySequenceGated` 경고를 발행하고, 미지정 SEQEND는 `UnassignedSequenceEnd` 경고와 함께 `Unsupported`로 처리되며, INSERT 속성 종단(`InsertAttributes`)으로 오인되지 않습니다.

### 3.7 Context Isolation & Pair Accounting
- **컨텍스트 격리**: 102 제어 그룹, 1001/1002 XDATA, 외래 서브클래스(`foreign-contexts.dxf`) 내부의 필드는 일반 속성으로 오염되지 않고 `UnmappedPairIndices`로 격리됩니다.
- **페어 회계**: 통제 도면 및 모든 픽스처의 93개 레코드 전수에서 `MappedIndices + UnmappedIndices`가 레코드 전체 페어 범위와 비트 단위로 100% 일치함을 확인하였습니다.

### 3.8 Resource Limits & Cancellation
- **자원 가드 (11개 차원)**: 블록 정의(32), 블록당 자식(64), INSERT(256), 주석(256), MTEXT 청크(32), 원시 주석 문자 수(16,384), 속성 시퀀스 길이(64), 매핑 레코드(512), 진단(5,000), 레코드당 필드(512), 구조 레코드(4,096), 소스 페어(250,000).
- 모든 가드 차원에 대해 정확한 한계 통과 및 첫 초과 시 `ResourceLimitExceeded` 발생이 검증되었습니다.
- 마지막 1개의 진단 슬롯은 터미널 진단용으로 예약되어 침묵하는 성공(Truncated Success)을 방지합니다.
- 취소 토큰 감지 시 즉시 중단되며 성공(`MappingComplete=true`)을 발행하지 않습니다.

---

## 4. Architectural Cleanliness & Zero Phase Leakage

- **PHASE 03E 누수 없음**: 전역 핸들 정규화, 핸들 충돌 검사, 소유권 그래프, 포인터 해결(Deref) 로직이 없습니다.
- **PHASE 03F 누수 없음**: `RawDrawingSnapshot` 조립 로직이 없습니다.
- **PHASE 04 누수 없음**: 단위 변환(mm 변환), 라디안 변환, OCS/WCS 변환, Arbitrary Axis Algorithm, 블록 기준점 오프셋 보정, INSERT 인스턴스 전개, ByLayer/ByBlock 해석, CAD IR 생성이 없습니다.
- **Revit/BIM 결합 없음**: Revit API 어셈블리 참조 0건, `BIMBraid Agent Rules` 1번 준수.

---

## 5. Review Findings

- **BLOCKER**: 0건
- **HIGH**: 0건
- **MEDIUM**: 0건
- **LOW**: 0건

모든 요구사항과 경계 조건이 사전에 정의된 V1 네이티브 DXF 스펙을 엄격히 충족하며 결함이 발견되지 않았습니다.

---

## 6. Final Verdict

```
PHASE_03D_PASS
```

Codex는 다음 단계인 **PHASE 03E (Cross-Section Handle & Ownership Graph Validation)** 계획 수립 및 구현으로 진행할 수 있습니다.
