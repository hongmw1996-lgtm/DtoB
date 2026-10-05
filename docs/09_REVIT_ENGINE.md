# Revit Engine

생성 순서:
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
11. FamilyInstances
12. Rooms
13. Parameters
14. Validation
15. Save

Builders: LevelBuilder, GridBuilder, WallBuilder, ColumnBuilder, FloorBuilder, DoorBuilder, WindowBuilder, RoomBuilder, ParameterWriter.

Trace parameters:
- DTB_SourceDrawing
- DTB_SourceHandle
- DTB_BimIrId
- DTB_Confidence
- DTB_GeneratedAt
