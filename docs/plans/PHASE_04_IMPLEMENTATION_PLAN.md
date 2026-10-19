> **Current owner checkpoint — 2026-10-16 / PHASE 04G implementation:** independent PHASE_04G_ARCHITECTURE_PASS resolved the historical contract gate without schema changes. Owner authorized04G only. The new [implementation report](../status/PHASE_04G_IMPLEMENTATION_REPORT.md) and [implementation matrix](PHASE_04G_IMPLEMENTATION_MATRIX.md) record canonical geometry, shared templates/projected leaves, strict CAD2 assembly and query-only analytic bounds. Local Release/Rebuild and1,315 tests pass; independent implementation review remains required. Historical PHASE_04G_REPORT.md is preserved. **No04H/PHASE05, commit or push.** Earlier checkpoints remain history.

> **Current owner checkpoint — 2026-10-16 / PHASE 04G contract gate:** owner authorized 04G after PHASE_04F_PASS. Inspection found that the frozen serialized occurrence has no field for the 04E orientation signs explicitly required by the new instruction. A direct source-entity/occurrence StyleId binding also needs an explicit assembly-policy decision. Per the owner's STOP-on-contract-gap rule, no product/test implementation or schema change was made. [04G contract-gap report](../status/PHASE_04G_REPORT.md) records compiled-type/strict-codec evidence and the unchanged 1,258-case passing baseline. **Architecture clarification is required before implementation; this is not 04G implementation readiness. No 04H/PHASE05, commit or push.** Earlier checkpoints below are retained history.

> **Current owner checkpoint — 2026-10-16 / PHASE 04F:** independent 04E verdict is PHASE_04E_PASS, zero findings. Owner authorized effective Layer/Color/Linetype resolution only. [Scope discovery](PHASE_04F_SCOPE.md) precedes implementation; [04F report](../status/PHASE_04F_REPORT.md) records 1,258 passing cases, all 1,188 baseline IDs and unchanged 04A–E evidence. Layer0 and ByBlock use immutable cached INSERT contexts; shared source definitions remain unchanged. Lineweight/transparency are explicitly deferred. Root depth remains1. Geometry transformation/materialization and CadDocument assembly remain04G despite wording errors in the unchanged 04E review. **STOP for independent 04F review; no 04G+, commit or push.** Earlier checkpoints below are retained history.

> **Current owner checkpoint — 2026-10-16 / PHASE 04E:** independent 04D verdict is PHASE_04D_PASS, zero findings. Owner authorized root/nested INSERT and row-major MINSERT occurrence expansion only. Local implementation/evidence: [PHASE_04E_REPORT.md](../status/PHASE_04E_REPORT.md). Core/full Release/Rebuild and 1,188 tests passed, including all 1,128 baseline IDs. Shared definitions, typed cad2 lineage, parent-times-local transforms, unscaled rotated cell spacing, active-path cycles, global monotonic budgets and cancellation are implemented. One internal 04D array-template entry point reuses its existing matrix path; public ordinary behavior and 04A/B/C/D evidence remain unchanged. **STOP for independent 04E review. No 04F+, commit or push.** Earlier checkpoints below are retained history.

> **Current owner checkpoint — 2026-10-16 / PHASE 04D:** independent 04C verdict is PHASE_04C_PASS, zero findings. Owner authorized only one BLOCK/INSERT local affine transform. Implementation/evidence: [PHASE_04D_REPORT.md](../status/PHASE_04D_REPORT.md). Independent 04D review is next. No 04E+, commit or push. Earlier checkpoints below are retained history.

> **Current owner checkpoint — 2026-10-16 / PHASE 04C:** independent 04B verdict is PHASE_04B_PASS (zero findings). Owner authorized only coordinate roles, extrusion/AAA and OCS-to-WCS projection. Local implementation and evidence: [PHASE_04C_REPORT.md](../status/PHASE_04C_REPORT.md). Next gate is independent 04C review; 04D+, commit and push remain unauthorized. Earlier checkpoints below are retained history.

> **Current owner checkpoint — 2026-10-16 / PHASE 04B:** independent 04A verdict is PHASE_04A_PASS (zero findings). Owner authorized scalar units, angles and numerical policy only. Local implementation/evidence is recorded in [PHASE_04B_REPORT.md](../status/PHASE_04B_REPORT.md). Next gate is independent 04B review. Earlier checkpoints below are historical; 04C+, commit and push remain unauthorized.

> **Current owner checkpoint — 2026-10-16 / PHASE 04A:** independent ADR verdict is
> PHASE_04_ADR_PASS; ADR-018/019/020 status is now Accepted (status-only changes).
> Owner authorized 04A only. Contracts/codec/boundary are implemented and locally
> verified; see [PHASE_04A_REPORT.md](../status/PHASE_04A_REPORT.md).
> Earlier dated Proposed/STOP-before04A statements below record planning/ADR-gate
> history. They do not override this owner checkpoint. No 04B implementation,
> commit or push is authorized; next gate is independent 04A review.

# PHASE 04 — CAD Normalization Engine / CAD IR Foundation

Architecture and implementation plan — 2026-10-15

**Current gate — 2026-10-15 architecture freeze:** independent [plan review](../../reviews/PHASE_04_PLAN_REVIEW.md) verdict is **PHASE_04_PLAN_PASS**. Owner now authorizes the working branch and three Proposed ADRs, not production implementation. [ADR gate report](../status/PHASE_04_ADR_GATE_REPORT.md) records this amendment. All ADRs remain Proposed pending independent review. STOP before 04A; no commit/push.

**Historical initial-planning status: PROPOSED — OWNER / ANTIGRAVITY ARCHITECTURE REVIEW REQUIRED.** The original instruction to avoid a phase branch applied to the initial planning task; the later architecture-freeze owner direction supersedes that restriction only. New contracts/policies below remain proposed until their governing ADRs are Accepted. This document does not authorize production code.

## 1. Current released baseline

PHASE 03 is closed: PR #6 merged; main merge commit `76ff6e0d8ccac797ba60d8c5657922c796a0e8c9`; release tag `v0.4-phase03`. Release evidence records **773/773 tests, Core Release, full Solution Release, 03G QA, 03H boundary and 86/86 source-byte audits passing**. These are historical results, not tests executed for this planning change. See [release report](../status/PHASE_03_RELEASE_REPORT.md) and [final gate](../status/PHASE_03_FINAL_GATE.md).

Historical initial-planning checkout: `phase/03-cad-data-extraction` at `c2755f04bff11e470e6482cf3aabdc9673acc725`. Its Git tree equals released main/tag tree `0ef1ca4b403dff5cd1e85958ca340d6fd0ae5cd4`; commit identities differ. No branch change is required for these documents. Existing untracked PHASE 03 release reports and historical spike files are preserved.

The preceding paragraph records the initial planning checkout. Architecture-freeze update: created `phase/04-cad-normalization` directly from `v0.4-phase03`, HEAD `76ff6e0d8ccac797ba60d8c5657922c796a0e8c9`, after confirming the old HEAD tree was identical. Preserved all local work; no commit/staging/push. The released tree remains the production baseline.

