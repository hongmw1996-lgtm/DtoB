# PHASE 04A — CAD IR Contracts & Normalization Boundary Independent Implementation Review

## Executive Summary

- **Review Target**: PHASE 04A — CAD IR Contracts & Normalization Boundary (`packages/DtoB.Cad/V2/`, `packages/DtoB.Cad.Normalize/`, `tests/DtoB.Tests/Cad*Tests.cs`, `docs/status/PHASE_04A_REPORT.md`)
- **Review Date**: 2026-10-16
- **Review Mode**: Independent Architecture & Implementation Gate Review
- **Reviewer**: Architecture / Adversarial Review Owner (Antigravity)
- **Status of Codex**: `PHASE_04A_PASS_WITH_KNOWN_LIMITATIONS`
- **Final Verdict**: **`PHASE_04A_PASS`**

DtoB 시스템의 텍스트 DXF 파이프라인(Phase 03) 완료 및 아키텍처 동결(ADR-018, ADR-019, ADR-020 수락)에 이어, 정규화된 2D/3D CAD 공간 모델의 첫 번째 구현 단계인 **PHASE 04A CAD IR Contracts & Normalization Boundary**에 대해 엄격하고 독립적인 구현 감사를 수행하였습니다.

PHASE 04A의 주 목적은 `RawDrawingSnapshot`을 입력으로 받는 순수 정규화 경계 인터페이스(`ICadNormalizer`), Revit에 독립적인 불변의 CAD v2 전송 계약(`DtoB.Cad.V2.CadDocument`), 전용 직렬화 코덱(`CadIrJson`), 엄밀한 도메인 분리 결정론적 식별자 프레이밍(`CadId`), 컴팩트한 소스 역추적 레지스트리(`CadProvenanceRegistry`), 그리고 방어적 불변 변환 값 객체(`CadTransform`)를 확립하는 것입니다.

독립 감사 결과:
1. **04A 스코프 경계 엄격 준수 (Zero Premature Scope Leakage)**:
   - 단위 변환($INSUNITS), 각도 변환(degree $\rightarrow$ radian), OCS $\rightarrow$ WCS (AAA), 블록 베이스 포인트 보정, INSERT 배치 행렬 합성, 중첩/MINSERT 전개, ByLayer/ByBlock 계통 해결, Bulge 호 변환, 기하 외접 바운즈 계산, 도면 인식, BIM 의미론, Revit 호출 등 04B 이후의 알고리즘이나 기능이 프로덕션 코드에 조기 구현되거나 누출되지 않고 순수 계약 및 인터페이스 스캐폴딩으로 정확히 한정되었습니다.
2. **CAD v2 스키마 소유권 및 v1 격리 (H-01 완전 구현)**:
   - Core의 레거시 `Contract.Version(1)` 및 `IrJson` 검증 경로를 우회하고, `DtoB.Cad.V2` 내부에 신설된 `CadContract`와 `CadIrJson`이 `schemaVersion == 2`를 독립적으로 검증함을 입증하였습니다.
   - 레거시 `CadContracts.cs` 및 `IAnalysisEngine`의 v1 바인딩이 100% 보존되었으며, 네임스페이스 모호성에 의한 v1/v2 오바인딩이 철저히 차단되었습니다.
3. **엄격한 폐쇄형 JSON 코덱 및 결정론적 직렬화**:
   - .NET 8 `JsonSerializer`의 생성자 파라미터 누락 취약점을 방어하기 위해 `ValidateShape` 재귀 검사기를 도입하여, 누락된 멤버(nullable 필드의 명시적 null 포함), 미인식 필드, 중복 필드, 잘못된 열거형/다형성 판별자를 중첩 레벨까지 엄격히 차단합니다.
   - 3대 문화권(Invariant, fr-FR, ko-KR) 및 복수 작업 디렉터리/프로세스 환경에서 고정 계약 픽스처의 UTF-8 직렬화 바이트 해시(`3E49A200C8AD688F6F53842F17556D849979B1C8F3F6A083438721CA02F58EBC`)가 단 1비트의 오차도 없이 일치함을 확인하였습니다.
4. **결정론적 cad2 바이너리 프레이밍 식별자**:
   - ADR-020의 사양에 따라 UTF-8 도메인 태그, 7대 필드 타입 태그, Big-Endian 4바이트 길이/카운트 프레이밍 기반의 SHA-256 디제스트(`cad2:<digest>`)가 완벽하게 구현되었습니다.
   - 수작업 바이트 프레이밍 검증 벡터(`Source/Text("A")` $\rightarrow$ `cad2:1c249a29d20262678537453687e3bc3428b736b403bb41db13c2e6061fc036b6`)가 정확히 일치함을 확인하였습니다.
