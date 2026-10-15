# PHASE03H — Minimal Raw DXF Diagnostic Viewer

**Status: PASS_WITH_KNOWN_LIMITATIONS — local implementation ready for independent Antigravity03H review.**

2026-10-15. This is not independent03H acceptance or the whole-PHASE03 final gate.
No commit/push. NoPHASE04.

## 1. Exact scope and baseline

Implemented only the snapshot diagnostic viewer authorized by the owner. Existing
WPF project is `apps/DtoB.Desktop/DtoB.Desktop.csproj`; Release executable is
`apps/DtoB.Desktop/bin/Release/net8.0-windows/DtoB.exe`. No guessed/new UI project.
Reviewed current03G verdict **PHASE_03G_PASS**, prior03A–03F specifications,
reports/reviews,checklist,ADR015/016 and03/04 architecture boundaries. Historical
pre-implementation stop text remains history; current owner explicitly authorizes03H.

Branch `phase/03-cad-data-extraction`, HEAD
`142100deb62e8eb3692ded84a95962548925731f`, unchanged. Prior dirty/untracked work
preserved using a before-change file/hash snapshot. Baseline Core/full Solution
Release and736/736 tests PASS before changes. Source and fixed-context Snapshot
hashes match03G. No prior viewer/normalization was reused or introduced downstream.

## 2. Files and projects changed

New:

- `apps/DtoB.Desktop/Diagnostics/DiagnosticExtraction.cs`
- `apps/DtoB.Desktop/Diagnostics/RawDiagnosticScene.cs`
- `apps/DtoB.Desktop/Diagnostics/DiagnosticViewport.cs`
- `apps/DtoB.Desktop/DiagnosticViewerWindow.xaml` and `.xaml.cs`
- `tests/DtoB.Tests/RawDiagnosticViewerTests.cs`
- `docs/adr/ADR-017-Reader-Lifecycle.md`
- `scripts/qa/Verify-Phase03HBoundary.ps1`
- This report.

Modified: Desktop project references,MainWindow XAML/code-behind entry/context
invalidation,tests project linked pure Desktop logic,FoundationDocumentTests
Desktop forward-reference allowlist,and append-only03H checklist evidence.
No solution/project persistence/parser/raw contracts/old ADR/report/review/fixture
change. The allowlist adds only authorized Desktop→Raw/Dxf; all reverse/vendor/
WPF/parser prohibitions and test bodies remain enforced.

## 3. Viewer architecture and decisions

Selected managed DXF→Desktop worker/verified read lease→existing03A→03F→immutable
Snapshot→Desktop-only RawDiagnosticScene→WPF DiagnosticViewport/Inspector.
Small code-behind follows existing shell conventions; no MVVM/DI/viewer package.
Pure presentation and application boundary classes are linked into the existing
runtime-only test project, as ShellViewModel already is. WPF rendering remains in
Desktop. Scene is ephemeral, not CAD IR, a project schema or input toPHASE04.

ADR017 evaluates same-lease extraction versus hashing during ordinary metadata
flows/reopening between verification and parsing, and existing Desktop versus a
new viewer project/external renderer. Existing accepted ADR001–016 stay unchanged.

## 4. Snapshot consumption boundary

Only a real successful03F Snapshot can create a scene. Viewer invokes existing
readers; no second parser, fabricated/bypass Snapshot or graph repair. Original
source facts/record references remain authoritative; scene item references its
exact RawDrawingRecordView and retains its structural locator. No stable ID is
created. No mutation or persisted derived viewer state.

## 5. ENTITIES view

Exact `SourceSection == ENTITIES` filtering. Controlled source independently
scanned via BCL lines/code0 matches114 diagnostic records:45LINE+12LWP=57 direct
raw geometry;17INSERT anchors;36TEXT anchors;4DIMENSION definition-point records.
BLOCKS geometry cannot appear here. Counts are asserted in tests, not hard-coded
in production UI. UI derives current count/summary from Snapshot/scene.

## 6. Block Definition view

All12 controlled definitions selectable;44 children retain exact source order.
Only selected definition children project in native definition-local XY. Empty
definitions produce a clear empty state. Base point marker references the BLOCK
record and exposes BasePoint facts; no compensation or placement is performed.
UI explicitly says “Raw block-definition coordinates — no occurrence transform
applied.” Controlled COLUMN_400 shows its one source LWP and base marker.
Separate nonzero-base fixture proves native pointX1 is not shifted by baseX12.5;
nested INSERT remains a single anchor with no recursion.

## 7. Supported diagnostic primitives

LINE source XY segments;POINT cross marker;SOLID four raw corner slots in source
order;CIRCLE center/radius;ARC source-degree screen arc;LWP ordered vertex chords,
closing only on explicit source flag. Nonzero bulge produces a visible Inspector
notice: chord display only/curved geometry not interpreted. No bulge conversion,
width interpretation,cleanup or CAD-fidelity claim. Invalid/absent XY and
Unsupported source records create no fake primitive.

