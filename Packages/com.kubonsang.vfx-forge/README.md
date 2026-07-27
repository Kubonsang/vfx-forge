# VFX Forge

Template-first VFX authoring starter for Unity 6.

## Requirements

- Unity 6
- Visual Effect Graph package installed in the host project
- Universal Render Pipeline recommended
- Unity Test Framework for tests

The verified Unity 6 package matrix and compatibility test procedure are documented in
[`Documentation~/unity-6-compatibility.md`](Documentation~/unity-6-compatibility.md).
The Recipe 1.0 required fields, defaults, path rules, and stable error codes are documented
in [`Documentation~/recipe-contract.md`](Documentation~/recipe-contract.md).

## Current Capabilities

- JSON Recipe parsing and normalization
- Basic semantic validation
- Validated Template Catalog registration and Property Binding inspection
- Safe independent Prefab compilation with generated-only overwrite protection
- Validation rule pipeline draft
- Editor Window
- BatchMode entry point draft

## Not Yet Implemented

- Preview Scene playback orchestration
- Frame-accurate capture
- Contact Sheet generation
- Runtime particle count measurement
- GPU/CPU profiler integration

## Setup

Use `Tools > VFX Forge > Bootstrap Project Assets` to create:

```text
Assets/VFXForge/
├─ Config/
├─ Recipes/
├─ Templates/
├─ Generated/
└─ Artifacts/
```

Register project-owned VFX Prefabs in the generated Template Catalog.
