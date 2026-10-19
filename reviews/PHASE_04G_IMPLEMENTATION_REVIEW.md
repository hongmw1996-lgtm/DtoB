# PHASE 04G — Canonical Geometry & CAD v2 Document Assembly Independent Implementation Review

- **Date**: 2026-10-19
- **Reviewer**: Antigravity (Independent Architecture & Implementation Reviewer)
- **Target Branch / Baseline Commit**: `phase/04-cad-normalization` (`76ff6e0d8ccac797ba60d8c5657922c796a0e8c9`, Tag: `v0.4-phase03`)
- **Authoritative Prerequisites**: `PHASE_04F_PASS`, `PHASE_04G_ARCHITECTURE_PASS`, Accepted `ADR-018`, `ADR-019`, `ADR-020`
- **Target Implementation Report**: [`docs/status/PHASE_04G_IMPLEMENTATION_REPORT.md`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/docs/status/PHASE_04G_IMPLEMENTATION_REPORT.md)
- **Historical Pre-Implementation STOP Report**: [`docs/status/PHASE_04G_REPORT.md`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/docs/status/PHASE_04G_REPORT.md)

---

## 1. Executive Verdict

**Verdict**: **`PHASE_04G_PASS`**

PHASE 04G의 실제 프로덕션 구현([`AssemblySupport.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/AssemblySupport.cs), [`CadNormalizer.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.cs), [`CadNormalizer.Geometry.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.Geometry.cs), [`CadNormalizer.Styles.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.Styles.cs), [`CadBoundsQuery.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadBoundsQuery.cs))과 신규 테스트 5개 파일(57개 테스트), 기존 경계 가드 테스트 5개 파일의 갱신 내역을 독립적으로 전수 검증하고 `Audit-04G.ps1` 및 VSTest 전체 스위트를 직접 실행한 결과, **단 하나의 BLOCKER, HIGH, MEDIUM, LOW 결함도 발견되지 않았습니다.**

04G는 `PHASE_04G_ARCHITECTURE_PASS`의 두 가지 아키텍처 결정(Contract Decision 1: Source-Template vs. Projected-Leaf `CadEntity` 모델, Contract Decision 2: `SchemaVersion = 2` 및 Wire 불변성 유지 + `CadOrientationValidation` / `CadBoundsQuery` 런타임 검증·질의 분리)을 한 치의 오차도 없이 정확히 구현하였으며, `04A`~`04F`의 정규화 엔진들을 재구현 없이 합성하여 결정론적이고 손실 없는 `CadDocument (schemaVersion = 2)`를 조립합니다.

- **전체 테스트 결과**: **1,315 / 1,315 PASS** (0 Failed, 0 Skipped; 기존 1,258개 100% 보존 + 신규 04G 테스트 57개 통과)
- **Controlled Source Canonical `CadDocument` SHA-256**: `8A5E078ADB9990C7127B7E626DEC1333071F481FA4A4B7F91CD17D23505F4F24` (2,233,701 bytes; 3 프로세스 × 3 컬처 = 9회 실행 완전 동일)
- **이전 단계(04A~04F) 증거 다이제스트**: 100% 불변 유지

---

## 2. Historical Report Separation Check

- **사전 정지 보고서 보존**: [`docs/status/PHASE_04G_REPORT.md`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/docs/status/PHASE_04G_REPORT.md)는 Codex가 구현 전 아키텍처 모호성을 발견하고 정지했던 역사적 증거로서 수정이나 삭제 없이 그대로 보존되어 있습니다.
- **실제 구현 보고서 분리**: 이번 구현 결과는 [`docs/status/PHASE_04G_IMPLEMENTATION_REPORT.md`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/docs/status/PHASE_04G_IMPLEMENTATION_REPORT.md) 및 [`docs/plans/PHASE_04G_IMPLEMENTATION_MATRIX.md`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/docs/plans/PHASE_04G_IMPLEMENTATION_MATRIX.md)에 명확히 분리 기록되어 있으며, 본 리뷰 역시 [`reviews/PHASE_04G_ARCHITECTURE_REVIEW.md`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/reviews/PHASE_04G_ARCHITECTURE_REVIEW.md)를 덮어쓰지 않고 [`reviews/PHASE_04G_IMPLEMENTATION_REVIEW.md`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/reviews/PHASE_04G_IMPLEMENTATION_REVIEW.md)로 독립 작성되었습니다.

---

## 3. Architecture Pass Compliance Audit

[`reviews/PHASE_04G_ARCHITECTURE_REVIEW.md`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/reviews/PHASE_04G_ARCHITECTURE_REVIEW.md)에서 승인된 아키텍처 명세와의 일치 여부를 검증했습니다:

| 승인 항목 | 구현 위치 | 검증 결과 |
|---|---|---|
| Contract Decision 1: BLOCK 내부 엔티티는 Source-Template `CadEntity`로, Occurrence 투영 엔티티는 Projected-Leaf `CadEntity`로 분리 생성 | [`CadNormalizer.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.cs#L121-L185), [`CadNormalizer.Geometry.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.Geometry.cs#L43-L119) | **PASS** |
| Root 엔티티는 불필요한 template/leaf 복제 없이 단일 `CadEntity`로 `ModelSpace(rootId, null)`에 직접 등록 | [`CadNormalizer.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.cs#L172-L176), [`CadNormalizer.Geometry.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.Geometry.cs#L65-L71) | **PASS** |
| `CadIdentityDomain.Leaf` 토큰 레시피: `[Text(sourceEntityId), Text(occurrenceId)]` | [`AssemblySupport.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/AssemblySupport.cs#L58-L70) | **PASS** |
| Contract Decision 2: `SchemaVersion = 2` 및 `DtoB.Cad` DTO/JSON 직렬화 포맷 무수정 유지 | `packages/DtoB.Cad/**` (0 bytes modified) | **PASS** |
| Orientation Sign 검증을 `CadOrientationValidation`에 정밀 유리수(BigInteger dyadic) 행렬식으로 구현 | [`AssemblySupport.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/AssemblySupport.cs#L121-L263) | **PASS** |
| Analytic Bounds 계산을 `CadBoundsQuery`로 제공 (`CadDocument`에 직렬화 필드 추가 금지) | [`CadBoundsQuery.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadBoundsQuery.cs#L49-L176) | **PASS** |
| `Units.Evidence`에 가짜 헤더 Source Record를 날조하지 않고 `null` 유지 | [`CadNormalizer.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.cs#L56-L62) | **PASS** |

---

## 4. Schema / Wire Freeze Audit

`Audit-04G.ps1` 및 파일 해시 대조를 통해 `packages/DtoB.Cad/` 산하의 모든 계약 파일(`CadDocument.cs`, `CadEntities.cs`, `CadGeometry.cs`, `CadStyles.cs`, `CadAnnotations.cs`, `CadProvenance.cs`, `CadTransform.cs`, `CadPrimitives.cs`, `Serialization/CadIrJson.cs`)이 **단 1바이트도 변경되지 않았음**을 확인했습니다.
- `CadDocument.CurrentSchemaVersion == 2` 유지.
- `CadIrJson` 직렬화 규칙(`snake_case_lower`, ordinal key sorting, round-trip formatting 등) 무수정.

---

## 5. Scope & Boundary Audit

변경 및 추가된 파일 목록은 다음과 같습니다:
- **신규 프로덕션 파일 (5개)**:
  - [`packages/DtoB.Cad.Normalize/Assembly/AssemblySupport.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/AssemblySupport.cs)
  - [`packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.cs)
  - [`packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.Geometry.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.Geometry.cs)
  - [`packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.Styles.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.Styles.cs)
  - [`packages/DtoB.Cad.Normalize/Assembly/CadBoundsQuery.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadBoundsQuery.cs)
- **신규 테스트 파일 (5개)**:
  - [`tests/DtoB.Tests/Cad04GAssemblyTests.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/tests/DtoB.Tests/Cad04GAssemblyTests.cs)
  - [`tests/DtoB.Tests/Cad04GGeometryAndBoundsTests.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/tests/DtoB.Tests/Cad04GGeometryAndBoundsTests.cs)
  - [`tests/DtoB.Tests/Cad04GEvidenceTests.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/tests/DtoB.Tests/Cad04GEvidenceTests.cs)
  - [`tests/DtoB.Tests/Cad04GIntegrationEdgeTests.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/tests/DtoB.Tests/Cad04GIntegrationEdgeTests.cs)
  - [`tests/DtoB.Tests/Cad04GGeometryBudgetBoundaryTests.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/tests/DtoB.Tests/Cad04GGeometryBudgetBoundaryTests.cs)
- **수정된 기존 테스트 파일 (5개)**: `ICadNormalizer` 구체 구현체 존재 여부를 검사하던 리플렉션 어서션 1줄씩만 `Assert.Empty`에서 `Assert.Single(..., typeof(CadNormalizer))`로 갱신됨.
- **보호 대상 불변 확인**:
  - `packages/DtoB.Cad.Raw/**`, `packages/DtoB.Cad.Dxf/**`, `packages/DtoB.Cad/**`: 무수정
  - `packages/DtoB.Cad.Normalize/Scalars/**`, `Coordinates/**`, `Placement/**`, `Occurrences/**`, `Styles/**`: 무수정
  - `packages/DtoB.Ir/**`, `packages/DtoB.Engine/**`, `apps/**`: 무수정
  - PHASE 05+ 추론/토폴로지/벽·문 인식 기능 침범 없음.

---

## 6. Stage Composition Audit (04A–04F)

[`CadNormalizer.Normalize`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.cs#L19-L216)는 이전 단계의 엔진들을 중복 구현하지 않고 정식 퍼블릭 API로 호출하여 합성합니다:
1. **04A**: `CadDeterministicId.Create` (`SourceRecord`, `Fact`, `Layer`, `Style`, `Geometry`, `Entity`, `Leaf`, `Annotation`, `Diagnostic`), `CadCanonicalOrdering`, `CadDocument`, `CadNormalizationLedger`.
2. **04B**: [`CadUnitNormalizer.NormalizeUnits`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.cs#L55)로 단위 문맥 해석, `CadAngleNormalizer`로 각도(`ARC`, `TEXT`, `MTEXT`, `DIMENSION` 등) 정규화, `CadNumericalPolicy`로 수치 임계값 판정.
3. **04C**: [`CadOcsNormalizer.NormalizeEntity`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.Geometry.cs#L130)를 통해 모든 기하/주석 엔티티의 OCS/WCS 좌표 프레임 및 3D 점/벡터를 정규화(`LWPOLYLINE`의 경우 로컬 정점과 OCS 프레임을 추출하여 bulge 호 보간 후 [`CadOcsFrameMath.ProjectOcsPoint`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.Geometry.cs#L421)로 투영).
4. **04D / 04E**: [`CadOccurrenceExpander.Expand`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.cs#L65-L71)를 통해 BLOCK 정의 카탈로그와 루트/중첩 `INSERT`/`MINSERT` Occurrence 트리 및 누적 아핀 변환 행렬을 전개.
5. **04F**: [`CadStyleInheritanceResolver.Resolve`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.cs#L72)를 통해 Layer0 / `ByLayer` / `ByBlock` 상속이 완료된 유효 스타일을 해석.

---

## 7. `ICadNormalizer` Implementation Audit

- [`CadNormalizer`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.cs#L12)는 `public sealed partial class CadNormalizer : ICadNormalizer`로 구현되었으며, 어셈블리 내 유일한 `ICadNormalizer` 구현체입니다.
- [`ValidateOptions`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.cs#L218-L249)를 통해 `snapshot`, `options`, `options.Profile`의 null 여부와 수치·확장 한도의 유효성(NaN, Infinity, 0 이하 한도 등)을 엄격히 검증하고, 부적합한 옵션에 대해 `ArgumentOutOfRangeException`을 즉시 발생시킵니다.

---

## 8. Source-Template vs Projected-Leaf Audit

BLOCK 정의 내부의 자식 엔티티와 Occurrence 투영 엔티티의 분리가 아키텍처 승인안대로 정확히 구현되었습니다:
- **Source-Template `CadEntity`**:
  - BLOCK 내부의 지원 기하 엔티티는 원본 `EntityId` (`cad2:entity:*`)를 가지며, 선언된 스타일(`DeclaredStyle`)의 `StyleId`와 공유 `GeometryId`를 참조합니다([`CadNormalizer.Geometry.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.Geometry.cs#L65-L94)).
  - 해당 `EntityId`는 [`CadBlockDefinition.ChildEntityIds`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.cs#L106-L119)에 등록되지만, `ModelSpace`에는 직접 등록되지 않습니다.
- **Projected-Leaf `CadEntity`**:
  - 각 `CadBlockOccurrence`에 의해 투영되는 리프 기하 엔티티는 [`CreateProjectedLeafEntity`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.Geometry.cs#L97-L119)를 통해 생성됩니다.
  - `EntityId`는 `cad2:leaf:*` 도메인을 사용하며, `GeometryId`는 Source-Template과 **동일한 공유 `GeometryId`**를 가리키고, `StyleId`는 04F가 계산한 해당 Occurrence 문맥의 유효 스타일(`EffectiveStyle`)을 가리킵니다.
  - `Evidence.SourceRecordIds`는 원본 템플릿 엔티티의 `SourceRecordIds`에 Occurrence 경로상의 모든 `INSERT`/`MINSERT` `SourceRecordId`들을 정렬 병합하여 추적성을 완벽히 보존합니다.

---

## 9. Root Entity Representation Audit

- Model-space 루트에 직접 배치된 지원 기하 엔티티(`owner.IsBlockChild == false`)는 [`CadNormalizer.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.cs#L172-L176)에서 단일 `CadEntity`(`cad2:entity:*`)로 생성되며, 04F 루트 유효 스타일을 부여받고 `new CadModelSpaceEntityRef(built.Entity.EntityId, null)`로 `ModelSpace`에 직접 등록됩니다.
- 불필요한 가짜 Occurrence나 중복 `cad2:leaf:*` 엔티티를 생성하지 않습니다.

---

## 10. `cad2:leaf:` Identity Audit

- [`CadAssemblyIdentity.LeafId(string sourceEntityId, string occurrenceId)`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/AssemblySupport.cs#L58-L70)는 정확히 승인된 레시피인 `CadDeterministicId.Create(CadIdentityDomain.Leaf, [CadIdentityToken.Text("source_entity_id", sourceEntityId), CadIdentityToken.Text("occurrence_id", occurrenceId)])`를 사용합니다.
- `sourceEntityId`는 `cad2:entity:` 접두사를, `occurrenceId`는 `cad2:occurrence:` 접두사를 강제 검증하여 잘못된 ID 조합이 유입될 수 없습니다.

---

## 11. Shared `CadGeometry` Audit

- BLOCK 내부 엔티티가 여러 `INSERT` 또는 `MINSERT` 셀을 통해 $N$회 인스턴스화되더라도, `CadGeometry`(및 `LWPOLYLINE`의 세그먼트 `CadGeometry`들)는 BLOCK 정의 엔티티 정규화 시 단 1회만 생성됩니다([`CadNormalizer.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.cs#L121-L169)).
- 모든 Projected-Leaf `CadEntity`는 `template.GeometryId`를 그대로 공유 참조하므로, Occurrence마다 기하 레코드가 복제되지 않습니다.
- Controlled Source에서도 26개의 Projected-Leaf 엔티티가 추가되었음에도 `Geometry` 카탈로그는 공유 정의 기하만 포함(총 149개 = 89개 소스 기하 + 60개 폴리라인 세그먼트)하는 것이 확인되었습니다.

---

## 12. `ModelSpace` Contract Audit

- `CadDocument.ModelSpace`의 모든 항목(`CadModelSpaceEntityRef`)은:
  - 루트 엔티티인 경우: `(EntityId: "cad2:entity:...", OccurrenceId: null)`
  - Occurrence 투영 엔티티인 경우: `(EntityId: "cad2:leaf:...", OccurrenceId: "cad2:occurrence:...")`
- 모든 `ModelSpace[i].EntityId`는 `CadDocument.Entities`에 존재하고, 그 `Entity.StyleId`는 해당 문맥(루트 또는 해당 Occurrence)의 04F 유효 스타일과 정확히 일치합니다.
- [`CadCanonicalOrdering.OrderModelSpace`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.cs#L209)에 의해 `(EntityId, OccurrenceId)` 기준으로 결정론적 정렬됩니다.

---

## 13. Supported Geometry Audit (`LINE` / `POINT` / `CIRCLE` / `ARC` / `LWPOLYLINE` / `SOLID`)

[`CadNormalizer.Geometry.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.Geometry.cs)에서 6종의 지원 기하 엔티티 변환을 전수 확인했습니다:
1. **`LINE`** ([`BuildLine`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.Geometry.cs#L211-L246)): 04C WCS `start`/`end` 점 추출 후 `policy.SamePoint(start, end)`이면 `CAD_ZERO_LENGTH_LINE`과 함께 `UnsupportedNormalization`으로 거부(조용한 치유 금지). 유효 시 `CadLine` 생성.
2. **`POINT`** ([`BuildPoint`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.Geometry.cs#L248-L273)): 04C WCS `location` 점 추출 후 `CadPoint` 생성.
3. **`CIRCLE`** ([`BuildCircle`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.Geometry.cs#L275-L303)): 04C `center` 및 OCS 프레임(`XAxis`, `YAxis`, `Normal`)과 04B `radius` 추출 후 `CadCircle` 생성.
4. **`ARC`** ([`BuildArc`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.Geometry.cs#L305-L361)): 04C `center`/OCS 프레임, 04B `radius` 및 `start_angle`(`Code 50`), `end_angle`(`Code 51`) 추출 후 `CadAngleNormalizer.NormalizeDirectedSweepRadians(start, end, true, policy)`로 양의 방향($+Z_{ocs}$ 기준 CCW) 스윕을 계산하여 `CadArc` 생성.
5. **`LWPOLYLINE`** ([`BuildLwPolyline`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.Geometry.cs#L363-L527)):
   - 정점 수 $< 2$이면 `CAD_DEGENERATE_POLYLINE` 거부.
   - `constant_width` 또는 정점 폭(`start_width`/`end_width`)이 비영(non-zero)이면 기하를 생성하되 `CAD_POLYLINE_WIDTH_NOT_MATERIALIZED` 진단과 함께 `PartiallyNormalized`로 격하(정보 은폐 금지).
   - 각 세그먼트에 대해 `policy.SamePoint(v0, v1)`이면 `CAD_DEGENERATE_POLYLINE_SEGMENT`로 엔티티 전체 거부(조용한 정점 병합 금지).
   - `bulge == 0`(또는 `policy.IsZeroBulge(bulge)`)이면 직선 세그먼트(`CadLine`), 아니면 호 세그먼트(`CadArc`)를 자식 `CadGeometry`로 생성하고 `CadPolyline(SegmentGeometryIds, IsClosed)`로 조립.
6. **`SOLID`** ([`BuildSolid`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.Geometry.cs#L529-L598)):
   - DXF `SOLID` 교차 정점 순서 $(c_0, c_1, c_2, c_3)$에 대해 삼각형 폴딩(`SamePoint(c2, c3)`일 때 $[c_0, c_1, c_2]$) 또는 사각형 외곽 순서 $[c_0, c_1, c_3, c_2]$를 결정론적으로 구성하고, 연속/비연속 중복 정점 또는 공선(collinear) 면적 퇴화(`areaSquared <= (minEdge * policy.LengthMm)^2`)를 검사하여 유효한 경우에만 `CadPlanarFace`를 생성합니다.

---

## 14. Bulge Mathematics & Endpoint Verification Audit

[`CadBulgeGeometry.Create`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.Geometry.cs#L823-L936)의 수학적 정확성을 검증했습니다:
- 현(chord) 벡터 $(x, y)$와 현 길이 $L = \operatorname{hypot}(x, y)$에 대해:
  - 포함각: $\Delta\theta = 4\arctan(b)$ (`Math.Abs(sweep) <= policy.AngleRad` 또는 `>= TwoPi - policy.AngleRad`이면 거부)
  - 반지름: $R = \frac{L}{4}\left(|b| + \frac{1}{|b|}\right)$ (`!double.IsFinite(radius)` 또는 `<= policy.LengthMm`이면 거부)
  - 중심 오프셋: $\text{offset} = \frac{L}{4}\left(\frac{1}{b} - b\right)$를 OCS 좌측 단위 법선 $\mathbf{n}_{\text{left}} = \frac{-y\,\mathbf{u} + x\,\mathbf{v}}{L}$ 방향으로 중점 $\mathbf{m}$에 더해 중심 $\mathbf{c}$를 계산.
  - 시작각/종료각: $\theta_0 = \operatorname{atan2}((\mathbf{p}_0 - \mathbf{c})\cdot\mathbf{v}, (\mathbf{p}_0 - \mathbf{c})\cdot\mathbf{u})$, $\theta_1 = \operatorname{atan2}((\mathbf{p}_1 - \mathbf{c})\cdot\mathbf{v}, (\mathbf{p}_1 - \mathbf{c})\cdot\mathbf{u})$.
  - **양 끝점 재구성 검증**: 계산된 $(\mathbf{c}, R, \theta_0, \Delta\theta)$로부터 시작점과 끝점을 다시 평가(`EvaluateArcPoint`)하여 원래 정점 $\mathbf{p}_0, \mathbf{p}_1$과의 거리가 `policy.LengthMm` 이내인지 검증합니다([`CadNormalizer.Geometry.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.Geometry.cs#L891-L905)). 이로 인해 `bulge = 1e-300`처럼 비정상적인 미세 bulge가 직선으로 조용히 스냅되거나 부정확한 큰 반지름 호를 만드는 현상을 원천 차단합니다.

---

## 15. Non-Uniform Scale & Affine Behavior Audit

- 공유 `CadGeometry`는 정의 공간(WCS/정의 로컬 mm)에서 정규화되며, 비균등 스케일(`ScaleX != ScaleY`)이나 회전+비균등 스케일이 결합된 `INSERT`/`MINSERT` 변환은 `CadBlockOccurrence.AccumulatedTransform`에 정확히 보존됩니다.
- `CIRCLE`이나 `ARC`가 비균등 스케일 Occurrence를 통해 배치될 때 이를 다각형으로 임의 근사(tessellation)하거나 조용히 버리지 않으며, [`CadBoundsQuery`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadBoundsQuery.cs#L244-L321)에서 아핀 변환된 타원 매개변수 방정식 $\mathbf{p}(\theta) = \mathbf{c}' + \mathbf{a}\cos\theta + \mathbf{b}\sin\theta$의 해석적 극값(analytic extrema)을 통해 정확한 경계 상자를 계산합니다.

---

## 16. Orientation Sign Exact Rational Audit

[`CadOrientationValidation`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/AssemblySupport.cs#L121-L263)의 구현은 수치해석적으로 매우 훌륭합니다:
- IEEE 754 배정밀도 부동소수점(`double`)의 모든 유한값은 정확한 이진 유리수(dyadic rational) $m \cdot 2^e$ ($m \in \mathbb{Z}, e \in \mathbb{Z}$)입니다.
- [`ToDyadic(double value)`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/AssemblySupport.cs#L230-L262)는 정규수(normal)와 비정규수(subnormal) 모두를 비트 수준에서 분해하여 `BigInteger` 가수와 2의 거듭제곱 지수로 정확히 변환합니다.
- [`Sign(CadMatrix4 transform)`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/AssemblySupport.cs#L183-L220)는 $3 \times 3$ 선형 블록의 각 행마다 최소 지수 $e_{\min, r}$를 기준으로 `BigInteger` 왼쪽 시프트를 수행하여 양의 스케일 인자 $2^{-e_{\min, r}} > 0$만 묶어낸 뒤, 순수 `BigInteger` 연산만으로 $3 \times 3$ 행렬식 $\det(M)$을 계산합니다.
- 따라서 부동소수점 오버플로우(`1e150`), 언더플로우(`1e-150`), 또는 거의 평행한 열 벡터 간의 유효숫자 상쇄(catastrophic cancellation)가 **전혀 발생하지 않으며**, 행렬식의 부호(`-1`, `0`, `+1`)가 수학적 참값과 100% 일치합니다.
- 04E 런타임 `CadExpandedOccurrence.LocalOrientationSign` / `AccumulatedOrientationSign`과 대조하여 불일치 시 `CAD_ORIENTATION_INVARIANT` 오류를, 특이 행렬(`Sign == 0`) 시 `CAD_SINGULAR_STORED_TRANSFORM` 경고 및 `PartiallyNormalized` 격하를 수행합니다.

---

## 17. Annotation Contract Audit (`TEXT` / `MTEXT` / `DIMENSION` / `ATTRIB` / `ATTDEF`)

- **`TEXT` / `MTEXT` / `DIMENSION`** ([`BuildAnnotation`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.Geometry.cs#L600-L751)):
  - 가짜 기하(`CadGeometry`)나 `CadEntity`로 변환하지 않고, `CadDocument.Annotations`(`CadAnnotationRecord`)에 보존합니다.
  - 04C WCS 기준점(`Anchor`), 04B 텍스트 높이(`HeightMm`), 회전각(`RotationRadians`), 텍스트 값(`TextValue`) 및 `CadAnnotationPlacement`(정의 스코프, 소유자 핸들, 04F 선언 스타일 ID, 추가 기준점 사전 `Points`, 원시 코드/값 보존 `PreservedAttributes`)를 모두 유지합니다.
- **`ATTRIB` / `ATTDEF`**:
  - 기하로 위장하지 않으며, [`NormalizeUnsupportedEntity`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.Geometry.cs#L753-L820)를 통해 `CAD_ATTRIBUTE_ENTITY_DEFERRED` 진단과 함께 `UnsupportedNormalization` 상태로 원장(Ledger) 및 Source/Facts에 100% 보존됩니다.

---

## 18. Analytic Bounds Engine Audit

[`CadBoundsQuery`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadBoundsQuery.cs)는 다음 5가지 정적 질의 메서드를 제공합니다:
1. `GeometryLocal(CadGeometry, CadDocument, CadNumericalPolicy?)`
2. `GeometryTransformed(CadGeometry, CadMatrix4, CadDocument, CadNumericalPolicy?)`
3. `Definition(string definitionId, CadDocument, CadNumericalPolicy?)`
4. `Occurrence(string occurrenceId, CadDocument, CadNumericalPolicy?)`
5. `Model(CadDocument, CadNumericalPolicy?)`

- **원의 아핀 변환 경계** ([`CircleBounds`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadBoundsQuery.cs#L244-L263)): 중심 $\mathbf{c}' = T(\mathbf{c})$, 축 벡터 $\mathbf{a} = L(R\mathbf{u})$, $\mathbf{b} = L(R\mathbf{v})$에 대해 각 좌표축 $i \in \{X, Y, Z\}$의 반폭을 $\operatorname{hypot}(a_i, b_i)$로 정확히 계산합니다.
- **호의 아핀 변환 경계** ([`ArcBounds`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadBoundsQuery.cs#L265-L321)): 양 끝점 $\theta_0, \theta_0 + \Delta\theta$를 포함하고, 각 좌표축별 정류점(stationary angles) $\theta^* = \operatorname{atan2}(b_i, a_i)$ 및 $\theta^* + \pi$가 유향 스윕 구간 내에 속하는지 [`ContainsSweepAngle`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadBoundsQuery.cs#L323-L345)로 판정하여 극값을 정확히 계산합니다.
- 주석 전용이거나 기하가 없는 경우에는 `CadBoundsState.Empty`를 반환하며 가짜 `[0,0,0]` 박스를 만들지 않습니다.

---

## 19. Header Extents Isolation Audit

- `CadBoundsQuery`의 모든 메서드는 오직 `CadDocument`만을 인자로 받으며 `RawDrawingSnapshot`이나 `$EXTMIN`, `$EXTMAX`, `$LIMMIN`, `$LIMMAX` 헤더 변수에 접근조차 할 수 없습니다.
- [`Cad04GGeometryAndBoundsTests.HeaderExtents_WhenContradictory_AreIgnoredByAnalyticBounds`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/tests/DtoB.Tests/Cad04GGeometryAndBoundsTests.cs#L232-L254)에서 모순된 `$EXTMIN=(-999999,-999999,0)`, `$EXTMAX=(999999,999999,0)` 헤더가 주어져도 실제 기하 경계 `[10..30, 20..50, 0..0]`만 정확히 반환됨을 검증했습니다.

---

## 20. Effective vs Declared Style Assembly Audit

[`CadNormalizer.Styles.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.Styles.cs)를 확인한 결과:
- **선언 스타일(`DeclaredStyle`)**: BLOCK 정의 내부의 Source-Template `CadEntity` 및 `CadAnnotationPlacement.StyleId`에 할당되어 원본 선언(`0` 레이어, `ByBlock`, `ByLayer` 등)을 그대로 보존합니다.
- **유효 스타일(`EffectiveStyle`)**: 04F `CadEffectiveStyleSelection`으로부터 생성되며, 루트 엔티티와 Projected-Leaf `CadEntity`, 그리고 `CadBlockOccurrence.EffectiveStyleId`에 할당됩니다.
- 두 스타일 모두 `CadIdentityDomain.Style` 결정론적 토큰으로부터 ID를 생성하고 중복 제거되어 `CadDocument.Styles` 카탈로그에 정렬 수록됩니다.

---

## 21. `CadNormalizationLedger` Total Accounting Audit

[`BuildLedgerAndOrderSource`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.cs#L430-L509)를 검증한 결과:
- 모든 `CadSourceRecord`는 초기 상태(`Section == HEADER || TABLES || BLOCKS`이면 `NotApplicable`, `ENTITIES`이면 `UnsupportedNormalization`)에서 출발하며, 각 정규화 단계를 거치며 [`PromoteDisposition`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.cs#L511-L529)에 의해 최종 상태로 결정됩니다.
- 원장(`CadNormalizationLedger`)은 최종 `Source` 레코드들로부터 직접 집계되므로 항상 다음 항등식을 만족합니다:
  $$\text{TotalSourceRecordCount} = |\text{Source}| = \text{Normalized} + \text{PartiallyNormalized} + \text{Unsupported} + \text{NotApplicable}$$
- Controlled Source 검증: $333 = 117 + 65 + 52 + 99$.

---

## 22. Unsupported / Partial / Raw Diagnostic Preservation Audit

- **Raw 진단 보존**: `snapshot.Diagnostics`의 모든 항목은 [`BuildRawDiagnostics`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.cs#L397-L428)를 통해 `CadDocument.RawDiagnostics`에 무손실 보존되며, 연결된 엔티티가 있는 경우 해당 엔티티의 정규화 상태를 `PartiallyNormalized`로 격하합니다.
- **미지원 엔티티 보존**: `HATCH`, `SPLINE`, `ELLIPSE`, `LEADER`, `MLEADER`, `POLYLINE`, `VERTEX`, `SEQEND`, `3DFACE`, `VIEWPORT`, `ATTRIB`, `ATTDEF` 및 알 수 없는 엔티티는 조용히 버려지지 않고 명시적인 진단 코드(`CAD_UNSUPPORTED_ENTITY`, `CAD_VIEWPORT_NOT_NORMALIZED`, `CAD_ATTRIBUTE_ENTITY_DEFERRED`)와 함께 `Source`, `Facts`, `Diagnostics`, `Ledger`에 모두 기록됩니다.

---

## 23. Paper-Space / Viewport Policy Audit

- [`IsPaperSpace`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/CadNormalizer.Geometry.cs#L194-L209)는 엔티티의 `IsPaperSpace (Code 67 != 0)` 또는 `*Paper_Space*` 소유자 블록 이름을 검사합니다.
- Paper-space 엔티티는 `ModelSpace` 기하 또는 `Annotations`로 절대 혼합되지 않으며, `CAD_PAPER_SPACE_ENTITY_SKIPPED` 진단 및 `UnsupportedNormalization` 상태로 격리 보존됩니다.

---

## 24. Materialized Resource Budget Audit (`249999 / 250000 / 250001`)

- [`CadMaterializationBudget`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/packages/DtoB.Cad.Normalize/Assembly/AssemblySupport.cs#L7-L55)는 ADR-020("250,000 materialized derived geometry/leaf entries session-wide; includes references per occurrence, not just shared definitions") 및 Plan §17의 명세에 따라 기본 한도 `250,000`을 적용합니다.
- 공유/루트 기하 레코드, 폴리라인 컨테이너, 폴리라인 세그먼트 기하, 그리고 Occurrence별 Projected-Leaf 엔티티 구체화 시마다 1 슬롯을 차감하며, 한도 초과 시 `CAD_MAX_MATERIALIZED_GEOMETRY_EXCEEDED` 진단을 남기고 결정론적으로 중단합니다.
- [`Cad04GGeometryBudgetBoundaryTests.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/tests/DtoB.Tests/Cad04GGeometryBudgetBoundaryTests.cs)에서 커스텀 한도뿐만 아니라 실제 기본 한도인 **249,999 (성공) / 250,000 (성공) / 250,001 (초과 진단 및 마지막 1개 거부)** 경계가 엔드투엔드로 검증되었습니다.

---

## 25. Controlled Source End-to-End Audit

Controlled Source (`architectural_example-imperial.dxf`) 정규화 결과를 직접 대조 검증했습니다:
- **Source Records**: `333` (`117` Normalized + `65` PartiallyNormalized + `52` UnsupportedNormalization + `99` NotApplicable = `333`)
- **Facts**: `5,535`
- **Layers**: `12`
- **Styles**: `201` (선언 스타일 + INSERT 컨텍스트 스타일 + 리프 유효 스타일 중복 제거)
- **Definitions**: `12` (`ChildEntityIds`에 템플릿 기하 엔티티 채워짐)
- **Occurrences**: `17`
- **Geometry**: `149` (89개 소스 프리미티브/폴리라인 + 60개 폴리라인 세그먼트; 이 중 `LINE` 119개 = 소스 `LINE` 59개 + 폴리라인 직선 세그먼트 60개)
- **Entities**: `184` (158개 소스 엔티티 + 26개 Projected-Leaf 엔티티)
- **ModelSpace**: `123` (97개 루트 엔티티 `OccurrenceId == null` + 26개 리프 엔티티 `OccurrenceId != null`)
- **Annotations**: `44` (`TEXT` 21 + `MTEXT` 3 + `DIMENSION` 20)
- **Diagnostics**: `117`, **RawDiagnostics**: `59`
- **Model Analytic Bounds**: `Min = (-1300.225, -10100, 0)`, `Max = (20700, 8500, 0)` (원시 헤더 extents와 독립적으로 실제 기하와 일치)
- **Canonical JSON SHA-256**: `8A5E078ADB9990C7127B7E626DEC1333071F481FA4A4B7F91CD17D23505F4F24` (`2,233,701` bytes)

---

## 26. Modified Existing Tests Audit

수정된 기존 테스트 파일 5개를 모두 검사했습니다:
1. [`CadNormalizationBoundaryTests.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/tests/DtoB.Tests/CadNormalizationBoundaryTests.cs#L156-L163)
2. [`Cad04BPolicyAndProvenanceTests.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/tests/DtoB.Tests/Cad04BPolicyAndProvenanceTests.cs#L176-L185)
3. [`Cad04CArchitectureAndDeterminismTests.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/tests/DtoB.Tests/Cad04CArchitectureAndDeterminismTests.cs#L22-L29)
4. [`Cad04DArchitectureAndDeterminismTests.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/tests/DtoB.Tests/Cad04DArchitectureAndDeterminismTests.cs#L24-L31)
5. [`CadOccurrenceProvenanceTests.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/tests/DtoB.Tests/CadOccurrenceProvenanceTests.cs#L186-L193)

- 5개 파일 모두 `04A`~`04E` 단계에서 "04G 전까지 `ICadNormalizer` 구체 클래스가 없어야 한다(`Assert.Empty(implementations)`)"고 막아두었던 리플렉션 가드 1줄만 `Assert.Single(implementations, typeof(CadNormalizer))`로 갱신하였으며, 테스트 이름, 테스트 수(1,258개), 그리고 다른 모든 어서션은 그대로 보존되었습니다.

---

## 27. New 04G Test Quality Audit

신규 추가된 5개 테스트 파일(총 57개 Fact/Theory 케이스)은 표면적인 통과가 아니라 수학적 오라클과 경계 조건을 정밀하게 검증합니다:
- [`Cad04GAssemblyTests.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/tests/DtoB.Tests/Cad04GAssemblyTests.cs): Source-Template vs Projected-Leaf 계약, `cad2:leaf:` ID 레시피, 중첩 발생 및 `MINSERT` 셀 투영, 공유 기하 불변성, `SOLID` 삼각형/사각형 폴딩 및 퇴화 거부, 주석/속성/뷰포트/페이퍼 공간 처리, 원장 합계 불변성 검증.
- [`Cad04GGeometryAndBoundsTests.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/tests/DtoB.Tests/Cad04GGeometryAndBoundsTests.cs): Bulge 양/음/반원/초과각($270^\circ$) 수기 오라클, `1e-300` 미세 bulge 거부, $R_{45^\circ} S(2,3)$ 아핀 타원 및 사분원 호의 해석적 극값($\pm\sqrt{26}$, $10-3\sqrt{2}$ 등) 검증, 모순된 헤더 extents 무시, `BigInteger` 이진 유리수 행렬식의 극단적 스케일(`1e150`/`1e-150`) 및 근접 특이 행렬 부호 검증.
- [`Cad04GEvidenceTests.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/tests/DtoB.Tests/Cad04GEvidenceTests.cs): Controlled Source 전수 카운트, JSON 왕복 직렬화, 컬처(`Invariant`, `fr-FR`, `ko-KR`) 불변성, 입력 엔티티/레이어/블록/테이블 역순 셔플 시 바이트 단위 동일성 검증.
- [`Cad04GIntegrationEdgeTests.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/tests/DtoB.Tests/Cad04GIntegrationEdgeTests.cs): 옵션 유효성 검증, OCS 기울임($Z_{ocs}=(0,1,0)$) 폴리라인 bulge 투영, 미러(`ScaleX < 0`) `MINSERT` 원호 경계 및 방향 부호 검증, 순환/깊이/셀/세션 한도 초과 시 조립 무결성 검증.
- [`Cad04GGeometryBudgetBoundaryTests.cs`](file:///d:/%23%23%ED%99%8D%EB%AF%BC%EC%9A%B0/02.%20%ED%94%84%EB%A1%9C%EA%B7%B8%EB%9E%A8/09.%20Codex/%23%23DtoB/DtoB_Repo/tests/DtoB.Tests/Cad04GGeometryBudgetBoundaryTests.cs): `249,999 / 250,000 / 250,001` 리소스 예산 경계 검증.

---

## 28. Prior Stage Evidence Preservation Audit

`Audit-04G.ps1` 실행 결과, `04A`~`04F`의 모든 증거 아티팩트 SHA-256 다이제스트가 100% 동일하게 유지됨을 확인했습니다:
- **04A (`contract-invariant.json` / `fr-FR` / `ko-KR`)**: `3E49A200C8AD688F6F53842F17556D849979B1C8F3F6A083438721CA02F58EBC`
- **04B (`scalar-contract-evidence.json`)**: `568D2A7C28863360A4839FF0CA59DF09C1E7C9066EC3A487701872506A68D4C3`
- **04C (`coordinate-evidence.json`)**: `81CA2010F120BD65888EF57AABE5A2F55E230DDC291276E1CBA8BAC9584F0342`
- **04D (`local-transform-evidence.json`)**: `02045C202CACF9DA66406827E297435CCCB7448D1D0A38C7B52E7FCAAA3A0EE5`
- **04E (`occurrence-expansion-evidence.json`)**: `143684A78B37AA4962345A28FC8C268E24CB4ECDCDF2EBD410B66EC99A1B626B`
- **04F (`effective-style-evidence.json`)**: `7A448C95EC2F67DCA6C65C3D60B4982E79312814C8C1FD8E5A62C55C611FFC01`

---

## 29. Determinism & Canonical Digest Audit

- Controlled Source를 대상으로 3개의 독립 프로세스(`release`, `process-a`, `process-b`) × 3개의 컬처(`Invariant`, `fr-FR`, `ko-KR`) = 총 9회 정규화 및 직렬화를 수행한 결과, 모든 실행이 정확히 `2,233,701` 바이트 및 SHA-256 `8A5E078ADB9990C7127B7E626DEC1333071F481FA4A4B7F91CD17D23505F4F24`를 산출했습니다.
- 입력 스냅샷의 테이블, 블록, 엔티티, 정점 외 팩트 순서를 역순으로 뒤섞어도 동일한 `CadDocument` JSON 바이트가 생성됨이 테스트로 입증되었습니다.

---

## 30. Revit Independence Audit

- `packages/DtoB.Cad.Normalize/DtoB.Cad.Normalize.csproj`는 오직 `DtoB.Cad`와 `DtoB.Cad.Raw`만 참조하며, Revit API(`RevitAPI.dll`, `RevitAPIUI.dll`)나 `DtoB.Revit`에 대한 참조가 전혀 없습니다.
- 내부 정규화 단위는 밀리미터(`mm`)와 라디안(`rad`)으로 고정되어 있으며, Revit 내부 단위(`feet`) 변환 코드는 존재하지 않습니다.

---

## 31. ADR-018 Compliance Matrix

| ADR-018 요구사항 | 준수 여부 | 근거 |
|---|---|---|
| Raw snapshot 불변성 및 3계층 경계(`Raw -> Normalize -> CadDocument`) | **PASS** | `RawDrawingSnapshot` 무수정 입력, `CadDocument (schemaVersion = 2)` 출력 |
| 모든 Source Record의 4분류 Disposition 전수 회계 | **PASS** | `CadNormalizationLedger`에서 누락 없이 전수 집계 (`333 = 117 + 65 + 52 + 99`) |
| 지원/미지원/부분 정규화 및 Paper-space 명시적 진단 기록 | **PASS** | 조용한 누락 없이 `CadNormalizationDiagnostic` 및 `RawDiagnostics` 보존 |
| `TEXT`/`MTEXT`/`DIMENSION` 주석 채널 보존 | **PASS** | `CadDocument.Annotations`에 배치 및 속성/점 사전 보존 |

---

## 32. ADR-019 Compliance Matrix

| ADR-019 요구사항 | 준수 여부 | 근거 |
|---|---|---|
| 내부 단위 `mm` 및 각도 `rad` 일원화 | **PASS** | 04B/04C를 거쳐 모든 기하/주석 스칼라 및 좌표가 `mm`/`rad`로 정규화됨 |
| 중앙 집중식 `CadNumericalPolicy` 사용 | **PASS** | 매직 넘버 없이 `policy.LengthMm`, `policy.AngleRad`, `policy.BulgeZero`만 사용 |
| `LWPOLYLINE` bulge 호 변환 및 퇴화 세그먼트 거부 | **PASS** | `CadBulgeGeometry`의 해석적 호 변환 + 끝점 재검증 + 퇴화 세그먼트 즉시 거부 |
| 원시 헤더 extents 독립적 해석적 경계 계산 | **PASS** | `CadBoundsQuery`가 원/호 아핀 변환의 해석적 극값을 직접 계산 |

---

## 33. ADR-020 Compliance Matrix

| ADR-020 요구사항 | 준수 여부 | 근거 |
|---|---|---|
| 공유 `CadBlockDefinition` 및 공유 `CadGeometry` 보존 | **PASS** | 블록 정의와 기하 레코드는 1회만 생성되어 모든 Occurrence에서 공유됨 |
| Source-Template vs Projected-Leaf (`cad2:leaf:*`) 분리 및 `ModelSpace` 참조 | **PASS** | 블록 내부 엔티티는 템플릿으로, 투영 리프는 `cad2:leaf:`로 `ModelSpace`에 등록 |
| Occurrence별 04F 유효 스타일 연결 | **PASS** | `CadEntity(leaf).StyleId`가 해당 Occurrence 문맥의 04F 유효 스타일을 참조 |
| 세션 전체 구체화 리소스 예산(`250,000`) 강제 | **PASS** | `CadMaterializationBudget`에서 기하 및 리프 구체화 시 카운트 및 `250,000` 한도 강제 |
| 변환 행렬 방향 부호(`OrientationSign`) 검증 | **PASS** | `CadOrientationValidation`에서 `BigInteger` 이진 유리수 행렬식으로 무오차 검증 |

---

## 34. BIMBraid Rules 1–15 Matrix

| 규칙 | 내용 | 준수 여부 |
|---|---|---|
| Rule 1 | CAD Parser를 Revit API와 직접 결합하지 않는다 | **PASS** |
| Rule 2 | BIM IR을 도메인 Source of Truth로 사용한다 (04G는 CAD IR 계층에 국한) | **PASS** |
| Rule 3 | 모든 Semantic Prediction은 `confidence`와 `evidence`를 가진다 (04G는 결정론적 CAD 정규화만 수행하며 출처 `Evidence`를 보존) | **PASS** |
| Rule 4 | Geometry 계산은 deterministic code가 담당한다 | **PASS** |
| Rule 5 | LLM 출력이 직접 Revit Element를 생성하게 하지 않는다 | **PASS** |
| Rule 6 | DWG/DXF의 원본 `handle` 또는 equivalent source identity를 보존한다 | **PASS** |
| Rule 7 | Coordinate Transform에는 반드시 테스트를 둔다 | **PASS** |
| Rule 8 | Unsupported Entity를 조용히 버리지 않는다 | **PASS** |
| Rule 9 | 지원하지 않는 경우 명시적으로 로그와 이슈(진단)를 남긴다 | **PASS** |
| Rule 10 | 새 기능에는 최소 1개의 fixture 또는 regression test가 필요하다 (57개 신규 테스트 추가) | **PASS** |
| Rule 11 | Revit Transaction은 기능 그룹별로 분리한다 (해당 없음 — Revit 독립 계층) | **PASS** |
| Rule 12 | 단위 변환은 Adapter 경계에서 명시적으로 수행한다 | **PASS** |
| Rule 13 | 임의의 tolerance hard coding을 금지한다 (`CadNumericalPolicy` 중앙 관리) | **PASS** |
| Rule 14 | Schema major change는 ADR 없이 수행하지 않는다 (`schemaVersion = 2` 동결 유지) | **PASS** |
| Rule 15 | 프로젝트 전체를 한 번에 리팩터링하지 않는다 (04G `Assembly/` 범위로 엄격히 제한) | **PASS** |

---

## 35. BLOCKER Issues

- **없음 (None)**

---

## 36. HIGH Issues

- **없음 (None)**

---

## 37. MEDIUM Issues

- **없음 (None)**

---

## 38. LOW Issues

- **없음 (None)**

---

## 39. Final Recommendation for PHASE 04H Entry

PHASE 04G (`Canonical Geometry & CAD v2 Document Assembly`)는 승인된 모든 아키텍처 결정(`ADR-018`, `ADR-019`, `ADR-020`, `PHASE_04G_ARCHITECTURE_PASS`)을 완벽하게 준수하여 구현되었으며, 전체 회귀 테스트 **1,315 / 1,315 PASS** 및 다중 프로세스/컬처 결정론적 다이제스트 검증을 통과했습니다.

따라서 PHASE 04G를 공식 승인하며, 다음 단계인 **PHASE 04H (Fixture Corpus, Golden Snapshots, Round-Trip & Release Gate)** 진입을 권고합니다.

**`PHASE_04G_PASS`**
