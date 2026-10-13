# PHASE 02 — Pre-Implementation Design Review

This is an independent adversarial review of the PHASE 02 proposed architecture and checklist.

## Identified Issues

**Issue 1**
Severity: HIGH
File / Section: `docs/status/PHASE_02_CHECKLIST.md` (Proposed drawing model / Runtime status)
Problem: The checklist specifies that a "HashMismatch error" is part of the live availability/status computed "on reopen/refresh". To detect a hash mismatch, the application must read and hash the entirety of every managed `.dwg` file. This directly violates the strict constraint that project Open must not synchronously hash large files on the UI thread. Even if executed asynchronously, hashing all managed assets on every Open or UI Refresh is unnecessary and will cause massive disk thrashing.
Impact: Severe performance degradation. Project Open and Refresh times will scale linearly with the total size of all managed assets, leading to unacceptable UI freezing and resource starvation on large projects.
Recommended Change: Remove `HashMismatch` from the live status computed during standard Open/Refresh. On Open/Refresh, compute only `ManagedMissing` (via a fast file existence check) or `Unreadable` (via a quick file size or permissions check). Strict hash verification should be deferred to an explicit user-triggered validation command or performed right before passing the asset to the CAD reader in PHASE 03.
Required Test or Verification: Add a test asserting that Project Open executes in constant time (e.g., <100ms) regardless of the total size of managed DWG files, and specifically verifies that stream reading/hashing is not invoked during Open or UI Refresh.

## Architecture Assessment

### Drawing identity model
**PASS.** The strict separation between `DrawingId` (logical domain identity) and `RevisionId` (content revision identity) is robust. The use of immutable UUIDs ensures that Replace/Save As operations preserve identity without ambiguity.

### Revision model
**PASS.** The decision to persist only the current accepted revision without building a complex historical diff engine or automatic garbage collection perfectly aligns with the requirement to avoid unnecessary complexity for DtoB V1. Explicitly deferring history browsing is a sound architectural choice.

### Storage ownership model
**PASS.** Storing a managed copy (Option C) while retaining the original source path as informational provenance guarantees reproducibility. It ensures the managed asset is the authoritative reader input and safely prevents any modification of the original external DWG.

### Project asset layout
**PASS.** Deriving the asset folder from `ProjectId` (`<ProjectId>.dtob-assets/`) rather than the `.dtob` filename is an excellent decision. It allows users to safely rename the `.dtob` file within the same directory without breaking paths, fully supporting portability.

### Duplicate policy
**PASS.** Project-local hash deduplication is deterministic and safe. By explicitly rejecting ambiguous additions and directing users to `Replace`, the design avoids complex duplicate-resolution logic while guaranteeing that no two drawings share the exact same content within a project.

### Replace policy
**PASS.** The replacement sequence (generate new `RevisionId`, publish managed file, then commit candidate state) is transactionally safe. The guarantee that it "never overwrites the prior revision bytes" prevents corruption of earlier project manifests (e.g., same-directory Save As copies) that still reference the old revision.

### Schema migration
**PASS.** The v1-to-v2 migration strategy is extremely safe. Loading v1 without dirtying the project, migrating in-memory, and explicitly rejecting v2 in older executables prevents silent data loss and correctly implements forward-only evolution. 

### Save / Save As safety
**PASS.** The "manifest-last" safety guarantee is preserved. `Save As` into a different directory correctly stages and verifies all assets before writing the `.dtob` JSON. If an IO failure occurs mid-flight, the session, `Recent` list, and `CurrentFilePath` remain strictly unmodified.

### File/path safety
**PASS.** Constraining managed paths to relative paths and validating containment completely mitigates path traversal and reparse-point escaping attacks.

### Future PHASE 03/04 compatibility
**PASS.** The asset layout and identity semantics ensure that the future CAD reader receives an immutable, stable `ManagedFilePath`. Marking `DetectedUnit` as `PendingReader` without guessing correctly defers semantic parsing to the actual engine.

### PHASE 02 scope discipline
**PASS.** The checklist explicitly excludes deep CAD parsing, viewer rendering, BIM generation, and AI/ML, adhering perfectly to the restricted file-management scope of this phase.

## Verdict

CHANGES_REQUIRED_BEFORE_IMPLEMENTATION

