# ADR-020 — Occurrence identity and expansion

Status: Accepted

Date: 2026-10-15

## Context

PHASE 04 introduces nested INSERT and MINSERT placement while Raw keeps original handles and unsupported-expansion evidence. Legacy ADR-007/v1 hex-string paths cannot distinguish generated cells. [Review](../../reviews/PHASE_04_PLAN_REVIEW.md) L-01 requires one global expansion budget; M-03 requires an explicit ADR gate. This proposes CAD2 identity/expansion, not acceptance or production code.

## Decision proposed

### Definitions, occurrences and compact lineage

CadBlockDefinition is shared factual content: definition BLOCK/BLOCK_RECORD source refs, name/handle, mm base and ordered entity/INSERT templates. CadBlockOccurrence is a placement of one real source INSERT into a parent context: definition ref, source INSERT ref, parent occurrence, typed outer-to-inner OccurrencePath, optional array r/c, local/accumulated immutable transform and effective-style context.

Keep source entities shared, with world leaves pointing to geometry+occurrence; no anonymous flattening, no clone of full Raw graphs. SourceRecordRef/SourceFactRef resolve document identity and original record/pair/physical line through one registry (ADR-018). Paths preserve every definition/INSERT step; a cell points to the same original INSERT, never a newly invented DXF handle. Source INSERT templates are not occurrence nodes.

### Deterministic cad2 identity

Leave Core.CadIdentity/legacy IDs unchanged. New identity scope contains DrawingId/RevisionId/FileHash and Raw-validated canonical handle; preserve OriginalHandle separately. Handleless source uses stable domain-qualified record locator. Same revision/file/context yields the same ID; new revision or bytes can change it. No cross-revision matching promised.

Proposed exact encoding policy: SHA-256 of a binary length-framed tuple, with UTF-8 domain tag `dtob/cad2/id/v1`, entity domain, then ordered typed fields. Each field includes explicit type tag and unsigned big-endian byte length followed by bytes; nested tuples include field count. GUIDs use lowercase N form; FileHash uses validated canonical uppercase hex; handles use Raw graph canonical spelling; integer indices use invariant decimal with no sign/leading zero except zero; array absence has a distinct null/type marker, not -1 or empty string. Domain distinguishes source, definition, source-insert, occurrence, leaf and derived component. Public ID is `cad2:` + lowercase hexadecimal SHA-256. Freeze encoding in fixtures before implementation acceptance; this is tuple hashing, not file-content I/O.

Occurrence tuple contains source INSERT identity and complete parent path; typed cell steps add row/column. Ordinary step has no cell fields and differs from MINSERT(0,0). Leaf tuple adds source geometry identity and derived segment ordinal where applicable. No random GUID, path/time, geometry hash or ambiguous string concatenation. Do not confuse these IDs with OriginalHandle.

### Nested transforms and MINSERT

Use [ADR-019](ADR-019-Coordinate-Normalization-Transforms.md) column vectors and exact local placement:

```text
L = A * T(i) * Rz(theta) * S * T(-base)
Mchild = Mparent * Lchild
d(r,c) = (c*columnSpacingMm, r*rowSpacingMm, 0)
Lrc = A * T(i) * Rz(theta) * T(d(r,c)) * S * T(-base)
```

Cell spacing is in normalized drawing lengths; rotate array offsets with INSERT, do not multiply by its own geometry scale. Parent transform still acts on nested grid. This is DtoB's explicit derivation of [INSERT source fields](https://help.autodesk.com/cloudhelp/2024/ENU/AutoCAD-DXF/files/GUID-28FA4CFB-9D5E-4880-9F11-36C97578252F.htm) and [rotated MINSERT array behavior](https://help.autodesk.com/cloudhelp/2016/ENU/AutoCAD-MAC-Core/files/GUID-A780A2FA-4A2E-4574-950F-E788AB71F527.htm); independently verified fixtures remain a mandatory 04E gate.

Rows/columns positive; documented absent defaults 1, spacing defaults 0 with evidence. Zero spacing retains coincident but distinct cells. Negative spacing follows signed offsets with fixtures. Row-major r outer/c inner, zero-based, ordinal r*columns+c. Per cell, depth-first definition-source-order descendants precede the next cell. At roots, source INSERT order applies. Source order, not map enumeration, governs all expansion.

### Cycles, budgets and safe termination — L-01

Use an active block-definition reference stack per traversal branch. Ownership/reference completeness does not imply no INSERT cycles. Direct/indirect reference cycle or missing/ambiguous target rejects that branch with full path, preserving unrelated valid roots and Partial status. Do not globally mark a definition visited and suppress legitimate repeated occurrences.

