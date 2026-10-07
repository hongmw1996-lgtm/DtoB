# PHASE 00 Independent Review (Architecture / Adversarial)

Reviewer: Antigravity · Branch: `phase/00-architecture` · Date: 2026-10-07

**Verdict: CHANGES_REQUIRED** (BLOCKER 0 / HIGH 3 / MEDIUM 9 / LOW 8)

## 0. Verification Method and Limits

| Codex claim | Independent result |
|---|---|
| Source read in full | Read all `*.cs`, `*.csproj`, `Directory.Build.props`, `global.json`, CI, install script, addin template, tests and report. ADRs were checked for Alternatives/Decision. |
| 27 tests pass | **Partially verified.** The existing `artifacts/phase00/tests.log` shows 27/0/0, and the test count matches the source (IPC 11 plus Contract and Geometry). I could not re-run the tests. `dotnet` fails to start child processes in my review sandbox (`MSB3883 / access denied`). An unmodified rebuild was not possible. |
| Core and full-solution builds, 2 warnings | **Not independently reproduced.** The build log is consistent with the claim. See M-01 for why the "TreatWarningsAsErrors" framing is misleading. |
| Real Revit PING/PONG, shutdown, restart | **Evidence is internally consistent but not reproducible by me.** The add-in log timeline, request IDs and Desktop JSON agree (STARTED 04:40:19 → PONG → STOPPED 04:41:28; restart PID 16156). Neither `%LOCALAPPDATA%\DtoB\phase00\connector` nor the Revit2025 `bin` output exists in my environment, so the DLL-hash match claim could not be re-checked. |
| 10 ADRs written before implementation | **Contradicted or unverifiable.** All ten ADR files have the same mtime, 13:42:49. Every source file is older (13:28–13:38). See M-02. |
| 2 Revit warnings only | Consistent with the log. Acceptability is analyzed in section 3. |
| No PHASE 01 work | No PHASE 01 features found. |

Revit API isolation was verified from the project graph. `DtoB.Core`, `Geometry`, `Cad`, `Bim`, `Ipc`, `Analysis`, `Revit.Core` and `Desktop` do not reference `Autodesk` or `RevitAPI`. Only `DtoB.Revit2025` does, with `Private=false`.

---

## 1. Issues

### HIGH

**H-01**
Severity: HIGH
File: Repository state (`git status`, `git log`, `.github/workflows/foundation.yml`)
Problem: The whole implementation is untracked. `git diff main` shows only the 43 staged specification files. Apps, packages, services, revit, tests, ADRs and docs/status are all `??`. The 4 governing docs (`ARCHITECTURE.md`, `README.md`, `START_HERE.md`, `CODEX_INSTRUCTIONS.md`) carry unstaged edits (+43/-91). `main` HEAD is a chain of "Delete …" commits. A stray junk file named `The string is missing the terminator…` sits in the repo root. The CI workflow has never run.
Impact: The reviewed artifact is not an immutable, reproducible revision. "Git diff against main" cannot be a review basis. Nothing guarantees that what was reviewed is what gets merged. The Phase Gate needs a commit hash and none exists. The CI claim is unproven.
Reproduction: `git status --short`; `git diff --stat main`; `git ls-files apps packages`.
Recommended Fix: Delete the junk file after owner approval. Commit PHASE 00 on `phase/00-architecture` in logical commits, with spec/baseline changes separate from the implementation. Push, then confirm that `foundation.yml` runs green. Record the commit hash in the report. Justify the `ARCHITECTURE.md` rewrite, or revert it.
Required Test: A fresh `git clone` of the branch followed by the documented commands (`dotnet build Core`, `dotnet test`) succeeds. The CI run is green and linked in the report.

