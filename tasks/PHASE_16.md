# PHASE 16 — QA, Regression & DtoB V1 Completion

## 1. 목적

생성된 RVT가 원본 Controlled CAD와 의미/수량/위치에서 일치하는지 검증하고 DtoB V1을 공식 완료한다.

## 2. 전제조건 / 가정

- PHASE 15 End-to-End 성공

## 3. 이번 Phase 지원 범위

- Object count QA
- Geometry deviation
- Semantic/type/host QA
- Failure report
- Golden test
- V1 dashboard

## 4. 아직 지원하지 않는 범위

- 현실의 다양한 CAD 품질 대응
- AI 고도화
- PDF/IFC/KG

## 5. Subphase 상세

### 16A Count QA

Levels/Grids/Walls/Columns/Floors/Doors/Windows/Rooms 개수를 비교한다.

### 16B Geometry QA

Wall baseline, Column centroid, Door/Window insertion, Floor boundary, Level elevation deviation을 계산한다.

### 16C Semantic QA

Correct type/level/host/mark를 확인한다.

### 16D Failure Report

실패 객체마다 Category/Reason/Source Drawing/Handle을 제공한다.

### 16E Golden Test

DtoB_TEST_2STORY를 V1 Golden Dataset으로 고정한다.

### 16F Dashboard

Upload/Parse/View Detection/Object Recognition/3D BIM/Mapping/Generation/QA PASS 상태를 보여준다.

### 16G V1 Freeze

지원범위와 Known Limitations를 문서화하고 V1 tag/release 기준을 준비한다.

## 6. 사용자에게 보이는 결과

DtoB V1 Validation Dashboard와 상세 QA Report를 확인한다.

## 7. 필수 테스트

- full pipeline regression
- count
- geometry tolerance
- mapping
- native category
- save/reopen RVT

## 8. Exit Criteria

- [ ] Desktop 안정 실행
- [ ] Controlled Drawing 전체 Pipeline
- [ ] 3D Preview
- [ ] Revit Catalog/Mapping
- [ ] Native RVT
- [ ] QA PASS
- [ ] Golden Regression PASS
- [ ] V1 limitations 문서화

## 9. Codex 실행 지시

1. `DtoB_FINAL_MASTER_PLAN.md`, `AGENTS.md`, `ARCHITECTURE.md`를 먼저 읽는다.
2. 이 문서의 PHASE 16 범위만 구현한다.
3. 구현 전 Repository 상태와 기존 테스트를 조사하고 작업 Checklist를 작성한다.
4. Subphase 순서대로 작은 단위로 구현한다.
5. 각 Subphase마다 테스트를 추가한다.
6. UI에서 확인 가능한 결과는 같은 Phase에서 최소 검증 UI를 제공한다.
7. Unsupported Case를 조용히 무시하지 말고 명시적으로 기록한다.
8. 전체 테스트 실행 후 `docs/status/PHASE_16_REPORT.md`를 작성한다.
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

Review 결과는 `reviews/PHASE_16_REVIEW.md`에 작성한다.

Severity: `BLOCKER / HIGH / MEDIUM / LOW`

## 11. Phase Gate

```text
PASS
PASS_WITH_KNOWN_LIMITATIONS
REVIEW_REQUIRED
BLOCKED
```

다음 Phase는 PASS 또는 합의된 PASS_WITH_KNOWN_LIMITATIONS 이후 시작한다.
