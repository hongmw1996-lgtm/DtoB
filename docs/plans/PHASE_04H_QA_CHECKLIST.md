# PHASE04H local QA scope freeze

The accepted implementation plan section 20 (04H: Deterministic QA/comparison),
sections 21–22 and the owner release-gate instruction agree. No production
semantics, architecture, schema, identity, transform or style changes are planned.

- Preserve the legacy 86-entry fixture manifest byte-for-byte; extend disk coverage
  through a separate, disjoint new-fixture set and test duplicate/omission rejection.
- Refer to historical fixtures rather than copy them. Their reviewed Phase03 stage
  expectations remain authoritative; not every malformed source can publish Raw.
- Add minimal physical Text DXF inputs only for missing end-to-end normalization
  coverage. Use explicit source-derived numerical assertions before capture.
- Regenerate the official controlled source through all native reader stages and
  normalization. Check independent source inventories, selected geometry/style,
  Unsupported52, provenance, ledger, and analytic extrema.
- Freeze exact canonical small JSON goldens plus digest/summary projections.
  The plan does not explicitly require a checked-in 2.2 MB official JSON: keep its
  digest and summary in Git and reproduce exact bytes in ignored evidence.
- Capture candidates only in ignored evidence; candidate capture is not acceptance.
  Promote only after factual assertions, validation, triple roundtrip, cultures and
  two fresh-process/distinct-CWD comparisons pass. Final golden tests have no bypass.
- Exercise accepted coordinate/unit/bulge/style/placement policies, retaining the
  existing lower-level analytic and resource boundary oracles as named coverage.
- Verify immutable Raw/input bytes; strict codec negatives on actual normalized
  output; source order and equivalent nonsemantic context-index construction order.
- Reproduce all seven accepted 04A–G digests and all 1,315 baseline test identities.
- Fresh Core/full Solution Release Rebuild; exact existing warning allowlist;
  unfiltered regression; protected boundaries/dependencies/recognition audit.
- Prepare deterministic fixture/golden/release manifests, exact release path/hash
  inventories, failure matrix, limitations and both local gate reports. Do not stage.

The independent 04G review's `architectural_example-imperial.dxf` is a prose
filename discrepancy. Authority is the Git blob and identical working bytes of
`datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf`:
96,621 bytes, 17,474 physical lines, 8,737 pairs,
SHA256 `2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC`.
The independent review is preserved. Its illustrative API/ID/schema prose does
not override accepted contracts: actual JSON is camelCase, IDs use `cad2:<digest>`,
annotations are embedded, and source/fact/layer/diagnostic references are not new
standalone ID domains. Freeze their locators/fingerprints, marking ID N/A.

Independent whole-PHASE04 review remains required after this local gate.
