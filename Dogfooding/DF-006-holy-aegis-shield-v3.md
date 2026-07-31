# DF-006 — Holy Aegis Shield V3

## Metadata

- Record ID: `DF-006`
- Task ID: `VF-019`
- Date: `2026-07-31`
- Operator: Codex
- Current phase: `production_review_rejected`
- Unity / URP / VFX Graph / Test Framework:
  `6000.3.8f1` / `17.3.0` / `17.3.0` / `1.6.0`

## Goal

- Real-use scenario: 판타지 기사가 탑다운 전투에서 거대한 원형 성광
  이지스를 전개한다.
- Dogfooding question: Shader와 문양을 추가하기 전에 원형 주 판, 굵은
  중앙 기사 문장, 림에 연결된 네 장식만으로 방패 의미가 읽히는가?

## Preserved Failure Evidence

- VF-013과 실패한 VF-014 Template, Recipe, Generated Prefab은 수정하지
  않는다.
- DF-005의 핵심 실패는 기술 검증이 아니라 의미불명, 파티클처럼 분리된
  요소, 조잡하고 반복적인 문양이었다.
- V3는 작은 룬, 분리된 조각, 미세 필리그리를 추가하지 않는다.

## Iteration 1 — Grayscale Silhouette

- Major properties: circular plate, central knight crest, four connected
  ornaments.
- 반경 `2.6m` 원형 주 판을 수직에서 지면 방향으로 `35°` 기울였다.
- 중앙에는 하나의 굵은 기사 문장 root만 두고, 좌·우·전·후 장식은
  림과 겹쳐 연결되는 네 개의 큰 Mesh로 제한했다.
- 모든 Material은 무채색 Unlit이며 이 단계에는 Production Shader,
  색, emission, 전개·소멸 timing을 넣지 않았다.
- ParticleSystem, spark, wisp, shard, dynamic Light, Distortion은 없다.

### Internal Visual Finding

- 첫 캡처의 좌·우 장식은 단순 마름모/화살촉으로 보여 방패 장식보다
  나침반 또는 UI 아이콘으로 읽힐 위험이 있었다.
- 이 초안은 실패 근거로
  `Dogfooding/Evidence/VF-019-silhouette/`에 보존한다.
- 두 번째 실루엣 반복에서는 Shader, 색, 크기, 카메라를 바꾸지 않고
  네 장식의 외곽선만 수정했다.
  - 좌·우: 림에서 이어지는 넓은 날개형 외곽
  - 전방: 하나의 큰 왕관형 장식
  - 후방: 하나의 큰 용골형 장식
- 반복되는 작은 문양이나 분리된 조각은 추가하지 않았다.

## Silhouette Verification

- Structural contract:
  - circular main plate: `1`
  - central knight crest root: `1`
  - connected rim ornaments: `4`
  - ParticleSystem: `0`
  - Light: `0`
- Context: 16:9 strict top-down, caster, target, light/medium/dark grounds.
- EditMode regression: `167 passed`, `0 failed`, `0 skipped`.
- Unity Console compile/Shader errors: `0`.
- Test artifact: `/tmp/vf019-silhouette-editmode.xml`.
- Console artifact: `/tmp/vf019-silhouette-editmode.log`.
- Evidence:
  - Initial:
    `Dogfooding/Evidence/VF-019-silhouette/silhouette-contact-sheet.png`
  - Review candidate:
    `Dogfooding/Evidence/VF-019-silhouette-v2/isolated-top.png`
  - Review candidate:
    `Dogfooding/Evidence/VF-019-silhouette-v2/gameplay-top.png`
  - Review candidate:
    `Dogfooding/Evidence/VF-019-silhouette-v2/silhouette-contact-sheet.png`

## Human Silhouette Review

- Status: `review_required`; 자동화나 Codex가 승인하지 않았다.
- 다음 항목을 사용자가 직접 판정한다.
  - 원형 주 판이 네 장식보다 먼저 읽히는가?
  - 정지된 top capture만 보고 거대한 기사 방패로 식별되는가?
  - 중앙 문장이 UI 아이콘이나 임의 도형이 아니라 기사 문장으로 읽히는가?
  - 네 장식이 분리된 파티클이 아니라 림에 연결된 구조로 읽히는가?
  - 캐릭터와 타깃을 과도하게 가리지 않는가?

사용자는 색·Shader·timing이 적용된 결과를 본 뒤 상세 평가하겠다고 판단해
Production 반복 진행을 승인했다. 이는 최종 품질 승인이 아니다.

## Iteration 2 — Production Shader and Timing

- 기존 Silhouette Prefab과 Mesh는 수정하지 않고 새 Production Template이
  읽기 전용 Mesh 참조를 사용한다.
- 모든 가시 Mesh Renderer `14개`에 전용
  `VFXForge/Dogfood/HolyAegisShield` Transparent Emissive Shader를
  적용했다.
