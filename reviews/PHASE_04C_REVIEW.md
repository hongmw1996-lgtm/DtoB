# PHASE 04C — OCS / WCS Coordinate Normalization Independent Implementation Review

## Executive Summary

- **Review Target**: PHASE 04C — OCS / WCS Coordinate Normalization (`packages/DtoB.Cad.Normalize/Coordinates/`, `tests/DtoB.Tests/Cad*04C*.cs`, `docs/status/PHASE_04C_REPORT.md`)
- **Review Date**: 2026-10-16
- **Review Mode**: Independent Architecture & Implementation Gate Review
- **Reviewer**: Architecture / Adversarial Review Owner (Antigravity)
- **Status of Codex**: `PHASE_04C_PASS_WITH_KNOWN_LIMITATIONS`
- **Final Verdict**: **`PHASE_04C_PASS`**

DtoB 시스템의 스칼라 해석 계층(단위, 길이 스칼라, 각도 역할, 수치 비교 정책; PHASE 04B) 구축 완료에 이어, CAD 정규화 엔진의 핵심 공간 좌표계 투영 계층인 **PHASE 04C OCS / WCS Coordinate Normalization**에 대해 독립적이고 적대적인(adversarial) 구현 감사를 수행하였습니다.

PHASE 04C의 주 목적은 `RawDrawingSnapshot`에 보존된 원본 좌표 사실(Raw coordinate facts)로부터:
1. 04B 단위/스칼라 해석 기반 치수 스케일링
2. 필드 수준의 WCS / OCS 좌표 공간 역할 분류
3. 압출 벡터(Extrusion normal)의 무차원 정규화 및 엄밀한 완전 부재 기본값($+Z$) 처리
4. Autodesk 임의 축 알고리즘(Arbitrary Axis Algorithm; AAA)에 따른 불변 OCS 정규직교 기저 구성
5. OCS 점/벡터의 WCS 투영 및 정규 밀리미터(mm) 좌표 산출
을 **PHASE 04D+의 블록 배치/인스턴스 전개/기하 조립으로의 누출 없이** 순수하고 결정론적으로 확립하는 것입니다.

독립 감사 결과:
1. **04C 스코프 경계 엄격 준수 (Zero Premature 04D+ Scope Leakage)**:
   - 블록 베이스 포인트($BASE) 원점 보정, INSERT T/R/S 복합 행렬 합성, INSERT 배치 `Transform3` 생성, 블록 정의 기하의 배치 변환, 부모/자식 인스턴스 전개, 중첩 INSERT / MINSERT 전개, 인스턴스 식별자 합성, ByLayer/ByBlock 속성 해석, Layer 0 계통 상속, Bulge 호 기하 생성, 공간 Circle/Arc 인스턴스 생성, 아핀 곡선 바운즈 계산, 전체 `CadDocument` 공간 조립, 도면 의미론 인식, BIM 속성 매핑, Revit API 바인딩 등 04D 이후의 일체의 배치 및 기하 합성 코드가 배제되었습니다.
2. **필드 수준의 엄밀한 좌표 공간 분류 (Field-Level WCS vs OCS)**:
   - 엔티티 단위가 아닌 **개별 필드 단위**로 WCS와 OCS 공간을 정밀하게 구분하여, 동일 엔티티 내에서도 서로 다른 좌표계를 갖는 복합 필드(예: DIMENSION의 정의점 10/13/14는 WCS, 텍스트 중간점 11은 OCS; MTEXT의 삽입점 10과 방향 벡터 11은 WCS)를 Autodesk DXF 사양에 100% 일치하도록 처리합니다.
3. **WCS 필드의 불변성 보장 (WCS Fields Stay WCS)**:
   - WCS 좌표 역할(`LineStartWcs`, `LineEndWcs`, `PointPositionWcs`, `MTextInsertionWcs`, `MTextDirectionWcs`, `DimensionDefinitionWcs` 등)은 압출 벡터가 존재하더라도 AAA 기저를 생성하거나 회전하지 않고, 오직 원본 유효성 검증 및 단위 길이 스케일링만 거칩니다.
