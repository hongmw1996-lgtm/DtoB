# PHASE 04E — Nested INSERT / MINSERT occurrence expansion

Date: 2026-10-16. Implementation owner: Codex. Status: **PHASE_04E_PASS_WITH_KNOWN_LIMITATIONS** — local implementation readiness only; independent Antigravity review remains required.

## 1. Scope

Implemented only root/nested INSERT and MINSERT occurrence expansion under accepted ADR-018/019/020. The public entry point is `CadOccurrenceExpander.Expand(snapshot, options, numericPolicy, cancellationToken)`. It is synchronous, pure in-memory CAD code; the caller owns scheduling. It returns an occurrence-expansion artifact, never a normalized CadDocument. No PHASE 04F or later work was started.

## 2. Baseline

Branch: `phase/04-cad-normalization`. HEAD remains `76ff6e0d8ccac797ba60d8c5657922c796a0e8c9`. Independent [04D review](../../reviews/PHASE_04D_REVIEW.md): **PHASE_04D_PASS**, BLOCKER/HIGH/MEDIUM/LOW all zero. Regression baseline: **1,128/1,128** from `artifacts/phase04/04d-20261016/final/tests.trx`.

The owner instruction, accepted ADRs, plan, relevant contracts, reviews and preceding implementation reports were inspected before implementation. The pre-existing working tree includes uncommitted/untracked 04A–04D work; it was preserved. `artifacts/phase04/04e-20261016/before.json` records the SHA-256 of all 436 Git-visible pre-existing files.

## 3. Files changed

Three pre-existing files changed relative to the 04E start snapshot:

| File | Change |
| --- | --- |
| `packages/DtoB.Cad.Normalize/Placement/CadInsertLocalTransform.cs` | Internal array-template entry point into the existing 04D implementation; ordinary public behavior remains unchanged |
| `docs/plans/PHASE_04_IMPLEMENTATION_PLAN.md` | Additive current 04E checkpoint; earlier checkpoints retained |
| `tasks/PHASE_04.md` | Additive current 04E checkpoint; earlier checkpoints retained |

Eleven new Git-visible files:

| File | Purpose |
| --- | --- |
| `packages/DtoB.Cad.Normalize/Occurrences/CadOccurrenceExpander.cs` | Iterative DFS, placement composition, cell derivation, cycles/budgets/cancellation |
| `packages/DtoB.Cad.Normalize/Occurrences/OccurrenceContracts.cs` | Owned immutable runtime catalog, occurrences, grids, diagnostics and results |
| `packages/DtoB.Cad.Normalize/Occurrences/OccurrenceIdentity.cs` | Existing cad2 framing applied to definition/source-INSERT/typed lineage |
| `tests/DtoB.Tests/Cad04EOccurrenceFixture.cs` | Synthetic DXF-to-Raw fixtures and point assertions |
| `tests/DtoB.Tests/CadNestedOccurrenceTests.cs` | Root/nested/shared-definition/cycle/orientation oracles |
| `tests/DtoB.Tests/CadMInsertExpansionTests.cs` | Cell ordering, matrix, units, spacing, scale and OCS oracles |
| `tests/DtoB.Tests/CadOccurrenceIdentityTests.cs` | Typed paths, independent byte framing, identity domains and source validation |
| `tests/DtoB.Tests/CadOccurrenceResourceTests.cs` | Depth, global/cell/diagnostic caps, cancellation, failure budgets and explosion |
| `tests/DtoB.Tests/CadOccurrenceProvenanceTests.cs` | Source evidence, ownership/immutability, invalid units and scope guards |
| `tests/DtoB.Tests/Cad04EDeterminismTests.cs` | Culture/process evidence and factual controlled-source integration |
| `docs/status/PHASE_04E_REPORT.md` | This report |

No project/dependency/solution changes were needed for 04E. Test fixtures use the real native DXF stages. The depth 33 test explicitly raises only the test reader's existing `MaxDefinitions` option to64; production PHASE 03 limits remain unchanged.

