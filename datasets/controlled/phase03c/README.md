# PHASE 03C hand-authored Text DXF fixtures

UTF-8/no BOM/LF, explicit AC1032 HEADER and ENTITIES/ENDSEC/EOF. Synthetic source files, not building drawing truth or upstream DWG equivalence. Pair contents and expected facts below were inspected directly, not generated from mapper outputs. All fixtures pass through actual03A/03B/03C readers in tests.

| Source | Expected factual behavior |
|---|---|
| circle-reviewed | Explicit AcDbCircle subclass. Center(-1.5,2.25,-3.5), radius2.5, thickness0.125, normal(0.1,0.2,1.0). Center X pair12/value line26; radius pair15/line32. Separate CIRCLE acceptance gate. |
| lwpolyline-full | Two vertices(-1,2),(5,6); flags1 without closing point; elevation3.5; constant width12.5 retained with vertex widths3/4, bulge-0.5, identifier17; second optional vertex fields absent; thickness0 and normal(1,2,3) unnormalized. |
| lwpolyline-count-mismatch | Declares2 but one source X/Y vertex; Invalid/VertexCountMismatch, no repair. |
| lwpolyline-missing-y | One X without Y; located missing fact/Invalid. |
| lwpolyline-invalid-order | Width before Y in X-delimited vertex; InvalidVertexOrder, evidence retained. |
| line-native-3d | HEADER INSUNITS1; coordinates(1,2,3)->(4,5,6), thickness7, normal(0.2,0.3,0.4); no source scaling. |
| line-optional-z | Raw-0.0/1e+20 spelling, Z/thickness/normal absent; no defaults. |
| line-zero-length | Both endpoints(1,2), retained as Supported raw LINE; no cleanup. |
| line-missing-y | Missing required Start.Y; absent/invalid, never zero. |
| line-nonfinite | NaN X, raw retained/numeric invalid, no typed zero. |
| line-duplicate-x | Two singleton X values1/2, both source indices retained/Invalid, no winner. |
| solid-degenerate | All four corner slots(1,2), retained separately; no collapse/reorder/mesh. |
| arc-native-degrees | Center(1,2), radius3, angles720/-90 degrees unchanged; no wrap/radians. |
| point-angle | Position(4,5), angle450 source degrees, Z absent; no rendering. |
| unsupported-ellipse | All pairs retained Unsupported; no approximation. |
| legacy-gated | POLYLINE flags64 + VERTEX + SEQEND; typed legacy mapping not implemented; sequence/subtype gate explicit. |

Embedded source fixtures additionally cover invalid radius, width/bulge duplicates, controls/XDATA/subclasses, extreme numbers, optional absences and injected resource/cancellation cases. No03D/global identity/snapshot/Viewer/normalization coverage claim.