- Emerald energy plate, gold connected rim, central knight crest, four
  connected ornaments의 네 레이어만 사용한다.
- 전개:
  - 중앙 문장
  - 원형 에너지 판과 금색 림
  - 네 연결 장식
- 소멸: 장식 → 림 → 판 → 중앙 문장 순서의 외곽-안쪽 감쇠.
- 수명: 전개 `0.28s`, 유지 `1.10s`, 소멸 `0.42s`, 총 `1.8s`.
- ParticleSystem, spark, wisp, shard, dynamic Light, Distortion은 없다.
- Preview playback 계약을 위해 활성 VFX Graph를 사용하지만 scale
  `0.0001`, capacity `1`로 두며 가시 레이어로 사용하지 않는다.

### Visual Adjustment

첫 Production Contact Sheet는 emission clipping 때문에 금색이 백색으로
날아가고 에메랄드 판보다 청록 링이 먼저 읽혔다.

한 번의 개선 반복에서 다음 세 Shader 속성만 변경했다.

1. HDR emission 출력 범위를 압축했다.
2. 금색과 에메랄드의 보조색 혼합을 분리했다.
3. 에너지 판 alpha를 올려 주 판이 먼저 읽히게 했다.

실루엣과 timing 곡선은 이 반복에서 변경하지 않았다.

## Pipeline Dogfooding Findings

- 실제 VFX Graph에서 `VAL-005`가 Unity 6의 overload 증가 때문에
  `AmbiguousMatchException`으로 실패했다.
  - `GetResourceAtPath(string)`과 호환되는
    `GetOrCreateGraph(resource)` overload를 명시적으로 고르도록
    capacity reader를 수정했다.
  - 수정 후 실제 capacity `1 / budget 1`로 통과했다.
- 0.12초 context 프레임은 foreground `0.49%`로 `VAL-007`에 실패했다.
  - 임계값 `1%`를 낮추지 않았다.
  - 아직 전개 중이면서 판독 가능한 `0.18초`로 첫 평가 시점을 옮겼다.
- 35° 판의 초기 side view는 edge-on으로 foreground `0%`였다.
  - V3 제품 문맥이 strict top-down이므로 Recipe 캡처를 top view로
    제한했다.
  - 직접 캡처로 우회하거나 빈 프레임을 성공 처리하지 않았다.

## Production Automated Verification

- Recipe 1.1 Primary / Variant:
  - Adapter Binding: timing, radius, colors, emission, sharpness, motion
  - MaterialPropertyBlock Binding: plate/rim color, emission, sharpness
  - Transform Binding: Recipe scale witness
- Primary와 Variant는 색·크기·timing·motion override가 실제로 다르다.
- Primary technical status: `passed`; product status: `review_required`.
- Variant technical status: `passed`; product status: `review_required`.
- Isolated/context foreground: 모든 프레임 `1%` 이상.
- Border foreground: 모든 프레임 `0%`.
- EditMode: `171 passed`, `0 failed`, `0 skipped`.
- PlayMode: `4 passed`, `0 failed`, `0 skipped`.
- Unity Console compile/Shader errors: `0`.
- Artifacts:
  - `Dogfooding/Evidence/VF-019-production-primary/`
  - `Dogfooding/Evidence/VF-019-production-variant/`
- Demo:
  `Assets/VFXForge/Dogfood/HolyAegisV3/Demo/HolyAegisV3Demo.unity`

## Human Production Review

- Status: `rejected`.
- Reviewer: project owner.
- Review time: `2026-07-31T06:48:48Z`.
- 자동화와 Codex는 `accepted`를 기록하지 않았다.
- 사용자 판정:
  - 방패 스킬이라는 의미와 기본 가독성은 전달된다.
  - 렌더링, 장식 마감, 시간 연출은 과거 휴대용 게임 시대의 효과처럼
    보인다.
  - 해당 시대의 품질을 목표로 한다면 합격할 수 있으나, 현대 인디
    게임의 실사용 품질 기준으로는 불합격이다.
- 실패한 육안 항목:
  - Shader / pattern finish
  - timing polish
- 추가 진단:
  - 평면 Mesh의 겹침과 단순 alpha/emission 변화만으로 구성돼 재질의
    깊이와 광학적 계층이 부족하다.
  - 금색 장식이 금속 구조가 아니라 밝은 평면 도형으로 보인다.
  - 에메랄드 면 내부의 에너지 흐름이 반복적인 동심원에 가까워 현대적
    VFX의 비정형 흐름과 세부 시간차가 없다.
  - 네 장식과 중앙 문장이 완성된 조형물보다 확대된 아이콘처럼 보인다.

VF-019는 `done` 또는 production-ready가 아니다. 현재 V3 결과를 보존하고,
후속 재설계는 단순 색·emission 조정이 아니라 geometry depth, material
response, layered temporal choreography를 다시 설계해야 한다.
