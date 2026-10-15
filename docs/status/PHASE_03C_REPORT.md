# PHASE 03C — Native Text DXF Raw Geometry Extraction

Date:2026-10-14 (Asia/Seoul). Branch phase/03-cad-data-extraction; HEAD142100deb62e8eb3692ded84a95962548925731f. No commit/push.

## Status / exact scope

**PASS_WITH_KNOWN_LIMITATIONS — ready for owner and independent Antigravity03C implementation review.** Not independent03C approval or full PHASE03 completion. Latest reviews confirm PHASE_03A_PASS and PHASE_03B_PASS; current baseline293/293 was rebuilt/tested before changes. Prior reports retain their historical review state.

Implemented factual native LINE/LWPOLYLINE/ARC/POINT/SOLID and separately fixture-gated CIRCLE extraction from immutable03B structural evidence. No file access/re-tokenizing in the mapper. Controlled89 core records map Supported, with geometry coverage complete for these profiles and zero new geometry diagnostics. Global/effective status stays **Partial** for unsupported/deferred records. Legacy POLYLINE/VERTEX sequence/subtype mapping remains gated; no generic legacy support claim.

## Files / contracts added

- packages/DtoB.Cad.Raw/DxfRawGeometryContracts.cs
- packages/DtoB.Cad.Dxf/DxfGeometryLimits.cs
- packages/DtoB.Cad.Dxf/TextDxfGeometryReader.cs
- packages/DtoB.Cad.Dxf/TextDxfGeometryReader.Polyline.cs
- tests/DtoB.Tests/TextDxfGeometryReaderTests.cs
- datasets/controlled/phase03c/README.md plus16 hand-authored .dxf source fixtures
- docs/status/PHASE_03C_REPORT.md

Only existing-file update: appended03C evidence to PHASE_03_CHECKLIST.md. No solution/project/package configuration change, SDK installation, production Desktop/Project/Analysis/Revit integration or accepted ADR change. All245 existing files matched their pre-task hashes before the checklist append; final protected comparison permits that append only. Existing03A/03B source, contracts, tests, reviews, reports, fixtures, old spike and Core/CAD IR/BIM/Project/IPC architecture are unchanged.

## Geometry result model / architecture decisions

Use a separate small DxfGeometryReadResult and immutable RawGeometryRecord/Payload hierarchy, rather than editing03B records, emitting normalized CadDocument or assembling a premature snapshot. This implements the accepted ADR015 additive source boundary/ADR016 native parser sequence. No persisted schema/units/coordinate/source-ID major change or new ADR needed.

Result references the **same DxfStructuralReadResult**, which references the original immutable lexical pairs. Geometry records reference the same structural records with StructuralRecordIndex as a QA locator, not a source ID. Models: RawCoordinateFacts, RawLine, RawLwPolyline/RawLwVertex, RawArc, RawPoint, RawSolid, RawCircle. No Core Point3/Transform3, WPF/Revit/vendor objects. New payload JSON discriminator is `$geometry`, used for deterministic derived QA only; no project/CAD IR/BIM serialization change.

GeometryDispositionCounts cover the mapped/gated/known unsupported geometry candidates. EffectiveRecordDispositionCounts replace candidate dispositions by structural record index while keeping the original333-record inventory intact. No structural mutation. StructuralEvidence.Diagnostics/counts remain03B evidence (including its original deferred aggregate), not recomputed current coverage. GeometryMappingComplete describes the explicitly inspected geometry candidates, never all CAD/unknown custom content; global status/effective counts still disclose unprocessed coverage. Refused oversized structural inventory omits effective counts with EffectiveInventoryAvailable=false instead of traversing beyond its limit.

## Common evidence / numeric / default policy

Every mapped scalar uses existing DxfSourceFact: kind, native nullable value, Presence/Validity, source pair indices and physical span. Raw text is retrieved from the one lexical store; not copied into every geometry fact. Fields is the accounting list; payload/common properties reference those same fact instances. Mapped indices + UnmappedPairIndices cover every record pair exactly once. Handles/owners remain original structural/raw evidence; no canonicalization, synthetic IDs, cross-drawing/occurrence identity or global graph.

Common facts: explicit Layer in unmarked/AcDbEntity context, thickness39 and extrusion210/220/230 in the relevant entity subclass. Existing03B subclass/control/XDATA markers exclude application/control payload from geometry. ARC center/radius/base fields use AcDbCircle; angles use AcDbArc. LINE/AcDbLine, LWPOLYLINE/AcDbPolyline, POINT/AcDbPoint, SOLID/AcDbTrace, CIRCLE/AcDbCircle. Foreign-context geometry fields are diagnosed and retained unmapped. Unmarked minimal synthetic records are explicitly supported through the same source field grammar.

