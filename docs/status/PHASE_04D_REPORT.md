# PHASE 04D — BLOCK / INSERT Local Transform Engine

Date: 2026-10-16

Local status: **PHASE_04D_PASS_WITH_KNOWN_LIMITATIONS**. Ready for independent Antigravity implementation review. No independent PASS or whole-PHASE04 completion is claimed.

## 1. Scope

Implemented one definition-to-immediate-parent affine placement, sourced from one BLOCK and one ordinary INSERT. Consumes04B units/rotation and04C anchor/frame, normalizes the actual block base, interprets dimensionless scale, produces immutable CadTransform, and provides distinct finite point/vector application.

Read the owner request, new independent04C review, actual Raw/reader/matrix contracts and relevant Accepted ADR-018/019/020, prior reviews/reports and implementation-plan sections.04C independent verdict is PHASE_04C_PASS with zero findings. Owner authorization covers04D only. No previously accepted source/technical ADR body is changed; no new schema/identity convention or ADR is required.

## 2. Baseline

Branch `phase/04-cad-normalization`; HEAD `76ff6e0d8ccac797ba60d8c5657922c796a0e8c9` (`v0.4-phase03`). Prior04A/B/C and documentation work remain intentionally uncommitted/untracked.

Baseline **1052/1052 PASS** from [04C final TRX](../../artifacts/phase04/04c-20261016/final/tests.trx). Captured all Git-visible source/document hashes before04D in [before.json](../../artifacts/phase04/04d-20261016/before.json). Audits compare to this reviewed working tree, preserving earlier untracked implementation.

## 3. Files changed

Four new production files in `packages/DtoB.Cad.Normalize/Placement/`:

- `LocalPlacementContracts.cs`: owned source/name facts, base/scale/placement/application results.
- `CadBlockBaseNormalizer.cs`: definition base dimensional normalization, authority and unit checks.
- `CadInsertScaleNormalizer.cs`: per-axis defaults and exact-zero/nonfinite refusal.
- `CadInsertLocalTransform.cs`: single target lookup, profile/owner checks, composition and point/vector application.

Five test files in `tests/DtoB.Tests/`: `Cad04DPlacementFixture.cs`, `CadBlockBaseAndScaleTests.cs`, `CadInsertLocalTransformTests.cs`, `CadInsertResolutionAndProvenanceTests.cs`, `Cad04DArchitectureAndDeterminismTests.cs`.

Documentation: this report and additive current-checkpoint notices in the implementation plan and `tasks/PHASE_04.md`. No project, solution, Geometry, Raw/Dxf or existing04A/B/C code change was required. Evidence/scripts remain under ignored artifacts.

## 4. Actual BLOCK / INSERT Raw mapping

| Source | Actual representation consumed |
| --- | --- |
| BLOCK | RawBlockBegin.Name (code2), AlternateName (retained; not a target fallback), BasePoint.X/Y/Z (10/20/30), Flags (70), XrefPath |
| Definition range | RawBlockDefinition.BeginRecordIndex / EndRecordIndex / Disposition; begin row identity supplies handle/locator |
| BLOCK_RECORD | Structural `TABLES/BLOCK_RECORD` fields Name and Flags; Flags is code70 insertion units, consumed through04B |
| INSERT target | RawInsert.TargetName (2) |
| Insertion / frame | RawInsert.Insertion and Extrusion through RawCoordinateAccess / CadCoordinateNormalizer |
| Rotation | RawInsert.RotationDegrees through RawScalarAccess / CadAngleNormalizer |
| Scale | RawInsert.Scale.X/Y/Z, codes41/42/43, not coordinate offsets |
| Array profile | Columns/Rows and OccurrenceExpansionUnsupported only to refuse arrays/invalid counts |
| Array spacing | ColumnSpacing / RowSpacing remain untouched and are not read for placement |

Facts preserve original pair/line evidence. All required source facts are available. No new parser mapping or Raw default was invented. Children are not consumed.

## 5. Block base semantics

