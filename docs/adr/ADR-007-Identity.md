# ADR-007-Identity: Source and domain identity

## Status
Accepted — user-approved PHASE 00 direction, 2026-10-07.

## Context
DtoB V1 targets a Controlled two-story CAD pipeline. The final master plan is authoritative; only PHASE 00 is authorized.

## Alternatives

| Option | Evaluation and disposition |
|---|---|
| Source-derived CAD ID + persistent BIM UUID | Selected: distinguish repeated occurrences and preserve source trace without geometry-dependent identity. |
| Random CAD IDs / geometry hashes | Rejected: random IDs lose deterministic trace; geometry hashes change after edits and collide for repeated symbols. |

## Decision
Drawing UUID is persistent; revision UUID separate. CAD identity encodes drawing UUID, source handle and insertion occurrence path without collisions. BIM UUID allocated once and persisted; provenance carries drawing/revision/handle/CAD identity. No stability guarantee for rerun recognition or across revisions yet.

## Consequences
Do not use geometry hashes as domain identity; repeated block occurrences remain distinct. Generation rerun identity semantics deferred.

## Migration Impact
Initial foundation, no existing source or persisted schemas to migrate. Later major schema/unit/coordinate/identity changes require a new ADR. No future phase implementation.

## Review amendment — 2026-10-07

Finalized during review resolution. File modification times do not establish original ADR/code ordering. No commit-history ordering claim is made.

Canonical occurrence ID normalizes hexadecimal handle and INSERT handles to uppercase. Original source Handle remains unchanged. SourceReference must match its drawing UUID and canonical handle segment; cross-document CAD membership/revision verification is not implemented in PHASE 00.