Absent optional source fields are Absent/NotApplicable, null typed value/empty source indices; no Z0, thickness0, normal(0,0,1), flags0, bulge0 or width defaults. Missing required fields are Absent/Invalid with a located MissingRequiredField diagnostic; no invented coordinate. In an entirely unsupported/gated profile, common typed properties are **null (unmapped)**, distinct from verified source absence; all raw pairs remain inspectable. A regression ensures a present unsupported layer/thickness/normal is not falsely labelled absent.

Invariant checked integers/doubles, only finite typed geometry numbers; raw1.0/1e+20/-0.0 retained. NaN/Infinity/overflow/invalid syntax and genuine singleton duplicates produce located Invalid facts/record; all occurrences retained, no winner/zero coercion. Invalid records yield Partial mapping with safely retained evidence, not a clean success. No arbitrary geometry tolerance or repair.

## LINE

Start10/20/30, End11/21/31, thickness39, extrusion210/220/230. X/Y at both endpoints required; Z optional. Native coordinates only. Zero-length endpoint equality remains a Supported raw source record. No length calculations, cleanup, snapping, deduplication, normalization or placement. Separate fixture with INSUNITS1 and coordinates1..6 proves no scaling into mm.

## LWPOLYLINE

Declared count90 is required checked positive integer for the initial raw profile; raw flags70 optional, elevation38/thickness39/constant width43 and extrusion optional. Each eligible code10 begins a vertex; subsequent20/40/41/42/91 attach to that vertex until the next10. X/Y required per vertex; start/end width, bulge and identifier optional. Code20/optional vertex fields before any10 are diagnosed and retained unmapped. Within this tested profile, optional vertex fields before Y are InvalidVertexOrder; duplicate Y/width/bulge/id is not overwritten. These are field-context rules, not coordinate-based grouping.

Vertices keep source order, ordinal, source span and all explicit/absent facts. Declared/actual count mismatch is Invalid/VertexCountMismatch with both facts intact; no repair. No appended closing point, bulge-to-arc conversion, triangulation, lengths, width precedence/elevation/normal transforms. Closed flag and simultaneous constant/per-vertex widths remain raw declarations.

All16 controlled polylines independently checked for declared/actual vertex count, flags, ordered X/Y pair indices and native values, optional vertex widths/bulge/id source indices/presence, elevation/width/thickness/normal presence.61 source vertices total, maximum4. Reviewed exact source goldens include the2-,3- and4-vertex records below; checks go beyond counts.

## ARC / POINT / SOLID

ARC: center10/20/30, radius40, start50/end51 **source degrees**, explicit thickness/extrusion. Radius must be strictly positive and finite for this implemented profile; zero/negative/nonfinite is Invalid, not absolute-valued/repaired. Angles720/-90 remain exactly720/-90; no radians, wrapping, swaps, tessellation or OCS conversion.

POINT: position10/20/30, optional thickness39, angle50 source degrees and extrusion. Optional Z/angle/normal absent remains absent. No display-style/rendering interpretation.

SOLID: four distinct corner slots10/20/30,11/21/31,12/22/32,13/23/33; X/Y required, Z optional, thickness/extrusion retained. Actual triangle's repeated third/fourth source coordinates keep separate pair provenance. Degenerate repeated corners also retained; no collapse/reordering/triangulation/normal computation/mesh. DXF SOLID does not imply3DSOLID support.

## CIRCLE acceptance / legacy / unsupported geometry

CIRCLE support is separately verified by **actual circle-reviewed.dxf** through03A→03B→03C, not inferred from the controlled file (which has zero CIRCLE). Its reviewed source has AcDbCircle, center(-1.5,2.25,-3.5), radius2.5, thickness0.125, normal(0.1,0.2,1.0). Center X pair12/value line26, radius pair15/line32, thickness pair16/line34, normal Z pair19/line40 are independently checked with BCL source lines and exact SourceFact provenance. Original fixture bytes remain unchanged. Invalid radius cases are also tested. Claim is this limited raw field profile, not universal circular entity compatibility.

Legacy POLYLINE/VERTEX explicitly stay DeferredTo03C with LegacyPolylineGated diagnostics, null payload/common fields and full source accounting. SEQEND remains original03B DeferredTo03D evidence; no subtype/sequence interpretation. Dedicated legacy-gated.dxf uses flags64 to ensure accidental mesh/polyface/3D support is not claimed.

