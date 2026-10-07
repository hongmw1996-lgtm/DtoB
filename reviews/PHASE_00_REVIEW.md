# PHASE 00 Independent Review (Architecture / Adversarial)

Reviewer: Antigravity · Branch `phase/00-architecture` · Reviewed commit `2a5c85b` (== `origin/phase/00-architecture`) · 2026-10-07

Counts: BLOCKER 0 / HIGH 2 / MEDIUM 7 / LOW 7. The final verdict is the last line of this file.

> This review **supersedes** the earlier `reviews/PHASE_00_REVIEW.md`, which was written against a pre-fix state (git history keeps it). Section 5 maps each earlier finding to its current status.

## 0. What I Verified vs. What I Could Not

| Codex claim | Independent result |
|---|---|
| Core build succeeds | **Verified.** `dotnet build packages/DtoB.Core -c Release --no-incremental` gave 0 warnings and 0 errors. |
| Full Solution build succeeds, **2 warnings** | **Build verified, count contradicted.** A clean clone of `2a5c85b` built with `-p:RevitInstallDir=D:\프로그램\Revit 2025`: 0 errors, **3 MSB3277 warnings** (`Microsoft.VisualBasic` 10.0 vs 10.1, `System.Drawing` 4.0 vs 8.0, `WindowsBase` 4.0 vs 8.0). The report and `verification-summary.json` say 2. `Verify-Phase00Foundation.ps1` already expects 3. The adapter output holds only `DtoB.Ipc.dll` and `DtoB.Revit2025.dll`, with no Autodesk DLL. |
| **27 tests pass** | **Contradicted as stale.** The suite now has **44** tests. A clean clone of `2a5c85b` passes 44/44. The **working tree fails 1/44** (see M-07). |
| CI | **Verified.** GitHub Actions run #1 on `2a5c85b` (workflow `PHASE 00 portable foundation`) concluded `success`. It builds only Core and Desktop and runs the tests. It does not build the Revit adapter. |
| Real Revit PING/PONG, disconnect, restart | **Not reproduced.** I did not launch Revit or register an add-in in the user's profile. The committed evidence is **from an older code revision** (see H-02). I did not run a real-host test against HEAD. |
| 10 ADRs | **Verified.** Each has an Alternatives table and a review amendment. ADR-005 records the owner's deferral quote. That quote cannot be verified independently. |
| No PHASE 01 work | **Verified.** I found no PHASE 01 feature. |
| Revit API isolation | **Verified.** Only `DtoB.Revit2025` references `RevitAPI`/`RevitAPIUI` (`Private=false`). The dependency-direction test covers all 9 production projects. |

Probes I ran, in a scratch project that I deleted afterwards:
- 4 connect-then-drop clients against `PingServer`, then a PING (H-01).
- 4 idle connected clients, then a PING (M-06).

## 1. Issues

### HIGH

**H-01**
Severity: HIGH
File: `packages/DtoB.Ipc/PipeProtocol.cs` `PingServer.RunAsync`, lines 103–105 and 119–120
Problem: `pipe.WaitForConnectionAsync(...)` runs outside the inner `try`. A client that connects and disconnects before the server finishes accepting makes it throw `IOException: 파이프가 닫히는 중입니다` ("the pipe is being closed"). The exception reaches the outer `catch (Exception)`, which sets `faulted=1` and **ends that worker for good**. Only pipe *creation* has a bounded retry. Nothing restarts a dead worker.
Impact: A single misbehaving or crashing same-user process, or a Desktop client that cancels at the wrong moment, kills all 4 workers. The Revit add-in then stays deaf until Revit is restarted. This breaks the "reconnect without restart" promise in ADR-004 and PHASE 12. `ListenerState.Faulted` is only visible in a log line. The add-in gives no signal to Desktop.
Reproduction (executed): start `PingServer(host, log, 1s)` and open/close `NamedPipeClientStream` repeatedly. The connect timed out at iteration 4. `State == Faulted`, and the log shows `IPC listener FAULTED: System.IO.IOException ... WaitForConnectionCoreAsync` four times. A later `PingAsync` throws `TimeoutException`, even after 1.5 s.
Recommended Fix:
1. Put `WaitForConnectionAsync` in the same `try` as the request, treating `IOException` as a per-connection failure. Dispose the pipe, log it and continue.
2. Add a bounded supervisor that restarts a worker that exits unexpectedly. Set `Faulted` only when recovery is exhausted.
3. Surface `Faulted` to the add-in. For example, log it and show a one-time notice.
Required Test: Open and drop at least 50 connections, then assert that PING returns PONG and `State == Running`. Add a test that forces the unrecoverable path and asserts `State == Faulted` and that `DisposeAsync` does not throw. `CreationFailureRecoversAndFaultedShutdownIsSafe` currently tests only recovery after one creation failure. The faulted path is untested, despite the test name.

