# PHASE 03F Raw Drawing Snapshot Assembly Implementation Review

## Executive Summary

- **Review Target**: PHASE 03F — Raw Drawing Snapshot Assembly (`packages/DtoB.Cad.Raw/RawDrawingSnapshot.cs`, `packages/DtoB.Cad.Dxf/RawDrawingSnapshotAssemblyLimits.cs`, `packages/DtoB.Cad.Dxf/TextDxfRawDrawingSnapshotAssembler.cs`, `tests/DtoB.Tests/RawDrawingSnapshotAssemblerTests.cs`)
- **Review Date**: 2026-10-15
- **Review Mode**: Final Implementation Review
- **Reviewer**: Architecture / Adversarial Review Owner (Antigravity)
- **Status of Codex**: `PASS_WITH_KNOWN_LIMITATIONS`
- **Final Verdict**: **`PHASE_03F_PASS`**

DtoB의 네이티브 텍스트 DXF 파이프라인의 6번째 세부 단계인 **PHASE 03F (Raw Drawing Snapshot Assembly)** 구현 및 테스트 전반에 대한 독립적이고 엄격한 아키텍처/적대적(Adversarial) 검토를 수행하였습니다.

PHASE 03F는 이전 5개 단계(03A 렉시컬 $\rightarrow$ 03B 구조 $\rightarrow$ 03C 기하 $\rightarrow$ 03D 블록/주석 $\rightarrow$ 03E 핸들/식별자 그래프)의 누적 증거를 단 1바이트의 재파싱, 복제, 단위 정규화, 좌표 변환 없이 순수 읽기 전용으로 수신하여 불변의 인메모리 스냅샷인 `RawDrawingSnapshot`을 조립하는 결합 계층입니다.

독립 검증 결과:
1. **린(Thin) 불변 조합 아키텍처**: 스냅샷은 03E의 `DxfIdentityGraphReadResult`를 단일 진실의 근원(Source of Truth Root)으로 보유하며 선행 단계들의 기하, 블록, 주석, 식별자 그래프를 재복제(Deep Clone)하지 않고 직접 참조합니다. QA 직렬화 시에도 포워딩 속성들을 `[JsonIgnore]` 처리하여 불필요한 직렬화 팽창을 방지하였습니다.
2. **333개 자식 레코드 뷰의 1:1 보존 및 심볼 테이블 헤더 격리**: 통제 도면의 333개 자식 레코드는 단 하나의 누락이나 중복, 타입별 재정렬 없이 원본 물리적 순서대로 정확히 333개의 `RawDrawingRecordView`로 매핑되었습니다. 03E에서 식별된 9개의 심볼 테이블 헤더 노드는 별도의 `TableHeader` 로케이터 도메인으로 엄격히 격리되어 자식 레코드 뷰로 위조되지 않음을 확인하였습니다.
3. **독립된 11개 카운트 도메인의 분리 검증**: 바이트(96,621), 라인(17,474), 페어(8,737), 섹션(6), 변수(251), 테이블(9), 자식 레코드(333), Supported(281), Unsupported(52), 기하(89), 03D 레코드(93), 블록 정의(12), 식별자 노드(327), 비식별자 CLASS(15), 참조 에지(458), 소유권 에지(317), 루트(10) 등 선행 단계에서 확립된 모든 카운트 도메인이 오염이나 혼동 없이 일치함을 입증하였습니다.
4. **완전성(Completeness) 10축 모델의 독립성**: `SnapshotAssemblyComplete`와 `TypedMappingComplete`가 명확히 분리되어 있으며, 52개의 `Unsupported` 레코드와 미매핑 HEADER 메타데이터로 인해 통제 도면의 `TypedMappingComplete`는 정확히 `false`를 유지하고 최종 커버리지는 정직하게 `Partial`로 수렴함을 확인하였습니다.
5. **엄격한 그래프 게이트에 대한 아키텍처 평가**: V1 조립기에서 `GraphValidated`, `IdentityCoverageComplete`, `ReferenceResolutionComplete` 3가지를 모두 요구하는 엄격한 정책에 대해 적대적 분석을 수행하였으며, 통제 프로파일에서는 100% 해소되므로 완벽히 통과하나, 장기적으로는 비소유자 댕글링 포인터가 존재하는 실도면의 부분 스냅샷 생성을 제한할 수 있다는 점을 구조적 관찰 항목(LOW-01)으로 기록하였습니다.
6. **빌드, 테스트, 결정론 무결성**: 솔루션 Release 빌드 0 에러, **총 558/558 테스트 전원 통과 (신규 03F 36건)**, 통제 도면 원본 해시 불변, 그리고 독립 3회 반복 스트림 파이프라인에서 결정론적 직렬화 SHA-256 해시(`4B91C83791F89DEC9BDC633C832001F2D1E537E7F0330BEE0666067CAE9A310C`)가 100% 비트 단위로 일치함을 검증하였습니다.

