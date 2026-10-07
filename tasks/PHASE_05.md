# PHASE 05 — Drawing Region & View Detection

## 1. 목적

하나의 ModelSpace 안에서 1F/2F 평면, Section, Elevation을 개별 View Region으로 구분한다.

## 2. 전제조건 / 가정

- View 사이 충분한 간격
- Title text가 명확
- 회전 없는 Controlled drawing

## 3. 이번 Phase 지원 범위

- Spatial cluster
- Title extraction
- Region-title association
- PLAN/SECTION/ELEVATION/UNKNOWN
- manual correction
- View별 entity query

## 4. 아직 지원하지 않는 범위

- 복잡한 overlapping detail
- AI vision classification
- 임의 회전 도면

## 5. Subphase 상세

### 05A Region Candidate

공간적으로 떨어진 Geometry cluster의 BoundingBox를 만든다.

### 05B Title Detection

1F FLOOR PLAN/2F FLOOR PLAN/SECTION A-A/FRONT ELEVATION Text를 찾는다.

### 05C Association

Title과 인접 Geometry cluster를 하나의 View로 연결한다.

### 05D Classification

title keyword 기반으로 PLAN/SECTION/ELEVATION/UNKNOWN을 분류한다.

### 05E View Metadata

ViewId/Type/Title/BBox/Rotation/SourceTextIds를 저장한다.

### 05F Manual Edit

사용자가 Type/Name/Boundary를 수정하고 저장할 수 있게 한다.

## 6. 사용자에게 보이는 결과

Viewer 위에 4개 View Region의 Boundary와 Label이 표시된다.

## 7. 필수 테스트

- 4 region detection
- title association
- view type
- manual override persistence
- unknown region

## 8. Exit Criteria

- [ ] 1F/2F PLAN 2개
- [ ] SECTION 1개
- [ ] ELEVATION 1개
- [ ] 사용자 수정
- [ ] View별 entity query

## 9. Codex 실행 지시

1. `DtoB_FINAL_MASTER_PLAN.md`, `AGENTS.md`, `ARCHITECTURE.md`를 먼저 읽는다.
2. 이 문서의 PHASE 05 범위만 구현한다.
3. 구현 전 Repository 상태와 기존 테스트를 조사하고 작업 Checklist를 작성한다.
4. Subphase 순서대로 작은 단위로 구현한다.
5. 각 Subphase마다 테스트를 추가한다.
6. UI에서 확인 가능한 결과는 같은 Phase에서 최소 검증 UI를 제공한다.
7. Unsupported Case를 조용히 무시하지 말고 명시적으로 기록한다.
8. 전체 테스트 실행 후 `docs/status/PHASE_05_REPORT.md`를 작성한다.
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

Review 결과는 `reviews/PHASE_05_REVIEW.md`에 작성한다.

Severity: `BLOCKER / HIGH / MEDIUM / LOW`

## 11. Phase Gate

```text
PASS
PASS_WITH_KNOWN_LIMITATIONS
REVIEW_REQUIRED
BLOCKED
```

다음 Phase는 PASS 또는 합의된 PASS_WITH_KNOWN_LIMITATIONS 이후 시작한다.
