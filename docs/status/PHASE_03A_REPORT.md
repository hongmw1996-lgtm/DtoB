# PHASE 03A — Native Text DXF Lexical / Group-Code Reader

Date: 2026-10-14 (Asia/Seoul). Branch phase/03-cad-data-extraction; HEAD142100deb62e8eb3692ded84a95962548925731f. No commit/push.

## Status / exact scope

**PASS_WITH_KNOWN_LIMITATIONS — 03A lexical scope ready for owner/Antigravity implementation review.** The limitation is explicit deferred version/structural acceptance, as allowed by the owner's03A instruction. This is not full PHASE03 completion or approval to begin03B.

Implemented pure .NET8 caller-stream UTF8/physical-line/group-code decoding, immutable ordered raw pairs/source lines, bounded limits and deterministic failure/cancellation results. No HEADER/section/table/entity/handle/unit/geometry interpretation, RawDrawingSnapshot assembly, Viewer or PHASE04 code. No Desktop/session integration, SourceFilePath access, DWG/DXF reconciliation or CAD SDK.

## Projects / changed files

Added:

- packages/DtoB.Cad.Raw/DtoB.Cad.Raw.csproj and DxfLexicalContracts.cs
- packages/DtoB.Cad.Dxf/DtoB.Cad.Dxf.csproj, DxfReaderLimits.cs and TextDxfLexicalReader.cs
- tests/DtoB.Tests/TextDxfLexicalReaderTests.cs
- docs/status/PHASE_03A_REPORT.md

Updated DtoB.sln (two projects/configuration/package-folder entries), tests/DtoB.Tests/DtoB.Tests.csproj (two pure project references), FoundationDocumentTests.cs (additive directions/forbidden-dependency/runtime tests), PHASE_03_CHECKLIST.md (03A evidence amendment only). Earlier uncommitted architecture/spike work predates this task. Accepted ADRs and existing production contracts/source files unchanged;82 protected legacy source/fixture/ADR/report/spike hashes match the start snapshot.

## Raw pair / result contract

DxfRawPair contains PairIndex, GroupCode, RawCodeText, RawTextValue, CodeLineNumber, ValueLineNumber. Pair indices start at **0**; physical lines start at **1**. Preserve whitespace, significant zeros, case, negative/scientific/NaN-looking values and literal DXF escape text. Values are strings, never typed entity data. Exact byte spans intentionally omitted; no offsets inferred from decoded lengths.

Owned immutable pair/diagnostic records and read-only copied collections. DxfLexicalReadResult contains explicit Status, VersionAcceptance, Pairs, Diagnostics, SourceBytesConsumed, PhysicalLineCount, HasUtf8Bom and observed LineEndings. Consumed byte counter includes guarded signature prefetch; fixed buffered reads can read ahead, bounded by input budget plus at most one over-budget probe byte. Physical line count counts completed/decoded lines; failed results do not claim full-source completeness.

**VersionAcceptance is always NotValidated in03A**, including lexical success. No $ACADVER/header mapping or SECTION/EOF validation. Older/missing versions and structurally invalid CAD can be lexically valid; callers must not claim AC1032 support/valid drawing until03B separately validates version/structure. Tests explicitly demonstrate this boundary. No silently accepted older-version compatibility claim.

## Encoding / lexical rules

Strict UTF-8 (ASCII subset and Korean/accented/emoji text accepted), optional UTF8 BOM only at input byte0; CRLF/LF, including mixed accepted line endings. Strip only terminators/BOM preamble. Blank value line valid; complete nonempty final value line may lack terminal newline. EOF after an orphan code line is InvalidInput. A final newline does not invent an extra empty physical line.

UTF16/32 BOMs, invalid UTF8, embedded NUL and misplaced/repeated UTF8 BOM marker explicitly fail UnsupportedEncoding. Bare CR is InvalidInput. Recognized DWG magic and Binary DXF signature fail UnsupportedInput; no conversion. U+FEFF outside the byte0 preamble is unsupported, not silently removed. DXF Unicode/M+ escapes remain literal. No Encoding.Default, codepage fallback or replacement decoding.

Code lines accept ASCII space/tab margins and optional decimal sign; digits/integer parsing are checked and culture-invariant. Floating code syntax, overflow, nondecimal digits and codes outside0..1071 produce located InvalidInput diagnostics. Valid range does not claim semantic support for every group code. Invalid code attempts remain diagnosed; raw pair output includes only valid-code pairs and can have index gaps on failed reads. Safely completed following pairs may be retained as incomplete evidence, never advertised as successful output.

## Resource limits

| Guard | Accepted default / implementation |
|---|---|
| Physical line | 65,536 UTF8 source bytes, excluding CRLF/LF and byte0 BOM; checked before append |
| Control/XDATA nesting | Configured16, **not interpreted/enforced by03A**; structural handling belongs03B |
| Stored diagnostics | 5,000; at most4,999 ordinary diagnostics and a reserved terminal failure slot; attempted overflow -> DiagnosticLimitExceeded / ResourceLimitExceeded |
| Raw pairs | 250,000; includes invalid-code pair attempts; checked before beginning an over-budget pair |
| Input | 16,777,216 bytes; stream length preflight when available plus actual bounded read/delivered-byte counters |

Limits immutable/configurable/injectable; positive values required and diagnostics>=2. Fixed4096-byte input buffer and signature prefix<=22 bytes; no ReadAllLines/ReadLine/unbounded pre-read in production parser. Input reads request at most remaining budget+one byte. Any exceeded implemented budget returns ResourceLimitExceeded and bounded incomplete evidence, never truncated-success or silent diagnostic eviction. Smaller injected exact/first-over-boundary tests passed. These are safety controls, not throughput/memory guarantees.

