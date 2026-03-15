# UI 월드맵 프로토타입 — 설계·세팅 가이드

Unity UI 기반 타일 월드맵 프로토타입의 전체 설계, 씬 구조, 스크립트 역할, Inspector 연결, 실행 방법을 정리합니다.

---

# 1. 전체 설계 요약

- **방향**: 3D 캐릭터 이동이 아닌, **UI 그리드 + 타일 이미지 + 클릭/탭** 중심.
- **맵**: **아이소메트릭 다이아몬드 격자**로 타일 위치 계산 (GridLayoutGroup 미사용). `x = (col - row) * xStep`, `y = -(col + row) * yStep`, 셀 크기는 인접 격자 간격으로 자동 계산해 정사각형이 겹치지 않게 함.
- **타일 1칸**: 클릭 영역은 셀 크기 유지, 시각은 셀 중앙 기준. 타입·잠금·자원값 등 **TileData**로 관리.
- **동작**: 타일 클릭 → 선택 하이라이트, TileInfoPanel 갱신, 잠금이 아니면 **+N Wood/Stone/Gold** 텍스트가 타일 위에서 떠오르고 사라짐.
- **구조**: 매니저 1개(WorldMapUIManager), 타일 셀 뷰(TileCellUI), 정보 패널(TileInfoPanelUI), Editor 메뉴로 씬/프리팹 일괄 생성.

---

# 2. 씬 하이어라키

```
Canvas
 ├── TopBar              (자원 수치 임시 표시)
 ├── WorldMapRoot
 │    └── WorldMapPanel  (WorldMapUIManager 붙음)
 │         └── Grid     (GridLayoutGroup, 타일 부모)
 ├── TileInfoPanel       (TileInfoPanelUI 붙음)
 ├── BottomMenu          (Build / Upgrade / Replace / Unlock 버튼)
 └── EffectLayer         (획득 텍스트 생성 위치)

EventSystem
Main Camera             (유지)
```

- **Grid**: TileCell 프리팹 부모. **GridLayoutGroup은 비활성화**되고, **WorldMapUIManager**가 행/열 기준으로 각 셀의 anchoredPosition·sizeDelta를 직접 설정.
- **WorldMapPanel**에 **WorldMapUIManager** 붙음. Grid 루트·TileCell 프리팹·xStep·yStep·autoCenterGrid·gridOrigin·TileInfoPanel·EffectLayer 참조.

---

# 2-2. 가짜 아이소메트릭 UI 배치 규칙

## 1. 개요

- **GridLayoutGroup**은 사용하지 않는다. 모든 타일 위치는 **WorldMapUIManager**에서 **tile index → row, column → (x, y)** 공식으로만 계산한다.
- 클릭 영역(RectTransform 크기)은 **cellSize**로 유지하고, 시각적 배치는 **아이소메트릭 보드**처럼 보이도록 한다.
- 잠금 타일을 포함해 **모든 타일이 동일한 규칙**으로 배치된다.

*(이하 3~7항은 예전 스태거/오프셋 방식 참고용. 현재는 위 배치 공식 + Cell Size / Iso Half Width·Height / Tile Image Offset (0,0) 권장.)*
- **아이소 타일 아트**: 마름모형 윗면이 **이미지 한가운데**에 있고, 위·아래·좌·우에 여백(투명 또는 그림자)이 들어가는 경우가 많다. 즉 “시각적 내용”이 셀의 기하학적 중심보다 아래쪽에 있거나, 셀 전체를 채우지 않는다.
- **결과**: 셀과 셀은 붙어 있어도, **타일 아트의 윗면끼리**는 멀리 떨어진 것처럼 보이고, “정사각형 슬롯 위에 이미지가 따로 올라가 있다”는 인상이 강해진다.

## 2. 배치 공식 (xStep / yStep 기반)

