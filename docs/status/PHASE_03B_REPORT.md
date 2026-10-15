# PHASE 03B — Native Text DXF Structural / Header / Tables Reader

Date: 2026-10-14 (Asia/Seoul). Branch: phase/03-cad-data-extraction. HEAD:142100deb62e8eb3692ded84a95962548925731f. No commit/push.

## Status / exact scope

**PASS_WITH_KNOWN_LIMITATIONS — targeted H-01/L-01 fixes verified; ready for independent Antigravity re-review.** The recorded independent verdict remains PHASE_03B_CHANGES_REQUIRED until re-review. This is a subphase implementation status, not independent approval or full PHASE03 completion. The real controlled file has validated section/table structure and Supported AC1032 version, but its mapping result is explicitly **Partial**, not a complete raw drawing. Current post-review source/evidence supersedes the earlier false DIMSTYLE limitation; pre-fix history is recorded below.

Implemented SECTION/ENDSEC/EOF structure, AC1032 gate, ordered HEADER variables and minimum typed facts, TABLE/ENDTAB and factual LAYER/LTYPE/STYLE/DIMSTYLE/BLOCK_RECORD fields, CLASSES declarations, source-addressable deferred/raw record inventories, control/reference roles, bounded diagnostics/resources and cancellation. Consumes only immutable 03A pairs; no source text re-read/re-tokenization in production 03B.

## Projects / changed files

Added within the existing pure projects:

- packages/DtoB.Cad.Raw/DxfStructuralContracts.cs
- packages/DtoB.Cad.Dxf/DxfStructuralLimits.cs
- packages/DtoB.Cad.Dxf/TextDxfStructuralReader.cs
- packages/DtoB.Cad.Dxf/TextDxfStructuralReader.Header.cs
- packages/DtoB.Cad.Dxf/TextDxfStructuralReader.Records.cs
- tests/DtoB.Tests/TextDxfStructuralReaderTests.cs
- datasets/controlled/phase03b/README.md and17 hand-authored .dxf files: unsupported-version, missing-version, duplicate-version, unknown-section, nested-section, missing-endsec, missing-eof, content-after-eof, malformed-header, duplicate-header, table-missing-name, nested-table, missing-endtab, unknown-table-count, dimstyle-identity, reactor-owner, korean-layer-style.
- docs/status/PHASE_03B_REPORT.md

Updated only PHASE_03_CHECKLIST.md with an appended03B evidence section. No solution/project/dependency configuration change in03B. The pre-existing dirty/untracked architecture/03A/spike work predates this task. All216 files captured before implementation remained byte-identical before the authorized checklist append; final protected comparison allows only that append. In particular03A lexical code/contracts/tests, FoundationDocumentTests, ADR001–016, historical reports/reviews/spike, Core/CAD/BIM/Project/Analysis/Desktop/Revit/IPC and original datasets are unchanged.

Targeted review-fix changes: TextDxfStructuralReader.Records.cs, TextDxfStructuralReader.Header.cs, TextDxfStructuralReaderTests.cs, phase03b/README.md, this report and checklist evidence append; added dimstyle-distinct-70-roles.dxf, dimstyle-duplicate-standard-flags.dxf, dimstyle-duplicate-text-fill-color.dxf. There are now20 supplemental source files. No raw contract/schema/ADR/project/dependency change in the fix. Review files and previous reports remain unchanged.

## Structural model / evidence ownership

DxfStructuralReadResult owns frozen Sections, Header, Tables, Records, Diagnostics and RecordDispositionCounts, and references the **same immutable DxfLexicalReadResult** as LexicalEvidence. No raw pair objects/text are duplicated into records. DxfSourceSpan contains inclusive zero-based pair indices and one-based physical start-code/end-value lines. Section/table/variable/record ranges reference that store. No guessed byte offsets, drawing UUIDs, upstream DWG identity or final RawDrawingSnapshot.

DxfSourceFact uses an explicit Text/Integer/Real kind, nullable parsed slots, Presence and Validity, frozen SourcePairIndices and a source span. Absent has no value/source; explicit0 is Present/Valid. Duplicate/invalid singleton retains every source index, no parsed default/first/last winner. DxfRawHeaderPoint is a raw HEADER fact grouping, not Core geometry or normalized mm. Table fields have stable factual names and ordered repeated pattern facts. UnmappedPairIndices + mapped field indices account for every record pair exactly once; the actual-file test checks this for every child and table header.

