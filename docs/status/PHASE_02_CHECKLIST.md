# PHASE 02 Implementation Checklist and Technical Proposal

## Status
Implementation authorized after owner approval and Antigravity's APPROVED_FOR_IMPLEMENTATION re-review, 2026-10-08 Asia/Seoul. ADR-013/014 were written before product code. The proposal below records the approved design; the execution checklist and PHASE_02_REPORT.md distinguish completed verification from pending work. No independent implementation PASS is claimed. No PHASE 03, commit or push.

## Inspection and baseline evidence
- [x] Read DtoB_FINAL_MASTER_PLAN.md completely (2,069 lines).
- [x] Read PHASE_INDEX.md, AGENTS.md, ARCHITECTURE.md, CODEX_INSTRUCTIONS.md and tasks/PHASE_02.md completely.
- [x] Read PHASE_01_REPORT.md and independent PHASE_01_REVIEW.md completely; review verdict is PASS. Historical implementation report still says REVIEW_REQUIRED and is not treated as the latest independent verdict.
- [x] Read ADR-005/007/008/010/011/012; inspect Core/CAD identities, Project persistence, Desktop UI/session and regression tests.
- [x] Inspect branch and initial working tree: phase/02-dwg-upload, clean; HEAD/main be27e7d712332506bc0b5d51b70720ab8fff8a86. v0.2-phase01 resolves to ffa0ee077606c3ae6a9da476765894d327998750; subsequent main commits finalize evidence.
- [x] Rebuild existing Core and full solution Release and run all tests: PASS, 78/78, no skips/failures. Logs/TRX: artifacts/phase02/baseline-20261008. Installed VS MSBuild 17.14.23, SDK files 8.0.416, VSTest 17.14, runtime 8.0.22. dotnet.exe is absent; retain the existing verified VisualStudio build alternative without security changes. Existing three Revit MSB3277 warning identities pass the unchanged gate.
- [x] Repository contains no actual DWG/DXF fixture. Owner supplied the external DtoB_TEST_2STORY_PACKAGE; inspected its specification and files read-only: DtoB_TEST_2STORY_R2018.dwg (46,400 bytes, raw header AC1024, SHA-256 3C2F4E880C63B2F247DD5D60EA2CB8D68BC72A99C505E233D88B446D59B9BAAE) and DtoB_TEST_2STORY_R2018.dxf (79,147 bytes, text HEADER beginning). DWG filename does not agree with its raw format code; never infer file metadata from its filename or package mm specification. Synthetic header fixtures will be labelled as such, never as fully parsed/valid CAD.

The latest owner instruction authorizes PHASE 02 despite historical PHASE 00-only wording in ARCHITECTURE.md/CODEX_INSTRUCTIONS.md. Preserve architectural decisions; make a small current-phase documentation update during implementation rather than allowing stale gate wording to imply a new architecture.

## Proposed drawing model
Project schema v2 adds a drawing collection. A drawing entry contains the current registered revision only; no history browser or revision diff engine.

| Field | Meaning |
|---|---|
| DrawingId | Persisted nonempty UUID for the logical drawing; unchanged by Replace/Save As/reopen. |
| RevisionId | Persisted nonempty UUID for a newly accepted content revision; distinct from DrawingId and hash. |
| OriginalFileName | Original basename, including Unicode; display only, never a managed destination path. |
| SourceFilePath | Absolute source path recorded at import; informational provenance, never runtime managed-path authority. |
| ManagedFilePath | Validated relative path to the immutable managed revision copy, resolved from actual CurrentFilePath's parent. |
| FileExtension | Normalized .dwg or .dxf. |
| FileSize | Positive 64-bit byte count of the completed copied stream. |
| FileHash | SHA-256, canonical uppercase hexadecimal, of the exact bytes copied. |
| DetectedDwgVersion | For a recognized DWG signature, persist the exact format code with HeaderDetected evidence (e.g. AC1024); this is file-format metadata, not the creating application version or complete integrity. Unrecognized/unavailable metadata is Unknown/PendingReader. DXF marks DWG-specific metadata NotApplicable. |
| DetectedUnit | PendingReader with no unit value; expected mm is not a detection result. |
| ImportedAt | UTC timestamp for the successfully registered current revision. |
| Validation | BasicHeaderOnly: describes the actual validation strength. |

