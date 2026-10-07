# PHASE 15 — Revit Native BIM Generation

## 1. 목적

BIM IR과 Mapping 결과로 실제 Native Revit Model을 생성하고 RVT로 저장한다.

## 2. 전제조건 / 가정

- 모든 Controlled object mapped
- Revit document connected

## 3. 이번 Phase 지원 범위

- Levels
- Grids
- Walls
- Columns
- Floors
- Doors
- Windows
- Room 기본
- Trace metadata
- Transaction grouping
- Generation Report
- RVT save

## 4. 아직 지원하지 않는 범위

- 복잡한 joins
- 고급 Room/Space
- MEP/Structure connection

## 5. Subphase 상세

### 15A Levels

1F/2F/RF를 생성 또는 기존 Level과 안전하게 매칭한다.

### 15B Grids

A/B/C 및 1/2/3 Grid를 Native Grid로 만든다.

### 15C Walls

baseline/type/base level/height로 Native Wall을 만든다.

### 15D Columns

Mapped FamilySymbol과 base/top level로 Native Column을 만든다.

### 15E Floors

Boundary/Type/Level로 Native Floor를 만든다.

### 15F Doors

Host Wall/FamilySymbol/XYZ/Level로 hosted Door FamilyInstance를 만든다.

### 15G Windows

Host Wall/FamilySymbol/XYZ/Level/Sill 정보로 Window를 만든다.

### 15H Rooms

Controlled 범위에서 기본 Room 생성, 실패 시 전체를 무조건 rollback하지 않도록 한다.

### 15I Traceability

DTB_BimIrId/SourceDrawing/SourceHandle/GeneratedAt 기록 방식을 적용한다.

### 15J Transaction

LevelsAndGrids/Walls/Structure/Floors/Openings/Metadata 등 기능 그룹으로 Transaction을 분리한다.

### 15K Report

Created/Failed/Skipped와 개별 Reason을 출력한다.

## 6. 사용자에게 보이는 결과

Generate BIM 실행 후 Revit 생성 진행상태와 결과 요약을 DtoB에서 본다.

## 7. 필수 테스트

- native category
- level
- wall length
- door host
- window sill
- count
- rerun duplicate behavior
- partial failure

## 8. Exit Criteria

- [ ] Native Level/Grid/Wall/Column/Floor/Door/Window
- [ ] Traceability
- [ ] Generation Report
- [ ] RVT Save
- [ ] Controlled End-to-End 성공

## 9. Codex 실행 지시

1. `DtoB_FINAL_MASTER_PLAN.md`, `AGENTS.md`, `ARCHITECTURE.md`를 먼저 읽는다.
2. 이 문서의 PHASE 15 범위만 구현한다.
3. 구현 전 Repository 상태와 기존 테스트를 조사하고 작업 Checklist를 작성한다.
4. Subphase 순서대로 작은 단위로 구현한다.
5. 각 Subphase마다 테스트를 추가한다.
6. UI에서 확인 가능한 결과는 같은 Phase에서 최소 검증 UI를 제공한다.
7. Unsupported Case를 조용히 무시하지 말고 명시적으로 기록한다.
8. 전체 테스트 실행 후 `docs/status/PHASE_15_REPORT.md`를 작성한다.
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

Review 결과는 `reviews/PHASE_15_REVIEW.md`에 작성한다.

Severity: `BLOCKER / HIGH / MEDIUM / LOW`

## 11. Phase Gate

```text
PASS
PASS_WITH_KNOWN_LIMITATIONS
REVIEW_REQUIRED
BLOCKED
```

다음 Phase는 PASS 또는 합의된 PASS_WITH_KNOWN_LIMITATIONS 이후 시작한다.
