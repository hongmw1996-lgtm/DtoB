# PHASE 12 — Revit Add-in Foundation

## 1. 목적

Revit에 DtoB Connector를 설치하고 Desktop과 실제 Revit Document를 안전하게 연결한다.

## 2. 전제조건 / 가정

- PHASE 00 IPC 설계
- 정확한 Revit build 확인

## 3. 이번 Phase 지원 범위

- Ribbon
- Connect
- Document metadata
- IPC
- Revit valid API context
- disconnect/error handling

## 4. 아직 지원하지 않는 범위

- Family Catalog 전체
- BIM 생성

## 5. Subphase 상세

### 12A Add-in Load

Revit 시작 시 DtoB Add-in이 정상 로드된다.

### 12B Ribbon

DtoB > Connect / Scan Catalog / Generate BIM 버튼을 만든다. 후속 버튼은 placeholder 가능.

### 12C Document Context

Revit Version/Document Title/Path/Units를 읽는다.

### 12D Connection

Desktop에서 Connected/Document 정보를 표시한다.

### 12E API Thread Safety

IPC thread가 Document를 직접 만지지 않고 ExternalEvent 등 안전한 실행 경로를 사용한다.

### 12F Error

Revit not running/no document/multiple session/version mismatch를 처리한다.

## 6. 사용자에게 보이는 결과

DtoB Desktop에 Connected to Revit / Document 정보가 보인다.

## 7. 필수 테스트

- add-in load
- ribbon
- connect/disconnect
- document metadata
- Revit restart
- invalid context 호출 방지

## 8. Exit Criteria

- [ ] Add-in 설치/로드
- [ ] Ribbon
- [ ] Desktop 연결
- [ ] Document metadata
- [ ] context-safe command pipeline

## 9. Codex 실행 지시

1. `DtoB_FINAL_MASTER_PLAN.md`, `AGENTS.md`, `ARCHITECTURE.md`를 먼저 읽는다.
2. 이 문서의 PHASE 12 범위만 구현한다.
3. 구현 전 Repository 상태와 기존 테스트를 조사하고 작업 Checklist를 작성한다.
4. Subphase 순서대로 작은 단위로 구현한다.
5. 각 Subphase마다 테스트를 추가한다.
6. UI에서 확인 가능한 결과는 같은 Phase에서 최소 검증 UI를 제공한다.
7. Unsupported Case를 조용히 무시하지 말고 명시적으로 기록한다.
8. 전체 테스트 실행 후 `docs/status/PHASE_12_REPORT.md`를 작성한다.
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

Review 결과는 `reviews/PHASE_12_REVIEW.md`에 작성한다.

Severity: `BLOCKER / HIGH / MEDIUM / LOW`

## 11. Phase Gate

```text
PASS
PASS_WITH_KNOWN_LIMITATIONS
REVIEW_REQUIRED
BLOCKED
```

다음 Phase는 PASS 또는 합의된 PASS_WITH_KNOWN_LIMITATIONS 이후 시작한다.
