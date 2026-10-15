# PHASE 03A Native Text DXF Lexical Reader Implementation Review

## Executive Summary

- **Review Target**: PHASE 03A — Native Text DXF Lexical / Group-Code Reader (`packages/DtoB.Cad.Raw`, `packages/DtoB.Cad.Dxf`, `tests/DtoB.Tests/TextDxfLexicalReaderTests.cs`)
- **Review Date**: 2026-10-14
- **Reviewer**: Architecture / Adversarial Review Owner (Antigravity)
- **Verdict**: **`PHASE_03A_PASS`**

DtoB의 아키텍처 방향 전환(CAD Data Extraction Foundation 및 Native Text DXF Reader 자체 파서 구현)에 따라, PHASE 03A의 구현 범위는 **순수 어휘(Lexical) 계층**으로 한정되었습니다.

독립적인 검증 결과, Codex가 구현한 `TextDxfLexicalReader`는 스트림 기반 UTF-8 디코딩, 물리적 라인 번호 추적, 그룹 코드 정수 파싱, 값 문자열의 무해석(verbatim) 보존, 엄격한 자원 한계 제어, 호출자 스트림 소유권 보존, 협력적 취소(CancellationToken), 그리고 결정론적 진단(Diagnostic) 시스템을 완벽하게 준수하였습니다.

또한 실제 통제 픽스처(`DtoB_TEST_2STORY_R2018.dxf`, 96,621 바이트)에 대해 .NET BCL 독립 오라클(`File.ReadAllLines`)과의 전수 비교를 통해 17,474행 8,737쌍의 그룹 코드/값/라인 번호 일치 및 원본 해시 무결성을 검증하였으며, 솔루션 전체 190/190 테스트가 100% 통과(Pass)함을 독립 빌드 및 VSTest 실행을 통해 확인하였습니다.

---

## 1. Scope & Boundary Enforcement

PHASE 03A의 범위 경계가 엄격히 준수되었는지 검증하였습니다:

1. **순수 Lexical 계층 분리**:
   - 구현체는 바이트 스트림을 순차적으로 읽어 `(GroupCode, RawCodeText, RawTextValue, CodeLineNumber, ValueLineNumber)` 형태의 `DxfRawPair`만을 생산합니다.
   - CAD 의미론적 해석(HEADER, SECTION, TABLES, BLOCKS, ENTITIES, Handle, Extrusion Normal, OCS/WCS 좌표계 변환, 단위 변환, 도면 계층 구조)은 일체 구현되지 않았으며, 어떠한 엔티티 객체나 `RawDrawingSnapshot`도 생성하지 않습니다.
2. **독립 패키지 구조 및 금지된 의존성 배제**:
   - `DtoB.Cad.Raw`: 순수 .NET 8 런타임 외 일체 의존성 없음 (`<PackageReference>` 전무).
   - `DtoB.Cad.Dxf`: `DtoB.Cad.Raw` 단 하나의 프로젝트 참조만 가지며 외부 패키지 없음.
   - ACadSharp, ODA, RealDWG, netDxf 등 외부 CAD 서드파티 라이브러리 참조 0개 검증 완료 (`FoundationDocumentTests.NativeRawParserForbiddenDependenciesAreRejected` 통과).
   - WPF(`UseWPF`), Revit API, Desktop Shell UI, `DtoB.Project`와의 결합 전무.

---

## 2. Lexical Contract & Immutability Verification

`packages/DtoB.Cad.Raw/DxfLexicalContracts.cs` 및 `packages/DtoB.Cad.Dxf/TextDxfLexicalReader.cs`:

1. **`DxfRawPair` 레코드**:
   - 인덱스는 0-based (`PairIndex`), 라인 번호는 1-based (`CodeLineNumber`, `ValueLineNumber`)로 일관성 있게 정의되었습니다.
   - 값 문자열(`RawTextValue`)은 앞뒤 공백, 0 표기, 대소문자, 부호, 지수 표기법(`1e+20`), 특수 값(`NaN`), DXF 특수 이스케이프(`\U+...`, `\M+...`)를 전혀 수정하지 않고 원본 그대로(verbatim) 보존합니다.
   - 실수/정수/Handle 등으로의 조기 형변환을 시도하지 않고 문자열 계약을 유지합니다.
2. **불변성(Immutability)**:
   - `DxfLexicalReadResult` 내의 `Pairs` 및 `Diagnostics`는 내부 리스트를 `Array.AsReadOnly(...)`를 통해 읽기 전용 컬렉션으로 동결하여 외부에 노출합니다.
   - 외부에서 파싱 결과를 사후 수정하거나 변조할 수 없습니다.

---

## 3. Encoding, BOM, Line-Ending, and Malformed Byte Handling

1. **엄격한 UTF-8 디코딩**:
   - `new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)`를 적용하여 잘못된 바이트 시퀀스나 유효하지 않은 UTF-8 바이트 발견 시 `\uFFFD` 대체 문자로 조용히 대체하지 않고 즉시 `UnsupportedEncoding` (`InvalidUtf8`) 진단을 기록합니다.
   - 한글(KS X 1001), 라틴 악센트 문자, 이모지 등 다국어 UTF-8 시퀀스가 정상 처리됨을 확인하였습니다.
2. **BOM(Byte Order Mark) 처리**:
   - 입력 스트림의 Byte 0 위치에 등장하는 표준 UTF-8 BOM(`0xEF, 0xBB, 0xBF`)만 유효한 프리앰블로 인정하고 물리적 라인 바이트 수 계산에서 안전하게 배제합니다.
   - 바이트 0이 아닌 본문 중간에 나타나는 `\uFEFF` 문자는 `UnexpectedBom` 오류로 거절합니다.
   - UTF-16 LE/BE, UTF-32 LE/BE 시그니처(`0xFF 0xFE ...`, `0x00 0x00 0xFE 0xFF ...`)는 `UnsupportedBom` 진단과 함께 즉시 명시적 실패 처리됩니다.
3. **개행(Line-Ending) 처리**:
   - CRLF(`\r\n`)와 LF(`\n`)를 모두 지원하며, 두 형식이 혼재된 경우 `DxfLineEndingKind.Mixed`로 상태를 추적합니다.
   - 고립된 CR(`\r`)은 지원되지 않는 개행(`InvalidLineEnding`)으로 즉시 거절합니다.
   - 파일 끝(EOF)에 개행이 없는 마지막 값 라인도 누락 없이 수용하며, 파일 끝에 정상 개행이 있는 경우 유령 빈 라인을 생성하지 않습니다.
4. **시그니처 감지 및 이진 파일 배제**:
   - DWG 시그니처(`AC10...`) 및 Binary DXF 시그니처(`AutoCAD Binary DXF\r\n\x1a\0`)는 최대 22바이트의 안전 프리페치를 통해 사전 감지되어 `UnsupportedInput`으로 즉시 거절됩니다.
5. **그룹 코드 무결성 및 인덱스 건너뛰기**:
   - 그룹 코드는 불변 문화권(`CultureInfo.InvariantCulture`) 기반 `int.TryParse`로 파싱되며, 0..1071 범위를 엄격히 검증합니다.
   - 부동소수점(`1.0`), 오버플로, 음수, 비숫자 문자는 `InvalidGroupCode` 또는 `GroupCodeOutOfRange` 진단을 생성합니다.
   - 그룹 코드 파싱 실패 시, `pairIndex`는 증가시키되 `Pairs` 결과 컬렉션에는 잘못된 쌍을 추가하지 않아 인덱스 공백(Gap)을 남기며, 뒤따르는 페어 구조가 깨지지 않도록 격리합니다.
   - 홀수 개의 물리 라인(`OddPhysicalLineCount`)이나 빈 입력(`EmptyInput`)도 정확히 탐지됩니다.

