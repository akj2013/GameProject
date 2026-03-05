# WoodLand

WoodLand는 한국 풍경과 전통 건축을 배경으로 한
자원 채집 중심의 모바일 게임이다.

## 핵심 컨셉

- 플레이어가 직접 캐릭터를 조작하여 자원을 채집
- 한국 전통 건축과 자연 풍경을 게임 세계에 표현
- 타일 확장 시스템을 통해 세계가 점점 확장됨
- NPC들이 실제로 생활하는 마을 시스템

## 주요 특징

- 직접 이동 캐릭터
- 자동으로 일하는 Worker NPC
- 상점 / 대화 NPC
- 타일 확장 시스템
- 자원 채집 루프

## 주요 자원

- 나무
- 돌
- 광석
- 금화
- 보석

## 게임 루프

1. 플레이어가 자원을 채집
2. 자원을 마을로 운반
3. 장비 업그레이드
4. 새로운 타일 해제
5. NPC 활동 확장
6. 더 많은 자원 확보

## Current Prototype Status (2026-03-06)

The core gameplay loop is now implemented and playable inside Unity.

Current loop:

Tile Expansion
→ Resource Spawn
→ Player Harvest
→ Resource Respawn
→ Expand Next Tile

Implemented systems:

- Square tile world (10x10 grid)
- Tile unlock system
- Cloud visual lock
- Resource spawn system
- Harvest system (E key)
- Resource respawn
- Save/Load using PlayerPrefs
- Unlock UI

This version is considered the **Core Gameplay Prototype**.

## 앞으로의 방향 (요약)

- 채집 → 판매 → 확장까지 이어지는 \"골드 루프\"를 명확하게 설계해, 플레이 타임 5~10분 단위로 성취감을 주는 것을 목표로 한다.
- NPC/마을 요소는 초반에는 장식에 가깝게 두고, 코어 루프가 단단해진 뒤 서서히 비중을 키운다.
- 한국 풍경/건축 느낌은 과한 디테일보다는 색감·실루엣·구도 위주로 살려, 모바일에서도 부담 없는 스타일을 유지한다.