4. **압출 벡터 정규화 및 미소 유한 법선 정책 (ADR-019 완전 준수)**:
   - $N$의 성분 중 최대 절댓값으로 사전 정규화(pre-scaling) 후 노름을 계산함으로써, 극단적인 유한 크기나 비정규 수(subnormal, down to `double.Epsilon`)에서도 부동소수점 오버플로/언더플로 없이 안전하게 단위 벡터를 도출합니다.
   - ADR-019 규정에 따라 엄격한 영벡터 $(0,0,0)$ 및 NaN/무한대는 즉시 거절(`InvalidExtrusionNormal`)하되, 미소 유한 크기 벡터에 대해 자의적인 엡실론 거절이나 스냅을 가하지 않습니다.
   - 오직 X/Y/Z가 완전히 매핑되고 진정으로 부재한 경우에만 결정론적 기본값 $+Z(0,0,1)$ 규칙(`DXF.OCS.Extrusion.DefaultPositiveZ`)을 부여하며, 부분 부재나 형식 결함은 엄격히 거절합니다.
5. **Autodesk 임의 축 알고리즘 (AAA) 수학적 무결성**:
   - $1/64$ 판별 조건은 공차(tolerance)가 아닌 사양 상수이며, $\text{strict } <$ (엄격한 미만) 조건을 만족합니다. $1/64$ 경계 비트(`BitDecrement`, exact, `BitIncrement`)에서 분기 거동이 검증되었습니다.
   - 외적 순서 $\text{WorldY} \times N$ 또는 $\text{WorldZ} \times N$ 및 $N \times Ax$가 정밀 구현되어 오른손 직교계($Ax \times Ay = N$, $\det(A) = 1$)를 완벽히 유지합니다.
   - 수작업 비축(Oblique) 법선 $N = (1,1,1)/\sqrt{3}$에 대한 WCS 투영점 `(-0.32955735700618094, 2.4988697677400094, 4.758890819541682)` 오라클이 정확히 재현되었습니다.
6. **점(Point)과 벡터(Vector)의 엄격한 의미론적 분리**:
   - 위치 점(`PointToWcs`, `PointMm`)과 방향/변위 벡터(`VectorToWcs`, `Vector`)를 별도 타입 및 메서드로 분리하여 익명 XYZ 변환에 의한 의미 훼손을 차단하였습니다.
   - 변위 벡터(`OcsDisplacementVector`)는 단위 스케일을 적용받지만, 방향 벡터(`OcsDirectionVector`, `MTextDirectionWcs`)는 무차원이며 단위 스케일을 적용하지 않고 크기를 원형 보존합니다.
7. **LWPOLYLINE 고도(Elevation) 단 1회 적용**:
   - LWPOLYLINE의 고도(코드 38)는 OCS Z좌표로 단 1회 결합된 후 단위 스케일 및 AAA 기저 변환을 거치며, WCS 변환 이후 중복 가산되지 않습니다. Z와 Elevation의 동시 공급 시 충돌 거절(`ConflictingCoordinateFacts`)을 수행합니다.
8. **회귀 검증, 다중 문화권 결정론 및 프로덕션 무결성**:
   - 기존 956개 테스트가 100% 보존된 상태에서 96개의 신규 04C 테스트가 추가되어 **총 1052개 중 1052개 전원 통과 (Pass 1052, Fail 0, Skip 0)**를 달성하였습니다.
   - 솔루션 릴리스 재빌드 결과 오류 0건, 승인된 Revit MSB3277 경고 3건 외 신규 경고 0건을 유지하였습니다.
   - Invariant, fr-FR, ko-KR 3대 로케일 및 2개의 독립 프로세스(`process-a`, `process-b`)에서 좌표 증거의 직렬화 SHA-256 해시(`81CA2010F120BD65888EF57AABE5A2F55E230DDC291276E1CBA8BAC9584F0342`)가 단 1비트의 편차도 없이 완벽히 일치함을 확인하였습니다.
   - 보호 대상인 `Cad.Raw`, `Cad.Dxf`, `Core`, `Geometry`, `Analysis`, `Desktop`, `Revit`, 86개 통제 픽스처는 0바이트 수정 없이 온전히 보존되었습니다.

