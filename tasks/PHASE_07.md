# PHASE 07 — Door / Window / Floor / Room Recognition

## 1. 목적

평면도의 개구부와 공간 요소를 BIM Candidate로 만든다.

## 2. 전제조건 / 가정

- Door=A-DOOR + D_SINGLE_900
- Window=A-WINDOW + W_SINGLE_1200
- Floor=A-FLOOR
- Room text 명확

## 3. 이번 Phase 지원 범위

- Door/Window position/rotation/width
- Host wall candidate
- Floor boundary
- Room 기본 candidate
- Opening abstraction

## 4. 아직 지원하지 않는 범위

- 복잡한 opening
- 커튼월
- 다중 leaf 문
- 비정형 floor void

## 5. Subphase 상세

### 07A Door

D_SINGLE_900 block reference에서 insertion/rotation/width/view를 읽는다.

### 07B Door Host

Door 근처 Wall 중 거리+orientation으로 Host 후보를 찾고 실패 시 NeedsReview로 둔다.

### 07C Window

W_SINGLE_1200에서 position/rotation/width/host 후보를 만든다.

### 07D Floor

A-FLOOR closed polyline을 Floor Boundary로 사용한다.

### 07E Room

closed space와 ROOM A/B/C/D Text를 기반으로 기본 Room Candidate를 만든다.

### 07F Opening Model

Door/Window가 공통적으로 Host/Position/Width를 가지도록 공통 데이터 모델을 정리한다.

## 6. 사용자에게 보이는 결과

Doors/Windows/Floors/Rooms count와 Overlay를 확인한다.

## 7. 필수 테스트

- door parse
- window parse
- rotated window
- nearest host wall
- missing host→NeedsReview
- floor boundary
- room name

## 8. Exit Criteria

- [ ] Door Candidate
- [ ] Window Candidate
- [ ] Floor 2개
- [ ] Room 기본 Candidate
- [ ] Host 후보
- [ ] Overlay

## 9. Codex 실행 지시

1. `DtoB_FINAL_MASTER_PLAN.md`, `AGENTS.md`, `ARCHITECTURE.md`를 먼저 읽는다.
2. 이 문서의 PHASE 07 범위만 구현한다.
3. 구현 전 Repository 상태와 기존 테스트를 조사하고 작업 Checklist를 작성한다.
4. Subphase 순서대로 작은 단위로 구현한다.
5. 각 Subphase마다 테스트를 추가한다.
6. UI에서 확인 가능한 결과는 같은 Phase에서 최소 검증 UI를 제공한다.
7. Unsupported Case를 조용히 무시하지 말고 명시적으로 기록한다.
8. 전체 테스트 실행 후 `docs/status/PHASE_07_REPORT.md`를 작성한다.
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

Review 결과는 `reviews/PHASE_07_REVIEW.md`에 작성한다.

Severity: `BLOCKER / HIGH / MEDIUM / LOW`

## 11. Phase Gate

```text
PASS
PASS_WITH_KNOWN_LIMITATIONS
REVIEW_REQUIRED
BLOCKED
```

다음 Phase는 PASS 또는 합의된 PASS_WITH_KNOWN_LIMITATIONS 이후 시작한다.