Runtime availability and integrity are separate, nonpersisted state. Persist revision facts/validation level, not a claim that the asset is currently verified. Do not add CAD handles, semantic prediction metadata or IR entities.

## Fast runtime status: Project Open and normal Refresh
Project Open reads and validates the .dtob manifest and resolves constrained managed paths. Its asset-status checks, and normal UI Refresh, use only filesystem metadata and, where needed, a short-lived readability handle probe with zero content reads. No managed-file SHA-256, whole-file read, header rescan, automatic background hash task or integrity-verification call is permitted in either path.

| Fast state | Evidence and meaning |
|---|---|
| Ready | Managed file is present and the inexpensive access probe succeeds; ready for file management only, not a content-integrity or CAD-validity guarantee. |
| SourceUnchecked | Default on Open; SourceFilePath is not probed. Explicit asynchronous Check Source can produce SourcePresent/SourceMissing/SourceUnreadable. |
| ManagedMissing | Managed path is absent, determined without hashing. |
| ManagedUnreadable | Access/stat/readability probe fails; no complete content read is required. |
| ReaderPending | CAD interpretation/unit detection remains deferred; independent of availability. |

An optional persisted FileSize comparison uses filesystem length only. A differing size is an unverified metadata warning, never HashMismatch. Normal checks scale with drawing count and filesystem access, not total managed-file bytes; constant wall-clock time or a <100ms guarantee is not claimed. Remote storage can still make metadata IO slow. No monitoring infrastructure is added.

Ready must be shown alongside Integrity: NotVerified until an actual explicit verification succeeds. Only explicit Check Source can report SourceMissing, which may coexist with Ready; ordinary Refresh preserves the last source observation and reader state remains separate. These status observations never dirty the project or change revision facts.

## Explicit integrity verification
| Integrity state | Evidence |
|---|---|
| NotVerified | No completed explicit SHA-256 verification for the current managed revision in this session. This is the default on every Project Open. |
| Verified | A completed explicit operation hashed the managed bytes and matched persisted FileHash; display verification time as a snapshot, not continuous assurance. |
| HashMismatch | A completed explicit operation actually hashed the managed bytes and obtained a different SHA-256. Never inferred from size, timestamps, availability or a stored prior-session result. |

Add a small user-triggered Verify Integrity command for the selected drawing. It streams SHA-256 asynchronously, supports cancellation and leaves the WPF UI thread responsive. It does not scan every drawing automatically. Hashing/copying at Add/Replace, destination validation in explicit Save As and a future pre-reader verification are separately bounded IO workflows; none may be reached through Project Open or normal Refresh. PHASE 03 pre-reader invocation is a future contract only, not an implementation in PHASE 02.

Normal Refresh can retain an explicitly obtained result only as a labelled verification-at-time snapshot. If observed file size/last-write metadata changes, the project/revision/path changes, or the file becomes unavailable, invalidate the current integrity state to NotVerified without hashing. Matching filesystem metadata is not proof of unchanged bytes. No watcher, periodic verification or automatic rehash is planned; same-size edits with preserved timestamps require another explicit verification to discover.

Before an explicit verify starts, reset current integrity to NotVerified. Cancellation, missing/unreadable file or other IO failure leaves it NotVerified and reports the availability/operation error, not HashMismatch. Commit a result only if the originating session, DrawingId, RevisionId and resolved managed path still match; reject stale results. Only completed content comparison produces Verified/HashMismatch. Expected hash, DrawingId, RevisionId, timestamps and project dirty state are not changed by verification. Verification results are not serialized and are reset on reopen.

