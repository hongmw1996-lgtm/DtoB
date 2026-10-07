# PHASE 10 — BIM IR & 3D Preview

## 1. 목적

PHASE 09의 논리 모델을 Revit 독립 BIM IR로 고정하고 DtoB 내부에서 3D로 확인한다.

## 2. 전제조건 / 가정

- 3D logical objects 존재

## 3. 이번 Phase 지원 범위

- BIM IR schema
- Level/Grid/Wall/Column/Door/Window/Floor/Room
- relationships
- 3D Viewer
- 2D↔3D sync
- property

## 4. 아직 지원하지 않는 범위

- 고급 렌더링
- 재질 표현
- IFC export

## 5. Subphase 상세

### 10A BIM IR

BimId/Category/Level/Geometry/Parameters/SourceEntityIds/Status 공통 구조를 고정한다.

### 10B Relationships

BELONGS_TO_LEVEL/HOSTED_BY/CONNECTED_TO/BOUNDED_BY/DERIVED_FROM를 저장한다.

### 10C 3D Viewer

Orbit/Pan/Zoom/Fit/Level Toggle/Category Toggle을 구현한다.

### 10D Selection Sync

2D Source와 3D BIM Object를 양방향 highlight한다.

### 10E Property

Level/Thickness/Height/Host/Source를 표시한다.

## 6. 사용자에게 보이는 결과

DtoB가 간단한 BIM Viewer로 동작한다.

## 7. 필수 테스트

- schema validation
- JSON roundtrip
- 2D↔3D identity
- level toggle
- host relation
- source trace

## 8. Exit Criteria

- [ ] BIM IR 생성
- [ ] Revit dependency 없음
- [ ] 2층 3D Preview
- [ ] 2D↔3D sync
- [ ] Property 표시

## 9. Codex 실행 지시

1. `DtoB_FINAL_MASTER_PLAN.md`, `AGENTS.md`, `ARCHITECTURE.md`를 먼저 읽는다.
2. 이 문서의 PHASE 10 범위만 구현한다.
3. 구현 전 Repository 상태와 기존 테스트를 조사하고 작업 Checklist를 작성한다.
4. Subphase 순서대로 작은 단위로 구현한다.
5. 각 Subphase마다 테스트를 추가한다.
6. UI에서 확인 가능한 결과는 같은 Phase에서 최소 검증 UI를 제공한다.
7. Unsupported Case를 조용히 무시하지 말고 명시적으로 기록한다.
8. 전체 테스트 실행 후 `docs/status/PHASE_10_REPORT.md`를 작성한다.
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

Review 결과는 `reviews/PHASE_10_REVIEW.md`에 작성한다.

Severity: `BLOCKER / HIGH / MEDIUM / LOW`

## 11. Phase Gate

```text
PASS
PASS_WITH_KNOWN_LIMITATIONS
REVIEW_REQUIRED
BLOCKED
```

다음 Phase는 PASS 또는 합의된 PASS_WITH_KNOWN_LIMITATIONS 이후 시작한다.
