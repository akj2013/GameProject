2026-03-05 19:10 JST - WoodLand3D 프로젝트 git 정리 및 저작권 안전화
 - 기존 main 히스토리를 분리하고, 서드파티/Asset Store 에셋이 포함되지 않은 새 main 브랜치 생성
 - .gitignore를 Unity 기본 규칙 + Assets 전역 무시 + Docs/MyPrefab/MyScenes/MyScript/Resources/Settings/Editor만 예외 허용으로 구성
 - MCPForUnity, TextMesh Pro, Screenshots, UnusedAssets* 등 서드파티/툴/스크린샷 폴더는 전부 무시 처리
 - GitHub 리포지토리 akj2013/WoodLand 을 WoodLand3D 기준 구조로 동기화 (main 브랜치)

2026-03-05 19:20 JST - 문서 및 아트 소스 추가 준비
 - ArtSource 전체(컨셉/메시/블렌더/Unity Export/레퍼런스/스크린샷)를 .gitignore 예외로 추가
 - Assets/Docs 내 기획·기술 문서 및 작업 로그 파일들을 git 추적 대상으로 설정
 - 앞으로 AI 작업 로그를 이 파일에 시간순으로 기록해, GPT 피드백 → 개발 → 재피드백 루프로 활용할 예정