The planned availability probe and integrity service have separate narrow interfaces. Open/Refresh depend on metadata/access probing only; the explicit verification service owns hashing. Test spies and fail-on-read/hash fakes will enforce these boundaries without a timing assumption.

## Drawing identity and duplicate/replacement rules
Preserve ADR-007 UUID policy. FileHash is content identity, not domain identity. PHASE 03/04 can receive the same DrawingId/RevisionId through the existing reader boundary without changing CAD IR now.

| Operation | Deterministic behavior |
|---|---|
| Same bytes imported again, any filename/path | Report duplicate and select the existing current drawing; no extra logical drawing, dirty change or copy. |
| Same normalized source path with different bytes | Add refuses ambiguous second registration and directs user to explicit Replace on the existing drawing. |
| Same basename, different source path and content | Allow a separate DrawingId; show source path/short ID to distinguish names. Basename alone is not identity. |
| Replace with different bytes | Preserve DrawingId, assign new RevisionId, hash, size/source metadata/time; publish new managed file before committing candidate state. |
| Replace with same hash | Explicit no-op; preserve all revision metadata/dirty state. |
| Replacement matches another drawing's current hash | Explicit duplicate rejection, no merge or state change. |
| Remove then re-import | Removed entry is absent; explicit re-import creates new drawing/revision IDs. No silent resurrection/history feature. |
| Original deleted after import | Managed copy remains usable; display SourceMissing. No redirect or automatic re-import. |
| Restart/project move | Use persisted IDs and actual opened .dtob directory; preserve metadata and clean state. Missing assets are visible, not silently dropped. |

Hash deduplication is project-local across currently registered drawings. No global registry or historical revision reconciliation.

## File ownership alternatives
| Option | Evaluation |
|---|---|
| A: external reference only | Least copy cost for large files, but broken/mutable paths undermine reproducibility, backup, portability and revision comparisons. Not proposed. |
| B: managed copy without original path | Stable bytes and portable project backup, but loses useful source provenance and source-location UX. Not proposed. |
| C: original source metadata plus managed copy | Proposed: reproducible current revision and future comparisons, explicit source provenance, no source writes; costs one streamed managed copy per accepted revision. |

Proposed layout, beside the actual .dtob file:

```text
Project.dtob
<ProjectId>.dtob-assets/
  drawings/<DrawingId>/<RevisionId>.dwg
```

The asset folder is derived from ProjectId, not project filename; renaming .dtob within the same directory works. Store only constrained relative managed paths, validate containment/expected UUID layout, and reject escaping/reparse destinations before writes. A portable backup/move includes both .dtob and its asset folder. Moving .dtob alone stays clean but shows missing managed files. Never silently fall back to original source bytes.

New unsaved projects must first complete the existing Save workflow before Add Drawing is enabled to perform import. Cancellation creates no managed files or drawing mutation. This avoids a second staging-workspace relocation protocol. If that prerequisite save succeeds but import later fails, the already completed save remains valid; import itself commits no drawing change.

Save As into another directory stages all referenced managed copies into the destination asset tree, verifies them against persisted hashes during this explicit bounded copy workflow, then safely publishes .dtob. This workflow runs asynchronously and is not an Open/Refresh prerequisite. Only after the final JSON save succeeds may session path/timestamps/clean state/Recent change. Source project and asset bytes remain unchanged on failure. Same-directory Save As can reference the same immutable project-ID asset tree. No single transaction spanning several files is claimed: publishing manifest last and preserving immutable prior files provides the safety contract.

Remove deletes only the project's drawing reference after confirmation; replacement never overwrites the prior revision bytes. Do not immediately delete old managed revisions: saved manifests, unsaved Discard and same-directory Save As may still refer to them. Failed-operation temporary/unpublished files are cleaned where possible; discarded or unreferenced published copies may remain. Automatic garbage collection and history browsing are explicitly deferred and documented as disk-retention limitations.

