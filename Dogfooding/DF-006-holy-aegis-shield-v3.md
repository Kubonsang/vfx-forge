# DF-006 — Holy Aegis Shield V3

## Metadata

- Record ID: `DF-006`
- Task ID: `VF-019`
- Date: `2026-07-31`
- Operator: Codex
- Current phase: `waiting_user_silhouette_approval`
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

## Automated Verification

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

## Next Gate

사용자 실루엣 승인 전에는 황금·에메랄드 Transparent Emissive Shader,
전개 `0.28s`, 유지 `1.10s`, 소멸 `0.42s`, Recipe Variant, Production
Template 반복으로 진행하지 않는다. VF-019는 `done` 또는
production-ready가 아니다.
