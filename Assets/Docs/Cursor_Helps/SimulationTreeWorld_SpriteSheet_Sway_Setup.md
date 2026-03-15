# SimulationTreeWorld — 4프레임 스프라이트 시트 흔들림 세팅

나무가 바람에 흔들리는 4프레임 루프를 **SimulationTreeWorld** 씬에서 재생하기 위한 세팅입니다.

---

## 1. 스크립트

- **`Assets/Scripts/UI/SpriteSheetLoop.cs`**
- RawImage에서 4프레임을 루프 재생합니다.

**모드**
- **Single Texture Row**: 한 장의 텍스처에 가로로 4프레임 (1×4). UV Rect로 프레임 전환.
- **Separate Textures**: 프레임마다 텍스처 4개를 배열로 할당.

---

## 2. 이미지 준비 (4프레임)

### A) 스프라이트 시트 1장 (권장)

- 나무 흔들림 4장을 **가로로 한 줄**로 붙인 이미지 1장.
- 예: 400×100 (한 프레임 100×100) 또는 800×200 (한 프레임 200×200).
- Unity에 넣고 **Texture Type = Default** 유지 (Texture2D).
- **Sprite Sheet Loop**의 **Mode = Single Texture Row**, **Sprite Sheet Texture**에 이 텍스처 할당.

### B) PNG 4장

- 프레임별 PNG 4장.
- **Mode = Separate Textures**, **Frame Textures** 배열에 0~3 순서로 할당.

---

## 3. SimulationTreeWorld 씬에서 세팅

### Step 1 — 흔들림 나무용 오브젝트 추가

1. **SimulationTreeWorld** 씬 열기.
2. 하이어라키에서 **Canvas → TreeContainer** 선택.
3. **TreeContainer** 우클릭 → **UI → Raw Image**.
4. 이름을 **Tree_Sway** (또는 원하는 이름)로 변경.

### Step 2 — RectTransform

- **Tree_Sway** 선택.
- Anchor: 원하는 위치 (예: 중앙 하단).
- Pos X/Y, Width/Height: 나무 크기에 맞게 (예: Width 100, Height 150).
- 기존 Tree_1, Tree_2, Tree_3와 비슷한 크기/위치로 두면 됨.

### Step 3 — SpriteSheetLoop 추가

1. **Tree_Sway** 선택.
2. **Add Component** → `SpriteSheetLoop` 검색 후 추가.
3. Inspector에서:

| 필드 | 값 |
|------|-----|
| **Mode** | Single Texture Row (한 장 시트) 또는 Separate Textures (PNG 4장) |
| **Sprite Sheet Texture** | (Single일 때) 4프레임이 가로로 붙은 텍스처 |
| **Frame Count** | 4 |
| **Horizontal Row** | 체크 (가로 1줄이면) |
| **Frame Textures** | (Separate일 때) Size 4, Element 0~3에 PNG 할당 |
| **Frames Per Second** | 6 (원하면 4~8) |
| **Play On Enable** | 체크 |

### Step 4 — 텍스처 할당

- **Single Texture Row**  
  - **Sprite Sheet Texture**에 4프레임 한 줄 텍스처 할당.  
  - Raw Image의 **Texture**는 스크립트가 동작하면 자동으로 이 텍스처로 설정됨.
- **Separate Textures**  
  - **Frame Textures**에 프레임 순서대로 4개 할당.  
  - 첫 프레임은 스크립트가 재생 시 적용함.

### Step 5 — Play

- **Play** 후 나무가 4프레임 루프로 흔들리면 세팅 완료.
- **Frames Per Second**로 속도 조절.

---

## 4. 요약

- **씬**: SimulationTreeWorld  
- **위치**: Canvas → TreeContainer 아래  
- **오브젝트**: Raw Image **Tree_Sway** + **SpriteSheetLoop**  
- **이미지**: 4프레임 한 장(가로 1줄) 또는 PNG 4장  
- **스크립트**: `Assets/Scripts/UI/SpriteSheetLoop.cs`

이미지 4장이 준비되면 위 순서대로 할당하면 됩니다.
