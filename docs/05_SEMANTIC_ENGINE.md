# Semantic Engine

## 원칙

단일 신호만으로 건축 객체를 확정하지 않습니다.

예:

```text
Layer
+ Geometry
+ Block
+ Text
+ Dimension
+ Context
+ Cross-view Evidence
```

## Wall Evidence 예

- 평행선
- 적정 두께
- A-WALL 계열 Layer
- Room boundary 기여
- Door/Window gap
- 단면에서 대응

## Door Evidence 예

- Swing arc
- Leaf
- Wall gap
- Block name
- Text mark
- Width
- Host wall

## 결과

모든 Candidate에:

- confidence
- evidence[]
- conflicts[]
- status

를 저장합니다.
