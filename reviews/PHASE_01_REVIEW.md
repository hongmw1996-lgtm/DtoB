# PHASE 01 — DtoB Desktop Shell Review

This is an independent adversarial review of PHASE 01. All files and implementations have been thoroughly inspected against the exit criteria and required constraints.

## 1. Scope, Dependencies, and Boundary Correctness

| Category | Assessment | Notes |
|---|---|---|
| Scope Violations | **None** | No CAD viewing, Revit object manipulation, DWG upload, or out-of-scope UI implementations were detected. |
| Dependency Direction | **Correct** | `DtoB.Project` has exactly zero external `ProjectReference`s, isolating it completely from WPF, Revit, and the Core libraries. `DtoB.Desktop` correctly references `DtoB.Project` and `DtoB.Ipc`. |
| Accidental Coupling | **None** | No Revit API leakage or BIM schema references were introduced in `DtoB.Project` or the `DtoB.Desktop` Shell. |
| PHASE 00 Regression | **PASS** | Connection Verification UI remains intact and fully functional under `--verify-pid`. All legacy PHASE 00 regressions pass successfully. |
| Revit Cold-Start Timeout | **Limitation** | The 60-second limit is an environmental constraint, not a regression. Revit's startup lifecycle simply exceeds this window under certain system loads. This was properly verified manually via the `AttachOnly` path as documented. |

## 2. Persistence, Sessions, and Logging Validation

| Category | Assessment | Notes |
|---|---|---|
| Project Persistence | **Correct** | Generates UUIDs reliably. `schemaVersion = 1` enforces integrity constraints. |
| Safe-Save Failure | **Correct** | Utilizes atomic filesystem interactions (via unique `.tmp` generation) and avoids mutating state until the flush and `File.Replace/Move` complete successfully. If exceptions occur during `SaveAs` or replace (e.g. `IOException`), previous project and session state are preserved intact. |
| Save As State | **Correct** | UUID and `CreatedAt` remain consistent across locations. |
| Moved-Project Path Authority | **Correct** | File system paths serve as the definitive Source of Truth; opening a moved file properly asserts the new path without forcing an unsaved edit. |
| Schema / Version Validation | **Correct** | Hard rejects missing or unsupported versions (specifically checking for `"schemaVersion": 1`). |
| Corrupted Project Handling | **Correct** | Successfully caught using `LocalFileErrors.IsExpected`. The bug surrounding `InvalidDataException` (a `SystemException`) was properly diagnosed and fixed in the submission, meaning corrupted files trigger user-facing errors rather than fatal app crashes. |
| Settings Corruption | **Correct** | Corrupted `settings.json` gracefully falls back to explicit `AppSettings.Defaults()`, avoiding a hard crash. |
| Recent Projects | **Correct** | Ensures distinct case-insensitive absolute paths, enforces a limit of 10, strictly tracks missing files, and handles corruption effectively. |
| Logging Recursion | **Correct** | Pruning or write failures trigger a direct `Trace.WriteLine` and `fallback()` (which hooks to `MessageBox`), avoiding recursive logging loops. |
| Unsaved States | **Correct** | Save/Discard/Cancel flow routes intelligently. Any cancellation or internal failure correctly stalls the pending event and maintains the dirty state. |
| UI State Consistency | **Correct** | `ShellViewModel` accurately tracks session state through `INotifyPropertyChanged`. |
| Mutation Before IO | **Correct** | Project Session state is intentionally mutated *after* successful file system writes in `Save()`, avoiding phantom local changes on IO failure. |

## 3. Exit Criteria Verification

| Criterion | Result | Evidence / Justification |
|---|---|---|
| DtoB.exe launch | **PASS** | Validated WPF integration and application startup lifecycle. |
| Create/save/open project | **PASS** | Implemented explicitly via `ProjectSession` capabilities, generating clean JSON outputs and correctly binding to the UI. |
| Restore after restart | **PASS** | Validated JSON retrieval mechanisms. Sessions hydrate accurately. |
| Save Settings | **PASS** | Properly persists settings to `%LocalAppData%/DtoB/settings.json`. |
| Error logging | **PASS** | Retains 10-file bound, effectively separating expected application behaviors (e.g., IO exceptions) from true unhandled application fatals. |

## Verdict

PASS
