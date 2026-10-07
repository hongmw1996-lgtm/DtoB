# PHASE 02 — DWG Upload & Drawing Management

## 1. 목적

DtoB 프로젝트에 실제 CAD 파일을 등록하고 파일 단위로 관리한다.

## 2. 전제조건 / 가정

- Controlled DWG/DXF
- 초기 Unit은 mm
- Xref 자동 수집 제외

## 3. 이번 Phase 지원 범위

- File Dialog
- Drag&Drop
- Drawing metadata
- Hash/중복 감지
- 파일 validation
- Drawing list
- 교체/삭제
- revision-ready identity

## 4. 아직 지원하지 않는 범위

- CAD 내용의 의미 분석
- Viewer rendering
- Xref dependency 자동 해석

## 5. Subphase 상세

### 02A Upload UI

Add Drawing 버튼과 Drag&Drop을 구현한다.

### 02B Metadata

DrawingId, OriginalFileName, LocalPath, FileSize, Hash, Extension, ImportedAt, Version, Unit을 저장한다.

### 02C Validation

존재/읽기 가능/확장자/중복/손상/Unit 확인을 수행한다.

### 02D Drawing List

Project Tree에 Drawing을 표시하고 remove/replace/reveal 기능을 둔다.

### 02E Revision Identity

DrawingId와 RevisionId/Hash를 분리해 향후 버전 비교가 가능하도록 한다.

## 6. 사용자에게 보이는 결과

Project > Drawings 아래에 DtoB_TEST_2STORY.dwg가 Status: Ready로 보인다.

## 7. 필수 테스트

- 정상 업로드
- 중복 업로드
- 잘못된 확장자
- Unicode 파일명
- 원본 파일 삭제 후 reopen
- replace

## 8. Exit Criteria

- [ ] DWG/DXF 추가
- [ ] Drawing metadata 저장
- [ ] 중복 감지
- [ ] 삭제/교체
- [ ] 오류 상태 UI

## 9. Codex 실행 지시

1. `DtoB_FINAL_MASTER_PLAN.md`, `AGENTS.md`, `ARCHITECTURE.md`를 먼저 읽는다.
2. 이 문서의 PHASE 02 범위만 구현한다.
3. 구현 전 Repository 상태와 기존 테스트를 조사하고 작업 Checklist를 작성한다.
4. Subphase 순서대로 작은 단위로 구현한다.
5. 각 Subphase마다 테스트를 추가한다.
6. UI에서 확인 가능한 결과는 같은 Phase에서 최소 검증 UI를 제공한다.
7. Unsupported Case를 조용히 무시하지 말고 명시적으로 기록한다.
8. 전체 테스트 실행 후 `docs/status/PHASE_02_REPORT.md`를 작성한다.
9. 다음 Phase를 임의로 시작하지 않는다.

## 10. Antigravity Review Focus

- Phase 범위 위반
- CAD Core와 Revit API 경계
- Unit / Coordinate hidden assumption
- Data loss / source trace 손실
- Silent failure
- Test 누락
- Regression 위험
- UI와 Core Logic의 과도한 결합
- 다음 Phase 확장을 방해하는 구조

Review 결과는 `reviews/PHASE_02_REVIEW.md`에 작성한다.

Severity: `BLOCKER / HIGH / MEDIUM / LOW`

## 11. Phase Gate

```text
PASS
PASS_WITH_KNOWN_LIMITATIONS
REVIEW_REQUIRED
BLOCKED
```

다음 Phase는 PASS 또는 합의된 PASS_WITH_KNOWN_LIMITATIONS 이후 시작한다.
