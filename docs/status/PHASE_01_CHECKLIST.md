# PHASE 01 Implementation Checklist

Approved 2026-10-07. Scope: Desktop project shell only. No commit/push or PHASE 02.

- [x] Read required specifications/review and inspect clean phase/01-desktop-shell baseline 4d7357f (v0.1-phase00).
- [x] Present approach and incorporate owner clarifications in ADR-011/012.
- [x] Independent project model, strict versioned JSON and safe temporary-file save.
- [x] Runtime path authority and success-only session changes; Save As/moved-file handling.
- [x] Settings, ten recent paths, bounded logging with nonrecursive fallback.
- [x] DtoB.exe WPF shell, project commands, settings and preserved connection verification.
- [x] Unsaved Save/Discard/Cancel protection.
- [x] Required regression tests and PHASE 00 suite.
- [x] Core/full Release build and all tests.
- [x] Actual Desktop restart/open, metadata, recent/settings/error workflows.
- [x] Diff review, evidence and PHASE_01_REPORT.md; stop for Antigravity.


Final gates 2026-10-08: Core/full Release PASS, 78/78 tests; owner-assisted Save/corrupted Open, actual restart/metadata/recent/settings, corrected expected-error path and actual Revit 25.4.50.35 PONG verified. REVIEW_REQUIRED, ready for Antigravity; PHASE 02 not started.

