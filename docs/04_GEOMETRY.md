# Geometry Core

## 기본 단위
millimeter

## 필수 기능

- Vector
- Point
- Segment
- Polyline
- Arc
- Polygon
- BoundingBox
- Transform
- Intersection
- Offset
- Projection
- Distance
- Parallel test
- Collinear test
- Spatial index

## Tolerance

Tolerance는 한 곳에서 관리합니다.

```text
GeometryTolerance
```

도면 유형/스케일/단위별 Profile 확장을 고려합니다.

## 테스트 필수 항목

- near-zero segment
- almost parallel
- overlapping line
- rotated block
- mirrored block
- nested block
- floating point boundary
