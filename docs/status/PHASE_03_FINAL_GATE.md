# PHASE03 — Whole-phase final release gate

Date: 2026-10-15 (Asia/Seoul). Implementation Owner: Codex.
Status: **PASS_WITH_KNOWN_LIMITATIONS**, submitted for owner acceptance.

## 1. Scope

This gate validates the accumulated, uncommitted Native Text DXF foundation:
03A–03F extraction → immutable factual RawDrawingSnapshot → 03G QA → 03H
diagnostic Viewer. No feature, parser refactor, normalization, PHASE04, commit,
push, PR or tag was performed. This gate changes documentation only: this report,
an append-only checklist entry, and additive current-status notices on ten current
entry/specification/ADR documents. Earlier bodies, reports and reviews remain intact.

Fresh evidence: `artifacts/phase03/final-gate-20261015/` (ignored local evidence).
Commands and exact results below distinguish this gate from historical subphase runs.

## 2. Final independent subphase verdicts

| Subphase | Authoritative independent review | Final verdict |
|---|---|---|
| 03A lexical | reviews/PHASE_03A_REVIEW.md | PHASE_03A_PASS |
| 03B structural/header/tables | reviews/PHASE_03B_REVIEW.md | PHASE_03B_PASS |
| 03C raw geometry | reviews/PHASE_03C_REVIEW.md | PHASE_03C_PASS |
| 03D blocks/INSERT/annotation | reviews/PHASE_03D_REVIEW.md | PHASE_03D_PASS |
| 03E identity/ownership | reviews/PHASE_03E_REVIEW.md | PHASE_03E_PASS |
| 03F snapshot | reviews/PHASE_03F_REVIEW.md | PHASE_03F_PASS |
| 03G deterministic QA | reviews/PHASE_03G_REVIEW.md | PHASE_03G_PASS |
| 03H diagnostic Viewer | reviews/PHASE_03H_REVIEW.md | PHASE_03H_PASS |

All final review/report bodies and the specification/checklist/ADR set were read.
Corrected 03F inventory governs: VPORT 1 + APPID 3 + OBJECTS 48 = 52.
Historical review corrections explicitly acknowledge earlier inventory mistakes;
they are not current assertions. No final subphase remains CHANGES_REQUIRED,
BLOCKED or REVIEW_REQUIRED. The failed historical SDK spike is a rejected option,
not a failed native implementation subphase. LOW-01 is carried forward (§18).

## 3. Git state and baseline

Branch: `phase/03-cad-data-extraction`.
HEAD: `142100deb62e8eb3692ded84a95962548925731f`, the pre-PHASE03 baseline.
No staged changes; no generated binary/evidence staged. Local reflog shows the
branch rename/checkout, without a PHASE03 commit. No commit/push/PR/tag command
was executed. Local refs provide no PHASE03 push evidence; this is not a claim
to have audited remote-server access logs. No reset, cleanup or deletion occurred.

The initial tree inventory is `before.json`; `tracked.diff`, `tracked-stat.txt`
and `git-status-before.txt` retain the initial accumulated diff. The gate-only
hash comparison permits precisely the documented status notices and checklist
append. Every existing product/test/fixture/review/historical report is unchanged
from that inventory. Existing historical spike files are preserved separately.

## 4. Complete accumulated diff classification

Paths below are repository-relative and exhaustive for nonignored changes.
Modified tracked: **14**. Untracked: **175**. Total: **189**.
No file is silently staged by this proposed classification.

| Category | Count | Disposition |
|---|---:|---|
| A production implementation/solution | 26 | intended |
| B Desktop diagnostic Viewer | 8 | intended |
| C automated tests/test integration | 13 | intended |
| D controlled fixtures/QA manifest | 92 | intended |
| E QA scripts | 3 | intended |
| F ADR/architecture/entry/specification docs | 11 | intended |
| G status/reviews/checklist, including this report | 27 | intended |
| H pre-existing work outside the native release payload | 9 | excluded, preserved |
| I generated ignored evidence/output | 2,967 (as enumerated) | excluded |

H consists of the isolated **related historical ACadSharp evaluation**, not
unknown or overwritten user work. It is outside the selected native implementation
and solution. There are zero unexplained/unrelated files. Excluding its nine
source files from the proposed native commit does not delete the spike or its
included historical decision reports; any later archival commit is an owner decision.

### Category A — 26 files

