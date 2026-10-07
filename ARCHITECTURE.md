# BIMBraid Architecture

## 1. High-Level Architecture

```text
┌─────────────────────────────┐
│          Web UI             │
│ Upload / Review / Mapping   │
└─────────────┬───────────────┘
              │
              ▼
┌─────────────────────────────┐
│       Application API       │
└───────┬───────────┬─────────┘
        │           │
        ▼           ▼
┌──────────────┐  ┌──────────────┐
│ Drawing Core │  │ Family Catalog│
└──────┬───────┘  └───────┬──────┘
       │                  │
       └─────────┬────────┘
                 ▼
          ┌────────────┐
          │   BIM IR   │
          └─────┬──────┘
                ▼
        ┌────────────────┐
        │ Revit Adapter  │
        └───────┬────────┘
                ▼
              RVT
```

## 2. Layer Responsibilities

### Input Adapter
- DWG/DXF/PDF 등 입력별 구현
- 원본 정보를 최대한 보존

### CAD IR
- LINE
- POLYLINE
- ARC
- CIRCLE
- TEXT
- MTEXT
- INSERT
- ATTRIB
- DIMENSION
- LAYER
- BLOCK

등 CAD 의미를 표현

### Semantic Engine
CAD primitive를 건축 의미로 변환

```text
Parallel Lines → WallCandidate
Swing Arc + Gap → DoorCandidate
Closed Rectangle + Grid → ColumnCandidate
```

### BIM IR
Revit과 독립적인 건축 객체 모델

### Revit Adapter
BIM IR을 Revit API 호출로 변환

## 3. 내부 단위

Core geometry 기본 단위:

```text
millimeter
```

Revit internal unit으로의 변환은 Revit Adapter에서 수행합니다.

## 4. Identity

```text
DWG Source Handle
→ CAD IR ID
→ BIM IR ID
→ Revit UniqueId / ElementId
```

추적 가능성을 유지합니다.

## 5. Revit Version Isolation

Revit 세부 빌드별 .NET/API 차이를 Core에 노출하지 않습니다.

```text
revit/
  BIMBraid.Revit.Core/
  BIMBraid.Revit2025.Net8/
  BIMBraid.Revit2025_5.Net10/
```

실제 설치된 Revit 빌드에 맞춰 Adapter 프로젝트를 선택합니다.