- `index` → `row = index / gridWidth`, `col = index % gridWidth`
- **위치**: `x = (col - row) * xStep - originX`, `y = -(col + row) * yStep - originY`
- **원점(origin)**: `autoCenterGrid`가 true면 그리드 중심이 (0,0)이 되도록 `originX`, `originY` 자동 계산. false면 `gridOrigin` 사용.
- **셀 크기**: 정사각형이 겹치지 않도록 `stepLength = sqrt(xStep² + yStep²)`, `cellSize = (stepLength, stepLength)` 로 자동 계산.

---

# 2-2-2. 왜 정사각형으로 보였는지 / 아이소 다이아몬드 배치 (요약)

## 1. 왜 현재 결과가 여전히 정사각형 배치였는지

- **위치만 아이소, 크기는 고정 정사각형**: 이전에는 타일 **중심**만 `(col - row) * halfWidth`, `-(col + row) * halfHeight` 로 옮기고, 각 셀의 **sizeDelta**는 `(72, 72)` 같은 고정 값이었음.
- **인접 격자 간격 < 셀 한 변**: 인접 두 타일 중심 간 거리는 `sqrt(36² + 36²) ≈ 51` 픽셀인데, 셀은 72×72라서 **정사각형이 크게 겹침**.
- **시각적 기반이 정사각형**: RectTransform과 TileImage가 모두 72×72 정사각형이라, “정사각형 블록을 비스듬히 겹쳐 놓은” 느낌만 나고, 다이아몬드 격자 위에 타일이 놓인 느낌이 나지 않음.
- **GridLayoutGroup 사고 잔재**: 셀 크기를 “한 칸 = 72픽셀”로 고정해 두고, 격자 간격(halfWidth, halfHeight)과 분리해서 쓰면서, 격자 간격이 셀보다 작아 겹침이 발생함.

## 2. 아이소풍 UI 배치 공식 설명

- **격자 정의**: 논리 좌표 `(col, row)` (col = 0..gridWidth-1, row = 0..gridHeight-1). 인덱스 `i = row * gridWidth + col`.
- **화면 위치 (패널 로컬)**  
  - `x = (col - row) * xStep - originX`  
  - `y = -(col + row) * yStep - originY`  
  - `xStep`: col이 1 증가할 때 x가 늘어나는 양.  
  - `yStep`: col 또는 row가 1 증가할 때 y가 줄어드는 양(화면 아래가 음수이므로 `-` 부호).
- **원점(origin)**: `(originX, originY)`는 “그리드 전체를 어디에 둘지”를 정함. `autoCenterGrid = true`면 그리드 중심이 (0,0)이 되도록 계산.  
  - `originX = ((gridWidth-1) - (gridHeight-1)) * 0.5f * xStep`  
  - `originY = ((gridWidth-1) + (gridHeight-1)) * 0.5f * yStep`
- **셀 크기(겹침 방지)**: 인접 격자 간격 `stepLength = sqrt(xStep² + yStep²)`. `cellSize = (stepLength, stepLength)`로 두면 정사각형 타일이 서로 겹치지 않고, 다이아몬드 격자 위에만 놓인 것처럼 보임.

## 3. 수정된 C# 코드 요약

- **WorldMapUIManager.cs**  
  - **필드**: `xStep`, `yStep`, `autoCenterGrid`, `gridOrigin`. (기존 `cellSize`, `isoHalfWidth`, `isoHalfHeight` 제거.)  
  - **ApplyIsoLayout()**: GridLayoutGroup은 비활성화하고, `row = i / gridWidth`, `col = i % gridWidth` 후  
    - `x = (col - row) * xStep - originX`, `y = -(col + row) * yStep - originY`  
    - `stepLength = sqrt(xStep² + yStep²)`, `sizeDelta = (stepLength, stepLength)`  
    - 각 TileCell의 `anchoredPosition` / `sizeDelta` 직접 설정 후 `RefreshTileImageLayout(cellSize)` 호출.  
  - 전체 코드는 `Assets/Scripts/UI/WorldMap/WorldMapUIManager.cs` 참고.
