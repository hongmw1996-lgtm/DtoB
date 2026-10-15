# PHASE 03 Native DXF Design Revision Report

Date: 2026-10-14 (Asia/Seoul). Branch phase/03-cad-data-extraction; HEAD142100deb62e8eb3692ded84a95962548925731f.

## Status

**REVIEW_REQUIRED — architecture/design proposal ready for owner and Antigravity review.** This is not PHASE03 implementation completion, parser compatibility proof or approval to start coding. Current task stops here. No parser, raw production contracts/projects, Viewer, PHASE04 code, SDK installation, commit or push.

## Design changes

DtoB V1 now uses **Native ASCII/Text DXF extraction**. DWG users externally Save As/Export DXF; direct DWG/automatic conversion/CAD SDK selection is no longer a V1 requirement. ACadSharp is **NOT SELECTED**; its ACADSHARP_CAPABILITY_FAIL report and spike stay historical unchanged, no repair/fork/integration/continued backend review or ODA/RealDWG escalation.

Rewrote PHASE_03_CHECKLIST with all30 requested sections: owned lexical pairs/raw spelling/physical code-value lines, structural/subclass/control contexts, source tables/geometry/BLOCK/INSERT/annotations/handles/owners/provenance, snapshot/completeness, encoding/version/error/resource policy, immutable managed authority/async/cancellation/stale guards, strong fixture/source QA and secondary diagnostic Viewer. Exact new03A–03H sequence is aligned across task/index/master. PHASE04 alone owns unit/default/radian/OCS-WCS resolution, local/final/nested block transforms/effective styles/normalization/CAD IR. PHASE03 contains no INSERT transform engine; 03H uses directly displayable native-frame geometry/local definition inspection/raw markers with explicit limits.

Proposed projects only: DtoB.Cad.Raw (pure owned contracts/evidence) and DtoB.Cad.Dxf (pure parser, references Raw only). Existing Core mm/CAD IRv1/BIM/Projectv2/source-ID/dependency directions unchanged; historical PHASE02 DWG registrations remain project metadata/ineligible reader inputs, no migration. Native implementation remains entirely planned.

## Architecture decisions / ADRs

ADR015 accepted additive raw boundary amended under explicit owner direction for Native DXF source-pair evidence, raw handle spelling and PHASE04-only transformation ownership. Original before-amendment text archived in ignored design evidence. ADR001–014 unchanged/unrenumbered.

**ADR-016-Native-DXF-Reader.md is Proposed, not Accepted.** Input/backend/lexical ownership/version/encoding/unsupported behavior require architecture review before implementation. No ACadSharp backend ADR. ADR017 reader lifecycle remains reserved if justified before integration; not created/Accepted.

## Controlled input inspection

Unchanged datasets/controlled/phase02/DtoB_TEST_2STORY_R2018.dxf: SHA256 **2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC**,96,621 bytes,17,474 physical lines/8,737 pairs,CRLF,noBOM,all bytes ASCII,no observed Unicode escapes. Header actual AC1032, DWGCODEPAGE ANSI_1252, INSUNITS4. EXTMIN=(+1e20,+1e20,+1e20), EXTMAX=(-1e20,-1e20,-1e20); preserve source values and mark unreliable/inverted reported bounds, never repair/use as reliable Fit extents.

Proposed initial AC1032 profile is strict UTF8 text including ASCII subset, optional UTF8 BOM/LF/CRLF. Pure ASCII fixture proves no multilingual encoding case; header codepage does not prove those bytes are ANSI-specific. Older versions, other codepages, non-ASCII/BOM/escape cases need explicit fixtures/tests before support claims. No binary/UTF16/nonstandard decoder or silent replacement fallback. [Maintained encoding reference](https://ezdxf.readthedocs.io/en/stable/dxfinternals/fileencoding.html) informs the limited design, not production dependency/compatibility proof.

## Changed files this task

- docs/status/PHASE_03_CHECKLIST.md (rewritten; previous SDK checklist preserved byte-for-byte in ignored artifacts)
- tasks/PHASE_03.md; tasks/PHASE_04.md; PHASE_INDEX.md; DtoB_FINAL_MASTER_PLAN.md
- ARCHITECTURE.md; CODEX_INSTRUCTIONS.md; README.md; START_HERE.md
- docs/adr/ADR-015-Raw-CAD-Extraction-Boundary.md; new Proposed docs/adr/ADR-016-Native-DXF-Reader.md
- docs/status/PHASE_03_NATIVE_DXF_DESIGN_REPORT.md

Historical spike/housekeeping/ODA reports, owner review, controlled/supplemental fixtures and spike code were not edited. Earlier uncommitted housekeeping/spike additions remain; they are not new product implementation in this task.

## Tests / verification

Fresh affected document/dependency verification **4/4 PASS**, zero failures/skips:

- FoundationDocumentTests.ForbiddenDependencyMutationIsRejected
- FoundationDocumentTests.AdrsContainEvaluatedAlternativesAndValidUtf8
- FoundationDocumentTests.AllProductionProjectDirectionsAreEnforced
- ContractTests.NoAutodeskDependencyLeaksIntoCoreOrAnalysis

Executed unchanged existing Release test binary against current documents/project files. No new tests or complete build needed for documentation-only edits. Earlier126/126 spike regression and baseline Core/full builds remain historical; they do not prove Native DXF support.

Checklist30-section order, master0–44 sections, official03A–03H order, unique ADR001–016 and Proposed016 status verified. Protected149 source/project/test/spike/input/prior-report/review/accepted-ADR/evidence files hash-identical. No tracked/untracked production file changes, no production CAD SDK reference, Raw/Dxf directories absent. Git documentation diff reviewed for obsolete SDK gates, transform leakage, raw-unit/presence/provenance and version/encoding claims. git diff --check PASS after removing trailing Markdown whitespace on the changed master goal line; no original DXF byte change.

Evidence: artifacts/phase03/native-dxf-design-20261014/controlled-dxf-inspection.json,protected-before.json,design-guards.json,design-documents.trx/log,extraction-sdk-checklist-history.md,ADR-015-before-native-dxf.md. Historical SDK evidence remains under artifacts/phase03/spike and was not rerun/modified by this task.

## Limitations / next step

Native reader and its encoding/version/entity/lifecycle coverage are unimplemented/unverified. Resource budget numbers await measurement before implementation acceptance. Optional families and PHASE03 native-frame display limits remain explicit; no vendor or transform workaround. Detailed ProposedADR016 and parser/fixture/default/resource policy need owner/Antigravity review; prior SDK review does not approve this native design.

STOP for architecture review. Do not start03A code/Viewer/PHASE04, install SDKs, commit or push.
