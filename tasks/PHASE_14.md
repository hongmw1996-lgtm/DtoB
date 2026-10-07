# PHASE 14 — Family / Type Matching

## 1. 목적

DtoB BIM 객체가 Revit에서 어떤 Native Type/Family로 생성될지 결정한다.

## 2. 전제조건 / 가정

- Controlled Revit template
- 명확한 test family/type

## 3. 이번 Phase 지원 범위

- Wall/Floor/Column/Door/Window matching
- 단순 size/name rule
- Manual Override
- Mapping Profile
- pre-generation validation

## 4. 아직 지원하지 않는 범위

- AI semantic matching
- 회사별 대규모 catalog ranking

## 5. Subphase 상세

### 14A Wall

Thickness와 configured rule로 Wall Type을 선택한다.

### 14B Floor

Controlled Floor Type을 선택한다.

### 14C Column

400x400에 맞는 Column Type을 선택한다.

### 14D Door

D1 900x2100을 테스트 Door Family/Type과 매칭한다.

### 14E Window

W1 1200x1500을 테스트 Window Family/Type과 매칭한다.

### 14F Manual Override

Object 또는 Mark 단위로 Mapping 변경 가능.

### 14G Profile

Mapping을 Project에 저장한다.

### 14H Validation

Mapped/Unmapped/InvalidHost/MissingType을 Generate 전에 검사한다.

## 6. 사용자에게 보이는 결과

각 Category에서 Recommended Revit Type과 수동 변경 Dropdown을 제공한다.

## 7. 필수 테스트

- wall 150/200
- door size
- window size
- column size
- manual override persistence
- unmapped detection

## 8. Exit Criteria

- [ ] 5개 Category match
- [ ] Manual override
- [ ] Controlled Test Mapping 100%
- [ ] Generate Ready 상태

## 9. Codex 실행 지시

1. `DtoB_FINAL_MASTER_PLAN.md`, `AGENTS.md`, `ARCHITECTURE.md`를 먼저 읽는다.
2. 이 문서의 PHASE 14 범위만 구현한다.
3. 구현 전 Repository 상태와 기존 테스트를 조사하고 작업 Checklist를 작성한다.
4. Subphase 순서대로 작은 단위로 구현한다.
5. 각 Subphase마다 테스트를 추가한다.
6. UI에서 확인 가능한 결과는 같은 Phase에서 최소 검증 UI를 제공한다.
7. Unsupported Case를 조용히 무시하지 말고 명시적으로 기록한다.
8. 전체 테스트 실행 후 `docs/status/PHASE_14_REPORT.md`를 작성한다.
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

Review 결과는 `reviews/PHASE_14_REVIEW.md`에 작성한다.

Severity: `BLOCKER / HIGH / MEDIUM / LOW`

## 11. Phase Gate

```text
PASS
PASS_WITH_KNOWN_LIMITATIONS
REVIEW_REQUIRED
BLOCKED
```

다음 Phase는 PASS 또는 합의된 PASS_WITH_KNOWN_LIMITATIONS 이후 시작한다.
