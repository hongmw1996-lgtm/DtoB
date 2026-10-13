# PHASE 02 REPORT

## Status

**REVIEW_REQUIRED — M-01, M-02 and M-03 implemented and verified; ready for Antigravity final re-review.** Updated 2026-10-13 (Asia/Seoul). This report describes the post-review-fix source tree, not the original 111-test implementation checkpoint. Independent acceptance remains pending; this is not a claim that Antigravity has approved the fixes. No PHASE 03, commit or push.

Branch: `phase/02-dwg-upload`. Base: `be27e7d712332506bc0b5d51b70720ab8fff8a86`. Original PHASE 02 implementation and these fixes remain in the uncommitted working tree. The owner-authored `reviews/PHASE_02_REVIEW.md` was read and preserved.

## Final Review Fixes

Verification of the actual source found all three MEDIUM fixes absent. They were implemented in this review-resolution pass.

### M-01 — Reparse Scope

- **Severity / issue:** MEDIUM; the previous `AssetPaths.RejectReparse` traversed every ancestor up to the volume root. A project beneath a junction could not save, even with no drawings; its managed files could appear unreadable.
- **Exact code change:** `RejectReparse(path, projectDirectory)` normalizes both paths, enforces project-relative containment, and checks each component from the final path up to the project directory **exclusive**. `Resolve` supplies the actual opened manifest's directory. `ValidateManifestPath` checks the manifest itself using its parent as boundary. Register and asset-copy paths supply the same boundary for destination checks, including checks after directory creation. Reparse points within the asset tree and at the final file remain rejected; the project directory and ancestors are allowed.
- **Before / after:** empty Save/Open, Add and asset-aware Save As previously failed beneath a junction ancestor. They now succeed while canonical UUID-relative paths, containment and managed-tree reparse rejection remain enforced. No cloud-sync compatibility or race-proof filesystem guarantee is claimed.
- **Junction-parent regression test:** `JunctionParentAllowsEmptySaveOpenRegistrationAndSaveAs` creates an actual Windows directory junction; verifies zero-drawing Save/Open, Add/Save/reopen and Save As beneath it, with identity preserved and Ready/clean state.
- **Asset-tree reparse rejection test:** `ReparseWithinAssetTreeIsRejectedWithoutWritingOutside` runs twice, at the ProjectId asset root and at `drawings/<DrawingId>`. Resolve/import reject, Open reports ManagedUnreadable, and the outside target remains empty. The existing unsafe-path regression also remains green.
- **Changed files:** `packages/DtoB.Project/DrawingMetadata.cs`, `DrawingFileService.cs`, `ProjectPersistence.cs`; `tests/DtoB.Tests/Phase02ReviewFixTests.cs`, `DrawingManagementTests.cs`; ADR-013, guide, checklist and this report.
- **Verification result:** PASS in the 126-test Release gate. Junction tests use `TestJunction` / PowerShell `New-Item -ItemType Junction` instead of privileged `Directory.CreateSymbolicLink`, resolving L-04's test requirement. This is automated real-filesystem verification; an additional junction-specific WPF walkthrough was not performed.

### M-02 — SourceFilePath I/O

