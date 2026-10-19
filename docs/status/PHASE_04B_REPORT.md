# PHASE 04B — Units / Angles / Numerical Policy

Date: 2026-10-16

Local status: **PHASE_04B_PASS_WITH_KNOWN_LIMITATIONS**. Ready for independent Antigravity 04B implementation review. This is not independent acceptance or whole-PHASE04 completion.

## 1. Scope

Implemented pure scalar interpretation of actual Raw source facts: document/block units, supported lengths, reviewed angle roles and a central immutable numerical comparison policy. There is no concrete ICadNormalizer engine or application integration, and no spatial CadDocument is generated.

Read the owner request, actual Raw/Dxf mappings, Accepted ADR-018/019/020, independent plan/ADR/04A reviews, the 04A report and the implementation plan before implementation. Independent 04A verdict is PHASE_04A_PASS with zero findings. This owner instruction authorizes only 04B.

## 2. Baseline

Branch `phase/04-cad-normalization`, HEAD `76ff6e0d8ccac797ba60d8c5657922c796a0e8c9` (`v0.4-phase03`). Approved 04A and earlier local documents were already uncommitted/untracked and were preserved.

Independent 04A baseline: **844/844 passed**. Baseline individual identities were read from [04A TRX](../../artifacts/phase04/04a-20261016/verified/tests.trx). Before editing, all Git-visible tracked/untracked files were hashed into [before.json](../../artifacts/phase04/04b-20261016/before.json). This permits comparison against the actual reviewed working tree, rather than incorrectly treating all untracked 04A code as new 04B work.

## 3. Files changed

04B adds six production files:

- `packages/DtoB.Cad/V2/CadNumericPolicy.cs`
- `packages/DtoB.Cad.Normalize/Scalars/ScalarContracts.cs`
- `packages/DtoB.Cad.Normalize/Scalars/CadUnitNormalizer.cs`
- `packages/DtoB.Cad.Normalize/Scalars/CadScalarLengthNormalizer.cs`
- `packages/DtoB.Cad.Normalize/Scalars/CadAngleNormalizer.cs`
- `packages/DtoB.Cad.Normalize/Scalars/RawScalarAccess.cs`

Five new test files: `Cad04BScalarFixture.cs`, `CadUnitNormalizationTests.cs`, `CadScalarLengthNormalizationTests.cs`, `CadAngleNormalizationTests.cs`, `Cad04BPolicyAndProvenanceTests.cs`, all under `tests/DtoB.Tests`.

Documentation: this report, an additive current-checkpoint notice in `docs/plans/PHASE_04_IMPLEMENTATION_PLAN.md`, and one in `tasks/PHASE_04.md`. No project/solution/reference changes were needed in 04B. Earlier Git changes to solution/test projects belong to the preserved 04A baseline.

## 4. Unit policy

Actual source: `snapshot.Header.InsertionUnits`, a DxfSourceFact, not a guessed property or inferred drawing extent. CadUnitNormalizer creates the existing 04A CadUnitDecision without changing its public contract. Original unit code remains original; selected units have separate resolved meaning.

No name, layer, size, geometry, Revit or machine settings enter unit interpretation. Uniform unit factors are scalar values only; SourceToModel and entity transforms are not calculated.

BLOCK_RECORD `TABLES/BLOCK_RECORD` code70 is preserved by the current structural reader in the field named `Flags`. RawScalarAccess reads that actual fact as insertion units only in this scope. It does not confuse this with BLOCK flags. Absent/0 uses the resolved document unit, matching declaration is accepted, contradictory/unsupported declaration refuses the branch, malformed declaration is invalid. No block-local scaling ratio or insertion-time heuristic exists. Document authority is checked before accepting block/length units.