- **TileCellUI.cs**: 변경 없음. `RefreshTileImageLayout(cellSize)`로 전달되는 `cellSize`가 위에서 계산된 `(stepLength, stepLength)`이므로, 타일 시각도 격자에 맞게 작은 정사각형으로 표시됨.

## 4. Inspector에서 조정할 값

- **World Map UIManager (WorldMapPanel)**  
  - **xStep**: 한 칸당 X 변화량 (기본 40). 크게 하면 격자가 넓어짐.  
  - **yStep**: 한 칸당 Y 변화량 (기본 24). 크게 하면 세로로 넓어짐.  
  - **autoCenterGrid**: 켜두면 그리드가 패널 중앙에 옴.  
  - **gridOrigin**: autoCenterGrid가 꺼져 있을 때 (0,0) 셀의 위치.
- **TileCell 프리팹 → Tile Cell UI**  
  - **Tile Image Offset**: (0, 0) 권장.  
  - **Tile Image Scale**: 1 권장 (셀 크기를 매니저에서 정하므로).

## 5. 추천 기본값 (xStep, yStep)

- **xStep = 40**, **yStep = 24**: 2:1에 가까운 아이소 느낌. `stepLength ≈ 46.6` → 셀 크기 약 47×47, 겹침 없음.  
- **xStep = 36**, **yStep = 36**: 정사각형에 가까운 다이아몬드. `stepLength ≈ 50.9` → 셀 크기 약 51×51.  
- xStep, yStep을 크게 하면 격자 간격이 커지고 타일이 작아 보임. 작게 하면 타일이 촘촘해짐.

## 6. 실제로 배치가 어떻게 달라지는지

- **이전**: 72×72 정사각형이 격자 중심만 아이소로 옮겨져서, 인접 격자 간격(약 51)보다 셀이 커서 **크게 겹침** → “정사각형을 비스듬히 겹쳐 놓은” 느낌.  
- **수정 후**: 셀 크기를 **인접 격자 간격(stepLength)**과 같게 해서, 타일이 **서로 겹치지 않고** 다이아몬드 격자점 위에만 놓임. 초록/갈색 블록이 작은 정사각형으로 격자점 중심에만 보이고, 전체적으로 **다이아몬드 형태의 보드 위에 타일이 놓인** 구도가 됨.  
- 클릭 영역은 여전히 각 셀의 RectTransform 크기(stepLength × stepLength)이며, 선택·정보 패널 연동은 그대로 동작함.

---

- **WorldMapPanel (WorldMapUIManager)**  
  - **Cell Size**, **Spacing**, **Use Iso Stagger**, **Iso Stagger X**, **Iso Row Scale Y** 필드로 그리드·아이소 보정을 제어한다.  
  - 맵 생성 직후 `ApplyGridLayout()`에서 GridLayoutGroup에 cellSize/spacing을 넣고, Use Iso Stagger가 켜져 있으면 레이아웃을 비활성화한 뒤 각 셀 위치를 직접 계산해 넣는다.  
  - 모든 타일에 대해 `RefreshTileImageLayout(cellSize)`를 호출해 TileImage 크기·위치를 통일한다.
- **TileCell 프리팹 / TileCellUI**  
  - **Tile Image Offset**: 타일 이미지의 anchoredPosition 보정 (기본 (0, -4)). Y를 음수로 주면 이미지가 아래로 내려가서, 아이소 “윗면”이 셀 경계에 더 가깝게 보이게 할 수 있다.  
  - **Tile Image Scale**: 셀 대비 타일 이미지 배율 (기본 1.05). 1보다 약간 크게 하면 셀 틈이 덜 보인다.  
  - **Selection Highlight**: 전체 셀 색 채우기 대신 알파를 낮춰(예: 0.25) 슬롯감을 줄였다. 외곽선/글로우 스프라이트로 바꾸면 더 자연스럽다.

## 4. 필요한 C# 코드

