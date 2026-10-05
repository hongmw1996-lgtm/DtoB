# DtoB Architecture

## High-Level Architecture

```text
Web UI
  ↓
Application API
  ├─ Drawing Core
  └─ Family Catalog
       ↓
      BIM IR
       ↓
  Revit Adapter
       ↓
      RVT
```

## Layer Responsibilities

### Input Adapter
DWG/DXF/PDF 등 입력별 구현.

### CAD IR
LINE, POLYLINE, ARC, CIRCLE, TEXT, MTEXT, INSERT, ATTRIB, DIMENSION, LAYER, BLOCK 등 CAD 의미 표현.

### Semantic Engine
CAD primitive를 건축 의미로 변환.

### BIM IR
Revit과 독립적인 건축 객체 모델.

### Revit Adapter
BIM IR을 Revit API 호출로 변환.

## 내부 단위

Core geometry 기본 단위는 millimeter.

Revit internal unit 변환은 Revit Adapter에서 수행합니다.

## Identity

```text
DWG Source Handle
→ CAD IR ID
→ BIM IR ID
→ Revit UniqueId / ElementId
```

## Revit Version Isolation

```text
revit/
  DtoB.Revit.Core/
  DtoB.Revit2025.Net8/
  DtoB.Revit2025_5.Net10/
```

실제 설치된 Revit 세부 빌드에 맞춰 Adapter를 선택합니다.
