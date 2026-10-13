# PHASE 02 controlled file fixtures
Owner-provided controlled two-story drawing; copied byte-for-byte, never edited. DWG (46,400 bytes) SHA-256: 3C2F4E880C63B2F247DD5D60EA2CB8D68BC72A99C505E233D88B446D59B9BAAE. Raw DWG format code is AC1024 despite the R2018 filename; no filename inference. DXF is text beginning 0/SECTION/2/HEADER. Units remain PendingReader in import metadata regardless of expected mm in the owner's drawing specification.

DrawingManagementTests also generates small synthetic header-only fixtures to exercise deterministic IO failures/identity. These are not structurally validated DWG models and must not be called parser fixtures.

Validation is BasicHeaderOnly: positive byte count above six, known six-byte DWG format code; text DXF beginning 0/SECTION/2/HEADER, with optional UTF-8 BOM and LF/CRLF. Binary/UTF-16 DXF and unrecognized headers fail explicitly. Only first 4 KiB is inspected during Add/Replace; no entity extraction or full-body corruption guarantee. Open/Refresh never reads these bytes.

Official format references are linked in PHASE_02_CHECKLIST.md. No proprietary SDK or runtime is shipped.