- **WorldMapUIManager.cs**  
  - `cellSize`, `spacing`, `useIsoStagger`, `isoStaggerX`, `isoRowScaleY` 필드 추가.  
  - `ApplyGridLayout()`: GridLayoutGroup에 cellSize/spacing 적용 후, useIsoStagger이면 레이아웃 비활성화하고 행/열 인덱스로 각 셀의 anchoredPosition·sizeDelta 설정, 마지막에 모든 셀에 `RefreshTileImageLayout(cellSize)` 호출.
- **TileCellUI.cs**  
  - `tileImageOffset`, `tileImageScale` 필드 추가.  
  - `RefreshTileImageLayout(Vector2 cellSize)`: TileImage의 anchor/pivot (0.5, 0.5), anchoredPosition = tileImageOffset, sizeDelta = cellSize * tileImageScale.

(위 내용은 이미 해당 스크립트에 반영되어 있다.)

## 5. Inspector에서 조정할 값

- **WorldMapPanel → World Map UIManager**  
  - **Cell Size**: (72, 72) 등 셀 픽셀 크기.  
  - **Spacing**: (0, 0) 권장.  
  - **Use Iso Stagger**: 체크 시 행별 X 스태거 + 행 간격 축소 적용.  
  - **Iso Stagger X**: 0.5면 짝수/홀수 행이 반 칸씩 어긋남.  
  - **Iso Row Scale Y**: 0.85~0.9 정도로 세로 간격을 줄인다.
- **TileCell 프리팹 → Tile Cell UI**  
  - **Tile Image Offset**: (0, -4) ~ (0, -8) 등으로 타일 이미지 세로 위치.  
  - **Tile Image Scale**: 1.0 ~ 1.1.  
- **Selection Highlight** (프리팹 내 Image): Color 알파를 0.2~0.3 수준으로 낮추거나, 스프라이트를 테두리/글로우로 교체.

## 6. 타일이 더 붙어 보이게 하기 위한 추천 수치 예시

- **Cell Size**: (72, 72)  
- **Spacing**: (0, 0)  
- **Use Iso Stagger**: true  
- **Iso Stagger X**: 0.5  
- **Iso Row Scale Y**: 0.88  
- **Tile Image Offset**: (0, -5)  
- **Tile Image Scale**: 1.05  

아트에 따라 **Tile Image Offset Y**를 -3 ~ -8, **Iso Row Scale Y**를 0.85 ~ 0.92 사이에서 미세 조정하면 된다.

## 7. 실제 아트 교체 시 주의할 점

- **픽셀/비율**: 새 타일 아트가 이전과 해상도·비율이 크게 다르면, **Tile Image Scale**과 **Tile Image Offset**을 다시 맞춰야 할 수 있다.  
- **피벗/윗면 위치**: 아트의 “윗면”이 이미지 중앙이 아니면 **Tile Image Offset**으로만 보정이 안 될 수 있다. 가능하면 아트 단에서 피벗을 타일 발밑 또는 윗면 중심에 두고 내보내면 조정이 쉽다.  
- **스태거**: **Iso Stagger X**를 바꾸면 전체 맵이 좌우로 어긋나므로, 최종 아트가 정사각형 그리드용이면 0으로 두고, 스태거가 필요하면 0.4~0.5 구간에서 테스트하는 것을 권장한다.

---

# 3. 프리팹 구조 (TileCell)

```
TileCell (Button)
 ├── TileImage           (Image, 타일 색상/스프라이트)
 ├── SelectionHighlight  (Image, 선택 시만 활성화)
 ├── LockIcon            (Image, 잠금 시만 활성화)
 ├── StateIcon           (선택 사항)
 └── GainTextAnchor      (RectTransform, 획득 텍스트 기준점)
```

- **TileCell**에 **TileCellUI** 스크립트. Inspector에서 TileImage, SelectionHighlight, LockIcon, GainTextAnchor 연결.
- Button 클릭 시 부모에서 **WorldMapUIManager**를 찾아 `OnTileClicked(this)` 호출.

