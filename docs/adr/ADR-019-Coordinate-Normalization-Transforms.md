# ADR-019 — Coordinate normalization and transforms

Status: Accepted

Date: 2026-10-15

## Context

PHASE 04 needs OCS placement and affine occurrence geometry while preserving Raw. [Plan review](../../reviews/PHASE_04_PLAN_REVIEW.md) M-01 requires immutable matrix ownership; M-02 requires exact affine curve bounds; M-03 requires approval before affected implementation. This proposes extending ADR-006's v1 applicability to CAD2; it does not change v1 contracts or modify Geometry during this gate.

## Decision proposed

### Conventions and actual Transform3 semantics

Length **mm**, angle **radians**, **right-handed XYZ, +Z up, CCW positive**. Row-major 4x4 affine matrices act on column vectors; translation indices 3,7,11; last row 0,0,0,1. Inspection of Geometry.cs confirms Apply computes row dot point plus translation; Then computes `next.Matrix * this.Matrix` with fresh output storage. Thus `inner.Then(outer)` applies inner then outer. Existing Placement is T*Rz*S for +Z, not a complete block transform.

Reuse existing algebra; add only small missing vector helpers in a later authorized implementation. DXF OCS logic belongs in Normalize, never parsers. Definition geometry becomes WCS-oriented local mm; occurrence matrices place it in drawing WCS mm. SourceToModel starts identity mm->mm. No UCS/recenter/alignment/feet policy.

### Immutable transform boundary — M-01

Transform3(double[] Matrix) is mutable and cannot itself be stored/exposed as immutable CAD2 state. **Clone on every input/factory/deserialization boundary and never expose owned storage.** Proposed CAD2 transform value holds a private defensive copy of 16 validated doubles, has no mutation/init-array escape, and offers scalar access or a fresh Transform3/array copy on every external projection. Serializer reads the owned values without publishing that array. This is an ownership wrapper, not a second matrix engine; calculations still use existing Transform3 on private working copies.

Apply to SourceToModel, definition transforms if present, entity transforms, local and accumulated occurrence matrices and defaults. Different occurrences never share writable arrays, even if numerically equal. Copy-on-construction alone is insufficient if getters expose the private array: copy-on-read/export is mandatory. Record copying/with-expression, caching or deserialization must not bypass this rule. Later geometry/document DTO construction uses frozen/read-only collections or owned snapshots as appropriate; no shared external matrix reference survives publication. Do not change existing Geometry/v1 or the 03H Raw viewer in this gate.

`TransformImmutabilityTests` must mutate original constructor arrays, input Transform3, exported transforms/arrays and one occurrence projection, then prove another occurrence, shared definition, previously created state, serialized output and bounds remain unchanged. This policy replaces the earlier plan's weaker revalidate-before-serialization-only description.

### Units

INSUNITS supported: 1 inch*25.4; 2 foot*304.8; 4 mm*1; 5 cm*10; 6 m*1000. Unitless 0/absent requires an explicit supported-unit decision/reason/ID or no document (NeedsExplicitUnits). Malformed/conflicting duplicate/unsupported values refuse explicitly; contradiction of a valid declared unit is rejected. No size/name/profile guessing. Raw facts stay untouched.

Coordinates, bases, elevations, radius/width and grid spacing convert once; scales/normals dimensionless. Literal lineweight 370 is hundredths-mm independently of drawing unit. BLOCK_RECORD 70 means units, not BLOCK flags. Initially accept absent/0/matching block units using stored coordinates/INSERT scales; contradictory units refuse the branch, without replaying unknown insertion-time conversion defaults.