따라서 최종 승인 판정(**`PHASE_03F_PASS`**)을 부여합니다.

---

## 1. Independent Verification & Test Execution Evidence

- **Release 솔루션 빌드**: `MSBuild.exe DtoB.sln -restore -t:Rebuild -p:Configuration=Release -p:RevitInstallDir="D:\프로그램\Revit 2025" -p:MSBuildEnableWorkloadResolver=false`
  - 결과: **0 Error(s)**, 3 Warning(s) (기존 승인된 Revit API 런타임 종속 어셈블리 경고 MSB3277 외 신규 경고 0건)
- **전체 단위/통합/회귀 테스트**: **558개 테스트 중 558개 전원 통과 (Pass 558, Failed 0, Skipped 0)**
  - `RawDrawingSnapshotAssemblerTests`: **36개 신규 03F 테스트 전원 통과**
  - `TextDxfIdentityGraphReaderTests`: 88개 03E 테스트 전원 통과
  - `TextDxfBlockAnnotationReaderTests`: 78개 03D 테스트 전원 통과
  - `TextDxfGeometryReaderTests`: 63개 03C 테스트 전원 통과
  - `TextDxfStructuralReaderTests`: 103개 03B 테스트 전원 통과
  - `TextDxfLexicalReaderTests`: 55개 03A 테스트 전원 통과
  - 아키텍처 및 종속성 불변 테스트: 9개 전원 통과
  - Phase 00~02 회귀 테스트: 126개 전원 통과
- **코드 검사**: `git diff --check` 결과 공백/CRLF 결함 0건
- **통제 도면 픽스처 원본 해시**: `datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf`
  - SHA-256: `2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC` (불변)
- **결정론적 스냅샷 직렬화 해시**:
  - `RawDrawingSnapshot` 직렬화 SHA-256: `4B91C83791F89DEC9BDC633C832001F2D1E537E7F0330BEE0666067CAE9A310C` (독립 3회 반복 파이프라인에서 100% 동일)

---

## 2. Independent Architecture & Scope Boundary Audit

### 2.0 Evidence Consistency Correction Note (근거 데이터 정합성 보정 기록)
- **보정 사유**: 최초 작성된 검토서의 2.3절 Unsupported 레코드 원본 타입 기술에서 텍스트 오기(`HATCH` 50개, `DIMENSION` 2개)가 포함되어 있었습니다.
- **독립 소스 전수 재감사(Source Audit)**: 통제 도면 원본(`datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf`)의 Group Code 0 엔티티 경계를 독립 BCL 스캐너로 전수 재검증하였습니다.
- **확인 결과**:
  1. 통제 도면 내 `HATCH` 레코드는 **0개(존재하지 않음)**입니다.
  2. 통제 도면 내 `DIMENSION` 레코드는 **정확히 4개**이며, 전량 `ENTITIES` 섹션에 속하며 선행 PHASE 03D 단계에서 지원 완료(`Supported`) 처리되어 03F 뷰에서도 `Supported` 처분을 유지합니다.
  3. 실측된 52개의 `Unsupported` 레코드는 정확히 다음으로 구성됩니다:
     - `TABLES` 섹션 (4개): `VPORT` 1개, `APPID` 3개
     - `OBJECTS` 섹션 (48개): `DICTIONARY` 12개, `ACDBDICTIONARYWDFLT` 1개, `ACDBPLACEHOLDER` 1개, `LAYOUT` 2개, `MATERIAL` 3개, `MLINESTYLE` 1개, `MLEADERSTYLE` 1개, `DICTIONARYVAR` 2개, `VISUALSTYLE` 25개
     - 총계: $1 + 3 + 48 = \mathbf{52\text{개}}$
  4. 프로덕션 코드 및 테스트 상의 결함은 0건이며, 본 수정은 검토서 내 근거 분류 기술을 원본 소스 팩트에 부합하도록 정정한 순수 문서 정합성 보정입니다.