모든 좌표계 정규화 요건 및 아키텍처 규칙이 완벽하게 충족되었으므로 최종 승인 판정(**`PHASE_04C_PASS`**)을 부여합니다.

---

## 1. Scope Audit & 28 Review Areas Detailed Assessment

### 1. 04C Scope Boundary & Zero 04D+ Leakage
- **검증 결과**: **적합 (PERFECT SCOPE ISOLATION)**
- `packages/DtoB.Cad.Normalize/Coordinates/` 디렉터리 내 모든 소스 코드를 전수 분석하였습니다.
- 블록 베이스 포인트($BASE) 원점 보정, INSERT T/R/S 합성, INSERT 배치 `Transform3`, 블록 정의 기하 배치, 인스턴스 전개(occurrence expansion), 중첩 INSERT / MINSERT 전개, ByLayer/ByBlock 속성 해석, Layer 0 상속, Bulge 호 기하 계산, 공간 Curve 생성, 아핀 바운즈 계산, 전체 `CadDocument` 공간 조립, 도면 의미론 인식, BIM 속성 매핑, Revit API 바인딩 등의 코드가 일절 포함되지 않았습니다.
- 04C는 순수하게 Raw 사실로부터 단위 결정에 기반한 좌표 스케일링, OCS $\rightarrow$ WCS 기저 투영, 불변 프레임 객체 생성만을 제공하는 순수 좌표 정규화 모듈로 엄밀히 한정되었습니다.

### 2. Actual Raw Coordinate Mapping
- **검증 결과**: **적합 (100% RAW FIDELITY)**
- LINE(`Start`, `End`), POINT(`Position`), LWPOLYLINE(`Vertices[i].X/Y`, `Elevation`), CIRCLE(`Center`), ARC(`Center`), SOLID(`First/Second/Third/Fourth`), TEXT/ATTRIB/ATTDEF(`Insertion`, `Alignment`), MTEXT(`Insertion`, `Direction`), INSERT(`Insertion`), DIMENSION(`DefinitionPoint`, `FirstExtensionPoint`, `SecondExtensionPoint`, `TextMidpoint`) 등 04C가 소비하는 모든 좌표 필드가 PHASE 03 Raw 정의에 실제로 존재하며, 보고된 시맨틱과 정확히 일치함을 확인하였습니다.
- 가공된 속성을 날조하거나, 원본 DXF 파일을 다시 열거나, 재파싱하거나, Raw 객체를 변조하는 코드가 전무합니다.

### 3. Coordinate-Role Table (Field-Level Space Classification)
- **검증 결과**: **적합 (SPECIFICATION COMPLIANT)**
- 엔티티 단위가 아닌 필드 수준에서 WCS와 OCS 공간을 정밀하게 분류하였습니다:
  - **WCS**: `LineStartWcs`, `LineEndWcs`, `PointPositionWcs`, `MTextInsertionWcs`, `MTextDirectionWcs`, `DimensionDefinitionWcs`, `DimensionFirstExtensionWcs`, `DimensionSecondExtensionWcs`
  - **OCS**: `LwPolylineVertexOcs`, `CircleCenterOcs`, `ArcCenterOcs`, `SolidFirstOcs`~`SolidFourthOcs`, `TextInsertionOcs`, `TextAlignmentOcs`, `InsertInsertionOcs`, `DimensionTextMidpointOcs`, `OcsDisplacementVector`, `OcsDirectionVector`
- 모든 분류가 Autodesk DXF 공식 사양과 100% 일치함을 독립적으로 대조 검증하였습니다.

### 4. WCS Handling (WCS Fields Stay WCS)
- **검증 결과**: **적합 (SATISFIED)**
- WCS 역할에 대해서는 AAA 기저 구성 및 법선 회전을 일절 적용하지 않으며, 프레임은 `null`로 유지되고 오직 단위 길이 스케일링만 수행됩니다.
- 엔티티에 비표준/기울어진 법선($N = (0, 1, 0)$ 등)이나 심지어 결함 있는 법선($N = (0, 0, 0)$)이 포함되어 있더라도, WCS 필드(예: `PointPositionWcs`, `MTextInsertionWcs`)의 출력 좌표는 회전되지 않고 원형 WCS 좌표를 엄격히 보존함을 확인하였습니다.

