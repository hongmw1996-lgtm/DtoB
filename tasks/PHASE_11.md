# PHASE 11 — Review Workbench

## 1. 목적

각 Phase에 흩어진 검수 기능을 하나의 전문 Workbench로 통합한다.

## 2. 전제조건 / 가정

- 2D/3D Selection 가능
- BIM IR 존재

## 3. 이번 Phase 지원 범위

- Project Tree
- 2D/3D Viewer
- Object Table
- Property/Evidence
- Issue Queue
- Confirm/Ignore
- 기본 속성 수정
- Audit

## 4. 아직 지원하지 않는 범위

- CAD 수준 자유 geometry editor
- 협업 리뷰
- AI learning 자동화

## 5. Subphase 상세

### 11A Layout

Project Tree + 2D/3D View + Property/Evidence + Object Table 구조를 만든다.

### 11B Table

ID/Category/View/Level/Mark/Status/Source를 표시한다.

### 11C Status

Confirmed/Suggested/NeedsReview/Ignored/Error 상태를 정의한다.

### 11D Edit

Category/Level/Width/Height/Thickness/Host를 수정한다.

### 11E Issue Queue

Host 없음, Metadata conflict 등 검토 대상을 모은다.

### 11F Audit

before/after/timestamp를 저장해 향후 학습 데이터로 활용할 수 있게 한다.

## 6. 사용자에게 보이는 결과

사용자가 Revit 생성 전 모든 NeedsReview 항목을 처리할 수 있다.

## 7. 필수 테스트

- edit save
- ignore excluded
- source trace 유지
- host manual correction
- audit

## 8. Exit Criteria

- [ ] Object Table
- [ ] 2D/3D/Table sync
- [ ] Issue Queue
- [ ] 속성 수정
- [ ] Ignore/Confirm
- [ ] BIM IR update

## 9. Codex 실행 지시

1. `DtoB_FINAL_MASTER_PLAN.md`, `AGENTS.md`, `ARCHITECTURE.md`를 먼저 읽는다.
2. 이 문서의 PHASE 11 범위만 구현한다.
3. 구현 전 Repository 상태와 기존 테스트를 조사하고 작업 Checklist를 작성한다.
4. Subphase 순서대로 작은 단위로 구현한다.
5. 각 Subphase마다 테스트를 추가한다.
6. UI에서 확인 가능한 결과는 같은 Phase에서 최소 검증 UI를 제공한다.
7. Unsupported Case를 조용히 무시하지 말고 명시적으로 기록한다.
8. 전체 테스트 실행 후 `docs/status/PHASE_11_REPORT.md`를 작성한다.
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

Review 결과는 `reviews/PHASE_11_REVIEW.md`에 작성한다.

Severity: `BLOCKER / HIGH / MEDIUM / LOW`

## 11. Phase Gate

```text
PASS
PASS_WITH_KNOWN_LIMITATIONS
REVIEW_REQUIRED
BLOCKED
```

다음 Phase는 PASS 또는 합의된 PASS_WITH_KNOWN_LIMITATIONS 이후 시작한다.