**H-02**
Severity: HIGH
File: `docs/status/evidence/phase00/*`, `docs/status/PHASE_00_REPORT.md`, `docs/status/PHASE_00_CHECKLIST.md`
Problem: The "real host" evidence belongs to an **older revision**, not to HEAD. Four signs:
- `host-ping.json` serializes `"Status": "PONG"` and `"Error": null`. HEAD's types are `IpcStatus`/`IpcError` enums. The Desktop evidence writer uses default serializer options, so it emits `0` today.
- The stack trace in `host-disconnected.json` points at `...\DtoB_Repo\packages\DtoB.Ipc\PipeProtocol.cs:line 52`. HEAD's `PingAsync` is structured differently and that path no longer exists.
- `verification-summary.json` hashes `DtoB.Core.dll` and `DtoB.Revit.Core.dll` as deployed from the adapter output. HEAD's adapter references only `DtoB.Ipc` and emits neither.
- The add-in log lines end in `error=` and the log file is `revit-<pid>.log`. HEAD writes `revit-<timestamp>-<pid>.log` and prunes old logs.

The following were never exercised inside a real Revit process: the 4-worker server, the `GetNamedPipeServerProcessId` identity check, `ListenerState`, `HostNotRunningException`, the new log naming and retention, and the shutdown path. `scripts/Verify-Phase00Host.ps1` was written to produce current evidence, but `docs/status/evidence/phase00-review/` does not exist.
The report is also internally stale. It says "No commit or push occurred" and "27 tests" (HEAD has 44). It says "2 warnings" (HEAD has 3) and "No independent review has yet been performed". The checklist still states `HEAD: 720c083` and "No solution… exists". The report also repeats "ADRs created before source implementation", which every ADR amendment now disclaims.
Impact: Exit Criterion "통신 PoC 성공" is proven for a different binary than the one being gated. The report cannot serve as the Phase Gate record.
Reproduction: `git show 2a5c85b:docs/status/evidence/phase00/host-ping.json`, compare with `PipeProtocol.cs`. Build `DtoB.Revit2025` and list `bin/Release/net8.0-windows`.
Recommended Fix: After H-01 is fixed, run `Verify-Phase00Host.ps1` on the final commit. Cover PING/PONG, normal shutdown with STOPPED, a Desktop PING that fails with an explicit error, and restart. Commit the evidence with the commit hash, Revit build and DLL hashes. Rewrite the report and checklist to match reality: 44+ tests, 3 warnings, commit state, and the deletion of `FILE_INDEX.md` and `MASTER_PLAN.md`, which the report omits. Replace the self-assigned gate status with "submitted for review".
Required Test: A fresh clone at the reported hash builds with Revit. The reproduced run's add-in log and Desktop JSON show the current schema and a request ID visible in both.

### MEDIUM

**M-01**
Severity: MEDIUM
File: `packages/DtoB.Cad/CadContracts.cs` `CadDocument.Validate`, `CadEntity.Validate`; `docs/adr/ADR-006`
Problem: The coordinate conventions are only documentation. ADR-006 states:
- Block-definition entities have an empty `InsertionPath` and identity `Transform`.
- `InsertionPath` is the outermost-to-innermost INSERT handle list.
- `Entity.Transform` is the accumulated placement.

