# PHASE 03 Pre-Implementation Architecture Review (Re-Scoped)

## Executive Summary

The owner has decisively re-scoped PHASE 03 from a "Viewer-first" direction to **CAD Data Extraction Foundation**.
DtoB's core mission is a deterministic BIM generation pipeline:
$$\text{Drawing} \longrightarrow \text{factual CAD extraction} \longrightarrow \text{normalized CAD IR} \longrightarrow \text{intelligence} \longrightarrow \text{BIM IR} \longrightarrow \text{Revit}$$
The Viewer is relegated to a secondary, minimal diagnostic surface.

This review evaluates the re-scoped technical proposal and checklist in `docs/status/PHASE_03_CHECKLIST.md` before any package installation, code modification, or capability spike execution.

---

## Primary Architecture Review: Additive Boundary Analysis

### Evaluation of `IRawDrawingReader` $\rightarrow$ `RawDrawingReadResult` $\rightarrow$ `RawDrawingSnapshot`

In PHASE 00, `IDrawingReader` was established with:
```csharp
public sealed record DrawingReadResult(CadDocument? Document, Diagnostic[] Diagnostics);
public interface IDrawingReader
{
    Task<DrawingReadResult> ReadAsync(string path, CancellationToken cancellationToken = default);
}
```
`CadDocument` strictly enforces **normalized, mm-scale WCS geometry**, explicit scale mapping, strict layer/block/entity structures, and positive finite tolerances (ADR-006, ADR-008).

The proposed additive boundary:
- **`IRawDrawingReader`**: Reads an authoritative managed drawing and produces a `RawDrawingSnapshot`.
- **`RawDrawingSnapshot`**: Represents purely factual, un-normalized source CAD data (native units, raw definition-space geometry, explicit field presence/absence, raw un-evaluated ByLayer/ByBlock colors, and parser diagnostics).
- **Retention of `IDrawingReader`**: Remains untouched as the contract for generating normalized `CadDocument` (CAD IR v1), which will be implemented in PHASE 04 as a normalizing consumer of `RawDrawingSnapshot`.

### Architectural Verification
1. **No False mm Representation**: Raw coordinates retain their authoring source units. Unresolved/unitless drawings are recorded with `DeclaredUnit: Unknown/Unitless` and explicit provenance, never fabricated as mm.
2. **Raw OCS / Coordinate Frames**: Definition-space geometries and extrusion vectors remain raw. Extrusion normals other than $+Z$ are captured as raw facts rather than immediately failing structural validation.
3. **Handle as Source Identity**: Original source object handles (`ulong` formatted as canonical hex) are preserved as the immutable identity of source objects.
4. **Distinction of Definition vs Occurrence**: `RawBlockDefinition` (the template) and `RawInsert` (the occurrence reference) are modeled separately. Occurrences track the hierarchical INSERT chain without prematurely flattening geometries.
5. **Phase 04 Independence**: PHASE 04 can consume `RawDrawingSnapshot` through clean DtoB-owned data contracts without referencing or coupling to ACadSharp.
6. **No Secondary Permanent IR**: `RawDrawingSnapshot` is strictly an in-memory extraction artifact and QA evidence export; it is not serialized into project manifests, not written to `.dtob`, and does not replace CAD IR v1.

**Conclusion**: The additive raw extraction boundary is **architecturally sound and recommended**.

---

## ACadSharp Capability Spike Review

ACadSharp is positioned as the **first capability evaluation candidate**, not a pre-approved backend.
The proposed evaluation criteria correctly mandate testing against the **actual controlled DWG/DXF files** (`DtoB_TEST_2STORY_R2018.dwg` [AC1024] and `.dxf`) rather than synthetic fixtures.

