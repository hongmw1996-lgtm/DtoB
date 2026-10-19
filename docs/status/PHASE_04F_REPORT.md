# PHASE 04F — Occurrence style / layer inheritance

Date: 2026-10-16. Implementation owner: Codex. Local status: **PHASE_04F_PASS_WITH_KNOWN_LIMITATIONS**. Independent Antigravity implementation review is pending; this is not an independent PASS.

## 1. Scope

Implemented a pure in-memory intermediate effective-style resolver for existing source entities and published 04E occurrences. Supported effective channels are Layer, ACI/TrueColor and Linetype name/catalog reference. No spatial geometry is transformed, materialized or assembled. No 04G or later work, commit or push.

Entry point: `CadStyleResolver.Resolve(RawDrawingSnapshot, CadOccurrenceExpansionResult, CadStyleLimits?, CancellationToken)`. The caller owns scheduling. The resolver does not expand occurrences or read files.

## 2. Baseline

Independent [04E review](../../reviews/PHASE_04E_REVIEW.md): **PHASE_04E_PASS**, zero BLOCKER/HIGH/MEDIUM/LOW findings. Baseline: **1,188/1,188**, `artifacts/phase04/04e-20261016/final/tests.trx`. Branch remains `phase/04-cad-normalization`, HEAD `76ff6e0d8ccac797ba60d8c5657922c796a0e8c9`.

The owner's authoritative corrections govern: root occurrence depth is **1**, depth 32 passes and attempted33 is refused; geometry transformation/materialization and CadDocument assembly belong to **04G**. Contrary wording/API examples in the independent review were not treated as code contracts. The review itself is untouched.

Accepted ADR-018/019/020, implementation plan/task, prior relevant contracts/reports/reviews and actual Raw/Dxf/04E source were inspected. Before implementation, all 448 pre-existing Git-visible files were hashed into `artifacts/phase04/04f-20261016/before.json`. The existing uncommitted/untracked work is preserved.

## 3. Files changed

Two existing documents changed additively: `docs/plans/PHASE_04_IMPLEMENTATION_PLAN.md` and `tasks/PHASE_04.md`. Earlier checkpoints remain history. No pre-existing production/test source was modified.

Eleven new Git-visible files:

| File | Purpose |
| --- | --- |
| `docs/plans/PHASE_04F_SCOPE.md` | Pre-implementation Raw/spec channel matrix and bounded technical decisions |
| `packages/DtoB.Cad.Normalize/Styles/StyleContracts.cs` | Immutable source facts, declarations, channel results, keys, catalogs, limits and result/artifact |
| `packages/DtoB.Cad.Normalize/Styles/RawStyleAccess.cs` | Context-filtered retained-pair accessor |
| `packages/DtoB.Cad.Normalize/Styles/CadStyleResolver.cs` | Catalog lookup, cached INSERT context and channel resolution |
| `tests/DtoB.Tests/Cad04FStyleFixture.cs` | Actual native DXF pipeline fixtures; no fake parser/backend |
| `tests/DtoB.Tests/CadLayerResolutionTests.cs` | Layer/Layer0/ByLayer/ByBlock independent oracles |
| `tests/DtoB.Tests/CadEffectiveStyleChannelTests.cs` | Color, TrueColor, Linetype, visibility and explicit deferred states |
| `tests/DtoB.Tests/CadStyleProvenanceTests.cs` | Shared-definition isolation, provenance, immutability, input/resource/scope checks |
| `tests/DtoB.Tests/CadStyleCancellationTests.cs` | Concurrent cancellation after actual style work |
| `tests/DtoB.Tests/Cad04FDeterminismTests.cs` | Culture/process/CWD evidence and factual controlled-source proof |
| `docs/status/PHASE_04F_REPORT.md` | This report |

No csproj, solution, dependency, ADR, historical report, independent review or fixture change. Build artifacts/audit tooling are ignored local evidence under `artifacts/phase04/04f-20261016/`.

## 4. Actual Raw style inventory

Geometry and annotation records expose a typed Layer source fact; unsupported profiles can have no typed mapping. Structural LAYER entries map Name2, AciColor62, TrueColor420, LinetypeName6, Flags70, Lineweight370 and related plotting fields. LTYPE entries map Name2 plus pattern metadata. INSERT/TEXT/MTEXT/geometry common color/linetype are not separate typed payload properties.

