2026-03-05 19:10 JST - WoodLand3D 프로젝트 git 정리 및 저작권 안전화
 - 기존 main 히스토리를 분리하고, 서드파티/Asset Store 에셋이 포함되지 않은 새 main 브랜치 생성
 - .gitignore를 Unity 기본 규칙 + Assets 전역 무시 + Docs/MyPrefab/MyScenes/MyScript/Resources/Settings/Editor만 예외 허용으로 구성
 - MCPForUnity, TextMesh Pro, Screenshots, UnusedAssets* 등 서드파티/툴/스크린샷 폴더는 전부 무시 처리
 - GitHub 리포지토리 akj2013/WoodLand 을 WoodLand3D 기준 구조로 동기화 (main 브랜치)

2026-03-05 19:20 JST - 문서 및 아트 소스 추가 준비
 - ArtSource 전체(컨셉/메시/블렌더/Unity Export/레퍼런스/스크린샷)를 .gitignore 예외로 추가
 - Assets/Docs 내 기획·기술 문서 및 작업 로그 파일들을 git 추적 대상으로 설정
 - 앞으로 AI 작업 로그를 이 파일에 시간순으로 기록해, GPT 피드백 → 개발 → 재피드백 루프로 활용할 예정

2026-03-05 19:40 JST - TextMesh Pro 및 SOUND 자산 Git 제외
 - git ls-files 로 TextMesh Pro 관련 추적 상태 확인 후, TextMesh Pro.meta를 untrack 처리
 - .gitignore에 /Assets/Resources/SOUND/ 규칙을 추가해 사운드 폴더 전체를 영구 무시
 - 기존에 커밋되어 있던 SOUND 내 wav/ogg/flac 파일과 meta들을 git rm --cached로 제거 (로컬에는 유지)

2026-03-05 19:50 JST - Docs/Prefab/Scene 정리 내용 Git 반영
 - Assets/Docs 구조를 01~10_*.md + Cursor_Helps 폴더로 재정리하고, Unity 메타 파일까지 rename/이동 반영
 - 사용하지 않는 MyPrefab (JemRoot, LogRoot 등) 및 Hexa/머티리얼 일부를 삭제하고 Git에도 삭제 상태 커밋
 - SampleSceneTest 2의 Lightmap/LightingData/ReflectionProbe 등 빌드 산출물 자산을 모두 제거해 리포 용량과 노이즈를 줄임

2026-03-06 09:10 JST - CLEAN_EXPORT 생성 및 GameProject에 클린 버전 업로드
 - 기존 WoodLand3D 루트 옆에 CLEAN_EXPORT 폴더를 생성하고, Packages/와 ProjectSettings/만 그대로 복사
 - Assets/Docs 전체와 새 타일 시스템 스크립트들(Assets/Scripts/*), ProtoTypeScene을 복사해 Bootstrap.unity로 구성
 - CLEAN_EXPORT 안에서 Unity 전용 .gitignore를 작성해 Legacy/SOUND/에셋스토어/바이너리 자산 전체를 차단
 - git init → git ls-files | findstr ... 로 Legacy/SOUND/바이너리/에셋스토어 패턴이 하나도 없는지 검증
 - 새 GitHub 리포지토리 akj2013/GameProject 에 Initial clean Unity repo (code+settings only) 커밋을 push

2026-03-06 09:40 JST - 타일 언락 UX 개선 및 리소스 스폰 시스템 추가
 - TileController 초기화 버그 수정: SetUnlocked에 force 플래그 추가해 잠긴 타일도 시각 상태를 항상 정확히 적용
 - TileController에 하이라이트 기능 및 구름 언락 연출(스케일 업 후 비활성화) 추가, TileUnlockSystem에 카메라 포커스 연동
 - Assets/Scripts/Core/Resources/ 아래에 ResourceType/ResourceNode/ResourceSpawnPoint/TileResourceSpawner/PlayerHarvestTest 구현
 - TileResourceSpawner를 TileController와 연동해 타일 언락 시 나무/바위 리소스를 스폰하고, 고갈 후 지연 리스폰 루프 구축

2026-03-06 10:10 JST - GameProject 리포에 프로토타입 씬/스크립트 동기화
 - CLEAN_EXPORT/Assets/Scripts 에 타일 시스템/리소스 시스템/카메라/언락 UI 스크립트를 최신 상태로 복사
 - CLEAN_EXPORT/Assets/Scenes/Bootstrap.unity 를 ProtoTypeScene 기준으로 갱신해, GitHub 상에서 GPT가 재현 가능한 테스트 씬 확보
 - GameProject main 브랜치에 \"Add tile resource spawning and prototype scene\" 커밋으로 푸시 완료

2026-03-06 (추가) - 스크립트 폴더 통일
 - Assets/Scripts/Core/Resources/ 를 Assets/MyScript/Resources/ 로 이전하여 스크립트를 MyScript 한 곳으로 통일
 - 리소스 관련 스크립트 5개(ResourceType, ResourceNode, ResourceSpawnPoint, TileResourceSpawner, PlayerHarvestTest) 이동, 기존 .meta GUID 유지로 프리팹/참조 유지
 - 빈 폴더 Scripts/Core/Resources 및 상위 Scripts 트리 제거
 - 네임스페이스(WoodLand3D.Core.Resources) 및 TileController 등 참조는 변경 없음
