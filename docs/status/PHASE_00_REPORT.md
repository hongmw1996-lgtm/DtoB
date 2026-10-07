# PHASE 00 REPORT

## Status

**PASS_WITH_KNOWN_LIMITATIONS** — PHASE 00 implemented and verified locally. Ready for independent Antigravity review. No PHASE 01 work, commit or push occurred.

Date: 2026-10-07 (Asia/Seoul). Authority: DtoB_FINAL_MASTER_PLAN.md, tasks/PHASE_00.md and the user's approved checklist/clarifications. The old MASTER_PLAN.md was not substituted.

## Implemented

- Ten accepted ADRs written before foundation source implementation.
- One DtoB.sln with ten projects, SDK 8.0.416 pin, nullable/deterministic builds and explicit dependencies.
- .NET 8 WPF technical verification UI: Revit PID entry, PING, connection/build/request status and explicit errors. No project management.
- Revit 2025 isolated x64 adapter and manifest deployment script; IExternalApplication startup/shutdown and host identity captured in valid API context. IPC never accesses Document.
- Current-user named-pipe PING/PONG: protocol version, request correlation, host identity, bounded frames, deadlines/cancellation, unsupported command errors, reconnect and UI-independent shutdown.
- Core diagnostics, drawing/revision/CAD identity, source references and mandatory BIM provenance. Optional domain Prediction metadata; semantic result wrapper requires confidence/evidence. Manually confirmed/non-predicted objects need no artificial confidence.
- Geometry in mm with explicit right-handed XYZ, Z-up/radian transforms, tested composition/mirroring and caller-supplied tolerance. Revit boundary mm/feet conversion.
- CAD IR document/layer/entity/block/text/dimension contracts; basic primitive/INSERT/unsupported variants, raw/effective metadata and raw properties. IDrawingReader interface only; no SDK dependencies.
- BIM IR document and eight typed categories, geometry/level/host/source contracts and validation.
- In-process Analysis boundary which validates CAD and returns ANALYSIS_NOT_IMPLEMENTED plus no BIM model.
- Synthetic CAD/BIM fixtures, xUnit regression tests, Windows portable-foundation CI definition, architecture and build/host documentation.

## Not Implemented

Actual DWG parsing or SDK integration, recognition rules, AI/ML/Python services, CAD/3D viewers, project create/open/save/settings, family catalog/mapping, document commands/ExternalEvent dispatch, Revit BIM generation/transactions/RVT output and later QA. These belong to subsequent phases. No future phase files were rewritten.

## Changed Files

Modified from the user's staged baseline: README.md, START_HERE.md, CODEX_INSTRUCTIONS.md, ARCHITECTURE.md. Original staged versions remain in the index; implementation changes are unstaged.

New or updated task files:

