# PHASE 04G — Canonical geometry and CAD v2 document assembly

Date: 2026-10-16. Implementation owner: Codex. Local status: **PHASE_04G_PASS_WITH_KNOWN_LIMITATIONS**. Ready for independent Antigravity implementation review. Independent implementation acceptance and whole-PHASE04 completion are not claimed.

Evidence root below means `artifacts/phase04/04g-implementation-20261016/`. Final evidence is **release/**, not the earlier development, failed final/, or intermediate verified/ runs.

## 1. Architecture-pass authority

Owner explicitly authorized04G implementation after [PHASE_04G_ARCHITECTURE_PASS](../../reviews/PHASE_04G_ARCHITECTURE_REVIEW.md), zero findings. Read the review, historical04G STOP report,04A–F reports/reviews, accepted ADR018/019/020, plan/task and actual frozen contracts/codec. The architecture review resolves both former questions: matrices are persisted orientation truth, and source templates plus occurrence leaves bind different styles without copying geometry. Historical [PHASE_04G_REPORT.md](PHASE_04G_REPORT.md) is byte-identical to the start snapshot.

## 2. Scope

Pure in-memory synchronous Raw→CAD2 composition, canonical source geometry, factual annotations, source/style/definition/occurrence/entity registries, complete ordered ledger, strict document validation and separate analytic bounds queries. Caller owns scheduling. No product I/O, parser, UI or downstream integration changes.

## 3. Baseline

Branch `phase/04-cad-normalization`; HEAD `76ff6e0d8ccac797ba60d8c5657922c796a0e8c9`. Independent04F verdict PASS, zero findings; **1,258 passing baseline identities** from `artifacts/phase04/04f-20261016/final/tests.trx`. Captured all **462** pre-existing Git-visible files in `before.json`, preserving earlier untracked04A–F work. No reset/stage/branch switch.

## 4. Files changed

Five new production files under `packages/DtoB.Cad.Normalize/Assembly/`: `AssemblySupport.cs`, `CadNormalizer.cs`, `CadNormalizer.Geometry.cs`, `CadNormalizer.Styles.cs`, `CadBoundsQuery.cs`.

Five new test files under `tests/DtoB.Tests/`: `Cad04GAssemblyTests.cs`, `Cad04GGeometryAndBoundsTests.cs`, `Cad04GEvidenceTests.cs`, `Cad04GIntegrationEdgeTests.cs`, `Cad04GGeometryBudgetBoundaryTests.cs`.

New [pre-code implementation matrix](../plans/PHASE_04G_IMPLEMENTATION_MATRIX.md) and this implementation report. Additive checkpoints in implementation plan/task. Five existing test files change exactly the obsolete assembly-wide absence-of-engine assertion to allow exactly the concrete04G CadNormalizer: `CadNormalizationBoundaryTests.cs`, `Cad04BPolicyAndProvenanceTests.cs`, `Cad04CArchitectureAndDeterminismTests.cs`, `Cad04DArchitectureAndDeterminismTests.cs`, `CadOccurrenceProvenanceTests.cs`. Test identities/names and remaining assertions are retained. No csproj, solution, dependency or accepted contract change in04G. Audit inventories additions separately because ordinary Git diff omits earlier untracked work.

## 5. Frozen contract mapping

[Implementation matrix](../plans/PHASE_04G_IMPLEMENTATION_MATRIX.md) was written before production edits and maps actual contract source, producer, identity, geometry/style ownership, occurrence relation, provenance and completeness. Schema2, immutable DTOs, CadId framing, CadModelSpaceRecord shape and strict CadIrJson remain unchanged. No serialized bounds, orientation signs, extension data or parallel side registry.

## 6. Concrete normalizer / assembler decision

Plan section20 explicitly assigns complete/partial document, assembler and simple queries to04G;04H owns QA/comparison. Therefore one small concrete `ICadNormalizer` is appropriate now. It composes existing04B units/scalars,04C coordinates,04E expansion and04F styles;04D remains indirectly reused. No engine was moved into another assembly to evade old guards. Existing stage-local dependency/algorithm bans remain enforced.

The injected numeric-policy ProfileId must match options.ProfileId; otherwise CAD_NUMERIC_PROFILE_MISMATCH refuses publication. The document's one existing ProfileId therefore identifies the actual supplied numerical policy. No hidden numeric defaults or schema field was added.

## 7. Source coverage matrix

| Source |04G result / limitation |
| --- | --- |
| LINE / POINT |04C WCS mm; POINT display angle explicitly deferred because frozen point has no metadata slot |
| LWPOLYLINE | Ordered exact line/arc references and Closed; width facts retained and deferred |
| CIRCLE / ARC |04B radius/angles plus04C OCS center/frame; shared analytic local payload |
| SOLID | Planar triangle/quad boundary; nonplanar/degenerate input refuses payload |
| TEXT / MTEXT | Factual content, anchors, height and supported scalar rotation; layout/direction limitations explicit |
| DIMENSION | Supported factual mixed-frame anchors and optional source measurement/rotation; no graphics synthesis |
| INSERT | Source entity/template plus unchanged04E occurrence; no duplicate flattened geometry |
| ATTRIB / ATTDEF | Explicit unsupported spatial instancing; source/ledger retained |
| Other unsupported Raw families | Explicit unsupported source/ledger/diagnostics; no silent dropping |

The pre-code matrix separately records actual Raw,04B,04C and04F support for each family. No legacy POLYLINE/entity parser extension.

## 8. Identity policy

Source entities use existing Source domain and factual authority GUID/GUID/hash + Raw canonical handle, or domain-qualified locator fallback. INSERTs/definitions/occurrences reuse04E policies. Geometry uses DerivedComponent(Text(sourceEntityId),Text("geometry")); segments use source entity + "segment" + factual ordinal. Source/template/effective style components are source/context/role scoped, never keyed by coordinate values. No second hashing format.

Leaf identity is exactly Leaf domain with two Text fields: SourceEntityId.Value, OccurrenceId.Value. An independently written binary-frame test verifies tags, big-endian lengths/counts and digest, without calling the production field writer to generate its expectation. Style, color, matrix, bounds, DFS ordinal, path/time/randomness cannot enter that two-field API.

## 9. Source-template model

One entity per direct BLOCK source child, retaining source ref, shared GeometryId and declared/unresolved StyleId. Definitions contain source templates only. Source INSERTs are factual templates/membership, distinct from occurrence nodes. Unused accepted definitions remain in the catalog; no fictitious model-space instances.

## 10. Projected-leaf model

One wrapper per emitted04F source/occurrence projection. It retains original Source/Evidence and shares template GeometryId/annotation facts; its own StyleId selects that projection's effective style. Only its ModelSpace relation introduces occurrence placement. Leaves never enter definition membership. Nested INSERTs contribute their own child occurrences/leaves, without a second INSERT geometry leaf.

## 11. Root entity model

ENTITIES non-INSERTs use one source entity with effective root style, ModelSpace(entity,null). No extra root template/leaf pair. Source-factual code67 is read only inside eligible retained common-entity context: paper/unknown-space roots remain catalog/ledger data and are explicitly excluded from model space, including their occurrence descendants. No layout classification.

## 12. Geometry sharing proof

Executable Red/Blue fixture: template E and both occurrence leaves reference exactly one GeometryId; leaves have different IDs and effective ACI1/5 styles. Definition has E only; ModelSpace has the two leaves and their occurrences. Root-line test has exactly one entity/geometry/model record. Nested/shared-transform tests prove placement changes query bounds without modifying shared geometry.

## 13. Style catalog policy

No value-only deduplication. Modes, resolution state, rule/evidence and source/context role make each style factual and deterministic. Style ordering is source declaration/root order, then INSERT contexts in04E order, then projected-child order. No reference hash or dictionary enumeration defines IDs/order. Duplicate layer names are all withheld and diagnosed; no first-wins catalog entry.

## 14. Source/template style

Explicit source values become Known; ByLayer/ByBlock declarations remain unresolved Unsupported with their modes; genuine absence remains Absent. Layer0 stays factual layer0 on the shared template. Explicit/deferred fields retain compact source facts. No first occurrence's effective value is assigned to a shared template.

## 15. Effective occurrence style

Exact `(source,occurrenceId)` lookup selects04F output; roots use `(source,null)`. Layer/Color/Linetype state/value/rule and original modes are copied, with supporting context facts flattened through the bounded already-resolved chain into ordered fact refs. Missing expected projection is a structured assembly refusal. Present lineweight/transparency/color-book fidelity remains deferred. No inheritance algorithm was rewritten in04G.

## 16. Model-space registry

Root non-INSERT source order first, followed by04E DFS/direct-child projection order. Existing two-field records only. Paper-space roots and their leaves are excluded explicitly. Unknown/unsupported model entities remain visible through catalog/ledger and unavailable/partial bounds; no source fallback or geometry repair.

## 17. Definition registry

Source order,12 accepted definitions in official input, including unused definitions. Reuse04E IDs/bases where reached; otherwise reuse04D base normalization and04B BLOCK_RECORD-unit validation. Ambiguous BLOCK_RECORD metadata, invalid base, external/dependent profile or incomplete definition is rejected explicitly. Empty XrefPath is correctly treated as empty metadata, not an external reference. Child IDs reference source templates only.

## 18. Occurrence registry

Preserve04E IDs, definition/parent refs, typed path, LocalTransform and AccumulatedTransform. Bind actual INSERT effective style. Root depth remains1; nested composition/cells/cycles/budgets remain04E behavior. No orientation fields, new occurrence identity or matrix rewrite.

## 19. Source/fact registry

Reuse04E's333 source-ordered structural locators/handles; construct5,535 legal facts. Record aggregate evidence plus named structural/geometry/annotation facts and contextual style facts retain exact pair/physical-line ranges. Repeated named fields receive deterministic source ordinals, avoiding loss of repeated linetype fields. Facts are record then ordinal-role ordered; all refs resolve. No Raw graph is embedded and no HEADER/table record is fabricated. Original Raw diagnostics retain invariant code/severity, available record ref, original stage/index addressing the paired snapshot.

## 20. LINE

04C WCS endpoints, including3D, converted once to mm; extrusion does not rotate WCS fields. Declared length-policy degeneracy withholds only that payload and retains failure ledger. Official LINE9C source120, pair2856, independently inspected start(0,0,0), end(900,0,0), source-backed shared GeometryId. Exact line bounds are component extrema.

## 21. POINT

04C WCS position, no point-display rotation. Explicit display-angle deferral when present; point geometry remains useful. Official POINT E1 source134/pair3021 is(0,-1300,0), inside an unused dimension-graphics definition; it is not falsely claimed as a real model-space occurrence. Synthetic tilted-extrusion test proves WCS position stays(2,3,4). Bounds min=max.

## 22. LWPOLYLINE

Frozen ordered segment-ID representation, never parallel serialized vertices/bulges.04C applies elevation/frame once. Closing last→first segment emitted exactly once when Closed. Container and each generated segment reserve budget. Entire source polyline publishes only after every segment succeeds; consumed reservations remain consumed on failure. Source evidence preserves original vertices/bulges/widths. Official open77 source108/pair2713 has endpoints(-.5,-.5,0)→(.5,.5,0), zero/default bulge, width.15 deferred; no fabricated curved official case.

## 23. Bulge geometry

Independently checked [Autodesk bulge definition](https://help.autodesk.com/cloudhelp/2024/ENU/AutoCAD-DXF/files/GUID-0741E831-599E-4CBF-91E1-8ADBCFD6556D.htm). Signed sweep=4atan(b); center=midpoint+left(chord)*(length/4/b-length/4*b); radius=length/4*abs(b)+length/4/abs(b). Source OCS axes and signed start parameter preserve negative/major arcs. b0 is a line; b±1 semicircles; ±2 major and ±.5 minor cases independently reproduce endpoints. Closure and small1e-6 tested. No tessellation.

Extreme tiny/large bulges whose finite double plane/radius/parameter cannot reproduce factual endpoints within the supplied length policy are refused, never flattened to a line or snapped. Explicit1e-300 refusal regression. This is a numerical representation limitation, not invented source geometry.

## 24. CIRCLE

04B positive radius and04C center/Ax/Ay produce CadCircle+CadPlane. Tilted+Y fixture independently gives center(-2,4,3), U(-1,0,0), V(0,0,1), radius5. Official input contains **zero CIRCLE** payloads; synthetic tests supply circle/affine coverage.

## 25. ARC

04B positive CCW interval and04C center/plane; ambiguous source sweep refuses explicitly. Official9D at pair2868 has center0, radius900, start0 and sweepπ/2. Bulge-derived arcs retain signed sweep. Mirror never rewrites the shared source sweep.

## 26. SOLID

Source facts remain in registry; canonical boundary is1,2,4,3. Identical third/fourth corners form triangle1,2,3. No triangulation, volume or recognition semantics. Nonplanar/degenerate source withholds payload. Official7B at pair2747 independently gives(-1,.16439898372402537,0),(0,0,0),(-1,-.16439898372402537,0); it is an unused shared definition primitive, not invented model content.

## 27. TEXT / MTEXT / DIMENSION

Only frozen factual content/anchors/height/supported rotation/optional source measurement. TEXT OCS insertion/alignment, MTEXT WCS insertion, DIMENSION's supported mixed WCS/OCS fields reuse04C. Missing dimension measurement stays null. No font/SHX lookup, glyph extents, layout, dimension graphics or implicit anonymous block occurrences. MTEXT direction cannot fit a frozen direction member and remains explicitly deferred. Annotation results are intentionally Partial/anchor-only; ATTRIB/ATTDEF instancing remains unsupported.

## 28. Affine occurrence representation

Shared local circle/arc plus existing immutable affine transform. No fake world-circle radius, ellipse fitter or per-occurrence geometry clone. SourceToModel remains mm→mm identity; query composition applies it once after accumulated placement.

## 29. Orientation validation

Exact determinant **sign** of the stored finite double3×3 block is calculated using dyadic mantissa/exponent BigInteger arithmetic. It avoids determinant-product overflow/underflow and cancellation guessing. Compare both local/accumulated signs with04E runtime QA signs in nonsingular cases; mismatch refuses with CAD_ORIENTATION_INVARIANT. Stored singular matrices are diagnosed Partial rather than pretending a sign can be recovered. No sign is serialized or used in identity. Huge/tiny/subnormal sign regressions pass.

## 30. Analytic bounds engine

Separate `CadBoundsQuery` over immutable final registries. GeometryLocal/GeometryTransformed, direct Definition, descendant-inclusive Occurrence and Model queries. States Exact/Partial/AnchorOnly/Unavailable; no field added to JSON. Invalid arithmetic withholds that geometry bound; cancellation throws before publishing query output. No padding, sampling, spatial index or HEADER extent dependency.

## 31. Full-circle bounds

Transform center as a point and plane axes with the linear block. Each coordinate has half extent hypot(Ru,Rv) using rescaled arithmetic. Uniform, R45*S(2,3) radius2 with C(10,-4,3), mirrored scale, tilted3D plane and shear are tested. Independent nonuniform XY half extents are sqrt26; Z is3. Tests exclude the false world-circle interpretation.

## 32. Arc bounds

Evaluate endpoints and derivative-zero candidates atan2(b,a) and its antipode only inside the signed modulo2π interval. No whole-circle shortcut for partial arcs. Independent R45*S(2,3) quadrant oracle: X=[10−3sqrt2,10+2sqrt2], Y=[−4+2sqrt2,−4+sqrt26], Z=3. Tests include no interior extrema, signed/near-full/wrapped sweeps, mirror and shear/tilt.

## 33. Definition / occurrence / model bounds

Definition means **direct local non-INSERT geometry only**; nested INSERT/unsupported/annotation content produces unavailable/partial/anchor-only coverage, not a falsely expanded definition bbox. Occurrence includes its own and descendant ModelSpace leaves under accumulated matrices. Model aggregates actual roots/leaves; annotation anchors participate only with explicit partial semantics. A partial document cannot report an Exact complete model bound. Shared geometry placements at X10/X20 independently produce different occurrence bounds. Line/point/SOLID/polyline literal bounds and nested transformed bounds pass.

## 34. EXTMIN / EXTMAX non-use

Actual inverted/unreliable official headers are retained. A regression replaces both extents with different sentinel coordinates, producing a different source hash/IDs but identical geometric bounds. No header repair or header-driven model extent. Bounds code accepts final CAD2 only; it cannot read Raw HEADER.

## 35. Derived geometry budget

One session-global monotonic admission counter. Count root/shared geometry payloads, CadPolyline containers, each generated segment and each projected-leaf entity materialization. Leaf wrappers count as entries because plan§17/ADR020 explicitly require them; they are not falsely counted as additional Geometry payloads. No per-definition/root reset, no refund, reserve before payload/leaf creation. Independent04F style-projection budget remains separate.

**Actual full Normalizer boundary tests**, not just counter tests:511 shared POINT payloads +488×511 leaves +120/121/122 direct roots request249,999/250,000/250,001 entries. First two publish full validated documents; last returns ResourceLimitExceeded, allocated250,000 and **Document=null**. Style projections stay below their own cap. Only existing test-reader options are increased to800 source geometries/600 children; no production source cap changes. Small segment-explosion, shared-leaf and failed-reservation/no-refund integration tests also pass.

## 36. Diagnostic budget

Global MaxDiagnostics5,000 includes normalization and retained Raw diagnostics plus one reserved terminal slot. Ordinary admissions stop at max−1; resource refusal occupies the terminal slot. No truncated-success document. Exact terminal-slot regression with injected cap1; upstream04E/04F full diagnostic-limit regressions retained. Official result has117 normalization +59 retained Raw diagnostics.

## 37. Cancellation

Checks at stage/source/catalog/root/definition/segment/occurrence/leaf/registry/ledger loops and before/after final validation/publication. Upstream tokens are forwarded. Pre-cancel, concurrent composition cancel, query cancel and earlier expansion/style cancellation regressions pass; cancelled normalization never returns a document. Concurrent scheduling is not asserted as a timing/throughput oracle. Frozen CadDocument constructor/validator has no token parameter; it is guarded before/after, and cancellation detected after that bounded synchronous validation still prevents publication. No responsiveness SLA is claimed.

## 38. Completeness / unsupported ledger

All333 records have exactly one ordered outcome, with resolving object IDs:117 Normalized,99 NotApplicable,65 PartiallyNormalized,52 UnsupportedNormalization. Original unsupported52 (VPORT1,APPID3,OBJECTS48) remain explicit; no unsupported coverage is hidden.44 annotations,8 unmaterialized source INSERTs in unused graphics definitions,1 width channel and12 deferred layer lineweights explain relevant Partial behavior. Whole result is PartiallyNormalized; successful primitives do not override upstream coverage limitations.

## 39. Controlled-source normalized document

Official unchanged source `datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf`, SHA256 `2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC`. Final artifact: `release/controlled-normalized-cad-document.json`; summary: `release/controlled-summary.json`.

| Registry / outcome | Actual count |
| --- | ---: |
| Source records / facts |333 /5,535 |
| Layers / styles |12 /201 |
| Definitions / occurrences |12 /17 |
| Source entities / projected leaves / total entities |158 /26 /184 |
| Model-space records |123 (97 roots +26 leaves) |
| Geometry |149 (119 LINE,12 POINT,16 polyline,1 ARC,1 SOLID) |
| Annotation entities |44 |
| Original unsupported records |52 |
| Normalization / retained Raw diagnostics |117 /59 |

Counts were not pre-guessed: independently asserted89 typed primitive sources and60 derived polyline segments give149;89+44+25 source INSERTs give158 sources;04F's26 projections give184;158+17+26 style roles give201. Fact count independently sums actual named source fields,333 aggregate facts,158×8 declaration facts and33×8 catalog facts. Controlled before/after Raw snapshot serialization is identical.

## 40. Controlled geometry oracles

LINE9C, ARC9D, open LWP77, POINT E1 and SOLID7B are checked against actual original code/pair values described above. Source-template9C belongs to D_SINGLE_900; real INSERT C3 occurrence `cad2:b3f9cc45f992044d1ee25a292b0d6a29caaee33578ad0a8c66b2b75c1111b1a1` supplies placement. Its projected leaf shares9C GeometryId and binds A-DOOR / ACI3 / Continuous. This is ByLayer, not a fabricated reachable ByBlock example. Synthetic Red/Blue and nested tests supply missing controlled cases. Official file has17 root occurrences, no reachable nested INSERT/MINSERT.

## 41. Controlled bounds proof

Model query is **Partial**, bounds Min(-1300.225,-10100,0), Max(20700,8500,0). Includes factual annotation anchors; it is not a rendered-text/glyph bbox or complete spatial coverage claim. Independent direct-byte witnesses in `controlled-bounds-byte-witnesses.json`: DIMENSION E4 code11 pair3954 supplies minX; LINE103/105/107 code11 pairs4178/4203/4228 supply maxX; TEXT142 code20 pair4525 supplies minY; TEXT15D code20 pair4896 supplies maxY. All model Z extrema are0. Source/template-only points/SOLID are not inserted merely to enlarge model bounds. Independent analytic subset fixtures provide curve/placement proof; header-sentinel regression proves non-use.

## 42. Strict CadIrJson roundtrip

Official normalized and shared-style synthetic documents serialize→deserialize→serialize with **identical UTF8 bytes** through unchanged strict CadIrJson. Final CadDocument construction/reference validation succeeds; every entity/geometry/style/definition/occurrence/model/ledger/fact reference resolves. All earlier strict missing/unknown/version/enum/duplicate cases remain passing. No sidecar/unknown field or permissive codec change.

## 43. Normalized document SHA256

**04G normalized CadDocument digest**,2,233,701 bytes:

`8A5E078ADB9990C7127B7E626DEC1333071F481FA4A4B7F91CD17D23505F4F24`

This names the locally tested04G canonical document, **not** a whole-PHASE04 release digest or independently accepted golden. It was recorded after source-linked mathematical assertions, not automatically blessed as an oracle.

## 44. Determinism

Invariant,fr-FR,ko-KR compare exact normalized bytes, counts, geometry assertions, bounds and diagnostic order. Two additional fresh VSTest/testhost processes from distinct CWDs `process-a`/`process-b` repeat all three cultures. All9 normalized JSON files match the digest above; bounds bytes match. No machine path, timestamp or randomized collection ordering enters canonical output. Evidence: culture files, fresh-process TRXs/logs and `final-audit.json`.

## 45. Tests

**57 new passing cases** across assembly, bulge, primitive, analytic bounds, integration edges, evidence/scope and full-budget test classes, in five new files. `new-test-inventory.json` records actual stable IDs/names. Coverage includes root/template/leaf policy, independent leaf framing, style isolation, exact source primitives, signed bulge/closure/precision refusal, nonplanarity, annotations/attributes, paper-space, missing/ambiguous styles, profile/units refusal, segment/leaf/global/no-refund/diagnostic budgets, cancellation, strict normalized roundtrip, source immutability, official facts, bounds/header and culture/process evidence.

Development failures are preserved and distinguished: repeated linetype field roles initially collided; empty XrefPath initially rejected definitions; both fixed without parser/schema changes. The first full gate had1,304/1,306 passing because two additional old assembly-wide no-engine guards needed the same legitimate04G evolution; intermediate verified/ then had1,309/1,309. Further full boundary/near-full tests and explicit source-count/immutability assertions preceded the final **release/1,315** gate. No earlier result is substituted for final evidence.

## 46. Regression

Final unfiltered suite: **1,315 total /1,315 executed /1,315 passed /0 failed /0 skipped**. All **1,258 baseline stable IDs and exact names** are present and Passed, including PHASE00–03 and04A–F. No deleted/renamed/filtered test identity. Five phase-aware guard assertion changes retain their old test IDs and all other boundary checks. Evidence: `release/tests.trx`, `release-gate.log`, `final-audit.json`.

## 47. Builds / warnings

**Core Release/Rebuild PASS,0 errors/0 warnings. Full DtoB.sln Release/Rebuild PASS,0 errors.** Established VS2022 MSBuild/Roslyn/VSTest fallback, SDK files8.0.416, installed Revit references `D:\프로그램\Revit 2025`; no SDK/security/global settings changes. Standard dotnet discovery still prints its existing no-SDK banner; actual fallback exits and test execution establish success.

Only approved MSB3277 conflicts: Microsoft.VisualBasic10.0/10.1 and System.Drawing4.0/8.0 (b03f5f7f11d50a3a), WindowsBase4.0/8.0 (31bf3856ad364e35), Culture=neutral. Exact pairs checked; no new warning identity. Reproduction:

```powershell
$ev = Join-Path (Get-Location) 'artifacts/phase04/04g-implementation-20261016/release'
foreach ($stage in '04G','04F','04E','04D','04C','04B','04A','03G','03H') {
    [Environment]::SetEnvironmentVariable("DTOB_${stage}_EVIDENCE_DIRECTORY", $ev, 'Process')
}
./scripts/Verify-Phase00Foundation.ps1 -Toolchain VisualStudio `
    -RevitInstallDir 'D:\프로그램\Revit 2025' -EvidenceDirectory $ev
```

The script actually restores/Rebuilds Core/full Release and runs unfiltered tests. Logs: `release/core-build.log`, `release/build.log`, `release/tests.log`. No IPC/Revit behavior changed; no Revit host run was required or claimed.

## 48. Raw / Dxf zero mutation

Raw/Dxf production bytes unchanged; no source re-opening, relexing, mappings, parser defaults or LOW-01 relaxation. All86 original fixture hashes match their manifest. Controlled and synthetic source snapshots remain serialization-identical before/after normalization. Large boundary tests use existing injectable reader limits only in test construction, without production cap changes or fake published Raw views.

## 49.04A–F evidence stability

Final full run regenerated and exactly matched:

| Evidence | SHA256 |
| --- | --- |
|04A all cultures | `3E49A200C8AD688F6F53842F17556D849979B1C8F3F6A083438721CA02F58EBC` |
|04B | `568D2A7C28863360A4839FF0CA59DF09C1E7C9066EC3A487701872506A68D4C3` |
|04C | `81CA2010F120BD65888EF57AABE5A2F55E230DDC291276E1CBA8BAC9584F0342` |
|04D | `02045C202CACF9DA66406827E297435CCCB7448D1D0A38C7B52E7FCAAA3A0EE5` |
|04E | `143684A78B37AA4962345A28FC8C268E24CB4ECDCDF2EBD410B66EC99A1B626B` |
|04F | `7A448C95EC2F67DCA6C65C3D60B4982E79312814C8C1FD8E5A62C55C611FFC01` |

Frozen CAD2/codec and all existing production Scalars/Coordinates/Placement/Occurrences/Styles bytes match before.json. Accepted ADRs, independent reviews and prior reports are unchanged. Only the five explicitly identified old test guards evolve.

## 50. No recognition / Revit leakage

No wall/door/window/column/room/story/grid/building classification, BIM semantics, confidence fabrication or AI/fuzzy cleanup. Two lines remain lines. No Revit API/types/internal feet, vendor/parser dependency, WPF/ViewModel, drawing-file I/O or background worker. Assembly scope and dependency guards pass; Core/Geometry/Analysis/Desktop/BIM/Revit sources are hash-protected and unchanged.

## 51. Explicit04H / PHASE05 exclusions

No04H whole-phase QA/release campaign, final release golden/manifest migration, new viewer, Analysis-v2 migration, recognition or PHASE05. Local04G process/culture evidence is the expressly requested04G validation. No commit, push, PR, tag or branch switch.

## 52. Known limitations

Partial official coverage is intentional: original52 unsupported records, deferred lineweight/width/color-book/transparency channels, unsupported attribute instancing and annotation layout/glyph extents. Official source cannot prove reachable nested/MINSERT/ByBlock/RGB/circle behavior; independent synthetic cases do. Unused dimension graphic blocks remain source catalog data, not fabricated occurrences. Definition bounds are direct-only, model bounds with annotation anchors are Partial.

Only existing04B five-unit decisions and04C required-XYZ/normal policy are supported; unresolved units refuse. Caller supplies numerical profile; no automatic unit selection/default policy added. IEEE finite/intermediate/endpoint precision limits can refuse affected geometry; no snapping or fallback. Frozen final structural validation is synchronous and bracketed by cancellation checks; no latency guarantee. Existing SDK discovery limitation, approved Revit reference warnings and LOW-01 remain. No known failing local test is hidden as a limitation.

## 53. Git state / self-review

Branch/HEAD unchanged, staged files0, no commit/push. Reviewed all new source/test files, protected-byte inventory and tracked/untracked deltas against the462-file starting manifest. Self-review led to duplicate-layer rejection, numeric profile traceability, endpoint-precision refusal, paper-space filtering, deferred-source ledger states and exact full-budget integration tests before final evidence.

`git diff --check`, strict UTF8/trailing-whitespace scans including untracked additions,86 fixture hashes, baseline IDs/names/outcomes, fixed prior digests and fresh-process bytes pass. Existing LF→CRLF Git advisories are informational. `Audit-04G.ps1`, `final-audit.json`, `git-status.txt`, `all-untracked-inventory.txt` and `new-test-inventory.json` record the reviewable delta. Previous uncommitted/untracked work is preserved; ordinary Git diff alone is not claimed to inventory it.

## 54. Next independent review gate

Local04G implementation/build/regression/controlled-data/strict-codec/determinism/resource evidence is complete with the explicit limitations above. **Ready for independent Antigravity04G implementation review**, not an independent implementation PASS. Return this new report with source/tests/evidence. Stop here; no04H/PHASE05, commit or push.

PHASE_04G_PASS_WITH_KNOWN_LIMITATIONS
