# PHASE 03 Native Text DXF — Final Pre-Implementation Housekeeping

Date: 2026-10-14 (Asia/Seoul). Branch: phase/03-cad-data-extraction; HEAD142100deb62e8eb3692ded84a95962548925731f.

## Status / verdict

Antigravity final verdict: **APPROVED_FOR_NATIVE_DXF_IMPLEMENTATION** (reviews/PHASE_03_REVIEW.md, Native DXF review §15). Required Issues1–3 resolved. **Housekeeping complete; ready for subsequent PHASE03A.** This turn stops before03A: no parser/raw production projects/Viewer/PHASE04 implementation, SDK installation, commit or push. Architecture approval does not mean phase PASS or implemented reader compatibility.

## Review resolutions

### Issue 1 — MEDIUM — Terminology

Active architecture/product documents use **Text DXF**, distinguishing the non-binary format. First V1 profile is **AC1032, strict UTF-8, ASCII subset naturally supported, optional UTF-8 BOM at byte0, CRLF / LF**. Valid Korean and other non-ASCII UTF-8 are allowed, not rejected merely for bytes>=128. Future03A tests explicitly require multilingual/BOM/line-ending acceptance; native parser tests are not implemented/executed here. ASCII-only descriptions remain solely measured fixture evidence or the explicitly required encoding-subset statement. Accepted ADR001–015 and historical reports/review/spike text are preserved; current ADR016 governs terminology/status.

### Issue 2 — MEDIUM — Authoritative identity

ADR016, checklist§1/13/14 and relevant planning documents now explicitly define source identity at **imported managed Text DXF**:

`DrawingId + RevisionId + FileHash + Original DXF Handle`

OriginalHandle is the handle written in that DXF, not an upstream DWG handle unless independently proven. No semantic/structural/identity equivalence guarantee; no responsibility for third-party export changes to handles, anonymous blocks, dimensions, proxy/custom objects, unsupported records or database structure. DtoB still owns faithful extraction/diagnosis of its managed input. **No DWG/DXF reconciliation in PHASE03 or PHASE04.** Existing source-ID/Core/CAD IR/project contracts remain unchanged.

### Issue 3 — LOW — Finite initial development limits

ADR016 and checklist§20 specify configurable/injectable defaults, checked before buffering/allocation:

| Limit | Initial default |
|---|---:|
| Physical line (UTF-8 source bytes, excluding line terminator/byte0 BOM) | 64 KiB /65,536 bytes |
| Combined control102/XDATA1002 nesting | 16 |
| Stored diagnostics (including reserved terminal slot) | 5,000 |
| Every raw pair, including delimiters/comments/unknowns | 250,000 |
| Input bytes | 16 MiB /16,777,216 bytes |

Controlled fixture measured96,621 bytes/8,737 pairs/max physical line56 bytes. These caps provide bounded development headroom, not performance claims. Stream length plus actual read count guards input; before-append/push guards line/pairs/depth. At most4,999 ordinary diagnostics with a reserved final ResourceLimitExceeded slot; no silent diagnostic eviction/truncated-success. Every exceeded limit yields **ResourceLimitExceeded** and only bounded incomplete evidence. Configuration validated finite/positive (diagnostics>=2); smaller deterministic exact-boundary/first-over-limit tests planned; later measured/reviewed revisions permitted. No resource-limit code created here.

## ADR acceptance / changed documents

**ADR-016-Native-DXF-Reader.md: Accepted**, dated2026-10-14 with Antigravity approval/owner direction and Proposed history archive. ADR numbering001–016 unique; ADR017 remains reserved/uncreated. Accepted ADR001–015 byte-identical; no reference edit needed. Historical Proposed wording in earlier ADR015/design report reflects its earlier state and does not override current ADR016 acceptance.

Updated: ADR016; PHASE_03_CHECKLIST; tasks03/04; PHASE_INDEX; master; ARCHITECTURE; CODEX_INSTRUCTIONS; README; START_HERE; this report. No earlier status report, owner review, spike code, fixture or production file edited in this turn. Earlier uncommitted housekeeping/spike files predate this work.

## Verification

Fresh existing document/contract suite: **4/4 PASS**, zero failures/skips:

- FoundationDocumentTests.ForbiddenDependencyMutationIsRejected
- FoundationDocumentTests.AdrsContainEvaluatedAlternativesAndValidUtf8
- FoundationDocumentTests.AllProductionProjectDirectionsAreEnforced
- ContractTests.NoAutodeskDependencyLeaksIntoCoreOrAnalysis

Unchanged existing Release binary checks current docs/projects; no new tests/full product rebuild/native parser compatibility claims for this documentation-only task. git diff --check PASS; new/edited document UTF8 and whitespace checked. Static guards confirm Text DXF terminology, UTF8/non-ASCII policy, identity disclaimer/no reconciliation, finite limits/overflow outcome, unique ADRs and Accepted016. Protected152 source/project/test/spike/fixture/prior-report/review/accepted-ADR/evidence hashes unchanged. Controlled DXF hash remains2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC.

No tracked/untracked production source/project/package/build change; Raw/Dxf production directories absent; no production CAD SDK references. Historical isolated ACadSharp spike retains its existing package reference outside DtoB.sln, is NOT SELECTED, and was not run/changed. This is not a claim that the historical spike's cache/reference was removed.

Evidence: artifacts/phase03/native-dxf-housekeeping-20261014/housekeeping-documents.trx/log,protected-before.json,final-guards.json,checklist-before-final-review.md,ADR-016-proposed-history.md. Prior native design and failed spike reports remain historical unchanged.

## Stop / readiness

Ready for PHASE03A on subsequent owner direction. Native reader/raw contracts remain unimplemented; actual encoding/resource/entity/read-lifecycle tests are future work. **STOP before03A this turn; no PHASE04/SDK install/commit/push.**
