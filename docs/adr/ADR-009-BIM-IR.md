# ADR-009-BIM-IR: BIM IR v1 and prediction metadata

## Status
Accepted — user-approved PHASE 00 direction, 2026-10-07.

## Context
DtoB V1 targets a Controlled two-story CAD pipeline. The final master plan is authoritative; only PHASE 00 is authorized.

## Alternatives

| Option | Evaluation and disposition |
|---|---|
| Typed independent BIM IR | Selected: domain source of truth, persistent provenance and prediction lineage. |
| Revit types / unrestricted property bags | Rejected: Autodesk coupling or untyped geometry and relationship semantics. |

## Decision
Versioned Revit-independent Level/Grid/Wall/Column/Door/Window/Floor/Room contracts with mandatory source/provenance. Optional Prediction metadata carries finite confidence in [0,1] and nonempty evidence. Non-predicted/manually confirmed domain objects need no artificial confidence. Semantic prediction results always require Prediction even when subsequently confirmed.

## Consequences
Manual objects remain first-class. Provenance origin records SourceDrawing/Manual and supports manual evidence. Level/host references validated. No prediction, mapping or generation algorithm.

## Migration Impact
Initial foundation, no existing source or persisted schemas to migrate. Later major schema/unit/coordinate/identity changes require a new ADR. No future phase implementation.

## Review amendment — 2026-10-07

Finalized during review resolution. File modification times do not establish original ADR/code ordering. No commit-history ordering claim is made.

Superseded checkpoint: prediction lineage boolean replaced by explicit provenance/manual action below. The open Parameters bag is removed; future typed output parameters require their own schema decision. ReviewStatus records object review state; no review workflow is implemented.

Final owner amendment: remove mutable IsSemanticPrediction. SourceDrawing provenance requires Prediction unless a documented ManualConfirmation (actor, action description, timestamp) is present. Manual domain origin needs no artificial prediction. A plain status change cannot waive evidence. Provenance changes are auditable transport data, not authorization; no cryptographic history is claimed.