Read completely: owner planning request; AGENTS; authoritative master plan; architecture and Codex instructions; PHASE 03/04 tasks; PHASE 03 release/final-gate reports; 03F/03G/03H reviews; ADR-015/016/017. Also inspected Core, Geometry, Cad.Raw, Cad.Dxf, Analysis and dormant CAD contracts, ADR-006/007/008, PHASE 05 needs and fixture coverage.

Repository findings:

- Raw snapshots are immutable source evidence. **LOW-01 remains unchanged:** `ReferenceResolutionComplete == false` refuses assembly.
- Cad.Dxf produces Raw without normalization. Raw document provenance is optional and must be validated separately at the production normalization boundary.
- Dormant CAD IR v1 requires a source path, handle-string insertion paths, +Z arc/circle planes and a dimension measurement. It cannot faithfully encode all newly authorized facts.
- Core `Contract.Version` / `IrJson` share v1 handling with BIM; changing their version globally is unsafe.
- Geometry already provides row-major Transform3 acting on column vectors. `inner.Then(outer)` means `outer * inner`. Existing Placement lacks block-base/OCS compensation. Matrix arrays are mutable transport data.
- `services/DtoB.Analysis` is a dormant v1-input boundary returning `ANALYSIS_NOT_IMPLEMENTED`, not an implemented recognizer.

## 2. Problem statement

Produce deterministic canonical-mm/radian CAD facts from a published RawDrawingSnapshot without building interpretation. Add coordinate placement, occurrence lineage and effective metadata for future PHASE 05 consumption. A useful partial document is preferable to corrupting good geometry, but unsupported records must remain explicit.

Never rewrite Raw facts or promote their historical dispositions to conceal normalization decisions. This phase does not promise that every controlled drawing record becomes supported geometry.

## 3. Explicit architecture boundary

```text
Managed Text DXF -> PHASE 03 native reader -> published RawDrawingSnapshot
  -> PHASE 04 pure normalizer + explicit options -> normalized CadDocument
  -> future recognition -> BIM IR -> Revit adapter
```

The normalizer performs **no filesystem, managed/source-path fallback, file-content hashing, vendor, Revit, WPF or project-session I/O**. It does not reopen DXF or infer units from geometry. FileHash comes from the established managed-source boundary. Application composition retains the existing lease/integrity/reader lifecycle.

Raw and Dxf must not reference normalization. Preserve Raw diagnostics even when PHASE 04 supports additional operations. A structurally valid typed MINSERT can retain its PHASE 03 unsupported-expansion disposition while PHASE 04 adds occurrence expansion.

## 4. Canonical coordinates and units

Retain ADR-006: **millimeters, radians, right-handed XYZ, +Z up, positive counter-clockwise**. Retain row-major storage and column-vector application. No automatic UCS, recentering, north alignment, story offset or Revit feet conversion.

Definition geometry uses local WCS-oriented mm axes after entity OCS conversion. Occurrence transforms place it in drawing WCS mm. SourceToModel is a unit-preserving mm-to-mm transform, initially identity, never another source-unit conversion. Its slot does not authorize an alignment UI.

ADR-006's non-+Z rejection remains a v1 limitation; extending v2 requires ADR-019, without changing these conventions or v1 validation.

## 5. Source-to-CAD-IR pipeline

1. Validate published snapshot, graph/reference completeness, document provenance and options. Do not demand every Raw mapping-completeness flag: explicitly unsupported records can exist in valid snapshots.
2. Build compact source registry and record eligibility ledger, including nonspatial/unsupported records.
3. Resolve document units and typed angular/default facts, or refuse the document explicitly.
4. Construct layer/block catalogs and entity facts; calculate OCS bases only for applicable fields.
5. Normalize definition primitives and factual annotation anchors with source-field references.
6. Expand model-space INSERT roots, nested definitions and bounded MINSERT cells; accumulate transforms and identities.
7. Resolve effective styles per occurrence; assemble roots, geometry refs, diagnostics and partial status.
8. Validate output, calculate supported geometric bounds and optionally serialize with the CAD-v2 codec.

Stages have cancellation checks between bounded batches. Keep numeric helpers, traversal and style rules independently testable. Caller-controlled worker scheduling is sufficient; no hidden background monitor or sync-over-async UI work.

## 6. Package and dependency design

**Proposed:** retain DtoB.Cad for contracts, add namespace `DtoB.Cad.V2`, and add one `DtoB.Cad.Normalize` library. No separate Cad.Ir project.

| Component | Responsibility / dependencies |
| --- | --- |
| Cad.Raw / Cad.Dxf | Existing production code/dependency direction unchanged |
| Core / Geometry | Reuse primitives/algebra; minimal tested vector helpers only if missing |
| Cad, new V2 namespace | Core / Geometry; contracts and CAD-only codec; no Raw types |
| New Cad.Normalize | Raw, Cad, Core, Geometry; pure transformation |
| Tests | Focused normalization and cross-pipeline fixtures; no Revit host for pure math |
| Desktop / Analysis / BIM / Revit | No implementation changes proposed |

Architecture-freeze clarification (csproj inspected): existing Raw has no project references; Dxf -> Raw; Geometry -> Core; Cad -> Core + Geometry; Analysis -> Cad + Bim. Future Normalize -> Raw + Cad + Geometry + Core, with V2 inside Cad. Forbidden: Raw -> Normalize/Cad-v2; Dxf -> Normalize/Cad-v2; Normalize/Cad-v2 -> Revit; Normalize -> Desktop/WPF/BIM/Analysis. No new project exists at this gate.

Alternatives: everything in Cad mixes traversal with transport; another Ir package adds unnecessary structure; unchanged v1 cannot represent required facts; global v1 replacement risks BIM/Analysis. Choose additive v2 and one new package.

Conceptual API, not implementation:

```text
ICadNormalizer.Normalize(RawDrawingSnapshot, NormalizationOptions, CancellationToken)
    -> CadNormalizationResult { CadDocument?, Status, Diagnostics }
```

Dormant v1 IDrawingReader / DrawingReadResult describe a path-facing facade; the actual native reader now publishes Raw. Keep that facade dormant rather than fabricate a reader or silently replace it. Future composition can connect reader -> normalizer. PHASE 05 must explicitly adopt v2; no lossy v2-to-v1 bridge.

Additive v2 avoids breaking current v1 validators, fixtures and the dormant Analysis signature while required OCS/cell/absent-measurement facts become representable. It is a migration stage, **not permanent parallel first-class CAD models**. PHASE04 leaves Analysis v1 untouched throughout; PHASE05 begins with explicit Analysis boundary migration to v2. No temporary v1↔v2 converter without separate justification. Later v1 deprecation/removal requires a named migration gate and consumer/fixture audit.

## 7. Proposed CAD IR model

| Contract | Proposed minimum content |
| --- | --- |
| V2 CadDocument | schemaVersion 2, algorithm/profile version, source scope, unit resolution, canonical conventions, SourceToModel, layers, definitions, entities, occurrences, roots, ledger, diagnostics/status |
| DocumentSource | DrawingId, RevisionId, SHA-256 FileHash; no required machine path/time |
| Layer/style catalogs | Original names/source refs; source and resolved properties, visibility/off/frozen facts |
| CadBlockDefinition | BLOCK and BLOCK_RECORD refs, name/handles, base mm, ordered entity/INSERT refs |
| Source templates | One normalized factual entity/INSERT per source, geometry/annotation refs and source styles |
| CadBlockOccurrence | Source INSERT, definition, parent, typed path/cell, local/accumulated transforms, effective style |
| Primitive/annotation | Point, line, planar arc/circle, polyline segments, SOLID polygon; factual text/dimension anchors; source/local frame/status |
| Model-space leaf | Shared primitive plus occurrence context, not cloned Raw data |
| Registry/ledger | Record/fact locators and explicit disposition for every Raw record |