```text
DtoB.sln
packages/DtoB.Cad.Dxf/DtoB.Cad.Dxf.csproj
packages/DtoB.Cad.Dxf/DxfBlockAnnotationLimits.cs
packages/DtoB.Cad.Dxf/DxfGeometryLimits.cs
packages/DtoB.Cad.Dxf/DxfIdentityGraphLimits.cs
packages/DtoB.Cad.Dxf/DxfReaderLimits.cs
packages/DtoB.Cad.Dxf/DxfStructuralLimits.cs
packages/DtoB.Cad.Dxf/RawDrawingSnapshotAssemblyLimits.cs
packages/DtoB.Cad.Dxf/TextDxfBlockAnnotationReader.cs
packages/DtoB.Cad.Dxf/TextDxfBlockAnnotationReader.Sequences.cs
packages/DtoB.Cad.Dxf/TextDxfGeometryReader.cs
packages/DtoB.Cad.Dxf/TextDxfGeometryReader.Polyline.cs
packages/DtoB.Cad.Dxf/TextDxfIdentityGraphReader.cs
packages/DtoB.Cad.Dxf/TextDxfIdentityGraphReader.Owners.cs
packages/DtoB.Cad.Dxf/TextDxfLexicalReader.cs
packages/DtoB.Cad.Dxf/TextDxfRawDrawingSnapshotAssembler.cs
packages/DtoB.Cad.Dxf/TextDxfStructuralReader.cs
packages/DtoB.Cad.Dxf/TextDxfStructuralReader.Header.cs
packages/DtoB.Cad.Dxf/TextDxfStructuralReader.Records.cs
packages/DtoB.Cad.Raw/DtoB.Cad.Raw.csproj
packages/DtoB.Cad.Raw/DxfIdentityGraphContracts.cs
packages/DtoB.Cad.Raw/DxfLexicalContracts.cs
packages/DtoB.Cad.Raw/DxfRawBlockAnnotationContracts.cs
packages/DtoB.Cad.Raw/DxfRawGeometryContracts.cs
packages/DtoB.Cad.Raw/DxfStructuralContracts.cs
packages/DtoB.Cad.Raw/RawDrawingSnapshot.cs
```
### Category B — 8 files

```text
apps/DtoB.Desktop/Diagnostics/DiagnosticExtraction.cs
apps/DtoB.Desktop/Diagnostics/DiagnosticViewport.cs
apps/DtoB.Desktop/Diagnostics/RawDiagnosticScene.cs
apps/DtoB.Desktop/DiagnosticViewerWindow.xaml
apps/DtoB.Desktop/DiagnosticViewerWindow.xaml.cs
apps/DtoB.Desktop/DtoB.Desktop.csproj
apps/DtoB.Desktop/MainWindow.xaml
apps/DtoB.Desktop/MainWindow.xaml.cs
```
### Category C — 13 files

```text
tests/DtoB.Tests/DtoB.Tests.csproj
tests/DtoB.Tests/ExtractionQaCampaignTests.Adversarial.cs
tests/DtoB.Tests/ExtractionQaCampaignTests.Cancellation.cs
tests/DtoB.Tests/ExtractionQaCampaignTests.cs
tests/DtoB.Tests/ExtractionQaCampaignTests.Limits.cs
tests/DtoB.Tests/FoundationDocumentTests.cs
tests/DtoB.Tests/RawDiagnosticViewerTests.cs
tests/DtoB.Tests/RawDrawingSnapshotAssemblerTests.cs
tests/DtoB.Tests/TextDxfBlockAnnotationReaderTests.cs
tests/DtoB.Tests/TextDxfGeometryReaderTests.cs
tests/DtoB.Tests/TextDxfIdentityGraphReaderTests.cs
tests/DtoB.Tests/TextDxfLexicalReaderTests.cs
tests/DtoB.Tests/TextDxfStructuralReaderTests.cs
```
### Category D — 92 files

