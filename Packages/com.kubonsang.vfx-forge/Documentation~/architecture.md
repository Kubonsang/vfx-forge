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

## Prefab Compiler Contract

The compiler validates the Recipe and the complete Template Catalog before creating
folders or scene objects. It then:

1. instantiates the registered Template Prefab;
2. unpacks the Prefab connection to produce an independent root;
3. applies only registered Property Bindings;
4. aborts before saving if any required Binding fails;
5. adds or updates one `VfxMetadata` component;
6. creates the destination folder and saves the generated Prefab.

Overwrite policies are:

- `Fail`: reject every occupied output path.
- `OverwriteGeneratedOnly`: replace only a Prefab containing `VfxMetadata`.
- `CreateVariant`: preserve the occupied path and generate a unique output path. Despite
  the legacy enum name, the saved output is an independent regular Prefab rather than a
  Unity Prefab Variant.

## Deliberate Gaps

Preview, capture, profiler metrics, and graph composition are intentionally left for later tasks. Do not hide these gaps with fake success values.
