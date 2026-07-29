# DF-003 — Production Crescent Slash Template V1

## Metadata

- Record ID: `DF-003`
- Task ID: `VF-012`
- Date: `2026-07-29`
- Operator: Codex
- Commit: VF-012 single task commit on `main`
- Unity / URP / VFX Graph / Test Framework:
  `6000.3.8f1` / `17.3.0` / `17.3.0` / `1.6.0`

## Goal

- Real-use scenario: 탑다운 검 캐릭터가 전방으로 백색 코어와 청록 에너지의 초승달 검기를 발사한다.
- Dogfooding question: VFX Forge Template과 Recipe Binding만으로 실사용 가능한 변형과 검증 근거를 만들 수 있는가?
- Acceptance criteria: VF-012 acceptance와 동일하다.

## Scope

- Included: project-owned VFX Graph/Shader Graph, transparent emissive crescent body, leading spark, trailing wisp, dissipate burst, Recipe Binding, gameplay demo, capture evidence.
- Excluded: damage, collision, hit VFX, sound, camera shake, pooling, dynamic Light, Distortion.
- Source Template: 새 production Template. DF-002 Template, Recipe, Generated Prefab은 수정하지 않았다.
- Primary Recipe: `Dogfooding/Recipes/production_crescent_slash.json`
- Variant Recipe: `Dogfooding/Recipes/production_crescent_slash_variant.json`
- Generated Prefabs:
  - `Assets/VFXForge/Dogfood/ProductionCrescentSlash/Generated/ProductionCrescentSlash.prefab`
  - `Assets/VFXForge/Dogfood/ProductionCrescentSlash/Generated/ProductionCrescentSlashVariant.prefab`

## Safety Check

- VFX Graph와 Shader Graph는 Unity `AssetDatabase`와 VFX authoring API로 복제·설정했다. 직렬화 텍스트는 수정하지 않았다.
- authoring target이 이미 있으면 생성 명령은 실패한다.
- Primary와 Variant 컴파일 전후 source hash:
  - Template Prefab: `3d8a74b7c90c3745bd3342b73d565aaea638ccfa466878527b560565f1b8f698`
  - VFX Graph: `127d762f566a8f774378d4539e958fce5cbd6140d184047d3f888289bc8a1ec4`
  - Shader Graph: `774d59976bb90fed2aa66d594e6bd8735a2422ffbd82ac65aae4e0924cef7ac7`
- Package와 project-owned Asset 경로의 쓰기 권한 및 Namespace/asmdef 컴파일을 검증했다.

## Iteration

한 시각 반복에서 주요 변경은 세 개 이하로 제한했다.

### Iteration 1

- Hypothesis: 140° 비대칭 테이퍼 초승달, 투명 발광 Noise/Dissolve, 빠른 출현-유지-꼬리 감쇠 envelope가 이동형 검격으로 읽힌다.
- Major properties: silhouette, material treatment, time envelope.
- Result: 유효한 crescent silhouette과 부드러운 출현/소멸을 얻었다. Recipe `radius=1.65`를 실제 월드 반경으로 적용하고, 감쇠 곡선의 `SmoothStep` 사용 오류를 수정했다.

### Iteration 2

- Hypothesis: leading spark, trailing wisp, 종료 dissipate burst가 진행 방향과 마무리를 강화한다.
- Major properties: leading spark, trailing wisp, dissipate burst.
- Result: 절차적 스트릭 Shader로 사각 billboard 인상을 제거했고, 후방 wisp와 종료 burst가 정지 화면에서도 위쪽 진행 방향을 보조한다.

## Verification Evidence

- Primary pipeline: passed, exit `0`.
  - `UnityCompatibilityProject/Artifacts/dogfood/VF-012-production-final-v7`
- Variant pipeline: passed, exit `0`.
  - `UnityCompatibilityProject/Artifacts/dogfood/VF-012-variant-final`
