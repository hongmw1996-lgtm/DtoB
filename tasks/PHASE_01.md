# PHASE 01 — DtoB Desktop Shell

## 1. 목적

실제로 실행하고 프로젝트를 만들고 저장할 수 있는 DtoB Desktop 기본 프로그램을 만든다.

## 2. 전제조건 / 가정

- PHASE 00 PASS
- Desktop Framework 확정
- Configuration/Logging 정책 존재

## 3. 이번 Phase 지원 범위

- DtoB.exe 실행
- New/Open/Save/Save As
- Recent Projects
- Settings
- Logging
- 기본 Error Dialog
- Project persistence

## 4. 아직 지원하지 않는 범위

- DWG Upload
- CAD Viewer
- 객체 분석
- Revit BIM 생성

## 5. Subphase 상세

### 01A Bootstrap

App startup, dependency/config/log 초기화, fatal error handler를 만든다.

### 01B Main Window

New Project, Open Project, Recent Projects를 제공하는 시작 화면을 만든다.

### 01C Project Model

ProjectId, Name, Path, CreatedAt, ModifiedAt, DtoBVersion을 정의한다.

### 01D Persistence

JSON 또는 .dtob 파일로 프로젝트를 저장/복구하고 schemaVersion 필드를 둔다.

### 01E Settings

Working Directory, Cache, Revit Target, Log Level을 저장한다.

### 01F Recent

최근 프로젝트를 표시하고 경로가 없어진 경우 오류 상태를 보여준다.

## 6. 사용자에게 보이는 결과

DtoB 실행 → New Project → Save → 종료 → 재실행 → Recent Project 열기.

## 7. 필수 테스트

- Project create/load/save
- invalid project file
- missing project path
- settings persistence
- recent list
- crash log

## 8. Exit Criteria

- [ ] DtoB.exe 실행
- [ ] 프로젝트 생성/저장/열기
- [ ] 재실행 후 복구
- [ ] Settings 저장
- [ ] 오류 logging

## 9. Codex 실행 지시

1. `DtoB_FINAL_MASTER_PLAN.md`, `AGENTS.md`, `ARCHITECTURE.md`를 먼저 읽는다.
2. 이 문서의 PHASE 01 범위만 구현한다.
3. 구현 전 Repository 상태와 기존 테스트를 조사하고 작업 Checklist를 작성한다.
4. Subphase 순서대로 작은 단위로 구현한다.
5. 각 Subphase마다 테스트를 추가한다.
6. UI에서 확인 가능한 결과는 같은 Phase에서 최소 검증 UI를 제공한다.
7. Unsupported Case를 조용히 무시하지 말고 명시적으로 기록한다.
8. 전체 테스트 실행 후 `docs/status/PHASE_01_REPORT.md`를 작성한다.
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

Review 결과는 `reviews/PHASE_01_REVIEW.md`에 작성한다.

Severity: `BLOCKER / HIGH / MEDIUM / LOW`

## 11. Phase Gate

```text
PASS
PASS_WITH_KNOWN_LIMITATIONS
REVIEW_REQUIRED
BLOCKED
```

다음 Phase는 PASS 또는 합의된 PASS_WITH_KNOWN_LIMITATIONS 이후 시작한다.