### 2.1 린(Thin) 불변 스냅샷 아키텍처
`RawDrawingSnapshot`은 새로운 복제 모델을 생성하지 않고, 이전 단계의 아티팩트를 직접 조합합니다:
- `IdentityEvidence`가 단일 루트 역할을 수행하며, `BlockAnnotationEvidence`, `GeometryEvidence`, `StructuralEvidence`, `LexicalEvidence`는 단방향 읽기 전용 체인으로 연결됩니다.
- 스냅샷이 새로 소유하는 데이터는 오직 다음 4가지로 한정됩니다:
  1. `Records`: 333개 자식 레코드와 이전 단계 인덱스를 연결하는 `RawDrawingRecordView` 불변 목록
  2. `Diagnostics`: 6개 단계의 불변 진단 객체를 가리키는 `(Stage, Index)` 참조 목록
  3. `AssemblyDiagnostics`: 조립 계층 고유 진단 목록 (정상 조립 시 0건)
  4. `Completeness`: 10개 세부 축으로 구성된 완전성 메트릭
- 기하 레코드(`RawGeometryRecord`), 블록 정의(`RawBlockDefinition`), 주석 페이로드, 식별자 노드(`DxfIdentityNode`) 등은 단 한 건도 복제(Clone)되지 않으며, `Assert.Same` 검증을 통해 동일한 인스턴스를 유지함을 확인하였습니다.

### 2.2 333개 자식 레코드 뷰(View) 및 테이블 헤더 격리
- 통제 도면의 333개 구조적 자식 레코드와 `RawDrawingRecordView`는 정확히 1:1 대응하며, `StructuralRecordIndex` 0부터 332까지 원본 순서가 그대로 유지됩니다.
- 03E의 9개 심볼 테이블 헤더 식별자 노드는 `DxfGraphRecordLocator(TableHeader, index)`로 관리되며, 자식 레코드 뷰 목록(`Records`)에 가상 엔트리로 침범하지 않습니다 ($333 \neq 342$).
- 레코드별 매핑 무결성:
  - 기하 페이로드 연결: 89건 (`Assert.Same`)
  - 블록/주석 페이로드 연결: 93건 (`Assert.Same`)
  - 식별자 노드 연결: 318건 (327개 노드 중 9개 테이블 헤더를 제외한 318개 자식 노드와 1:1 일치)
  - 상충 페이로드(기하와 블록/주석 동시 할당 등) 검출 시 즉각 `ConflictingAssociation`으로 조립을 거부합니다.

### 2.3 Unsupported 레코드 보존
- 통제 도면의 52개 `Unsupported` 자식 레코드는 결코 버려지거나 누락되지 않고 333개 뷰의 정식 구성원으로 보존됩니다.
- 각 `Unsupported` 뷰는 원본 레코드 타입(`TABLES`의 `VPORT` 1개, `APPID` 3개 및 `OBJECTS`의 48개 객체: DICTIONARY 12, ACDBDICTIONARYWDFLT 1, ACDBPLACEHOLDER 1, LAYOUT 2, MATERIAL 3, MLINESTYLE 1, MLEADERSTYLE 1, DICTIONARYVAR 2, VISUALSTYLE 25), 원본 스팬, 원본 렉시컬 페어, 그리고 식별자 노드(52개 모두 유효 핸들 보유)를 그대로 유지합니다. (통제 도면에 HATCH는 존재하지 않으며, DIMENSION 4개는 전량 03D에서 Supported 처리되어 유지됨)
- 조립 성공(`Succeeded`)이 `Unsupported` 레코드를 `Supported`로 임의 승격시키지 않습니다.

