# ADR-013-Drawing-Storage-Identity: Drawing ownership and revision identity

## Status
Accepted — owner approval and Antigravity APPROVED_FOR_IMPLEMENTATION, 2026-10-08.

## Context
PHASE 02 registers controlled DWG/text DXF files without CAD interpretation. Preserve ADR-007 UUID identity and ADR-011 safe project saving.

## Alternatives
| Option | Evaluation and disposition |
|---|---|
| External references only | Rejected: source deletion/modification breaks reproducibility, portable backup and revision evidence. |
| Managed copies without source paths | Rejected: loses source provenance and useful original-location navigation. |
| Source provenance plus managed immutable copies | Selected: reproducible reader input and provenance with simple streaming file IO and no database. |

## Decision
DrawingId is the persistent logical UUID; RevisionId identifies an accepted content revision; SHA-256 FileHash identifies bytes. ManagedFilePath is the authoritative future reader input, relative to the actual opened project directory. SourceFilePath is provenance only. Store <ProjectId>.dtob-assets/drawings/<DrawingId>/<RevisionId>.<extension>; filename changes do not alter identity. Reject rooted/traversal/reparse escapes. Never write, overwrite or delete original CAD.

Same current hash on Add selects the existing drawing without mutation. Same source path with different bytes requires explicit Replace. Same basename at another path with different bytes is a separate drawing. Replace preserves DrawingId, allocates RevisionId for changed content, rejects another drawing's hash and treats same-hash input as a no-op. Publish immutable new revision bytes before candidate session mutation. Remove affects registration only. Prior revision files remain, protecting saved manifests and Discard.

Require first project save before import. Different-directory Save As asynchronously copies/verifies referenced assets and publishes the project manifest last; path/timestamps/Recent/clean state commit only after success. Same-directory Save As can share the immutable asset tree. Preserve original manifest/assets on failure; clean unpublished temporary files where possible and report cleanup failures.

Open/Refresh perform only metadata/access probes, zero managed-content reads or hashes, and launch no hash scans. Availability and session-local integrity are separate. Every open starts NotVerified. Only explicit completed async SHA-256 comparison yields Verified or HashMismatch; cancellation/IO failure stays NotVerified. Results carry verification time and are invalidated by observed metadata/session/revision/path changes. Matching metadata cannot prove integrity; immutable means DtoB's write policy, not protection against external tampering. A future reader must explicitly verify before consuming managed bytes; no reader implementation here.

## Consequences
Backup/move includes manifest and ProjectId asset directory. Missing source does not invalidate managed input. No revision browser, garbage collector or background monitoring. Discarded/published orphan copies and old revisions can accumulate. Basic header validation does not prove CAD integrity. Binary DXF is explicitly unsupported; units stay PendingReader.

## Implementation-review clarification (2026-10-13)
M-01: reparse checks stop at the actual project directory (exclusive). Check the manifest itself and every managed-asset component below that boundary, including the final file and directories before/after creation. A junction at or above the project directory is allowed. Unbounded ancestor rejection breaks ordinary project saving without improving containment; exact UUID paths and the project-relative prefix still constrain destinations. This is a local single-user path guard, not protection against an attacker racing filesystem mutations.

M-02: Open, ordinary Refresh and selection perform **zero SourceFilePath filesystem I/O**, for local and UNC provenance alike. Source status starts SourceUnchecked. Only explicit Check Source runs a selected-source metadata/access probe on a worker task; its cached SourcePresent/SourceMissing/SourceUnreadable result is runtime-only. Cancellation stops awaiting the result; an already-running OS probe may finish later, and its result is not applied. Session changes reject stale results. Reveal Source requires a cached SourcePresent result and rechecks existence asynchronously only on explicit invocation. No managed-to-source fallback exists. Lazy selection probing was rejected because selection should not silently contact the original host.

M-03: each drawing row exposes managed availability, source-check state, reader-pending and integrity state. Open/Refresh additionally show aggregate counts. Both read the runtime cache and introduce no content reads or hashing. A missing managed asset is visible before selection.

Import attempts may leave empty UUID directories as well as retained revision/orphan files; duplicates are detected after the single streamed copy/hash. Retaining these directories avoids adding cleanup/monitoring infrastructure in this phase.

## Migration Impact
No CAD/BIM schema or handle policy changes. PHASE 03/04 reuse drawing/revision UUIDs; project format migration is ADR-014.