```text
datasets/controlled/phase03b/content-after-eof.dxf
datasets/controlled/phase03b/dimstyle-distinct-70-roles.dxf
datasets/controlled/phase03b/dimstyle-duplicate-standard-flags.dxf
datasets/controlled/phase03b/dimstyle-duplicate-text-fill-color.dxf
datasets/controlled/phase03b/dimstyle-identity.dxf
datasets/controlled/phase03b/duplicate-header.dxf
datasets/controlled/phase03b/duplicate-version.dxf
datasets/controlled/phase03b/korean-layer-style.dxf
datasets/controlled/phase03b/malformed-header.dxf
datasets/controlled/phase03b/missing-endsec.dxf
datasets/controlled/phase03b/missing-endtab.dxf
datasets/controlled/phase03b/missing-eof.dxf
datasets/controlled/phase03b/missing-version.dxf
datasets/controlled/phase03b/nested-section.dxf
datasets/controlled/phase03b/nested-table.dxf
datasets/controlled/phase03b/reactor-owner.dxf
datasets/controlled/phase03b/README.md
datasets/controlled/phase03b/table-missing-name.dxf
datasets/controlled/phase03b/unknown-section.dxf
datasets/controlled/phase03b/unknown-table-count.dxf
datasets/controlled/phase03b/unsupported-version.dxf
datasets/controlled/phase03c/arc-native-degrees.dxf
datasets/controlled/phase03c/circle-reviewed.dxf
datasets/controlled/phase03c/legacy-gated.dxf
datasets/controlled/phase03c/line-duplicate-x.dxf
datasets/controlled/phase03c/line-missing-y.dxf
datasets/controlled/phase03c/line-native-3d.dxf
datasets/controlled/phase03c/line-nonfinite.dxf
datasets/controlled/phase03c/line-optional-z.dxf
datasets/controlled/phase03c/line-zero-length.dxf
datasets/controlled/phase03c/lwpolyline-count-mismatch.dxf
datasets/controlled/phase03c/lwpolyline-full.dxf
datasets/controlled/phase03c/lwpolyline-invalid-order.dxf
datasets/controlled/phase03c/lwpolyline-missing-y.dxf
datasets/controlled/phase03c/point-angle.dxf
datasets/controlled/phase03c/README.md
datasets/controlled/phase03c/solid-degenerate.dxf
datasets/controlled/phase03c/unsupported-ellipse.dxf
datasets/controlled/phase03d/attributes-malformed.dxf
datasets/controlled/phase03d/attributes.dxf
datasets/controlled/phase03d/block-malformed.dxf
datasets/controlled/phase03d/blocks-nested.dxf
datasets/controlled/phase03d/dimension-aligned.dxf
datasets/controlled/phase03d/dimension-unsupported.dxf
datasets/controlled/phase03d/duplicate-nonfinite.dxf
datasets/controlled/phase03d/foreign-contexts.dxf
datasets/controlled/phase03d/insert-array.dxf
datasets/controlled/phase03d/legacy-seqend.dxf
datasets/controlled/phase03d/mtext-chunks.dxf
datasets/controlled/phase03d/README.md
datasets/controlled/phase03d/text-alignment.dxf
datasets/controlled/phase03e/ambiguous-target.dxf
datasets/controlled/phase03e/contextual-references.dxf
datasets/controlled/phase03e/duplicate-case.dxf
datasets/controlled/phase03e/duplicate-cross.dxf
datasets/controlled/phase03e/duplicate-same.dxf
datasets/controlled/phase03e/duplicate-zero.dxf
datasets/controlled/phase03e/identity-context.dxf
datasets/controlled/phase03e/malformed.dxf
datasets/controlled/phase03e/missing-required.dxf
datasets/controlled/phase03e/multiple-owners.dxf
datasets/controlled/phase03e/null-reference.dxf
datasets/controlled/phase03e/owner-cycle.dxf
datasets/controlled/phase03e/pointer-cycle.dxf
datasets/controlled/phase03e/README.md
datasets/controlled/phase03e/root.dxf
datasets/controlled/phase03e/self-owner.dxf
datasets/controlled/phase03e/unresolved-owner.dxf
datasets/controlled/phase03e/unresolved-pointer.dxf
datasets/controlled/phase03e/unsupported-target.dxf
datasets/controlled/phase03f/blocks-native.dxf
datasets/controlled/phase03f/circle-native.dxf
datasets/controlled/phase03f/README.md
datasets/controlled/phase03g/bare-cr.bin
datasets/controlled/phase03g/binary-dxf.bin
datasets/controlled/phase03g/dwg-signature.bin
datasets/controlled/phase03g/embedded-nul.bin
datasets/controlled/phase03g/invalid-code.bin
datasets/controlled/phase03g/invalid-utf8.bin
datasets/controlled/phase03g/manifest.json
datasets/controlled/phase03g/missing-value.bin
datasets/controlled/phase03g/mixed-endings.bin
datasets/controlled/phase03g/negative-code.bin
datasets/controlled/phase03g/noninitial-bom.bin
datasets/controlled/phase03g/out-of-range.bin
datasets/controlled/phase03g/positive-signed-code.bin
datasets/controlled/phase03g/README.md
datasets/controlled/phase03g/repeated-bom.bin
datasets/controlled/phase03g/truncated-utf8.bin
datasets/controlled/phase03g/utf16.bin
datasets/controlled/phase03g/utf32.bin
datasets/controlled/phase03g/valid-bom.bin
```
### Category E — 3 files

```text
scripts/qa/Invoke-Phase03GQaAudit.ps1
scripts/qa/New-Phase03GFixtures.ps1
scripts/qa/Verify-Phase03HBoundary.ps1
```
### Category F — 11 files

```text
ARCHITECTURE.md
CODEX_INSTRUCTIONS.md
docs/adr/ADR-015-Raw-CAD-Extraction-Boundary.md
docs/adr/ADR-016-Native-DXF-Reader.md
docs/adr/ADR-017-Reader-Lifecycle.md
DtoB_FINAL_MASTER_PLAN.md
PHASE_INDEX.md
README.md
START_HERE.md
tasks/PHASE_03.md
tasks/PHASE_04.md
```
### Category G — 27 files

