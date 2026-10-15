# PHASE 03H Minimal Raw DXF Diagnostic Viewer Independent Final Review

## Executive Summary

- **Review Target**: PHASE 03H — Minimal Raw DXF Diagnostic Viewer (`apps/DtoB.Desktop/Diagnostics/`, `apps/DtoB.Desktop/DiagnosticViewerWindow.xaml*`, `docs/adr/ADR-017-Reader-Lifecycle.md`, `tests/DtoB.Tests/RawDiagnosticViewerTests.cs`, `scripts/qa/Verify-Phase03HBoundary.ps1`, `docs/status/PHASE_03H_REPORT.md`, `artifacts/phase03/03h-viewer-20261015/`)
- **Review Date**: 2026-10-15
- **Review Mode**: Independent Architecture & Host Behavior Implementation Review
- **Reviewer**: Architecture / Adversarial Review Owner (Antigravity)
- **Status of Codex**: `PASS_WITH_KNOWN_LIMITATIONS`
- **Final Verdict**: **`PHASE_03H_PASS`**

DtoB 시스템의 텍스트 DXF 파이프라인(Phase 03A~03G) 결과물인 `RawDrawingSnapshot`을 사용자 및 개발자 관점에서 시각적으로 진단·검증할 수 있는 진단 전용 뷰어(**PHASE 03H Minimal Raw DXF Diagnostic Viewer**)에 대해 엄격하고 독립적인 아키텍처 및 런타임 호스트 감사를 수행하였습니다.

PHASE 03H는 Phase 04의 기하 정규화나 CAD IR 생성으로 넘어가는 다리가 아니며, 기존에 완성된 불변의 `RawDrawingSnapshot`과 원본 DXF 레코드 팩트를 **있는 그대로(as-is)** 화면에 투영하고 원본 소스 줄/페어 위치까지 역추적하는 **순수 진단(Diagnostic-only) 프레젠테이션 계층**입니다.

독립 감사 결과:

1. **ADR-017 Reader Lifecycle 아키텍처 공식 승인**:
   - `AssetPaths.Resolve()`를 통한 관리 파일 경로 접근 및 TOCTOU(Time-of-Check to Time-of-Use) 레이스를 원천 차단하는 단일 스트림 임대(`FileAccess.Read`, `FileShare.Read`) 생명주기를 정립하였습니다.
   - 단일 임대 스트림 내에서 렉시컬 파일 크기 상한 검사 $\rightarrow$ 등록된 SHA-256 해시 검증 $\rightarrow$ `stream.Position = 0` 되감기(Rewind) $\rightarrow$ 순수 파이프라인(03A~03F) 실행으로 이어지는 흐름이 안전하게 작동함을 확인하였습니다.
   - 일반 프로젝트 열기나 새로고침 시 불필요한 자동 파싱을 방지하고, 명시적인 진단 뷰어 실행 요청 시에만 백그라운드 워커(`Task.Run`)를 통해 파이프라인을 격리 실행하도록 설계되었습니다.
2. **프로덕션 파서/계약 0바이트 불변 (Zero Production Modification in Raw/Dxf)**:
   - `packages/DtoB.Cad.Raw` 및 `packages/DtoB.Cad.Dxf` 내 모든 소스 파일 및 프로젝트 파일은 Phase 03F/03G 기준선과 비교하여 **단 1바이트의 수정도 없이 100% 동일**합니다.
   - 새로운 뷰어 기능을 지원하기 위해 파서 코드를 왜곡하거나, 파서 내부에 UI/WPF/렌더링 종속성을 추가하는 역류 현상이 전혀 발생하지 않았습니다.
3. **엄격한 단방향 의존성 및 경계 보존**:
   - 승인된 의존성 흐름인 `DtoB.Desktop` $\rightarrow$ `DtoB.Cad.Dxf` $\rightarrow$ `DtoB.Cad.Raw`를 엄격히 준수합니다.
   - `FoundationDocumentTests`의 허용 목록(Allowlist)은 오직 Desktop에서 Raw/Dxf로의 참조만을 허용하도록 최소한으로 조정되었으며, 역방향 참조나 Revit/WPF의 하위 계층 침범 금지 테스트는 완벽히 통과하였습니다.
   - `RawDiagnosticScene`은 일회성/표현 전용 모델로서 CAD IR, 프로젝트 스키마, 또는 Phase 04의 입력으로 오용되지 않도록 완벽히 격리되었습니다.
