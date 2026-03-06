# Tile Expansion System

맵은 사각 타일 기반으로 구성된다.

## 타일 상태

Locked
- 구름으로 가려짐

Unlocked
- 플레이 가능

## 해제 방법

플레이어가 타일 근처에 가면
해제 UI가 나타난다.

해제 비용:

- 금화
- 자원

## 해제 과정

1. 플레이어 접근
2. 해제 UI 표시
3. 비용 확인
4. 해제 버튼 클릭
5. 구름 애니메이션
6. 타일 활성화

## 타일 구조

Tile
 ├ Locked
 │   └ Cloud
 ├ Unlocked
 │   ├ Resources
 │   └ Buildings
 └ Trigger


 ## Current Implementation

Tile grid:

Grid Size: 10 x 10  
Tile Size: 10 units

World generation:

- SquareGridManager generates the tile grid.
- Only the center tile starts unlocked.

Unlock condition:

A tile can be unlocked if:

- At least one adjacent tile (up, down, left, right) is already unlocked.

Unlock process:

1. Player approaches a locked tile.
2. Tile highlight appears.
3. Unlock UI appears.
4. Player pays gold cost.
5. Cloud disappears with animation.
6. Tile becomes active.
7. Resources spawn.

Unlock data is saved using PlayerPrefs.

## 디자인 메모

- 타일 확장 비용 곡선은 \"조금씩 비싸지지만, 한 번에 크게 막히지는 않도록\" 완만한 커브로 설계한다.
- 플레이어가 괜히 열었다고 느끼지 않게, 새로 언락한 타일에는 항상 눈에 띄는 변화(자원, 뷰, NPC 등)를 배치한다.
- 너무 많은 잠금 타일이 한 번에 보이면 피로하니, 카메라 구성과 안개/배경을 활용해 항상 \"다음 두세 칸\" 정도만 신경 쓰이게 만든다.