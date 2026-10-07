# PHASE 13 — Revit Family & Type Catalog

## 1. 목적

현재 Revit Template/Project에서 사용할 수 있는 Native BIM Type/Family를 DtoB에 가져온다.

## 2. 전제조건 / 가정

- Revit connection PASS
- Controlled test template 준비

## 3. 이번 Phase 지원 범위

- Walls
- Floors
- Structural Columns
- Doors
- Windows
- Type/Family params
- host behavior
- unit normalize
- catalog snapshot

## 4. 아직 지원하지 않는 범위

- 모든 Revit category
- 회사 표준 라이브러리 관리

## 5. Subphase 상세

### 13A Category Collect

MVP 5개 Category만 수집한다.

### 13B Wall/Floor Type

TypeName/Thickness/Parameters를 읽는다.

### 13C FamilySymbol

FamilyName/TypeName/Width/Height/Host/Parameters를 읽는다.

### 13D Unit Normalize

Revit internal unit을 DtoB mm로 변환한다.

### 13E Snapshot

현재 Template Catalog를 Project에 저장하고 rescan 가능하게 한다.

### 13F Catalog UI

Category별 count/search/details를 제공한다.

## 6. 사용자에게 보이는 결과

Walls 12 / Floors 4 / Columns 5 / Doors 8 / Windows 6처럼 Catalog를 탐색한다.

## 7. 필수 테스트

- collector
- width/height
- unit conversion
- duplicate name
- inactive symbol
- missing parameter

## 8. Exit Criteria

- [ ] 5개 Category Catalog
- [ ] DtoB UI 조회
- [ ] mm normalize
- [ ] Snapshot 저장

## 9. Codex 실행 지시

1. `DtoB_FINAL_MASTER_PLAN.md`, `AGENTS.md`, `ARCHITECTURE.md`를 먼저 읽는다.
2. 이 문서의 PHASE 13 범위만 구현한다.
3. 구현 전 Repository 상태와 기존 테스트를 조사하고 작업 Checklist를 작성한다.
4. Subphase 순서대로 작은 단위로 구현한다.
5. 각 Subphase마다 테스트를 추가한다.
6. UI에서 확인 가능한 결과는 같은 Phase에서 최소 검증 UI를 제공한다.
7. Unsupported Case를 조용히 무시하지 말고 명시적으로 기록한다.
8. 전체 테스트 실행 후 `docs/status/PHASE_13_REPORT.md`를 작성한다.
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

Review 결과는 `reviews/PHASE_13_REVIEW.md`에 작성한다.

Severity: `BLOCKER / HIGH / MEDIUM / LOW`

## 11. Phase Gate

```text
PASS
PASS_WITH_KNOWN_LIMITATIONS
REVIEW_REQUIRED
BLOCKED
```

다음 Phase는 PASS 또는 합의된 PASS_WITH_KNOWN_LIMITATIONS 이후 시작한다.
