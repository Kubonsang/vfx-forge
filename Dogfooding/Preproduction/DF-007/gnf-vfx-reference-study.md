# GNF_ VFX Reference Study

## Scope and provenance

- Source: local sibling Unity project `GNF_`, inspected read-only on 2026-08-03.
- Purpose: study realtime layer separation, Shader controls, and temporal organization.
- Redistribution: no GNF_ Prefab, Material, Shader, texture, screenshot, or third-party
  package asset is copied into VFX Forge.
- Design use: abstract production principles only. Existing silhouettes, runes, magic
  circles, textures, and authored motifs must not be reproduced.

## Relevant sources

### PixPlays Water Shield

Inspected source:
`Assets/PixPlays/ElementalShields/WaterShield/Version_BuiltIn/WaterShield.prefab`

- Six GameObjects split the effect into a primary `WaterShield` Mesh, water particles,
  ground particles, droplets, and a secondary ripple.
- The primary surface uses a dedicated Shader Graph Material rather than a flat
  unshaded card.
- Its exposed Material data separates base noise, inner detail, offset deformation,
  two colors, opacity remaps, cell density, scroll speed, and dissolve.
- The Prefab combines one Mesh-rendered particle layer, a trail/non-rendering support
  layer, billboard particles, and a dedicated MeshRenderer for the shield body.
- Spawn and despawn AnimationClips are authored independently. The despawn curve holds
  most of its change until the last part instead of linearly fading every layer at once.

Transferable principle: build the shield as a readable primary surface, then assign
internal motion, boundary response, sparse accents, and ground contact to separate
jobs. A single uniformly emissive Material must not carry the whole effect.

### Hovl Magic Shield Holy Loop

Inspected source:
`Assets/Hovl Studio/Magic circles/Prefabs/Loop version/Magic shield holy loop.prefab`

- Fourteen ParticleSystems and fourteen renderers run coordinated one-second loops.
- The density comes from multiple independently rendered layers rather than one flat
  sprite.
- The literal rune and magic-circle language conflicts with DF-006 feedback and the
  current shield brief.

Transferable principle: use independent timing and intensity bands for surface, rim,
and transient accents. Repeated runes, radial glyphs, and concentric circles are a
negative reference and remain forbidden.

### GNF_ skill wrappers and gameplay evidence

The GNF_ skill library maps warrior fortress and cleric aegis skills to several Hovl
and PixPlays variants. Existing lab captures confirm that dense thin glyphs can look
ornate in isolation yet collapse into a bright emblem at top-down gameplay scale.

Transferable principle: judge material detail only after the enemy-facing barrier
silhouette survives a 1080p gameplay composite. Fine glyph density is not evidence of
modern finish.

## Rules for the second concept set

1. The shield plane normal follows the caster-to-target direction. It must not face the
   sky or present a complete heraldic disc to the top-down camera.
2. Candidate D contributes only its enemy-facing mantlet orientation. Its split lobes,
   ornament count, and exact outline are not inherited.
3. The primary barrier must read in grayscale before any Shader cue is considered.
4. Color concepts may reserve distinct zones for a deep energy body, controlled edge
   charge, structural gold, and minimal ground contact.
5. Internal flow uses broad dual-scale noise and a directional drift, not repeated
   runes or a full-surface uniform glow.
6. Deployment grows from the caster-facing root toward the target-facing edge; decay
   breaks from the outer edge back toward the root.
7. Sparks or wisps, if proposed later, are sparse accent layers and cannot repair the
   silhouette.

## Gate consequence

The first A-D set remains historical rejected evidence. It is not replaced or mutated.
VF-021 stays in progress until a new camera-locked candidate set is reviewed and one
candidate passes all five human concept criteria.