5. **완벽한 불변성 방어 (M-01 완전 구현)**:
   - `CadTransform`은 16개 double의 내부 배열을 `private readonly`로 은닉하고, 생성/역직렬화/외부 프로젝션(`ToTransform3()`) 시 방어적 복사를 수행하여 외부 배열 변조 공격으로부터 도면 및 인스턴스 행렬을 완벽히 보호합니다.
   - 모든 DTO 컬렉션은 읽기 전용 래퍼로 노출되어 외부 변경 시도 시 `NotSupportedException`을 발생시킵니다.
6. **패키지 의존성 및 프로덕션 소스 무결성**:
   - 신규 `DtoB.Cad.Normalize` 라이브러리는 오직 `DtoB.Cad.Raw`와 `DtoB.Cad`만을 참조하며, UI/Desktop/Revit/Project/WPF 역참조가 전무합니다.
   - 보호 대상인 `DtoB.Cad.Raw`, `DtoB.Cad.Dxf`, `DtoB.Core`, `DtoB.Geometry`, `DtoB.Analysis`, 86개 통제 픽스처는 단 1바이트의 수정도 없이 보존되었습니다.
7. **테스트 스위트 무결성 및 레거시 ADR 테스트 적응 검증**:
   - 기존 773개 릴리스 테스트가 전원 통과하는 상태에서 71개의 신규 04A 테스트가 추가되어 **총 844개 중 844개 전원 통과 (Pass 844, Fail 0, Skip 0)**를 달성하였습니다.
   - `FoundationDocumentTests`의 ADR 테스트 수정은 ADR-001~017의 기존 검증을 100% 보존하면서, 소유자가 공식 승인한 ADR-018~020의 형식(대안 선택 1건 이상, 기각 2건 이상과 명확한 사유)을 엄격히 검증하도록 좁고 정확하게 조정된 정당한 적응임을 확인하였습니다.

모든 구현 및 아키텍처 기준이 완벽하게 충족되었으므로 최종 승인 판정(**`PHASE_04A_PASS`**)을 부여합니다.

---

## 1. Scope Audit & 24 Review Areas Independent Assessment

### 1. ADR Acceptance Integrity
- **검증 결과**: **적합 (SATISFIED)**
- ADR-018, ADR-019, ADR-020의 파일 변경 내역을 전수 검사한 결과, 오직 `Status: Proposed` 라인이 `Status: Accepted`로만 변경되었음을 확인하였습니다. (글자 수 8자로 동일하여 파일 바이트 수 ADR-018: 8,925B, ADR-019: 13,170B, ADR-020: 10,113B 완벽 불변).
- 기술 본문이나 거버넌스 내용에 대한 무단 수정은 단 1자도 없었습니다.

### 2. 04A Scope Boundary
- **검증 결과**: **적합 (CLEAN BOUNDARY)**
- `packages/DtoB.Cad/V2/` 및 `packages/DtoB.Cad.Normalize/` 소스 코드를 전수 분석한 결과, 단위 변환($INSUNITS), 각도 변환(degree $\rightarrow$ radian), OCS $\rightarrow$ WCS, Arbitrary Axis Algorithm, 블록 베이스 포인트 보정, INSERT 배치 행렬 합성, 중첩 INSERT/MINSERT 전개, ByLayer/ByBlock 상속, Bulge 호 변환, 아핀 곡선 바운즈 계산 등의 후속 페이즈 알고리즘이 전혀 포함되지 않았습니다.
- 04A는 순수하게 전송 계약 DTO, 스키마 검증기, 코덱, 불변 래퍼, 식별자 프레이밍, 정규화 인터페이스 선언으로 엄격히 한정되었습니다.

### 3. CAD v2 / Legacy CAD v1 Isolation
- **검증 결과**: **적합 (SATISFIED)**
- 레거시 `packages/DtoB.Cad/CadContracts.cs`는 0바이트 수정 없이 온전히 보존되었습니다.
- `DtoB.Cad.Normalize/NormalizationBoundary.cs`에서 `using V2Document = DtoB.Cad.V2.CadDocument;` 별칭을 사용하여, 상위 네임스페이스 `DtoB.Cad.CadDocument`(v1)와의 모호성을 원천 차단하고 명시적으로 v2 타입을 바인딩하였습니다.

