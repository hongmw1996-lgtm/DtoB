# PHASE 04 — CAD Data Engine

## 1. 목적

DWG의 원본 CAD 정보를 손실 없이 구조화하여 이후 인식 엔진이 사용할 CAD IR을 만든다.

## 2. 전제조건 / 가정

- Controlled CAD
- 명확한 Layer/Color/Block
- OCR 불필요

## 3. 이번 Phase 지원 범위

- Primitive/Annotation extraction
- Layer/Color
- Block/INSERT
- Effective metadata
- Coordinate normalize
- 기초 cleanup
- Spatial index
- CAD IR JSON

## 4. 아직 지원하지 않는 범위

- AI 객체 의미 추론
- 복잡한 Dynamic Block
- Xref resolve
- 고급 CAD 복구

## 5. Subphase 상세

### 04A Primitive Extraction

LINE/LWPOLYLINE/POLYLINE/ARC/CIRCLE/ELLIPSE/SPLINE/HATCH를 id, handle, layer, color, geometry와 함께 읽는다.

### 04B Annotation Extraction

TEXT/MTEXT/DIMENSION/LEADER/MLEADER/TABLE을 native CAD 데이터로 읽는다.

### 04C Layer & Color

CadLayer에 Name/Color/Linetype/Lineweight를 저장하고 Entity raw/effective color 확장 여지를 둔다.

### 04D Block Engine

Block Definition/INSERT/position/rotation/scale/attribute를 읽고 테스트 Block 3종을 정확히 복원한다.

### 04E Effective Metadata

raw_layer, insert_layer, owner_block, root_insert를 보존해 미래 Layer 0/nested block에 대비한다.

### 04F Coordinate Normalization

내부 mm 좌표로 정규화하고 insertion/rotation/scale transform을 검증한다.

### 04G Cleanup

zero-length와 exact duplicate를 탐지하고 자동 수정은 최소화한다.

### 04H Spatial Index

bbox/point/line 근접 질의와 layer+region 조건 검색을 지원한다.

### 04I CAD IR Serialization

분석 결과를 JSON으로 내보내 Debug/Regression에 사용한다.

## 6. 사용자에게 보이는 결과

Analysis Inspector에 Entities/Layers/Blocks/Texts/Dimensions/Unsupported 개수와 Layer별 count를 표시한다.

## 7. 필수 테스트

- line/polyline/arc/circle 좌표
- layer/color/handle
- block insertion/rotation
- text/dimension
- JSON round-trip
- spatial query

## 8. Exit Criteria

- [ ] Controlled test의 주요 Entity 전부 읽기
- [ ] Layer/Color/Handle 손실 없음
- [ ] 3개 Block 읽기
- [ ] Text/Dimension 읽기
- [ ] CAD IR 생성
- [ ] Viewer ID와 연결
- [ ] Unsupported 보고

## 9. Codex 실행 지시

1. `DtoB_FINAL_MASTER_PLAN.md`, `AGENTS.md`, `ARCHITECTURE.md`를 먼저 읽는다.
2. 이 문서의 PHASE 04 범위만 구현한다.
3. 구현 전 Repository 상태와 기존 테스트를 조사하고 작업 Checklist를 작성한다.
4. Subphase 순서대로 작은 단위로 구현한다.
5. 각 Subphase마다 테스트를 추가한다.
6. UI에서 확인 가능한 결과는 같은 Phase에서 최소 검증 UI를 제공한다.
7. Unsupported Case를 조용히 무시하지 말고 명시적으로 기록한다.
8. 전체 테스트 실행 후 `docs/status/PHASE_04_REPORT.md`를 작성한다.
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

Review 결과는 `reviews/PHASE_04_REVIEW.md`에 작성한다.

Severity: `BLOCKER / HIGH / MEDIUM / LOW`

## 11. Phase Gate

```text
PASS
PASS_WITH_KNOWN_LIMITATIONS
REVIEW_REQUIRED
BLOCKED
```

다음 Phase는 PASS 또는 합의된 PASS_WITH_KNOWN_LIMITATIONS 이후 시작한다.
