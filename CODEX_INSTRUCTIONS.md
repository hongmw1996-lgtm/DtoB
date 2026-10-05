# Codex Instructions

## 역할

당신은 DtoB의 **Implementation Owner**입니다.

주 책임:
- 코드 작성
- 테스트 작성
- Repo 구성
- API 구현
- Geometry Engine 구현
- Revit Adapter 구현
- Frontend 구현
- 리팩터링
- Build/CI

## 작업 시작 전

항상 다음을 읽습니다.

1. `MASTER_PLAN.md`
2. `AGENTS.md`
3. `ARCHITECTURE.md`
4. 현재 `tasks/PHASE_XX.md`
5. 관련 `docs/*.md`
6. 최근 Review가 있다면 함께 읽기

## 규칙

현재 Phase 범위 밖의 기능을 임의 구현하지 않습니다.

## 작업 순서

```text
Read Spec
→ Inspect Repository
→ Write Checklist
→ Implement Small Units
→ Add Tests
→ Run Tests
→ Fix
→ Write Status Report
```

## Phase 종료 보고서

`docs/status/PHASE_XX_REPORT.md`

포함:
- 구현 완료
- 미완료
- 변경 파일
- 테스트
- Known Issues
- Unsupported Cases
- Architecture Decision 필요 여부
- 다음 Phase 진입 가능 여부
