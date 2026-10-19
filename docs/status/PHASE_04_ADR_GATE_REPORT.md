# PHASE 04 — Architecture Freeze / ADR Gate Report

Date: 2026-10-15

## Status and scope

**ADR_REVIEW_REQUIRED.** Architecture documents only. PHASE 04 production implementation, including 04A, has not started. Three ADRs are **Proposed**, not Accepted. Plan approval is not ADR acceptance or implementation authorization.

## 1. Released PHASE 03 baseline

PR #6 merged; main merge/tag commit `76ff6e0d8ccac797ba60d8c5657922c796a0e8c9`; tag `v0.4-phase03`; tree `0ef1ca4b403dff5cd1e85958ca340d6fd0ae5cd4`. Local origin/main also resolved to that commit when inspected. Historical release evidence records 773/773 tests, Core/full Release, 03G/03H and 86/86 source-byte audit PASS. These are released PHASE 03 results, not new tests/builds for this documentation gate.

## 2. Plan PASS evidence

Read [independent plan review](../../reviews/PHASE_04_PLAN_REVIEW.md) completely; authoritative verdict **PHASE_04_PLAN_PASS**. Review remains unchanged as independent evidence. Read the complete plan, prior planning report and PHASE 04 task; inspected actual Geometry/Core/Analysis contracts and csproj dependencies. Reconfirmed AAA/INSERT/BLOCK/MINSERT against primary Autodesk references linked in ADRs, plus independent arithmetic derivations.

## 3. Current branch / preservation

Initial branch `phase/03-cad-data-extraction`, HEAD `c2755f04bff11e470e6482cf3aabdc9673acc725`, had exactly the release tree. After checking status/ref/tree identities and absence of the requested branch, ran `git switch -c phase/04-cad-normalization v0.4-phase03` successfully.

Current branch: **phase/04-cad-normalization**. HEAD remains release commit `76ff6e0d8ccac797ba60d8c5657922c796a0e8c9`. Uncommitted plan/task work and untracked historical PHASE 03 reports/spikes were preserved. No force checkout, stash, deletion, staging, commit or push.

## 4. Changed documentation files

1. [ADR-018-CAD-IR-Normalization-Boundary.md](../adr/ADR-018-CAD-IR-Normalization-Boundary.md) — new, Proposed.
2. [ADR-019-Coordinate-Normalization-Transforms.md](../adr/ADR-019-Coordinate-Normalization-Transforms.md) — new, Proposed.
3. [ADR-020-Occurrence-Identity-and-Expansion.md](../adr/ADR-020-Occurrence-Identity-and-Expansion.md) — new, Proposed; filename includes **and** as owner requested.
4. [PHASE_04_IMPLEMENTATION_PLAN.md](../plans/PHASE_04_IMPLEMENTATION_PLAN.md) — integrated review amendments; preserve initial planning history with clear current gate/branch notices.
5. [PHASE_04.md](../../tasks/PHASE_04.md) — additive architecture-freeze notice; previous content preserved.
6. This report.

Prior planning report, accepted ADRs and independent review text are not rewritten.

## 5–9. Review findings disposition

| Finding | Severity | Documentation resolution | Required future verification |
| --- | --- | --- | --- |
| H-01 | HIGH | Plan sections 6/7/20/23 and ADR-018 isolate typed CadIrJson/CadContract major 2 validation from Core.Contract.Version/IrJson/v1 DTO validation. No legacy version gate changes. Analysis v1 untouched through PHASE 04; PHASE 05 begins explicit migration. | CadIrJsonV2Tests: v2 serialize/deserialize/re-serialize identical bytes, strict invalid/unsupported-major handling, legacy 1 success/legacy 2 rejection; no v2 call path through legacy major 1 restriction. |
| M-01 | MEDIUM | Plan sections 7/12/20 and ADR-019 require owned private matrix copies on construction/deserialization and fresh copies on export. No public writable internal array, alias between occurrences/definitions or mutable state after publication. Existing Geometry unchanged. | TransformImmutabilityTests: mutate source arrays, input/exported Transform3 and occurrence projections; prior state, other occurrences/definition, bounds and serialization unchanged. |
| M-02 | MEDIUM | Plan sections 16/20 and ADR-019 define exact transformed Circle/Arc extrema, generalized to tilted 3D planes, signed sweep/wrap/mirror and shear. Includes hand-calculated radius 2/R45°/S2,3 half extents sqrt26 and partial-arc oracle. No authoritative tessellated bounds. | NonuniformCurveBoundsTests with hand oracle, mirrors, arc endpoints/interior extrema/wrap/tilt/shear. |
| M-03 | MEDIUM | Plan sections 20/23, all ADR governance sections and task notice use explicit Accepted prerequisites. All three authored now as Proposed. 04A cannot implement a later governing decision still Proposed. | Documentation gate now; independent ADR review/acceptance before affected code. |
| L-01 | LOW | Plan sections 14/17/20 and ADR-020 specify one monotonic TotalOccurrencesAllocated per Normalize session. Count root/nested INSERT and every MINSERT cell across all repeated/nested arrays; no reset/refund or synthetic cell-container. Reserve before allocation; >100,000 refuses publication deterministically. | MinsertResourceExplosionTests: individually legal nested arrays exceed global cap; exact/one-over and no-reset/refund; no document after resource exhaustion. |

