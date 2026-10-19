# CAD v2 contract foundation (04A)

ADR-018/019/020 govern this namespace. It contains immutable contracts and a
typed codec, not a normalization engine. Geometry payloads are representable;
no code here converts source units/angles, creates an INSERT matrix, expands
occurrences, resolves styles, converts bulges or computes spatial bounds.

`CadIrJson` accepts only a v2 `CadDocument`. It never uses the legacy Core version
gate. Every declared JSON member is required, including explicit null for nullable
facts; a null means unavailable, never a synthesized zero/default. Unknown,
duplicate, missing, malformed and unsupported-major fields are rejected. Enums
use exact camel-case names, not numeric values. `$kind` discriminates closed
geometry variants. Fixed property order is schemaVersion first, then ordinal
camel-case names; JSON is UTF-8/no BOM, invariant finite numbers, canonical zero,
LF and one final LF. The codec does not reorder source collections or renumber refs.

Collection semantics:

- Provenance records: first pair, source domain, source record index; unique
  domain/index. Facts: registry record index, then ordinal unique field role.
  Pairs are sorted unique zero-based indices; physical lines are one-based,
  inclusive ranges. Absent fact has empty pairs and explicit null line range.
- Layers, definitions, entities and geometry: source record order; derived
  segments use source segment order. Catalog order is supplied by the future engine.
- Definition children and model-space records retain source order. Occurrence
  paths are outer-to-inner; future occurrence collection order is DFS source
  order, with array cells row-major. Contract construction does not perform DFS.
- Style catalogs use source/context order; evidence/diagnostics retain stable
  source, path, stage, code and field ordering supplied by the future engine.
- Ledger is exactly one entry per registry record in registry-index order.
  References must resolve. A Partial document can retain valid siblings and
  explicit unsupported/invalid outcomes. Completeness is not inferred from
  diagnostic count.

All collections copy input and return read-only views. Elements are sealed
immutable contracts; geometry inheritance is restricted to this assembly.
Resolved properties accept only immutable name/color values. CadTransform owns
matrix copies; its Values view cannot mutate storage and ToTransform3 exports a
fresh copy. It stores values only and does not implement algebra.

CadId is distinct from source handles. Framing v1 uses tags null=0, text=1,
GUID=2, FileHash=3, canonical handle=4, nonnegative index=5, tuple=6. Each frame
is tag byte + UInt32 BE byte length + payload. A tuple payload begins with
UInt32 BE field count. Root is Tuple(Text("dtob/cad2/id/v1"), Text(domain),
Tuple(fields)). Scalars use strict UTF-8; GUID lower-N, hash uppercase hex,
Raw-validated canonical handle, index invariant decimal. Output is cad2: plus
lowercase SHA-256 hex. The tests freeze an independently enumerated frame vector.
This primitive does not generate an occurrence tree or normalize a raw handle
in place.

Structure checks cover required values, schema, source/evidence ranges and
references, unique IDs, closed enum/variant types and ledger coverage. Numeric
normalization, plane orthonormality, coordinate degeneracy, analytic curve
bounds and occurrence graph/placement validation belong to later subphases.
Canonical conventions declare the target, not proof that conversion occurred.
The test fixture is hand-authored contract data, not production normalized output.
