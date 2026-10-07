# ADR-006-Units-Coordinates: Units and coordinates

## Status
Accepted — user-approved PHASE 00 direction, 2026-10-07.

## Context
DtoB V1 targets a Controlled two-story CAD pipeline. The final master plan is authoritative; only PHASE 00 is authorized.

## Alternatives

| Option | Evaluation and disposition |
|---|---|
| Millimeters / explicit transforms | Selected: matches Controlled CAD and avoids implicit recentering; adapter conversion explicit. |
| Meters / Revit feet / inferred origins | Rejected: additional conversion or hidden alignment at core boundaries. |

## Decision
Core double-precision millimeters; right-handed XYZ, Z up, XY plan; radians; positive counterclockwise rotation about +Z. Affine transform applies scale then Z rotation then translation, composed outer-after-inner. Source/model origin transform is explicit. Tolerance profile is caller supplied, finite and positive; no hidden default. Revit boundary converts mm/feet using exactly 304.8 mm per foot.

## Consequences
Keep source unit metadata. No automatic alignment/UCS reading or geometry cleanup. Invalid/nonfinite values rejected; singular transforms may represent degenerate source data but are not inverted.

## Migration Impact
Initial foundation, no existing source or persisted schemas to migrate. Later major schema/unit/coordinate/identity changes require a new ADR. No future phase implementation.

## Review amendment — 2026-10-07

Finalized during review resolution. File modification times do not establish original ADR/code ordering. No commit-history ordering claim is made.

Definition-frame geometry is in mm. Entity Transform is the accumulated occurrence placement into mm WCS; SourceToModel maps mm WCS into model mm, so modelPoint = SourceToModel.Apply(entity.Transform.Apply(definitionPoint)). InsertionPath is outermost-to-innermost INSERT source handles, excluding the entity handle. Block definitions have empty paths and identity transforms. Arc/Circle Points contains exactly one center; angles are radians from +X, CCW about +Z in the definition frame. Mirroring is represented in Transform and reverses apparent world orientation, never rewrites source angles. Non-+Z OCS normals are unsupported in V1 and must be represented as CadUnsupported with raw normal and UNSUPPORTED_ENTITY diagnostic. Exact affine last-row checks are intentional algebraic constraints; geometric comparisons use explicit caller tolerance.

Final owner amendment (2026-10-07): preserve original SourceUnit (mm/cm/m/in/ft) and explicit SourceToMillimetersScale; geometry and transforms in IR have already been normalized to mm. Conversion helper is explicit; SourceToModel maps normalized mm WCS to model mm and must not scale a second time. ValidateStructure checks definitions have identity Transform/empty InsertionPath and matching OwnerBlockId; ValidateGeometry(tolerance) resolves each outer-to-inner path to INSERT occurrence prefix and compares composed local Placement to accumulated Transform. INSERT Placement is local to its parent; INSERT Transform includes its own Placement and ancestors.

Transform agreement uses LengthMm for translation entries and an explicitly supplied dimensionless MatrixCoefficientTolerance for linear coefficients; AngleRadians is never reused as a matrix coefficient budget. CAD geometry validation requires that optional profile component explicitly. Definition transforms are exact identity because they contain no occurrence placement.
