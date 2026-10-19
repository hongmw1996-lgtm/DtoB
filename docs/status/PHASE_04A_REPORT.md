# PHASE 04A — CAD IR Contracts and Normalization Boundary

Date: 2026-10-16

Local status: **PHASE_04A_PASS_WITH_KNOWN_LIMITATIONS**. Ready for independent Antigravity implementation review; this is not an independent PASS or whole-PHASE 04 completion.

## 1. Scope

Implemented immutable Revit-independent CAD2 transport contracts, strict typed JSON, compact provenance, deterministic identity framing and a pure normalization interface/configuration/result boundary. No normalizer implementation exists. The accepted canonical convention is declared metadata, not evidence that coordinates were converted.

Fixed test data is hand-authored **contract fixture** data. It is not a normalized drawing or a fabricated production result. Empty/unpopulated collections are representable without generating geometry.

## 2. Accepted ADR state

Read [PHASE_04_ADR_REVIEW.md](../../reviews/PHASE_04_ADR_REVIEW.md) completely: **PHASE_04_ADR_PASS**. Owner explicitly authorized acceptance and 04A. Changed **only** the `Status: Proposed` line to `Status: Accepted` in:

- [ADR-018](../adr/ADR-018-CAD-IR-Normalization-Boundary.md)
- [ADR-019](../adr/ADR-019-Coordinate-Normalization-Transforms.md)
- [ADR-020](../adr/ADR-020-Occurrence-Identity-and-Expansion.md)

Before/after comparison verifies all other ADR text unchanged, including historical authoring context. The independent review was not edited. No new architectural departure or schema major beyond accepted CAD2.

## 3. Branch and baseline

Branch remains `phase/04-cad-normalization`. HEAD and released `v0.4-phase03` commit: `76ff6e0d8ccac797ba60d8c5657922c796a0e8c9`. No branch switch, commit or push in 04A. Continued the approved uncommitted working-tree workflow.

Released suite: 773 cases. Current regression comparison actually matches those test names against final TRX; all 773 are retained and passing. Evidence: [regression.json](../../artifacts/phase04/04a-20261016/regression.json).

## 4. Files changed

| Area | Files owned by 04A |
| --- | --- |
| Existing Cad, additive V2 only | `packages/DtoB.Cad/V2/CadContract.cs`, `CadProvenance.cs`, `CadTransform.cs`, `CadIdentity.cs`, `CadGeometryContracts.cs`, `CadStyleContracts.cs`, `CadDocument.cs`, `CadIrJson.cs`, `README.md` |
| New pure library | `packages/DtoB.Cad.Normalize/DtoB.Cad.Normalize.csproj`, `NormalizationBoundary.cs` |
| Solution/test integration | `DtoB.sln`, `tests/DtoB.Tests/DtoB.Tests.csproj`, `FoundationDocumentTests.cs` |
| New tests/fixture | `tests/DtoB.Tests/CadV2ContractFixture.cs`, `CadIrJsonV2Tests.cs`, `CadV2ImmutabilityTests.cs` (also TransformImmutabilityTests), `CadV2ProvenanceTests.cs`, `CadV2IdentityTests.cs`, `CadNormalizationBoundaryTests.cs`, `CadV2ArchitectureTests.cs` |
| Documents | Three ADR status lines, additive current checkpoint in implementation plan/task, this report |

Earlier untracked PHASE 03 local release reports, plans, reviews and spike/history files were preserved. Legacy CadContracts.cs, Core, Geometry, Raw/Dxf, Analysis, Desktop/Revit and existing source fixtures were not modified. No external package added.

## 5. CAD v2 contract model

Root contains schema/algorithm/profile metadata, canonical conventions, a representable unit decision, immutable SourceToModel value, provenance registry, layers/styles, shared definitions, entities, occurrences/model-space references, geometry payloads, ledger, completeness and separate normalized/Raw diagnostics.

Point/line/plane/circle/arc/polyline-reference/SOLID and factual annotation payload types establish contract space only. Constructors check finite values/basic ranges and source/reference structure; they do not derive geometry, normalize planes or evaluate spatial validity. Optional measurement remains null rather than zero. Typed occurrence steps can carry a source INSERT/definition and optional row/column without creating an occurrence tree or a fake source handle.

