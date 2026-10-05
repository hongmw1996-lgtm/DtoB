# PHASE 03 — Drawing Region & View Classification

## 목표
한 DWG 안의 평면/단면/입면/상세도를 분리합니다.

## 주요 작업
- spatial clustering
- border
- title text
- view bbox
- PLAN/SECTION/ELEVATION/DETAIL/SCHEDULE/LEGEND/UNKNOWN
- manual edit

## 완료 조건
사용자가 각 Region을 확인/수정

## Codex 실행 절차
1. MASTER_PLAN.md, AGENTS.md, ARCHITECTURE.md를 읽는다.
2. 이 Phase 관련 docs를 읽는다.
3. Repository 상태를 조사한다.
4. Checklist를 먼저 만든다.
5. 작은 단위로 구현한다.
6. 테스트를 추가/실행한다.
7. 결과를 docs/status/PHASE_03_REPORT.md에 기록한다.
8. 다음 Phase를 임의로 시작하지 않는다.

## Antigravity Review
Focus:
- false split
- overlapping details

Review 결과는 reviews/PHASE_03_REVIEW.md에 기록한다.

## Phase Gate
PASS / PASS_WITH_KNOWN_LIMITATIONS / REVIEW_REQUIRED / BLOCKED
