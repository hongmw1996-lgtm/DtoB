# PHASE 01 — DWG Inspector

## 목표
실제 DWG 내부 정보를 손실 없이 읽어 Inspection 가능한 상태로 만듭니다.

## 주요 작업
- IDrawingReader
- DWG SDK PoC
- Mock reader
- LINE/POLYLINE/ARC/CIRCLE/TEXT/MTEXT/BLOCK/ATTRIB/DIMENSION/Layer
- source handle
- unit
- extents
- unsupported log
- CAD IR JSON
- 2D Viewer

## 완료 조건
실제 DWG를 업로드하고 원본 좌표에 가깝게 벡터와 텍스트 표시

## Codex 실행 절차
1. MASTER_PLAN.md, AGENTS.md, ARCHITECTURE.md를 읽는다.
2. 이 Phase 관련 docs를 읽는다.
3. Repository 상태를 조사한다.
4. Checklist를 먼저 만든다.
5. 작은 단위로 구현한다.
6. 테스트를 추가/실행한다.
7. 결과를 docs/status/PHASE_01_REPORT.md에 기록한다.
8. 다음 Phase를 임의로 시작하지 않는다.

## Antigravity Review
Focus:
- data loss
- handle preservation
- transform correctness
- unsupported entity

Review 결과는 reviews/PHASE_01_REVIEW.md에 기록한다.

## Phase Gate
PASS / PASS_WITH_KNOWN_LIMITATIONS / REVIEW_REQUIRED / BLOCKED