```text
docs/status/PHASE_03_ACADSHARP_SPIKE_REPORT.md
docs/status/PHASE_03_CHECKLIST.md
docs/status/PHASE_03_FINAL_GATE.md
docs/status/PHASE_03_HOUSEKEEPING_REPORT.md
docs/status/PHASE_03_NATIVE_DXF_DESIGN_REPORT.md
docs/status/PHASE_03_NATIVE_DXF_HOUSEKEEPING_REPORT.md
docs/status/PHASE_03_ODA_EVALUATION.md
docs/status/PHASE_03A_REPORT.md
docs/status/PHASE_03B_REPORT.md
docs/status/PHASE_03C_REPORT.md
docs/status/PHASE_03D_REPORT.md
docs/status/PHASE_03E_REPORT.md
docs/status/PHASE_03F_REPORT.md
docs/status/PHASE_03G_CANCELLATION_MATRIX.md
docs/status/PHASE_03G_COVERAGE.md
docs/status/PHASE_03G_LIMIT_MATRIX.md
docs/status/PHASE_03G_REPORT.md
docs/status/PHASE_03H_REPORT.md
reviews/PHASE_03_REVIEW.md
reviews/PHASE_03A_REVIEW.md
reviews/PHASE_03B_REVIEW.md
reviews/PHASE_03C_REVIEW.md
reviews/PHASE_03D_REVIEW.md
reviews/PHASE_03E_REVIEW.md
reviews/PHASE_03F_REVIEW.md
reviews/PHASE_03G_REVIEW.md
reviews/PHASE_03H_REVIEW.md
```
### Category H — 9 files

```text
spikes/DtoB.Cad.ACadSharp.Spike/Audit-Reference.ps1
spikes/DtoB.Cad.ACadSharp.Spike/DtoB.Cad.ACadSharp.Spike.csproj
spikes/DtoB.Cad.ACadSharp.Spike/Fixtures/README.md
spikes/DtoB.Cad.ACadSharp.Spike/Fixtures/supplemental-basepoint.dxf
spikes/DtoB.Cad.ACadSharp.Spike/Fixtures/supplemental-minsert.dxf
spikes/DtoB.Cad.ACadSharp.Spike/Make-Supplemental.ps1
spikes/DtoB.Cad.ACadSharp.Spike/Program.cs
spikes/DtoB.Cad.ACadSharp.Spike/README.md
spikes/DtoB.Cad.ACadSharp.Spike/Verify-Evidence.ps1
```

Category I's **2,967** files are enumerated exactly, one path per row, in the local ignored
`artifacts/phase03/final-gate-20261015/ignored-files.csv`; its count and timestamp
are in `ignored-summary.json`. This is an as-enumerated inventory, including the
inventory files themselves, not a claim that future builds cannot generate files.
It covers artifacts, bin/obj, test/build/manual output and historical spike output.
These are reproducible/local evidence, not intended Git payload.

## 5. Dependency graph

```text
DtoB.Cad.Raw     → .NET8 runtime only
DtoB.Cad.Dxf     → DtoB.Cad.Raw
DtoB.Desktop     → DtoB.Ipc, DtoB.Project, DtoB.Cad.Dxf, DtoB.Cad.Raw
```

Project XML inspected; Raw/Dxf have zero PackageReference entries, Raw has no
ProjectReference and Dxf has exactly one, to Raw. Neither references Desktop,
Project, WPF, Revit or Core geometry. Desktop presentation is not a reverse
dependency. Runtime-linked diagnostic tests do not add WPF to the reader packages.
The 23 reader/contract C# sources plus their two csproj files total 25 package files.
No production ACadSharp, ODA, RealDWG, netDxf or Autodesk CAD SDK. The isolated
historical spike is not in DtoB.sln. Dependency tests and both QA audits pass.

## 6. Source-of-truth policy

Authoritative extraction input is the selected immutable **managed Text DXF**,
resolved from actual CurrentFilePath and managed asset metadata. Conceptual
identity is DrawingId + RevisionId + FileHash + Original DXF Handle.
OriginalHandle retains source spelling/evidence; CanonicalHandle is a separate
lookup key. No upstream DWG identity guarantee, DWG↔DXF reconciliation, direct
DWG decoder, external converter or CAD SDK backend exists in production.
SourceFilePath remains provenance only; extraction has no fallback to it.

## 7. Text DXF support profile

AC1032 Text DXF; strict UTF-8; optional UTF-8 BOM at byte 0; LF, CRLF and mixed
endings. Valid non-ASCII UTF-8 is supported by fixtures. DWGCODEPAGE is metadata.
DWG, binary DXF, UTF-16/32, ANSI/codepage guessing and unreviewed DXF versions
are outside this initial profile and explicitly diagnosed/refused. Registration
of historical DWG assets remains valid without granting extraction support.

## 8. Controlled-source final oracle

Source: `datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf`.
SHA256: `2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC`.

| Source fact | Verified value |
|---|---:|
| Bytes / physical lines / pairs | 96,621 / 17,474 / 8,737 |
| Sections / HEADER variables / tables | 6 / 251 / 9 |
| Semantic children | 333 |
| Supported / Unsupported / Invalid | 281 / 52 / 0 |
| Deferred03C / Deferred03D | 0 / 0 |
| Unsupported VPORT / APPID / OBJECTS | 1 / 3 / 48 |
| HATCH / DIMENSION | 0 / 4; all four DIMENSION Supported |
| Raw geometry / 03D records / block definitions | 89 / 93 / 12 |
| Inspected identity locations / nodes / nonidentity CLASS | 342 / 327 / 15 |
| References / resolved / null | 458 / 438 / 20 |
| Owner edges / roots / cycles | 317 / 10 / 0 |

