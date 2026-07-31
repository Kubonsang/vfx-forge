# VFX Forge

Unity 6에서 검증된 VFX Template을 안전하게 복제하고, JSON Recipe로 Exposed
Property를 적용한 뒤 Prefab 생성·검증·Preview·프레임 캡처까지 한 번에 수행하는
Editor 도구입니다.

VFX Graph 직렬화 파일을 직접 수정하지 않습니다. 원본 Template Prefab과 VFX
Asset은 읽기 전용 입력으로 취급하며, 생성된 결과와 검증 근거를 별도 Artifact로
남깁니다.

## 주요 기능

- JSON Recipe 1.0/1.1 파싱, 정규화 및 안정적인 오류 코드
- 검증된 Template Catalog와 명시적 Property Binding
- VFX·Transform·Material·Mesh·Adapter 대상 typed Binding
- 원본과 연결되지 않은 독립 Prefab 생성
- `VfxMetadata` 기반의 생성 Asset 전용 덮어쓰기 보호
- 생성 Prefab의 bounds·capacity 검증과 심각도 기반 `validation.json`
- 명시적 시간 평가 계약을 갖춘 격리 Preview Scene
- 고정 bounds framing을 사용하는 정면·측면·상단 PNG 캡처
- foreground·border·bounds 지표를 기록하는 Capture Manifest 1.1
- Catalog allowlist 기반 Gameplay Review Context 캡처
- 시간순 Contact Sheet와 frame·manifest SHA-256 Review Manifest
- Editor Window의 `Run All`과 단계별 실패 차단
- 자동화용 BatchMode 인수, 한 줄 JSON 결과, 단계별 종료 코드
- 3개 성공·2개 실패 Recipe를 사용하는 종단 간 통합 fixture

## 검증 환경

| 구성 요소 | 검증 버전 |
| --- | --- |
| Unity Editor | 6000.3.8f1 |
| Universal Render Pipeline | 17.3.0 |
| Visual Effect Graph | 17.3.0 |
| Unity Test Framework | 1.6.0 |

패키지의 최소 Unity 버전은 `6000.0`입니다. 현재 검증 기준에서는 EditMode 테스트
153개와 기존 Dogfooding PlayMode 테스트가 통과했고 Unity Console Error는 0개입니다.

## Production Crescent Slash 데모

`UnityCompatibilityProject`를 Unity에서 연 뒤
`Tools > VFX Forge > Dogfood > Open VF-012 Gameplay Demo`를 실행하면 탑다운
16:9 데모를 직접 확인할 수 있습니다. 검격은 1.1초마다 명·중·암 지면에서 자동
발사되며 Space 키로 즉시 다시 발사할 수 있습니다.

- Demo Scene:
  `Assets/VFXForge/Dogfood/ProductionCrescentSlash/Demo/ProductionCrescentDemo.unity`
- Primary / Variant Recipe:
  `Dogfooding/Recipes/production_crescent_slash.json`,
  `production_crescent_slash_variant.json`
- Dogfooding record:
  `Dogfooding/DF-003-production-crescent-slash.md`
- Gameplay captures:
  `Dogfooding/Evidence/VF-012/`

## 설치

Unity Package Manager에서 `Add package from git URL...`을 선택하고 다음 URL을
입력합니다.

```text
https://github.com/Kubonsang/vfx-forge.git?path=/Packages/com.kubonsang.vfx-forge#main
```

또는 프로젝트의 `Packages/manifest.json`에 직접 추가합니다.

```json
{
  "dependencies": {
    "com.kubonsang.vfx-forge": "https://github.com/Kubonsang/vfx-forge.git?path=/Packages/com.kubonsang.vfx-forge#main"
  }
}
```

호스트 프로젝트에는 URP와 Visual Effect Graph가 설치되어 있어야 합니다. 테스트를
실행하려면 Unity Test Framework도 필요합니다.

## 빠른 시작

