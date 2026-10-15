# PHASE 03 — Native DXF Data Extraction Foundation

Native Text DXF design revision, 2026-10-14 (Asia/Seoul).

Status: **APPROVED_FOR_NATIVE_DXF_IMPLEMENTATION — final pre-implementation housekeeping**. Antigravity review and owner direction dated 2026-10-14 authorize closing Issues1–3 and accepting ADR016. Current task is documents only, then STOP before03A. No parser/raw production contracts/projects/Viewer/PHASE04 code, SDK install, commit or push. Architecture approval is not phase completion or parser compatibility evidence.

Branch: phase/03-cad-data-extraction; HEAD142100deb62e8eb3692ded84a95962548925731f. Earlier SDK-first checklist is archived byte-for-byte under artifacts/phase03/native-dxf-design-20261014/extraction-sdk-checklist-history.md. Historical reports/reviews/spike/evidence remain intact; earlier APPROVED_FOR_CAPABILITY_SPIKE does not approve this new reader implementation.

## 1. V1 DXF-only decision

**DtoB V1 uses Native Text DXF extraction.** Users externally Save As/Export DWG to Text DXF in their CAD application, then import DXF. DtoB does no conversion/automation and directly decodes no DWG. First compatibility profile: controlled **AC1032** Text DXF only; other versions are UnsupportedVersion until real fixture/regression gates add explicit profiles. Text DXF denotes the non-binary format; the V1 profile is **AC1032 + strict UTF-8**, ASCII subset naturally supported, optional UTF-8 BOM at byte0, **CRLF / LF**. Valid non-ASCII UTF-8 including Korean is permitted, never rejected merely for containing bytes>=128; actual parser acceptance tests are still required. See section17. No claim that every DXF/export is supported.


**Authoritative source identity begins at the imported managed Text DXF:** DrawingId + RevisionId + FileHash + Original DXF Handle. OriginalHandle means the handle written in that imported DXF, not an upstream DWG database handle unless independently proven. DtoB does not guarantee semantic, structural or identity equivalence between upstream DWG and exported DXF, and assumes no responsibility for third-party export changes to handles, anonymous blocks, dimensions, proxy/custom objects, unsupported records or database structure. No DWG/DXF reconciliation in PHASE03 or PHASE04. DtoB remains responsible for faithfully extracting and diagnosing the managed Text DXF itself.

Existing PHASE02 can register DWG and DXF; its historical behavior, stored registrations and project v2 remain unchanged. Future extraction rejects a selected DWG with UnsupportedInputFormat and an external-export instruction; existing project Open does not become invalid or dirty. Proposed Desktop import preference is DXF; narrowing existing registration UI is a later reviewed integration choice, not a schema/asset migration here.

## 2. ACadSharp disposition

**NOT SELECTED** for production. Historical ACADSHARP_CAPABILITY_FAIL and all original evidence remain unchanged. Nonzero-base-point rotated/scaled INSERT failure motivated preserving raw parameters and assigning transforms exclusively to PHASE04. Do not patch/fork/repair/re-run/integrate it or continue its backend review. No ODA/RealDWG installation/escalation. Isolated spike package/reference is historical outside solution; no production reference permitted. Its fixture source audit is reference QA, never the native implementation or sole oracle.

## 3. Exact objective

Authoritative managed Text DXF -> strict lexical pairs -> structural records -> typed owned raw facts + preserved source evidence -> RawDrawingSnapshot -> deterministic factual QA. Only afterward minimal diagnostic inspection. Every important value has a source field/range or explicit absence/invalid status. No meaningful source record silently disappears; all raw records have a disposition and count.

## 4. PHASE03 / PHASE04 boundary

| PHASE03 owns | PHASE04 owns |
|---|---|
| Strict text decoding, raw group codes, sections/tables/entity/object structure | Explicit source-unit resolution and mm conversion |
| Original text/handles/owners/block children/INSERT fields/native coordinates | OCS->WCS and model frames; local/nested block transform composition |
| Presence, syntax/reference diagnostics, source lineage and QA | ByLayer/ByBlock effective styles, normalization/cleanup/analysis structures |
| Derived diagnostic camera projection only, with limitations | Existing IDrawingReader -> DrawingReadResult -> CadDocument / strict CAD IRv1 |

PHASE03 does **not calculate final INSERT matrices**, nested occurrence transforms, measured dimension values, effective inheritance or CAD IR. Raw source angles retain the DXF unit (degrees for relevant angle codes); PHASE04 explicitly maps to Core radians. No default mm/normal/scale is inserted into source facts. Existing Core/BIM/Analysis/Revit/source-ID/schema contracts unchanged.

## 5. Lexical architecture

Read immutable stream forward with bounded buffers and cooperative cancellation. Recognize binary DXF/DWG/unsupported BOMs before structural parsing. Decode only the selected profile, using strict decoder fallback. Read a code line and its following value line; strip line terminator only. Whitespace on a code line may be parsed, but original code/value spelling remains. Empty value lines are valid evidence; a final newline is not an additional orphan line. An unterminated last value line can be accepted if EOF structure is complete. Odd physical lines/invalid code yield located fatal InvalidInput, never an empty success.

Lexical layer does not know CAD entities or treat HEADER code5 as object identity. Ordered source pairs are held once in an owned evidence store; records/typed fields reference indices/ranges instead of copying the entire input per entity. This is bounded O(input) evidence, not constant-memory fiction or a database. No reflection/vendor graph.

## 6. Group Code model

Proposed DxfRawPair: PairIndex; GroupCode; RawCodeText; RawTextValue; CodeLineNumber; ValueLineNumber; optional byte span when obtainable exactly; scalar parse state/value/diagnostic. SourceSpan maps to immutable input hash, pair and physical line range. Byte offsets are never guessed from UTF-16 string length.

Codes are invariant integer syntax within documented DXF ranges; unknown legitimate codes remain evidence with explicit mapping disposition. Typed conversion uses an explicit code/subclass/version map, InvariantCulture and checked integers; finite numeric values only. Raw spelling such as 1234.500, signed values and repeated codes is retained. Integers are not reinterpreted as doubles/handles merely because a conversion succeeds. Distinguish supported raw scalar from supported entity semantics.

Repeated codes are ordered lists (polyline vertices, MTEXT chunks, control groups, references), not a last-value-wins dictionary. Conflicting singleton fields are INVALID with all raw occurrences retained. Numeric typed values do not overwrite source text; no irreversible escape processing.

## 7. Section parser

Explicit state machine: outside section -> code0 SECTION + code2 name -> known section -> code0 ENDSEC -> outside -> code0 EOF. Require complete delimiters, one terminal EOF, no meaningful records after EOF, no nested SECTION and no duplicate singleton HEADER. Section order is not hardcoded beyond version/header gating; deferred reference resolution is a second bounded pass over owned records. EOF alone is not a valid drawing.

HEADER uses code9 variable boundaries; CLASSES records declarations without inventing handles; TABLES handles TABLE/ENDTAB; BLOCKS handles BLOCK/ENDBLK; ENTITIES and OBJECTS partition source records at code0. Code999 comments are retained and explicitly IGNORED_WITH_DIAGNOSTIC (aggregate count with representative locations within diagnostic budget), not entity starts. Subclass100, control102 and XDATA1001/1002 ranges remain structured evidence so repeated330 references are not confused with owner fields.

