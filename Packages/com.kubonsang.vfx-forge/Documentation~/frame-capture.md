# Frame Capture

`VfxFrameCapture` renders a validated Recipe capture plan from an active
`VfxPreviewSession`.

## Capture Contract

- Frame times are sorted ascending.
- Views use the stable order `front`, `side`, then `top`.
- Every view is simulated independently from time zero.
- Capture dimensions are limited to 64–8192 pixels per axis.
- A plan may contain at most 4096 output frames.
- The Preview playback state is restored after capture.
- Existing PNG files and `capture-manifest.json` are never overwritten.
- A render, write, or playback-restore exception returns `CAPTURE-FAILED`.
- Files created by the failed attempt are removed. Cleanup failures are included in the
  returned error message.

## File Names

PNG names include the Recipe ID, stable frame index, view, and time in microseconds:

```text
{recipeId}_f{index:000}_{view}_t{microseconds:00000000}.png
```

Example:

```text
arcane_impact_f004_side_t00100000.png
```

The capture directory also contains `capture-manifest.json`. It records the Recipe ID,
status, duration, dimensions, and an ordered entry for every PNG.

## Editor Workflow

1. Open `Tools > VFX Forge > Open Window`.
2. Select a Recipe and generated Prefab.
3. Open the Preview.
4. Select `Capture Frames`.

Outputs are written below `<Artifact Directory>/capture`.

The structural fixture verifies Camera rendering, PNG encoding, dimensions, viewpoints,
and the manifest. Visual quality must be evaluated with project-owned VFX Graph assets;
the compatibility fixture does not claim VFX quality.
