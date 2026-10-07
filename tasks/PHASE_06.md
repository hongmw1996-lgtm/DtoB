# PHASE 06 — Grid / Wall / Column Recognition

## 1. 목적

명확한 Layer와 Block을 사용하여 최초 Building Object Candidate를 생성한다.

## 2. 전제조건 / 가정

- Grid=A-GRID
- Wall=A-WALL
- Column=S-COLUMN + COLUMN_400
- 직선 Wall
- 두께 150/200mm

## 3. 이번 Phase 지원 범위

- Grid label/intersection
- Wall parallel pair/thickness/centerline/junction
- Column insertion/size
- 2D overlay

## 4. 아직 지원하지 않는 범위

- 곡선 벽
- Layer 혼용
- 복합 wall assembly
- AI classification

## 5. Subphase 상세

### 06A Grid Recognition

A-GRID Line과 주변 A/B/C/1/2/3 Text를 연결하고 X/Y 그룹 및 intersection을 생성한다.

### 06B-1 Wall Candidate Filter

A-WALL Layer만 Wall 분석 대상으로 제한한다.

### 06B-2 Parallel Pair

동일 View 내 Line pair의 parallel, overlap length, perpendicular distance를 계산한다.

### 06B-3 Thickness

150/200mm를 tolerance 내에서 분류한다.

### 06B-4 Centerline

두 Boundary의 중간 Centerline을 만든다.

### 06B-5 Junction

L/T/X 기본 연결 관계를 생성한다.

### 06B-6 Wall Candidate

WallId, ViewId, BoundaryIds, Centerline, Thickness를 저장한다.

### 06C Column

COLUMN_400 INSERT를 Position/Rotation/400x400 Column Candidate로 만든다.

### 06D Review Overlay

Grid/Wall/Column을 CAD 위에 category별로 겹쳐 보여주고 Ignore/Confirm 가능하게 한다.

## 6. 사용자에게 보이는 결과

Category Panel에 Grid/Wall/Column count가 보이고 선택 시 원본 CAD Source가 highlight된다.

## 7. 필수 테스트

- grid label/intersection
- 150 wall
- 200 wall
- A-FLOOR+A-FURN parallel line이 wall로 들어오지 않음
- vertical/horizontal wall
- column insertion

## 8. Exit Criteria

- [ ] 1F/2F Grid
- [ ] A-WALL만 Wall 후보
- [ ] 150/200 구분
- [ ] Wall centerline
- [ ] Column 8개
- [ ] Overlay review

## 9. Codex 실행 지시

1. `DtoB_FINAL_MASTER_PLAN.md`, `AGENTS.md`, `ARCHITECTURE.md`를 먼저 읽는다.
2. 이 문서의 PHASE 06 범위만 구현한다.
3. 구현 전 Repository 상태와 기존 테스트를 조사하고 작업 Checklist를 작성한다.
4. Subphase 순서대로 작은 단위로 구현한다.
5. 각 Subphase마다 테스트를 추가한다.
6. UI에서 확인 가능한 결과는 같은 Phase에서 최소 검증 UI를 제공한다.
7. Unsupported Case를 조용히 무시하지 말고 명시적으로 기록한다.
8. 전체 테스트 실행 후 `docs/status/PHASE_06_REPORT.md`를 작성한다.
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

Review 결과는 `reviews/PHASE_06_REVIEW.md`에 작성한다.

Severity: `BLOCKER / HIGH / MEDIUM / LOW`

## 11. Phase Gate

```text
PASS
PASS_WITH_KNOWN_LIMITATIONS
REVIEW_REQUIRED
BLOCKED
```

다음 Phase는 PASS 또는 합의된 PASS_WITH_KNOWN_LIMITATIONS 이후 시작한다.
