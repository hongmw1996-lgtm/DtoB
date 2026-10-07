# PHASE 01 Desktop Shell

Run apps/DtoB.Desktop/bin/Release/net8.0-windows/DtoB.exe after a Release build. Runtime .NET 8 Desktop is required; no installer/self-contained package is provided in PHASE 01.

New creates an unsaved project. Edit its name, Save to a .dtob file, close and relaunch, then Open or double-click a recent entry. Save As changes the current location while preserving UUID/creation time and the old file. File movement does not make an opened project dirty. Actual CurrentFilePath is authoritative; LastKnownPath in JSON is informational.

Local state is under %LOCALAPPDATA%/DtoB: settings.json, recent-projects.json, desktop-logs. Settings are Working Directory, Cache Directory, Revit Target (2025) and Log Level. Cache is a directory preference only. Missing recent files stay listed. Corrupt settings/recent state warns and uses defaults without rewriting the damaged file on startup.

Expected project IO/format errors keep the active project and show a dialog. New/Open/Close/application exit with unsaved changes asks Save/Discard/Cancel. Save dialog cancellation/failure prevents leaving. Recent-state failure is reported separately after the project has already been successfully saved/opened.

Connection Verification remains a separate technical window. The existing --verify-pid <positive PID> --evidence <file> mode still launches the dedicated connection UI. Verify-Phase00Host.ps1 now addresses DtoB.exe. No IPC/Revit implementation changes.

Build and all tests using the installed toolchain:

```powershell
./scripts/Verify-Phase00Foundation.ps1 -RevitInstallDir 'D:\프로그램\Revit 2025' -Toolchain VisualStudio -EvidenceDirectory "$PWD/artifacts/phase01/final"
```

This runs Core-only and full Release builds and all xUnit tests, checking the three previously accepted Revit MSB3277 assembly identities. Normal dotnet build/test remains configured for machines with the SDK executable; this machine currently uses existing VS MSBuild/Roslyn/SDK files/VSTest.

Limitations: one active project; no autosave, concurrent-editor merge, cloud, database, localization, DWG management, CAD parsing/viewer, recognition or BIM generation. Temp/replace saving protects the old file against tested write/replace errors, not every filesystem/power-loss condition. Multiple application instances use last-writer-wins local state. Logs may include private exception paths; public evidence must be redacted.