ELLIPSE/SPLINE/HATCH/3DSOLID/REGION/MESH/HELIX/ACAD_PROXY_ENTITY in source entity/block scopes retain Unsupported raw geometry records and located diagnostics; no approximation. Other unknown/custom records remain in structural evidence/effective unsupported inventory with original03B diagnostics. No source disappearance or claim that an unrecognized custom object was understood as geometry.

## Resource limits / cancellation

Finite injectable DxfGeometryLimits; positive validation, diagnostics>=2. Existing03A/03B limits remain unchanged.

| Guard | Default | Measured controlled input |
|---|---:|---:|
| Geometry candidates |512|89|
| Vertices per LWPOLYLINE |64|Max4|
| Total geometry coordinate slots |1,024|196: LINE118 + LWP61 + ARC1 + POINT12 + SOLID4|
| Fields/pairs per geometry record |512 (same approved structural cap)|Max20|
| Stored geometry diagnostics |5,000|0 new|
| Structural input records / effective-count work |4,096|333|
| Lexical source pairs |250,000|8,737|

Total coordinate-slot budget counts LINE endpoints, centers, POINT positions and SOLID corners as well as LWP vertices; extrusion vectors are not position slots. Invalid required coordinate slots still consume allocation budget, never fabricate coordinates. Guards precede collection append/payload growth. Diagnostics reserve the last terminal slot: ordinary overflow → DiagnosticLimitExceeded/ResourceLimitExceeded, never silent truncation/success. Refused input remains source-addressable through the immutable evidence reference. Oversized structural inventory is not re-traversed during failure materialization. Exact and first-excess budget tests cover all dimensions. These are measured development safety caps, not performance guarantees.

Token checks during input/refusal, record traversal, context/scalar mapping, vertex grouping/fact creation, counting/accounting and before/after collection construction. Result freezing/effective count traversal checks each item; Canceled never advertises geometry completeness/success. Pre-cancel and deterministic cancellation during result freezing are tested. Synchronous parsing/numeric conversion is bounded; checkpoints were source-reviewed, no timing assertion or claimed mid-loop latency guarantee. No file/stream/worker/UI ownership introduced; later integration must schedule worker work and own managed lease/session guards.

## Controlled results / independent QA

Original controlled file independently scanned using BCL physical lines plus code0 record boundaries; no production mapper/SDK-generated expected output:

| Family | Independent source inventory | Mapped / Supported |
|---|---:|---:|
| LINE |59|59|
| LWPOLYLINE |16|16|
| ARC |1|1|
| POINT |12|12|
| SOLID |1|1|
| Total |89|89|

Every exact record range/order and every mapped fact's native numeric/text value, physical lines and source index is compared with the independent source lines; full pair accounting for every record. For every LWP, independent raw10/20 positions and per-vertex optional codes are checked, not copied from production grouping. Hand-inspected goldens:

| Record / inclusive pair range | Selected source fact |
|---|---|
| LWP2713..2727 | width2723=0.15; vertices2724/2725=-0.5/-0.5 and2726/2727=0.5/0.5 |
| LWP2786..2801 | Y2797=0.16439898372402537, Y2801=-0.16439898372402537;3 source vertices retained |
| LWP2821..2836 | X2829=-200.0, Y2836=200.0;4 source vertices retained |
| LINE2856..2867 | Start.X2862=0.0, End.X2865=900.0 |
| ARC2868..2880 | radius2877=900.0, start2879=0.0/end2880=90.0 degrees |
| POINT3021..3029 | Y3028=-1300.0 |
| SOLID2747..2766 | distinct third.Y2762 and fourth.Y2765 both-0.16439898372402537 |

Pair indices0-based; code line=2*pair+1, value line=2*pair+2. Names/scopes/handles/coordinates remain source facts, not expanded occurrences or semantic building counts.

| Actual effective inventory | Before03C | After03C |
|---|---:|---:|
| Supported |99|188|
| DeferredTo03C |89|0|
| Unsupported |52|52|
| DeferredTo03D |93|93|
| Invalid |0|0|
| Total child records |333|333|

Measured output, not forced status: Partial, GeometryMappingComplete=true for89 inspected profiles, zero new geometry diagnostics. Original structural serialization is identical before/after mapper invocation, including its99/89/52/93 source dispositions and59 diagnostics. No structural mutation to obtain188/0 effective counts.

## Supplemental fixtures / tests / builds