`Validate()` checks none of these:
- Entities inside blocks can carry any path or transform.
- `InsertionPath` handles need not match any `CadInsert`.
- `Transform` need not equal the composition of its INSERTs.
- `OwnerBlockId` is not checked against the block that holds the entity.
- Arc angles are only checked for finiteness.

Impact: A PHASE 04 reader can produce internally inconsistent geometry that still passes `Validate()`. The result is silently wrong placement, which is the "hidden assumption" category this phase is meant to eliminate.
Reproduction: Edit `datasets/controlled/phase00/nested-mirrored.json` so the arc's `transform` is identity but its `insertionPath` is `["A0","B0"]`. `Validate()` still passes.
Recommended Fix: Add checks for the three ADR-006 rules above that are decidable without tolerance. Check composed-transform consistency with a caller-supplied tolerance. Confirm `OwnerBlockId` matches the containing block.
Required Test: Negative tests for each rule: a non-empty path on a definition entity, a path handle with no matching INSERT, and a transform that disagrees with the composed INSERT chain.

**M-02**
Severity: MEDIUM
File: `CadDocument.Validate` (`SourceUnit == "mm"`); `docs/adr/ADR-006` ("Keep source unit metadata")
Problem: The contract accepts only `SourceUnit == "mm"`, so the original DWG unit (`INSUNITS`) cannot be recorded. A reader for an inch drawing must pre-scale into mm and lose the original unit. ADR-006 says to keep source unit metadata, but the contract rejects any value other than mm. `SourceToModel` has no stated relationship to unit scale.
Impact: Information loss, in tension with AGENTS rule 12 (explicit unit conversion at the boundary). PHASE 04 will need a schema change, which requires an ADR under rule 14.
Reproduction: Deserialize any CAD fixture with `sourceUnit: "inch"` and call `Validate()`. It throws.
Recommended Fix: Decide now between two options. (a) `SourceUnit` is informational and the producer must declare a unit scale. (b) The unit stays a validated enum with an explicit scale field. Record the choice in ADR-006/008. If the owner intentionally defers, list it under Known Limitations.
Required Test: A fixture with a non-mm source unit that either round-trips with its original unit or fails with an explicit, documented diagnostic.

**M-03**
Severity: MEDIUM
File: `packages/DtoB.Bim/BimContracts.cs` `BimObject.Validate` (line 62), `IsSemanticPrediction`
Problem: Prediction enforcement depends on the producer-declared boolean `IsSemanticPrediction`. `(object from SourceDrawing) with { IsSemanticPrediction = false, Status = Confirmed, Prediction = null }` passes validation. The regression test `ConfirmingPredictionCannotDiscardEvidence` covers only the `true` path.
Impact: AGENTS rule 3 ("every semantic prediction has confidence/evidence") is voluntary. A recognition engine can erase evidence by clearing a flag. The flag is redundant with `Provenance.Origin`.
Reproduction: Apply the test's mutation with `IsSemanticPrediction=false` to a `SourceDrawing`-origin wall. It passes.
Recommended Fix: Derive the rule from `Provenance.Origin`. A `SourceDrawing` object needs `Prediction`, or it must have been explicitly confirmed by a documented manual action. Drop the flag, or make it derived.
Required Test: A `SourceDrawing`-origin object with Confirmed, flag false and null prediction is rejected. A `Manual` object with the same shape passes.

