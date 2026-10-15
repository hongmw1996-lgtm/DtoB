# PHASE 03E Native Text DXF Handle / Ownership / Source Provenance Validation Implementation Review

## Executive Summary

- **Review Target**: PHASE 03E — Native Text DXF Handle / Ownership / Source Provenance Validation (`packages/DtoB.Cad.Raw/DxfIdentityGraphContracts.cs`, `packages/DtoB.Cad.Dxf/DxfIdentityGraphLimits.cs`, `packages/DtoB.Cad.Dxf/TextDxfIdentityGraphReader.cs`, `packages/DtoB.Cad.Dxf/TextDxfIdentityGraphReader.Owners.cs`, `tests/DtoB.Tests/TextDxfIdentityGraphReaderTests.cs`)
- **Review Date**: 2026-10-14
- **Review Mode**: Final Implementation Review
- **Reviewer**: Architecture / Adversarial Review Owner (Antigravity)
- **Status of Codex**: `PASS_WITH_KNOWN_LIMITATIONS`
- **Final Verdict**: **`PHASE_03E_PASS`**

DtoB의 네이티브 텍스트 DXF 파이프라인의 5번째 세부 단계인 **PHASE 03E (Handle / Ownership / Source Provenance Validation)** 구현 및 테스트 전반에 대한 독립적이고 엄격한 아키텍처/적대적(Adversarial) 검토를 수행하였습니다.

PHASE 03E는 이전 단계(03A 렉시컬 $\rightarrow$ 03B 구조 $\rightarrow$ 03C 기하 $\rightarrow$ 03D 블록/주석)의 증거를 변경 없이 순수 읽기 전용으로 수신하여, 객체 핸들(Handle) 식별자의 유효성 검증, 정규화(Canonicalization), 동일 도면 내 중복 검출, 문맥 인식 기반 참조(Reference) 해결, 소유권(Ownership) DAG 구성 및 순환(Cycle) 검증, 원본 출처(Provenance) 결합을 수행하는 무상태 검증 계층입니다.

독립 검증 결과:
1. **정규화 및 원본 보존 규칙의 철저한 분리**: `OriginalHandle`(원본 표기, 물리적 행/스팬 보존)과 `CanonicalHandle`(ASCII 16진수 대문자 변환, 선두 0 제거)을 명확히 분리하고, 정수 오버플로 취약점이 없는 가변 길이 문자열 알고리즘을 적용하였습니다. 특히 참조 대상 `"0"`은 `Null` 해소 상태로, 객체 식별자 `"0"`은 `Malformed` 식별자로 엄격히 구분하여 처리함을 확인하였습니다.
2. **중복 식별자 승자독식 배제(No Winner-Take-All)**: 대소문자/선두 0 동치 중복을 포함하여 충돌 발생 시 모든 충돌 노드를 보존하고 `Ambiguous`로 표기하며, 임의의 핸들 재할당/병합 없이 `GraphValidated = false`로 엄격히 실패 처리함을 검증하였습니다.
3. **식별자 노드 적격성(Eligibility) 실측 입증**: 333개 자식 레코드와 9개 TABLE 헤더(총 342개 위치) 중 15개 CLASS 선언은 비식별자(`NonIdentityRecords`)로 격리되고, 나머지 **327개 노드가 유효한 식별자 노드**로 정밀하게 집계됨을 소스 코드와 원본 DXF 레코드 전수 스캔으로 확인하였습니다.
4. **포인터 종류 vs 문맥 역할(Role)의 완전한 분리**: Group Code 330이더라도 제어 그룹 `{ACAD_REACTORS` 내부는 `Reactor`, `{ACAD_XDICTIONARY` 내부는 `ExtensionDictionary`, XDATA 1005는 `XData`로 분기되며, 임의의 '첫 번째 330 승자' 로직 없이 오직 순수한 `Owner` 역할만 소유권 에지(317개) 및 루트(10개) 판정에 참여함을 확인하였습니다.
5. **소유권 순환(Cycle) 비재귀적 검증**: 재귀 호출 없는 스택 기반 반복적 DFS를 통해 자체 소유(`SelfOwner`) 및 상호 소유 순환을 탐색하며, 에지 삭제나 수정 없이 정확한 순환 증인(Witness)을 보존함을 확인하였습니다.
6. **빌드 및 회귀 무결성**: 솔루션 전체 Release 빌드 0 에러, **총 522/522 테스트 전원 통과 (신규 03E 88건)**, 통제 도면 원본 해시 불변 및 결정론적 직렬화 해시(`A27ACDE11709AEB611B381377F9BD57295364BA410B2774E7E6FE685003C34A7`) 일치를 확인하였습니다.

