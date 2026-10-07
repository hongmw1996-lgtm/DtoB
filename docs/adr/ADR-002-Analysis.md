# ADR-002-Analysis: Analysis boundary

## Status
Accepted — user-approved PHASE 00 direction, 2026-10-07.

## Context
DtoB V1 targets a Controlled two-story CAD pipeline. The final master plan is authoritative; only PHASE 00 is authorized.

## Alternatives

| Option | Evaluation and disposition |
|---|---|
| In-process C# | Selected: deterministic geometry, one runtime and direct typed contracts. |
| C# + Python process/service | Deferred: useful ML libraries later, but packaging, IPC and environment failures add no value before recognition. |

## Decision
In-process C# analysis library with an explicit CAD-to-BIM interface. No recognition implementation.

## Consequences
No second runtime/service required. A future Python adapter must honor the same contracts.

## Migration Impact
Initial foundation, no existing source or persisted schemas to migrate. Later major schema/unit/coordinate/identity changes require a new ADR. No future phase implementation.

## Review amendment — 2026-10-07

Finalized during review resolution. File modification times do not establish original ADR/code ordering. No commit-history ordering claim is made.