Collections retain the documented future source/DFS/row-major ordering; codec never sorts/renumbers source references. Registry validates source order and unique roles; ledger accounts for every registry record exactly once. Complete/Partial/Unsupported document states and record-level Normalized/Partial/Unsupported/Invalid/NotApplicable are explicit. See [contract README](../../packages/DtoB.Cad/V2/README.md).

## 6. CadContract and CadIrJson

CadContract owns major 2 validation. Typed CadIrJson serializes/deserializes only `DtoB.Cad.V2.CadDocument`. No v2 call to legacy Core.Contract.Version, Core.IrJson or v1 document validation. Existing non-version Geometry/Core value validation is reused.

Because .NET8 does not require constructor parameters in JSON, the codec checks declared shape before binding immutable constructors. All members, including explicit nulls for unavailable nullable facts, are required. Unknown/duplicate members are rejected at nested levels too. Geometry uses closed `$kind` variants; enum names are exact camel-case, never numeric fallbacks.

## 7. Schema v2 validation

Tests verify integer schemaVersion exactly 2, missing/major 1/future/noninteger/string/bool/null rejection, malformed roots, required nested coordinates, unknown/duplicate fields, bad discriminators/enums and broken references. CAD2 roundtrip succeeds while legacy major 2 rejection and legacy CAD1/BIM1 validation remain unchanged. No v1↔v2 adapter or permissive version fallback.

## 8. Provenance registry

One immutable source document stores DrawingId/RevisionId/FileHash. Records retain domain/index, inclusive zero-based pair range, inclusive one-based physical lines and OriginalHandle/CanonicalHandle. Facts reference registry records and role/pairs/line evidence; verified absence is empty pairs plus null line range. Objects carry small record/fact references, not Raw objects or duplicated graphs.

Resolve methods validate registry lookups; tests verify roundtrip and reject out-of-range refs/evidence, invalid identity/hash/handle, duplicate/reordered records and inconsistent presence. Runtime linkage back to the original Raw artifact is via the retained locators; actual Raw traversal/normalization is not implemented in 04A.

## 9. Identity primitive

Implemented ADR-020 domain-separated binary framing, SHA-256 and `cad2:<lowercase hex>`. Types distinguish null/text/GUID/hash/canonical handle/nonnegative index/nested tuple; UInt32 BE lengths/counts remove tuple-boundary ambiguity. Input fields own storage. No random GUID/time/path or geometry-derived identity.

The encoding tags/root framing are documented in the README. An independently enumerated frame for Source/Text("A") freezes digest `1c249a29d20262678537453687e3bc3428b736b403bb41db13c2e6061fc036b6`. Tests cover domain/type/null/tuple separation, canonical forms, invalid inputs and culture. No INSERT/MINSERT expansion or future full occurrence-ID algorithm is implemented.

## 10. Normalization boundary

`ICadNormalizer.Normalize(RawDrawingSnapshot, CadNormalizationOptions, CancellationToken)` returns CadNormalizationResult. Its Document property explicitly uses the v2 alias; a same-name parent-namespace v1 binding was found during compilation and corrected. Tests assert the actual return/property/parameter types and absence of any concrete engine.

Result states represent normalized/partial/unsupported/invalid and document-less cancellation/resource-limit/not-implemented refusal with diagnostics. Contradictory fake success/document publication is rejected. Options expose immutable validated positive development limits (32/10,000/100,000/250,000/5,000). No counters, resource accounting, scheduling, I/O or cancellation algorithm exists yet; the token is a boundary contract only.

## 11. Immutability

Constructors copy collections and expose read-only views of sealed immutable elements. External mutable geometry subtypes cannot enter the closed hierarchy. Resolved property values are restricted to immutable name/color types. No List/Dictionary/shared writable array leaks from published state.

CadTransform stores a private validated defensive matrix copy. Its Values view rejects mutation; ToTransform3 returns a new array. It provides storage/identity value only, without placement/composition/OCS math. Tests mutate original input, exported matrix, other occurrence projection and collection sources, proving previous state/other occurrences/definition value/JSON remain unchanged, including after deserialization. Bounds do not exist in 04A and are not claimed tested.