## Pre-Implementation Re-review

The previously identified HIGH issue regarding the potential for massive UI thread blocking via `HashMismatch` evaluation during Project Open and normal UI Refresh has been thoroughly evaluated against the revised `docs/status/PHASE_02_CHECKLIST.md` proposal.

All requirements for decoupling file availability from deep content verification have been successfully met:

1. **No Hashing on Open/Refresh**: Open and Refresh rely purely on file metadata and quick readability checks. Zero content-reading or SHA-256 hashing takes place.
2. **Explicit Verification Boundaries**: `HashMismatch` is strictly the result of a discrete, asynchronous, user-triggered `Verify Integrity` command.
3. **Session Safety**: Integrity status defaults to `NotVerified` across all opens. Stale results are explicitly discarded upon path, session, or revision updates. Cancellation and IO exceptions degrade to `NotVerified` rather than a false mismatch. Size differences yield metadata warnings, not mismatches.
4. **No Background Process Escalation**: No watchers or periodic background hashing loops were introduced to sidestep the problem.
5. **Deterministic Testing Constraints**: The testing strategy properly rejects fragile wall-clock gating (e.g. `<100ms`) in favor of deterministic spies/fakes that strictly fail the test suite on any unintended `Read` or `Hash` invocation during standard Open/Refresh flows.

The previously verified architecture—comprising stable `DrawingId`/`RevisionId` semantics, authoritative immutable managed copies, ProjectId-derived asset trees, robust deduplication policies, safe `Save As` asset sequencing, and v1-to-v2 strict schema migrations—remains completely intact and untampered.

No new architecture, correctness, data-loss, or safety problems were introduced during the revision.

APPROVED_FOR_IMPLEMENTATION


## Final Implementation Review

### Review basis and independently re-run evidence

| Item | Result |
|---|---|
| Reviewed revision | Base commit `be27e7d712332506bc0b5d51b70720ab8fff8a86`, branch `phase/02-dwg-upload`, **uncommitted working tree** (nothing committed/pushed). Evidence `source-snapshot.json`: all 51 listed files re-hashed today, **0 mismatches** with the current tree. |
| Out-of-scope diff | `git diff HEAD` shows **no change** under Core / Cad / Bim / Geometry / Analysis / Ipc / Revit adapters. `DtoB.Project.csproj` is unchanged (no references, no packages). |
| Build (re-run by me) | VS MSBuild 17.14 + SDK 8.0.416 (`dotnet.exe` is absent on this host, as documented). `DtoB.Tests.csproj` (pulls `DtoB.Project` + linked `ShellViewModel.cs`) and `DtoB.Desktop.csproj` Release **Rebuild: PASS**. The full Revit solution build was **not** re-run by me; I relied on the evidence consistency checks below. |
| Tests (re-run by me) | `vstest.console` on the freshly built DLL: **111 / 111 passed**, 0 failed, 0 skipped. Matches the report. (Raw logs: `artifacts/phase02-review/`.) |
| Revit host evidence | `desktop-ping.json` request `c94dcdf1-…` appears in `revit-host.log` as `Pong request=c94dcdf1-… pid=16068`, with `STARTED` and normal `STOPPED`; Revit 2025 build 25.4.50.35; source tree hash in `verification-summary.json` matches the reviewed tree. Consistent. |
| Manual UI evidence | Not re-executed by me. I checked the report text against the code. Drag & Drop and Save As dialogs are, per the report, **not** manually exercised (code-reviewed only). |

### Findings

No BLOCKER and no HIGH finding. Three MEDIUM findings and several LOW findings.

---