## 8. INSERT policy

Purple insertion cross only; exact target,rotation,scale/array/normal facts are in
Inspector. Absent scale stays absent, never fabricated1.0. Same policy in both
spatial modes. No target block placement,base-point subtraction,matrix,anonymous
dimension block expansion,MINSERT cells or occurrence source identities.

## 9. Annotation policy

TEXT/MTEXT insertion anchors and raw content/chunks in Inspector. DIMENSION shows
its existing definition point and exposes type/code70/style/block/override/
encoded measurement/other facts. No calculated measurement,SHX/font loading,text
extents or dimension graphics. Missing facts are clearly absent; no rendering
defaults are written back into source facts.

## 10. Screen projection boundary

Desktop-only DiagnosticCamera translates/uniformly scales/inverts screenY.
WPF ARC presentation alone uses source degrees→screen angle trigonometry. Neither
enters Raw/Dxf or becomes reusable normalized geometry. Camera is not extraction
truth or part of scene-model determinism digest. UI labels 2DXY with no Z/
extrusion/thickness transformation. Pixel marker sizes,hit widths,24px Fit padding
and finite10-million-pixel drawing guards are UI choices, not CAD tolerances.
Zoom/Pan/Reset are deterministic and leave serialized Snapshot unchanged.

## 11. Fit bounds policy

Bounds derive only from selected scope's valid renderable XY/center-radius facts;
never HEADER EXTMIN/EXTMAX. ARC uses conservative full-circle bounds. INSERT and
annotation anchor points participate as diagnostic positions. Block base marker
does not invent child bounds. No facts/no finite representable span→empty state;
no synthetic drawing extents. Fit uses final viewport dimensions after WPF layout.
Reset restores identity pixel camera; Fit restores framed source display.

## 12. Selection and source trace

Click hit-test uses screen-space stroke proximity and exact item→record reference.
The all-source list includes all333 children across all sections, even when a
record has no primitive in the current spatial mode. Inspector shows record index,
section,type,effective disposition,OriginalHandle/CanonicalHandle,layer,source span,
mapped native facts with presence/validity/source-pair indices,Owner/other reference
roles/resolution and located diagnostics. Full source-range trace gives mapped
field names,pair index,original code spelling,physical code/value lines and raw
value, including unmapped pairs. LINE/LWP/INSERT/TEXT/DIMENSION/handle/reference
trace paths tested. Scope changes refresh the selection notice; base marker has
its own truthful notice, not “no primitive.” No source handle replacement.

## 13. Evidence-derived extraction summary

UI displays bytes/lines/pairs/sections/children,Supported/Unsupported/Coverage,
geometry/03D/definitions,nodes/references/owners and completeness flags. Actual
controlled screen:96621bytes,17474lines,8737pairs,6sections,333children,281Supported,
52Unsupported,89geometry,93raw03D,12definitions,327nodes,458references,317owners,
CoveragePartial;source/graph/reference/assembly complete and typed incomplete.
No production count constants. PHASE02 ReaderPending/integrity metadata is not
overwritten by an ephemeral viewer extraction.

## 14. Diagnostics and failure UI

Stage/code/source/message grid preserves historical diagnostic text and stage
attribution. Effective disposition comes from current Snapshot, not old Deferred
messages. Successful controlled extraction retains existing59stage03B diagnostics.
No fake geometry for52Unsupported (VPORT1+APPID3+OBJECTS48); source/Inspector remain.

Format,managed access,registered hash mismatch,lexical/structural/resource/cancel/
graph/assembly failure exposes stage/status and located diagnostics,with no
Snapshot/scene. LOW-01 reaches strict03F refusal with original03E warnings visible.
Unexpected Viewer projection errors clear the result and show a Viewer diagnostic,
with Trace fallback; no valid-empty success or intentional fatal shell exception.
Changing context clears scene,records,Inspector,summary,diagnostics and camera text.

## 15. Threading, cancellation and stale safety

Explicit extraction runs on Task.Run; async file/hash reads and synchronous pure
reader loops use the existing token. UI Cancel,window close and changed context
cancel the worker. Existing immutable parser contracts are unchanged. Captured
projectId/manifest/drawingId/revision/hash plus UI request epoch reject stale
success AND failure before applying controls. Shell changes schedule a current-
context check; no global session redesign/cache. Old invalidated viewer instructs
reopening for the new selection. No automatic read on selection/ordinary Refresh.

## 16. Managed-file and provenance policy

