# VFX Forge Agent Rules

## Mission

Unity 6 프로젝트에서 VFX Template을 안전하게 복제하고, Recipe로 Exposed Property를 설정하며, 검증·캡처 근거를 남기는 도구를 구현한다.

## Non-negotiable Rules

1. PRD와 SRS를 범위 기준으로 사용한다.
2. VFX Graph 직렬화 파일을 텍스트로 직접 수정하지 않는다.
3. 원본 Template Prefab과 원본 VFX Asset을 수정하지 않는다.
4. 기존 사용자 Asset을 자동 덮어쓰지 않는다.
5. Unity Console Error가 있으면 Task를 완료 처리하지 않는다.
6. 캡처나 Validation 근거 없이 품질이 좋아졌다고 주장하지 않는다.
7. 한 번의 개선 반복에서 주요 Property는 최대 3개만 변경한다.
8. Task 범위를 임의로 확장하지 않는다.
9. 실패를 숨기거나 Warning으로 낮추지 않는다.
10. EditMode 테스트가 가능한 로직은 Editor UI와 분리한다.

## Required Startup Check

- Unity Editor 버전
- URP 버전
- VFX Graph 버전
- 설치된 Test Framework 버전
- 기존 asmdef와 Namespace 충돌
- Package 경로와 쓰기 권한

## Work Loop

1. `feature_list.json`에서 현재 Task 확인
2. 관련 코드와 테스트만 읽기
3. 최소 변경 계획 작성
4. 구현
5. 컴파일 확인
6. 관련 EditMode/PlayMode 테스트 실행
7. Console Error 확인
8. 결과와 제한사항 기록
9. Task 상태 갱신

## Completion Report

- Task ID
- 변경 파일
- 구현 요약
- 실행 명령 또는 Unity 메뉴
- 테스트 결과
- Validation 결과
- 생성 Asset 및 Artifact 경로
- 남은 제한사항
- 다음 Task

## Forbidden Completion Claims

다음 근거가 없으면 `완료`, `정상`, `품질 개선`이라고 보고하지 않는다.

- 컴파일 결과
- 관련 테스트 결과
- Console 상태
- 생성 파일 또는 Validation Report
- 시각 변경의 경우 Capture 결과
