# PHASE 04 — WHOLE-PHASE FINAL INDEPENDENT REVIEW V2 (`04A` ~ `04H`)
## REVIEW-INTEGRITY CORRECTION & AUTHORITATIVE WHOLE-PHASE RELEASE GATE

---

## 1. Verdict

**`PHASE_04_FINAL_PASS_WITH_KNOWN_LIMITATIONS`**

---

## 2. Why V2 Was Required

이전 1차 최종 리뷰 문서인 [`reviews/PHASE_04_FINAL_REVIEW.md`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Review/reviews/PHASE_04_FINAL_REVIEW.md)는 컨텍스트 요약(`CONTEXT_SUMMARY`) 직후 실제 저장소 파일과 증거 아티팩트를 재대조하지 않은 채 작성되어, 이미 승인된 `04G` 독립 구현 리뷰([`reviews/PHASE_04G_IMPLEMENTATION_REVIEW.md`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/reviews/PHASE_04G_IMPLEMENTATION_REVIEW.md)) 및 `04H` 릴리스 게이트 보고서([`docs/status/PHASE_04H_REPORT.md`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/docs/status/PHASE_04H_REPORT.md), [`docs/status/PHASE_04_FINAL_GATE.md`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/docs/status/PHASE_04_FINAL_GATE.md))와 충돌하는 다수의 사실적 오류(`REVIEW_V1_ERROR`)를 포함하고 있었다.

구체적으로 V1 리뷰는:
1. 실제 존재하지 않는 프로덕션 클래스/파일명(`CadSnapshotNormalizer.cs`, `CadDocumentAssembler.cs`, `CadCanonicalGeometryBuilder.cs` 등)을 기재했고,
2. 실제 승인된 `04A`~`04F` 다이제스트 대신 근거 없는 해시(`DBDA...`, `8416...`, `B654...`, `9DBC...`, `5A75...`, `0395...`, `99D1...`)를 기재했으며,
3. 기하/주석 앵커로부터 계산된 모델 경계 `(-1300.225, -10100, 0)..(20700, 8500, 0)`를 원시 DXF 헤더 `$EXTMIN`/`$EXTMAX`(실제 값은 `(1e+20, 1e+20, 1e+20)` / `(-1e+20, -1e+20, -1e+20)`)와 혼동했고,
4. `04G`의 비등방 스케일/전단 `Circle`/`Arc` 처리를 실제 아키텍처(공유 정의 로컬 `CadCircle`/`CadArc` + 아핀 `CadBlockOccurrence` 변환 + `CadBoundsQuery` 해석적 극값 질의) 대신 `GeometryId = null` 거부 정책으로 잘못 서술했으며,
5. 공식 통제 문서의 `Geometry`(`149`개), `Styles`(`201`개), `Annotations`(`44`개) 내부 구성과 `MaxDiagnostics`(`5,000`), 전체 솔루션 빌드 경고 상태(`MSB3277` 허용 목록 3건), `04A` 역사적 테스트 수(`844`개)를 부정확하게 기술했다.

이에 따라 [`reviews/PHASE_04_FINAL_REVIEW.md`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Review/reviews/PHASE_04_FINAL_REVIEW.md)는 실패한 1차 리뷰의 역사적 증거로 무수정 보존(SHA-256 `7FE0479ADA589178BCEF3F28BA00E4E879DCCCD58CC2F213AC5C869ECB820CDC`)하고, 실제 저장소 파일시스템·Git 상태·원시 DXF 바이트·정준 JSON·전체 Release 리빌드 및 1,402개 회귀 실행에 100% 기반한 본 **V2 리뷰(`reviews/PHASE_04_FINAL_REVIEW_V2.md`)**가 V1을 공식 대체(supersede)한다.

---

## 3. Repository Identity

리뷰 수행 전 실제 Git 명령으로 확인한 저장소 정체성은 다음과 같다:

- **`git rev-parse --show-toplevel`**: `D:/##홍민우/02. 프로그램/09. Codex/##DtoB/DtoB_Repo`
- **`git branch --show-current`**: `phase/04-cad-normalization`
- **`git rev-parse HEAD`**: `76ff6e0d8ccac797ba60d8c5657922c796a0e8c9` (`v0.4-phase03` 릴리스 베이스 커밋; PHASE 04 작업은 지침에 따라 의도적으로 미스테이징/미커밋 상태로 유지됨)
- **`git status --porcelain=v2` 추적 파일 변경(Tracked Modified, 5개, 모두 Unstaged `1 .M`)**:
  1. `DtoB.sln` (`72c050fbbc83758df407a361971dd42fb8ba6e2d`)
  2. `tasks/PHASE_04.md` (`cf91f4450532fa9e190801c772a207971b5d7dae`)
  3. `tests/DtoB.Tests/DtoB.Tests.csproj` (`59848297e6d976ce188646453521192261ff98b8`)
  4. `tests/DtoB.Tests/ExtractionQaCampaignTests.cs` (`ac522cfd6e1e530671e83e6d418d52b7ca5ae1a8`)
  5. `tests/DtoB.Tests/FoundationDocumentTests.cs` (`e637a60136518d0f21d376d346902bfa8415961a`)
- **Staged Files**: `0`개

---

## 4. Actual Production File Inventory

실제 파일시스템에서 확인한 PHASE 04 프로덕션 C# 파일 전체 목록(총 34개 신규 파일; 기존 `packages/DtoB.Cad/CadContracts.cs`는 무수정)은 다음과 같다:

### 4.1 `packages/DtoB.Cad/V2/` (9개 파일 — `04A` 동결 계약)
1. [`packages/DtoB.Cad/V2/CadContract.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad/V2/CadContract.cs)
2. [`packages/DtoB.Cad/V2/CadDocument.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad/V2/CadDocument.cs)
3. [`packages/DtoB.Cad/V2/CadGeometryContracts.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad/V2/CadGeometryContracts.cs)
4. [`packages/DtoB.Cad/V2/CadIdentity.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad/V2/CadIdentity.cs)
5. [`packages/DtoB.Cad/V2/CadIrJson.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad/V2/CadIrJson.cs)
6. [`packages/DtoB.Cad/V2/CadNumericPolicy.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad/V2/CadNumericPolicy.cs)
7. [`packages/DtoB.Cad/V2/CadProvenance.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad/V2/CadProvenance.cs)
8. [`packages/DtoB.Cad/V2/CadStyleContracts.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad/V2/CadStyleContracts.cs)
9. [`packages/DtoB.Cad/V2/CadTransform.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad/V2/CadTransform.cs)

### 4.2 `packages/DtoB.Cad.Normalize/` (25개 C# 파일 + `.csproj`)
- **Boundary (`04A`)**:
  10. [`packages/DtoB.Cad.Normalize/NormalizationBoundary.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/NormalizationBoundary.cs) (`ICadNormalizer`, `CadNormalizationResult`, `CadNormalizationLimits`, `CadNormalizationOptions`)
