# Data Model

## CAD IR

```json
{
  "id": "cad-001",
  "source_handle": "1A2B",
  "entity_type": "LINE",
  "layer": "A-WALL",
  "geometry": {},
  "properties": {}
}
```

## BIM IR

```json
{
  "id": "bim-wall-001",
  "category": "Wall",
  "level_id": "level-01",
  "geometry": {},
  "parameters": {},
  "confidence": 0.95,
  "evidence": [],
  "source_entities": ["cad-001", "cad-002"]
}
```

Relationships: HOSTS, CONNECTED_TO, ALIGNED_WITH, BELONGS_TO_LEVEL, BOUNDED_BY, MATCHES_VIEW, ANNOTATED_BY, DERIVED_FROM.
