# PHASE 00 REPORT

## Status

**REVIEW_REQUIRED — ready for final Antigravity re-review.** Latest local Release builds, all 56 tests, PONG, shutdown, disconnect and restart gates are verified. Independent approval and a green remote CI run are not claimed. PHASE 01 has not started. Codex did not commit or push.

Date: 2026-10-07, Asia/Seoul. Authority: DtoB_FINAL_MASTER_PLAN.md, PHASE_INDEX.md, tasks/PHASE_00.md, AGENTS.md and the owner's approved technical choices and later review-fix request.

## Git state and approval history

Branch: phase/00-architecture. Current base HEAD: `2a5c85b74bd66cb783664609ca49f5bce4193f01`. The repository acquired fc3633d and 2a5c85b externally during this work; Codex did not create them. The final fixes are **uncommitted working-tree changes**, not the contents of HEAD alone. Final source-tree SHA-256: `53B0ECB5C349683373775286FCED74425664F76E7D3D5258FC50B88FCED6C930`. See evidence/phase00-review/final-source-snapshot.json for relative-file hashes. Build-time manifests exist in each final host run. Their compiled inputs match the final tree; no captured build input changed after these final host runs.

The owner explicitly approved removal of FILE_INDEX.md and MASTER_PLAN.md and rewrites of README.md, ARCHITECTURE.md, START_HERE.md and CODEX_INSTRUCTIONS.md. The removed files are already absent from the current committed baseline. The rewrites align entry guidance with the authoritative Desktop/Controlled CAD plan. No future-phase task specification was rewritten. Original review H-01's commit/push/remote-CI requirement is not performed because the owner's latest instruction explicitly prohibits commit/push.

## Implemented

- One .NET 8 solution; WPF technical PING window; in-process C# Analysis boundary; isolated Revit 2025 adapter. No Autodesk dependency outside that adapter.
- Explicit CAD definition/occurrence frames, local INSERT Placement, accumulated Transform, ownership/path validation and nested/mirror Arc fixtures. Non-+Z normals require explicit unsupported data/diagnostics.
- Original source units retained with explicit SourceToMillimetersScale. IR geometry is mm; normalization is explicit, without scaling during deserialize.
- Strict IR v1 envelope/version/unknown-field/discriminator policy and null-safe minimum-contract validation. Canonical hex source identities and provenance identity checks.
- ValidateStructure versus ValidateGeometry APIs, caller-supplied length/angular/dimensionless matrix budgets, tolerance-aware closure and degeneracy.
- SourceDrawing provenance requires prediction confidence/evidence or a documented manual confirmation. Mutable IsSemanticPrediction and the open Parameters bag were removed. Manual domain objects need no artificial confidence.
- Typed PING/PONG/error protocol, OS server-PID verification, bounded concurrent workers, recoverable accept/request errors, bounded supervision/retries, explicit Faulted state and safe shutdown. Four idle clients do not starve a legitimate PING in the regression test.
- Timestamp/PID logs with bounded retention; pruning failures do not prevent startup. Closed-window cancellation, explicit missing-process feedback and verification-argument validation.
- Reproducible build/test/host scripts, dependency-graph tests, exact three-identity warning gate, deployment hash manifests and relative source snapshots. Historical evidence is explicitly superseded.

## Not Implemented

Actual DWG parsing/SDK integration, object recognition, AI/ML, project management, final CAD/3D viewer, family catalog/mapping, Revit Document commands/ExternalEvent implementation, BIM generation/transactions, RVT output and later QA. No PHASE 01 work. No commit/push, new SDK installation, security exclusion, security disabling or quarantined-executable restoration.

## Changed Files

Current changes relative to the externally created 2a5c85b baseline:

- README.md, START_HERE.md, ARCHITECTURE.md, CODEX_INSTRUCTIONS.md
- packages/DtoB.Core/Contracts.cs, DtoB.Geometry/Geometry.cs, DtoB.Cad/CadContracts.cs, DtoB.Bim/BimContracts.cs, DtoB.Ipc/PipeProtocol.cs
- services/DtoB.Analysis/AnalysisBoundary.cs
- apps/DtoB.Desktop/App.xaml.cs and MainWindow.xaml.cs
- revit/DtoB.Revit2025/ConnectorApplication.cs
- tests/DtoB.Tests/ContractTests.cs, GeometryTests.cs, IpcTests.cs, FoundationDocumentTests.cs, ReviewRegressionTests.cs
- datasets/controlled/phase00/cad.json, bim.json, nested-mirrored.json and README.md
- scripts/Verify-Phase00Foundation.ps1, Verify-Phase00Host.ps1, new Get-Phase00Snapshot.ps1
- docs/PHASE_00_BUILD.md; ADR-004, ADR-006, ADR-008, ADR-009; this report/checklist and evidence notes/manifests

