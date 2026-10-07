# PHASE 00 build and verification

Authoritative scope: DtoB_FINAL_MASTER_PLAN.md and tasks/PHASE_00.md. Only synthetic contracts and technical PING verification are implemented.

## Requirements

Windows, .NET SDK 8.0.416 (global.json), installed licensed Revit 2025 using .NET 8 for actual adapter/host verification. This machine has Revit 2025.4, executable/API FileVersion 25.4.50.35, product build 20260410_1515(x64), at D:\프로그램\Revit 2025. Installed RevitAPI/RevitAPIUI runtimeconfig files declare net8.0. Re-check after a Revit update; never select the adapter runtime from the newest installed SDK alone.

## Build/test

```powershell
dotnet build packages/DtoB.Core/DtoB.Core.csproj -c Release
dotnet build DtoB.sln -c Release '-p:RevitInstallDir=D:\프로그램\Revit 2025'
dotnet test DtoB.sln -c Release --no-build '-p:RevitInstallDir=D:\프로그램\Revit 2025'
```

Stop if build fails; --no-build must only follow a successful current build. Without Revit, build Desktop and run tests/DtoB.Tests/DtoB.Tests.csproj independently. The adapter reports a clear missing-DLL build error. Proprietary API DLLs are not copied or redistributed.

## Actual Revit host

1. Close Revit and run scripts/Install-Phase00Connector.ps1. It deploys DtoB binaries to %LOCALAPPDATA%\DtoB\phase00\connector and writes %APPDATA%\Autodesk\Revit\Addins\2025\DtoB.Phase00.addin. It refuses to replace another add-in identity and refuses deployment while Revit runs.
2. Start the installed Revit 2025 normally. No project needs to be opened. No model modification occurs.
3. Find its PID in Task Manager/Get-Process Revit. Launch apps/DtoB.Desktop/bin/Release/net8.0-windows/DtoB.Desktop.exe. Enter that PID and click PING Revit. Expect Connected/PONG with matching PID and build.
4. Technical evidence mode: DtoB.Desktop.exe --verify-pid PID --evidence ABSOLUTE_JSON_PATH. The same window/client operation runs automatically and records UTC, Desktop PID and correlated host response. Use a writable directory. This is a verification tool, not a project save feature.
5. Close Revit normally, check STOPPED in %LOCALAPPDATA%\DtoB\logs\revit-PID.log, and click PING again to see a disconnected timeout. Restart and verify with the new PID.

To remove only this connector, close Revit and delete its DtoB.Phase00.addin manifest; leave unrelated add-ins untouched. The deployed folder can remain until cleanup is requested.

## Contract and transport limits

Core units are mm; adapter helpers convert exactly 304.8 mm/foot. Coordinate conventions and transform composition are in ADR-006. Tolerance is explicitly caller supplied; no recognition tolerance is chosen. Validate documents after deserialization. JSON discriminators precede properties on .NET 8; invalid/unsupported input fails explicitly. Raw CAD fields have a dedicated JSON property dictionary.

The reader SDK is deferred. IDrawingReader is a contract only. FoundationAnalysisEngine validates CAD and returns an explicit ANALYSIS_NOT_IMPLEMENTED diagnostic and no BIM model. BIM objects always have provenance; non-predicted/manual objects need no confidence. Recognition APIs use SemanticPrediction<T>, and Suggested BIM objects require Prediction; confirmed predictions retain their evidence.

Named pipes are current-user Windows-local, bounded to 16 KiB, one request per connection with version/correlation checks and request deadline. Only PING is supported. Host version/build are captured during valid Revit startup; background IPC never reads Document. Future Revit operations require valid API context (ExternalEvent) and separate feature transactions.

CI checks portable foundation/Desktop build and synthetic tests on Windows. CI does not claim licensed Revit host validation.
