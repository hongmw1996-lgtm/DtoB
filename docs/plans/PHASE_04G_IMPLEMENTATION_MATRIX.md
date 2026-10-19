# PHASE 04G implementation matrix

Date: 2026-10-16. Authority: owner resume instruction and PHASE_04G_ARCHITECTURE_PASS (zero findings). Written before production changes. Frozen CAD2 contracts and strict codec remain unchanged. Historical PHASE_04G_REPORT.md is preserved.

## Contract map

| Actual contract | Source / producer | Identity / ownership / relationships | Provenance / completeness |
| --- | --- | --- | --- |
| CadDocument | Pure CadNormalizer composition of 04B/C/E/F and assembly | Schema2; algorithm/profile; identity SourceToModel | All structural records accounted; any unsupported/deferred channel makes Partial |
| CadEntity root | ENTITIES non-INSERT; canonical builder | Source domain, authority + source token; one root entity; effective style; ModelSpace(id,null) | Original source and geometry facts; never root template/leaf duplication |
| CadEntity template | Direct BLOCK child | Source domain (SourceInsert for INSERT); definition membership only; shared geometry; declared style | Factual source; explicit unsupported/invalid dispositions |
| CadEntity projected leaf | 04F (source, occurrence) projection | Leaf domain fields Text(sourceEntityId), Text(occurrenceId) only; shared geometry, effective style; ModelSpace only | Same original source/evidence; occurrence reference provides lineage |
| CadGeometry variants | Raw geometry via 04B/C | DerivedComponent(Text(sourceEntityId),Text("geometry")); segment adds Index(ordinal); source-specific, never style/world dependent | Exact source facts; finite or refuse affected payload |
| CadBlockDefinition | All accepted Raw definitions; reuse04E bases/IDs when reached | Definition domain existing04E source policy; source-order template children, no leaves | BLOCK / available BLOCK_RECORD refs; rejected definitions explicit |
| CadBlockOccurrence | Existing04E Value | Preserve ID/path/parent/matrices, bind INSERT context style; no persisted sign fields | Validate runtime sign against exact sign of finite matrix determinant; source unchanged |
| CadModelSpaceRecord | Roots then04E DFS/direct-child order | Existing entity/optional occurrence relation only | Root direct or projected leaf, no nested INSERT duplicates |
| CadStyle | 04F declarations/effective results | DerivedComponent source/context + role; no value-only dedup (evidence/modes matter); immutable | Supporting context evidence flattened to compact fact refs, declaration modes preserved |
| CadLayer | 04F layer catalog | Source-order unique names; duplicate names diagnosed and withheld | Source + off/frozen; no invented catalog colors |
| CadAnnotation | Raw TEXT/MTEXT/DIMENSION via04B/C | Embedded factual content/anchors/height/rotation/measurement only | Partial for omitted layout/direction; ATTRIB/ATTDEF spatial instancing deferred |
| CadProvenanceRegistry | 04E source registry + assembly facts | Same structural index; facts ordered record then ordinal role; no Raw references | Legal pair/line ranges only; no invented HEADER record |
| CadUnitDecision | 04B HeaderUnits | Existing scale/kind/decision/reason; Evidence null | Global unresolved units refuse document |
| CadCompleteness / CadRecordOutcome | Source-index ordered assembly ledger | Exactly one outcome per registry record; IDs resolve | Raw unsupported retained, nonspatial NotApplicable; never complete by geometry alone |
| CadDiagnostic | Upstream and local outcomes | Stable stage/source/path/code/arguments; bounded terminal slot | Original Raw diagnostics separate; no localized exception text |
| CadIrJson | Existing strict codec | No edits or extension fields | Final structure validation and exact roundtrip |

Plan section20 explicitly assigns complete/partial document and assembler/simple queries to04G;04H owns QA. Implement one small concrete ICadNormalizer in the existing Normalize assembly. Evolve only stale no-engine test assertions while retaining identities.

## Source coverage matrix

| Source | Raw | 04B | 04C | 04F | CAD2 /04G scope |
| --- | --- | --- | --- | --- | --- |
| LINE | Typed start/end | Units | WCS mm | Common style | CadLine; degenerate refused explicitly |
| POINT | Typed position/angle | Units/display angle | WCS mm | Common style | CadPoint; display angle not representable, explicit deferred metadata |
| LWPOLYLINE | Ordered vertices/flags/bulges/widths | Units/elevation | OCS vertices/frame | Common style | CadPolyline ordered exact line/arc segments; closure once; widths retained in provenance/deferred |
| CIRCLE | Typed center/radius | Radius | OCS center/frame | Common style | CadCircle + plane; affine placement remains occurrence |
| ARC | Typed center/radius/angles | Radius/start/sweep | OCS center/frame | Common style | CadArc + plane; no ambiguous full circle |
| SOLID | Four factual corners | Units | OCS corners | Common style | Planar boundary1,2,4,3; repeated3/4 triangle; nonplanar refused |
| TEXT | Content/anchors/height/rotation | Height/rotation | OCS anchors | Common style | Factual CadAnnotation; no glyph metrics, anchor-only bounds |
| MTEXT | Content/anchor/direction | Height/context rotation | WCS anchor | Common style | Factual CadAnnotation; unsupported direction/layout explicitly Partial |
| DIMENSION | Current typed subtype facts | Optional supported measurement/rotation | Mixed WCS/OCS anchors | Common style | Factual CadAnnotation only; unsupported subtype explicit; no graphics synthesis |
| INSERT | Source structure | Units/rotation | Anchor | Context style | Source template + existing04E occurrence; no flattened geometry |
| ATTRIB / ATTDEF | Nested text/source association | Some scalars | Anchors | Common style | Explicit unsupported spatial instancing; source/ledger retained |
| Other Raw families | Factual retained records | Not applicable | Unsupported | Common where available | Unsupported entities or nonspatial ledger; all52 original unsupported records accounted |

## Query and resource policy

Bounds are query-only. Geometry bounds are local; definition bounds include direct non-INSERT geometry only and explicitly flag nested INSERT/annotation/unsupported content as Partial. Occurrence bounds aggregate that occurrence and its descendant model-space leaves using accumulated matrices. Model bounds aggregate actual ModelSpace roots/leaves using SourceToModel. Annotation anchors are labeled AnchorOnly, never glyph extents. Missing/invalid geometry produces partial/unavailable results. HEADER extents never enter queries.

Exact analytic circle/arc extrema after affine projection; no tessellation. Signed bulge theta=4atan(b), center midpoint+left(chord)*(1/b-b)/4, stable reciprocal branches; segment facts preserve source bulge and endpoints. Degeneracy uses injected numeric policy without snapping.

Plan sections17 and ADR020 explicitly count occurrence leaf entries: one session counter reserves for each root/shared geometry payload (including CadPolyline container), each emitted segment, and each projected leaf wrapper. Wrappers are counted as materialization entries, not claimed to be additional Geometry payloads. No multiplication of shared geometry by occurrences; no reset/refund. Development maximum250000 inclusive; attempted250001 refuses the entire document. One global diagnostic cap5000 includes terminal slot and retained Raw diagnostics. Cancellation never publishes a document.

Implementation tests must independently assert source geometry, identities, style isolation, analytic bounds, exact/one-over budgets, no-refund, cancellation, full ledger/strict codec, official controlled source and culture/process evidence. Preserve all1258 baseline IDs and04A–F digests. No04H/PHASE05, I/O/vendor/Revit/UI/recognition, commit or push.
