# Architecture Notes

## Dependency Direction

```text
Runtime Data <- Editor Recipe/Catalog <- Compiler/Validation <- UI/CLI
```

Runtime code must not reference `UnityEditor`.
Editor UI must delegate parsing, compilation, validation, and reporting to separate classes.

## Asset Safety Boundary

- Template assets are read-only inputs.
- Generated Prefabs must include `VfxMetadata`.
- `OverwriteGeneratedOnly` may overwrite only Prefabs containing `VfxMetadata`.
- Temporary instances are destroyed in `finally` blocks.

## Deliberate Gaps

Preview, capture, profiler metrics, and graph composition are intentionally left for later tasks. Do not hide these gaps with fake success values.