- **Severity / issue:** MEDIUM; normal Open/Refresh probed untrusted original paths synchronously, and selection's `CanRevealSource` performed `File.Exists`, potentially contacting UNC hosts on the UI thread.
- **Exact code change:** `DrawingFileService.Probe` resolves/probes **only ManagedFilePath** and returns SourceUnchecked. `SourceAvailability` is separate runtime state. `ShellViewModel.CanRevealSource` reads cached SourcePresent instead of calling the filesystem. `ProjectSession.Open` resets runtime state, and normal Refresh preserves an explicitly obtained source observation without probing it.
- **Zero source I/O:** `ProjectSession.Open` performs **zero SourceFilePath filesystem I/O**, for local and UNC paths. Syntactic path validation and displaying the provenance string do not access that path. The same no-source-I/O rule applies to normal Refresh and row selection. Managed availability still uses metadata/access probes with no drawing-content reads.
- **Explicit/asynchronous check:** the selected-row **Check Source** command calls `RefreshSourceStatusAsync`; `ProbeSourceAsync` runs the metadata/access probe through `Task.Run` and a cancellable wait. It produces cached SourcePresent, SourceMissing or SourceUnreadable. Generation/path checks reject stale results; cancellation leaves SourceUnchecked. State is neither serialized nor dirtying. Reveal Source is enabled only after SourcePresent and rechecks existence asynchronously on explicit invocation. A running OS probe cannot itself be forcibly cancelled; cancelling stops waiting and prevents its result from being applied.
- **No fallback:** missing/unreadable managed files remain missing/unreadable even when the original exists. Verify and Save As consume managed paths; they never substitute SourceFilePath. Original input is read only during an explicitly requested Add/Replace or source-location action.
- **Tests added/updated:** `OpenRefreshAndSelectionNeverProbeLocalOrUncSource` (local + UNC), `MissingManagedAssetNeverFallsBackToPresentSource`, `ExplicitSourceCheckPublishesOnlyRuntimeStatus` (present/missing/unreadable), `PendingSourceCheckDoesNotBlockCallerAndRejectsCancelledOrStaleResult` (cancel + stale). Source-probe fakes throw if normal paths touch the original; UNC tests use no real network endpoint. Existing no-read/hash tests now assert every normal probe targets the managed path. The missing-source test explicitly invokes Check Source before expecting SourceMissing.
- **Changed files:** `DrawingFileService.cs`, `DrawingMetadata.cs`, `ShellViewModel.cs`, `MainWindow.xaml`, `MainWindow.xaml.cs`; both drawing test files and documentation.
- **Verification result:** PASS in the 126-test gate. Actual Release UI opens a fixture with UNC provenance as SourceUnchecked; explicit local checks yield SourcePresent/SourceMissing and affect Reveal Source correctly. Zero-I/O proof comes from deterministic dependency tests and code inspection, not a claimed network capture.

### M-03 — Drawing Status Visibility

- **Severity / issue:** MEDIUM; filenames alone were shown in the drawing list, and Open/Refresh success text hid missing assets until a row was selected.
- **Exact code change / per-row status:** `DrawingRow` and `DrawingRows` expose cached `DrawingStatus.Display`; the ListBox item template renders filename plus wrapped availability/source/ReaderPending/integrity text. `DrawingSummary` groups cached states by managed availability and source-check state, with SizeChanged/known HashMismatch counts when present.
- **Aggregate Open/Refresh summary:** `OpenPathAsync` and `Refresh_Click` show `DrawingSummary`. Actual verification displayed `2 drawings: 1 Ready, 1 ManagedMissing, 2 SourceUnchecked` immediately after Open and normal Refresh.
- **Missing visible without selection:** the Missing-UNC.dwg row displayed ManagedMissing while the metadata panel still said `Select a drawing to inspect metadata.` No selection or attempted Save was necessary.
- **No hashing/content reads introduced:** row rendering and summary use runtime values only. No reader, hash or source filesystem dependency is called by those properties. Production Open/Refresh tests still reject all managed-content read APIs and hash calls for small and advertised 8 GB assets.
- **Selection:** ListBox binds its SelectedValue to the underlying DrawingFile. Refresh captures the selected DrawingId before replacing ItemsSource, then restores selection after the binding can clear it. Registration refreshes the rows before selecting the returned drawing. This also addresses L-08.
- **Tests added:** `RowsAndSummaryExposeProblemsWithoutSelectionOrContentReads` (missing/unreadable/size mismatch) and `RefreshRestoresSelectionAfterItemsSourceBindingClearsIt` (refresh, replacement revision, removal). The latter simulates a binding resetting selection during the ItemsSource notification; actual WPF selection retention was additionally observed.
- **Changed files:** `ShellViewModel.cs`, `MainWindow.xaml`, `MainWindow.xaml.cs`, `Phase02ReviewFixTests.cs`, documentation.
- **Verification result:** PASS in the 126-test gate and actual post-fix Release UI. Row text, aggregate summary, no-selection missing visibility and selection retention were observed directly.

## Implemented

.NET 8 WPF drawing management within the existing shell: file dialog/shared file-drop registration, immutable managed revisions, SHA-256 at explicit bounded operations, separate drawing/revision UUIDs, duplicate policy, Replace and registration-only Remove, strict project schema v2 with clean v1 migration, manifest-last asset-aware Save As, cancellation and expected-error handling. Post-review fixes add scoped reparse checks, explicit asynchronous source checks and per-row/aggregate statuses. Existing project commands, local Settings/Recent/logging and separate Connection Verification remain.

