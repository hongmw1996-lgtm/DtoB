# ADR-012-Desktop-State: Desktop state and services

## Status
Accepted — PHASE 01 owner approval, 2026-10-07.

## Context
The shell needs project commands, settings, recent files and errors while preserving PHASE 00 verification.

## Alternatives

| Option | Evaluation and disposition |
|---|---|
| Small WPF ViewModel and explicit services | Selected: testable session transitions without external MVVM/DI dependencies. |
| External MVVM/DI framework | Rejected for this phase: the small command surface does not justify another framework. |
| Database/cloud application state | Rejected for this phase: local settings and ten recent paths need only separate JSON files. |

## Decision
Keep WPF/.NET 8. DtoB.exe opens the project shell; connection verification remains a separate window and existing verification arguments work. DtoB.Project contains session, file persistence, settings, recent and logging services without UI/API dependencies. Desktop references Project and Ipc only.

Settings and recent projects are separate version-1 JSON documents under LocalApplicationData/DtoB. Settings: WorkingDirectory, CacheDirectory, RevitTarget (2025), LogLevel. No localization. Recent: successful opens/saves only, newest first, case-insensitive normalized absolute Windows paths, ten entries; missing entries remain visible. Corrupt local state warns and uses defaults without automatic overwrite; explicit user actions can save new state.

Unsaved New/Open/Close/exit asks Save/Discard/Cancel. Save cancellation/failure keeps the session and blocks transition. Expected errors preserve the current session and show an error dialog. Unexpected fatal errors log then terminate with direct notification. File logs include UTC/PID/severity and retain at most ten sessions. Logging failures use Trace and direct notification, never recursive logging. Pruning failures warn without disabling the application.

## Consequences
Single active project; last successful local-state writer wins across multiple instances. No autosave, cache-management engine, authentication or future-phase UI.

## Migration Impact
Preserve ADR-001 through ADR-010 and all IR/analysis/Revit boundaries. Update executable-path consumers and graph tests for the new Project dependency. PHASE 00 evidence remains historical.
