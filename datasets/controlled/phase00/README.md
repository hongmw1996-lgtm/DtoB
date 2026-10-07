# Synthetic PHASE 00 fixtures

cad.json covers basic primitives, definitions/INSERT, text, dimension and unsupported raw data. nested-mirrored.json adds nested and mirrored INSERT occurrences, a definition-frame Arc and an unsupported -Z extrusion normal with a source-linked diagnostic. bim.json contains eight typed BIM categories, prediction evidence and explicit manual confirmations.

These are hand-authored contract fixtures, not real DWG output or recognition results. Core geometry is already mm; original source unit and conversion scale are metadata. Unitless/unknown units are rejected explicitly. $kind/$category precede data properties in IR v1, and unknown fields are forbidden.

Call ValidateStructure() after deserialization. Call ValidateGeometry(tolerance) before relying on geometric validity. CAD needs MatrixCoefficientTolerance explicitly; Floor/Room closure and near-zero segments need LengthMm. No reader SDK or recognition tolerance is selected here.