Earlier resolution changes to the adapter csproj, install script, CI workflow and all ten ADR comparisons are already in the external baseline; they remain part of the reviewable final implementation. git diff --check passed, and the substantive diff was inspected including new snapshot/evidence files.

## Tests Added

56 xUnit cases are executed, up from 27 in the initial independent-review checkpoint and 44 in the current committed checkpoint. Added/updated coverage includes fifty connect/drop cycles, unexpected worker restart, exhausted recovery/Faulted shutdown, four idle clients, actual OS PID mismatch using a controlled child process, a timed missing-process failure, strict envelope/missing-array/discriminator errors, provenance bypass/manual-action cases, five negative coordinate cases, non-mm conversion/serialization, open boundaries, source identity and nested/mirrored Arc fixtures. Dependency discovery now uses DtoB.sln, never arbitrary recursive csproj discovery. ADR tests require evaluated decision/rejection reasons.

Manual UI check: --verify-pid 0 displayed the explicit usage error and produced no evidence file; the process subsequently exited. The observation did not capture an OS exit code, so no numeric exit-code verification is claimed. See invalid-arguments.json. UI shell robustness is not represented as automated UI coverage.

## Test Results

Final Release gate: **56 passed / 0 failed / 0 skipped**. VSTest 17.14 ran xUnit on **.NET 8.0.22**. Final TRX/logs are retained in ignored artifacts/phase00-review/re-review-final-ping and re-review-final-restart; the public verification-summary contains the counts without user paths. Both gates passed the project graph and ADR checks.

Additional evidence checks verified both request IDs against logs, both STOPPED markers, source/deployed/current DLL hash agreement, zero changes to compiled inputs after host verification, full docs Markdown UTF-8 decoding and absence of U+FFFD. Literal question marks in valid prose are allowed; targeted corrupted ADR status text is rejected. No stale log is used as proof of current success.

## Build Result

Core-only Release build without Revit references: **PASS, zero warnings/errors**. Complete solution Release build against installed Revit: **PASS, zero errors and exactly three MSB3277 conflict identities**:

| Assembly | Conflict |
|---|---|
| Microsoft.VisualBasic | 10.0.0.0 / 10.1.0.0 |
| System.Drawing | 4.0.0.0 / 8.0.0.0 |
| WindowsBase | 4.0.0.0 / 8.0.0.0 |

UseWPF and the unused Revit.Core reference were removed from the adapter. These conflicts remain through installed Autodesk references. Verify-Phase00Foundation rejects changed warning codes/headings/identity pairs; they are not silently suppressed. Acceptance is limited to this PHASE 00 PING host proof.

The user reported dotnet.exe being blocked as ransomware; its normal installed executable later became unavailable. Initial CLI checkpoints passed earlier, but final CLI execution was not claimed. The final gates used the existing **Visual Studio MSBuild 17.14.23/Roslyn + SDK 8.0.416 files + VSTest**, retaining net8.0/net8.0-windows targets and global.json. Invocation-scoped SDK/compiler selection and the unused workload-resolver setting are documented; no security controls were changed. SDK resolver diagnostics about the missing CLI remain visible. Remote CI uses the normal Dotnet toolchain and has not been executed by Codex.

## Actual Revit Host Verification

Installed Revit 2025.4: FileVersion **25.4.50.35**, ProductVersion **20260410_1515(x64)**. Startup captured Revit version/build from the actual API context. The verification opened no model.