- Binding: 계획에 열거된 항목은 합계 12개이며 모두 required binding으로 적용했다. 두 Prefab 사이 11개 override 값이 다르다.
- VFX budget: Graph capacity `64`; ParticleSystem capacity 합계 `54`; Light `0`; Distortion 비활성.
- EditMode: `132 passed / 0 failed`.
  - `UnityCompatibilityProject/Artifacts/vf-012-editmode-final.xml`
- PlayMode: `1 passed / 0 failed`.
  - 데모 Scene 발사, 활성 VFX Graph, 11m/s 이동, 0.52초 수명 종료 확인.
  - `UnityCompatibilityProject/Artifacts/vf-012-playmode.xml`
- Console: final clean probe Error `0`.
  - `UnityCompatibilityProject/Artifacts/vf-012-console-final.log`
- Pipeline foreground ratios:
  - `0.02s`: `1.22%`
  - `0.08s`: `3.20%`
  - `0.18s`: `3.53%`
  - `0.32s`: `4.42%`
  - `0.48s`: `3.35%`
- Gameplay captures:
  - Peak: `Dogfooding/Evidence/VF-012/peak-dark.png`, `peak-mid.png`, `peak-bright.png`
  - Sequence: `sequence-002.png`, `sequence-008.png`, `sequence-018.png`, `sequence-032.png`, `sequence-048.png`

## Product Findings

### Worked

- Catalog/Recipe compiler가 같은 Template에서 청록 Primary와 보라 Variant Prefab의 override를 정확히 분리했다.
- gameplay zoom의 명·중·암 지면 모두에서 백색 코어와 청록 외곽이 식별된다.
- 검격은 캐릭터보다 앞에서 이동하고 타깃과 겹칠 때도 투명 감쇠되어 gameplay actor를 완전히 가리지 않는다.
- Preview capture를 실제 전경 픽셀로 검사하자 빈 PNG를 성공으로 오인하는 문제를 차단할 수 있었다.

### Friction

- Preview Camera가 격리 Scene을 명시하지 않아 초기 Pipeline Preview가 배경만 렌더했다.
- Preview Scene의 `BroadcastMessage`는 `ShouldRunBehaviour` Assertion을 발생시켰다. 명시적 preview-time method invocation으로 교체했다.
- 소수 지수 taper 계산에서 `sin(π)` 부동소수점 음수가 `NaN` 정점을 만들어 Mesh bounds를 무효화했다. 입력을 0 이상으로 제한했다.
- Recipe budget `64`와 실제 Graph capacity `128`이 달라도 기존 validator가 통과했다. 이번 Graph는 Unity API로 `64`에 고정했다.

### Defect / Gap

- 일반 pipeline은 아직 캡처 전경 비율, Mesh finite bounds, 실제 VFX Graph capacity를 자동 검증하지 않는다.
- Recipe binding 수를 문장으로만 관리하면 “11개”처럼 산술 오류가 생길 수 있다. 계약은 실제 binding 목록에서 계산해야 한다.
- 이 Template의 custom body animation은 명시적인 `EvaluatePreviewTime(float)` hook에 의존한다. 일반화된 preview-time interface는 후속 설계가 필요하다.

## Result

- Status: accepted.
- Visual reading: gameplay capture 기준으로 진행 방향, 검날형 초승달, soft reveal/dissolve, actor 비가림을 확인했다.
- Demo Scene:
  `Assets/VFXForge/Dogfood/ProductionCrescentSlash/Demo/ProductionCrescentDemo.unity`
- Unity menu:
  `Tools > VFX Forge > Dogfood > Open VF-012 Gameplay Demo`
- Remaining limitations: 별도 Hit VFX와 충돌·대미지·사운드·카메라 연출은 범위 밖이다.
- Next action: `VF-013`에서 실제 Graph budget, finite bounds, preview foreground, preview-time hook을 일반 품질 게이트로 승격한다.