4. **ENTITIES vs BLOCK Definition 공간의 엄격한 분리**:
   - `ENTITIES` 모드는 오직 `SourceSection == "ENTITIES"`인 레코드만을 정확히 필터링하여 투영합니다. 통제 도면 기준 57개 직접 원시 기하(LINE 45 + LWPOLYLINE 12) + 17개 INSERT 앵커 + 36개 TEXT + 4개 DIMENSION 정의점 레코드 등 총 114개 진단 레코드만이 표시되며, `BLOCKS` 섹션의 기하가 모델 공간에 누출되지 않습니다.
   - `Block Definition` 모드는 선택된 블록 정의의 로컬 좌표계(XY)만을 투영합니다. 원점 베이스 마커는 좌표 변환을 가하지 않고 블록의 기준점 팩트만을 시각화하며, 블록 내부의 중첩 INSERT 역시 단일 앵커로만 표시되고 재귀적 전개를 일절 수행하지 않습니다.
5. **Phase 04 기하 정규화/의미 해석의 완벽한 배제 (Zero Leakage)**:
   - 단위 변환(mm/m), 라디안 각도 변환, OCS $\rightarrow$ WCS 법선 벡터 변환, WCS 배치 행렬 적용, MINSERT 격자 전개, ByLayer/ByBlock 스타일 전파, CAD IR 합성, SHX 폰트 렌더링 등이 철저하게 배제되었습니다.
   - 폴리라인의 Bulge는 호(Arc)로 보간하지 않고 직선 현(Chord)으로만 렌더링하며, 인스펙터에 명시적인 진단 안내를 출력합니다.
   - 치수(DIMENSION) 역시 치수선/치수 텍스트를 임의 생성하지 않고 오직 원본 정의점(Definition Point)만을 정직하게 표시합니다.
6. **신뢰할 수 있는 소스 팩트 기반 Fit Bounds**:
   - 도면 헤더의 왜곡되거나 누락되기 쉬운 `EXTMIN`/`EXTMAX`를 전적으로 배제하고, 현재 선택된 뷰 모드의 유효한 기하/앵커 좌표 팩트로부터 직접 바운딩 박스를 계산합니다.
   - WPF 레이아웃 완료 후 실제 뷰포트 크기를 기반으로 `Fit()`을 계산하며, 무한대/오버플로 원형(Circle) 기하에 대한 경계 보호가 적용되어 있습니다.
7. **양방향 선택 및 완전한 소스 역추적성 (Source Traceability)**:
   - 캔버스 클릭 히트 테스트와 333개 전체 레코드 목록 간의 양방향 연동이 원활하게 동작합니다.
   - 인스펙터는 단순 속성 요약에 그치지 않고, 선택된 레코드의 물리적 시작/끝 행, 바이트 범위, DXF Group Code 페어 인덱스, 원본 코드 철자, 원시 문자열 값까지 완벽하게 역추적(Full Source-Range Trace)할 수 있는 증거를 제공합니다.
   - 화면에 표시되지 않는 52개의 `Unsupported` 레코드(VPORT 1 + APPID 3 + OBJECTS 48)도 전체 레코드 목록과 인스펙터를 통해 정직하게 확인 가능합니다.
8. **스레드 취소 및 Stale 결과 격리 안전성**:
   - `DiagnosticRequestGate`를 통해 요청 에포크(Epoch)와 프로젝트 ID, 매니페스트 경로, 도면 ID, 리비전 ID, 파일 해시를 비교하여, 도면 변경이나 창 닫기 시 지연된 비동기 작업 결과가 UI를 오염시키는 문제를 완벽하게 방지하였습니다.