Every section/record: SUPPORTED, IGNORED_WITH_DIAGNOSTIC (explicit policy, e.g. preview/comment data), UNSUPPORTED (preserved raw, no typed understanding), or INVALID. Unknown sections preserve a bounded raw range and produce UnsupportedSection/Partial; never a clean success. CLASSES may be structurally supported while custom class behavior is unsupported. Unknown TABLES/OBJECTS records likewise remain source-addressable.

## 8. Header strategy

Typed minimum: $ACADVER(code1), $INSUNITS(code70), $EXTMIN/$EXTMAX(code10/20/30); also $DWGCODEPAGE and $HANDSEED as evidence. Remaining variables stay ordered named raw ranges with explicit unmapped disposition. Missing/duplicate/invalid $ACADVER blocks typed parsing; absent/unknown INSUNITS is preserved, not guessed. HEADER handles/pointers/$HANDSEED are not source-object handles.

Actual fixture: AC1032; INSUNITS4; DWGCODEPAGE ANSI_1252; extmin(+1e20,+1e20,+1e20), extmax(-1e20,-1e20,-1e20). Preserve these finite source-confirmed values and report UnusableReportedExtents because min>max. Never use them as reliable Fit bounds or synthesize repaired header values. Future diagnostic bounds are separately derived from displayable raw geometry. Units are declarations, not proof of authoring scale.

## 9. Tables

Initial profiles: LAYER, LTYPE, STYLE, DIMSTYLE, BLOCK_RECORD. Parse TABLE headers/ENDTAB and typed child entries, preserving table/entry handles/owners/names/flags/color/linetype/style/block references and locations. DIMSTYLE identity is code105; code5 has a different field role there. Layer color sign/ACI/truecolor and raw flags are retained; ByLayer/ByBlock are modes, not calculated effective values. On/Off/frozen can be decoded from explicit documented source bits with pair provenance; absent flags stay absent, not a fabricated source field.

Record LTYPE ordered elements and STYLE/DIMSTYLE raw settings sufficient to explain referenced names/handles; no linetype drawing/font load/style measurement. Unknown table records and optional metadata preserved with diagnostics. Declared table counts are evidence, actual entry counts independent. Preserve name spelling; case-insensitive reference lookup uses explicit profile policy and rejects ambiguous duplicate names instead of selecting a first match.

## 10. Geometry scope

| Family | Initial raw mapping / acceptance |
|---|---|
| LINE | Start/end XYZ, thickness and extrusion where present; do not remove zero length |
| LWPOLYLINE | Declared vertex count, ordered XY/bulge/widths/id, elevation/flags/normal; repeated code mapping by vertex order, no triangulation |
| ARC | Center/radius/start/end angle degrees/normal/thickness; no wrap correction or OCS transformation |
| POINT | Position and explicit rotation/normal/thickness facts where present |
| SOLID | All four source corner slots, preserving repeated triangular fourth corner; not 3DSOLID |
| CIRCLE | Simple center/radius/normal/thickness mapping planned; real separately documented Text DXF test required before support claimed |
| POLYLINE/VERTEX/SEQEND | Separate legacy sequence subtask/profile, original vertex/terminator handles/owners/flags retained; 2D/3D/mesh distinctions explicit; only tested subtypes supported |
| ELLIPSE/SPLINE/HATCH/others | Later investigation; raw record + UNSUPPORTED diagnostic initially, no line approximation |

Raw facts include source frame labels/native units, not Core mm types. Required missing fields/invalid numbers are explicit record failures; optional defaults stay absent. Declared polyline count mismatch/ambiguous vertex grouping is INVALID, not repaired. Each claimed type/subtype must have source-addressed fixture assertions.

## 11. Blocks / INSERT

BLOCK_RECORD table entry, BLOCK start and ENDBLK delimiter have distinct handles/roles. Preserve name/base point/flags/owner/source ranges/ordered raw children; reconcile membership and record-name references without merging identities. Layout/anonymous/Xref flags stay factual. Nested BLOCK or missing ENDBLK is structural InvalidInput; nesting of INSERT references is recorded as graph edges, not expanded geometry.

INSERT fields: code5 identity, contextual330 owner, code2 target name, 10/20/30 insertion, 41/42/43 scale, 50 source-degree rotation, 210/220/230 extrusion, 70/71 column/row counts, 44/45 spacing, 66 attributes-follow, source fields. No final/local/accumulated transform calculation in PHASE03. Preserve missing optional fields; PHASE04 interprets documented defaults with explicit provenance. Zero/nonfinite/invalid values receive field diagnostics, no hidden repair.

MINSERT subclass or explicit multi-row/column facts produce UnsupportedOccurrenceExpansion while retaining the base INSERT and array facts. No cells/IDs/flatten/Explode. Absent defaults must not be presented as source-confirmed counts. A safely delimited following entity is parsed normally. Attribute-follow sequence links ATTRIB/SEQEND raw records; malformed sequences are located invalid rather than swallowed into the next unrelated entity.

## 12. Annotation

TEXT: raw code1 + optional separately decoded content; insertion/alignment, height/rotation/style/normal/flags. MTEXT: all code3 chunks plus final code1 in defined order; retain formatting/control escapes, raw chunk locations, insertion/direction/height/rectangle/style/spacing. No font/OCR/rich layout requirement; unsupported escapes remain raw with diagnostics. Decoded convenience text is labelled derived, never replaces source text.

DIMENSION: raw subtype flags, source defining points and text placement/override/style/anonymous-block refs; retain all mapped/unmapped fields and locations. Do not calculate Measurement or interpret number strings/levels/room names. ATTDEF/ATTRIB investigation is a separate explicit coverage subtask: tag/value/placement/flags and owners/SEQEND links; unsupported subtypes remain raw and tested. HATCH extraction beyond raw retention is not an initial required typed feature.

## 13. Handles / owners

Keep raw lexical handle plus validated numeric/canonical hex when valid; source identity is DrawingId + RevisionId + FileHash + Original DXF Handle, written in the imported managed Text DXF. No assumed upstream DWG handle or DWG/DXF reconciliation. Required object handles use profile-specific code5/code105 outside control/reactor ranges. Structural delimiters/HEADER variables/CLASS declarations are not diagnosed as missing entity handles. Object handles must be nonzero valid hex within documented width; duplicate values include case/leading-zero equivalents. Missing/malformed/duplicate object handles generate diagnostics, never substitutes or geometry-derived IDs.

A record ordinal/span is a QA locator only, not a replacement CAD handle or released stable-ID policy. Owner330 is parsed in the correct common/subclass context, not the first330 anywhere. Reactors, XDictionary and XDATA references keep their roles. Zero pointer may be a documented null/root reference; do not flag every zero pointer as a missing object. Bounded post-pass validates owner/target/name memberships, unresolved hard/soft pointers with role/impact; unresolved references into unsupported raw objects are distinct from missing raw records. Preserve ambiguous candidates rather than overwriting handle maps.

## 14. Source provenance

