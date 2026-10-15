# ADR-017 — Desktop diagnostic extraction lifecycle

Status: Accepted for owner-authorized PHASE03H,2026-10-15.

## Alternatives

| Option | Evaluation |
|---|---|
| Same managed read lease for verification and extraction | Selected: preserves registered-byte authority without reopening a mutable path. |
| Hash during ordinary project Open or metadata Refresh | Rejected: adds unbounded UI work to a metadata-only workflow. |
| New viewer project or external CAD renderer | Rejected: existing Desktop and runtime-only linked tests suffice for diagnostic presentation. |

## Decision

Use the existing Desktop application as the integration boundary. An explicit
Viewer Run/Refresh opens only AssetPaths.Resolve(CurrentFilePath,ProjectId,drawing),
using FileAccess.Read/FileShare.Read, and retains the same seekable read lease
through registered SHA256 verification and03A→03F extraction. Reject oversized
input before hashing using the existing lexical byte cap; verify against existing
DrawingRevision.FileHash, rewind the same lease, then parse. Hashing on normal
Open/Refresh/selection is rejected; reopening between hash and parse is rejected
because it weakens the same-file guarantee. No source-path fallback or project
state/ID/hash/schema changes. FileShare.Read prevents concurrent writes/deletes
while extraction owns the lease; standard filesystem/reparse limitations remain.

Run extraction on a Desktop background Task using existing parser tokens and
owned stream disposal. Preserve stage/status/code/source diagnostics on failure;
never bypass03F/LOW-01. A small UI request epoch plus captured project/path/drawing/
revision/hash context rejects stale success AND failure. Cancel/invalidate on
selection/project change or window close. No global session redesign or cache.

Snapshot-only presentation classes live under apps/DtoB.Desktop/Diagnostics and
are linked into existing runtime-only tests, following ShellViewModel convention.
This is simpler than another solution project or external viewer dependency.
Raw/Dxf dependencies and accepted ADR001–016 remain unchanged.

ENTITIES and one raw block-definition-local view remain separate. INSERT only
anchors; annotation only anchors/factual trace. View-only XY camera/math and
ARC screen drawing never feed04, alter native values or become reusable CAD IR.
Fit uses displayable XY bounds, not HEADER extents. Bulge chords and omitted
Z/extrusion/thickness spatial interpretation are visibly labelled. No effective
style, fonts, units, occurrence transforms, persistence or Revit behavior.

## Consequences / tests

Explicit extraction reads/hash twice on one bounded lease; normal metadata flows
remain unchanged. Tests cover managed-only access, mismatch/missing/format/parser/
cancel/refusal, stream disposal, stale publication, scope/child order, view-only
camera/primitives/trace/immutability/determinism. Actual WPF smoke verification
is separate from model tests; report any UI limitation without claiming execution.
