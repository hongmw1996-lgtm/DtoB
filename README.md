# DtoB — Drawing to BIM

DtoB V1 targets one Controlled two-story drawing pipeline:

```text
Controlled DWG -> Desktop -> CAD Analysis -> BIM IR -> 3D Preview
-> Family Mapping -> Revit Add-in -> Native Revit BIM -> RVT -> QA
```

CAD and BIM meaning remain separate: DWG -> CAD IR -> BIM IR -> Revit Adapter. Initial recognition in later phases prioritizes Layer, Color, Block and simple deterministic geometry. The authoritative plan is DtoB_FINAL_MASTER_PLAN.md, with PHASE_INDEX.md defining the current phase sequence.

PHASE 00 implements Revit-independent contracts, synthetic fixtures and a minimal WPF/Revit PING verification. Actual DWG reading, recognition, final viewers, project management and BIM generation are not implemented. DWG SDK selection is explicitly deferred by the owner.

Read START_HERE.md, AGENTS.md, ARCHITECTURE.md, CODEX_INSTRUCTIONS.md and tasks/PHASE_00.md. Build/deployment instructions are in docs/PHASE_00_BUILD.md. Final review-fix evidence and limitations are in docs/status/PHASE_00_REPORT.md. PHASE 00 remains REVIEW_REQUIRED; PHASE 01 has not started.