**M-01**
Severity: MEDIUM (PHASE 01 regression)
File: `packages/DtoB.Project/DrawingMetadata.cs` `AssetPaths.RejectReparse` (L20-27); called from `ProjectPersistence.cs` `ValidateManifestPath` (L92) and from `AssetPaths.Resolve` (L18), `DrawingFileService.RegisterAsync` (L69), `CopyAssetsAsync` (L96)
Problem: `RejectReparse` walks **every ancestor up to the drive root** and throws if any directory is a reparse point. The approved design (ADR-013, checklist) only requires rejecting *escaping/reparse destinations* inside the project's own storage. Because `ValidateManifestPath` now runs on every `Save`/`SaveAsync`, a project whose `.dtob` sits below any junction/symlink/reparse ancestor can no longer be saved **even with zero drawings**. PHASE 01 `Save` had no such check.
Impact: Previously working PHASE 01 behavior is lost. Typical hits: folders reached through a junction/symlink or a redirected/synced library folder that carries a reparse attribute. Users get "Save failed: Reparse paths are unsupported…" for a project that has no drawings. Existing drawings in such a location show `ManagedUnreadable` because `Probe` swallows the same exception.
Reproduction: I replicated the exact loop logic: `mklink /J %TEMP%\x\link %TEMP%\x\real`; for `…\link\sub\project.dtob` the walk reports `…\link` as a reparse ancestor, i.e. `RejectReparse` would throw. (Not reproduced inside DtoB.exe itself; cloud-synced folder behavior was not available on this machine and is **not** claimed.)
Recommended Fix: Limit the reparse check to the part of the path **below the opened project directory** (`<ProjectId>.dtob-assets\…` components, the destination directory being created, and the final file), and to the manifest file itself. Containment is already enforced by the exact-UUID relative-path match plus the prefix check, so ancestors above the project directory add no safety. Keep the symlink-at-assets-directory rejection that the existing test covers.
Required Test: (1) New: a project under a junction parent with zero drawings saves and reopens successfully (PHASE 01 regression). (2) New: Add/Save As under a junction parent works, while a symlinked `<ProjectId>.dtob-assets` or `drawings\<id>` directory is still rejected (existing test stays green).

---

**M-02**
Severity: MEDIUM (security/robustness of untrusted manifest content)
File: `DrawingFileService.Probe` (L47-54) via `ProjectSession.Open` (`ProjectPersistence.cs` L71) and `ShellViewModel.CanRevealSource`
Problem: `SourceFilePath` is untrusted data from the `.dtob` manifest (validation only requires "fully qualified"). On every Open/Refresh the code calls `io.Probe(drawing.SourceFilePath)` (`FileInfo.Exists` + a handle open), synchronously. `MainWindow.OpenPathAsync` wraps `Session.Open` in `RunAsync`, but the lambda is synchronous (`Task.CompletedTask`), so the probe runs on the WPF UI thread. A UNC path (e.g. `\\host\share\x.dwg`) causes an outbound SMB connection (credential/NTLM exposure, name resolution) the moment a project is opened, and a dead share blocks the UI thread for the network timeout. The same applies to `File.Exists(selected.SourceFilePath)` on selection.
Impact: Opening an untrusted or stale project can freeze the shell and contact arbitrary hosts; contradicts the intent that Open be cheap and safe. Source provenance is "informational only", yet it drives network I/O at Open.
Reproduction: Hand-edit a valid v2 manifest so `sourceFilePath` is `\\10.255.255.1\share\a.dwg` (non-routable), open it from Recent; the shell blocks until the SMB timeout. (Not measured by me; derived from code path and standard Windows behavior.)
Recommended Fix: Do not touch `SourceFilePath` during Open. Compute `SourceMissing` lazily (on selection / explicit Refresh) and off the UI thread, or only for local fixed-drive paths. Treat UNC/remote `SourceFilePath` as "SourceUnchecked" without I/O unless the user explicitly refreshes. ADR-013 already accepts that metadata I/O may be slow on remote storage; this finding is about untrusted-path-driven access on the UI thread.
Required Test: A fake `IAssetFiles` spy asserting `Probe` is **not** invoked with `SourceFilePath` during `ProjectSession.Open` (managed-path probes only); a UNC `SourceFilePath` does not trigger any `IAssetFiles` call until an explicit source-status refresh.

---

