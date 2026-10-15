# Hand-authored PHASE 03B Text DXF fixtures

Synthetic source files, not controlled-building/export compatibility claims. UTF-8 without BOM, LF. Each two physical lines are a code/value pair. These small sources were inspected directly; expected outcomes below are specified independently of production parser serialization.

| File | Expected evidence/outcome |
|---|---|
| unsupported-version | Explicit AC1024; UnsupportedVersion |
| missing-version | Empty HEADER; MissingAcadVersion |
| duplicate-version | AC1032 then AC1024; both retained, DuplicateAcadVersion |
| unknown-section | UNKNOWN/CUSTOM raw range retained; Partial |
| nested-section | SECTION inside HEADER; NestedSection |
| missing-endsec | HEADER reaches EOF; MissingEndSec |
| missing-eof | Complete HEADER with no EOF; MissingEof |
| content-after-eof | Code1 extra after EOF; ContentAfterEof |
| malformed-header | Code70 before any code9 variable; MalformedHeaderVariable |
| duplicate-header | Two singleton HEADER sections; DuplicateHeaderSection |
| table-missing-name | TABLE followed by ENDTAB; MissingTableName |
| nested-table | TABLE before prior ENDTAB; NestedTable |
| missing-endtab | LAYER table reaches ENDSEC; MissingEndTab |
| unknown-table-count | CUSTOM declares2, contains1 entry; retained/Partial/count diagnostic |
| dimstyle-identity | HEADER HANDSEED FF is metadata; DIMSTYLE105=00AB identity,5=LEGACY setting |
| reactor-owner | LAYER330=B inside reactors;330=C outside is owner |
| korean-layer-style | LAYER name 벽체, STYLE name 한글스타일 preserved UTF-8; no font loading |

Additional embedded pair-text fixtures in TextDxfStructuralReaderTests cover extents/absences, XDictionary/XDATA, repeated raw fields, code70 ambiguity, malformed controls, boundaries, budgets and cancellation. No upstream DWG equivalence or other-version support is claimed.

## Antigravity H-01 supplemental fixtures

All three sources include105=00AB identity,5=LEGACY setting and explicit AcDbSymbolTableRecord/AcDbDimStyleTableRecord markers. Reviewed raw roles follow code2 Name's symbol-header run and code69 DIMTFILL's separate color run; next code40/code71 closes the respective run.

| File | Independent expected source facts |
|---|---|
| dimstyle-distinct-70-roles | Flags16; code69 mode2 then TextFillColor7; Supported, both70 pairs accounted exactly once |
| dimstyle-duplicate-standard-flags | Two symbol-header70 values16/32 before40; Flags Invalid, color7 Valid, InvalidTableValue/Invalid entry |
| dimstyle-duplicate-text-fill-color | Flags16 Valid; code69 followed by70 values7/8 before71; TextFillColor Invalid, InvalidTableValue/Invalid entry |

The earlier “code70 ambiguity” description records pre-review tests. The mapping bug treating different-role70 fields as duplicate flags is corrected; genuine same-role duplicates remain invalid. No full DIMSTYLE semantics or formatting engine.
