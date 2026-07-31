# DF-005 — Ornate Top-Down Giant Shield V2

## Metadata

- Record ID: `DF-005`
- Task ID: `VF-014`
- Date: `2026-07-30`
- User review date: `2026-07-31`
- Operator: Codex
- Commit: VF-014 single task commit on `main`
- Unity / URP / VFX Graph / Test Framework:
  `6000.3.8f1` / `17.3.0` / `17.3.0` / `1.6.0`

## Goal

- Real-use scenario: 탑다운 전투에서 캐릭터 전방 장벽뿐 아니라 좌우 날개,
  전방 첨탑, 후방 문장까지 한눈에 읽히는 화려한 방패를 전개한다.
- Dogfooding question: 기본 입자처럼 보이는 보조 요소 없이 전용 Shader
  Mesh만으로 충분히 풍부한 전개 실루엣을 만들 수 있는가?

## Scope

- Included: five-panel field, shadered side wings, front spire, rear crest,
  rune ring, Primary/Variant Recipe, top-down demo and captures.
- Excluded: ParticleSystem anchor/mote/shard, collision, damage mitigation,
  sound, camera shake, dynamic Light, Distortion, pooling.
- Source safety: VF-013 Template, Graph, Recipe, Generated Prefab은 보존한다.

## Iteration

한 시각 반복에서 주요 변경은 세 개 이하로 제한한다.

### Iteration 1

- Hypothesis: 좌우 3중 날개, 전방 첨탑, 후방 문장이 탑다운 실루엣을
  단순한 반원 장벽에서 의식용 방패로 바꾼다.
- Major properties: side wings, front spire, rear crest.
- Result: 탑다운 Preview에서 좌우 3중 날개, 전방 왕관형 첨탑, 후방
  문장과 꼬리가 서로 겹치지 않는 네 방향 실루엣을 만들었다. 엄격한
  상단 시점에서도 방패 본체의 곡면 호와 장식 방향이 함께 읽힌다.

### Iteration 2

- Hypothesis: 전용 회로/룬 Shader, 바닥 rune ring, 순차 전개 envelope가
  기본 입자 느낌을 제거한다.
- Major properties: ornament shader, rune ring, deployment envelope.
- Result: 기존 anchor burst, edge mote, dissolve shard를 모두 제거했다.
  5개 장식은 전용 `VFXForge/Dogfood/OrnateShield` Shader가 적용된
  MeshRenderer이며 Generated Prefab의 ParticleSystem 수는 0이다.

## Verification Evidence

- Compile: Unity BatchMode authoring import와 Primary/Variant pipeline 모두
  exit `0`.
- Binding: 12개 required binding 적용. Primary와 Violet Variant 사이
  12개 override가 모두 다르고 동일한 원본 Graph를 참조한다.
- Tests: EditMode `136 passed / 0 failed`, PlayMode `3 passed / 0 failed`.
- Console: Error `0`, Warning `12`.
  `UnityCompatibilityProject/Artifacts/vf-014-console.json`
- Preview captures:
  - Primary:
    `UnityCompatibilityProject/Artifacts/dogfood/VF-014-primary-final/capture`
  - Variant:
    `UnityCompatibilityProject/Artifacts/dogfood/VF-014-variant-final/capture`
  - 탑다운 12장 유효 픽셀 비율 최소 `1.65%`.
- Gameplay captures:
  `Dogfooding/Evidence/VF-014`의 탑다운 6장과 3/4 시점 6장.

## Product Findings

### Automated Checks That Passed

- 장식을 수평 Mesh로 만들자 카메라를 향한 billboard 없이도 탑다운
  footprint가 안정적으로 유지됐다.
- 좌우 날개 세 줄의 길이와 곡률 차이가 대칭 구조 안에서도 장식 밀도를
  만든다.
- cyan circuit, platinum edge, gold rune가 하나의 전용 Shader 안에서
  layer mode로 분리되어 기본 재질처럼 보이는 요소가 남지 않았다.
