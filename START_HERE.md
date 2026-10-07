# START HERE

## 최초 1회

Codex와 Antigravity 모두 같은 Git Repository를 엽니다.

예:

```text
C:\Projects\DtoB
```

## Codex 첫 지시

```text
Read:
START_HERE.md
DtoB_FINAL_MASTER_PLAN.md
AGENTS.md
ARCHITECTURE.md
CODEX_INSTRUCTIONS.md
tasks/PHASE_00.md

You are the implementation owner.
Execute PHASE 00 only.
Do not start PHASE 01.
After implementation and tests, write docs/status/PHASE_00_REPORT.md.
```

## Antigravity 첫 지시

Codex가 PHASE 00을 끝낸 뒤:

```text
Read:
DtoB_FINAL_MASTER_PLAN.md
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

## 이후 반복

```text
Codex Implement
→ Antigravity Review
→ Codex Fix
→ Tests
→ Phase Gate
→ Next Phase
```

## 가장 중요한 운영 원칙

한 번에 한 Phase만 진행합니다.
