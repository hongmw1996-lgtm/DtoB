# PHASE 01 REPORT

## Status
REVIEW_REQUIRED — PHASE 01 implementation and Desktop Exit Criteria verified; independent Antigravity review is required before phase acceptance. No PHASE 02 implementation, commit or push.

## Implemented
.NET 8 WPF DtoB.exe shell; New/Open/Save/Save As/Close and editable project name; unsaved Save/Discard/Cancel; independent DtoB.Project library; UTF-8 schemaVersion 1 project JSON; UUID and creation-time preservation; authoritative runtime CurrentFilePath and informational LastKnownPath; same-directory temporary serialization/flush/replace before session state changes; local settings and newest-first normalized ten-entry recent list; visible missing paths; bounded session logging and nonrecursive Trace/direct-notification fallback; expected-error dialog and fatal handlers. PHASE 00 Connection Verification remains separate.

## Not Implemented
DWG upload/drawing management/parser/viewer, recognition, BIM model changes or Revit generation; PHASE 02; database/cloud/accounts/autosave/localization. Settings cache directory is a preference, not a cache-management engine.

## Changed Files
- DtoB.sln
- apps/DtoB.Desktop/DtoB.Desktop.csproj, App.xaml.cs, MainWindow.xaml, MainWindow.xaml.cs
- apps/DtoB.Desktop/ConnectionWindow.xaml, ConnectionWindow.xaml.cs, SettingsWindow.xaml, SettingsWindow.xaml.cs, ShellViewModel.cs
- packages/DtoB.Project/DtoB.Project.csproj, ProjectPersistence.cs, DesktopState.cs, LocalFileErrors.cs
- tests/DtoB.Tests/DtoB.Tests.csproj, FoundationDocumentTests.cs, ProjectShellTests.cs
- scripts/Verify-Phase00Host.ps1 (executable/process name only)
- docs/adr/ADR-011-Project-Persistence.md, ADR-012-Desktop-State.md
- docs/PHASE_01_DESKTOP.md, docs/status/PHASE_01_CHECKLIST.md, this report and docs/status/evidence/phase01
- New host evidence under docs/status/evidence/phase00-review/phase01-20261008 uses the existing verifier's directory convention; historical PHASE 00 evidence is unchanged.

CAD/BIM/Geometry/Core/Analysis/IPC/Revit implementation and ADR-001 through ADR-010 are unchanged.

## Tests Added
22 PHASE 01 cases: create/save/reload identity and metadata; Save As preserving original bytes/identity; moved-file actual path and clean state; six malformed/unsupported/missing-metadata JSON cases; nonexistent file; locked-target replacement and missing-directory Save As failure preserving target/session/temp cleanup; settings persistence/corruption; recent ordering/case-normalization/deduplication/limit/missing paths; three unsaved choices and cancelled save; recent-write failure after completed project save; corrupt recent-state preservation; filtered/FATAL logging/ten-file retention/direct fallback; recoverable local-state error classification. Existing 56 PHASE 00 cases remain in the complete suite; dependency-graph test adds the approved Project boundary.

## Test Results
78 executed, 78 passed, 0 failed, 0 skipped. Final error-fix Core-only and full Solution Release rebuilds passed, followed by all tests. Actual-host verifier repeated the same complete gates: 78/78 passed. Raw logs/TRX: artifacts/phase01/final-error-fix-20261008 and artifacts/phase00-review/phase01-20261008. Public source hashes, binary hashes and counters: docs/status/evidence/phase01/source-snapshot.json and verification-summary.json.

## Build Result
PASS using installed Visual Studio MSBuild 17.14.23/Roslyn, SDK files 8.0.416 and VSTest 17.14 with .NET 8.0.22. dotnet.exe is unavailable in this environment; no security exclusions or replacement executable were used. Core has no compiler warnings. Full solution has the three pre-existing Revit MSB3277 identity conflicts (Microsoft.VisualBasic 10.0/10.1, System.Drawing 4.0/8.0, WindowsBase 4.0/8.0), checked by the existing strict baseline gate. SDK-executable resolution diagnostics remain visible, but native MSBuild completes successfully.

