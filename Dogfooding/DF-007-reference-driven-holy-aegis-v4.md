# DF-007 — Reference-driven Holy Aegis V4

## Record

- Record ID: `DF-007`
- Pipeline Tasks: `VF-020` through `VF-023`
- Current phase: First concept set rejected; enemy-facing revision brief prepared
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

## VF-021 candidate set

Four camera-locked 1536×1024 boards were generated with the built-in image tool. Each
board contains grayscale, color, three-ground, and layer-breakdown evidence.

- A — Faceted Bastion:
  `Dogfooding/Evidence/VF-021-concepts/candidate-a-faceted-bastion-board.png`
  - Strength: dominant plate and strong material separation.
  - Risk: braces still resemble attached gold blocks.
- B — Cathedral Wing:
  `Dogfooding/Evidence/VF-021-concepts/candidate-b-cathedral-wing-board.png`
  - Strength: strongest rim-to-ornament structural logic.
  - Risk: wings overtake the plate and approach an emblem silhouette.
- C — Fractured Ward:
  `Dogfooding/Evidence/VF-021-concepts/candidate-c-fractured-ward-board.png`
  - Strength: clearest asymmetry and mass hierarchy.
  - Risk: horizontal plane can be mistaken for a ground sigil.
- D — Solar Mantlet:
  `Dogfooding/Evidence/VF-021-concepts/candidate-d-solar-mantlet-board.png`
  - Strength: only candidate whose 35° tilt clearly communicates shield thickness.
  - Risk: four braces are not equally countable and the split suggests two lobes.

Input and output hashes are recorded in
`Dogfooding/Evidence/VF-021-concepts/concept-candidates.json`. The current
`concept-review.json` state is `selection_required`; no candidate has been selected by
automation.

## Next review gate

The project owner rejected the entire A-D set. Candidate D was explicitly the closest
direction because its barrier can face the enemy, but it was not selected or approved.
The decisive failure was orientation: the other shields exposed their primary faces to
the sky and therefore read as ground-facing emblems from the top-down camera.

The rejection is stored in `concept-review.json` with an empty `selectedCandidateId`.
The original candidate boards, manifest, prompt set, Board, and Brief remain unchanged
as historical evidence.

## GNF_ reference study and revision

The project owner requested that the next Shader and effect language study the sibling
GNF_ project. The inspection was read-only, and no third-party source Asset or screenshot
was copied into this public repository.

- PixPlays Water Shield contributed the production principle of separating a primary
  Mesh surface, internal noise, boundary response, ground support, and sparse droplets.
- Hovl Magic Shield Holy Loop confirmed the value of independently timed visual layers,
  while its repeated rune and magic-circle language remains explicitly forbidden.
- Existing GNF_ lab captures reinforced that dense glyph detail collapses into an icon
  at gameplay scale and cannot compensate for a weak silhouette.

Detailed evidence and transfer rules:
`Dogfooding/Preproduction/DF-007/gnf-vfx-reference-study.md`

The next candidate set must use:
`Dogfooding/Preproduction/DF-007/art-direction-brief-v2.json`

The second Brief changes the primary contract from a tilted disc to an enemy-facing
interception plane. Candidate D contributes only that directional premise. Its exact
outline, split lobes, and ornament layout are not approved.

## Next review gate

VF-021 remains in progress. A second camera-locked concept set must be generated from
the V2 Brief and reviewed by the project owner. VF-022 still cannot create any Unity
Mesh, Shader Graph, or VFX Graph Asset until one new candidate passes all five concept
criteria with matching, non-stale hashes.

## Rejection update verification

- JSON parsing: passed for the review record, V2 Brief, and `feature_list.json`.
- Historical manifest hash: unchanged at
  `051a4723c053825bca58cb0c84207c6ebf321fb401bc46f364f7ac8ba71ea2d8`.
- V2 Brief SHA-256:
  `c34142035aae49d3fcb07f31a3074988ef438ab24e13dd0adb52dc80157c4feb`.
- `git diff --check`: passed.
- Targeted `VfxConceptReviewTests`: attempted, but the Unity 6000.3.8f1 batch process
  could not reconnect to the local Licensing Client and was stopped. The last completed
  VF-021 run remains 187 passed, 0 failed, with Console errors 0; this documentation-only
  rejection update does not replace that historical result or claim a fresh Unity run.

## VF-021 candidate set V2

Four new 1536×1024 boards were generated from the V2 Brief. Candidate D was supplied to
the image generator only as an orientation and board-layout reference. No first-set
candidate was promoted or edited into an approved concept.

- E — Cavalier Wall:
  `Dogfooding/Evidence/VF-021-concepts-v2/candidate-e-cavalier-wall-board.png`
  - Strength: clearest single continuous barrier and layer separation.
  - Risk: heavy roots and regular frame can read as permanent architecture.
- F — Winged Bulwark:
  `Dogfooding/Evidence/VF-021-concepts-v2/candidate-f-winged-bulwark-board.png`
  - Strength: strongest directional energy and most dynamic silhouette.
  - Risk: the center valley can still separate into two visual lobes.
- G — Lanceguard Prow:
  `Dogfooding/Evidence/VF-021-concepts-v2/candidate-g-lanceguard-prow-board.png`
  - Strength: restrained asymmetry and a clean continuous surface.
  - Risk: tall supports can read as a portal; the advance is subtle at thumbnail scale.
- H — Citadel Arc:
  `Dogfooding/Evidence/VF-021-concepts-v2/candidate-h-citadel-arc-board.png`
  - Strength: broad protective sweep and explicit enemy-facing curvature.
  - Risk: crown posts and gold mass push the design toward a fantasy gate.

The prompt and every board hash are stored in
`Dogfooding/Evidence/VF-021-concepts-v2/concept-candidates-v2.json`. The new review record
is `concept-review-v2.json` with status `selection_required`; automation has not selected
or approved a candidate.

The next review must first judge whether any candidate avoids the new common risk:
enemy-facing orientation is now readable, but a large framed plane can become a piece
of architecture instead of a temporally deployed VFX skill.

## Human concept approval

The project owner approved Candidate E — Cavalier Wall on 2026-08-03. All five concept
criteria passed, and the selected evidence remains bound to candidate manifest SHA-256
`674b5e324cf9aa44a3094822203fe28659be410c421fd327e5512921352646d7`.

The approval adds one mandatory implementation behavior:

- The barrier forward normal follows the player's current facing or aim direction.
- The wall surface therefore faces the intended enemy direction and must not stay fixed
  in world space after deployment.
- Rotation changes orientation only; it must not distort the approved E silhouette or
  turn the wall into an upward-facing plate.

This closes VF-021 and unlocks VF-022 grayscale Unity translation. It does not approve
the future Unity Mesh, Shader, timing, or production VFX result; those retain their own
shape and production review gates.

Approval record verification passed JSON parsing, manifest SHA-256 matching, selected
board SHA-256 matching, and `git diff --check`. A fresh targeted
`VfxConceptReviewTests` run was attempted, but Unity 6000.3.8f1 again lost its Licensing
Client channel before the test runner started; the process was stopped and the log is
stored at `/tmp/vf021-approval-editmode.log`. No fresh test or Console result is claimed.