## Not Implemented

DWG/DXF entity parsing, CAD SDK dependencies, viewer, unit inference/conversion, recognition, AI, BIM/Revit generation, PHASE 03, Xref collection, history browser, revision diff, garbage collection, background monitors, database, cloud, accounts and autosave. BasicHeaderOnly is not full CAD validity.

## Changed Files

- Project: `packages/DtoB.Project/ProjectPersistence.cs`, `DrawingMetadata.cs`, `DrawingFileService.cs`.
- Desktop: `apps/DtoB.Desktop/App.xaml.cs`, `DtoB.Desktop.csproj`, `MainWindow.xaml`, `MainWindow.xaml.cs`, `ShellViewModel.cs`.
- Tests: `tests/DtoB.Tests/DtoB.Tests.csproj`, `ProjectShellTests.cs`, `DrawingManagementTests.cs`, **new review-fix file `Phase02ReviewFixTests.cs`**.
- Fixtures: `datasets/controlled/phase02/README.md` and owner-provided byte-identical DWG/DXF files.
- Decisions/docs: ADR-013, ADR-014, `docs/PHASE_02_DRAWINGS.md`, `docs/status/PHASE_02_CHECKLIST.md`, this report, current-phase prefixes in ARCHITECTURE.md/CODEX_INSTRUCTIONS.md.
- Historical evidence retained: `docs/status/evidence/phase02/` and `docs/status/evidence/phase00-review/phase02-20261008-final/`.
- **New post-fix evidence:** `docs/status/evidence/phase02-review-fixes/` (build logs, TRX, source snapshot, binary/test summary, manual observations, manual manifest snapshot and Desktop log). Raw workspace outputs: `artifacts/phase02-review-fixes-20261013/`.

No new production project/dependency/solution restructuring. Core/Geometry/CAD IR/BIM IR/Analysis/IPC/Revit implementations and earlier ADRs are unchanged. The independent review is not edited by Codex. The evidence manifest JSON is a record, not a portable project with all assets included.

## Tests Added

The new `Phase02ReviewFixTests` contributes **15 executed cases**: 1 junction-parent workflow, 2 reparse asset boundaries, 2 local/UNC no-source-I/O paths, 1 no-fallback case, 3 explicit source statuses, 2 asynchronous cancellation/stale results, 3 unselected-row status cases, and 1 selection-restoration regression. Exact methods and assertions are listed under each review fix above.

Existing DrawingManagementTests were updated for SourceUnchecked/explicit source checks, managed-only probe counts and unprivileged junction creation. Original 33 PHASE 02 cases remain: actual controlled DWG/DXF registration, synthetic invalid headers, identity/bytes/hash/size roundtrip, migration/schema failures, duplicates/Replace/Remove, safe save failure/cancellation, locked/missing inputs, moved projects, path rejection, explicit SHA-256 and modified-content mismatch, stale/cancelled integrity, cleanup warnings and normal Save without hashing. The real metadata probe's access streams reject every content-read API. Small/8 GB metadata tests follow the same Open/Refresh path; no timing threshold or actual multi-GB benchmark is claimed.

## Test Results

| Checkpoint | PHASE 00 | PHASE 01 | Original PHASE 02 | Review-fix cases | Total / result |
|---|---:|---:|---:|---:|---|
| Historical implementation, 2026-10-08 | 56 | 22 | 33 | 0 | **111/111 PASS**, before M-01/M-02/M-03 |
| **Post-review-fix final source, 2026-10-13** | **56** | **22** | **33** | **15** | **126/126 PASS**, 0 failed, 0 skipped |

The new TRX is `docs/status/evidence/phase02-review-fixes/tests.trx`; class counts and binary hashes are in `verification-summary.json`. Do not use the original 111-test checkpoint as evidence for these fixes. All PHASE 00/01 automated regressions pass in the new 126-case run, including dependency directions, geometry/contracts, IPC, project persistence, Settings/Recent/logging and unsaved-state transitions.

## Build Result

**Core-only Release: PASS. Full Solution Release: PASS.** Both were rebuilt by the existing gate, which exited 0:

