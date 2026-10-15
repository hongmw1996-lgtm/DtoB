# PHASE 03 — ODA Evaluation Register

Updated: 2026-10-14 (Asia/Seoul).

## Status
**HISTORICAL / INACTIVE**, superseded by the owner's 2026-10-14 extraction-first PHASE 03 direction. ACadSharp must be investigated first. No ODA evaluation ran: all historical gates remain NOT_RUN. SDK access is not the current proposal's blocker. The earlier request for an ODA SDK path is no longer needed to review the new proposal. No commercial purchase or production SDK acceptance.

The remaining sections preserve the earlier Viewer-first evaluation plan, not instructions to acquire or execute ODA now. Current scope and extraction-based commercial escalation are in PHASE_03_CHECKLIST.md. Any future ODA/RealDWG comparison is based on data fidelity, handles, blocks, transforms and raw annotation access rather than display quality, and requires the owner's commercial-SDK decision.

## Authorization and existing evidence
Owner's ten gates are the acceptance criteria. Provisional decision recorded in ADR-015. Branch `phase/03-dwg-viewer`, base `142100deb62e8eb3692ded84a95962548925731f`. Existing PHASE 00/01/02 baseline: Core/full Solution Release PASS, 126/126 tests PASS under `artifacts/phase03/baseline-20261014`. These results predate this documentation-only update and prove no ODA capability.

No trial archive/SDK found in examined repository dependencies, top-level Downloads, DtoB parent directories, installed-product entries or package cache. Exact SDK release, supported wrapper targets, entitlement/start/expiry and license activation are unknown. This is not a comprehensive disk/entitlement audit; an owner-provided path may resolve access.

## Acquisition facts and blocker
The [current official ODA trial page](https://www.opendesign.com/free-trial) describes application, email confirmation, download and activation, with trial documentation/examples available through the account. It advertises a 60-day, one-desktop trial. The [trial-vs-regular FAQ](https://www.opendesign.com/faq/question/what-difference-between-trial-version-and-official-version) describes desktop-bound activation and a VS2017 trial platform; this may be legacy guidance and **must not be substituted for the actual downloaded release manifest** when evaluating VS2022/.NET 8. The older `/trial/release/` download URL returned HTTP 404 during investigation; do not rely on legacy download instructions.

Registration/login/activation must use the owner's actual account and entitlement. No personal details invented, form submitted, account created or activation bypassed. Do not send account passwords or license keys in chat. Required next input: path to the activated Windows x64 Drawings trial, matching .NET wrappers, docs/examples and non-secret release/version information. Trial approval is not activation evidence.

## Ten capability gates
| Gate | Required verification/evidence | Current result |
|---|---|---|
| 1 — Windows 11 x64 / VS2022 / .NET 8 | Exact archive/version/hash/targets/runtime dependencies; build and run supplied sample and narrow net8 evaluation harness on current host. | NOT_RUN |
| 2 — Real DWG AC1024 and DXF | Read actual repo fixtures through the SDK, record backend diagnostics/scene evidence; no synthetic substitute. | NOT_RUN |
| 3 — Required presentation | Actual LINE/POLYLINE/ARC/CIRCLE/TEXT/MTEXT/INSERT/DIMENSION cases, including block placement and annotation appearance. Add labelled real CAD fixtures for types absent from existing sample; no fabricated reader. | NOT_RUN |
| 4 — Layers/colors/visibility | Layer inventory and original/effective color including ByLayer/ByBlock; default off/frozen state and controlled visual toggle evidence. | NOT_RUN |
| 5 — Handles/occurrence ancestry | Original handles and outer-to-inner INSERT ancestry, repeated block instances distinguishable; dimension-generated graphics mapped to owning source handle. | NOT_RUN |
| 6 — Owned RenderScene | Actual documented SDK calls producing owned numeric/path/glyph data; dispose SDK before scene consumption; no vendor object types/pointers in Desktop/ViewModel or returned scene. | NOT_RUN |
| 7 — Read-only managed input | Register fixtures into an owned evaluation project; resolve actual managed path only; compare source/managed/project bytes and IDs/hash/dirty state before/after success/failure. | NOT_RUN |
| 8 — File sharing/read lease | Hold read lease excluding writes/deletes from verification through SDK read; prove sharing compatibility and write denial with actual SDK. Any requirement for write access is a critical failure under current proposal. | NOT_RUN |
| 9 — Threads/disposal/cancellation | Document required worker context; pending/cancel/switch/late-result/disposal tests, safe cleanup order and native cancellation limits. | NOT_RUN |
| 10 — Offline/runtime deployment | Identify exact DLL/VC/runtime/font/activation requirements; offline run on activated trial machine. Trial desktop restriction distinguished from prospective production deployment; production claims needing paid entitlement stay unverified. | NOT_RUN |

## Evaluation execution plan once SDK is available
1. Inspect archive/docs without modifying product projects; inventory non-secret versions/hashes and actual APIs. Confirm matched Kernel/Drawings/.NET package set for the selected release rather than guessing filenames.
2. Verify trial entitlement/platform restrictions and expiry. Keep proprietary files in external SDK root or ignored local artifacts; no license key in logs/evidence/Git. No paid membership accepted.
3. Run vendor sample as first runtime proof. Build a small isolated evaluation harness only against **actual documented APIs**, outside product dependency graph. Label all evidence as trial evaluation.
4. Create disposable owned managed-project fixtures via existing Project library. Verify hash, read-only lease, real render data and source IDs before considering WPF integration. Do not expose SourceFilePath to backend.
5. Record one result per gate: PASS/FAIL/NOT_RUN, actual API/version, raw logs/hash/fixture evidence and limitations. A visual image alone cannot prove handles, ownership, read leases or cancellation.
6. Stop on any critical FAIL. Return to architecture review for ACadSharp + owned WPF renderer. No AutoCAD automation, private Autodesk DLLs, bitmap-only substitution or fake Viewer.
7. Present capability results for architecture/production decision. Passing a trial gate alone does not authorize commercial purchase/redistribution or bypass Antigravity design review.

## Current changes, tests and limitations
Documentation only: PHASE_03_CHECKLIST updated with trial approval, provisional ADR-015 and this register added. No new ODA tests run; no build required for product changes because none exist. Existing 126-test baseline remains historical baseline. Current expected blocker is SDK access, not failed compatibility. Antigravity PHASE_03 design review file is not present. Trial start/expiry unknown. No implementation or later-phase work.

## Next step
Owner supplies the activated SDK path after account download/activation (or identifies an existing SDK). Then execute the bounded capability evaluation; do not guess APIs or mark gates PASS from marketing pages. Stop for architecture review if a critical gate fails.

## Housekeeping follow-through — 2026-10-14
The unaccepted Viewer ADR015 draft was replaced by ADR-015-Raw-CAD-Extraction-Boundary.md under explicit owner instruction. Its old text is archived in ignored housekeeping artifacts, not an active ADR. This register's former ADR015 references are historical. New sequence: 015 raw boundary established, 016 backend reserved after actual capability, 017 lifecycle reserved. ODA has no active evaluation or acquisition request in current ACadSharp-first 03A preparation.
