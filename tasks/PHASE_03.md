# PHASE 03 — DWG Viewer

## 1. 목적

DtoB 내부에서 업로드된 CAD를 직접 확인하고 Layer/Entity를 탐색할 수 있는 최소 Viewer를 만든다.

## 2. 전제조건 / 가정

- PHASE 02 Drawing 존재
- Controlled test drawing

## 3. 이번 Phase 지원 범위

- LINE/POLYLINE/ARC/CIRCLE 렌더링
- TEXT/MTEXT/INSERT/DIMENSION/HATCH 확장
- Zoom/Pan/Fit
- Layer ON/OFF/Solo/Search
- Entity selection
- Inspector

## 4. 아직 지원하지 않는 범위

- AutoCAD 편집 기능
- 완전한 plotting
- 복잡한 3D CAD

## 5. Subphase 상세

### 03A Viewport

Zoom/Pan/Fit/Reset을 만든다.

### 03B Geometry Render

CAD world coordinate를 유지해 기본 Entity를 렌더링한다.

### 03C Layer Panel

Layer name/color/visible과 On/Off/Solo/Search를 제공한다.

### 03D Selection

클릭 선택, highlight, clear selection을 구현한다.

### 03E Inspector

Type/Handle/Layer/Color/Block/Geometry summary를 표시한다.

### 03F Performance

Controlled drawing을 실사용 가능한 속도로 그리도록 batching/cache를 필요한 만큼 적용한다.

## 6. 사용자에게 보이는 결과

1F/2F/Section/Elevation 전체 도면이 보이고 A-WALL Layer를 끄면 벽 선만 사라진다.

## 7. 필수 테스트

- coordinate accuracy
- zoom/pan
- layer visibility
- selection identity
- color
- empty drawing
- unsupported entity fallback

## 8. Exit Criteria

- [ ] 테스트 도면 표시
- [ ] Zoom/Pan/Fit
- [ ] Layer 제어
- [ ] Entity 선택
- [ ] Handle/Layer/Color Inspector
- [ ] Viewer↔Source ID 유지

## 9. Codex 실행 지시

1. `DtoB_FINAL_MASTER_PLAN.md`, `AGENTS.md`, `ARCHITECTURE.md`를 먼저 읽는다.
2. 이 문서의 PHASE 03 범위만 구현한다.
3. 구현 전 Repository 상태와 기존 테스트를 조사하고 작업 Checklist를 작성한다.
4. Subphase 순서대로 작은 단위로 구현한다.
5. 각 Subphase마다 테스트를 추가한다.
6. UI에서 확인 가능한 결과는 같은 Phase에서 최소 검증 UI를 제공한다.
7. Unsupported Case를 조용히 무시하지 말고 명시적으로 기록한다.
8. 전체 테스트 실행 후 `docs/status/PHASE_03_REPORT.md`를 작성한다.
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

Review 결과는 `reviews/PHASE_03_REVIEW.md`에 작성한다.

Severity: `BLOCKER / HIGH / MEDIUM / LOW`

## 11. Phase Gate

```text
PASS
PASS_WITH_KNOWN_LIMITATIONS
REVIEW_REQUIRED
BLOCKED
```

다음 Phase는 PASS 또는 합의된 PASS_WITH_KNOWN_LIMITATIONS 이후 시작한다.
