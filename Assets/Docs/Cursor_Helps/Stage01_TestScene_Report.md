# Stage_01_Woodland 테스트 셋업 보고서

## 1. 생성된 씬 계층

메뉴 **WoodLand3D > Setup Stage_01_Woodland Test Scene** 실행 후 `Stage_01_Woodland.unity` 에 만들어지는 계층:

```
StageRoot
├─ Systems
│  ├─ GridManager          (SquareGridManager)
│  └─ TileUnlockSystem    (TileUnlockSystem)
├─ World
│  └─ Tiles               (타일 인스턴스 부모, 런타임에 GridManager가 채움)
├─ Player                 (Capsule)
├─ Main Camera            (Camera + CameraFollow)
└─ Canvas
   ├─ HUD                 (ResourceHUD + Tree/Rock/Ore 카운터 텍스트)
   ├─ UnlockPanel         (UnlockPanelUI + TilePos/Message 텍스트, Unlock/Close 버튼)
   └─ WorldUI             (FloatingTextWorld, ResourcePickupVFX)
```

- **EventSystem** 은 씬에 없으면 새로 생성됨.
- **Resources / SpawnPoints** 는 타일 프리팹 내부에서 TileResourceSpawner 가 랜덤 오프셋으로 스폰하므로 별도 오브젝트 없음.

---

## 2. 스크립트 부착 대상

| 오브젝트 | 스크립트 |
|----------|----------|
| GridManager | SquareGridManager |
| TileUnlockSystem | TileUnlockSystem |
| Player | PlayerInventory, PlayerHarvestTest |
| Main Camera | CameraFollow |
| Canvas/HUD | ResourceHUD |
| Canvas/UnlockPanel | UnlockPanelUI |
| Canvas/WorldUI | FloatingTextWorld, ResourcePickupVFX |

**프리팹 (에디터에서 생성):**

| 프리팹 | 스크립트/구성 |
|--------|----------------|
| Prefabs/Resources/Resource_Tree.prefab | Cube, BoxCollider, ResourceNode (Tree) |
| Prefabs/Resources/Resource_Rock.prefab | Cube, BoxCollider, ResourceNode (Rock) |
| Prefabs/Resources/Resource_Ore.prefab | Cube, BoxCollider, ResourceNode (Ore) |
| Prefabs/UI/FloatingTextItem.prefab | FloatingTextItem, World Space Canvas, TMP_Text |
| Prefabs/Tiles/Tile.prefab | TileController, BoxCollider, TileResourceSpawner / CloudVisual, UnlockedRoot |

---

## 3. 연결된 인스펙터 참조

- **SquareGridManager**: tilePrefab = Tile 프리팹의 TileController, tilesRoot = World/Tiles, width=10, height=10, tileSize=10, startUnlockedPos=(5,5).
- **TileUnlockSystem**: grid = (런타임에 OnGridReady로 설정), inventory = Player의 PlayerInventory, ui = UnlockPanel의 UnlockPanelUI, cameraFollow = Main Camera의 CameraFollow.
- **TileController** (타일 프리팹): cloudVisual, unlockedRoot, triggerCollider, highlightTarget, resourceSpawner = 동일 오브젝트의 TileResourceSpawner.
- **TileResourceSpawner** (타일 프리팹): tileController, spawnRoot = 타일 루트, prefabs = [Tree, Rock, Ore] 각각 해당 리소스 프리팹.
- **PlayerInventory**: startingGold = 500 (테스트용).
- **CameraFollow**: target = Player transform.
- **ResourceHUD**: treeCounterText, rockCounterText, oreCounterText = HUD 하위 TMP_Text.
- **UnlockPanelUI**: rootCanvas, tilePosText, messageText, unlockButton, closeButton.

---

## 4. 코어 루프 중 기대 동작

- **그리드**: 10x10 타일이 런타임에 생성되고, (5,5)가 시작 해제 타일.
- **시작 타일 자원 스폰**: 해제된 타일에서 Tree 3개, Rock 2개 스폰 (TileResourceSpawner).
- **채집**: Player에서 E 키 → 레이캐스트로 ResourceNode.ApplyDamage(1) → 고갈 시 보상 → PlayerInventory + ResourceGainedEvents.Raise.
- **인벤토리 / UI**: ResourceHUD 카운터 갱신, FloatingTextWorld 플로팅 텍스트, ResourcePickupVFX (픽업 프리팹 있으면 재생).
- **잠긴 타일 접근**: Player 태그 "Player", 타일 트리거 진입 시 TileUnlockSystem.HandleTileTriggerEnter → UnlockPanelUI 표시, CameraFocus 타일.
- **언락**: Unlock 버튼 클릭 시 골드 차감, 타일 해제, SaveState(PlayerPrefs).
- **저장/로드**: 재생 종료 후 재실행 시 LoadStateAndApply로 해제된 타일 복원.

---

## 5. 실패 가능 지점 / 제한사항

- **런타임 미검증**: 실제 재생은 Unity에서 메뉴 실행 후 직접 해야 함. 자동 재생/테스트는 수행하지 않음.
- **ResourcePickupVFX**: pickupVisualPrefab 미할당 시 VFX는 스킵되며, 게임플레이는 정상.
- **FloatingTextItem**: World Space Canvas 사용 시 스케일/정렬은 placeholder 수준이며, 이후 UI로 다듬을 수 있음.
- **기존 씬 오브젝트**: 메뉴 재실행 시 기존 "StageRoot" 가 있으면 제거 후 다시 생성함.

---

## 6. 컴파일 / 런타임 에러

- **작업 시점**: 에디터 스크립트 `Assets/Editor/Stage01TestSceneSetup.cs` 기준 린트 에러 없음.
- **예상 이슈**: TextMeshPro / Unity UI 어셈블리가 없으면 컴파일 실패 가능. 프로젝트에 TMP/UI 사용 중이면 해당 없음.

---

## 7. 적용한 소규모 수정

- **타일 프리팹**: Plane 스케일을 (0.1,0.1,0.1) → (1,1,1), BoxCollider size를 (1,0.2,1) → (10,0.2,10) 로 변경해 tileSize=10 과 일치시킴.
- **씬 셋업**: 기존 "StageRoot" 가 있으면 제거 후 새로 생성하도록 처리해 메뉴 재실행 시 중복 방지.

---

## 8. 이후 수동 교체 권장 사항

- **타일/자원 비주얼**: 현재 Plane + Cube placeholder → 실제 타일/나무/돌/광석 메시·머티리얼로 교체.
- **잠금 클라우드**: CloudVisual → 최종 연기/이펙트 아트로 교체.
- **플레이어**: Capsule → 최종 캐릭터 모델/애니메이션.
- **UI**: UnlockPanel / HUD 레이아웃·폰트·테마를 프로젝트 UI에 맞게 조정.
- **ResourcePickupVFX**: pickupVisualPrefab 에 작은 아이콘/파티클 프리팹 할당 시 픽업 연출 적용.

---

**사용 방법**: Unity에서 **WoodLand3D > Setup Stage_01_Woodland Test Scene** 실행 후 재생하여 위 코어 루프를 확인하면 됩니다. 상세 단계는 `Assets/Docs/Cursor_Helps/Stage01_TestScene_Setup_Guide.md` 참고.