### 2.4 완전성(Completeness) 모델 및 상태 분리
- `RawSnapshotCompleteness`는 10개 독립 축을 제공합니다:
  - `LexicalSucceeded`: `true`
  - `StructureValidated`: `true`
  - `GeometryMappingComplete`: `true`
  - `BlockAnnotationMappingComplete`: `true`
  - `IdentityCoverageComplete`: `true`
  - `ReferenceResolutionComplete`: `true`
  - `GraphValidated`: `true`
  - `SourceEvidenceComplete`: `true` (6개 섹션이 모두 기검토된 공식 섹션임)
  - `TypedMappingComplete`: **`false`** (52개 Unsupported 레코드와 unmapped HEADER 변수가 존재하므로 거짓으로 평가)
  - `SnapshotAssemblyComplete`: `true`
  - 파생 `Coverage`: `Partial`
- `SnapshotAssemblyComplete = true`와 `Coverage = Partial`이 정상 공존하며, 부분 커버리지라고 해서 작업 상태가 `InvalidInput`으로 왜곡되지 않습니다.

---

## 3. Strict Graph Gate Policy Assessment (Critical)

조립기(`TextDxfRawDrawingSnapshotAssembler`) 35~37행에 정의된 엄격한 사전 조건:
```csharp
if (!summary.GraphValidated || !summary.IdentityCoverageComplete || !summary.ReferenceResolutionComplete)
    Refuse(RawSnapshotDiagnosticCode.PredecessorRejected, "V1 snapshot requires successful source stages and complete validated identity/reference coverage.");
```
이에 대한 아키텍처/적대적 평가는 다음과 같습니다:

1. **Option A (의도된 V1 보수적 게이트)**:
   - 통제 프로파일(`DtoB_TEST_2STORY_R2018`)에서는 458개 참조가 100% 해소(Resolved 438, Null 20, 미해소 0)되므로 조립에 아무런 문제가 없습니다.
   - 파이프라인 개발 초기 단계에서 식별자/참조 미해소 상태의 불완전한 데이터가 후속 정규화 계층(Phase 04)으로 유입되는 것을 원천 차단하는 방어적 게이트로서의 역할을 수행합니다.

2. **Option B (완전성 해소와 스냅샷 조립의 개념적 결합 — Architectural Tension)**:
   - 도메인 모델인 `RawSnapshotCompleteness.Coverage`는 다음과 같이 정의되어 있습니다:
     ```csharp
     public RawSnapshotCoverage Coverage => SourceEvidenceComplete && TypedMappingComplete && GraphValidated &&
         IdentityCoverageComplete && ReferenceResolutionComplete ? RawSnapshotCoverage.Complete : RawSnapshotCoverage.Partial;
     ```
     즉, `ReferenceResolutionComplete`가 `false`이더라도 `Coverage.Partial`을 산출할 수 있도록 설계되어 있습니다.
   - 그러나 조립기의 진입 게이트가 `!summary.ReferenceResolutionComplete`일 때 조립 자체를 거부(`Snapshot = null`)하므로, 실제 발행된 스냅샷 내부에서는 `Coverage.Partial` 중 `ReferenceResolutionComplete == false`인 케이스가 결코 존재할 수 없는 모순이 발생합니다.
   - 실도면 환경에서는 리액터(`Reactor`)나 확장 사전(`XDictionary`)과 같은 선택적 비소유자 포인터가 댕글링(Unresolved)되어 있는 경우가 빈번합니다. 이러한 경우에도 기하학적 요소와 블록은 완벽히 보존될 수 있으므로, 비소유자 포인터 미해소만으로 스냅샷 발행 전체를 거부하는 것은 "사실적 CAD 추출 보존"이라는 스냅샷 본연의 취지와 상충될 소지가 있습니다.

이 긴장 관계를 **LOW-01** 관찰 항목으로 정식 기록합니다 (V1 통제 도면 통과에는 영향 없음).

---

## 4. Controlled Count Domains Summary