---

# 4. C# 스크립트 전체 코드

이미 다음 파일들로 작성되어 있습니다.

- **Assets/Scripts/UI/WorldMap/TileType.cs** — enum Empty, Forest, House, Mountain, Locked
- **Assets/Scripts/UI/WorldMap/TileData.cs** — type, isLocked, level, displayName, resourceValue, resourceLabel
- **Assets/Scripts/UI/WorldMap/TileCellUI.cs** — 타일 시각 적용, 선택/잠금 표시, 클릭 시 매니저에 전달
- **Assets/Scripts/UI/WorldMap/WorldMapUIManager.cs** — 5x5 맵 생성, 샘플 데이터 할당, 선택 처리, TileInfoPanel 갱신, EffectLayer에 획득 텍스트 생성
- **Assets/Scripts/UI/WorldMap/TileInfoPanelUI.cs** — 선택 타일 정보 표시 (이름, 타입, 레벨, 잠금, Yield)
- **Assets/Editor/UIWorldMapPrototypeSetup.cs** — 메뉴 "WoodLand3D > Setup UI World Map Prototype Scene" 실행 시 TileCell 프리팹 생성 및 씬 구성

코드는 복붙해서 쓰는 형태가 아니라, 위 경로에 생성된 스크립트를 그대로 사용하면 됩니다.

---

# 5. Inspector 세팅 방법

## 자동 세팅 (권장)

1. Unity에서 **메뉴 → WoodLand3D → Setup UI World Map Prototype Scene** 실행.
2. TileCell 프리팹이 없으면 생성되고, 씬이 새로 구성되며 WorldMapUIManager·TileInfoPanel·EffectLayer가 서로 연결됩니다.
3. 씬 저장: **File → Save As → Assets/Scenes/UIWorldMapPrototype.unity**.

## 수동으로 연결할 때

- **WorldMapPanel** 선택 → **World Map UIManager**  
  - Grid Root: **Grid**의 RectTransform  
  - Tile Cell Prefab: **Assets/Prefabs/UI/WorldMap/TileCell**  
  - Tile Info Panel: **TileInfoPanel**의 TileInfoPanelUI  
  - Effect Layer: **EffectLayer**의 RectTransform  

- **TileInfoPanel** 선택 → **Tile Info Panel UI**  
  - Content Root: Content 오브젝트  
  - Title/Type/Level/Locked/Yield Text: Content 자식 TMP_Text들  

- **TileCell** 프리팹 열기 → **Tile Cell UI**  
  - Tile Image, Selection Highlight, Lock Icon, Gain Text Anchor: 해당 자식 오브젝트 할당  
  - (선택) **Sprite Empty / Sprite Forest / Sprite House / Sprite Mountain / Sprite Locked**: 타입별 이미지 할당 시 해당 타일은 색상 대신 이미지로 표시됨  

---

# 5-2. 나무 타일(이미지) 넣는 방법

## PNG 파일을 쓰는 방법

Unity에서 **Image** 컴포넌트는 **Sprite**를 사용합니다. **.png 파일**은 그대로 두면 Texture2D로만 인식되므로, **한 번만** 아래처럼 바꿔 주면 됩니다.

1. **나무 타일 PNG**를 프로젝트에 넣기  
   - 예: `Assets/Art/Tiles/Tile_Forest.png` (원하는 폴더에 복사).

2. **Project** 창에서 해당 PNG 선택 → **Inspector**에서:
   - **Texture Type**: **Sprite (2D and UI)** 로 변경.
   - **Sprite Mode**: Single.
   - **Alpha Is Transparency**: 체크 (검은 배경을 투명하게 쓰려면).
   - **Apply** 클릭.

이렇게 하면 같은 .png 파일이 **Sprite**로도 쓰이게 됩니다. 파일은 그대로 .png이고, Unity가 임포트 설정만 Sprite로 바꾼 것입니다.

