# PHASE04H — Corpus, goldens and whole-phase local QA

Date: 2026-10-19. Implementation owner: Codex.
Local status: **PASS_WITH_KNOWN_LIMITATIONS**. Independent whole-PHASE04 review is
pending. This is implementation/QA acceptance, not complete source coverage.

Evidence root: `artifacts/phase04/04h-20261019/`. Authoritative full build/test
results are **final/** and `gate-final.log`, not the initial failed development run.
Machine-readable [release manifest](phase04/release-manifest.json),
[fixture manifest](../../datasets/controlled/phase04/manifest.json) and
[golden manifest](../../datasets/controlled/phase04/goldens/manifest.json) identify
the actual payload. All paths in these deterministic manifests are relative.

## 1. Scope

04H only: source corpus, independent QA, golden acceptance, strict roundtrip,
determinism, resource/cancellation regression and local release preparation.
No production code change. No PHASE05, recognition, UI, BIM/Revit generation,
schema, ID, unit, coordinate, transform or style change.

## 2. Baseline

Branch `phase/04-cad-normalization`; HEAD/released base
`76ff6e0d8ccac797ba60d8c5657922c796a0e8c9` (`v0.4-phase03`). Independent04G
implementation verdict PHASE_04G_PASS, zero findings. Baseline **1,315/1,315**
from `artifacts/phase04/04g-implementation-20261016/release/tests.trx`.
Captured **475** pre-existing Git-visible files in `before.json`; prior untracked
04A–G work was preserved, not misclassified as new04H work.

## 3. Files changed

04H adds two C# test files (`Cad04HCorpusTests.cs`, `Cad04HReleaseTests.cs`),
`scripts/qa/Invoke-Phase04ReleaseAudit.ps1`, 33 minimal DXFs, corpus manifest/README,
31 summary goldens, 30 exact small canonical JSONs and golden manifest.
The legacy `ExtractionQaCampaignTests.cs` coverage assertion now checks the
disjoint manifest union and still requires the original86 and fixed manifest hash.
Added scope checklist, QA matrix, limitations, this report, whole-phase gate,
release/test/path inventories; plan/task checkpoints are additive.
Exact accumulated release paths are in [whole-phase inventory](phase04/whole-phase-path-inventory.json).

## 4. Actual04H plan interpretation

Accepted implementation plan section20 is Deterministic QA/comparison; sections21–22
require separate additive fixture coverage and source-linked numerical evidence.
This matches owner scope. [Pre-code checklist](../plans/PHASE_04H_QA_CHECKLIST.md)
froze interpretation and promotion policy before test changes. No ADR amendment
or production fix was necessary.

## 5. Controlled-source authority verification

Actual Git-controlled path is `datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf`.
Binary-safe `git cat-file blob HEAD:<path>` stream and working file both have:
96,621 bytes; 17,474 physical lines; 8,737 pairs; SHA256
`2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC`.
The exact Git-returned blob OID is recorded in `source-authority-final.json`;
content SHA256 above is the authority. Initial and final byte audits match.
Only managed Text DXF is truth; no upstream DWG semantic-equivalence claim.

## 6. Review filename discrepancy handling

The04G review's `architectural_example-imperial.dxf` is a textual discrepancy.
The review remains byte-identical. The additive checklist records the actual path
and clarifies that illustrative review API/ID prose does not supersede frozen
contracts: JSON is camelCase; annotations embedded; actual ID domains unchanged.

## 7. Fixture corpus design

119 physical inputs = original86 references +33 new minimal inputs. No historical
fixture copied or repaired. New cases cover five accepted units, three unit
refusals, eight OCS axes/thresholds, shared styles, nested INSERT, MINSERT,
nonzero block base, seven bulges, tilted closed polyline, degenerate chord,
missing catalog, cycle, and two affine circle/arc cases. Existing executable
mathematical/resource fixtures remain explicitly distinct from disk goldens.

## 8. Fixture manifest

Each entry freezes key, relative path, SHA256, byte size, input profile, intended
coverage, expected stage/result, document/bounds/diagnostic golden availability,
and exclusion reason. Legacy rejection expectations remain stage-specific in the
unchanged03G manifest. All disk DXF/bin paths must equal the manifest union;
omission, duplicate and case-duplicate negatives reject. Golden-file coverage also
rejects omitted/orphaned files. No dates/absolute paths in canonical manifests.

## 9. Input immutability

All119 input hashes/sizes checked. Original86 and original manifest digest remain
exact. Every04H pipeline compares physical input before/after and Raw serialized
bytes before/after normalization. All pre04H production, reports, reviews, ADRs,
fixtures and accepted evidence remain unchanged. Only the legacy coverage test
and additive plan/task checkpoints change among the475 captured files.

## 10. Golden policy

31 documents accepted only after physical source hash checks, independent
source/math assertions, strict validation, A=B=C, three cultures and two fresh
testhosts/distinct CWDs. Candidate capture writes only ignored evidence and is
not a golden-update switch. Final assertions require existing checked-in files.
The accepted plan does not explicitly require a large official JSON: store its
hash+summary; regenerate exact bytes. All30 synthetic outputs have real-codec JSON.

## 11. Controlled golden verification

Regenerated official document SHA256:
`8A5E078ADB9990C7127B7E626DEC1333071F481FA4A4B7F91CD17D23505F4F24`,
**2,233,701 bytes**. Existing04G candidate was independently regenerated and
accepted after the remaining gates; no changed digest was blessed.

## 12. Independent factual oracles

[Corpus oracle table](../../datasets/controlled/phase04/README.md) identifies the
production method, independent source/algebra and expected values per category.
Literal DXF pairs/handles, Raw inventories, chord equations, vector cross products,
cell-spacing algebra, hand-derived matrix action and analytic extrema are used.
Expected values are not generated by CadBoundsQuery/normalizer. Existing lower-level
independent oracles remain in the full suite. Golden bytes alone are regression
evidence, not the independent numerical oracle.

## 13. Round-trip campaign

All31 documents: strict serialize A → deserialize/validate → serialize B →
deserialize/validate → serialize C; A=B=C byte-for-byte. Repeated in three cultures
and separate processes. Raw remains byte-identical before/after composition.

## 14. Strict-codec negative corpus

Nine new mutations of a real normalized nested document reject unknown members,
missing required data, duplicate properties, wrong schema, enum, ID syntax/domain
shape, dangling reference, matrix shape and null list member. All original strict
codec negative and domain-separation tests retained. No relaxed codec path added.

## 15. Determinism campaign

Invariant, fr-FR, ko-KR for all31 documents, process-a at repository CWD and
process-b at user TEMP CWD, followed by the fresh final unfiltered testhost.
**186 candidate artifacts** (JSON+summary ×31×3) match exactly across all three
contexts. `process-comparison-final.json` records each path/hash.
Canonical contracts contain source GUID/hash/locators, no runtime path/time/random
identity. The official normalized JSON remains identical in the04G evidence test.
Reversed nonsemantic context-index construction order preserves exact official bytes;
lexical/source order is unchanged.

## 16. ID stability

Summaries freeze ordered Style/Geometry/Entity/Definition/Occurrence IDs; complete
source/fact/layer/annotation/diagnostic projections are fingerprinted. Leaf IDs are
included in entities. SourceRecord/Fact/Layer/embedded Annotation/Diagnostic have
locators/references rather than invented standalone IDs (ID N/A). Existing framing,
domain separation and lineage-only identity tests retained. Exact cross-process
JSON equality covers every ID, matrix, path and reference.

## 17. Source-template / leaf golden

`shared-style`: one geometry, one source template in definition, two ModelSpace
leaves, distinct leaf/style IDs, shared GeometryId. Effective ACI red vs TrueColor
blue; template is not inserted into ModelSpace and geometry is not duplicated.

## 18. Nested occurrence golden

`nested`: root R90, child S(2,1,1), source endpoint(2,0,0) maps to(0,4,0).
DFS path depths1/2, parent link, local/accumulated matrices, IDs and inherited red
style frozen. Model bounds(0,0,0)..(0,4,0), within accepted numerical tolerance.

## 19. MINSERT golden

`minsert`: 2×3 row-major cells, R90 S(2,3,1), spacing10/20. Origin=(-20row,10column,0),
endpoint adds(0,4,0); bounds(-20,0,0)..(0,24,0). Cell path/IDs/matrices/leaves frozen.
Independent assertions prove own scale does not scale spacing. Coincident cells
and nested arrays remain covered by baseline regressions.

## 20. Style golden

Physical unit/OCS cases exercise ByLayer; shared/nested cases Layer0, ByBlock,
nested ByBlock, TrueColor precedence and occurrence isolation. Templates remain
declared, leaves effective. Existing04F suite covers linetype, visibility, malformed
channels, ambiguous catalogs and deferred channels without false Complete.

## 21. Bulge golden

b=0, ±1, ±0.5, ±2; independently derived center/radius/start/signed sweep/endpoints.
Closed tilted polyline preserves segment order and one closing segment. Coincident
nonzero-bulge chord keeps an invalid ledger and withholds geometry. Existing tiny
bulge precision refusal remains. No tessellation golden.

## 22. Bounds golden

Circle, quarter arc, nonuniform scale, rotation, mirror and tilted3D OCS tested
analytically. Affine full-circle half-extents sqrt26; partial arc extrema use
separate hand-derived endpoint/stationary values. Nested bounds use accumulated
placement. Existing shear, signed/wrapped arc and source/leaf query tests retained.
No serialized cached bounds added.

## 23. Unit / coordinate golden

Accepted codes1/2/4/5/6 verify XYZ scaling; absent/0/3 refuse. Existing explicit
selection, contradiction and block-unit tests retained. Eight OCS physical inputs
cover +Z/-Z/+X/+Y/oblique and both sides/exact AAA threshold1/64. Expected axes
come from independent cross-product algebra using the specified normalized normal.

## 24. Block-base regression golden

Base(3,4,0), R90 S(2,3,1): source base→insertion(0,0,0), endpoint→(0,2,0).
This would fail if subtraction were omitted or done after scale/rotation.
Historical nonzero-base, mirror and tilt tests remain passing.

## 25. Resource boundary QA

Depth32/33, exact cells10000/10001, occurrences99999/100000/100001,
materialization249999/250000/250001 and diagnostics5000 execute in final suite.
New exact cell limit+1 cases reach the concrete normalizer. Large materialization
tests use real native pipeline with documented test-only reader capacities.
Diagnostic cap reserves one terminal slot. Global exhaustion never publishes a
truncated-success artifact. See [QA matrix](PHASE_04_QA_MATRIX.md) and final TRX.
No giant JSON for resource/cancellation cases.

## 26. Cancellation QA

Existing03 read/freeze,04E observed-progress,04F concurrent style and04G
pre/concurrent normalization/query cancellation pass. New final refusal case also
publishes no document. Scheduling-dependent reservation counts are excluded from
canonical goldens; no throughput/timing guarantee is inferred.

## 27. Failure / partial matrix

[Release QA matrix](PHASE_04_QA_MATRIX.md) records condition, stage, result,
publication, diagnostic and coverage for unsupported/invalid/missing-style/cycle,
depth/occurrence/materialization/diagnostic exhaustion, cancellation, invalid
authority, units, profile mismatch and malformed source/JSON.

## 28. Completeness semantics

25 golden documents are Normalized; six PartiallyNormalized, including official.
Three unit-refusal inputs publish nothing. Unsupported/invalid record states
remain in the ordered ledger. Structural validation rejects a Complete document
that hides partial records. Invalid/failed/cancelled/resource results never become
fake successful documents; phase-specific enums are not conflated.

## 29. Unsupported52 preservation

Exact VPORT1 + APPID3 + OBJECTS48 =52. Raw dispositions and corresponding final
ledger states cross-checked for every source. No removal, reclassification or silent
promotion from successful primitive normalization.

## 30. Known limitations

[Current limitations](PHASE_04_LIMITATIONS.md) lists Text DXF authority, unit subset,
XYZ completeness, deferred lineweight/transparency/color-book/width,
ATTRIB/ATTDEF instancing, unavailable glyph extents/dimension synthesis,
Unsupported52, strict LOW01 and no recognition/BIM/Revit. PASS does not hide these.

## 31. Whole-phase path inventory

[Inventory](phase04/whole-phase-path-inventory.json) classifies every changed or
untracked path since released base: architecture/docs,04A–G production,04H QA,
reviews, fixtures and tests. Twelve historical untracked03 release/spike files
remain excluded/preserved. Ignored generated evidence has a separate inventory.
No unexplained production file or vendor dependency; exact candidate paths included.

## 32. Protected-boundary audit

Git diff/untracked checks against base for Raw/Dxf/Core/Geometry/Analysis/Desktop/
BIM/Revit report zero paths. Pre04H hash protection additionally proves no04A–G
production bytes changed during04H. Historical reviews/ADRs/reports also unchanged.

## 33. Dependency audit

Raw→runtime only; Dxf→Raw; Cad→Core+Geometry; Normalize→Cad+Raw.
Exact csproj references frozen; zero external packages in these projects.
No Revit, WPF, Desktop, vendor SDK or semantic Analysis coupling.

## 34. Recognition / Revit leakage audit

Whole-phase changed-production classification and targeted source scans show no
Wall/Door/Window/Column/Room/Story candidate or BuildingElement implementation,
recognition confidence or vendor/UI/Revit import. Existing architecture tests
also run. CAD v2 remains semantic-neutral. No PHASE05 files.

## 35. Source / fact / registry validation

Every new pipeline verifies record count, actual pair locator and original handle
mapping. Official independent field inventories yield5,535 facts. Real codec
structural validation verifies refs, ordered unique registry roles/IDs, complete
ordered ledger, leaf/source lineage, polyline segments and occurrence paths.

## 36. Strict normalized JSON roundtrip

All31 real normalized outputs pass A=B=C; exact checked-in synthetic bytes and
official digest/size/summary are compared anew in final unfiltered suite.
No pretty-printed secondary semantic codec or golden-regeneration bypass.

## 37. Controlled counts / bounds

333 sources;5,535 facts;12 layers;201 styles;12 definitions;17 occurrences;
158 source entities+26 leaves=184;123 ModelSpace;149 geometry;44 annotations;
117 normalization diagnostics+59 retained Raw diagnostics. Ledger117 Normalized,
65 PartiallyNormalized,52 UnsupportedNormalization,99 NotApplicable (sum333).
Bounds Partial: min(-1300.225,-10100,0), max(20700,8500,0). HEADER extents excluded;
annotation anchors included, not glyph boxes.

## 38. Fixed digests

All exact accepted values reproduced; no golden update:

| Stage | SHA256 |
| --- | --- |
|04A|3E49A200C8AD688F6F53842F17556D849979B1C8F3F6A083438721CA02F58EBC|
|04B|568D2A7C28863360A4839FF0CA59DF09C1E7C9066EC3A487701872506A68D4C3|
|04C|81CA2010F120BD65888EF57AABE5A2F55E230DDC291276E1CBA8BAC9584F0342|
|04D|02045C202CACF9DA66406827E297435CCCB7448D1D0A38C7B52E7FCAAA3A0EE5|
|04E|143684A78B37AA4962345A28FC8C268E24CB4ECDCDF2EBD410B66EC99A1B626B|
|04F|7A448C95EC2F67DCA6C65C3D60B4982E79312814C8C1FD8E5A62C55C611FFC01|
|04G|8A5E078ADB9990C7127B7E626DEC1333071F481FA4A4B7F91CD17D23505F4F24|

03G Raw fixed-context hash4B91C837…A310C and03H scene9D179A0E…3963A7 also exact.
The03G fixture uses different provenance strings from the valid-GUID normalization
fixture; those Raw hashes are not incorrectly compared to each other.

## 39. Fresh-process reproduction

process-a/process-b final candidate runs each execute native read→Raw→normalizer→
strict triple roundtrip→summary under all three cultures. process-b uses TEMP CWD.
Final unfiltered testhost reproduces the same186 artifacts after fresh builds.
Local gate comparison checks every hash rather than relying on in-process cache.

## 40. Tests

**87 added cases**:34 physical corpus cases, capture/culture campaign1,
manifest omission/duplication3, corpus/golden inventory1, golden assertions31,
equivalent index-order1, strict final-document negatives9, final refusal matrix5,
exact cell boundaries2. Exact test IDs/names in [new-test inventory](phase04/new-test-inventory.json).

## 41. Regression

Unfiltered final **1,402 passed /0 failed /0 skipped**. All1,315 baseline IDs/names
retained/passing; [baseline inventory](phase04/baseline-test-inventory.json).
Includes PHASE00/01/02,03G/03H and04A–G. Original86 fixture theory cases remain.
Initial development failures were test-input defects: missing ENDBLK identity,
unsafe slash in candidate key, two incorrectly predicted fixture completeness
states, and missing style catalog masking the10000-cell test with diagnostic
exhaustion. All corrected in new QA data/tests; no production defect or changed
accepted digest. Initial failed evidence is retained separately, not final authority.

## 42. Builds / warnings

Fresh **Core Release Rebuild PASS**,0 errors/0 warnings; **full DtoB.sln Release
Rebuild PASS**,0 errors. Existing Visual Studio2022 MSBuild fallback with SDK8.0.416,
Revit references at the established local installation; normal SDK CLI unavailable.
Only exact existing MSB3277 conflicts: Microsoft.VisualBasic10.0/10.1 and
System.Drawing4/8 (b03f5f7f11d50a3a), WindowsBase4/8 (31bf3856ad364e35), neutral.
No new warning identities. Git LF/CRLF advisory messages are separate from build
warnings; no attributes/config/fixture normalization changed.

Reproduce with `scripts/Verify-Phase00Foundation.ps1 -Toolchain VisualStudio
-RevitInstallDir <installed-Revit2025> -EvidenceDirectory <ignored-final-directory>`;
set DTOB_04H/04G/04F/04E/04D/04C/04B/04A/03G/03H_EVIDENCE_DIRECTORY to that directory.
Then run the04H audit script against the campaign root containing before/process evidence.

## 43. Raw / Dxf zero mutation

Zero production Git changes since released base and byte-identical before/after04H.
Immutable Raw comparisons cover official and every normalized physical synthetic
case. No parser, source authority, LOW01 or rendering changes.

## 44. Release manifest

[Release manifest](phase04/release-manifest.json) freezes branch/base, corpus/input/
normalized/prior hashes, tests/build/warnings, coverage, schema2, ADRs, boundaries,
limitations, artifact inventories and unstaged/uncommitted/unpushed state.
No timestamps/absolute machine paths. Self-referential inventory is path-only to
avoid recursive hashes; corpus/golden content is independently hash-protected.

## 45. Candidate release payload inventory

Exact paths/categories in [whole-phase inventory](phase04/whole-phase-path-inventory.json),
including current manifests/reports themselves. Historical exclusions are explicit.
Generated JSON/TRX/logs remain ignored and separate, including failed development
history. Nothing was staged or silently deleted. Full untracked source was included
in UTF8/trailing-whitespace checks, not just ordinary tracked Git diff.

## 46. Git state

Same branch/base HEAD. Index empty; no stage/commit/push/PR/tag operation.
`git diff --check` PASS; strict UTF8 and trailing-whitespace audit PASS for every
candidate path, including untracked04A–H production/tests/docs/fixtures.
Self-reviewed tracked diff plus complete untracked path/source changes; no
unexplained production or historical mutation. Local changes remain reviewable.

## 47. Whole-PHASE04 local verdict

**PHASE_04_FINAL_PASS_WITH_KNOWN_LIMITATIONS**: independent04A–G reviews pass,
local04H QA passes, fixed evidence stable, whole-phase build/regression pass,
release manifests complete, no architecture change/leakage and limitations explicit.
Official source coverage is still Partial. No independent whole-phase PASS claim.

## 48. Next independent whole-phase review gate

Ready for independent Antigravity whole-PHASE04 final review of this report,
[local gate](PHASE_04_FINAL_GATE.md), exact payload and actual source/evidence.
Stop here. PHASE05 and Git release actions require subsequent owner direction.

PHASE_04H_PASS_WITH_KNOWN_LIMITATIONS