- .gitignore, global.json, Directory.Build.props, DtoB.sln, .github/workflows/foundation.yml
- apps/DtoB.Desktop/: project, App.xaml/.cs, MainWindow.xaml/.cs
- packages/DtoB.Core/: project and Contracts.cs
- packages/DtoB.Geometry/: project and Geometry.cs
- packages/DtoB.Cad/: project and CadContracts.cs
- packages/DtoB.Bim/: project and BimContracts.cs
- packages/DtoB.Ipc/: project and PipeProtocol.cs
- services/DtoB.Analysis/: project and AnalysisBoundary.cs
- revit/DtoB.Revit.Core/: project and RevitUnits.cs
- revit/DtoB.Revit2025/: project, ConnectorApplication.cs, DtoB.addin.template
- tests/DtoB.Tests/: project, ContractTests.cs, GeometryTests.cs, IpcTests.cs
- datasets/controlled/phase00/: README.md, cad.json, bim.json
- scripts/Install-Phase00Connector.ps1, docs/PHASE_00_BUILD.md
- docs/adr/ADR-001 through ADR-010 listed below
- docs/status/PHASE_00_CHECKLIST.md, this report, docs/status/evidence/phase00/*

The 43 initially staged specification/template/placeholder files were not reset or unstaged. An unrelated untracked file named like a PowerShell missing-terminator error existed at implementation start and was left untouched. No commit/push.

## Tests Added

27 executed xUnit cases covering CAD/BIM round trips and all eight BIM categories; source/occurrence identity and unsupported/raw property preservation; unsupported schema, invalid host/level/provenance; manual objects without prediction and required semantic evidence/confidence; explicit unimplemented analysis; Autodesk dependency isolation; mm/feet conversion; counterclockwise rotation/translation/mirror/composition; nonfinite geometry and tolerance validation; PING/reconnect/restart/shutdown; version/command errors; cancellation; oversize/truncated/malformed frames; idle request deadlines; forged host/version/correlation rejection; synchronous Revit-style shutdown under a non-pumping UI synchronization context.

The shutdown test deliberately performs a bounded synchronous wait to reproduce OnShutdown. xUnit1031 is suppressed only around that regression, with an explanatory comment.

## Test Results

**27 passed, 0 failed, 0 skipped.** TRX counters: total=27, executed=27, passed=27, failed=0, notExecuted=0. Final run duration displayed by runner: 271 ms.

Commands run:

```powershell
 dotnet build packages/DtoB.Core/DtoB.Core.csproj -c Release
 dotnet build DtoB.sln -c Release '-p:RevitInstallDir=D:\프로그램\Revit 2025'
 dotnet test DtoB.sln -c Release --no-build --logger 'trx;LogFileName=phase00.trx' --results-directory artifacts/phase00 '-p:RevitInstallDir=D:\프로그램\Revit 2025'
```

Raw local evidence: artifacts/phase00/core-build.log, build.log, tests.log and phase00.trx. These generated artifacts are ignored by Git. A concise verified summary and actual host outputs are preserved in docs/status/evidence/phase00/.

## Actual Revit Host Verification

Installed path: D:\프로그램\Revit 2025. Revit.exe/RevitAPI.dll/RevitAPIUI.dll FileVersion=25.4.50.35, ProductVersion=20260410_1515(x64). RevitAPI/RevitAPIUI/AddInJournalEngine runtimeconfig files declare net8.0. Revit.runtimeconfig.json does not exist and was not used as evidence.

1. Latest deployed DtoB DLL hashes were compared to current Release outputs; all matched. No Autodesk/Revit DLL was present in output. [Deployment/build/test summary](evidence/phase00/verification-summary.json).
2. Actual Revit startup PID 12672 logged STARTED at 04:40:19 UTC (13:40:19 KST), Revit=2025, Build=25.4.50.35. Desktop PID 26288 received PONG at 04:40:42 UTC, RequestId=959dd8b9-8465-421a-a428-5efea102b681. [Desktop response](evidence/phase00/host-ping.json), [matching Add-in log](evidence/phase00/revit-12672.log).
3. Windows Computer Use observed Desktop's Connected/PONG status with matching host/build. The displayed later button request f625c4ef-ddb4-4e82-b9ad-d35719d1f9d9 also appears in the Add-in log. No Revit model was opened or modified.
4. Revit was closed normally from Home. Process exit was verified and STOPPED logged at 04:41:28 UTC. A subsequent Desktop PING displayed Disconnected/error; [timeout evidence](evidence/phase00/host-disconnected.json) records the stopped PID 12672.
5. Revit restart PID 16156 loaded the same deployment. Desktop PID 25664 received correlated PONG RequestId=3d9d32aa-9d4d-415c-9b1a-02ea4e228fbc at 04:42:49 UTC (13:42:49 KST). [Restart response](evidence/phase00/host-restart.json), [restart host log](evidence/phase00/revit-16156.log).

This was Desktop-to-loaded-Revit communication, not a mock-host test. Revit Home and the technical Desktop application remain available for inspection after restart. Session selection is manual PID entry; connection status reflects the last PING.

Local deployment outside Git: %APPDATA%\Autodesk\Revit\Addins\2025\DtoB.Phase00.addin; %LOCALAPPDATA%\DtoB\phase00\connector; %LOCALAPPDATA%\DtoB\logs. No unrelated add-in was changed.

## Unsupported Cases

DWG SDK product decision remains deferred as explicitly approved. IDrawingReader is not a working parser. Synthetic fixtures do not demonstrate extraction/recognition. Non-mm source fixture units are rejected. Unknown CAD entities must be retained with source-linked diagnostics. Unknown schema major versions and unsupported IPC commands are explicitly rejected. Network/cross-user IPC is not supported.

V1 excludes Xref, Dynamic Blocks, complex nested blocks, messy CAD repair and curved/irregular walls. No such support was introduced.

## Known Limitations

- Revit adapter build retains **two MSB3277 warnings** for Microsoft.VisualBasic/System.Drawing version conflicts reached through installed Revit API references. They are not suppressed. Latest adapter loaded and PING/shutdown/restart succeeded; this does not validate unused Revit API functionality.
- Minimum geometry validation checks finite values, required dimensions/points and explicit closure; it does not prove polygon topology, self-intersection freedom, planarity or generation readiness.
- Source/BIM identities are persisted contracts; stability across revision replacement, re-recognition and generation reruns is not promised yet.
- No automatic session discovery or continuous connection monitoring. Current-user Windows pipes, PING-only messages and explicit PID selection are intentional PHASE 00 limits.
- JSON polymorphic metadata must precede ordinary properties under .NET 8. Call Validate() after deserialization. Raw unknown CAD data belongs in RawProperties rather than silently ignored top-level fields.
- Windows CI workflow was authored but not run remotely because no push was authorized. Local builds/tests were verified.

## Failure Cases and Fixes

Initial synthetic fixtures placed JSON discriminators after ordinary fields and encoded a negative coordinate as a string; fixed fixtures and round-trip tests now pass. Adding WPF references exposed missing System.IO imports; corrected. An initial Revit exit left a process after its window closed: server startup/async disposal could depend on UI synchronization context. Corrected with a background Task and ConfigureAwait(false) in disposal, added the non-pumping-context regression, then verified actual normal host exit/STOPPED and restart. Only task-launched Home sessions were cleaned up during redeployment. These initial failures are not counted as final passing host evidence.

Self-review also found malformed-frame exceptions could stop the listener; corrected exception handling and added malformed-client/reconnect coverage. Diff EOF whitespace was corrected. No unresolved failing test remains.

## Architecture Decisions

C#/.NET 8/WPF; in-process C# analysis; isolated installed-build Revit adapter; local named pipes; deferred SDK with reader interface only; mm/right-handed coordinates/caller-supplied tolerance; drawing/revision/source identity; versioned independent CAD/BIM IR; provenance distinct from prediction; one solution and layered tests. All changes preserve DWG -> CAD IR -> BIM IR -> Revit Adapter.

## ADR Files Created

- ADR-001-Desktop-UI.md
- ADR-002-Analysis.md
- ADR-003-Revit.md
- ADR-004-IPC.md
- ADR-005-Drawing-Reader.md
- ADR-006-Units-Coordinates.md
- ADR-007-Identity.md
- ADR-008-CAD-IR.md
- ADR-009-BIM-IR.md
- ADR-010-Build-Test.md

All are under docs/adr/ and accepted with user clarifications. No database/provider/generation semantics change was made.

## Build Result

Core-only Release: **succeeded, 0 warnings, 0 errors**, without Revit references. Full Solution Release (including Desktop and installed-API Add-in): **succeeded, 2 warnings, 0 errors**. All tests then passed. Missing RevitInstallDir fails clearly by design. API DLL CopyLocal=false was checked against output contents.

## PHASE 00 Exit Criteria

| Criterion (master plan + task) | Evidence | Result |
|---|---|---|
| Desktop/UI framework | ADR-001, compiled/launched WPF verification window | Satisfied |
| Analysis strategy | ADR-002, C# library and explicit boundary regression | Satisfied |
| Revit architecture/target | ADR-003, installed metadata, adapter build and actual load | Satisfied |
| Communication method and PING/PONG success | ADR-004, tests plus correlated actual host evidence | Satisfied |
| DWG reader direction | ADR-005, IDrawingReader + synthetic fixtures; product deferred by approval | Satisfied |
| Unit/coordinate convention | ADR-006 and geometry/conversion regression tests | Satisfied |
| Source/stable identity policy | ADR-007 and source occurrence tests | Satisfied |
| CAD IR minimum schema | ADR-008, typed contracts/fixture/round trip/unsupported retention | Satisfied |
| BIM IR minimum schema | ADR-009, eight categories/provenance/prediction tests | Satisfied |
| Repository/build/test structure | ADR-010, one solution, successful local build/tests | Satisfied |
| Architecture ADRs | Ten accepted records created before source implementation | Satisfied |

All PHASE 00 Exit Criteria satisfied: **Yes**, within approved scope and explicit deferred SDK decision. Every locally applicable required build/test/real-host criterion was verified. Known warnings/limits are listed above; this is not a zero-warning PASS.

## Independent Antigravity Review Readiness

**Yes.** Review current working tree (including untracked implementation files), ADRs, fixtures/tests, this report and preserved host evidence. Antigravity should independently assess adapter/API isolation, provenance/prediction semantics, transform conventions, error/lifecycle behavior and the two reference warnings. No independent review has yet been performed.

## Next Step

Stop for Antigravity PHASE 00 review. Do not start PHASE 01, commit or push without explicit user instruction.
