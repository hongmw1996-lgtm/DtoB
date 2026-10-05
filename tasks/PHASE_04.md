# PHASE 04 — Grid Recognition

## 목표
Grid를 인식하여 Multi-view 정합 Anchor로 사용합니다.

## 주요 작업
- long-line candidate
- repeated alignment
- bubble
- text association
- naming
- intersection graph
- manual correction

## 완료 조건
Grid label/위치가 원본과 연결되고 intersection query 가능

## Codex 실행 절차
1. MASTER_PLAN.md, AGENTS.md, ARCHITECTURE.md를 읽는다.
2. 이 Phase 관련 docs를 읽는다.
3. Repository 상태를 조사한다.
4. Checklist를 먼저 만든다.
5. 작은 단위로 구현한다.
6. 테스트를 추가/실행한다.
7. 결과를 docs/status/PHASE_04_REPORT.md에 기록한다.
8. 다음 Phase를 임의로 시작하지 않는다.

## Antigravity Review
Focus:
- non-grid long lines
- text association

Review 결과는 reviews/PHASE_04_REVIEW.md에 기록한다.

## Phase Gate
PASS / PASS_WITH_KNOWN_LIMITATIONS / REVIEW_REQUIRED / BLOCKED