Only safe AssetPaths.Resolve(actual manifest,existingProjectId,existingDrawing)
opens. FileAccess.Read/FileShare.Read lease spans size-cap check,SHA256 comparison
to registered FileHash,rewind and03A→03F. Input bytes above existing lexical cap
refuse before hashing. Streams are disposed on success/failure/cancel. Actual
managed lease test denies concurrent write access; exact bytes remain unchanged.
No SourceFilePath access/fallback,including a valid original or UNC source. DWG
refuses before any stream open and gives external Text DXF export guidance.
Provenance passes existing DrawingId/RevisionId/FileHash; no new source identity,
DWG reconciliation,project dirty state or persistence/schema change.

## 17. Automated tests and builds

Baseline736/736. Final **773/773 PASS**,0failed/0skipped:37 new viewer cases,
all prior03A55/03B103/03C63/03D78/03E88/03F36/03G178 and135legacy+architecture
cases passing. Existing architecture test's Desktop forward allowlist alone was
extended to reflect the approved references; no existing assertion was removed.

New37 cover managed pipeline/project/source/provenance,missing managed+zero source/
UNC access,DWG no-open refusal,same/disposed lease/pre/during cancellation,hash/
byte cap/lexical/structure/graph/LOW-01 refusal,independent ENTITIES inventory,
all block children/nonzero local base/nested anchors,six primitive kinds,closed/
bulge policy,camera/resize Fit/overflowing-circle empty bounds/immutability,5source trace families,summary/
Unsupported/frozen scene,three cultures/determinism,context/epoch,actual write-
denying lease,and manual fixture preparation via existing project APIs.

**Core Release PASS/full Solution Release PASS/exact existing Revit warning gate
PASS.** VisualStudio2022 MSBuild/Roslyn fallback with SDK8.0.416 files/VSTest and
.NET8.0.22 testhost. Known dotnet CLI discovery message remains; actual fallback
exit0/TRX proves execution. No security/toolchain change,new package or remote CI
claim. No Revit host test required/claimed because IPC/Revit code is unchanged.

## 18. Actual WPF host smoke verification

Ran built Release DtoB.exe with computer-use. Initial Open file dialog could not
be filled by the tool (`element131 is not available in cached app state`); owner
opened the prepared `manual/Viewer.dtob` and confirmed. Subsequent restart used
Recent Projects without dialog assistance. Fixture project was created/saved and
both real controlled DXF/DWG registered through existing public project APIs.

| Step | Actually observed |
|---|---|
| Launch/open/select managed DXF/open Viewer/Run |Release app,project restored,two drawings;Snapshot Succeeded|
| Summary/ENTITIES |Evidence counts above;raw lines/LW visible;114diagnostic records|
| Fit/Zoom/Pan/Reset |Final autoFit scale0.025606;wheel0.030728;drag changes pan;Reset1/0,0;Fit restores framing|
| LINE canvas selection |record191/HandleB7/A-GRID;pair3671..3682;physical7343..7366;native endpoints/source strings|
| INSERT canvas selection |record199/HandleC3,targetD_SINGLE_900;insertion4000/2550/0;rotation90;scale absent;no expansion|
| Block Definition/COLUMN_400 |Separate local square/source LWP;red base marker;explicit no-occurrence-transform notice|
| Base marker source trace |record116/Handle96;BasePoint0/0/0;pair2808..2820;correct marker notice after fix|
| Unsupported |record332OBJECTS/DICTIONARYVAR/Handle160;explicit Unsupported,located diagnostic/raw pairs;no primitive|
| Viewer close |Desktop remains stable and project clean|
| DWG selection/Viewer command |Text DXF required guidance;no native decode/Viewer scene|
| Restart/final Fit regression |Recent restore;full controlled drawing automatically framed after summary layout|
| Selection invalidation |Old scene/summary/Inspector disabled/cleared on drawing change;final cleanup verification recorded in stale evidence|

Screenshots and accessibility traces under
`artifacts/phase03/03h-viewer-20261015/manual/`:final-entities,final-zoom-pan,
final-line,final-insert,final-column400,final-column-base,final-unsupported,
final-fit-reset,dwg-guidance,stale-invalidation. Earlier non-final captures are
retained separately. No exhaustive UI stress,DPI,pixel-golden or cancellation-
timing claim. Automated tokens/epoch checks complement actual host smoke.

## 19. Determinism

Serialize the ENTITIES scene followed by all12 block-definition scenes in source
order;source references excluded from JSON in favor of existing record locators.
Two fresh complete parses per culture,invariant/fr-FR/ko-KR,produce identical
model JSON. Reviewed source-scope/count/native-coordinate assertions independently
anchor the digest; WPF pixels are not hashed. Authoritative presentation digest:

`9D179A0EFAC5EE803E8AEC2AC3C48E1AB0986F1C6159BCB1764E8071933963A7`.

No window size,DPI,camera,timestamp,random ID or machine path in scene model.
Camera is separately tested. Scene/model/Snapshot collections immutable.

