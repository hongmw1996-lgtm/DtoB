# PHASE 00 implementation checklist — restarted analysis

Date: 2026-10-07 (Asia/Seoul). Status: implemented and verified; PASS_WITH_KNOWN_LIMITATIONS.

## Authority and inspected baseline

DtoB_FINAL_MASTER_PLAN.md is authoritative. Re-read it completely, together with PHASE_INDEX.md, AGENTS.md, ARCHITECTURE.md, CODEX_INSTRUCTIONS.md and tasks/PHASE_00.md. Older MASTER_PLAN.md does not govern conflicting decisions. Preserve the current task sequence; do not rewrite future phases during this work.

Branch: phase/00-architecture. HEAD: 720c083. Remote previously verified as https://github.com/hongmw1996-lgtm/DtoB.git. There are 43 staged specification/template/placeholder files and two existing untracked status documents. No solution, source projects, automated tests or independent review exists. Preserve staged content and do not commit without instruction.

Installed Revit at D:\프로그램\Revit 2025: Revit.exe, RevitAPI.dll and RevitAPIUI.dll all have FileVersion 25.4.50.35 and ProductVersion 20260410_1515(x64). RevitAPI.runtimeconfig.json, RevitAPIUI.runtimeconfig.json and AddInJournalEngine.runtimeconfig.json explicitly declare net8.0 and .NET/WindowsDesktop 8.0.0. Revit.runtimeconfig.json does not exist; do not cite it as evidence. Installed SDKs are 8.0.416, 9.0.308 and 10.0.101. Previously inspected Visual Studio 2022 is 17.14.23; Windows build is 26100/24H2.

ARCHITECTURE.md still contains a Web UI architecture and speculative Net10 adapter; update these during implementation to match the final master plan and actual installed runtime. Existing test documentation's ten-DWG target is later work; PHASE 00 needs synthetic contract fixtures and connection tests only.

## Accepted technical choices and ADR inventory

All decisions below are accepted with the user clarifications of 2026-10-07. Each ADR will include context, alternatives, decision, consequences, migration impact and status before its implementation.

| ADR | Alternatives | Accepted V1 choice |
|---|---|---|
| 001 Desktop/UI | WPF; WinUI; web desktop host; WPF + WebView2 | C# .NET 8 WPF, minimal connection window with the IPC client in a separate library. Reserve WebView2 evaluation for viewer work; no browser dependency in the PING window. |
| 002 Analysis | In-process C#; Python process; local Python HTTP service | C# library invoked by Desktop, deterministic geometry. Data contracts permit a later process boundary; no service host or ML implementation now. |
| 003 Revit isolation | One installed-version adapter; speculative multi-version targets | net8.0-windows x64 DtoB.Revit2025 with local API references and CopyLocal=false. Revit-free shared helper library; IExternalApplication starts/stops IPC. Future document commands require ExternalEvent. |
| 004 IPC | Named pipes; localhost HTTP; gRPC | Current-user Windows named pipes, versioned JSON request/response, PING only. One request per connection, bounded message size and timeout, request correlation, explicit errors and cancellation. Endpoint includes Revit PID; minimal verification UI takes session/PID explicitly. |
| 005 DWG boundary | ODA Drawings SDK; Autodesk RealDWG; AutoCAD-assisted extraction | IDrawingReader returning CAD IR plus diagnostics. SDK product decision is explicitly deferred; synthetic fixtures and the interface only. No SDK dependency or parser implementation in PHASE 00. SDK product selection stays pending availability/licensing, with this limitation explicit. |
| 006 Units/coordinates | mm vs meters vs Revit feet; drawing origin vs implicit recentering | Core double-precision mm, right-handed XYZ, Z up, XY plan, radians and positive counterclockwise rotation about +Z. Explicit source-to-model transform and origin metadata; never silently recenter. Central validated length/angular tolerance profile supplied explicitly. No recognition tolerance guessed now. |
| 007 Identity | Random persistent IDs; geometry hashes; source-derived IDs | DrawingId is persistent UUID; RevisionId separate. CAD identity derives from DrawingId + source handle; block occurrence identity includes insertion path. BIM ID is allocated once and persisted with source references; do not promise stability after re-recognition or across revisions yet. |
| 008 CAD IR | Untyped bags; typed contracts | Versioned System.Text.Json contracts for document/layer/entity/block/text/dimension and basic geometry. Preserve source unit/handle, raw and effective metadata, owner/insert path, transform and raw properties. Unknown entity is explicit with diagnostics, never dropped. |
| 009 BIM IR | Revit types; generic property bags; typed domain objects | Revit-independent document and Level/Grid/Wall/Column/Door/Window/Floor/Room contracts. Common ID, status, mandatory provenance and parameters; optional prediction metadata requires confidence [0,1] and nonempty evidence only when prediction exists; typed geometry and explicit level/host relationships. |
| 010 Build/test/layout | Multiple solutions/services; one solution | One DtoB.sln, SDK 8.0.416 pin, deterministic nullable builds, xUnit tests and JSON fixtures. Revit install path is configurable; missing DLLs fail clearly. Core builds without Revit. Windows CI tests Core/Desktop; installed Revit host checks are a separate gate. |