| 도메인 구분 | 기대/측정값 | Snapshot 노출 일치 여부 |
|---|---|:---:|
| 소스 바이트 / 라인 / 렉시컬 페어 | 96,621 / 17,474 / 8,737 | 일치 (`LexicalEvidence`) |
| 섹션 / HEADER 변수 / 심볼 테이블 | 6 / 251 / 9 | 일치 (`StructuralEvidence`) |
| 구조적 자식 레코드 총계 | 333 | 일치 (`Records.Count`) |
| 시맨틱 Disposition (Supported / Unsupported) | 281 / 52 | 일치 (`SemanticDispositionCounts`) |
| 기하 레코드 / 03D 레코드 / 블록 정의 | 89 / 93 / 12 | 일치 (`Geometry`, `BlockAnnotations`, `BlockDefinitions`) |
| 식별자 검사 위치 (자식 333 + 헤더 9) | 342 | 일치 (`Nodes` 327 + `NonIdentityRecords` 15) |
| 식별자 노드 (TABLES 97, BLOCKS 68, ENTITIES 114, OBJECTS 48) | 327 | 일치 (`IdentityNodes.Count`) |
| 참조 에지 (Resolved 438, Null 20, 미해소 0) | 458 | 일치 (`References.Count`) |
| 소유권 에지 / 독립 루트 / 소유권 순환 | 317 / 10 / 0 | 일치 (`Ownership.Count`, `OwnerRoots.Count`) |
| 진단 참조 총계 (전체 03B에서 발생) | 59 | 일치 (`Diagnostics.Count`) |

---

## 5. Traceability Goldens Verification

스냅샷 인터페이스를 통해 최하위 물리적 렉시컬 페어 및 텍스트 원본까지 단 하나의 왜곡 없이 추적 가능함을 확인하였습니다:
- `$ACADVER`: Pair 3 $\rightarrow$ 물리 행 7~8 $\rightarrow$ `"AC1032"`
- `INSUNITS`: Raw 4 (mm로의 임의 변환 없음)
- `EXTMIN` / `EXTMAX`: 각각 `+1e20` / `-1e20` 센티넬 원본 값 유지 (바운드 역전 수선 없음)
- `$HANDSEED`: Pair 277 $\rightarrow$ `"161"` (객체 식별자로의 위조 없음)
- `A-WALL` 레이어: Pair 1150 $\rightarrow$ `"A-WALL"`
- `LINE` Start.X: Pair 2862 $\rightarrow$ `"0.0"`, End.X = 900
- `LWPOLYLINE` Vertex 0: Pairs 2724/2725 $\rightarrow$ `"-0.5"`, `"-0.5"`
- `ARC` 각도: Pairs 2879/2880 $\rightarrow$ `0.0`도, `90.0`도 (라디안 변환 없음)
- `COLUMN_400`: 블록 정의 이름 일치
- `INSERT` `_ARCHTICK`: Pair 2977 $\rightarrow$ `"_ARCHTICK"`, Insertion.Y Pair 2979 $\rightarrow$ `"-1300.0"`, 회전 180도
- `TEXT` 1F FLOOR PLAN: Pair 3431 $\rightarrow$ `"1F FLOOR PLAN"`
- `DIMENSION`: Pair 3930 $\rightarrow$ TypeFlags 32, measurement 42 결측(Absent) 정확히 보존
- `DIMSTYLE` Handle: Pair 1701 $\rightarrow$ Code 105 `"2B"`
- `DICTIONARY` Root Handle: Pair 4906 $\rightarrow$ `"A"`
- `Owner` 에지: Pair 2664 $\rightarrow$ Target `"17"`
- `Reactor` 에지: Pair 5187 $\rightarrow$ Target `"E"` $\rightarrow$ 대상 소스 Pair 4953 추적 완료

---

## 6. Review Findings

### LOW-01: Assembler Precondition Strictly Requires `ReferenceResolutionComplete == true`, Precluding Partial Snapshots with Dangling Non-Owner Pointers

