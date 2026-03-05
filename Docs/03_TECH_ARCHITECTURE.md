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