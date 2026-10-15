# PHASE 03 Architecture Housekeeping Report

Date: 2026-10-14 (Asia/Seoul)

## Status

Housekeeping complete; ready for a later isolated **03A Reader Capability Spike**. This is not PHASE 03 completion or backend acceptance. Antigravity's pre-implementation verdict is APPROVED_FOR_CAPABILITY_SPIKE; the three required housekeeping dispositions are recorded below. Stop before installation/spike/product implementation until owner continuation.

Branch: `phase/03-dwg-viewer` (historical name). HEAD: `142100deb62e8eb3692ded84a95962548925731f`. No commit or push performed.

## Review resolutions

### Issue 1 — HIGH — Authoritative phase scope

Updated master/index/specs/architecture/instructions and entry documents to:

`managed DWG/DXF -> raw extraction -> RawDrawingSnapshot -> factual QA -> minimal diagnostic Viewer`

PHASE 03 owns factual extraction; PHASE 04 owns unit/frame/OCS-WCS normalization, effective-data policies, cleanup and analysis preparation into normalized CAD IR; PHASE 05+ owns intelligence. tasks/PHASE_03.md specifies 03A–03H and factual Exit Criteria. Full Viewer graphics no longer gate backend selection. PHASE 04 documentation was aligned; no PHASE 04 implementation began.

### Issue 2 — MEDIUM — Raw boundary and draft ADR sequence

Replaced the untracked/unaccepted Viewer-backend ADR-015 draft with **ADR-015-Raw-CAD-Extraction-Boundary.md**. It defines the additive conceptual `IRawDrawingReader -> RawDrawingReadResult -> RawDrawingSnapshot` boundary while retaining `IDrawingReader -> DrawingReadResult -> CadDocument` as the normalized PHASE 04 boundary.

Native units, field presence/default provenance, original handles, definitions versus occurrences and copied vendor-independent data are explicit. Snapshot is derived data, not project/domain truth. No .dtob schemaVersion 2 or CAD IR v1 change. ADR-016 Reader Backend is reserved pending empirical capability evidence; ADR-017 Reader Lifecycle is reserved. Neither was created or accepted. Accepted ADR-001–014 remain unchanged and numbered identically.

### Issue 3 — MEDIUM — MINSERT scope

V1 occurrence expansion is explicitly unsupported. Preserve the original base handle and array row/column/count/spacing/placement facts, emit source-linked UNSUPPORTED_ENTITY, and continue safely. No synthetic cell IDs, flattening or Explode; no ADR-007 change. This policy is aligned in task, ADR and checklist. Runtime retention/continuation/no-cell-ID regression remains required during later authorized spike/implementation; no parser exists or new runtime test is claimed here.

## Changed files

- DtoB_FINAL_MASTER_PLAN.md; PHASE_INDEX.md; tasks/PHASE_03.md; tasks/PHASE_04.md
- ARCHITECTURE.md; CODEX_INSTRUCTIONS.md; README.md; START_HERE.md
- docs/adr/ADR-015-Raw-CAD-Extraction-Boundary.md (replaces the unaccepted Viewer draft)
- docs/status/PHASE_03_CHECKLIST.md (targeted dispositions; earlier design history retained)
- docs/status/PHASE_03_ODA_EVALUATION.md (historical/inactive register; corrected ADR references)
- docs/status/PHASE_03_HOUSEKEEPING_REPORT.md

reviews/PHASE_03_REVIEW.md is owner-provided input, not edited. The previous Viewer draft and pre-housekeeping checklist are preserved under ignored artifacts/phase03/housekeeping-20261014. Historical status reports, fixtures and evidence were not rewritten.

## Verification

Current housekeeping verification: **4/4 passed**, zero failures/skips, using existing unchanged Release test binary with current documentation/project files:

- FoundationDocumentTests.ForbiddenDependencyMutationIsRejected
- FoundationDocumentTests.AdrsContainEvaluatedAlternativesAndValidUtf8
- FoundationDocumentTests.AllProductionProjectDirectionsAreEnforced
- ContractTests.NoAutodeskDependencyLeaksIntoCoreOrAnalysis

Evidence: artifacts/phase03/housekeeping-20261014/housekeeping.trx. No new tests added: no product feature was implemented. Earlier Core/full Release builds and **126/126** PHASE 00/01/02 tests belong to the pre-rescope baseline (artifacts/phase03/baseline-20261014), not a new housekeeping build. No complete solution rebuild is claimed for this documentation-only task.

Git diff reviewed for scope, phase boundaries and historical preservation. git diff --check passed after removing two extra EOF blank lines. SHA-256 comparison of 93 protected accepted-ADR/report/evidence/fixture files passed. Production source/tests/build/package/project/dependency files have no Git differences. No ACadSharp production reference or local NuGet cache, and no spikes directory. No package installation or spike execution performed.

## Known limitations and next boundary

ACadSharp is an evaluation candidate, not an accepted production backend. Real controlled DWG/DXF fidelity, handles, presence/default evidence, read-only leases, cancellation/disposal and deployment remain UNVERIFIED. Diagnostic Viewer/raw contracts are architectural plans only. No reader, Viewer, normalization or semantic code added. No Revit host verification is required by these documentation-only changes.

The documentation is ready for Antigravity/owner inspection and later isolated 03A continuation. Stop here; no PHASE 04, package installation, spike, commit or push.
