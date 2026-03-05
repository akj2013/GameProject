# FarmLand 스타일 광석: Blender UV → Unity 적용 (순서)

Blender에서 만든 **OreRock** 메쉬(Icosphere + Randomize + Z 0.7)에 UV를 펼치고, **Ore Atlas(Stone/Gold/Ruby/Sapphire)** 텍스처를 붙여서 Unity까지 가져오는 순서입니다.

---

## 전제

- Blender에서 **OreRock** 메쉬가 이미 만들어져 있음 (Edit Mode → Randomize → S Z 0.7 완료).
- **Ore Atlas** 텍스처가 프로젝트에 있음.  
  - 파일명 예: `OreAtlas.png` 또는 `OreAtlas_Stone_Gold_Ruby_Sapphire.png`  
  - 권장 위치: `Assets/MyPrefab/MyResources/Ores/OreAtlas.png`  
  - 레이아웃: 2×2 (좌상 Stone | 우상 Gold / 좌하 Ruby | 우하 Sapphire), 씬·컨셉 이미지와 색감 맞춤.

---

## Part 1. Blender에서 UV 펼치기

### 1단계: UV Editing 워크스페이스로 전환

1. Blender 상단 **워크스페이스 탭**에서 **UV Editing** 클릭.
2. 왼쪽이 **UV Editor**, 오른쪽이 **3D Viewport**로 보이면 됨.

---

### 2단계: Edit Mode 진입

1. **OreRock**(또는 Icosphere) 오브젝트 선택.
2. **Tab** → **Edit Mode** 진입.
3. **A** 한 번 → **전체 선택**.

---

### 3단계: Smart UV Project로 UV 자동 펼치기

광석은 형태가 단순하므로 **Smart UV Project** 한 번이면 충분합니다.

1. **3D Viewport**에서 **우클릭** (또는 상단 **UV** 메뉴).
2. **UV** → **Smart UV Project** 선택.
3. 뜨는 창에서:
   - **Angle Limit**: `66` (기본값 유지 가능).
   - **Island Margin**: `0.02`.
4. **OK** 클릭 → 자동으로 Unwrap 됨.

---

### 4단계: UV가 0~1 영역 안에 오도록 정리 (선택)

1. **UV Editor** 왼쪽에서 **전체 선택** 아이콘(네모 네 개) 클릭하거나, UV 영역을 **A**로 전체 선택.
2. **S** (Scale) → **2** 정도 입력 후 **Enter** → UV가 작게 모여 있으면 키움.
3. **G** (Move)로 **0~1** 사각형 안에 들어오게 배치.
   - 광석은 **메쉬 하나 + 텍스처 하나**이므로, UV를 **한 덩어리**로 두고 **전체가 (0,0)~(1,1)을 쓰도록** 두면 됨.  
   - 나중에 Unity에서 **Material별 Tiling/Offset**으로 Stone/Gold/Ruby/Sapphire 영역을 나눠 씀.

---

### 5단계: 머티리얼 + 텍스처 연결 (Blender에서 미리보기용)

1. **오른쪽 Properties** → **Material Properties**(빨간 구 아이콘).
2. **New** → 머티리얼 이름: `OreMaterial`.
3. **Surface** → **Base Color** 옆 **점** 클릭 → **Image Texture**.
4. **Open** → 프로젝트의 **OreAtlas.png** 선택 (아직 없으면 Unity에서 만든 뒤 Blender에서 열어도 됨).
5. **UV Editor** 상단 **Image** → **Open** → 같은 **OreAtlas.png** 선택.
   - UV Editor에 아틀라스가 보이고, 펼친 UV가 그 위에 올라가 있으면 OK.

---

### 6단계: FBX로 내보내기

1. **Object Mode**로 전환 (**Tab**).
2. **File** → **Export** → **FBX (.fbx)**.
3. 설정:
   - **Selected Objects**만 체크 (OreRock만 선택된 상태로).
   - **Mesh** → **Apply Modifiers** 체크.
   - **Geometry** → **Tangent Space** 등은 기본값.
