> **Current status — 2026-10-15 whole-PHASE03 final gate.** Native Text DXF
> extraction 03A–03F, deterministic QA 03G and the diagnostic-only Viewer 03H
> are implemented; each has its final independent `PHASE_03A_PASS` through
> `PHASE_03H_PASS`. See [final release gate](../status/PHASE_03_FINAL_GATE.md) for the current
> integrated build, tests, limitations and owner-acceptance status.
> ADR-015's additive raw boundary, Accepted ADR-016 and Accepted ADR-017
> together govern the implementation: Raw is runtime-only; Dxf references Raw;
> Desktop owns explicit managed-file hash/read lifecycle and WPF presentation.
> Managed AC1032 strict UTF-8 Text DXF is authoritative; DWG decoding and CAD SDKs
> are not production capabilities. Snapshot coverage remains Partial, including
> semantic Unsupported 52 (VPORT 1, APPID 3, OBJECTS 48); LOW-01 remains accepted.
> PHASE04, commit, push, PR and tag are **not authorized** by this gate.
>
> **Historical checkpoint notice:** earlier dated "current task", "STOP before03A",
> "proposed projects", "not implemented" and "ADR017 reserved" statements below
> record the earlier design/housekeeping authorization, superseded by the owner's
> subsequent 03A–03H instructions. They are not current implementation status.
> Earlier Proposed ADR-016 references are superseded by its Accepted decision;
> ADR-017 is now Accepted. Technical phase boundaries and future-phase requirements
> below remain in force. Original checkpoint text is retained without rewriting
> historical reports/reviews or changing accepted architectural decisions.

# ADR-016-Native-DXF-Reader: Native Text DXF input and reader architecture

## Status
Accepted — 2026-10-14 (Asia/Seoul). Antigravity Native Text DXF architecture review verdict **APPROVED_FOR_NATIVE_DXF_IMPLEMENTATION** (reviews/PHASE_03_REVIEW.md, Native DXF review §15); owner explicitly directed acceptance after incorporating terminology/source-identity findings and final resource-limit housekeeping. Earlier Proposed text is archived under artifacts/phase03/native-dxf-housekeeping-20261014/ADR-016-proposed-history.md.

This accepts architecture, not implemented parser compatibility or phase completion. Current owner task ends after document verification/report, before03A; no parser/Raw-Dxf production projects/Viewer/PHASE04 code, SDK install, commit or push now.
## Context
ACadSharp capability spike ended ACADSHARP_CAPABILITY_FAIL; its report/evidence remain unchanged. The owner explicitly selected user-controlled external DWG Save As/Export DXF, then a DtoB-owned native reader. Direct DWG and CAD SDK selection are no longer V1 requirements. CAD IR v1 requires normalized mm and cannot represent lexical evidence/native unknown units honestly; ADR015 preserves the additive raw boundary.

## Alternatives

| Option | Evaluation and disposition |
|---|---|
| DtoB-owned Text DXF lexical/structural parser | Selected: exposes original group-code spelling/location/presence with deterministic diagnostics, no SDK dependency and a bounded Controlled V1 scope. |
| Patch/integrate ACadSharp or select ODA/RealDWG | Rejected: owner chose native DXF and explicitly ended SDK review; vendor transforms/defaults and proprietary dependencies are unnecessary for this source contract. |
| Direct DWG parser or automatic CAD conversion | Rejected: beyond V1, much larger format/host/licensing surface; users explicitly export DXF outside DtoB instead. |
| Regex entity extraction into normalized CadDocument | Rejected: loses ordered/repeated codes, control groups, source provenance and structural accounting; incorrectly conflates raw facts with PHASE04 normalization. |

## Decision

Select pure net8 package architecture: DtoB.Cad.Raw (owned source-evidence/raw contracts) and DtoB.Cad.Dxf (lexical/section/table/entity parser, references Raw only). No third-party CAD/WPF/Revit/private-DLL dependency. Do not create them in this pre-implementation housekeeping turn. Existing normalized DtoB.Cad/IDrawingReader/CadDocument and Core geometry contracts unchanged.