Sources: [header units](https://help.autodesk.com/cloudhelp/2024/ENU/AutoCAD-DXF/files/GUID-A85E8E67-27CD-4C59-BE61-4DC9FADBE74A.htm), [BLOCK_RECORD](https://help.autodesk.com/cloudhelp/2023/ENU/AutoCAD-DXF/files/GUID-A1FD1934-7EF5-4D35-A4B0-F8AE54A9A20A.htm), [insertion settings](https://help.autodesk.com/cloudhelp/2025/ENG/AutoCAD-Core/files/GUID-6C46049D-8636-442D-8BAC-CF4FD515FDC0.htm).

### Angular field roles

| Current mapped fact | Normalized meaning |
| --- | --- |
| ARC start/end | Degrees*π/180; wrapped start [0,2π), positive CCW sweep; zero/ambiguous sweep not a fabricated circle |
| INSERT rotation | Degrees*π/180; absent documented zero with Defaulted evidence |
| TEXT and ATTRIB/ATTDEF text rotation | Degrees*π/180; absent documented zero; retain alignment/justification without font reconstruction |
| Supported linear/rotated DIMENSION rotation | Degrees*π/180; preserve absent state unless documented subtype default approved |
| POINT angle | Optional degree->radian display/UCS fact only; does not rotate position |
| STYLE oblique | Signed degree->radian metadata, not text-layout geometry |
| MTEXT | WCS direction vector; documented code 50 rotation is already radians, not degrees; contextually colliding column values are not rotation |

Unambiguous MTEXT direction/rotation precedence needs field-context fixtures; otherwise unsupported orientation, retaining source. Other angular pairs require reviewed role mapping, not a global group 50 conversion. Bulge is dimensionless with signed sweep 4*atan(b), not source degrees. Preserve original facts; normalize -0 only downstream.

Sources: [MTEXT](https://help.autodesk.com/cloudhelp/2020/ENU/AutoCAD-DXF/files/GUID-5E5DB93B-F8D3-4433-ADF7-E92E250D2BAB.htm), [general angle codes](https://help.autodesk.com/cloudhelp/2024/ENU/AutoCAD-DXF/files/GUID-3F0380A5-1C15-464D-BC66-2C5F094BCFB9.htm). Entity-specific exception wins over range assumptions; object-API radians are not file-format proof.

### OCS/WCS and AAA

Use finite nonzero normal n, rescale by max(abs components), normalize to unit N. Wholly absent normal defaults +Z with rule evidence; initial partial/zero/nonfinite normal is invalid unless a documented per-field default is approved/tested. Tiny nonzero magnitude alone is not invalid.

```text
if abs(Nx) < 1/64 and abs(Ny) < 1/64:
    Ax = normalize(WorldY × N)
else:
    Ax = normalize(WorldZ × N)
Ay = normalize(N × Ax)
A = [Ax Ay N]       # columns, origin zero
pointW = A * pointO # w=1
vectorW = A * vecO  # w=0, never translation
```

The strict 1/64 branch is a specified constant, not tolerance. AAA specifies normalizing Ay too; in exact arithmetic unit N/Ax already imply unit Ay. Normalize in numeric implementation and verify orthonormality/RH determinant.

Field frames: LINE/POINT positions WCS; MTEXT insertion/direction WCS. Circle/arc/SOLID/TEXT/INSERT insertion and LWPOLYLINE x/y/elevation use their OCS roles. DIMENSION 10/13/14 point triplets WCS, text midpoint 11 OCS for supported profiles. TEXT anchors OCS, with justification flags retained. Missing Z defaults only per documented field, never global flattening. LWP elevation enters point z once before basis conversion.

Primary sources: [AAA](https://help.autodesk.com/cloudhelp/2024/ENU/AutoCAD-DXF/files/GUID-E19E5B42-0CC7-4EBA-B29F-5E1D595149EE.htm), [OCS](https://help.autodesk.com/cloudhelp/2024/ENU/AutoCAD-DXF/files/GUID-D99F1509-E4E4-47A3-8691-92EA07DC88F5.htm), [dimension frames](https://help.autodesk.com/cloudhelp/2020/ENU/AutoCAD-DXF/files/GUID-EDD54EAC-A339-4EBA-AEA6-EC8066505E2B.htm).

### BLOCK and INSERT derivation

For definition q and base b in mm, insertion i in INSERT OCS mm, dimensionless S and radian theta:

```text
L = A * T(i) * Rz(theta) * S * T(-b)
Mchild = Mparent * Lchild       # Lchild.Then(Mparent)
qModel = SourceToModel * Mchild * q
```

Derivation: subtract base -> scale in block axes -> rotate in INSERT OCS -> translate in OCS -> convert that frame into parent WCS. Entity OCS conversion already happened in q. Do not rotate WCS source fields, double-convert insertion, subtract base after rotation or repeat unit scale.

Reconfirmed against actual Transform3 row/column code and the stored fields in [INSERT](https://help.autodesk.com/cloudhelp/2024/ENU/AutoCAD-DXF/files/GUID-28FA4CFB-9D5E-4880-9F11-36C97578252F.htm) / [BLOCK](https://help.autodesk.com/cloudhelp/2024/ENU/AutoCAD-DXF/files/GUID-66D32572-005A-4E23-8B8B-8726E8C14302.htm). These pages define facts/frames; the product composition is an explicit derivation, not an unexecuted runtime PASS.

Independent arithmetic: b=(10,20,0), q=(12,23,0), S=(2,3,1), R90°, i=(100,200,0), A=identity. q-b=(2,3,0), scaled=(4,9,0), rotated=(-9,4,0), translated=(91,204,0). With sx=-2 -> (-4,9,0) -> (-9,-4,0) -> (91,196,0). q=b maps to A*i. For N=+Y, AAA maps (x,y,z)->(-x,z,y), so first result becomes (-91,0,204); insertion maps (-100,0,200). Parent translation(10,0,0) then makes (-81,0,204), confirming left composition. Actual future tests must independently encode these values.

Keep local analytic circle/arc with accumulated affine transform under nonuniform/negative scale. Negative determinant preserves reflection/winding metadata; never rewrite source sweep to hide it. Exact zero scale rejects occurrence; nonzero small scale is not snapped. No historical ACadSharp helper/dependency.

### Exact transformed Circle/Arc bounds — M-02

For local XY circle radius R, transformed center C and 2D linear matrix [[a,b],[c,d]], exact half extents:

```text
Xhalf = R * hypot(a,b)
Yhalf = R * hypot(c,d)
min = C - half; max = C + half
```

General 3D/tilted-plane form: local orthonormal plane axes U,V, accumulated linear part B; u=B*U, v=B*V. Curve p(t)=C+R*(u*cos(t)+v*sin(t)). Full-circle half extent on coordinate j is R*hypot(u[j],v[j]); includes X,Y,Z and shear. Translation acts only on C. Use robust hypot/finite checks; no sampled/tessellated authoritative bounds.

For ARC, include start/end and each coordinate's derivative-zero parameters `atan2(v[j],u[j]) + k*π` that lie in its original signed parameter interval modulo2π, including wrap and endpoints under the central angular policy. Evaluate original affine p(t), not a world-circle approximation. Source sweep parameter stays authoritative under mirror. If u[j]=v[j]=0 that coordinate is constant; endpoints suffice. Bulge arcs may have signed sweep. This handles nonuniform scales and nested shear without fitting an ellipse.

Hand-calculated test oracle: local radius 2, center 0, B=R45°*S(2,3), translation C=(10,-4,3). Rows are (sqrt2,-3/sqrt2) and (sqrt2,3/sqrt2). Half extents both sqrt26. Full-circle X=[10-sqrt26,10+sqrt26], Y=[-4-sqrt26,-4+sqrt26], Z=3. Mirroring sx=-2 changes first column sign and winding, but these circle bounds stay identical.

For source arc t in [0,π/2] with positive scales: X endpoints yield [10-3*sqrt2,10+2*sqrt2]; Y minimum=-4+2*sqrt2, maximum=-4+sqrt26 at t=atan(3/2). X full-circle extrema are outside this sweep. This proves endpoint-only and whole-circle-extent shortcuts are both wrong. `NonuniformCurveBoundsTests` must cover this oracle, mirrored arcs, wrapping, tilted planes and ancestor shear.

### Numeric domains

Source validity (finite/required/legal counts), normalization precision (GeometryTolerance LengthMm/AngleRadians/MatrixCoefficientTolerance plus documented conditioning policy), and future recognition tolerance are separate. All comparison policy is explicit/injectable/validated, with units/profile recorded; no scattered guessed constants. Use existing dimensionless matrix-tolerance slot rather than duplicate it.

Robustly normalize arbitrarily small/large finite normals; exact zero/invalid facts reject. Small nonzero scales preserve forward placement, diagnose conditioning where an inverse is required; negative scales allowed. Geometry degeneracy is explicit ledger/diagnostic, not snapping/deletion. Arithmetic overflow refuses affected operation/branch; invalid global options refuse document. Serialization never rounds away wrong geometry. AAA 1/64 is specification, not configurable epsilon.

## Alternatives / consequences

Directly store mutable Transform3: rejected; constructor-copy with exposed internal array also rejected. Selected private ownership wrapper preserves existing algebra without changing legacy Geometry. Tessellated bounds or a single scaled radius: rejected as authoritative; analytic parameter extrema selected. Flat XY placement: rejected for general OCS; per-field frame mapping selected.

Additional wrapper/codec tests are necessary. No Revit units/types. Exact parametric representation avoids a new ellipse fitter and keeps provenance compact. Accepted ADR-006 remains intact for v1; this Proposed ADR extends CAD2 capabilities only after acceptance.

## Required future tests / migration gate

TransformImmutabilityTests, NonuniformCurveBoundsTests, unit/angle/default tests, axis/tilted/extreme-normal AAA, point/vector/elevation, nonzero-base/mirror/nested placement and finite/degenerate cases. Independently calculated goldens, not helper output as oracle. This gate implements none of them.

**ADR-019 must be Accepted before 04B, 04C or 04D**, and before coordinate-dependent 04E/04G implementation. 04B also requires Accepted ADR-018. No Geometry production change or acceptance is performed now.