**M-04**
Severity: MEDIUM
File: `packages/DtoB.Bim/BimContracts.cs` (`BoundaryContract`, `BimDocument.Validate()`), `packages/DtoB.Geometry/Geometry.cs` (`Segment3.Validate()`)
Problem: There are two validation paths. The tolerance-aware `Validate(GeometryTolerance)` checks closure and degeneracy. The default `Validate()` — used by the fixtures, the Analysis engine and most tests — does not. Its boundary check is only `Length >= 4`, and `Segment3.Validate()` uses exact `Start != End`. An unclosed Floor or Room, or a 1e-9 mm wall, passes `BimDocument.Validate()`.
Impact: A caller who forgets the tolerance overload silently skips geometry validation. This sits uneasily with rule 13 (no hard-coded tolerance) and with the report's "caller-supplied tolerance".
Reproduction: A `BimFloor` whose last point differs from its first passes `BimDocument.Validate()`.
Recommended Fix: Make `Validate()` clearly "structural only" by renaming it. Or remove the exact-equality overloads so geometry validation always needs a tolerance.
Required Test: An open floor boundary fails the geometry-validation entry point and passes the structural entry point, with names that make this obvious.

**M-05**
Severity: MEDIUM
File: `docs/status/PHASE_00_REPORT.md` (Changed Files, Unsupported Cases)
Problem: The commit `2a5c85b` **deletes `FILE_INDEX.md` and `MASTER_PLAN.md`** and rewrites `ARCHITECTURE.md`, `README.md`, `START_HERE.md` and `CODEX_INSTRUCTIONS.md`. The report mentions the rewrites but not the two deletions. It also states that "the 43 initially staged files were not reset". The `AGENTS.md` and `START_HERE.md` instructions do not authorize changing governing documents during PHASE 00 implementation.
Impact: Undisclosed scope and a misleading change log. Whether the deletions are intended is a project-owner decision.
Reproduction: `git diff --name-status fc3633d HEAD`.
Recommended Fix: Owner to confirm both deletions and the governing-document rewrites. Record them explicitly in the report, or restore the files.
Required Test: None (process).

**M-06**
Severity: MEDIUM
File: `packages/DtoB.Ipc/PipeProtocol.cs` (`PingServer`, `PingClient`); `tests/DtoB.Tests/IpcTests.cs`
Problem: The server has 4 workers, each with a request deadline of 3 s. Four idle connections (unauthenticated, same-user) hold every worker. A legitimate Desktop PING then times out at the 1 s connect limit. `IdleConnectionDoesNotBlockSecondClient` uses only one idle client, so this is not tested.
Impact: Local self-DoS for the duration of the idle deadline, repeatable indefinitely. The Desktop shows "Disconnected" while Revit is healthy. ADR-004 accepts that peers are trusted same-user processes. It does not state that they can starve the endpoint.
Reproduction (executed): connect 4 idle `NamedPipeClientStream`s, then `PingAsync(..., 5 s)`. It threw `TimeoutException` after 1028 ms.
Recommended Fix: Shorten the first-frame deadline to a small value. Accept more concurrent connections or hand each connection to its own bounded task. State the same-user starvation limit in ADR-004 if it is accepted.
Required Test: Open N = workers idle clients and assert that a PING still succeeds within the deadline, or that the limitation is the documented behavior.

**M-07**
Severity: MEDIUM
File: `tests/DtoB.Tests/FoundationDocumentTests.cs` (`AllProductionProjectDirectionsAreEnforced`, line 33), `IpcTests.cs` (`OsProcessIdentityRejectsImpersonation`, line 130)
Problem:
- The test finds each project with `Directory.EnumerateFiles(Root(), name + ".csproj", AllDirectories).Single()`. The untracked nested copy `DtoB_Review/` in this working tree duplicates every project, so `Single()` throws. **Reproduced: 43 passed, 1 failed in the working tree. A clean clone passes 44/44.**
- The impersonation test picks `GetProcessesByName("explorer").First()`. It depends on the machine having an interactive shell. It throws `InvalidOperationException` on a headless or service session instead of testing.
- `NonexistentProcessFailsFast` never asserts "fast". It would pass after a 5 s wait.
- `AdrsContainEvaluatedAlternativesAndValidUtf8` checks only the heading text and table line count. It does not assert that rejected or selected options have reasons.