---

## 4. Stream Ownership & Resource Safety

1. **호출자 스트림 소유권**:
   - 리더는 호출자가 제공한 `Stream`을 소유하지 않으며, `Dispose()`나 `Close()`를 호출하지 않습니다.
   - 스트림을 되감거나(`Seek(0)`), 임의의 위치로 이동하거나, 쓰기를 수행하지 않습니다. 호출자의 현재 위치에서 읽기를 시작합니다.
2. **제한된 버퍼링 및 자원 가드 (`DxfReaderLimits`)**:
   - `MaxInputBytes` (기본 16MB): Seekable 스트림의 경우 길이 사전 검사 수행, Non-seekable 스트림의 경우 읽기 루프에서 `remaining + 1` 바이트를 요구하여 무제한 선행 버퍼링 없이 즉시 `InputTooLarge`를 반환합니다.
   - `MaxLineBytes` (기본 64KB): 물리적 라인 버퍼에 추가하기 전에 UTF-8 바이트 수를 엄격히 측정하여 한도 초과 시 `LineTooLong`으로 거절합니다.
   - `MaxPairs` (기본 250,000): 페어 생성 전 한도를 검사하여 초과 시 `PairLimitExceeded` 발생.
   - `MaxDiagnostics` (기본 5,000): 4,999개의 일반 진단 슬롯과 1개의 예약된 터미널 진단 슬롯(Reserved Terminal Slot)을 두어, 진단 한도 초과 시 `DiagnosticLimitExceeded`를 누락 없이 기록합니다.
   - `MaxControlNesting` (기본 16): 구성 파라미터로 검증되나, 03A에서는 제어 그룹 스택을 해석하지 않고 03B 구조 계층으로 위임함을 명시했습니다.
3. **비동기 협력적 취소**:
   - 스트림 읽기 전/후, 바이트/라인 처리 루프, EOF 도달 시점, 결과 빌드 직전/직후에 `cancellationToken.ThrowIfCancellationRequested()` 및 취소 감지를 철저히 수행하여, 취소 시 `DxfLexicalStatus.Canceled`를 반환하고 부분 성공 결과를 오발행하지 않습니다.

---

## 5. Real Fixture QA vs Independent Oracle

- **대상 픽스처**: `datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf` (96,621 바이트)
- **무결성 해시 확인**:
  - 원본 SHA-256: `2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC`
  - 리더 실행 후 원본 해시: `2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC` (완전 일치, 무변조)
- **독립 BCL 오라클 검증**:
  - `System.IO.File.ReadAllLines` 기반 독립 오라클 측정치: 17,474 물리 라인 / 8,737 그룹 코드 페어.
  - `TextDxfLexicalReader` 결과: 정확히 8,737개 순서 보장 페어 생성.
  - 모든 페어의 `GroupCode`, `RawCodeText`, `RawTextValue`, `CodeLineNumber`, `ValueLineNumber`가 독립 오라클과 100% 비트 단위로 완벽하게 일치함을 검증 완료하였습니다.

---

## 6. Version Boundary Discipline (NotValidated)

- `DxfLexicalReadResult.VersionAcceptance`는 어휘적 성공 여부와 무관하게 **항상 `DxfVersionAcceptance.NotValidated`**로 설정됩니다.
- 본 리더는 `$ACADVER` 헤더 변수를 읽거나 AC1032(AutoCAD 2018) 버전을 승인하지 않으며, SECTION/EOF 등의 파일 구조적 완결성을 검증하지 않습니다.
- 따라서 상위 계층이나 호출자가 PHASE 03A의 결과만으로 "지원되는 DXF"라고 오판하는 것을 원천 차단하였으며, 버전 판정의 책무를 PHASE 03B로 정확히 위임하였습니다.

---