## Diagnostics / failure / ownership / cancellation

Statuses: Succeeded, InvalidInput, UnsupportedInput, UnsupportedEncoding, ResourceLimitExceeded, Canceled, IoFailure. Failures always carry stable typed code/severity/category/message and physical line/pair when known. Examples include InvalidUtf8, UnsupportedBom, InputTooLarge, LineTooLong, OddPhysicalLineCount, InvalidGroupCode, GroupCodeOutOfRange, PairLimitExceeded, DiagnosticLimitExceeded, Canceled, IoFailure. No timestamps/machine-specific IO exception text in deterministic content. Caller API misuse (null stream/invalid limit configuration) throws argument exceptions before parsing; IO/unsupported/disposed-stream faults have explicit results.

Reader starts at caller's current position; caller must supply the intended input start and immutable lease. It never seeks/rewinds/closes/disposes/writes/reopens input or mutates project state. Cancellation checked during reads, byte/line/pair processing, EOF and before/after result materialization; requested cancellation yields Canceled, no success publication. Caller retains stream ownership on success/failure/cancel. No thread abort or hidden worker/service. Later integration schedules decoding on a worker and owns ManagedFilePath/hash/session/stale guards;03A has no DtoB.Project dependency.

## Real controlled fixture / independent QA

Original datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf unchanged:

- **96,621 bytes**, empirically ASCII-only, no BOM, CRLF
- independent BCL File.ReadAllLines oracle: **17,474 physical lines / 8,737 pairs** (matches pre-existing design measurements)
- lexical result: **8,737 ordered pairs**, every code/value spelling and1-based line number compared to that independent source-line oracle
- SHA256 before/after: **2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC**

No semantic entity/section/handle counts claimed. Test reads the actual original with FileAccess.Read/FileShare.Read; original bytes are never modified. Pure result serialization SHA256: **04303C2F26C2645D4106CEC7652B84470B46C2F122E866DB3D697A540D0E130F**. Includes explicit NotValidated version state. Evidence: controlled-pairs.json and controlled-qa.json under ignored artifacts/phase03/03a-lexical-20261014.

## Tests / builds

Final **190/190 PASS**, zero failures/skips:

| Group | Cases |
|---|---:|
| New03A lexical tests | **55** |
| New dependency/architecture tests | **9** |
| Existing PHASE00/01/02 regression cases | **126** |

Coverage: actual fixture/hash/all lines, raw spelling/blank/escapes, CRLF/LF/final newline, UTF8/Korean/accented/emoji/BOM, invalid encodings/NUL/bareCR, DWG/Binary detection, empty/orphan/odd/overflow/noninteger/range errors, tiny line-byte/pair/input/diagnostic budgets, defaults/invalid config, read-only caller ownership/IO failures, pre/during-read/EOF cancellation, repeated/fragmented/buffer-boundary JSON determinism, no03B interpretation, frozen result collections and forbidden dependency mutations.

Core-only Release **PASS**; full Solution Release **PASS**, including Raw/Dxf/Tests/Desktop/Revit adapter; original exact warning-identity gate passed (known Revit MSB3277 baseline only). Existing VS2022 MSBuild/Roslyn + SDK8.0.416 files/VSTest .NET8.0.22 fallback used because normal dotnet.exe remains absent. No toolchain/security changes. No actual Revit host verification needed: IPC/Revit code/integration unchanged.

Initial run188/189 exposed a duplicated solution registration termination from the edit; repaired before final full build. Post-review EOF cancellation regression added; final build/test rerun is190/190. Initial/final evidence is kept separately; earlier189-pass run is not the final test count.

## Dependency verification / diff

Raw references only standard runtime; Dxf references only Raw plus standard runtime. Project assets: Raw has no NuGet libraries; Dxf has only DtoB.Cad.Raw/1.0.0 project library, zero package dependencies. No WPF/Revit/Autodesk/Core geometry/Project/CAD SDK. New dependency tests reject forbidden packages/frameworks/assemblies/targeting/project edges without weakening old rules. Historical ACadSharp spike remains outside solution and NOT SELECTED, not integrated/rerun.

Self-reviewed solution/test changes and new contracts/reader/tests for source scope, bounds, caller ownership, cancellation, raw spelling and explicit version deferral. git diff --check **PASS**, new code/report UTF8/whitespace checked. Evidence: artifacts/phase03/03a-lexical-20261014/final/core-build.log,build.log,tests.trx/tests.log,final-gate.log,final-verification.json,input-before.json,protected-before.json,initial-tests.trx/log,checklist-before-03a.md.

## Known limitations / stop

Lexical success alone does not establish Text DXF version/structure acceptance;03B must reject unsupported/missing versions and validate sections. Control-group nesting is configured only, no102/1002 stack in03A. No broad raw CAD models/RawDrawingSnapshot, semantic value/handle parsing, geometry/radians/mm/OCS/transforms/effective styles, diagnostic Viewer or project-managed/session integration. Invalid raw code lines are diagnosed rather than emitted with fabricated GroupCode. Fixed buffer/signature lookahead may consume ahead of the diagnostic line; caller owns any rewind/retry. No performance claim or universal DXF compatibility.

Ready for independent03A implementation review. **STOP: do not start03B+, Viewer or PHASE04; no commit/push.**