### Audit of Critical ACadSharp Behaviors & Risks
- **`Failsafe = true` (Risk: Silent Omissions)**: ACadSharp defaults to `Failsafe = true` and `KeepUnknownEntities = false`. The spike configuration **must explicitly disable failsafe mode** or capture all parser notification events. Any swallowed or missing entity must be reported as a critical capability failure.
- **Default Value Fabrication vs. Source Facts**: ACadSharp initializes entity properties to C# defaults (e.g. `InsUnits`, elevation, colors). The spike must differentiate between explicit source field presence and library default initialization.
- **Reader Stream & Lease Ownership**: ACadSharp's file path overload opens streams with `FileShare.ReadWrite`. The spike must strictly use DtoB-managed `Stream` overloads with `FileAccess.Read` and `FileShare.Read`, guaranteeing immutable snapshot isolation.
- **`Insert.GetTransform()` Correctness**: ACadSharp's internal matrix getter handles base points and arbitrary axis transforms. The spike must independently verify transformation matrices against known placement coordinates rather than blindly trusting `GetTransform()`.
- **Cancellation Limitations**: ACadSharp lacks native `CancellationToken` support in reader loops. The spike must verify thread isolation and clean stream disposal when an operation is cancelled.

---

## Capability Spike Isolation

To protect the production codebase from premature dependency pollution:
- The capability evaluation must be conducted in an **isolated spike project**:
  `spikes/DtoB.Cad.ACadSharp.Spike` (or equivalent test harness).
- Neither `DtoB.Desktop`, `DtoB.Cad`, nor `DtoB.Project` may reference `ACadSharp` during the spike.
- If ACadSharp fails critical extraction gates (e.g. corrupts handles, drops entities silently, fails AC1024 loading, or miscalculates block transforms), execution must **STOP**.
- No fallback workarounds (AutoCAD COM automation, private Autodesk binaries, bitmap conversions) are permitted. If ACadSharp fails, an architectural review will evaluate commercial ODA/RealDWG under identical extraction criteria.

---

## Raw Model Scope & Boundary

The proposed `RawDrawingSnapshot` models are well-proportioned:
- **`RawHeader`**: Signature, version, INSUNITS code, declared extents, presence flags.
- **`RawLayer`**: Name, handle, color index/true color, flags, linetype.
- **`RawBlockDefinition`**: Handle, name, base point, child entity references.
- **`RawInsert` / `RawOccurrence`**: Position, scale, rotation, normal, attribute values, and ordered hierarchical path.
- **`RawEntity`**: Original handle, entity type, raw definition geometry, raw styling.
- **`RawFact / Diagnostic`**: Distinguishes `SourceConfirmed`, `LibraryDecodedPresenceUnknown`, `Unsupported`, and `Invalid`.

**Boundary Enforcement**:
- PHASE 03 strictly extracts raw facts and generates diagnostic reports.
- PHASE 04 owns unit conversion, OCS-to-WCS alignment, geometric cleanup (zero-length pruning, deduplication), spatial indexing, and CAD IR v1 assembly.
- PHASE 05+ owns semantic architectural recognition (walls, doors, spaces).

---

## Identified Issues

### Issue 1
**Severity: HIGH**
**File / Section**: `tasks/PHASE_03.md`, `tasks/PHASE_04.md`, `PHASE_INDEX.md`, `DtoB_FINAL_MASTER_PLAN.md`, `ARCHITECTURE.md`
**Problem**: The repository's authoritative task and planning documents still define PHASE 03 as "DWG Viewer" (focusing on Zoom/Pan/Fit, Layer Solo/Search, window selection, and rendering quality) and assign primary extraction to PHASE 04. While `docs/status/PHASE_03_CHECKLIST.md` proposes updates in Section 23, the governing task files remain unedited.
**Impact**: Contradictory governing documents create implementation ambiguity for Codex and compromise auditability against master plans.
**Recommended Change**: Update the authoritative task documents (`tasks/PHASE_03.md`, `tasks/PHASE_04.md`, and `PHASE_INDEX.md`) to reflect the extraction-first scope *prior* to beginning product implementation. The capability spike itself, however, can proceed in isolation.
**Required Test / Verification**: Verify that `tasks/PHASE_03.md` exit criteria match the factual extraction gates, not viewer graphic milestones.

