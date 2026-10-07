# ADR-010-Build-Test: Repository, build and testing

## Status
Accepted — user-approved PHASE 00 direction, 2026-10-07.

## Context
DtoB V1 targets a Controlled two-story CAD pipeline. The final master plan is authoritative; only PHASE 00 is authorized.

## Alternatives

| Option | Evaluation and disposition |
|---|---|
| One solution + xUnit fixtures | Selected: simple dependency graph and reproducible layered gates. |
| Multiple solutions / integration-only tests | Rejected: fragmented build ordering or licensed-host requirement for every contract test. |

## Decision
One DtoB.sln, SDK 8.0.416 with latestPatch roll-forward, nullable deterministic builds; explicit project boundaries, xUnit plus synthetic JSON fixtures. RevitInstallDir configurable and missing API DLLs fail with clear error. Core-only and portable test commands work independently of Revit. Windows CI checks Core/Desktop; actual Revit host gate is local.

## Consequences
No proprietary DLL redistribution. PHASE 00 requires real host PING/PONG plus isolated tests. Existing staged user documents preserved; no commits/pushes.

## Migration Impact
Initial foundation, no existing source or persisted schemas to migrate. Later major schema/unit/coordinate/identity changes require a new ADR. No future phase implementation.

## Review amendment — 2026-10-07

Finalized during review resolution. File modification times do not establish original ADR/code ordering. No commit-history ordering claim is made.
