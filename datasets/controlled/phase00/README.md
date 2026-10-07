# Synthetic PHASE 00 fixtures

cad.json exercises all minimum CAD contracts, four basic primitive shapes, one block/INSERT occurrence, text, dimension and an unsupported proxy with retained properties and a source-linked warning. bim.json exercises eight BIM categories, a synthetic prediction and a confirmed manual room without artificial confidence. Dimensions follow the final plan's controlled 8000 x 6000 mm example.

These files are hand-authored contract fixtures, not parsed DWG or recognition output. Polymorphic `$kind`/`$category` metadata must precede data properties for System.Text.Json on .NET 8. Schema version is 1; unknown major versions are unsupported. Call Validate() after deserialization and before using contracts.