Record disposition counts cover the333 child/deferred/object/CLASS records, not section markers, table headers, HEADER variables or comment pairs. These have their own ranges/dispositions/inventories. Code999 stays in lexical evidence and record unmapped fields; one bounded CommentsRetained diagnostic explicitly classifies them as ignored for mapping with count/representative source. Unmapped optional HEADER variables are IgnoredWithDiagnostic under the explicit retain-without-typed-mapping policy, aggregated as UnmappedHeaderVariables; no silent loss.

## Section state machine / version acceptance

Outside -> code0 SECTION -> code2 nonempty name -> body -> code0 ENDSEC -> Outside -> code0 EOF. Comments do not start records and can occur around structural delimiters; trailing comments are permitted, meaningful content after EOF is rejected. No arbitrary section ordering enforced. Known sections: HEADER, CLASSES, TABLES, BLOCKS, ENTITIES, OBJECTS.

Reject nested SECTION, ENDSEC outside a section, missing name/ENDSEC/EOF, EOF-only file, duplicate singleton HEADER, stray fields/invalid transitions and malformed HEADER/TABLE boundaries. Unknown sections retain their original range/raw records, emit UnknownSection and produce Partial; their body is not assumed to follow known table grammar.

03A's VersionAcceptance remains **NotValidated** unchanged.03B uses a separate DxfStructuralVersionAcceptance: NotValidated / Supported / Unsupported / Invalid. Exactly AC1032 is Supported. Missing ACADVER, malformed value or duplicate ACADVER (even identical duplicates) is InvalidInput/Invalid. Valid AC1021/AC1024/AC1027/AC1009 is UnsupportedVersion/Unsupported; no filename inference. Unsupported profiles expose structural/header evidence but do not run the accepted table/record field map; StructureValidated remains false because the full03B profile checks were not completed. Supported version is separate from structural/mapping status and cannot override a later failure.

## HEADER extraction / source facts

All251 variables retained in source order. Code9 begins a variable; following pairs belong to it until next9 or ENDSEC. Empty/malformed variables and duplicate variable names are explicitly diagnosed; no dictionary overwrite.245 optional/unmapped variables retain their complete source ranges.

| Fact | Source pair / value line | Raw value | Typed result |
|---|---|---|---|
| ACADVER |3 /8|AC1032|Text AC1032, Present/Valid|
| DWGCODEPAGE |7 /16|ANSI_1252|Metadata only; no re-decoding|
| EXTMIN X/Y/Z |17/18/19, lines36/38/40|1e+20 each|+1e20 each, Present/Valid|
| EXTMAX X/Y/Z |21/22/23, lines44/46/48|-1e+20 each|-1e20 each, Present/Valid|
| HANDSEED |277 /556|161|HEADER metadata only, not object identity|
| INSUNITS |455 /912|4|Raw integer declaration4, no scale/authoring correctness assertion|

Inverted but finite extents retain their exact source values and produce UnusableReportedExtents state/diagnostic. No swap/repair/replacement bounds/Fit bounds. Missing optional units/Z/seed stays absent, not zero/mm/default normal. Scalar conversion is checked invariant culture; nonfinite/overflow values retain raw evidence and invalid fact state. No unit/frame/angle conversion.

## Tables / factual fields

Explicit TABLE + code2 name -> table header -> code0 child entries -> ENDTAB, only inside TABLES. Reject missing names/ENDTAB, nested TABLE, ENDTAB outside TABLE and stray fields. Keep source-declared70 count and independently measured child count; mismatch is a warning, never an allocation/read index or forced repair. Unknown tables/entries retain raw ranges and explicit unsupported diagnostics/Partial.

| Table | Independently scanned actual children | Mapping |
|---|---:|---|
| VPORT |1|Unsupported, raw retained|
| LTYPE |21|Name/handle/owner/flags/description/alignment/count/length; ordered49 and related pattern fields, no drawing/resource loading|
| LAYER |12|Name/handle/owner/flags/signed ACI/true color/linetype/plot/lineweight/plot-style reference; no effective colors/categories|
| STYLE |27|Name/handle/owner/flags/height/width/source-degree oblique angle/generation/last height/font file names; no font loading/measurement|
| VIEW |0|Unsupported table retained|
| UCS |0|Unsupported table retained|
| APPID |3|Unsupported entries retained|
| DIMSTYLE |12|105 identity,5 legacy setting, separate Flags and TextFillColor code70 roles, raw scale/arrow/text settings/style reference and all unmapped fields|
| BLOCK_RECORD |12|Table identity/name/handle/owner/raw70 declaration/layout/explodability/scalability; not BLOCK contents|