### Issue 2
**Severity: MEDIUM**
**File / Section**: `docs/adr/ADR-015-Viewer-Backend.md` & Proposed ADR Sequence
**Problem**: `ADR-015-Viewer-Backend.md` was drafted for the superseded ODA Viewer trial. The checklist proposes ADR-016 (Reader Backend), ADR-017 (Raw Extraction Boundary), and ADR-018 (Reader Lifecycle), leaving ADR-015 marked as "superseded" in uncommitted status.
**Impact**: Retaining unaccepted draft ADRs with obsolete numbering creates confusion in the architecture decision log.
**Recommended Change**: Replace or repurpose ADR-015 directly as `ADR-015-Raw-CAD-Extraction-Boundary.md` to define the additive `IRawDrawingReader` and `RawDrawingSnapshot` architecture. ADR-016 can then be authored for `CAD-Reader-Backend` once the capability spike yields empirical evidence.
**Required Test / Verification**: ADR UTF-8 validation and sequence check pass cleanly in `FoundationDocumentTests`.

### Issue 3
**Severity: MEDIUM**
**File / Section**: `docs/status/PHASE_03_CHECKLIST.md` (Section 7, MINSERT handling)
**Problem**: The checklist notes that MINSERT array cell identification is unresolved and marks it as `ARCHITECTURE_DECISION_REQUIRED`.
**Impact**: If a drawing containing MINSERT is encountered, lack of occurrence identification policy could lead to unhandled crashes or inconsistent entity counts.
**Recommended Change**: For DtoB V1, explicitly classify `MINSERT` as **Unsupported** with an explicit diagnostic (`CadUnsupported` / `UNSUPPORTED_ENTITY`), preserving the base entity handle and array parameters as raw metadata without attempting multi-cell instance expansion.
**Required Test / Verification**: Synthetic or fixture test asserting `MINSERT` generates an explicit unsupported diagnostic without terminating the parse.

---

## Architecture Assessment

| Area | Assessment |
|---|---|
| **Extraction-First Phase Direction** | **PASS**. Directly aligns with DtoB's core goal of reliable Revit BIM generation rather than CAD visualization. |
| **PHASE 03 / PHASE 04 Boundary** | **PASS**. Clear separation: 03 extracts and preserves raw facts; 04 normalizes into mm WCS CAD IR and builds analysis structures. |
| **Additive Raw Reader Architecture** | **PASS**. Preserves the existing `CadDocument` (CAD IR v1) contract without corruption or premature normalization. |
| **RawDrawingSnapshot Design** | **PASS**. Appropriately typed, avoids reflection dumps of vendor models, captures explicit presence/absence. |
| **Handle / Identity Strategy** | **PASS**. Preserves `ulong` source handle value as canonical hex; respects ADR-007 occurrence path rules. |
| **Block Definition / Occurrence Strategy**| **PASS**. Maintains definition-occurrence separation; rejects cloned "Explode" entities. |
| **ACadSharp Capability Spike** | **PASS**. Rigorous evaluation criteria; testing against real controlled DWG/DXF fixtures. |
| **Read-Only Contract** | **PASS**. Enforces strict `FileAccess.Read` / `FileShare.Read` leases on managed files; zero writes or conversions. |
| **Unsupported Entity Policy** | **PASS**. Explicit diagnostics; no silent entity dropping or placeholder fabrication. |
| **QA Plan** | **PASS**. Independent group-code and handle comparison; requires exact count/scope/coordinate matching. |
| **Diagnostic Viewer Scope** | **PASS**. Secondary tool; minimal WPF geometry projection and Inspector; no AutoCAD-level plotting ambitions. |
| **Planning Document Update Requirement** | **PASS (with Issue 1)**. Task documents must be updated prior to production code implementation. |
| **ADR Strategy** | **PASS (with Issue 2)**. Clean sequence proposed; backend selection deferred until spike results are recorded. |
| **Future PHASE 04 Compatibility** | **PASS**. Clean data hand-off without downstream ACadSharp coupling. |

---

## Verdict

The architectural re-scoping is sound, necessary, and thoroughly prepared. The capability spike may proceed in an isolated spike workspace to establish empirical data on ACadSharp.

**APPROVED_FOR_CAPABILITY_SPIKE**


---

# Native DXF Reader Pre-Implementation Architecture Review

Date: 2026-10-14 (Asia/Seoul)
Author: Architecture / Adversarial Review Owner (Antigravity)
Subject: PHASE 03 — Native Text DXF Reader Pre-Implementation Architecture Review

## 1. Context & Executive Summary

