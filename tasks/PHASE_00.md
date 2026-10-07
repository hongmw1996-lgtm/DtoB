# PHASE 00 — Architecture & Technical Foundation

## 1. 목적

DtoB Desktop + Revit Add-in이라는 제품 구조를 확정하고, 이후 개발의 기술 경계와 데이터 계약을 결정한다.

## 2. 전제조건 / 가정

- Windows 11
- Visual Studio 2022
- Revit 2025 계열
- Controlled CAD 우선
- GitHub hongmw1996-lgtm/DtoB

## 3. 이번 Phase 지원 범위

- Desktop Framework 결정
- Analysis Engine 전략
- Revit Add-in 구조
- Desktop↔Revit 통신 PoC
- DWG Reader 후보
- 내부 Unit/Coordinate 규칙
- CAD IR/BIM IR 최소 Schema
- Repository/Test 구조

## 4. 아직 지원하지 않는 범위

- 실제 DWG 의미 분석
- Wall/Door/Window 인식
- 3D BIM Viewer
- Native Revit 모델 생성

## 5. Subphase 상세

### 00A Desktop Framework

C#/.NET/WPF/WebView2를 우선 검토하고 배포성, Viewer 삽입, Revit 생태계와의 일관성을 평가한다.

### 00B Analysis Engine

전체 C# 또는 C# Desktop + Python Analysis Service 구조를 비교한다. AI/ML 확장을 고려하되 MVP에서 불필요한 복잡성은 피한다.

### 00C Revit Architecture

DtoB.Core와 Revit API 프로젝트를 분리한다. 정확한 Revit Build와 Target Framework를 기록한다.

### 00D IPC PoC

Named Pipe/localhost HTTP/gRPC 등 후보를 비교하고 Desktop→PING, Add-in→PONG 수준의 최소 통신을 성공시킨다.

### 00E Unit/Coordinate

Core geometry 단위를 mm로 정의하고 X/Y/Z, rotation, precision, tolerance 정책을 문서화한다.

### 00F CAD IR

CadDocument/CadLayer/CadEntity/CadBlock/CadText/CadDimension 최소 Schema를 정의한다.

### 00G BIM IR

Level/Grid/Wall/Column/Door/Window/Floor/Room 최소 Schema를 Revit 독립적으로 정의한다.

### 00H Repo/Test

apps/packages/services/revit/tests/datasets/docs/tasks/reviews 구조를 확정하고 Solution build/test 명령을 고정한다.

## 6. 사용자에게 보이는 결과

최소 Desktop Shell에서 Revit 연결 상태를 확인할 수 있으면 충분하다.

## 7. 필수 테스트

- Desktop build
- Revit add-in build
- Core가 Revit DLL 없이 build
- IPC PING/PONG
- unit conversion smoke test
- CAD IR/BIM IR serialization

## 8. Exit Criteria

- [ ] Desktop Framework 확정
- [ ] Analysis Engine 전략 확정
- [ ] Revit 구조 확정
- [ ] 통신 PoC 성공
- [ ] Unit/Coordinate 규칙 확정
- [ ] CAD IR/BIM IR 최소 Schema
- [ ] Repository/Test 구조 확정
- [ ] Architecture ADR 작성

## 9. Codex 실행 지시

1. `DtoB_FINAL_MASTER_PLAN.md`, `AGENTS.md`, `ARCHITECTURE.md`를 먼저 읽는다.
2. 이 문서의 PHASE 00 범위만 구현한다.
3. 구현 전 Repository 상태와 기존 테스트를 조사하고 작업 Checklist를 작성한다.
4. Subphase 순서대로 작은 단위로 구현한다.
5. 각 Subphase마다 테스트를 추가한다.
6. UI에서 확인 가능한 결과는 같은 Phase에서 최소 검증 UI를 제공한다.
7. Unsupported Case를 조용히 무시하지 말고 명시적으로 기록한다.
8. 전체 테스트 실행 후 `docs/status/PHASE_00_REPORT.md`를 작성한다.
9. 다음 Phase를 임의로 시작하지 않는다.

## 10. Antigravity Review Focus

- Phase 범위 위반
- CAD Core와 Revit API 경계
- Unit / Coordinate hidden assumption
- Data loss / source trace 손실
- Silent failure
- Test 누락
- Regression 위험
- UI와 Core Logic의 과도한 결합
- 다음 Phase 확장을 방해하는 구조

Review 결과는 `reviews/PHASE_00_REVIEW.md`에 작성한다.

Severity: `BLOCKER / HIGH / MEDIUM / LOW`

## 11. Phase Gate

```text
PASS
PASS_WITH_KNOWN_LIMITATIONS
REVIEW_REQUIRED
BLOCKED
```

다음 Phase는 PASS 또는 합의된 PASS_WITH_KNOWN_LIMITATIONS 이후 시작한다.