모든 요구사항과 경계 격리 규칙을 완벽하게 만족하므로 최종 승인(**`PHASE_03E_PASS`**)을 부여합니다.

---

## 1. Independent Verification & Test Execution Evidence

- **Release 솔루션 빌드**: `MSBuild.exe DtoB.sln -restore -t:Rebuild -p:Configuration=Release -p:MSBuildEnableWorkloadResolver=false`
  - 결과: **0 Error(s)**, 0 Warning(s) (기존 승인된 Revit API 런타임 종속 어셈블리 경고 MSB3277 외 신규 경고 0건)
- **전체 단위/통합/회귀 테스트**: **522개 테스트 중 522개 전원 통과 (Pass 522, Failed 0, Skipped 0)**
  - `TextDxfIdentityGraphReaderTests`: **88개 신규 03E 테스트 전원 통과**
  - `TextDxfBlockAnnotationReaderTests`: 78개 블록/주석 테스트 전원 통과
  - `TextDxfGeometryReaderTests`: 63개 기하 리더 테스트 전원 통과
  - `TextDxfStructuralReaderTests`: 103개 구조 리더 테스트 전원 통과
  - `TextDxfLexicalReaderTests`: 55개 렉시컬 리더 테스트 전원 통과
  - 아키텍처 및 종속성 불변 테스트: 9개 전원 통과
  - Phase 00~02 회귀 테스트: 126개 전원 통과
- **코드 검사**: `git diff --check` 결과 CRLF/공백 결함 0건
- **통제 도면 픽스처 해시**: `datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf`
  - SHA-256: `2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC` (단 1바이트의 변형도 없이 일치)
- **결정론적 직렬화 결과 해시**:
  - `DxfIdentityGraphReadResult` 직렬화 SHA-256: `A27ACDE11709AEB611B381377F9BD57295364BA410B2774E7E6FE685003C34A7` (독립 스트림 3회 반복 실행 시 100% 비트 일치)

---

## 2. Independent Source Inventory Audit

통제 도면 원본 파일(`datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf`)의 물리적 행 및 Group Code 경계를 독립 BCL 스캐너로 전수 검증한 결과는 다음과 같습니다:

### 2.1 식별자 노드 적격성 실측 (총 327개 노드)
- 검토 대상 소스 위치 총계: **342개**
  - 자식 레코드: 333개
  - 심볼 테이블 헤더 레코드: 9개 (`VPORT`, `LTYPE`, `LAYER`, `STYLE`, `VIEW`, `UCS`, `APPID`, `DIMSTYLE`, `BLOCK_RECORD`)
- **비식별자 레코드**: **15개** (`CLASSES` 섹션 내 15개 `CLASS` 선언은 Code 5/105가 없으며 소유자 참조도 없어 `NonIdentityRecords`로 분리)
- **식별자 노드 총계**: $342 - 15 = \mathbf{327\text{개}}$
  - `TABLES` 섹션: 97개 (엔트리 88개 + 헤더 9개)
  - `BLOCKS` 섹션: 68개 (BLOCK 12, ENDBLK 12, 기하 32, INSERT 8, MTEXT 4)
  - `ENTITIES` 섹션: 114개 (기하 57, INSERT 17, TEXT 36, DIMENSION 4)
  - `OBJECTS` 섹션: 48개 (DICTIONARY 12, VISUALSTYLE 25 등)
