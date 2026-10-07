# DtoB

## DWG → Revit BIM Automation Platform

DtoB는 2D DWG 도면의 선, 호, 폴리라인, 블록, 문자, 치수, 레이어, 평면/단면/입면 관계를 해석하여
검수 가능한 BIM 중간 데이터(BIM IR)를 생성하고, 이를 Revit Family/Type과 매칭하여 Native Revit 객체로 구축하는 프로젝트입니다.

## 최종 흐름

```text
DWG Upload
→ CAD Parsing
→ CAD IR
→ Drawing/View Classification
→ Semantic Recognition
→ Plan/Section/Elevation Alignment
→ BIM IR
→ Human Review
→ Revit Family Mapping
→ Revit Native Element Generation
→ RVT
```

## 가장 중요한 원칙

1. DWG를 Revit API에 바로 연결하지 않는다.
2. `CAD IR → BIM IR → Revit Adapter` 구조를 유지한다.
3. Geometry 계산은 deterministic code로 처리한다.
4. AI 판단에는 반드시 `confidence + evidence`를 남긴다.
5. 사용자가 애매한 결과를 수정할 수 있어야 한다.
6. Revit 결과물은 가능한 한 Native BIM 객체여야 한다.
7. Codex는 구현 담당, Antigravity는 검토 담당으로 역할을 분리한다.

## 시작 순서

1. `DtoB_FINAL_MASTER_PLAN.md`
2. `AGENTS.md`
3. `ARCHITECTURE.md`
4. `CODEX_INSTRUCTIONS.md`
5. `ANTIGRAVITY_INSTRUCTIONS.md`
6. `tasks/PHASE_00.md`
7. 이후 단계별 Phase 문서

## MVP

첫 번째 MVP는 다음 범위로 제한합니다.

- 입력: 건축 평면 DWG + 필요 시 단면/입면
- 객체: Grid, Level, Wall, Column, Door, Window, Floor, Room
- 출력: Revit Native Element 기반 RVT
- 필수: Review UI, Family Mapping, Traceability, Failure Report

처음부터 모든 도면 유형과 MEP/철골/배근까지 확장하지 않습니다.

## PHASE 00 foundation

DtoB Desktop + Revit Add-in technical verification only. DWG SDK selection is deferred; no parsing, recognition, viewer or BIM generation is implemented. See [build and host verification](docs/PHASE_00_BUILD.md) and [PHASE 00 report](docs/status/PHASE_00_REPORT.md).