[Autodesk BLOCK](https://help.autodesk.com/cloudhelp/2024/ENU/AutoCAD-DXF/files/GUID-66D32572-005A-4E23-8B8B-8726E8C14302.htm) describes the base point and the definition's own WCS established from its creation UCS.04D treats b as a point in that definition frame, with source lengths converted once to mm by the resolved04B factor.

No INSERT OCS basis is applied to b separately. T(-b) subtracts it from the definition-coordinate q before INSERT scale/rotation/basis. This follows Accepted ADR-019; no centroid, origin inference, filename/name heuristic, recentering or assumed zero base.

All XYZ facts must be present/valid/finite. Missing/malformed/nonfinite base refuses; no missing-Z zero default. Truly malformed DXF numbers may be rejected before snapshot publication by the unchanged Raw pipeline. Tests cover both that refusal and direct boundary validation.

## 6. Scale / default policy

[INSERT codes41/42/43](https://help.autodesk.com/cloudhelp/2024/ENU/AutoCAD-DXF/files/GUID-28FA4CFB-9D5E-4880-9F11-36C97578252F.htm) are dimensionless and each defaults1 when genuinely absent. New deterministic rule IDs: `DXF.INSERT.Scale.X.DefaultOne`, `.Y.DefaultOne`, `.Z.DefaultOne`. Facts remain absent with empty pair evidence. Mixed explicit/default axes are supported.

Exact zero on any axis is InvalidInsertScale. Finite negative and tiny nonzero scales remain valid; no epsilon replacement or snapping. NaN/infinities/invalid facts refuse. Scaling is never multiplied by the source-length unit factor. The existing numeric policy is unchanged.

## 7. Rotation reuse

Create calls the existing04B role-specific rotation path exactly once. Composition consumes CadScalarResult.Value in canonical radians. No degree conversion, alternative angle wrapping or new absent rule is implemented in04D. Existing absent `DXF.INSERT.Rotation.DefaultZero` is reused directly.

Standard right-handed Rz applies positive CCW rotation around local +Z. Independent0/90/180/270-degree fixtures assert canonical0/π/2/π/3π/2 and rotated unit vectors. Trigonometric near-zero coefficients are preserved, not snapped.

## 8. OCS frame reuse

Create obtains InsertInsertionOcs through04C. Its PointMm is already WCS-oriented in the immediate parent frame, and its CadOcsFrame supplies Ax/Ay/N. No new AAA or normal normalization exists in Placement. Existing whole-normal+Z default and evidence are preserved through the anchor result.

For an INSERT inside a definition, the returned coordinates remain in that immediate definition's coordinate frame. They are not accumulated into model space.

## 9. Actual transform derivation from04C contracts

ADR-019 defines `L = A * T(i_ocs) * R * S * T(-b)`.04C returns `anchor = A*i_ocs`. For the origin-coincident linear basis A:

```text
A * T(i_ocs) * x = A*x + A*i_ocs
T(anchor) * A * x = A*x + anchor
therefore A*T(i_ocs) = T(anchor)*A

L = T(anchorWcs) * A * R * S * T(-b)
```

This is algebraically the same accepted local placement, using the actual04C contract. It does not redefine the coordinate convention or approximate an inverse to recover i_ocs. No extra A is applied to the already normalized anchor.

## 10. No-double-A proof

For +Y normal,04C's A maps `(x,y,z)` to `(-x,z,y)`. Source insertion `(100,200,0)` therefore gives anchor **(-100,0,200)**. The local transform maps b to that same anchor.

Independent fixture b=(10,20,0), q=(12,23,0), scale=(2,3,1), rotation90°: relative(2,3,0)→scaled(4,9,0)→rotated(-9,4,0)→add source insertion(91,204,0)→A gives **(-91,0,204)**. Tests detect missing/double A and wrong translation space.

## 11. Matrix convention

Existing Geometry.Transform3 stores row-major4×4 and applies column vectors. `inner.Then(outer)` computes `outer*inner` into fresh storage and validates finite affine coefficients. Existing CadTransform copies on publication/export and exposes only read-only Values. Neither contract was modified.

04D uses private working Transform3 instances and publishes CadTransform.FromTransform3. It does not add CAD-specific behavior to Geometry or a placement method to CadTransform.

## 12. Local transform equation

Actual builder order:

```text
subtractBase.Then(rotateScale).Then(basis).Then(translateAnchor)
```

This yields `T(anchor)*A*(R*S)*T(-b)`. `Transform3.Placement(0,theta,scale)` already produces R*S. Basis rows are populated from the existing Ax/Ay/N columns. Translation contains only the04C anchor.

No parent matrix parameter, recursive call or occurrence output exists.

## 13. Transform application order

q→subtract base→dimensionless scale→local Rz→OCS basis→add canonical parent anchor. Insertion translation is not scaled or rotated by Rz. Its necessary basis orientation has already happened in04C.

Independent fixtures distinguish R*S from S*R, base-before-rotation from base-after-rotation, and translation-last from translated-vector mistakes. Source units are applied to b and insertion facts once; they are not applied again during composition or point application.

## 14. Base compensation

The mathematical invariant is **L*b = anchor**, independent of rotation and valid nonzero scale. The required BlockBaseMapsExactlyToInsertionAnchor test uses a nonzero3D base, nonuniform/negative scale, rotation90 and tilted+Y frame; its selected values compare exactly to the04C anchor. Oblique/extreme arithmetic uses the supplied dimensional comparison policy without rewriting outputs.

This invariant is algebraic; bit-for-bit equality for all arbitrary floating inputs is not claimed. Normal IEEE cancellation/roundoff remains.

## 15. Point transform

ApplyPoint calls a fresh exported Transform3.Apply on a canonical-mm definition point. Translation applies. Output must be finite; failure returns no point with NonFiniteTransformedValue. This operation does not normalize raw source units or generate geometry.

Private matrix construction checks every intermediate composition via Transform3 validation. Nonfinite coefficients produce NonFiniteTransform with no published placement. Large base/anchor and scale composition overflow cases are tested.

## 16. Vector transform

ApplyVector explicitly uses only the linear3×3 block of the immutable matrix. Translation does not apply. Input/output use the distinct CadVector3 type, with no point-helper shortcut or inverse. The caller supplies vector components in the definition frame at the appropriate dimensional scale.

Translation-only fixture: point(1,2,3)→(101,202,303), vector(1,2,3)→(1,2,3). Transformed vector overflow is a structured failure with no vector.

## 17. Nonuniform scale

For relative point(1,2,0), scale(2,3,1), rotation90°, S produces(2,6,0), then R produces **(-6,2,0)**. The wrong R-then-S result(-4,3,0) is explicitly excluded by the independent test.

Centimeter integration fixture proves dimensional base/anchor use factor10 while scale remains(2,3,1): source b=(10,20,30), i=(100,200,300), canonical q=(110,220,330) gives **(1020,2060,3030)** mm at zero rotation.

## 18. Negative / mirror scale

Negative X/Y/Z and combinations preserve their affine coefficients and mirrored point/vector behavior. No winding, vertex-order, arc-sweep or geometry correction occurs. A negative scale is not rejected as degeneracy. Exact zero is refused separately.

## 19. Determinant / orientation

OrientationSign is the parity of negative nonzero source scale factors. A is right-handed and R is a proper rotation, so this equals the mathematical sign of det(linear L).

It avoids multiplying three huge/tiny values merely to determine sign. Determinant magnitude is not stored: it can overflow/underflow even when all matrix coefficients are finite. Ordinary mirror fixtures independently compute the3×3 determinant from frozen matrix values to verify the sign; tiny negative and huge positive scale cases verify stable metadata without a determinant-magnitude dependency. No inverse/conditioning claim is made.

## 20. Target definition resolution

Only source definitions' BLOCK names and begin/end/disposition are inspected. Matching is deterministic OrdinalIgnoreCase, retaining original name spelling. Case-colliding definitions are ambiguous and refuse rather than selecting the first. AlternateName is not an alias fallback. Anonymous `*D`/`*A` names have no inferred semantics.

Missing/ambiguous/invalid definitions receive structured diagnostics. Known external/xref/dependent profiles refuse rather than loading external content. BLOCK_RECORD metadata is looked up only for unit consistency; a resolved source BLOCK owner must agree with that named record. Contradictory/unsupported units retain04B refusal semantics. Absent BLOCK_RECORD metadata does not fabricate a declaration or local conversion ratio.

No Children collection is read. A self-referencing child fixture proves root placement terminates without expansion; explicitly requesting the child itself yields only its own local translation, without the outer transform. Arrays and invalid row/column counts refuse with UnsupportedInsertProfile and no origin-cell placement. No spacing is applied. Cancellation is checked at metadata-loop boundaries.

## 21. Provenance / diagnostics

Placement retains compact document GUIDs/SHA256, INSERT original/canonical handle and record range, target-name fact, insertion coordinate facts/04C frame/defaults,04B rotation fact/default, scale axis facts/defaults, BLOCK source handle/record/name/base facts and unit decision/optional BLOCK_RECORD evidence. Numeric policy ID is recorded. No Raw graph, child list, full definition payload or file path is retained.

Diagnostics are immutable enum Code, compact source identity/range, invariant Field and copied pair indices where applicable. Default rule ordering is X/Y/Z scale, rotation, then04C anchor rules. No timestamp/localized free-text identity. Successful/failed placement status agrees with matrix presence. Existing upstream strict snapshot refusal remains in force.

## 22. Analytic hand oracles

| Case | Independent expected result |
| --- | --- |
| All-zero base/insertion, unit scale, zero rotation,+Z | Full identity4×4 |
| Translation(100,200,300), q=(1,2,3) | (101,202,303); vector unchanged |
| b=(10,20,30), i=(100,200,300), q=(11,22,33) | (101,202,303) |
| Rotation90, q=(1,0,0) | (0,1,0), compared using declared policy |
| Scale(2,3,1) then90, q=(1,2,0) | (-6,2,0) |
| Scale(-1,1,1) | Mirrored X; orientation-1 |
| Nonzero-base regression | (940,2020,0) |
| +Y tilted fixture | (-91,0,204) |
| +Y plus negative X,3D base | b→(-100,0,200); q=(12,23,30)→(-91,0,196) |

Oblique fixture uses independently derived04C basis constants/arithmetic; expected coordinates are not generated by the production builder. Entire4×4 coefficient expectations are frozen for identity and the historical90° fixture.

## 23. Historical nonzero-base regression

Dedicated in-memory DXF fixture: b=(100,50,0), q=(110,70,0), i=(1000,2000,0), scale=(2,3,1), rotation90,+Z.

Hand sequence: q-b=(10,20,0)→S=(20,60,0)→R=(-60,20,0)→+i=**(940,2020,0)**. Omitting base compensation gives the distinct wrong(790,2220,0).

Independent full matrix:

```text
[ 0 -3  0 1150 ]
[ 2  0  0 1800 ]
[ 0  0  1    0 ]
[ 0  0  0    1 ]
```

Tests compare all16 coefficients within the explicit coefficient tolerance, preserving actual sin/cos roundoff. Historical ACadSharp context motivates the bug class only; no vendor package/code/helper was consulted or copied into production.

## 24. Controlled-source proof

Official byte-exact source: `datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf`, SHA256 `2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC`.

- INSERT original/canonical handle **DB**, structural record130, source pairs2971–2983 / lines5943–5968. Target `_ARCHTICK`.
- Source insertion(0,-1300,0); source degrees180→04B π; scale X/Y0.175, Z genuinely absent→1; extrusion genuinely absent→04C +Z.
- BLOCK **75**, record107, pairs2700–2712. Name pair2706; base X/Y/Z pairs2708/2709/2710 are explicitly(0,0,0).
- BLOCK_RECORD **74**, record93, unit fact pair2565=0;04B UsesDocumentUnit with source INSUNITS4→mm/factor1. Owner identity agrees.
- Expected one-local-placement q=base→**(0,-1300,0)**; q=(1,0,0)→**(-0.175,-1300,0)** within declared numeric policy.

DB is inside a definition; these are immediate-parent coordinates, not an accumulated model-space occurrence. No child geometry was expanded. The actual zero-base controlled case supplements the dedicated nonzero-base fixture; it does not replace it.

Proof: [controlled-local-transform.json](../../artifacts/phase04/04d-20261016/final/controlled-local-transform.json) and ControlledArchTickProducesOneLocalTransformWithoutExpansion. Raw snapshot serialization remains identical before/after.

## 25. Tests

**76 new passing cases** in all ten required classes:

| Class | Cases |
| --- | --- |
| CadBlockBaseNormalizationTests | 9 |
| CadInsertScaleTests | 15 |
| CadInsertLocalTransformTests | 11 |
| CadInsertTransformOrderTests | 3 |
| CadInsertTiltedOcsTests | 3 |
| CadInsertMirrorTests | 7 |
| CadInsertDefinitionResolutionTests | 21 |
| CadInsertProvenanceTests | 4 |
| Cad04DScopeArchitectureTests | 2 |
| Cad04DDeterminismTests | 1 |

Fixtures are small hand-authored in-memory DXF plus the actual controlled snapshot. No disk fixture was rewritten. Tests cover the ten analytic cases, full matrices, source/default evidence, owned immutability, units, unknown/ambiguous/external profiles, array refusal, owner disagreement, cancellation and application/composition overflow.

First focused run43/48 passed: five tests incorrectly assumed malformed/nonfinite DXF would publish a snapshot. Actual upstream refusal was retained; tests now verify that real rejection separately and exercise nonfinite boundary facts with explicit hand-authored inputs. No fake snapshot bypass or Raw change was introduced. Intermediate full suite1126/1126 passed. Owner-binding regression adds two cases; **final1128/1128** below is authoritative.

## 26. Regression

Final complete unfiltered suite: **1128 total / 1128 executed / 1128 passed / 0 failed / 0 skipped**. All1052 baseline stable IDs and display names remain present and Passed. Missing baseline0; failed baseline0; new IDs76. Older colliding display names remain distinguished by stable IDs.

Evidence: [final TRX](../../artifacts/phase04/04d-20261016/final/tests.trx), [gate log](../../artifacts/phase04/04d-20261016/final-gate.log), [identity/source audit](../../artifacts/phase04/04d-20261016/final-audit.json), [new test inventory](../../artifacts/phase04/04d-20261016/new-test-inventory.json).

## 27. Builds / warnings

Core Release/Rebuild **PASS,0 errors,0 warnings**. Full DtoB.sln Release/Rebuild **PASS,0 errors**. No new warning identity. The established gate verifies exact approved MSB3277 pairs:

| Assembly | Versions | PublicKeyToken / Culture |
| --- | --- | --- |
| Microsoft.VisualBasic | 10.0.0.0 / 10.1.0.0 | b03f5f7f11d50a3a / neutral |
| System.Drawing | 4.0.0.0 / 8.0.0.0 | b03f5f7f11d50a3a / neutral |
| WindowsBase | 4.0.0.0 / 8.0.0.0 | 31bf3856ad364e35 / neutral |

Used VS2022 MSBuild/Roslyn/VSTest fallback with SDK8.0.416, workload resolver disabled, Revit reference directory `D:\프로그램\Revit 2025`. Existing dotnet-discovery banner remains; actual process exits/compiler outputs establish success. No SDK installation/security/global.json change.

Reproduction: `scripts/Verify-Phase00Foundation.ps1 -Toolchain VisualStudio -RevitInstallDir 'D:\프로그램\Revit 2025' -EvidenceDirectory <directory>` with DTOB_04D/04C/04B/04A/03G/03H_EVIDENCE_DIRECTORY pointing there. Logs: [Core](../../artifacts/phase04/04d-20261016/final/core-build.log), [Solution](../../artifacts/phase04/04d-20261016/final/build.log).

## 28. Determinism

Fixed04D projection is compared exactly under Invariant/fr-FR/ko-KR. Two additional fresh VSTest processes, distinct ignored CWDs, each repeat all three cultures. Their UTF8 evidence bytes match final-suite bytes.

Includes source unit factor/document authority, BLOCK handle/name/base/facts, INSERT handle/target/insertion facts/anchor, Ax/Ay/N, canonical radians, scale XYZ, matrix16, orientation sign, independently asserted point/vector inputs/outputs, default-rule IDs/order, policy ID and diagnostic code/field/pair order. Mathematical assertions precede serialization; no production-generated golden is automatically blessed.

SHA256 **`02045C202CACF9DA66406827E297435CCCB7448D1D0A38C7B52E7FCAAA3A0EE5`**. This is **04D local-transform evidence**, not the final PHASE04 normalized CadDocument digest.

Evidence: [projection](../../artifacts/phase04/04d-20261016/final/local-transform-evidence.json), [process A](../../artifacts/phase04/04d-20261016/process-a/determinism.trx), [process B](../../artifacts/phase04/04d-20261016/process-b/determinism.trx).

## 29. Raw / Dxf zero mutation

Every Raw/Dxf source byte matches before04D. All86 original fixture hashes match the released manifest. Actual pipeline tests prove unchanged snapshot serialization. No source/managed file, reader mapping, Raw status/default, identity graph or LOW-01 policy was changed. No source reopening or filesystem IO exists in Placement.

## 30. 04A / B / C stability

All previous04A/B/C production/test/project files are byte-identical to before04D. CadId vectors and all1052 baseline tests pass. Core/Geometry/Analysis/legacy contracts and source remain unchanged. No schema or occurrence-identity change.

Actual final evidence retains:

- 04A JSON: `3E49A200C8AD688F6F53842F17556D849979B1C8F3F6A083438721CA02F58EBC`.
- 04B scalar: `568D2A7C28863360A4839FF0CA59DF09C1E7C9066EC3A487701872506A68D4C3`.
- 04C coordinate: `81CA2010F120BD65888EF57AABE5A2F55E230DDC291276E1CBA8BAC9584F0342`.

The audit checks these exact values, rather than merely comparing new outputs with each other.

## 31. No Revit leakage

Source/project/assembly hard gates pass. Placement contains no Autodesk.Revit, RevitAPI/UI, Revit Transform/XYZ, ElementId, BuiltInCategory or internal-feet behavior. It references only existing DtoB math/contracts. Desktop/IPC/Revit code is unchanged. No Revit host behavior/run is required or claimed for this pure local-placement change.

## 32. Explicit 04E+ exclusions

No nested traversal, outer-parent composition, recursive expansion, MINSERT cells/spacing, occurrence paths/IDs or global occurrence budget. No Layer0/ByLayer/ByBlock, geometry flattening, bulge, final spatial curves, affine bounds, full CadDocument, recognition, BIM or Revit output. Scope tests forbid child access, array spacing, occurrence/geometry assembly and vendor dependencies in production Placement source.

## 33. Known limitations

- Single ordinary INSERT local placement only. Array profiles refuse; no root-cell approximation. No application command or full normalizer.
- Definition lookup is a bounded scan of published metadata using OrdinalIgnoreCase, not a cached graph/name service or Unicode CAD-symbol equivalence guarantee. Duplicate/case-colliding names refuse.
- Missing required base/anchor XYZ remains invalid. Existing stricter source/snapshot refusal can stop malformed source before04D is called.
- External/dependent definitions refuse; no external file loading. Missing BLOCK_RECORD metadata is retained as absence; no insertion-time block unit heuristic.
- Point/vector input is already in the definition's canonical coordinate frame; this is not a geometry extraction/normalization shortcut.
- Orientation sign is mathematical source-scale parity; determinant magnitude/inverse conditioning is not computed. Tiny nonzero scales retain IEEE precision limits. Intermediate overflow refuses even if a rearranged mathematical expression might cancel; no repair/snapping.
- Algebraic b→anchor invariant is tested exactly on selected fixtures and numerically for oblique values, without a universal bit-exact claim.
- Existing five-unit restriction, caller-supplied numeric profile, Raw partial coverage/LOW-01, approved reference warnings and dotnet discovery limitation remain.

## 34. Git state / self-review

Stayed on `phase/04-cad-normalization` at released HEAD. No staging/commit/push or deletion/reset of prior work. Reviewed all four new production files, five new test files, additive documentation and tracked/untracked diff inventory. Self-review added owner/name agreement checks before final build.

`git diff --check` passes. Strict UTF8/trailing whitespace checks cover untracked04D files too. Audit permits only these nine source/test additions, this report and two additive documentation changes. Accepted ADR bodies, independent reviews, previous reports and protected source are unchanged. Existing Git LF→CRLF notices are conversion notices, not whitespace-check failures or compiler warnings.

Evidence: [audit script](../../artifacts/phase04/04d-20261016/Audit-04D.ps1), [final audit](../../artifacts/phase04/04d-20261016/final-audit.json), [Git inventory](../../artifacts/phase04/04d-20261016/git-status.txt). Local evidence remains ignored and preserved.

## 35. Next independent review gate

All local04D acceptance items have the source/math/test/build/determinism evidence above. Ready for **independent Antigravity PHASE04D implementation review**. Stop here; no04E, commit or push. No independent acceptance is claimed.

PHASE_04D_PASS_WITH_KNOWN_LIMITATIONS
