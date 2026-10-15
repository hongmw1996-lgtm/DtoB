> **Current status — 2026-10-15 whole-PHASE03 final gate.** Native Text DXF
> extraction 03A–03F, deterministic QA 03G and the diagnostic-only Viewer 03H
> are implemented; each has its final independent `PHASE_03A_PASS` through
> `PHASE_03H_PASS`. See [final release gate](../status/PHASE_03_FINAL_GATE.md) for the current
> integrated build, tests, limitations and owner-acceptance status.
> ADR-015's additive raw boundary, Accepted ADR-016 and Accepted ADR-017
> together govern the implementation: Raw is runtime-only; Dxf references Raw;
> Desktop owns explicit managed-file hash/read lifecycle and WPF presentation.
> Managed AC1032 strict UTF-8 Text DXF is authoritative; DWG decoding and CAD SDKs
> are not production capabilities. Snapshot coverage remains Partial, including
> semantic Unsupported 52 (VPORT 1, APPID 3, OBJECTS 48); LOW-01 remains accepted.
> PHASE04, commit, push, PR and tag are **not authorized** by this gate.
>
> **Historical checkpoint notice:** earlier dated "current task", "STOP before03A",
> "proposed projects", "not implemented" and "ADR017 reserved" statements below
> record the earlier design/housekeeping authorization, superseded by the owner's
> subsequent 03A–03H instructions. They are not current implementation status.
> Earlier Proposed ADR-016 references are superseded by its Accepted decision;
> ADR-017 is now Accepted. Technical phase boundaries and future-phase requirements
> below remain in force. Original checkpoint text is retained without rewriting
> historical reports/reviews or changing accepted architectural decisions.

# ADR-015-Raw-CAD-Extraction-Boundary: Additive raw extraction

## Status
Accepted for the architectural boundary by the owner's PHASE 03 housekeeping direction, 2026-10-14, following Antigravity APPROVED_FOR_CAPABILITY_SPIKE. Native DXF owner amendment below governs the current source path; detailed ADR-016 remains Proposed and implementation is not authorized by this amendment.

## Context
PHASE 03 now observes raw factual CAD data; PHASE 04 normalizes it for analysis. Existing IDrawingReader -> DrawingReadResult -> CadDocument requires supported units/explicit scale and already-normalized mm geometry under ADR-006/008. Unknown units, unnormalized OCS/definition geometry, broader source entity facts and field-presence evidence cannot be represented by pretending raw data is mm or weakening strict CAD IR v1 validation.

The previous ADR-015-Viewer-Backend was an untracked/unaccepted production draft. It is replaced at the same draft number under owner instruction; its earlier trial-only design is archived in ignored housekeeping artifacts. No accepted ADR-001 through ADR-014 is renumbered or changed. Native DXF is now owner-selected; detailed parser architecture/profile ADR016 remains Proposed.

## Alternatives

| Option | Evaluation and disposition |
|---|---|
| Add IRawDrawingReader / owned RawDrawingSnapshot | Selected: preserves source/native-unit/presence facts independently and leaves existing normalized reader/schema/Analysis boundaries unchanged. |
| Change IDrawingReader to return raw data | Rejected: conflates raw and normalized contracts and requires unnecessary migrations across existing CAD IR consumers/tests. |
| Partial CadDocument or Viewer render data as raw truth | Rejected: false mm/default representation violates normalized validation, while flattened rendering loses original source structure and identity. |

## Decision

Introduce an **additive conceptual contract**, to be implemented only under later authorization:

```text
IRawDrawingReader -> RawDrawingReadResult -> RawDrawingSnapshot
```

The native reader consumes authoritative managed ASCII/Text DXF using its actual runtime location and verified content identity. Owned ordered group-code/code-value-line evidence remains alongside typed facts; no CAD SDK object model is source truth. RawDrawingReadResult represents success/partial/failure plus diagnostics; a snapshot is not automatically complete simply because library Read returned. Snapshot contains DtoB-owned header/space/table/layer/style/definition/entity/INSERT/object facts, identity, source pairs/ranges, field presence and diagnostic/completeness evidence. No vendor/WPF/Revit object or deferred SDK callback crosses the boundary.

The existing contract remains intact for PHASE 04:

```text
IDrawingReader -> DrawingReadResult -> CadDocument (normalized CAD IR v1)
```

PHASE 04 may later implement this normalized reader as a facade consuming raw extraction plus explicit normalization, subject to its own phase plan. No normalized reader, raw interfaces or schemas are coded in this documentation task.

### Units, frames and field evidence

Raw coordinates remain native drawing units, with source OCS/WCS/definition-frame normals/elevations/placement evidence. Raw types are distinct from Core mm Point3/Transform3. INSUNITS is declared-unit evidence, not proof of authoring correctness. Missing/unknown/unitless remains explicit; no assumed mm or recentering. Core double-precision mm/ADR-006 normalization semantics are unchanged.

