# Changelog

## Unreleased

- Added `reference-board-1.0` and `art-direction-brief-1.0` preproduction
  contracts with provenance, usage, silhouette, depth, and material-zone rules.
- Added a hash-bound `ready_for_concepts` gate and Editor Window validation UI.
- Added DF-007 link-only reference research for the rejected Holy Aegis redesign.
- Added hash-bound concept candidate and human selection contracts with locked camera,
  evidence-role, file-integrity, and stale-review validation.

## 0.2.0

- Added mandatory hash-bound human visual approval with separate
  `visual-review-1.0` records.
- Added review-required, rejected, stale, and accepted product states plus
  BatchMode exit codes 80, 81, and 82.
- Added Editor Contact Sheet opening and five-criterion Accept/Reject controls.
- Added allowlisted `VfxReviewContext` Prefabs with explicit Camera, effect
  anchor, caster, and target references.
- Added gameplay Context capture at isolated frame times, no-effect delta
  metrics, deterministic Contact Sheets, and `review-manifest-1.0` hashes.
- Added a 16:9 top-down dogfooding Context with light, medium, and dark grounds.
- Added deterministic capture-time bounds prepass and fixed orthographic/perspective
  framing with 15 percent padding.
- Added `IVfxPreviewTimeEvaluable` with a deprecated Recipe 1.0 reflection fallback.
- Added finite bounds, actual Recipe 1.1 particle-capacity, empty-frame, and clipping
  quality gates (`VAL-004`, `VAL-005`, and `VAL-007`).
- Extended capture manifests to 1.1 with per-frame foreground, border, and bounds data.
- Added backward-compatible Recipe 1.1 parsing and validation.
- Added typed Transform, Material PropertyBlock, Mesh allowlist, and explicit
  Adapter binding targets without arbitrary reflection.
- Added three valid and two invalid versioned Recipe integration fixtures.
- Added full Batch pipeline coverage through Prefab generation, Preview, Capture, and reports.
- Added Template, Catalog, and referenced VFX asset immutability assertions.
- Hardened the BatchMode entry point with required arguments and project-relative path resolution.
- Added one-line JSON results and stable exit codes for every pipeline failure stage.
- Added BatchMode contract tests and external `-executeMethod` invocation documentation.
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
