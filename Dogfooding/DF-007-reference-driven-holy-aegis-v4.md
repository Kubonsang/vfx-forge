# DF-007 — Reference-driven Holy Aegis V4

## Record

- Record ID: `DF-007`
- Pipeline Tasks: `VF-020` through `VF-023`
- Current phase: Reference Board and Art Direction Brief validated
- Production status: Not ready; no concept has been selected

## Why this iteration exists

DF-006 passed technical validation but failed the project owner's production review.
The shield meaning was readable, yet its flat overlapping shape, icon-like ornaments,
uniform emission, and concentric timing language evoked an older handheld-game effect.
Adding a better Shader to the same silhouette would preserve the weak foundation.

DF-007 therefore moves the first human gate ahead of Unity authoring. The order is now
reference analysis, shape brief, multiple concept candidates, human selection, Unity
grayscale translation, shape approval, and only then material/timing work.

## Reference research

The Board links to creator pages without copying their images into this repository.
Every item is `inspiration_only`; literal motifs, silhouettes, and texture patterns are
explicitly prohibited.

- [Thomas Bernardet — Energy Shield VFX](https://www.artstation.com/artwork/zP8EwZ):
  primary protective volume and value hierarchy.
- [MS Artworks — Stylized Force Field Shader](https://gokums06.artstation.com/projects/rlowNO):
  separation between surface motion and controlled edge response.
- [Naked Singularity Studio — Dark fantasy shield concept set](https://www.artstation.com/artwork/zPZJmq):
  authored depth breaks and structurally connected decoration.
- [Lucie Travaux — Mizuchi Shield VFX](https://lixsh.artstation.com/projects/9EymGq):
  concept-to-realtime breakdown and restrained ground integration.

These references are quality and process benchmarks, not a target style collage. The
new design uses an original solar-knight motif and must remain recognizably different
from every linked work.

## Shape direction frozen for concept exploration

- Primary mass: forward-weighted, faceted round shield plate with a broad upper crown
  and compressed lower point.
- Mass allocation: 68% primary plate, 22% connected structure, 10% negative space.
- Connections: four broad braces grow from the rim; crest is embedded; energy gaps
  terminate inside the frame.
- Depth: ground contact, recessed emerald volume, raised structural rim, restrained
  crest highlight.
- Material intent: deep nonuniform emerald energy, warm aged gold, and narrow contact
  occlusion. This intent does not authorize material implementation before shape
  approval.
- Forbidden: concentric ring stacks, flat yellow geometry, repeated runes, detached
  glyphs, UI-badge symmetry, particles that hide shape, and uniform emission.

## Contract artifacts

- Reference Board:
  `Dogfooding/Preproduction/DF-007/reference-board.json`
- Art Direction Brief:
  `Dogfooding/Preproduction/DF-007/art-direction-brief.json`
- Board SHA-256:
  `ad4f00bdec04dec423c3082a9aa623c9e1d1c508f81d74c6e4b2d8dfb35ab274`
- Gate status: `ready_for_concepts`

The status means only that the research inputs are complete and mutually consistent.
It is not an aesthetic approval.

## Next review gate

VF-021 must generate four strict-top-down candidates at a fixed 1920×1080 gameplay
context and approximately 360 px effect footprint. Each candidate needs a grayscale
silhouette, full-color concept, light/mid/dark ground composite, and labeled layer
breakdown. The project owner must select one candidate before VF-022 creates any Unity
Mesh, Shader Graph, or VFX Graph asset.