Impact: The suite passes in CI and fails on a dirty checkout. A reviewer or developer sees a red test that comes from the environment, not from a regression. Some assertions are weaker than their names claim.
Reproduction: Run `dotnet test tests/DtoB.Tests -c Release` in the working tree. Then run it in `git clone`.
Recommended Fix: Resolve projects from the solution file or from `git ls-files`-style paths. Exclude `bin/`, `obj/` and nested repositories. Use `Process.GetCurrentProcess().Id`-independent fake PIDs, or any other running non-test PID, for the impersonation test. Add an elapsed-time bound to the fast-fail test.
Required Test: The suite is green when a duplicate nested copy of the repository exists. The fast-fail test fails if the elapsed time exceeds a stated bound.

### LOW

**L-01**
Severity: LOW
File: `apps/DtoB.Desktop/App.xaml.cs` (line 12), `MainWindow.xaml.cs`
Problem: If `--verify-pid` is passed with the wrong argument count or form, the arguments are silently ignored and a normal window opens. No error or evidence file results. The Desktop evidence writer uses default JSON options (numeric enums) but the wire uses string enums.
Impact: A failed evidence run looks like a normal start. The evidence JSON uses a different enum encoding from the protocol.
Reproduction: Run `DtoB.Desktop.exe --verify-pid abc`.
Recommended Fix: Report bad arguments in the status text. Use the same options for evidence as for the wire, or document the difference.
Required Test: Not required for a verification shell.

**L-02**
Severity: LOW
File: `revit/DtoB.Revit2025/ConnectorApplication.cs` (lines 20–21, 26–27)
Problem: Log pruning (`old.Delete()`) sits inside the startup `try`. An `IOException` on an old locked log makes the whole connector fail to start. `TaskDialog.Show` is called on the startup path. `Log()` swallows failures to `Trace` only.
Impact: A housekeeping failure can disable the add-in. Log loss is invisible.
Reproduction: Hold an old `revit-*.log` open with deny-delete, then start Revit.
Recommended Fix: Wrap pruning in its own try-catch and continue.
Required Test: Not required for PHASE 00.

**L-03**
Severity: LOW
File: `packages/DtoB.Cad/CadContracts.cs` (`CadEntity.Validate`, line 64–65)
Problem: The "non-+Z normal is unsupported" rule hides in `RawProperties["extrusionNormal"]`. The check uses exact `== 0`/`== 1` on `GetDouble()`. A non-numeric element throws `InvalidOperationException`, not `InvalidDataException`. A reader that omits the key on a flipped entity passes silently.
Impact: A typed concept is stored in an untyped bag and compared exactly. A DWG normal of `(1e-17, 0, 1)` would be rejected.
Reproduction: Set `extrusionNormal` to `["a", 0, 1]`.
Recommended Fix: Make the normal a typed optional field with a documented tolerance. Convert JSON-type errors to `InvalidDataException`.
Required Test: Malformed and near-+Z normals produce an explicit, typed outcome.

**L-04**
Severity: LOW
File: `packages/DtoB.Bim/BimContracts.cs` (`ReviewStatus`, `Parameters`), `packages/DtoB.Core/Contracts.cs` (`SemanticPrediction<T>`)
Problem: `ReviewStatus` is a PHASE 11 workflow concept. `Parameters` is an untyped `Dictionary<string, JsonElement>`, although ADR-009 rejects property bags. `SemanticPrediction<T>` has no production caller, only a test. The earlier review raised the first two; they remain.
Impact: Scope creep and an uncontrolled extension point.
Reproduction: Read the contracts.
Recommended Fix: Remove `Parameters`, or define what may go in it. Remove `SemanticPrediction<T>` until recognition exists.
Required Test: None.

