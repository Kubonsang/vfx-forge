# DF-002 — Top Down Crescent Sword Slash

## Metadata

- Record ID: `DF-002`
- Task ID: `DF-002`
- Date: `2026-07-29`
- Operator: Codex
- Commit: pending (validation complete)
- Unity / URP / VFX Graph / Test Framework:
  `6000.3.8f1` / `17.3.0` / `17.3.0` / `1.6.0`

## Goal

- Real-use scenario: 탑다운 검 캐릭터가 `+Z` 방향으로 초승달 검격을 발사.
- Dogfooding question: top-down orthographic view에서 shape가 초승달로 읽히고 이동 방향이 명확한가?
- Acceptance criteria:
  - XZ plane 기준 초승달 silhouette.
  - TopDown Demo Camera에서 core/glow 분리 식별.
  - `+Z` 이동과 0.55초 lifetime.
  - Validation Error 0, Console Error 0.

## Scope

- Included: extruded crescent Mesh, core/glow Material, projectile, Template, Recipe, Catalog, top-down Demo Scene.
- Excluded: hit effect, damage, target collision, sound, camera shake, final particle polish.
- Source Template: built-in `03_Simple_Burst.vfx` project-owned copy.
- Recipe: `Dogfooding/Recipes/topdown_crescent_slash.json`
- Output Prefab:
  `Assets/VFXForge/Dogfood/TopDownSwordSlash/Generated/TopDownCrescentSlash.prefab`

## Safety Check

- Original Template unchanged: target exists면 authoring 실패.
- Original VFX Asset unchanged: built-in source는 Unity API copy만 사용.
- Existing user Asset overwrite: authoring target / evidence target 존재 시 실패.
- Package path writable: verified.
- asmdef / Namespace collision: no collision observed during Unity compile.

## Iteration

### Iteration 1

- Hypothesis: 112° annular sector와 white core/cyan outer glow가 top-down에서 초승달 검격으로 읽힌다.
- Major properties:
  1. `shape.radius = 1.72`
  2. `style.primaryColor = #0A61FFFF`
  3. `style.emissionIntensity = 4.0`
- Command / menu:
  `Tools/VFX Forge/Dogfood/Open Top Down Crescent Demo`, then Play. The demo
  auto-fires every `0.9` seconds; Space also fires once.
- Result: direct top-down Camera capture shows a cyan 224-degree crescent with
  a distinct white inner core. The projectile moves along `+Z`.
- Decision: retain this visual variant. No further major Property changes in
  this iteration.

## Verification Evidence

- Compile: Unity BatchMode authoring, pipeline, demo-scene, capture and
  console-probe commands completed without script compilation errors.
- Tests: EditMode `129/129 passed` (`failed=0`).
- Console: `errors=0`, `warnings=12`, `logs=0` in
  `UnityCompatibilityProject/Artifacts/vf-011-console.json`.
- Validation Report:
  `UnityCompatibilityProject/Artifacts/dogfood/DF-002-topdown-crescent/validation.json`
  (`status=passed`).
- Capture Manifest:
  `UnityCompatibilityProject/Artifacts/dogfood/DF-002-topdown-crescent/capture/capture-manifest.json`
  (`4` top-view frames, `status=passed`). Those isolated Preview frames are
  background-only and are not used as visual-quality evidence.
- Direct visual evidence:
  `Dogfooding/Evidence/DF-002-topdown-crescent-still.png` — rendered by the
  dedicated orthographic demo Camera and visually inspected.

## Product Findings

### Worked

- Unity API-only authoring safely copied the built-in VFX Graph, created a
  project-owned Template/Catalog, and generated the Recipe output Prefab.
- The dedicated top-down demo provides a reliable human-visible verification
  path despite the isolated Preview limitation.

### Friction

- Isolated Preview capture remains background-only for this mesh-based
  projectile, even while pipeline validation and capture-manifest generation
  pass. The current Preview implementation does not render this hierarchy.

### Defect / Gap

- Current Recipe binding cannot drive Mesh/Material/Transform values.

## Result

- Status: verified implementation.
- Generated Assets:
  `Assets/VFXForge/Dogfood/TopDownSwordSlash/Generated/TopDownCrescentSlash.prefab`,
  `Assets/VFXForge/Dogfood/TopDownSwordSlash/Demo/TopDownSwordSlashDemo.unity`.
- Limitations: no hit/damage, sound, camera shake, or particle polish; isolated
  Preview capture remains background-only. Recipe has no Mesh/Material/Transform
  bindings, so the three visual properties are documented contract values rather
  than runtime bindings.
- Next action: add component bindings and fix isolated Preview rendering for
  non-VFX-Graph mesh hierarchies.