## 12. Actual dependency graph

`X -> Y` means a project reference:

```text
DtoB.Cad.Raw       -> none
DtoB.Cad.Dxf       -> DtoB.Cad.Raw
DtoB.Cad           -> DtoB.Core, DtoB.Geometry       (unchanged)
DtoB.Cad.Normalize -> DtoB.Cad.Raw, DtoB.Cad         (new)
DtoB.Analysis      -> DtoB.Cad (v1), DtoB.Bim       (unchanged)
```

Normalize needs no direct Geometry/Core edge for this interface scaffolding; accepted dependencies remain transitive through Cad. Pure net8.0 inherited from Directory.Build.props. Inspected csproj PackageReference/Reference/imports: no new packages, SDK/vendor libraries, Desktop/Project/WPF/Revit references or reverse Raw/Dxf edge. Runtime/source architecture tests also pass. [Boundary audit](../../artifacts/phase04/04a-20261016/boundary-audit.json) records actual references/imports.

## 13. Tests and results

Final actual totals: **844 total / 844 passed / 0 failed / 0 skipped**. Released 773 retained/passing; **71 new cases**:68 in new 04A test classes and 3 Normalize dependency-mutation cases added to the existing architecture theory. Final [TRX](../../artifacts/phase04/04a-20261016/verified/tests.trx).

Tests cover the requested CadIrJsonV2Tests, CadV2ImmutabilityTests, TransformImmutabilityTests, CadV2ProvenanceTests, CadV2IdentityTests, CadV2ArchitectureTests and CadNormalizationBoundaryTests, plus all PHASE 00–03 regressions.

| Verification attempt | Actual result |
| --- | --- |
| Initial focused run | 67/67 PASS before final framing vector and mutation cases were added |
| First full run | 841 executed, 840 passed, 1 failed: old ADR test assumed every ADR used a table |
| Final full Release gate | **844/844 PASS**, 0 failed/skipped |
| Two additional fresh-process/CWD checks | 6/6 each PASS (JSON and source-ID culture cases) |

The old document test now accepts evaluated prose **only for owner-approved ADR-018–020**, requiring selected/rejected alternatives with reasons; the exact old table checks remain for ADR-001–017. This preserves the instruction to change only ADR status, rather than rewriting accepted technical content to satisfy an older formatting assumption. Existing test names/cases were retained. No runtime behavior was loosened to admit CAD2.

Compilation also exposed and resolved the v1/v2 name binding and xUnit assertion-analyzer errors; no suppression or new warning allowance. Intermediate failure artifacts are retained under the04A evidence directory; final readiness uses the verified gate, not earlier outcomes.

## 14. Builds

**Core Release/Rebuild PASS. Full DtoB.sln Release/Rebuild PASS,0 errors.** Used the established Visual Studio 2022 MSBuild/Roslyn/VSTest fallback with SDK files 8.0.416 and installed Revit path `D:\프로그램\Revit 2025`. Normal dotnet discovery still reports unavailable; actual fallback compiler/tool exit and fresh test results prove execution. No global SDK/security configuration was changed.

Command: `scripts/Verify-Phase00Foundation.ps1 -Toolchain VisualStudio -RevitInstallDir 'D:\프로그램\Revit 2025' -EvidenceDirectory artifacts/phase04/04a-20261016/verified`.

[Core log](../../artifacts/phase04/04a-20261016/verified/core-build.log), [full build log](../../artifacts/phase04/04a-20261016/verified/build.log), [gate log](../../artifacts/phase04/04a-20261016/verified-gate.log), [warning identity summary](../../artifacts/phase04/04a-20261016/build-summary.json).

No new warning identities. Existing MSB3277 pairs remained exactly Microsoft.VisualBasic10.0/10.1, System.Drawing4.0/8.0 and WindowsBase4.0/8.0 with the approved culture/public-key identities checked by the unchanged build script.

## 15. Determinism evidence

Executed the same DLL in **two separate VSTest processes, two working directories**, with Invariant/fr-FR/ko-KR tests in each. All 6 JSON files are byte-identical; all 6 typed source-ID outputs match. [Evidence](../../artifacts/phase04/04a-20261016/determinism.json).