**L-05**
Severity: LOW
File: `packages/DtoB.Ipc/PipeProtocol.cs`, `DtoB.Ipc.csproj`
Problem: `DtoB.Ipc` targets plain `net8.0` but calls the Windows-only `GetNamedPipeServerProcessId` and hard-codes the product string `"Revit"` and endpoint prefix `dtob-revit-`. The protocol layer knows its host. The `JsonStringEnumConverter` also accepts raw integers, so `(IpcCommand)99` flows through.
Impact: Weakens the claimed layering and platform neutrality. Not a defect today.
Reproduction: Read the file.
Recommended Fix: Make the project Windows-targeted, or isolate the P/Invoke. Move the host product name into configuration.
Required Test: None.

**L-06**
Severity: LOW
File: `docs/status/evidence/phase00/host-disconnected.json`
Problem: The stack traces embed the developer's absolute path and username-derived directory. The disconnected evidence is an old-revision timeout, not the current `HostNotRunningException` fast-fail.
Impact: Minor privacy exposure and a stale failure mode (see H-02).
Reproduction: Open the file.
Recommended Fix: Regenerate under H-02 and strip absolute paths.
Required Test: None.

**L-07**
Severity: LOW
File: Repository root, `DtoB_Review/`
Problem: An untracked, full nested copy of the repository (`DtoB_Review/` with its own `.git`) sits in the working directory. It breaks M-07's test, doubles file searches, and risks reviewing the wrong tree.
Impact: Confusing workspace state.
Reproduction: `git status` shows `?? DtoB_Review/`.
Recommended Fix: The owner decides whether to delete or ignore it. I did not touch it.
Required Test: None.

## 2. Boundary, Dependency and Scope

| Item | Result |
|---|---|
| Revit API leakage | **None.** Only `DtoB.Revit2025` references RevitAPI/RevitAPIUI with `Private=false`, and the adapter output has no Autodesk DLL. IPC never touches `Document`. |
| Dependency direction | **Correct.** Core → Geometry → Cad/Bim → Analysis. Desktop and Revit2025 depend on Ipc only. `DtoB.Revit.Core` depends on Core only, has no Revit API, and is used only by tests. The name is misleading but harmless. The dependency test covers all 9 production projects. |
| Revit 2025 / .NET 8 | **Correct.** The target is `net8.0-windows`, x64. Revit's runtime config declares net8.0. Note that this is evidence from one installation, 25.4.50.35. |
| Units | mm core. The 304.8 conversion lives only in `Revit.Core`. The adapter does not call it yet, so the conversion is only unit-tested. See M-02 for the source-unit gap. |
| Scope | No PHASE 01+ feature. Extras are small: `ReviewStatus`, `Parameters`, `--verify-pid`, `SemanticPrediction<T>` (L-04). See M-05 for undisclosed deletions. |
| Over-engineering | Moderate and acceptable. The 4-worker server is more than a PING PoC needs, and it introduced H-01. |
| Silent failures | Unsupported CAD entities are explicit. Silent paths: a dead listener (H-01), bad `--verify-pid` arguments (L-01) and log pruning/log loss (L-02). |
| Local IPC security | `CurrentUserOnly`, a bounded 16 KiB frame, an OS-level server PID check and request deadlines are sound for the trusted same-user model that ADR-004 states. Resource starvation by same-user peers is not handled (M-06, H-01). |
| Serialization/versioning | `schemaVersion` is probed before the typed parse. Unknown fields are rejected. Missing arrays produce typed errors. Discriminator order is documented and tested. The "any addition is a major change" policy is strict but documented. |

## 3. The Three Documented Revit Warnings (MSB3277)

I reproduced them on a clean clone. They are version-unification warnings from RevitAPI/RevitAPIUI pulling .NET Framework identities (`System.Drawing` 4.0, `WindowsBase` 4.0, `Microsoft.VisualBasic` 10.0) onto .NET 8 reference assemblies. `UseWPF` is correctly gone, yet `WindowsBase` still appears, so it comes from the Revit API references.

