# DF-001 — Basic Sword Slash

## Metadata

- Record ID: `DF-001`
- Task ID: `DF-001`
- Date: `2026-07-29`
- Operator: Codex
- Commit: pending
- Unity / URP / VFX Graph / Test Framework:
  `6000.3.8f1` / `17.3.0` / `17.3.0` / `1.6.0`

## Goal

- Real-use scenario: caster가 바라보는 `+Z` 방향으로 날아가는 청백색 초승달 검격.
- Dogfooding question: 새 Template과 Recipe만으로 실사용 Prefab, 검증, 캡처를 만들 수 있는가?
- Acceptance criteria:
  - 원본 built-in VFX Graph와 Template Prefab 불변.
  - 생성 Prefab에 검격 형상, particle burst, 전진/수명 동작 포함.
  - Validation Error 0.
  - Console Error 0.
  - `front` 4-frame capture 생성.

## Scope

- Included: project-owned VFX Graph 복제, 검격 Mesh/Material, projectile 동작, Recipe, Catalog, Pipeline.
- Excluded: 충돌 판정, 데미지, 사운드, 카메라 흔들림, 최종 아트 polish.
- Source Template:
  `Packages/com.unity.visualeffectgraph/Editor/Templates/03_Simple_Burst.vfx`
- Recipe: `Dogfooding/Recipes/sword_slash_basic.json`
- Output Prefab:
  `Assets/VFXForge/Dogfood/SwordSlash/Generated/SwordSlashBasicV8.prefab`

## Safety Check

- Original Template unchanged: V1–V8을 별도 Prefab으로 생성해 각 iteration 보존.
- Original VFX Asset unchanged: built-in `03_Simple_Burst.vfx`는 Unity API로 복제 후 원본 불변.
- Existing user Asset overwrite: authoring 단계에서 target 존재 시 실패.
- Package path writable: verified.
- asmdef / Namespace collision: compile 성공.

## Iteration

### Iteration 1

- Hypothesis: 넓은 cyan glow와 좁은 white core 조합이면 평범한 원거리 검격으로 읽힌다.
- Major properties:
  1. `shape.radius = 1.6`
  2. `style.primaryColor = #42C8FFFF`
  3. `style.emissionIntensity = 4.5`
- Command / menu: pending.
- Result: pending capture.
- Decision: pending.

### Iteration 2

- Trigger: Iteration 1 capture 4장이 모두 background-only.
- Major changes:
  1. arc mesh에 양면 triangle 추가.
  2. core material을 opaque URP Unlit로 변경.
  3. outer material을 opaque URP Unlit로 변경.
- Source preservation: V1 Template, Graph, Mesh, Material, Generated Prefab 유지.
- Result: URP 정상 구성 후에도 background-only.

### Iteration 3

- Trigger: Preview instance와 Shader는 활성이나 planar procedural Mesh가 렌더되지 않음.
- Major changes:
  1. planar procedural Mesh를 built-in volumetric cube segment arc로 교체.
- Source preservation: V1/V2 Template과 Generated Prefab 유지.
- Result: background-only.

### Iteration 4

- Trigger: built-in cube geometry도 V2 Material 사용 시 렌더되지 않음.
- Major changes:
  1. outer Material을 control fixture와 같은 shader default state로 재생성.
  2. core Material을 control fixture와 같은 shader default state로 재생성.
- Source preservation: V1/V2/V3 Template과 Generated Prefab 유지.
- Result: background-only. 동일 Material의 단일 control cube는 정상 표시.

### Iteration 5

- Trigger: Material은 정상이나 nested arc hierarchy가 capture에서 미표시.
- Major changes:
  1. arc segment를 중간 group 없이 root 직속으로 배치.
  2. segment 두께와 화면 점유 크기를 확대.
- Source preservation: V1–V4 Template과 Generated Prefab 유지.
- Result: background-only.

### Iteration 6

