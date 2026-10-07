# PHASE 00 final review-fix checklist

Date: 2026-10-07, Asia/Seoul. Status: REVIEW_REQUIRED, ready for final Antigravity re-review. PHASE 01 has not started.

## Authority and actual Git history

DtoB_FINAL_MASTER_PLAN.md and PHASE_INDEX.md govern V1. The owner approved the PHASE 00 technical direction, deferred DWG SDK selection, prediction/provenance distinction and minimal connection UI. The later owner request approved deletion of FILE_INDEX.md/MASTER_PLAN.md and rewrites of four entry documents. Both removed documents are absent from the current committed baseline.

Branch phase/00-architecture; current base HEAD 2a5c85b74bd66cb783664609ca49f5bce4193f01. fc3633d/2a5c85b appeared externally during the work; Codex did not commit them. Final fixes remain modified/uncommitted, identified by source/DLL manifests rather than misrepresented as that base commit. No commit/push is authorized. Original staged-count/720c083 baseline statements are historical and are not the current state.

The on-disk review IDs and the owner's later pasted IDs differ. The report records both namespaces. No review finding is silently renumbered. ADRs were finalized/amended during review; original ADR/code chronological ordering is not proven by mtimes.

## Approved technical decisions

| Topic | Final PHASE 00 choice / constraint |
|---|---|
| Desktop/UI | C#/.NET 8 WPF, technical PID/PING status only |
| Analysis | In-process C# library boundary, explicit not implemented |
| Revit | net8.0-windows x64 isolated Revit 2025 adapter; no API outside adapter |
| IPC | Current-user named pipes, typed protocol 1, OS PID check, bounded workers/recovery |
| DWG reader | IDrawingReader plus synthetic fixtures only; ODA/RealDWG dependencies absent |
| Units | Core mm, original SourceUnit retained, explicit SourceToMillimetersScale |
| Coordinates | Definition frame -> accumulated INSERT Transform -> mm WCS -> SourceToModel |
| CAD schema | Strict v1, raw/effective metadata, ownership, local Placement, explicit unsupported diagnostics |
| BIM schema | Eight typed independent categories, mandatory provenance, prediction or documented manual confirmation for SourceDrawing |
| Identity | Canonical hex occurrence IDs retaining original handles; persisted BIM UUID |
| Validation | ValidateStructure separate from ValidateGeometry; caller-supplied length/angular/matrix budgets |
| Repository | One DtoB.sln, production graph asserted from solution entries |
| Build/test | SDK 8.0.416 pin, xUnit fixtures; alternate installed Visual Studio MSBuild/Roslyn + VSTest verified |

ADRs 001–010 evaluate alternatives. 004/006/008/009 record final review amendments. ADR-005 records dated owner approval to defer product/licensing/runtime/fidelity resolution until before later actual reader/viewer work.

## Final resolution checklist

- [x] Read independent review completely and incorporate the owner's later request.
- [x] Preserve phase scope; no parser, recognition, project management, final viewer or BIM generation.
- [x] Finalize ADR alternatives and approved reader deferral; correct history/encoding claims.
- [x] Recover accept/request IO; dispose failed connections; bounded worker supervision and Faulted shutdown.
- [x] Test at least fifty connect/drop cycles, unexpected restart and forced unrecoverable fault.
- [x] Bound idle-client blocking; verify legitimate PING with one and four idle clients.
- [x] Enforce definition identity/path/owner and actual INSERT path/composition invariants.
- [x] Test five negative coordinate cases and nested/mirrored Arc fixture with unsupported normal diagnostics.
- [x] Preserve original non-mm source units; test explicit conversion/serialization and wrong scale.
- [x] Remove mutable IsSemanticPrediction; derive evidence requirement from provenance/manual action.
- [x] Remove the open Parameters bag; retain ReviewStatus as stored state only.
- [x] Separate structural/tolerance-aware geometry APIs; test open boundaries/near-zero geometry.
- [x] Resolve production projects from DtoB.sln; no arbitrary recursive discovery/Explorer dependency.
- [x] Time-bound missing-PID regression and verify meaningful ADR decision/rejection reasons.
- [x] Remove unused adapter WPF/Revit.Core references; gate the three actual MSB3277 identities.
- [x] Add reproducible configurable build/host/deployment/snapshot scripts and current hash evidence.
- [x] Verify Core-only Release build without proprietary references.
- [x] Verify complete solution Release build: zero errors, three documented warning identities.
- [x] Execute every automated test: 56 passed, zero failed/skipped, .NET 8.0.22 runtime.
- [x] Generate actual final-code Desktop/Revit PONG, disconnect, normal shutdown and restart evidence.
- [x] Verify request correlations, both STOPPED logs, current/deployed hashes and final source manifests.
- [x] Reject invalid verification arguments manually; record limits of UI/exit-code evidence.
- [x] Supersede stale evidence and avoid unnecessary absolute user paths in new public evidence.
- [x] Inspect current Git diff, including new files; git diff --check clean.
- [x] Rewrite report with final facts and one resolution entry per HIGH/MEDIUM/fixed LOW issue.
- [ ] Remote CI/final commit: not performed, explicitly prohibited; disposition requires owner/reviewer.
- [ ] Independent Antigravity phase approval: pending re-review.

## Environment and verification limits

Actual installed host: Revit 2025.4, file 25.4.50.35, product 20260410_1515(x64); installed API runtime configs declare .NET 8. The user reported security blocking of dotnet.exe, which became unavailable. Existing Visual Studio MSBuild 17.14.23/Roslyn with the intact SDK 8.0.416 files and VSTest 17.14 completed the final builds/tests. No security exclusion, renamed executable, quarantined restoration or new SDK install occurred. Targets/global.json remain unchanged. Normal Dotnet CI is configured but unexecuted.

Actual host scripts use a configurable installation path and do not modify a model. Only PING/lifecycle are verified. SDK product selection, real-DWG fidelity, unknown/unitless units, arbitrary OCS, hostile same-user saturation, cross-document source reconciliation, advanced geometry and model generation remain unsupported/limited as detailed in PHASE_00_REPORT.md.

All locally verifiable PHASE 00 technical criteria are supported by re-review-final-ping/re-review-final-restart evidence. Both build-time source manifests exactly match final-source-snapshot.json; all 56 tests passed in both final Release gates. This does not grant overall phase completion. Stop for Antigravity re-review; do not begin PHASE 01.
