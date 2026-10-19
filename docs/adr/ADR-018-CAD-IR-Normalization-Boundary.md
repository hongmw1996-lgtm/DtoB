# ADR-018 — CAD IR and normalization boundary

Status: Accepted

Date: 2026-10-15

## Context

The owner authorized architecture freeze after [PHASE_04_PLAN_PASS](../../reviews/PHASE_04_PLAN_REVIEW.md), not 04A implementation. Findings H-01 and M-03 require independent CAD-v2 validation and explicit implementation gates. This ADR proposes the decision for independent review; it is not Accepted.

PHASE 03 publishes immutable factual RawDrawingSnapshot. Dormant `DtoB.Cad.CadDocument` v1 requires a source path, hex-handle-only insertion paths, +Z curves and a dimension measurement. It cannot encode typed array cells, general OCS planes or absent measurement faithfully. Core `Contract.Version(int)` accepts only 1; Core `IrJson.Deserialize<T>` calls it. Analysis currently takes that v1 CadDocument and returns ANALYSIS_NOT_IMPLEMENTED. Existing v1 fixtures/consumers are compatibility obligations.

## Decision proposed

### Purpose and boundary

CAD IR records deterministic, normalized CAD geometry, placement, occurrence lineage, factual annotations and effective properties. It carries no architectural classifications or prediction confidence. BIM IR remains the later building-domain source of truth.

```text
Published RawDrawingSnapshot -> Normalize -> CAD v2
    -> future recognition -> BIM IR -> output adapters
```

Never rewrite Raw facts, defaults, source angles, handles or original unsupported dispositions. No direct CAD IR -> Revit generation. LOW-01 remains: unresolved reference graph refuses Raw assembly; normalization cannot bypass it. Valid Raw snapshots may still contain unsupported/mapping-incomplete records, so do not blanket-require typed mapping completeness.

### Ownership and exact dependency graph

`X -> Y` below means X references Y:

```text
Existing:
DtoB.Cad.Raw -> no project references
DtoB.Cad.Dxf -> DtoB.Cad.Raw
DtoB.Geometry -> DtoB.Core
DtoB.Cad -> DtoB.Core, DtoB.Geometry
DtoB.Analysis -> DtoB.Cad, DtoB.Bim

Future after authorization:
DtoB.Cad (new DtoB.Cad.V2 namespace) -> Core, Geometry (unchanged graph)
DtoB.Cad.Normalize -> Cad.Raw, Cad, Geometry, Core
```

These existing edges were inspected in csproj files. No Cad.Ir project is needed. Prohibited edges: Raw -> Normalize/Cad-v2; Dxf -> Normalize/Cad-v2; Normalize/Cad-v2 -> Revit; Normalize -> Desktop/WPF/BIM/Analysis. No project is created by this ADR gate.

Normalize is a pure .NET 8 in-memory library with conceptual API `Normalize(RawDrawingSnapshot, NormalizationOptions, CancellationToken) -> CadNormalizationResult`. No file/stream/path fallback, content hashing, lease acquisition or UI access. Source SHA-256 comes from the established managed-reader lifecycle; tuple hashing for deterministic IDs is allowed. The existing IDrawingReader facade remains dormant; do not invent a path reader here.

### CAD v2 and version isolation — H-01

Add contracts in namespace `DtoB.Cad.V2` within existing Cad. Schema major is exactly **2**. CAD-v2 owns both `CadContract` validation and `CadIrJson` codec in that boundary. Its entry points explicitly serialize/deserialize **v2 CadDocument**, not a permissive generic v1/v2 union.

CadIrJson must inspect the integer schemaVersion and require 2, validate required CAD-v2 structure and reject unsupported majors, missing/invalid version, unknown fields and invalid payloads. No try-v2-then-v1 fallback or automatic upgrade. It must never call legacy `Core.Contract.Version`, `Core.IrJson.Deserialize` or v1 CadDocument validation on the v2 route. Non-version Core finite/require helpers remain reusable; Geometry validation does not impose a schema major.

Leave PHASE 00–03 `Contract.Version(1)`/IrJson behavior unchanged, including rejection of major 2 through legacy deserialization. Do not change BIM/Project schema or loosen strictness merely to admit CAD2. CAD-v2 codec owns stable property/collection order and invariant finite numbers, UTF-8/no BOM/LF and normalized zero. No machine path or timestamp participates in canonical data. See [identity ADR](ADR-020-Occurrence-Identity-and-Expansion.md).

