# PHASE 03B Native Text DXF Structural / Header / Tables Reader Implementation Review

## Executive Summary

- **Review Target**: PHASE 03B — Native Text DXF Structural / Header / Tables Reader (`packages/DtoB.Cad.Raw/DxfStructuralContracts.cs`, `packages/DtoB.Cad.Dxf/*`, `tests/DtoB.Tests/TextDxfStructuralReaderTests.cs`)
- **Review Date**: 2026-10-14
- **Review Mode**: Targeted Re-Review after H-01 / L-01 Corrections
- **Reviewer**: Architecture / Adversarial Review Owner (Antigravity)
- **Previous Verdict**: `PHASE_03B_CHANGES_REQUIRED`
- **Final Verdict**: **`PHASE_03B_PASS`**

이전 검토에서 지적된 **H-01 (DIMSTYLE 중복 Group Code 70 오분류 결함)** 및 **L-01 (`UnmappedHeaderVariables` 진단의 Source Span 누락)**에 대한 집중 재검토를 수행하였습니다.

독립적인 검증 결과, Codex는 문맥 인식 기반의 `DimstyleCode70Roles` 분류기를 구현하여:
1. 심볼 테이블 표준 플래그(`StandardFlags`, code 70)와 치수 텍스트 배경 채우기 색상(`TextFillColor` / `DIMTFILLCLR`, code 70)을 first/last-wins 없이 결정론적 문맥에 따라 완벽히 분리 매핑하였으며,
2. 통제 도면(`DtoB_TEST_2STORY_R2018.dxf`)의 12개 DIMSTYLE 엔트리 전체를 정상적인 `DxfDisposition.Supported`로 복원(InvalidTableValue 에러 0건)하였고,
3. 동일 역할 내의 진정한 싱글톤 중복(동일 문맥 내 복수 code 70)은 여전히 `InvalidTableValue`로 엄격히 차단함을 증명하였습니다.
4. L-01 역시 단일 집계 진단에 첫 번째 미매핑 헤더 변수의 원본 대표 스팬(`Source != null`)을 정확히 전달하도록 수정되었습니다.

솔루션 전체 **293/293 테스트 전원 통과** 및 Release 빌드 검증을 완료하였으며, 통제 DXF 원본 해시 무결성 및 결정론적 직렬화 해시(`2546B5513275D3EC1EEF025DA817249CC3F724A1BA4A986790041C94C3C4DB91`)를 확인하였으므로 최종 승인(**`PHASE_03B_PASS`**)을 부여합니다.

---

## 1. Targeted Re-Review of H-01: DIMSTYLE Code 70 Roles Separation

### 1.1 수정 구현 분석 (`TextDxfStructuralReader.Records.cs`)
Codex는 `DimstyleCode70Roles` 메서드를 도입하여 `inspection.Eligible` 내의 code 70 출현 위치를 문맥 상태 머신으로 추적합니다:
- **`StandardFlags` 역할 윈도우**: `AcDbSymbolTableRecord` 또는 `AcDbDimStyleTableRecord` 서브클래스 문맥 하에서 치수 변수 페이로드가 시작되기 전, code 2(`Name`)가 열고 다음 변수 페이로드(예: code 40 등)가 닫음.
- **`TextFillColor` 역할 윈도우**: 치수 변수 문맥 하에서 code 69(`DIMTFILL`)가 열고 다음 변수 페이로드(예: code 71 `DIMTOL`)가 닫음.
- **제어 그룹 및 XDATA 격리**: `{...}` 브레이스 그룹 및 1001/1002 XDATA 내부의 code 70은 `inspection.Eligible` 단계에서 이미 배제되어 치수 역할로 오인되지 않음.
- **서브클래스 전이 및 외부 서브클래스 차단**: 공인되지 않은 외부 서브클래스(`ForeignSubclass`) 또는 알 수 없는 문맥의 code 70은 즉시 `InvalidTableValue` 에러 진단과 함께 미매핑(`unmapped`)으로 보존됨.

