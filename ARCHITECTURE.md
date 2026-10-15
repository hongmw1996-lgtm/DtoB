> **Current status — 2026-10-15 whole-PHASE03 final gate.** Native Text DXF
> extraction 03A–03F, deterministic QA 03G and the diagnostic-only Viewer 03H
> are implemented; each has its final independent `PHASE_03A_PASS` through
> `PHASE_03H_PASS`. See [final release gate](docs/status/PHASE_03_FINAL_GATE.md) for the current
> integrated build, tests, limitations and owner-acceptance status.
> ADR-015's additive raw boundary, Accepted ADR-016 and Accepted ADR-017
> together govern the implementation: Raw is runtime-only; Dxf references Raw;
> Desktop owns explicit managed-file hash/read lifecycle and WPF presentation.
> Managed AC1032 strict UTF-8 Text DXF is authoritative; DWG decoding and CAD SDKs
> are not production capabilities. Snapshot coverage remains Partial, including
> semantic Unsupported 52 (VPORT 1, APPID 3, OBJECTS 48); LOW-01 remains accepted.
> PHASE04, commit, push, PR and tag are **not authorized** by this gate.
>
> **Historical checkpoint notice:** earlier dated "current task", "STOP before03A",
> "proposed projects", "not implemented" and "ADR017 reserved" statements below
> record the earlier design/housekeeping authorization, superseded by the owner's
> subsequent 03A–03H instructions. They are not current implementation status.
> Earlier Proposed ADR-016 references are superseded by its Accepted decision;
> ADR-017 is now Accepted. Technical phase boundaries and future-phase requirements
> below remain in force. Original checkpoint text is retained without rewriting
> historical reports/reviews or changing accepted architectural decisions.

# Current architecture — PHASE 03 Native DXF Data Extraction Foundation

Owner direction, 2026-10-14: **DtoB V1 uses Native Text DXF extraction.** DWG users externally Save As/Export Text DXF; no direct DWG decoder, SDK selection/automatic conversion in V1. Initial AC1032 + strict UTF-8 Text DXF profile; explicit tested profiles required for more versions/encoding cases. Pure ASCII fixture does not prove all multilingual UTF8 support.

```text
Managed Text DXF bytes -> DtoB lexical pairs (raw spelling/source lines)
 -> section/object parser -> typed raw facts + source evidence
 -> IRawDrawingReader / RawDrawingReadResult / RawDrawingSnapshot
 -> factual QA -> minimal native-frame diagnostic Viewer (secondary)
PHASE04 -> source units/defaults/radian/frame/OCS-WCS normalization
 -> local/nested BLOCK-INSERT transform composition / effective styles
 -> cleanup / analysis structures -> IDrawingReader / CadDocument / CAD IR
PHASE05+ -> recognition -> BIM IR -> Revit Adapter -> Native BIM
```

Proposed pure net8 DtoB.Cad.Raw (owned contracts/evidence) and DtoB.Cad.Dxf (lexical/structural/typed parser, references Raw only). No WPF/Revit/ACadSharp/ODA/RealDWG/AutoCAD/private DLL. No production project creation/reference or code in this design task. Existing normalized Cad/Core/BIM/Analysis/Revit architecture unchanged; no .dtob v2 or CAD IRv1 change. ADR015 native amendment retains additive boundary; ADR016-Native-DXF-Reader is **Accepted**, ADR017 lifecycle reserved.

Owned raw evidence preserves ordered/repeated group codes, code/value physical lines, section/object/field ranges, file/drawing/revision/hash, handle spelling/numeric value and contextual owners/refs. No third-party object model/source defaults or synthetic replacement handles. Snapshot is derived in-memory/QA data, not project/domain truth. Unknown/ignored/invalid records are explicitly located/accounted; bounded resources and cancellation, no valid-empty parser failure. Detailed profiles/safety/encoding/QA: PHASE_03_CHECKLIST sections5–24.

PHASE03 keeps native coordinates/source-degree angles/INSERT parameters only: **no final/local/nested transforms, unit/OCS conversions, measured dimensions or ByLayer/ByBlock resolution**. PHASE04 owns these with independent tests, including nonzero block base points. MINSERT metadata/handle retained, unsupported expansion diagnostic, no cells/IDs/Explode. Diagnostic Viewer draws only supported native-frame data/local definition view/raw INSERT markers; no second parser or PHASE04 placement engine.

PHASE02 authority: actual CurrentFilePath + safe ManagedFilePath only. Explicit extraction verifies selected DXF hash/size on read-only immutable lease, parses on worker with cancellation/epoch/stale-success-error guards; no SourceFilePath fallback or persisted/dirty mutation. Normal Open/Refresh/metadata selection remains zero asset-content/hash/sourceIO. Existing DWG registrations are preserved but ineligible for native extraction; no migration in this design.

ACadSharp historical **ACADSHARP_CAPABILITY_FAIL / NOT SELECTED**; preserve isolated spike/report/evidence, no repair/fork/integration/continued backend review/SDK escalation. PHASE00/01/02 completed/reviewed/merged. Older pre-spike Antigravity review is historical, not approval for native implementation.

Current task: **final pre-implementation housekeeping only; STOP before03A**. Antigravity APPROVED_FOR_NATIVE_DXF_IMPLEMENTATION. No parser/raw production contracts/projects/Viewer/PHASE04/SDK install/commit/push. The foundation text below is unchanged historical PHASE00 evidence; its old current-phase/UI/gate statements do not override this direction.
## Final Native Text DXF pre-implementation housekeeping — 2026-10-14

Antigravity verdict: **APPROVED_FOR_NATIVE_DXF_IMPLEMENTATION**. Issues1–3 incorporated; **ADR-016 Accepted**, accepted ADR001–015 unchanged. Ready for subsequent PHASE03A; current turn STOP before parser/projects/Viewer/PHASE04 implementation, no commit/push.

Official profile: **Text DXF, AC1032, strict UTF-8**, ASCII subset naturally supported, optional UTF-8 BOM, **CRLF / LF**. Valid Korean/other non-ASCII UTF-8 is allowed, not forbidden by format terminology; actual acceptance tests remain required.

Authoritative source identity begins at **imported managed Text DXF**: **DrawingId + RevisionId + FileHash + Original DXF Handle**. OriginalHandle is the handle written in imported DXF, not an upstream DWG handle unless independently proven. DtoB guarantees no semantic/structural/identity equivalence and assumes no responsibility for third-party export changes to handles, anonymous blocks, dimensions, proxy/custom objects, unsupported records or database structure. DtoB remains responsible for extracting that managed DXF faithfully. No DWG/DXF reconciliation in PHASE03 or PHASE04.

Initial configurable/injectable development limits: physical source line **64 KiB (65,536 UTF-8 bytes)**, combined control nesting **16**, stored diagnostics **5,000** (reserve terminal slot), raw pairs **250,000**, input **16 MiB (16,777,216 bytes)**. Enforce before allocation; exceed -> **ResourceLimitExceeded**, never truncated-success. Smaller deterministic test profiles and later measured revisions permitted; no performance claim. Full semantics: ADR016 and checklist§20. Status: docs/status/PHASE_03_NATIVE_DXF_HOUSEKEEPING_REPORT.md.

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
