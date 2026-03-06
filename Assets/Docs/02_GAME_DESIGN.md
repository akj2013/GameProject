# Game Design

## 장르

자원 채집 + 마을 확장 + 캐릭터 조작 게임

## 플레이 방식

플레이어는 캐릭터를 직접 이동시키며 자원을 채집한다.

채집 대상:

- 나무
- 돌
- 광석
- 농작물

## 카메라

아이소메트릭 스타일

Rotation
X: 45°
Y: 45°

넓은 시야로 마을과 풍경을 보여주는 것이 목표.

## 맵 구조

사각 타일 기반

각 타일은 다음 상태를 가진다:

- Locked (구름으로 가려짐)
- Unlocked (사용 가능)

타일 확장은 자원 또는 금화를 사용한다.

## 지역 유형

- 숲 지역
- 광산 지역
- 농장 지역
- 마을 지역
- 시장
- 사찰

## Core Gameplay Loop

The main gameplay loop is based on exploration and expansion.

1. Player moves across tiles.
2. Locked tiles are covered by clouds.
3. Player unlocks tiles using gold.
4. Unlocked tiles spawn resources.
5. Player harvests resources.
6. Resources respawn over time.
7. Player expands the map further.

This loop ensures continuous progression and exploration.

## 향후 디자인 메모

- 각 타일은 \"이 타일을 여는 이유\"가 분명하도록, 타일마다 고유 보상(자원 타입, NPC, 스토리 조각)을 최소 하나씩 가진다.
- 플레이어가 길을 잃지 않도록, 항상 다음으로 노려볼 만한 타일(추천 타일)을 UI 상에서 부드럽게 강조해 준다.
- 반복 채집이 지루해지지 않도록, 특정 확률로 작은 이벤트(희귀 자원, NPC 대사, 날씨 변화 등)를 섞어 리듬을 만든다.