Dimension measurement is optional with presence state, not zero or a fabricated computed value. No building categories or prediction confidence: deterministic normalization is not semantic prediction.

Use a CAD-only strict CadIrJson v2 codec. Preserve shared Core IrJson, BIM v1, Project schema and CAD v1 fixtures. Reject unsupported versions/unknown fields under ADR-008 principles. No silent v1->v2 upgrade: missing hash/Raw evidence requires re-normalization from a valid snapshot.

**H-01 freeze:** `DtoB.Cad.V2.CadContract` owns major2 validation and CadIrJson owns typed v2 serialization/deserialization. The v2 route must never call Core.Contract.Version, Core.IrJson deserialization or v1 CadDocument validation. No permissive generic v1/v2 route or fallback. Preserve legacy major1 success/major2 rejection. `CadIrJsonV2Tests` must prove serialize2 -> deserialize2 -> identical canonical bytes plus strict unsupported/malformed-major/field rejection and legacy behavior. Non-version Core/Geometry validation remains reusable.

Reuse geometry algebra. **M-01 freeze:** Transform3 has mutable arrays; cloning only on construction is insufficient if getters expose them. CAD-v2 immutable transform values privately own defensive copies at every factory/deserialization boundary and expose only scalars or fresh Transform3/array copies. No externally writable matrix may be shared with an occurrence, definition or previously published state. Apply to SourceToModel/local/accumulated/entity transforms and defaults. Revalidate arithmetic, but never rely on revalidation to repair mutated state. `TransformImmutabilityTests` mutate inputs and returned arrays and verify all prior state/bounds/serialized bytes and other occurrences remain unchanged. No legacy Geometry or Raw-viewer modification now; see ADR-019.

## 8. Provenance model

Production normalization requires valid nonempty DrawingId/RevisionId GUIDs and SHA-256 FileHash. Raw permits general/absent provenance: refusal belongs in Normalize, not a Raw schema change. New fixtures use fixed valid GUIDs; old PHASE 03 contexts/goldens remain untouched.

Document registry stores source scope once. SourceRecordRef identifies record domain/kind and stable index/range. SourceFactRef adds field role, pair indices and physical lines where available. Resolve these to RawDrawingRecordView, SourceFact and lexical evidence. Do not invent byte offsets. Runtime artifact composition can retain one snapshot; serialized IR carries digest/locators, not the entire graph per object.

Every primitive/entity/occurrence resolves original/canonical handle, source record, definition, INSERT chain, parent and array row/column. Derived polyline segments reference source vertices/ordinal. Handleless records use actual locators; never fabricate handles.

A small context-aware Normalize source-access helper may read already retained lexical pairs inside record ranges for style facts missing from typed payloads. Honor subclass/application/control contexts and malformed/duplicate facts; do not reread bytes or relex. BLOCK_RECORD code 70 is units despite the generic Raw fact name Flags: interpret by record role without modifying Raw.

## 9. Unit normalization

| INSUNITS | Unit | mm factor |
| --- | --- | --- |
| 1 | inch | 25.4 |
| 2 | foot | 304.8 |
| 4 | millimeter | 1 |
| 5 | centimeter | 10 |
| 6 | meter | 1000 |

Proposed initial supported set is exactly this table. Controlled code 4 preserves numeric lengths. Unitless 0/absent -> NeedsExplicitUnits and no document unless an explicit supported-unit decision with reason/ID is supplied. Preserve original absence/value. Malformed/conflicting duplicate/unsupported codes refuse explicitly. Contradicting valid declared units with an override is rejected initially. No geometry-size or filename inference.

Convert positions, bases, insertion/elevation, radii, widths and array spacing once. Scale factors/normals are dimensionless. Lineweight 370 uses hundredths-mm independently of drawing-unit scale.

Initially support absent/0 or matching BLOCK_RECORD units using stored coordinates and INSERT scale. Contradictory explicit block units refuse the occurrence pending independent fixtures. Do not replay insertion-time scaling from unavailable CAD profile defaults or apply unit ratios twice.

