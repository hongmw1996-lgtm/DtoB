# DtoB Architecture — PHASE 00

DtoB_FINAL_MASTER_PLAN.md is the authoritative V1 plan. See docs/adr/ADR-001 through ADR-010 for accepted decisions and docs/PHASE_00_BUILD.md for build/deployment instructions.

```text
DWG -> IDrawingReader -> CAD IR -> C# Analysis -> BIM IR
                                                   |
                               Desktop Review / Mapping (later)
                                                   |
                                      Revit Adapter -> Native BIM -> RVT (later)
```

PHASE 00 implements contracts and technical connection verification only. DWG SDK selection is deferred. No actual reader, recognition algorithm, CAD/3D viewer, project management or BIM generation is present.

## Product and project boundaries

- apps/DtoB.Desktop: .NET 8 WPF verification window, explicitly selected Revit PID and PING status.
- packages/DtoB.Core: source/provenance identity, diagnostics, prediction metadata and JSON policy.
- packages/DtoB.Geometry: double-precision mm primitives, explicit affine transforms and caller-supplied tolerance.
- packages/DtoB.Cad: versioned CAD IR and IDrawingReader. Raw/effective CAD metadata and unsupported entity diagnostics retained.
- packages/DtoB.Bim: versioned BIM IR for Level/Grid/Wall/Column/Door/Window/Floor/Room; domain source of truth.
- packages/DtoB.Ipc: current-user named-pipe protocol/client/server, no Autodesk dependency.
- services/DtoB.Analysis: in-process C# library boundary; explicit not-implemented diagnostic.
- revit/DtoB.Revit.Core: Revit-free boundary helpers (mm/feet conversion).
- revit/DtoB.Revit2025: sole Autodesk API consumer. net8.0-windows x64; installed API references are never copied locally.
- tests and datasets/controlled/phase00: xUnit regression tests and synthetic JSON contracts.

The CAD parser never references Revit. No separate Python/web service or speculative runtime adapter is created. WebView2 evaluation belongs to future viewer requirements.

## Revit and IPC

Verified local Revit: 25.4.50.35, product build 20260410_1515(x64), .NET 8 runtime config, installed at D:\프로그램\Revit 2025. Re-check the installed runtime before retargeting.

IExternalApplication captures host metadata in valid Revit startup context and starts a background named-pipe server dtob-revit-PID. Only PING is accepted; all other commands return explicit errors. Frames are length-prefixed UTF-8 JSON (maximum 16 KiB), versioned and correlated. Reads/connects have deadlines; shutdown cancels outstanding work without depending on a UI synchronization context. Diagnostics are logged under %LOCALAPPDATA%\DtoB\logs.

No IPC worker accesses a Revit Document. Future document commands require ExternalEvent or another valid API context, with feature-group Transactions. These later features are not implemented here.

## Units, coordinates and identity

Core mm, right-handed XYZ, XY plan, Z up, radians and positive counterclockwise Z rotation. Transform placement is scale -> rotation -> translation; Then(next) applies next after the current transform. No implicit recentering, UCS inference or alignment. Unit conversion is explicit at adapter boundaries; exactly 304.8 mm per Revit internal foot. Tolerance values are validated and caller supplied, not hidden constants.

Drawing UUID and revision UUID are separate. CAD IDs encode drawing, original handle and insertion path. BIM IDs are persisted UUIDs with mandatory provenance (source references or explicit manual origin). Every semantic prediction result carries finite confidence [0,1] and nonempty evidence via Prediction/SemanticPrediction. Manually confirmed or non-predicted objects need no artificial confidence; confirming a prediction does not strip its metadata.

## Schema and unsupported data

Initial schema major is 1; major changes need ADR. Contracts are Revit independent, with dedicated raw JSON property dictionaries. Unknown CAD types are represented by CadUnsupported plus source-linked diagnostics. Validate documents after JSON deserialization and before use. Geometry topology/recognition/generation validation belongs to later phases; these are minimum data contracts.

## Build and test

One DtoB.sln and .NET SDK 8.0.416 pin. Core-only build and synthetic tests need no proprietary DLLs. Full adapter build requires RevitInstallDir. Actual Desktop-to-loaded-Revit PING/PONG is a separate host verification gate; an isolated named-pipe test server does not satisfy it.