### Canonical document contract

Minimum v2 model: schema/algorithm/policy version; document source identity and unit decision; canonical conventions; immutable transform value for SourceToModel; source registry; layer/style catalogs; shared block definitions and source entity/INSERT templates; typed occurrences and model-space roots; analytic geometry/factual annotation references; per-record ledger; independent normalization and retained Raw diagnostics.

SourceToModel initially identity maps mm -> mm, never repeats unit scaling. Dimension source measurement is optional with presence state, not fabricated zero. Geometry/transform policy is [ADR-019](ADR-019-Coordinate-Normalization-Transforms.md); occurrence/cell structure is [ADR-020](ADR-020-Occurrence-Identity-and-Expansion.md).

### Provenance and publication

Require valid nonempty DrawingId/RevisionId GUIDs and SHA-256 FileHash at the production normalizer boundary; reject absent/invalid authority without tightening Raw contracts or rewriting old Raw test contexts. Store document identity once; compact SourceRecordRef identifies stable domain/kind/index/range, SourceFactRef identifies role/pair/physical-line evidence. Preserve OriginalHandle plus Raw-validated CanonicalHandle where available. Never invent byte offsets or source handles. Resolve references to RawDrawingRecordView/facts using one paired snapshot, not graph copies in every entity.

Read already retained contextual pairs for missing typed styles only inside Normalize, honoring subclass/control/application boundaries. Do not reopen/relex source or change parser payloads. Preserve block/INSERT/cell lineage and derived segment ordinal.

Use separate dispositions Normalized/PartiallyNormalized/UnsupportedNormalization/InvalidNormalization and explicit nonspatial NotApplicable ledger entries. One unsupported entity/property/occurrence must not corrupt unrelated valid geometry. Invalid global authority/units/options, cancellation or resource exhaustion yields no published document; partial success is not a truncated prefix. Diagnostics have invariant codes/arguments and stable source/path ordering, retaining upstream evidence separately.

## Alternatives

- Mutate v1 in place: destabilizes validators/consumers and changes accepted fixture semantics. Rejected.
- Loosen Core version gate: mixes CAD2 with legacy BIM/CAD1 routes. Rejected.
- Add Cad.Ir plus Normalize: unnecessary package split. Rejected.
- Translate v2 to v1 for Analysis: loses new facts and creates dual-model translation debt. Rejected absent a separately justified owner decision.
- Additive v2 with an independent codec: selected for a bounded migration while preserving legacy behavior.

## Consequences and migration impact

CAD1 and CAD2 are **not intended as permanent parallel first-class domain models**. PHASE 04 introduces v2 additively, preserving the dormant Analysis-v1 boundary untouched throughout PHASE 04. PHASE 05 begins with an explicit migration of Analysis input/validation/tests to CAD2 before recognition uses it. No temporary v1↔v2 converter is created by default. Later deprecation/removal of v1 requires an explicit migration gate identifying remaining consumers, fixture/history policy and compatibility impact.

Old v1 JSON cannot acquire missing source evidence by changing its major. Regenerate v2 from valid Raw instead of silent deserialization migration. Accepted ADR-008 strictness remains; this ADR scopes the new CAD-only version boundary without rewriting accepted historical ADRs.

## Required future verification

- `CadIrJsonV2Tests`: serialize valid v2 -> deserialize v2 -> reserialize identical canonical bytes, including immutable transforms and ordering; no major==1 gate on this path.
- Strict missing/noninteger/unsupported major, unknown/duplicate field and invalid required structure rejection. Legacy major 1 still passes and legacy major 2 still fails.
- Dependency/call-path checks prevent accidental Core.Version/IrJson or v1 CadDocument validation use in v2. Runtime roundtrip must prove the route as well as static checks.
- Compact provenance resolution, record coverage, honest Partial/refusal/cancellation outcomes, unchanged Raw before/after and PHASE 00–03 regression.

These tests are requirements, not implemented or executed by this documentation task.

## Governance / non-goals

**ADR-018 must be Accepted before 04A.** 04A cannot implement coordinate/occurrence decisions still Proposed; defer those governed details or wait for their governing acceptance. All three ADRs are authored now for a complete independent review; authoring is not acceptance or implementation authorization.

No recognition, semantic building types, DWG reader, Raw/Dxf rewrite, BIM changes, Revit dependencies, new viewer, database/cloud or package creation in this gate.