### 4. CAD v2 Version Ownership (Finding H-01 구현 확인)
- **검증 결과**: **적합 (SATISFIED)**
- `DtoB.Cad.V2.CadContract`에 `SchemaMajor = 2` 상수가 선언되어 있으며, `CadContract.Version(value)`가 이를 전담 검증합니다.
- Core의 `Contract.Version(1)`이나 `IrJson.Deserialize`를 호출하는 코드가 V2 경로에 전무함을 확인하였습니다.
- major 1, major 3, 부동소수점, 문자열, 불리언, null, 결측 버전 등에 대해 `CadIrJson.Deserialize`가 즉시 `InvalidDataException`을 발생시킴을 독립 테스트로 확인하였습니다.

### 5. Strict JSON Contract
- **검증 결과**: **적합 (SATISFIED)**
- `CadIrJson.ValidateShape()`를 통해 역직렬화 전 JSON AST를 재귀 순회하여:
  - 선언된 속성 외의 미인식 멤버 차단 (`Unknown CAD member`)
  - 동일 객체 내 중복 키 차단 (`Duplicate JSON member`)
  - 누락된 멤버 차단 (`Missing CAD member; nullable facts require explicit null`)
  - 다형성 기하 판별자(`$kind`) 검증
  - 열거형의 정수 표기 차단 (카멜케이스 문자열만 허용)
- 이를 통해 .NET 8의 생성자 기본값 바인딩 결함을 원천 차단하고 엄격한 폐쇄형 스키마를 보장합니다.

### 6. Deterministic Serialization
- **검증 결과**: **적합 (BYTE-IDENTICAL CONFIRMED)**
- `CadIrJson`은 `schemaVersion`을 항상 최상단에 배치하고, 그 외 속성을 `StringComparer.Ordinal`로 정렬하여 직렬화합니다. 또한 줄바꿈을 `\n`으로 정규화하고 음의 영(`-0.0`)을 `0.0`으로 정규화합니다.
- `CadV2ContractFixture.Document()`를 Invariant, `fr-FR`, `ko-KR` 3대 로케일에서 직렬화한 결과, 모든 환경에서 정확히 동일한 바이트와 SHA-256 해시(`3E49A200C8AD688F6F53842F17556D849979B1C8F3F6A083438721CA02F58EBC`)가 산출됨을 확인하였습니다.

### 7. CAD2 Identity Framing
- **검증 결과**: **적합 (MATHEMATICALLY & BYTE VERIFIED)**
- `CadIdentityField`는 7대 타입 태그(0=null, 1=text, 2=GUID, 3=hash, 4=handle, 5=index, 6=tuple)와 4바이트 Big-Endian 길이를 사용하여 모호성 없는 바이너리 튜플을 생성합니다.
- `CadId.Create`는 UTF-8 도메인 태그 `dtob/cad2/id/v1`, 엔티티 도메인명, 필드 튜플을 결합하여 SHA-256 디제스트(`cad2:<lowercase hex>`)를 생성합니다.
- 수작업 검증 벡터 `Source/Text("A")`의 해시 `1c249a29d20262678537453687e3bc3428b736b403bb41db13c2e6061fc036b6` 및 튜플 ID `cad2:d737c924710a2add9545fb55932593b7b604672a63267a131819bbc0fcd694e3`가 일치함을 확인하였습니다.

### 8. Provenance Registry
- **검증 결과**: **적합 (SATISFIED)**
- `CadSourceDocument` (DrawingId, RevisionId, FileHash), `CadSourceLocator` (Domain, RecordIndex, FirstPair, LastPair, FirstPhysicalLine, LastPhysicalLine), `CadSourceRecord` (OriginalHandle, CanonicalHandle), `CadSourceFact` (RecordRef, Role, Presence, PairIndices, FirstPhysicalLine, LastPhysicalLine)가 컴팩트하게 구조화되었습니다.
- 거대한 Raw 객체를 매 DTO마다 복제하지 않고 인덱스 참조(`CadSourceRecordRef`, `CadSourceFactRef`)를 사용하며, 레지스트리는 순서 정렬, 중복 방지, 바운드 검사를 엄격히 수행합니다.

### 9. Immutability (Finding M-01 구현 확인)
- **검증 결과**: **적합 (CLOSED_BY_TESTS)**
- `CadTransform`은 16개 double 배열을 방어적 복사하여 은닉하고, `ToTransform3()` 호출 시에도 `(double[])matrix.Clone()`을 반환합니다.
- 외부에서 입력 배열을 수정하거나, 반환된 `Transform3.Matrix`를 수정하거나, `Values` 뷰를 수정하려 해도 원본 상태가 변조되지 않음이 `TransformImmutabilityTests`에서 완벽히 입증되었습니다.
- DTO 내 모든 컬렉션은 읽기 전용 래퍼로 보호됩니다.