9. **DWG 파일 거부 및 안내 정책**:
   - `.dwg` 파일에 대해서는 스트림을 열기 전에 조기 거부하고, 네이티브 텍스트 DXF 내보내기 지침을 명확히 안내합니다.
10. **테스트 스위트 무결성 (773 / 773 PASS)**:
    - 신규 추가된 37건의 뷰어 단위/통합 테스트를 포함하여 총 773건의 테스트가 전원 통과(Fail 0, Skip 0)하였습니다.
    - 통제 도면 및 스냅샷 해시 불변, 3대 문화권(Invariant, fr-FR, ko-KR)에서의 씬 모델 디제스트 SHA-256 해시 일치(`9D179A0EFAC5EE803E8AEC2AC3C48E1AB0986F1C6159BCB1764E8071933963A7`)가 검증되었습니다.
11. **WPF 런타임 호스트 실제 동작 확인**:
    - Release 빌드 `DtoB.exe` 호스트를 실행하여 통제 프로젝트 및 통제 DXF 도면 로드, 114개 엔티티 뷰 투영, 줌/팬/맞춤/리셋 캔버스 조작, LINE/INSERT/COLUMN_400/BasePoint/Unsupported 항목 선택 및 인스펙터 소스 추적, DWG 거부 안내, 비활성화/컨텍스트 전환 시의 상태 정리 등이 완벽하게 동작함을 실제 화면 스모크와 접근성 트레이스를 통해 확인하였습니다.

모든 아키텍처 및 런타임 검증 기준을 완벽하게 충족하였으므로 최종 승인 판정(**`PHASE_03H_PASS`**)을 부여합니다.

---

## 1. Independent Verification & Test Execution Evidence

- **Release 솔루션 빌드**: `MSBuild.exe DtoB.sln -restore -t:Rebuild -p:Configuration=Release -p:RevitInstallDir="D:\프로그램\Revit 2025" -p:MSBuildEnableWorkloadResolver=false`
  - 결과: **0 Error(s)**, 3 Warning(s) (기존 허용된 Revit 런타임 어셈블리 바인딩 경고 MSB3277 외 신규 경고 0건)
- **전체 단위/통합/회귀 테스트**: **773개 테스트 중 773개 전원 통과 (Pass 773, Failed 0, Skipped 0)**
  - `RawDiagnosticViewerTests`: **37개 신규 03H 테스트 전원 통과**
    - 안전한 관리 자산 해석 및 단일 임대 스트림 유지/되감기 검증
    - 동시 쓰기 잠금(`FileShare.Read`) 및 임대 해제 후 안전한 재접근 검증
    - 원본 소스 경로 / UNC 경로 접근 차단 검증
    - DWG 파일 스트림 오픈 전 거부 및 사용자 지침 검증
    - 입력 크기 상한(MaxRawDxfInputBytes) 초과 시 해싱 전 조기 거부 검증
    - 등록된 해시 불일치 시 03A 진입 전 조기 거부 검증
    - 렉시컬/구조/그래프 실패 및 손상 입력 처리 검증
    - 비소유자 미해소 포인터(LOW-01) 시 스냅샷 null 거부 및 진단 보고 검증
    - 취소 토큰 반응성 (사전 취소 및 파이프라인 중간 취소) 검증
    - 통제 소스 `ENTITIES` 인벤토리 일치 검증 (114 진단 레코드: LINE 45 + LWP 12 + INSERT 17 + TEXT 36 + DIMENSION 4)
    - 통제 소스 12개 블록 정의 및 44개 자식 순서 보존 검증
    - 블록 정의 비영(Non-zero) 베이스 포인트 오프셋 비적용 팩트 보존 검증
    - 중첩 INSERT 단일 앵커 표시 및 재귀 전개 배제 검증
    - 6대 원시 기하 프리미티브 렌더링 팩트 검증
    - Closed 플래그 및 Bulge 직선 현(Chord) 렌더링 지침 검증
    - 유효 기하 부재 시 합성 바운즈 미생성 및 오버플로 원형 경계 안전장치 검증
    - 카메라 줌/팬/맞춤/리셋 결정론 및 스냅샷 불변성 검증
    - 5대 소스 역추적 경로(LINE, LWP, INSERT, TEXT, DIMENSION) 정합성 검증
    - 인스펙터 Group Code 페어 인덱스, 물리 행, 원시 텍스트 매핑 검증
    - 비지원 엔티티(Unsupported) 52개 레코드의 정직한 인스펙터 보존 검증
    - 다중 문화권(Invariant, fr-FR, ko-KR) 씬 모델 디제스트 SHA-256 일치 검증
    - 에포크 및 컨텍스트 변경 시 Stale 결과 UI 반영 차단 검증
  - 선행 테스트 스위트 (736건 전원 통과 유지):
    - `ExtractionQaCampaignTests`: 178건 통과
    - `RawDrawingSnapshotAssemblerTests`: 36건 통과
    - `TextDxfIdentityGraphReaderTests`: 88건 통과
    - `TextDxfBlockAnnotationReaderTests`: 78건 통과
    - `TextDxfGeometryReaderTests`: 63건 통과
    - `TextDxfStructuralReaderTests`: 103건 통과
    - `TextDxfLexicalReaderTests`: 55건 통과
    - 아키텍처 및 종속성 불변 테스트: 9건 통과
    - Phase 00~02 회귀 테스트: 126건 통과
