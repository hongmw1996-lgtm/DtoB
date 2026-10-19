# PHASE04 source corpus and release goldens

`manifest.json` is the union of the unchanged 86 Phase03 reviewed inputs and 33
new minimal end-to-end Text DXF inputs. Legacy paths are references, not copies.
All inputs have exact size/hash protection. Malformed legacy inputs retain their
original stage-specific rejection expectations in `phase03g/manifest.json`;
they are not falsely represented as successful normalization goldens.

`goldens/manifest.json` describes 31 published normalized documents: the official
controlled document and 30 synthetic cases. Every synthetic document has exact
real-codec JSON; the official 2,233,701-byte document has a digest and summary and
is regenerated under ignored `artifacts/phase04/04h-20261019/`. Summary files
include ordered ID projections, reference/annotation/diagnostic fingerprints,
ledger counts and queried bounds. Fingerprints are QA projections, not new IDs.
Three unit-refusal inputs intentionally have no document or bounds golden.

Golden acceptance was gated by source hashes, independent assertions, strict
validation, three serialization generations, three cultures and two fresh
testhosts in distinct CWDs. Tests never write authoritative golden files.
`CaptureCandidatesOnlyAfterOraclesRoundtripAndCultureEquality` writes only ignored
candidate evidence after its assertions. The final golden assertion cannot bypass
missing/mismatching files. No automatic golden-update switch exists.

| Category / input | Independent oracle and expected facts |
| --- | --- |
| Official controlled | Physical DXF pairs, literal handles and pair witnesses; 333 records; 89 primitive sources + 60 polyline segments = 149 geometry; 158 sources + 26 leaves = 184 entities; 158 + 17 + 26 = 201 styles. Raw fields plus declared/catalog facts = 5,535. |
| Official unsupported | Raw dispositions: VPORT 1, APPID 3, OBJECTS 48; all 52 ledger entries remain UnsupportedNormalization. |
| Official bounds | E4/code11 pair3954=-1300.225; LINE103/code11 pair4178=20700; TEXT142/code20 pair4525=-10100; TEXT15D/code20 pair4896=8500. Z=0. Annotation anchors participate; HEADER extents do not. State Partial. |
| unit-1/2/4/5/6 | Literal LINE endpoint (1,2,3), multiplied by 25.4/304.8/1/10/1000 mm. Source units do not scale angles or coefficients. |
| unit-refuse-0/3, unit-absent | No explicit accepted unit decision; InvalidNormalization, no document. Block-unit contradiction remains in existing scalar/block regressions. |
| ocs-0..7 | Normals +Z, -Z, +X, +Y, (1,2,3), (0.015624,0,1), (0.015625,0,1), (0.015626,0,1). Independent normalized vector cross products implement the published DXF AAA formula, not the production helper. Center=2U+3V+4N; radius=5; each extent=5 sqrt(Ui²+Vi²). |
| shared-style | One LINE geometry (0,0,0)→(2,0,0); one template in definition; two leaves in ModelSpace; explicit ACI red and TrueColor blue (255) override ACI5. Different leaf/style IDs, shared geometry ID. |
| nested | Root R90; child S(2,1,1); LINE endpoint maps to (0,4,0). DFS depths 1,2; nested ByBlock resolves red. Both matrices/path/IDs frozen in exact canonical JSON. |
| minsert | 2 rows × 3 columns; R90 S(2,3,1); spacing(10,20). Cell origin=(-20 row,10 column,0); LINE end adds(0,4,0). Row-major paths; bounds(-20,0,0)..(0,24,0). Own scale must not multiply spacing. |
| block-base | Base(3,4,0), LINE base→base+(1,0,0), R90 S(2,3,1). Base maps to insertion(0,0,0), endpoint(0,2,0). |
| bulge-* | Chord(0,0)→(2,0). b=0 LINE. Otherwise center=(1,(1/b-b)/2), radius=(abs(b)+1/abs(b))/2, signed sweep=4 atan(b), both reconstructed endpoints. b=±1,±0.5,±2. |
| bulge-closed-tilted | Three ordered segments, exactly one closing LINE; +Y OCS closing edge(-2,0,2)→(0,0,0). No tessellation. |
| bulge-degenerate | Coincident chord with nonzero bulge refuses payload and retains InvalidNormalization ledger; document Partial. |
| affine-positive/mirror | Circle R2 + quarter ARC, R45 S(±2,3,1), translation(10,-4,3). Full circle half-extents sqrt(26). Positive ARC: min(10-3sqrt2,-4+2sqrt2,3), max(10+2sqrt2,-4+sqrt26,3). Mirrored ARC: min(10-sqrt26,-4-2sqrt2,3), max(10-2sqrt2,-4+3sqrt2,3). No production bounds helper generates expected numbers. |
| missing-catalog | Geometry retained, style unresolved, Partial. |
| cycle | Self INSERT branch rejected with cycle diagnostic; partial factual document, no fabricated leaves. |

Lower-level independent mathematical oracles remain explicitly distinct from
physical end-to-end goldens: the existing 04B contradiction/default tests, 04C
coefficient/axis tests, 04D composition tests, 04E limit/depth/cancellation tests,
04F channel/isolation tests and 04G affine/shear/bulge/resource tests. The unfiltered
release suite retains every baseline test identity and executes these again.

No upstream DWG equivalence, semantic recognition, BIM or Revit claim is made.