- **Scalars (`04B`)**:
  11. [`packages/DtoB.Cad.Normalize/Scalars/CadAngleNormalizer.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Scalars/CadAngleNormalizer.cs)
  12. [`packages/DtoB.Cad.Normalize/Scalars/CadScalarLengthNormalizer.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Scalars/CadScalarLengthNormalizer.cs)
  13. [`packages/DtoB.Cad.Normalize/Scalars/CadUnitNormalizer.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Scalars/CadUnitNormalizer.cs)
  14. [`packages/DtoB.Cad.Normalize/Scalars/RawScalarAccess.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Scalars/RawScalarAccess.cs)
  15. [`packages/DtoB.Cad.Normalize/Scalars/ScalarContracts.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Scalars/ScalarContracts.cs)
- **Coordinates (`04C`)**:
  16. [`packages/DtoB.Cad.Normalize/Coordinates/CadArbitraryAxis.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Coordinates/CadArbitraryAxis.cs)
  17. [`packages/DtoB.Cad.Normalize/Coordinates/CadCoordinateNormalizer.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Coordinates/CadCoordinateNormalizer.cs)
  18. [`packages/DtoB.Cad.Normalize/Coordinates/CoordinateContracts.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Coordinates/CoordinateContracts.cs)
  19. [`packages/DtoB.Cad.Normalize/Coordinates/RawCoordinateAccess.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Coordinates/RawCoordinateAccess.cs)
- **Placement (`04D`)**:
  20. [`packages/DtoB.Cad.Normalize/Placement/CadBlockBaseNormalizer.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Placement/CadBlockBaseNormalizer.cs)
  21. [`packages/DtoB.Cad.Normalize/Placement/CadInsertLocalTransform.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Placement/CadInsertLocalTransform.cs)
  22. [`packages/DtoB.Cad.Normalize/Placement/CadInsertScaleNormalizer.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Placement/CadInsertScaleNormalizer.cs)
  23. [`packages/DtoB.Cad.Normalize/Placement/LocalPlacementContracts.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Placement/LocalPlacementContracts.cs)
- **Occurrences (`04E`)**:
  24. [`packages/DtoB.Cad.Normalize/Occurrences/CadOccurrenceExpander.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Occurrences/CadOccurrenceExpander.cs)
  25. [`packages/DtoB.Cad.Normalize/Occurrences/OccurrenceContracts.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Occurrences/OccurrenceContracts.cs)
  26. [`packages/DtoB.Cad.Normalize/Occurrences/OccurrenceIdentity.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Occurrences/OccurrenceIdentity.cs)
- **Styles (`04F`)**:
  27. [`packages/DtoB.Cad.Normalize/Styles/CadStyleResolver.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Styles/CadStyleResolver.cs)
  28. [`packages/DtoB.Cad.Normalize/Styles/RawStyleAccess.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Styles/RawStyleAccess.cs)
  29. [`packages/DtoB.Cad.Normalize/Styles/StyleContracts.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Styles/StyleContracts.cs)
- **Assembly (`04G` — 유일한 구체 정규화기 `CadNormalizer : ICadNormalizer`)**:
  30. [`packages/DtoB.Cad.Normalize/Assembly/AssemblySupport.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/AssemblySupport.cs) (`2,854` bytes — `CadMaterializationBudget`, `CadAssemblyIdentity`, `CadOrientationValidation`)
  31. [`packages/DtoB.Cad.Normalize/Assembly/CadBoundsQuery.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadBoundsQuery.cs) (`8,386` bytes — `CadBoundsQuery`, `CadBoundsResult`, `CadBounds`, `CadBoundsState`)
  32. [`packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.cs) (`17,905` bytes — `CadNormalizer : ICadNormalizer`)
  33. [`packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.Geometry.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.Geometry.cs) (`17,281` bytes — 기하/주석 조립 및 `CadBulgeGeometry`)
  34. [`packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.Styles.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.Styles.cs) (`5,282` bytes — 선언/유효 스타일 및 레이어 조립)

> **04H 무변경 검증**: [`artifacts/phase04/04h-20261019/before.json`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/artifacts/phase04/04h-20261019/before.json)과 현재 작업 트리의 SHA-256 해시를 대조한 결과, 위 34개 프로덕션 파일은 `04H` 동안 **단 1바이트도 수정되지 않았다**.

---

## 5. V1 Discrepancy Matrix