16 reviewed actual Text DXFs: circle-reviewed, lwpolyline-full, lwpolyline-count-mismatch, lwpolyline-missing-y, lwpolyline-invalid-order, line-native-3d, line-optional-z, line-zero-length, line-missing-y, line-nonfinite, line-duplicate-x, solid-degenerate, arc-native-degrees, point-angle, unsupported-ellipse, legacy-gated. Source contents/independent expectations in phase03c/README.md. Embedded source fixtures additionally cover duplicate/invalid vertex fields, optional numeric failure, checked identifier overflow, radial failures, context controls/XDATA/foreign subclasses, source absences, safety/cancellation and unsupported families. All use the actual lexical/structural/geometry pipeline, not fake geometry objects as sole proof. Only materialization-cancellation test directly constructs a result to test its freeze boundary.

Baseline before changes **293/293 PASS**. Final **356/356 PASS**, zero failures/skips:

| Cases | Count |
|---|---:|
| New03C geometry |63|
| Unchanged03B structural |103|
| Unchanged03A lexical |55|
| Existing dependency additions |9|
| PHASE00/01/02 baseline regression |126|

Dedicated03C filter **63/63 PASS**. Core-only Release **PASS**, full Solution Release **PASS**, exact known Revit MSB3277 assembly-warning identity gate **PASS**, no new compiler warning. VS2022 MSBuild/Roslyn + installed SDK8.0.416 files/VSTest .NET8.0.22 fallback; normal dotnet.exe unavailable as before, no toolchain/security change. Intermediate331/349 runs passed but are not final evidence. No actual Desktop/Revit verification needed/claimed: pure new library code, no IPC/host/UI change.

## Determinism / dependencies / source hash / diff

Two independent FileAccess.Read/FileShare.Read streams, fresh lexical/structural parses and geometry maps serialize identically. First stream is closed before geometry mapping, proving no hidden file access. No timestamps/machine paths/elapsed time/exception text in content. Deterministic complete result JSON SHA256: **FB467C3FA0CEE622B8B616B3D04B6EAD608EB3E76CD5FFE703DA2F2D70A85E8E**.

Original96,621-byte DXF before/after/final SHA256 remains **2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC**. No original source/managed bytes or project state written. Raw project remains runtime-only/no NuGet libraries; Dxf only references Raw and standard runtime. New System.Text.Json annotations are standard .NET metadata, not a package/vendor dependency. Existing architecture/runtime/forbidden-dependency tests pass unchanged; no ACadSharp/ODA/RealDWG/netDxf/Autodesk/Revit/WPF/Core geometry/Project dependency.

Self-reviewed source/default/context mapping, LWP ordering/counts, unsupported/gated null vs absent distinction, raw radii/angles/corners, coordinate allocation budgets, cancellation, immutable publication and effective count overlay. git diff --check PASS; new-file strict UTF-8/whitespace and dependency/scope scan PASS. All245 pre-task files unchanged except authorized checklist append. No ADR/review/previous-test/report rewrite; pre-existing unrelated dirty tree preserved.

Evidence: ignored artifacts/phase03/03c-geometry-20261014/before.json, checklist-before.md, independent-records.json, controlled-geometry.json, controlled-qa.json, geometry-tests.log/geometry.trx; baseline and intermediate build evidence retained. **Final:** final/core-build.log, build.log, tests.log/tests.trx, final-gate.log, final-verification.json, diff-check.log. These describe356 tests, not293 baseline or intermediate331/349.

## Known limitations / unsupported cases / stop

AC1032 strict Text DXF profile only. Supported raw geometry does not imply normalized/renderable/semantically valid BIM geometry. Z/default resolution, units/frames, effective style and geometric degeneracy policy remain future work. LWP optional fields before Y are rejected in this limited tested profile; additional field ordering/subtypes need reviewed fixtures. Legacy sequences entirely gated; no mesh/polyface/3D-polyline support. Unsupported families retain raw data, no approximation. Unknown/custom content remains structural evidence, never a fabricated primitive. Exact positive-radius/positive-declared-count profile constraints are not tolerance-based cleanup.

No03D RawBlockDefinition/RawInsert/RawText/RawMText/RawDimension/ATTRIB/ATTDEF semantics/MINSERT expansion/measurement; no03E global identity/handle-owner graph; no RawDrawingSnapshot, persistence/.dtob schema change, Viewer or PHASE04. No mm/radian/OCS-WCS/Arbitrary Axis Algorithm/block/INSERT/nested transforms/ByLayer-ByBlock resolution/snapping/deduplication/cleanup/CAD IR/recognition/Revit generation. Reader-managed integration and project/runtime lifecycle remain later authorized work.

Next step: **owner and independent Antigravity03C implementation review only. STOP — no03D/03E/PHASE04, no commit/push.**
