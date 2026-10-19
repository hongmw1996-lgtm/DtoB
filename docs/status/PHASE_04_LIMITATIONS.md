# PHASE04 accepted limitations

- Only managed imported Text DXF V1 is authoritative. No upstream DWG handle,
  entity, anonymous-block or database-structure equality guarantee.
- Accepted INSUNITS subset: 1, 2, 4, 5, 6. Absent/0/unresolved units require an
  explicit selection; the concrete automatic normalizer refuses unresolved input.
- Ordinary XYZ completeness is required by the accepted coordinate profiles.
- Lineweight, transparency and color-book fidelity remain deferred.
- Polyline width is retained as source evidence, not materialized geometry.
- ATTRIB/ATTDEF instancing remains unsupported.
- TEXT/MTEXT glyph layout and exact extents are unavailable; bounds use factual
  anchors, not glyph boxes. No DIMENSION graphics synthesis.
- No recognition, drawing-region inference, BIM generation or Revit generation.
- The official document remains Partial. Phase03 Unsupported52 stays visible:
  VPORT 1, APPID 3, OBJECTS 48. No primitive success upgrades global coverage.
- LOW01: strict Phase03 unresolved-reference policy remains; some sources cannot
  publish Raw even when a non-owner pointer could be ignored by another reader.
- Development resource limits are bounded policy, not throughput guarantees:
  depth32, cells/INSERT10000, global occurrences100000, materialized entries250000,
  diagnostics5000 (one slot reserved for the terminal refusal).
- Cancellation scheduling/progress counts are not canonical golden data.
- Normalization is a synchronous, deterministic library; the caller owns scheduling.
- Whole-PHASE04 independent Antigravity review remains pending after the local gate.