- **경계 감사 스크립트 실행**: `powershell -ExecutionPolicy Bypass -File scripts/qa/Verify-Phase03HBoundary.ps1`
  - 결과: **PASS**
  - Desktop $\rightarrow$ Raw/Dxf 외 역참조 없음, `DtoB.Cad.Raw` 및 `DtoB.Cad.Dxf` 내 WPF/UI 종속성 0건 확인
- **불변 해시 검증**:
  - 통제 도면 원본 DXF (`datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf`):
    `2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC` (동일)
  - 통제 도면 스냅샷 직렬화 해시:
    `4B91C83791F89DEC9BDC633C832001F2D1E537E7F0330BEE0666067CAE9A310C` (동일)
  - 씬 모델 결정론 직렬화 해시 (Invariant, fr-FR, ko-KR 전 문화권 공통):
    `9D179A0EFAC5EE803E8AEC2AC3C48E1AB0986F1C6159BCB1764E8071933963A7` (동일)

---

## 2. Detailed Architecture & Implementation Findings

### 2.1 ADR-017 Reader Lifecycle & Managed Asset Boundary
- **검토 내용**:
  - `ADR-017-Reader-Lifecycle.md`는 DXF 리더의 실행 생명주기와 파일 접근 보안 정책을 명문화하였습니다.
  - 일반적인 프로젝트 로드나 파일 목록 새로고침 시 무거운 DXF 파싱이 유발되지 않도록 메타데이터 전용 모드를 유지합니다.
  - 진단 뷰어 실행 시에는 `AssetPaths.Resolve()`를 통해 관리 디렉터리 내의 통제된 경로만을 확인하며, 임의의 소스 경로나 네트워크 UNC 경로로의 우회 접근을 원천 차단합니다.
  - 파일 스트림 오픈 시 `FileAccess.Read`, `FileShare.Read`를 지정하여 읽기 작업 중 외부의 임의 수정을 차단하며, 크기 상한 검사 $\rightarrow$ SHA-256 검증 $\rightarrow$ 스트림 되감기(`Position = 0`) $\rightarrow$ 렉시컬 리더 전달을 단일 임대 내에서 원자적으로 수행합니다.
- **평가**: **승인 (Approved)**. TOCTOU 보안 결함과 무분별한 파일 I/O를 방지하는 매우 견고한 설계입니다.

