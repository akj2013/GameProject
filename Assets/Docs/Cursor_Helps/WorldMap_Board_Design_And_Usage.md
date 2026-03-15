# 격자형 타일 배치 보드 (World Map Board) — 설계·사용 가이드

런타임에 월드맵을 자동 생성하는 것이 아니라, **씬 안에서 직접 타일을 조립할 수 있는 격자형 배치 보드**를 위한 문서입니다.

---

## 1. 이 방식이 지금 프로젝트에 맞는 이유

- **작업대 중심**: 포토피아 등에서 만든 타일 PNG를 슬롯마다 직접 넣어 가며 월드맵을 구성할 수 있음.
- **자동 생성 없음**: 맵은 실행 시 생성되지 않고, 씬에 미리 배치된 `TileSlot`들이 고정된 “작업판” 역할을 함.
- **Scene / Game 뷰 모두 확인 가능**: 보드는 씬에 있으므로 Scene 뷰에서 배치를 보면서 편집하고, Game 뷰에서도 동일하게 확인 가능 (Canvas를 World Space로 두면 Scene 뷰에서도 보임).
- **Inspector 중심 워크플로**: 각 슬롯에 스프라이트를 넣으면 즉시 반영되고, 나중에 배치 데이터 저장/로드로 확장하기 좋은 구조.

---

## 2. 씬 하이어라키 설계

```
Canvas                    (World Space 권장 — Scene 뷰에서 보이게)
 └── WorldMapBoard        (WorldMapBoard 컴포넌트 부착)
      ├── TileSlot_00
      ├── TileSlot_01
      ├── TileSlot_02
      └── ...
```

- **Canvas**: World Space로 두면 Scene 뷰에서도 격자/타일이 보임.
- **WorldMapBoard**: 격자 설정(행/열, xStep, yStep 등)을 갖는 루트. 자식으로 `TileSlot_xx`만 가짐.
- **TileSlot_00, 01, …**: 각 칸 하나. Inspector에서 타일 이미지(Sprite) 지정.

---

## 3. TileSlot 프리팹 구조

```
TileSlot (RectTransform, TileSlotUI)
 ├── Border    (Image + Outline)  — 격자선/테두리 (흰색 또는 밝은 테두리)
 └── TileImage (Image)           — 타일 스프라이트 표시
```

- **TileSlot**: 슬롯 하나의 루트. `TileSlotUI`에서 `Border`/`TileImage` 참조.
- **Border**: 슬롯 칸 구분용. 반투명 흰색 + Outline로 테두리 표시. 항상 보이도록 설정.
- **TileImage**: Inspector에서 지정한 스프라이트 표시. 비우면 빈 칸.

프리팹 경로: `Assets/Prefabs/UI/WorldMap/TileSlot.prefab`  
(메뉴로 한 번 생성해 두면 이후 재사용.)

---

## 4. 보드 생성 방식

- **자동 생성 아님**: 플레이 시 스크립트가 슬롯을 만들지 않음.
- **에디터 메뉴로 한 번 생성**:
  - **WoodLand3D > World Map Board > Create Board And Generate Slots**
  - 현재 씬에 Canvas가 없으면 World Space Canvas를 만들고, 그 아래에 `WorldMapBoard`를 만든 뒤, `WorldMapBoard`의 **Grid Width / Grid Height / X Step / Y Step**에 따라 `TileSlot_00` … `TileSlot_N`을 생성·배치.
- **아이소메트릭 배치**: 기존 월드맵과 동일한 공식 사용  
  `x = (col - row) * xStep - originX`, `y = -(col + row) * yStep - originY`  
  셀 크기는 `stepLength = sqrt(xStep² + yStep²)`로 정사각형 격자.

생성 후에는 **씬을 저장**하면 보드와 슬롯 구조가 씬에 고정됩니다.

---

## 5. 각 슬롯에 이미지를 넣는 방법

1. Hierarchy에서 **WorldMapBoard** 펼친 뒤 원하는 **TileSlot_xx** 선택.
2. Inspector에서 **Tile Slot UI (Script)** 의 **Tile Sprite** 슬롯에 타일 PNG(스프라이트) 할당.
3. 할당하는 즉시 해당 슬롯의 TileImage에 반영됨 (OnValidate).
4. 비우려면 **Tile Sprite**를 None으로 두면 빈 칸으로 표시.

타일 이미지는 프로젝트에서 Texture Type을 **Sprite (2D and UI)** 로 설정한 뒤 사용하면 됩니다.

---

## 6. 격자선/테두리 표시 방법

- **TileSlot** 프리팹의 **Border** 오브젝트:
  - **Image**: 반투명 흰색 등으로 슬롯 영역 표시.
  - **Outline**: 흰색 테두리로 칸 구분 (effect distance로 두께 조절).
- Scene 뷰에서 보이려면 **Canvas**를 **World Space**로 두고, 해당 Canvas가 씬 카메라에 그려지도록 하면 됨.
- Border 스프라이트를 커스텀 프레임으로 바꾸고 싶다면, TileSlot 프리팹을 열어 Border의 Image에 원하는 스프라이트를 넣으면 됨.

---

## 7. 필요한 C# 코드 전체

