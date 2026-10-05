# PHASE 02 — CAD Normalization

## 목표
좌표/블록/중복 표현을 동일 Geometry 기준으로 정규화합니다.

## 주요 작업
- Unit→mm
- UCS/WCS policy
- block/nested transform
- mirror/rotation
- near-zero removal
- duplicate detection
- merge
- spatial index
- tolerance service

## 완료 조건
Block 내부/외부 Geometry를 동일 World Coordinate에서 비교

## Codex 실행 절차
1. MASTER_PLAN.md, AGENTS.md, ARCHITECTURE.md를 읽는다.
2. 이 Phase 관련 docs를 읽는다.
3. Repository 상태를 조사한다.
4. Checklist를 먼저 만든다.
5. 작은 단위로 구현한다.
6. 테스트를 추가/실행한다.
7. 결과를 docs/status/PHASE_02_REPORT.md에 기록한다.
8. 다음 Phase를 임의로 시작하지 않는다.

## Antigravity Review
Focus:
- hidden assumptions
- tolerance
- transform order

Review 결과는 reviews/PHASE_02_REVIEW.md에 기록한다.

## Phase Gate
PASS / PASS_WITH_KNOWN_LIMITATIONS / REVIEW_REQUIRED / BLOCKED