- 전수 검증 결과: 327개 노드 모두 유효(`Valid 327`), 결측(`MissingRequired 0`), 기형(`Malformed 0`), 중복(`DuplicateGroups 0`).

### 2.2 문맥 참조(Reference) 및 해소(Resolution) 실측 (총 458개)
- **역할별 발생 빈도**:
  - `Owner`: 327개
  - `Reactor`: 30개
  - `ExtensionDictionary`: 0개
  - `XData`: 0개
  - `Other`: 101개
  - **참조 총합**: $327 + 30 + 101 = \mathbf{458\text{개}}$
- **해소 상태**:
  - `Resolved`: 438개 (동일 도면 내 고유 노드 연결)
  - `Null`: 20개 (대상 값이 `"0"` 또는 `"0000"`인 명시적 널 포인터: Owner 10개 + Other 10개)
  - `Unresolved`: 0개
  - `Ambiguous`: 0개
  - `Malformed`: 0개
  - **해소 총합**: $438 + 20 = \mathbf{458\text{개}}$

### 2.3 소유권(Ownership) 에지 및 루트 실측
- **소유권 에지 (Resolved Owner Edges)**: **317개** (327개 Owner 참조 중 Null 대상 10개를 제외한 317개가 유효한 자식 $\rightarrow$ 부모 소유권 에지 형성)
- **소유권 루트 (Owner Roots)**: **10개** (Owner 대상이 `"0"`인 유효 루트: 9개 TABLE 헤더 + 1개 최상위 DICTIONARY)
- **소유권 순환 (Owner Cycles)**: **0개**, 자체 소유(**Self-Owner**): **0개**

### 2.4 시맨틱 불변성 (Semantic Invariance) 검증
- 03E 검증 전후 시맨틱 레코드 처분(Disposition) 분류는 단 1건도 변경되지 않았습니다:
  - `Supported`: 281개
  - `Unsupported`: 52개 (OBJECTS 48, VPORT 1, APPID 3)
  - `DeferredTo03C`: 0개, `DeferredTo03D`: 0개, `Invalid`: 0개 (총 333개 불변)
- 48개의 OBJECTS 및 미지원 테이블 항목들은 시맨틱하게 `Unsupported`로 유지되면서도 유효한 핸들을 소유하고 참조 그래프에 정상적으로 참여합니다.

---

## 3. Detailed Component & Adversarial Analysis

### 3.1 원본 표기(Original) vs 정규화(Canonical) 핸들 규칙
- **정규화 알고리즘 (`Canonical`)**:
  - ASCII 16진수(`0-9`, `a-f`, `A-F`) 외의 문자가 1글자라도 포함되면 즉시 `null`을 반환합니다.
  - 선두의 `'0'`을 제거하고 대문자로 변환합니다 (`"000a"` $\rightarrow$ `"A"`).
  - 공백 제거(trim), 부호(`+`/`-`), `0x` 접두사 제거를 수행하지 않고 즉시 기형(`Malformed`)으로 거절합니다.
  - 가변 길이 문자열 슬라이싱으로 구현되어 64비트(`UInt64`) 제한을 초과하는 초대형 16진수 문자열도 오버플로 없이 안전하게 처리합니다.
- **참조 대상 `"0"` vs 객체 식별자 `"0"`의 엄격한 분리**:
  - 포인터 참조에서 대상 값이 `"0"` 또는 `"0000"`인 경우: 정규화 키 `"0"`을 거쳐 `DxfReferenceResolution.Null`로 정확히 분류됩니다. 허위 미해소(`Unresolved`) 에러를 내지 않습니다 (`null-reference.dxf` 검증).
  - 객체 식별자(Code 5/105)에서 값이 `"0"` 또는 `"0000"`인 경우: DXF 규격상 객체 핸들 0은 허용되지 않으므로, 정규화 키를 `null`로 비우고 `DxfIdentityValidity.Malformed` 에러를 발생시킵니다 (`malformed.dxf` 검증).

