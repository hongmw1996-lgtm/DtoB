# PHASE03E hand-reviewed source fixtures

Native AC1032 Text DXF, strict UTF-8/LF. All18 files exercise the actual
03A →03B →03C →03D →03E pipeline. Source names/handles are factual test values,
not generated graph nodes. OBJECTS stay semantically Unsupported.

| Fixture | Source / expected validation |
| --- | --- |
| duplicate-same | Two DICTIONARY A records in OBJECTS; both ambiguous, no winner. |
| duplicate-cross | POINT A in ENTITIES and DICTIONARY A in OBJECTS; same-document duplicate. |
| duplicate-case | A and a: different original spellings, canonical A conflict. |
| duplicate-zero | A and000A: original spellings retained, canonical A conflict. |
| malformed | G, empty and0 object handles; no valid canonical keys. |
| missing-required | AcDbEntity POINT with Owner A but no handle; MissingRequired. |
| root | DICTIONARY A with Owner0; null parent, root, no false unresolved error. |
| unresolved-owner | A has Owner FF; located error, incomplete graph, not a root. |
| unresolved-pointer | A has340 FF; warning and incomplete resolution, no ownership edge. |
| ambiguous-target | A/000a conflict and B Owner A; ambiguous target, no selected owner. |
| self-owner | A Owner A; one-node witness, edge retained, graph invalid. |
| owner-cycle | A Owner B and B Owner A; two-node witness, both edges retained. |
| pointer-cycle | A340 B/B340 A, both Owner0; no ownership cycle. |
| contextual-references | A has reactor330 B, XDictionary330/360 B, XDATA1005/330 B; all resolved, zero ownership edges. |
| null-reference |3400000 retained raw, canonical0/Null, no false missing target. |
| unsupported-target | DICTIONARY A Owner VISUALSTYLE B; source target resolves without semantic promotion. |
| identity-context | DIMSTYLE105 B identity versus5 LEGACY-NOT-HANDLE; TABLE header D, normal object A distinct. |
| multiple-owners | A330 B and330 C; both factual edges retained, no parent chosen, error. |

Tests additionally use inline Text DXFs for large string-form handles, foreign
subclass/control/XDATA exclusions, missing-owner profile, unknown sections,
nonidentity CLASS references, caller provenance, budgets and rejected versions.
No reconciliation, transforms, source repair, project I/O or synthetic handles.