The published snapshot also retains immutable lexical pairs, structural record pair/physical-line spans and ContextMarkers identifying subclass, application/XDATA and control nesting. Accepted ADR-018 and plan section 15 expressly permit reading these retained contextual style facts inside Normalize. Thus entity6/62/420 and the deferred common fields are available without modifying Raw/Dxf or reopening/relexing source.

`RawStyleAccess` scans each relevant record once. Common entity facts require genuine legacy pre-subclass context or `AcDbEntity`. Table facts require their appropriate symbol-table subclass; names may also be in `AcDbSymbolTableRecord`. Application/control/XDATA data is excluded. MTEXT-native background420 and unrelated subclass62/420 cannot become entity color. Duplicate eligible facts are Invalid, not first-wins. Text is preserved exactly; only integer parsing uses invariant numeric rules.

## 5. Final supported-channel matrix

The complete discovery matrix, including defaults, dependency and reasons, was written before production in [PHASE_04F_SCOPE.md](../plans/PHASE_04F_SCOPE.md).

| Channel | Raw/code | Default / modes | Dependency / final scope |
| --- | --- | --- | --- |
| Layer | typed Layer and contextual8; LAYER Name2 | Required name; absent remains Absent | Unique catalog lookup; Layer0 substitutes containing INSERT effective layer; supported |
| ACI | contextual entity62; LAYER 62 | Absent entity means ByLayer;256 ByLayer;0 ByBlock; indexed 1–255 | Effective layer or real parent INSERT; supported, with special values distinct from palette indices |
| TrueColor | contextual/entity and LAYER 420 | Valid24-bit integer; precedes simultaneous62 | Explicit RGB or inherited factual RGB; supported, no palette approximation |
| Linetype | entity6, LAYER 6, LTYPE 2 | Absent entity means ByLayer; BYLAYER/BYBLOCK are modes | Effective layer or parent INSERT, then unique LTYPE name; supported name/reference only |
| Lineweight | contextual entity370; typed/retained LAYER 370 | Special/default/width policy not resolved | Source evidence retained; present deferred state Unsupported; no width/pixel/Revit mapping |
| Transparency | retained contextual440 | Bit/mode/default policy not resolved | Source evidence retained; present deferred state Unsupported; no fabricated opacity |

Color-name430 is preserved and deferred for color-book fidelity. Source visibility 60 and layer off/frozen evidence are retained separately without deletion or combined rendering visibility. `Complete` concerns the supported effective channels, not every possible CAD property; an absent deferred source fact remains Absent, not a claim of a resolved width/opacity. A present deferred channel emits UnsupportedStyleChannel and contributes Partial.

## 6. Source entity style contract

`CadDeclaredStyle` is one shared owned declaration per source structural row. It retains source reference, layer, ACI, TrueColor, color name, linetype, lineweight, transparency and visibility facts. Each `CadStyleFact` retains group code, presence, validity, original scalar text, parsed integer when applicable, exact pair indices and physical lines. It holds no Raw graph/fact/pair objects.

Typed source color modes: Aci, Rgb, ByLayer, ByBlock, Absent, Invalid, Unsupported. Typed source linetype modes: Explicit, ByLayer, ByBlock, Absent, Invalid. Source absence remains different from an explicit mode/default. Visibility retains its factual/default rule. No source field is overwritten with a resolved value.

## 7. Occurrence style context

The resolver consumes the actual immutable 04E artifact, source registry, source INSERT, target definition child references, parent occurrence ID and DFS order. It validates source GUIDs/hash and every structural locator/handle against the paired snapshot. Refused or mismatched inputs publish no style artifact.

Each occurrence's INSERT style is resolved against its already cached real parent before direct child entities are projected. Cache key is the stable existing occurrence ID, with immutable values. No definition recursion, ancestry rescanning, new occurrence ID, transform calculation or mutable global/thread-static state.

Result lists are explicit: Roots contains ENTITIES non-INSERT source rows in source order; OccurrenceContexts follows 04E DFS exactly; Entities follows occurrence DFS, then direct non-INSERT definition-child source order. Nested INSERTs are represented by their actual separate 04E occurrence contexts, including array cells. Source declarations/catalogs remain in structural source order. No style-value sorting changes output order.