```powershell
./scripts/Verify-Phase00Foundation.ps1 -RevitInstallDir 'D:\프로그램\Revit 2025' -Toolchain VisualStudio -EvidenceDirectory 'artifacts/phase02-review-fixes-20261013'
```

This host still has no usable dotnet.exe. The already-approved Visual Studio MSBuild 17.14.23/Roslyn, installed SDK files 8.0.416, VSTest 17.14 and runtime 8.0.22 route was used. SDK-executable resolution diagnostics appear in logs, but native MSBuild/gate results succeeded. No security settings, exclusions or quarantined-executable workaround was used. Core has no compiler warnings; full build passes the unchanged gate for only the three pre-existing Revit MSB3277 identities (Microsoft.VisualBasic, System.Drawing, WindowsBase).

Tested source snapshot SHA-256: `06F338805E811A56C3531EDC55798905A2FE7BC58427BAD5EA1734C6E91C6C32`. Recomputed after manual verification: identical. Later edits are reporting/evidence only. Source snapshot covers implementation/build/test source; evidence/fixture hashes are separately recorded. The exact Desktop EXE/DLL, Project DLL and test DLL hashes are in the new verification summary.

## Manual Verification After the Fixes

Actual post-fix Release DtoB.exe, 2026-10-13, PID 14644. Owner assisted native Open because the UI tool reported `element 127 is not available in cached app state`; subsequent actions used Computer Use against DtoB. Accessibility text returned null, so the record is agent visual observation of returned WPF screenshots, not a raw accessibility transcript. See `manual-observations.json`, `manual-manifest.json` and `desktop-log.txt` in the new evidence directory.

An owned verification project `artifacts/phase02-review-fixes-20261013/manual/Review.dtob` (UUID `4448c7de-07e8-441d-99e4-71a80c861a79`) was prepared with two rows: missing managed DWG plus UNC provenance, and an available managed DXF plus local source copy. The managed DXF is the real controlled fixture. No owner-original CAD was edited and no UNC source check was invoked.

1. Opened corrected fixture: no selected row; Missing-UNC.dwg visibly showed ManagedMissing/SourceUnchecked/ReaderPending/NotVerified; Ready-local.dxf showed Ready/SourceUnchecked/ReaderPending/NotVerified. Aggregate displayed `2 drawings: 1 Ready, 1 ManagedMissing, 2 SourceUnchecked`.
2. Selected Ready-local.dxf: metadata appeared, source stayed SourceUnchecked, Reveal Source remained disabled, Reveal Managed enabled.
3. Clicked Refresh Status: selection and metadata remained; aggregate retained the correct counts and source stayed unchecked.
4. Clicked Check Source: only the local row became SourcePresent, Reveal Source enabled, aggregate included 1 SourceUnchecked and 1 SourcePresent. Title remained clean.
5. Temporarily moved only the owned local source copy, then clicked Check Source: row/panel became Ready/SourceMissing; Reveal Source disabled, Reveal Managed enabled; UNC row stayed unchecked.
6. Clicked Verify Integrity while local original was absent: managed DXF became Verified with a timestamp while SourceMissing remained. This confirms the visible managed/source separation; automated no-fallback tests provide the dependency assertion. Restored the owned source copy afterward.
7. Alt+F4 closed normally with no dirty prompt. PID disappeared; log records `Desktop closed.` and no FATAL. Manifest SHA-256 remained `3F5D194AEC6D3C0C65B99D015BA1156CD795A4D013244F7E3FA22C5509175CC2`, confirming no metadata rewrite by these observations.

**Additional restart limitation:** two subsequent Computer Use launch attempts failed with `accessibility window-opened handler did not become ready`, and no DtoB process remained. No successful post-fix restart UI check is claimed. Reopen/runtime-reset behavior is covered by the new automated tests; the earlier full create/save/restart workflow is historical evidence below. The completed M-02/M-03 UI checks and normal exit are unaffected by this helper limitation.

## Failure Cases Encountered During Verification

The first hand-built manual manifest had non-UTC ImportedAt values introduced by PowerShell JSON date conversion, plus duplicated fixture hashes. Production validation correctly reported `DtoB error: Open failed: Invalid drawing revision metadata`; no fatal shutdown occurred. Corrected the test fixture to UTC strings and distinct real DWG/DXF revision facts, then successfully opened it. This was a verification-fixture error, not a product fix or an accepted invalid project. Its expected-error log is retained.