1. Unity 메뉴에서 `Tools > VFX Forge > Bootstrap Project Assets`를 실행합니다.
2. 생성된 Template Catalog에 프로젝트 소유 VFX Prefab을 등록합니다.
3. 아래 형식으로 Recipe JSON을 만들고 Unity 프로젝트에 추가합니다.
4. `Tools > VFX Forge > Open Window`를 엽니다.
5. Recipe JSON, Template Catalog, Artifact Directory를 선택한 뒤 `Run All`을
   실행합니다.

Bootstrap 메뉴는 다음 구조를 만듭니다.

```text
Assets/VFXForge/
├─ Config/
├─ Recipes/
├─ Templates/
├─ Generated/
└─ Artifacts/
```

### 최소 Recipe 예제

`template`은 Template Catalog에 등록된 ID와 일치해야 하며, `outputPath`는
`Assets/` 아래의 `.prefab` 경로여야 합니다.

```json
{
  "schemaVersion": "1.0",
  "id": "arcane_impact_blue",
  "displayName": "Arcane Impact Blue",
  "template": "impact_core",
  "outputPath": "Assets/VFXForge/Generated/ArcaneImpactBlue.prefab",
  "seed": 1024,
  "timing": {
    "duration": 0.5
  },
  "budget": {
    "maxParticles": 300,
    "maxDuration": 0.8,
    "maxBoundsRadius": 3.0,
    "allowDistortion": false,
    "allowLight": false
  },
  "capture": {
    "duration": 0.5,
    "frameTimes": [0.0, 0.1, 0.25, 0.5],
    "views": ["front", "side", "top"],
    "width": 512,
    "height": 512
  }
}
```

전체 필드와 기본값은
[Recipe Contract 1.0](Packages/com.kubonsang.vfx-forge/Documentation~/recipe-contract.md)에서
확인할 수 있습니다.

## Run All 파이프라인

```text
Recipe Parse
  → Input Validation
  → Prefab Compile
  → Generated Prefab Validation
  → Isolated Preview
  → Frame Capture
  → Validation Report
```

어느 단계에서든 실패하면 이후의 생성·Preview·Capture 단계는 실행되지 않습니다.
실패 보고서를 작성할 수 있는 상태라면 `validation.json`만 증거로 남깁니다.

성공 시 다음 결과를 확인할 수 있습니다.

```text
Recipe.outputPath
└─ Generated Prefab + VfxMetadata

Artifact Directory/
├─ validation.json
├─ capture/
│  ├─ *.png
│  └─ capture-manifest.json
└─ review/                  # capture.contexts 사용 시
   ├─ contexts/*.png
   ├─ contact-sheet.png
   └─ review-manifest.json
```

## BatchMode

자동화 환경에서는 Unity의 `-executeMethod`로 동일한 파이프라인을 실행할 수
있습니다. 전체 실행은 프레임을 렌더링하므로 `-nographics`를 사용하지 않습니다.

```bash
/Applications/Unity/Hub/Editor/6000.3.8f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath "/path/to/UnityProject" \
  -executeMethod Kubonsang.VfxForge.Editor.VfxForgeBatchEntry.Run \
  -recipe "/path/to/recipe.json" \
  -templateCatalog "Assets/VFXForge/Config/VfxTemplateCatalog.asset" \
  -artifactPath "/path/to/artifacts/run-001" \
  -logFile -
```

Entry point가 계약에 정의된 코드로 Unity를 종료하므로 `-quit`을 추가하지 않습니다.
성공은 `0`, 인수 오류는 `10`, Recipe 파싱 오류는 `20`이며 이후 단계도 10 단위의
고정 코드로 구분됩니다. 전체 표와 JSON 출력 형식은
[BatchMode Contract](Packages/com.kubonsang.vfx-forge/Documentation~/batchmode.md)를
참조하세요.

## 안전 계약

- VFX Graph 직렬화 파일을 텍스트로 수정하지 않습니다.
- 원본 Template Prefab과 원본 VFX Asset을 수정하지 않습니다.
- 기존 사용자 Asset을 자동으로 덮어쓰지 않습니다.
- 기본 정책은 `VfxMetadata`가 있는 기존 생성 Prefab만 교체할 수 있습니다.
- Template Catalog에 등록한 Binding만 적용합니다.
- Preview는 활성 Scene과 분리된 임시 Scene에서 실행됩니다.
- 요청 시점 전체의 Renderer bounds로 구도를 고정하며 빈 프레임과 clipping을
  `VAL-007`로 차단합니다.