## Validation, hashing and IO failure behavior
Use a small BCL-only file-registration service in DtoB.Project, not IDrawingReader implementation or a CAD SDK. Check existence/readability, .dwg/.dxf, nonzero size and practical bounded signature/header structure. Initially support controlled DWG and text DXF; binary DXF/unsupported text encodings receive explicit Unsupported diagnostics rather than a pretend success. Exact header-check acceptance cases are documented alongside fixtures before implementation, without full entity/header-variable parsing.

Basic signature checks detect obvious wrong/empty inputs; they cannot establish complete DWG structural integrity. Do not claim full corruption detection. DWG format code is HeaderDetected when the exact known signature is available; units remain PendingReader. DXF semantic/header-variable extraction stays deferred. Files with a plausible header but damaged body may pass this limited file-management gate; later reader validation is still required.

Open source read-only with writer exclusion, stream to a unique temporary file in the owned destination directory, hash/count those same bytes, complete copy/flush, validate staged header, resolve duplicates, then move into a new revision path without overwrite. No whole-file byte array, arbitrary geometry tolerance, vendor SDK, conversion service or AI. Async IO and cancellation keep the WPF shell responsive; disable conflicting project commands while an operation is active and verify the originating session before committing.

Only successful registration changes drawing collection/dirty state. Add/Replace failure or cancellation preserves prior drawing metadata, path, project timestamps and dirty state. No import success is shown before required IO succeeds. Safe .dtob save remains manifest-last; required assets must pass inexpensive availability/metadata checks, and a currently known integrity error is reported rather than ignored. An ordinary manifest Save does not silently initiate full integrity verification; NotVerified does not imply either a match or a mismatch. Explicit Save As copy validation rejects actual content inconsistency before publishing the destination manifest. Opening a project still displays its metadata and missing-asset errors for remediation without reading/hashing asset content. Temp cleanup failures are logged separately without hiding the original error. Never overwrite/delete external CAD, even for Remove/Replace.

## Project schema migration
Keep UTF-8 JSON, introduce project schemaVersion 2 with required drawings array. Add an explicit validated v1-to-v2 in-memory conversion with empty drawings. Opening v1 preserves UUID/name/timestamps/version/path authority and stays clean; show that the next explicit Save writes v2. Do not rewrite or create a backup automatically during Open. Safe Save preserves v1 bytes on failure; no automatic downgrade/reader fallback for unknown schema versions. PHASE 01 executables will reject v2 explicitly rather than silently lose drawings.

Separate project version dispatch from LocalJson's existing settings/recent version-1 policy. Settings, Recent, CAD IR and BIM IR schema versions stay unchanged. Validate drawing IDs, revision IDs, relative paths, sizes, hashes, required arrays and duplicate invariants; reject null/malformed entries. Copy collections for mutations so previous snapshots are not modified before successful IO.

## Proposed Desktop UI
Keep existing shell commands, Settings, Recent and separate Connection Verification. Add Project > Drawings list/tree, selected metadata panel, Add/Replace/Remove/Reveal Source Location and Reveal Managed Location. No source CAD execution/editor launch. Disable unavailable Reveal commands with a clear missing-source explanation. Selected drawing shows file facts, logical/revision IDs, validation level and PendingReader metadata. Show fast availability separately from Integrity: NotVerified/Verified/HashMismatch. Provide selected-drawing Verify Integrity with busy/cancel feedback; ordinary selection/Refresh must never trigger it.

File dialog and thin file-drop handlers call the same registration service. Multi-file drop is processed sequentially and stops at the first failure/cancellation: earlier successful imports stay registered; the status explicitly reports that remaining inputs were not processed. Drawing changes participate in existing Save/Discard/Cancel. WPF UI remains small ViewModels/services with no new MVVM/DI framework.