## 나무 타일이 보이게 연결하기

**방법 A — 매니저에 넣기 (권장)**  
플레이 시 타일은 **프리팹에서 매번 새로 생성**되므로, 스프라이트는 **씬의 WorldMapPanel**에 넣어야 합니다.

3. **WorldMapPanel** 선택 (Hierarchy에서 Canvas → WorldMapRoot → **WorldMapPanel**).
4. Inspector에서 **World Map UIManager (Script)** 찾기.
5. **Sprite Forest** (및 Sprite House, Sprite Mountain 등) 슬롯에 나무 타일 스프라이트를 드래그해서 넣기.
6. **Play** → Forest 타입 타일이 나무 이미지로 표시됩니다.

**방법 B — 프리팹에 넣기**  
프리팹 자체를 수정해서 쓰려면:

3. **TileCell 프리팹** 열기 (Project에서 `Assets/Prefabs/UI/WorldMap/TileCell` 더블클릭. **Hierarchy에 뜬 인스턴스가 아니라 프리팹 편집 화면**이어야 함).
4. **TileCell** 루트 선택 → **Tile Cell UI (Script)** → **Sprite Forest** 등에 스프라이트 할당.
5. 프리팹 저장 후 Play.

# 6. 실행 후 동작

1. **Play** 시 5x5 그리드가 중앙에 표시되고, 타입별 placeholder 색상(Empty 연초록, Forest 진초록, House 노랑/갈색, Mountain 회색, Locked 어두운 갈색)이 적용됩니다.
2. 타일 클릭 시  
   - 해당 타일만 **SelectionHighlight** 활성화,  
   - **TileInfoPanel**에 타일 이름·타입·레벨·잠금·Yield 갱신,  
   - 잠금이 아니고 resourceValue > 0이면 **+N Wood/Stone/Gold** 텍스트가 **GainTextAnchor** 근처에서 위로 떠오르며 페이드 아웃 후 제거됩니다.
3. BottomMenu의 Build/Upgrade/Replace/Unlock 버튼은 배치만 되어 있고, 선택 타일 연동 등은 추후 확장 가능합니다.

---

# 7. 다음 단계에서 확장하면 좋은 것

- **타일 이미지**: TileCellUI에서 **Sprite Empty / Forest / House / Mountain / Locked** 필드에 타입별 스프라이트 할당 (PNG는 Texture Type을 Sprite로 바꾼 뒤 사용). 할당한 타입만 이미지로 표시되고, 나머지는 placeholder 색상.
- **맵 데이터**: GetSampleMapData()를 **ScriptableObject** 또는 JSON/데이터 테이블로 분리.
- **TopBar**: 실제 자원 수치와 연동 (플레이어 인벤토리, 자원 이벤트 구독).
- **BottomMenu**: Build/Upgrade/Replace/Unlock에 선택 타일 기준 로직·비용·실행 처리.
- **저장/로드**: 타일 상태·잠금 해제·레벨을 저장하고 씬 로드 시 복원.
- **사운드/이펙트**: 타일 클릭 시 사운드, 선택 시 추가 이펙트 등.

---

# 8. 최종 세팅 순서 (따라하기)

1. Unity 6 프로젝트 열기.
2. **TextMeshPro** 임포트: Window → TextMeshPro → Import TMP Essential Resources (최초 1회).
3. 메뉴 **WoodLand3D → Setup UI World Map Prototype Scene** 실행.
4. **File → Save As** → `Assets/Scenes/UIWorldMapPrototype.unity` 저장.
5. **Play** → 타일 클릭으로 선택/정보패널/획득 텍스트 확인.
6. (선택) WorldMapPanel·TileInfoPanel·TileCell 프리팹에서 Inspector 참조가 비어 있으면 위 "수동으로 연결할 때"대로 채우기.

이 순서대로 하면 씬 세팅 + UI 월드맵 프로토타입 + 클릭 반응까지 바로 동작합니다.