### 5. Extrusion Default Semantics
- **검증 결과**: **적합 (SATISFIED)**
- X, Y, Z 압출 성분이 완전히 매핑되고 진정으로 부재(`AllAbsent`)한 경우에만 결정론적 기본값 $+Z(0, 0, 1)$과 소명 규칙 `DXF.OCS.Extrusion.DefaultPositiveZ`를 부여합니다.
- 성분 중 일부만 부재하거나, 형식이 결함이 있거나, 매핑되지 않은 메타데이터인 경우에는 기본값을 날조하지 않고 `InvalidExtrusionNormal`로 정직하게 거절합니다.

### 6. Non-Unit Normal Normalization
- **검증 결과**: **적합 (NUMERICALLY ROBUST)**
- `CoordinateVectorMath.Unit(x, y, z)`는 성분 중 최대 절댓값 `maximum = max(|x|, max(|y|, |z|))`으로 사전 나눗셈을 수행한 후 노름 $\sqrt{x^2 + y^2 + z^2}$을 계산합니다.
- 이로 인해 $(0, 2, 0)$, $(0, -2, 0)$은 물론 극단적으로 거대한 수(`double.MaxValue`)나 비정규 수(`double.Epsilon`, `1e-300`)에서도 제곱 오버플로/언더플로 없이 완벽한 단위 벡터 $(0, 1, 0)$ 등이 도출됩니다.
- 법선 벡터는 무차원이며 단위 길이 스케일링이 일절 적용되지 않습니다.

### 7. Tiny Finite Normal Policy (ADR-019 Comparison)
- **검증 결과**: **적합 (COMPLIANT WITH ADR-019)**
- ADR-019(라인 53~54, 112)는 "Wholly absent normal defaults +Z... initial partial/zero/nonfinite normal is invalid... Tiny nonzero magnitude alone is not invalid" 및 "Robustly normalize arbitrarily small/large finite normals; exact zero/invalid facts reject"를 명시하고 있습니다.
- 따라서 엄격한 영벡터 $(0, 0, 0)$만 무효화하고 `double.Epsilon`에 이르는 미소 유한 크기를 정상 정규화하는 구현은 ADR-019의 승인된 결정과 100% 일치합니다.

### 8. Zero / Invalid Extrusion Refusal
- **검증 결과**: **적합 (SATISFIED)**
- $(0, 0, 0)$, NaN, $+\infty$, $-\infty$, 부분 부재 성분, 중복/비정상 소스 팩트에 대해 침묵하지 않고 즉시 `InvalidExtrusionNormal` 진단을 발생시키며 프레임 발행을 거절합니다. 임의의 $+Z$ 수복을 시도하지 않습니다.

### 9. Arbitrary Axis Algorithm (AAA) Math
- **검증 결과**: **적합 (AUTODESK STANDARD VERIFIED)**
- Autodesk AAA 사양에 따라:
  - $|Nx| < 1/64 \text{ and } |Ny| < 1/64 \implies Ax = \text{normalize}(\text{WorldY} \times N)$
  - $\text{else} \implies Ax = \text{normalize}(\text{WorldZ} \times N)$
  - $Ay = \text{normalize}(N \times Ax)$
- 외적 순서가 정확하여, 오른손 직교 좌표계($Ax \times Ay = N$)와 양의 행렬식($\det(A) = 1$)이 완벽히 성립함을 독립 수학 검증으로 확인하였습니다.

### 10. Strict 1/64 Branch Behavior
- **검증 결과**: **적합 (STRICT INEQUALITY VERIFIED)**
- 분기 조건은 `Math.Abs(normalizedNormal.X) < 1d/64d && Math.Abs(normalizedNormal.Y) < 1d/64d`로 엄격한 미만(`<`)입니다.
- $\text{BitDecrement}(1/64)$, 정확한 $1/64$, $\text{BitIncrement}(1/64)$ 엣지 케이스에서 정확한 분기 거동이 독립 테스트로 입증되었으며, 수치 비교 정책의 엡실론이 분기 선택에 개입하지 않습니다.

