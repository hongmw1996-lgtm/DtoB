# PHASE 09 — Plan + Section + Elevation → 3D Logical Model

## 1. 목적

평면 X/Y와 단면·입면 Z/Height를 연결하여 Revit 없이 완전한 3D 논리 BIM을 만든다.

## 2. 전제조건 / 가정

- 1F/2F Plan 명확
- SECTION A-A
- FRONT ELEVATION
- 1F=0,2F=3000,RF=6000
- W1/D1 표기 명확

## 3. 이번 Phase 지원 범위

- Level recognition
- Plan level assignment
- Section relation
- coordinate alignment
- Wall/Column/Floor vertical extents
- Door height
- Window height+sill
- 3D assembly
- conflict state

## 4. 아직 지원하지 않는 범위

- 복수 Section 자동 최적화
- 비대칭 복잡 건물
- 경사 지붕
- 복잡한 Level offset

## 5. Subphase 상세

### 09A Level Recognition

1F +0, 2F +3000, RF +6000을 Level 객체로 만든다.

### 09B Plan Assignment

1F PLAN 객체는 1F, 2F PLAN 객체는 2F에 귀속한다.

### 09C Section Marker

Plan의 A-A 표기와 SECTION A-A View를 연결한다.

### 09D Coordinate Alignment

Grid와 건물 폭 등 명확한 Anchor로 Plan X/Y와 Section 축을 맞춘다.

### 09E Wall Vertical

1F Wall base=0,height=3000, 2F Wall base=3000,height=3000으로 만든다.

### 09F Door Vertical

D1의 Height=2100, BaseOffset=0을 부여한다.

### 09G Window Vertical

Plan X/Y/Width/Host에 Elevation/Section의 Height=1500, Sill=900을 결합한다.

### 09H Column/Floor

Column base/top Level과 Floor elevation/boundary를 만든다.

### 09I 3D Assembly

Wall/Door/Window/Column/Floor가 XYZ와 Level/Host를 가진 완전한 logical object가 되게 한다.

### 09J Conflict Policy

상충 정보는 Resolved/NeedsReview/Unresolved로 분리하고 임의 덮어쓰기를 금지한다.

## 6. 사용자에게 보이는 결과

객체 선택 시 Level/X/Y/Z/Height/Sill/Host를 확인하고 3D 준비 상태를 표시한다.

## 7. 필수 테스트

- level elevations
- plan level assignment
- wall height
- door height
- window sill
- plan↔section relation
- conflict→NeedsReview

## 8. Exit Criteria

- [ ] 1F/2F/RF Level
- [ ] 모든 Plan 객체 Level
- [ ] Wall height
- [ ] Door height
- [ ] Window height/sill
- [ ] Column vertical
- [ ] Floor elevation
- [ ] 3D Logical Model

## 9. Codex 실행 지시

1. `DtoB_FINAL_MASTER_PLAN.md`, `AGENTS.md`, `ARCHITECTURE.md`를 먼저 읽는다.
2. 이 문서의 PHASE 09 범위만 구현한다.
3. 구현 전 Repository 상태와 기존 테스트를 조사하고 작업 Checklist를 작성한다.
4. Subphase 순서대로 작은 단위로 구현한다.
5. 각 Subphase마다 테스트를 추가한다.
6. UI에서 확인 가능한 결과는 같은 Phase에서 최소 검증 UI를 제공한다.
7. Unsupported Case를 조용히 무시하지 말고 명시적으로 기록한다.
8. 전체 테스트 실행 후 `docs/status/PHASE_09_REPORT.md`를 작성한다.
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

Review 결과는 `reviews/PHASE_09_REVIEW.md`에 작성한다.

Severity: `BLOCKER / HIGH / MEDIUM / LOW`

## 11. Phase Gate

```text
PASS
PASS_WITH_KNOWN_LIMITATIONS
REVIEW_REQUIRED
BLOCKED
```

다음 Phase는 PASS 또는 합의된 PASS_WITH_KNOWN_LIMITATIONS 이후 시작한다.
