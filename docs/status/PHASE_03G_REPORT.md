# PHASE03G — Native Text DXF extraction QA

**Status: PASS_WITH_KNOWN_LIMITATIONS — ready for independent Antigravity03G review.**

Local QA result,2026-10-15. No independent03G acceptance is implied. No03H or
PHASE04 authorization is implied. No production code changed during this campaign.

## 1. QA purpose and scope

Adversarial, deterministic validation of actual Text DXF→03A→03B→03C→03D→03E→03F.
Added QA tests, source byte fixtures, reviewed manifest, matrices, audit scripts
and this report. No new extraction feature, project schema, renderer or SDK.
No production hook was added merely for coverage.

## 2. Baseline state

Read all20 owner-required documents, including the complete03F review and corrected
evidence. Independent predecessors PHASE03A/B/C/D/E/F_PASS. Branch
`phase/03-cad-data-extraction`, HEAD `142100deb62e8eb3692ded84a95962548925731f`.
The checkout already contained prior03 work, including untracked native reader
files and tracked architecture changes; those were preserved. Before-change
snapshot:324 existing files, plus original checklist copy in ignored evidence.

Baseline Core/full Solution Release and exact existing Revit warning identity
gate PASS; **558/558 tests**,0failed/0skipped. Baseline evidence:
`artifacts/phase03/03g-qa-20261015/baseline/` and `baseline-gate.log`.
RawDrawingSnapshot existed; no viewer or normalization was added.

## 3. Controlled golden verification

Independent BCL physical lines/code0 scanner, literal reviewed pair/line goldens,
all unchanged prior source-oracle tests, and actual complete pipeline agree:

| Domain | Verified facts |
|---|---|
| Source |96621bytes;17474physical lines;8737ordered raw pairs|
| Structure |6sections;251HEADER variables;9tables;333children|
| Semantic |Supported281;Unsupported52;Invalid0;Deferred03C0;Deferred03D0|
| Unsupported |VPORT1+APPID3+OBJECTS48|
| OBJECTS breakdown |DICTIONARY12;ACDBDICTIONARYWDFLT1;ACDBPLACEHOLDER1;LAYOUT2;MATERIAL3;MLINESTYLE1;MLEADERSTYLE1;DICTIONARYVAR2;VISUALSTYLE25|
| Independent source inventory |HATCH0;DIMENSION4, all4 Supported|
| Geometry |89; unchanged native coordinates/degrees/absence|
|03D|93records;12block definitions|
| Identity |342inspected locations;327nodes;15nonidentity CLASS|
| References |458;438Resolved;20Null; no unresolved/ambiguous/malformed|
| Owner |317edges;10roots;0cycles|
| Snapshot |SourceEvidenceComplete=true;TypedMappingComplete=false;GraphValidated=true;IdentityCoverageComplete=true;ReferenceResolutionComplete=true;SnapshotAssemblyComplete=true;Coverage=Partial|

The03F independent-review HATCH/DIMENSION evidence correction is retained. No
obsolete “HATCH50/DIMENSION2” claim is used. Counts are test oracles, never
production constants.