| # | 항목 | V1 리뷰 (`PHASE_04_FINAL_REVIEW.md`)의 잘못된 기술 | 실제 저장소 / 승인된 증거 (Authoritative Truth) | 분류 |
|---|---|---|---|---|
| 1 | `04G` 프로덕션 파일 및 구체 정규화기 | `CadSnapshotNormalizer.cs`, `CadDocumentAssembler.cs`, `CadCanonicalGeometryBuilder.cs`, `ICadSnapshotNormalizer` 등 실재하지 않는 이름 기재 | `packages/DtoB.Cad.Normalize/Assembly/` 산하 5개 파일(`AssemblySupport.cs`, `CadBoundsQuery.cs`, `CadNormalizer.cs`, `CadNormalizer.Geometry.cs`, `CadNormalizer.Styles.cs`) 및 `CadNormalizer : ICadNormalizer` | `REVIEW_V1_ERROR` |
| 2 | `04A`~`04F` 고정 다이제스트 | `DBDA...`, `8416...`, `B654...`, `9DBC...`, `5A75...`, `0395...`, `99D1...` (근거 없는 해시) | `04A`: `3E49A2...`, `04B`: `568D2A...`, `04C`: `81CA20...`, `04D`: `02045C...`, `04E`: `143684...`, `04F`: `7A448C...`, `04G`: `8A5E07...` | `REVIEW_V1_ERROR` |
| 3 | 원시 DXF HEADER `$EXTMIN`/`$EXTMAX` | `$EXTMIN = (-1300.225, -10100, 0)`, `$EXTMAX = (20700, 8500, 0)`라고 서술 (모델 Bounds와 혼동) | 원시 DXF `$EXTMIN = (1e+20, 1e+20, 1e+20)` (pair 16), `$EXTMAX = (-1e+20, -1e+20, -1e+20)` (pair 20). 역전된 신뢰 불가 헤더 값이며 `CadBoundsQuery`는 이를 절대 읽지 않음 | `REVIEW_V1_ERROR` |
| 4 | `04G` 비등방 스케일/전단 `Circle`/`Arc` 의미론 | 비등방 스케일/전단 시 `GeometryId = null`로 기하 생성을 거부한다고 서술 | 공유 정의 로컬 `CadCircle`/`CadArc` + 아핀 `CadBlockOccurrence` 변환을 유지하며, `CadBoundsQuery`가 타원 매개변수 방정식으로 해석적 극값을 계산함 | `REVIEW_V1_ERROR` |
| 5 | 공식 통제 문서 `Geometry`(`149`) 내부 구성 | `LINE 68 + LWPOLYLINE 12 + CIRCLE 1 + ARC 8 = 89` 및 "확장된 리프 기하 60개"라고 서술 | 총 `149`개 = `119 LINE` (`59` 소스 `LINE` + `60` 폴리라인 파생 직선 세그먼트) + `12 POINT` + `16 LWPOLYLINE` + `1 ARC` + `1 SOLID` (`CIRCLE`은 `0`개). 투영 리프(`26`개)는 `GeometryId`를 공유하며 기하를 복제하지 않음 | `REVIEW_V1_ERROR` |
| 6 | 공식 통제 문서 `Styles`(`201`) 의미론 | `196 Linetypes + 3 TextStyles + 2 DimensionStyles = 201` (DXF 테이블 카탈로그와 혼동) | `CadDocument.Styles`는 엔티티/occurrence 스타일 레지스트리임: `158` 소스 엔티티 스타일 + `17` occurrence 컨텍스트 유효 스타일 + `26` 투영 리프 유효 스타일 = `201` | `REVIEW_V1_ERROR` |
| 7 | 공식 통제 문서 `Annotations`(`44`) 내부 구성 | `TEXT 23 + MTEXT 3 + DIMENSION 6 + ATTRIB 12 = 44`라고 서술 (`ATTRIB` 인스턴스화 구현처럼 오기) | `CadEntity.Annotation` 총 `44`개 = `TEXT 36` + `MTEXT 4` + `DIMENSION 4`. 공식 DXF에는 `ATTRIB`/`ATTDEF`가 없으며 PHASE 04에서 `ATTRIB`/`ATTDEF` 인스턴스화는 명시적 미지원(deferred/unsupported)임 | `REVIEW_V1_ERROR` |
| 8 | 리소스 상한 (`MaxDiagnostics` 및 계약 명칭) | `MaxDiagnostics = 50,000` 및 `MaxExpandedEntities`, `MaxCanonicalGeometries`로 오기 | `CadNormalizationLimits.Development` = `MaxDepth: 32`, `MaxCellsPerInsert: 10,000`, `MaxOccurrences: 100,000`, `MaxDerivedGeometry: 250,000`, `MaxDiagnostics: 5,000` | `REVIEW_V1_ERROR` |
| 9 | 빌드 경고 상태 | 전체 Release 빌드가 `0 warnings`라고 서술 | Core Release Rebuild는 `0 warnings, 0 errors`. 전체 `DtoB.sln` Release Rebuild(`-RevitInstallDir` 포함)는 `0 errors` 및 기존 승인된 `MSB3277` 3개 어셈블리 쌍 충돌만 존재 | `REVIEW_V1_ERROR` |
| 10 | 역사적 서브페이즈 테스트 수 | `04A`를 `907/907`로 기재 | `PHASE 03`: `773`, `04A`: `844`, `04B`: `956`, `04C`: `1,052`, `04D`: `1,128`, `04E`: `1,188`, `04F`: `1,258`, `04G`: `1,315`, `04H`: `1,402` | `REVIEW_V1_ERROR` |

---

## 6. Fixed-Digest Correction

V1 리뷰에 기재되었던 `DBDA...`, `8416...`, `B654...`, `9DBC...`, `5A75...`, `0395...`, `99D1...` 값은 실제 저장소의 어떤 아티팩트에서도 유래하지 않은 **1차 리뷰 작성 시의 잘못된 데이터(`REVIEW_V1_ERROR`)**임을 확인하고 폐기한다.

실제 `04A`~`04H` 게이트 및 본 V2 리뷰의 독립 테스트 실행(`DTOB_*_EVIDENCE_DIRECTORY`)에서 재현된 **유일한 권위 있는 고정 다이제스트(Authoritative Accepted Digests)**는 다음과 같다:

| 단계 / 증거 파일 | SHA-256 다이제스트 | 바이트 크기 |
|---|---|---:|
| **`04A`** (`contract-invariant.json`) | `3E49A200C8AD688F6F53842F17556D849979B1C8F3F6A083438721CA02F58EBC` | `4,534` |
| **`04B`** (`scalar-contract-evidence.json`) | `568D2A7C28863360A4839FF0CA59DF09C1E7C9066EC3A487701872506A68D4C3` | `1,365` |
| **`04C`** (`coordinate-evidence.json`) | `81CA2010F120BD65888EF57AABE5A2F55E230DDC291276E1CBA8BAC9584F0342` | `897` |
| **`04D`** (`local-transform-evidence.json`) | `02045C202CACF9DA66406827E297435CCCB7448D1D0A38C7B52E7FCAAA3A0EE5` | `1,779` |
| **`04E`** (`occurrence-expansion-evidence.json`) | `143684A78B37AA4962345A28FC8C268E24CB4ECDCDF2EBD410B66EC99A1B626B` | `64,486` |
| **`04F`** (`effective-style-evidence.json`) | `7A448C95EC2F67DCA6C65C3D60B4982E79312814C8C1FD8E5A62C55C611FFC01` | `53,380` |
| **`04G`** (`normalized-invariant.json` — 공식 통제 `CadDocument`) | `8A5E078ADB9990C7127B7E626DEC1333071F481FA4A4B7F91CD17D23505F4F24` | `2,233,701` |
| **`03G`** (`controlled-snapshot.json` — Raw 고정 컨텍스트) | `4B91C83791F89DEC9BDC633C832001F2D1E537E7F0330BEE0666067CAE9A310C` | `3,776,986` |
| **`03H`** (`SceneSHA256` — 진단 씬 회귀) | `9D179A0EFAC5EE803E8AEC2AC3C48E1AB0986F1C6159BCB1764E8071933963A7` | — |

---

## 7. Controlled Source Authority

공식 통제 입력 파일의 물리적·Git 권위를 독립 검증했다:
- **정준 경로**: [`datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf)
- **바이트 크기**: `96,621` bytes
- **물리 줄 수 / 그룹 코드 쌍**: `17,474` physical lines / `8,737` pairs
- **SHA-256**: `2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC`
- **Git Blob OID (`git cat-file blob HEAD:datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf`)**: `8d93c05bb4fb81e9d81d2f0804255b47d9c71ba0` (`BlobEqualsWorkingBytes = true`)
- **`04G` 리뷰 파일명 오기 조정**: [`reviews/PHASE_04G_IMPLEMENTATION_REVIEW.md`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/reviews/PHASE_04G_IMPLEMENTATION_REVIEW.md)는 1바이트도 수정되지 않고 보존되었으며(`BA5F48D1180C1A45D94E98093C3FE75BD579B5D2F37223F229768E12E54A60A1`), 섹션 25의 `architectural_example-imperial.dxf` 표기가 텍스트 오기이고 실제 코드·테스트·골든·매니페스트는 모두 `datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf`만 사용함이 [`PHASE_04H_REPORT.md`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/docs/status/PHASE_04H_REPORT.md) §6 및 [`Cad04HReleaseTests.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/tests/DtoB.Tests/Cad04HReleaseTests.cs)에서 검증되었다.