Layer names exactly:0, Defpoints, A-WALL, A-DOOR, A-WINDOW, A-FLOOR, S-COLUMN, A-GRID, A-TEXT, A-DIM, A-LEVEL, A-SECTION. A-WALL name pair1150/value line2302 is asserted with raw spelling and Present/Valid state.

**Corrected DIMSTYLE mapping:** code70 has distinct Flags and TextFillColor(DIMTFILLCLR) roles. Standard's Flags is pair1706/value line3414; code69 DIMTFILL is pair1726/value line3454; TextFillColor is pair1727/value line3456. Both70 values are source-confirmed0, Present/Valid with distinct spans. All12 DIMSTYLE entries are Supported; zero InvalidTableValue diagnostics on the controlled file. Genuine same-role duplicates remain Invalid with every occurrence retained. The previous claim that controlled70 fields were ambiguous was a parser bug, resolved as H-01 below.

## Deferred records / CLASSES / OBJECTS

Structural child inventory: TABLES88, CLASSES15, BLOCKS68, ENTITIES114, OBJECTS48 = **333**, independently compared with raw code0 boundaries per section/table. ENTITIES is a source section name, not a semantic/model-space classification. No block occurrence expansion or space-owner graph resolution.

Post-fix disposition totals: Supported99, Unsupported52, Invalid0, DeferredTo03C89, DeferredTo03D93. Deferred geometry/annotation records have no typed geometry fields, only type/scope/source/context/raw evidence. LINE/LWPOLYLINE/ARC/POINT/SOLID/CIRCLE/legacy geometry names are deferred03C; BLOCK/ENDBLK/INSERT/TEXT/MTEXT/DIMENSION/attribute/SEQEND names deferred03D. Recognizing boundaries does not claim family support. OBJECTS are all retained/Unsupported with located diagnostics; no AutoCAD database recreation. CLASS declarations retain names/application/flags/count fields; custom class behavior is not implemented or approved by structural support.

## Context-aware handles / references

Record-local100 subclass markers,102 brace groups and1001/1002 XDATA contexts are retained. Combined active102/1002 nesting is bounded16; unmatched/cross-record groups are InvalidInput.330 inside reactors is Reactor, inside XDictionary is ExtensionDictionary, inside XDATA is XData; it cannot populate RawOwner. Ordinary330 maps to owner only in explicit common/symbol-table context. Other pointer ranges remain tagged Other with source/subclass/application/control path. No global handle graph/resolution, canonical/numeric identity policy, cross-drawing IDs, synthetic replacement handles or generic first330 winner.

DIMSTYLE RawHandle uses105 only; absent105 stays absent even when5 exists. Code5 remains LegacyCode5Setting. HEADER HANDSEED is excluded from record identity. Raw handle spelling is preserved, not globally normalized/validated in03B;03E still owns global handle/reference validation. Every contextual reference source index within records/table headers is compared with the independent raw-code inventory (458 occurrences here).

## Diagnostics / statuses / completeness

Stable typed diagnostic codes, Information/Warning/Error severity, constant messages, source span/count; no timestamps/machine paths/exception strings. Includes requested version/section/header/table/extents codes, control/reference safety, comments/unmapped/deferred aggregation and resource/cancellation outcomes. Post-fix controlled result has **59 diagnostics**, zero InvalidTableValue errors: retained unsupported content/unknown tables,245 unmapped-variable aggregate with representative source, unusable-extents warning and182 deferred-record aggregate. Status remains Partial because of unsupported/deferred mappings.

Succeeded means03B supported structure/profile with no unsupported/deferred/invalid record mapping; it is not a final snapshot/whole CAD compatibility claim. Partial retains usable structural/factual evidence with explicit coverage limits. Malformed structure/version/header -> InvalidInput. Table scalar ambiguity/invalidity -> Invalid fact/entry plus Partial if boundaries remain intact. Lexical failure is not parsed as valid partial input; original lexical diagnostics remain in LexicalEvidence, with an explicit03B refusal and mapped failure status. UnsupportedEncoding at lexical level maps to structural UnsupportedInput without losing its encoding diagnostic. Resource/cancel/IO failure never publishes complete success.

## Resource limits / cancellation

Finite injectable DxfStructuralLimits, positive validation and diagnostics>=2:

