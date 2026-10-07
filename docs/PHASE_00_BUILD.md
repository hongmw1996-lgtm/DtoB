# PHASE 00 build and host verification

Only PHASE 00 is implemented. DtoB_FINAL_MASTER_PLAN.md and PHASE_INDEX.md govern V1. No DWG SDK, parsing, recognition, project management, final viewer or BIM generation is present.

## Normal toolchain

Windows, .NET SDK 8.0.416 and licensed Revit 2025 are required for the full gate. Set the actual installation directory; do not assume a machine-specific path.

```powershell
./scripts/Verify-Phase00Foundation.ps1 -RevitInstallDir '<installed Revit 2025 directory>'
```

Without Revit, omit RevitInstallDir to build Core/Desktop and run every portable test. The adapter alone references Autodesk DLLs with Private=false. The script rejects unexpected warning codes and reference-conflict identities. It allows exactly Microsoft.VisualBasic 10.0/10.1, System.Drawing 4.0/8.0 and WindowsBase 4.0/8.0 for the locally verified Revit 25.4.50.35.

## Existing Visual Studio toolchain

The current machine's dotnet.exe was reported blocked by security software and subsequently absent. No security setting, exclusion, renamed executable or quarantine restoration was performed. The already installed Visual Studio 2022 MSBuild 17.14.23 and Roslyn compiler can build these SDK-style projects using the intact SDK 8.0.416 files; VSTest 17.14 runs the tests on .NET 8.0.22.

```powershell
./scripts/Verify-Phase00Foundation.ps1 -RevitInstallDir '<installed directory>' -Toolchain VisualStudio
```

VisualStudioDirectory and SdkDirectory are configurable parameters. The script sets MSBuildSDKsPath only for the invocation, selects the installed Visual Studio compiler and disables the unused workload resolver. This project has no MAUI/mobile workloads. Targets remain net8.0/net8.0-windows and the global.json pin is unchanged. SDK resolver diagnostics about the absent CLI may still appear; actual compile/test exit codes are checked. Use the normal Dotnet toolchain in CI. A suspected false positive should be investigated through the security vendor's process; this fallback does not validate a quarantined executable.

## Actual host gate

Close the verification Desktop and Revit normally before rebuilding. Do not stop other work or modify open models.

```powershell
./scripts/Verify-Phase00Host.ps1 -RevitInstallDir '<installed directory>' -Toolchain VisualStudio -RunName '<unique run name>'
```

The script runs Core and full Release builds, the warning gate and all tests, verifies deployment hashes, starts the installed Revit and runs the actual WPF/client verification. Raw build/TRX output stays in ignored artifacts; public evidence uses relative source paths and installation-independent version metadata. Evidence is under docs/status/evidence/phase00-review/<run name>.

Normal shutdown remains an observed UI action: close Revit, retain the dated host log including STOPPED, and use the Desktop PING or --verify-pid/--evidence mode to verify HostNotRunningException. Close all verification Desktop windows. Repeat the host gate with another run name to verify restart. -AttachOnly requires exactly one live host and does not rebuild/deploy; it is not a substitute for the full final gate. Inspect authentication/security prompts manually; do not bypass them.

Install-Phase00Connector.ps1 refuses an open Revit or an unrelated manifest identity, copies only DtoB DLL/deps files and emits source/deployed SHA-256 pairs. Logs use revit-<UTC timestamp>-<PID>.log, retaining twenty files; pruning failures are warnings. No model is opened or modified by PING. IPC workers never access Document.

## Contracts

Use ValidateStructure() for structure and ValidateGeometry(tolerance) for geometry. CAD transform validation requires explicit length and dimensionless matrix budgets. BIM boundary/segment validation uses caller-supplied length tolerance. Definition geometry is in mm; accumulated INSERT Transform maps it to mm WCS, then SourceToModel maps mm WCS to model mm. Original SourceUnit and SourceToMillimetersScale remain explicit; NormalizeSourcePoint is an explicit conversion, not implicit scaling during deserialization.

IR major version 1 forbids unknown fields. The envelope checks schemaVersion before parsing; $kind/$category must be first for .NET 8. Transport records with arrays are mutable snapshots, not value-comparable objects. Revalidate after mutation. SourceDrawing provenance requires prediction confidence/evidence or a documented ManualConfirmation; a status change alone cannot waive this requirement. Manual domain objects need no artificial confidence.

Only PING/PONG is implemented. Protocol 1 exact-match, typed commands/status/errors, current-user named pipes, OS server-PID verification, 16 KiB framing, bounded workers/recovery and a 250 ms first-request budget support the technical PoC. Continuous malicious same-user saturation is not prevented. CI covers portable builds/tests, not a licensed Revit host.