All five are addressed **at the architecture/document level**. Their future code/test obligations are not claimed implemented or verified.

## 10. ADR-018 summary

Pure Raw->Normalize->CAD2, immutable source evidence, one new Normalize library and existing Cad.V2 namespace, independent typed codec/version ownership, compact provenance/partial publication and Revit-independent document. Explicit current/proposed dependency graph was checked against csproj reality. No direct CAD->Revit output shortcut or permanent dual-model strategy.

## 11. ADR-019 summary

Retains mm/radians/RH/+Z/CCW, actual row-major column-vector Then semantics. Specifies defensive private transform ownership, units/angles/MTEXT exception, AAA/frame/elevation behavior and base-compensated `A*T(i)*R*S*T(-b)`. Re-derives point placement with +Z/+Y hand examples. Defines exact affine full-circle bounds and source-parameter extrema membership for arcs, plus central numeric policy and invalid/near-zero handling. No Geometry code modified.

## 12. ADR-020 summary

Shared definitions and source INSERT templates, typed real-handle occurrence paths/cells, domain-separated framed cad2 identities, deterministic DFS/row-major order and array-spacing matrix. Active-path cycles, depth 32 / cells 10,000 / session occurrences 100,000 / derived geometry 250,000 / diagnostics 5,000, explicit no-reset counter, bounded cancellation and occurrence-context style evidence.

## 13. CAD v2 migration strategy

PHASE 04: introduce v2 additively, leave legacy Core/CAD1/BIM/Project validation and dormant Analysis-v1 boundary untouched. PHASE 05: begin with explicit Analysis API/validation/test migration to v2. No default temporary v1↔v2 converter; no silent JSON upgrade. Future v1 deprecation/removal requires an explicit consumer/fixture migration gate. CAD1/CAD2 are not permanent parallel first-class domain models.

## 14. No-production-code verification

Captured hashes for **376** existing tracked/untracked visible files before branch creation under ignored `artifacts/phase04/adr-gate-20261015/before.json`. Final comparison found only the plan and additive task changed; the remaining **374** files retained their hashes, including protected production/tests/fixtures/solution/projects/packages, accepted ADRs, historical reports/spikes and independent review. Inventory comparison found exactly four new files: the three ADRs and this report; no missing baseline files or unexpected additions.

No files changed/created in Cad, Raw, Dxf, Normalize, Geometry, Core, Analysis, Desktop or Revit production trees. No Normalize project directory exists. No project/solution/package/test created or edited. No new automated tests, builds or host verification run: this is documentation/architecture verification only.

## 15. Document consistency verification

Re-read amended plan and three ADRs. Checked CAD1/2/version ownership/Analysis migration, immutable storage, affine bounds, global budget and ADR timing across sections. All 26 planning sections and 04A–H/eight attributes retained. Plan links use exact owner-requested ADR filenames. Three new ADR numbers are unique and each has exactly `Status: Proposed`; no acceptance claimed.

`git diff --check`, strict UTF-8 decode, whitespace and local Markdown link checks pass. Compared before/after file inventories and SHA-256 values; only allowed documentation changed. Independent review/prior planning report unchanged; full original task text remains a prefix. No staged files. Evidence: ignored `artifacts/phase04/adr-gate-20261015/document-verification.json`; plan diff retained alongside it for document audit. Existing task line-ending conversion warning is not a fixture edit.

## 16. Unresolved architecture questions / limitations

Independent ADR review/acceptance is pending; no new architectural decision is Accepted. No additional blocking design question is being deferred to an unapproved production workaround. Runtime correctness of MINSERT order/spacing, transform ownership, numeric extremes and curve bounds requires the named future tests, not document claims. Concrete code layout/fixture bytes/golden digests remain implementation details to validate under the accepted policies.

Complex annotations, contradictory block units and unsupported source features remain explicit limits in the plan. LOW-01, Raw/Dxf, 03H Raw diagnostic viewer and all legacy source fixtures remain unchanged. No normalized viewer, semantic recognition, BIM or Revit implementation.

## 17. Exact next gate

Independent Antigravity review of all three **Proposed ADRs** and amended plan. Accepted prerequisites:

| Stage | Required Accepted ADRs |
| --- | --- |
| 04A | 018; coordinate/identity detail still waits for its governing acceptance |
| 04B | 018 + 019 |
| 04C / 04D | 019 |
| 04E | 019 + 020 |
| 04F | 018 + 020 where occurrence-context inheritance applies |
| 04G | All relevant 018/019/020 |
| 04H | Verify entire accepted architecture |

Owner authorization is still required for subsequent implementation; neither writing ADRs nor plan PASS starts 04A. Stop now. No commit/push.

ADR_REVIEW_REQUIRED
