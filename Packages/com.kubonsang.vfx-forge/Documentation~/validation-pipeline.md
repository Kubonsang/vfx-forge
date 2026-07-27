# Validation Pipeline

`VfxValidationPipeline.Run` executes the default generated-Prefab rules in a stable order:

| Rule ID | Rule | Failure condition |
| --- | --- | --- |
| `VAL-001` | Missing assets | Missing script, VisualEffect component/asset, or Renderer material |
| `VAL-002` | Property Binding | Unresolved Recipe value, invalid component target, or missing/wrong exposed property |
| `VAL-003` | Duration budget | Recipe duration exceeds the effective Recipe/Profile budget |
| `VAL-006` | Layer support | A requested semantic layer is unsupported by the Template |
| `VAL-008` | Light policy | A Light exists while the effective Recipe/Profile policy forbids it |

The runner does not stop after a failed rule. A rule exception, null result, null rule
collection, empty result ID, or duplicate rule ID becomes a deterministic validation
failure rather than being hidden.

## Report status

`VfxReportWriter.ResolveStatus` maps results as follows:

- any failed `Error` result: `failed`;
- otherwise, any failed `Warning` result: `warning`;
- otherwise: `passed`.

Every generated failure is required to carry a non-empty `ruleId`. Result factories use
`VALIDATION-UNSPECIFIED` only as a defensive fallback for callers that omit an ID.

## Pipeline integration

The Editor Window and BatchMode entry point run the default pipeline after a Prefab is
successfully compiled, then include all rule results in `validation.json`. A post-compile
validation failure marks the report as failed but retains the generated Prefab as an
inspection artifact; it is not silently deleted or reported as successful.

Each rule currently returns one summary result. Property Binding validation prioritizes
required failures over optional-property warnings.