This is not a Snapshot-only oracle. The fresh unfiltered suite executes the BCL
physical-line/pair comparison, independent section/table/record scans, every-node/
edge/owner-chain scanner in TextDxfIdentityGraphReaderTests, and corrected 03G
source inventory plus exact line goldens. It compares raw source handles, locations,
reference roles and chains independently with the production pipeline. Geometry
and 03D tests account for each relevant original record. The manifest separately
revalidates all 86 source-file hashes, including this existing controlled input.

## 9. Snapshot contract, completeness and hash

Immutable, in-memory, raw/factual and source-traceable; a PHASE04 input candidate.
Not persisted .dtob truth, normalized CAD IR, BIM IR or rendered geometry truth.

LexicalSucceeded, StructureValidated, GeometryMappingComplete,
BlockAnnotationMappingComplete, IdentityCoverageComplete,
ReferenceResolutionComplete, GraphValidated, SourceEvidenceComplete and
SnapshotAssemblyComplete are **true** for the controlled fixture.
**TypedMappingComplete is false; Coverage is Partial.** Identity validation does
not promote the 52 semantic Unsupported records. All retain raw evidence.

Fresh 03A→03F runs, three cultures, three repetitions with interleaved corrupt input:
`4B91C83791F89DEC9BDC633C832001F2D1E537E7F0330BEE0666067CAE9A310C`.
Fixed provenance is drawing-fixed/revision-fixed/controlled source hash.
`final/controlled-snapshot.json` hashes to this exact value. Separate testhost
working directories also reproduce it. This is the established serialization
digest, not a persisted schema or a guarantee for arbitrary caller identities.

## 10. Viewer contract, counts and hash

RawDrawingSnapshot → Desktop RawDiagnosticScene → WPF. The Viewer does not
reparse DXF or construct normalized geometry. ENTITIES and one BLOCK DEFINITION
local view remain separate, with no occurrence expansion or base compensation.

Controlled ENTITIES: LINE 45 + LWPOLYLINE 12 = **57** direct geometry records;
INSERT 17 + TEXT 36 + DIMENSION 4 gives **114** diagnostic records overall.
BLOCKS: **12** definitions, **44** total ordered children. INSERTs/annotations
are factual anchors, not rendered assembled CAD symbols/fonts/dimensions.

All 13 scene modes (ENTITIES plus 12 definitions) produce established digest
`9D179A0EFAC5EE803E8AEC2AC3C48E1AB0986F1C6159BCB1764E8071933963A7`
under invariant/fr-FR/ko-KR. Fresh `final/scene-*.sha256` all agree. No WPF pixel
hash is used; caller IDs, paths, camera and DPI are not this scene contract.

## 11. ADR-015 / ADR-016 / ADR-017 consistency

Accepted ADR-015 defines the additive raw boundary; its native amendment replaces
the earlier SDK path. Accepted ADR-016 selects pure native Text DXF. Accepted
ADR-017 integrates the explicit Desktop lifecycle without widening Raw/Dxf.
Earlier Proposed/reserved wording is now explicitly labelled historical by
additive notices; the original decisions/bodies and numbering are preserved.

Managed asset remains authoritative. Ordinary Project Open/Refresh/selection is
metadata-only, with no source I/O or managed hash/content scan. Explicit Viewer
Run may hash/read on a background Task; FileAccess.Read/FileShare.Read spans
hash → rewind → parse on the same stream. Cancellation/disposal and captured
epoch/project/path/drawing/revision/hash suppress stale successes and errors.
No SourceFilePath fallback, persistence/schema migration or identity changes.
This is the standard-filesystem lease contract, not a claim of absolute path-race
elimination on all storage/reparse environments.

## 12. PHASE02 and earlier regressions

The full suite retains the original 126 PHASE00/01/02 tests; nine later architecture
checks bring the foundation/legacy grouping to 135. Schema v2/v1 migration,
DrawingId/RevisionId/hash, immutable assets, source provenance, Save/Open/SaveAs,
failure rollback, clean/dirty semantics and historical DWG registration pass.
Project/Core/Geometry/Analysis/Revit production files are unchanged by PHASE03.
Viewer managed-only access, no project mutation, metadata-only ordinary paths,
lease/disposal and stale context behavior pass. UI extraction preserves the saved
project and clean state; it does not persist a Snapshot or synthesize a revision.
No new Revit functionality or host PING rerun is claimed by this gate.

## 13. Complete automated test gate

Fresh full unfiltered suite: **773 total, 773 passed, 0 failed, 0 skipped**.
TRX has executed 773, notExecuted 0, error/timeout/aborted 0.