### 1.2 First/Last-Wins 배제 및 진정한 중복 검증
- 각 역할 윈도우 내에서 발견된 모든 code 70 인덱스는 리스트(`flags`, `colors`)로 수집되어 `Fact()`로 전달됩니다.
- 만약 특정 역할 내에서 동일 code 70이 2회 이상 등장할 경우(`indices.Count != 1`):
  - `dimstyle-duplicate-standard-flags` (code 40 이전에 code 70이 "16", "32" 2회 출현) $\rightarrow$ `Flags.Validity = Invalid`, `InvalidTableValue` 에러 발생.
  - `dimstyle-duplicate-text-fill-color` (code 69 직후 code 70이 "7", "8" 2회 출현) $\rightarrow$ `TextFillColor.Validity = Invalid`, `InvalidTableValue` 에러 발생.
- First-value-wins나 Last-value-wins를 사용하지 않고, 모든 출처 인덱스를 보존하면서 싱글톤 위반을 정확히 포착합니다.

### 1.3 통제 픽스처(`DtoB_TEST_2STORY_R2018.dxf`) 전수 검증
실제 통제 픽스처의 12개 DIMSTYLE 엔트리 전수 비교 결과:
- Standard: Flags pair 1706 (value line 3414, 값 0), TextFillColor pair 1727 (value line 3456, 값 0)
- EZDXF: Flags pair 1774, TextFillColor pair 1795 (값 0, 0)
- EZ_M_100_H25_CM ~ EZ_CURVED 등 총 12개 엔트리 모두:
  - `Flags.Presence == Present`, `Flags.Validity == Valid`, `Flags.Integer == 0`
  - `TextFillColor.Presence == Present`, `TextFillColor.Validity == Valid`, `TextFillColor.Integer == 0`
  - `Disposition == DxfDisposition.Supported`
  - `RawHandle`: code 105 유지
  - `LegacyCode5Setting`: code 5 유지

### 1.4 통제 결과 메트릭 변동 확인
| 항목 | H-01 수정 전 | H-01 수정 후 (현재) | 확인 결과 |
|---|:---:|:---:|:---:|
| DIMSTYLE Supported / Invalid | 0 / 12 | **12 / 0** | **정상 복원** |
| 전체 레코드 Supported | 87 | **99** | **정상 복원** |
| 전체 레코드 Invalid | 12 | **0** | **정상 복원** |
| `InvalidTableValue` 에러 진단 | 12 | **0** | **허위 에러 전멸** |
| 전체 진단 수 | 71 | **59** | **12개 허위 진단 제거** |
| Unsupported / DeferredTo03C / DeferredTo03D | 52 / 89 / 93 | **52 / 89 / 93** | **불변 유지** |
| 상태 및 검증 플래그 | Partial / true | **Partial / true** | **불변 유지** |

---

## 2. Targeted Re-Review of L-01: Unmapped HEADER Diagnostic Source

`TextDxfStructuralReader.Header.cs` line 52-54:
```csharp
var unmapped = variables.Count(v => v.Disposition == DxfDisposition.IgnoredWithDiagnostic);
if (unmapped > 0)
    Add(DxfStructuralDiagnosticCode.UnmappedHeaderVariables, DxfStructuralDiagnosticSeverity.Information,
        "Additional HEADER variables retained as raw ranges without typed mapping.",
        variables.First(v => v.Disposition == DxfDisposition.IgnoredWithDiagnostic).Source, unmapped);
```
- 단일 집계 진단(`Count = 245`)을 유지하여 진단 폭증을 방지하면서,
- `Source`가 `null`이 아닌 첫 번째 미매핑 변수(`$ACADMAINTVER`, pair 4..5, physical lines 9..12)의 스팬을 정확히 가리키도록 개선되었습니다.
- 테스트 `UnmappedHeaderAggregateHasFirstRepresentativeSourceWithoutDiagnosticExpansion`을 통해 `diagnostic.Source != null` 및 exact span이 검증되었습니다.