---

## 8. Raw HEADER Extents Correction

실제 [`datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf)의 원시 HEADER 섹션을 직접 파싱하여 확인한 결과:
- **`$ACADVER`** (pair index `2`): `1 = AC1032`
- **`$EXTMIN`** (pair index `16`): `10 = 1e+20, 20 = 1e+20, 30 = 1e+20` (`(1e+20, 1e+20, 1e+20)`)
- **`$EXTMAX`** (pair index `20`): `10 = -1e+20, 20 = -1e+20, 30 = -1e+20` (`(-1e+20, -1e+20, -1e+20)`)
- **`$INSUNITS`** (pair index `454`): `70 = 4` (`Millimeters`)

즉, 공식 통제 DXF의 `$EXTMIN` / `$EXTMAX`는 `+1e20 > -1e20`으로 역전된 미초기화 센티넬 값이며 공간적 참값(spatial truth)이 아니다.
- 원시 HEADER 레코드와 팩트는 `CadDocument.Provenance`에 원시 증거로만 보존된다.
- [`CadBoundsQuery`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadBoundsQuery.cs)는 오직 `CadDocument`의 정규화된 기하 및 주석 앵커만 소비하며 HEADER extents를 절대 읽지 않는다.
- [`Cad04GEvidenceTests.HeaderExtentsCannotAlterGeometryDerivedModelBounds`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/tests/DtoB.Tests/Cad04GEvidenceTests.cs#L33-L51)는 `$EXTMIN`/`$EXTMAX`의 6개 좌표를 `±123456789`로 치환해도 `new CadBoundsQuery(doc).Model()` 결과가 `Min = (-1300.225, -10100, 0), Max = (20700, 8500, 0)`으로 100% 동일함을 증명한다.

---

## 9. 04G Geometry / Affine Semantics Correction

V1 리뷰의 "비등방 스케일/전단 시 `Circle`/`Arc`를 `GeometryId = null`로 거부한다"는 서술은 틀렸으며 실제 `04G` 아키텍처 및 구현은 다음과 같다:
- 블록 정의 내부의 `CIRCLE` 및 `ARC`(그리고 bulge로부터 생성된 폴리라인 호 세그먼트)는 **정의 로컬 공간의 공유 해석적 `CadCircle` / `CadArc` 페이로드**로 1회 생성된다([`CadNormalizer.Geometry.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.Geometry.cs#L100-L106)).
- 비균등 스케일(`ScaleX != ScaleY`), 회전, 반사(mirror), 전단(shear)을 포함하는 배치 변환은 `CadBlockOccurrence.LocalTransform` 및 `AccumulatedTransform`에 아핀 행렬로 보존되며, 투영된 리프(`V2Entity`)는 정의 템플릿의 `GeometryId`를 그대로 공유한다(가짜 월드 원을 만들거나 `GeometryId = null`로 거부하지 않음).
- 경계 상자 질의 시 [`CadBoundsQuery.Curve`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadBoundsQuery.cs#L111-L130)가 평면 기저 $\mathbf{u}, \mathbf{v}$에 선형 블록을 적용한 벡터 $\mathbf{a} = r L(\mathbf{u}), \mathbf{b} = r L(\mathbf{v})$로부터:
  - 전체 원(`CadCircle`): 각 좌표축 반폭 $\operatorname{hypot}(a_j, b_j)$ (예: $R(45^\circ) S(2,3,1)$ 및 $r=2$일 때 반폭 $\sqrt{26}$)
  - 부분 호(`CadArc`): 시작/끝점과 유향 스윕 내부에 속하는 도함수 영점 $\theta^* = \operatorname{atan2}(b_j, a_j)$ 및 $\theta^* + \pi$를 평가하여 정확한 해석적 경계를 계산한다(`affine-positive.dxf`, `affine-mirror.dxf` 골든 및 `Cad04GGeometryAndBoundsTests`로 검증).

---

## 10. Controlled Geometry Inventory

실제 공식 통제 문서(`normalized-invariant.json`, SHA-256 `8A5E078ADB9990C7127B7E626DEC1333071F481FA4A4B7F91CD17D23505F4F24`)에서 직접 추출한 `CadDocument.Geometry` 카탈로그(총 **`149`개**)의 정확한 구성은 다음과 같다:
- **`CadLine` (`$kind = "line"`)**: **`119`개** = 원시 `LINE` 엔티티 `59`개 + `LWPOLYLINE`으로부터 파생된 직선 세그먼트 `60`개
- **`CadPoint` (`$kind = "point"`)**: **`12`개** (미사용 치수 그래픽 블록 정의 내부의 원시 `POINT` `12`개)
- **`CadPolyline` (`$kind = "polyline"`)**: **`16`개** (`60`개의 파생 직선 세그먼트 ID를 순서대로 참조하는 원시 `LWPOLYLINE` 컨테이너 `16`개)
- **`CadArc` (`$kind = "arc"`)**: **`1`개** (원시 `ARC` 핸들 `9D`, pair `2868`, 반지름 `900`, 시작각 `0`, 스윕 $\pi/2$)
- **`CadSolid` (`$kind = "solid"`)**: **`1`개** (원시 `SOLID` 핸들 `7B`, pair `2747`, 삼각형 경계)
- **`CadCircle` (`$kind = "circle"`)**: **`0`개** (공식 통제 DXF에는 원시 `CIRCLE` 엔티티가 존재하지 않음)
- **합계**: 원시 기하/폴리라인 컨테이너 `89`개(`59 + 12 + 16 + 1 + 1`) + 폴리라인 파생 세그먼트 `60`개 = **`149`개**.
- **투영 리프의 기하 공유**: `26`개의 Projected-Leaf 엔티티는 블록 정의 템플릿 엔티티의 `GeometryId`를 공유 참조하며, occurrence별 중복 기하 페이로드를 생성하지 않는다.

---

## 11. Controlled Style Registry Semantics

`CadDocument.Styles`의 **`201`개** 항목은 DXF `TABLE` 카탈로그 수(`LTYPE`, `STYLE`, `DIMSTYLE`)가 아니라, [`CadNormalizer.Styles.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.Styles.cs#L30-L54)가 조립한 **선언 및 유효 `CadStyle` 레코드 레지스트리**이다:
1. **원시 엔티티 스타일 (`158`개)**: `ENTITIES` 섹션의 비-`INSERT` 루트 엔티티(`97`개)에 대한 루트 유효 스타일(`role = "effective-style"`) + `BLOCKS` 내부 템플릿 및 `INSERT` 소스 엔티티(`61`개)에 대한 선언 스타일(`role = "declared-style"`).
2. **구체화된 `INSERT` occurrence 컨텍스트 스타일 (`17`개)**: `17`개 `CadBlockOccurrence`에 바인딩된 컨텍스트 유효 스타일(`role = "insert-style"`).
3. **Projected-Leaf 유효 스타일 (`26`개)**: `26`개 투영 리프 엔티티에 바인딩된 occurrence별 유효 스타일(`role = "effective-style"`).
- **합계**: $158 + 17 + 26 = \mathbf{201}$개 (`CadStyle`은 선언 모드, 해석 상태, 규칙 ID, 출처 팩트 증거 체인을 포함하므로 값만 같다고 임의 병합하지 않음).

---

## 12. Annotation Inventory

공식 통제 문서의 주석은 별도 최상위 컬렉션이 아니라 `CadEntity.Kind == CadEntityKind.Annotation`인 엔티티에 내장된 `CadEntity.Annotation`(`CadAnnotation`)으로 표현되며, 총 **`44`개**의 실제 내역은 다음과 같다:
- **`CadAnnotationKind.Text` (`text`)**: **`36`개**
- **`CadAnnotationKind.MText` (`mText`)**: **`4`개**
- **`CadAnnotationKind.Dimension` (`dimension`)**: **`4`개**
- **합계**: $36 + 4 + 4 = \mathbf{44}$개.
- **`ATTRIB` / `ATTDEF` 정책**: 공식 통제 DXF에는 `ATTRIB`/`ATTDEF` 레코드가 `0`개이며, PHASE 04에서 `ATTRIB`/`ATTDEF`는 공간 인스턴스화가 미지원(`CadEntityKind.Unsupported`, `CAD_ENTITY_UNSUPPORTED`)으로 명시적 연기되어 있다.

---

## 13. Resource-Limit Correction

[`NormalizationBoundary.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/NormalizationBoundary.cs#L38-L53)에 정의된 실제 프로덕션 리소스 한도 계약(`CadNormalizationLimits.Development`)은 다음과 같다:
- **`MaxDepth`**: `32`
- **`MaxCellsPerInsert`**: `10,000`
- **`MaxOccurrences`**: `100,000`
- **`MaxDerivedGeometry`**: `250,000` (루트/공유 기하 페이로드, 폴리라인 컨테이너, 파생 세그먼트, 그리고 투영 리프 엔티티 구체화를 단일 단조 카운터 `CadMaterializationBudget`으로 합산)
- **`MaxDiagnostics`**: **`5,000`** (`diagnostics.Count + rawDiagnostics.Count >= options.Limits.MaxDiagnostics - 1`에서 일반 진단 수용을 멈추고 마지막 1슬롯을 종료 진단용으로 예약; V1 리뷰의 `50,000` 표기는 오류였음)

---

## 14. Build-Warning Correction

독립 실행한 MSBuild Release Rebuild의 실제 경고 상태는 다음과 같다:
- **Core Release Rebuild (`packages/DtoB.Core/DtoB.Core.csproj`)**: **`0 errors`, `0 warnings`**.
- **Desktop / Non-Revit Release Rebuild (`apps/DtoB.Desktop/DtoB.Desktop.csproj`, `tests/DtoB.Tests/DtoB.Tests.csproj`)**: **`0 errors`, `0 warnings`**.
- **Full Solution Release Rebuild (`DtoB.sln` with `-p:RevitInstallDir="D:\프로그램\Revit 2025"`)**: **`0 errors`**, 그리고 `DtoB.Revit2025.csproj`에서 기존 PHASE 00부터 승인된 **`MSB3277` 어셈블리 참조 버전 충돌 3쌍**만 발생한다:
  1. `Microsoft.VisualBasic, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a|Microsoft.VisualBasic, Version=10.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a`
  2. `System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a|System.Drawing, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a`
  3. `WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35|WindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35`
- 신규 경고 코드나 신규 어셈블리 충돌은 **0건**이다.

---

## 15. Historical Test-Count Correction

각 단계별 보고서 및 TRX 증거에서 확인된 정확한 누적 테스트 수는 다음과 같다:

| 단계 | 누적 통과 테스트 수 | 단계별 신규 추가 테스트 수 | 근거 |
|---|---:|---:|---|
| **PHASE 03 Final Baseline** | **`773 / 773`** | — | `PHASE_03_RELEASE_REPORT.md` |
| **PHASE 04A** | **`844 / 844`** | `+71` | `PHASE_04A_REPORT.md`, `PHASE_04A_REVIEW.md` |
| **PHASE 04B** | **`956 / 956`** | `+112` | `PHASE_04B_REPORT.md`, `PHASE_04B_REVIEW.md` |
| **PHASE 04C** | **`1,052 / 1,052`** | `+96` | `PHASE_04C_REPORT.md`, `PHASE_04C_REVIEW.md` |
| **PHASE 04D** | **`1,128 / 1,128`** | `+76` | `PHASE_04D_REPORT.md`, `PHASE_04D_REVIEW.md` |
| **PHASE 04E** | **`1,188 / 1,188`** | `+60` | `PHASE_04E_REPORT.md`, `PHASE_04E_REVIEW.md` |
| **PHASE 04F** | **`1,258 / 1,258`** | `+70` | `PHASE_04F_REPORT.md`, `PHASE_04F_REVIEW.md` |
| **PHASE 04G** | **`1,315 / 1,315`** | `+57` | `PHASE_04G_IMPLEMENTATION_REPORT.md`, `PHASE_04G_IMPLEMENTATION_REVIEW.md` |
| **PHASE 04H (Final)** | **`1,402 / 1,402`** | `+87` | `PHASE_04H_REPORT.md`, `PHASE_04_FINAL_GATE.md` |

---

## 16. ADR-018 Audit (`ADR-018-CAD-IR-Normalization-Boundary.md`)
- **3계층 분리**: `RawDrawingSnapshot`(`DtoB.Cad.Raw`) → `ICadNormalizer`(`DtoB.Cad.Normalize`) → `CadDocument`(`DtoB.Cad.V2`, `SchemaVersion = 2`) 경계가 엄격히 유지되며 BIM IR(`DtoB.Bim`) 및 Revit API와 완전히 분리되어 있다.
- **전수 회계 원장(`Ledger`)**: 모든 원시 레코드(`0..Provenance.Records.Count-1`)가 `Normalized`, `PartiallyNormalized`, `UnsupportedNormalization`, `NotApplicable` 중 정확히 하나의 상태를 부여받으며 `ValidateStructure()`가 이를 강제한다.

---

## 17. ADR-019 Audit (`ADR-019-Coordinate-Normalization-Transforms.md`)
- **단위 및 각도**: 정준 길이 단위는 밀리미터(`mm`), 정준 각도 단위는 라디안(`rad`)으로 통일되었다.
- **OCS/WCS 및 AAA**: WCS 고유 역할과 OCS 투영 역할이 분리되었고, Autodesk Arbitrary Axis Algorithm($|N_x| < \frac{1}{64} \land |N_y| < \frac{1}{64}$)과 단일 `INSERT` 로컬 변환 행렬 $L = A \cdot T(\mathbf{i}_{ocs}) \cdot R(r) \cdot S(s_x, s_y, s_z) \cdot B(-b_x, -b_y, -b_z)$가 정확히 구현되었다.

---

## 18. ADR-020 Audit (`ADR-020-Occurrence-Identity-and-Expansion.md`)
- **결정론적 ID 및 발생 확장**: `cad2:` SHA-256 바이너리 프레이밍 ID(`Source`, `Definition`, `SourceInsert`, `Occurrence`, `DerivedComponent`, `Leaf`), 공유 `CadBlockDefinition`, 중첩 `INSERT`/`MINSERT` 행 우선 확장, 순환 참조 거부, 04F 유효 스타일 해석, 세션 전체 리소스 예산이 모두 승인된 명세대로 동작한다.

---

## 19. Schema / Codec (`04A`)
- [`CadDocument.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad/V2/CadDocument.cs) 및 [`CadIrJson.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad/V2/CadIrJson.cs)는 `schemaVersion = 2`, camelCase 속성명, 필수 필드 강제, 알 수 없는 필드/중복 프로퍼티/잘못된 참조 거부, `-0.0` 정규화를 보장한다.

---

## 20. Units (`04B`)
- [`CadUnitNormalizer.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Scalars/CadUnitNormalizer.cs)는 `$INSUNITS` 코드 `1`(`25.4`), `2`(`304.8`), `4`(`1.0`), `5`(`10.0`), `6`(`1000.0`)만 허용하며, `0`, `3`, 미지정(absent) 입력은 추측 없이 `CAD_UNITS_UNRESOLVED`(`InvalidNormalization`, `Document = null`)로 거부한다(`unit-1/2/4/5/6.dxf`, `unit-refuse-0/3.dxf`, `unit-absent.dxf`로 검증).

---

## 21. Coordinates / AAA (`04C`)
- [`CadArbitraryAxis.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Coordinates/CadArbitraryAxis.cs) 및 [`CadCoordinateNormalizer.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Coordinates/CadCoordinateNormalizer.cs)는 WCS 전용 좌표 역할과 OCS 투영 역할을 엄격히 구분하며, `ocs-0.dxf`~`ocs-7.dxf`(8개 물리 픽스처)에서 $+Z, -Z, +X, +Y$, 임의 경사 법선, 그리고 $\frac{1}{64}$ 임계값 경계 양측이 독립 외적 오라클로 검증되었다.

---

## 22. Placement (`04D`)
- [`CadInsertLocalTransform.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Placement/CadInsertLocalTransform.cs)는 $L = A \cdot T(\mathbf{i}_{ocs}) \cdot R(r) \cdot S(s_x, s_y, s_z) \cdot B(-b_x, -b_y, -b_z)$를 계산하며, `block-base.dxf`(기준점 `(3,4,0)`, $R(90^\circ)$, $S(2,3,1)$)에서 기준점이 `(0,0,0)`으로, 끝점 `(4,4,0)`이 `(0,2,0)`으로 매핑됨이 검증되었다.

---

## 23. Occurrences / MINSERT / Cycle (`04E`)
- [`CadOccurrenceExpander.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Occurrences/CadOccurrenceExpander.cs)는 DFS 순서로 루트/중첩 `INSERT` 및 행 우선(`row-major`) `MINSERT` 셀을 확장한다.
- `nested.dxf`(루트 $R(90^\circ)$, 자식 $S(2,1,1)$, 끝점 `(2,0,0) -> (0,4,0)`), `minsert.dxf`($2 \times 3$ 셀, 간격 `10/20`이 자체 스케일 `(2,3,1)`에 의해 중복 스케일되지 않고 원점 `(-20*row, 10*col, 0)` 생성), `cycle.dxf`(활성 경로 순환 감지 및 진단 보존)가 골든 및 독립 오라클로 검증되었다.

---

## 24. Styles (`04F`)
- [`CadStyleResolver.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Styles/CadStyleResolver.cs)는 `Layer0` 상속, `ByBlock` 조상 체인 탐색, `ByLayer` 카탈로그 조회, `TrueColor` 우선순위를 해석하며, `missing-catalog.dxf`에서 누락된 카탈로그 참조가 조용히 치유되지 않고 `PartiallyNormalized`로 기록됨이 확인되었다.

---

## 25. Template / Leaf Architecture (`04G`)
- 블록 정의 내부의 소스 엔티티는 선언 스타일을 가진 Source-Template `CadEntity`로 `CadBlockDefinition.ChildEntityIds`에만 속하고, 각 occurrence 투영은 공유 `GeometryId`와 occurrence별 유효 스타일을 가진 Projected-Leaf `CadEntity`(`cad2:leaf:*`)로 생성되어 `CadDocument.ModelSpace`에 `(leafId, occurrenceId)`로 등록된다(`shared-style.dxf`에서 1개 공유 기하, 1개 정의 템플릿, 2개 리프 엔티티[ACI Red vs TrueColor Blue]로 검증).

---

## 26. Canonical Geometry (`04G`)
- `CadPoint`, `CadLine`, `CadCircle`, `CadArc`, `CadPolyline`(정렬된 세그먼트 참조 목록 + `Closed`), `CadSolid`(평면 삼각형/사각형 경계)가 정확히 생성된다.
- Bulge 세그먼트(`b = 0, ±0.5, ±1, ±2`, 닫힌 경사 폴리라인 `bulge-closed-tilted.dxf`, 퇴화 현 거부 `bulge-degenerate.dxf`, 극미세 `1e-300` 정밀도 거부)가 모두 독립 현/호 방정식 오라클로 검증되었다.

---

## 27. Annotations (`04G`)
- `TEXT`, `MTEXT`, 지원되는 선형 `DIMENSION`은 `CadEntity.Annotation`에 정규화된 기준점(`AnchorsMm`), 텍스트 내용, 높이(`HeightMm`), 회전각(`RotationRadians`), 선택적 원시 측정값(`SourceMeasurementMm`)을 보존하며, 글리프 박스나 치수 그래픽 블록을 임의 합성하지 않고 `CAD_ANNOTATION_ANCHORS_ONLY` 진단과 함께 `PartiallyNormalized`로 정직하게 표시된다.

---

## 28. Bounds (`04G`)
- [`CadBoundsQuery`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadBoundsQuery.cs)는 `GeometryLocal`, `GeometryTransformed`, `Definition`, `Occurrence`, `Model` 질의를 제공하며, 원시 HEADER `$EXTMIN`/`$EXTMAX`를 전혀 참조하지 않는다.
- 공식 통제 문서의 `Model()` 경계는 `State = Partial`, `Min = (-1300.225, -10100, 0)`, `Max = (20700, 8500, 0)`이며 원시 DXF 바이트 증인(`DIMENSION E4` pair `3954` -> `minX = -1300.225`, `LINE 103/105/107` pairs `4178/4203/4228` -> `maxX = 20700`, `TEXT 142` pair `4525` -> `minY = -10100`, `TEXT 15D` pair `4896` -> `maxY = 8500`)과 일치한다.

---

## 29. Source / Fact / Ledger (`04G`)
- 공식 통제 문서는 `333`개 원시 레코드와 `5,535`개 원시 팩트를 보존하며, `Ledger`는 `117 Normalized + 65 PartiallyNormalized + 52 UnsupportedNormalization + 99 NotApplicable = 333`으로 전수 회계된다.
- Phase 03에서 식별된 `Unsupported 52`(`1 VPORT + 3 APPID + 48 OBJECTS`)가 단 1건의 누락이나 조용한 승격 없이 그대로 보존된다.

---

## 30. Fixtures / Goldens (`04H`)
- **물리 입력 코퍼스(`119`개)**: 기존 Phase 03 픽스처 `86`개(무수정 참조, [`datasets/controlled/phase03g/manifest.json`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/datasets/controlled/phase03g/manifest.json) 해시 `1B3D1798CD1137C8F4933E44AB3CD8A2717DA3B3F9E7AAADC08A1E49E60B0C4B` 불변) + 신규 Phase 04 최소 텍스트 DXF `33`개([`datasets/controlled/phase04/manifest.json`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/datasets/controlled/phase04/manifest.json) 해시 `DEF3DCA17EF99F2C6CEE42FB5AE6A1830DFFA8F2ABFD328466B1A9100AC23ED7`).
- **정규화 골든(`31`개 문서 + `3`개 단위 거부)**: [`datasets/controlled/phase04/goldens/manifest.json`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/datasets/controlled/phase04/goldens/manifest.json)(해시 `08AD8DCA5035D0B4BD732F066784B3F6C6A895E85393B386EEE8774F65CEC000`)에 `30`개 소형 정준 JSON + `31`개 요약 JSON(`25 Normalized`, `6 PartiallyNormalized`)이 수록되어 있으며, 단위 거부 3건(`unit-refuse-0`, `unit-refuse-3`, `unit-absent`)은 문서를 게시하지 않는다.

---

## 31. Independent Oracles (`04H`)
- [`Cad04HCorpusTests.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/tests/DtoB.Tests/Cad04HCorpusTests.cs)의 `SourceAndMathOracle`는 `CadNormalizer`나 `CadBoundsQuery`를 호출하여 정답을 만들지 않고, 원시 DXF 그룹 코드와 독립 대수/기하 수식(단위 배율, AAA 외적, 현-반지름-스윕 공식, 행렬 곱, `MINSERT` 격자 오프셋, 타원 극값 $\sqrt{26}$ 및 사분원 극값)으로 각 골든을 검증한다.

---

## 32. Strict Roundtrip (`04H`)
- `31`개 정규화 문서 전부가 `SerializeUtf8(A) -> DeserializeAndValidate -> SerializeUtf8(B) -> DeserializeAndValidate -> SerializeUtf8(C)` 3중 왕복에서 `A == B == C` 바이트 일치를 만족하며, 9종의 최종 문서 변조 네거티브 케이스(`Cad04HReleaseTests`)가 모두 즉시 거부된다.

---

## 33. Determinism (`04H`)
- `InvariantCulture`, `fr-FR`, `ko-KR` 3개 문화권 및 서로 다른 작업 디렉터리(저장소 루트 CWD `process-a`, 사용자 TEMP CWD `process-b`, 최종 게이트 `final`)의 3개 독립 프로세스에서 생성된 **`186`개 산출물(`31`개 문서 $\times$ JSON/Summary `2`종 $\times$ `3`개 문화권)**이 단 1바이트의 차이도 없이 일치한다(`ProcessComparisons = 186`, 차이 `0`건).
- 비의미론적 내부 컨텍스트 인덱스 구성 순서를 역순으로 뒤집어도(`NonsemanticContextIndexConstructionOrderCannotChangeCanonicalOutput`) 공식 통제 문서의 UTF-8 바이트는 동일하다.

---

## 34. Fixed Accepted Digests (`04A` ~ `04G`, `03G`, `03H`)
- 본 V2 리뷰 과정에서 독립 실행한 VSTest 게이트(`$env:TEMP\phase04-v2-independent-gate`)에서 섹션 6의 모든 고정 다이제스트(`04A` `3E49A2...`, `04B` `568D2A...`, `04C` `81CA20...`, `04D` `02045C...`, `04E` `143684...`, `04F` `7A448C...`, `04G` `8A5E07...` [`2,233,701` bytes], `03G` `4B91C8...`, `03H` `9D179A...`)가 100% 동일하게 재현됨을 직접 확인했다.

---

## 35. Raw / Dxf Immutability
- `packages/DtoB.Cad.Raw` 및 `packages/DtoB.Cad.Dxf` 프로덕션 파일은 릴리스 베이스(`76ff6e0d8ccac797ba60d8c5657922c796a0e8c9`) 이후 변경이 **0건**이다.
- `119`개 물리 픽스처 바이트와 정규화 전후 `RawDrawingSnapshot` 직렬화 바이트가 100% 불변임이 검증되었다.

---

## 36. Protected Boundaries
- `packages/DtoB.Core`, `packages/DtoB.Geometry`, `packages/DtoB.Bim`, `packages/DtoB.Ipc`, `packages/DtoB.Project`, `services/DtoB.Analysis`, `apps/DtoB.Desktop`, `revit/*`의 프로덕션 코드는 PHASE 04 동안 단 1바이트도 수정되지 않았다.
- WPF, Revit API, 외부 상용 CAD SDK, 의미론적 객체 인식(벽/문/창문/방 후보) 또는 PHASE 05 코드 유입은 **0건**이다.

---

## 37. Resource / Cancellation
- 깊이(`32`/`33`), 셀(`10,000`/`10,001`), 전역 occurrence(`99,999`/`100,000`/`100,001`), 파생 기하 구체화(`249,999`/`250,000`/`250,001`), 진단(`5,000`, 마지막 1슬롯 종료 진단 예약) 경계와 사전/동시 취소(`CancellationToken`)가 실제 `CadNormalizer`까지 엔드투엔드로 검증되었으며, 한도 초과나 취소 시 부분 문서를 절대 게시하지 않는다(`Document == null`).

---

## 38. Limitations (`docs/status/PHASE_04_LIMITATIONS.md`)
- [`docs/status/PHASE_04_LIMITATIONS.md`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/docs/status/PHASE_04_LIMITATIONS.md)는 관리형 텍스트 DXF V1 전용 권위, `$INSUNITS` 허용 부분집합(`1, 2, 4, 5, 6`), 일반 XYZ 완결성 요구, 선가중치/투명도/색상북/폴리라인 폭 구체화 연기, `ATTRIB`/`ATTDEF` 인스턴스화 미지원, 텍스트 글리프 경계 및 치수 그래픽 미합성, 공식 문서의 `PartiallyNormalized` 상태(`Unsupported 52`), 엄격한 `LOW-01` 미해결 참조 정책, 리소스 상한(`32 / 10,000 / 100,000 / 250,000 / 5,000`), 호출자 스케줄링 책임을 투명하고 정직하게 명시하고 있다.

---

## 39. Tests (`1,402 / 1,402`)
- 독립 실행한 필터 없는 전체 테스트 스위트 결과: **`1,402 total / 1,402 passed / 0 failed / 0 skipped`**.
- `04G` 기준선 `1,315`개 테스트 ID 및 이름이 100% 유지되었고, `04H` 신규 QA 테스트 `87`개가 추가되었다.

---

## 40. Builds / Warnings
- **Core Release Rebuild**: `0 errors`, `0 warnings`.
- **Full `DtoB.sln` Release Rebuild (`-RevitInstallDir "D:\프로그램\Revit 2025"`)**: `0 errors`, 승인된 기존 `MSB3277` 3개 어셈블리 충돌 쌍(`Microsoft.VisualBasic 10.0/10.1`, `System.Drawing 4.0/8.0`, `WindowsBase 4.0/8.0`)만 발생하며 신규 경고는 **0건**이다.

---

## 41. Release Manifest / Inventory
- [`docs/status/phase04/release-manifest.json`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/docs/status/phase04/release-manifest.json), [`whole-phase-path-inventory.json`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/docs/status/phase04/whole-phase-path-inventory.json), [`candidate-release-payload.json`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/docs/status/phase04/candidate-release-payload.json), [`baseline-test-inventory.json`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/docs/status/phase04/baseline-test-inventory.json), [`new-test-inventory.json`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/docs/status/phase04/new-test-inventory.json)가 실제 저장소 상태 및 테스트/빌드 결과와 100% 일치한다.

---

## 42. Git State
- 브랜치 `phase/04-cad-normalization`, `HEAD = 76ff6e0d8ccac797ba60d8c5657922c796a0e8c9`, 스테이징된 파일 `0`개, 커밋/푸시/PR/태그 작업 없음.
- `git diff --check` 및 UTF-8 / 후행 공백(trailing whitespace) 감사가 모두 PASS 상태이다.

---

## 43. Release Readiness
- 실제 프로덕션 코드(`packages/DtoB.Cad`, `packages/DtoB.Cad.Normalize`)와 `04H` 릴리스 증거(`datasets/controlled/phase04`, `docs/status/phase04`, `tests/DtoB.Tests`)에는 결함이 전혀 없으며(`PRODUCTION_ERROR = 0`, `RELEASE_EVIDENCE_ERROR = 0`), 모든 불일치는 1차 리뷰 문서의 서술 오류(`REVIEW_V1_ERROR`)였음이 입증되었다.
- 따라서 PHASE 04는 소유자 결정에 따라 커밋/태그 및 PHASE 05 계획 단계로 진행할 준비가 완료되었다.

---

## 44. Findings

### 44.1 Production & Release Evidence Findings (`PRODUCTION_ERROR` / `RELEASE_EVIDENCE_ERROR`)
- **`BLOCKER`**: `0`건
- **`HIGH`**: `0`건
- **`MEDIUM`**: `0`건
- **`LOW`**: `0`건

### 44.2 Historical V1 Review Correction Findings (`REVIEW_V1_ERROR` — Resolved by V2)

#### Finding V1-01
- **Severity**: `HIGH` (V1 리뷰 문서 무결성 이슈; 프로덕션/릴리스 증거 결함 아님)
- **Category**: `REVIEW_V1_ERROR`
- **File / Evidence**: [`reviews/PHASE_04_FINAL_REVIEW.md`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Review/reviews/PHASE_04_FINAL_REVIEW.md) (§6, §8~§15, §19, §24)
- **Problem**: V1 리뷰가 실제 `packages/DtoB.Cad.Normalize/Assembly/` 프로덕션 파일(`AssemblySupport.cs`, `CadBoundsQuery.cs`, `CadNormalizer.cs`, `CadNormalizer.Geometry.cs`, `CadNormalizer.Styles.cs`) 대신 존재하지 않는 파일명을 기재하고, `04A`~`04F` 고정 다이제스트, 원시 HEADER `$EXTMIN`/`$EXTMAX` 값, 아핀 `Circle`/`Arc` 경계 의미론, 공식 문서의 `Geometry`(`149`)/`Styles`(`201`)/`Annotations`(`44`) 세부 내역, `MaxDiagnostics`(`5,000`), 솔루션 `MSB3277` 경고 허용 목록, `04A` 테스트 수(`844`)를 잘못 기술함.
- **Impact**: 실제 저장소 코드는 정상이나 V1 리뷰 문서를 권위 있는 릴리스 게이트 근거로 사용할 경우 승인된 `04G`/`04H` 증거와 모순이 발생함.
- **Reproduction**: `reviews/PHASE_04_FINAL_REVIEW.md`와 `docs/status/PHASE_04G_IMPLEMENTATION_REPORT.md`, `docs/status/PHASE_04H_REPORT.md`, `docs/status/phase04/release-manifest.json`을 대조하면 즉시 확인됨.
- **Recommended action**: `reviews/PHASE_04_FINAL_REVIEW.md`를 수정하지 않고 실패한 1차 리뷰 증거로 보존하며, 본 [`reviews/PHASE_04_FINAL_REVIEW_V2.md`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Review/reviews/PHASE_04_FINAL_REVIEW_V2.md)를 유일한 권위 있는 최종 독립 리뷰로 채택함.
- **Required gate**: 본 V2 리뷰(`reviews/PHASE_04_FINAL_REVIEW_V2.md`)에서 전 항목 교정 완료.

---

PHASE_04_FINAL_PASS_WITH_KNOWN_LIMITATIONS