4. 저장 경로: Unity 프로젝트 `Assets/MyPrefab/MyResources/Ores/` (또는 원하는 폴더).
5. 파일명 예: `OreRock.fbx` → **Export FBX**.

---

## Part 2. Unity에서 적용

### 1단계: FBX와 아틀라스 가져오기

1. **OreRock.fbx**를 Unity `Assets` 안으로 넣기 (드래그 또는 복사).
2. **OreAtlas.png**를 같은 프로젝트(예: `Assets/MyPrefab/MyResources/Ores/`)에 넣기.
3. **OreAtlas** 선택 → Inspector에서 **Texture Type**: `Default`, **Max Size** 적당히(예: 1024), **Apply**.

---

### 2단계: 머티리얼 4개 만들기 (Stone / Gold / Ruby / Sapphire)

아틀라스가 **2×2**일 때:

- **좌상** (U 0~0.5, V 0.5~1): Stone  
- **우상** (U 0.5~1, V 0.5~1): Gold  
- **좌하** (U 0~0.5, V 0~0.5): Ruby  
- **우하** (U 0.5~1, V 0~0.5): Sapphire  

**방법: Tiling & Offset**

1. **Assets**에서 우클릭 → **Create** → **Material**.
2. 이름: `MAT_Ore_Stone`.
3. **Shader**: **Universal Render Pipeline / Lit** (또는 프로젝트 기본 Lit).
4. **Base Map**에 **OreAtlas** 할당.
5. **Tiling**: `X 0.5, Y 0.5` (한 칸만 쓰기).
6. **Offset**:
   - **Stone**: X `0`, Y `0.5`
   - **Gold**: X `0.5`, Y `0.5`
   - **Ruby**: X `0`, Y `0`
   - **Sapphire**: X `0.5`, Y `0`
7. 같은 방식으로 **MAT_Ore_Gold**, **MAT_Ore_Ruby**, **MAT_Ore_Sapphire** 만들고 각각 Offset만 위 값으로 설정.

---

### 3단계: 프리팹 만들기

1. **OreRock** FBX를 씬에 드래그.
2. **Hierarchy**에서 **OreRock** 선택 → **Inspector**에서 **Material** 슬롯에 **MAT_Ore_Stone** 할당 → 씬에서 Stone으로 보이는지 확인.
3. **Project**에서 **OreRock**을 드래그해 **Prefab** 폴더에 넣어 **OreStone** 프리팹 생성.
4. **OreGold**, **OreRuby**, **OreSapphire** 프리팹은 **OreStone** 복제 후 Material만 각각 **MAT_Ore_Gold / Ruby / Sapphire**로 바꾸면 됨.

---

### 4단계: (선택) MaterialPropertyBlock으로 런타임에 종류만 바꾸기

머티리얼 1개만 쓰고, 스크립트에서 **MaterialPropertyBlock**으로 **Base Map Offset**만 바꾸면 인스턴스당 머티리얼 복제 없이 Stone/Gold/Ruby/Sapphire 전환이 가능합니다.  
필요하면 해당 스크립트 예제를 별도 문서로 정리할 수 있습니다.

---

## 요약 표

| 단계 | 위치 | 할 일 |
|------|------|--------|
| 1 | Blender | UV Editing 워크스페이스, Edit Mode, A 전체 선택 |
| 2 | Blender | UV → Smart UV Project (Angle 66, Margin 0.02) → OK |
| 3 | Blender | UV Editor에 OreAtlas.png 열기, 머티리얼 Base Color에 Image Texture 연결 |
| 4 | Blender | File → Export → FBX (OreRock.fbx) |
| 5 | Unity | OreRock.fbx, OreAtlas.png 임포트 |
| 6 | Unity | MAT_Ore_Stone/Gold/Ruby/Sapphire 생성, Tiling (0.5, 0.5), Offset으로 영역 지정 |
| 7 | Unity | OreRock에 머티리얼 할당 → 프리팹 4종 생성 |

이 순서대로 하면 씬/컨셉 이미지와 색감을 맞춘 Ore Atlas로 광석을 바로 사용할 수 있습니다.