Native facts distinguish source-confirmed fields with pair/line provenance, absent, unsupported, invalid and separately labelled derived display convenience. No source-default fabrication. Earlier library-decoded/presence-unknown classification belongs to historical SDK evidence; no CAD SDK is in the native production path. PHASE03 does not calculate measurement, INSERT matrices or frame/unit normalization. Source degree angles/placement parameters remain raw; PHASE04 owns transforms, radians/mm normalization and derived measurements. Diagnostic camera/bounds are separately labelled and never overwrite reported header values. Unsupported raw normals can be retained without claiming normalized CAD IR supports them.

### Original handles and definition/occurrence

Preserve DrawingId/RevisionId/FileHash, original DXF handle spelling and validated numeric value. Canonical hexadecimal is an additional representation, never a replacement source identity; malformed/missing handle keeps its source range and diagnostic. Missing/duplicate handles receive diagnostics, never synthetic replacements. Definition source objects are counted once; raw INSERT references retain source graph membership; PHASE04 later assembles outer-to-inner occurrence ancestry under ADR-007 without PHASE03 expansion. Blocks are not Exploded into replacement source entities. Raw definitions and INSERT source placement parameters are separate owned facts; no PHASE03 local/final/nested matrix is produced; geometry is not rewritten into normalized world space.

### MINSERT — explicit V1 unsupported expansion

Preserve original MINSERT/base INSERT handle when available and raw row/column/count/spacing/placement metadata. Emit source-linked **UNSUPPORTED_ENTITY** diagnostic identifying unsupported MINSERT occurrence expansion. Continue parsing subsequent source records safely. Do not drop the array source record, synthesize cell occurrence IDs, flatten or Explode the array. Count source record and unsupported status separately from not-expanded cells. ADR-007 stable-ID policy is unchanged. Native reader continuation/no-cell-ID regression is required during later approved implementation; historical spike evidence is reference only, not native parser proof.

### Ownership and persistence

Managed ASCII/Text DXF revision is the V1 reader source-byte authority; existing DWG registrations remain historical/ineligible for extraction; SourceFilePath is provenance only and never reader fallback. RawDrawingSnapshot is **derived extraction data**, initially in-memory and QA evidence export, not project/domain source of truth or a secondary permanent IR. It is not serialized into .dtob. No project schemaVersion 2, CAD IR v1, BIM IR or source-ID semantics change. Any independent raw QA export version is explicit and not an implicit CAD IR schema extension.

Diagnostic Viewer renders only from owned extracted data; it cannot become a separate DXF parser or analysis truth. Future Analysis consumes normalized CAD IR, BIM IR remains domain truth and Revit remains output adapter.

## Consequences

Unknown/default/partial source facts can be represented honestly without fabricating a valid CadDocument. Raw-to-normalized lineage remains auditable. Supported source types beyond CAD IR v1 do not silently extend that strict schema; PHASE 04 must review mapping/unsupported/schema evolution explicitly. ACadSharp capability result is historical ACADSHARP_CAPABILITY_FAIL / NOT SELECTED. V1 source path is owner-selected Native DXF; detailed parser/version/encoding proposal is ADR016, not accepted implementation.

## Migration Impact

Additive boundary only; existing IDrawingReader/DrawingReadResult/CadDocument, Core mm, project v2, DrawingId/RevisionId/FileHash, PHASE 02 managed authority and Analysis/Revit behavior remain unchanged. No existing persisted artifact/evidence is rewritten. Accepted ADR-001–014 retain their numbering/history.

## Following decisions

- ADR-016 — Native DXF Reader: Proposed; no ACadSharp backend ADR or continued SDK review.
- ADR-017 — Reader Lifecycle: reserved for managed lease/hash/stream/thread/disposal/cancellation/stale results before integration.

Routine diagnostic display decisions can follow without dominating reader choice. Current stop is Native DXF design revision; no parser/product projects/Viewer/PHASE04 code or SDK installation.

## Owner Native DXF amendment — 2026-10-14

V1 input is ASCII/Text DXF, initially the controlled AC1032 profile. DWG requires external user-controlled Save As/Export DXF; no direct DWG/SDK decoder or automatic conversion. ACadSharp is NOT SELECTED; preserve its failed spike report/evidence unchanged, no patch/fork/integration or ODA/RealDWG escalation.

Native lexical pairs retain raw code/value text, ordered field indices, physical code/value lines and record/section/source hash provenance. Typed facts preserve presence/absence/invalidity without source-default fabrication. Unknown/ignored content has an explicit disposition and located diagnostic. Raw export is derived QA only. Existing source-ID/Core mm/CAD IRv1/projectv2/BIM contracts remain unchanged.

PHASE03 owns raw BLOCK/INSERT facts, no final/local/nested transformation or effective-style calculation. PHASE04 owns default resolution with provenance, unit/radian/frame normalization, block composition and effective ByLayer/ByBlock. ADR016 proposed details/profile require architecture review; ADR017 lifecycle remains reserved. This amendment refines the already accepted additive raw boundary under explicit owner direction; no accepted ADR001–014 renumbering or changes.