### 11. Frame Handedness & Orthonormality
- **검증 결과**: **적합 (MATHEMATICALLY PROVEN)**
- 프로덕션 코드와 독립된 테스트 로컬 행렬식 계산($Ax \cdot (Ay \times N)$)을 통해 오른손 직교계($\det = 1$)를 검증하였습니다.
- $|Ax| = 1, |Ay| = 1, |N| = 1$ 및 상호 직교성($Ax \cdot Ay = 0, Ax \cdot N = 0, Ay \cdot N = 0$)이 호출자가 제공한 `MatrixCoefficientTolerance` 이내에서 완벽히 성립합니다.
- 공차를 초과할 경우 벡터를 억지로 수정하지 않고 `InvalidOcsFrame`으로 정직하게 거절합니다.

### 12. Hand-Oracle Axis Cases
- **검증 결과**: **적합 (INDEPENDENT REPRODUCTION)**
- 입력 점 $(2, 3, 4)$에 대해 수작업 기저 계산 결과:
  - $N = +Z \implies Ax = (1, 0, 0), Ay = (0, 1, 0) \implies W = (2, 3, 4)$
  - $N = -Z \implies Ax = (-1, 0, 0), Ay = (0, 1, 0) \implies W = (-2, 3, -4)$
  - $N = +Y \implies Ax = (-1, 0, 0), Ay = (0, 0, 1) \implies W = (-2, 4, 3)$
  - $N = +X \implies Ax = (0, 1, 0), Ay = (0, 0, 1) \implies W = (4, 2, 3)$
- 모든 축 케이스가 수작업 기대값과 오차 없이 일치함을 확인하였습니다.

### 13. Oblique Normal Oracle
- **검증 결과**: **적합 (CRITICAL NON-AXIS PROOF)**
- $N = (1, 1, 1)/\sqrt{3}$에 대해:
  - $Ax = (-1, 1, 0)/\sqrt{2}$
  - $Ay = (-1, -1, 2)/\sqrt{6}$
- 입력 점 $(2, 3, 4)$의 WCS 투영:
  - $W_x = -2/\sqrt{2} - 3/\sqrt{6} + 4/\sqrt{3} \approx -0.32955735700618094$
  - $W_y = 2/\sqrt{2} - 3/\sqrt{6} + 4/\sqrt{3} \approx 2.4988697677400094$
  - $W_z = 0 + 6/\sqrt{6} + 4/\sqrt{3} \approx 4.758890819541682$
- 독립 산출된 상수와 소수점 16자리까지 정확히 일치함을 확인하였습니다.

### 14. Point vs Vector Semantics
- **검증 결과**: **적합 (TYPE & SEMANTIC INTEGRITY)**
- 위치 점은 `PointMm`(`Point3`)로, 벡터는 `Vector`(`CadVector3`)로 분리 노출됩니다.
- `CadOcsFrame`에는 평행이동(`Translation`) 속성이나 메서드가 전무하며, `PointToWcs`와 `VectorToWcs`가 엄격히 구분되어 있습니다.

### 15. Dimensional vs Dimensionless Vector
- **검증 결과**: **적합 (SATISFIED)**
- `OcsDisplacementVector`: 단위 스케일이 적용되는 변위 벡터.
- `OcsDirectionVector`: 무차원 방향 벡터로 단위 스케일이 적용되지 않으며, 도면 단위 결정 없이도 정규화가 가능합니다. 방향 벡터의 크기를 1로 강제 정규화하지 않고 원본 크기를 보존합니다.

### 16. Elevation Applied Exactly Once
- **검증 결과**: **적합 (MATHEMATICALLY & LOGICALLY VERIFIED)**
- LWPOLYLINE의 경우 OCS 좌표 $(x, y, \text{elevation})$으로 구성된 후 단위 스케일링이 1회 적용되고, 이어서 AAA 기저 $A \cdot (x, y, z)^T$가 적용됩니다.
- WCS 변환 후 고도가 중복 가산되는 결함이 전혀 없습니다.
- 기울어진 법선($N = +Y$) 환경에서 $x=2, y=3, \text{elevation}=5$가 WCS $(-2, 5, 3)$으로 정확히 변환되어(고도 5가 WCS Y축으로 사상됨), WCS Z에 고도가 중복 가산되지 않음이 입증되었습니다.
- Z좌표와 별도 고도가 동시 공급되면 `ConflictingCoordinateFacts`로 엄격히 거절됩니다.

