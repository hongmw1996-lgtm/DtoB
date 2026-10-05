# PHASE 05 — Wall Recognition

## 목표
건축 벽체 Candidate를 안정적으로 추출합니다.

## 주요 작업
- parallel pair
- thickness
- wall layer
- hatch/polyline
- junction
- room boundary
- opening gap
- confidence/evidence
- manual correction

## 완료 조건
샘플 평면 Wall Candidate를 Overlay로 확인/수정

## Codex 실행 절차
1. MASTER_PLAN.md, AGENTS.md, ARCHITECTURE.md를 읽는다.
2. 이 Phase 관련 docs를 읽는다.
3. Repository 상태를 조사한다.
4. Checklist를 먼저 만든다.
5. 작은 단위로 구현한다.
6. 테스트를 추가/실행한다.
7. 결과를 docs/status/PHASE_05_REPORT.md에 기록한다.
8. 다음 Phase를 임의로 시작하지 않는다.

## Antigravity Review
Focus:
- furniture/dimension false positive
- double/curved/tiny wall

Review 결과는 reviews/PHASE_05_REVIEW.md에 기록한다.

## Phase Gate
PASS / PASS_WITH_KNOWN_LIMITATIONS / REVIEW_REQUIRED / BLOCKED
