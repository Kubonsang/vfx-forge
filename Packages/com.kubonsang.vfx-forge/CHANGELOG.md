# Changelog

## Unreleased

- Added the Editor Window Run All workflow with ordered stage progress and failure gates.
- Added generated Prefab, validation report, and capture manifest result navigation.
- Added end-to-end Editor pipeline tests for success and downstream-stage suppression.
- Added deterministic frame-time PNG capture for front, side, and top Preview views.
- Added capture manifests, playback restoration, output collision protection, and cleanup.
- Added Editor capture controls and render/encoding/failure-path EditMode coverage.
- Added an isolated, pathless Preview Scene with generated-Prefab playback and cleanup.
- Added a fixed Preview Camera rig and Editor Window controls for open, restart, and close.
- Added tests for active Scene isolation, Prefab immutability, and temporary object removal.
- Added the default generated-Prefab validation pipeline and Property Binding rule.
- Hardened rule execution against exceptions, null results, empty IDs, and duplicate IDs.
- Connected generated-Prefab validation and severity-based reports to Editor and BatchMode flows.
- Added independent Template Prefab duplication, Recipe preflight validation, and Metadata integration tests.
- Added generated-only overwrite protection and unique-path preservation for occupied user assets.
- Added atomic Template Catalog registration, duplicate detection, and Inspector validation.
- Added Recipe path/type, component target, and exposed-property inspection for Property Bindings.
- Blocked Prefab compilation when the selected Template Catalog is invalid.
- Hardened Recipe 1.0 parsing with required-field, unknown-field, duplicate-field, and type checks.
- Preserved optional defaults during JSON deserialization and aligned them with the JSON Schema.
- Added traversal-safe Prefab output path validation and stable parser error codes.
- Verified Unity 6000.3.8f1 compatibility with URP and Visual Effect Graph 17.3.0.
- Added EditMode coverage for the Visual Effect Graph runtime API used by VFX Forge.
- Added a minimal compatibility host project with deterministic package versions.

## 0.1.0

- Added UPM package skeleton.
- Added Recipe data contract and JSON Schema.
- Added Template Catalog and Property Binding models.
- Added parser, normalizer, validator, compiler draft, validation rules, Editor Window, and BatchMode entry point.
- Added starter tests and sample Recipe.