## ADRs required before implementation
- ADR-013-Drawing-Storage-Identity.md: ownership alternatives, managed layout, existing DrawingId/revision UUID policy, duplicates/Replace/Remove, immutable files, manifest-last asynchronous Save As and retention. State explicitly that immutability is a DtoB write policy, not proof against external tampering; Open/Refresh perform no content hashing, and integrity is obtained only by explicit bounded verification with SHA-256.
- ADR-014-Project-Schema-Evolution.md: project v2, strict v1 migration and old-reader behavior; settings/recent/IR separation. Persist FileHash/FileSize as revision facts, but do not serialize runtime availability or a current integrity verdict. Opening v1/v2 stays clean and initializes integrity to NotVerified without reading complete asset content. This evolves ADR-011 deliberately without changing its path/safe-save guarantees.

ADR-013/014 recorded the approved choices before product code changes. No SDK selection or CAD/BIM schema ADR is needed in this phase because neither boundary changes.

## Planned automated tests
- Controlled DWG header and text DXF registration fixtures; explicit distinction from actual parsed CAD; Unicode filenames.
- Unsupported extension/encoding/binary DXF, nonexistent, locked/unreadable source where reproducible, empty/bad header and wrong extension/signature.
- Deterministic streamed SHA-256, copied-byte hash/size equality and immutable original bytes.
- All duplicate table cases, case-normalized source path, same basename/different bytes, different basename/same bytes.
- Replace: DrawingId preserved, RevisionId/hash/time update; same-hash no-op; duplicate-target refusal; old file intact.
- Remove reference only; Discard restores prior saved collection and old managed bytes remain.
- Save/reload/move with drawing IDs/facts; source deletion independent of managed availability; missing/unreadable managed files are visible without hashing and do not dirty Open. Corrupt content is reported as HashMismatch only after explicit content verification.
- Deterministic no-hashing/no-content-read tests: inject spies/fakes into the actual Project Open and normal Refresh paths; assert zero hash invocations and zero managed-content Read calls, and fail the test immediately if either is attempted. Reading the .dtob manifest is allowed and counted separately.
- Missing managed files produce ManagedMissing with zero hashing/content reads; optional length mismatch produces an unverified warning, not HashMismatch.
- Large-asset path regression: run the same Open/Refresh assertions with fake managed metadata advertising small and multi-GB sizes; prove identical dependency/call paths and zero content reads/hash calls regardless of reported asset size. Do not allocate/hash large fixture bodies just to make a timing claim.
- Explicit Verify Integrity invokes streamed SHA-256 and reports Verified for matching bytes; an explicitly verified modified asset, including a same-length modification, produces HashMismatch. No mismatch is reported before that operation.
- Open/reopen always starts NotVerified; Changed metadata invalidates an earlier explicit result without hashing. Cancellation/IO failure stays NotVerified; stale revision/session verification results are rejected; integrity results never change dirty state or revision facts.
- Async boundary test: a controllably pending verifier does not complete until released and supports cancellation; results/errors are presented by Desktop on the UI context. Manual large-file verification confirms responsive WPF UI. Optional performance smoke observations supplement, never replace, the deterministic no-read/no-hash tests; no fixed <100ms gate.
- Failed Add/Replace/cancellation preserves complete session/dirty state; source/target locks, failed flush/move and practical temp cleanup through a narrow injectable file-operation boundary.
- Save As copies referenced assets; intermediate copy/final JSON failure preserves original file/session/Recent; no external overwrite; no stale manifest.
- Relative traversal/rooted paths/invalid IDs and reparse destinations rejected; no silent missing-array acceptance.
- v1 migration preserves identity and clean state, saves v2 only on explicit success; malformed/future project schemas rejected; settings/recent stay v1.
- Multi-file drop success/failure results, command/session consistency, all 56 PHASE 00 and 22 PHASE 01 regression cases, existing dependency-boundary test.

