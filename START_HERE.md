# START HERE

## DtoB 개발 시작 방법

Codex와 Antigravity 모두 **같은 Git Repository**를 사용합니다.

```text
hongmw1996-lgtm/DtoB
```

로컬 예:

```text
C:\Projects\DtoB
```

## Codex 첫 지시

```text
Read:
START_HERE.md
MASTER_PLAN.md
AGENTS.md
ARCHITECTURE.md
CODEX_INSTRUCTIONS.md
tasks/PHASE_00.md

You are the implementation owner for DtoB.
Execute PHASE 00 only.
Do not start PHASE 01.
After implementation and tests, write docs/status/PHASE_00_REPORT.md.
```

## Antigravity 첫 지시

Codex가 PHASE 00을 끝낸 뒤:

```text
Read:
MASTER_PLAN.md
AGENTS.md
ARCHITECTURE.md
ANTIGRAVITY_INSTRUCTIONS.md
tasks/PHASE_00.md
docs/status/PHASE_00_REPORT.md
and the current implementation.

Review PHASE 00.
Do not perform a broad rewrite.
Write reviews/PHASE_00_REVIEW.md.
```

## 반복

```text
Codex Implement
→ Antigravity Review
→ Codex Fix
→ Tests
→ Phase Gate
→ Next Phase
```

한 번에 한 Phase만 진행합니다.