| Dimension | Default | Controlled evidence |
|---|---:|---|
| Sections |32|6|
| Tables |32|9|
| Work records/HEADER variables/table headers |4,096|333+251+9 =593|
| Pairs per record/variable |512|Largest child record131; largest HEADER variable4|
| Reference occurrences |16,384|458 record/table-header references|
| Combined102/1002 depth |16|Observed1 in child records|
| Stored03B diagnostics |5,000|59 post-fix|
| Input lexical evidence pairs |250,000|8,737|

These measured development headroom caps are safety controls, not universal DXF limits/throughput guarantees. Existing03A bytes16MiB/line64KiB/pairs250,000/diagnostics5,000 remain unchanged and apply before03B. Structural guards precede bounded allocations/appends/pushes; max work/fields/references additionally constrain typed lists. Comments are bounded by input pair count. No reference graph/occurrence expansion or background monitoring.

At most4,999 ordinary structural diagnostics plus one reserved terminal diagnostic. Next ordinary diagnostic -> DiagnosticLimitExceeded/ResourceLimitExceeded, no silent eviction/truncated-success. Guard refusal produces bounded incomplete evidence, StructureValidated=false. Tests cover exact-boundary acceptance and each injected first-excess dimension, combined102/1002 stack and reserved terminal slot.

Cancellation checks at evidence validation, section/header/table/record traversal, field/context/reference loops, between stages and during result collection freezing/disposition counts, with checks before/after materialization. Cancellation -> Canceled with a terminal diagnostic, no success publication. Pre-canceled parser and deterministic cancellation during frozen-result construction tested; no fixed timing assertion. Individual synchronous loop checkpoints were source-reviewed; no timing/performance claim for mid-loop latency. API is synchronous CPU work with no file/stream/thread ownership; future managed integration must schedule it off WPF UI and own lease/hash/session/stale-result behavior.

## Controlled fixture / independent QA / original hash

Original96,621-byte/17,474-line/8,737-pair file unchanged:

**2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC**

BCL File.ReadAllLines plus independent code0/code9 source scanning verifies counts/names/order, section marker pairs0/557/680/2660/3419/4903, every record/table range, selected hand-inspected HEADER/LAYER line goldens, table counts, all mapped/unmapped pair accounting and reference occurrences. Expected values are not generated by production serializer/parser or copied from SDK evidence. Real controlled BLOCK_RECORD count12 is independently measured. The17 supplemental source files are hand-authored, labelled separately, and exercised through actual03A lexical +03B structural parsing.

Two separate read-only FileStreams/lexical reads ->03B results serialize identically. The first stream is disposed before structural parsing, proving03B has no original-file I/O requirement. Post-fix deterministic structural JSON SHA256: **2546B5513275D3EC1EEF025DA817249CC3F724A1BA4A986790041C94C3C4DB91**. Pre-fix digest0C61B7BB8E37AA0FC068514425EAAE624F223A500D8035BB52E7CF149905F74B remains historical. Source hash checked before/after parsing and final verification.

## Tests / build results

Original03A baseline **190/190 PASS**; original03B pre-review **285/285 PASS**. Final **post-review-fix293/293 PASS**, zero failures/skips:

| Group | Cases |
|---|---:|
| Original03B structural cases (targeted assertions/one faulty-role fixture corrected) |95|
| Added review-fix cases |8|
| Unchanged03A lexical tests |55|
| Existing03A dependency additions |9|
| PHASE00/01/02 regression baseline |126|

Coverage: actual source/oracle/hash/goldens/determinism; top-level transitions/order/unknown/comment policy; version gate/missing/duplicate/invalid/unsupported; header facts/absences/duplicates/nonfinite/raw ranges; all five table maps/unknown/ranges/count discrepancies; DIMSTYLE105/code5/HANDSEED and repeated70; reactor/XDictionary/XDATA/subclass roles; deferred raw-only boundaries; immutable collections; all resource dimensions/default validation/diagnostic overflow; cancellation/refused lexical evidence;17 actual supplemental files.

Core-only Release **PASS**; full Solution Release **PASS**, original exact known Revit MSB3277 assembly-warning identity gate **PASS**, no new compiler warning. Existing VS2022 MSBuild/Roslyn + SDK8.0.416 files/VSTest .NET8.0.22 fallback used; normal dotnet.exe remains absent, no toolchain/security changes. No actual Revit/UI verification necessary or claimed: no IPC/Revit/Desktop integration change.

