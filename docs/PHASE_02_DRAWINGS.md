# PHASE 02 Drawing Management

Launch the Release DtoB.exe. New/Open a project, save an unsaved project first, then Add Drawing using the dialog or drop DWG/text DXF files. Select a drawing for metadata and status. Source DWG/DXF files are never modified.

Project JSON is schemaVersion 2. Version 1 opens clean with an empty drawings collection and upgrades only on explicit successful Save. Older PHASE 01 readers reject v2. Actual opened CurrentFilePath remains authoritative.

The manifest and sibling <ProjectId>.dtob-assets directory are one portable project. Back up/move both. Save As into a different directory asynchronously copies and verifies current referenced assets before publishing JSON. Remove deletes registration only; old revisions and abandoned copies may remain. No garbage collector/history viewer is included.

Same bytes are a duplicate regardless of filename; another source path with the same basename and different bytes is a separate drawing. A registered source path changed externally requires Replace. Replace preserves DrawingId, creates RevisionId for changed bytes and rejects duplicates of another drawing. Same-hash Replace does nothing.

Open and Refresh Status probe managed-file metadata/access only; they never touch SourceFilePath, including UNC paths. Each row shows availability and separate source/reader/integrity states; the Open/Refresh status line summarizes counts, so missing assets are visible without selecting a row. Ready means file-management availability. Integrity starts NotVerified; Verify Integrity streams SHA-256 explicitly and asynchronously, with Cancel operation available. Verified/HashMismatch are session-local completed-operation snapshots. Size/timestamp changes invalidate a snapshot without rehashing. Original source is never used as fallback.

Source starts SourceUnchecked. Select a row and explicitly click Check Source to check its recorded original location asynchronously. SourcePresent/SourceMissing/SourceUnreadable are cached observations, independent of managed availability; Refresh retains the last source observation, and reopening resets it. Reveal Source stays disabled until Check Source reports SourcePresent. Cancelling Check Source stops waiting and discards the result, but cannot terminate an OS filesystem call already in progress. No background source scan runs.

Unit remains PendingReader; known DWG header codes are HeaderDetected. Validation is BasicHeaderOnly and does not establish full CAD integrity. No viewer, parser, recognition, AI or BIM generation. Text DXF must begin 0/SECTION/2/HEADER; optional UTF-8 BOM is supported. Leading 999 comments, omitted HEADER, binary/UTF-16 DXF fail explicitly even when another reader could support them. Drawing selection does not render CAD.

Projects may reside beneath a directory junction. Reparse components inside the managed asset tree and the manifest file itself are rejected. Failed/cancelled/duplicate imports can leave empty UUID directories; duplicate detection follows the streamed copy/hash. No automatic cleanup of retained directories or revisions occurs.

Reveal Source/Managed opens Explorer at an existing file, never a CAD editor. Multi-file import/drop proceeds in order and stops on failure/cancellation, retaining earlier successes and reporting unprocessed remaining input. Save/Discard/Cancel still protects dirty registration changes.

Build: use Verify-Phase00Foundation.ps1 with RevitInstallDir and -Toolchain VisualStudio on this host (dotnet.exe remains absent). Review PHASE_02_REPORT.md for actual evidence and limitations.