- **Severity**: LOW
- **File / Section**: `packages/DtoB.Cad.Dxf/TextDxfRawDrawingSnapshotAssembler.cs` L36-37
- **Problem**: 조립기 사전 조건에서 `!summary.ReferenceResolutionComplete`일 경우 `PredecessorRejected`로 조립 자체를 거부합니다. 반면 `RawSnapshotCompleteness` 계약에서는 `ReferenceResolutionComplete`가 `false`여도 `Coverage.Partial` 상태의 스냅샷을 구성할 수 있도록 지원하고 있습니다.
- **Impact**: V1 통제 도면 및 통제 프로파일에서는 모든 참조가 해결되므로 영향이 없습니다. 그러나 향후 실제 외부 CAD 도면에서 비소유자 포인터(선택적 리액터, 뷰스타일 등)가 미해소된 상태일 때, 완벽하게 추출된 기하 및 엔티티 정보를 담은 부분 스냅샷(`Coverage.Partial`)을 생성하지 못하고 전체 거부될 수 있습니다.
- **Reproduction**: 비소유자 댕글링 포인터가 포함된 `datasets/controlled/phase03e/unresolved-pointer.dxf` 입력 시 `Assemble()`이 `InvalidInput`으로 실패함.
- **Recommended Fix**: V1에서는 통제 프로파일 준수를 위해 현재 보수적 정책을 유지하되, 향후 일반 도면 수용 단계(Phase 03G 또는 Phase 04)에서 `GraphValidated == true`(핸들 고유성 및 소유권 DAG 무결성) 조건만 필수로 유지하고, 비소유자 포인터 미해소 시에는 경고 진단을 첨부하여 `Coverage.Partial` 스냅샷을 정상 발행하도록 조건을 완화할 것을 권장합니다.
- **Required Test**: 향후 완화 시 `unresolved-pointer` 입력에 대해 `Status == Succeeded`, `Snapshot != null`, `Completeness.ReferenceResolutionComplete == false`, `Completeness.Coverage == Partial`을 검증하는 테스트 추가 필요.

---

## 7. Final Assessment & Verdict

| 검토 영역 | 상태 | 세부 평가 |
|---|:---:|---|
| Snapshot 모델 및 린 조합 | **PASS** | 선행 단계 인스턴스 직접 참조, 재복제 0건, JSON 포워더 무시 완벽 |
| 333개 자식 레코드 뷰 1:1 보존 | **PASS** | 원본 물리적 순서 100% 유지, 누락/중복/위조 0건 |
| 심볼 테이블 헤더 도메인 격리 | **PASS** | `TableHeader` 로케이터 분리로 자식 뷰 오염 원천 차단 |
| 11개 카운트 도메인 분리 | **PASS** | 선행 단계 확립 수치와 단 1건의 오차 없이 전수 일치 |
| Unsupported 레코드 보존 | **PASS** | 52개 레코드 완전 보존 및 임의 승격 배제 |
| 완전성 10축 모델 | **PASS** | `SnapshotAssemblyComplete`와 `TypedMappingComplete` 엄격 분리 |
| 엄격한 그래프 게이트 | **PASS (LOW)**| V1 통제 기준 충족 (LOW-01 아키텍처 관찰 사항 기록) |
| 출처 추적성 (Traceability) | **PASS** | Goldens 16건 전수 물리 행/페어/텍스트 추적 완료 |
| 불변성 (Immutability) | **PASS** | `Freeze` 방어 복제 및 컬렉션 불변 뷰 보장 |
| 결정론적 QA 직렬화 | **PASS** | 3회 반복 실행 SHA-256 비트 일치 검증 완료 |
| 리소스 한도 및 취소 | **PASS** | 3개 한도 초과 시 즉각 거부 및 취소 토큰 감지 정상 작동 |
| 종속성 및 Phase 경계 | **PASS** | .NET 8 순수 라이브러리 유지, Viewer/정규화/Revit 침범 0건 |
| 회귀 안전성 | **PASS** | 558/558 전체 테스트 전원 통과, 통제 도면 해시 불변 |

### 최종 판정:
$$\mathbf{PHASE\_03F\_PASS}$$
- **BLOCKER**: 0건
- **HIGH**: 0건
- **MEDIUM**: 0건
- **LOW**: 1건 (LOW-01: 향후 일반 도면 수용을 위한 아키텍처 개선 권고)