Following the empirical failure of the ACadSharp capability spike (`ACADSHARP_CAPABILITY_FAIL`, where non-zero base point transformed INSERT occurrences suffered severe translation and rotation errors) and the complete rejection of both proprietary CAD SDK dependencies (ODA, RealDWG) and direct binary DWG parsing in V1, the owner has directed a clean, robust, and permanent architectural course:

**DtoB V1 will own its CAD extraction pipeline using a Native Text DXF Reader.**

The pipeline is now:
$$\text{DWG} \xrightarrow{\text{external user Save As/Export}} \text{Text DXF} \xrightarrow{\text{Native DXF Reader}} \text{RawDrawingSnapshot} \xrightarrow{\text{PHASE 04}} \text{CAD IR} \xrightarrow{\text{PHASE 05+}} \text{BIM IR} \xrightarrow{\text{Revit}}$$

No ACadSharp. No ODA. No RealDWG. No direct DWG decoder in V1. No automatic conversion heuristics.

This review provides the formal adversarial architecture inspection of:
- `docs/status/PHASE_03_CHECKLIST.md` (30-section comprehensive native DXF proposal)
- `docs/status/PHASE_03_NATIVE_DXF_DESIGN_REPORT.md`
- `docs/adr/ADR-015-Raw-CAD-Extraction-Boundary.md` (accepted & amended)
- `docs/adr/ADR-016-Native-DXF-Reader.md` (proposed)
- Updated task and master planning documents (`tasks/PHASE_03.md`, `tasks/PHASE_04.md`, `PHASE_INDEX.md`, `DtoB_FINAL_MASTER_PLAN.md`, `ARCHITECTURE.md`, `CODEX_INSTRUCTIONS.md`)

---

## 2. Critical Review 1 — Source of Truth & Identity Boundary

### Analysis of Input Provenance
In DtoB V1, users perform an external export or Save As from their CAD software (AutoCAD, ZWCAD, etc.) to Text DXF. DtoB does not execute or control this translation.
Therefore, **DtoB cannot and must not claim semantic or identity equivalence between the original DWG and the imported DXF file.**

The architectural boundary must be strictly formulated:
1. **Authoritative Input Boundary**: The source of truth for DtoB V1 begins exclusively at the **registered managed Text DXF file**.
2. **Source Identity Definition**: 
   $$\text{Source Identity} = (\text{DrawingId}, \text{RevisionId}, \text{FileHash}, \text{Original DXF Handle})$$
3. **Handle Semantics**: "OriginalHandle" in PHASE 03 strictly means **the handle as written in the imported DXF**, not an assumed upstream DWG database handle.
4. **No Unverified Claims**: DtoB makes no assertion that third-party CAD exporters preserved upstream database handles, anonymous block keys, dimension measurement overrides, or proxy object structures.

**Verification**: `ADR-016` (Decision & Consequences) and `PHASE_03_CHECKLIST.md` (§1, §13, §14) explicitly mandate this rule. Upstream DWG handles are treated as unverified external provenance.

---

## 3. Critical Review 2 — Terminology & Encoding Profile

### Terminology Formulation: "Text DXF" vs "ASCII DXF"
Historically, DXF documentation often conflated "ASCII DXF" with "non-binary DXF." However, starting with AutoCAD 2007 (AC1021) and continuing through AutoCAD 2018+ (AC1032), DXF text files are formatted in **UTF-8**, which is a superset of 7-bit ASCII.

- Using "ASCII DXF" loosely can mislead implementers into assuming that characters with byte values $\ge 128$ (e.g. Korean Hangul, European accented characters) are illegal or require ANSI/Windows codepage decoding.
- The official terminology across ADRs, checklists, code namespaces, and user documentation must be **Text DXF** (as opposed to **Binary DXF**).

### Encoding & Version Contract for V1
1. **Initial Profile**: Strict **AC1032 Text DXF**.
2. **Text Encoding**: Strict **UTF-8** (which naturally accepts the pure 7-bit ASCII subset).
3. **BOM Policy**: Optional UTF-8 BOM (`EF BB BF`) at byte 0 is cleanly detected and skipped. UTF-16 / UTF-32 BOMs cause immediate `UnsupportedEncoding` rejection.
4. **Line Endings**: Invariant handling of both CRLF (`\r\n`) and LF (`\n`).
5. **No OS-Default Guessing**: No fallback to Windows-1252, CP949, or system default encodings.
6. **$DWGCODEPAGE**: Persisted as informational raw source metadata; it does **not** switch the decoder away from UTF-8.
7. **DXF Unicode Escapes**: String escapes (such as `\U+XXXX` or `\M+nXXXX`) must be preserved in their raw lexical form; decoding is treated as a secondary derived display property with located diagnostics for malformed escapes.

