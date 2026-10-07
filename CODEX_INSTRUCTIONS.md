# Codex Instructions

Codex is the Implementation Owner. Read DtoB_FINAL_MASTER_PLAN.md, PHASE_INDEX.md, AGENTS.md, ARCHITECTURE.md, the current task specification, relevant build/contracts documentation and the latest independent review before changing code.

Inspect repository/environment/Git state; present the implementation checklist and technical decisions before implementation. Document major unit/coordinate/identity/schema decisions in ADRs. Follow the owner's approved scope and clarifications; do not implement later phases.

PHASE 00 uses C#/.NET 8, minimal WPF verification UI, in-process C# analysis boundary, isolated Revit 2025 adapter, named-pipe PING, Core mm geometry and Revit-independent CAD/BIM IR. Define IDrawingReader and synthetic fixtures only; DWG SDK product selection is deferred.

Preserve source handles/provenance and explicit unit/coordinate transforms. Predictions require confidence/evidence or a recorded manual confirmation under the approved provenance policy. Distinguish ValidateStructure() from ValidateGeometry(tolerance). Unsupported data must retain diagnostics. Do not directly connect a parser/LLM to Revit element creation.

Add meaningful fixture/regression tests for fixes. Run Core and complete Release builds, the warning gate, all automated tests and actual Revit verification for IPC/integration changes. Review the complete Git diff, including untracked work. Preserve user changes. Never infer a successful gate from stale logs or an older commit; record base commit, dirty state, source and deployed DLL hashes.

Write docs/status/PHASE_00_REPORT.md with implementation, omissions, changed files, tests/results, unsupported cases, limitations, ADR history, build/host evidence, exit criteria and one resolution entry per review issue. Status must be PASS, PASS_WITH_KNOWN_LIMITATIONS, REVIEW_REQUIRED or BLOCKED, with independent approval distinguished from local verification. Do not commit/push unless explicitly instructed. Stop for Antigravity re-review; do not begin PHASE 01.
