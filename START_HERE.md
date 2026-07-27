# VFX Forge Starter Package

이 저장물은 VFX Forge PRD/SRS를 실제 Unity 프로젝트 구현으로 시작하기 위한 스타트 패키지다.
PRD와 SRS 원문은 포함하지 않는다.

## 포함 항목

- Unity Package Manager 패키지 골격
- Runtime/Editor Assembly Definition
- Recipe 데이터 계약과 JSON Schema
- Template Catalog와 Property Binding 모델
- Recipe Parser, Normalizer, Validator
- Template 기반 Prefab Compiler 초본
- 기본 Validation Rule
- Editor Window
- BatchMode 진입점
- EditMode 테스트 초본
- `AGENTS.md`, `feature_list.json`, 에이전트 시작 프롬프트
- 샘플 Recipe

## 설치

1. 이 폴더의 `Packages/com.kubonsang.vfx-forge`를 대상 Unity 프로젝트의 `Packages/` 아래에 복사한다.
2. 대상 프로젝트에 VFX Graph가 설치되어 있는지 확인한다.
3. Unity를 열고 컴파일 오류를 먼저 해결한다.
4. `Tools > VFX Forge > Bootstrap Project Assets`를 실행한다.
5. 생성된 Catalog에 직접 준비한 VFX Template Prefab을 등록한다.
6. `Tools > VFX Forge`에서 샘플 Recipe를 검증한다.

## 에이전트 시작 순서

1. 저장소 루트의 `AGENTS.md`를 읽는다.
2. `feature_list.json`에서 `current_task`를 확인한다.
3. `.agent/START_PROMPT.md`의 지시를 따른다.
4. Task를 한 번에 하나만 완료한다.
5. Unity Console Error가 남아 있으면 완료 처리하지 않는다.

## 현재 구현 범위

현재 코드는 완성품이 아니라 **컴파일 가능한 구현 출발점**을 목표로 한다.
Preview Scene, Capture, Contact Sheet, 실제 Particle Count 측정은 후속 Task로 남겨 두었다.

## 정적 점검

Unity에 넣기 전 다음 명령으로 패키지 구조와 JSON을 점검할 수 있다.

```bash
python3 tools/validate_starter.py
```
