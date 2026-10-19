# PHASE 04G — Contract inspection / architecture-gap report

Date: 2026-10-16. Owner: Codex. Status: **PHASE_04G_CHANGES_REQUIRED**.

**Implementation has not started.** The owner's pre-implementation contract gate was exercised. The frozen serialized occurrence does not carry the separately computed 04E orientation signs required by the instruction. No schema/codec change or alternative representation was silently introduced. This report is ready for architecture clarification, **not** independent implementation PASS.

The controlling instruction is: “If current04A contract cannot represent a required 04G result without a breaking change: STOP. Report the architecture gap.” The serialized representation/policy needs to be resolved before implementation continues.

## 1. Scope

Only repository/branch inspection, accepted-contract mapping, compiled-type/strict-codec probes and unchanged-baseline build/tests were performed. No production implementation, test source, schema, parser, dependency, geometry normalization, bounds calculation or final document was added.

## 2. Baseline

Branch `phase/04-cad-normalization`, HEAD `76ff6e0d8ccac797ba60d8c5657922c796a0e8c9`. [Independent 04F review](../../reviews/PHASE_04F_REVIEW.md): PHASE_04F_PASS, BLOCKER/HIGH/MEDIUM/LOW all zero. Baseline 1,258 tests. Prior accepted ADRs/reviews/reports retain their authority and were not changed.

Actual types in `packages/DtoB.Cad/V2`, the boundary, plan sections7/15–20, 04E/04F contracts and relevant accepted ADR semantics were inspected. All460 pre-existing Git-visible files were hashed before documentation changes into `artifacts/phase04/04g-20261016/before.json`.

## 3. Files changed

New: `docs/status/PHASE_04G_REPORT.md` only. Additive current-stop notices: `docs/plans/PHASE_04_IMPLEMENTATION_PLAN.md`, `tasks/PHASE_04.md`. No pre-existing production/test/ADR/review/report/fixture file changed.

Ignored local evidence: `artifacts/phase04/04g-20261016/contract-inspection.json`, `codec-contract-probes.json`, `baseline/` build/TRX/evidence, `baseline-gate.log`, `final-audit.json`, `all-untracked-inventory.txt`.

## 4. Actual CAD v2 contract mapping

| Requirement / subject | Actual frozen representation | Finding |
| --- | --- | --- |
| Source geometry | CadEntity.GeometryId → shared CadGeometry; definition ChildEntityIds | Existing representation supports shared source payloads |
| Affine occurrence | CadBlockOccurrence.LocalTransform / AccumulatedTransform, DefinitionId, Path, ParentOccurrenceId | Existing representation supports local analytic curves plus affine placement |
| Orientation sign | 04E CadExpandedOccurrence has LocalOrientationSign and AccumulatedOrientationSign; serialized CadBlockOccurrence has neither | **Required retained data has no explicit final slot; architecture gate** |
| Source entity style | CadEntity.StyleId, one per CadEntity | Can hold a root/source style; alone cannot select two resolved styles for one shared entity |
| INSERT context style | CadBlockOccurrence.StyleId | Describes the INSERT, not arbitrary descendant entity-channel overrides |
| Entity/occurrence relation | CadModelSpaceRecord(EntityId, OccurrenceId) | No StyleId or explicit style-binding record |
| Bounds | No bounds member on CadDocument/Definition/Occurrence/Geometry | Plan expressly allows simple queries; absence alone is **not** claimed to require schema change |
| Header fact locator | CadSourceRecordDomain is StructuralRecord or TableHeader; CadSourceFact requires a record | A future explicit header-unit evidence policy is needed if such evidence is populated; nullable Units.Evidence exists, so this is not treated as a separate mandatory blocker |

**AG-04G-01 — required orientation retention.** The new instruction says occurrence must retain Orientation and 04G must preserve the sign supplied by 04E. There is no constructor parameter/property in the frozen `CadBlockOccurrence` for either sign. Copying only its nested `.Value` omits the separately computed04E signs. Recovering a sign from matrix algebra would be a new derived/query policy, not copying that metadata; it must not silently replace the stated requirement. Matrices encode reflection in ordinary nonsingular cases, but this does not establish an approved persistence policy for the independent source-scale sign, particularly under extreme finite arithmetic/conditioning.