- rune ring과 후방 crest가 캐릭터 주변을 감싸지만 actor 중심은 비워
  gameplay 식별성을 유지했다.

이 항목들은 구조·렌더링·비어 있지 않은 캡처를 확인한 결과다. VFX의
의미 전달, 문양 완성도, 실사용 가능한 미술 품질을 입증하지 않는다.

### User Visual Review

- Verdict: `failed / rejected`.
- 전체 이펙트가 무엇을 표현하는지 의미가 불명확하다. 방패 전개라는
  의도가 실전 탑다운 화면에서 즉시 읽히지 않는다.
- 시각적으로 파티클처럼 읽히는 요소와 장식 문양이 조잡하며, 요소 간
  형태 언어와 밀도에 통일감이 없다.
- 전용 Shader가 적용되어 있다는 기술적 사실과 무관하게 표면 표현과
  문양의 마감이 낮아 게임 실사용 품질에 도달하지 못했다.
- 따라서 foreground ratio, Renderer/Shader 검사, Console Error 0,
  테스트 통과만으로는 production VFX 품질을 승인할 수 없다.

### Friction

- 격리 Preview의 고정 top camera에서는 가장 긴 날개 끝이 화면 경계에
  가깝다. gameplay demo에서는 더 넓은 16:9 카메라로 전체가 보인다.
- 최초 소멸 캡처 `1.88s`가 유효 픽셀 `0.74%`로 gate에 실패했다.
  시각 envelope는 유지하고 검증 프레임을 잔상이 읽히는 `1.78s`로
  조정해 `15.00%`로 재검증했다.

### Defect / Gap

- 가장 큰 결함은 기술적 동작이 아니라 시각적 의미와 미술 완성도다.
  장식의 수를 늘린 접근이 방패의 핵심 실루엣을 강화하지 못하고 오히려
  파티클과 문양의 시각적 잡음을 만들었다.
- strict top view에서는 수직 장벽 면이 얇은 호로 보이므로 장식 footprint가
  주 실루엣을 담당한다. 카메라 각도가 기울어질수록 장벽 면과 장식이
  함께 읽힌다.
- 실제 projectile block, 방어 판정, sound와 hit response는 여전히 없다.
- 공통 pipeline의 bounds 기반 camera framing은 VF-015 후보로 남는다.

## Result

- Status: `failed / rejected`.
- Production readiness: `not approved`.
- Failure reason: 이펙트의 의미가 불명확하고, 파티클성 요소와 문양이
  조잡해 실사용 가능한 VFX 품질에 미달한다.
- 기존 Asset과 캡처는 성공 사례가 아니라 실패 분석 근거로 보존한다.
- Generated Assets:
  - `Assets/VFXForge/Dogfood/OrnateGiantShield/Generated/OrnateGiantShield.prefab`
  - `Assets/VFXForge/Dogfood/OrnateGiantShield/Generated/OrnateGiantShieldVariant.prefab`
- Demo Scene:
  `Assets/VFXForge/Dogfood/OrnateGiantShield/Demo/OrnateGiantShieldDemo.unity`
- Unity menu:
  `Tools > VFX Forge > Dogfood > Open VF-014 Demo`
- Source hashes after both compiles:
  - Template:
    `df9ac776f6f469916d9a816b1fe7ce8111c2991a1e05a8aa2dc2068481b1eb12`
  - VFX Graph:
    `0fcbdb2bf27af99ee0476cdee3facf96f41a20524e3716bf83c91413386c53c2`
- Limitations: gameplay logic, sound, dynamic Light, Distortion, pooling은
  범위 밖이다.
- Next action: 후속 Task에서는 장식 수를 추가하기 전에 방패로 즉시
  읽히는 핵심 실루엣과 일관된 문양 언어를 다시 설계한다. 의미 전달과
  미술 완성도를 판정하는 육안 승인 단계를 자동 capture gate보다 먼저
  둔다.