- Trigger: root 직속 segment도 capture에서 미표시.
- Major changes:
  1. Template의 pre-authored `VfxPlayer` 제거.
  2. Template의 `SwordSlashProjectile` 제거.
- Source preservation: V1–V5 Template과 Generated Prefab 유지.
- Result: background-only.

### Iteration 7

- Trigger: pre-authored runtime component 제거 후에도 background-only.
- Major changes:
  1. assigned Graph은 유지하고 Template의 `VisualEffect.enabled=false` 설정.
- Source preservation: V1–V6 Template과 Generated Prefab 유지.
- Result: background-only.

### Iteration 8

- Trigger: Graph 실행 차단 후에도 background-only.
- Major changes:
  1. arc hierarchy를 root 직속 blade cube 2개로 최소화.
  2. Graph child를 inactive로 설정.
- Source preservation: V1–V7 Template과 Generated Prefab 유지.
- Result: Pipeline capture는 background-only. Demo Scene Camera에서는 청백색 대각 검기 표시.

## Verification Evidence

- Compile: Unity BatchMode compile 성공.
- Tests: graphics BatchMode EditMode `129/129 passed`.
- Console: `errors=0`, `warnings=12`, `logs=0`.
- Validation Report:
  `UnityCompatibilityProject/Artifacts/dogfood/DF-001-sword-slash-basic-v8/validation.json`
  (`status=passed`).
- Capture Manifest:
  `UnityCompatibilityProject/Artifacts/dogfood/DF-001-sword-slash-basic-v8/capture/capture-manifest.json`
  (`4` frames). 모든 frame이 background-only.
- Demo Scene:
  `UnityCompatibilityProject/Assets/VFXForge/Dogfood/SwordSlash/Demo/SwordSlashDemo.unity`
- Direct visual evidence: [`Evidence/DF-001-demo-still.png`](Evidence/DF-001-demo-still.png).

## Product Findings

### Worked

- Recipe parse, Template resolve, generated-only overwrite, Validation, manifest/PNG 생성 성공.
- Demo Scene의 일반 Camera는 검기 Mesh를 정상 렌더.
- `Tools > VFX Forge > Dogfood > Open Sword Slash Demo` 메뉴 제공.
- PlayMode에서 0.8초 자동 발사, `Space` 수동 발사 제공.

### Friction

- built-in VFX Graph Template 4종의 Exposed Property가 모두 0개였다.
- URP package는 설치됐지만 project에 active URP Pipeline Asset이 없었다.
- Pipeline `passed`가 PNG의 실제 시각 content를 보장하지 않았다.

### Defect / Gap

- 현재 Catalog binding은 `VisualEffect` Exposed Property만 지원한다.
- Mesh, Material, Transform, projectile speed/lifetime은 Recipe로 적용할 수 없다.
- 1차 결과물은 해당 값을 authoring asset에 직접 설정한다. Recipe 값은 의도를 기록하지만 시각값의 단일 source of truth가 아니다.
- `VfxPreviewSession` capture는 V8 Generated Prefab을 background-only로 렌더하지만
  Demo Scene Camera는 동일 Prefab을 정상 렌더한다.
- `VfxFrameCapture`는 background-only PNG도 성공으로 판정한다.

## Result

- Status: prototype usable; visual capture gate failed. 최종 품질 완료 아님.
- Generated Assets:
  - `Assets/VFXForge/Dogfood/SwordSlash/Generated/SwordSlashBasicV8.prefab`
  - `Assets/VFXForge/Dogfood/SwordSlash/Demo/SwordSlashDemo.unity`
- Limitations:
  - V8 Graph child는 Template에서 inactive. Demo Controller가 PlayMode spawn 시 활성화.
  - Recipe-driven mesh/material/motion binding 없음.
  - 공식 Preview capture는 background-only.
- Next action:
  1. Preview Scene과 Demo Scene render 경로 차이 수정.
  2. background-only capture rejection rule 추가.
  3. generic Component/Material binding contract 설계.