The original implementation's WPF reentrant Closing failure was fixed before the historical 111-test checkpoint by scheduling Close through Dispatcher.BeginInvoke; the current clean-close manual check passed again. Native file-dialog and launch-helper errors above are reported as tool limitations, not product test successes.

## Historical Original Implementation / Revit Evidence

The **2026-10-08, 111-test** checkpoint verified real DWG Add, Save/close/restart/Recent and metadata, duplicate no-op, real DXF Replace with DrawingId retained/RevisionId changed, Remove with files retained, Settings and normal close. Details and hashes remain in `docs/status/evidence/phase02/`. The original source-disappearance workflow used the then-automatic source probe and is superseded by the explicit Check Source verification above.

That original checkpoint also produced actual Revit 2025 Build 25.4.50.35 PONG, host PID 16068/Desktop PID 21264, request `c94dcdf1-bd65-4b5a-bb8b-7d458f792bd7`, Status=0/Error=0 and normal host STOPPED. Evidence remains in `docs/status/evidence/phase00-review/phase02-20261008-final/`. It is **historical host evidence**, not a new post-fix run. M-01/M-02/M-03 modify drawing storage/status and the ordinary shell only; IPC/Revit/ConnectionWindow and the verification startup path were unchanged, so no new host launch was required for these fixes. Current automated PHASE 00 IPC regressions all pass.

## Drawing Identity / Revision Strategy

DrawingId is the logical UUID; RevisionId identifies accepted immutable bytes; FileHash is SHA-256. Different-content Replace retains DrawingId and creates RevisionId, never overwrites the old revision. Same hash is a no-op; another registered drawing's hash is rejected. Changed content at an already registered source path requires Replace. Same basename at another path with different bytes is independent. Remove alters registration only.

## File Storage Strategy

SourceFilePath is provenance. ManagedFilePath resolved from actual CurrentFilePath is authoritative: `<ProjectId:N>.dtob-assets/drawings/<DrawingId:N>/<RevisionId:N>.dwg|.dxf`. Canonical relative paths, containment and the corrected scoped reparse guard protect storage. Import streams into a same-directory unique temp, hashes/counts, completes/flushes and publishes without overwrite before session mutation. Save As copies/verifies managed revisions asynchronously before safe manifest publication. Path/timestamps/Recent/clean state change only on success. Published orphans can remain after a later failure; no cross-file transaction is claimed.

## Integrity Verification Strategy

Open/Refresh use managed metadata and zero-content-read access handles only. Source defaults to SourceUnchecked and is checked only explicitly/asynchronously. Availability, source state, ReaderPending and integrity are separate runtime observations. Integrity starts NotVerified; only completed explicit SHA-256 can yield Verified/HashMismatch. Changed metadata invalidates cached integrity without hashing. Ordinary Save does not hash and rejects a known mismatch; explicit asset-aware Save As verifies copied bytes. Same-size/timestamp-preserving edits require another explicit verification. No background monitoring.

## Schema Migration

Project v2 requires validated drawings. Strict v1 converts in memory to an empty collection while preserving identity/metadata and clean state; only successful explicit Save writes v2. Unknown versions/fields fail. Settings/Recent remain v1; CAD/BIM IR unchanged. Runtime availability/source/integrity/time are never serialized. Review fixes do not change persisted schema or IDs.

## Architecture Decisions

ADR-013 retains hybrid provenance/managed immutable copies, UUID identity, duplicate/retention and manifest-last Save As. Its implementation-review clarification records scoped reparse checking, zero automatic source I/O and explicit asynchronous source status. A lazy selection probe was rejected because it would silently contact original hosts. ADR-014 retains explicit v2/v1 migration. No new ADR or architecture redesign was required for these local corrections; .NET 8 WPF, independent Project library, IR boundaries and Revit isolation remain.

## Antigravity Review Resolution

The original pre-implementation HIGH issue (HashMismatch inferred during Open/Refresh) remains resolved by explicit asynchronous hashing and deterministic no-read/hash tests. The subsequent implementation review returned CHANGES_REQUIRED for M-01/M-02/M-03; exact severity, resolution, files, tests and verification are documented in Final Review Fixes above. Independent final re-review remains pending.