### 3.2 중복 검출(Duplicate Detection) 및 승자 배제
- 대소문자 중복(`duplicate-case.dxf`), 선두 0 중복(`duplicate-zero.dxf`), 동일 섹션 중복(`duplicate-same.dxf`), 섹션 교차 중복(`duplicate-cross.dxf`) 전수 테스트:
  - 중복된 모든 노드는 `Ambiguous`로 전이되며, 각 노드마다 위치 지정 진단(`DuplicateHandle`)이 발행됩니다.
  - 먼저 등장한 노드나 나중에 등장한 노드를 임의로 선택(First/Last-wins)하거나 핸들을 변경하지 않습니다.
  - `GraphValidated = false`로 설정되며, 중복된 노드를 대상으로 가리키는 참조는 `Ambiguous` 해소 상태로 전이되어 잘못된 소유권 연결을 원천 차단합니다.

### 3.3 식별자 필수성 및 결측 프로필 (Missing-Handle Profiles)
- 도면 내의 모든 레코드에 맹목적으로 핸들을 요구하지 않습니다:
  - `AcDbEntity`, `AcDbSymbolTable`, `AcDbSymbolTableRecord` 서브클래스 마커가 있거나, 명시적 Owner 참조가 있거나, 검토된 OBJECTS 타입(DICTIONARY, LAYOUT, MATERIAL, VISUALSTYLE)인 경우에만 핸들을 필수로 요구합니다.
  - 핸들이 필수인 객체에서 핸들이 누락된 경우 `MissingRequired` 에러 진단을 발생시킵니다 (`missing-required.dxf`).
  - 식별자 요구 프로필이 없는 레코드(예: 15개 `CLASS` 선언)는 `NonIdentityRecords`로 조용히 격리되며 허위 에러를 발행하지 않습니다.

### 3.4 참조 역할(Role) vs 포인터 종류(PointerKind)의 격리
- **숫자 그룹 코드와 문맥의 분리**:
  - Code 330이더라도 `{ACAD_REACTORS` 제어 그룹 내부에 있으면 `Reactor`로 분류됩니다.
  - Code 360(Hard Owner)이더라도 `{ACAD_XDICTIONARY` 내부에 있으면 `ExtensionDictionary`로 분류됩니다.
  - Code 1005는 `XData`로 분류됩니다.
- **첫 번째 330 승자독식 배제**:
  - 통제 도면의 `VISUALSTYLE` 레코드(시작 페어 5454)의 경우, 동일한 대상 핸들 `"16"`을 가리키는 참조가 페어 5457(`Reactor`)과 페어 5459(`Owner`)로 동시에 존재합니다. 두 참조는 상호 덮어쓰기 없이 각각 독립된 역할과 에지로 보존됩니다.

### 3.5 소유권 DAG 및 순환(Cycle) 검증
- **루트(Root) 판정의 엄격성**:
  - 단순 "소유권 에지가 없음"을 루트로 간주하지 않습니다. 미해소/모호한 소유자 또는 경쟁 소유자를 가진 노드는 결코 루트가 될 수 없습니다.
  - 오직 식별자가 유효하고 소유자 필수 실패가 없으며, 소유자 참조가 전혀 없는 허용 프로필이거나 명시적인 `Null` 소유자 1개만을 가진 경우에만 `OwnerRoots`로 승인됩니다.