### 2.2 Dependency Structure & Zero-Mutation Boundary
- **검토 내용**:
  - `packages/DtoB.Cad.Raw` 및 `packages/DtoB.Cad.Dxf` 패키지의 소스 코드가 단 1바이트도 수정되지 않았습니다.
  - 뷰어 기능 구현에 필요한 모든 진단 씬 모델, 뷰포트 렌더링, 인스펙터, UI 컨트롤은 `apps/DtoB.Desktop` 내부의 `Diagnostics/` 네임스페이스에만 격리되어 구현되었습니다.
  - `DtoB.Tests` 프로젝트에서 프레젠테이션 로직을 테스트하기 위해 순수 로직 파일들(`DiagnosticExtraction.cs`, `RawDiagnosticScene.cs`)을 링크 참조(Linked Source) 방식으로 포함하였으며, WPF 런타임 종속 클래스(`DiagnosticViewport.cs`, `DiagnosticViewerWindow.xaml.cs`)는 데스크톱에만 남겨두어 테스트 프로젝트의 순수성을 보존하였습니다.
- **평가**: **완벽 준수 (Clean & Zero Leakage)**.

### 2.3 Spatial Mode Separation (ENTITIES vs Block Definition)
- **검토 내용**:
  - `RawDiagnosticViewMode.Entities`는 정확히 `SourceSection == "ENTITIES"` 레코드만을 추출하여 투영합니다. 블록 정의 내부의 선, 호, 폴리라인 등의 자식 엔티티는 모델 공간에 절대 나타나지 않으며, 오직 `INSERT` 심볼 앵커만이 지정된 삽입점에 표시됩니다.
  - `RawDiagnosticViewMode.BlockDefinition`은 콤보박스에서 선택된 단일 블록 정의의 로컬 좌표계(Definition-local XY)만을 투영합니다. 블록의 베이스 포인트는 원점 오프셋으로 차감되거나 보정되지 않고 독립적인 베이스 마커(Base Marker)로 정직하게 표시됩니다. 또한 블록 내부의 중첩 INSERT 역시 인스턴스 전개나 재귀 없이 단일 앵커로 유지됩니다.
- **평가**: **완벽 준수 (Accurate Domain Modeling)**.

### 2.4 Diagnostic Primitives & Phase 04 Boundary
- **검토 내용**:
  - 화면에 표시되는 기하는 LINE, POINT, SOLID, CIRCLE, ARC, LWPOLYLINE의 6가지 순수 원시 형태뿐입니다.
  - 단위 환산(예: mm 단위를 Revit 내부 피트로 변경), 좌표계 변환, OCS 투영, 각도 단위 정규화 등 Phase 04의 영역이 전혀 개입하지 않았습니다.
  - 폴리라인의 Bulge는 곡선으로 과대 해석하지 않고 직선 현으로만 표시하며 인스펙터에 비해석 상태를 정직하게 통보합니다.
  - 치수(DIMENSION)와 텍스트(TEXT/MTEXT)는 기하학적 폰트 외형이나 치수 보조선을 임의로 생성하지 않고, 원본 정의점과 앵커 위치만을 정직하게 시각화합니다.
- **평가**: **완벽 준수 (Zero Phase 04 Leakage)**.

### 2.5 Fit Bounds Calculation
- **검토 내용**:
  - CAD 파일의 `HEADER` 섹션에 기록된 `EXTMIN`/`EXTMAX`는 도면 편집 과정에서 갱신되지 않거나 왜곡된 경우가 많아, 이를 경계 상자 계산에 사용하지 않았습니다.
  - 현재 활성화된 뷰 모드 내 유효한 XY 좌표값과 원형(Center $\pm$ Radius) 팩트로부터 직접 유효 바운즈를 도출합니다.
  - 기하가 없거나 유효 좌표가 없는 경우 임의의 가상 바운즈를 만들지 않고 빈 상태(Empty State)로 처리하며, 원형 오버플로 가드가 적용되어 있습니다.
  - WPF 레이아웃 패스 완료 후(`DispatcherPriority.Loaded`) 실제 측정된 뷰포트 크기를 기반으로 `Fit()`을 계산하여 도면이 잘리거나 여백이 왜곡되는 문제를 방지하였습니다.
- **평가**: **견고함 (Robust & Reliable)**.