| Group | Cases |
|---|---:|
| Foundation/legacy (including nine architecture additions) | 135 |
| 03A lexical | 55 |
| 03B structural | 103 |
| 03C geometry | 63 |
| 03D blocks/annotations | 78 |
| 03E identity | 88 |
| 03F Snapshot | 36 |
| 03G QA | 178 |
| 03H diagnostic Viewer | 37 |
| **Total** | **773** |

Per-class counts are in `test-groups.json`; final results in `final/tests.trx`.
No new feature/test was needed for this documentation-only gate. Existing source,
architecture and regression tests were rerun after the status notices. Additional
filtered working-directory determinism runs supplement, not replace, the full run.

## 14. Release build gate

Executed from the repository root:

```powershell
$env:DTOB_03G_EVIDENCE_DIRECTORY = "$PWD/artifacts/phase03/final-gate-20261015/final"
$env:DTOB_03H_EVIDENCE_DIRECTORY = $env:DTOB_03G_EVIDENCE_DIRECTORY
./scripts/Verify-Phase00Foundation.ps1 -Toolchain VisualStudio `
  -RevitInstallDir 'D:\프로그램\Revit 2025' `
  -EvidenceDirectory $env:DTOB_03G_EVIDENCE_DIRECTORY
```

Core Release: **PASS, 0 errors**. Full DtoB.sln Release/Rebuild: **PASS, 0 errors**.
Visual Studio 2022 MSBuild/Roslyn/VSTest fallback was actually used with .NET8;
normal dotnet SDK discovery is unavailable on this machine. Discovery notices
are preserved in logs; success is based on actual MSBuild exit/results, not those
notices. No toolchain/security setting was changed.

Only pre-approved Revit MSB3277 identities remain (the gate checks full identity,
culture, public-key token and pair, not merely warning code):

- Microsoft.VisualBasic 10.0.0.0 / 10.1.0.0, neutral, b03f5f7f11d50a3a.
- System.Drawing 4.0.0.0 / 8.0.0.0, neutral, b03f5f7f11d50a3a.
- WindowsBase 4.0.0.0 / 8.0.0.0, neutral, 31bf3856ad364e35.

No new warning code/identity. Core/build/test logs are in `final/`.

## 15. Fresh QA results

03G QA audit and 03H boundary audit: **PASS**. Revalidated manifest 86 entries,
SHA256 `1B3D1798CD1137C8F4933E44AB3CD8A2717DA3B3F9E7AAADC08A1E49E60B0C4B`;
all source hashes match. Controlled, Snapshot and three scene hashes match.
Dependency/scope guards pass; different-working-directory full pipeline hashes
agree. Protected-source checks preserve all product/tests/fixtures and historical
reports/reviews. Checklist original prefix remains byte-for-byte text intact.

The initial gate audits used the captured final integrated tree, not the old
pre-03H baseline. The post-notice audits in `post-docs-audit/` use the documented
post-status-notice snapshot. `gate-only-changes.json` separately checks changes
against the untouched original gate baseline; this prevents rebaselining from
concealing any production change. Existing audit scripts were not modified.

`git diff --check`: exit 0. Git emits existing LF→CRLF conversion notices, not
whitespace errors. Strict UTF-8 was checked for every changed text file, including
untracked files; the 17 deliberate byte fixtures are explicitly exempt. Exact
trailing-line inventory is `all-text-checks.json`. Preserved exceptions: raw MTEXT
chunk trailing space (phase03d/mtext-chunks.dxf line 22), original master Markdown
hard breaks, one historical review line, and historical spike source whitespace.
No newly authored trailing whitespace; no source fixture byte was trimmed.

## 16. Actual Desktop smoke evidence and exact-build correspondence

Actual Release UI evidence remains in
`artifacts/phase03/03h-viewer-20261015/manual/`; `README.md` there documents capture
ordering and limitations. No new UI run is fabricated for this gate. Fresh rebuilt
binaries match the actual final 03H manual build receipt exactly:

- DtoB.exe SHA256: `98928E037B830ECE6995169704E947A32A398A847FEEB05BBA66532B1CCC6075`.
- DtoB.dll SHA256: `FA15D240456B317D95044A987C528E013BEFF89B378A905834FEA8DF8F6B2839`.

`ui-build-correspondence.json` also records that gate source changes are documents
only. All production code is unchanged since the final reviewed 03H source tree.

| Actual observation | Retained evidence |
|---|---|
| DtoB.exe launch, project open/restart via Recent, controlled extraction/summary/ENTITIES | 03H report/manual README and final-entities.png/txt |
| Fit, Zoom, Pan, Reset | final-zoom-pan.png, final-fit-reset.txt; camera 0.025606→0.030728, pan 40/20, reset 1/0/0 then Fit |
| LINE source trace | final-line.png/txt: record191, B7, A-GRID, pairs3671–3682/physical lines7343–7366 |
| INSERT source trace | final-insert.png/txt: record199, C3, D_SINGLE_900, native4000/2550/0, rotation90, scale absent |
| COLUMN_400 local block/base trace | final-column400.png, final-column-base.png/txt: one local LWP child, BLOCK116/96, raw base000 |
| Unsupported Inspector | final-unsupported.png/txt: record332 DICTIONARYVAR, handle160, pairs8729–8734 |
| DWG external Text DXF export guidance | dwg-guidance.png/txt |
| Stale-context clearing | stale-invalidation.png/txt: scene/summary/Inspector/list/diagnostics/camera cleared; Run disabled |
| Viewer close leaves Desktop stable | 03H report/manual host observations; tested instances closed normally |

