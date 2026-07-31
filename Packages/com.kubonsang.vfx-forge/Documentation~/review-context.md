# Gameplay Review Context

Recipe 1.1 may request allowlisted gameplay contexts through
`capture.contexts`. Values are Catalog IDs, never Scene or Asset paths.

## Context Contract

Each Catalog `VfxReviewContextEntry` references one persistent Prefab containing
`VfxReviewContext`. The component explicitly references:

- the disabled Camera used for capture;
- the Transform that receives the generated effect instance;
- the caster Transform;
- the target Transform.

The validator rejects duplicate or invalid IDs, non-Prefab objects, missing
components, missing references, and references outside the Context Prefab.
The Pipeline instantiates both the Context and generated effect in a temporary
Preview Scene. It never saves the instance, changes the source Prefab, or dirties
the active Scene.

## Capture and Contact Sheet

Gameplay frames use the same sorted frame times and dimensions as the isolated
capture. Context IDs are sorted ordinally, and the review manifest orders each
time as:

1. isolated `front`, `side`, and `top` frames requested by the Recipe;
2. gameplay Context frames sorted by Catalog ID.

Gameplay foreground and clipping metrics compare the rendered frame to the same
Context rendered without the effect. Static ground, caster, and target pixels
therefore do not create false-positive foreground.

When at least one Context is requested, the Pipeline writes:

```text
Artifact Directory/
├─ capture/
│  ├─ *.png
│  └─ capture-manifest.json
└─ review/
   ├─ contexts/
   │  └─ *.png
   ├─ contact-sheet.png
   └─ review-manifest.json
```

`review-manifest.json` uses `review-manifest-1.0`. It records the deterministic
frame order, time, source kind and ID, foreground and border ratios, per-frame
SHA-256, isolated manifest SHA-256, and Contact Sheet SHA-256.

## Default Dogfooding Context

The Unity compatibility project contains a 16:9 top-down Context with explicit
caster, target, effect anchor, and light/medium/dark grounds:

```text
Assets/VFXForge/Dogfood/ReviewContexts/TopDownThreeGrounds.prefab
```

Recreate it through
`Tools > VFX Forge > Dogfood > Create Default Review Context`. The authoring
command does not overwrite an existing Prefab.
