# PHASE03F assembly fixtures

Two small hand-reviewed UTF-8/LF AC1032 sources exercise actual03A→03F.
They are separate assembly fixtures; older approved fixtures are unchanged.

- `circle-native.dxf`: reviewed03C circle source values retained; explicit
  common330=0 added before AcDbEntity to satisfy the03E owner profile. Handle A,
  center(-1.5,2.25,-3.5), radius2.5, thickness.125 and normal(.1,.2,1) stay raw.
- `blocks-native.dxf`: reviewed03D NONZERO definition/native placement facts,
  anonymous *D99, one POINT and nested/standalone INSERT retained. INSERT handles
  are valid unique hexadecimal C1/C2 for03E rather than03D's nonidentity test
  spelling I1. No coordinate/default/transform change. No occurrence expansion.

Older fixtures proved their narrower stages and are not automatically complete
graph inputs: circle-reviewed lacks required owner; blocks-nested has I1 handles.
03F tests use these separately labelled source files, not weakened graph checks.
Inline actual sources cover explicit Unsupported ELLIPSE/MINSERT preservation,
fully supported POINT, invalid typed facts and stage diagnostics. Controlled
whole-file expectations come from independently reviewed03A–03E evidence, not
a new03G formal QA campaign.