**Acceptable for PHASE 00 under conditions.**
1. The adapter uses none of these assemblies. Loading and PING worked in the old-revision evidence.
2. The report must state the true count, 3, and the three identities. It currently says 2.
3. `TreatWarningsAsErrors` does not catch MSB3277, but `Verify-Phase00Foundation.ps1` fails on any change in code or identity. That gate runs only with a local Revit installation. CI cannot run it. State that explicitly.
4. They must be re-evaluated when a Revit API call touches `System.Drawing` or WPF types (ribbon icons, dialogs). That work is PHASE 12 or later. Passing PING does not validate it.

## 4. Exit Criteria Verification

Task criteria, `tasks/PHASE_00.md` §8:

| # | Criterion | Verdict | Basis |
|---|---|---|---|
| 1 | Desktop Framework 확정 | **Met** | ADR-001 compares WPF, WPF+WebView2 and WinUI/web, and picks WPF. The Desktop project builds in CI. I did not launch the window. |
| 2 | Analysis Engine 전략 확정 | **Met** | ADR-002 and an explicit in-process boundary. `FoundationAnalysisEngine` returns `ANALYSIS_NOT_IMPLEMENTED` and has a test. |
| 3 | Revit 구조 확정 | **Met** | Isolated `net8.0-windows` x64 adapter, no Autodesk DLL in the output, and the dependency test. The adapter builds from a clean clone with Revit installed. Load verification applies to the older revision only (H-02). |
| 4 | 통신 PoC 성공 | **Not met for HEAD** | Loopback tests pass (44/44, clean clone). Real-host evidence is from a different revision (H-02). A reproduced defect permanently disables the listener (H-01), and four idle clients starve it (M-06). |
| 5 | Unit/Coordinate 규칙 확정 | **Partially met** | The conventions are documented in ADR-006 and unit-tested (rotation, mirroring, composition). Validation does not enforce them (M-01) and the source-unit model conflicts with the ADR (M-02). |
| 6 | CAD IR/BIM IR 최소 Schema | **Partially met** | Typed contracts, fixtures, round-trip tests, version probing and typed errors exist. Gaps: M-01, M-02, M-03, M-04. |
| 7 | Repository/Test 구조 확정 | **Met, with caveats** | The layout matches the plan, work is committed and pushed, and CI is green. The suite is fragile (M-07). Documentation drifts from the code (H-02, M-05). |
| 8 | Architecture ADR 작성 | **Met** | 10 ADRs with alternatives and amendments. The report's "written before implementation" claim still contradicts the ADR amendments (H-02). |

Master-plan criteria, `DtoB_FINAL_MASTER_PLAN.md` §14:

| Criterion | Verdict |
|---|---|
| Desktop Framework 확정 | Met (1) |
| Revit Add-in 구조 확정 | Met (3) |
| 통신 방식 확정 | Method decided in ADR-004 (met). The PoC is not met for HEAD (4). |
| CAD Reader 방향 확정 | **Met only as a recorded owner deferral.** ADR-005 evaluates ODA, RealDWG and AutoCAD-assisted candidates and records the deferral to PHASE 03/04. This is acceptable only if the project owner agrees. Passing it as `PASS_WITH_KNOWN_LIMITATIONS` needs their explicit acceptance. |
| CAD IR 최소 Schema | Partially met (6) |
| BIM IR 최소 Schema | Partially met (6) |
| Repository 구조 확정 | Met (7) |
| Test Strategy 확정 | Met |

Required tests, `tasks/PHASE_00.md` §7:

| Test | Result |
|---|---|
| Desktop build | **Verified** (CI and clean clone). |
| Revit add-in build | **Verified** locally on a clean clone (0 errors, 3 warnings). CI does not build it. |
| Core builds without Revit DLL | **Verified** (0 warnings). |
| IPC PING/PONG | Loopback verified. Real host verified for an older revision only. |
| Unit conversion smoke | Verified. |
| CAD IR/BIM IR serialization | Verified for happy and common error paths. |

## 5. Disposition of the Earlier Review