Original implementation history: initial build caught three xUnit analyzer violations, corrected. First252-case run was251/252; the response then incorrectly reclassified valid different-role DIMSTYLE70 fields as invalid facts/Partial. Antigravity H-01 correctly identified this as a bug; that interim policy is now replaced by separate role mapping. Intermediate252/274/283 and pre-review285 evidence is retained, not final evidence. Review-fix targeted03B **103/103 PASS** and full gate **293/293 PASS** are authoritative.

## Dependencies / architecture / diff review

Raw project has zero NuGet/project/vendor/WPF/Revit dependencies. Dxf references only Raw; assets contain Raw project library only, zero NuGet packages. Existing runtime/dependency allowlist/forbidden-dependency tests pass unchanged. No ACadSharp/ODA/RealDWG/netDxf/private Autodesk DLL, Core geometry, Project/session, Desktop/WPF or Revit reference introduced. Historical failed SDK spike remains isolated and NOT SELECTED.

Self-review covered top-level/record delimiters, unknown-section grammar isolation, source/default preservation, context roles, duplicate70 handling, collection immutability, cancellation/materialization, resource bounds and no phase leakage. git diff --check PASS; new-file UTF-8/whitespace/source-scope scan PASS. The existing Git CRLF-conversion notices are not whitespace errors. Existing216 file hashes unchanged except the authorized checklist evidence append; no accepted ADR or prior tests/report/review modified.

## Evidence files

Ignored artifacts/phase03/03b-structural-20261014 preserves original pre-review implementation/build/285-test evidence unchanged. **Current authoritative post-review evidence:** artifacts/phase03/03b-review-fixes-20261014, with before.json, report-before.md, checklist-before.md, qa-before.json, targeted-build.log, targeted.log/targeted.trx, controlled-diagnostics.json, controlled-structural.json, controlled-independent-qa.json, final/core-build.log, build.log, tests.log/tests.trx, final-gate.log, diff-check.log and final-verification.json. Controlled QA exports from the amended test now write to this separate directory, preserving earlier evidence.

## Known limitations / failure cases / next step / STOP

AC1032 strict Text DXF only; unsupported profiles are explicit. Missing/duplicate/bad structure fails. Invalid scalar or genuine same-role duplicate retains source, no default/repair. DIMSTYLE mapping is limited to the tested field contexts; an eligible70 without a supported symbol-header/DIMTFILLCLR context is diagnosed and retained unmapped, never assigned by ordinal. Other settings remain source-addressable; no full DIMSTYLE semantic engine. Unknown VPORT/VIEW/UCS/APPID and all OBJECTS are preserved unsupported. No table-name/handle uniqueness/global graph validation; no upstream DWG reconciliation. Source scope is section/table only, not resolved model/paper/block occurrence semantics. Raw drawing/revision/hash identity association, strict managed lease/hash/session stale guards and worker/UI integration remain later approved work, not implied by pure parsing.

No03C RawLine/RawLwPolyline/RawArc/RawPoint/RawSolid/RawCircle/vertices/bulges; no03D RawBlockDefinition/RawInsert/RawText/RawMText/RawDimension/measurements; no RawDrawingSnapshot assembly, Viewer, PHASE04, mm/radian/OCS-WCS/INSERT/nested transforms/effective styles/CAD IR/semantic recognition or Revit generation. Source bytes/project state remain untouched.

Next step is **owner and independent Antigravity03B review only**. Stop here; no03C/03D/PHASE04 or commit/push.

## Antigravity Review Fixes — H-01 / L-01

Review: reviews/PHASE_03B_REVIEW.md, PHASE_03B_CHANGES_REQUIRED. Review/history file unchanged. The before-report and285-test evidence are preserved separately; this section and the current sections above describe the corrected source tree.

### H-01 — DIMSTYLE different-role code70 mapping (HIGH)

Root cause: MakeRecord called generic Map("Flags",70) for DIMSTYLE, treating standard symbol flags and DIMTFILLCLR as one singleton. The original report wrongly called valid fields ambiguous; this was a mapping bug, not an unsupported-input limitation.

Exact correction: DIMSTYLE alone calls DimstyleCode70Roles and passes disjoint index lists to Integer facts **Flags** and **TextFillColor**. Other table Flags mapping unchanged. Deterministic scanning uses existing eligible (non-control/non-XDATA) indices and active subclass markers. Within AcDbSymbolTableRecord/AcDbDimStyleTableRecord, or an unmarked minimal fixture, code2 Name opens the standard-flags run before variable payload. Code69 DIMTFILL opens the separate TextFillColor run. Next non-common payload ends a run; subclass transitions close runs. Common identity/pointer metadata and comments/control data do not select roles. All70 values in each run are collected, so same-role duplicates remain InvalidTableValue/Invalid facts and entry, without first/last wins. Unclassified eligible70 is diagnosed with exact source and remains unmapped. No broad DIMSTYLE schema/formatting/measurement engine.

