# ADR-001-Desktop-UI: Desktop and UI

## Status
Accepted — user-approved PHASE 00 direction, 2026-10-07.

## Context
DtoB V1 targets a Controlled two-story CAD pipeline. The final master plan is authoritative; only PHASE 00 is authorized.

## Alternatives

| Option | Evaluation and disposition |
|---|---|
| WPF | Selected: native Windows deployment, C# and Revit ecosystem match; viewer can be added later. |
| WPF + WebView2 | Deferred: suitable web viewer host, but adds runtime and interop without a PHASE 00 need. |
| WinUI / web desktop | Rejected for V1 foundation: additional packaging/toolchain without a connection-PoC benefit. |

## Decision
C#/.NET 8 WPF. Minimal technical connection/PING window only; no PHASE 01 project management or WebView2 dependency.

## Consequences
Windows-only deployment. Future viewer choices remain open.

## Migration Impact
Initial foundation, no existing source or persisted schemas to migrate. Later major schema/unit/coordinate/identity changes require a new ADR. No future phase implementation.

## Review amendment — 2026-10-07

Finalized during review resolution. File modification times do not establish original ADR/code ordering. No commit-history ordering claim is made.
