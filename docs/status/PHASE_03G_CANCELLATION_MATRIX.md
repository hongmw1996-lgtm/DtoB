# PHASE03G cancellation matrix

No sleeps, races, CancelAfter or production instrumentation. Results are separated
by mechanism: **executed** cancellation at public boundaries/real reads/existing
constructor enumerations, versus **source inspection** of CPU-only inner loops.
The latter is not a claim that a token was dynamically canceled at that loop.

| Location requested | Executed deterministic evidence | CPU checkpoint source reviewed | Result / limitation |
|---|---|---|---|
| Before execution03A–03F|`PreCanceledStagesNeverPublishOwnCompletionAndRejectDownstreamSnapshot`6 cases|Each reader entry and final publication|PASS; own Canceled, own completion false, downstream Snapshot null;03A position0|
| During lexical read|`CancellationDuringActualLexicalReadsIsDeterministicAndCallerOwnsStream`|TextDxfLexicalReader read/byte/line/pair checks|PASS; injected stream cancels on read30, caller retains stream; no Snapshot|
| Structural traversal|03B-records freeze cancellation; pre03B|TextDxfStructuralReader main/section/table loops and Header/Records loops|Boundary/freeze PASS; mid-traversal cancellation only source-inspected|
| Geometry mapping|03C-geometry freeze cancellation; pre03C|TextDxfGeometryReader mapping/field loops|Boundary/freeze PASS; mid-mapping only source-inspected|
| Polyline grouping|pre03C, preserved prior grouping/freeze tests|TextDxfGeometryReader.Polyline per-pair/per-vertex checks|Mid-group token injection unavailable through frozen input; source-inspected|
| Annotation mapping|03D-records freeze cancellation; pre03D|TextDxfBlockAnnotationReader mapping/field loops|Boundary/freeze PASS; mid-mapping only source-inspected|
| Block membership|03D-blocks freeze cancellation; pre03D|TextDxfBlockAnnotationReader.Sequences block/child loops|Boundary/freeze PASS; mid-membership only source-inspected|
| Identity node collection|03E-nodes freeze cancellation; pre03E|TextDxfIdentityGraphReader per-source/node checks and Guard|Boundary/freeze PASS; mid-collection only source-inspected|
| Reference resolution|03E-references freeze cancellation; pre03E|TextDxfIdentityGraphReader per-record/reference checks|Boundary/freeze PASS; mid-resolution only source-inspected|
| Owner DFS|03E-ownership freeze cancellation; pre03E|Owners.Work→Guard→token.ThrowIfCancellationRequested on iterative traversal/stack work|Boundary/freeze PASS; mid-DFS only source-inspected|
| Snapshot association|pre03F;03F-records freeze cancellation|Assembler per-source/association/child/membership loops|Boundary/freeze PASS; mid-association only source-inspected|
| Snapshot freeze|03F-records and03F-diagnostics cancellation|RawDrawingSnapshot.Freeze per-item/final token check|PASS; constructor throws OperationCanceledException; no object published|

`CancellationDuringExistingFreezeEnumerationsNeverPublishesObject` has9 cases:
03B-records,03C-geometry,03D-records/blocks,03E-nodes/references/ownership,
03F-records/diagnostics. The enumerable yields one item, cancels immediately before
the second item, and the existing constructor must throw without publishing an
object. It is an owned-contract negative test, not a fabricated source proof.
Actual complete source parsing supplies every precursor.

**Known limitation:** immutable sealed upstream results expose no deterministic
mid-CPU iteration callback. Adding a production hook only for03G would violate the
owner's production-code policy. No such hook was added. All listed loops have
cooperative checks and finite resource caps, but scheduling latency and dynamic
cancellation at each inner CPU location have not been measured/proved. These rows
remain explicitly limited source inspections for independent review. No exhaustive
responsiveness or arbitrary concurrent scheduling claim.

Evidence:16 new executed cancellation tests, unchanged prior cancellation tests,
`final/cancel-*.json`, final TRX and unchanged production-source hash audit.
