# PHASE 03A — ACadSharp Reader Capability Spike Report

Date: 2026-10-14 (Asia/Seoul). Branch: `phase/03-cad-data-extraction`. HEAD: `142100deb62e8eb3692ded84a95962548925731f`.

## Recommendation

**ACADSHARP_CAPABILITY_FAIL** — stop for owner/Antigravity architecture review. Actual original DWG/DXF decode succeeded, but independent supplemental nonzero-base-point INSERT placement exposes a critical GetTransform defect. No workaround, SDK escalation, production integration or Accepted ADR-016 was performed. This is not PHASE 03 completion.

## 1. Artifact / license

Exact released NuGet **ACadSharp 3.8.0**, selected `lib/net8.0`; empty net8.0 dependency group, confirmed by restored project.assets.json (only ACadSharp). Declared license **MIT**. Nuspec repository commit: `3a1c66b9932f9e2dbfbbf3f03660c97d52e49b02`. No mutable master reference. [Released package](https://www.nuget.org/packages/ACadSharp/3.8.0), [pinned source](https://github.com/DomCR/ACadSharp/tree/3a1c66b9932f9e2dbfbbf3f03660c97d52e49b02).

Nupkg SHA-256: `3B7D0E4643CB1494F3B974E902EC558077FE7242D764D24B03EC7E14CDF5D705`. Downloaded artifact and restored cache artifact match. Version/hash/framework/dependencies/license were recorded in `artifacts/phase03/spike/package.txt` before reference/restore. Binaries and upstream source archive are ignored artifacts, not Git additions. Trial authorization does not accept a production backend.

## 2. Environment / build

Windows 11 Pro x64, 10.0.26100; VS2022 Community MSBuild **17.14.23.42201**, existing Roslyn + SDK files **8.0.416**, .NET 8 runtime **8.0.22**. Normal dotnet.exe remains absent. Existing documented Visual Studio fallback built the isolated net8.0 executable successfully; apphost executed it. No security setting/exclusion or toolchain installation.

Release spike build: **PASS**, actual MSBuild exit 0 (`build.log`). SDK-resolution notices about the absent CLI remain in the log; they are not compile failures. Earlier harness compile errors (MText property name and nullable access) were corrected before final build. Full product rebuild was not required or rerun: production projects/source/build files are unchanged. Fresh VSTest run: **126/126 PASS**, zero failures/skips, including all FoundationDocumentTests and dependency checks; `regression.trx`/`regression.log`. This fresh test run is distinct from the earlier baseline build/test evidence.

## 3. Inputs / hashes

Authoritative original inputs, never saved/audited/repaired/converted:

| Input under datasets/controlled/phase02 | SHA-256 before and after |
|---|---|
| DtoB_TEST_2STORY_R2018.dwg | `3C2F4E880C63B2F247DD5D60EA2CB8D68BC72A99C505E233D88B446D59B9BAAE` |
| DtoB_TEST_2STORY_R2018.dxf | `2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC` |

Both **UNCHANGED**. These are repository-controlled files specified by the owner, not a saved .dtob managed revision/session integration test. Identical strict lease behavior was tested directly on these bytes; actual project read-coordinator integration remains unimplemented.

Two separate real text DXF supplemental variations were generated, not in-memory vendor objects: MINSERT parameters on B9; nonzero COLUMN_400 base point plus rotated/scaled B9. Their generator and provenance are under `spikes/DtoB.Cad.ACadSharp.Spike/Fixtures`. They retain the source database structure, are not Controlled V1 truth, and never overwrite the original. No supplemental DWG created.

## 4. Exact configuration

- Both readers: `Failsafe=false`, `KeepUnknownEntities=true`, `KeepUnknownNonGraphicalObjects=true`.
- DXF: `CreateDefaults=false`; `ClearCache=true` (released default).
- DWG: `CrcCheck=false`, `ReadSummaryInfo=true`, `IgnoreProxyGraphics=true` (released defaults). CRC validation/proxy graphic fidelity was not claimed.
- No RestoreHandles/CreateDefaults/UpdateBlock/Explode/writer/repair API call.
- Stream constructor only; `FileMode.Open`, `FileAccess.Read`, `FileShare.Read`. SHA-256 on that same lease, rewind, decode; filename overload never used. All notification events captured.

## 5. DWG result

Raw six-byte signature **AC1024**, decoded **AC1024**, independent of misleading R2018 filename. Original strict decode exit 0; **173** direct definition/space entities and **427** registered objects collected. ModelSpace 129, PaperSpace 0, remaining definition children 44. Three warnings (section 16). Repeat summary, handle/transform audits and notifications byte-identical.

## 6. DXF result

Independent text `$ACADVER` **AC1032**, decoded **AC1032**. Original strict decode exit 0; **158** direct entities and **305** registered objects collected. ModelSpace 114, PaperSpace 0, remaining definition children 44. Independent group-code QA: **1,353 checks passed / 0 failed** (retention/type/handle/owner/layer, explicit coordinate components, vertex counts/order/XY, radius/angles, INSERT block/position/scale/rotation, text Unicode decoding and dimension defining points). This does not claim every optional width/bulge/style property was independently checked. Repeat evidence byte-identical.

## 7. Header / units / presence

Both decoded INSUNITS **4 / Millimeters**. DXF `$INSUNITS` code70=4 and `$ACADVER` code1=AC1032 are SOURCE_CONFIRMED by independent line pairs. DWG decoded units and all other unproven property-presence claims stay LIBRARY_DECODED_PRESENCE_UNKNOWN. Header-reported ModelSpace/PaperSpace min/max exported; extents reliability not independently established. No coordinate conversion/recentering or import metadata update.

## 8. Layers

Both have **12** layers: 0, Defpoints, A-WALL, A-DOOR, A-WINDOW, A-FLOOR, S-COLUMN, A-GRID, A-TEXT, A-DIM, A-LEVEL, A-SECTION. Names match. Handles/owners, raw ACI/ByLayer/ByBlock/truecolor mode fields, linetype names/handles, On/Off and flags are copied. Frozen=true/Off/truecolor variant behavior was not separately exercised. Payload is observed data, not effective occurrence-context style resolution.

## 9. Blocks

Both have **12** block records: ModelSpace, PaperSpace, _ARCHTICK, _CLOSEDFILLED, _CLOSEDBLANK, COLUMN_400, D_SINGLE_900, W_SINGLE_1200 and four anonymous dimension blocks. DWG names *D6–*D9 versus DXF *D1–*D4. Original base points all zero. Names/handles/owners/BLOCK handles/base points/flags/units/layout/Xref paths and ordered child handles exported. No Xref resolution or dynamic block support claimed.

## 10. INSERT

Both have **25** source INSERT records, preserving original handle/owner/block reference, insertion, radians rotation, XYZ scale, normal, arrays and attributes collection. No Explode or occurrence flattening. Controlled originals have no MINSERT or nonzero block base point; attributes collection is empty, so attribute-content/subentity fidelity is unverified. Nested ancestry and non-+Z transforms are unverified.

## 11. Geometry coverage

Counts include all direct block/space children, not expanded instances:

| Type | DWG | DXF | Evidence |
|---|---:|---:|---|
| LINE | 74 | 59 | Ends, normal/thickness copied; DXF explicit ends checked |
| LWPOLYLINE | 16 | 16 | Ordered XY/bulge/width/id and flags/elevation/normal retained; XY/order independently checked |
| ARC | 1 | 1 | Center/radius/radians angles/normal/thickness; DXF degrees comparison passed |
| POINT | 12 | 12 | Position/normal/thickness; DXF position checked |
| SOLID | 1 | 1 | Four original corner slots including repeated triangular corner; DXF checked |
| CIRCLE / legacy POLYLINE / ELLIPSE / SPLINE | 0 | 0 | UNVERIFIED; no API-existence support claim |

All exported geometry stays native source coordinates. Broader geometry supplemental work stopped after the critical transform failure.

## 12. Annotation

Both: TEXT **36**, MTEXT **4**, DIMENSION **4**. Raw decoded text/control formatting, insertion/alignment, styles, normals and available dimensional defining/reference points copied; DXF text/coordinates checked. DIMENSION Measurement explicitly labelled LIBRARY_DERIVED. Perfect rendering was neither attempted nor required. HATCH absent: UNVERIFIED. Complete MTEXT column/formatting/style-override coverage remains unverified; copied payload is a spike subset, not a final raw contract.

## 13. Handle audit

DWG: **427 collected / 427 nonzero / 427 unique**. DXF: **305 / 305 / 305**. Collected duplicate and zero handles: **0**; unresolved collected owners and INSERT block references: **0**. Canonical hex represents the original numeric handle value. No substitute IDs.

Inventory uses public lookup 1..decoded HandleSeed (DWG503/DXF356), plus definition children, with an explicit one-million lookup budget; synthetic CadDocument handle0 excluded. This is registered-object inventory, not proof every DWG source object was retained. A ulong API cannot distinguish originally missing handle from zero or prove no internal registration defaults; arbitrary malformed-handle behavior remains unverified. Independent DXF inventory has **327 handled source records**, excluding HEADER/SECTION pseudo-records; **25 VISUALSTYLE** source records are unrepresented and separately warned. Do not equate decoded registered-object count with original source record count.

## 14. Owner / space audit

Every block record enumerated once, not just CadDocument.Entities (ModelSpace only). Both layouts: Model handle1A/block17 and Layout1 handle1E/block1B. Reusable/anonymous definitions are separate from ModelSpace; scope type/count tables exported. Public object lookup also inventories exposed delimiter/owned/nongraphical records without multiplying objects by occurrence. Nonempty PaperSpace, legacy vertex/SEQEND and attribute cases are absent/unverified; no exhaustive arbitrary-owner fidelity claim.

## 15. Transform audit — critical failure

Original **25 DWG + 25 DXF INSERTs** passed independent +Z placement probes (origin, axes, block base and arbitrary point), including translated/rotated and scaled dimension symbols. Explicit tolerance supplied by the spike invocation: **1e-9 native units**; not a product tolerance default. Original nonzero base-point coverage absent.

Supplemental real DXF: block COLUMN_400 base `(100,200,0)`, INSERT **B9**, insertion `(1000,1000,0)`, scale `(2,3,1)`, rotation **90°**, normal +Z. Those parameters decode correctly. Independent column-vector placement is `T(insertion) * R * S * T(-base)`; the base point must land at insertion. [Autodesk INSERT field semantics](https://help.autodesk.com/cloudhelp/2018/ENU/AutoCAD-DXF/files/GUID-28FA4CFB-9D5E-4880-9F11-36C97578252F.htm).

| Probe | Independent expected | ACadSharp GetTransform |
|---|---|---|
| Base (100,200,0) | (1000,1000,0) | (300,1000,0) |
| Origin | (1600,800,0) | (900,800,0) |

Max absolute component error **700**; executable exit **3**, exactly one transform failure. Source at the package commit composes translation by `(insertion-base)` before rotation/scale rather than transforming the base offset with them. [Pinned GetTransform source](https://github.com/DomCR/ACadSharp/blob/3a1c66b9932f9e2dbfbbf3f03660c97d52e49b02/src/ACadSharp/Entities/Insert.cs#L381). This explains the measured result; it is not a repaired transform in the harness. `basepoint/transform-audit.json` preserves actual/expected probes and reconstructed matrix. No vendor patch or DtoB placement workaround. **CRITICAL_FAIL** triggers STOP even though raw parameters are available.

## 16. Unsupported / notifications

DWG: two warnings preserve unknown nongraphical objects TCH_DBCONFIG handle175 and CELLSTYLEMAP handle1DA; third warns unresolved TextStyle reference in a TableStyle CellStyle. DXF: **25** warnings for missing ACAD_VISUALSTYLE entries, corresponding to unrepresented VISUALSTYLE records. No required controlled geometry disappearance detected by independent DXF QA. Universal DWG omission detection is not proven; warnings + decoded graph alone cannot certify unseen binary records.

Supplemental MINSERT B9: rows2/columns3/row spacing600/column spacing800 preserved, one spike-owned `UNSUPPORTED_ENTITY/UnsupportedOccurrenceExpansion`, expanded cells0, source INSERT count remains25, subsequent BB retained. The library itself does not emit the DtoB unsupported-expansion policy; spike evidence mapping does. No synthetic cell IDs. Unsupported graphical/proxy/malformed/truncated inputs remain unverified after STOP.

## 17. Default versus source findings

DXF INSERT DB example: row71/column70, Z-scale43 and normal210/220/230 are absent, yet properties yield rows1/columns1/Z-scale1/normal+Z. Source audit classifies the absent codes ABSENT, not SOURCE_CONFIRMED; corresponding library values are LIBRARY_DECODED_PRESENCE_UNKNOWN. CreateDefaults=false does not disable initialized C# properties. Public getters do not generally expose per-field source presence. DXF QA can independently establish presence; DWG property presence remains unknown unless separately proven. Transform/Measurement are LIBRARY_DERIVED; the defective supplemental transform is additionally INVALID. Unsupported expansion is UNSUPPORTED. No default falsely reported as explicitly stored data.

## 18. Read-only / lifecycle

Both originals successfully read from the caller's strict stream; concurrent FileAccess.Write attempt was rejected. Reader Dispose closes the supplied stream, so future ownership must treat it as transferred or use a verified leave-open wrapper. Returned document remained usable for evidence copying after reader disposal. Original content hashes remain unchanged. No permissive reopen or SourceFilePath fallback, no .dtob/session mutation.

Console isolation avoids WPF UI work; no production coordinator/thread system implemented. No native CancellationToken overload identified. Cancellation, malformed-read cleanup, cooperative stream interruption, stale publication, queue ownership and concurrent read cancellation were **not tested** before critical STOP. Success disposal does not prove cancellation safety or full PHASE02 managed-session integration.

## 19. DWG / DXF comparison

Layer names and reusable block names match; format versions and anonymous dimension names differ. DWG has15 more LINE records: ModelSpace129 versus114; definition totals both44. DWG-only handles and shared-handle samples exported. Eight of nine representative shared payloads are serialized-identical; SOLID7B differs only in a corner ordinate by about3e-17. These sample matches corroborate selected DWG facts, not complete paired-file equivalence. Additional lines/changed anonymous handle sets are recorded as decoded database differences; their authoring/export cause and independent binary-source completeness are not established, and no silent reader loss is inferred from the paired counts alone. No forced count matching.

## 20. Limitations

Unverified: absent families, nonempty PaperSpace, attributes/legacy subentities, nested/mirrored/arbitrary-normal transforms, CRC/corruption behavior, universal DWG record completeness, robust per-field DWG presence, native cancellation/thread/disposal failure paths, offline deployment on a clean machine. No production RawDrawingSnapshot, reader, Viewer, CAD IR normalization, Analysis or Revit implementation. No Revit host check required by isolated non-IPC work. Phase 03A ended at the critical gate; later capabilities were not fabricated or tested around it.

## 21. Capability matrix

Each row has one result; scope limitations above remain binding.

| Capability | Result | Evidence / limit |
|---|---|---|
| Windows11 / .NET8 isolated integration | PASS | Release compile and apphost execution |
| Real AC1024 DWG decode | PASS | Strict original decode, matching signature |
| Controlled DXF decode | PASS | Strict original decode, raw AC1032 |
| Original collected handles / references | PARTIAL | Nonzero unique inventory; DXF source checks; universal omission/default cases unknown |
| Layers and raw style/visibility fields | PASS | 12 layers with copied factual fields; special variants untested |
| Definitions / INSERT structure | PASS | 12 blocks,25 INSERTs each; source coordinates/references |
| Required present geometry | PARTIAL | Observed families; independent DXF checks and selected DWG corroboration, not exhaustive binary-source proof |
| CIRCLE / legacy POLYLINE | UNVERIFIED | Absent; stopped before supplemental coverage |
| ELLIPSE / SPLINE | UNVERIFIED | Absent; no support claim |
| TEXT / MTEXT / DIMENSION | PARTIAL | Present data/source checks; broader optional field fidelity not complete |
| HATCH | UNVERIFIED | Absent |
| Space/owner enumeration | PARTIAL | All available layouts/blocks; PaperSpace empty |
| Header/unit/presence fidelity | PARTIAL | DXF source-confirmed declarations; DWG presence unknown |
| Original +Z/zero-base transforms | PASS | 50 independently checked instances |
| Nonzero-base rotated/scaled transform | CRITICAL_FAIL | B9 error700; actual matrix wrong |
| Nested/non-+Z transform | UNVERIFIED | Not tested |
| MINSERT unsupported continuation | PASS | Supplemental file, preserved base metadata, BB retained, no cells |
| Omission detection for controlled data | PARTIAL | DXF required inventory retained; warned visual-style omissions; DWG completeness not proven |
| Strict read-only stream / byte preservation | PASS | Owned-stream decode, write denied, matching hashes |
| Repeated evidence determinism | PASS | Original outputs byte-identical |
| Success stream disposal | PASS | Stream closed by reader; returned document usable |
| Cancellation / failure lifecycle | UNVERIFIED | Critical STOP before tests |
| Production/vendor dependency isolation | PASS | Only isolated csproj references package |
| Offline clean-machine runtime deployment | UNVERIFIED | Current-machine execution only |

## 22. GO / NO-GO

**ACADSHARP_CAPABILITY_FAIL**. Return exact transform evidence for architecture review. Do not automatically install ODA/RealDWG, fix vendor code, reconstruct production transforms, restrict away the failed case or accept ADR-016. Owner/Antigravity determines the next evaluation direction.

## 23. Changes / verification / production confirmation

Added only isolated csproj/Program, independent QA/supplemental generator/evidence assertions, labelled DXF fixtures/provenance, checklist execution amendment and this report. Existing uncommitted architecture-housekeeping files predate this spike; historical reports/evidence and accepted ADR-001–015 were preserved. No solution entry, production source/package/project/test/build change. No production ACadSharp reference; no production raw contract or Viewer, PHASE04 code or Accepted ADR-016. No commit/push.

Verification: spike Release build PASS; repository tests **126/126 PASS**; source QA **1353/1353 PASS**; MINSERT and repeat checks PASS; `Verify-Evidence.ps1` assertions PASS (reads saved evidence only and confirms the expected backend defect, not a capability pass); `git diff --check` PASS. No new xUnit cases: spike assertions/real fixtures are separate from the unchanged126 production regression count. Protected prior ADR/report/evidence/fixture hashes **93/93 unchanged**.

## 24. Evidence / original-file confirmation

All original before/after hashes match section3. Evidence root: `artifacts/phase03/spike/` (ignored): environment.txt, package.txt/nuspec/assets, input-hashes-before.json/input-hashes.txt, build.log, dwg/dxf summaries/handle-audit/transform-audit/parser-notifications, repeat outputs, dxf-reference-audit.json, shared-handle-samples.json, minsert/basepoint outputs, transform-source-excerpt.txt, regression.trx/regression.log, evidence-tests.log, capability-matrix.md and spike.log. No package binary committed.

Ready for **owner and Antigravity capability-failure review**. STOP; no production/backend acceptance or next phase.