Identity/provenance starts at imported managed Text DXF, not its upstream DWG or exporter. Preserve DrawingId + RevisionId + FileHash + Original DXF Handle; no semantic/structural/identity equivalence or exporter-change responsibility (section1). File hash/revision + section/table/block scope + object type/raw handle + record source span + field pair indices/value lines. Provenance is mandatory for significant raw facts and diagnostics, including unknown records. Record the decoder/profile/schema version and observed input version. Every unsupported/ignored object remains in source inventory; no silent dropping because it is not drawable. Raw bounds, raw words, missing fields and source relationships remain inspectable without vendor objects.

## 15. RawDrawingSnapshot

Proposed owned contracts: source identity/evidence store; Header; Tables(Layers/LineTypes/TextStyles/DimStyles/BlockRecords); Blocks; Spaces; Entities; Objects; Diagnostics; coverage/disposition counts. SourceFact<T> holds optional parsed value, raw pair references and presence/validity; optional absent != explicit zero/default. STRUCTURE_SUPPORTED does not imply ALL_FIELDS_SUPPORTED.

Snapshot copies/finalizes arrays before publishing and contains no stream/lazy callback/vendor/WPF/Revit objects. Derived in-memory extraction and deterministic QA export only, not .dtob truth or a second permanent CAD IR. Initial raw QA export format version is independent; versioned exact fields are finalized during reviewed contracts, not by changing CAD IRv1/projectv2 now. Normalized IDrawingReader/CadDocument unchanged. Incomplete snapshot cannot be advertised as complete or blindly passed to PHASE04.

## 16. Malformed / unsupported policy

| Case | Proposed outcome |
|---|---|
| Invalid code/odd line count/truncated pair; broken SECTION/ENDSEC/EOF or BLOCK/ENDBLK | InvalidInput; retained bounded evidence, no valid-empty result |
| Unsupported format/version/encoding | Explicit UnsupportedInputFormat/UnsupportedVersion/UnsupportedEncoding; no fabricated drawing |
| Invalid/nonfinite scalar; duplicate/missing handle; unresolved required membership | Located INVALID diagnostic; Partial if structure is safely intact, required coverage unusable |
| Unknown entity/section/object or unsupported family | Raw retention + UNSUPPORTED, Partial unless explicitly approved nonessential ignored policy |
| Malformed POLYLINE/ATTRIB sequence/SEQEND | Located INVALID; resynchronization only at a proven structural boundary, never invent members |
| Excessive bytes/line length/pairs/entities/diagnostics/depth | ResourceLimitExceeded; no complete/truncated-success snapshot |
| Cancellation/IO fault | Canceled/IoFailure; no current successful publication |

Read result distinguishes Succeeded, Partial, InvalidInput, UnsupportedInput, ResourceLimitExceeded, Canceled, IoFailure with stable diagnostics, not catch-all empty arrays. Unknown or ignored counts and structural/coverage completeness are separate from read success. Diagnostics include code/severity/source range/handle when present and budget-overflow summary. No automatic audit/repair/duplicate removal/handle reassignment.

## 17. Encoding policy

Measured controlled fixture: 96,621 bytes, zero bytes>=128, no BOM, CRLF, 17,474 physical lines = **8,737 raw pairs**, no Unicode escape occurrences; hash2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC. Pure ASCII validates ASCII subset only, not multilingual fidelity. The ANSI_1252 header alone does not prove the byte encoding of this ASCII-only file.

Accepted initial **AC1032 Text DXF profile**: strict UTF-8, ASCII subset naturally supported, optional UTF-8 BOM at byte0, CRLF / LF supported. Korean and all other valid non-ASCII UTF-8 characters are allowed, not an UnsupportedEncoding case merely because they are non-ASCII; no OS-default/codepage guessing or replacement characters. Retain $DWGCODEPAGE as source metadata; do not switch AC1032 decoder based on it. Reject UTF-16/32 BOM, NUL-bearing nonstandard text, binary signature and invalid UTF-8 explicitly. Missing/unsupported version fails typed compatibility even if bytes decoded. No support for older versions merely because their bytes are decodable text.