---

## 3. Pair Accounting & Immutability Verification

1. **페어 회계 완전성**:
   - 모든 레코드에서 `Mapped SourcePairIndices + UnmappedPairIndices`의 합집합이 레코드 전체 페어 범위(`StartPairIndex..EndPairIndex`)와 비트 단위로 100% 일치함을 재확인하였습니다.
   - DIMSTYLE의 두 code 70은 각각 `Flags`와 `TextFillColor`의 독립된 `SourcePairIndices`로 정확히 분할 등록되어, 중복되거나 누락된 페어가 존재하지 않습니다 (`flags.Intersect(color) == empty`).
2. **식별자 및 핸들 경계**:
   - DIMSTYLE 객체 식별자는 오직 code 105만 사용됩니다.
   - code 5는 `LegacyCode5Setting`으로 격리 매핑되어 있으며, code 105가 없는 경우 code 5로 대체(fallback)하지 않습니다 (`MissingDimstyle105NeverFallsBackToLegacy5` 통과).
   - HEADER `$HANDSEED`(code 5) 역시 객체 식별자에서 엄격히 배제됩니다.

---

## 4. Regression & Boundary Hygiene Verification

1. **이전 통과 기능 회귀 없음**:
   - SECTION/ENDSEC/EOF 상태 머신 (중첩/누락/순서/주석 등)
   - AC1032 strict 버전 게이트 (누락/중복/타버전 거절)
   - EXTMIN/EXTMAX 역전 범위 원본 보존 및 `UnusableReportedExtents` 플래그
   - LAYER, LTYPE, STYLE, BLOCK_RECORD 테이블 필드 매핑
   - code 330 참조 역할 분기 (Reactor, ExtensionDictionary, XData, Owner)
   - 102/1002 제어 그룹 통합 스택 깊이(16) 가드
   - 지연 레코드(03C 89건, 03D 93건) 및 CLASSES(15건)/OBJECTS(48건) 보존
   - 자원 제한 및 협력적 취소
2. **미구현 경계 격리**:
   - PHASE 03C 형상 변환(LINE, ARC 등), PHASE 03D 블록/INSERT/주석 데이터 모델, PHASE 03E 전역 핸들 그래프, RawDrawingSnapshot, Viewer, PHASE 04 정규화, 좌표계 변환, 단위 환산 코드 전무.

---

## 5. Test Suite Analysis & Build Verification

- **독립 빌드 실행**:
  - `DtoB.Core`: Release 빌드 성공 (경고 0건)
  - `DtoB.sln`: Release 빌드 성공 (기존 인지된 Revit 2025 MSB3277 경고 외 신규 경고 0건)
- **독립 테스트 실행**:
  - 총 테스트 수: **293건** (기존 285건 + 신규 역할 검증 테스트 8건)
  - 통과: **293건**, 실패: **0건**, 건너뜀: **0건**
- **정적 검사 및 무결성**:
  - `git diff --check`: PASS (공백 및 인코딩 오류 없음)
  - 통제 픽스처 SHA-256: `2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC` (불변)
  - 결정론적 구조 JSON SHA-256: `2546B5513275D3EC1EEF025DA817249CC3F724A1BA4A986790041C94C3C4DB91`

---

## 6. Findings Summary

- **BLOCKER**: 0건
- **HIGH**: 0건 (이전 H-01 완벽히 해결됨)
- **MEDIUM**: 0건
- **LOW**: 0건 (이전 L-01 완벽히 해결됨)

---

## 7. Final Decision

```text
PHASE_03B_PASS
```

PHASE 03B (Native Text DXF Structural / Header / Tables Reader)의 모든 설계 및 구현 요구사항이 완벽하게 검증되었습니다.

Codex는 이제 **PHASE 03C (Native Text DXF Entities / Raw Geometry Extraction)**의 설계 및 구현 준비 단계로 진행할 수 있습니다.
