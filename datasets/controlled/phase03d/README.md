# PHASE 03D hand-authored Text DXF fixtures

All files use strict UTF-8, LF and AC1032 HEADER evidence. They pass through
the actual lexical → structural → geometry → block/annotation pipeline.
These are small source-profile regression fixtures, not CAD application exports
or evidence of universal DXF support. Pair indices/lines refer to the original
fixture contents; no fonts, transforms, source defaults or vendor objects.

| File | Reviewed source facts / expected coverage |
| --- | --- |
| blocks-nested.dxf | NONZERO base (12.5,-30,7), empty xref, nested and standalone INSERT (100,200,300), XYZ scale (2,3,4), rotation45 degrees, normal (0,1,0), one unchanged POINT child, anonymous *D99. No placement/expansion. |
| insert-array.dxf | INSERT I1, columns2/rows3/spacings10/20, unsupported expansion; following TEXT survives. |
| text-alignment.dxf | Korean/%%d/literal DXF escape, insertion/alignment, style, height, justification2/3, rotation90 and thickness.5. |
| mtext-chunks.dxf | Ordered code3,3,1; literal formatting and Korean text. Second chunk intentionally ends with a space. Direction, rectangle, attachment and spacing facts only. |
| attributes.dxf | Explicit66=1 INSERT → ATTRIB → SEQEND; independent ATTDEF in NONZERO. Value “explicit value” differs from “default only”; prompt/tag/flags retained. |
| attributes-malformed.dxf | Missing SEQEND before TEXT; sequence invalid, following TEXT survives. |
| dimension-aligned.dxf | Flags33/aligned subclass, anonymous block name, empty override, native extension points, encoded42=123.456 (not calculated), rotation22.5. |
| dimension-unsupported.dxf | Flags34/angular subclass; common facts retained, subtype-specific13/14/50 remain unmapped, unsupported disposition. |
| duplicate-nonfinite.dxf | Two code1 values and NaN/Infinity/overflow numeric values; invalid facts, all source occurrences retained. |
| foreign-contexts.dxf | Foreign target/X must not populate facts; correct target RIGHT/X1; controlled41 and XDATA42 remain unmapped, no defaults. |
| block-malformed.dxf | Nested BLOCK and unmatched ENDBLK; located invalid sequence evidence, following TEXT survives. |
| legacy-seqend.dxf | POLYLINE/VERTEX/SEQEND stays gated; second unassigned SEQEND unsupported, never an attribute terminator. |

Additional inline real Text DXF fixtures in TextDxfBlockAnnotationReaderTests
cover missing/explicit defaults, array subclasses, orphan/zero-follow attributes,
section boundaries, duplicates, missing MTEXT final chunks, source limits and
unsupported versions. These also use the full pipeline, not fabricated03D DTOs.
