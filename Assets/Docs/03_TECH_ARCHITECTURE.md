# Technical Architecture

## 주요 시스템

### Player

- PlayerMovement
- PlayerController
- AutoAttack

### Resource System

- TreeManager
- LogItem
- PlayerCollector

### Camera

- CameraFollowSmoothDamp
- CameraDistanceCuller

### UI

- DropItemPanelManager
- FloatingCoinFx

### World

- TileStageConfig
- Grid System

현재 HexGridManager는 사용하지 않으며
Square Grid 기반으로 변경 예정.


## World System

SquareGridManager
- Generates tile grid
- Manages tile lookup
- Provides neighbor queries

TileController
- Controls tile state
- Handles cloud visuals
- Detects player trigger

TileUnlockSystem
- Handles unlock rules
- Calculates unlock cost
- Saves unlocked tiles

## Resource System

ResourceNode
- Resource health
- Depletion logic
- Respawn timer

TileResourceSpawner
- Spawns resources on unlocked tiles
- Manages respawn

## Player System

PlayerInventory
- Stores gold amount
- Handles spending and adding gold

PlayerHarvest
- Player damages resource nodes
- Triggered by E key

## 기술 메모

- 타일/리소스/플레이어/카메라 시스템은 서로 직접 참조를 최소화하고, 가능한 한 인터페이스나 이벤트를 통해 소통하도록 유지한다.
- 에디터 유틸리티(디버그 메뉴, 테스트 단축키)는 런타임 코드와 네임스페이스를 분리해, 빌드 시 쉽게 제외할 수 있게 만든다.
- 성능 이슈가 보이기 전까지는 조기 최적화 대신, 가독성과 디버깅 편의성을 우선한다.