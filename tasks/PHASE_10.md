# PHASE 10 — BIM IR & Constraint Graph

## 목표
CAD 해석 결과를 Revit 독립 BIM 모델로 변환합니다.

## 주요 작업
- Level/Grid/Wall/Column/Floor/Door/Window/Room/Opening
- HOSTS/CONNECTED/ALIGNED/BELONGS/BOUNDED/MATCHES/ANNOTATED/DERIVED
- schema/stable ID/source trace/version

## 완료 조건
BIM IR만으로 Preview와 Revit Generation 입력 생성

## Codex 실행 절차
1. MASTER_PLAN.md, AGENTS.md, ARCHITECTURE.md를 읽는다.
2. 이 Phase 관련 docs를 읽는다.
3. Repository 상태를 조사한다.
4. Checklist를 먼저 만든다.
5. 작은 단위로 구현한다.
6. 테스트를 추가/실행한다.
7. 결과를 docs/status/PHASE_10_REPORT.md에 기록한다.
8. 다음 Phase를 임의로 시작하지 않는다.

## Antigravity Review
Focus:
- Revit-specific leakage
- unstable IDs

Review 결과는 reviews/PHASE_10_REVIEW.md에 기록한다.

## Phase Gate
PASS / PASS_WITH_KNOWN_LIMITATIONS / REVIEW_REQUIRED / BLOCKED
