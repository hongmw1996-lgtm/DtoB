# Revit Engine

## 생성 순서

1. Template
2. Units/Coordinates
3. Levels
4. Grids
5. Walls
6. Columns
7. Floors
8. Beams
9. Doors
10. Windows
11. Other FamilyInstances
12. Rooms
13. Parameters
14. Validation
15. Save

## Builder

- LevelBuilder
- GridBuilder
- WallBuilder
- ColumnBuilder
- FloorBuilder
- DoorBuilder
- WindowBuilder
- RoomBuilder
- ParameterWriter

## Transaction

큰 단일 Transaction을 피하고
기능 그룹별 Transaction으로 분리합니다.

## Trace Parameter

가능하면:

- BB_SourceDrawing
- BB_SourceHandle
- BB_BimIrId
- BB_Confidence
- BB_GeneratedAt
