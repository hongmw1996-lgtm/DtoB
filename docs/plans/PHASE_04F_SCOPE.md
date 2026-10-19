# PHASE 04F pre-implementation scope discovery — 2026-10-16

Owner authorized 04F only after PHASE_04E_PASS (zero findings). Baseline1,188 tests. Preserve root depth1, depth32/33 policy, occurrence IDs/transforms, accepted ADR bodies and independent reviews. Geometry transformation/materialization and CadDocument assembly remain04G.

## Actual retained-source capability / supported-channel matrix

| Channel | Actual Raw fact / DXF code | Absent/default semantics | ByLayer | ByBlock | Layer dependency | Occurrence dependency | 04F decision / reason |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Layer | Geometry/annotation Layer fact; retained contextual8; LAYER Name2 | Required source name; absent stays Absent, no invented0 | Not a layer mode | Not a layer mode | Unique named LAYER, OrdinalIgnoreCase; no trimming | Layer0 in a block adopts containing INSERT effective layer | Support, accepted plan/ADR |
| ACI color | Entity contextual62; LAYER AciColor typed62 and retained pairs | Absent entity62 means ByLayer |256 |0 | Effective layer62; signed negative layer value retains off flag, magnitude1–255 | Containing INSERT's already resolved color | Support1–255; source/special states typed; no palette/RGB approximation; root ByBlock unresolved per accepted plan |
| TrueColor | Entity contextual420; LAYER TrueColor typed420 | Absent does not synthesize RGB | Follows color inheritance when no explicit420 | Follows color inheritance when no explicit420 | Layer420 may override62 | Resolved parent RGB can propagate | Support integer0..0xFFFFFF;420 precedes62; malformed present420 never silently falls back |
| Linetype | Entity contextual6; LAYER LinetypeName6; LTYPE Name2 | Absent entity6 means ByLayer, not CONTINUOUS |BYLAYER |BYBLOCK | Unique LAYER then unique named LTYPE | Containing INSERT resolved linetype | Support name/source-record identity only; no dash/shape/font/pattern interpretation |
| Lineweight | Entity contextual370; LAYER typed370 | No invented width or Default policy | Special enum not interpreted here | Special enum not interpreted here | Not resolved | Not resolved | Deferred; accepted plan requires a separate explicit special/default policy before materializing widths. Preserve source evidence; present channel Unsupported |
| Transparency | Entity contextual440; table retained pairs if present | No fabricated opacity | Not interpreted | Not interpreted | Not resolved | Not resolved | Deferred; current typed IR lacks an accepted transparency mode/result contract. Preserve facts; present channel Unsupported |
| Color name / color book | Retained contextual430 | Absent stays Absent | Not interpreted | Not interpreted | Not resolved | Not resolved | Preserve; no color-book lookup. Without valid420, a present430 cannot produce a fabricated effective color |
| Visibility / layer off/frozen | Entity60; LAYER signed62 / flags70 | Entity60 absent means visible; missing table state remains unknown | N/A | N/A | Preserve table flags/off separately | No combined display filtering | Preserve factual states/default evidence; never remove an entity |

Entity fields are read once from existing LexicalEvidence.Pairs plus DxfStructuralRecord.ContextMarkers. Only AcDbEntity or genuine pre-subclass legacy context is eligible;102/1002 controls and1001 XDATA are excluded. MTEXT background/native color and foreign subclasses cannot become common420. Table fields use the appropriate symbol-table subclass. Duplicate eligible codes remain Invalid, rather than first-wins. No source reopen/relex/Raw parser extension.

## Verified rules / references

- [Autodesk common entity codes/defaults](https://help.autodesk.com/cloudhelp/2015/ENU/AutoCAD-DXF/files/GUID-3610039E-27D1-4E23-B6D3-7E60B22BB5BD.htm): entity8/6/62/420/430/440/60 mapping.
- [Autodesk block properties](https://help.autodesk.com/cloudhelp/2026/ENU/AutoCAD-LT-MAC/files/GUID-25E9F20C-D146-426C-8815-37DF48D2D33F.htm): Layer0/ByLayer and ByBlock inheritance applies also to nested blocks. Page fetched successfully into local evidence when search cache could not open it.
- [Autodesk420/62 precedence](https://help.autodesk.com/cloudhelp/2026/ENU/AutoCAD-AutoLISP-Reference/files/GUID-C7D27797-247E-49B9-937C-0D8C58F4C832.htm): conflicting true color wins over ACI. No AutoLISP dependency.
- [LAYER reference](https://help.autodesk.com/cloudhelp/2015/ENU/AutoCAD-DXF/files/GUID-D94802B0-8BE8-4AC9-8054-17197688AFDB.htm), [LTYPE reference](https://help.autodesk.com/cloudhelp/2017/ENU/AutoCAD-DXF/files/GUID-F57A316C-94A2-416C-8280-191E34B182AC.htm): factual catalog names/color/off/frozen; no rendering fidelity claim.

The accepted plan section15 deliberately requires missing/ambiguous catalogs and root ByBlock to remain unresolved; do not emulate AutoCAD import repairs or guess ACI7/CONTINUOUS. Layer substitution precedes ByLayer lookup. Nonzero child layer stays its own layer. INSERT resolves against its actual04E parent first. No definition recursion or style-dependent occurrence IDs.

## Smallest implementation shape / decisions

Add only `DtoB.Cad.Normalize/Styles/` contracts, contextual accessor and resolver, plus focused tests. Reuse unchanged CadColor, CadResolutionState, CadOccurrenceExpansionResult and its source registry. A source style is shared once per structural record. A projection key is the typed `(SourceRecordRef, OccurrenceId?)` pair; no new hash/ID policy is needed. Root entities have null occurrence. INSERT contexts follow04E DFS; direct non-INSERT definition children follow source order per occurrence. Store effective channels on these projections, never shared definitions.

Cache one immutable context per occurrence ID and indexed source/catalog declarations. Provenance stores compact factual evidence, supporting layer/ltype records and supporting projection keys, avoiding recursive Raw/value copies. Root projections, contexts and entity projections are ordered lists. Resolution work is linear in retained input plus emitted projections, with bounded dictionary lookup and no ancestry rescan. Introduce validated injectable style projection/diagnostic caps(250,000/5,000) and cancellation; this is not geometry-budget enforcement. Refusal/cancellation publishes no style artifact. Channel failures preserve valid channels and Partial state.

No new ADR: this implements accepted ADR018/020 style policy without a schema/identity/unit/convention change. No external library, DI, rendering, UI or CAD math changes. Required tests cover independent inheritance oracles, source context collisions/duplicates, invalid/missing symbols, partial/deferred channels, shared-definition isolation, provenance/immutability, cancellation/resources, official controlled facts, unchanged04A–E evidence and1,188 stable baseline IDs, culture/process/CWD byte equality. Stop after report for independent Antigravity review; no04G, commit or push.
