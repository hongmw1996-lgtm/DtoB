# Text & Layer Intelligence

## DWG Text

OCR보다 먼저:

- TEXT
- MTEXT
- ATTRIB
- DIMENSION text
- MLEADER text
- TABLE cell

을 직접 읽습니다.

## Text Semantic

예:

- `1F` → Level
- `FL +4200` → Elevation
- `W1` → Window Mark
- `D03` → Door Mark
- `SLAB THK 150` → Slab Thickness
- `CH=2400` → Ceiling Height

## Layer Mapping

회사별 Profile을 지원합니다.

```yaml
wall:
  - A-WALL
  - WALL
  - 건축_벽체
door:
  - A-DOOR
  - DOOR
```

사용자가 수정한 매핑은 Company/Profile 지식으로 저장합니다.