## Unresolved LOW Findings / Known Limitations

| Finding | Current disposition |
|---|---|
| L-01 — empty directories / duplicate full copy | Retained/documented: failed, cancelled or duplicate imports can leave empty UUID directories; duplicates are detected after streamed copy/hash. No cleanup or pre-hash optimization added. |
| L-02 — multi-file stop-on-first-failure | Retained/documented; earlier successes remain, later items are explicitly unprocessed. Checklist wording corrected; no claim of attempting every file. |
| L-03 — restricted text DXF start | Retained/documented: leading 999 comments or missing HEADER are unsupported, as are binary/UTF-16 DXF. BasicHeaderOnly is deliberately bounded. |
| L-04 — symlink privilege in test | Resolved with actual directory-junction test helper; old path-safety test and new junction regressions pass. |
| L-05 — close during busy operation | Unresolved LOW: close is safely ignored while busy without an explanatory close message. User can use the existing Cancel operation command. |
| L-06 — defensive stale Add/Save exception | Unresolved LOW: stale guards still throw InvalidOperationException and would reach fatal handling if triggered outside the shell's single-flight controls. Current UI disables session mutations during those operations. No general concurrent-session API guarantee. |
| L-07 — Replace ordering | Unresolved cosmetic LOW: replacement is appended to the collection, so row order may change. |
| L-08 — selection after refresh | Addressed: deterministic binding-reset regression plus actual WPF Refresh selection/metadata retention observed after fixes. |

Other limits: framework-dependent .NET 8 Desktop runtime/no installer; retained old/orphan revisions; best-effort temp cleanup; point-in-time status/integrity; slow remote **managed-storage** metadata can still affect synchronous Open (source provenance is no longer touched). Cancelling an explicit source check cannot abort its in-flight OS call. Path checks do not close adversarial filesystem TOCTOU races. Drop, native dialog failure matrix and post-fix restart were not all manually exercised. No fixed <100ms or multi-GB UI benchmark is claimed.

## Unsupported Cases

Unrecognized DWG signatures; text DXF outside the bounded HEADER-start format; binary/UTF-16 DXF; CAD structure validation/interpretation; Xrefs/dependencies; reparse components inside managed storage and reparse manifest files; unknown project schemas, concurrent merges and missing/unreadable required assets at Save. Errors remain explicit. A plausible header with a damaged body may pass BasicHeaderOnly; units remain PendingReader rather than guessed mm.

## Git Diff Review Result

Reviewed tracked diffs and new implementation/test/document files, including source-path call sites, save ordering, scoped reparse callers, UI bindings/selection, test seams and retained low-priority limitations. `git diff --check` passes (only repository LF/CRLF informational warnings). The post-manual implementation/build/test source hash matches the 126-test snapshot. No new forbidden dependency, parser, CAD/BIM schema, recognition, IPC/Revit behavior or later-phase scope was introduced. Historical evidence and owner review were preserved. No commit or push.

## Exit Criteria Verification

| PHASE 02 criterion | Evidence / result |
|---|---|
| Add DWG/DXF | PASS: original real UI Add/Replace evidence; both real controlled fixtures pass in current 126-test suite. |
| Persist metadata | PASS: current roundtrip/migration/Save As tests; original full restart evidence; post-fix status operations leave manual manifest byte-identical. |
| Duplicate detection | PASS: current matrix and historical owner-assisted no-op; unchanged policy. |
| Remove / Replace | PASS: current identity/retention/Discard cases; historical UI workflow. |
| Error status UI | PASS after M-03: unselected missing row and aggregate Open/Refresh counts observed in post-fix Release UI; explicit source checks and automated unreadable/size cases. |

All five phase criteria have evidence; review-specific M-01/M-02/M-03 regression gates also pass. Manual and historical evidence are explicitly distinguished from the current automated build. Final acceptance is Antigravity's decision, not inferred from passing tests.

## Readiness for Independent Antigravity Final Re-review

**Ready for final re-review, status REVIEW_REQUIRED.** Current source, 126-test Release evidence, actual review-fix UI verification, exact resolutions, remaining LOW findings and tool limitations are recorded. Next step is independent Antigravity re-review of this working tree. Stop at PHASE 02; do not start PHASE 03 or commit/push.