---

## 4. Lexical Reader & Structural Parser Architecture

### Lexical Pair Representation
The proposed `DxfRawPair`:
```csharp
public sealed record DxfRawPair(
    int PairIndex,
    int GroupCode,
    string RawCodeText,
    string RawTextValue,
    int CodeLineNumber,
    int ValueLineNumber,
    SourceSpan? ByteSpan = null);
```
- **Strengths**: Preserves physical line numbers for precise diagnostic locators. Captures `RawCodeText` and `RawTextValue` verbatim, protecting against loss of significant formatting (e.g. trailing zeros in `10.500`, uppercase hex `1A` vs `1a`, leading spaces in text values).
- **Memory Safety**: Pairs are owned in a single sequential evidence store per document. Downstream structures reference pair indices or contiguous ranges rather than re-allocating copies.
- **Conversion Discipline**: Parsed typed values (`SourceFact<T>`) accompany the pair; parsed values never overwrite or mutate the raw text string. Invariant culture parsing is enforced.

### Structural State Machine
The top-level parser enforces a strict finite state machine:
$$\text{Initial} \longrightarrow \text{SECTION} \longrightarrow \text{Known Section Handler} \longrightarrow \text{ENDSEC} \longrightarrow \dots \longrightarrow \text{EOF}$$

- **Structural Strictness**:
  - Nested `SECTION` declarations $\longrightarrow$ `InvalidInput`.
  - Duplicate singleton sections (e.g. multiple `HEADER`) $\longrightarrow$ `InvalidInput`.
  - Premature EOF, missing `ENDSEC`, or missing `EOF` $\longrightarrow$ `InvalidInput`.
  - Malformed block delimiters (missing `ENDBLK` or nested `BLOCK`) $\longrightarrow$ `InvalidInput`.
  - Malformed entity sequences (e.g. `POLYLINE` without matching `SEQEND`, `ATTRIB` following non-INSERT) $\longrightarrow$ localized `InvalidInput` or `Partial`, never silent ingestion into subsequent entities.
- **No Malformed File Becomes a Valid Empty Drawing**: A corrupt or truncated file must fail deterministically with located diagnostics; it can never produce a clean `DrawingReadResult.Succeeded` with an empty entity array.

---

## 5. Handle, Ownership, and Table Semantics

### Contextual Handle Disambiguation
The native parser avoids naive assumptions:
1. **Group Code 5 vs 105**:
   - In most entities and tables, code 5 represents the persistent object handle.
   - In the `DIMSTYLE` table, code 105 represents the handle (while code 5 serves a different legacy role). The parser handles `DIMSTYLE` contextually.
   - Code 5 in the `HEADER` section (e.g. `$HANDSEED`) is a generator seed, not an entity handle.
2. **Ownership (Group Code 330)**:
   - Group 330 represents soft/hard ownership pointers only within their documented entity/table context.
   - Group 330 inside reactor lists (`{ACAD_REACTORS`) or extension dictionaries (`{ACAD_XDICTIONARY`) must not be misinterpreted as the parent block/space owner.
3. **No Synthetic Handles**: Missing, duplicate, or unparseable handles produce explicit diagnostics (`INVALID` / `UNSUPPORTED`). The reader never generates surrogate handles (such as UUIDs or geometry hashes) for source objects.

---

## 6. Blocks and INSERT Occurrence Boundary

### Strict PHASE 03 Extraction Scope
- **`BLOCK`**: Extracted with its name, base point coordinates ($X, Y, Z$), flags, handle, and ordered child entity definitions.
- **`INSERT`**: Extracted with its handle, target block name (code 2), insertion point ($X, Y, Z$), scales ($X, Y, Z$), rotation angle in **source degrees**, normal/extrusion vector, row/column array parameters, and attribute-follow flags.