Local reproducible evidence and audit tooling are under `artifacts/phase04/04e-20261016/` (ignored build artifacts). The complete untracked inventory, including preserved earlier work, is in `all-untracked-inventory.txt`; the 04E-only delta is in `final-audit.json`.

## 4. Definition / occurrence model

`CadOccurrenceDefinition` is a shared runtime catalog entry: existing cad2 definition ID, source record reference, the 04D base/unit result, and ordered child source references. One instance is cached per referenced BLOCK-begin source record. Repeated placements share that exact instance; tests use reference equality to prove it.

`CadExpandedOccurrence.Value` reuses the unchanged 04A `CadBlockOccurrence`: real INSERT source, definition ID, parent occurrence ID, typed path, immutable local/accumulated transforms, and null StyleId. The wrapper adds depth, DFS ordinal, optional cell ordinal, local/accumulated orientation signs, source-INSERT ID, cached 04D placement evidence and grid facts. Parent references are IDs, not cyclic object graphs. No full Raw graph or cloned definition geometry is published.

This is a runtime occurrence catalog, not the 04G CadDocument definition/entity registry. Its structural source registry intentionally includes every Raw structural row in source order, so a SourceRecordRef index resolves the matching structural row. Fact-level evidence stays in owned 04B/C/D/grid results; the registry's fact list is empty at this stage.

## 5. Root occurrence semantics

ENTITIES-scope INSERT rows are visited in source order. Each ordinary root has depth 1, no parent ID and one ordinary typed step. Its accumulated matrix equals its local matrix (`Identity * Lroot`). Array roots have one occurrence per cell, with no extra array-container occurrence. No synthetic model-space source handle or DXF entity is invented.

## 6. Nested transform composition

For each admitted occurrence, referenced definition children are inspected in source order. INSERT children reuse 04D placement. `Mchild = Mparent * Lchild` maps a child definition point through its immediate parent and then to model space. DFS is implemented with a stack of iterators, avoiding CLR recursive calls. Shared definitions are reusable in separate active branches.

## 7. Actual Then / composition derivation

Existing `Transform3` uses column-vector affine matrices and `inner.Then(outer) = outer * inner`. Therefore the implemented accumulated composition is:

```text
local.ToTransform3().Then(parent.AccumulatedTransform.ToTransform3())
= Mparent * Lchild
```

The two-level test freezes `(100,12,0)` and separately proves reversed order gives `(210,1,0)`. The expected coordinates are hand calculations, not generated by the expander.

## 8. Occurrence path

Unchanged 04A `CadOccurrencePath` / `CadOccurrenceStep` provide the authoritative immutable structure. Every step contains source INSERT and definition references plus either both null cell indices (ordinary INSERT) or both nonnegative row/column indices (cell). Nested paths are outer-to-inner.

Tests freeze `A→D(cell0,0)` and `A→D(cell0,1)`, reject half-present indices, prove defensive copying/read-only collections, and distinguish ordinary INSERT from cell(0,0). Handles are resolved through the owned source registry; display strings are never authoritative lineage.

## 9. cad2 occurrence identity

`CadOccurrenceIdentity` calls existing `CadId.Create`; no second hash format was introduced. The application tuple is frozen by the independent byte-writer test:

```text
Source/definition fields:
  DrawingId, RevisionId, FileHash, SourceToken
Occurrence fields:
  DrawingId, RevisionId, FileHash, innermost INSERT SourceToken,
  Tuple(outer-to-inner steps)
Step:
  Tuple(Text("insert" | "cell"), INSERT SourceToken, definition SourceToken,
        Null | Index(row), Null | Index(column))
SourceToken:
  Handle(Raw-validated canonical handle)
  or Tuple(Text("record"), Text(source domain), Index(record index))
```

The existing outer domain remains `dtob/cad2/id/v1` with `definition`, `source-insert` or `occurrence`. Existing tags, big-endian byte lengths/counts, lowercase GUID-N, uppercase file hash, canonical handle spelling and invariant integer text remain unchanged. The public ID is `cad2:` plus lowercase SHA-256.