| Check | New evidence |
|---|---|
| Final Desktop -> Revit PONG | Revit PID 26648, Desktop PID 13192, request `fd042e24-35d0-4d2b-8832-9397b12b0901` |
| Normal first-host shutdown | STOPPED at 07:12:27.6900016 UTC; process exited normally |
| Disconnected behavior | Desktop JSON at 07:13:01.7885485 UTC: HostNotRunningException for PID 26648 |
| Revit restart -> PONG | New Revit PID 15456, Desktop PID 14504, request `edb33dad-c7bb-4248-97db-bd4a36df18c8` |
| Normal restarted-host shutdown | STOPPED at 07:13:55.9298802 UTC; verification windows closed |

Both requests occur identically in Desktop JSON and the matching dated Revit log. New evidence: evidence/phase00-review/re-review-final-ping and re-review-final-restart. Log naming is revit-<UTC timestamp>-<PID>.log. Desktop evidence schemaVersion is 1; response Status 0 means Pong, Error 0 means None. Wire protocol version is 1 with enum string fields. CAD/BIM IR remains strict major 1 with the amended unit/placement/provenance fields under the documented ADR migration policy.

SHA-256 of built/deployed DtoB.Ipc.dll: `11E19E8450E4ED23CBF152F9E91B402DA8163FDE1E9029FEEA93C98B861A7F39`.
SHA-256 of built/deployed DtoB.Revit2025.dll: `ECD83709619EF9BB7FE40396BF94AD3E8E20421B75E124AC517BD2835DA7E6E1`.
Per-file manifests include deps JSON. Relative source snapshots identify all build inputs. The base commit is explicitly distinguished from the tested modified tree. Public new evidence omits installation/user paths; historical path strings were redacted and stale checkpoints clearly marked superseded.

## Unsupported Cases

Unknown/unitless source units, arbitrary non-+Z OCS, DWG SDK fidelity, Xrefs/dynamic blocks/production CAD repair, recognition and model generation remain unsupported. Unsupported CAD retains raw data and diagnostics; no actual parser is present to classify real files. Nested/mirror fixtures verify the contracts only.

## Known Limitations

- The final changes are uncommitted by explicit owner instruction. There is no final commit or green remote CI proof; source hashes identify this local artifact. Original review H-01 remains a documented process exception requiring owner/reviewer disposition.
- The security block was not diagnosed as a confirmed false positive. The original CLI remains unavailable; the supported installed Visual Studio path was independently exercised.
- Same-user peers remain trusted; OS PID verification is not cryptographic authentication. Four bounded workers plus short first-frame deadlines limit accidental starvation, not continuous hostile saturation. No version negotiation exists until another protocol version is required.
- SourceReference checks identity consistency, not CAD-document membership or cross-revision reconciliation. Arrays are mutable snapshots, not deeply immutable/value-comparable records.
- Strict IR v1 rejects additive unknown fields and requires discriminator ordering. Schema amendments update synthetic-only data, with no released persisted DWG data to migrate.
- ManualConfirmation is auditable contract metadata, not a security authorization mechanism. Prediction history retention beyond this minimum is future review-workbench work.
- Geometry validation is minimum closure/degeneracy/placement validation, not self-intersection, topology cleanup or DWG recognition. Unitless drawings need explicit resolution.
- Host verification covers startup/PING/lifecycle only. No model API operation, unit conversion inside a Revit model, transaction or generation semantics was verified. UI behavior beyond recorded manual checks is not automated.
- Log-write failure falls back to Trace; a host with no retained log cannot satisfy the evidence gate. The required gate does not silently accept missing host logs.

## Architecture Decisions and ADR Files

ADR-001 Desktop/UI, 002 Analysis, 003 Revit isolation, 004 IPC, 005 deferred DWG reader, 006 units/coordinates, 007 identities, 008 CAD IR, 009 BIM IR/provenance, 010 build/test/repository. All ten contain evaluated alternatives. They exist in the current external baseline and were finalized/amended during review. Original ADR-before-code chronology is not proven by mtimes and is no longer claimed. This cycle amended 004/006/008/009 before the corresponding final contract changes; no new numbered ADR or SDK dependency was introduced.

ADR-005 records the owner's dated explicit deferral, licensing/runtime/block/Xref/handle evaluation criteria and the requirement to resolve the product before later actual reader/viewer implementation.

## Exit Criteria and Review Readiness