Initial native file-dialog selection required owner assistance because the UI
tool reported a stale cached element; later opens used Recent Projects. Some
immediate accessibility captures lag one UI tick; final-column400.txt is explicitly
not the block-mode text oracle. Corrected base trace and screenshot provide that
evidence. Detailed interactions preceded the final small overflowing-circle guard;
the final 773-test build was launched again for controlled extraction/autoFit/
summary/scene. The rare overflow case has an automated regression; this gate
does not claim every UI action was repeated after that guard or exhaustive DPI/
pixel/scheduling coverage. Existing evidence plus exact build/source correspondence
satisfies the requested reconfirmation without overstating the manual execution.

## 17. Known limitations and unsupported cases

Controlled initial profile only (§7); no general production-CAD compatibility
claim. Unsupported records remain explicit source evidence; no silent discard.
Raw source units/degree angles/presence/extrusion remain unnormalized. MINSERT
metadata is preserved with unsupported expansion, without cells/occurrence IDs.
No unit/default resolution, OCS-WCS, nested blocks, effective styles, fonts,
dimension calculation, 3D CAD fidelity or optional layer filtering. Viewer XY
projection, raw INSERT anchors and bulge chords are labelled diagnostic.
Resource limits are bounded; 42 configuration limits × 3 boundaries and 16
pre/read/freeze cancellation cases execute. Mid-CPU-loop dynamic cancellation
injection is not exhaustive; bounded loops/token checks also have source review.
Standard filesystem/reparse lease limits, local toolchain/manual-tool limitations
and caller-dependent Snapshot identity are documented above. These are accepted
scope/verification limits, not hidden data corrections or new HIGH defects.

## 18. LOW-01 — KNOWN_V1_POLICY

ReferenceResolutionComplete == false causes 03F to refuse Snapshot, even where
an unresolved non-owner pointer is warning-only and the ownership graph is valid.
This conservative V1 gate is intentionally retained; the Viewer exposes its
structured refusal. Existing `WarningOnlyUnresolvedPointerRemainsKnownV1SnapshotRefusalPolicy`
and associated identity/assembly tests pass. No relaxation was attempted. Broader
partial-Snapshot admission requires a future architecture decision and tests.

## 19. PHASE04 leakage audit

Complete reader/contract and Desktop production additions were reviewed with
the accumulated diff, project XML and source guards. No normalized CadDocument/
IDrawingReader implementation, unit→mm or model degree→radian conversion,
OCS→WCS/AAA, INSERT matrices, base compensation, nested/MINSERT expansion,
effective ByLayer/ByBlock, topology/snapping/deduplication, recognition, BIM IR
change or Revit generation is introduced. Existing Core/Geometry/Analysis/CAD IR/
BIM IR/Project/Revit source is protected. Desktop-only camera/XY/ARC screen math
is presentation, never fed back as CAD truth. Searches are supporting evidence,
not a substitute for the reviewed dependency and code paths.

## 20. Documentation consistency audit

Found and resolved stale **current-status** declarations in eight entry/spec docs
and ADR-015/016 follow-up status text. Added explicit top notices with current
03A–03H implementation/review state, Accepted ADR-017, final gate link and current
STOP boundary. Original document bodies and historical reports/reviews are retained;
`documentation-status-fixes.json` proves additive-only preservation. This resolves
the obsolete stop-before03A/proposed-project/reserved-ADR017 wording without
rewriting technical requirements or prior decisions.

Current authorities agree: native Text DXF, extraction-first03A–03F, QA03G,
separate diagnostic03H, PHASE04-only normalization. Earlier SDK/Viewer-first/
DWG design records are explicitly historical. ASCII is an UTF-8 subset, not
the official encoding restriction. Corrected Unsupported52 is VPORT1+APPID3+
OBJECTS48; HATCH0/DIMENSION4 Supported. PHASE03F does not include the Viewer;
no whole-phase completion is inferred from pre-03H evidence.

Two review prose precisions do not change accepted verdicts: source tracing
exposes pair indices and physical line spans, **not byte offsets**; package
inventory is 23 C# sources plus two csproj, not 25 C# sources. No absolute
path-race prevention or instantaneous cancellation timing is claimed here.
Historical independent review text remains untouched; this report states the
actual implementation contract. No unexplained architecture/data finding remains.

## 21. Exact proposed PHASE03 commit payload