Fixed04A contract fixture SHA-256:
`3E49A200C8AD688F6F53842F17556D849979B1C8F3F6A083438721CA02F58EBC`.

Typed source tuple ID:
`cad2:d737c924710a2add9545fb55932593b7b604672a63267a131819bbc0fcd694e3`.

These identify the contract fixture/identity primitive only, **not a PHASE 04 normalized-geometry digest**. No source path/time/random value in that fixture. Property order and finite-number encoding are stable; source collection order is preserved explicitly.

## 16. Raw/Dxf zero-mutation evidence

Captured 381 existing tracked/untracked visible files before work.165 protected files (Raw/Dxf/Core/Geometry/Analysis/Desktop/Revit/datasets/reviews) retain their hashes. No missing baseline files. All 86 controlled source fixture SHA-256 values match their existing manifest. Released Raw snapshot/scene golden assertions pass in the full suite. No parser deficiencies required a Raw change.

Final audit checks new/changed file allowlists and compares ADR content with only the status substitution allowed. Existing legacy CAD source and upstream reports/spikes are preserved. [Boundary evidence](../../artifacts/phase04/04a-20261016/boundary-audit.json), [final inventory/diff check](../../artifacts/phase04/04a-20261016/final-audit.json).

## 17. Analysis v1 unchanged

Source hashes and actual method reflection confirm IAnalysisEngine still consumes legacy CadDocument. No converter or Normalize coupling added. PHASE 05 owns explicit Analysis-v2 migration; permanent dual models are not intended.

## 18. No Revit leakage

Source/project/assembly tests forbid Autodesk.Revit, RevitAPI/UI, ElementId, BuiltInCategory, FamilySymbol and WallType in V2/Normalize; all pass. No changes to Revit integration/IPC or UI and no host behavior introduced. No Revit host smoke run was required or claimed for pure 04A contracts.

## 19. Known limitations

- No normalizer engine or application path invokes real normalization. Interfaces/results/configuration establish future capability only.
- Geometry payloads have structural validation, not normalization/orthonormality/degeneracy/affine bounds validation; those remain later accepted subphases.
- Source units may be represented unresolved and canonical target metadata can exist without conversion; the future engine must enforce source-unit publication policy. Fixture values are manually specified contracts.
- Raw-to-IR traversal, full occurrence identity composition, cycle/expansion/resource enforcement and style resolution are not implemented.
- Numerical tolerance policy and actual unit/angle decisions are deferred to 04B. Limits are immutable configuration, not proof of bounded expansion.
- Accepted ADR body context retains historical Proposed/gate wording exactly as owner instructed; current status lines and new checkpoints govern implementation.
- Existing approved Revit reference warnings/toolchain-discovery limitation remain; no new warning identities.

## 20. Explicit 04B+ exclusions

No INSUNITS conversion/scale calculation; degree conversion; OCS/WCS/AAA/extrusion normalization; block-base compensation or INSERT matrix creation; nested/MINSERT expansion; Layer0/ByLayer/ByBlock inheritance; bulge conversion; analytic curve bounds; generated normalized geometry or spatial document assembly; recognition/BIM/Revit behavior. No Raw rewrite, new DWG/CAD SDK, viewer, database/cloud, v1 migration or source fallback.

## 21. Git state and self-review

Remained on requested branch at released HEAD; no staged files, commit or push. Reviewed tracked solution/project/test diff and every new contract/test file, including immutable aliases, strict JSON constructor binding, reference checks and the absence of later algorithms. Self-review led to closed geometry inheritance, immutable scalar restrictions and result/status contradiction checks before final build.

`git diff --check`, strict UTF-8, trailing-whitespace and local link checks pass; new files are checked explicitly because ordinary git diff omits untracked additions. Final inventory/protected hashes and status-only ADR comparisons pass. Historical untracked files were not cleaned/reset. Working tree intentionally includes this implementation and prior authorized documents for independent review.

## 22. Next independent review gate

Independent Antigravity review of this 04A implementation/report. Do not begin 04B, commit or push. All local 04A acceptance items have evidence above; no independent acceptance is claimed.

PHASE_04A_PASS_WITH_KNOWN_LIMITATIONS