**M-03**
Severity: MEDIUM (silent-failure surface / UI status)
File: `apps/DtoB.Desktop/MainWindow.xaml` (Drawings `ListBox`, `DisplayMemberPath="OriginalFileName"`), `MainWindow.xaml.cs` L55 (status text after Open), `ShellViewModel.DrawingMetadata`
Problem: Per-drawing availability (`ManagedMissing`, `ManagedUnreadable`, `SourceMissing`, `SizeChanged`) is shown **only in the metadata panel of the currently selected drawing**. After Open the status line always says "Project opened. Integrity is NotVerified; no asset hashing performed." even when every managed file is missing. The task spec (§6) expects the drawing to appear with a visible status under Project > Drawings; the list shows only the file name. The first visible failure for a moved-alone `.dtob` is a later `Save` error ("Required managed drawing is unavailable").
Impact: A project with missing/unreadable managed assets looks healthy in the list; users can proceed unaware (Save is then blocked by `CommitSave`). It meets the letter of "error status visible" but weakens the "no silent failure" rule.
Reproduction: Open a project, delete/move the `<ProjectId>.dtob-assets` folder, click Refresh Status: the list is unchanged and the status bar still reads "availability refreshed". Status appears only after selecting the row.
Recommended Fix: Show a per-row status (text or glyph from `Session.Runtime`) in the list item template, and add an aggregate count (e.g. "2 drawings: 1 ManagedMissing") to the Open/Refresh status text. No new infrastructure is needed.
Required Test: A `ShellViewModel`-level test (the test assembly already links the real `ShellViewModel`) that after `Open`/`RefreshDrawings` a missing managed file yields a non-Ready per-drawing status string/summary exposed on the view model, without any content read.

---

LOW findings (no gate impact; listed so they are not lost):

| ID | File | Problem | Recommended handling |
|---|---|---|---|
| L-01 | `DrawingFileService.RegisterAsync` L69 | A new random `DrawingId` directory is created before the copy; failed, duplicate or cancelled attempts leave **empty directories** (and duplicate detection happens only after a full streamed copy). Orphan *files* are documented; empty dirs are not. | Document, or remove the empty directory in `finally`; optionally hash first and copy only when not a duplicate. Test: empty-dir count after a rejected/duplicate import. |
| L-02 | `MainWindow.xaml.cs` `RegisterAsync` L67-74 | Multi-file import stops at the first failure and skips the rest; the checklist wording ("per-file results") suggests attempting each file. The status text does say remaining files were not processed (documented in the report/guide). | Accept as documented limitation, or continue on expected per-file failures and show a summary. |
| L-03 | `DrawingFileService.ValidateHeader` L61-62 | Text DXF must begin exactly `0/SECTION/2/HEADER`. Valid DXF files that start with `999` comment lines or without a HEADER section are rejected as unsupported (explicit error, no silent failure). | Document in the supported-input list (already partially in the guide); revisit in the reader phase. |
| L-04 | `DrawingManagementTests.UnsafePathsAndReparseDestinationsAreRejected` | Uses `Directory.CreateSymbolicLink`, which needs symlink privilege (admin/Developer Mode). It passed here; on an unprivileged machine it will fail with `UnauthorizedAccessException`, not skip. | Use a junction (`mklink /J`, no privilege) or skip with a clear message when symlink creation is not permitted. |
| L-05 | `MainWindow.xaml.cs` `OnClosing` / `CanLeaveAsync` | While a Save/Add/Verify operation is running, clicking the window close button is silently ignored (no message). Safe, but unexplained. | Show the status text "Operation in progress; cancel it before closing." |
| L-06 | `ProjectSession.AddDrawingAsync` L57, `SaveAsync` L84 | The stale-state guards throw `InvalidOperationException`, which `LocalFileErrors.IsExpected` does not treat as expected, so it would reach the fatal handler if ever triggered. The UI disables the commands that could change state during an operation, so this is currently unreachable. | Map to the expected-error path or document as defensive-only. |
| L-07 | `ProjectSession.AddDrawingAsync` L58 | Replace moves the replaced entry to the **end** of `Drawings` (`Where(...).Append(...)`), changing list order. | Preserve position (ordering is cosmetic). |
| L-08 | `ShellViewModel.Refresh` / `MainWindow.xaml` | Whether the Drawings `ListBox` keeps the selection after `Refresh()` replaces `Drawings` (Add path sets `SelectedDrawing`, then `RunAsync` finally calls `Refresh`) was **not verified by me** (no UI automation run). The report's manual evidence shows metadata displayed after Add. | Covered by manual evidence only; add a view-model test if selection loss is ever observed. |

### Verification of the requested review areas

**1. Scope discipline — PASS.** No CAD parser, viewer, semantic recognition, IR extraction, BIM/Revit change, AI/ML or PHASE 03 code. Header validation is a bounded signature/DXF-HEADER check labelled `BasicHeaderOnly`; `DetectedUnit` is always `PendingReader` and `Validate` rejects anything else.

