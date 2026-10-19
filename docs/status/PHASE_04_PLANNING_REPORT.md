# PHASE 04 Planning Report

Date: 2026-10-15

## Status

**REVIEW_REQUIRED — architecture proposal only.** PHASE 04 implementation has not started. This is not a PHASE 04 completion report or build/test gate result.

Deliverable: [PHASE_04_IMPLEMENTATION_PLAN.md](../plans/PHASE_04_IMPLEMENTATION_PLAN.md).

## Work completed

- Read the complete owner attachment and required repository specifications, PHASE 03 release/final-gate evidence, 03F/03G/03H reviews and ADR-015/016/017.
- Inspected Raw/Dxf/Core/Geometry/Analysis, dormant CAD v1, ADR-006/007/008 and downstream PHASE 05 requirements.
- Verified released tree and current checkout identity. Branch remains `phase/03-cad-data-extraction`; HEAD `c2755f04bff11e470e6482cf3aabdc9673acc725`; tree `0ef1ca4b403dff5cd1e85958ca340d6fd0ae5cd4` equals `v0.4-phase03` / released main tree. Release merge commit is `76ff6e0d8ccac797ba60d8c5657922c796a0e8c9`.
- Produced all 26 required planning sections and eight 04A–04H subphases, each with purpose, inputs, outputs, owned contracts, exclusions, tests, gate and ADR requirement.
- Studied primary Autodesk DXF documentation for AAA/OCS, INSERT/BLOCK, units, MINSERT, styles, bulge and MTEXT; citations are next to the proposed rules/math.
- Proposed additive CAD v2 in existing Cad plus one pure Normalize library; preserve existing v1/BIM/Project and keep Raw/Dxf independent.
- Identified planned ADR-018/019/020. No new ADR has been created or accepted.
- Added a historical-context/planning-gate notice to PHASE_04 task without rewriting earlier content.

## Changed files

1. New `docs/plans/PHASE_04_IMPLEMENTATION_PLAN.md`.
2. Additive `tasks/PHASE_04.md` notice.
3. New `docs/status/PHASE_04_PLANNING_REPORT.md`.

Existing untracked PHASE 03 Git/PR/release reports and spike files belong to the prior workspace state; this task preserves them. No product, test, fixture, solution/project, package, accepted ADR or review text was changed. No branch creation/switch, staging, commit, push or remote mutation.

## Verification

- Captured SHA-256 baseline for 373 existing tracked/untracked visible files under ignored `artifacts/phase04/planning-20261015/before.json` before document edits.
- Compared every baseline file: only `tasks/PHASE_04.md` changed; its complete original text remains an exact prefix. All other baseline file hashes match, including historical evidence and source/test/fixture files.
- Verified numbered sections 1–26 and eight subphases with all eight required attributes.
- Reviewed the complete new documents and additive task diff; checked UTF-8, whitespace and local Markdown links. `git diff --check` passed. The existing Git CRLF conversion warning for the task document is informational; no fixture conversion occurred.
- No new product tests, builds, Revit host or Desktop checks were run for this documentation-only task. **773/773 and Release PASS are PHASE 03 release evidence, not newly executed PHASE 04 results.**

## Known limitations / unresolved decisions

- Additive CAD-v2 schema and identity policy require owner/Antigravity review and accepted ADRs before code.
- MINSERT spacing/scale/OCS composition is explicitly proposed; independent analytic fixtures must validate it before the 04E capability gate.
- Initial block-unit mismatch, ambiguous annotation/default contexts and complex presentation remain explicit unsupported/refusal cases pending evidence, not guessed conversions.
- Dormant Analysis takes v1; future PHASE 05 adoption of v2 is a separate explicit change, not automatic compatibility.
- Existing QA manifest test covers all controlled files against the historical 86-set. Plan requires a narrowly scoped legacy-plus-new union coverage assertion when fixtures are added; no test was weakened or changed now.
- No normalized viewer is included by default. Existing 03H Raw diagnostic viewer and LOW-01 remain unchanged.

## Failure cases considered

The proposal covers missing/malformed/unsupported units; invalid normals/scale and overflow; missing/recursive block targets; oversized arrays; absent provenance; unresolved styles; degenerate geometry; ambiguous annotation frames and cancellation. These are planned tests/policies, not implemented fixes or verified runtime behavior.

## Next step / stopping point

Return the implementation plan for owner architecture review and independent Antigravity review. After explicit approval, finalize required ADRs before affected implementation. **Stop now before 04A.** No production work or later phase is authorized by this report.