## 7. Test Suite Analysis & Build Verification

- **독립 빌드 실행**:
  - `DtoB.Core`: 경고 없이 빌드 성공 (Release)
  - `DtoB.sln`: 빌드 성공 (기존 인지된 Revit 2025 종속성 어셈블리 경고 MSB3277 3건 외 신규 경고 0건)
- **독립 테스트 실행**:
  - 총 테스트 수: **190건**
  - 통과: **190건**, 실패: **0건**, 건너뜀: **0건**
  - 신규 어휘 테스트: 55건 (`TextDxfLexicalReaderTests`)
  - 신규 의존성 및 아키텍처 규칙 테스트: 9건 (`FoundationDocumentTests`)
  - 기존 회귀 테스트 (PHASE 00/01/02): 126건 전원 통과

---

## 8. Findings

### BLOCKER
- 없음 (None)

### HIGH
- 없음 (None)

### MEDIUM
- 없음 (None)

### LOW
- **L-01: 그룹 코드 행의 선행 부호 수용 동작에 대한 명시적 인지**
  - **File / Section**: `packages/DtoB.Cad.Dxf/TextDxfLexicalReader.cs` (`TryParseGroupCode`)
  - **Problem**: 그룹 코드 파싱 시 `NumberStyles.AllowLeadingSign`이 지정되어 있어 `+0`, `+10`과 같은 선행 `+` 부호가 정수로 파싱됩니다 (음수는 범위 검사 `0..1071`에 의해 차단됨).
  - **Impact**: 표준 DXF 생성기에서는 `+` 부호를 붙이지 않으나, 비표준 DXF에서 `+10`이 들어올 경우 그룹 코드 10으로 파싱됩니다. 이는 어휘 수준에서는 허용 범위 내이며 파괴적이지 않습니다.
  - **Recommendation**: PHASE 03B 또는 향후 엄격 검증 모드 도입 시, 원본 코드 텍스트(`RawCodeText`)에 숫자가 아닌 부호 문자가 포함되어 있는지를 엄격 검증할 수 있는 진단 옵션을 고려할 수 있습니다.
  - **Required Test**: 현재의 `RawCodeText` 보존 계약으로 충분히 추적 가능하므로 03A의 변경은 불필요합니다.

- **L-02: Non-seekable 스트림에 대한 버퍼 선행 읽기 오버런의 호출자 인지**
  - **File / Section**: `packages/DtoB.Cad.Dxf/TextDxfLexicalReader.cs` (`BufferedInput`)
  - **Problem**: 4096 바이트 고정 청크로 스트림을 읽으므로, 파싱이 에러(예: InvalidUtf8, GroupCodeOutOfRange)로 조기 중단될 때 스트림 내부 포인터는 에러 발생 물리 라인의 바이트보다 앞선 위치를 가리키게 됩니다.
  - **Impact**: 호출자가 실패 후 동일 스트림에서 후속 읽기를 수행하려 할 경우 스트림 오프셋이 파싱 중단 위치와 일치하지 않습니다.
  - **Recommendation**: 호출자가 실패 후 복구를 원할 경우 스트림을 재오픈하거나 되감아야 함을 API 문서에 명시해야 합니다. (현재 보고서 및 테스트에 호출자 스트림 소유권 원칙으로 이미 문서화됨).
  - **Required Test**: `CallerStreamIsReadOnlyAndNeverDisposedOrWritten`에 의해 소유권 계약이 명확히 검증되어 있습니다.

---

## 9. Final Decision

**`PHASE_03A_PASS`**

PHASE 03A (Native Text DXF Lexical Reader)는 합의된 모든 사양, 경계 제약, 인코딩 규칙, 오라클 정합성, 자원 보호 가드를 만족스럽게 구현하였습니다.

Codex는 이제 **PHASE 03B (Structural & Section DXF Reader)**의 설계 및 구현 준비 단계로 진행할 수 있습니다.