### 17. Ordinary XYZ Roles / Missing Z
- **검증 결과**: **적합 (CONSERVATIVE & HONEST)**
- 일반 3D 좌표 역할에서 Z가 누락된 경우 임의로 0으로 간주하여 평면화(flattening)하지 않고 `MissingRequiredCoordinateFact`로 정직하게 거절합니다.
- 오직 사양상 고도 기본값이 문서화된 LWPOLYLINE(코드 38 결측 시 0)에 대해서만 정당한 기본값 규칙(`DXF.LWPOLYLINE.Elevation.DefaultZero`)이 적용됩니다.

### 18. LINE
- **검증 결과**: **적합 (SATISFIED)**
- `LineStartWcs`, `LineEndWcs`는 WCS로 유지되며, 압출 벡터가 존재하더라도 엔드포인트 좌표가 회전되지 않습니다. 단위 스케일링만 1회 적용됩니다.

### 19. POINT
- **검증 결과**: **적합 (SATISFIED)**
- `PointPositionWcs`는 WCS로 유지됩니다. POINT의 표시 각도(AngleDegrees)는 04B 메타데이터로만 유지되며 좌표를 회전하거나 이동시키지 않습니다.

### 20. LWPOLYLINE
- **검증 결과**: **적합 (SATISFIED)**
- 각 정점은 순차적인 정점 서수(`VertexOrdinal`)를 보존하며 개별적으로 OCS $\rightarrow$ WCS 투영됩니다.
- Bulge 호 변환, 선폭 기하 생성, 닫힘/토폴로지 계산, 바운즈 산출 등이 일절 개입하지 않았습니다.

### 21. CIRCLE / ARC
- **검증 결과**: **적합 (SATISFIED)**
- 중심점(`CircleCenterOcs`, `ArcCenterOcs`)과 OCS 기저 프레임만 정규화됩니다.
- 04B에서 확립된 스칼라 반지름 및 각도는 불변 보존되며, 공간 Curve 객체나 외접 바운즈가 생성되지 않습니다.

### 22. SOLID
- **검증 결과**: **적합 (SATISFIED)**
- First, Second, Third, Fourth 정점 좌표가 원본 순서 그대로 개별 정규화되며, 임의의 와인딩 수정이나 폴리곤 조립이 발생하지 않습니다.

### 23. TEXT / ATTRIB / ATTDEF
- **검증 결과**: **적합 (SATISFIED)**
- 삽입점(`TextInsertionOcs`)과 정렬점(`TextAlignmentOcs`)은 OCS 앵커 좌표 사실로만 정규화됩니다. 정렬점이 완전히 부재한 경우 `Absent` 상태로 정직하게 보존되며, 폰트 메트릭 계산이나 문자열 배치 렌더링을 시도하지 않습니다.

### 24. MTEXT (Critical WCS Verification)
- **검증 결과**: **적합 (AUTODESK SPECIFICATION CONFIRMED)**
- Autodesk 공식 사양(MTEXT 10/20/30: Insertion point in WCS; 11/21/31: X-axis direction vector in WCS)에 따라, 삽입점과 방향 벡터가 모두 WCS로 정확히 분류되었습니다.
- 방향 벡터는 무차원 벡터로서 단위 스케일링이 배제되며 원본 크기가 유지됩니다. 회전 코드 50이나 레이아웃 방위 해석을 개입시키지 않습니다.

### 25. INSERT 04D Boundary
- **검증 결과**: **적합 (STRICT BOUNDARY)**
- `InsertInsertionOcs`와 압출 법선만 정규화되어 앵커 점과 OCS 프레임만 산출됩니다.
- 블록 참조의 축척(Scale), 회전각(Rotation), 대상 블록 정의명, 블록 베이스 포인트($BASE) 등은 배치 계산에 일절 개입하지 않으며, `Transform3` 합성 코드가 전무함을 확인하였습니다.

