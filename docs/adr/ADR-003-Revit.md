# ADR-003-Revit: Revit isolation

## Status
Accepted — user-approved PHASE 00 direction, 2026-10-07.

## Context
DtoB V1 targets a Controlled two-story CAD pipeline. The final master plan is authoritative; only PHASE 00 is authorized.

## Alternatives

| Option | Evaluation and disposition |
|---|---|
| Installed Revit 2025 adapter | Selected: test against the actual licensed host and isolate Autodesk references. |
| Multi-version/runtime adapters | Deferred: no second host requirement; increases builds and unsupported API assumptions. |

## Decision
net8.0-windows x64 adapter for installed Revit 25.4.50.35 / 20260410_1515(x64) at D:\프로그램\Revit 2025. API/runtime configs explicitly declare net8.0. Only this project references Autodesk DLLs, CopyLocal=false. IExternalApplication starts/stops IPC; future Document operations must use ExternalEvent/valid API context.

## Consequences
Core and Desktop build without Revit. No Net10 target or document operations introduced.

## Migration Impact
Initial foundation, no existing source or persisted schemas to migrate. Later major schema/unit/coordinate/identity changes require a new ADR. No future phase implementation.

## Review amendment — 2026-10-07

Finalized during review resolution. File modification times do not establish original ADR/code ordering. No commit-history ordering claim is made.