### 10. CadTransform Scope
- **검증 결과**: **적합 (STORAGE ONLY)**
- `CadTransform`에는 `Placement`, `Then`, 행렬 곱, OCS 변환, 단위 변환 등의 연산 엔진 코드가 전혀 없으며, 순수한 불변 저장 값 객체(Storage value object) 역할만 수행함을 확인하였습니다.

### 11. Normalization Boundary
- **검증 결과**: **적합 (PURE CONTRACT)**
- `ICadNormalizer`는 `Normalize(RawDrawingSnapshot snapshot, CadNormalizationOptions options, CancellationToken cancellationToken = default)` 시그니처를 가지며, 반환 타입은 `CadNormalizationResult`입니다.
- 어셈블리 내에 `ICadNormalizer`를 상속받은 가짜 정규화 엔진 클래스가 일절 존재하지 않음을 리플렉션 테스트로 확인하였습니다.

### 12. Result State Consistency
- **검증 결과**: **적합 (SATISFIED)**
- `CadNormalizationResult` 생성자에서:
  - `Normalized`/`PartiallyNormalized` 상태는 반드시 `Document != null`이어야 함.
  - `InvalidNormalization`/`Cancelled`/`ResourceLimitExceeded`/`NotImplemented` 상태는 반드시 `Document == null`이어야 함.
  - 거부(`Document == null`) 시에는 반드시 진단(`Diagnostics.Count > 0`)이 포함되어야 함.
  - 모순된 상태 조합을 원천 차단함을 확인하였습니다.

### 13. Options / Resource Contracts
- **검증 결과**: **적합 (ALIGNED WITH ADR-020)**
- `CadNormalizationLimits.Development` 기본값: `MaxDepth = 32`, `MaxCellsPerInsert = 10,000`, `MaxOccurrences = 100,000`, `MaxDerivedGeometry = 250,000`, `MaxDiagnostics = 5,000`.
- 04A에서는 양수 유효성 검사만 수행하며, 실제 카운터나 전개 알고리즘은 포함되지 않았습니다.

### 14. Project Dependency Graph
- **검증 결과**: **적합 (CLEAN GRAPH)**
- `DtoB.Cad.Raw` $\rightarrow$ 참조 없음
- `DtoB.Cad.Dxf` $\rightarrow$ `DtoB.Cad.Raw`
- `DtoB.Cad` $\rightarrow$ `DtoB.Core`, `DtoB.Geometry`
- `DtoB.Cad.Normalize` $\rightarrow$ `DtoB.Cad.Raw`, `DtoB.Cad`
- `DtoB.Analysis` $\rightarrow$ `DtoB.Cad (v1)`, `DtoB.Bim`
- 어떠한 역참조나 UI/Desktop/Revit/Project/WPF 종속성도 없습니다.

### 15. Raw / Dxf / Legacy Source Immutability
- **검증 결과**: **적합 (ZERO MUTATION)**
- `DtoB.Cad.Raw`, `DtoB.Cad.Dxf`, `DtoB.Core`, `DtoB.Geometry`, `DtoB.Cad/CadContracts.cs`, `DtoB.Analysis`, `DtoB.Desktop`, `DtoB.Revit` 내 모든 파일의 Git diff가 0바이트임을 확인하였습니다.
- 86개 통제 픽스처 해시 역시 100% 동일합니다.

### 16. Revit Independence Audit
- **검증 결과**: **적합 (0 FINDINGS)**
- V2 및 Normalize 소스, 어셈블리 메타데이터, 프로젝트 파일 전수 검사 결과, `Autodesk.Revit`, `RevitAPI`, `ElementId`, `BuiltInCategory`, `FamilySymbol`, `WallType`, 내부 피트 단위 등의 오염이 전무함을 확인하였습니다.

### 17. Geometry Contract Honesty
- **검증 결과**: **적합 (HONEST CONTRACTS)**
- `CadGeometry` 하위 클래스(`CadPoint`, `CadLine`, `CadCircle`, `CadArc`, `CadPolyline`, `CadSolid`)는 기본적인 수치 유한성/양수성 검사만 수행하며, 공간 유효성이나 직교 기저, 곡선 바운즈 등을 사전에 거짓 주장하지 않습니다.
- `CadAnnotation`의 `SourceMeasurementMm`은 부재 시 0이 아닌 null로 정직하게 유지됩니다.

