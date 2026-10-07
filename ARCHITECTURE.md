# DtoB Architecture — PHASE 00

DtoB_FINAL_MASTER_PLAN.md and PHASE_INDEX.md are authoritative V1 planning documents. PHASE 00 defines the Controlled CAD foundation and technical connection PoC. Independent review remains CHANGES_REQUIRED until Antigravity re-reviews the resolution; no later phase starts.

```text
DWG -> IDrawingReader -> CAD IR -> C# Analysis -> BIM IR -> Revit Adapter -> Native BIM
```

Only contracts, synthetic fixtures and PING are implemented. The DWG SDK product is deferred by explicit owner decision. No parser is coupled to Revit; no recognition, project-management workflow, final CAD/3D viewer or native BIM generation exists.

## Boundaries

One DtoB.sln contains Core, Geometry, Cad, Bim, Ipc, Analysis, Desktop, Revit.Core, Revit2025 and Tests. Desktop is a minimal .NET 8 WPF connection window and references Ipc only. Analysis is an in-process C# library boundary that reports ANALYSIS_NOT_IMPLEMENTED. Core/CAD/BIM remain Revit independent. Only the net8.0-windows x64 Revit2025 adapter references installed Autodesk DLLs, with Private=false. Revit.Core contains tested, Revit-free mm/feet helpers; it is not an unused adapter reference.

The actual verified host is Revit 2025.4, file version 25.4.50.35, product build 20260410_1515(x64). Its installed runtime configurations use .NET 8. RevitInstallDir is configurable. Retargeting requires fresh installed-host evidence.

## Coordinate and source contracts

Core geometry is double-precision millimeters, right-handed XYZ, Z up, XY plan and radians. Preserve original SourceUnit and explicit SourceToMillimetersScale. Conversion is an explicit normalization operation; serialized geometry is already mm. SourceToModel maps normalized mm WCS into model mm, without repeated unit scaling or implicit recentering.

Entity Points are definition-frame geometry. Arc/Circle Points[0] is the sole center; Arc angles run CCW from +X about +Z. Entity Transform is the accumulated occurrence placement. INSERT Placement is local; InsertionPath is outermost-to-innermost INSERT handles. Definitions have empty paths, exact identity transforms and matching OwnerBlockId. ValidateGeometry(tolerance) resolves occurrences and checks composition with explicit mm and dimensionless matrix budgets. Non-+Z extrusion is explicit unsupported data with raw normal and source-linked diagnostics. Nested/mirror fixtures challenge contracts; this is not a block parser.

CAD IDs canonicalize hexadecimal handle/path components while raw handles remain preserved. SourceReference validates drawing/handle/CAD-ID agreement. BIM UUIDs persist with mandatory provenance. Cross-document source membership and revision reconciliation remain unimplemented.

## Schema and validation

IR v1 is strict: probe major version first, forbid unknown fields and require $kind/$category first for the .NET 8 serializer. Major schema changes need ADRs. Missing arrays and invalid producer input fail explicitly. ValidateStructure() checks transport structure; ValidateGeometry(tolerance) checks closure/near-zero geometry and coordinate composition. No hidden geometry tolerance is selected. Arrays are mutable transport snapshots; records containing arrays are not value-comparable. Revalidate after mutation.

SourceDrawing provenance requires Prediction confidence/evidence unless an explicit ManualConfirmation records actor, action and timestamp. Manual domain objects may omit confidence. IsSemanticPrediction is removed; a plain ReviewStatus change cannot discard evidence. ReviewStatus is stored state only; no review workflow is implemented. The open Parameters bag is removed.

## IPC and host lifecycle

IExternalApplication captures host metadata in valid startup context. Current-user named pipes implement protocol 1, typed PING/PONG/error fields, correlation, bounded frames and OS server-PID checks. Four bounded workers recover accept/request failures and retry unexpected exits; exhausted recovery is Faulted, and shutdown cancels and awaits workers without capturing the Revit UI context. A short first-request budget limits idle-client starvation. Continuous hostile same-user saturation remains outside the trusted-user PoC scope; cryptographic authentication and negotiation are not claimed.

No worker accesses Document. Future model commands require ExternalEvent or another valid API context and feature-group transactions. Logs include timestamp/PID; retention failures cannot prevent startup.

## Build and phase gate

The normal SDK is pinned to 8.0.416. Installed Visual Studio MSBuild/Roslyn and VSTest provide a verified alternative on the current machine where dotnet.exe is blocked; .NET targets remain unchanged. Core and complete Release builds, explicit warning-identity gate, all xUnit tests, deployment hashes and actual Revit shutdown/restart PING evidence are separate checks. Portable CI is configured but no green remote run is claimed without execution.

See docs/PHASE_00_BUILD.md, docs/adr/ADR-001 through ADR-010, and docs/status/PHASE_00_REPORT.md. The owner approved the planning-document cleanup and entry-document rewrites; no commit or push is authorized in this review-fix cycle.
