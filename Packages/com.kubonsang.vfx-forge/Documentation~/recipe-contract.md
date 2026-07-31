# Recipe Contract 1.0 and 1.1

VFX Forge validates Recipe JSON in two stages:

1. `VfxRecipeParser` checks JSON syntax and the structural contract before deserialization.
2. `VfxRecipeValidator` checks semantic constraints and returns stable validation rule IDs.

The canonical machine-readable contract is
[`Schemas/vfx-recipe-1.0.schema.json`](../Schemas/vfx-recipe-1.0.schema.json).
Recipe 1.1 is defined by
[`Schemas/vfx-recipe-1.1.schema.json`](../Schemas/vfx-recipe-1.1.schema.json).

Recipe 1.0 remains unchanged. Recipe 1.1 adds optional typed data used by
multi-target bindings and later review stages:

- `motion.speed`, `motion.localDirection`
- `geometry.variant`
- `quality.minimumForegroundRatio`,
  `quality.maximumBorderForegroundRatio`, `quality.requireHumanReview`
- `capture.contexts`

Unity `JsonUtility` may serialize the new sections into a 1.0 object. Their
1.1 defaults are accepted for programmatic compatibility, but any non-default
motion, geometry, quality, or context override in a 1.0 Recipe is rejected by
semantic validation.

## Required fields

Every Recipe must provide:

- `schemaVersion`
- `id`
- `template`
- `outputPath`
- `timing.duration`
- `budget.maxParticles`
- `budget.maxDuration`

Unknown and duplicate fields are rejected. Field and array item types must match the
schema. Optional fields retain the defaults declared by the runtime data model and the
JSON Schema.

## Output path safety

`outputPath` must:

- be a project-relative path under `Assets/`;
- end in `.prefab`;
- contain no empty, `.` or `..` path segments;
- contain no drive separator (`:`) or control character.

Backslashes are normalized to forward slashes. The compiler repeats the same check
before calling `AssetDatabase`, so bypassing the validator does not permit an unsafe
write target.

## Parser error codes

| Code | Meaning |
| --- | --- |
| `RECIPE-FILE-PATH-EMPTY` | The Recipe file path is empty. |
| `RECIPE-FILE-NOT-FOUND` | The Recipe file does not exist. |
| `RECIPE-FILE-READ-FAILED` | The Recipe file cannot be read. |
| `RECIPE-JSON-EMPTY` | The JSON text is empty. |
| `RECIPE-JSON-MALFORMED` | The JSON syntax is invalid. |
| `RECIPE-JSON-ROOT-TYPE` | The JSON root is not an object. |
| `RECIPE-JSON-DESERIALIZE` | Unity could not deserialize a structurally valid Recipe. |
| `RECIPE-SCHEMA-MISSING-FIELD` | A required field is absent. |
| `RECIPE-SCHEMA-UNKNOWN-FIELD` | A field is not part of contract 1.0. |
| `RECIPE-SCHEMA-DUPLICATE-FIELD` | An object contains the same field more than once. |
| `RECIPE-SCHEMA-TYPE-MISMATCH` | A field or array item has the wrong JSON type. |

Semantic failures are returned by `VfxRecipeValidator` as rule IDs such as
`RECIPE-OUTPUT`, `RECIPE-DURATION`, and `RECIPE-MAX-PARTICLES`.

## Current limitation

The parser enforces the structural subset required by VFX Forge directly rather than
loading a general-purpose JSON Schema evaluator at runtime. Numeric ranges, string
patterns, uniqueness, supported capture views, and cross-field constraints are enforced
by `VfxRecipeValidator`.
