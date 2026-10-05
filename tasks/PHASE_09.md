# PHASE 09 — Multi-view Resolver

## 목표
평면/단면/입면 객체를 대응시켜 3D 정보로 결합합니다.

## 주요 작업
- view mapping
- grid/level correspondence
- section marker
- object matching
- conflict resolution
- confidence propagation

## 완료 조건
Plan Window X/Y/Width + Elevation Sill/Height가 하나의 BIM object로 병합

## Codex 실행 절차
1. MASTER_PLAN.md, AGENTS.md, ARCHITECTURE.md를 읽는다.
2. 이 Phase 관련 docs를 읽는다.
3. Repository 상태를 조사한다.
4. Checklist를 먼저 만든다.
5. 작은 단위로 구현한다.
6. 테스트를 추가/실행한다.
7. 결과를 docs/status/PHASE_09_REPORT.md에 기록한다.
8. 다음 Phase를 임의로 시작하지 않는다.

## Antigravity Review
Focus:
- wrong pairing
- repeated marks
- asymmetric building

Review 결과는 reviews/PHASE_09_REVIEW.md에 기록한다.

## Phase Gate
PASS / PASS_WITH_KNOWN_LIMITATIONS / REVIEW_REQUIRED / BLOCKED