```text
DWG -> user's external Save As/Export DXF
Text DXF bytes -> lexical pairs -> section/object records
-> typed raw facts + source evidence -> RawDrawingSnapshot
-> PHASE04 normalization / CAD IR -> PHASE05+ -> BIM IR -> Revit Adapter
```

V1 first supported profile is **Text DXF + AC1032 + strict UTF-8**, ASCII subset naturally supported, optional UTF-8 BOM at byte0, **CRLF / LF supported**. Korean and other valid non-ASCII UTF-8 characters are allowed; bytes>=128 are not forbidden. Parser support remains to be verified by actual fixtures/tests; other versions require explicit profile gates. General version capability is a future explicit profile, fixture and reviewed gate. BinaryDXF, directDWG, UTF16/32/nonstandard text, DWF, cloud/host conversion are UnsupportedInput. No filename-derived version or silent fallback.

### Authoritative source identity

DtoB V1 source identity begins exclusively at the **imported managed Text DXF**, the authoritative source-byte truth:

```text
DrawingId + RevisionId + FileHash + Original DXF Handle
```

OriginalHandle means the handle written in that imported DXF, preserving its raw spelling plus validated numeric/canonical form. It does NOT mean an upstream DWG database handle unless independently proven. DtoB guarantees neither semantic, structural nor identity equivalence between upstream DWG and exported DXF. DtoB assumes no responsibility for third-party CAD export changes to handles, anonymous blocks, dimensions, proxy/custom objects, unsupported records or database structure. Faithful extraction/diagnosis of the imported managed Text DXF remains DtoB's responsibility. **No DWG/DXF reconciliation in PHASE03 or PHASE04.** External Save As/Export is outside DtoB; no automatic conversion or upstream identity inference.
### Lexical/source ownership

DxfRawPair retains raw code/value spelling, parsed group code, ordered pair index and code/value physical lines; optional exact byte span. Source field evidence references immutable file hash/revision and owned pair/record ranges. Keep repeated groups and control/subclass/XDATA context. Values are never globally Trimmed or irreversibly decoded. Invariant checked numeric conversion leaves invalid/nonfinite raw evidence and diagnostics. Header code5 is not an object handle; DIMSTYLE code105 is identity; contextual330 owner differs from reactor pointers.

Explicit SECTION/ENDSEC/EOF, TABLE/ENDTAB, BLOCK/ENDBLK and legacy/attribute sequences; unknown content retained with SUPPORTED/IGNORED_WITH_DIAGNOSTIC/UNSUPPORTED/INVALID disposition and source location. Do not generate source handles or let parser errors become valid empty drawings. Own source evidence once with bounded buffers/evidence/diagnostics and resource refusal, not unlimited reflection bags.

### Encoding/version policy

The actual controlled fixture is ASCII-only, no BOM, AC1032, $DWGCODEPAGE ANSI_1252; it cannot prove multilingual encoding compatibility. Accepted AC1032 Text DXF profile is strict UTF-8, ASCII subset naturally supported, optional UTF-8 BOM, CRLF / LF. Valid Korean/multilingual UTF-8 is permitted; the empirically 7-bit ASCII fixture alone does not prove those cases. Retain DWGCODEPAGE metadata; do not switch that profile to ANSI/system encoding. Non-ASCII UTF8/BOM/Unicode-escape support needs reviewed fixtures before claimed coverage. Invalid bytes/unsupported BOM/binary input produce explicit UnsupportedEncoding/UnsupportedInputFormat. Unproven versions produce UnsupportedVersion. No replacement-character decoding, implicit older-codepage support or guessed profile.

