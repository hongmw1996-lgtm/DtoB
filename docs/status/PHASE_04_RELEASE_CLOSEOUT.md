# PHASE04 release closeout

This is the additive release-preparation record. Historical local gate reports,
including PHASE_04_FINAL_GATE.md, retain their correct pre-independent-review state.

- Authoritative independent review: `reviews/PHASE_04_FINAL_REVIEW_V2.md`.
  Source and copied file SHA256:
  `061B6E622A179B7434484D43E455B2D3526A983BD9EC36CCF829D4E2F75903F6`.
  The review was copied byte-for-byte from the Antigravity clone without rewriting.
- Final independent verdict: **PHASE_04_FINAL_PASS_WITH_KNOWN_LIMITATIONS**.
  Production findings: 0 BLOCKER /0 HIGH /0 MEDIUM /0 LOW.
  Release-evidence findings in V2: 0 BLOCKER /0 HIGH /0 MEDIUM /0 LOW.
- V1 is superseded because of REVIEW_V1_ERROR, explicitly resolved by V2. Its
  existing local file is unchanged (SHA256
  `7FE0479ADA589178BCEF3F28BA00E4E879DCCCD58CC2F213AC5C869ECB820CDC`).
  It was absent from the original candidate inventory and is preserved locally,
  excluded from this release payload; V2 is the sole authoritative final review.
- Fresh unfiltered regression: **1402/1402 PASS**,0 failed,0 skipped;all1315
  pre04H IDs/names retained. Core and full Solution Release Rebuild pass;
  Core0 warnings/errors, full0 errors and only the three exact approved MSB3277 pairs.
- Official controlled DXF SHA256:
  `2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC`.
  Official normalized CadDocument SHA256:
  `8A5E078ADB9990C7127B7E626DEC1333071F481FA4A4B7F91CD17D23505F4F24`.
  schemaVersion2;04A–04G/03G/03H accepted semantic digests unchanged.
- Known limitations remain in PHASE_04_LIMITATIONS.md: official normalization
  remains Partial, Unsupported52 and LOW01 retained; no DWG-equivalence,
  recognition, BIM or Revit-generation claim.
- Owner authorization is commit + push only, one PHASE04 candidate commit.
  PR creation, merge, main checkout, tag and PHASE05 are outside this authorization.

Release transport amendment: pre-commit filtering exposed CRLF normalization of
the two accepted PHASE04 manifests and checkout normalization of exact LF JSONs.
The owner explicitly approved these two additional `.gitattributes` rules:
`datasets/controlled/phase04/**/*.json -text` and
`reviews/PHASE_04_FINAL_REVIEW_V2.md -text`. They preserve existing source/golden/
review bytes; no semantic evidence or fixture expectation changes. The existing
04H release audit passed before this narrow amendment; the subsequent closeout
audit separately verifies the exact approved attributes delta, index bytes and
all protected content. The historical audit script is unchanged.

The two frozen CRLF manifests trigger Git's default CR-at-EOL whitespace warning
after exact-byte staging. Diff checks therefore use command-scoped
`core.whitespace=blank-at-eol,blank-at-eof,space-before-tab,cr-at-eol`; all ordinary
whitespace checks remain enabled, alongside the strict UTF8/space/tab audit.
No repository/global Git configuration is changed. Release-metadata inventory
hashes refer to their UTF8 LF projection/Git blob, avoiding platform checkout EOL
dependence; fixture/golden/review hashes remain exact physical byte hashes.

This committed document and release manifest record the pre-commit preparation
snapshot. The one authorized commit and push are executed only after all staged
invariants pass; their actual identifiers are recorded in the post-action receipt.

Fresh logs, byte audits and execution receipts are under
`artifacts/phase04/release-closeout-20261019/`. Commit/tree/remote SHAs, once created,
must be recorded after execution in the ignored receipt and final response;
embedding a commit's own SHA inside its committed content would be circular.

Recommended next action after successful commit/push: **CREATE PHASE04 PR**,
under a separate owner instruction. No PR/merge/tag/PHASE05 action performed.

## Additive PR #7 CI portability correction

PR CI exposed a checkout-dependent physical-byte assertion for the tracked
`datasets/controlled/phase03g/manifest.json`. Its logical JSON and all86 entries,
paths, fixture hashes and expected dispositions are unchanged. The prior
`1B3D1798CD1137C8F4933E44AB3CD8A2717DA3B3F9E7AAADC08A1E49E60B0C4B`
remains historical accepted LOCAL physical-byte evidence (mixed CRLF/LF).

The repository release transport authority is the existing committed LF blob:
56355 bytes, CR0/LF974, SHA256
`A3C7CBD16E94498A0CDE094664DFEFA520DC3182DA4899DB92EAE6C1649B7B36`.
An exact-path `datasets/controlled/phase03g/manifest.json -text` rule now prevents
checkout conversion. The existing assertion protects this single canonical hash;
it is neither removed nor replaced by OS-dependent/multiple accepted hashes.
The local target is materialized from the existing Git blob without JSON rewriting.

Owner authorized one new narrow corrective commit after `e053fb4...`, normal push
to update PR #7, fresh targeted/related/full verification and CI observation.
No normalization production behavior, PHASE03 DXF/bin, manifest semantic content,
golden or accepted03G/03H/04A–G semantic evidence changes. Prior independent reviews
and historical reports remain unchanged. Release path inventory stays220 because
all corrected release files were already candidate paths.

Fresh correction evidence is under `artifacts/phase04/ci-portability-20261019/`.
The original blocker and subsequent commit/CI result are recorded in the local,
uncommitted `docs/status/PHASE_04_PR_GATE.md`. This correction does not authorize
merge, squash/rebase, amend, force push, tag, main checkout or PHASE05.