| Earlier finding | Current status |
|---|---|
| H-01 uncommitted work, unproven CI | **Resolved.** Committed, pushed, CI green. The junk root file is gone. |
| H-02 shallow ADRs | **Resolved.** Alternatives tables and a recorded deferral. |
| H-03 undefined CAD frame/arc/normal | **Documented, not enforced** (M-01). |
| M-01 `UseWPF` and a warning gate | `UseWPF` removed, identity gate added locally. The report count is wrong (H-02). |
| M-03 versioning and null handling | **Resolved.** |
| M-04 prediction not enforced | **Partly resolved**, bypassable (M-03). |
| M-05 exact floating equality | **Resolved** for the tolerance overload (M-04 remains). |
| M-06 serial server and self-DoS | **Reworked and worse in one respect** (H-01, M-06). |
| M-07 IPC types and PID trust | **Resolved.** |
| M-08 dependency-direction test | **Resolved.** |
| M-09 handle normalization | **Resolved.** |

## 6. Required Before Re-review

1. H-01: fix the listener fault path, add the connect-drop and forced-fault tests.
2. H-02: re-run real-host verification on the final commit, commit current evidence, and correct the report and checklist.
3. M-01 to M-04 and M-07: targeted fixes, or recorded owner acceptance as known limitations.
4. M-05: the owner confirms the deletions and governing-document rewrites.
5. M-06 and the LOW items may be accepted into PHASE 01 as tracked items if the owner agrees.

## Verdict

CHANGES_REQUIRED

## Final Re-review

**Reviewed Git Commit SHA:** `466c7f7a1b7135a12ee80b3200e8959cd10c068e`
**Build Result:** `PASS` (Core: 0 errors/warnings, Solution: 0 errors, 3 MSB3277 warnings)
**Test Count/Result:** `56/56 PASS`
**Real Revit Host Evidence Result:** `PASS` (Verified from `docs/status/evidence/phase00-review/re-review-final-ping/` and `re-review-final-restart/`. PING/PONG works, restart is properly handled, and `RequestId` matching is correct).

### Status of Previous HIGH/MEDIUM Findings

| Finding | Status | Notes |
|---|---|---|
| H-01 (IPC listener fault recovery) | **Resolved** | The listener catches `IOException` during `WaitForConnectionAsync` and continues without faulting the worker. |
| H-02 (Final Revit host evidence) | **Resolved** | The evidence in `docs/status/evidence/phase00-review/re-review-final-*/` matches the final IPC contracts (with `RequestId` and correct logging). |
| M-01 (CAD coordinate/insertion path invariants) | **Resolved** | `CadContracts.cs` now properly asserts that `InsertionPath.Length == 0` and `Transform` is Identity for block definitions. |
| M-02 (Original source unit preservation) | **Resolved** | `SourceUnit` conversion is strictly verified against `SourceToMillimetersScale` using a switch expression. |
| M-03 (Prediction evidence bypass) | **Resolved** | The `IsSemanticPrediction` flag was replaced with proper structural validation semantics checking `Prediction` and `Provenance.Confirmation`. |
| M-04 (Tolerance-aware validation) | **Resolved** | A dedicated `ValidateGeometry(GeometryTolerance)` method properly checks bounded equality and closure. |
| M-05 (Owner-approved deletion documentation) | **Resolved** | `PHASE_00_REPORT.md` explicitly documents the owner's approval for deleting `FILE_INDEX.md` and `MASTER_PLAN.md`. |
| M-06 (Idle client starvation) | **Resolved** | The server reads requests with a maximum 250ms deadline, aborting idle client connections and preventing starvation. |
| M-07 (Test stability with nested repos/PID) | **Resolved** | `FoundationDocumentTests` climbs directories searching for `DtoB.sln` instead of `.git`. `IpcTests` uses `Environment.ProcessId` instead of `explorer.exe`. |

### Exit Criteria Verification

All previous partial/failing exit criteria have been addressed. The project cleanly defines the architecture, validates the boundaries, establishes the IPC patterns with proper fault tolerance, enforces CAD/BIM contract constraints, and provides comprehensive documentation/evidence.

## Verdict

PASS