- 실패를 Warning으로 낮추거나 후속 단계를 계속 실행하지 않습니다.
- 시각 품질은 Capture와 Validation 근거 없이는 주장하지 않습니다.

## 테스트

저장소의 `UnityCompatibilityProject`는 로컬 UPM 패키지를 참조하는 검증용 호스트
프로젝트입니다. Preview와 Capture 테스트가 포함되어 있으므로 그래픽 BatchMode로
실행합니다.

```bash
/Applications/Unity/Hub/Editor/6000.3.8f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath "$PWD/UnityCompatibilityProject" \
  -runTests \
  -testPlatform EditMode \
  -testResults "$PWD/UnityCompatibilityProject/Artifacts/editmode-results.xml" \
  -logFile "$PWD/UnityCompatibilityProject/Artifacts/editmode.log"
```

통합 fixture는 성공 Recipe 3개에 대해 Prefab·Report·PNG·Manifest 생성을 확인하고,
실패 Recipe 2개가 정확한 단계에서 중단되는지 검증합니다. Template Prefab의
SHA-256, dependency hash, 참조 VFX Asset hash, Catalog JSON도 실행 전후 비교합니다.

## 저장소 구조

```text
.
├─ Packages/com.kubonsang.vfx-forge/
│  ├─ Runtime/          # Recipe, Metadata, Runtime playback
│  ├─ Editor/           # Catalog, Compiler, Validation, Preview, Capture, UI, CLI
│  ├─ Tests/            # EditMode tests and five Recipe fixtures
│  ├─ Documentation~/   # Package documentation
│  ├─ Samples~/         # Basic Recipe sample
│  └─ Schemas/          # Recipe 1.0 JSON Schema
├─ UnityCompatibilityProject/  # Unity 6 verification host
└─ feature_list.json           # Implemented task and verification record
```

## 문서

| 문서 | 내용 |
| --- | --- |
| [Architecture](Packages/com.kubonsang.vfx-forge/Documentation~/architecture.md) | 의존성 방향과 Asset 안전 경계 |
| [Recipe Contract](Packages/com.kubonsang.vfx-forge/Documentation~/recipe-contract.md) | Recipe 1.0 필드, 기본값, 오류 코드 |
| [Template Authoring](Packages/com.kubonsang.vfx-forge/Documentation~/template-authoring.md) | Template과 Property Binding 작성 규칙 |
| [Validation Pipeline](Packages/com.kubonsang.vfx-forge/Documentation~/validation-pipeline.md) | 검증 규칙과 Report 상태 |
| [Editor Window](Packages/com.kubonsang.vfx-forge/Documentation~/editor-window.md) | Run All, 진행 상태, 결과 이동 |
| [Frame Capture](Packages/com.kubonsang.vfx-forge/Documentation~/frame-capture.md) | 결정론적 Capture와 Manifest |
| [Gameplay Review Context](Packages/com.kubonsang.vfx-forge/Documentation~/review-context.md) | 게임 문맥 Capture, Contact Sheet, Review Manifest |
| [BatchMode Contract](Packages/com.kubonsang.vfx-forge/Documentation~/batchmode.md) | CLI 인수, JSON 결과, 종료 코드 |
| [Integration Fixture](Packages/com.kubonsang.vfx-forge/Documentation~/integration-fixture.md) | 5개 Recipe 종단 간 검증 |
| [Unity 6 Compatibility](Packages/com.kubonsang.vfx-forge/Documentation~/unity-6-compatibility.md) | 검증 버전과 호스트 프로젝트 |

## 현재 제한사항

- Human Visual Approval Gate는 아직 지원하지 않습니다.
- Runtime particle count 측정은 아직 지원하지 않습니다.
- GPU/CPU Profiler 연동은 아직 지원하지 않습니다.
- 통합 fixture의 Capture는 구조적 렌더링 근거이며 VFX 시각 품질 평가가 아닙니다.

패키지 버전은 현재 `0.1.0`입니다.