**2. Drawing identity — PASS.** `DrawingId` = logical UUID; `RevisionId` generated per accepted copy; `FileHash` is SHA-256 of the exact copied bytes (hashed from the same stream written to the temp file). Replace keeps `DrawingId`, different bytes get a new `RevisionId`, same hash returns `Changed=false` (no dirty), another drawing's hash is rejected. Remove only filters the manifest list; no file deletion (`RemoveAndDiscardKeepRevisionFiles`). Duplicate rules are deterministic (hash first, then normalized source path).

**3. Managed-copy authority — PASS.** `SourceFilePath` is used only for provenance/duplicate-path detection and the SourceMissing warning; no fallback to the source for reading. Source is opened `FileAccess.Read`; the only `Delete` calls target temp files. `Publish` is `File.Move(..., overwrite:false)`, so old revisions cannot be overwritten; revision paths are unique GUIDs.

**4. Project schema — PASS.** v2 requires a non-null `drawings`, strict per-drawing validation (exact UUID-derived relative path, canonical uppercase SHA-256, positive size, UTC time, enum strings only, uniqueness of drawing/revision/hash/source path), unknown fields rejected. v1 loads via a strict legacy record, converts in memory, `IsDirty=false`, **no file write on Open** (test compares bytes), explicit `Save` writes v2, schema 3 and malformed variants fail while preserving the active session. `LocalJson.Read` (settings/recent) is still version-1 only and old PHASE 01 readers reject v2. `DrawingStatus`/`Integrity` live only in the runtime dictionary, never in `ProjectFile`.

**5. Open/Refresh integrity boundary — PASS (critical gate).** Production paths read directly: `ProjectSession.Open` → `RefreshDrawings` → `DrawingFileService.Probe` → `IAssetFiles.Probe` (metadata + a handle open, no read). `ShellViewModel.Refresh` → `Session.RefreshDrawings`. Neither references `IContentHasher` or `OpenRead`. No timers, watchers or background tasks exist. `HashMismatch` is assigned only in `VerifyAsync`. The test `ActualOpenAndViewModelRefreshNeverReadOrHashManagedAssets` exercises the **real** `ProjectSession.Open` and the **real linked** `ShellViewModel`, with a probe stream that throws on any read API, `OpenRead` forbidden, hasher forbidden, and 70 B / 8 GB advertised sizes — a deterministic gate, not a timing test. Size differences set `SizeChanged` (warning), never `HashMismatch`.

**6. Explicit integrity verification — PASS.** Default `NotVerified` after Open. Only the completed SHA-256 comparison in `VerifyAsync` yields `Verified`/`HashMismatch` (same-length tamper detected, test flips one byte). Cancellation propagates `OperationCanceledException`; missing/unreadable/changed-during-hash throw `IOException`; all leave `NotVerified`. The result is applied only if the session `generation` and `CurrentFilePath` are unchanged, and the post-verify probe must equal the pre-verify probe (otherwise `IOException`, not a verdict). `generation` increments on Open/Close/New/Rename/Add/Remove/Save, so a stale result cannot apply after session, DrawingId/RevisionId or path change (stale test passes). Verification never sets `IsDirty`.

**7. Save / Save As safety — PASS.** `SaveAsync` copies/verifies referenced assets to the destination first (`CopyAssetsAsync`: stream copy + SHA-256 + size check + flush + non-overwriting publish; an existing destination file is hash-verified, never overwritten) and only then calls `CommitSave` → `store.Save` (manifest last, temp+replace). `CurrentFilePath`, `Project`, `IsDirty` and Recent are updated only after success (`RecordRecent` runs after `SaveAsync` in the same lambda). The test injects publish failure and manifest-save failure: `CurrentFilePath`, project instance, dirty flag, original `.dtob` bytes and Recent file bytes are unchanged and no destination manifest exists. Orphan destination assets after a late failure are retained and documented as a limitation. Ordinary same-directory `Save` performs no read or hash (test with forbidden reads/hash). `CommitSave` additionally refuses to publish when an asset is missing/size-changed or a mismatch is already known — stricter than PHASE 01 but explicit and recoverable (Replace/Remove).