### Strict Non-Transformation Policy
**PHASE 03 MUST NOT calculate final, local, or nested insertion transformation matrices.**
The previous ACadSharp failure was caused by buggy internal transform math. PHASE 03 extracts raw placement parameters as written in the source text.
PHASE 04 solely owns:
- Documented defaults (scale = 1.0, normal = $(0,0,1)$).
- Unit conversion (degrees $\to$ radians, source units $\to$ mm).
- Block base point offset compensation.
- Arbitrary Axis Algorithm (OCS $\to$ WCS).
- Composed transformation matrices for nested INSERT occurrences.

### MINSERT Policy
MINSERT (multiple INSERT) array expansion is **explicitly unsupported in V1**.
- The parser preserves the base entity handle and captures row count, column count, and spacing as raw properties.
- An `UNSUPPORTED_ENTITY` diagnostic is emitted.
- No virtual array cells or synthetic IDs are generated. The parser continues safely to subsequent records without exploding or flattening the array.

---

## 7. Raw Snapshot & Package Architecture

### Proposed Package Separation
```text
DtoB.Cad.Raw  (Pure .NET 8 library: typed raw contracts, SourceFact<T>, SourceSpan, diagnostics)
      ^
      |
DtoB.Cad.Dxf  (Pure .NET 8 library: lexical tokenizer, structural state machine, DXF mapper)
```

- **Dependency Discipline**:
  - `DtoB.Cad.Raw` has zero dependencies on CAD SDKs, WPF, or Revit.
  - `DtoB.Cad.Dxf` depends only on `DtoB.Cad.Raw` and standard .NET runtime libraries.
  - Neither library references `DtoB.Core` geometry (`Point3`, `Transform3`), ensuring that raw authoring coordinates and un-normalized frames remain isolated from Core mm assumptions.
- **Lifecycle & Authority**:
  - `RawDrawingSnapshot` is strictly an in-memory extraction result and QA export artifact.
  - It is not persisted in `.dtob` project files and does not replace `CadDocument` (CAD IR v1).

---

## 8. Entity Coverage Strategy for V1

| Category | Entities Included | Policy |
|---|---|---|
| **Core Required** | `LINE`, `LWPOLYLINE`, `ARC`, `POINT`, `SOLID`, `TEXT`, `MTEXT`, `DIMENSION`, `BLOCK`, `INSERT` | Must extract exact coordinates, handles, and properties from controlled fixtures. |
| **Separately Gated** | `CIRCLE`, `POLYLINE` / `VERTEX` / `SEQEND`, `ATTRIB` / `ATTDEF` | Implemented via explicit subphase with dedicated fixtures; support claimed only after independent verification. |
| **Initially Unsupported** | `ELLIPSE`, `SPLINE`, `HATCH`, 3DSOLID, Proxy Objects, Unknown custom entities | Retained as raw un-parsed records with `UNSUPPORTED` disposition; no approximation into lines or meshes. |

This provides a disciplined, verifiable boundary tailored to the controlled 2-story drawing requirements of DtoB.

---

## 9. Resource Limits & Parser Security

To prevent denial-of-service or memory exhaustion from adversarial or corrupted DXF files, the reader must enforce explicit, configurable guards before unbounded buffering:
- Maximum physical line length (e.g. 32 KB per line).
- Maximum total pair count per drawing.
- Maximum nested control group depth (`{ ... }` limit $\le 16$).
- Maximum diagnostics collection cap (e.g. 10,000 diagnostics before summarizing).
- Cooperative `CancellationToken` checks on every read loop iteration.
Exceeding a budget results in `ResourceLimitExceeded` with a partial diagnostic snapshot; it never creates a truncated "success" or crashes the application process.

---

## 10. QA & Verification Strategy

QA must be strictly independent of the parser being tested:
1. **No Self-Validating Oracles**: Expected counts and coordinates must not be generated solely by running the new native parser.
2. **Independent Group-Code Audit**: Expected entity counts and line numbers are derived from:
   - Hand-audited group-code inspection of `DtoB_TEST_2STORY_R2018.dxf`.
   - The established baseline inventory (e.g. LINE: 59, LWPOLYLINE: 16, ARC: 1, INSERT: 25, TEXT: 36, MTEXT: 4, DIMENSION: 4, POINT: 12, SOLID: 1, LAYER: 12, BLOCK: 12).
   - Known sample handles and coordinates audited directly from physical file lines.