## 8. Layer resolution

An explicit layer name must match one unique LAYER entry. Lookup is `StringComparer.OrdinalIgnoreCase`; original source spelling is retained, while the effective name uses the unique catalog entry's spelling. No culture folding, silent trim or first duplicate wins. Missing/ambiguous/invalid names are diagnosed; an absent source layer remains Absent. No Layer0, missing catalog or display default is invented.

Independent tests cover explicit/root names, case-insensitive match, case collisions, leading-space distinction, missing Layer0, absent layer, and Turkish-culture i/I versus dotless-ı behavior.

## 9. Layer0 semantics

For a source entity inside a block occurrence, declared layer exactly `0` adopts the containing INSERT's **effective** layer. Nonzero child layers keep their own named layer. At root, layer 0 refers to the actual layer 0 catalog, with no fake parent.

This follows accepted plan section 15/ADR-020 and Autodesk [block property inheritance](https://help.autodesk.com/cloudhelp/2026/ENU/AutoCAD-LT-MAC/files/GUID-25E9F20C-D146-426C-8815-37DF48D2D33F.htm), whose guidance includes nested block components. The page was fetched into `spec-block-properties.html` when the search cache could not fetch that exact version. Source and expected values were verified before coding.

## 10. Nested Layer0 behavior

Hand oracle: root A on A-INST → nested D on0 → child E on0 yields A-INST for D and E. If D instead declares C-INST, E adopts C-INST. Each substitution uses the immediate resolved INSERT context; it is not a source-definition search for an arbitrary nonzero name. Independent tests freeze these literal effective names.

## 11. ByLayer semantics

Layer substitution precedes channel lookup. Color62=256 or genuine absent 62 resolves the effective LAYER's factual color; absent 6/BYLAYER resolves the effective LAYER's named linetype and corresponding LTYPE entry. Absent declarations produce Defaulted results with `DXF.Color.AbsentByLayer` / `DXF.Linetype.AbsentByLayer` rules; explicit ByLayer produces Inherited. No fallback to definition layer 0 after substitution.

Source/default meanings were checked against [Autodesk common entity codes](https://help.autodesk.com/cloudhelp/2015/ENU/AutoCAD-DXF/files/GUID-3610039E-27D1-4E23-B6D3-7E60B22BB5BD.htm). Missing/malformed catalog facts stay unsupported; no blind ACI 7 or CONTINUOUS repair.

## 12. ByBlock semantics

Color62=0 and linetype BYBLOCK use the containing INSERT's already resolved corresponding channel. This operates independently from the child layer, so a nonzero child layer cannot replace an explicitly inherited parent color. SupportingContext records the actual parent projection key.

At root, or when the parent channel itself remains unresolved, the channel is Unsupported with `UnresolvedByBlockContext`. This is the approved normalization policy, not an attempted emulation of renderer fallback. The resolver never fabricates a white color or CONTINUOUS linetype.

## 13. Nested ByBlock chain

Independent oracle: A explicit ACI 2/DASHED → D Layer0,ByBlock/ByBlock → E nonzero B-ENTITY,ByBlock/ByBlock gives E layer B-ENTITY but ACI 2/DASHED. The parent context must first resolve D; a one-level source-only resolver would fail.

Mixed oracle: A explicit ACI 1 → D on C-INST with ByLayer → E Layer0,ByBlock gives E layer C-INST and ACI 5, sourced from C-INST's layer color, not A's explicit color. Provenance traces E→D→A where applicable via typed causal keys.

## 14. Color model

The unchanged `DtoB.Cad.V2.CadColor` represents effective ACI or RGB as tagged factual values. Indexed ACI 1–255 is distinct from 0/256 modes. Entity negative/out-of-range/duplicate/malformed ACI is explicitly unsupported; signed negative LAYER ACI uses magnitude1–255 for color and separately preserves layer-off state. It is never an ordinary negative palette index.

No RGB approximation of ACI, UI pixel choice, Revit color/material or rendering API is used. Valid channels survive independent layer/linetype/deferred-channel failures.

## 15. TrueColor decision

Valid420 in 0..0xFFFFFF takes precedence over simultaneous explicit ACI, ByLayer or ByBlock62. RGB 0 is valid black. A malformed, duplicated or out-of-range present420 does not silently fall back to 62. Source62/420/430 evidence is retained even when one takes precedence.

Precedence was verified in Autodesk's [420/62 conflict documentation](https://help.autodesk.com/cloudhelp/2026/ENU/AutoCAD-AutoLISP-Reference/files/GUID-C7D27797-247E-49B9-937C-0D8C58F4C832.htm); no AutoLISP dependency exists. Tests cover each62 mode, zero, invalid bounds, layer RGB inheritance through ByBlock, control/XDATA/foreign-subclass collisions and actual MTEXT-native background420 exclusion.

## 16. Linetype decision

Resolve only the factual explicit name and unique LTYPE source record. Explicit names, ByLayer, ByBlock, absent defaults and nested propagation are distinct. Catalog absence/case collision and missing layer linetype remain explicit. Supporting evidence includes declaration, layer 6 when used and LTYPE Name2.

The [LAYER](https://help.autodesk.com/cloudhelp/2015/ENU/AutoCAD-DXF/files/GUID-D94802B0-8BE8-4AC9-8054-17197688AFDB.htm) and [LTYPE](https://help.autodesk.com/cloudhelp/2017/ENU/AutoCAD-DXF/files/GUID-F57A316C-94A2-416C-8280-191E34B182AC.htm) source roles were checked. Pattern/shape/text fields remain Raw evidence; no dash geometry, scale, font loading or fidelity claim. LTYPE entries do not acquire layer off/frozen semantics; those metadata values are null there.

## 17. Lineweight decision

Deferred. Raw retains entity/LAYER 370, but the accepted plan requires an explicit special/default policy before materializing a factual width. This stage preserves source integer/text/presence/validity/pairs and marks a present lineweight Unsupported, including catalog lineweights. No interpretation of ByLayer/ByBlock/Default into a numeric width and no pixels/Revit mapping. This is a scope decision, not a claim that Raw lacks370.

## 18. Unsupported / deferred channels

Transparency440 and color-book/name430 fidelity are deferred with explicit facts/diagnostics. Without valid 420, a present430 cannot create a guessed effective color. With valid 420, factual RGB is still known, while unsupported color-book fidelity remains diagnosed. Absent deferred source facts stay explicitly Absent.

Visibility60 absent defaults to visible with a documented rule;0/1 are retained, invalid values remain unknown/diagnosed. LAYER negative62 and flags 70 preserve off/frozen independently. No entity or geometry is deleted because of those facts. Combined viewport/render visibility, transparency inheritance, line patterns, fonts, plot styles/materials and width rendering remain outside this stage.

## 19. Layer-table lookup

Source-ordered catalog entries are indexed once into ordinal case-insensitive name→entry-list dictionaries. Invalid names cannot enter lookup; duplicates remain lists and produce ambiguity diagnostics containing supporting source-name facts. Unique layer references lead to channel facts through a record-index cache. There is no current-culture key or lookup-order first-wins behavior.

Retained-pair table access uses the appropriate subclass/control/application boundaries even when a typed structural field contains broader retained occurrences. The foreign-table subclass regression proves an unrelated62/420 cannot override the correct layer color.

## 20. Shared-definition occurrence isolation

Critical fixture: one shared BLOCK B contains E declared Layer0/ByBlock. Root A on A-INST explicit ACI 1 and root D on C-INST explicit ACI 5 yield A/E=A-INST,ACI 1 and D/E=C-INST,ACI 5. The same `CadDeclaredStyle` object, source reference and 04E shared definition are reused. E's declared layer remains0 and source ACI remains0. Effective values live only on separate `(source, occurrence)` projections.

Array cells likewise retain separate existing occurrence IDs while sharing the same source declaration. No effective style is written back to a shared definition or a 04E occurrence.

## 21. Effective-style contracts

`CadStyleEntityKey` is a typed source-record/optional occurrence-ID pair, not another hash or DXF handle. Root entities have null occurrence. Occurrence paths and authority resolve through the unchanged input artifact. No style-dependent identity policy is introduced.

`CadEffectiveStyle` combines the shared declaration with `CadStyleProperty<string>` Layer/Linetype and `CadStyleProperty<CadColor>` Color. Properties carry existing CadResolutionState, nullable value, invariant rule ID, owned source facts, supporting catalog record and supporting context key. State/value consistency is validated. Deferred-channel states expose source presence without inventing resolved values.

Artifact/result lists and facts are copied into read-only collections; public properties are get-only. Existing CadColor/resolution states are reused. New wrappers are intermediate runtime contracts, not changes to CAD-v2 schema/codec or its final style-ID catalog. No new ADR was needed: accepted ADR018/020 already govern these rules and boundary; no major schema, identity, coordinate or unit policy changed.

## 22. Partial resolution

Independent channels preserve valid information. A missing linetype can coexist with known layer/color; an invalid/missing layer does not erase explicit color. A present deferred property or channel diagnostic yields Partial, not a false complete result. Partial 04E coverage remains Partial even when all emitted styles resolve. Unreferenced definitions are preserved as source declarations/catalog information, not invented occurrences.

`Complete` is supported-channel traversal coverage, not whole-CAD-document normalization. Cancellation, invalid paired input or resource exhaustion returns no style artifact. Diagnostic limits include a reserved terminal slot; no successful truncated prefix is published.

## 23. Provenance / rule chain

Document DrawingId/RevisionId/FileHash is stored once via the unchanged 04E source registry. Each fact addresses a structural record and exact pair/physical lines. The registry preserves original/canonical handles. Each projection key identifies source entity plus real occurrence; the input provides its complete typed path.

Examples: explicit ACI→source 62; ByLayer→effective-layer decision→LAYER 62/420; Layer0→supporting INSERT layer projection; ByBlock→supporting INSERT color/linetype projection→its own rule/evidence. Supporting keys link immutable already-resolved projections, avoiding recursive object/Raw copies. Layer and LTYPE source facts remain independently identifiable in evidence.

The context cache computes each INSERT once. Resolution is linear in bounded retained input plus emitted projections with dictionary lookup; it does not repeatedly walk ancestry. No mutable global cache. Cancellation checks occur at validation, each source/catalog/root/occurrence/child loop, each projection and final publication.

## 24. Diagnostics

Invariant codes include MissingLayerDefinition, AmbiguousLayerDefinition, InvalidLayerReference, InvalidColor, InvalidLinetype, MissingLinetypeDefinition, AmbiguousLinetypeDefinition, UnresolvedByBlockContext, UnsupportedStyleChannel, InvalidVisibility, InvalidInput, Cancelled and ResourceLimitExceeded. Diagnostics carry typed projection/source keys, fixed channel identifiers and factual evidence; no localized exception message, machine path or timestamp enters evidence.

Order is deterministic: source-ordered catalog diagnostics, source-ordered root diagnostics, then 04E DFS context/direct-child diagnostics with fixed Layer→Color→Linetype→deferred/visibility ordering. A style cycle cannot be introduced: the algorithm only consumes a parent context that precedes the child in finite04E order, and never recurses through source definitions. A broken/mismatched parent/source relation is refused.

Validated injectable caps are250,000 total style projections and5,000 diagnostics including terminal slot. One counter covers roots/INSERT contexts/child projections; reserve before creation, no reset/refund. This is **not geometry-budget enforcement**. Small exact-boundary and diagnostic exhaustion tests prove global refusal behavior. Pre-cancel and concurrent-cancel tests pass; final concurrent evidence records **7,290** allocated style projections, Cancelled and no artifact. Scheduling count is observational and excluded from deterministic style digest.

## 25. Controlled-source factual inventory

Official source: `datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf`, SHA-256 `2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC`. No source bytes changed. Test source authority uses the same fixed caller GUIDs as existing scalar/coordinate/occurrence fixtures; DXF is not claimed to supply project GUIDs.

Actual result:158 shared source declarations,12 LAYER entries,21 LTYPE entries;97 root non-INSERT styles,17 INSERT contexts,26 reachable block-child projections = **140 total**. Root entity/INSERT layer inventory independently extracted from source pairs:

| Layer | ENTITIES source rows |
| --- | ---: |
| A-DIM |4 |
| A-DOOR |4 |
| A-FLOOR |5 |
| A-GRID |12 |
| A-LEVEL |10 |
| A-SECTION |6 |
| A-TEXT |26 |
| A-WALL |25 |
| A-WINDOW |14 |
| S-COLUMN |8 |

All114 ENTITIES source rows omit explicit common color/linetype, yielding ByLayer defaults. Reachable COLUMN_400/D_SINGLE_900/W_SINGLE_1200 children use nonzero S-COLUMN/A-DOOR/A-WINDOW layers, likewise ByLayer. There is no reachable Layer0/ByBlock/RGB inheritance demonstration in this file. Three ByBlock/Layer0 declarations in _ARCHTICK/_CLOSEDFILLED/_CLOSEDBLANK exist but are not reached from real root INSERTs. DIMENSION graphic references are not fabricated as occurrences. Missing cases use synthetic oracles.

The actual controlled style status is **Partial**, with 12 `UnsupportedStyleChannel` diagnostics for LAYER lineweight fields. This honestly preserves the deferred channel; the supported140 projections still have their factual effective values. Evidence: `controlled-byte-style-inventory.json`, `final/controlled-style-evidence.json`.

## 26. Controlled-source style proof

Reachable source LINE **9C**, structural row 120, starts pair 2856/physical line 5713, inside D_SINGLE_900 placed by root INSERT **C3**, row 199/pair 3760. C3's layer fact is pair 3764. The existing occurrence ID is `cad2:b3f9cc45f992044d1ee25a292b0d6a29caaee33578ad0a8c66b2b75c1111b1a1`.

9C declares A-DOOR at pair 2860/line 5721; common62/6 are absent. Its nonzero layer remains A-DOOR, not Layer0 substitution. Unique LAYER **8B**, row 40, supplies ACI 3 at pair 1164. LAYER linetype6 points to **Continuous**, unique LTYPE **26**, row 18. Effective result: A-DOOR, tagged ACI 3, Continuous. This is a factual ByLayer proof, not a claim that9C inherits an explicit C3 color through ByBlock. The test freezes literal handles/pairs/names/ACI and checks unchanged input snapshots before/after.

## 27. Analytic nested style oracles

Expected values are hand-authored from accepted and verified rules, never generated by the resolver: Layer0 chain A-INST→0→0 gives A-INST; nonzero B-ENTITY stays B-ENTITY; A explicit 2/DASHED→D ByBlock→E ByBlock gives2/DASHED; A red1/C-INST intermediate ByLayer→E ByBlock gives5 from C-INST; one shared source under red1/blue5 occurrences resolves separately.

TrueColor0 overrides62 values 1/0/256 without becoming absent. Signed layerACI−5 becomes indexed 5 plus IsOff=true; flags 1 preserves IsFrozen=true. Missing catalogs and root ByBlock never synthesize values. Comparison tests use no production style output to construct expectations.

## 28. Tests

**70 new cases**; exact inventory generated from final stable TRX IDs:

| Class | Cases |
| --- | ---: |
| CadLayerResolutionTests |12 |
| CadByLayerResolutionTests |3 |
| CadByBlockResolutionTests |4 |
| CadEffectiveColorTests |18 |
| CadEffectiveLinetypeTests |8 |
| CadStyleProvenanceTests |4 |
| CadStylePartialResolutionTests |8 |
| CadStyleOccurrenceIsolationTests |2 |
| CadStyleResourceTests |7 |
| CadStyleCancellationTests |1 |
| Cad04FScopeArchitectureTests |1 |
| Cad04FDeterminismTests |1 |
| Cad04FControlledSourceTests |1 |

Tests cover retained subclass/control/XDATA and MTEXT background collisions, absent/default/duplicate/invalid/special values, missing and ambiguous symbols, culture-insensitive comparison, exact causal references, shared source/definition identity, unchanged Raw/04E serialization, read-only collections/no Raw graph retention, resource limits/cancellation and forbidden scope/dependencies. Lineweight interpretation tests were not added because that channel is deferred; its explicit preservation/Unsupported state is tested.

## 29. Regression

Final complete unfiltered suite: **1,258 total /1,258 executed /1,258 passed /0 failed /0 skipped**. All **1,188 baseline stable test IDs and names** retained and passed. Missing0; baseline failed0. This includes PHASE 00–03 and04A–E regressions. Evidence: `final/tests.trx`, `gate.log`, `new-test-inventory.json`, `final-audit.json`.

Initial focused runs (47 channel cases,21 focused style/provenance cases and3 scope/evidence cases) were development checks, not the final regression. During self-review, off/frozen metadata was restricted to LAYER entries; LTYPE has no such semantics. Tests assert null LTYPE off/frozen. The full build/tests and two fresh-process evidence runs were repeated after that final source correction.

## 30. Builds / warnings

Core **Release/Rebuild PASS**,0 errors/0 compiler warnings. Full **DtoB.sln Release/Rebuild PASS**,0 errors, only the approved MSB3277 exact conflict pairs:

| Assembly | Versions | PublicKeyToken |
| --- | --- | --- |
| Microsoft.VisualBasic |10.0.0.0 /10.1.0.0 | b03f5f7f11d50a3a |
| System.Drawing |4.0.0.0 /8.0.0.0 | b03f5f7f11d50a3a |
| WindowsBase |4.0.0.0 /8.0.0.0 |31bf3856ad364e35 |

Existing Visual Studio2022 MSBuild/VSTest fallback, SDK path 8.0.416 and installed Revit reference directory `D:\프로그램\Revit 2025` were used. Existing SDK-discovery no-SDK text remains in output; actual approved MSBuild exits successfully. No SDK/security installation or setting changes. No new warning identity/package was accepted. Pure CAD source changes do not affect IPC/Revit integration; no Revit host verification is claimed or needed for 04F.

Reproduction from repository root:

```powershell
$ev = Join-Path (Get-Location) 'artifacts/phase04/04f-20261016/final'
foreach ($stage in '04F','04E','04D','04C','04B','04A','03G','03H') {
    [Environment]::SetEnvironmentVariable("DTOB_${stage}_EVIDENCE_DIRECTORY", $ev, 'Process')
}
./scripts/Verify-Phase00Foundation.ps1 -Toolchain VisualStudio `
    -RevitInstallDir 'D:\프로그램\Revit 2025' -EvidenceDirectory $ev
```

The existing gate runs `-restore -t:Rebuild -p:Configuration=Release -m:1`, checks exact warning identities, then executes unfiltered tests. Logs: `final/core-build.log`, `final/build.log`, `gate.log`, `final/tests.trx`.

## 31. Determinism

**04F effective-style evidence**,53,380 bytes:

```text
SHA256 7A448C95EC2F67DCA6C65C3D60B4982E79312814C8C1FD8E5A62C55C611FFC01
```

Invariant,fr-FR,ko-KR each construct the fixed fixture and compare exact serialized bytes. Two additional fresh VSTest/testhost invocations use distinct CWDs `artifacts/phase04/04f-20261016/process-a` and `process-b`, rerun all cultures and produce identical bytes/hash. All fresh tests passed after final source correction.

Projection includes source authority/record handles/locators, declarations/modes, effective values/states, source fact references, supporting catalogs/parent keys, limits/count, existing occurrence IDs/typed paths/parent IDs/depth and deterministic diagnostics/order. No timestamps, machine paths or localized messages. Concurrent cancellation progress is excluded because scheduling is not deterministic. This is **not a final normalized CadDocument digest**.

## 32. Raw / Dxf zero mutation

No Raw/Dxf production file changed. Source files were not reopened or reparsed by production normalization. All86 original fixture hashes match their manifest. Synthetic/controlled tests prove serialized Raw unchanged before/after resolution. Contextual access uses only existing retained evidence; no PHASE03 parser extension or unsupported-status rewrite occurred. Audit compares all 448 pre-existing Git-visible files with only the two additive document exceptions.

## 33. 04A / B / C / D / E stability

All expected evidence was regenerated in the final full run and compared exactly:

| Evidence | SHA-256 |
| --- | --- |
|04A contract, all three cultures | `3E49A200C8AD688F6F53842F17556D849979B1C8F3F6A083438721CA02F58EBC` |
|04B scalar | `568D2A7C28863360A4839FF0CA59DF09C1E7C9066EC3A487701872506A68D4C3` |
|04C coordinate | `81CA2010F120BD65888EF57AABE5A2F55E230DDC291276E1CBA8BAC9584F0342` |
|04D local transform | `02045C202CACF9DA66406827E297435CCCB7448D1D0A38C7B52E7FCAAA3A0EE5` |
|04E occurrence expansion | `143684A78B37AA4962345A28FC8C268E24CB4ECDCDF2EBD410B66EC99A1B626B` |

04A–E production/tests, accepted ADR technical bodies, independent reviews and historical reports are byte-identical to 04F start. Existing occurrence IDs, immutable transforms, source sharing, root depth 1 and all budget semantics are untouched. No style value enters an occurrence identity calculation.

## 34. No Revit leakage

Style production has no Revit/vendor/WPF/BIM/Analysis dependency, Autodesk type, ElementId, graphics/material/category mapping, internal feet or view rendering. Scope tests scan for forbidden types, transformation/occurrence-expansion calls, ID creation and document assembly; assembly-reference guards also pass. Core/Geometry/Analysis/Desktop/Revit source files remain protected by the baseline hash audit.

## 35. Explicit 04G+ exclusions

No Apply/Then/Transform3, spatial Point/Line/Arc/Circle/Solid creation, geometry flattening, curve/bulge conversion, bounds, text/DIMENSION rendering, final root CadDocument population, geometry/leaf/entity registry assembly or ICadNormalizer implementation. No recognition, BIM, Revit generation, viewer/UI, parser/backend changes or PHASE05 work. The output is only a factual style/context projection keyed to unchanged source/occurrence references.

## 36. Known limitations

Supported effective values are bounded to Layer, ACI/RGB and Linetype name/reference. Lineweight, transparency and color-book fidelity remain explicitly deferred; source absence for those channels does not assert an interpreted default. No viewport/material/plot/annotation rendering fidelity or combined visibility is claimed. Unsupported entity-specific styling beyond common facts remains in the paired Raw evidence.

The official controlled source does not exercise reachable Layer0/ByBlock/nested-style/RGB inheritance; independent synthetic fixtures supply those proofs. Its12 layer lineweight diagnostics make Partial status intentional. Unreferenced definitions are not fabricated as instances. Root ByBlock and missing catalogs stay unresolved under the accepted policy instead of emulating AutoCAD import/display repairs.

The intermediate artifact reuses the 04E structural source registry; owned style facts carry their own pair/line evidence. Final CadDocument fact-registry/style-ID integration is04G. On global refusal/cancellation, diagnostic source keys resolve against the caller's unchanged snapshot/input because no style artifact is published. The output has no new final persistence codec.

Style projection/diagnostic budgets can refuse very large materializations; repeated occurrence-specific diagnostics count toward the cap. They never masquerade as a successful prefix. Normalization adds no background monitor, I/O, file hashing or worker thread. Caller-controlled cancellation after observed nonzero work is verified, without a fixed performance assertion.

Failure cases tested: missing/ambiguous/absent layer; invalid/duplicate/out-of-range colors; malformed420 without fallback; root unresolved ByBlock; missing/colliding/duplicate/absent linetypes; deferred channels; invalid visibility; mismatched source authority; partial expansion; projection/diagnostic exhaustion and cancellation. Independent04F review remains the next gate; no known failing test is hidden as a limitation.

## 37. Git state

Self-review covered the three new production files, six test files, discovery matrix, current checkpoint additions and report. Scope/immutability/context/evidence/lookup/resource paths were inspected. The final metadata correction and its regression assertions were followed by complete Release/Rebuild, unfiltered tests and fresh-process evidence.

`git diff --check` passes. Existing LF→CRLF advisories on earlier tracked work are informational. A separate strict UTF-8/trailing-whitespace check includes all new/untracked04F files, which ordinary Git diff would omit. `Audit-04F.ps1` verifies source hashes,86 fixtures, stable baseline tests, warning pairs, prior evidence and fresh hashes.

All **98 untracked files**, including preserved earlier work, are listed in `artifacts/phase04/04f-20261016/all-untracked-inventory.txt`. Only the 11 additions listed in section 3 belong to 04F. Only the two additive checkpoint documents changed from the 448-file start snapshot. Branch/HEAD unchanged; staged files0; no commit, push or branch switch. Final audit: `artifacts/phase04/04f-20261016/final-audit.json`.

## 38. Next independent review gate

Ready for independent **Antigravity PHASE 04F implementation review** with code, channel matrix, independent expected values, controlled-source facts, complete build/regression results, unchanged 04A–E evidence and limitations above. No independent PASS is claimed. Stop here; do not implement04G or later and do not commit/push without further owner instruction.

PHASE_04F_PASS_WITH_KNOWN_LIMITATIONS