### 18. Critical Review of Modified Legacy ADR Test
- **검증 결과**: **적합 (APPROVED NARROW ADAPTATION)**
- `FoundationDocumentTests.cs`의 `FoundationDocumentsAreIntact` 테스트 변경 내역을 정밀 조사하였습니다:
  - 레거시 ADR-001~017에 대해서는 기존의 엄격한 Markdown 테이블(`| Option | Evaluation`) 검증을 100% 동일하게 유지합니다.
  - 신규 ADR-018~020에 대해서는 소유자가 공식 승인한 형식(불릿/산문 형태)에 맞춰, `## Alternatives` 섹션이 100자 이상이고, 선택된 대안(`selected`) 1개 이상 및 기각된 대안(`rejected`) 2개 이상과 사유를 엄격히 강제하도록 좁게 분기 처리되었습니다.
  - 이는 런타임 프로덕션 테스트나 아키텍처 거버넌스를 약화시킨 것이 아니라, 소유자가 정당하게 승인한 형식을 엄밀하게 검증하도록 테스트를 정밀 조정한 것입니다.

### 19. Test Quality / Self-Oracle Check
- **검증 결과**: **적합 (INDEPENDENT ORACLES)**
- `CadV2ContractFixture`는 수작업으로 작성된 불변 계약 픽스처이며, 프로덕션 파서의 출력을 오라클로 사용하는 자가 오라클(Self-oracle) 현상이 없습니다.
- `CadIdentity`의 바이너리 프레이밍 테스트 역시 독립 열거된 바이트 스트림과 수작업 해시를 기대값으로 사용하였습니다.

### 20. Release Regression Result
- **검증 결과**: **적합 (773 RETAINED & PASS)**
- Phase 03 릴리스 기준 773개 테스트 케이스의 식별자를 전수 대조한 결과, 누락되거나 실패한 테스트는 0건이며 전원 통과함을 확인하였습니다.
- 신규 71개 테스트를 합산하여 **총 844건 전원 통과**를 달성하였습니다.

### 21. Builds and Warning Gate
- **검증 결과**: **적합 (0 ERRORS, NO NEW WARNINGS)**
- Solution Release Rebuild: **0 Errors, 3 Warnings** (기존에 허용된 Revit 런타임 어셈블리 바인딩 경고 MSB3277 외 신규 경고 0건).

### 22. Diff and Scope Audit
- **검증 결과**: **적합 (CLEAN WORKING TREE)**
- 생성된 파일은 순수 C# 소스 파일, 프로젝트 파일, 문서 파일뿐이며, 의도되지 않은 바이너리, obj/bin 누출, 임시 파일, 비밀 키 등이 작업 트리에 존재하지 않음을 확인하였습니다.

### 23. Known Limitations Validity
- **검증 결과**: **적합 (ACCURATE & HONEST)**
- 보고서에 명시된 한계(정규화 엔진 미구현, OCS/WCS 미변환, 블록 미전개, 수치 공차 정책 04B 이관 등)가 실제 코드 상태와 완벽히 일치함을 확인하였습니다.

### 24. 04B Readiness
- **검증 결과**: **적합 (READY FOR 04B)**
- 04A의 계약 기반(`CadDocument`, `CadUnitDecision`, `CadConventions`, `CadAnnotation`)은 04B에서 단위($INSUNITS), 각도 필드 역할, 수치 공차 정책을 도입할 때 계약의 파괴적 수정 없이 자연스럽게 수용할 수 있도록 완벽히 준비되었습니다.

---

## 2. Findings Summary

- **BLOCKER**: 0건
- **HIGH**: 0건
- **MEDIUM**: 0건
- **LOW**: 0건

모든 구현 내용이 ADR-018, ADR-019, ADR-020의 규정과 아키텍처 경계를 완벽하게 충족하고 있습니다.

---

## 3. Final Verdict

```text
================================================================================
FINAL VERDICT: PHASE_04A_PASS
================================================================================
```

PHASE 04A 구현은 ADR-018, ADR-019, ADR-020의 아키텍처 결정을 충실히 이행하였으며, 후속 페이즈 기능의 조기 누출 없이 견고하고 엄격한 CAD v2 계약 및 정규화 경계를 성공적으로 구축하였습니다. 이에 따라 **`PHASE_04A_PASS`**를 선언합니다.

*(주의: 본 판정은 PHASE 04A 단일 서브페이즈에 대한 최종 승인이며, PHASE 04B 시작, 커밋 또는 푸시는 작업 소유자의 명시적 지시에 의해서만 진행되어야 합니다.)*
