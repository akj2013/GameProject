# Resource Gain Feedback – Setup Guide

Instant resource gain feedback (floating text, pickup VFX, HUD)를 사용하기 위한 Unity Inspector 설정 가이드입니다.

---

## 1. PlayerInventory

- **위치**: 기존처럼 **플레이어 캐릭터** GameObject에 부착.
- **변경 사항**: Gold API는 그대로이며, 리소스 인벤토리와 이벤트가 추가됨.
- **연결**: TileUnlockSystem이 이미 이 컴포넌트를 참조하고 있으면 추가 설정 없음.  
  ResourceHUD에서 사용할 때는 같은 씬에 PlayerInventory가 하나만 있으면 자동 탐색 가능.

---

## 2. ResourceHUD (리소스 카운터 + 최근 획득)

- **추가할 GameObject**: 씬의 **Canvas** 또는 UI 전용 빈 오브젝트.
- **컴포넌트**: `ResourceHUD` 스크립트 추가.
- **할당**:
  - **Inventory**: 플레이어의 `PlayerInventory` (비워두면 씬에서 자동 탐색).
  - **Tree Counter Text** / **Rock Counter Text** / **Ore Counter Text**:  
    각 리소스 개수를 표시할 TMP_Text (TextMeshPro - Text (UI)) 3개.
  - **Recent Gain Texts**: 최근 획득 3~4개를 표시할 TMP_Text 배열 (Size 4, 각 요소에 Text 할당).
  - **Max Recent Entries**: 4 권장.

---

## 3. FloatingTextWorld (월드 플로팅 텍스트)

- **추가할 GameObject**: 씬 루트 또는 빈 오브젝트 (예: `ResourceFeedback`).
- **컴포넌트**: `FloatingTextWorld` 스크립트 추가.
- **Prefab 제작** (선택):
  - 빈 GameObject에 `FloatingTextItem` 스크립트 추가.
  - 자식으로 **UI → Text - TextMeshPro** 생성, 월드 스페이스로 사용하거나,  
    **3D TextMeshPro** 사용.
  - 카메라를 바라보도록 하려면 빌보드 스크립트를 나중에 붙일 수 있음.
- **할당**:
  - **Floating Text Prefab**: 위에서 만든 프리팹.  
    비워두면 `FloatingTextWorld`가 경고만 남기고 동작은 유지됨.
  - **Spawn Offset**: (0, 1, 0) 등 리소스 위쪽 오프셋.

---

## 4. ResourcePickupVFX (픽업 비주얼)

- **추가할 GameObject**: 같은 `ResourceFeedback` 오브젝트 또는 별도 빈 오브젝트.
- **컴포넌트**: `ResourcePickupVFX` 스크립트 추가.
- **Prefab 제작** (선택):
  - 작은 Quad, Sprite, 또는 아이콘용 3D 오브젝트.
  - 리소스 타입별 아이콘을 쓰려면 나중에 확장 가능.
- **할당**:
  - **Pickup Visual Prefab**: 위에서 만든 작은 비주얼 프리팹.  
    비워두면 VFX 없이 진행 (경고 없이 스킵).
  - **Target Transform**: 플레이어 Transform.  
    비워두면 `PlayerInventory`가 붙은 오브젝트를 자동 탐색.
  - **Max Visuals Per Gain**: 3 권장.
  - **Pop Up Height** / **Move Duration** / **Timeout**: 기본값으로 두고 필요 시 조정.

---

## 5. 테스트 방법

1. 플레이 모드 진입.
2. 나무/바위 앞에서 **E** 키로 채집.
3. 확인:
   - 리소스가 고갈되면 **+3 Tree** / **+2 Rock** 같은 **플로팅 텍스트**가 리소스 위에 잠깐 나타났다가 사라짐.
   - **픽업 비주얼**이 리소스 위치에서 살짝 올랐다가 플레이어 쪽으로 이동 후 사라짐.
   - **ResourceHUD**의 Tree/Rock/Ore **숫자가 즉시 증가**.
   - **최근 획득** 영역에 `+3 Tree` 등이 최신 순으로 표시됨.

---

## 6. 요약 체크리스트

| 항목 | 할 일 |
|------|--------|
| PlayerInventory | 플레이어에 유지. TileUnlockSystem 연결 그대로 사용. |
| ResourceHUD | Canvas 등에 추가, TMP 카운터 3개 + 최근 획득용 TMP 4개 할당. |
| FloatingTextWorld | 빈 오브젝트에 추가, FloatingTextItem + TMP 있는 프리팹 할당 (선택). |
| ResourcePickupVFX | 빈 오브젝트에 추가, 작은 비주얼 프리팹 + Target(플레이어) 할당 (선택). |

프리팹을 할당하지 않아도 게임플레이는 동작하며, 인벤토리와 HUD 카운터는 정상 갱신됩니다.

---

## 7. 파일 목록 및 연동 요약

- **생성**: `MyScript/Resources/ResourceGainedEvents.cs`, `MyScript/UI/FloatingTextItem.cs`, `FloatingTextWorld.cs`, `ResourcePickupVFX.cs`, `ResourceHUD.cs`, `Docs/13_RESOURCE_GAIN_FEEDBACK_SETUP.md`
- **수정**: `PlayerInventory.cs` (리소스 + 이벤트), `TileResourceSpawner.cs` (즉시 보상 + Raise)
- **연동**: TileUnlockSystem은 Gold만 사용(변경 없음). TileResourceSpawner가 고갈 시 보상 지급 및 이벤트 발생. UI/VFX는 이벤트 구독.
- **확장 포인트**: 리소스별 아이콘, 플로팅 풀링/빌보드, 보상량 데이터 분리.