Compiled reflection confirms the fields. A valid hand-authored document containing an occurrence round-trips under the existing codec. Adding `accumulatedOrientationSign` to that occurrence is rejected as **`Unknown CAD member accumulatedOrientationSign.`** A schemaVersion2 tag does not authorize changing the strict shape.

**AG-04G-02 — assembly-policy clarification, not a claim of mathematical impossibility.**04F keys styles by `(source record, occurrence ID)`. With one shared source CadEntity E referenced under two occurrences, E.StyleId cannot simultaneously refer to Red and Blue. Adding StyleId to CadModelSpaceRecord is rejected by the codec. A possible existing-shape alternative is to keep one source template plus separate context-specific CadEntity leaf wrappers sharing the same GeometryId, with ModelSpace pointing to each wrapper and occurrence. This may avoid geometric duplication, but the source-template versus projected-entity identity/membership semantics must be explicitly chosen against plan section 7 before encoding it as the final contract. It is not silently assumed here, and no claim is made that a schema extension is the only option for style linkage.

No accepted ADR was edited and no proposed field was implemented. Owner/architecture review must either specify an approved existing-shape orientation/leaf policy that satisfies the required retention, or authorize a reviewed contract/codec migration. Schema-major/version compatibility and the fixed04A evidence obligations must be considered explicitly; no automatic schemaVersion3 or weakened v2 codec is proposed as an implementation shortcut.

Minimal review proposal: keep shared geometry unchanged and use context-specific leaf wrappers only if the owner confirms that membership/identity interpretation. For orientation, prefer an explicitly typed retention contract if both 04E signs must survive serialization as independent metadata; the alternative is an explicit owner decision that matrix-based query semantics satisfy the requirement. Do not add persisted bounds merely because query results are needed: the plan already permits analytic queries. These are concrete alternatives for architecture review, not approved schema edits.

## 5. Orchestration / ICadNormalizer decision

Plan04G owns assembler/simple queries and a complete/partial document;04H owns QA/comparison. A small composed normalizer is appropriate in 04G once the contract gate is resolved, reusing04B/C/E/F and injecting the accepted numeric policy. No concrete normalizer was added. Existing earlier-stage no-concrete-normalizer assertions will need phase-aware evolution while retaining baseline test identities when actual implementation is authorized; they are not reported as a separate architecture blocker.

## 6. Source entity coverage matrix

This is inspection/planned mapping only, **not normalized support certification**.

| Raw source | Existing helper capability | Frozen target | Current04G status |
| --- | --- | --- | --- |
| LINE |04C WCS endpoints;04F common style | CadLine | Not implemented |
| POINT |04C WCS position;04B display-angle metadata | CadPoint | Not implemented |
| LWPOLYLINE |04C OCS vertices/elevation; retained bulge/width facts | CadPolyline referencing CadLine/CadArc | Not implemented |
| CIRCLE |04B radius;04C center/frame | CadCircle / CadPlane | Not implemented |
| ARC |04B angle interval/radius;04C center/frame | CadArc / CadPlane | Not implemented |
| SOLID |04C OCS vertices | CadSolid boundary | Not implemented |
| INSERT |04E occurrences;04F contexts | Source Insert entity plus existing occurrence graph as contract requires | Assembly policy pending |
| TEXT / MTEXT | Accepted coordinate/scalar helpers and 04F common style | CadEntity + CadAnnotation | Not implemented; no text-layout/extents claim |
| DIMENSION | Accepted subtype-dependent factual points/optional measurement | CadAnnotation.Dimension | Not implemented; no anonymous graphics synthesis |
| Unsupported/unmapped families / attributes | Retained Raw evidence | Explicit ledger/diagnostics, supported-channel-specific policy | Not implemented; no silent coverage claim |

## 7. Model-space geometry

Not implemented. No source point was snapped, transformed, recognized or published. Direct source geometry would use existing WCS/mm helpers without a synthetic occurrence after the gate is resolved.

## 8. Definition geometry

Not implemented. The frozen definition references source entities; shared local geometry is representable. No per-occurrence geometry copies were made.

## 9. Occurrence relationship

04E input remains unchanged: same IDs, typed paths, transforms, root depth 1, orientation signs and sharing. Final orientation persistence is AG-04G-01. No determinant-derived substitute, new occurrence ID or matrix rewrite was introduced.