| Resource | Initial development default / counting rule |
| --- | --- |
| Depth | 32 occurrence steps; root INSERT depth 1; each ordinary nested INSERT or cell adds 1; no synthetic array-container step |
| Single MINSERT | 10,000 cells; checked rows*columns before allocation |
| Total occurrences | 100,000 for the entire normalization session |
| Derived geometry | 250,000 materialized derived geometry/leaf entries session-wide; includes references per occurrence, not just shared definitions |
| Diagnostics | 5,000 including one reserved terminal slot; count/overflow explicit, no silent successful truncation |

`TotalOccurrencesAllocated` is **one normalization-session-wide monotonically increasing counter**. Count each admitted CadBlockOccurrence exactly once: top-level ordinary INSERT, nested ordinary INSERT and every MINSERT cell, including arrays inside blocks and recursively nested arrays. Do not allocate a separate array container occurrence; source templates do not consume this count. It never resets per definition, parent, root, entity, recursion frame or after a branch is rejected. No decrement/refund allows allocation churn to evade the limit. One new Normalize call starts one new counter.

Before each occurrence allocation reserve against the global counter using checked arithmetic: if next total >100,000 (or injected lower test limit), issue ResourceLimitExceeded and terminate deterministically without allocation/publication. Exactly 100,000 is allowed if all other budgets permit. Local count<=10,000 does not override the global check. Branch-local geometry errors can yield Partial; exceeding any global resource budget aborts the result with **no CadDocument**, never a successful first-N prefix. Check depth and cell product before recursive descent/allocation. Development limits are injectable/validated, not recognition tolerances or performance claims.

Depth or per-MINSERT resource-cap exhaustion likewise returns ResourceLimitExceeded without a document; do not turn a capped subtree into successful Partial coverage. Invalid source counts (zero/negative) are branch-invalid facts, distinct from valid counts exceeding resource capacity. Shared definition primitives and root primitives also consume the derived-geometry budget when materialized; all output allocations must be bounded.

Check cancellation on stage entry, each source/entity/occurrence/cell loop and between bounded primitive/serialization batches, including large definitions. Cancellation is distinct from ResourceLimitExceeded and publishes no document. No hidden background worker; caller controls scheduling. All diagnostics/resources follow deterministic source traversal order.

### Style context and ordering

Occurrence context supplies layer 0 inheritance and containing INSERT effective ByBlock properties. Shared definition source style stays unchanged; effective properties are stored/resolved per occurrence with rule/source chain. Explicit property wins; ByLayer uses effective layer; ByBlock recurses through real parent context, never inferred from geometry/ID. Root unresolved remains explicit. State/diagnostic rules follow ADR-018 and plan section 15; identity does not include mutable effective style.

Catalog/source objects ordered by stable source domain/index/pair with ordinal tie-breakers; occurrence DFS/source order/row-major; geometry source+segment order; diagnostics source locator/path/stage/code/field. Sorting and tuple encoding use invariant/ordinal rules. Independent process/culture/CWD/lookup-insertion order must produce identical bytes/IDs. Machine paths/timestamps never enter CAD2 canonical identity.

## Alternatives

Flat anonymous geometry or fake cell handles: rejected for lost lineage. Random IDs: rejected for nondeterminism. Reuse v1 string path: cannot encode cells without a breaking policy change. Per-parent/per-definition budgets: rejected for exponential nesting bypass. Selected shared definitions + typed source paths + one session-wide budget.

## Consequences / migration impact

CAD2 IDs are distinct from v1 and are revision scoped. New cells are normalized occurrences, not extracted DXF entities. Raw MINSERT remains historically unsupported-expansion evidence; no Raw status rewrite. Preserve ADR-007/v1 behavior; no BIM stable-ID migration here. Compact paths/counters add bounded bookkeeping but no database, reconciliation or parser dependency.

## Required future tests / governance

- Nested/repeated/cyclic/missing definitions, exact DFS and row-major goldens, ordinary vs cell0, original-handle preservation and contextual styles.
- Identity framing ambiguity/culture/leading-zero/raw-canonicalization and stable segment paths.
- `MinsertResourceExplosionTests`: multiple and recursively nested arrays individually below 10,000 but globally above budget; boundary 100,000/100,001; equivalent injected small profiles; counter never resets/refunds; no output document after exhaustion.
- Depth 32/33, checked product overflow, geometry/diagnostic limits, cancellation inside nested loops and unchanged Raw evidence.

Tests are future requirements only. **ADR-020 must be Accepted before 04E** (with ADR-019); also before 04F occurrence-context inheritance and affected 04G contracts/assembly. Authoring all three now permits independent complete-foundation review, not implementation or acceptance.