### 26. DIMENSION Mixed-Coordinate Behavior
- **검증 결과**: **적합 (FIELD-LEVEL SPACE SEPARATION)**
- Autodesk DXF 사양에 따라, 정의점(10/20/30) 및 확장점(13/23/33, 14/24/34)은 WCS로, 텍스트 중간점(11/21/31)은 OCS로 필드별 분리 처리됩니다.
- 법선 $+Y$ 조건에서 원본 좌표 $(2, 3, 4)$가 정의점은 WCS $(2, 3, 4)$로, 텍스트 중간점은 OCS $\rightarrow$ WCS $(-2, 4, 3)$으로 정확히 분기 변환됨을 확인하였습니다.

### 27. RawCoordinateAccess Authority & Row Ownership
- **검증 결과**: **적합 (SECURE ACCESS)**
- `RawCoordinateAccess.Record`는 대상 행이 해당 스냅샷에 실제로 소속되어 있는지 `ReferenceEquals(snapshot.Records[row.StructuralRecordIndex], row)`를 통해 엄격히 검증합니다. 외부/위조된 행은 즉시 `InvalidDataException`으로 거절됩니다.
- 취소 토큰(`cancellationToken.ThrowIfCancellationRequested()`)이 정상 동작하며, 원본 스냅샷의 컬렉션은 방어적으로 캡슐화되어 외부 변조가 불가능합니다.

### 28. Provenance & Deterministic Diagnostics
- **검증 결과**: **적합 (COMPACT & TRACEABLE)**
- 결과 객체는 도면/리비전 GUID, 소스 파일 해시, 원본/정규 핸들, 정점 서수, 레코드 및 팩트의 페어/행 범위를 컴팩트하게 보유합니다.
- 진단 코드는 정형화된 열거형(`CadCoordinateDiagnosticCode`)과 불변 필드명만을 포함하여 타임스탬프나 파일 경로 등 비결정론적 요소가 전무합니다.

### 29. Numerical Policy Reuse & Tight Tolerance Refusal
- **검증 결과**: **적합 (IMMUTABLE POLICY REUSE)**
- 04B의 `CadNumericPolicy`를 수정 없이 재사용하며, 새로운 엡실론이나 자의적 기본값을 도입하지 않았습니다.
- 호출자가 극단적으로 엄격한 행렬 공차(`1e-30`)를 주입할 경우 `InvalidOcsFrame`으로 정직하게 거절하며, 자의적인 공차 완화나 벡터 강제 조정을 수행하지 않습니다.

### 30. Finiteness & Overflow Protection
- **검증 결과**: **적합 (SATISFIED)**
- 입력 좌표나 중간 계산, 단위 스케일링, 기저 변환 결과가 NaN/무한대로 발산할 경우 `NonFiniteSourceCoordinate` 또는 `NonFiniteNormalizedCoordinate`로 명시적 거절되며, 클램핑이나 좌표 포화(saturation)를 일으키지 않습니다.

### 31. Controlled Source Coordinate Proof
- **검증 결과**: **적합 (OFFICIAL GOLDEN CONFIRMED)**
- 공식 통제 도면 `datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf` (SHA-256: `2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC`)의 LINE 9C (레코드 120, 페어 2856~2867) 엔드포인트(X 페어 2865=900.0, Y=0, Z=0)를 검증하였습니다.
- WCS 역할로서 프레임 생성 없이 정확히 $(900, 0, 0)$ mm가 산출되었으며, 처리 전후의 Raw 스냅샷 직렬화가 100% 동일함을 확인하였습니다.

### 32. Test Quality & Zero Self-Oracle
- **검증 결과**: **적합 (INDEPENDENT ORACLES)**
- 96개의 신규 테스트는 독립적으로 수작업 계산된 수학적 상수(기저 행렬, 비축 투영 좌표, 경계 이웃값 등)를 기대값으로 사용하며, 프로덕션 메서드를 호출하여 오라클을 생성하는 자가 오라클 현상이 전무합니다.