### 2.6 Full Source Traceability & Unsupported Handling
- **검토 내용**:
  - 화면 상의 엔티티를 클릭하거나 전체 레코드 목록에서 항목을 선택하면, 인스펙터에 해당 레코드의 인덱스, 섹션, 타입, 원본 핸들, 소유권 관계뿐만 아니라 정확한 바이트 오프셋, 라인 번호, Group Code 페어 인덱스, 원본 DXF 텍스트가 표시됩니다.
  - 화면에 렌더링되지 않는 52개의 `Unsupported` 레코드(VPORT 1, APPID 3, OBJECTS 48)도 전체 레코드 목록에 정직하게 나열되며, 선택 시 원본 페어와 비지원 사유를 정확하게 확인할 수 있습니다.
- **평가**: **완벽 준수 (Full Transparency & Source Identity)**.

### 2.7 Threading, Cancellation, & Epoch Gate
- **검토 내용**:
  - 무거운 파일 읽기 및 파이프라인 조립은 UI 스레드를 차단하지 않고 `Task.Run` 백그라운드 워커에서 실행됩니다.
  - 창을 닫거나 사용자가 취소 버튼을 누르면 `CancellationToken`을 통해 비동기 작업이 즉시 취소됩니다.
  - 다중 작업 요청 시 발생할 수 있는 레이스 컨디션을 방지하기 위해 `DiagnosticRequestGate`를 도입하여, 현재 실행 요청의 고유 에포크(Epoch) 및 도면 식별 정보가 일치하지 않는 지연 완료 결과(Stale Result)는 성공/실패 여부를 불문하고 UI에 반영되지 않고 조용히 폐기됩니다.
- **평가**: **스레드 안전성 확보 (High Concurrency Integrity)**.

### 2.8 WPF Host Verification
- **검토 내용**:
  - 실제 Release 빌드 바이너리(`DtoB.exe`)를 실행하여 통제 프로젝트 및 통제 DXF 도면 로드, 114개 진단 엔티티 투영, 줌/팬/맞춤/리셋 캔버스 조작, LINE/INSERT/COLUMN_400/BasePoint/Unsupported 항목 선택 및 인스펙터 소스 추적, DWG 거부 안내, 비활성화/컨텍스트 전환 시의 상태 정리 등이 완벽하게 동작함을 확인하였습니다.
  - 관련 스크린샷과 접근성 트레이스가 `artifacts/phase03/03h-viewer-20261015/manual/`에 정밀하게 보존되어 있습니다.
- **평가**: **호스트 검증 완료 (Production-ready UX)**.

---

## 3. Findings & Observations

### Defect Findings
- **BLOCKER**: 0건
- **HIGH**: 0건
- **MEDIUM**: 0건
- **LOW**: 1건 (기존 정책 동결)
  - `LOW-01 (Policy)`: 비소유자 미해소 포인터(Unresolved Non-Owner Pointers)에 대해 03F 조립 단계에서 엄격하게 `Snapshot == null` 거부를 유지하는 정책입니다. 뷰어에서는 이러한 실패 상태가 구조화된 진단 오류 화면으로 명확하게 노출되며, 충돌 없이 안전하게 거부됨을 확인하였습니다. 본 동작은 `KNOWN_V1_POLICY`로 계속 유지됩니다.

---

## 4. Final Verdict

```text
================================================================================
FINAL VERDICT: PHASE_03H_PASS
================================================================================
```

PHASE 03H의 진단 뷰어 구현은 ADR-017 아키텍처 결정을 충실히 이행하였으며, 하위 파서 패키지의 무결성을 100% 보존하면서도 안전하고 신뢰할 수 있는 소스 역추적 진단 환경을 성공적으로 완성하였습니다. 이에 따라 **PHASE_03H_PASS**를 선언합니다.

*(주의: 본 판정은 PHASE 03H 단일 기능에 대한 최종 승인이며, 전체 PHASE 03 종합 게이트 통과나 PHASE 04 시작을 의미하지 않습니다. 사용자의 명시적 승인 후 다음 단계로 진행하십시오.)*
