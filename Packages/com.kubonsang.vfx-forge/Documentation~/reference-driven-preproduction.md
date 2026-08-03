# Reference-driven preproduction

VFX Forge separates visual exploration from Unity implementation. A production VFX
must first describe its references and shape direction with two independent JSON
documents. The Unity shape, Shader, and timing stages consume only a validated and
human-selected concept in later tasks.

## Contracts

- `reference-board-1.0` records links or project-owned images, creator/origin,
  rights status, allowed usage, desired properties, and properties that must not be
  copied.
- `art-direction-brief-1.0` converts those observations into camera, gameplay
  footprint, silhouette mass ratios, negative space, connected motifs, depth layers,
  material zones, forbidden traits, and required concept evidence.
- The Brief stores the SHA-256 of the exact Reference Board JSON after BOM removal
  and CRLF-to-LF normalization. Editing the board makes the Brief stale.

The JSON Schemas live in `Schemas/`. Runtime semantic validation is available through
`VfxReferenceContractParser`, `VfxReferenceContractValidator`, and
`VfxPreproductionGate` in the Editor assembly.

## Reference safety

`sourceType` accepts only:

- `url`: an HTTP(S) link. VFX Forge does not download or redistribute it.
- `project_asset`: a traversal-free path below `Assets/` for an image the project can
  retain.
- `generated`: a generated image stored below `Assets/`.

Every reference needs a creator/origin and rights status. `usage` is one of
`inspiration_only`, `redistributable`, or `generated_owned`. A reference marked
`inspiration_only` informs qualities such as mass hierarchy or material separation;
it is not copied into package assets.

## Gate behavior

Open `Tools > VFX Forge > Open Window`, assign both JSON TextAssets, and select
`Validate Concept Inputs`. The gate returns `ready_for_concepts` only when:

1. both contracts are semantically valid;
2. Board and Brief IDs and Task IDs match;
3. the Brief contains the current Board hash;
4. three to six concept candidates are requested;
5. grayscale silhouette, full-color concept, three-ground composite, and labeled
   breakdown are all required.

This status means the inputs are complete enough to generate concepts. It is not an
aesthetic approval. Automated tests and agents must never select a production concept
on behalf of the human reviewer.

## Recommended workflow

1. Link or import references and record provenance before visual analysis.
2. Extract desired qualities and explicit avoid rules; do not ask to copy a specific
   artist or game asset.
3. Freeze the Board hash in the Art Direction Brief.
4. Generate three to six candidates at the same top-down camera and gameplay scale.
5. Compare candidates in grayscale before judging Shader finish.
6. Require human selection before producing Unity Mesh, Shader Graph, or VFX Graph
   assets.