## 10. Style linkage

AG-04G-02 is recorded in section 4. A resolved entity style cannot simply be stored on a shared entity as the first occurrence's result. INSERT.StyleId is not an arbitrary child-style override. Context-specific leaf wrappers sharing source geometry are a candidate existing-shape policy, pending explicit review. No Red/Blue binding was fabricated or lost in a claimed final document.

## 11. Geometry identity

No production geometry/entity/style ID policy was implemented. Existing cad2 framing is unchanged. IDs used in the isolated codec probe identify hand-authored contract-inspection objects only; they are not blessed normalized IDs or an independent geometry oracle.

## 12. LINE

No04G payload builder or new endpoint/bounds test. Existing04C tests remain green; they are not evidence of completed04G assembly.

## 13. POINT

No04G payload builder or point bounds test. Display-angle metadata/point geometry mapping remains pending.

## 14. LWPOLYLINE

No builder/decomposition. The actual frozen CadPolyline holds ordered segment IDs and Closed, rather than a second vertex/bulge representation. Source widths would need supported/deferred handling; no alternate representation was invented.

## 15. Bulge decision / implementation

Accepted plan assigns exact signed bulge segment interpretation to 04G, including closure, degeneracy and resources. Not implemented. No tessellated truth, guessed defaults, geometric formula or new oracle is claimed.

## 16. CIRCLE

Frozen CadCircle has a CadPlane origin/U/V and positive radius. This can preserve a tilted definition-local circle without calling a nonuniformly transformed world ellipse a circle. No04G creation or circle test was added.

## 17. ARC

Frozen CadArc adds start/sweep radians. Source-local arc plus occurrence affine matrix is representable. No source sweep mutation or arc assembly/bounds implementation.

## 18. SOLID

Frozen CadSolid holds a boundary point list. DXF factual third/fourth ordering and the accepted boundary ordering must be tested during implementation. No triangulation, polygon creation or area/semantic calculation occurred.

## 19. Annotation / DIMENSION handling

CadAnnotation contains kind/content/anchors/optional source measurement/height/rotation. No annotations assembled, no dimension graphics generated, no font/layout work and no fabricated extents/measurement. Unsupported attribute-instance handling remains to be made explicit during implementation.

## 20. Affine transform representation

Existing local analytic geometry plus occurrence matrix is a viable representation. Nonuniform affine placement alone is **not** being reported as a missing-ellipse-contract blocker. No flattened world geometry was produced.

## 21. Nonuniform circle / arc handling

Not implemented. Reuse shared local curve and affine context; do not manufacture a world circular radius. Exact analytic query work remains pending after the contract gate.

## 22. Mirrored occurrence behavior

Source ordering and 04E matrices/signs are intact in current runtime inputs. The requested final orientation retention needs the explicit policy in AG-04G-01. No winding reversal or reflection repair was attempted.

## 23. Exact full-circle bounds

Not implemented. ADR-019's analytic affine extrema remain authoritative. The absence of a serialized Bounds field does not itself block a pure query over the final registries; plan04G permits simple queries. No sampled/fitted bounds were substituted.

## 24. Exact arc bounds

Not implemented. Endpoint/stationary-point signed-sweep logic, wrap, tilt and shear oracles remain required. Existing stage tests are not claimed as these new bounds tests.

## 25. Definition / occurrence / model bounds

No new bounds API, bound registry or cached metadata. Before implementation, define direct-definition versus nested-occurrence aggregation and unknown/partial annotation bounds using actual document references. No hidden side registry was created to conceal a schema decision.

## 26. EXTMIN/MAX non-use proof

No new model bounds computation exists; therefore no04G sentinel regression is claimed. Existing Raw/header evidence remains unchanged. A future04G test must independently prove the known unreliable HEADER values do not affect geometry-derived bounds.

## 27. Resource accounting

No04G allocations or derived geometry counter. Required249,999/250,000/250,001, bulge expansion, no-refund, diagnostics and cancellation checks remain **unverified for 04G**. Existing04E/04F budgets passed unchanged; they do not substitute for a geometry budget. The eventual accounting must count shared/root payloads, generated segments and occurrence leaf materializations under the accepted plan, without counting transient algebra objects.

## 28. Source registry

