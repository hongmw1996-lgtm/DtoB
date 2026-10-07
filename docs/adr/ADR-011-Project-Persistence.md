# ADR-011-Project-Persistence: Project persistence

## Status
Accepted — PHASE 01 owner approval, 2026-10-07.

## Context
The Desktop shell needs metadata persistence without CAD/BIM or Revit coupling. Opening a moved file must not create unsaved edits.

## Alternatives

| Option | Evaluation and disposition |
|---|---|
| UTF-8 JSON .dtob | Selected: one inspectable metadata file, no database/runtime dependency. |
| ZIP project container | Rejected for this phase: there are no drawings or embedded artifacts requiring a container. |
| SQLite | Rejected for this phase: transactions and queries add dependencies without a metadata-only need. |

## Decision
DtoB.Project is independent of WPF and Revit. Schema version 1 contains ProjectId, Name, CreatedAt, ModifiedAt, DtoBVersion and optional informational LastKnownPath. Runtime CurrentFilePath comes exclusively from the actual opened/successfully saved path. A moved-file open is clean. UUID/CreatedAt survive Save As; Save As changes the session location only on success. Unsupported versions, unknown fields and invalid required metadata are rejected; no automatic migration.

Save writes a unique temporary file in the target directory, completes serialization and flushes to disk, then replaces an existing target or moves to a new target. Failures preserve the previous target and session; cleanup is best effort. Only successful replacement commits CurrentFilePath, ModifiedAt and clean state. Recent updates follow success separately and cannot invalidate an already completed project save.

## Consequences
No autosave or concurrent-editor merge. Local files only; power-loss guarantees depend on filesystem behavior. LastKnownPath is informational and never used to redirect an open.

## Migration Impact
New project format only. CAD/BIM schemas, unit/coordinate conventions and existing UUID policies remain unchanged. A future incompatible project format needs an ADR and explicit migration.