## Expected changes
Reuse DtoB.Project and DtoB.Desktop; no new production project/dependency or solution restructuring. Add DrawingMetadata.cs, DrawingFileService.cs and small, separate asset-availability/integrity services with testable boundaries to packages/DtoB.Project; evolve ProjectPersistence.cs with explicit migration. Extend Desktop MainWindow.xaml/.cs and ShellViewModel, possibly a small DrawingViewModel. Add DrawingManagementTests and schema/fixture/no-hashing-boundary tests; update existing project tests to distinguish v1/v2 and copy fixtures via test csproj. Add labelled datasets/controlled/phase02 fixtures and README. Add ADR-013/014, phase guide/current-architecture notes, checklist/report and verification evidence. No Core/CAD/BIM/Analysis/IPC/Revit implementation changes are planned.

## Implementation and verification checklist
- [x] Finalize owner-reviewed ADR-013/014 before code changes.
- [x] Implement drawing revision model and explicit project v2 migration; retain state schema v1.
- [x] Implement fast Open/Refresh availability probing with zero hashing/managed-content reads and integrity initially NotVerified.
- [x] Implement explicit selected-drawing async Verify Integrity with cancellation, revision/session guards and separate runtime results.
- [x] Implement streamed basic validation/hash/copy and safe managed paths.
- [x] Implement deterministic duplicates/Add/Replace/Remove with success-only session changes.
- [x] Extend safe Save/Save As for referenced managed assets and project portability.
- [x] Add drawing list/properties, status, file dialog, shared Drag & Drop and reveal commands.
- [x] Add fixture/failure/migration/identity/dependency and deterministic Open/Refresh no-read/no-hashing regressions; keep old regression coverage.
- [x] Run final Core-only Release build, full Solution Release build and all tests: artifacts/phase00-review/phase02-20261008-final, 111/111, no skips; unchanged Revit warning gate.
- [x] Manually run actual DtoB.exe with a real controlled test drawing; add/list/save/close/restart/reopen and metadata comparison. Native dialogs were owner-assisted; clean-close regression fixed and retried.
- [x] Verify duplicate, Replace, Remove, missing original (temporarily move only owned source copy) and PHASE 01 New/Open/Save/Recent/Settings regression. Actual Revit native PONG and normal host STOPPED verified; capture-tool limitations are recorded.
- [x] Inspect complete Git diff and new files, fix issues, record source/binary/evidence hashes in docs/status/evidence/phase02 and new actual-host evidence.
- [x] Write docs/status/PHASE_02_REPORT.md with all required sections and evidence-based Exit Criteria; REVIEW_REQUIRED, ready for independent Antigravity implementation review. Stop at PHASE 02; no commit/push.

## Explicitly out of scope and limitations
PHASE 03/viewer/CAD rendering; full DWG/DXF parser or SDK selection; CAD entity handles/IR extraction; units guessing/geometry conversion; Xref resolution/Dynamic Blocks; semantics/Wall/Door/Window recognition; AI/ML; BIM IR/Revit generation; database/cloud/accounts/autosave; historical revision comparison/automatic asset garbage collection; background monitoring/watchers or automatic integrity scanning on Open/Refresh. Integrity is a completed-operation snapshot; metadata probes cannot detect every same-size/timestamp-preserving change. Actual controlled DWG/DXF files are identified in the owner-provided external package and remain unchanged. Final implementation acceptance is pending independent review; the execution report records readiness and exact coverage limits.

