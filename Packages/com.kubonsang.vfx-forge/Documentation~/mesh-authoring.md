# ProBuilder Mesh Authoring

VFX Forge 0.2 can keep an editable ProBuilder Prefab as the source of truth while
exporting a runtime Prefab that contains only regular Unity Mesh assets. The workflow is
intended for shape review before Shader Graph, VFX Graph, or temporal polish begins.

## Contracts

- `mesh-reference-1.0` binds the selected concept and model sheet to four locked views,
  explicit camera position/target, normalized landmarks, part roles, scale, and SHA-256
  hashes. It also fixes the center/edge surface depth and structural frame thickness so
  the orthographic panels cannot be interpreted as unrelated flat silhouettes.
- `mesh-authoring-1.0` records the editable source Prefab, runtime output, topology
  budget, dependency hashes, and material zones.
- `mesh-review-1.0` stores a separate human decision for either the `model_sheet` or
  `blockout` stage. A changed input hash makes the decision stale.

The corresponding JSON Schemas are stored in the package `Schemas` folder. Existing
Recipe, Concept Review, Validation, and Visual Review contracts are unchanged.

## Source and runtime separation

`VfxProBuilderRuntimeExporter.Export` accepts three project-relative `Assets/` paths:
the source Prefab, runtime Prefab, and runtime Mesh folder. The destination paths must
not already exist. The exporter loads an isolated Prefab copy, compiles every
`ProBuilderMesh`, saves ordinary Mesh assets, removes ProBuilder components, and then
saves the runtime Prefab. It verifies that the source dependency hash is unchanged.

Do not edit the source Prefab or generated Mesh assets as YAML. Use ProBuilder and Unity
Editor APIs. A rejected blockout remains an editable source revision; it is not promoted
to a production Template.

## Review window

Open `Tools > VFX Forge > Vfx Mesh Review`.

1. Load a repository-relative `mesh-reference-1.0` manifest.
2. Select a reference view and lock the active SceneView camera.
3. Adjust reference opacity and inspect clay, wireframe, and normal captures.
4. Record all five criteria before accepting, or provide a rejection reason.

The window refuses to overwrite an existing capture or review record. Review approval
must come from a human reviewer; tests and automation may only validate its hashes and
criteria.
