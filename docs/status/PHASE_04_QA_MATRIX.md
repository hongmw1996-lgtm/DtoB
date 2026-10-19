# PHASE04 release QA matrix

These are actual contract outcomes, not a common replacement enum. `Partial`
in expansion/style and `PartiallyNormalized` in final CAD refer to incomplete
coverage. A valid primitive does not make the entire document Complete.

| Condition | Stage / result | Published artifact? | Diagnostic / coverage effect | Executable coverage |
| --- | --- | --- | --- | --- |
| Supported source, resolved units/style | 04G Normalized | Complete CadDocument | Ordered ledger only Normalized/NotApplicable | physical unit/OCS/bulge goldens |
| Unsupported source | 03F Raw Partial → 04G PartiallyNormalized | Partial document | RAW_UNSUPPORTED; UnsupportedNormalization ledger | official source oracle + original controlled regressions; exact Unsupported52 |
| Invalid geometry | 04G PartiallyNormalized | Document, invalid payload withheld | CAD_INVALID_GEOMETRY_OR_ANNOTATION; InvalidNormalization ledger | bulge-degenerate, NonplanarSolidWithholdsPayloadAndKeepsFailureLedger |
| Missing style catalog | 04F Partial → 04G PartiallyNormalized | Geometry retained, unresolved style | STYLE_MissingLayerDefinition and unresolved channels | missing-catalog + whole-channel regressions |
| Block cycle | 04E Partial → 04G PartiallyNormalized | Factual document; cyclic branch absent | OCCURRENCE_Cycle | cycle physical fixture + DirectCycleRejectsChildAndKeepsIndependentSibling |
| Depth 32 / 33 | 04E Complete / ResourceLimitExceeded | Expansion / no expansion at33 | ResourceLimitExceeded, Depth | RootCountsAsOneAndDepthBoundaryIsExact |
| Cells 10000 / 10001 | 04G allowed / ResourceLimitExceeded | Document / no document at10001 | OCCURRENCE_ResourceLimitExceeded; CAD_OCCURRENCE_REFUSED | ExactMinsertCellLimitAndLimitPlusOneReachFinalNormalizer; original product/overflow regressions retained |
| Global occurrences 99999 / 100000 / 100001 | 04E Complete / Complete / ResourceLimitExceeded | No prefix on failure | ResourceLimitExceeded, Occurrences | GlobalBoundaryIncludesAllRootsWithOneCounter; nested-array explosion |
| Materialized entries 249999 / 250000 / 250001 | 04G allowed / allowed / ResourceLimitExceeded | No document at250001 | CAD_GEOMETRY_LIMIT | RealNormalizerHonorsFullDevelopmentBoundaryWithoutPublishingPrefix; full real pipeline with test-only reader capacities |
| Diagnostics cap5000 | 04E ResourceLimitExceeded | No expansion | 4999 ordinary + one terminal; never5001 diagnostics | DiagnosticCapReservesOneTerminalSlotAndRefusesPublication(5000), plus caps1/3; final normalizer reserved-slot regression |
| Cancellation | 03 read/freeze, 04E/04F/04G | No final artifact at interrupted stage | Canceled/Cancelled or CAD_CANCELLED; no scheduling-dependent canonical count | existing lexical/assembly cancellation, occurrence observed-progress cancellation, ConcurrentCancellationDoesNotPublishAStylePrefix, ConcurrentCancellationRefusesWholeComposition; new final refusal case |
| Invalid source authority | 04G InvalidNormalization | No document | CAD_INVALID_INPUT_OR_CONTRACT | WholePipelineRefusalPublishesNoDocument(authority); source/foreign-unit regressions |
| Units absent/0/unsupported | 04B unresolved → 04G InvalidNormalization | No document | CAD_UNITS_UNRESOLVED | three physical refusal cases; explicit-selection and contradictory-override regressions |
| Block unit contradiction | 04D branch refusal | No accepted local placement for branch | ContradictoryBlockUnits / UnsupportedBlockUnits | existing block unit/placement regressions; no new unit policy |
| Numeric profile mismatch | 04G InvalidNormalization | No document | CAD_NUMERIC_PROFILE_MISMATCH | WholePipelineRefusalPublishesNoDocument(profile) |
| Malformed/unsupported source cannot publish Raw | appropriate 03A–F rejected status | No Raw, hence no CAD | Legacy manifest's exact stage-specific codes retained | all86 ManifestCaseUsesReadOnlySourceAndReviewedStageExpectation cases |
| Corrupt normalized JSON | strict codec rejection | No deserialized document | InvalidDataException; no repair/default | nine new mutations of real normalized document + all original strict codec negatives |

The legacy boundary/depth/resource/cancellation cases are executable source
fixtures already present in the retained suite. They are not mislabeled as newly
added disk goldens. Large resource/cancellation outputs intentionally have no
checked-in JSON snapshot; final TRX records exact test names/parameters and outcomes.
New physical corpus/source and golden hashes are in the separate manifests.