## Historical Antigravity proposal revision (before re-review approval)
- Review: reviews/PHASE_02_REVIEW.md, verdict CHANGES_REQUIRED_BEFORE_IMPLEMENTATION, read completely.
- Severity: HIGH. Issue: HashMismatch on normal reopen/Refresh would imply full managed-asset reading/hashing and scale with total bytes.
- Design resolution: separate inexpensive availability from session-local NotVerified/Verified/HashMismatch; prohibit managed-content reads/hash calls/background verification from Open/Refresh; require explicit asynchronous verification to produce a content verdict.
- Tests planned: actual Open/Refresh spy/fail-on-read/hash assertions, missing/large-asset metadata cases, explicit SHA-256 and modified-asset mismatch, invalidation/cancellation/stale-result/clean-state cases. Timing assertions are not the primary gate.
- Changed file in this revision: docs/status/PHASE_02_CHECKLIST.md only. ADR-013/014 wording is revised above; ADR files and product code are not created yet. Independent review document is preserved.
- Verification result: planning-document consistency and scope reviewed; no new implementation, build or automated-test result is claimed. The earlier 78/78 baseline remains baseline evidence only. Ready for owner review of the revised proposal, not phase-completion review.
- Unchanged approved choices: logical/revision UUID separation, immutable managed revisions, hybrid source provenance, ProjectId layout, duplicates/Replace, v2/v1 migration, manifest-last Save As, path safety, future CAD boundaries and PHASE 02-only scope.
- Next step: stop here for owner review; do not begin PHASE 02 implementation or PHASE 03 and do not commit/push.

## Approved implementation follow-through
The subsequent independent re-review in reviews/PHASE_02_REVIEW.md approved implementation. The owner then authorized this implementation. The historical stop above describes the earlier proposal gate, not the current task. Production Open and actual ShellViewModel.Refresh use only DrawingFileService.Probe; IContentHasher is a separately injectable explicit-operation dependency. Fail-on-OpenRead/hash spies prove the normal paths cannot read/hash assets, including advertised 8 GB metadata and missing assets. Explicit verification invokes real SHA-256 and detects same-length edits; cancellation/stale-session tests reject incomplete verdicts. Availability and integrity stay separate and nonpersisted. The execution report will record final manual evidence and remaining limitations.

## Header reference and fixture evidence
DWG header format-code checks will use Autodesk's documented code table, not filenames or guessed years: https://www.autodesk.com/support/technical/article/caas/sfdcarticles/sfdcarticles/drawing-version-codes-for-autocad.html . DXF text HEADER and binary-sentinel distinction use the official DXF reference: https://help.autodesk.com/cloudhelp/2025/ENU/AutoCAD-DXF/files/GUID-FC1C3C69-DBC2-49E4-893A-000D6538C0FE.htm . No Autodesk runtime/API dependency is introduced. Unit mm in the package specification is expected test context, not detected import metadata.

## Final implementation-review fixes (2026-10-13)
These amendments supersede the original 111-test implementation checkpoint above. Antigravity's implementation verdict was CHANGES_REQUIRED; M-01/M-02/M-03 were absent and have now been implemented for final re-review.

- [x] M-01: stop reparse traversal at the actual project-directory boundary; allow junction ancestors while rejecting reparse asset components/final file and manifest file itself.
- [x] M-01 regressions: real junction-parent empty Save/Open, Add/Save As and root/nested asset-tree rejection without external writes. Use unprivileged junction creation instead of privileged symbolic links (L-04).
- [x] M-02: Open/normal Refresh/selection have zero SourceFilePath I/O, including UNC; SourceUnchecked is the default. Explicit Check Source runs asynchronously, supports cancellation and stale-result rejection, and never changes project facts/dirty state. No managed-to-source fallback.
- [x] M-02 regressions: forbidden local/UNC source probes; cached present/missing/unreadable states; pending/cancelled/stale checks; missing managed with present source cannot fall back.
- [x] M-03: all rows expose availability/source/reader/integrity text; Open/Refresh show aggregate counts without row selection, reads or hashes. Preserve selection after ItemsSource refresh (L-08).
- [x] M-03 regressions: missing/unreadable/size warnings exposed without selection; forbidden content/hash operations; selection restored after simulated WPF binding reset.
- [x] Post-fix Core and full Solution Release build; all 126 tests pass (56 PHASE 00, 22 PHASE 01, 33 prior PHASE 02, 15 review-fix cases). Artifacts: artifacts/phase02-review-fixes-20261013.

Actual post-fix UI evidence, final diff review and remaining LOW findings are recorded in PHASE_02_REPORT.md. No new schema, SDK, IR, analysis, IPC or Revit architecture decision; no PHASE 03 or commit/push.
