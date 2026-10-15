# PHASE03G resource-limit matrix

Test: `ExtractionQaCampaignTests.EveryEnforcedLimitHasBelowExactFirstOverEvidence` (42 theory cases,126 executions). Fixed hand-counted source workload; configure its budget one larger / exact / one smaller. “Below” means workload below its budget. Diagnostic budgets include one reserved terminal slot. No timing assertion or production oracle generates these expected thresholds.

| Phase | Configurable limit | Below budget / result | Exact budget / result | First-over budget / result |
|---|---|---|---|---|
|03A|MaxInputBytes|5 / Succeeded|4 / Succeeded|3 / ResourceLimitExceeded|
|03A|MaxPairCount|4 / Succeeded|3 / Succeeded|2 / ResourceLimitExceeded|
|03A|MaxPhysicalLineBytes|7 / Succeeded|6 / Succeeded|5 / ResourceLimitExceeded|
|03A|MaxStoredDiagnostics|4 / InvalidInput|3 / InvalidInput|2 / ResourceLimitExceeded|
|03B|MaxControlGroupDepth|3 / Partial|2 / Partial|1 / ResourceLimitExceeded|
|03B|MaxFieldsPerRecord|7 / Partial|6 / Partial|5 / ResourceLimitExceeded|
|03B|MaxPairCount|16 / Partial|15 / Partial|14 / ResourceLimitExceeded|
|03B|MaxRecords|4 / Partial|3 / Partial|2 / ResourceLimitExceeded|
|03B|MaxReferenceOccurrences|3 / Partial|2 / Partial|1 / ResourceLimitExceeded|
|03B|MaxSections|3 / Partial|2 / Partial|1 / ResourceLimitExceeded|
|03B|MaxStoredDiagnostics|4 / Partial|3 / Partial|2 / ResourceLimitExceeded|
|03B|MaxTables|3 / Succeeded|2 / Succeeded|1 / ResourceLimitExceeded|
|03C|MaxFieldsPerRecord|7 / Succeeded|6 / Succeeded|5 / ResourceLimitExceeded|
|03C|MaxGeometryRecords|3 / Succeeded|2 / Succeeded|1 / ResourceLimitExceeded|
|03C|MaxSourcePairs|16 / Succeeded|15 / Succeeded|14 / ResourceLimitExceeded|
|03C|MaxStoredDiagnostics|6 / Partial|5 / Partial|4 / ResourceLimitExceeded|
|03C|MaxStructuralRecords|3 / Succeeded|2 / Succeeded|1 / ResourceLimitExceeded|
|03C|MaxTotalVertices|3 / Succeeded|2 / Succeeded|1 / ResourceLimitExceeded|
|03C|MaxVerticesPerPolyline|4 / Succeeded|3 / Succeeded|2 / ResourceLimitExceeded|
|03D|MaxAnnotations|3 / Succeeded|2 / Succeeded|1 / ResourceLimitExceeded|
|03D|MaxAttributeSequenceLength|3 / Succeeded|2 / Succeeded|1 / ResourceLimitExceeded|
|03D|MaxChildrenPerBlock|3 / Succeeded|2 / Succeeded|1 / ResourceLimitExceeded|
|03D|MaxDefinitions|3 / Succeeded|2 / Succeeded|1 / ResourceLimitExceeded|
|03D|MaxFieldsPerRecord|6 / Succeeded|5 / Succeeded|4 / ResourceLimitExceeded|
|03D|MaxInserts|3 / Succeeded|2 / Succeeded|1 / ResourceLimitExceeded|
|03D|MaxMappedRecords|3 / Succeeded|2 / Succeeded|1 / ResourceLimitExceeded|
|03D|MaxMTextChunks|4 / Succeeded|3 / Succeeded|2 / ResourceLimitExceeded|
|03D|MaxRawAnnotationCharacters|4 / Succeeded|3 / Succeeded|2 / ResourceLimitExceeded|
|03D|MaxSourcePairs|15 / Succeeded|14 / Succeeded|13 / ResourceLimitExceeded|
|03D|MaxStoredDiagnostics|6 / Partial|5 / Partial|4 / ResourceLimitExceeded|
|03D|MaxStructuralRecords|3 / Succeeded|2 / Succeeded|1 / ResourceLimitExceeded|
|03E|MaxConflictMembers|5 / Partial|4 / Partial|3 / ResourceLimitExceeded|
|03E|MaxDuplicateGroups|3 / Partial|2 / Partial|1 / ResourceLimitExceeded|
|03E|MaxNodes|3 / Succeeded|2 / Succeeded|1 / ResourceLimitExceeded|
|03E|MaxRecords|3 / Succeeded|2 / Succeeded|1 / ResourceLimitExceeded|
|03E|MaxReferences|3 / Succeeded|2 / Succeeded|1 / ResourceLimitExceeded|
|03E|MaxSourcePairs|13 / Succeeded|12 / Succeeded|11 / ResourceLimitExceeded|
|03E|MaxStoredDiagnostics|4 / Partial|3 / Partial|2 / ResourceLimitExceeded|
|03E|MaxTraversalWork|6 / Succeeded|5 / Succeeded|4 / ResourceLimitExceeded|
|03F|MaxChildAssociations|334 / Succeeded|333 / Succeeded|332 / ResourceLimitExceeded|
|03F|MaxDiagnosticReferences|60 / Succeeded|59 / Succeeded|58 / ResourceLimitExceeded|
|03F|MaxSourceLocations|343 / Succeeded|342 / Succeeded|341 / ResourceLimitExceeded|

All42 first-over cases assert own completion=false;03F also asserts Snapshot=null. Below/exact rows intentionally retain semantic InvalidInput/Partial when the source is malformed: acceptance by a resource budget is separate from source correctness. No budget assertion weakens semantic validation.

`MaxControlGroupDepth` on03A DxfReaderLimits is explicitly RESERVED, not enforced by lexical reading. Its actual03B equivalent is tested below/exact/over. `LimitMatrixHasAnEntryForEveryEnforcedConfigurationProperty` reflects every limit type and rejects missing rows; reserved03A depth is the sole explicit exception.

42 source workloads are checked-in in `ExtractionQaCampaignTests.Limits.cs`: small actual text/pair fixtures plus the independently reviewed controlled342 source locations/333 child associations/59 diagnostic references. Configurations are injected only via existing public limit records. Complete result evidence: `artifacts/phase03/03g-qa-20261015/final/limit-*.json`. These boundary tests use lowered positive budgets; they do not allocate maximum-size inputs for every default cap or claim an external memory/performance benchmark.
