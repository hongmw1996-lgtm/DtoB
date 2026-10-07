# BIMBraid Agent Rules

Codex와 Antigravity 모두 이 규칙을 준수합니다.

## 공통 규칙

1. CAD Parser를 Revit API와 직접 결합하지 않는다.
2. BIM IR을 도메인 Source of Truth로 사용한다.
3. 모든 Semantic Prediction은 `confidence`와 `evidence`를 가진다.
4. Geometry 계산은 deterministic code가 담당한다.
5. LLM 출력이 직접 Revit Element를 생성하게 하지 않는다.
6. DWG의 원본 `handle` 또는 equivalent source identity를 보존한다.
7. Coordinate Transform에는 반드시 테스트를 둔다.
8. Unsupported Entity를 조용히 버리지 않는다.
9. 지원하지 않는 경우 명시적으로 로그와 이슈를 남긴다.
10. 새 기능에는 최소 1개의 fixture 또는 regression test가 필요하다.
11. Revit Transaction은 기능 그룹별로 분리한다.
12. 단위 변환은 Adapter 경계에서 명시적으로 수행한다.
13. 임의의 tolerance hard coding을 금지한다.
14. Schema major change는 ADR 없이 수행하지 않는다.
15. 프로젝트 전체를 한 번에 리팩터링하지 않는다.

## 변경 전 ADR가 필요한 항목

- BIM IR major schema
- 좌표계 convention
- 내부 geometry unit
- Stable ID 정책
- Revit output semantics
- Database major migration
- DWG SDK 교체
- AI provider 교체

## 각 작업 종료 시

Codex:
- 구현 내용
- 테스트
- 알려진 제한
- 실패 사례
- 다음 단계

를 `docs/status/`에 기록합니다.

Antigravity:
- BLOCKER
- HIGH
- MEDIUM
- LOW

심각도로 Review를 `reviews/`에 기록합니다.