[Maintained ezdxf encoding documentation](https://ezdxf.readthedocs.io/en/stable/dxfinternals/fileencoding.html) describes newer UTF-8 versus legacy codepages; it informs this limited proposal, not a production dependency or proof of parser support. Detailed field maps use [Autodesk DXF codes](https://help.autodesk.com/cloudhelp/2024/ENU/AutoCAD-DXF/files/GUID-3F0380A5-1C15-464D-BC66-2C5F094BCFB9.htm) and [header reference](https://help.autodesk.com/cloudhelp/2024/ENU/AutoCAD-DXF/files/GUID-A85E8E67-27CD-4C59-BE61-4DC9FADBE74A.htm); compatibility still requires actual tests.

### Initial development resource limits

All are finite configurable/injectable ReaderLimits defaults, checked before unbounded buffering/allocation:

| Limit | Default |
|---|---:|
| Physical line, source UTF-8 bytes excluding line terminator and byte0 BOM | **64 KiB / 65,536 bytes** |
| Combined active control102 / XDATA1002 group nesting | **16** |
| Stored diagnostics, including reserved terminal resource diagnostic | **5,000** |
| All lexical pairs (including delimiters/comments/unknowns) | **250,000** |
| Input source bytes | **16 MiB / 16,777,216 bytes** |

Use stream length where known plus cumulative actual read counts; never trust persisted FileSize alone. Enforce line/pair/depth before append/push; no unconstrained ReadLine/ReadAllLines followed by a late limit check. Records/vertices/reference occurrences remain additionally bounded by raw pair/input limits and no occurrence expansion. At most4,999 ordinary diagnostics; attempting to consume the reserved last slot instead terminates with ResourceLimitExceeded, total diagnostics<=5,000. Never silently drop diagnostics and return success.

Configuration is validated finite/positive (diagnostics>=2); deterministic tests inject smaller bounds and check exact-limit vs first-over-limit behavior. Any exceeded limit returns **ResourceLimitExceeded**, bounded incomplete diagnostic evidence, **never truncated-success** or a usable complete snapshot. Measured controlled input is96,621 bytes/8,737 pairs/max line56 bytes. These are initial safety caps with development headroom, not performance/memory claims; later explicit measurement/review may revise them without disabling limits.

### Raw/normalized separation

PHASE03 extracts source coordinates/angles/units, raw definitions, INSERT placement parameters, text/dimension defining facts and owner/reference relationships only. No final/local/nested INSERT matrix, OCS->WCS, mm/radian normalization, effective ByLayer/ByBlock or measured dimensions. PHASE04 owns these operations with explicit source-default provenance and tested composition. MINSERT expansion remains unsupported with original base handle/array fields, diagnostic and safe continuation; no cells/IDs/Explode.

Minimal diagnostic Viewer consumes owned snapshot and displayable native-frame geometry; no second parser or inserted/nested block transform implementation. Unsupported projection is explicit. RawDrawingSnapshot is derived extraction plus QA evidence, not .dtob/project truth, normalized IR or Revit data.

### Read authority / lifecycle

ManagedFilePath resolved against actual CurrentFilePath under ADR013; no SourceFilePath IO/fallback. Explicit extraction may hash/read on a strict immutable selected-stream lease. Ordinary Open/Refresh/selection remains zero content/hash/sourceIO. Native parsing observes CancellationToken on owned loops/async reads, bounds resources, disposes through normal ownership, and rejects stale success/error by session/read epoch before UI publication. No project dirty/ID/hash/persisted schema mutation. Separate ADR017 remains reserved for integration lifecycle if justified.

## Consequences

DtoB takes responsibility for source-aware parsing/coverage/safety, so controlled raw counts/line samples/reference audits and malformed/unknown/resource/cancel fixtures are mandatory. Optional entity/version/encoding support is explicit rather than universal. External DXF export is a required user step for DWG; no automatic equivalence claim between DWG and exported DXF. Existing PHASE02 DWG registrations may remain readable as project metadata but are ineligible for native extraction.

## Migration Impact

No project v2, CAD IR v1, BIM IR, mm/coordinate or stable-ID migration. Accepted ADR001–015 remain unchanged in this final housekeeping; ADR015 already defines the additive native raw boundary. This acceptance does not migrate their contracts or numbering. Prior spike/evidence stays NOT SELECTED historical. Future solution/dependency-test additions are reviewed, not made now. Architecture review approved; STOP after final pre-implementation housekeeping before03A; no implementation/SDK installation/Viewer/PHASE04/commit/push.

Preserved follow-up/proposed wording in earlier ADR015 and historical design reports records its earlier state; the current ADR016 Accepted status and review disposition above govern. No ADR001–015 edit is required for this acceptance.