| PHASE 00 criterion | Evidence-based local result |
|---|---|
| Desktop framework/UI | WPF/.NET 8 ADR and actual connection UI, Release build |
| Analysis strategy | In-process library boundary, explicit unimplemented test |
| Revit architecture/runtime | Isolated API references, actual 2025.4 build, .NET 8 host |
| Communication PoC | New actual PONG, disconnect, normal shutdown and restart evidence |
| Reader direction | IDrawingReader/CAD IR boundary selected; SDK product deferred by owner |
| Units/coordinates | Explicit source normalization, frames/path/placement, positive/negative tests |
| CAD/BIM schemas | Strict versioned typed contracts, provenance and fixture tests |
| Repository/test/build | One solution, graph tests, full Release/56-test local gates |
| ADRs | Ten evaluated decisions, recorded amendments/history limits |

All locally verifiable task/master-plan technical exit criteria are supported by current evidence, with the explicitly approved reader-product deferral. **Overall phase completion is not asserted:** Antigravity approval and original H-01 process disposition remain outstanding. Ready for final independent Antigravity re-review. Next action is review, not PHASE 01.

## Antigravity Review Resolution

The on-disk review still contains the original IDs (H-01 repository state, H-02 ADRs, H-03 CAD frames). The owner's later pasted request reuses IDs for different findings (H-01 listener, H-02 stale evidence). Both namespaces are recorded below to avoid dropping or silently renumbering an issue.

### On-disk independent review

