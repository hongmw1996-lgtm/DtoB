# Geometry Core

기본 단위: millimeter.

필수 기능:
- Vector / Point / Segment / Polyline / Arc / Polygon
- BoundingBox / Transform
- Intersection / Offset / Projection / Distance
- Parallel / Collinear test
- Spatial index

Tolerance는 중앙 `GeometryTolerance`에서 관리합니다.

필수 테스트:
near-zero, almost parallel, overlap, rotated block, mirrored block, nested block, floating point boundary.