Independent controlled source confirmation (pair indices are0-based):

| DIMSTYLE | Flags pair | DIMTFILL69 pair | TextFillColor pair | Values / final disposition |
|---|---:|---:|---:|---|
| Standard |1706|1726|1727|0 /0 / Supported|
| EZDXF |1774|1794|1795|0 /0 / Supported|
| EZ_M_100_H25_CM |1844|1864|1865|0 /0 / Supported|
| EZ_M_50_H25_CM |1913|1933|1934|0 /0 / Supported|
| EZ_M_25_H25_CM |1982|2002|2003|0 /0 / Supported|
| EZ_M_20_H25_CM |2051|2071|2072|0 /0 / Supported|
| EZ_M_10_H25_CM |2120|2140|2141|0 /0 / Supported|
| EZ_M_5_H25_CM |2189|2209|2210|0 /0 / Supported|
| EZ_M_1_H25_CM |2258|2278|2279|0 /0 / Supported|
| EZ_RADIUS |2327|2347|2348|0 /0 / Supported|
| EZ_RADIUS_INSIDE |2397|2417|2418|0 /0 / Supported|
| EZ_CURVED |2467|2487|2488|0 /0 / Supported|

Values were read from actual source lines, not copied from review expectations. The controlled test corroborates code2/code69 context, exact reviewed indices, raw value text, source span/value lines (value line=2*pair+2), Presence=Present/Validity=Valid and Integer matching invariant parsing of those source strings. Both70 indices are distinct and accounted once. Complete mapped+unmapped accounting and105 identity are checked on every real entry. Code5 remains LegacyCode5Setting; HEADER HANDSEED and330 reactor/XDictionary/XDATA behavior unchanged and regression-tested.

| Actual controlled output | Before fix | After fix |
|---|---:|---:|
| DIMSTYLE Supported / Invalid |0 /12|12 /0|
| All Supported records |87|99|
| All Invalid records |12|0|
| InvalidTableValue diagnostics |12|0|
| Total diagnostics |71|59|
| Unsupported / Deferred03C / Deferred03D |52 /89 /93|52 /89 /93|
| Total child records |333|333|
| Status / StructureValidated / Version |Partial /true /Supported|Partial /true /Supported|

Tests added/updated: actual controlled oracle now checks both facts for all12 records and both70 accounting; existing negative test corrected to genuine duplicate standard-flags context (two70 values before40). Added three actual fixture cases for different-role16/7, duplicate flags16/32, duplicate colors7/8; four contextual cases for missing69, run closure at71, foreign subclass and code70 inside control group. Exact source indices, role disjointness, duplicate diagnostics/fact spans,105/5 behavior and whole record accounting asserted. Verification: targeted103/103, full293/293, Core/full Release PASS.

### L-01 — Unmapped HEADER aggregate source (LOW)

TextDxfStructuralReader.Header.cs now supplies the first IgnoredWithDiagnostic HEADER variable's SourceSpan to the single UnmappedHeaderVariables aggregate. No per-variable diagnostic expansion. Controlled representative span is pairs4..5, physical lines9..12; count remains245. Added a two-variable regression asserting one aggregate, Count2, non-null exact first source; controlled test also checks the representative source. Verification: PASS in targeted/full suites; total diagnostic reduction is exclusively removal of H-01's12 false errors.

### Final verification / stop

Final test count **293/293**, including103 structural cases, unchanged55 lexical cases,9 dependency additions and126 legacy regression cases; zero failed/skipped. Core Release, full Solution Release and exact known Revit warning-identity gate PASS. No IPC/host integration change, no Revit/UI host claim. Original DXF SHA256 remains2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC. Independent stream repeat serialization equal; final structural SHA2562546B5513275D3EC1EEF025DA817249CC3F724A1BA4A986790041C94C3C4DB91. Diff/UTF8/dependency/protected-source checks PASS.

H-01 and L-01 implemented and verified; independent re-review remains pending. No03C/03D/PHASE04/RawDrawingSnapshot/Viewer/global handle graph/full DIMSTYLE semantics, no commit/push. Stop for Antigravity re-review.