**H-02**
Severity: HIGH
File: `docs/adr/ADR-005-Drawing-Reader.md`, `tasks/PHASE_00.md` (00A/00B/00D/DWG Reader candidates), `DtoB_FINAL_MASTER_PLAN.md` Exit "CAD Reader … 확정"
Problem: The task requires candidate comparison. It names Desktop framework (WPF/WebView2), Analysis (C# vs C#+Python), IPC (pipe/HTTP/gRPC) and DWG reader candidates. Each ADR has a one-line "Alternatives" list (for example "Named pipes, localhost HTTP, gRPC.") and no evaluation criteria, scoring, rejection reasons or risks. The DWG reader direction is "deferred". The master-plan Exit Criterion "CAD Reader 방향 확정" is therefore not met on the record. The "user approval" cited in the report is not recorded in any file.
Impact: ADRs are decision statements, not decision records. Nobody can later tell why pipe was chosen over HTTP/gRPC, or why in-process C# was chosen over Python AI services. The DWG SDK choice is the largest technical and licensing risk in the project and is left open with no criteria.
Reproduction: Open ADR-001 to ADR-005. The `## Alternatives` section is a single line. `## Context` and `## Migration Impact` are copy-pasted identical text in every ADR.
Recommended Fix: Add to each ADR a short comparison table (criteria, per-option verdict, rejected-because). For ADR-005, evaluate at least ODA, RealDWG and AutoCAD-assisted reading against license, .NET 8 compatibility, Xref/block fidelity and handle preservation. Record who approved the deferral and the conditions to resolve it (target phase). Otherwise mark the criterion as an explicitly agreed `PASS_WITH_KNOWN_LIMITATIONS`.
Required Test: A document check, verified by the reviewer, that every ADR lists at least 2 evaluated alternatives with reasons. ADR-005 contains a dated approval note for the deferral.

**H-03**
Severity: HIGH
File: `packages/DtoB.Cad/CadContracts.cs` (`CadEntity.Transform`, `CadPrimitive`, `CadInsert`), `docs/adr/ADR-006`, `ADR-008`
Problem: The coordinate-space semantics of CAD IR are undefined. (a) It is not stated whether `Points` of an entity are in the block-local frame or the world frame, or whether `Transform` is the INSERT-accumulated transform or the entity's own. (b) The meaning of `Points` for Arc and Circle (center? `Points[0]`?) and the sweep direction for `StartAngleRadians`/`EndAngleRadians` are not documented. (c) DWG's OCS/extrusion normal (arbitrary-axis algorithm) has no field. `Transform3.Placement` supports only Z-rotation. Mirrored or flipped-normal arcs, polylines and text lose information with no diagnostic. (d) `InsertionPath` entries are not defined (INSERT handle or Id?). (e) The meaning of `SourceToModel` relative to `SourceUnit == "mm"` is unclear.
Impact: PHASE 04 (CAD Data Engine) will either guess these conventions or need a major schema change. That would need a new ADR under AGENTS rule 14 and would invalidate PHASE 00 fixtures. Wrong Arc direction and normal produce silently wrong walls and columns, which violates the "hidden assumption / data loss" review focus.
Reproduction: Read `CadPrimitive.Validate`. It checks only count, radius and finiteness. No test places an entity inside a nested transformed INSERT with an Arc.
Recommended Fix: In ADR-008 and ADR-006, specify frame semantics (recommended: entity geometry is stored in the definition frame, and the accumulated INSERT transform is the entity's `Transform`). Define the Arc/Circle layout and angle direction. Add an optional `ExtrusionNormal` (default +Z) or require non-+Z normals to produce an `UNSUPPORTED_ENTITY` diagnostic. Define `InsertionPath` elements.
Required Test: Fixture with a nested INSERT, a mirrored INSERT and an Arc. Test that the world-space Arc endpoints are as expected after `Transform`. Test that a non-+Z normal is either represented or diagnosed.

### MEDIUM

**M-01**
Severity: MEDIUM
File: `Directory.Build.props`, `revit/DtoB.Revit2025/DtoB.Revit2025.csproj`
Problem: `TreatWarningsAsErrors=true` does not elevate MSBuild task warnings such as MSB3277. That is why the full build is "successful with 2 warnings". The two warnings are therefore not governed by any gate. A new MSB3277 conflict, such as a Revit API assembly version mismatch, will pass silently. `UseWPF=true` is set in the add-in project but no WPF type is used. It is the likely source of the `System.Drawing`/`Microsoft.VisualBasic` conflicts.
Impact: A warning budget that cannot regress is unenforced. The unnecessary framework reference widens the adapter dependency surface.
Reproduction: Read the csproj. Grep `Revit2025/*.cs` for `System.Windows` (no match).
Recommended Fix: Remove `UseWPF` from `DtoB.Revit2025` and rebuild. If the warnings disappear, drop the "known limitation". If not, record the conflicting assembly identities and add `MSBuildTreatWarningsAsErrors` with a scoped `NoWarn`/allow-list for exactly this documented warning. Also remove the unused `ProjectReference` to `DtoB.Revit.Core`, or add a call site.
Required Test: A build-log check (script or CI step) that fails if the warning count or codes change from the documented baseline.

**M-02**
Severity: MEDIUM
File: `docs/adr/ADR-001…010`, `docs/status/PHASE_00_REPORT.md` ("created before foundation source implementation")
Problem: ADR mtimes are all 13:42:49, 4 to 14 minutes after the source files. Nothing proves the ADR-first ordering. Every ADR contains a literal `?` where an em dash was intended ("Accepted ? user-approved"), so the files were written through a lossy encoding path. `docs/PHASE_00_BUILD.md` and the report show the same corruption in the Revit path (`D:\프로그램\Revit 2025`).
Impact: An unverifiable process claim in the report. Garbled non-ASCII text in the permanent record.
Reproduction: `Get-ChildItem docs/adr | Select Name,LastWriteTime`; `Select-String '\?' docs/adr`.
Recommended Fix: Commit history is the evidence of ordering (see H-01). Remove the claim or restate it as "ADRs finalized after implementation". Re-save all docs as UTF-8 and fix the corrupted characters.
Required Test: A check script (or CI) that fails on `?`/U+FFFD in `docs/**/*.md`.

**M-03**
Severity: MEDIUM
File: `packages/DtoB.Core/Contracts.cs` (`IrJson`, `Contract.Version`), `CadContracts.cs`, `BimContracts.cs`
Problem: (a) `UnmappedMemberHandling.Disallow` plus an integer-only `SchemaVersion` means a document from a newer producer fails in `Deserialize` with a `JsonException` about an unmapped member. `Validate()` never runs, so the clear "Unsupported schema major version" message is unreachable. There is no minor version, so any additive change is a breaking change. (b) The documents are positional records without `required`. STJ on .NET 8 fills missing constructor parameters with `default`, so `{"schemaVersion":1}` yields `Layers == null`. `Validate()` then throws `NullReferenceException`, not `InvalidDataException`. (Code-read, not executed because of the sandbox limit.) (c) The discriminator `$kind`/`$category` must be the first property (documented). A non-.NET producer, such as the planned Python service, will fail unpredictably.
Impact: Non-explicit failures at the most important data boundary. Forward-compatibility policy is undefined.
Reproduction: Deserialize `{"schemaVersion":1}` as `CadDocument`, then call `Validate()`. Deserialize a v2 document with an extra field.
Recommended Fix: Parse `schemaVersion` first (small envelope probe) and reject unsupported versions before the full parse. Add `schemaMinor` plus a unknown-field policy, or document that unknown fields are forbidden in IR. Make `Validate()` null-safe, or add `required` on the arrays. Document the key-order requirement as a producer contract.
Required Test: Tests for a missing array, a v2 document, an unknown field and an out-of-order discriminator, each asserting a typed, explicit error.

**M-04**
Severity: MEDIUM
File: `packages/DtoB.Bim/BimContracts.cs` (`BimObject.Validate`)
Problem: Only `Status == Suggested` requires a `Prediction`. A `SourceDrawing`-origin object can be flipped to `Confirmed` with `Prediction = null`, and validation passes. The recognition evidence and confidence are dropped. ADR-009 and the report say "confirmed predictions retain evidence", but that is not enforced.
Impact: This violates AGENTS rule 3 (every semantic prediction has confidence/evidence) across review transitions in PHASE 11.
Reproduction: `(level with { Status = Confirmed, Prediction = null })` where `Provenance.Origin == SourceDrawing` (the existing test uses a Manual level for the pass case only).
Recommended Fix: For `SourceDrawing` provenance, require `Prediction` for `Suggested`, `NeedsReview` and `Confirmed`. Require it for `Confirmed` unless the origin is `Manual`.
Required Test: A SourceDrawing object with Confirmed and no Prediction is rejected. A Manual object with Confirmed and no Prediction passes.

**M-05**
Severity: MEDIUM
File: `packages/DtoB.Geometry/Geometry.cs`, `BimContracts.cs` (`BoundaryContract`), `Segment3.Validate`
Problem: Geometry predicates use exact floating equality. `boundary[0] == boundary[^1]` is the closure test, `Start != End` is the degeneracy test, and `Matrix[12] == 0` is the affine test. The DWG-derived closure test will reject near-closed polylines. A sliver segment of 1e-9 mm passes as valid. AGENTS rule 13 forbids arbitrary hard-coded tolerance. The correct fix is the caller-supplied `GeometryTolerance` that already exists but is not wired in.
Impact: Closure and degeneracy semantics will be wrong for real CAD data.
Reproduction: `BimFloor` with a last point offset by 1e-12.
Recommended Fix: Pass `GeometryTolerance` into the geometry predicates. Keep exact checks only where the exactness is intentional (the Transform affine row).
Required Test: A near-closed boundary passes with a supplied tolerance and fails with a tight one. A degenerate segment of length below the tolerance is rejected.

**M-06**
Severity: MEDIUM
File: `packages/DtoB.Ipc/PipeProtocol.cs` (`PingServer.RunAsync`)
Problem: The server has `maxNumberOfServerInstances = 1` and serves connections serially with a 3 s request deadline. Any same-user process, or a stuck Desktop, that connects and idles blocks all other clients for 3 s. Repeated connects keep the endpoint unavailable. A fatal error in `CreatePipe()` inside the loop ends the listener task, and the only signal is a log line. The server then appears "disconnected" to the Desktop with no health state. `DisposeAsync` re-throws the stored exception, so `OnShutdown` returns `Result.Failed`.
Impact: Local self-DoS and silent loss of the listener. This is a lifecycle weakness that PHASE 12 will inherit.
Reproduction: Open a raw `NamedPipeClientStream` and send nothing. A second `PingAsync` waits for the first deadline (the existing idle test only checks recovery after the deadline).
Recommended Fix: Allow multiple server instances, or accept connections and handle each in a bounded task. Make the listener restart on pipe-creation failure with a logged, bounded retry. Expose a `Faulted` state. Make `DisposeAsync` swallow a faulted `loop` after logging.
Required Test: A concurrency test: an idle connection is open, a second client still receives PONG within the deadline. A test that injects a pipe-creation failure and checks that the listener recovers.

**M-07**
Severity: MEDIUM
File: `packages/DtoB.Ipc/PipeProtocol.cs`, `docs/adr/ADR-004-IPC.md`
Problem: There are no protocol-level types. The framing is generic, but the message types are `PingRequest`/`PingResponse` with magic strings (`"PING"`, `"PONG"`, `"ERROR"`, `"Revit"`). The IPC options are default `JsonSerializerOptions`, separate from `IrJson`. The server and client versions are exact-match (`== 1`), with no negotiation. The identity check trusts self-reported fields: `Host.ProcessId` and `Product` come from the peer, so the "host mismatch" test only exercises a mismatched forged value.
Impact: Adding any second command in PHASE 12 changes wire contracts. Magic strings invite typos. ADR-004 documents none of this, and an impersonating same-user process passes the client checks.
Reproduction: Read the code. `ClientRejectsForgedResponse("host")` only covers a differing PID.
Recommended Fix: Introduce a minimal request/response envelope (`Command` enum, `Status` enum, `Error` code) now. Keep it a PHASE 00-sized change. State in ADR-004 that the pipe is current-user only, with no authentication, and that same-user processes are trusted. Verify the pipe server's process ID from the pipe handle on the client (`GetNamedPipeServerProcessId`).
Required Test: A forged server owned by a different PID but reporting the expected PID is rejected.

**M-08**
Severity: MEDIUM
File: `docs/status/PHASE_00_REPORT.md`, `.github/workflows/foundation.yml`, `docs/PHASE_00_BUILD.md`
Problem: Real-host evidence is a one-time manual procedure on one machine. It depends on a hard-coded local path (`D:\프로그램\Revit 2025`), a manual PID and a local deployment. CI covers Core, Desktop and tests only. The Revit2025 adapter build is not in CI, and a Revit API change cannot be detected. The tests project does not reference the Desktop or the Revit adapter, so the "no Autodesk dependency" test covers only 7 assemblies and not the Desktop or the add-in project.
Impact: Build reproducibility for the adapter is local-only. A regression in `DtoB.Revit2025` or the Desktop boundary will not be caught by CI.
Reproduction: Review the workflow.
Recommended Fix: Add a documented self-hosted or manual gate script (`scripts/Verify-Phase00Host.ps1`) that builds, deploys and PINGs, and keeps its output under `docs/status/evidence/`. Add a project-reference graph test (parse `*.csproj`) that asserts the allowed dependency directions, including that `Desktop` and the Core packages never reference `Revit*`.
Required Test: A dependency-direction test that fails when a forbidden `ProjectReference` or `Reference` is added.

**M-09**
Severity: MEDIUM
File: `packages/DtoB.Core/Contracts.cs` (`CadIdentity`), `SourceReference`
Problem: Source identity is weak in three places. (a) DWG handles are hexadecimal strings with no normalization (`"FF"` and `"ff"` give different identities). (b) `SourceReference` carries both `Handle` and `CadEntityId`, which are redundant and never checked for consistency. A BIM object can claim `Handle=A` and `CadEntityId` for a different handle. (c) BIM provenance is not validated against the CAD document (an intentional PHASE 00 limit, but not listed in "Known Limitations").
Impact: Source trace can silently break (AGENTS rule 6). Identity collisions or mismatches are possible after a reader changes its case convention.
Reproduction: `CadIdentity.Create(id, "ff") != CadIdentity.Create(id, "FF")`.
Recommended Fix: Define a handle normalization (upper-case hex) in ADR-007 and apply it in `Create`. Derive or verify `Handle` against `CadEntityId` in `SourceReference` validation. Add the cross-document check to the Known Limitations.
Required Test: Case-variant handles produce the same identity. A mismatched Handle/CadEntityId pair is rejected.

### LOW

**L-01**
Severity: LOW
File: `CadContracts.cs`, `BimContracts.cs`, `Geometry.cs` (records holding `double[]`, `Point3[]`, `string[]`)
Problem: Records advertise value equality, but array members compare by reference. `Transform3.Equals` and `CadEntity.Equals` are wrong across copies. Arrays are mutable after `Validate()`.
Impact: Latent equality and immutability bugs. Tests such as `o == door` work only on the same instance.
Reproduction: `new Transform3([...]) == new Transform3([...])` is false.
Recommended Fix: Use `ImmutableArray<T>` or custom equality. Alternatively, document that records here are not value-comparable.
Required Test: Equality test on two independently deserialized identical documents.

**L-02**
Severity: LOW
File: `ConnectorApplication.cs` (`Log`)
Problem: An append-only log named `revit-{pid}.log` is never rotated. PID reuse appends to old files. `Log` swallows all failures and falls back to `Trace`, so persistent loss of logging is invisible. A `TaskDialog` on startup failure blocks the Revit startup thread.
Impact: Log growth and ambiguous history. Silent log loss.
Reproduction: Review the code.
Recommended Fix: Include the start timestamp in the log file name and set a simple retention rule.
Required Test: Not required for PHASE 00.

**L-03**
Severity: LOW
File: `apps/DtoB.Desktop/MainWindow.xaml.cs`
Problem: `Closed` cancels the token while a request may be pending. The `OperationCanceledException` path then writes to UI controls of a closed window. The catch list is closed (specific exception types), so any other exception from `async void` crashes the process. Connection status is last-PING only. There is no UI state for "Revit not running" versus "hung".
Impact: Minor robustness issue in the verification-only UI.
Reproduction: Click PING and close the window immediately.
Recommended Fix: Skip UI updates after cancellation. Handle unexpected exceptions with a status message.
Required Test: Not required (UI shell).

**L-04**
Severity: LOW
File: `BimContracts.cs` (`ReviewStatus`, `Parameters`)
Problem: `ReviewStatus` (a PHASE 11 workflow concept) and the open `Parameters` bag (`Dictionary<string, JsonElement>`) are in the minimal BIM IR. ADR-009 rejected "property bags".
Impact: Mild scope creep and an uncontrolled extension point that can carry untyped semantics.
Reproduction: Read the contract.
Recommended Fix: Keep `ReviewStatus` only if required by the provenance rule (see M-04). Define what may go into `Parameters` or remove it.
Required Test: None.

**L-05**
Severity: LOW
File: `scripts/Install-Phase00Connector.ps1`
Problem: The script copies every `DtoB.*` file, including PDBs, into the user profile. It refuses to run while any Revit process exists but does not verify the deployed hash. The evidence JSON has a hash comparison done by hand, not by a script.
Impact: Deployment is not reproducible from the repo.
Reproduction: Review the script.
Recommended Fix: Emit a SHA-256 manifest as part of the install script.
Required Test: None.

**L-06**
Severity: LOW
File: `packages/DtoB.Ipc/PipeProtocol.cs` (`PingClient`)
Problem: A client timeout covers connect and the whole round trip, so "Revit not running" takes the full 5 s to report. A hung add-in cannot be told apart from a missing add-in.
Impact: Slow and ambiguous user feedback.
Reproduction: PING a nonexistent PID.
Recommended Fix: Check the process (`Process.GetProcessById`) and use a short connect timeout.
Required Test: PING a nonexistent PID fails fast with a distinct error.

**L-07**
Severity: LOW
File: `datasets/controlled/phase00/*.json`
Problem: The fixtures are synthetic only (acknowledged). No real or controlled DWG-derived fixture exists to challenge the contracts (see H-03).
Impact: The schema is validated only against self-authored data.
Reproduction: Read the fixtures.
Recommended Fix: Add a hand-written fixture that mimics real DWG output (nested blocks, mirrored INSERT, an unsupported entity with raw properties) before PHASE 04.
Required Test: Round-trip plus validation on that fixture.

**L-08**
Severity: LOW
File: `docs/status/PHASE_00_REPORT.md`
Problem: The report says "Ready for independent review" and lists "Not performed: commit/push", and it also carries a self-assessed `PASS_WITH_KNOWN_LIMITATIONS` status. The task Phase Gate allows only the Review owner to issue it. The Known Limitations omit the items in H-03, M-03 and M-09.
Impact: Overstated assurance.
Reproduction: Read the report header and Known Limitations.
Recommended Fix: Replace the self-assigned status with "Submitted for review". Add the missing limitations.
Required Test: None.

---

## 2. Boundary, Dependency and Scope Review

| Item | Result |
|---|---|
| Revit API leakage outside the adapter | **None found.** Only `DtoB.Revit2025` references `RevitAPI`/`RevitAPIUI` (`Private=false`). IPC never touches `Document`. |
| CAD parser ↔ Revit coupling | None. `IDrawingReader` is an interface only. |
| Dependency direction | Correct: Core → Geometry → Cad/Bim → Analysis. Desktop and Revit2025 depend on Ipc only. `Revit.Core` depends on Core only. It holds no Revit API, so the name is misleading but harmless. |
| Units | mm in core, 304.8 conversion only in `RevitUnits` at the adapter boundary. Adapter use is not exercised in the host (it is unused by `ConnectorApplication`). |
| .NET 8 / Revit 2025 | The assumption is correct for Revit 2025. `net8.0-windows`, x64, `Private=false` are right. Evidence is a runtimeconfig reading from one installation (25.4.50.35). |
| Scope | No PHASE 01+ feature. Mild extras: `ReviewStatus`, `Parameters`, the `--verify-pid` evidence mode. All are small. |
| Over-engineering | Not significant. The Prediction / SemanticPrediction / Provenance trio is slightly heavy, but it supports AGENTS rules 3 and 6. |
| Silent failures | The Unsupported-entity path is explicit (diagnostic required). Listener death and log loss are silent (M-06, L-02). |
| Local IPC security | `CurrentUserOnly` plus a bounded frame (16 KiB) plus a deadline are sound for the local-user model. There is no authentication of the server (M-07) and serial handling allows self-DoS (M-06). |

## 3. The Two Revit Reference Warnings (MSB3277)

Acceptable **conditionally**. They are version-unification warnings on `Microsoft.VisualBasic` and `System.Drawing`, reached via the Revit API references. The add-in loaded and the PING/shutdown/restart succeeded, so they are not a functional blocker.

The conditions are:

1. First remove `UseWPF` from `DtoB.Revit2025` and retest (M-01). It is the cheapest likely cause.
2. If the warnings persist, record the assembly identities and add a warning allow-list gate, so new MSB3277 warnings fail the build.
3. Keep the "not validated beyond PING" statement, because no Revit API function is exercised.

## 4. Exit Criteria Verification

Task Exit Criteria (`tasks/PHASE_00.md` §8):

| # | Criterion | Verdict | Basis |
|---|---|---|---|
| 1 | Desktop Framework 확정 | **Partially met** | WPF/.NET 8 is decided and builds, and the shell runs. ADR-001 has no real alternative comparison (H-02). |
| 2 | Analysis Engine 전략 확정 | **Partially met** | In-process C# is decided, with an explicit boundary and test. The Python option is not evaluated in the ADR (H-02). |
| 3 | Revit 구조 확정 | **Met** | Isolated adapter, `net8.0-windows`, the Core/adapter split and the boundary test. The build ID is recorded. Reproduction is local only (M-08). |
| 4 | 통신 PoC 성공 | **Met (evidence-based)** | The real-host timeline is internally consistent. It was not reproduced by me. Lifecycle weaknesses are in M-06 and M-07. |
| 5 | Unit/Coordinate 규칙 확정 | **Partially met** | Units, axes, rotation and composition are defined and tested. The CAD frame/OCS/arc semantics are undefined (H-03). Exact-equality predicates conflict with the tolerance policy (M-05). |
| 6 | CAD IR/BIM IR 최소 Schema | **Partially met** | Typed contracts, fixtures and round-trip tests exist. Version and null-handling gaps (M-03), prediction enforcement (M-04) and frame semantics (H-03). |
| 7 | Repository/Test 구조 확정 | **Partially met** | The structure matches the requested layout. The work is uncommitted and CI is unproven (H-01, M-08). |
| 8 | Architecture ADR 작성 | **Partially met** | 10 files exist. They are shallow, the ordering claim is unsupported and the text is corrupted (H-02, M-02). |

Master-plan Exit Criteria (`DtoB_FINAL_MASTER_PLAN.md` §14):

| Criterion | Verdict |
|---|---|
| Desktop Framework 확정 | Partially met (as 1) |
| Revit Add-in 구조 확정 | Met |
| 통신 방식 확정 | Met (as 4) |
| CAD Reader 방향 확정 | **Not met** (deferred, no criteria or recorded approval, H-02) |
| CAD IR 최소 Schema | Partially met (as 6) |
| BIM IR 최소 Schema | Partially met (as 6) |
| Repository 구조 확정 | Partially met (as 7) |
| Test Strategy 확정 | Met (xUnit, synthetic fixtures, layered tests). Gaps are listed in M-08. |

Required tests from `tasks/PHASE_00.md` §7:

| Test | Result |
|---|---|
| Desktop build | Reported (not reproduced) |
| Revit add-in build | Reported (not reproduced) |
| Core builds without Revit DLL | Reported (0 warnings) and consistent with the project graph |
| IPC PING/PONG | Met by tests and real-host evidence |
| Unit conversion smoke | Met |
| CAD/BIM IR serialization | Met for the happy path. Error paths are weak (M-03). |

## 5. Required Before Re-review

1. H-01: commit, push and a green CI run.
2. H-02: ADR comparisons and the recorded DWG-reader deferral (or an explicit agreed `PASS_WITH_KNOWN_LIMITATIONS`).
3. H-03: define the CAD frame, arc and OCS semantics with tests.
4. M-01, M-03, M-04, M-06 (small, targeted fixes).
5. Update the report and its Known Limitations.

M-02, M-05, M-07, M-08, M-09 and the LOW items may be accepted into PHASE 01 as tracked items if the owner agrees.

## Verdict

**CHANGES_REQUIRED**
