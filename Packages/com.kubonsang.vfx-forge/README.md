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
Generated-Prefab rules and report severity behavior are documented in
[`Documentation~/validation-pipeline.md`](Documentation~/validation-pipeline.md).
Preview Scene isolation, playback, and cleanup are documented in
[`Preview~/README.md`](Preview~/README.md).
Deterministic PNG and manifest output are documented in
[`Documentation~/frame-capture.md`](Documentation~/frame-capture.md).
The Run All workflow, failure gates, progress, and result navigation are documented in
[`Documentation~/editor-window.md`](Documentation~/editor-window.md).
The external command arguments, result JSON, Artifact layout, and exit codes are
documented in [`Documentation~/batchmode.md`](Documentation~/batchmode.md).
The five-Recipe end-to-end fixture and Template immutability checks are documented in
[`Documentation~/integration-fixture.md`](Documentation~/integration-fixture.md).

## Current Capabilities

- JSON Recipe parsing and normalization
- Deterministic generated-Prefab validation pipeline and severity-based reports
- Validated Template Catalog registration and Property Binding inspection
- Safe independent Prefab compilation with generated-only overwrite protection
- Isolated generated-Prefab Preview Scene with camera, playback, and cleanup
- Deterministic frame-time and front/side/top PNG capture with manifest output
- Validation rule pipeline
- Editor Window with Run All, progress state, and result navigation
- BatchMode entry point with stable arguments, JSON result, Artifact path, and exit codes
- Five-Recipe Batch integration fixture with Template and VFX asset immutability checks

## Not Yet Implemented

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