**8. File/path safety — PASS with M-01.** Managed path must equal the exact `"{ProjectId:N}.dtob-assets/drawings/{DrawingId:N}/{RevisionId:N}{ext}"` string, which by construction rejects rooted, `..`, drive-letter and backslash variants (all four are in the test), plus a containment prefix check. Symlinked `assets` directory is rejected before any write. External paths cannot be overwritten (`ValidateManifestPath` requires `.dtob`; test passes a `.dwg` path). TOCTOU between check and publish is not claimed to be closed (acceptable for a local single-user shell). See M-01 for the over-broad ancestor walk.

**9. WPF/session correctness — PASS (code review + report's actual-run evidence; no UI automation run by me).** `OnClosing` always sets `Cancel=true`, awaits `CanLeaveAsync`, then sets `closingAllowed` and schedules `Close` through `Dispatcher.BeginInvoke`, so a synchronous completion cannot re-enter `Close` inside the event. Clean close: no prompt. Dirty + Yes: `SaveAsync` through `RunAsync`; failure or cancellation → stay open, session intact. No: discard (true). Cancel (or invalid name): stay. `RunAsync` is single-flight (`operation != null` rejects), disables command panels, Recent list and project-name edit, rejects drop during an operation and `CanLeaveAsync` returns false while busy, so project state cannot be changed concurrently from the UI. Cancel reports "previous project state retained". This area has **no automated test** (WPF code-behind); the report states it is manually covered. See L-05/L-06.

**10. Regression integrity — PASS.** `ConnectionWindow`, Settings, Recent, logging/`FileLogger` and `LocalFileErrors` are unchanged; the PHASE 00 `--verify-pid` path in `App.xaml.cs` is unchanged (only the `DrawingFileService` wiring was added). Dependency test updated only for the already-approved `Desktop → Project` edge; `Project` still has no references. All 56 PHASE 00 + 22 PHASE 01 cases still pass inside the 111. Revit PING evidence is correlated and from the final tree. The only regression found is M-01 (Save under a reparse ancestor).

**11. Test quality — PASS with gaps.** Spy/forbid tests hit production code (not mocks of the unit under test); failure injection through the narrow `IAssetFiles`/`IContentHasher` seams covers publish/flush/delete failure, cancellation mid-hash, stale verify after re-Open, manifest failure in Save As, migration byte-equality and strict envelope variants. Gaps (none gating): no automated test for the WPF Closing flow, Drop, selection preservation (L-08), Save As with a pre-existing *different-bytes* destination asset, UNC/remote `SourceFilePath` (M-02), junction-parent portability (M-01), and the symlink test's privilege dependence (L-04). The same-length tamper test changes the timestamp too; this does not weaken the claim because `VerifyAsync` ignores metadata and hashes the bytes.

### Exit Criteria Verification

| Exit criterion | Result | Independent basis |
|---|---|---|
| Add DWG/DXF | **PASS** | Registration tests for `.dwg` and `.dxf`, Unicode filename, bad header/extension/binary DXF rejection, real controlled fixtures (`DtoB_TEST_2STORY_R2018.dwg/.dxf`, header `AC1024` reported from bytes, not filename); source bytes unchanged. UI wiring (dialog + optional Drop) reviewed in code; manual Add evidence recorded by Codex. |
| Persist drawing metadata | **PASS** | Save/Close/Open round trip compares serialized drawing facts; project v2 JSON validated strictly; v1 migration clean; runtime state not persisted. |
| Duplicate detection | **PASS** | Hash and normalized-source-path matrix tested (same bytes any name → no change; same path different bytes → refuse/Replace; same basename different bytes → separate drawing). |
| Remove / Replace | **PASS** | Replace keeps `DrawingId`, new `RevisionId`/hash, old bytes retained, same-hash no-op, duplicate-target refusal; Remove registration-only, files retained, Discard restores saved collection. |
| Error status UI | **PASS (with M-03)** | Ready/ManagedMissing/ManagedUnreadable/SourceMissing/SizeChanged and NotVerified/Verified/HashMismatch are computed and displayed in the selected-drawing panel; Open/read/IO failures surface through `DtoB error` dialogs. Per-row status is missing (M-03). |

### Final assessment

The central architecture gates are genuinely met in the production code: identity separation, immutable managed copies, manifest-last Save As, strict v1→v2 migration, and an Open/Refresh path that provably performs no content read or hashing. The scope is clean and the reported 111/111 and the Revit PING evidence were independently confirmed. One finding, M-01, reintroduces a PHASE 01 behavior regression (Save blocked under any reparse-point ancestor even with no drawings) and should be fixed with a small, local change before merge; M-02 and M-03 should be fixed in the same pass because they concern untrusted-path I/O on the UI thread and visibility of asset failures. No broad rewrite is needed.

CHANGES_REQUIRED

## Final Re-review

### Summary of Re-review Execution

Codex has implemented fixes for all three MEDIUM findings (M-01, M-02, M-03) and resolved LOW findings L-04 and L-08.
The current working tree in `DtoB_Repo` was independently inspected and re-verified.

- **Build Verification**: Rebuilding `DtoB.Desktop.csproj` and `DtoB.Tests.csproj` (which pulls `DtoB.Project` and links `ShellViewModel.cs`) succeeds cleanly without warnings or errors (Roslyn / MSBuild 17.14, .NET SDK 8.0.416).
- **Test Execution**: `vstest.console.exe` was executed directly against `DtoB.Tests.dll`. **126 / 126 tests passed** (0 failed, 0 skipped), confirming that all 15 newly added regression/fix cases in `Phase02ReviewFixTests.cs` succeed alongside the existing 111 test suite.
- **Architectural Scope**: Zero changes were introduced to Core, Cad, Bim, Geometry, Analysis, Ipc, or Revit adapters. PHASE 03 scope has not been started.

---

### M-01 Verification: Reparse Path & Ancestor Handling

- **Implementation**: In `DrawingMetadata.cs`, `AssetPaths.RejectReparse(string path, string projectDirectory)` now explicitly stops the ancestor loop once it reaches `projectDirectory`. 
  ```csharp
  for(var current=full; !StringComparer.OrdinalIgnoreCase.Equals(current,root); current=Path.GetDirectoryName(current)!)
  ```
  It checks only path components beneath the opened project boundary (including `.dtob-assets`, subdirectories, and files) or the manifest itself, rather than traversing upward to the volume drive root.
- **Phase 01 Preservation**: Projects located in a folder structure reached via a parent junction/reparse point outside the project boundary can now be created, saved, opened, and modified without false rejection, restoring full compatibility for zero-drawing and standard projects alike.
- **Reparse Containment**: Junctions or symlinks placed *inside* the project directory (e.g. at `<ProjectId>.dtob-assets` or inside `drawings/<DrawingId>`) remain strictly forbidden and throw `InvalidDataException`. Containment and relative path validations remain fully active.
- **Test Evidence**: Verified by `Phase02ReviewFixTests.JunctionParentAllowsEmptySaveOpenRegistrationAndSaveAs` and `ReparseWithinAssetTreeIsRejectedWithoutWritingOutside` (covering root asset directory and nested drawing folder junctions using Windows directory junctions via PowerShell without privilege requirements).
- **Status**: **RESOLVED (PASS)**.

---

### M-02 Verification: SourceFilePath Provenance & No-I/O Contract

- **Implementation**: 
  - `DrawingFileService.Probe(...)` during normal `ProjectSession.Open` and `RefreshDrawings` now sets `SourceStatus = SourceAvailability.SourceUnchecked` without calling `io.Probe(drawing.SourceFilePath)` or touching the disk/network.
  - `SourceMissing` is now a computed property on `DrawingStatus` returning `SourceStatus == SourceAvailability.SourceMissing`.
  - An explicit async method `ProjectSession.RefreshSourceStatusAsync` was added for checking source paths, hooked to a dedicated "Check Source" UI action. It runs off the UI thread via `Task.Run` with cancellation and stale-generation rejection.
  - Selecting a row in `ShellViewModel` does not touch `SourceFilePath`. `CanRevealSource` is enabled only if `SourceStatus == SourceAvailability.SourcePresent` (which requires explicit "Check Source" first, clearly hinted via UI tooltip).
- **Network / UNC Safety**: Non-routable or remote UNC paths (e.g. `\\unreachable-dtob-test.invalid\share\source.dwg`) in `SourceFilePath` trigger zero I/O, zero handle opens, zero File.Exists probes, and zero SMB connection attempts during Open, normal Refresh, or row selection.
- **Reader Authority**: Managed copy remains strictly authoritative. If a managed file is missing, the system never silently falls back to `SourceFilePath`.
- **Test Evidence**: Verified by `Phase02ReviewFixTests.OpenRefreshAndSelectionNeverProbeLocalOrUncSource`, `MissingManagedAssetNeverFallsBackToPresentSource`, `ExplicitSourceCheckPublishesOnlyRuntimeStatus`, and `PendingSourceCheckDoesNotBlockCallerAndRejectsCancelledOrStaleResult`.
- **Status**: **RESOLVED (PASS)**.

---

### M-03 Verification: Direct Visibility of Asset Status in Drawings List

- **Implementation**: 
  - `ShellViewModel` now defines `DrawingRow(DrawingFile Drawing, DrawingStatus? Status)` with properties `OriginalFileName` and `StatusText`.
  - In `MainWindow.xaml`, the `ListBox` item template was updated to bind to `DrawingRows`, rendering `OriginalFileName` and a secondary text block displaying `StatusText` directly beneath each file name.
  - `ShellViewModel.DrawingSummary` produces an aggregate summary (e.g., `2 drawings: 1 Ready, 1 ManagedMissing, 2 SourceUnchecked`), and this summary is reflected in `Status.Text` upon Open and Refresh.
  - A missing, unreadable, or size-changed managed asset is immediately visible in the Drawings list and status bar upon project load or refresh, without requiring row selection.
- **Contract Adherence**: Row status generation queries `Session.Runtime` dictionary lookup only; zero SHA-256 calculation, zero managed-content reading, and zero `SourceFilePath` I/O are performed.
- **Test Evidence**: Verified by `Phase02ReviewFixTests.RowsAndSummaryExposeProblemsWithoutSelectionOrContentReads` (confirming row `StatusText` and `DrawingSummary` expose missing/unreadable/size-changed states without content reads or hashing).
- **Status**: **RESOLVED (PASS)**.

---

### Verification of Previously Identified LOW Findings

- **L-04 (Symlink Privilege Dependency)**: Resolved. Replaced `Directory.CreateSymbolicLink` in `DrawingManagementTests.cs` and new tests with junction creation via PowerShell `New-Item -ItemType Junction` (`TestJunction`), which works universally on standard Windows user environments without Developer Mode or elevation.
- **L-08 (Selection Retention across Refresh)**: Resolved. In `ShellViewModel.Refresh()`, `selectedId` is preserved and restored after replacing collection notifications (`SelectedDrawing = Drawings.FirstOrDefault(d => d.DrawingId == selectedId);`). Verified by automated test `Phase02ReviewFixTests.RefreshRestoresSelectionAfterItemsSourceBindingClearsIt`.
- **Remaining Minor Documented Limitations (L-01, L-02, L-03, L-05, L-06, L-07)**: These represent accepted, non-regressing design choices and documented limitations (e.g., stopping multi-file import at first error, conservative DXF header parsing, empty directory retention on failed add) that do not impair safety, data integrity, or core functionality.

---

### Exit Criteria Verification

| Exit Criterion | Result | Independent Verification Basis |
|---|---|---|
| Add DWG/DXF | **PASS** | Controlled fixtures (`.dwg` with `AC1024` header, `.dxf` with text `HEADER`) successfully register; relative managed copy created, hashed, and validated; original bytes unmodified. |
| Persist drawing metadata | **PASS** | `ProjectFile` schemaVersion 2 persistence validated; clean v1 migration; round-trip serialization tested across session restarts. |
| Duplicate detection | **PASS** | Deterministic duplicate rejection by content hash and source path; same-content import is a clean no-op. |
| Remove / Replace | **PASS** | Replace preserves `DrawingId`, issues new `RevisionId`, retains previous immutable revisions; Remove removes registration only without deleting files. |
| Error status UI | **PASS** | Row-level status (`StatusText`), aggregate summary (`DrawingSummary`), and metadata panel accurately reflect availability, unverified integrity, and source check states without unneeded I/O. |

---

### Final Verdict

All three MEDIUM findings from the initial implementation review have been completely and accurately resolved. The implementation adheres strictly to the architectural constraints (safe storage layout, immutable revisions, zero-hashing Open/Refresh, and strict provenance boundaries).

PASS
