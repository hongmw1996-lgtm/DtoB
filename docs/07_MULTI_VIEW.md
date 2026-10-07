# Multi-view Reasoning

## 목적

평면의 X/Y와
단면/입면의 Z 정보를 결합합니다.

## Anchor 우선순위

1. Grid
2. Level
3. Section Marker
4. Dimension
5. Column
6. Unique Geometry

## 예

Plan:
- Window W1
- X/Y
- Width

Elevation:
- W1
- Sill 900
- Height 1500

결과:
- X/Y from Plan
- Z/Sill/Height from Elevation

## Conflict

상충하는 정보는 임의 선택하지 않습니다.

```text
resolved
review_required
unresolved
```

상태로 관리합니다.