No registry construction/remapping. Actual CadSourceRecordDomain contains StructuralRecord/TableHeader; fact references are required to stay inside their record spans. A header-unit fact cannot be relabeled as an entity/table record for convenience. Units.Evidence is nullable; its final provenance policy should be explicit rather than using a fabricated locator. This is a mapping issue to settle alongside assembly, not a reason to patch Raw.

## 29. Style catalog

No final CadStyle IDs/deduplication. The contract has resolved Layer/Color/Linetype properties with source modes/states/rule/evidence, and one StyleId on each entity/INSERT occurrence. Value-based catalog equality must account for the actual contract's evidence/modes, not an RGB-only key or dictionary/object hash.04F deferred channels remain deferred.

## 30. Definition / entity / occurrence registries

None assembled. All source-template/contextual-leaf membership and ID semantics must be explicit before implementing AG-04G-02. No duplicate source geometry, anonymous root or unmatched reference was published.

## 31. Provenance

Existing source GUIDs/hash, handles, compact refs, facts and typed occurrence paths remain unchanged. Codec probes used existing contract fixture data and did not claim it was normalized controlled source. Final source/fact registry validation and strict normalized-document provenance tests are pending.

## 32. Completeness / unsupported coverage

No04G document/completeness/ledger. In particular, the owner's52 unsupported-source-record obligation has not been certified by a new normalized ledger. No Partial baseline artifact was relabeled Normalized. Every required final per-record outcome/diagnostic remains outstanding.

## 33. Controlled-source normalized document

Not produced. Previous04E/04F controlled facts remain valid but are not a 04G result. Final counts for entities/styles/geometry/definitions/diagnostics/bounds have not been guessed or frozen from production output.

## 34. Controlled geometry proofs

Not performed for 04G: LINE9C endpoints, controlled ARC parameters/frame, LWPOLYLINE vertices/closure/bulges, POINT, SOLID and occurrence-local geometry/style mapping. These are required after architecture resolution; no self-oracle was generated.

## 35. Controlled bounds proof

Not performed. No whole-model bounds or header-derived expected bounds. Future small hand-derived subsets and finite geometry-derived model bounds remain pending.

## 36. Tests

New automated product tests: **0**, because implementation stopped before code changes. Local read-only contract probes inspected compiled properties and exercised the existing strict codec. They are evidence of the gate, not completion of the 22 requested 04G test families.

`contract-inspection.json` records compiled property names/types, source domains, successful unchanged 04A golden roundtrip and rejection of a proposed unknown document binding member. `codec-contract-probes.json` records a valid occurrence-containing fixture roundtrip and strict rejection of an added occurrence sign / model-space StyleId. No codec strictness was weakened.

## 37. Regression

Unchanged baseline verification: **1,258 total /1,258 executed /1,258 passed /0 failed /0 skipped**. All1,258 prior stable IDs/names are retained and passing. No new04G behavior was tested. Evidence: `baseline/tests.trx`, `baseline-gate.log`, `final-audit.json`.

## 38. Builds / warnings

Existing-source Core Release/Rebuild PASS,0 errors/0 compiler warnings. Full DtoB.sln Release/Rebuild PASS,0 errors, approved MSB3277 exact conflict identities only: Microsoft.VisualBasic10.0/10.1 (b03f5f7f11d50a3a), System.Drawing4.0/8.0 (b03f5f7f11d50a3a), WindowsBase4.0/8.0 (31bf3856ad364e35). No new warning identity.

Used the existing VisualStudio2022 MSBuild/VSTest fallback and Revit reference directory `D:\프로그램\Revit 2025`, via `scripts/Verify-Phase00Foundation.ps1 -Toolchain VisualStudio -RevitInstallDir ... -EvidenceDirectory artifacts/phase04/04g-20261016/baseline`. No SDK/security/installation changes. This proves the unchanged baseline builds, **not** that04G has been built or completed.

## 39. CadIrJson strict roundtrip

Existing04A contract fixture serialize→deserialize→serialize matches its fixed hash. A separately hand-authored valid occurrence-containing contract document also round-trips. Adding `accumulatedOrientationSign` to its occurrence fails with `Unknown CAD member accumulatedOrientationSign.` Adding `styleId` to its ModelSpace record fails with `Unknown CAD member styleId.` Adding a document-level `occurrenceEntityStyles` registry similarly fails. These are actual codec results, not assumptions about extensibility.

