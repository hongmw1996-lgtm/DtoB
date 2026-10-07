# Product Definition

## 사용자 문제

기존 2D CAD 도면을 BIM으로 재작성하려면 반복적인 수작업이 많이 필요합니다.

BIMBraid는 이 작업을 자동화하되,
완전 자동화보다 **자동 분석 + 전문가 검수** 구조를 목표로 합니다.

## 기본 사용자 흐름

1. DWG 업로드
2. 자동 분석
3. 도면별/객체별 결과 확인
4. 잘못된 분류 수정
5. Revit Family Mapping 확인
6. Generate BIM
7. 결과 QA
8. RVT 저장

## 사용자에게 보여줘야 할 정보

각 객체:

- Category
- Level
- Geometry
- Type
- Confidence
- Evidence
- Source Handle
- Family Mapping
- Status

## 주요 상태

- Confirmed
- Suggested
- Needs Review
- Error
- Ignored