The independent test manually writes all field tags, lengths, counts and payloads, hashes those bytes, then compares with production encoding. `A→BC` differs from `AB→C`; cell(1,23) differs from cell(12,3). The ID API accepts no matrix, world point, bounds, machine path, timestamp or random value. Holding authority/path constant while replacing transforms preserves identity. Original handle spelling `000a` is retained while canonical `A` alone enters identity.

Ambiguous/malformed/multiple source-handle identities are refused before publication. The real PHASE 03 assembler already refuses duplicate required handles; a deliberately invalid owned Raw view tests this additional boundary defense. Handleless locator encoding is contract-tested, not a claim that required-handle-invalid DXF passes PHASE 03.

## 10. Source-handle policy

All cells retain the one real INSERT source reference, OriginalHandle and canonical handle. Their distinct identities come from typed row/column lineage. Tests prove every cell of source `A` still resolves to handle `A`, and lowercase source `d` retains OriginalHandle `d` / canonical `D`. No generated cell handle exists.

## 11. MINSERT source mapping

Actual RawInsert Rows(group71), Columns(70), RowSpacing(45), ColumnSpacing(44), insertion, scale, rotation, target and extrusion are consumed. Genuine absent counts resolve to1; absent spacing resolves to0, with explicit default rule IDs and original absence evidence retained. Present invalid/nonpositive counts reject only that branch. Spacing uses existing 04B length conversion once, not a new unit table.

04D intentionally refuses arrays through public `Create`. That prevented reuse of its placement path for MINSERT. The narrowly scoped internal `CreateArrayTemplate` delegates to the same private core, allowing validated positive array counts and the Raw array-only unsupported disposition. Public ordinary refusal is unchanged and regression-tested. BLOCK/base/units/scale/rotation/AAA/anchor/local matrix mathematics were not rewritten; no fake INSERT, fact mutation or duplicated transform implementation was introduced. This is the one justified 04D source modification, not a claim that all 04D source bytes are unchanged.

Array ATTRIB instancing remains explicitly unsupported, diagnosed `UnsupportedArrayAttributes`. PHASE 03's historical MINSERT unsupported-expansion evidence is retained on the input.

## 12. MINSERT row-major ordering

Iteration is row outer, column inner, zero-based. Cell ordinal is `r * Columns + c`. A2×3 fixture freezes `(0,0),(0,1),(0,2),(1,0),(1,1),(1,2)` and ordinals 0–5. For each cell all source-order descendants are traversed before advancing to the next cell. A nested ordinary→array→ordinary fixture freezes handles `A,D,E,D,E`.

## 13. MINSERT spacing transform derivation

Let `A` be the accepted AAA basis, `i` the source INSERT OCS point converted to mm, `b` the BLOCK base in mm, `R` the rotation, `S` the source scale and `d=(c*columnSpacingMm,r*rowSpacingMm,0)`. 04C provides `anchorWcs=A*i`; 04D provides:

```text
Lzero = T(anchorWcs) * A * R * S * T(-b)
Lrc   = T(anchorWcs) * A * R * T(d) * S * T(-b)
      = T(A*R*d) * Lzero
```

The implementation transforms only the displacement by `R`, then uses the existing frame's `VectorToWcs` for `A`. It composes `Lzero.Then(T(A*R*d))`. Translation commutation above is valid for the displacement under the linear `A*R`; `S` is deliberately outside that displacement transform. Thus the existing local matrix is reused without double-applying `A`, insertion, base or units. Cell(0,0) reuses Lzero directly.