| Review ID | Severity | Issue | Resolution | Changed Files | Tests Added/Updated | Verification Result |
|---|---|---|---|---|---|---|
| H-01 | HIGH | Untracked artifact, immutable revision and CI | Implementation now tracked in external base 2a5c85b; record modified-tree/source hashes and approved doc cleanup. Commit/push/remote CI remain a process exception under explicit owner prohibition. | Report/checklist, entry docs, Get-Phase00Snapshot.ps1, evidence manifests | Dependency graph, local reproducible build/test gate | PARTIAL / owner-policy disposition remains; no green CI or final commit claim |
| H-02 | HIGH | Shallow ADR alternatives / deferred SDK | All ten ADRs evaluate alternatives/rejection reasons. ADR-005 records dated owner deferral and licensing/runtime/fidelity criteria before later reader work. | docs/adr/ADR-001 through ADR-010 | AdrsContainEvaluatedAlternativesAndValidUtf8 | PASS locally; product selection explicitly deferred |
| H-03 | HIGH | Undefined CAD frames, Arc/normal/path semantics | Define definition geometry, local Placement, accumulated Transform and original-unit metadata; preserve unsupported normals and nested/mirror fixtures. | CadContracts.cs, Geometry.cs, ADR-006/008, cad.json, nested-mirrored.json | NestedFixturePreservesArcAndUnsupportedNormal, nested Arc endpoints, five negative coordinate cases | PASS, included in 56-case final gate |
| M-01 | MEDIUM | Ungoverned MSBuild conflicts / unused WPF/reference | Remove adapter UseWPF/unused reference; exact warning code/heading/identity gate retains three installed-API conflicts. | Revit2025.csproj, Verify-Phase00Foundation.ps1, build docs | Complete Release build and warning gate | PASS: three specified identities, zero errors |
| M-02 | MEDIUM | Unproven ADR ordering / encoding | Remove ordering claim, state actual external baseline/amendments; verify UTF-8 without blanket rejection of valid punctuation. | ADRs, report/checklist/build docs | ADR UTF-8 test; full docs strict decode/U+FFFD check | PASS current encoding; original chronology explicitly not claimed |
| M-03 | MEDIUM | Version probe, missing arrays and serializer policy | Probe major before typed parse, strict unknown-field policy, explicit InvalidDataException and null-safe arrays; discriminator ordering documented. | Core/Cad/Bim contracts, ADR-008/009 | ExplicitEnvelopeFailures, MissingArraysHaveTypedValidationError, OutOfOrderDiscriminatorIsExplicitError | PASS in final suite |
| M-04 | MEDIUM | Confirmed predictions could lose metadata | Remove boolean; enforce from SourceDrawing provenance unless explicit validated manual action. Manual objects remain non-predicted. | Core/Bim contracts, bim.json, ADR-009 | SourceProvenanceCannotBypassPredictionWithoutManualAction; manual and confirmed-prediction regressions | PASS in final suite |
| M-05 | MEDIUM | Exact geometry predicates / unused tolerance | Separate structural and geometry APIs; caller budgets control boundary closure/segment degeneracy. Exact affine row/definition identity are intentional. | Geometry/Bim/Cad contracts, ADR-006 | ToleranceControlsClosureAndDegeneracy, OpenBoundaryFailsGeometryButCanPassStructure | PASS in final suite |
| M-06 | MEDIUM | Serial starvation and listener failure | Four workers, short first-frame deadline, recoverable accept/request IO, bounded creation and unexpected-exit recovery; exhausted Faulted shutdown safe. | PipeProtocol.cs, ADR-004 | FiftyConnectAndDropCyclesPreserveListener, PingSurvivesFourIdleClients, creation/supervision/fault regressions | PASS tests and new actual-host lifecycle |
| M-07 | MEDIUM | Magic protocol strings / self-reported identity | Typed command/status/error enums; exact v1 documented; client verifies OS pipe server PID in addition to payload correlation. | PipeProtocol.cs, ADR-004, IpcTests.cs | OsProcessIdentityRejectsImpersonation, forged version/host/correlation tests | PASS tests and real Revit PID check |
| M-08 | MEDIUM | Nonreproducible host gate / incomplete project boundaries | Manual gate builds/deploys/PINGs with configurable installation/toolchain, hashes and public path-free evidence; dependency graph covers all production projects. | Verify-Phase00Host/Foundation.ps1, Install script, FoundationDocumentTests, build docs, CI | AllProductionProjectDirectionsAreEnforced, ForbiddenDependencyMutationIsRejected; two final host gates | PASS locally; licensed host outside public CI by design |
| M-09 | MEDIUM | Handle case and redundant source identity | Uppercase canonical hexadecimal IDs retain raw handle; validate full source identity/path agreement; cross-document membership limitation explicit. | Core contracts, ADR-007, report | CanonicalHandlesAndConsistentSourceReference | PASS identity checks; cross-document check deferred |
| L-01 | LOW | Array record equality / mutability | Document transport-snapshot semantics and content comparison/revalidation. No deep equality/immutability guarantee. | ADR-008, architecture/build docs | IndependentSnapshotsCompareByContent | PASS chosen documented alternative |
| L-02 | LOW | PID-only log growth / startup logging | Dated names, twenty-file retention, nonfatal pruning failures. Trace fallback is recorded; a missing log cannot pass host verification. | ConnectorApplication.cs, build/report docs | Actual startup/PONG/STOPPED logs, both new host sessions | PASS recorded lifecycle; persistent log-failure UI not implemented |
| L-03 | LOW | Updating a closed WPF window / async exceptions | Skip UI updates after cancellation/close; handle unexpected errors and evidence-write failure. | MainWindow.xaml.cs | IPC cancellation tests and manual verification-window closure | PASS manual lifecycle; no automated UI shell test claimed |
| L-04 | LOW | ReviewStatus/property bag scope | Keep ReviewStatus as stored data only; remove open Parameters bag rather than permit untyped generation semantics. | BimContracts.cs, bim.json, ADR-009 | BIM round-trip/all-eight-types/provenance tests | PASS, no review workflow implemented |
| L-05 | LOW | Loose deployment files / missing automated hashes | Copy DLL/deps only, manifest identity checked before copy; emit and verify SHA-256 pairs. | Install-Phase00Connector.ps1, deployment manifests | Two final host deployments and built/deployed/current hash comparison | PASS; legacy unrelated deployed artifacts not purged |
| L-06 | LOW | Missing process waits full timeout | Check process first, distinct HostNotRunningException; one-second connection budget and total caller deadline. | PipeProtocol.cs, MainWindow.xaml.cs | NonexistentProcessFailsFast asserts <1 second; real stopped Revit evidence | PASS automated and actual Desktop behavior |
| L-07 | LOW | Only simplistic synthetic fixtures | Hand-authored nested/mirror INSERT/Arc and unsupported-normal fixture added; actual DWG output deliberately absent. | nested-mirrored.json, README, tests | NestedFixturePreservesArcAndUnsupportedNormal round trip and geometry validation | PASS fixture contract; no real-DWG fidelity claim |
| L-08 | LOW | Self-issued PASS and omitted limits | REVIEW_REQUIRED, explicit pending review/CI/commit disposition and complete unsupported/limitation lists. | PHASE_00_REPORT.md, PHASE_00_CHECKLIST.md | Evidence cross-check and report inspection | Submitted for re-review; no self-issued phase PASS |

