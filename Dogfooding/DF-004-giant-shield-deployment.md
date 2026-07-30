# DF-004 — Giant Shield Deployment Template V1

## Metadata

- Record ID: `DF-004`
- Task ID: `VF-013`
- Date: `2026-07-30`
- Operator: Codex
- Commit: VF-013 single task commit on `main`
- Unity / URP / VFX Graph / Test Framework:
  `6000.3.8f1` / `17.3.0` / `17.3.0` / `1.6.0`

## Goal

- Real-use scenario: 캐릭터가 전방에 거대한 곡면 마법 방패를 전개해 공격 방향을 명확히 차단한다.
- Dogfooding question: 대형·지속형 VFX도 Recipe Binding과 격리 Preview에서 크기, 전개 순서, 가독성을 검증할 수 있는가?

## Scope

- Included: five-panel field, emissive rim, anchor burst, edge motes, dissolve shards, Primary/Variant Recipe, gameplay demo and captures.
- Excluded: collision, damage mitigation, projectile blocking, sound, camera shake, dynamic Light, Distortion, pooling.
- Source Template: new project-owned Template. Existing slash Templates and Generated Prefabs remain untouched.

## Iteration

한 번의 시각 반복에서 주요 변경은 세 개 이하로 제한한다.

### Iteration 1

- Hypothesis: 중앙에서 순차 전개되는 5개 곡면 세그먼트, 육각 에너지 필드, 두꺼운 백금 림이 거대한 방어 구조물로 읽힌다.
- Major properties: silhouette, field material, deployment timing.
- Result: 중앙 패널이 먼저 솟고 바깥 패널이 뒤따르는 5단 전개가
  격리 Preview의 `0.12s` 프레임에서 확인됐다. 곡률은 탑다운 캡처에서
  캐스터 전방을 감싸는 120° 방어 호로 읽힌다.

### Iteration 2

- Hypothesis: 바닥 anchor burst, 가장자리 mote, 종료 shard가 무게감과 수명 마감을 보강한다.
- Major properties: anchor burst, edge motes, dissolve shards.
- Result: 초기 바닥 방사형 streak, 유지 중 edge mote, 종료 시 하강하는
  shard가 각각 전개·지속·소멸 단계의 구분을 보강했다.

## Verification Evidence

- Compile: Unity BatchMode authoring import와 Primary/Variant pipeline 모두
  exit `0`.
- Binding: 12개 required binding 적용. 두 Generated Prefab의 12개
  override가 모두 다르고 동일한 원본 VFX Graph를 참조한다.
- Tests: EditMode `134 passed / 0 failed`, PlayMode `2 passed / 0 failed`.
- Console: 최종 import, pipeline, capture gate, test log Error `0`.
- Preview captures:
  - Primary:
    `UnityCompatibilityProject/Artifacts/dogfood/VF-013-primary/capture`
  - Variant:
    `UnityCompatibilityProject/Artifacts/dogfood/VF-013-variant/capture`
  - 총 12장 유효 픽셀 비율 최소 `3.49%`.
- Gameplay captures:
  `Dogfooding/Evidence/VF-013`의 전면 6장과 탑다운 6장.

## Product Findings

### Worked

- 하나의 Template이 Primary 청록 장벽과 Amber Variant의 timing, radius,
  spread, emission, color override를 분리했다.
- 5개 패널의 중앙-외곽 지연이 단순 scale-up보다 방패를 “전개하는”
  동작으로 읽힌다.
- 전면 카메라에서는 캐릭터보다 큰 벽, 탑다운 카메라에서는 공격 방향을
  가로막는 곡면 호가 동시에 확인된다.
- Preview 전 프레임과 gameplay evidence 전 프레임이 빈 이미지 방지
  1% gate를 통과했다.

### Friction

- custom barrier Shader의 변수명 `point`가 Metal shader compiler 예약어와
  충돌했다. `gridPoint`로 교체한 뒤 최종 Shader Error는 0이다.
- 격리 Preview의 고정 카메라는 방패가 화면 절반가량을 차지한다.
  대형 지속형 VFX에는 bounds 기반 자동 framing이 더 적합하다.
- gameplay Bloom에서는 백금 rim과 중앙 scan이 일부 프레임에서
  과노출된다. 현재 실루엣은 유지되지만 밝은 맵용 exposure preset은
  후속 변형 후보다.

### Defect / Gap

- 충돌, 실제 projectile block, 방어 수치, hit response가 없으므로 이번
  결과는 시각 Template이다.
- 육각 필드 패턴은 정면에서 작은 마름모 체인처럼 보이는 구간이 있다.
  화면 해상도별 pattern LOD는 아직 없다.
- generic pipeline은 여전히 foreground ratio와 custom preview-time hook을
  Template별 helper에 의존한다. 일반 품질 gate는 VF-014 후보로 이관한다.

## Result

- Status: accepted.
- Generated Assets:
  - `Assets/VFXForge/Dogfood/GiantShield/Generated/GiantShieldDeployment.prefab`
  - `Assets/VFXForge/Dogfood/GiantShield/Generated/GiantShieldDeploymentVariant.prefab`
- Demo Scene:
  `Assets/VFXForge/Dogfood/GiantShield/Demo/GiantShieldDemo.unity`
- Unity menu:
  `Tools > VFX Forge > Dogfood > Open VF-013 Demo`
- Source hashes after both compiles:
  - Template:
    `68b55667d11b44df03a85acd592d18cb2a7228347e419e29590d0887f09b2223`
  - VFX Graph:
    `28e4de4762c59186c059f07afd0eb74899fc2425877e772ec0e9d6db9b62810a`
- Limitations: gameplay logic, sound, camera shake, dynamic Light,
  Distortion, pooling은 범위 밖이다.
- Next action: `VF-014`에서 공통 capture/bounds/capacity 품질 gate를
  설계한다.