3. **Negative & Edge-Case Fixtures**: Dedicated minimal test files for:
   - Truncated files (missing `EOF`, truncated pair).
   - Corrupted numbers (non-finite, NaN, scientific notation edge cases).
   - UTF-8 with BOM and without BOM.
   - Rejecting binary DXF headers and UTF-16 encodings.
   - MINSERT handling.

---

## 11. Minimal Diagnostic Viewer (Subphase 03H)

The diagnostic Viewer is confirmed as a strictly secondary inspection tool:
- Consumes `RawDrawingSnapshot` directly.
- Renders only directly displayable native-frame geometries (`LINE`, `LWPOLYLINE`, `ARC`, `POINT`, `SOLID`) in ModelSpace.
- Displays block definitions in a separate local definition canvas.
- Represents INSERT occurrences as raw placement point markers and vectors (without attempting nested composed geometry rendering).
- Renders text and dimensions as raw placement indicators with text inspector panels.
- Clicking an element inspects its source handle, type, layer, and original DXF line numbers.
- **Strictly excludes**: AutoCAD-quality plotting, lineweight simulation, font rendering (SHX/TrueType layout engines), pattern-hatch graphics, and complex window selection algorithms.

---

## 12. Planning Documents & ADR Assessment

### Planning Document Synchronization
Codex has updated:
- `tasks/PHASE_03.md` (renamed to "Native DXF Data Extraction Foundation", subphases 03A–03H).
- `tasks/PHASE_04.md` (owns normalization, mm conversion, and block transforms).
- `PHASE_INDEX.md`, `DtoB_FINAL_MASTER_PLAN.md`, `ARCHITECTURE.md`, `CODEX_INSTRUCTIONS.md`.

All documents now uniformly reflect the Native Text DXF architecture, eliminating prior Viewer-first and SDK-first contradictions.

### ADR-016 Status
`docs/adr/ADR-016-Native-DXF-Reader.md` is currently marked as **Proposed**.
It accurately details the native DXF approach, AC1032 UTF-8 profile, managed authority, lexical ownership, and transform boundaries.
**Recommendation**: ADR-016 is architecturally complete, sound, and ready to be marked **Accepted** upon approval of this review.

---

## 13. Identified Issues

### Issue 1
**Severity: MEDIUM**
**File / Section**: `docs/adr/ADR-016-Native-DXF-Reader.md`, `tasks/PHASE_03.md`, `PHASE_INDEX.md`
**Problem**: The proposal and ADR-016 repeatedly use the phrase **"ASCII DXF"** (e.g. "ASCII/Text DXF", "Native ASCII DXF") alongside "AC1032 UTF-8". While the intent is to contrast with Binary DXF, "ASCII" formally denotes 7-bit US-ASCII ($0\text{--}127$). AC1032 DXF text files generated by modern CAD applications are explicitly UTF-8 encoded and routinely contain multi-byte characters.
**Impact**: Ambiguity regarding whether valid multi-byte UTF-8 characters (e.g. Korean annotations) are permitted, or whether non-ASCII bytes will be rejected as encoding errors.
**Recommended Change**: Establish **"Text DXF"** as the primary official domain term across ADR-016, task documents, and code namespaces. Explicitly state that "Text DXF" distinguishes the format from "Binary DXF", and that the AC1032 profile mandates strict **UTF-8** (which includes ASCII as a subset).
**Required Verification**: Verify that ADR-016 and checklist define the encoding as strict UTF-8 Text DXF and verify non-ASCII UTF-8 characters are accepted in test fixtures without being rejected as non-ASCII.

