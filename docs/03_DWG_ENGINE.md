# DWG Engine

## 목적
DWG를 이미지가 아니라 원본 Vector/Object 데이터로 읽습니다.

## 기본 Entity
LINE, LWPOLYLINE, POLYLINE, ARC, CIRCLE, ELLIPSE, SPLINE, HATCH, TEXT, MTEXT, INSERT, ATTRIB, ATTDEF, DIMENSION, LEADER, MLEADER, TABLE.

## Reader Abstraction

```text
IDrawingReader
 ├─ RealDwgReader
 ├─ OdaReader
 ├─ AutoCadReader
 └─ DxfReader
```

필수 보존: handle, layer, color, linetype, lineweight, block ownership, transform, geometry, raw properties.
Unsupported Entity도 type/count를 남깁니다.