- **순환 탐색 알고리즘**:
  - `TextDxfIdentityGraphReader.Owners.cs`는 3색(0=미방문, 1=방문중, 2=완료) 스택 기반 반복적 DFS를 수행하여 자체 소유(`self-owner.dxf`) 및 2-노드 순환(`owner-cycle.dxf`)을 완벽히 포착합니다.
  - 소유권 에지가 아닌 일반 포인터 간의 순환(`pointer-cycle.dxf`)은 소유권 순환으로 오분류되지 않습니다.
  - 탐색 작업마다 `traversalWork`를 카운팅하여 무한 루프 및 DoS를 방어합니다.

### 3.6 복수 소유자(Multiple Owners) 충돌 방어
- 한 노드가 2개 이상의 Code 330 소유자 참조를 가지는 경우(`multiple-owners.dxf`), 임의로 부모를 선택하지 않고 `MultipleOwners` 에러를 발행하며, 감사 추적을 위해 모든 해소된 에지를 `owners` 컬렉션에 보존하되 단일 부모 관계는 거절합니다.

### 3.7 참조 실패 심각도(Severity) 분리
- **소유권 참조 실패**: `UnresolvedOwner`는 치명적 `Error`로 취급되어 `GraphValidated = false`로 즉시 실패합니다.
- **비소유권 참조 실패**: `UnresolvedReactor`, `UnresolvedPointer` 등은 `Warning`으로 처리되며, 소유권 DAG가 온전하다면 `GraphValidated = true`를 유지할 수 있으나 `ReferenceResolutionComplete = false` 및 `Status = Partial`로 명시 분리됩니다 (`unresolved-pointer.dxf` 검증).

### 3.8 출처(Provenance) 및 문서 컨텍스트 경계
- **외부 종속성 격리**: `DxfDocumentProvenance`는 외부 파일 I/O, `.dtob` 프로젝트 파일 열기, 세션 관리자 결합 없이 순수하게 호출자가 제공한 `DrawingId`, `RevisionId`, 64자리 16진수 `FileHash`를 구문 검증하고 래핑합니다.
- **전체 식별자 튜플 (`DxfFullSourceIdentity`)**:
  - `(Document, OriginalHandle, Validity)`로 구성되며, 정규화된 키가 아닌 **DXF 원본 핸들 표기(`OriginalHandle`)**를 보존합니다.
  - 중복 또는 기형 핸들은 유효한 풀 식별자로 승격되지 않고 `Ambiguous`/`Malformed` 상태를 유지합니다.

### 3.9 수작업 검토 통제 도면 골든(Goldens) 전수 확인
- `TABLE/VPORT`: 시작 페어 682, 핸들 `"8"`, 페어 684, Code 5, 1369행
- `DIMSTYLE/Standard`: 시작 페어 1700, 핸들 `"2B"`, 페어 1701, Code 105, 3403행 (Code 5 비식별자 격리 확인)
- `BLOCK/*Model_Space`: 시작 페어 2662, 핸들 `"18"`, 페어 2663, Code 5, 5327행
- `LINE in BLOCKS`: 시작 페어 2856, 핸들 `"9C"`, 페어 2857, Code 5, 5715행
- `OBJECTS DICTIONARY`: 시작 페어 4905, 핸들 `"A"`, 페어 4906, Code 5, 9813행
- `OBJECTS VISUALSTYLE`: 시작 페어 5454, 핸들 `"5A"`, 페어 5455, Code 5, 10911행
- HEADER `$HANDSEED` (페어 277) 식별자 배제 확인

---

## 4. Architectural Cleanliness & Zero Phase Leakage

- **PHASE 03F 누수 없음**: `RawDrawingSnapshot` 조립 및 전수 통합 루트 모델 생성이 없습니다.
- **Viewer 누수 없음**: WPF 바인딩, 렌더링 루프, 뷰어 인터페이스가 일체 없습니다.
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
PHASE_03E_PASS
```

Codex는 다음 단계인 **PHASE 03F (RawDrawingSnapshot & Minimal Diagnostic Viewer Support)** 계획 수립 및 구현으로 진행할 수 있습니다.
