# DtoB Master Plan

## 1. 비전

DtoB의 목표는 단순히 2D 선을 3D 형상으로 Extrude하는 것이 아닙니다.

도면이 표현하는 **건축 의도**를 해석하여 다음 BIM 객체로 재구성합니다.

- Level
- Grid
- Wall
- Floor
- Column
- Beam
- Door
- Window
- Room
- Hosted Family
- Parameters
- Relationships

## 2. 제품 핵심 기능

1. DWG 업로드
2. DWG 내부 객체 분석
3. 레이어/블록/텍스트/치수/도면 종류 해석
4. 분석 결과 시각화
5. 평면/단면/입면 복합 정합
6. BIM IR 생성
7. Revit Family Catalog 조회
8. Family/Type 자동 매칭
9. 사용자 수동 수정
10. Revit Native BIM 생성
11. 결과 검증 및 오류 보고
12. 수정 이력과 학습 데이터 축적

## 3. 기술 원칙

```text
Input Adapter
→ CAD IR
→ Semantic Engine
→ BIM IR
→ Output Adapter
```

Revit은 출력 Adapter 중 하나로 취급합니다.

향후:

```text
BIM IR
├─ Revit
├─ IFC
├─ Speckle
├─ glTF
├─ Web Viewer
└─ Knowledge Graph
```

## 4. 개발 단계

- PHASE 00: Foundation
- PHASE 01: DWG Inspector
- PHASE 02: CAD Normalization
- PHASE 03: Drawing Region / View Classification
- PHASE 04: Grid Recognition
- PHASE 05: Wall Recognition
- PHASE 06: Column / Door / Window Recognition
- PHASE 07: Room / Floor Recognition
- PHASE 08: Section / Elevation Intelligence
- PHASE 09: Multi-view Resolver
- PHASE 10: BIM IR & Constraint Graph
- PHASE 11: Review Workbench
- PHASE 12: Revit Family Catalog Scanner
- PHASE 13: Family Matching Engine
- PHASE 14: Revit Generator MVP
- PHASE 15: Validation / Golden Test
- PHASE 16: AI Enhancement / Learning from Corrections
- PHASE 17: Production / Cloud / Expansion

## 5. 개발 순서 원칙

다음이 끝나기 전에 AI 블랙박스 구조부터 만들지 않습니다.

```text
DWG Parsing
→ Geometry Normalization
→ Deterministic Semantic Rules
→ Review UI
→ Ground Truth
```

## 6. 첫 Vertical Slice

```text
단순 DWG
→ 벽 4개
→ 문 1개
→ 창 1개
→ Review
→ Family Mapping
→ Revit 생성
```

## 7. 장기 비전

```text
DWG → BIM
PDF → BIM
Image → BIM
Text → BIM
BIM → Knowledge Graph
Knowledge Graph → BIM Agent
```

DtoB의 최종 목표는 AEC Modeling Engine입니다.