이미 다음 파일들로 구현되어 있습니다.

| 파일 | 역할 |
|------|------|
| **Assets/Scripts/UI/WorldMap/TileSlotUI.cs** | 슬롯 하나. `[SerializeField] Sprite tileSprite`, Inspector 변경 시 `OnValidate`로 즉시 표시 갱신. `RefreshDisplay()`, `SetSlotIndex()` |
| **Assets/Scripts/UI/WorldMap/WorldMapBoard.cs** | 보드 루트. 격자 설정(gridWidth, gridHeight, xStep, yStep, autoCenterGrid, gridOrigin). `GetSlot(index)`, `GetSlot(row,col)` |
| **Assets/Editor/WorldMapBoardEditor.cs** | 메뉴 "Create Board And Generate Slots", "Create TileSlot Prefab Only". TileSlot 프리팹 생성/보드·슬롯 일괄 생성 |

- **TileSlotUI**: 타일 스프라이트 표시, Border/TileImage 참조, slotIndex/row/col (나중에 저장용 확장).
- **WorldMapBoard**: 런타임 생성 로직 없음. 설정만 보관하고, 에디터에서 생성한 자식 슬롯만 관리.
- **WorldMapBoardEditor**: 에디터 전용. 슬롯 프리팹 생성 및 보드에 슬롯 배치.

---

## 8. Inspector 세팅 방법

### WorldMapBoard 선택 시

- **Grid Width / Grid Height**: 격자 행/열 수. 슬롯을 **다시 생성**할 때만 반영됨 (기존 슬롯 개수는 바꾸지 않음).
- **X Step / Y Step**: 아이소메트릭 한 칸당 이동량. 슬롯 생성 시 위치와 셀 크기에 사용.
- **Auto Center Grid**: 켜면 그리드 중심이 (0,0).
- **Grid Origin**: Auto Center가 꺼져 있을 때 (0,0) 셀 위치.

슬롯 개수나 간격을 바꾸려면 메뉴에서 **Create Board And Generate Slots**를 다시 실행하면 됩니다 (기존 자식 슬롯은 제거 후 새로 생성).

### TileSlot_xx 선택 시

- **Tile Sprite**: 이 칸에 표시할 타일 이미지. 넣으면 즉시 반영, 비우면 빈 칸.
- **Tile Image Display / Border Display**: 프리팹에서 연결됨. 필요 시 수동으로 할당.
- **Slot Index / Row / Col**: 보드 생성 시 자동 설정. 나중에 저장/로드 시 인덱스로 사용 가능.

---

## 9. 실제로 월드맵을 조립하는 작업 순서

1. **보드 만들기**  
   메뉴 **WoodLand3D > World Map Board > Create Board And Generate Slots** 실행.  
   (TileSlot 프리팹이 없으면 같은 메뉴가 자동으로 생성함.)

2. **Canvas가 World Space인지 확인**  
   Canvas가 없으면 자동 생성됨. 이미 있으면 스크립트가 World Space로 바꿀 수 있음.  
   Scene 뷰에서 격자가 보이면 OK.

3. **씬 저장**  
   File > Save (또는 Save As)로 씬 저장. 보드와 슬롯이 씬에 고정됨.

4. **칸마다 타일 넣기**  
   - Hierarchy에서 **WorldMapBoard** 펼치고 **TileSlot_00**, **TileSlot_01** … 선택.
   - Inspector **Tile Slot UI** > **Tile Sprite**에 타일 PNG(스프라이트) 드래그.
   - 포토피아 등에서 만든 타일을 칸별로 넣어 가며 월드맵 구성.

5. **배치 확인**  
   - **Scene 뷰**: 격자선과 타일 배치 확인.  
   - **Game 뷰**: 플레이 없이도 보드가 보이면(Canvas 설정에 따라) 동일하게 확인.

6. **(선택) 격자 크기/간격 변경**  
   **WorldMapBoard**에서 Grid Width/Height, X Step/Y Step 수정 후,  
   다시 **Create Board And Generate Slots** 실행하면 슬롯이 새 설정으로 재생성됨.  
   기존에 넣어 둔 스프라이트는 초기화되므로, 필요하면 설정을 먼저 확정한 뒤 타일을 넣는 편이 좋음.

7. **나중에 확장**  
   - 각 **TileSlotUI**의 `slotIndex`/`row`/`col`과 `TileSprite`를 이용해 배치 데이터를 수집.
   - ScriptableObject나 JSON으로 저장한 뒤, 다른 씬/런타임에서 같은 배치를 재현하는 로직을 추가하면 됨.

---

## 요약

- **자동 생성 없음** → 씬에 고정된 **TileSlot**들이 “격자형 타일 배치 보드” 역할.
- **한 번** 메뉴로 보드·슬롯 생성 후, **Inspector에서 칸마다 Tile Sprite 지정**으로 월드맵 조립.
- **격자선/테두리**는 TileSlot의 **Border** (Image + Outline)로 표시.
- **Scene / Game 뷰**에서 배치 확인 가능 (Canvas World Space 권장).
- 이후 **배치 데이터 저장/로드**는 TileSlotUI의 인덱스·스프라이트 참조로 확장 가능.