All earlier approved HEADER/INSUNITS/ACADVER/codepage/extents/HANDSEED,
A-WALL/DIMSTYLE105-vs5, LINE/LWP/ARC, BLOCK/INSERT/TEXT/DIMENSION and Owner/Reactor
goldens reran unchanged. New QA explicitly checks POINT pair3028, separate SOLID
third/fourthY pairs2762/2765, MTEXT3019, TABLE header684, BLOCK2663, ENTITY2857,
named COLUMN_400/D_SINGLE_900/W_SINGLE_1200/*D1..*D4 and HANDSEED exclusion through
the Snapshot back to independent source lines. Every333 record association is
source-ordered; every source-span raw value matches BCL lines.

## 4. Fixture manifest

`datasets/controlled/phase03g/manifest.json`:86 disk sources = controlled1 +03B20
+03C16 +03D12 +03E18 +03F2 +03G17byte files. Every entry records FixtureId, Path,
Purpose, ExpectedStage, ExpectedStatus, mandatory ExpectedDiagnosticCodes,
ExpectedPreservationBehavior and OriginalSHA256. Every entry executes as a theory
case with before/after hash/content and read-only stream checks. Accepted lexical
values/code spelling/physical lines are compared against an independent BCL reader.

Expected outcomes are authored from hand-reviewed bytes and existing fixture
READMEs/assertions, not extracted from production outputs. Narrow-stage success
does not imply graph/Snapshot success. Diagnostics lists are mandatory subsets.
The generator reads and hashes older DXFs; it writes only its named new byte files
and manifest. Two successive regenerations produced the same manifest hash:

`1B3D1798CD1137C8F4933E44AB3CD8A2717DA3B3F9E7AAADC08A1E49E60B0C4B`.

Inline corruption cases have deterministic case IDs/source strings in checked-in
tests and export their actual source to ignored evidence. No random/external fuzz
dependency or exhaustive fuzz claim. Coverage matrix: `PHASE_03G_COVERAGE.md`.

## 5. Lexical corruption results

Actual byte fixtures cover invalid/truncated UTF8,UTF16/32 BOM,binary DXF/DWG
signature,NUL,bareCR,missing value,invalid code,-1/1072,out-of-position/repeated BOM,
mixed endings,valid optional BOM and multilingual text. All expected statuses and
codes PASS. Prior tests also cover malformed signatures/code overflow, invalid
attempt pair accounting, nonseekable oversized input, long lines, one-byte reads,
buffer boundaries and I/O failure. Budget refusals do not report truncated success;
stream remains caller-owned. Positive signed `+1` is accepted by the existing
numeric profile; negative out-of-range signs refuse. No new sign policy invented.

## 6. Structural corruption results

Manifest fixtures and named actual-source corruptions exercise missing/nested
sections/end markers,duplicate/trailing EOF,outside records, malformed/missing/nested
TABLE, malformed/duplicate HEADER values,missing/duplicate/non-AC1032 ACADVER,
unclosed102/1002 nesting and finite caps. Named corruptions repeat twice with
identical graph serialization and refuse Snapshot. Invalid structures never
masquerade as downstream complete evidence. Prior assertions were not weakened.

## 7. Geometry corruption results

Existing source fixtures/inline sources plus new end-to-end source corruptions
cover missing/duplicate/nonfinite LINE coordinates; absenceZ;zero-length;LW
counts/order/duplicate vertex fields/width/bulge/caps; invalid CIRCLE radii; ARC
raw degrees; separate repeated SOLID corners/missing required coordinate.
Invalid geometry refuses Snapshot; valid degenerate raw facts remain unchanged.
No tolerance, normalization, approximation or topology was added.

## 8. Block and annotation corruption results

03D source fixtures and unchanged tests cover malformed BLOCK boundaries and
recovery, raw nested/array INSERT, optional absences, foreign contexts, continuation,
TEXT absent/empty/Unicode/escapes/alignment, repeated/malformed MTEXT chunks and
raw-character caps, DIMENSION0/1/unsupported flags/encoded42, ATTRIB vs ATTDEF
and active SEQEND/missing/orphan/wrong roles. New named invalid INSERT/TEXT/MTEXT
sources explicitly refuse Snapshot and repeat deterministically. Following
independent records remain available after malformed boundaries/sequences.

## 9. Identity and reference graph corruption results

All18 reviewed03E sources reran with manifest expectations plus unchanged detailed
graph tests. Duplicate/case/leading-zero conflicts retain all nodes without a
winner; malformed/zero/missing/multiple identities refuse; arbitrary-length valid
hex strings retain original spelling. Owner/Reactor/Other/ExtensionDictionary/
XData contexts,null and ambiguous targets remain distinct. Self/two-node cycles
and competing owners retain evidence and invalidate ownership. Pointer cycles do
not become owner cycles. HANDSEED/DIMSTYLE5 exclusions and105 identity retained.
New named duplicate,unresolved Owner/Reactor,self-owner/cycle sources refuse03F.

## 10. Snapshot failure policy

Unchanged36 assembly tests plus new full-pipeline QA cover rejected lexical/
structural, geometry resource/cancel, invalid/incomplete03D, invalid03E, LOW-01,
association conflicts/foreign source or payload, caps and cancellation.
Rejected cases publish Snapshot=null and SnapshotAssemblyComplete=false.
Constructed corrupt DTO tests are explicitly owned-contract negatives; source
corruption proofs independently use real lexical→03F inputs. Intentional Unsupported
can still assemble Partial typed coverage without pretending to be supported.

## 11. Immutability campaign

Mutation attempts on lexical pairs,structural records,geometry,block/annotation
records,block children,MTEXT chunks,identity nodes/references/owners,Snapshot
records/diagnostics throw NotSupportedException. Serialization remains unchanged.
Caller lists are copied eagerly; clearing record/diagnostic lists after Snapshot
construction does not alter the333/59 frozen counts. Completeness is an immutable
record reference. Existing earlier constructor/list tests remain passing. Nine
deterministic cancellation-during-enumeration cases publish no object.

## 12. Resource-limit matrix

`PHASE_03G_LIMIT_MATRIX.md`:42 enforced configuration properties across03A–03F,
each below/exact/first-over =126 boundary executions. Reflected property coverage
prevents silent omissions. All42 over-budget results are ResourceLimitExceeded
with own completion false;03F Snapshot=null. Semantic Partial/InvalidInput below
a budget remains semantically invalid. Diagnostic thresholds include the reserved
terminal slot. Lexical MaxControlGroupDepth is explicitly reserved for03B;03B
depth is exercised. No default maximum-size memory/performance claim.

## 13. Cancellation matrix

`PHASE_03G_CANCELLATION_MATRIX.md` distinguishes execution from source inspection.
Six pre-canceled stages,one cancellation at actual lexical read30,and nine existing
freeze-enumeration points PASS without timing races/sleeps. Own canceled stages
never advertise completion; propagated failures refuse Snapshot. Constructors
interrupted during freeze throw before publishing an object.

CPU-only inner traversal/grouping/mapping/resolution/DFS/association loops have
cooperative checks and finite caps, confirmed by source inspection. Frozen sealed
inputs expose no deterministic iteration hook; dynamic cancellation inside each
CPU loop was **not** executed. No production instrumentation was added to claim
coverage. This remains a visible review limitation, not an assertion of exhaustive
cancellation responsiveness.

## 14. Culture and numeric validation

Dedicated tests run invariant,fr-FR and ko-KR thread contexts and restore both
culture settings in finally. Native1.5,1e+20,-0.0 parse identically, including
negative-zero sign bit. Complete controlled serialization has the same hash in
all contexts. No locale-dependent reinterpretation or degree conversion.

## 15. Encoding validation

V1 remains AC1032,strictUTF8,optional byte0UTF8 BOM,LF/CRLF/mixed endings. Korean,
accented Latin and emoji are preserved exactly. Invalid/truncated sequences refuse
without replacement. DWGCODEPAGE=ANSI_1252 is metadata, not a decoding instruction.
No ANSI/legacy encoding, binary DXF or DWG decoding acceptance is claimed.

## 16. Repeated full-pipeline determinism

Dedicated final tests:3fresh sequential streams per culture,3cultures =9runs;
two additional testhost processes in different temporary working directories
repeat those9runs each =27dedicated full runs. Invalid unrelated parses are
interleaved within each run; the separate processes also execute after the full
regression suite. No parallel test changes global CurrentDirectory.

Serialized equality includes all pair/structural/geometry/block-child/reference/
diagnostic/Snapshot order. Every run retains authoritative03F SHA256:

`4B91C83791F89DEC9BDC633C832001F2D1E537E7F0330BEE0666067CAE9A310C`.

Supplied provenance is exactly drawing-fixed/revision-fixed/official source hash.
The Snapshot has no machine path/timestamp/random GUID contribution. Working
directory processes independently locate the repository via test assembly base.
Evidence contains the controlled Snapshot JSON and three culture records per
process. No accepted digest replacement was necessary.

## 17. Source and hash immutability

Official source before/after SHA256:

`2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC`.

All86 manifest fixtures match their hashes and exact bytes after reading. Before
snapshot checks preserve324 earlier files except the explicitly appended checklist;
this includes prior tests/reports/reviews/ADRs,production,project metadata and
controlled/reference data. Checklist prefix is separately compared with its
before copy. Sources open FileAccess.Read/FileShare.Read; no project asset write.
Production static audit finds no source File/Directory path I/O or writer use.

## 18. Dependency audit

Raw has no ProjectReference/PackageReference; Dxf has only Raw ProjectReference
and no PackageReference. Both retain shared .NET8 target.23 production C# files
have no vendor/Revit/WPF/Core geometry/Project imports. Existing dependency/
architecture regressions pass. All new helpers live in tests or scripts/qa;
production project files and dependency graph are unchanged. Historical isolated
spikes are not selected backends or production dependencies.

## 19. PHASE04 boundary audit

Reviewed unchanged production files and targeted source scans find no unit/mm or
degree→radian conversion,Sin/Cos transform composition,OCS/WCS/AAA,INSERT matrix,
base-point compensation,occurrence/cell expansion,effective ByLayer/ByBlock style,
topology/snapping/deduplication,CAD IR or architectural recognition implementation.
Existing factual fields,raw source bookkeeping and handle canonical lookup do not
perform those later-stage operations. Static patterns supplement source/hash
inspection; they are not claimed to prove arbitrary future semantics automatically.

## 20. Unsupported-content verification

Controlled52 Unsupported remain explicit source records and diagnostics. New
ELLIPSE,SPLINE,HATCH,3DSOLID,ACAD_PROXY_ENTITY sources retain evidence with null
approximate payload and TypedMappingComplete=false. Legacy sequences and array
expansion remain gated. No unsupported record is silently removed or promoted by
valid identity/Snapshot assembly. TypedCoverage Partial is preserved.

## 21. LOW-01 V1 policy

**KNOWN_V1_POLICY, unchanged:** warning-only unresolved non-owner can produce
GraphValidated=true and IdentityCoverageComplete=true but
ReferenceResolutionComplete=false. Current assembler requires all three and
refuses Snapshot. New direct policy regression passes; no relaxation/repair.
This remains the unresolved LOW finding from03F for owner architecture review.

## 22. Full regression results

| Campaign | Total/pass | Failed/skipped |
|---|---|---|
| Before03G baseline |558/558|0/0|
| Final03G complete solution suite |**736/736**|**0/0**|
| New03G QA cases |178/178|0/0|
| Extra working-directory testhost A/B |3/3 each|0/0|

Unchanged prior groups:03A55,03B103,03C63,03D78,03E88,03F36,existing architecture
additions9,PHASE00/01/02 regressions126 =558. New178:manifest86,manifest coverage1,
determinism3,controlled goldens1,immutability1,LOW-01policy1,limits42,limit coverage1,
cancellation16,named corruption21,unsupported5. No prior assertion/test modified.

## 23. Build results and evidence package

Final **Core Release PASS**, **full Solution Release PASS**, exact pre-existing
Revit MSB3277 identity gate PASS. No new warning code/identity. Used existing
VisualStudio2022 MSBuild/VSTest fallback and SDK8.0.416 files; testhost.NET8.0.22.
dotnet CLI discovery still prints the known SDK-not-found message; fallback
MSBuild rebuilds exit0 and actual TRX proves tests ran. No toolchain/security
settings were changed. This is a pure QA change, so Revit host PING/UI execution
was not required or claimed.

Reproduce full build/tests:

```powershell
$env:DTOB_03G_EVIDENCE_DIRECTORY = Join-Path (Get-Location) 'artifacts/phase03/03g-qa-20261015/final'
./scripts/Verify-Phase00Foundation.ps1 -Toolchain VisualStudio -RevitInstallDir 'D:\프로그램\Revit 2025' -EvidenceDirectory $env:DTOB_03G_EVIDENCE_DIRECTORY
./scripts/qa/Invoke-Phase03GQaAudit.ps1
```

Ignored package `artifacts/phase03/03g-qa-20261015/` contains baseline/final Core,
solution and test logs/TRX;before/source/protected hashes;manifest hash;copied
coverage/limit/cancellation matrices;42limit result files;16cancellation result
files;named corrupt source/output evidence;culture hashes/controlled Snapshot;
two working-directory logs/TRX;dependency/scope and diff audits. Baseline and
final evidence are separate. Intermediate targeted logs are not final gate results.

## 24. Newly discovered defects and diff review

**No new production defect discovered; no production fix required.** No unresolved
BLOCKER/HIGH. Initial QA compilation caught an xUnit analyzer convention; initial
new assertions used three incorrect diagnostic *names*, corrected against the
existing enum/source grammar (InvalidTransition/UnclosedControlGroup). These were
test-authoring corrections, not suppressed parser failures. The audit script's
native warning output was converted to strings before JSON serialization to avoid
PowerShell ErrorRecord recursion. Complete final suite then reran successfully.

Reviewed task-only new test/script/fixture/manifest/docs content against before
hashes. No prior source change; checklist append only. `git diff --check` exit0
(only existing checkout LF→CRLF notices). New untracked text also separately
checked for strictUTF8/trailing whitespace because git diff excludes untracked
files. Invalid byte fixtures intentionally excluded from text decoding checks.
No accidental generated binaries/evidence staged; no commit/push.

Changed files:four `tests/DtoB.Tests/ExtractionQaCampaignTests*.cs` files;
two `scripts/qa/*Phase03G*.ps1` files;17 `datasets/controlled/phase03g/*.bin`
fixtures plus manifest/README;this report,coverage/limit/cancellation matrices;
append to `docs/status/PHASE_03_CHECKLIST.md`. Production/projects/ADRs unchanged.

## 25. Known limitations and unsupported cases

- LOW-01 strict graph/reference completeness gate remains V1 policy.
- Dynamic mid-CPU cancellation at each internal loop is source-inspected only;
  deterministic boundary/read/freeze cancellation executed. No scheduling guarantee.
- Finite hand-reviewed/generated cases are not exhaustive fuzzing. Lowered-cap
  boundary tests are not maximum-default memory or throughput benchmarks.
- One controlled exported Text DXF plus small fixtures does not prove arbitrary
  production CAD,legacy encodings/versions,binaryDXF,DWG or source reconciliation.
- Current supported raw geometry/block/annotation profiles remain unchanged;
  Unsupported coverage is explicit, not typed acceptance.
- CLI discovery limitation and pre-existing Revit reference warnings remain;
  actual fallback Release builds/tests passed with exact warning gate.

## 26. Independent review readiness

Ready for owner and independent Antigravity03G review with the cancellation
coverage limitation visible. Controlled goldens and all source hashes match;
all558 prior and178 new tests pass; all tested resource/cancellation failures
refuse false completion; authoritative determinism hash stable; no dependency or
PHASE04 leakage; no unreported production defect. Local
**PASS_WITH_KNOWN_LIMITATIONS** is not an independent verdict or fullPHASE03 PASS.

## 27. Explicit stop

No03H Viewer,noPHASE04,no normalization,noCAD IR,no project persistence change,
no SDK,no occurrence expansion,no semantic/BIM generation. No commit or push.
**STOP for independent Antigravity03G review.**