Specification: [Autodesk HEADER](https://help.autodesk.com/cloudhelp/2024/ENU/AutoCAD-DXF/files/GUID-A85E8E67-27CD-4C59-BE61-4DC9FADBE74A.htm), [BLOCK_RECORD](https://help.autodesk.com/cloudhelp/2023/ENU/AutoCAD-DXF/files/GUID-A1FD1934-7EF5-4D35-A4B0-F8AE54A9A20A.htm); Accepted ADR-019 governs the conservative refusal policy.

## 5. Supported INSUNITS table

| Source code | Source unit | Scale to canonical mm |
| --- | --- | --- |
| 1 | Inches | 25.4 |
| 2 | Feet | 304.8 |
| 4 | Millimeters | 1.0 |
| 5 | Centimeters | 10.0 |
| 6 | Meters | 1000.0 |

Factors are exact decimal literals before conversion to double storage. Subsequent arithmetic follows IEEE double precision; arbitrary-precision geometry is not claimed.

## 6. Unresolved / unsupported units

| Actual source | Result |
| --- | --- |
| Absent or integer0 | NeedsExplicitUnits; no factor without explicit supported selection |
| Supported declaration | Resolved / Declared |
| Other known DXF integer code in1–24 | UnsupportedUnits; explicit selection cannot silently replace it |
| Negative, greater than24, wrong kind, invalid/duplicate/inconsistent fact or missing present evidence | InvalidUnits |
| Explicit supported selection for absent/0 with decision ID and reason | Resolved / Explicit; original null/0 retained |
| Override contradicting a valid declaration | InvalidUnits / ContradictoryUnitOverride |

No unresolved/unsupported/invalid result carries a guessed scale. Downstream scalar refusal preserves the unit refusal reason rather than suggesting that an unsupported declaration merely needs selection.

## 7. Controlled fixture result

Read the official byte-exact `datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf` through the released lexical → structural → geometry → annotation → graph → published snapshot pipeline, supplying fixed valid drawing/revision GUIDs and the actual source SHA256.

Source hash: `2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC`.

Observed/verified: `$INSUNITS=4`, source unit Millimeters, canonical unit mm, scale **1.0**. The controlled ARC at source pair2868 has code40 radius900.0, start0°, end90°; scalar output is radius900mm, start0rad, sweepπ/2. INSERT at pair2971 has rotation180° → π. No coordinates were spatially transformed to establish these results.

## 8. Scalar length normalization

CadScalarLengthNormalizer accepts an explicit supported field role and source fact, checks finite input and document/unit authority, and computes `source * scale`. No snapping, recognition epsilon, integer rounding or global reinterpretation of Raw numbers.

Supported roles: ARC/CIRCLE radius, TEXT/MTEXT height, MTEXT rectangle width, INSERT column/row spacing, LWPOLYLINE elevation/constant width and supported rotated-linear DIMENSION source measurement. Radius/height must be positive; width/measurement nonnegative; elevation/spacing permit signs. Missing facts remain absent. Unsupported roles refuse. Finite overflow returns NonFiniteNormalizedValue and no canonical value. Source NaN/infinities return NonFiniteSourceValue.

Dimensionless scales, normals, bulges and literal lineweight370 are not source-length roles. There is no lineweight conversion or style engine here. Raw remains unchanged, including all unmapped values.

## 9. Angle role model

| Role | Actual Raw source | 04B interpretation |
| --- | --- | --- |
| ARC start/end | RawArc.StartAngleDegrees / EndAngleDegrees | Degrees, scalar start/sweep |
| INSERT rotation | RawInsert.RotationDegrees | Degrees; documented absent zero |
| TEXT / ATTRIB / ATTDEF rotation | RawText.RotationDegrees, including nested Text payload | Degrees; documented absent zero |
| Rotated-linear DIMENSION rotation | RawDimension.RotationDegrees, valid subtype0 | Degrees; absent remains absent |
| POINT display angle | RawPoint.AngleDegrees | Degrees, metadata only |
| STYLE oblique | Structural `ObliqueAngle`, code50 | Signed degrees, metadata only |
| MTEXT rotation | Retained lexical code50 and structural context markers | Already radians; narrow unambiguous profile only |

Code number alone never selects a conversion. DIMENSION subtype1/aligned orientation and other ambiguous subtype semantics are explicitly deferred/UnsupportedAngleRole even where Raw retains some fields. No missing subtype is fabricated.

## 10. Degrees / radians

One centralized implementation performs degrees × π/180. Wrapped degree roles first reduce modulo360, avoiding overflow for extreme finite degree inputs. STYLE oblique stays signed and is not reduced. Already-radian MTEXT bypasses degree conversion. Canonical results normalize negative zero through the existing finite contract.

Independent hand expectations cover0/90/180/270/360/-90 and multiple revolutions, every supported degree role, signed STYLE and nonfinite source refusal. The [generic Autodesk DXF angle-code specification](https://help.autodesk.com/cloudhelp/2024/ENU/AutoCAD-DXF/files/GUID-3F0380A5-1C15-464D-BC66-2C5F094BCFB9.htm) establishes degree-based file facts; the entity-specific MTEXT exception takes precedence. Actual typed mappings were inspected rather than inferred from API angle conventions.

## 11. MTEXT exception

RawMText has no typed rotation property. The field is nevertheless available in the immutable Raw lexical store with pair/line/subclass/application/control evidence. RawScalarAccess projects the retained simple code50 fact into an owned scalar input; it does not change Raw, re-open files or claim a nonexistent typed API.

The implemented profile requires one main/null-subclass code50 and no direction or column markers. Direction-vector precedence, multiple rotations, foreign subclasses and column collisions produce UnsupportedAngleRole. Private control/application data cannot become rotation. No vector calculation or MTEXT layout is implemented.

Real pipeline regression: source `1.5707963267948966` → **π/2**, expressly unequal to π²/360. Additional real pipeline tests cover direction/columns/duplicates/foreign contexts, private control group, absence and malformed numeric content.

Primary source: [Autodesk MTEXT](https://help.autodesk.com/cloudhelp/2020/ENU/AutoCAD-DXF/files/GUID-5E5DB93B-F8D3-4433-ADF7-E92E250D2BAB.htm). Relevant downloaded specification text is retained under the local 04B evidence directory.

## 12. Canonical angle range

Rotation/ARC endpoint policy is **[0,2π)**; STYLE oblique is signed metadata. Remainder arithmetic has no epsilon comparison. Exact ±2π and ±0 become positive0. Adjacent representable boundary values are independently tested.

If adding2π to a tiny negative remainder rounds to the excluded2π endpoint, the result is the immediately preceding representable double (`Math.BitDecrement(Math.Tau)`). This is directed endpoint repair to uphold the half-open range, not tolerance-based equality or quantization. Tiny positive radians remain nonzero. Large nonexact binary multiples are treated by their actual stored value, without an epsilon pretending they are exact revolutions.

## 13. ARC scalar semantics

CadArcScalars contains source start radians plus a positive CCW sweep and both source evidences. It checks source document/record compatibility and required roles. Hand oracles:0→90 gives start0/sweepπ/2;270→90 gives start3π/2/sweepπ; -90→90 gives the same;720→810 gives0/π/2.

Equal/equivalent endpoints produce AmbiguousArcSweep / Unsupported with no invented full circle. Missing/nonfinite or mismatched sources refuse. No center, plane, OCS, world geometry, bounds or placement is constructed.

## 14. Numerical policy

CadNumericPolicy is immutable, owns a validated copy of existing GeometryTolerance and records a required profile ID. Caller explicitly supplies positive finite LengthMm, AngleRadians and MatrixCoefficientTolerance. ADR-019 freezes their representation/domains, not universal numeric values; no production defaults were invented.

Three domains remain distinct: source validity uses finite/legal facts; normalization comparisons use the explicit dimensional policy; future recognition tolerance is absent. Inclusive near-zero helpers only compare. Exact-zero validity is separate; neither comparison changes values. Scalar length/angle conversion does not apply tolerance. Matrix tolerance is a dimensionless contract/helper, not a matrix engine or new conditioning algorithm. IEEE double rounding/underflow limits remain.

## 15. Provenance / diagnostics

Owned scalar inputs/results retain drawing/revision GUIDs, SHA256, field role, original/canonical handle when available, record pair/line range and fact pair/line evidence. No Raw graph is retained in scalar state. Header units deliberately have no invented structural record index; header fact pair/line plus source document identify the fact.

Existing CadUnitDecision may carry a caller-supplied registry reference; the scalar wrapper always retains actual compact source evidence. Registry/document assembly remains later scope. Absent INSERT/TEXT defaults record a deterministic rule ID, not fabricated present evidence. Diagnostics have enum codes, ordered source evidence and frozen arguments; no path, timestamp or localized free-text identity.

RawScalarAccess only consumes a published source-complete/graph-validated snapshot, verifies row ownership, and honors cancellation at MTEXT record scan boundaries. Partial typed coverage remains acceptable; there is no heavy document traversal or new cancellation framework.

## 16. Tests

**112 new passing cases** in ten classes (the nine requested classes plus determinism):

| Class | Cases |
| --- | --- |
| CadUnitNormalizationTests | 26 |
| CadScalarLengthNormalizationTests | 20 |
| CadAngleNormalizationTests | 25 |
| CadMTextAngleTests | 7 |
| CadAngleRoleTests | 15 |
| CadArcScalarSemanticsTests | 8 |
| CadNumericPolicyTests | 5 |
| CadUnitProvenanceTests | 3 |
| Cad04BScopeArchitectureTests | 2 |
| Cad04BDeterminismTests | 1 |

Small analytic facts and in-memory DXF fixtures supplement the actual controlled source. No disk fixture bytes were added/rewritten. Expected conversions use independent constants/arithmetic; no production converter generated the expected oracle or golden.

Initial development checks found an MSBuild workload-resolver invocation issue, a C# test-pattern syntax error and an xUnit analyzer rule; corrected before the gate. Initial focused run:101/102 passed, one mistaken controlled radius expectation400 versus original source900.0. The expectation was corrected after directly inspecting source code40 bytes; implementation was not changed to satisfy it. Intermediate full gate953/953 preceded three additional refusal-reason tests. **Final evidence below is956/956**, not those earlier runs.

## 17. Regression comparison

Final unfiltered TRX: **956 total / 956 executed / 956 passed / 0 failed / 0 skipped**. Every one of the844 baseline stable test IDs and corresponding case names remains present and Passed; none were removed, renamed or filtered. Comparing IDs also distinguishes three earlier cases whose display names collide. New identities112. Earlier PHASE00–03 and all04A contract/provenance/identity/immutability cases therefore remain passing.

Evidence: [final TRX](../../artifacts/phase04/04b-20261016/final/tests.trx), [final gate log](../../artifacts/phase04/04b-20261016/final-gate.log), [identity/byte audit](../../artifacts/phase04/04b-20261016/final-audit.json), [new test inventory](../../artifacts/phase04/04b-20261016/new-test-inventory.json).

## 18. Builds / warnings

Core Release/Rebuild **PASS:0 errors,0 warnings**. Full DtoB.sln Release/Rebuild **PASS:0 errors**. The existing verification script checked exact approved MSB3277 conflict identities; no new warning identity appeared:

| Assembly | Versions | PublicKeyToken (Culture neutral) |
| --- | --- | --- |
| Microsoft.VisualBasic | 10.0.0.0 / 10.1.0.0 | b03f5f7f11d50a3a |
| System.Drawing | 4.0.0.0 / 8.0.0.0 | b03f5f7f11d50a3a |
| WindowsBase | 4.0.0.0 / 8.0.0.0 | 31bf3856ad364e35 |

Used the established Visual Studio2022 MSBuild/Roslyn/VSTest fallback with SDK8.0.416, workload resolver disabled, and Revit reference directory `D:\프로그램\Revit 2025`. Standard dotnet SDK discovery still prints its existing unavailable-SDK banner; no security setting, global.json or toolchain installation was changed. Actual MSBuild/VSTest process success and compilation outputs establish the results.

Reproduction: `scripts/Verify-Phase00Foundation.ps1 -Toolchain VisualStudio -RevitInstallDir 'D:\프로그램\Revit 2025' -EvidenceDirectory <evidence>` with DTOB_04B/04A/03G/03H_EVIDENCE_DIRECTORY environment variables pointing there.

Logs: [Core](../../artifacts/phase04/04b-20261016/final/core-build.log), [Solution](../../artifacts/phase04/04b-20261016/final/build.log).

## 19. Determinism evidence

The fixed scalar test independently asserts the expected unit/angle values, then compares an explicit evidence projection across Invariant, fr-FR and ko-KR. It includes unit decisions/scales/statuses, angle values/roles, diagnostic codes/order and fact pair indices.

The same test was executed in two additional fresh VSTest processes from distinct ignored working directories `process-a` and `process-b`. Each process repeats all three cultures. Their exact UTF8 evidence bytes equal the final-suite bytes.

Scalar evidence SHA256: `568D2A7C28863360A4839FF0CA59DF09C1E7C9066EC3A487701872506A68D4C3`.

This is **04B scalar evidence**, not a final normalized CAD-document digest, and is a recorded reproducibility result rather than an automatically blessed expected golden. Evidence: [final projection](../../artifacts/phase04/04b-20261016/final/scalar-contract-evidence.json), [process A](../../artifacts/phase04/04b-20261016/process-a/determinism.trx), [process B](../../artifacts/phase04/04b-20261016/process-b/determinism.trx).

## 20. Raw / Dxf zero-mutation

All `DtoB.Cad.Raw` / `DtoB.Cad.Dxf` source bytes match the pre-04B snapshot. All86 existing fixture SHA256 values match the released manifest. Source/graph serialization before/after scalar access is unchanged in tests. No Raw contract, parser mapping, reader limit or LOW-01 behavior was modified. The MTEXT accessor reads retained evidence without mutating/replacing the upstream snapshot.

## 21. 04A / legacy contract stability

Every existing04A source/test/project file is byte-identical to the captured baseline. The only new V2 file is CadNumericPolicy. CadUnitDecision, CadIrJson v2, Cad2 framing, compact provenance, immutability and the pure normalizer boundary remain unchanged.

Existing CAD2 contract JSON hash still equals `3E49A200C8AD688F6F53842F17556D849979B1C8F3F6A083438721CA02F58EBC` in all three cultures. Legacy Cad v1, Core, Geometry and Analysis source bytes and regression behavior are unchanged. No BIM schema or API migration occurred.

## 22. No Revit leakage

Source/assembly/package tests pass. Scalar helpers contain no Autodesk.Revit, RevitAPI/UI, ElementId, BuiltInCategory, FamilySymbol, WallType or internal-feet behavior. No UI/IPC/Revit production code changed. A Revit host run is neither required for these pure scalar changes nor claimed as evidence.

## 23. Explicit 04C+ exclusions

No OCS/WCS, AAA, extrusion/vector basis construction, frame conversion, block-base compensation, INSERT matrices, nested/MINSERT expansion, Layer0/ByLayer/ByBlock resolution, bulge geometry, affine bounds, geometry/spatial document assembly, semantic recognition, BIM or Revit generation. No concrete full normalizer, new viewer, DWG reader/backend or application command was added.

## 24. Known limitations

- Scalar foundation only: there is no complete Raw→CAD2 conversion or application flow. Full normalized document readiness is not claimed.
- Supported document units are deliberately limited to five; no guessing or block-local conversion heuristics.
- Ambiguous MTEXT direction/rotation/column orientation and non-rotated-linear DIMENSION angular semantics remain explicit unsupported cases.
- Absent values only receive zero defaults for the approved INSERT/TEXT rotations. Other absent facts are not synthesized.
- Numerical tolerances require caller values/profile; no universal production profile or future conditioning algorithm is selected here. Double arithmetic precision remains finite.
- Scalar evidence is compact, not an assembled provenance registry; full CAD2 source registry/geometry assembly is later scope.
- Existing Raw partial typed coverage and LOW-01 graph/snapshot refusal behavior remain accepted upstream limitations.
- Existing approved Revit reference warning identities and standard dotnet discovery limitation remain.

## 25. Git state / self-review

Reviewed all six new production files, five new test files, additive documents and the existing tracked diff. Self-review tightened malformed source evidence, document/record authority and exact refusal reasons before the final build.

Stayed on `phase/04-cad-normalization` at the released HEAD. No staging, commit, push, branch switch or cleanup of earlier evidence. Git's existing LF→CRLF notices are working-copy conversion notices, not compiler warning identities or whitespace-check failures.

`git diff --check` passes. Untracked04B additions were inventoried separately and checked for strict UTF8 and trailing whitespace, since ordinary git diff omits them. Before/after audit permits only the eleven new source/test files, this report and two additive documentation changes. Accepted ADR technical bodies, independent reviews, prior reports and protected source hashes remain unchanged. Local evidence/scripts are under the ignored artifacts tree and preserved.

Evidence: [audit script](../../artifacts/phase04/04b-20261016/Audit-04B.ps1), [final audit](../../artifacts/phase04/04b-20261016/final-audit.json), [Git inventory](../../artifacts/phase04/04b-20261016/git-status.txt).

## 26. Next review gate

All local04B acceptance items have the build/test/source/determinism evidence above. Ready for **independent Antigravity PHASE04B implementation review**. No independent PASS is claimed. Stop here; 04C+, commit and push require separate owner authorization.

PHASE_04B_PASS_WITH_KNOWN_LIMITATIONS