## 20. Dependency and scope audit

Raw remains pure runtime/no ProjectReference or PackageReference;Dxf references
only Raw. Both production packages byte-identical to before03H. Desktop adds Raw
and Dxf forward references;Project remains runtime-only,unchanged. No reverse
Desktop/Project/WPF/Core geometry/Revit/vendor dependency. No external package.
Targeted scope scans find no normalization/WPF/vendor imports or trigonometric
placement in Raw/Dxf;view-only ARC/camera live exclusively in Desktop. Existing
strict dependency tests remain passing with the explicitly updated forward list.

## 21. Controlled source hash

Before/after official unchanged:

`2676958463BB8BA183A6E255B0032495C4AD2958E3AF43BEB809015B71DB30BC`.

All existing dataset files/manifest remain unchanged. Manual operations use an
ignored managed copy;tests/source hashes confirm no source/managed/project write
from extraction. Existing file hashes audited;only approved shell/test/checklist
files differ. No historical report/review/ADR edit.

## 22. Snapshot hash regression

Unchanged03G fixed supplied provenance drawing-fixed/revision-fixed/official hash
still serializes to:

`4B91C83791F89DEC9BDC633C832001F2D1E537E7F0330BEE0666067CAE9A310C`.

This regression is separate from the real project Snapshot whose supplied IDs
come from its registered project. Viewer creates no replacement source identity.
Scene hash intentionally excludes that caller context. Final03G regression ran
unchanged as part of773tests,including original86fixture hashes/42limit matrix.

## 23. Known limitations, fixes and diff review

- Diagnostic sourceXY only;no unit/frame/OCS/3D/occurrence/style interpretation.
- Bulges display chords;SOLID raw corner order may not match a CAD application's
  filled face;ARC bounds conservative;no text/dimension font fidelity.
- Optional layer filter/toggles omitted to keep03H minimal;explicit layer facts
  remain inspectable. All source records list remains unfiltered.
- No viewer persistence/cache/autoread;PHASE02 ReaderPending stays separate.
- Extreme/unrepresentable screen arithmetic is refused/cut off;source evidence
  remains available. Reset is identity camera;Fit frames current scope.
- Pre/mid-read cancellation and epoch/context are automated;not every scheduling
  race or CPU-loop token point is host-injected.03G's approved limitation remains.
- Native Windows file-dialog tooling needed one owner action;no manual steps are
  represented as automated. Tool screenshots can lag accessibility one UI tick;
  stable observations and saved traces were used for conclusions.

Host smoke found early autoFit using pre-summary layout dimensions;fixed by
post-layout Fit and reran resize regression/full build/tests/actual app. Also
fixed scope selection notice/base-marker notice and invalidation diagnostic/
camera cleanup. Final diff review also guarded center±radius overflow so a CIRCLE
cannot fabricate center-only Fit bounds; raw facts stay inspectable and a
dedicated regression verifies the empty/unrepresentable state. No Raw/Dxf defect
or architecture bypass. Initial QA fixture
path/filename mistakes were corrected,not suppressed. Final all tests pass.

Reviewed task diff against before hashes,including untracked new files;no accidental
refactor/source changes. `git diff --check` PASS with only existing LF→CRLF
notices. StrictUTF8/trailing-whitespace check supplements untracked diff review.
No generated evidence/binaries staged;no commit/push.

## 24. LOW-01 unchanged

KNOWN_V1_POLICY:ReferenceResolutionComplete=false→03F refusal even when owner
graph remains valid. Viewer exposes03E warning/03F refusal and publishes no scene.
No viewer-specific bypass,reference repair,policy relaxation or synthetic Snapshot.

## 25. Explicit noPHASE04

No normalized CAD IR,unit→mm/model radians,OCS/WCS/AAA,INSERT/base-point/nested
transform,effective ByLayer/ByBlock,cleanup,snapping,deduplication,topology,
recognition,BIM,Revit,SDK or DWG conversion. View-only screen math is isolated.
Project v2,CAD IRv1/BIM/Analysis/Revit/accepted prior ADRs remain unchanged.

## 26. Readiness and stop

Ready for owner and independent Antigravity03H review with the limitations above.
Local **PASS_WITH_KNOWN_LIMITATIONS**;no independent verdict/fullPHASE03 final
gate/commit/PR/tag implied. Evidence directory:
`artifacts/phase03/03h-viewer-20261015/` baseline/final build logs/TRX,scene/hash
records,Snapshot regression,protected/dependency/scope/diff audits and actual
manual screenshots/traces. Checklist receives only03H append.

**STOP. NoPHASE04,no commit/push.** After independent PHASE_03H_PASS,a separately
authorized whole-PHASE03 final gate is required before commit/PR/tag.
