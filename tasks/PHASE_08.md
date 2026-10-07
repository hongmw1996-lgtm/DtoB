# PHASE 08 — Text / Dimension / Metadata Intelligence

## 1. 목적

Text와 Dimension으로 기존 객체 Candidate의 Mark, 크기, Level 등 Metadata를 보완한다.

## 2. 전제조건 / 가정

- TEXT/MTEXT native extraction 완료
- 표기 형식이 Controlled

## 3. 이번 Phase 지원 범위

- 1F/2F/RF
- Level elevation
- D1 900x2100
- W1 1200x1500
- ROOM A~D
- Dimension value
- Spatial association
- Evidence source

## 4. 아직 지원하지 않는 범위

- 자유형 자연어 해석
- OCR
- 복잡한 leader inference
- LLM semantic parser

## 5. Subphase 상세

### 08A Text Reader

PHASE 04 Text Entity를 semantic parser에 입력한다.

### 08B Deterministic Parser

Regex/규칙으로 Level, Door Mark/Size, Window Mark/Size, Room Name을 파싱한다.

### 08C Dimension

Measured value와 geometry를 읽고 초기에는 Validation Evidence로 사용한다.

### 08D Spatial Association

View Region + Category + 거리로 Text를 Candidate에 연결한다.

### 08E Metadata Update

Door/Window/Room/Level 속성을 보완한다.

### 08F Evidence

각 속성 값이 Block/Text/Dimension 중 어디서 왔는지 Source를 저장한다.

## 6. 사용자에게 보이는 결과

Property Panel에서 W1 Width 1200, Height Hint 1500과 그 Source Text/Block을 확인한다.

## 7. 필수 테스트

- level text
- D1 parse
- W1 parse
- room name
- bad pattern fallback
- wrong view association 방지

## 8. Exit Criteria

- [ ] Controlled Text parsing
- [ ] Door/Window metadata
- [ ] Room name
- [ ] Level text
- [ ] Evidence 저장

## 9. Codex 실행 지시

1. `DtoB_FINAL_MASTER_PLAN.md`, `AGENTS.md`, `ARCHITECTURE.md`를 먼저 읽는다.
2. 이 문서의 PHASE 08 범위만 구현한다.
3. 구현 전 Repository 상태와 기존 테스트를 조사하고 작업 Checklist를 작성한다.
4. Subphase 순서대로 작은 단위로 구현한다.
5. 각 Subphase마다 테스트를 추가한다.
6. UI에서 확인 가능한 결과는 같은 Phase에서 최소 검증 UI를 제공한다.
7. Unsupported Case를 조용히 무시하지 말고 명시적으로 기록한다.
8. 전체 테스트 실행 후 `docs/status/PHASE_08_REPORT.md`를 작성한다.
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

Review 결과는 `reviews/PHASE_08_REVIEW.md`에 작성한다.

Severity: `BLOCKER / HIGH / MEDIUM / LOW`

## 11. Phase Gate

```text
PASS
PASS_WITH_KNOWN_LIMITATIONS
REVIEW_REQUIRED
BLOCKED
```

다음 Phase는 PASS 또는 합의된 PASS_WITH_KNOWN_LIMITATIONS 이후 시작한다.