Exactly the **180** files individually listed in categories A–G in §4; no implicit
directory globs. `classified-files.json`/CSV additionally records each exact path,
category, SHA256 and IntendedCommit=true. This includes the final report/checklist,
accepted native ADRs, all eight independent reviews and historical decision reports.
It is a proposed reviewable payload only; **nothing was staged or committed**.

## 22. Exact exclusions

Exactly the nine category-H paths listed in §4, preserved as historical local SDK
evaluation outside the native release payload, plus every category-I path in
`ignored-files.csv` (as-enumerated count in `ignored-summary.json`). No proprietary
SDK binary, NuGet cache, generated DLL/EXE/PDB, bin/obj, test result, screenshot,
managed manual project asset tree or transient gate helper script is intended
for commit. Existing official PHASE02 fixture is unchanged/tracked, not a new
PHASE03 change. No unspecified unrelated file is proposed for inclusion.

## 23. Release readiness

All eight independent native subphase PASS verdicts are present. Fresh Core/full
Release builds, unfiltered 773-case suite, hashes, QA/dependency/scope/protected
source checks, exact-build UI evidence and corrected current-document status
support readiness for **owner acceptance of the whole-PHASE03 gate**.
Only accepted LOW-01 and explicitly documented profile/presentation/verification
limits remain. This is not authorization for Git release actions or PHASE04.
Next step is owner review of this report; then stop.

## Owner-authorized release amendment — 2026-10-15

This additive packaging amendment supersedes the historical 180-path payload,
fixture-storage and whitespace-gate details above. The accepted implementation,
test semantics, source expectations and subphase review bodies remain unchanged.
The owner accepted the whole-phase gate and authorized commit/push only after
the amended release gates pass. No PHASE04, PR, merge or tag is authorized.

### Root cause and source-data contract

Existing core.autocrlf=true cleaned CRLF bytes during ordinary Git staging.
That transformed the mixed-ending corruption/control fixture and historically
stored the PHASE02 controlled source as LF. Controlled DXF/byte fixtures are
source data. Root .gitattributes now contains exactly these narrow rules:

```gitattributes
datasets/controlled/**/*.dxf -text
datasets/controlled/**/*.bin -text
```

No Git configuration or unrelated text behavior is changed. The authoritative
PHASE02 fixture remains the accepted working CRLF source,96,621 bytes/SHA256
2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC.
Its historical LF Git representation,79,147 bytes/SHA256
AFB1FE6D8C7A2294F01637C88A6F3CD98BE9D528066AB30EF70B223A5FB6D14D,
is superseded by raw index canonicalization using the existing file mode,
git hash-object -w --no-filters and git update-index --cacheinfo. The working
source and expected manifest hash are not edited. The repaired mixed-endings.bin
raw blob stays28 bytes,2CR/4LF,SHA256
F5785446E12E2B72658F8B87C20F46FEE89036CC320A88DA2B35D39EE96E206B.

### Exact amended payload and exclusions

The current intended payload is **182 paths**, consisting of every A–G path
listed in §4 plus exactly these two additions:

- Category D: datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf.
- Category F: .gitattributes.

| Category | Amended count |
|---|---:|
| A | 26 |
| B | 8 |
| C | 13 |
| D | 93 |
| E | 3 |
| F | 12 |
| G | 27 |
| **Total** | **182** |

Category H's exact9 historical spike paths and all generated Category I evidence
remain excluded/preserved. docs/status/PHASE_03_GIT_RELEASE_REPORT.md is an
explicitly excluded,untracked local task-status report; it is not part of the182.
Historical inventories/counts above describe their original gate snapshots.
The amended exact path/hash inventory is saved separately under ignored
artifacts/phase03/git-release-20261015/amended/; original inventories are retained.

### Exact amended whitespace and reproducibility gates

ApprovedContentWhitespaceExceptions remains **exactly3**, with original bytes,
reasons and exact working/index SHA256 verification:

- datasets/controlled/phase03d/mtext-chunks.dxf.
- datasets/controlled/phase03g/bare-cr.bin.
- reviews/PHASE_03_REVIEW.md.

ByteFixtureEolCheckExclusions is a **separate exactly2-path category**:

- datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf.
- datasets/controlled/phase03g/mixed-endings.bin.

The latter two are exact-size/hash/raw-blob-validated source data,not additional
content-whitespace exceptions. Cached whitespace checking excludes only these
five literal paths; every other177 staged path must pass without a sixth exclusion.
All86 controlled DXF/bin sources must match their working and manifest bytes
directly in the index. Manifest JSON expected data remains unchanged. Before
commit the182-path set,fixtures,three existing content exceptions,full773-case
suite,Core/full Release,03G/03H,dependency/protected-source checks and established
source/Snapshot/scene hashes must pass again. After commit,direct committed-blob
and clean Git-archive extraction must independently reproduce the96,621-byte
controlled source and28-byte mixed fixture without EOL conversion. The updated
untracked Git release report records actual results/commit/push evidence; this
amendment does not infer success from a planned command.

PHASE_03_FINAL_PASS_WITH_KNOWN_LIMITATIONS