### 33. Full Regression & Build/Warning Gate
- **검증 결과**: **적합 (1052 PASS, 0 ERRORS, NO NEW WARNINGS)**
- 전체 솔루션 Release 재빌드: 오류 0건, 승인된 Revit MSB3277 경고 3건 외 신규 경고 0건.
- VSTest 실행 결과: **총 1052개 테스트 중 1052개 전원 통과 (0 failed, 0 skipped)**. Phase 00~04B의 956개 회귀 테스트가 단 하나의 누락이나 실패 없이 100% 유지되었습니다.

### 34. Multi-Culture & Multi-Process Determinism
- **검증 결과**: **적합 (BYTE-IDENTICAL DETERMINISM)**
- Invariant, fr-FR, ko-KR 3대 로케일 및 2개의 독립 프로세스(`process-a`, `process-b`)에서 직렬화한 좌표 증거 SHA-256 해시가 `81CA2010F120BD65888EF57AABE5A2F55E230DDC291276E1CBA8BAC9584F0342`로 완벽히 일치함을 확인하였습니다.

### 35. Raw / Dxf & 04A / 04B Stability
- **검증 결과**: **적합 (ZERO MUTATION)**
- `DtoB.Cad.Raw`, `DtoB.Cad.Dxf`, `Core`, `Geometry`, `Analysis`, `Desktop`, `Revit` 소스 및 86개 통제 픽스처는 0바이트 수정 없이 보존되었습니다.
- 04A 계약 해시(`3E49A200C8AD688F6F53842F17556D849979B1C8F3F6A083438721CA02F58EBC`)와 04B 스칼라 해시(`568D2A7C28863360A4839FF0CA59DF09C1E7C9066EC3A487701872506A68D4C3`)가 완벽히 불변 유지됩니다.

### 36. Revit Independence Audit
- **검증 결과**: **적합 (CLEAN CODEBASE)**
- 소스, 어셈블리 메타데이터, 프로젝트 파일 전수 검사 결과, Revit API(`Autodesk.Revit`, `RevitAPI`, `ElementId`, `BuiltInCategory` 등) 및 내부 피트 단위의 누출이 전무합니다.

### 37. 04D Readiness
- **검증 결과**: **적합 (READY FOR 04D)**
- 04C는 PHASE 04D(블록 배치 행렬 합성 및 인스턴스 전개)가 요구하는 핵심 요소인:
  - WCS 정규 앵커 좌표 (`PointMm`)
  - 정규직교 OCS 기저 프레임 (`CadOcsFrame`)
  - 04B의 정규 라디안 회전각
  - 무차원 스케일 팩터
  - 불변 소스 증거 및 공차 정책
을 완벽하게 제공하며, 04D에서 $L = A \cdot T(i) \cdot R_z(\theta) \cdot S \cdot T(-b)$를 합성할 때 04C 공개 계약의 파괴적 변경 없이 매끄럽게 연결될 수 있도록 설계되었습니다.

---

## 2. Findings Summary

- **BLOCKER**: 0건
- **HIGH**: 0건
- **MEDIUM**: 0건
- **LOW**: 0건

모든 구현 내용이 ADR-018, ADR-019, ADR-020의 아키텍처 규정과 PHASE 04C 좌표 정규화 스코프 경계를 완벽하게 충족하고 있습니다.

---

## 3. Final Verdict

```text
================================================================================
FINAL VERDICT: PHASE_04C_PASS
================================================================================
```

PHASE 04C 구현은 원본 CAD 좌표 사실로부터 단위 스케일링, 필드 수준 좌표 공간 분류, Autodesk 임의 축 알고리즘(AAA), 오른손 직교 OCS 프레임 구성, WCS 정규 mm 투영, 점/벡터 분리, 단 1회 고도 결합을 완벽하게 구현하였습니다. PHASE 04D+ 배치/전개 기능의 조기 누출 없이 견고하고 결정론적인 좌표 정규화 계층을 성공적으로 구축하였으므로 **`PHASE_04C_PASS`**를 선언합니다.

*(주의: 본 판정은 PHASE 04C 단일 서브페이즈에 대한 독립 승인이며, 프로덕션 코드 수정, 커밋/푸시, 또는 PHASE 04D 진입은 작업 소유자의 명시적 지시에 의해서만 진행되어야 합니다.)*