### Owner-request IDs (later pasted instructions)

| Review ID | Severity | Issue | Resolution | Changed Files | Tests Added/Updated | Verification Result |
|---|---|---|---|---|---|---|
| H-01 | HIGH | Listener fault recovery | Accept-time IO continues with a fresh pipe; three-attempt worker supervision and creation recovery; Faulted only after exhaustion; disposal safe. | PipeProtocol.cs, ADR-004, IpcTests.cs | 50 connect/drop cycles; unexpected restart; unrecoverable Faulted/shutdown | PASS 56 tests and real shutdown/restart |
| H-02 | HIGH | Final-code Revit evidence | Regenerate two final sessions, request correlation, disconnect and both normal STOPPED logs; hashes and base/modified-tree identity recorded, stale checkpoints superseded. | Host/snapshot scripts, new evidence, report | Actual Revit 26648 -> disconnect -> restart 15456 PONG | PASS actual host; final uncommitted tree distinguished from base hash |
| M-01 | MEDIUM | Decidable coordinate/path invariants | Definition path/identity/owner structure checks and tolerance-aware actual INSERT resolution/composition. | CadContracts.cs, Geometry.cs, fixtures, ADR-006/008 | Five negative coordinate cases plus nested/mirror positive fixture | PASS |
| M-02 | MEDIUM | Original source unit preservation | Preserve mm/cm/m/in/ft with explicit conversion scale; geometry remains normalized mm, no deserialize scaling. | CadContracts.cs, ADR-006/008, fixtures | Non-mm serialization/conversion and mismatched scale rejection | PASS |
| M-03 | MEDIUM | Mutable prediction-flag bypass | Remove IsSemanticPrediction; require from SourceDrawing provenance unless documented manual confirmation; validate action metadata. | Core/Bim contracts, ADR-009, bim.json | Source provenance bypass, invalid manual action, manual/no-prediction cases | PASS |
| M-04 | MEDIUM | Structure versus geometric validity | Explicit ValidateStructure()/ValidateGeometry(tolerance); open Floor/Room closure and near-zero segments fail geometric checks. | Cad/Bim/Geometry APIs, docs and tests | Open-boundary and tolerance tests | PASS |
| M-06 | MEDIUM (recommended) | Idle connection starvation | Four bounded workers and 250 ms first-frame budget; document continuous hostile saturation limit. | PipeProtocol.cs, ADR-004 | One and four idle clients while legitimate PING succeeds | PASS |
| M-07 | MEDIUM | Nested repository / Explorer / weak tests | Resolve projects only from DtoB.sln; controlled child process for identity; elapsed-time bound; evaluated ADR reasons. | FoundationDocumentTests.cs, IpcTests.cs | Project graph/mutation, process mismatch, timed missing PID, ADR reason checks | PASS final suite in normal workspace |
| L-01 | LOW | Invalid --verify-pid arguments | Validate complete argument shape/positive PID/nonempty path; explicit usage error, no PING. | App.xaml.cs, invalid-arguments.json | Actual zero-PID invocation and observed usage dialog/no evidence file | PASS manual rejection; exit code not captured |
| L-02 | LOW | Log-pruning failure stops startup | Catch individual/enumeration pruning failures and log warnings; startup continues. | ConnectorApplication.cs | Actual startup/lifecycle on both final hosts; code exception-path review | PASS host startup; faulted filesystem pruning not injected |
| L-06 | LOW | Stale/private-path evidence | Clearly supersede historical/intermediate checkpoints; redact historical private paths; new public evidence uses relative paths and version metadata. | Evidence README/JSON/logs, report, host script | Correlation/hash checks and path inspection | PASS |
| L-03/L-04/L-05 | LOW (unspecified in pasted request) | Remaining contract corrections / limits | The pasted request does not restate these issue descriptions, and the on-disk IDs differ. Original corresponding LOW entries are addressed above: snapshot equality documented, open parameter bag removed, deployment hashes verified. No unprovided finding is invented. | ADRs/contracts/install script/report | Original LOW tests/checks above | Original descriptions resolved/documented; reviewer to map any different final-review IDs |