### Issue 2
**Severity: MEDIUM**
**File / Section**: `docs/status/PHASE_03_CHECKLIST.md` (§1, §13, §14), `docs/adr/ADR-016-Native-DXF-Reader.md`
**Problem**: While the documents note that DWG export is external, they do not sufficiently emphasize to future implementers that **DtoB provides no guarantee of identity or semantic equivalence between upstream DWG entities and exported DXF entities.**
**Impact**: Risk that developers in PHASE 04 or PHASE 05 attempt to reconcile DWG handles with DXF handles or assume DXF block definitions mirror DWG internal structures identically.
**Recommended Change**: Add an explicit architectural principle to ADR-016 and the checklist:
> *"DtoB V1 source identity is defined exclusively by the imported Text DXF. DtoB makes no claim and assumes no responsibility for handle stability, proxy object degradation, or structural shifts introduced by third-party CAD applications during DWG-to-DXF export."*
**Required Verification**: Check that ADR-016 and checklist contain this explicit disclaimer.

### Issue 3
**Severity: LOW**
**File / Section**: `docs/status/PHASE_03_CHECKLIST.md` (§20, Resource Limits)
**Problem**: The resource limits (line length, pair count, diagnostic cap) are marked as "to be measured and configured during implementation" without initial baseline safety caps.
**Impact**: Early subphase parser tests without hard limits could hang or consume excessive memory if fed malformed loop fixtures.
**Recommended Change**: Define explicit initial development bounds in code constants (e.g. max line length = 64 KB, max nesting depth = 16, max diagnostics = 5,000) that can be overridden by configuration or test parameters.
**Required Verification**: Automated test verifying parser rejects lines exceeding the safety cap with `ResourceLimitExceeded`.

---

## 14. Architecture Assessment

| Evaluation Area | Status | Notes |
|---|---|---|
| **V1 Text DXF Strategy** | **PASS** | Cleanest possible path for V1; eliminates unstable third-party SDK dependencies and native crashes. |
| **Source of Truth Boundary** | **PASS (with Issue 2)** | Managed Text DXF is the sole authority; external DWG conversion is outside DtoB scope. |
| **Encoding / Version Policy** | **PASS (with Issue 1)** | AC1032 profile with strict UTF-8, BOM handling, and CRLF/LF invariance is robust. |
| **Lexical Reader Architecture** | **PASS** | `DxfRawPair` preserving raw text spelling and physical line numbers ensures total auditability. |
| **Structural Parser State Machine** | **PASS** | Explicit section/delimiter machine prevents malformed streams from becoming valid empty drawings. |
| **Raw Fact / Provenance Model** | **PASS** | `SourceFact<T>` with field presence/absence tracking prevents false source defaults. |
| **Handle / Owner Disambiguation**| **PASS** | Context-sensitive handling of code 5 vs 105, reactor 330 exclusion, and canonical hex equality. |
| **Block / INSERT Boundary** | **PASS** | Zero matrix computation in PHASE 03; all transforms cleanly deferred to PHASE 04. |
| **MINSERT Array Policy** | **PASS** | Preserves base metadata, emits diagnostic, skips virtual cell expansion, continues safely. |
| **Entity Coverage Discipline** | **PASS** | Core 2D primitives + text/dimension covered; legacy and complex families gated or unsupported. |
| **Resource Safety Design** | **PASS (with Issue 3)** | Line and pair limit guards prevent resource exhaustion. |
| **PHASE 02 Integration** | **PASS** | Authoritative `ManagedFilePath` only; zero source I/O; clean immutable leases. |
| **PHASE 03 / PHASE 04 Separation** | **PASS** | 03 = raw extraction & diagnostics; 04 = normalization, mm scale, and CAD IR. |
| **Diagnostic Viewer Scope** | **PASS** | Secondary native-frame diagnostic projection only; no AutoCAD graphics ambitions. |
| **Package Separation** | **PASS** | `DtoB.Cad.Raw` and `DtoB.Cad.Dxf` split cleanly without leaky abstractions. |
| **ADR-016 Readiness** | **PASS** | Comprehensive, thorough, and ready for acceptance. |

---

## 15. Verdict

The re-scoped Native Text DXF Reader architecture is **exceptionally well-conceived, technically rigorous, and aligned with DtoB's core Revit BIM mission.** It decisively resolves the external SDK failure mode and establishes complete ownership over the extraction pipeline.

Subject to incorporating the terminology and identity clarifications in Issues 1 and 2:

**APPROVED_FOR_NATIVE_DXF_IMPLEMENTATION**

*(Do NOT begin implementation until authorized by the project owner. Implementation must follow the phased 03A–03H sequence.)*
