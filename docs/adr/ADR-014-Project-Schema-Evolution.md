# ADR-014-Project-Schema-Evolution: Project v2 drawings

## Status
Accepted — owner approval and Antigravity APPROVED_FOR_IMPLEMENTATION, 2026-10-08.

## Context
PHASE 01's strict project v1 cannot silently accept a new drawings collection. Evolve only the project format while retaining runtime path authority and safe-save guarantees.

## Alternatives
| Option | Evaluation and disposition |
|---|---|
| Add drawings to strict v1 without version change | Rejected: older readers cannot retain the new collection safely and strict unknown-field checks conflict. |
| Project v2 with explicit validated v1 migration | Selected: preserves existing identity and clean Open while old readers explicitly reject incompatible data. |
| Database/container conversion | Rejected: unnecessary storage/runtime complexity for small controlled drawing collections. |

## Decision
Project schemaVersion 2 requires drawings, including an empty array. Validate v1 with its original strict fields, convert to v2 in memory with empty drawings, retain project identity/metadata and keep Open clean. Never rewrite on Open. Explicit successful Save emits v2 using same-directory temporary serialization/flush/replace. Unknown versions and fields fail explicitly. Old PHASE 01 readers reject v2, with no downgrade that loses drawings.

Settings/Recent remain schema 1; CAD/BIM IR versions remain independent and unchanged. FileHash/FileSize are persisted revision facts. Runtime availability, integrity verdicts and verification timestamps are not persisted. Open v1/v2 initializes NotVerified without asset-content reads or hashing. Persist expected format detection evidence, not guessed units.

Strictly validate required collection, non-null entries, nonempty/unique drawing and revision UUIDs, normalized supported extensions, exact UUID-derived relative managed paths, nonempty provenance/filename, positive byte counts, canonical SHA-256, UTC revision timestamps and current-drawing duplicate invariants. Use copied collection snapshots for mutations, never mutate a previous project snapshot before successful required IO.

## Consequences
Opening v1 is backward-compatible; successful Save upgrades to v2. Migration failure preserves the active session. Assets are a multi-file project with manifest-last safety rather than a cross-file transaction. Runtime observations do not dirty the project.

## Migration Impact
Evolves ADR-011 project format only; preserves authoritative CurrentFilePath, informational source paths, safe saving and identity. ADR-012 state/logging and ADR-001 through ADR-010 architecture remain intact.