AGENTS.md explicitly requires ADRs before unit, coordinate, stable ID and BIM schema decisions. No database, AI provider or Revit generation semantics changes belong here.

## Ordered implementation checklist

- [x] Inspect repository and re-read authoritative prerequisites.
- [x] Reconfirm installed Revit build and runtime evidence.
- [x] Present checklist, alternatives, proposed choices and unresolved SDK procurement.
- [x] 00A: Write Desktop/UI ADR; create only the minimal connection verification window. No New/Open/Save/Recent/Settings product features.
- [x] 00B: Write analysis ADR and define the C# library boundary. No recognition rules or Python host.
- [x] 00C: Write Revit ADR; create adapter and lifecycle/manifest deployment instructions. API DLLs stay local; Core/Desktop have no Autodesk references.
- [x] 00D: Write IPC ADR; implement PING/PONG with protocol/version validation, timeout, unsupported-command errors and clean shutdown. No Document access from IPC worker.
- [x] 00E: Write unit/coordinate and identity ADRs before implementing primitives, adapter conversion and explicit transforms.
- [x] 00F: Write CAD schema/reader ADRs; implement minimum contracts, validation, reader interface and synthetic fixtures. Do not read actual DWG yet.
- [x] 00G: Write BIM schema ADR; implement all eight category contracts and source/level/host relationships. No semantic prediction or Revit element creation.
- [x] 00H: Create build/test ADR and one solution under the structure below; add build guidance and relevant Windows CI.
- [x] Update ARCHITECTURE.md and entry documentation to name the authoritative plan and actual Desktop pipeline. Preserve future phase files.
- [x] Run Core-only build without Revit, full solution build and all tests; fix findings.
- [x] Launch Desktop and actual Revit Add-in; prove PING/PONG and record host/build evidence. Test server success alone is insufficient.
- [x] Inspect own Git diff and verify combined master-plan/task exit criteria.
- [x] Write final PHASE_00_REPORT.md with every requested section and supported verdict; stop for independent Antigravity review.

## Minimum contract details

CAD: CadDocument has schemaVersion, drawing/revision IDs, source file/unit/coordinate metadata, layer/block/entity collections and diagnostics. CadEntity has ID/handle/type/layer/color/linetype/lineweight, geometry, ownership, transform, raw properties and support status. Text carries insertion/content/rotation/height; dimension carries defining points, measurement and display text. INSERT refers to a block definition and occurrence transform. Unsupported data retains type/handle/raw properties and a diagnostic reason.

BIM: Level has name/elevation; Grid has label/axis; Wall has baseline/thickness/base level/height; Column has position/rotation/size/base level/height; Door and Window have insertion/size/host/level, with window sill; Floor has boundary/level/thickness; Room has boundary/level/name. Optional unresolved fields stay explicit rather than guessed. Every prediction carries confidence and evidence even in Controlled CAD. Serialization fixtures are synthetic, not proof of DWG recognition.

## Implemented repository structure and commands

- apps/DtoB.Desktop — WPF verification UI
- packages/DtoB.Core — shared identities, diagnostics, versioned common contracts
- packages/DtoB.Geometry — deterministic primitives, transforms and tolerance policy
- packages/DtoB.Cad — CAD IR and IDrawingReader
- packages/DtoB.Bim — BIM IR
- packages/DtoB.Ipc — Revit-independent protocol and named-pipe client/server
- services/DtoB.Analysis — C# library, no independently running service
- revit/DtoB.Revit.Core — Revit-free output boundary helpers, unit conversion
- revit/DtoB.Revit2025 — sole Autodesk API consumer
- tests/ — unit, serialization, protocol and dependency-boundary tests
- datasets/controlled/phase00/ — JSON fixtures only
- docs/adr/, docs/status/, tasks/, reviews/ — decisions, evidence and review

Planned commands: dotnet build DtoB.sln -c Release -p:RevitInstallDir="D:\프로그램\Revit 2025"; dotnet test DtoB.sln -c Release --no-build; dotnet build packages/DtoB.Core/DtoB.Core.csproj -c Release. These commands were run successfully; see PHASE_00_REPORT.md and its evidence.

## Verification and phase gate

Automated: CAD/BIM JSON round trips for all minimum types, source identity preservation, unsupported entity diagnostics, invalid confidence/nonfinite geometry/version rejection, deterministic unit conversion, translation/rotation/mirror/transform composition, PING/PONG correlation/version/timeout/cancellation/reconnect, and absence of Revit dependencies outside its adapter. Every new capability gets fixture or regression coverage.

Manual host gate: Desktop launches, installed Revit loads adapter, Desktop receives matching PONG from that Revit process, stopped Add-in yields an explicit disconnected/timeout state, and lifecycle cleanup is verified. No RVT modification required.

Exit criteria combine both authoritative master plan and tasks/PHASE_00: Desktop/UI, analysis, Revit structure, IPC success, reader direction, unit/coordinate rules, minimum CAD/BIM schemas, repository/test strategy and ADRs. All PHASE 00 criteria were implemented/verified; SDK product selection remains deferred by explicit user decision. Do not begin PHASE 01.
