# PHASE 15 — Validation & Golden Tests

## 목표
생성 결과 품질을 수치화하고 회귀를 방지합니다.

## 주요 작업
- counts
- unhosted
- duplicate
- overlap
- short wall
- enclosure
- level mismatch
- mapping failure
- CAD-vs-BIM projection
- golden dataset

## 완료 조건
main 변경에서 golden regression 수행

## Codex 실행 절차
1. MASTER_PLAN.md, AGENTS.md, ARCHITECTURE.md를 읽는다.
2. 이 Phase 관련 docs를 읽는다.
3. Repository 상태를 조사한다.
4. Checklist를 먼저 만든다.
5. 작은 단위로 구현한다.
6. 테스트를 추가/실행한다.
7. 결과를 docs/status/PHASE_15_REPORT.md에 기록한다.
8. 다음 Phase를 임의로 시작하지 않는다.

## Antigravity Review
Focus:
- false pass
- metric gaming
- missing edge cases

Review 결과는 reviews/PHASE_15_REVIEW.md에 기록한다.

## Phase Gate
PASS / PASS_WITH_KNOWN_LIMITATIONS / REVIEW_REQUIRED / BLOCKED