No normalized04G document roundtrip exists. Existing fixture success is not presented as a normalized-data golden.

## 40. Normalized CadDocument digest

**Not produced.** There is no04G normalized CadDocument JSON and no normalized-document digest to report. Contract-inspection fixture hashes and prior-stage evidence are explicitly not substitutes.

## 41. Determinism

Existing stage culture/digest tests ran as part of the unchanged full suite. No04G normalized document/culture/fresh-process/CWD campaign occurred because there is no approved assembled result. No whole-PHASE 04 final-release claim.

## 42. Raw / Dxf zero mutation

All Raw/Dxf source and 86 original fixture hashes remain unchanged. No source graph rewrite, parser extension, file conversion or vendor backend. No production content hash/read/lease operation was added.

## 43. 04A–F stability

The baseline run reproduced every required digest exactly:

| Stage | SHA-256 |
| --- | --- |
|04A | `3E49A200C8AD688F6F53842F17556D849979B1C8F3F6A083438721CA02F58EBC` |
|04B | `568D2A7C28863360A4839FF0CA59DF09C1E7C9066EC3A487701872506A68D4C3` |
|04C | `81CA2010F120BD65888EF57AABE5A2F55E230DDC291276E1CBA8BAC9584F0342` |
|04D | `02045C202CACF9DA66406827E297435CCCB7448D1D0A38C7B52E7FCAAA3A0EE5` |
|04E | `143684A78B37AA4962345A28FC8C268E24CB4ECDCDF2EBD410B66EC99A1B626B` |
|04F | `7A448C95EC2F67DCA6C65C3D60B4982E79312814C8C1FD8E5A62C55C611FFC01` |

Production/tests, contracts, codec, ADRs, independent reviews and historical reports remain byte-identical to this turn's snapshot. Only the two checkpoint documents changed, and this report was added.

## 44. No recognition / Revit leakage

No product implementation was added, including recognition, BIM, Revit, family mapping, viewer/UI or spatial interpretation. The baseline-source hash audit confirms those packages are unchanged. No new vendor/framework dependency.

## 45. Explicit04H / PHASE 05 exclusions

Neither phase started. No final-release golden, Raw-vs-normalized QA campaign, Analysis migration or semantic recognition. No commit/push.

## 46. Known limitations

This is an architecture-gate stop, **not a mostly completed implementation with known geometry limitations**. Geometry assembly, style binding, bounds, budget boundary tests, cancellation, normalized roundtrip and controlled final document remain unimplemented.

The confirmed mismatch is the absence of a final orientation-sign field for the explicit retention requirement. The style issue needs a source-template/leaf policy choice; an existing-shape contextual wrapper alternative is documented rather than incorrectly declaring every style solution impossible. Bounds queries are already allowed by the plan and are not inflated into a required schema change. Header evidence nullability is acknowledged rather than faking a header record.

## 47. Git state

No commit, push, stage or branch switch. Branch/HEAD unchanged. All100 untracked files are explicitly inventoried in `artifacts/phase04/04g-20261016/all-untracked-inventory.txt`;99 predate this report. All460 starting Git-visible files are hash-protected, except the two additive checkpoint documents. `git diff --check` and strict UTF-8/trailing-whitespace checks pass. Existing LF→CRLF advisories are informational; no Git configuration was changed.

Self-review focused on whether a legal existing representation was being overlooked: shared local curves plus affine transforms and separate bounds queries are viable; contextual CadEntity leaf wrappers are a reviewable style option. No unapproved field, encoded argument-string workaround, fake source locator or parallel serialized registry was introduced. Full evidence summary: `final-audit.json`.

## 48. Next independent review gate

**Ready for owner/Antigravity architecture clarification; not ready for 04G implementation PASS.** Resolve AG-04G-01's orientation persistence requirement and choose AG-04G-02's source-template/contextual-leaf style association. Any contract/codec change must be explicitly reviewed with version/compatibility and fixed04A evidence obligations. Existing-shape policies must be explicit and independently testable rather than assumed.

After that decision, implement04G and run every outstanding geometry/document gate. Current work stops here under the owner's contract-gap instruction. Do not proceed to 04H/PHASE 05 or commit/push.

PHASE_04G_CHANGES_REQUIRED