The accepted ADR is authoritative. Official Autodesk [INSERT group codes](https://help.autodesk.com/cloudhelp/2024/ENU/AutoCAD-DXF/files/GUID-28FA4CFB-9D5E-4880-9F11-36C97578252F.htm) and [MINSERT array rotation behavior](https://help.autodesk.com/cloudhelp/2016/ENU/AutoCAD-MAC-Core/files/GUID-A780A2FA-4A2E-4574-950F-E788AB71F527.htm) inform the source mapping/rotation interpretation; full scale-order correctness is proven against the accepted ADR equation and independent fixtures, not inferred from a vendor renderer. The MINSERT page was fetched for inspection into `spec-MINSERT.txt`.

## 14. Scale / spacing separation

With columnSpacing 100 and scaleX2, cell1 origin displacement is100 at rotation0 and `(0,100,0)` at90°, rather than200. Nonuniform(2,3,1), mirrored(-2,3,1), signed negative row spacing and coincident zero-spacing cells have independent expected coordinates. Block geometry still receives S. An ancestor's scale subsequently affects the entire child placement/grid when `Mparent * Lrc` is composed; it does not justify applying the child INSERT's S to its own spacing.

The centimeter/nonzero-base fixture verifies mm conversion once: source base(10,20), insertion(100,200), columnSpacing 100 and S(2,3,1) map canonical base point(100,200) in cell1 to(2000,2000).

## 15. MINSERT tilted OCS behavior

The +Y extrusion fixture uses accepted `Ax=(-1,0,0), Ay=(0,0,1), N=(0,1,0)`. The R90/nonzero insertion/nonuniform scale cell(1,1) maps q(1,1,0) to **(-797,0,2102)**. This detects missing/double A, a displacement applied in WCS prematurely, and own-scale spacing multiplication.

## 16. Local vs accumulated transform

Local maps referenced definition coordinates to immediate parent coordinates. Accumulated maps them to model space. Both are immutable `CadTransform` values; published Values collections reject edits and `ToTransform3()` returns a copy. For a child cell with local translation10 under parent translation100, the independent provenance test freezes local10 and accumulated110. No source definition geometry is transformed or materialized.

## 17. Orientation propagation

The local sign comes from the unchanged 04D source-scale parity. Accumulated sign is exactly `parentSign * localSign`; root parent sign is+1. Tests cover all four ± sign combinations, including parent X mirror plus child Y mirror producing+1. Rotation/AAA are treated as proper frames. No determinant magnitude, geometry winding correction or curve conversion is used.

## 18. Active-path cycle detection

Before allocation, the candidate's definition source is checked against its actual ancestor definition references in the typed path. Direct and three-definition indirect cycles produce `Cycle` diagnostics containing the attempted path and Partial status. A cycle reached through two array cells records distinct column paths. Reuse under separate roots is allowed and shares one definition instance. No global visited-definition suppression exists.

## 19. Depth policy

The default is32 occurrence steps, root depth 1, one step per ordinary INSERT or cell. Depth32 passes with32 allocations. The depth 33 attempt returns `ResourceLimitExceeded`, requested 33/limit 32, no expansion artifact. The iterative traversal prevents managed-call-stack recursion. The test's explicitly increased upstream definition capacity is fixture setup only, not a parser change.

## 20. Single-MINSERT resource cap

The checked widened `long` product of validated int Rows×Columns is tested before the cell loop or allocation. Default cap 10,000:100×100 is allowed;100×101 is refused with zero allocations from that source. `int.MaxValue²` is safely represented as4,611,686,014,132,420,609 and refused, rather than overflowing a32-bit product. An oversized array after a valid root still aborts publication of the whole expansion; it does not publish a first-N prefix.

## 21. Session-global occurrence budget

Exactly one `allocated` counter belongs to one Expand call. Before each occurrence's derived state/object construction, the next slot must fit MaxOccurrences(default 100,000). Roots, ordinary nested INSERTs and every array cell use that same counter. Templates/shared definitions do not create container occurrences. The counter is returned as `TotalOccurrencesAllocated` even on refusal.

Tests freeze99,999 and100,000 as successful full artifacts, and request 100,001 as `ResourceLimitExceeded`, allocated 100,000, requested 100,001, limit 100,000 and null artifact. Multiple top-level sources in the same session prove it does not reset between roots.

## 22. Monotonic / no-refund proof

The reservation counter increments once before cell derivation/accumulated composition and never decrements. A failed post-reservation transform conservatively consumes its slot; it does not permit allocation churn to bypass the cap. Successful slots correspond one-to-one with occurrences. Separate Expand calls start fresh counters.

The independent overflow fixture has parent scale 1e200 and child scale 1e200. Both local matrices are finite; their accumulated product is not. With total limit 3: root succeeds, child reserves then fails, later root succeeds; returned allocated 3 but published occurrences2, Partial. With limit 2: the later root's request 3 is refused globally, with no artifact. A spacing-overflow cell similarly retains its consumed slot and allows the next valid sibling when budget remains. Architecture checks prohibit reset/refund assignments.

## 23. Cancellation

Cancellation checks exist at entry, source registry and definition inventory loops, root/child traversal, source candidate lookup, placement lookup, each row and column, admission before reservation, definition child-reference copying and final artifact construction/publication. All iterators are disposed in finally. Cancellation returns `Cancelled` with a terminal diagnostic and null artifact, never Complete/Partial.

Tests cover pre-cancel and cancellation requested from a concurrent caller. `final/occurrence-cancellation-evidence.json` records the final run: **2,607 reservations**, **Cancelled**, **ArtifactPublished=false**, one terminal diagnostic. This observed nonzero progress proves cancellation after prior internal allocations in this environment. A10ms scheduling request is used to exercise concurrent cancellation; no fixed throughput or response-time assertion is claimed, and the reservation count is scheduling-dependent. It is excluded from the fixed deterministic expansion digest.

## 24. Branch-local vs global failure policy

| Condition | Result / publication |
| --- | --- |
| Supported occurrence traversal with no expansion diagnostics | Complete occurrence artifact |
| Missing/ambiguous target or other rejected 04D profile, invalid grid facts, unsupported array attributes, cycle, nonfinite derived cell/accumulated matrix | Partial; retain valid ancestors/prior cells/independent siblings; diagnose rejected branch |
| Depth, per-array cells, global occurrences or diagnostics exhaustion | ResourceLimitExceeded; no expansion artifact |
| Cancellation | Cancelled; no expansion artifact |
| Invalid source authority, source identity/structure or unresolved header units | InvalidInput; no expansion artifact |

An ordinary diagnostic can consume only MaxDiagnostics−1 slots. The next one uses the reserved terminal slot to return resource exhaustion; no silent successful truncation. Limits1,3 and5,000 are tested, with exactly the configured number of diagnostics and null artifacts. `Complete` describes only INSERT occurrence traversal, not whole Raw coverage or a complete normalized CAD document.

## 25. Provenance / diagnostics

The owned catalog retains source DrawingId/RevisionId/FileHash; record domain/index; exact pair and physical-line ranges; OriginalHandle/canonical handle. Occurrences retain every parent/definition/INSERT/cell step, target name, source scale, rotation, frame/anchor, base/unit decisions, row/column count facts, spacing facts and default rules. Source order is explicit via DFS ordinal; CellOrdinal is separate from DFS ordinal.

Diagnostics contain code, source record reference, attempted/parent typed path, fixed detail key, requested/limit where applicable and immutable upstream 04D placement diagnostics with fact/pair evidence. For placement rejection before a candidate exists, Source identifies the rejected row and Path is its known parent lineage. On global refusal the artifact/registry is not published; the caller still has the original snapshot whose structural index and document authority resolve those diagnostic references. No Raw graph is copied into diagnostics and no fabricated source facts are added.

## 26. Analytic nested hand oracles

Two levels: B q(1,0,0) → S(2,1,1)=(2,0,0) → +(10,0,0)=(12,0,0) → parent R90=(0,12,0) → +(100,0,0)=**(100,12,0)**. Wrong Lchild×Mparent gives **(210,1,0)**.

Three levels: C q(1,2,0) → mirrorX +translation(0,5,0)=(-1,7,0); B scale(2,3,1)+translation(10,0,0)=(8,21,0); A R90+translation(100,0,0)=**(79,8,0)**. These expected points are literals derived by hand, with the accepted numeric policy used only for comparison tolerance.

## 27. MINSERT hand / full-matrix oracle

For base0, insertion(1000,2000,0), S(2,3,1), R90, column 100,row 200, cell(1,1), A=I:

```text
q=(1,1,0) -> S=(2,3,0) -> +d=(102,203,0)
-> R90=(-203,102,0) -> +i=(797,2102,0)

Independent expected row-major matrix:
[ 0 -3  0  800 ]
[ 2  0  0 2100 ]
[ 0  0  1    0 ]
[ 0  0  0    1 ]
```

All16 coefficients are frozen independently and compared using the existing policy coefficient tolerance. No expected matrix is generated by production composition.

## 28. Resource-explosion proof

`MinsertResourceExplosionTests` uses a10×10 root array, each cell referencing a definition containing a100×100 child array. Every individual array is within10,000. The session requests more than100,000 occurrences. It stops at allocated 100,000, next request 100,001, ResourceLimitExceeded and no artifact. The offending typed path identifies root cell(row 0,column 9) and a child cell at depth 2. The executable test completes without OOM; its final run took786ms on this host, which is observational evidence rather than a performance threshold.

## 29. Controlled-source occurrence proof

Official source: `datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf`; unchanged SHA-256 `2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC`.

Independent group-pair inventory in `controlled-byte-inserts.json` and the actual Raw integration agree: **25 INSERT records;17 ENTITIES roots;8 BLOCK-scope INSERTs;0 arrays**. The occurrence result is **17 roots,0 reachable nested occurrences,3 shared referenced definitions**: COLUMN_400, D_SINGLE_900 and W_SINGLE_1200. Root source-order handles are:

```text
B9 BB BD BF C3 C7 CA CC CE 109 10B 10D 10F 113 117 119 11B
```

Example real root C3 begins at pair 3760, targets D_SINGLE_900, insertion(4000,2550,0), R90/base0/unit scale 1. Independent q(1,0,0) gives **(4000,2551,0)**; integration test passes.

Actual nested source DB is structural row 130/pair 2971 under an anonymous DIMENSION graphics definition, targeting _ARCHTICK. Its peers DD, EA, EC,128,12A,137,139 are likewise BLOCK-scope INSERTs. Those parent graphics definitions are not reached from any ENTITIES INSERT; DIMENSION block references are not invented as root INSERTs. Thus **no real top-level→nested INSERT path or actual MINSERT can honestly be claimed for this file**. Full nested/array point/path proof uses the independent synthetic fixtures described above. Controlled evidence is in `final/controlled-occurrence-evidence.json`; the snapshot serialization before/after expansion is byte-equal.

## 30. Tests

**60 new executable cases**, including all required test classes:

| Class | Cases |
| --- | ---: |
| CadOccurrencePathTests | 3 |
| CadOccurrenceIdentityTests | 6 |
| CadNestedOccurrenceTests | 4 |
| CadMInsertExpansionTests | 9 |
| CadOccurrenceCycleTests | 4 |
| CadOccurrenceDepthTests | 2 |
| CadOccurrenceResourceTests | 19 |
| CadOccurrenceOrientationTests | 4 |
| CadOccurrenceProvenanceTests | 4 |
| Cad04EScopeArchitectureTests | 2 |
| Cad04EDeterminismTests | 1 |
| Cad04EControlledSourceTests | 1 |
| MinsertResourceExplosionTests | 1 |

`new-test-inventory.json` is generated from final TRX stable IDs. Initial test development found an upstream32-definition fixture cap, an unterminated test ATTRIB sequence and an overly large source-record test fixture. These fixtures were corrected without changing PHASE 03 production. The final invalid-source defense test intentionally constructs an invalid owned view because actual duplicate/missing required handles are already rejected upstream. Compiler/xUnit analyzer findings in new tests were fixed before the final gate.

## 31. Regression

Final complete unfiltered run: **1,188 total /1,188 executed /1,188 passed /0 failed /0 skipped**. All **1,128 baseline stable test IDs and names** remain present and passed. Baseline missing0; baseline failed0. This includes PHASE 00/01/02/03 and04A/B/C/D tests. Evidence: `final/tests.trx`, `gate.log`, `final-audit.json`.

Earlier 04E development runs (including the 1,186-case run before the two source-identity tests) are not the final result. The authoritative final TRX is the 1,188-case run after the defensive source-identity change and all final test fixes.

## 32. Builds / warnings

Core **Release/Rebuild PASS**,0 errors/0 compiler warnings. Complete **DtoB.sln Release/Rebuild PASS**,0 errors; existing approved MSB3277 identities only:

| Assembly | Conflicting versions | PublicKeyToken |
| --- | --- | --- |
| Microsoft.VisualBasic |10.0.0.0 /10.1.0.0 | b03f5f7f11d50a3a |
| System.Drawing |4.0.0.0 /8.0.0.0 | b03f5f7f11d50a3a |
| WindowsBase |4.0.0.0 /8.0.0.0 |31bf3856ad364e35 |

The existing foundation gate checks exact warning pairs. No new warning identity or package was accepted. Existing Visual Studio2022 MSBuild/VSTest fallback was used with SDK path8.0.416 and RevitInstallDir `D:\프로그램\Revit 2025`. SDK discovery still prints its existing no-SDK diagnostic text; the approved MSBuild path actually builds successfully. No SDK/security/environment installation changes were made.

Reproduction from repository root:

```powershell
$ev = Join-Path (Get-Location) 'artifacts/phase04/04e-20261016/final'
foreach ($stage in '04E','04D','04C','04B','04A','03G','03H') {
    [Environment]::SetEnvironmentVariable("DTOB_${stage}_EVIDENCE_DIRECTORY", $ev, 'Process')
}
./scripts/Verify-Phase00Foundation.ps1 -Toolchain VisualStudio `
    -RevitInstallDir 'D:\프로그램\Revit 2025' -EvidenceDirectory $ev
```

Actual gate uses `-restore -t:Rebuild -p:Configuration=Release -m:1`. Logs: `final/core-build.log`, `final/build.log`, `gate.log`, `final/tests.trx`. No Revit host behavior changed, so a host PING/PONG rerun was not required or claimed for this pure CAD change.

## 33. Determinism evidence

**04E occurrence-expansion evidence**,64,486 bytes:

```text
SHA256 143684A78B37AA4962345A28FC8C268E24CB4ECDCDF2EBD410B66EC99A1B626B
```

Invariant,fr-FR andko-KR each construct the same fixture and compare exact JSON bytes in-process. Two additional VSTest/testhost invocations, using different CWDs `artifacts/phase04/04e-20261016/process-a` and `process-b`, each rerun all three cultures and produce the same hash as `final/occurrence-expansion-evidence.json`. Both fresh tests passed. This was rerun after the last production change.

Projection includes document identity, options/policy, shared definition IDs/source/base/children, occurrence order/count/IDs/source-INSERT IDs/parent IDs, full typed path, real handle/locator, grid facts/row/col, local/accumulated16-coefficient matrices, both signs, depth, final reservation count and ordered diagnostics/upstream evidence. Fixed fixture yields18 occurrences and16 deterministic branch diagnostics. Cancellation scheduling, machine paths and times are excluded. This is **not a final PHASE 04 normalized CadDocument digest**.

## 34. Raw / Dxf zero mutation

No `packages/DtoB.Cad.Raw` or `packages/DtoB.Cad.Dxf` file changed relative to the 04E start snapshot. Source child relationships were sufficient; no PHASE 03 patch was needed. Synthetic and controlled before/after serialized snapshots match. All **86 original fixture file hashes** in the existing manifest still match. The invalid-view test constructs a separate test snapshot; it does not mutate an existing Raw graph or change reader production.

## 35. 04A / B / C / D stability

All expected hashes were regenerated by the final full test run and compared exactly:

| Evidence | SHA-256 |
| --- | --- |
|04A contract (each culture) | `3E49A200C8AD688F6F53842F17556D849979B1C8F3F6A083438721CA02F58EBC` |
|04B scalar | `568D2A7C28863360A4839FF0CA59DF09C1E7C9066EC3A487701872506A68D4C3` |
|04C coordinate | `81CA2010F120BD65888EF57AABE5A2F55E230DDC291276E1CBA8BAC9584F0342` |
|04D local transform | `02045C202CACF9DA66406827E297435CCCB7448D1D0A38C7B52E7FCAAA3A0EE5` |

04A/B/C source, existing tests, accepted ADR technical bodies, reviews, historical reports and all other baseline files remain byte-identical except the explicitly listed three changed files. The04D exception is solely the internal array-template dispatch/validation entry point; its original local matrix body, public behavior and fixed evidence remain stable. No schema major/convention/unit/identity framing change required a new ADR.

## 36. No Revit leakage

No Revit API/types, Autodesk/vendor dependencies, internal feet, WPF/ViewModel code, parser dependency, BIM or Analysis implementation is introduced. Architecture tests scan occurrence production for forbidden scope/types and check assembly references. Core/Geometry/Analysis/Revit source files are protected by the start-of-task hash audit and unchanged.

## 37. Explicit 04F+ exclusions

No Layer0/ByLayer/ByBlock/effective style resolution; StyleId stays null. No flattened/transformed definition geometry, bulge conversion, final affine circles/arcs, curve bounds, text/DIMENSION rendering or CadDocument assembly. Non-INSERT definition children remain ordered source references for later stages. No recognition, story inference, BIM generation, UI/viewer, DWG backend or PHASE05 work. No ICadNormalizer implementation is supplied; the existing later document-assembly boundary remains deferred.

## 38. Known limitations

This stage expands supported INSERT structure only. It does not certify complete drawing normalization. Array attributes, external-reference definitions, unsupported04D profiles and invalid grid facts remain explicit branch failures. Unresolved header units refuse the session; this entry point does not add a new explicit-unit configuration surface. Diagnostic references on refused output resolve against the caller's input snapshot because no catalog artifact is published.

The official controlled file has no reachable nested INSERT path or MINSERT; synthetic analytic fixtures supply those gates. Original PHASE 03 production caps may reject very large source inventories before normalization; isolated normalization depth/budget tests use bounded test setup rather than raising parser production limits. Development normalization limits remain injectable through the accepted options contract.

MaxDerivedGeometry250,000 remains configured, but04E allocates no derived geometry and makes **no geometry-budget enforcement claim**. Definition child catalogs contain references, not normalized entity/geometry lists. CadDocument assembly, downstream integration and any remaining semantic/style/geometry diagnostics belong to later authorization.

Failure cases tested include direct/indirect/cell cycles, missing target, invalid/nonpositive counts, unsupported array attributes, matrix/cell overflow, depth 33, oversized arrays, global100,001, diagnostic exhaustion, cancellation, invalid units and deliberately invalid source identity. Valid siblings/ancestors survive branch-local failures; global failures never publish a prefix. No unresolved defect is being hidden as a successful coverage claim. Independent Antigravity04E review is still pending.

## 39. Git state

Self-review covered all new production/tests, the small04D dispatch seam, docs and all Git-visible deltas against `before.json`. Resource reservations, `Then` order, identity framing, defensive copies, source graph ownership, cancellation and source-order iteration were checked. The extra source-validity refusal was added during this review and the full gate rerun afterward.

`git diff --check` passes. Its existing LF→CRLF advisories for previously edited tracked files are informational, not whitespace errors. A separate strict UTF-8/trailing-whitespace scan covers all 04E changed and added files, including untracked source that normal Git diff does not show. `Audit-04E.ps1` verifies all 436 pre-existing file hashes with only the three explicit change exceptions,86 fixtures, stable test IDs, warning identities, prior digests and fresh-process hashes.

All **86 untracked files** are explicitly inventoried in `all-untracked-inventory.txt`; only the 11 files in section 3 were added by 04E. Other untracked work predates04E and is preserved. Branch/HEAD unchanged, staged files0, no commit/push/branch switch. Final evidence summary: `artifacts/phase04/04e-20261016/final-audit.json`.

## 40. Next independent review gate

Ready for independent **Antigravity PHASE 04E implementation review**, with code, independent oracles, controlled-source facts, complete regression/build evidence and limitations above. No independent PASS is claimed. Stop here. Do not implement04F or later; do not commit or push without further owner authorization.

PHASE_04E_PASS_WITH_KNOWN_LIMITATIONS