## Manual Verification
2026-10-08, actual Release DtoB.exe:
- Launched and created a new project. Owner assisted native Windows Save dialog due computer-use modal input failure. Project saved as artifacts/phase01/manual/Project.dtob.
- Application terminated, relaunched and project opened from Recent. Name Untitled Project, UUID 89c4a5d0-3d36-4907-b180-f538ed0c0cb0, CreatedAt 2026-10-07T22:27:55.2517746+00:00, ModifiedAt 2026-10-07T22:30:19.0492676+00:00 and DtoBVersion 0.2.0 matched saved JSON. Title had no dirty marker.
- Recent survived restart; newest project was first; successful repeated open did not duplicate either of the two entries. Missing-path marker, limit and Windows normalization were verified by automated tests.
- Working Directory changed through Settings UI, saved, application closed/relaunched; Settings UI showed the same directory. Cache/Revit Target/Log Level were also retained. Corrupt settings/recent handling is covered by service/error-policy tests, not claimed as an actual startup-dialog walkthrough.
- Owner selected corrupted.dtob through Open. Initial execution exposed a real fatal error; fixed and rebuilt. Retry showed DtoB error / Open failed: Corrupted or invalid JSON document. Agent observed unchanged metadata behind the dialog; owner confirmed existing project remained after OK. Log records ERROR and normal subsequent close.
- Save As, locked-save failure, moved files and unsaved-choice branches are automated cases; exhaustive manual dialog coverage is not claimed.

Evidence: docs/status/evidence/phase01/manual-verification.json and desktop-log-excerpt.txt. The initial actual fatal failure also verifies FATAL logging with exception stack; the application is not deliberately crashed again after the fix.

## Failure Found and Fixed
InvalidDataException derives from SystemException rather than IOException. Desktop's initial expected-error filters missed it, so corrupt project JSON reached the fatal handler. Added tested shared LocalFileErrors.IsExpected policy and applied it to project commands/save/recent writes and settings/recent startup loading. InvalidDataException now uses the expected-error path; unrelated programmer exceptions remain fatal. Actual corrupted-project retry passed after the complete 78-test build gate. No failure is represented as a successful initial run.

## Unsupported Cases
Future schema versions/unknown fields, concurrent-editor merge, remote/cloud persistence, all later-phase features. Unsupported project versions fail explicitly.

## Known Limitations
Framework-dependent output needs .NET 8 Desktop runtime; no installer. One active project, no autosave; multiple instances use last-writer-wins local application state. Filesystem/power-loss behavior cannot be universally guaranteed. Temporary cleanup and log pruning are best effort when the OS denies access, with logging failure notifications. Native file dialogs required owner assistance because the UI tool misrouted modal inputs; no product workaround was added. Actual moved/Save As/error branch matrix remains automated rather than exhaustive manual coverage. Independent review is still outstanding.

## Architecture Decisions
ADR-011 selects inspectable UTF-8 JSON over ZIP/database, establishes identity/path authority/schema policy and safe-save sequencing. ADR-012 selects small WPF ViewModels/explicit services over external MVVM/DI or cloud/database state, separates local settings/recent, and defines errors/logging/retention. Both record alternatives and consequences; approved PHASE 00 boundaries are preserved.

## Exit Criteria Verification
| PHASE 01 criterion | Result | Evidence |
|---|---|---|
| DtoB.exe launch | PASS | Actual Release window |
| Create/save/open project | PASS | Owner-assisted native save; actual recent reopen; JSON/UI identity comparison |
| Restore after restart | PASS | Process close/relaunch and metadata comparison |
| Save Settings | PASS | Settings UI save/restart/reopen; settings JSON |
| Error logging | PASS | Actual initial FATAL, fixed retry ERROR and preserved active project; logger regression tests |

All five PHASE 01 Exit Criteria are satisfied with the distinctions between owner-assisted/manual and automated coverage above. Git diff and newly added files reviewed; git diff --check passes (line-ending normalization notice only). No CAD/Revit architecture expansion was introduced.

## Readiness for independent Antigravity review
Desktop implementation is ready for independent review. Actual Revit connection regression also passed, as recorded below. Stop at PHASE 01; do not proceed to PHASE 02.

## Revit Connection Regression
PASS: preserved dedicated Connection Verification UI displayed Connected / PONG from Revit 2025 Build 25.4.50.35, PID 22704. Native response Status=0, Error=0, matching process identity and request f9ab0b9a-b3fb-4e91-beed-0a86d8f34f0a. Deployment manifest source/deployed hashes match. No document or BIM generation was used. Both verification windows closed normally; Revit log contains STOPPED.

First launch exceeded the verifier's 60-second connector-start timeout. Revit then reached Home and STARTED. Retried with AttachOnly against that running host and succeeded; the timeout was not a PING success. Evidence: docs/status/evidence/phase00-review/phase01-20261008 (source/deployment and initial attempt) and phase01-20261008-attach (desktop-ping.json, environment.json, revit-host.log, source-snapshot.json). Raw build/test rerun is artifacts/phase00-review/phase01-20261008. These are new PHASE 01 regression evidence, not modifications to historical PHASE 00 results.