Sources: [header variables](https://help.autodesk.com/cloudhelp/2024/ENU/AutoCAD-DXF/files/GUID-A85E8E67-27CD-4C59-BE61-4DC9FADBE74A.htm), [BLOCK_RECORD](https://help.autodesk.com/cloudhelp/2023/ENU/AutoCAD-DXF/files/GUID-A1FD1934-7EF5-4D35-A4B0-F8AE54A9A20A.htm), [insertion unit settings](https://help.autodesk.com/cloudhelp/2025/ENG/AutoCAD-Core/files/GUID-6C46049D-8636-442D-8BAC-CF4FD515FDC0.htm).

## 10. Angle normalization

Use a field-role conversion table. ARC start/end, INSERT rotation and TEXT rotation degrees become radians with original fact refs. Normalize rotations/start to [0,2π); ARC stores positive CCW sweep. Zero/ambiguous sweep is invalid/unsupported, not a fabricated circle. Bulge sweep remains signed. Normalize -0 only in normalized output.

Do not convert every group 50 as degrees. MTEXT rotation is documented in radians, and context-dependent group 50 also serves other data. Initially use verified WCS direction facts; support rotation only with unambiguous context/precedence and fixtures, otherwise retain unsupported orientation. POINT display/UCS angle does not rotate WCS position. Other angular facts need an explicit table entry/test. Distinguish absent/defaulted from explicit source values.

Source: [MTEXT codes](https://help.autodesk.com/cloudhelp/2020/ENU/AutoCAD-DXF/files/GUID-5E5DB93B-F8D3-4433-ADF7-E92E250D2BAB.htm). Do not conflate file-format units with object-API units.

Initial angular role table for currently mapped facts:

| Role | Normalized treatment |
| --- | --- |
| RawArc.StartAngleDegrees / EndAngleDegrees | Convert degrees with π/180; wrapped start plus positive CCW sweep; retain both source facts |
| RawInsert.RotationDegrees | Degrees -> radians; absent means documented zero rotation, marked Defaulted |
| RawText.RotationDegrees, including ATTRIB/ATTDEF text payload | Degrees -> radians; documented absent zero; preserve justification/anchor facts without inventing text-layout geometry |
| RawDimension.RotationDegrees in supported linear/rotated subtype | Degrees -> radians; preserve absent as absent unless a subtype-specific default is established; no angular-dimension reconstruction |
| RawPoint.AngleDegrees | Convert to optional radian display/UCS metadata only; never use to rotate point position |
| STYLE ObliqueAngle | Degrees -> signed radian metadata; no text shearing/font layout implementation |
| RawMText.Direction | WCS vector; not a degree field. Contextually ambiguous rotation/column values remain unsupported rather than globally converted |

This table does not authorize extracting new parser fields. Other retained angular pairs remain explicit source evidence until a reviewed contextual mapping is added. See [numerical group-code units](https://help.autodesk.com/cloudhelp/2024/ENU/AutoCAD-DXF/files/GUID-3F0380A5-1C15-464D-BC66-2C5F094BCFB9.htm) and [linear/rotated dimension roles](https://help.autodesk.com/cloudhelp/2018/ENU/AutoCAD-DXF/files/GUID-F0004556-493C-48D5-8619-61D6ADF05C04.htm); MTEXT's documented exception takes precedence over a blanket code-range assumption.

## 11. OCS/WCS design

DXF-specific basis logic lives in Normalize geometry helpers, not parsers. Reuse existing geometry, adding only missing tested vector/cross/dot helpers.

Normalize finite nonzero extrusion n by first rescaling with its largest absolute component to avoid overflow/underflow. For unit N, Arbitrary Axis Algorithm:

```text
if abs(N.x) < 1/64 and abs(N.y) < 1/64:
    Ax = normalize(WorldY × N)
else:
    Ax = normalize(WorldZ × N)
Ay = N × Ax
A = [ Ax  Ay  N ]              # columns
pWcs = A * pOcs                # homogeneous point w=1
vWcs = A * vOcs                # vector w=0; no translation
```

1/64 is a specified algorithm constant, not a chosen tolerance. Wholly absent normal defaults +Z with rule evidence. Explicit zero/nonfinite or initially partially specified normal is invalid unless a documented per-field policy is approved/tested. Tiny finite nonzero normal is not automatically zero.

Specification reconfirmation for ADR-019: normalize Ay after the cross product as well; the display equation above is exact arithmetic with already orthonormal Ax/N, not permission to omit numeric normalization. The strict branch and basis orientation remain unchanged.

Apply frames per field: LINE/POINT positions WCS; LWPOLYLINE x/y/elevation, circle/arc/SOLID/TEXT and INSERT insertion use documented OCS. MTEXT insertion/direction are WCS. DIMENSION mixes frames: field-role mapping is mandatory, never rotate all points together. Missing Z uses only documented defaults, with Defaulted evidence. Add elevation once before basis conversion; preserve nonplanar WCS data.

For currently supported linear/aligned DIMENSION facts, definition point 10/20/30 and extension points 13/23/33, 14/24/34 are WCS; text midpoint 11/21/31 is OCS. Preserve their distinct source roles after conversion. TEXT alignment points are OCS, but justification can make one anchor authoritative: retain both factual anchors/flags rather than claiming font-derived placement. Sources: [common dimension frames](https://help.autodesk.com/cloudhelp/2020/ENU/AutoCAD-DXF/files/GUID-EDD54EAC-A339-4EBA-AEA6-EC8066505E2B.htm), [TEXT alignment](https://help.autodesk.com/cloudhelp/2023/ENU/AutoCAD-DXF/files/GUID-62E5383D-8A14-47B4-BFC4-35824CAE8363.htm).

Independent goldens for OCS (2,3,4): N+Z -> (2,3,4); N-Z -> (-2,3,-4); N+Y -> (-2,4,3); N+X -> (4,2,3). Also test tilted normal, exact/neighboring AAA threshold, orthogonality/determinant+1, point/vector distinction, absent/zero/partial normals and extreme magnitudes.

Primary sources: [Arbitrary Axis Algorithm](https://help.autodesk.com/cloudhelp/2024/ENU/AutoCAD-DXF/files/GUID-E19E5B42-0CC7-4EBA-B29F-5E1D595149EE.htm), [OCS field rules](https://help.autodesk.com/cloudhelp/2024/ENU/AutoCAD-DXF/files/GUID-D99F1509-E4E4-47A3-8691-92EA07DC88F5.htm). Implementation must follow evidence and tests, not memory.

## 12. BLOCK / INSERT transform mathematics

Let q be normalized definition geometry, b BLOCK base, i INSERT OCS insertion, all mm; S dimensionless scale, Rz radian rotation and A INSERT OCS-to-parent-WCS basis. Proposed column-vector placement, applied right to left:

```text
L = A * T(i) * Rz(theta) * S(sx,sy,sz) * T(-b)
qParent = L * q
Mchild = Mparent * Lchild       # Lchild.Then(Mparent)
qModel = SourceToModel * Mchild * q
```

Compensate base before scale/rotation. A converts insertion translation too: do not preconvert i and apply A again. Entity OCS has already been applied once; units are not multiplied at every nesting level.

Hand goldens: +Z, b=(10,20,0), q=(12,23,0), S=(2,3,1), R90°, i=(100,200,0) -> (91,204,0); q=b -> i. With sx=-2 the first result is (91,196,0). Test unequal xyz scale, tilted normal, 3D base, mirror and noncommutative nesting.

Historical ACadSharp spike exposed approximately 700 source-unit error with a nonzero base. Use it as regression inspiration only, never as a production helper/dependency.

Stored-fact sources: [INSERT](https://help.autodesk.com/cloudhelp/2024/ENU/AutoCAD-DXF/files/GUID-28FA4CFB-9D5E-4880-9F11-36C97578252F.htm), [BLOCK](https://help.autodesk.com/cloudhelp/2024/ENU/AutoCAD-DXF/files/GUID-66D32572-005A-4E23-8B8B-8726E8C14302.htm). Matrix composition is DtoB's proposed derivation and requires independent numerical verification.

Architecture freeze reconfirmed actual Apply/Then implementation and these primary fields. ADR-019 records hand derivation q-b -> scaled -> rotated -> translated -> OCS basis, including a +Y-normal example. Proposed immutable CAD2 storage must not share Transform3's arrays during composition. Future tests remain mandatory; algebra review is not runtime verification.

## 13. Nested INSERT model

Keep shared definitions and explicit occurrences: actual INSERT/definition source refs, parent, outer-to-inner typed path, local/accumulated matrices. Leaves reference source primitive plus occurrence context; never anonymous flattening or per-instance mutation of definition geometry.

Traverse source order in depth-first preorder. Detect active-path block-reference cycles: an acyclic ownership graph does not prove acyclic INSERT-by-name references. Missing/ambiguous definition or cycle rejects that branch, records its path and preserves independent roots. Retain unused definitions; no fake empty target.

Honor model/paper-space facts. Expose model-space roots for future analysis; retain other scopes in catalog/ledger without silently importing them into model space. No layout/title classification.

## 14. MINSERT model

Use columns 70, rows 71, column spacing 44 and row spacing 45 source facts. **Proposed row-major, zero-based:** r outer, c inner, ordinal r*columns+c. This is deterministic DtoB ordering, not separate DXF source records.

```text
d(r,c) = (c*columnSpacingMm, r*rowSpacingMm, 0)
Lrc = A * T(i) * Rz(theta) * T(d(r,c)) * S * T(-b)
```

Offsets rotate with the array, but its own geometry scale does not multiply spacing. An ancestor transform still acts on the entire nested array. Autodesk documents rotated arrays and drawing-unit spacing; this exact matrix remains a proposed derivation requiring independently reviewed fixtures before 04E passes.

Golden 2x3: i=(100,200), R90°, column 10, row 20 gives row 0 (100,200),(100,210),(100,220); row 1 (80,200),(80,210),(80,220), including own scale(2,3). Test ancestor scale separately. Signed negative spacing has dedicated tests; zero spacing preserves distinct coincident occurrences. Counts<=0 invalid; checked products/budgets before allocation.

Cell paths retain the same real INSERT source plus r/c; no invented handles. Ordinary INSERT differs from MINSERT cell(0,0). Raw unsupported-expansion evidence stays unchanged. Array ATTRIB instancing is an explicit unsupported subfeature until factual placement is proven.

Source: [Autodesk MINSERT](https://help.autodesk.com/cloudhelp/2016/ENU/AutoCAD-MAC-Core/files/GUID-A780A2FA-4A2E-4574-950F-E788AB71F527.htm). No AutoCAD automation authorized.

**L-01 freeze:** a single session-wide TotalOccurrencesAllocated includes top-level/nested INSERT and every cell, including arrays within repeated/nested blocks. Never reset per definition/parent/entity/root/recursion or refund on rejected branches. Each materialized occurrence counts once; no synthetic array-container occurrence. Check/reserve before allocation; >100,000 terminates ResourceLimitExceeded with no document. Local 10,000-cell compliance does not bypass global count. See section 17 and ADR-020.

## 15. Effective layer/style resolution

Retain source and resolved properties with **Known / Inherited / Defaulted / Unsupported / Absent**, rule ID and compact inheritance/evidence chain. Required unresolved properties remain explicit/partial; no fabricated confidence or visual defaults.

- Definition layer 0 inherits containing INSERT effective layer; nonzero layer stays named; root 0 means actual layer 0.
- Explicit wins; ByLayer uses effective layer; ByBlock follows containing INSERT recursively. Root ByBlock without resolvable context is unsupported/unresolved, not invented ACI 7.
- ACI and RGB remain tagged values; 420 overrides 62. Absent color follows documented ByLayer default with rule evidence, not an explicit invented source color.
- Absent linetype follows ByLayer, never blind Continuous. Missing/ambiguous catalog entries stay explicit. Lookup is ordinal case-insensitive with original spelling/collision diagnostics; no culture folding or silent trimming.
- Preserve visibility/off/frozen without deleting geometry. Lineweight specials need explicit policy; literal 370 is hundredths-mm. Do not synthesize complex linetype/font/color-book fidelity.

Resolve retained source pairs in Normalize; do not expand PHASE 03 parser payloads for convenience.

Sources: [common entity defaults](https://help.autodesk.com/cloudhelp/2015/ENU/AutoCAD-DXF/files/GUID-3610039E-27D1-4E23-B6D3-7E60B22BB5BD.htm), [block properties](https://help.autodesk.com/cloudhelp/2026/ENU/AutoCAD-LT-MAC/files/GUID-25E9F20C-D146-426C-8815-37DF48D2D33F.htm), [true-color precedence](https://help.autodesk.com/cloudhelp/2026/ENU/AutoCAD-AutoLISP-Reference/files/GUID-C7D27797-247E-49B9-937C-0D8C58F4C832.htm). Last link is evidence, not an AutoLISP dependency.

## 16. Normalized geometry strategy

Reuse Point3/Segment3/GeometryTolerance/Transform3. Add CAD-specific plane/source/occurrence payloads, not another algebra engine.

| Source | Proposed normalized facts / limitations |
| --- | --- |
| POINT/LINE | Actual WCS mm, including nonplanar 3D; do not rotate WCS endpoints by extrusion |
| CIRCLE/ARC | Plane origin/orthonormal axes, radius/start/sweep; affine occurrence retained |
| LWPOLYLINE | Ordered line/arc segments, width/closure/source vertices; OCS/elevation once |
| SOLID | Planar triangle/quad; DXF boundary 1,2,4,3, repeated final corner as triangle; not BIM solid |
| TEXT/MTEXT | Content/anchor/known height/supported orientation; no fonts/SHX extents |
| DIMENSION | Mixed-frame points/style/text/block refs and optional source measurement; no invented measurement/anonymous block occurrence |
| ATTRIB | Source association/content/placement; do not double-apply parent transform or presume template instancing; spatial support fixture-gated |

Bulge b: b=0 is straight; signed sweep 4*atan(b); center midpoint + left normal * chordLength*(1/b-b)/4; radius chordLength*(abs(b)+1/abs(b))/4, using stable branches and finite checks. Test negative, semicircle, major and tiny bulges. Exact arcs, not tessellated substitutes. Closure creates last-to-first once. Repeated/zero-length geometry has explicit degenerate disposition; no silent deletion. Widths stay lengths, not stroke polygons.

Under nonuniform/mirrored placement, keep exact local circle/arc plus affine matrix. World queries expose transformed parametric axes, not a fake circular radius; no ellipse fitting required. Preserve finite 3D lines. Unsupported nonplanar SOLID/thickness-derived volume stays explicit; rejected nonfinite source never becomes zero.

Bounds come from supported geometry/affine extrema, not HEADER sentinel extents. Annotation extents remain unknown/partial; anchor bounds are separately labeled. PHASE 05 gets mm geometry, world placement, style, text anchors/content and provenance without recognition. Linear queries suffice; no R-tree/topology engine.

**M-02 freeze — exact affine bounds:** for 2D linear matrix [[a,b],[c,d]], transformed circle center C/radius R gives Xhalf=R*hypot(a,b), Yhalf=R*hypot(c,d), bounds C±half. General tilted 3D plane U,V and accumulated linear B give u=B*U, v=B*V, p(t)=C+R*(u*cos(t)+v*sin(t)); each full-circle coordinate j has half extent R*hypot(u[j],v[j]). ARC bounds evaluate endpoints plus derivative-zero parameters atan2(v[j],u[j])+k*π lying in the original signed sweep modulo2π. Constant coordinates need endpoints only. Preserve source parameter under mirror; no whole-circle shortcut, sampling or tessellation as geometry truth.

`NonuniformCurveBoundsTests`: radius2, B=R45°*S(2,3), C=(10,-4,3) yields both XY half extents sqrt26, Z=3. Mirroring sx=-2 retains full-circle bounds, reversing winding. Arc [0,π/2] under positive scales yields X=[10-3*sqrt2,10+2*sqrt2], Y=[-4+2*sqrt2,-4+sqrt26]; interior Y extremum at atan(3/2). Include mirrored/wrapped arcs, tilted planes and shear. ADR-019 provides derivation; expected values must not be generated by the implementation under test.

Source: [DXF bulge definition](https://help.autodesk.com/cloudhelp/2024/ENU/AutoCAD-DXF/files/GUID-0741E831-599E-4CBF-91E1-8ADBCFD6556D.htm). This does not add legacy POLYLINE parsing beyond current payload support.

## 17. Error/diagnostic model

Separate normalized dispositions from Raw semantic status: Normalized, PartiallyNormalized, UnsupportedNormalization, InvalidNormalization; nonspatial records explicitly NotApplicable. Result distinguishes complete/partial/refused/cancelled/resource-limited. Diagnostic counts alone do not prove coverage.

| Failure | Proposed scope |
| --- | --- |
| One bad primitive/subfeature | Retain source/ledger, withhold only bad geometry; document Partial |
| Bad INSERT scale/normal/target/cycle | Reject branch, preserve independent roots; Partial |
| Unresolved style | Preserve valid geometry plus explicit unresolved property; Partial |
| Invalid authority/reference precondition, document units/schema/policy | No document; explicit refusal |
| Cancellation/global budget exceeded | No published document; never truncated-success |

Diagnostics include stable code/severity/stage/source locator/field/path/invariant arguments. No canonical localized text/time/path. Upstream diagnostics stay separate.

Proposed injectable development limits: depth 32; cells/INSERT 10,000; total occurrences 100,000; derived primitives 250,000; diagnostics 5,000 including reserved terminal slot. These are resource policies, not tolerance/performance claims. Validate options/check products before allocation; exact-boundary/one-over tests use smaller profiles. Do not change Raw reader limits.

TotalOccurrencesAllocated is one monotonically increasing session-wide admission counter, never scoped/reset/refunded per subtree. Count root/nested ordinary INSERT and each array cell before allocation, not source templates; cells themselves carry the occurrence, without a synthetic container. Exactly 100,000 is allowed if other limits permit; any attempted next occurrence refuses the entire publication deterministically. Derived geometry/leaf-entry count and diagnostic budget are also session-wide; count occurrence leaves rather than only shared definition geometry. Root occurrence depth 1, maximum 32 steps. Cancellation is checked within source/entity/cell loops, not only stage boundaries. `MinsertResourceExplosionTests` covers individually legal arrays whose nested/multiple combination exceeds global budget, exact/one-over and no-reset behavior.

As detailed in ADR-020, any resource-cap exhaustion, including depth/per-INSERT cell cap, returns ResourceLimitExceeded with no document; invalid zero/negative source counts are separate branch-invalid facts. Count materialized shared/root primitives and occurrence leaf entries toward derived geometry, not only one category of output allocation.

## 18. Deterministic identity/order

ADR-020 defines **new CAD-v2 policy**, preserving Core.CadIdentity and legacy BIM/CAD IDs. Scope: DrawingId+RevisionId+FileHash+Raw graph canonical handle; retain OriginalHandle. Use Raw's canonicalization, not v1's uppercase-only helper. Handleless records use domain-qualified source locator. IDs are revision-scoped; no reconciliation across revisions promised.

Proposed domain-separated SHA-256 of versioned length-framed canonical tuples, e.g. `cad2:<digest>`. Separate source/definition/INSERT/occurrence/leaf/segment domains. No ambiguous concatenation, random GUID, machine path/time, dictionary order or geometry hash.

Typed path steps contain real INSERT source plus optional row/column. Ordinary INSERT differs from cell(0,0). Full outer-to-inner path and derived segment ordinal distinguish leaves while definitions remain shared.

Order catalogs/source entities by stable source record order (pair/index and domain), documented ordinal name/handle ties; occurrences DFS/source order and row-major cells; primitives source/segment order; diagnostics locator/path/stage/code/field. Sort map keys ordinally. Meaningful source order remains fixed even when lookup insertion order changes.

CAD-v2 serialization uses fixed property order, invariant finite numbers, normalized zero, UTF-8 without BOM and LF. No local paths/time. Require byte-identical output across culture/process/CWD/lookup construction order.

## 19. Numerical tolerance policy

Separate source validity, normalization numerical comparisons and future recognition tolerance. The last does not enter this phase.

Required explicit NormalizationPolicy reuses GeometryTolerance length-mm/angle-radians and adds documented dimensionless matrix/conditioning budgets. Caller provides validated values; no guessed hidden product defaults. Test profiles label values/units. Policy/profile version accompanies output.

- Source checks use finite values/legal counts/exact presence; epsilon cannot repair invalid counts or missing facts.
- Exact zero scale invalid; negative scale valid reflection. Tiny nonzero scale is not snapped; inverse-dependent operations use central conditioning policy and diagnostics.
- Normalize normals by rescaling; finite nonzero raw magnitude is not alone a failure. Exact zero is invalid; AAA threshold is specified.
- Geometry degeneracy uses declared mm/angle policy; no snapping/joining/quantization cleanup.
- Check matrix/position products for finiteness. Isolate operation overflow where possible; invalid global options refuse output.
- Numeric golden comparison has explicit test tolerance; canonical serialization compares exact bytes. Do not round away incorrect math to manufacture determinism.

## 20. Proposed subphases

Implementation requires owner approval and ADR acceptance. Each subphase produces evidence/status and independent review; do not advance automatically across an owner gate.

**M-03 frozen prerequisite table** (Accepted is a future requirement, not current status): 04A -> ADR-018; 04B -> ADR-018 + ADR-019; 04C/04D -> ADR-019; 04E -> ADR-019 + ADR-020; 04F -> ADR-018 + ADR-020 for occurrence-context inheritance; 04G -> all relevant 018/019/020; 04H -> verify entire accepted architecture. All three are authored now as Proposed for complete independent review. If 04A reaches coordinate/identity details governed by still-Proposed 019/020, defer those details or wait for acceptance; no stage may implement a still-Proposed decision.

### 04A — Contracts and architecture

- **Purpose:** establish additive v2 and pure normalization boundary.
- **Inputs:** approved plan, Accepted ADR-018, existing contracts; coordinate/identity details additionally wait for their governing ADR acceptance.
- **Outputs:** V2 DTOs, registry/ledger, strict CAD codec boundary, interface/options/result and dependency scaffolding.
- **Owned contracts:** Cad.V2, CadIrJson, ICadNormalizer; CAD-only schema 2.
- **Excluded scope:** working transforms, fake successful normalizer, Raw/Dxf/BIM/Analysis API edits.
- **Tests:** CadIrJsonV2Tests (independent version2 roundtrip/deterministic bytes and strict rejection), provenance, legacy v1/BIM regression, dependency allowlist; TransformImmutabilityTests and typed-path detail tests only once governing 019/020 are Accepted.
- **Gate criteria:** represent OCS/array paths/absent measurement honestly; preserve v1 and dependency direction.
- **ADR requirement:** Accepted ADR-018 before 04A; do not implement coordinate/identity details governed by still-Proposed 019/020.

### 04B — Units/angles/numeric policy

- **Purpose:** explicit dimensional facts and centralized numeric rules.
- **Inputs:** Raw header/typed facts, options, unit/angle-role table.
- **Outputs:** mm/radian facts, unit decisions/default refs and refusals.
- **Owned contracts:** UnitResolution, NormalizationPolicy, angular role rules.
- **Excluded scope:** placement, inference, Revit units.
- **Tests:** five units, code 4, absent/0/malformed/duplicate/unsupported/override, wrapping/-0, MTEXT radians/context, lineweight distinction, invalid policy.
- **Gate criteria:** no implicit units, once-only conversion, traceable defaults, finite results.
- **ADR requirement:** Accepted ADR-018 + ADR-019 before 04B; changed default behavior requires review.

### 04C — OCS/WCS

- **Purpose:** specification-derived reusable basis/field placement.
- **Inputs:** unit-normalized facts, normals and frame-role map.
- **Outputs:** bases, point/vector transforms, entity-plane coordinates.
- **Owned contracts:** Normalize OCS helpers; minimal missing Geometry vector helpers.
- **Excluded scope:** parser edits, global extrusion, rendering.
- **Tests:** axis/tilted normals, threshold neighbors, extremes, missing/zero/partial normal, elevation once, WCS LINE, mixed frames, point/vector.
- **Gate criteria:** independent goldens, orthonormal RH basis, no Raw mutation/repeated units.
- **ADR requirement:** Accepted ADR-019 before 04C; preserve ADR-006/v1 behavior.

### 04D — BLOCK/INSERT transform engine

- **Purpose:** correct base/scale/rotation/OCS local placement.
- **Inputs:** normalized base and INSERT facts/basis.
- **Outputs:** local matrices and operation diagnostics.
- **Owned contracts:** pure placement builder using Transform3.
- **Excluded scope:** recursive/array expansion, external helper.
- **Tests:** section 12/historical nonzero-base goldens, unequal/negative/zero scales, tilted/3D base, overflow, Then order.
- **Gate criteria:** base maps to insertion; independent noncommutative goldens; unit factor once.
- **ADR requirement:** Accepted ADR-019 before 04D; formula departures return to review.

### 04E — Nested INSERT/MINSERT

- **Purpose:** bounded deterministic trees and full lineage.
- **Inputs:** ordered definitions/INSERTs, local matrices, identity/budgets.
- **Outputs:** nodes/cells/accumulated matrices/leaf paths and branch failures.
- **Owned contracts:** occurrence/typed path, v2 identity, budgets/active-path cycles.
- **Excluded scope:** anonymous flattening/fake handles/Raw status change/array ATTRIB instancing.
- **Tests:** nested repeats/cycles/missing target, 2x3 rotated/nonuniform golden, signed/zero spacing, ancestor scale, cell0 vs ordinary IDs, count overflow/depth/budget edges; MinsertResourceExplosionTests verifies one global non-resetting counter and refusal without publication.
- **Gate criteria:** independently proven spacing formula/order; real source paths; independent roots survive; no truncated success.
- **ADR requirement:** Accepted ADR-019 + ADR-020 before 04E; no silent movement of spacing across scale.

### 04F — Styles/provenance

- **Purpose:** factual effective properties without invented fidelity.
- **Inputs:** source catalogs/style pairs, occurrence tree/registry.
- **Outputs:** effective properties/states and evidence chains.
- **Owned contracts:** resolution result and retained-pair contextual accessor.
- **Excluded scope:** fonts/pixels/parser expansion/semantic confidence.
- **Tests:** layer 0/nonzero, nested ByLayer/ByBlock/root unresolved, RGB precedence, absent/malformed/duplicate/missing symbols, culture/context collisions, compact refs.
- **Gate criteria:** trace every default/resolution; visible unsupported facts; no blind Continuous/ACI 7.
- **ADR requirement:** Accepted ADR-018 and ADR-020 for occurrence-context inheritance before 04F; no trivial extra ADR.

### 04G — Geometry/document assembly

- **Purpose:** useful normalized primitives and honest coverage.
- **Inputs:** normalized facts/planes/occurrences/styles/ledger.
- **Outputs:** definition primitives, model-space refs, annotation facts, bounds and complete/partial document.
- **Owned contracts:** primitive payloads/assembler/simple queries/dispositions.
- **Excluded scope:** recognition/topology/BIM/font bounds/fake measurements/new viewer.
- **Tests:** all planned primitives, bulge/closure/degenerate/nonplanar cases, affine curves, NonuniformCurveBoundsTests including rotated 2x3/mirror/sweep extrema, missing measurement, anchors/ATTRIB refusal, sentinel bounds, good geometry with bad records, complete ledger.
- **Gate criteria:** source/occurrence trace, no silent loss, explicit subfeature failures and truthful controlled-data status.
- **ADR requirement:** All relevant ADR-018/019/020 Accepted before 04G; major changes return to review.

### 04H — Deterministic QA/comparison

- **Purpose:** reproducible normalization and legacy evidence.
- **Inputs:** prior outputs, reviewed fixtures and frozen golden policy.
- **Outputs:** canonical golden, numerical source-linked Raw-vs-normalized tables, build/QA evidence and report.
- **Owned contracts:** QA manifest coverage evolution/golden format; no new UI required.
- **Excluded scope:** second viewer, changed 03H, performance promises, recognition.
- **Tests:** process/culture/CWD/order equality, identity, full 03->04, unchanged Raw bytes/facts, no I/O/dependency leaks, PHASE 00–03 suite/03G/03H, cancellation/resources/roundtrip.
- **Gate criteria:** section 26 evidenced, measured totals, no unresolved blocker/high and reviewed limitations.
- **ADR requirement:** verify entire Accepted ADR-018/019/020 architecture at 04H; departures stop gate.

## 21. Fixture strategy

Preserve all 86 legacy controlled fixtures/hashes and 03G manifest/goldens. Add focused sources under datasets/controlled/phase04 with a separate manifest; obey committed .gitattributes byte policy. Compare working/index/committed/clean-checkout bytes. Never normalize legacy line endings or regenerate PHASE 03 evidence.

Families: units/refusals; axis/tilted/threshold/invalid normals; base/rotation/nonuniform/mirror; nested/cycles; MINSERT/budgets; style inheritance/context; positive/negative/major/tiny bulges; closure/degenerate/nonplanar; mixed annotation frames/absent measurement; block units. Hand-audit source pair/line locators; fixed valid GUID provenance.

**Existing QA trap:** ExtractionQaCampaignTests.ManifestCoversEveryReviewedDiskFixtureAndHashesAreUniqueByPath enumerates all controlled DXF/bin against the old 86 manifest. New fixtures require a narrow test evolution: keep exact legacy 86-set assertion; validate new manifest independently; require disjoint union equals all controlled files, with omission/duplicate negative tests. Do not weaken old cases or run its generator over new data to rewrite old manifest.

Expected coordinates derive from reviewed hand calculations, never the implementation under test. Freeze new golden/hash only after those tests pass; do not invent expected counts/hashes now. Historical SDK spike is not a dependency.

## 22. Test/verification strategy

Layers: pure math goldens; strict contracts/codec; source/default/identity; record negatives; full Raw->IR; subprocess determinism; no-I/O/dependency checks; unchanged PHASE 00–03 regression.

Prove architecture rather than fixed timing: construct snapshot before the normalization boundary, make source files unavailable, verify pure transformation still works. Compare Raw facts/serialization hashes before/after repeats. Inject cancellation and smaller budgets. Shuffle lookup construction, not meaningful source order.

Use en-US/ko-KR/tr-TR, different CWDs/fresh processes and exact byte/ID/diagnostic equality. Verify no machine paths/time/randomness. Account for every source record including unsupported facts.

Implementation gates run Core-only Release, full Solution Release and complete suite; final gate also 03G/03H/source-byte audit. Preserve 773 released cases; measure actual new totals. Reports identify configuration/commands/source tree or diff/artifacts. Protected-source audit updates allow only approved Cad-v2/Normalize/minimal Geometry/QA coverage changes; keep Raw/Dxf and old bytes protected.

Planning-only work does not execute or claim new product build/test PASS.

Viewer evaluation: optional normalized mode could aid visual QA but expands WPF scope and cannot prove math. **Default proposal: no second viewer, unchanged 03H**. Use JSON plus numerical/source-linked comparison artifacts. Any later UI mode needs explicit owner scope approval and independent tests.

## 23. ADR strategy

Architecture-freeze update: authored the following as **Proposed**, not Accepted. Earlier planning report remains historical evidence; no ADR acceptance or implementation authorization follows from plan PASS.

| Proposed ADR | Decisions/alternatives |
| --- | --- |
| [ADR-018-CAD-IR-Normalization-Boundary.md](../adr/ADR-018-CAD-IR-Normalization-Boundary.md) | Additive CAD v2, CAD-owned version/codec excluding Core.Version, exact package graph, partial/provenance boundary and bounded v1 migration. |
| [ADR-019-Coordinate-Normalization-Transforms.md](../adr/ADR-019-Coordinate-Normalization-Transforms.md) | Preserve canonical conventions; field frames/AAA/unit/angle mapping, transform composition, private defensive matrix ownership, analytic affine curve bounds and central numeric policy. |
| [ADR-020-Occurrence-Identity-and-Expansion.md](../adr/ADR-020-Occurrence-Identity-and-Expansion.md) | Typed revision-scoped cells/occurrences, original handles/framed IDs, array composition, deterministic order, cycles and session-global budget. |

Accept before affected implementation. Preserve accepted ADRs; document changed applicability to new v2 rather than rewriting history. ADR-008 strict-schema principles remain. No ADR for trivial helper naming.

ADR-018 acceptance gates 04A; ADR-019 gates 04B/C/D; ADR-020 gates 04E (with 019), plus the exact later-stage requirements in section 20. All three remain Proposed until independent review/acceptance. **Current next gate is independent ADR review, not 04A implementation.**

Older task 04 subdivisions/deferrals are historical. Its additive notice points here; future accepted ADRs finalize contract changes. Nothing in this proposal overrides accepted contracts by itself.

## 24. Risks

| Risk | Gate/response |
| --- | --- |
| MINSERT spacing/scale/OCS | Independent goldens before 04E; contradiction stops architecture review |
| Mixed field frames/defaults | Primary evidence/role table before support; ambiguity explicit |
| Block unit/user-profile conversion | Refuse contradictory context, never replay unknown defaults |
| Raw unsupported/style facts | Read retained contexts; no parser change or blanket mapping-complete requirement |
| General/absent Raw provenance | New-boundary refusal; preserve old Raw goldens |
| Shared version/mutable transport | CAD-only v2 codec, owned copies and validation |
| Expansion growth | Pre-allocation budgets/active-path cycles; no truncation |
| Extreme/degenerate numbers | Central policy, robust basis and finite checks |
| Annotation/affine fidelity | Exact local curve+matrix; explicit unsupported subfeatures |
| Fixture manifest/bytes | Legacy set plus union coverage and clean-checkout audit |
| Analysis currently v1 | Explicit PHASE 05 adoption, no false automatic compatibility |

Contradictory specification/fixture evidence returns to ADR review. Do not compensate with vendor parsers, source edits or fake rendering.

## 25. Explicit non-goals

No DWG SDK/ODA/ACadSharp/AutoCAD automation/private DLLs; no parser/entity extension, Raw/Dxf rewrite, LOW-01 relaxation or DWG/DXF reconciliation. No database/cloud/accounts/monitor/asset management/broad refactor.

No grid/wall/column/door/window/floor/room recognition; no plan/section/elevation/title classification or story inference/AI/fuzzy cleanup. No BIM schema/family matching/Revit generation/dependency. No topology cleanup/dedup/snapping/font fidelity/second viewer/fabricated confidence. PHASE 05+ is outside authorization.

## 26. Measurable final acceptance

Future gates, **not achieved by this plan**. Final docs/status/PHASE_04_REPORT.md maps each to evidence and honest PASS / PASS_WITH_KNOWN_LIMITATIONS / REVIEW_REQUIRED / BLOCKED.

1. Accepted ADR-018/019/020 and reviewed 04A–H; no unauthorized schema/dependency change.
2. Raw/Dxf production and 86 legacy source hashes unchanged; snapshot facts/evidence unchanged before/after. LOW-01 regression passes.
3. Unit table/code 4 mm and explicit unknown/invalid decisions pass; field-specific radians and MTEXT exception verified.
4. OCS independent goldens/threshold/default/invalid cases pass; WCS fields unchanged; elevation/vector semantics proven.
5. Nonzero-base/nonuniform/mirrored/tilted INSERT tests pass; units applied once.
6. Real nested identity chain, branch cycles/errors and deterministic row-major MINSERT grid/IDs/budgets pass.
7. Effective Layer/Color/Linetype and default/unresolved states pass without fabricated properties.
8. Every normalized entity/primitive/occurrence resolves DrawingId/RevisionId/FileHash, actual handle where available, Raw record/fact/pair/line and occurrence chain. Every Raw record has ledger disposition.
9. Primitive/bulge/closure/degeneracy/annotation/affine cases pass; partial status and unknown bounds truthful.
10. Strict v2 golden roundtrip and exact process/culture/CWD/lookup-order equality pass; no random IDs/path/time.
11. Cancellation/resources refuse publication; no normalization I/O/vendor/WPF/BIM/Revit or recognition.
12. Core/full Release and complete tests pass with actual totals, all legacy cases retained; 03G/03H/byte audit and complete manifest union pass.
13. Self-reviewed diff, evidence/limitations and independent Antigravity final review/owner decision recorded. No completion claim from planning alone.

Review-derived additions to future acceptance: CadIrJsonV2Tests must roundtrip major2 without the legacy version1 gate while legacy behavior remains; TransformImmutabilityTests prove no external matrix mutation affects existing state; NonuniformCurveBoundsTests prove analytic affine sweep bounds; MinsertResourceExplosionTests proves session-global limits. Actual implementation/test evidence is still pending.

**Current stop:** independent ADR review of three Proposed ADRs and gate report. No 04A/production/tests implementation, commit or push. The previously prohibited branch creation was separately authorized and completed by the architecture-freeze owner direction; initial planning history above is retained explicitly as history.

## PHASE04H local whole-phase QA checkpoint — 2026-10-19

The earlier planning/ADR/subphase stop checkpoints above are preserved as history.
Owner subsequently authorized each accepted04A–G implementation and now04H only.
Independent04A–G reviews pass;04H adds QA/data/docs only, with no production change.

119 physical inputs (unchanged86 +33 minimal new),31 normalized goldens,
independent numerical/source oracles, strict triple roundtrip, three cultures,
two fresh processes/distinct CWDs and final unfiltered reproduction pass.
All accepted04A–G digests remain exact. Fresh Core/full Solution Release Rebuild
and1402/1402 tests pass;all1315 baseline IDs/names remain,87 new QA cases.
Raw/Dxf and protected application/domain boundaries unchanged. Official document
remains Partial;Unsupported52 and LOW01 remain explicit.

See docs/status/PHASE_04H_REPORT.md, docs/status/PHASE_04_FINAL_GATE.md and
machine-readable release/payload inventories under docs/status/phase04/.
Local verdict PHASE_04_FINAL_PASS_WITH_KNOWN_LIMITATIONS; independent whole-phase
review PENDING. No PHASE05, staging, commit, push, PR or tag. STOP for Antigravity.
