# Development Log

## 2026-03-05

### 작업 내용

- GitHub 저장소 생성
- Unity 프로젝트 업로드
- .gitignore 설정
- 자원 채집 시스템 구현
- 캐릭터 공격 시스템 구현

### 결정 사항

- 맵 구조: Square Tile
- 캐릭터 스타일: Chibi
- 캐릭터 제작: Meshy AI
- NPC 시스템 도입

## 2026-03-06

Major milestone reached.

Implemented systems:

- Square tile grid (10x10)
- Tile unlock system
- Cloud lock visuals
- Unlock UI
- Resource spawn system
- Harvest interaction (E key)
- Resource respawn
- Save/load system (PlayerPrefs)

Test result:

- Game start: only center tile unlocked
- Player unlocks adjacent tiles
- Resources spawn on unlocked tiles
- Player harvests resources
- Resources respawn correctly
- Tile unlock state persists after restarting the game

Status:

Core gameplay prototype completed.

## 2026-03-06 (추가 메모)

- 타일 언락 UX, 리소스 스폰/리스폰, Unlock UI까지 한 덩어리로 묶여서 \"플레이 타임 5분짜리 루프\"가 완성됨.
- GameProject 리포에 클린 버전(씬 + 스크립트 + 설정 + 문서)을 올려, 앞으로는 버전 관리/협업을 안전하게 진행할 수 있는 기반이 마련됨.