The maintained [ezdxf encoding documentation](https://ezdxf.readthedocs.io/en/stable/dxfinternals/fileencoding.html) describes UTF-8 from AC1021 onward and older codepage-based files. This informs the profile, not a dependency or proof of our compatibility. Actual non-ASCII UTF-8, BOM and DXF Unicode-escape fixtures are required before claiming those content cases. A valid UTF-8 file with unsupported CAD string escape remains raw and diagnostic; unpaired/invalid Unicode escape may not silently replace data. Future versions/codepages require explicit tested profile + architecture review, not automatic fallback.

## 18. Managed read-only authority

Actual CurrentFilePath plus validated ManagedFilePath only, using PHASE02 project-relative/reparse rules (junction parent allowed; asset-tree reparse rejected). Capture immutable project/drawing/revision/hash/session identity. Explicit Extract takes selected DXF read lease (FileAccess.Read/FileShare.Read), checks registered size/hash on the same stream, rewinds, parses, holds through copying and disposes in finally. No SourceFilePath I/O/fallback, write/repair/conversion/Xref fetch, hash update, saved metadata mutation or dirty-state change.

Open/Refresh/metadata selection stay no asset-content reads/hashes/no source I/O; extraction eligibility is cheap extension/recorded-format metadata, not implicit parse. Managed missing/unreadable/hash mismatch yields explicit result, never opens original/UNC provenance. Hash state follows ADR013 and is runtime only. Explicit extraction failure/cancel leaves prior project and clean-state untouched.

## 19. Async / cancellation

Pure native reader accepts CancellationToken; owns lexical/mapping loops and checks bounded progress, unlike vendor synchronous Read. One worker active, latest pending request bounded; no UI-thread decoding. Capture read epoch + session/project/path/drawing/revision/hash, reject stale success AND stale error on replace/remove/open/close/SaveAs/switch. Hash, async stream reads and loops observe cancellation; normal owner finally disposes, no cross-thread disposal/thread abort. UI coordinator publishes owned complete/partial result only when identity/epoch is current; partial is visibly labelled, never changes project truth. No external process/service or complex monitoring.

## 20. Initial resource limits / performance

Finite initial **development defaults**, configurable/injectable via a validated ReaderLimits profile before any read:

| Limit | Initial default | Measurement / enforcement |
|---|---:|---|
| Maximum physical line length | **64 KiB = 65,536 UTF-8 bytes** | Count source bytes before growing a line buffer; excludes CRLF/LF and byte0 BOM; no non-ASCII ban or char-count shortcut |
| Maximum control-group nesting depth | **16** | Combined active nested code102/control and code1002/XDATA group stack; check before push |
| Maximum stored diagnostics | **5,000** | Reserve final slot for ResourceLimitExceeded; at most4,999 ordinary diagnostics |
| Maximum raw pair count | **250,000** | Counts every lexical pair, including delimiters/comments/unknowns; check before evidence append |
| Maximum input bytes | **16 MiB = 16,777,216 bytes** | Metadata/stream length preflight where known plus cumulative actual read count, not trusted serialized FileSize alone |

The controlled fixture is96,621 bytes/8,737 pairs, maximum physical line56 UTF-8 bytes; these initial caps leave bounded development headroom without claiming a throughput/memory benchmark. Source evidence stores raw text once; total records, vertex facts and reference occurrences are additionally bounded by the pair/input budgets, with no occurrence expansion. No unlimited pre-read/ReadLine/ReadAllLines allocation before applying line/input guards.

All limits must be finite positive validated configuration (diagnostics>=2 to reserve terminal slot). Tests inject smaller values and assert exact-boundary acceptance versus first-over-boundary ResourceLimitExceeded. If another ordinary diagnostic would occupy the reserved final slot, stop with the terminal budget diagnostic; never silently evict/drop excess diagnostics and return success. The returned refusal carries only bounded incomplete diagnostic evidence, **never truncated-success** or a usable complete snapshot. Size/line/pair/depth limit failure likewise returns ResourceLimitExceeded before unbounded allocation. Explicit documented later measurement/review may revise defaults; do not remove safety caps while measuring.

Forward lexical/structural pass + bounded reference pass, no occurrence expansion; cancellation checks during reads and own loops. UI visual batching rather than one WPF object/entity. No database/cache/background scans. Resource bounds are safety controls, not geometry tolerance constants or performance guarantees.
## 21. Extraction QA

Native QA reports raw pair/section/record/disposition counts; source scopes(ModelSpace/PaperSpace/reusable/anonymous block/subentities); handles/unique/invalid/missing/duplicates/references; layers/names; BLOCK_RECORD versus BLOCK/ENDBLK; INSERT counts; exact raw coordinates/text/dimension defining facts; unsupported/ignored counts and stable diagnostics. Every record is accounted exactly once; expanded occurrences are not produced in PHASE03. No semantic counts or guessed geometry meaning.

Prior independent whole-file DXF inventory: LINE59/LWPOLYLINE16/ARC1/INSERT25/TEXT36/MTEXT4/DIMENSION4/POINT12/SOLID1/LAYER12/BLOCK12. Reference QA only, includes definitions/tables. Previous SDK ModelSpace114/direct entities158 are observations, not forced native goldens. Independently hand-audit selected source line/handle samples and role-aware object inventory; SDK327 handled-record evidence is not blindly adopted as source truth.

Golden deterministic source facts/diagnostics tied to actual input hash, profile/config and raw export version, stable source-order plus documented table ordering. Exclude run timestamp/elapsed time from content digest. Canonical output equality on repeated reads, source ranges and raw strings exact; parsed numbers checked without arbitrary normalization. Fixture review must be independent of parser internals, not self-generated expected values.

## 22. Minimal diagnostic Viewer (03H)

Consumes snapshot only, no file/parser calls on mouse/layer actions. Basic display/Zoom/Pan/Fit/Layer On-Off/click -> Handle/Type/Layer/raw properties/value-line inspector. Camera Y-up->screen Y-down is display only, no model coordinate convention change. Use independently derived display bounds; unusable header extents stay visible as source evidence.

To respect PHASE04 ownership, PHASE03 display covers directly displayable source-frame geometry; block definitions may be inspected in a separate local-frame view and INSERTs as raw placement markers. No inserted/nested block geometry transformation, OCS->WCS, mm conversion or effective ByBlock styling. Unsupported frames/annotations/rendered cases explicitly marked and inspected as facts. No AutoCAD fidelity/SHX/hatch/plot/dimension rendering/window-selection gate. Display exceptions cannot discard extracted records or dirty project state.

## 23. Automated test plan

Planned, not added/executed native parser tests in this design task:

| Area | Required assertions |
|---|---|
| Lexical | Exact raw spelling/blank values/code/value lines; CRLF/LF/BOM/last newline; odd lines/invalid/out-of-range codes; invariant finite numbers; stable repeated pairs |
| Encoding/version | Actual empirically ASCII-only AC1032 fixture; valid Korean/multilingual UTF-8 accepted (not rejected for non-ASCII), with/without BOM and CRLF/LF; raw escapes preserved; strict invalid bytes/UTF16/binary/unknown/missing/duplicate ACADVER; no guessed older support |
| Structure | All known sections, unknown dispositions/comments, EOF/ENDSEC, duplicate/header variable boundaries; TABLE/ENDTAB and control/subclass/XDATA nesting |
| Geometry | Known handle/source-line numeric fields, vertices/bulges/widths/counts, degenerate raw retention, missing/invalid optional vs required; real CIRCLE and separate legacy sequence fixtures |
| Blocks/annotations | BLOCK_RECORD/BLOCK/ENDBLK identity/ranges; raw INSERT nonzero-base/rotated/scaled facts unchanged with NO matrix; MINSERT unsupported+following entity; TEXT/MTEXT chunks/escapes; DIMENSION raw vs no calculated Measurement; attribute sequences |
| Identity/owners | Code5 vs105 vsHEADER; reactors330 vs owner330; canonical duplicates/zero/malformed/missing; unsupported referenced objects; ambiguous names; every source scope accounted once |
| Safety/lifecycle | Finite defaults and smaller injected budgets; line-byte/control-depth/diagnostic/pair/input exact-boundary and first-over-boundary ResourceLimitExceeded, reserved terminal diagnostic and no truncated-success; no unbounded allocation, cancel loops/hash/dispose, stale success/error, no source fallback/write/session mutation |
| Regression | All PHASE00/01/02 tests, deterministic Open/Refresh no content/hash/sourceIO assertions; production dependency allowlist excludes SDK/WPF/Revit from raw/parser |
| Diagnostic | Owned source selection/provenance, native-frame limits, no reader invocation or project dirty change, viewport geometry projection tests |

PHASE04 later tests unit/radian/frame/block-base/nested/mirror transformations; do not implement these to pass PHASE03.

## 24. Fixtures

First authoritative implementation fixture: unchanged datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf (actualAC1032, not inferred filename). DWG retained historical, not a new native reader test target. Before/after hash checks mandatory. Previous two supplemental DXFs are usable raw-field/unsupported reference cases; never run ACadSharp to accept native output.

Create later small Text DXF fixtures with reviewed group-code contents/expected source lines/handles for CIRCLE, POLYLINE/VERTEX/SEQEND, ATTRIB/ATTDEF, nonempty PaperSpace, Unicode, malformed boundaries/handles/numbers and unsupported records. Label synthetic/supplemental separately from Controlled V1 truth. A hand-authored file tests file parsing; fake snapshot tests contracts only and cannot prove actual controlled extraction. Other versions need actual version-specific fixture + explicit acceptance. No original mutation/conversion/repair.

## 25. ADR requirements / acceptance

ADR015 accepted additive boundary/native amendment remains unchanged in this housekeeping; accepted ADR001–015 are protected/unrenumbered. **ADR-016-Native-DXF-Reader.md: Accepted**, 2026-10-14, following Antigravity APPROVED_FOR_NATIVE_DXF_IMPLEMENTATION and explicit owner acceptance direction after Issues1–3. Official Text DXF profile, imported-managed-DXF identity boundary and finite development defaults are documented. No ACadSharp backend ADR. ADR017 lifecycle remains reserved if a separate integration decision is justified; not created/Accepted now. No Core/BIM/CAD IR/project major schema/ID changes.
## 26. Expected projects/files

Propose only, **do not create this turn**:

| Project | Dependencies / responsibility |
|---|---|
| packages/DtoB.Cad.Raw | Pure net8, no WPF/Revit/vendor; raw/source evidence/result contracts; independent native numeric/frame types |
| packages/DtoB.Cad.Dxf | Depends only on DtoB.Cad.Raw; .NET decoding/lexical/section/table/entity parsing and located diagnostics |
| Desktop integration | Later direct composition with raw/parser + existing Project read-authority boundary; small cancellable coordinator/factual inspector/03H UI |
| Tests | Existing portable suite references new pure libraries only after approved project/dependency-test update; reviewed text fixtures/goldens |

Preserve packages/DtoB.Cad normalized API and all existing dependency directions. Future solution and allowlist additions are explicit reviewed additions, not a workaround/vendor leak. No external MVVM/DI/database/SDK packages. Documentation-only changes listed in section27; no project/test/build/package file modification now.

## 27. Authoritative planning updates

Update tasks03 official name/objective/03A-H/gates; tasks04 sole transform/default/effective normalization responsibility; PHASE_INDEX; master current input/pipeline/section17+18 and goal/test text; architecture/instructions/readme/start native path and design STOP; checklist; AcceptedADR016 and references. Existing ADR015 amendment is retained unchanged in final housekeeping. PHASE00/01/02 reports/ADRs and prior Antigravity/spike/housekeeping reports remain historical unchanged. Historical phase02 DWG registration does not imply V1 reader support. Older SDK approval/review is historical and superseded as production direction by this owner request.

## 28. Explicit out of scope

Direct DWG/BinaryDXF/UTF16/nonstandard encodings/DWF/cloud conversion/AutoCAD automation; ACadSharp/ODA/RealDWG/privateDLL/revit/WPF parser dependencies; semantic recognition/AI/OCR; mm/OCS/WCS/INSERT composition/effective styles/cleanup/CAD IR output in03; CAD editing/audit/repair/handle replacement/Xref resolution/dynamic3D; database/cloud/accounts/autosave; AutoCAD-level rendering. No parser/project/Viewer implementation or SDK installation in this design task, no PHASE04/commit/push.

## 29. Approved architecture sequence — do not start03A this turn

1. Finalize review Issues1–3 and AcceptedADR016/document verification, then STOP. Subsequent owner-directed implementation starts03A with accepted contracts/profile/finite limits; no projects/parser in this turn.
2. Create pure Raw/Dxf projects and regression dependency allowlist; 03A lexical/profile/source evidence first.
3. 03B Sections/Header/Tables; 03C core geometry and separately gated CIRCLE/legacy sequences.
4. 03D Blocks/INSERT/Annotation, MINSERT unsupported/raw continuation; no transforms.
5. 03E Handle/Owner/Source Provenance; 03F owned snapshot assembly/completeness.
6. 03G controlled + supplemental factual QA/goldens/safety/hash/cancellation/regression tests.
7. 03H minimal diagnostic Viewer only after extraction QA; reviewed async managed integration.
8. Core/full Release, all tests, actual controlled extraction/UI/lifecycle/hash evidence, self-diff, final PHASE_03_REPORT and independent review. No PHASE04 without its gate/owner instruction.

Current turn ends after final housekeeping documents, affected document/dependency tests and PHASE_03_NATIVE_DXF_HOUSEKEEPING_REPORT. No new03A code.

## 30. PHASE03 Exit Criteria (future implementation)

- [ ] Controlled AC1032 Text DXF parsed with exact raw pair/line evidence and explicit version/encoding profile.
- [ ] Sections/Header/Tables/Blocks/Entities/Objects accounted once with supported/ignored/unsupported/invalid dispositions.
- [ ] Required core geometry + TEXT/MTEXT/DIMENSION raw facts, provenance and coverage tested; optional family support claimed only after real fixtures.
- [ ] Original raw/canonical handles, contextual owners/refs/definitions/spaces validated without synthetic replacements.
- [ ] INSERT facts/native frame/units/source degree angles preserved; no PHASE03 matrix/composition/mm/effective-style engine.
- [ ] MINSERT unsupported expansion preserves base metadata, diagnostics, safe continuation and no cells/IDs.
- [ ] Owned deterministic snapshot/completeness/QA, invalid/unsupported inputs never valid empty success.
- [ ] Strict managed-only selected extraction/hash/lease/cancellation/stale handling; no Open/Refresh reads/hashes/sourceIO/dirty changes; bytes unchanged.
- [ ] Bounded resource/diagnostic limits measured, malformed/nonfinite input cases and prior regressions pass.
- [ ] Minimal snapshot-only native-frame diagnostic Viewer works with explicit display limitations; no second parser/normalization.
- [ ] Core/full Release + all tests + actual workflow/hash/source QA + diff/report + independent review evidence.

Antigravity architecture verdict is APPROVED_FOR_NATIVE_DXF_IMPLEMENTATION; Issues1–3 resolved and ADR016 Accepted. This is not phase PASS or native compatibility proof. **Ready for later PHASE03A; STOP before implementation in this turn.**
## 03A execution evidence — 2026-10-14

Latest owner authorized03A lexical implementation only after final housekeeping. **PASS_WITH_KNOWN_LIMITATIONS; ready for independent03A review**, not whole PHASE03 completion or03B permission. Earlier design/housekeeping wording above is preserved historical scope; pre-03A checklist archived under artifacts/phase03/03a-lexical-20261014/checklist-before-03a.md.

Implemented only pure DtoB.Cad.Raw lexical contracts and DtoB.Cad.Dxf stream/UTF8/physical-line/group-code reader with default/injected guards, deterministic diagnostics, owned raw spelling/lines and cancellation. Core/full Solution Release PASS; final **190/190 tests** (55 new lexical +9 new dependency +126 previous regressions), zero failures/skips. Actual original96,621-byte fixture preserves17,474 lines/8,737 pairs and unchanged SHA2562676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC. No production CAD SDK dependency; old contracts/ADRs unchanged.

VersionAcceptance explicitly **NotValidated** even on lexical success: no ACADVER/header/structure interpretation, no older-version acceptance. Control-depth16 is configured but reserved for03B structural enforcement. No entity/handle/geometry models, RawDrawingSnapshot assembly, Viewer/session integration, unit/OCS/INSERT/effective-style engine or PHASE04 code.

Full contracts/limits/cancellation/fixture/source-QA/determinism/dependency/diff/results/limitations: **docs/status/PHASE_03A_REPORT.md**; raw build/TRX/pair evidence under artifacts/phase03/03a-lexical-20261014. **STOP after03A for owner/Antigravity review; do not start03B or commit/push.**

## 03B execution evidence — 2026-10-14

03A independent verdict **PHASE_03A_PASS**; latest owner authorized03B Sections/Header/Tables only. Historical stop/planning text above is preserved. **PASS_WITH_KNOWN_LIMITATIONS — ready for independent03B review**, not full PHASE03 PASS or03C authorization.

Implemented immutable03A evidence consumption, explicit section/table state machines and AC1032 gate, ordered HEADER variables/factual provenance, five table maps, raw deferred/class/object inventories, role-aware controls/references, bounded diagnostics/resources/cancellation. No source re-read/re-tokenization in03B. Unknown content retained with dispositions. Controlled file has StructureValidated=true/VersionAcceptance=Supported and **Partial mapping**: unsupported tables/objects, deferred03C/03D and12 DIMSTYLE ambiguous code70 facts retained/Invalid, never first/last wins.

Original8,737 pairs/6 sections/251 header variables/9 tables/333 child records; independent source scanner verifies LAYER12/LTYPE21/STYLE27/DIMSTYLE12/BLOCK_RECORD12 and raw ranges/names/selected exact source lines.17 supplemental actual Text DXF files plus embedded source fixtures. Before/after original SHA2562676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC unchanged; separate read results deterministic.

Final Core/full Release PASS; **285/285 tests PASS** (95 new03B +55 unchanged03A lexical +9 dependency additions +126 prior regression), zero failures/skips. Defaults: sections32/tables32/work records4096/fields512/references16384/control depth16/diagnostics5000/pairs250000, justified against measured controlled input; exact/injected budgets and reserved diagnostic terminal tested. Existing03A/defaults/ADRs/dependencies/production architecture unchanged.

Exact models/policies/limits/goldens/counts/build/test history/dependency/hash/diff/evidence/limitations: **docs/status/PHASE_03B_REPORT.md**. Final gate under artifacts/phase03/03b-structural-20261014/final-verified. **STOP for owner/Antigravity03B review; no03C/03D/RawDrawingSnapshot/Viewer/PHASE04, no commit/push.**

## 03B Antigravity targeted corrections — 2026-10-14

Independent verdict **PHASE_03B_CHANGES_REQUIRED**; H-01/L-01 corrected and verified, ready for re-review. The preceding03B entry is preserved **pre-review history**. Its claim that the controlled DIMSTYLE70 fields were ambiguous was incorrect: they are distinct StandardFlags and DIMTFILLCLR roles, not a supported-input limitation. Current evidence below supersedes that claim/counts; previous285-test evidence is preserved unchanged.

DIMSTYLE alone uses named symbol-header/code69 DIMTFILL contexts to map separate Integer facts **Flags** and **TextFillColor**, each with exact source indices/spans/Presence/Validity. No first/last wins; actual same-role duplicates remain InvalidTableValue/Invalid. Control/XDATA/foreign-subclass fields cannot silently supply those roles.105 identity/5 legacy setting and330 roles unchanged. Both70 pairs and every other mapped/unmapped record pair remain accounted once.

Actual controlled12 DIMSTYLE entries: both facts Present/Valid/raw integer0, all12 Supported, false InvalidTableValue12→0; total Supported87→99, Invalid12→0; diagnostics71→59; records333/Unsupported52/Deferred03C89/Deferred03D93 unchanged. Result remains Partial, StructureValidated=true, VersionAcceptance=Supported. L-01 aggregate now has first unmapped variable source (pairs4..5/lines9..12), still one diagnostic for245 variables.

Three additional hand-authored Text DXF fixtures (20 total);8 new targeted cases plus corrected prior duplicate-role fixture and expanded real-file/source-accounting assertions. Targeted03B **103/103 PASS**; full **293/293 PASS** (103 structural +55 unchanged lexical +9 dependency additions +126 legacy). Core/full Release/exact Revit warning gate PASS. Original SHA2562676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC unchanged; independent stream outputs equal, structural SHA2562546B5513275D3EC1EEF025DA817249CC3F724A1BA4A986790041C94C3C4DB91.

Current report: **PHASE_03B_REPORT.md — Antigravity Review Fixes**; evidence artifacts/phase03/03b-review-fixes-20261014/final. Reports/reviews/ADRs/03A/legacy production architecture preserved except authorized targeted changes. No03C/03D/PHASE04/full DIMSTYLE semantics/RawDrawingSnapshot/Viewer/global handle graph; no commit/push. **STOP for independent Antigravity re-review.**

## 03C execution evidence — 2026-10-14

Latest independent review **PHASE_03B_PASS**, after H-01/L-01; baseline293/293 rebuilt before changes. Owner authorized03C raw geometry only. Earlier planning/stop/review statements above retain historical context. **PASS_WITH_KNOWN_LIMITATIONS — ready for owner/Antigravity03C review**, not full PHASE03 completion or03D approval.

Added separate native factual geometry contracts/result and mapper consuming immutable03B evidence; no source re-read/re-tokenization, normalization or structural mutation. Source facts preserve exact pair/line/raw text references and absent/invalid states; all mapped+unmapped pairs accounted once. LINE59/LWPOLYLINE16/ARC1/POINT12/SOLID1 independently verified and mapped Supported89, zero new geometry diagnostics. All16 polylines'61 ordered vertices (max4), counts/flags/optional fields and selected exact goldens compared against raw source lines. Effective counts Supported188/Unsupported52/Deferred03D93/Invalid0/Deferred03C0; global status still Partial. Original03B evidence/dispositions/diagnostics unchanged.

CIRCLE separately verified through hand-reviewed actual Text DXF source with exact pair assertions. Legacy POLYLINE/VERTEX/SEQEND mapping remains gated; unsupported geometry retained without approximation. No source defaults; unsupported/gated unmapped common properties null rather than falsely absent. Finite injected caps: geometry512, per-polyline64, total coordinate slots1024 (controlled196), fields512, diagnostics5000 with reserved terminal, structural4096/pairs250000; safety/cancel/immutable tests pass.

Added16 actual supplemental DXFs plus source fixtures. Final Core/full Release/exact known Revit warning gate PASS; **356/356 tests PASS** (63 new03C +103 unchanged03B +55 unchanged03A +9 dependency additions +126 prior regression); targeted63/63. Original SHA2562676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC unchanged; independent repeated result digestFB467C3FA0CEE622B8B616B3D04B6EAD608EB3E76CD5FFE703DA2F2D70A85E8E. Existing245-file snapshot unchanged except this append; old tests/reports/reviews/ADRs/production architecture preserved.

Details: **docs/status/PHASE_03C_REPORT.md**; final evidence artifacts/phase03/03c-geometry-20261014/final. **STOP for independent03C review; no03D/03E/RawDrawingSnapshot/Viewer/PHASE04, no commit/push.**

## 03D execution evidence — 2026-10-14

Independent predecessor verdict **PHASE_03C_PASS**; baseline356/356 rebuilt before changes. Owner authorized03D native raw BLOCK/INSERT/annotation extraction only. Historical planning/review/stop text above remains unchanged. **PASS_WITH_KNOWN_LIMITATIONS — ready for owner and independent Antigravity03D review**, not whole PHASE03 completion or03E authorization.

Added separate immutable DxfBlockAnnotationReadResult referencing unchanged geometry→structural→lexical evidence. BLOCK/ENDBLK source containment with ordered child locators, raw INSERT placement/array facts, TEXT/ordered MTEXT chunks, limited DIMENSION and separately fixture-gated ATTRIB/ATTDEF/explicit attribute SEQEND. No defaults, target resolution, transforms or clones. Every mapped+unmapped pair accounted once. Malformed boundaries/attribute sequences retain following unrelated records with located diagnostics. Arrays explicitly UnsupportedOccurrenceExpansion; legacy POLYLINE sequences remain gated.

Independent raw code0 inventory BLOCK12/ENDBLK12/INSERT25/MTEXT4/TEXT36/DIMENSION4=93; all93 mapped Supported, zero new diagnostics. Twelve definitions/44 children/max9; eight nested raw INSERT references; original89 geometry records unchanged. Four controlled DIMENSION flags32/Aligned+Rotated subclasses, absent42 measurements; no calculations. Effective333 records: Supported281/Unsupported52/Deferred03D0/Invalid0; global Partial with03D MappingComplete=true. Text budget measured974 raw characters; finite injectable guards/exact-first-over/cancellation/immutable freeze verified. Twelve supplemental actual DXFs plus inline full-pipeline fixtures.

Final Core/full Solution Release and exact existing Revit warning gate **PASS**. **434/434 tests PASS**:78 new03D +63 unchanged03C +103 unchanged03B +55 unchanged03A +9 architecture/dependency additions +126 PHASE00/01/02 regressions; no failures/skips. Original SHA2562676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC unchanged. Three independent streams serialize identically; result digestDB9E048EFCBEAD2B38F2F70A52621506D09AECDF35EBB6A2430563BC15F2B747. Prior269-file snapshot preserved except this append; no dependency/project/ADR/old-test/report/review changes.

Details: **docs/status/PHASE_03D_REPORT.md**. Build/TRX/independent source/hash/diff evidence: artifacts/phase03/03d-raw-20261014. **STOP for independent03D review. No03E/03F/RawDrawingSnapshot/Viewer/PHASE04 or occurrence expansion; no commit/push.**

## 03E execution evidence — 2026-10-14

Independent predecessor verdict **PHASE_03D_PASS**; baseline434/434 rebuilt before changes. Owner authorized03E Handle/Owner/source provenance validation only. Historical planning/review/stop text above is preserved. **PASS_WITH_KNOWN_LIMITATIONS — ready for owner and independent Antigravity03E review**, not full PHASE03 completion or03F authorization.

Added separate immutable DxfIdentityGraphReadResult referencing unchanged03D→03C→03B→03A evidence. Same-file string hexadecimal canonicalization preserves OriginalHandle; no integer-width overflow or synthetic IDs. Contextual identity roles exclude HEADER/CLASS/DIMSTYLE5/control/foreign fields. Source-order TABLE headers and structural records, all conflicting nodes, contextual reference edges, Owner-only iterative bounded graph/cycle witnesses and neutral caller-supplied document tuple. Duplicate/malformed/missing identities are never accepted as valid full identity. No Project dependency/file/hash re-read or session integration.

Independent controlled source audit:327 identity nodes (97TABLES incl.9 headers,68BLOCKS,114ENTITIES,48OBJECTS);15 CLASS records nonidentity. All327 valid/unique; no malformed/missing-required/duplicate groups.458 references: Owner327 (317resolved/10null), Reactor30 (all resolved), Other101 (91resolved/10null), XDictionary0/XData0. Total438resolved/20null, zero unresolved/ambiguous/malformed; Owner edges317/roots10, no self/cycle cases, zero new diagnostics. GraphValidated/IdentityCoverageComplete/ReferenceResolutionComplete=true, graph Succeeded. Semantic03D remains Partial/Supported281/Unsupported52, never promoted by graph validation. Eighteen actual supplemental Text DXFs plus inline full-pipeline fixtures gate failures/contexts/provenance/limits/cancel/unknown scopes.

Final Core/full Solution Release and existing exact Revit warning-identity gate **PASS**. **522/522 tests PASS**:88 new03E +78 unchanged03D +63 unchanged03C +103 unchanged03B +55 unchanged03A +9 architecture/dependency additions +126 PHASE00/01/02 regressions; zero failures/skips. Before/after original SHA2562676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC unchanged. Three fresh full pipelines serialize identically; fixed-context graph digestA27ACDE11709AEB611B381377F9BD57295364BA410B2774E7E6FE685003C34A7. Prior289 files preserved except this append; no project/old-contract/test/ADR/report/review changes.

Details: **docs/status/PHASE_03E_REPORT.md**; evidence artifacts/phase03/03e-identity-20261014. **STOP for independent03E review. No03F/RawDrawingSnapshot/Viewer/PHASE04/project persistence or occurrence expansion; no commit/push.**

## 03F execution evidence — 2026-10-14

Independent predecessor verdict **PHASE_03E_PASS**; baseline522/522 rebuilt before changes. Owner authorized03F RawDrawingSnapshot Assembly only. Review closing Viewer wording does not authorize03H. Historical planning/review/stop text above preserved. **PASS_WITH_KNOWN_LIMITATIONS — ready for owner and independent Antigravity03F review**, not full PHASE03 completion or03G/03H authorization.

Added pure immutable Snapshot/assembly result, source-order child association views, stage/index diagnostic references and independent completeness axes over exact unchanged03E→03A evidence. No parsing/source reads/graph rebuild/payload cloning. Strict V1 graph+identity+reference completeness required. Parser/stage failures, invalid/unfinished mappings and conflicting associations refuse Snapshot; completed intentional Unsupported coverage remains assemblable with typed/stage flags partial. No source defaults/transforms or semantic promotion.

Controlled counts preserved:96621 bytes/17474 lines/8737 pairs;333 children Supported281/Unsupported52;89 geometry/93 raw03D records/12 definitions;342 identity-inspected locations incl.9 headers,327 nodes/15 nonidentity;458 refs(438resolved/20null),317 owners/10 roots/0 cycles. SourceEvidenceComplete=true/TypedMappingComplete=false/graph validated and complete/SnapshotAssemblyComplete=true, operation Succeeded with Partial coverage. Exact source facts/pairs/line goldens and references retained. Two distinct03F Text DXFs plus inline actual fixtures prove native/no-normalization/unsupported/failure behavior.

Final Core/full Solution Release/exact existing Revit warning gate **PASS**. **558/558 tests PASS**:36 new03F +88 unchanged03E +78 unchanged03D +63 unchanged03C +103 unchanged03B +55 unchanged03A +9 architecture/dependency additions +126 PHASE00/01/02 regressions; no failures/skips. Three fresh full03A→03F pipelines serialize identically; Snapshot SHA2564B91C83791F89DEC9BDC633C832001F2D1E537E7F0330BEE0666067CAE9A310C. Original source SHA2562676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC unchanged. Prior315 files preserved except this append; no old contracts/tests/reports/reviews/ADRs/project/dependency change.

Details: **docs/status/PHASE_03F_REPORT.md**; evidence artifacts/phase03/03f-snapshot-20261014. **STOP for independent03F review. No03G/03H Viewer/PHASE04/project persistence/occurrence expansion; no commit/push.**

## 03G QA execution evidence — 2026-10-15

Independent predecessor **PHASE_03F_PASS** and evidence correction verified. Baseline **558/558** tests/Core/full Solution Release passed before changes; branch phase/03-cad-data-extraction unchanged. Owner authorized only03G Extraction QA. Historical statements above remain unchanged. **Local PASS_WITH_KNOWN_LIMITATIONS — ready for owner and independent Antigravity03G review**, not whole PHASE03 completion or03H/PHASE04 authorization.

No production change. Added deterministic86-source fixture manifest (17new byte cases), independent physical-line/source inventory QA, named source corruptions and unsupported-preservation cases, immutable publication tests,42enforced-limit below/exact/first-over matrix (126executions),16deterministic pre/read/freeze cancellation cases, culture/working-directory fresh-pipeline validation and source/dependency/scope audits. CPU inner-loop cancellation remains explicitly source-inspected only; no test-only production hook/racy sleep. LOW-01 warning-only unresolved non-owner Snapshot refusal remains KNOWN_V1_POLICY.

Corrected semantic inventory revalidated Supported281/Unsupported52=VPORT1+APPID3+OBJECTS48;Invalid/Deferred0;HATCH0;DIMENSION4 allSupported. All approved prior goldens reran, plus POINT/SOLID/MTEXT/table/block/entity source pair goldens. Controlled counts/identity graph/completeness remain unchanged. Twenty-seven dedicated complete runs across three cultures/three working-directory contexts retain Snapshot SHA2564B91C83791F89DEC9BDC633C832001F2D1E537E7F0330BEE0666067CAE9A310C. Source SHA2562676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC unchanged;86fixture hashes preserved;324pre-existing files preserved except this append.

Final **Core Release PASS/full Solution Release PASS/exact existing Revit warning gate PASS;736/736 tests PASS** (558unchanged+178new),0failed/0skipped;two extra separate-working-directory testhosts3/3 each. No new production defect/BLOCKER/HIGH. No old test/report/review/ADR/project/dependency change;git diff --check PASS and new untracked text audit. Details: **docs/status/PHASE_03G_REPORT.md**, coverage/limit/cancellation matrices beside it;ignored evidence **artifacts/phase03/03g-qa-20261015/**.

**STOP for independent Antigravity03G review. No03H Viewer/noPHASE04/no normalization/noCAD IR/no commit/push.**

## 03H implementation evidence — 2026-10-15

Independent predecessor **PHASE_03G_PASS** verified; baseline736/736/Core/full Solution Release passed before changes. Owner authorized only03H Minimal Raw DXF Diagnostic Viewer. Historical phase/stop wording above remains unchanged. **Local PASS_WITH_KNOWN_LIMITATIONS — ready for independent Antigravity03H review**, not independent03H acceptance or a whole-PHASE03 final gate.

Existing apps/DtoB.Desktop WPF shell now opens a diagnostic viewer for selected managed Text DXF. Same FileAccess.Read/FileShare.Read lease verifies registered SHA256 then runs unchanged03A→03F on a background worker. ADR-017-Reader-Lifecycle documents application-only integration,provenance,lease/cancel/stale choices;Raw/Dxf dependencies/contracts remain unchanged. No SourceFilePath fallback,project dirty/schema/state change,SDK or directDWG decode.

Desktop-only immutable scene/pixel camera separates ENTITIES from one raw block definition. Independent controlled inventory57direct geometry=45LINE+12LWP;17INSERT anchors;36TEXT anchors;4DIMENSION records;12definitions/44ordered children. Native XY only,no base compensation/INSERT transform/expansion. LINE/POINT/SOLID/CIRCLE/ARC/LWP diagnostic primitives;bulge chords explicitly labelled. All333 source records,including52Unsupported,remain available for Inspector/source pair/physical-line/handle/reference tracing. Summary/completeness/diagnostics derive from Snapshot;HEADER sentinel extents never drive Fit. UI epoch/context/token discards stale success/error and clears stale display.

Final **773/773 tests PASS** (736previous+37viewer),0failed/0skipped;**Core Release/full Solution Release/exact existing Revit warning gate PASS**. Narrow Desktop→Raw/Dxf dependency allowlist updated for authorized references;all parser/reverse/vendor dependency prohibitions retained. Fixed-context Snapshot hash unchanged4B91C83791F89DEC9BDC633C832001F2D1E537E7F0330BEE0666067CAE9A310C;source hash unchanged2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC. Three-culture scene model digest9D179A0EFAC5EE803E8AEC2AC3C48E1AB0986F1C6159BCB1764E8071933963A7.

Actual Release WPF smoke executed:project open/restart via Recent,DXF selection/extraction,summary/visible ENTITIES,Fit/Zoom/Pan/Reset,canvas LINE/INSERT source inspection,COLUMN_400 local view/base marker,Unsupported Inspector,Viewer close/Shell stability,DWG external-export guidance and selection invalidation. Owner assisted initial file dialog due cached-control tool failure. AutoFit layout/base-marker/scope notice/stale diagnostic cleanup issues found and fixed;full tests/build and relevant host checks rerun. LOW-01 unchanged;layer filtering omitted as optional;no CAD text/font/3D/fidelity claim.

Details: **docs/status/PHASE_03H_REPORT.md**. Ignored evidence **artifacts/phase03/03h-viewer-20261015/** contains baseline/final logs/TRX,hashes/scene/source/boundary/diff audits and actual screenshot/accessibility traces. Old reports/reviews/ADRs/fixtures/parser source preserved. **STOP for independent Antigravity03H review. NoPHASE04/no commit/push. Separate whole-PHASE03 gate only after independent03H PASS and owner direction.**


## Whole-PHASE03 final release gate — 2026-10-15

Owner authorized only the integrated release gate after authoritative independent
PHASE_03A_PASS through PHASE_03H_PASS. Fresh Core Release and full Solution
Release/Rebuild pass with zero errors and only the exact existing Revit MSB3277
identity pairs. Full unfiltered suite: 773 total / 773 passed / 0 failed / 0 skipped.
03G QA and03H boundary audits,86-source manifest,strict text/diff/dependency/scope
and protected-source checks pass. Source2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC;
Snapshot4B91C83791F89DEC9BDC633C832001F2D1E537E7F0330BEE0666067CAE9A310C;
scene9D179A0EFAC5EE803E8AEC2AC3C48E1AB0986F1C6159BCB1764E8071933963A7
remain unchanged. Snapshot Coverage Partial/TypedMappingComplete false; corrected
Unsupported52 = VPORT1 + APPID3 + OBJECTS48; HATCH0,all4DIMENSION Supported.

Actual03H WPF evidence was rechecked against identical freshly rebuilt EXE/DLL
hashes and unchanged product source;no new UI execution is claimed. Detail and
capture-order/tool limitations are recorded in the final report. Current entry/spec
docs and ADR015/016 receive additive status/history notices only;historical report/
review bodies and original checklist prefix are preserved. All production/test/
fixture files remain unchanged during this gate. LOW-01 stays KNOWN_V1_POLICY.

Exact accumulated inventory:189nonignored files;180proposed native-phase payload,
9isolated historical SDK-spike files explicitly excluded/preserved;generated ignored
evidence separately enumerated. Branch phase/03-cad-data-extraction,HEAD142100deb62e8eb3692ded84a95962548925731f,
no staging/commit/push/PR/tag/cleanup. No PHASE04 implementation.

**PHASE_03_FINAL_PASS_WITH_KNOWN_LIMITATIONS — submitted for owner acceptance.**
See [PHASE_03_FINAL_GATE.md](PHASE_03_FINAL_GATE.md),with all23requested sections,
exact proposed/excluded files and fresh evidence under
artifacts/phase03/final-gate-20261015/. STOP for owner review;Git release actions
and PHASE04 require subsequent authorization. Earlier checkpoint statuses above
remain historical and do not override this integrated final gate.
