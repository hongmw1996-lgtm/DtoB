# ADR-005-Drawing-Reader: Deferred DWG SDK

## Status
Accepted — user-approved PHASE 00 direction, 2026-10-07.

## Context
DtoB V1 targets a Controlled two-story CAD pipeline. The final master plan is authoritative; only PHASE 00 is authorized.

## Alternatives

| Option | Evaluation and disposition |
|---|---|
| ODA Drawings SDK | Candidate only: commercial SDK license and redistribution terms require written confirmation; .NET 8 binding compatibility and nested block/Xref/handle fidelity must be tested with a licensed SDK. |
| Autodesk RealDWG | Candidate only: licensing/distribution approval required; managed runtime support must be confirmed for selected release; block/Xref and persistent handle extraction require a licensed spike. |
| AutoCAD-assisted reader | Candidate only: requires installed licensed AutoCAD, introduces host/process dependency; native database access can retain handles, but Xref/block fidelity and runtime integration still need fixtures. |

## Decision
Define IDrawingReader and synthetic CAD IR fixtures only. DWG SDK product decision remains deferred. No ODA/RealDWG dependency, actual parsing, conversion workaround or license procurement.

## Consequences
Future SDK selection requires its own evidence and ADR; PHASE 00 reader capability reports not implemented.

## Migration Impact
Initial foundation, no existing source or persisted schemas to migrate. Later major schema/unit/coordinate/identity changes require a new ADR. No future phase implementation.

## Review amendment — 2026-10-07

Finalized during review resolution. File modification times do not establish original ADR/code ordering. No commit-history ordering claim is made.

The human project owner explicitly approved on 2026-10-07: "Keep the DWG SDK product decision deferred. PHASE 00 must define IDrawingReader and synthetic CAD IR fixtures only. Do not add ODA or RealDWG dependencies yet." Resolve product/license/runtime/fidelity evidence before the PHASE 03 viewer / PHASE 04 actual reader starts. No vendor compatibility or license approval is assumed by this comparison.
