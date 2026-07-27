# Architecture Notes

## Dependency Direction

```text
Runtime Data <- Editor Recipe/Catalog <- Compiler/Validation <- UI/CLI
```

Runtime code must not reference `UnityEditor`.
Editor UI must delegate parsing, compilation, validation, and reporting to separate classes.

Preview orchestration belongs to the Editor assembly. Runtime playback remains in
`VfxPlayer`, which has no `UnityEditor` dependency.

`VfxForgePipelineRunner` owns Run All ordering and failure gates. `VfxForgeWindow`
observes progress and exposes result navigation without implementing pipeline logic.

## Asset Safety Boundary

- Template assets are read-only inputs.
- Generated Prefabs must include `VfxMetadata`.
- `OverwriteGeneratedOnly` may overwrite only Prefabs containing `VfxMetadata`.
- Temporary instances are destroyed in `finally` blocks.

## Preview Scene Contract

- Preview accepts only a persistent Prefab asset containing `VfxMetadata`.
- The Prefab must contain at least one `VisualEffect`.
- `EditorSceneManager.NewPreviewScene` creates an isolated, pathless Scene.
- The generated Prefab is instantiated only inside that Scene.
- A fixed, disabled Camera is created for later explicit frame rendering.
- `VfxPreviewSession.Dispose` closes the Preview Scene and removes all temporary objects.
- The active Scene, its dirty state, and the generated Prefab asset remain unchanged.

## Frame Capture Contract

- Each requested time and view starts from a reinitialized Preview instance.
- The Camera renders to a temporary `RenderTexture`; CPU-side PNG data is written only
  after encoding succeeds.
- Stable frame names use sorted time, canonical view order, and microsecond time values.
- The manifest is written after every PNG and playback restoration succeeds.
- Existing outputs are rejected before rendering.
- Failed attempts remove the files they created and return a failed result.

## Run All Contract

- Input validation completes before Prefab compilation.
- Every failed stage prevents subsequent Compile, Preview, or Capture operations.
- Failure reports are evidence output and do not resume the generation pipeline.
- Preview sessions are disposed on both Capture completion and exceptions.
- The run result preserves generated Prefab, report, and capture paths for UI navigation.

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

Contact sheets, profiler metrics, and graph composition are intentionally left for later
tasks. The compatibility capture fixture proves structural rendering, not VFX visual
quality. Do not hide these gaps with fake success values.
