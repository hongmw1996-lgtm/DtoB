# ADR-008-CAD-IR: CAD IR v1

## Status
Accepted — user-approved PHASE 00 direction, 2026-10-07.

## Context
DtoB V1 targets a Controlled two-story CAD pipeline. The final master plan is authoritative; only PHASE 00 is authorized.

## Alternatives

| Option | Evaluation and disposition |
|---|---|
| Typed CAD IR | Selected: minimum explicit geometry and source metadata with validation. |
| Generic property bag / SDK object graph | Rejected: hides coordinate contracts or couples consumers to a licensed parser. |

## Decision
Versioned typed System.Text.Json CAD document/layer/entity/block/text/dimension contracts, basic geometry, transforms, original/effective layer/color, linetype/lineweight, ownership, occurrence path and raw JSON properties. Unknown entities remain explicit with a source-linked warning diagnostic.

## Consequences
Synthetic fixtures prove serialization only. Unknown schema major and invalid references rejected; raw fields are preserved, no promise of complete DWG fidelity.

## Migration Impact
Initial foundation, no existing source or persisted schemas to migrate. Later major schema/unit/coordinate/identity changes require a new ADR. No future phase implementation.

## Review amendment — 2026-10-07

Finalized during review resolution. File modification times do not establish original ADR/code ordering. No commit-history ordering claim is made.

IR v1 is strict: unknown fields are forbidden, additive wire fields require an ADR and schema-major migration before deployment. Probe schemaVersion before typed deserialization. $kind/$category must appear first on polymorphic objects for .NET 8; producer violations yield InvalidDataException. Records with arrays are transport snapshots, not value-comparable or deeply immutable. Compare serialized contents and revalidate after mutation. Frame/arc/normal conventions are defined in ADR-006.
