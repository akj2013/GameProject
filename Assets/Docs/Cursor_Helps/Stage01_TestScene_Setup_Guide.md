# Stage_01_Woodland 테스트 씬 셋업 가이드

리팩터 후 코어 루프(그리드 → 자원 스폰 → 채집 → 인벤토리/UI → 타일 언락 → 저장/로드) 검증용 최소 셋업입니다.

## 자동 셋업 (권장)

1. Unity에서 프로젝트 열기
2. 메뉴 **WoodLand3D > Setup Stage_01_Woodland Test Scene** 실행
3. `Assets/Scenes/Stages/Stage_01_Woodland.unity` 저장 여부 확인
4. 재생(Play) 후 아래 검증 체크리스트 실행

이 메뉴는 다음을 수행합니다.

- **프리팹 생성** (없을 때만)
  - `Assets/Prefabs/Resources/Resource_Tree.prefab` (Cube + ResourceNode Tree)
  - `Assets/Prefabs/Resources/Resource_Rock.prefab` (Cube + ResourceNode Rock)
  - `Assets/Prefabs/Resources/Resource_Ore.prefab` (Cube + ResourceNode Ore)
  - `Assets/Prefabs/UI/FloatingTextItem.prefab` (FloatingTextItem + World Space Canvas + TMP_Text)
  - `Assets/Prefabs/Tiles/Tile.prefab` (TileController + TileResourceSpawner + CloudVisual + UnlockedRoot)
- **씬 계층 구성**
  - StageRoot → Systems (GridManager, TileUnlockSystem), World/Tiles, Player, Main Camera, Canvas (HUD, UnlockPanel, WorldUI)
- **인스펙터 참조 연결**
  - SquareGridManager: tilePrefab, tilesRoot, width/height 10, tileSize 10, startUnlockedPos (5,5)
  - TileUnlockSystem: inventory, ui, cameraFollow
  - Player: PlayerInventory (startingGold 500), PlayerHarvestTest, Tag "Player"
  - Camera: CameraFollow target = Player
  - ResourceHUD: tree/rock/ore counter TMP_Text
  - UnlockPanelUI: rootCanvas, tilePosText, messageText, unlockButton, closeButton
  - FloatingTextWorld: floatingTextPrefab
  - ResourcePickupVFX: (선택) pickupVisualPrefab 없으면 VFX 스킵

## 수동으로 할 일 (필요 시)

- **골드 부족 시**: Player 인스펙터에서 `PlayerInventory.startingGold` 를 500 이상으로 설정해 언락 테스트
- **저장 초기화**: PlayerPrefs 키 `TileUnlockData` 삭제 후 재생하면 시작 타일만 해제된 상태로 로드
- **VFX**: ResourcePickupVFX에 작은 비주얼 프리팹을 할당하면 픽업 시 플레이어 쪽으로 날아가는 이펙트 재생 (없어도 게임플레이는 동작)

## 검증 체크리스트

1. 재생 시작 → 시작 타일(5,5) 해제됨, 나무/돌 스폰
2. 플레이어를 자원 쪽으로 이동 → E 키로 채집
3. 자원 고갈 → 인벤토리 증가, 플로팅 텍스트, HUD 카운터 갱신
4. 잠긴 인접 타일로 이동 → Unlock UI 표시
5. 골드 충분 시 Unlock 버튼 클릭 → 타일 해제, 자원 스폰
6. 재생 종료 후 다시 재생 → 해제된 타일이 저장/로드로 복원되는지 확인

## 문제 발생 시

- **타일이 안 보임**: Main Camera 위치/각도 확인 (스크립트는 (0,15,-10) 근처로 설정)
- **E 키 반응 없음**: 플레이어가 자원을 정면으로 바라보고 있는지, 레이거리(기본 3) 안인지 확인
- **Unlock UI 안 뜸**: 플레이어 Tag "Player", TileController 트리거(BoxCollider isTrigger) 확인
- **컴파일 에러**: `Assets/Editor/Stage01TestSceneSetup.cs` 네임스펙/어셈블리 참조 확인

이후 실제 아트/프리팹으로 교체할 때는 동일 계층과 참조를 유지한 채 메시/비주얼만 교체하면 됩니다.
