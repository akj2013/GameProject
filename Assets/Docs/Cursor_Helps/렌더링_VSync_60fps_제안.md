# 60 FPS를 위한 렌더링·VSync 제안

현재 CPU ~31ms, GPU ~20ms로 약 30 FPS입니다. 60 FPS(16.67ms/프레임)를 위해 **렌더링**과 **VSync** 쪽에서 손볼 수 있는 항목만 정리했습니다.

---

## 1. VSync 쪽 (프레임 페이싱)

### 1-1. 현재 의미

- **vSyncCount: 1** (Medium) = 매 프레임 모니터 vblank에 맞춤 → 60Hz 화면이면 **최대 60 FPS**.
- 프레임이 16ms보다 오래 걸리면(지금 31ms) 한 vblank를 놓치므로 **실제로는 ~30 FPS**로 보입니다.
- 프로파일러의 "VSync" 구간이 크다는 건, **한 프레임이 16ms를 넘어서** 다음 vblank까지 기다리거나 프레임이 끊기는 시간이 길다는 뜻입니다.

### 1-2. 손볼 수 있는 것

| 항목 | 위치 | 제안 |
|------|------|------|
| **vSyncCount** | Edit → Project Settings → **Quality** → 사용 중인 레벨(예: Medium) | **0 (Don’t Sync)** 로 두면 vblank 대기를 하지 않음. 60에 못 미쳐도 제출 간격이 더 촘촘해져 체감 프레임이 올라갈 수 있으나 **화면 티어링** 가능. 모바일에서는 기기/드라이버에 따라 VSync가 강제될 수도 있음. |
| **Application.targetFrameRate** | 스크립트 (예: GameManager Awake) | `Application.targetFrameRate = 60;` 으로 상한 고정. VSync 끈 상태에서 과도한 프레임 제출을 막아 배터리/발열 완화. |
| **Adaptive VSync** | Quality → **Adaptive VSync** | 1로 두면 FPS가 떨어질 때 자동으로 vSyncCount를 2(30 FPS) 등으로 바꿔 주어 끊김을 완화. “60으로 올리기”보다는 “30을 안정화”할 때 유용. |

**정리:**  
60 FPS를 **안정적으로** 만들려면 **프레임 시간을 16ms 아래로 줄이는 것**이 우선입니다. VSync 설정만으로는 31ms를 16ms로 줄일 수 없고, **렌더링·스크립트 비용 감소**가 필요합니다.

---

## 2. 렌더링 (CPU·GPU 둘 다)

### 2-1. 그림자 (지금도 부담이 큰 편)

| 항목 | 현재(Medium) | 제안 |
|------|----------------|------|
| **Shadow Distance** | 20 | **25~30** 권장(아래 참고). 너무 줄이면 캐릭터 근처 그림자가 사라짐. |
| **Shadow Cascades** | 1 | 1 유지 권장. 이미 최소. |
| **Shadow Resolution** | 0 (Low) | 유지. |
| **Shadows** | 1 (Hard) | 1 유지. Soft(2)는 더 무거우므로 모바일에서는 1 유지. |

**Shadow Distance는 카메라 기준입니다.**  
- Unity는 **카메라에서** 이 거리 **안쪽**에 있는 것만 그림자를 그림. 거리 **밖**은 전부 그림자 없음.
- 값을 **15**처럼 너무 줄이면, 3인칭처럼 카메라가 캐릭터 뒤쪽에 있을 때 **캐릭터/캐릭터 발밑**이 카메라로부터 15 유닛 밖으로 잡혀서, 캐릭터 근처에 그림자가 아예 안 나올 수 있음.

**그래서 어떻게 하면 되나:**  
- **캐릭터 주변까지 그림자가 나오게 하려면** Shadow Distance를 **25~30** 정도로 두는 걸 권장. (20보다는 부하가 조금 있지만, 15면 캐릭터 근처가 잘려서 비추천.)
- 극한으로 줄이고 싶다면 **20**까지는 괜찮은 경우가 많고, **15**는 카메라–캐릭터 거리가 짧은 게임이 아니면 캐릭터 근처가 잘림.

**설정 위치:**  
Edit → Project Settings → **Quality** → 사용 중인 품질 레벨(예: Medium) → **Shadow Distance** 등.

### 2-2. URP 에셋 (있는 경우)

- **URP Asset** (Project에서 사용 중인 Render Pipeline Asset 선택)에서:
  - **Main Light → Cast Shadows** 켜져 있는지 확인(끄면 그림자 자체가 없어져 가장 가볍지만 그림자 없음).
  - **Shadow Distance** 가 Quality보다 작게 잡혀 있으면 그 값이 실제 상한이 됨. 캐릭터 근처까지 그림자 나오게 하려면 25~30 권장(15는 카메라 기준이라 잘림).
  - **Cascade Count** 1로 두기.
- **Renderer** (Forward Renderer 등):
  - **Shadows** 관련에서 불필요한 옵션 끄기.

### 2-3. 해상도 스케일 (GPU·일부 CPU 감소)

- **Quality** → **Resolution Scaling Fixed DPI Factor** (또는 프로젝트에 있는 **Resolution Scale**):
  - 1 → **0.9** 또는 **0.85** 로 낮추면 픽셀 수가 줄어 GPU 부하와 일부 렌더링 CPU가 감소.
- 모바일에서는 **Player Settings → Resolution and Presentation** 에서 기본 해상도/전체화면 비율을 낮추는 방법도 있음.

### 2-4. 카메라·오브젝트

- **Occlusion Culling** 사용 가능하면 사용(복잡한 맵일 때).
- **Far Clip Plane** 을 필요 이상으로 크게 두지 않기.
- 이미 적용했다면 유지: **나무 제외 환경 오브젝트 Static**, **단일 머티리얼(PandaMat)** 로 SetPass/배칭 유지.

### 2-5. 포스트 프로세싱·효과

- **Volume** 에서 불필요한 이펙트(블룸, 색수차, 비네팅 등) 끄거나 강도 낮추기.
- **안티앨리어싱** 이 켜져 있으면 Quality에서 끄기(Medium은 이미 0이면 유지).

### 2-6. 라이트

- **실시간 그림자** 쓰는 건 방향광 1개만 두기(이미 그렇게 되어 있으면 유지).
- **Realtime Reflection Probes** 는 Quality에서 0 유지(Medium에서 이미 0이면 유지).

---

## 3. 적용 순서 제안

1. **Quality (Medium)**  
   - **Shadow Distance** 20 → **25** 또는 **30** (캐릭터 근처 그림자 유지. 15는 카메라 기준이라 잘림).
2. **URP Asset**  
   - Shadow Distance / Cascade 1 확인, 필요 시 25~30으로 맞추기.
3. **Resolution Scale**  
   - 0.9 시도 후 체감 품질 보고 0.85까지 검토.
4. **VSync**  
   - 60이 안정적으로 나오기 전까지는 **vSyncCount 1 유지** 권장.  
   - 끊김이 심하면 **Adaptive VSync** 켜서 30 FPS로 안정화하는 선택지.
5. **targetFrameRate**  
   - 스크립트에서 `Application.targetFrameRate = 60;` 한 번 설정(이미 있으면 유지).

---

## 4. 기대 효과 (대략)

- **Shadow Distance 20 → 25~30:** 캐릭터 근처 그림자 유지하면서, 40~70보다는 가벼움. 15로 줄이면 캐릭터 주변 잘림.
- **Resolution Scale 1 → 0.9:** 픽셀 수 19% 감소 → GPU, 일부 CPU 감소.
- **VSync 0:** 체감 프레임은 올라갈 수 있으나, 31ms가 그대로면 “60처럼 부드럽다”까지는 어렵고, 티어링 가능.

**핵심:**  
VSync/설정만으로는 31ms를 16ms로 만들 수 없습니다. **렌더링(그림자·해상도·배칭)** 과 **스크립트(이미 적용한 탐색 주기·캐시)** 를 같이 줄여서 **한 프레임 총 시간**을 16ms 아래로 낮추는 것이 60 FPS로 가는 지름길입니다.

---

## 5. 이 Quality 창에서 정확히 바꿀 것 (체크리스트)

**위치:** Edit → Project Settings → **Quality** → 사용 중인 품질(예: **Medium**) 선택 후, 아래만 순서대로 바꿉니다.

| # | 섹션 | 항목 이름 | 현재 값 | 바꿀 값 | 비고 |
|---|------|-----------|---------|--------|------|
| 1 | **Rendering** | **Resolution Scaling Fixed DPI Factor** | `1` | **`0.9`** | 숫자 직접 입력. GPU 부하 감소. |
| 2 | **Rendering** | **VSync Count** | Every V Blank | **Don't Sync** | 드롭다운에서 "Don't Sync" 선택. (Android는 무시될 수 있음) |
| 3 | **Shadows** | **Shadow Distance** | `20` | **`25`** 또는 **`30`** | 숫자 입력. 15로 줄이면 카메라 기준이라 캐릭터 근처 그림자가 잘림 → 25~30 권장. |
| 4 | **Textures** | **Global Mipmap Limit** | 0: Full Resolution | **1: Half Resolution** | 드롭다운. 멀리 있는 텍스처 해상도 낮춤. |
| 5 | **Level of Detail** | **LOD Group Bias** | `0.7` | **`0.5`** | 숫자 입력. LOD가 더 빨리 낮은 단계로 전환. |
| 6 | **Level of Detail** | **Maximum LOD Group Level** | `0` | **`1`** 또는 **`2`** | 0이면 항상 최고 디테일만 사용. 1 이상이어야 LOD1/LOD2 사용 가능. **(나무·환경 모델에 LOD 그룹이 있을 때만 효과 있음)** |

**유지할 것 (건드리지 않기):**
- Realtime Reflection Probes: 체크 해제 유지
- Realtime GI CPU Usage: Low 유지
- Render Pipeline Asset: Medium URP 유지 (별도로 “Low” URP 에셋 만든 뒤 바꾸는 건 나중에)

**선택(원하면 추가로):**
- **Terrain** 섹션에서 **Detail Distance** `80` → `50`, **Tree Distance** `5000` → `3000` 등으로 줄이면 지형/나무 부하 감소 (지형 많이 쓰는 경우만).
- **Anisotropic Textures**: `Per Texture` → `Disable` 하면 GPU 조금 더 줄지만, 품질 저하 가능.

적용 후 플레이해서 프레임·체감 확인하고, 여전히 무거우면 Resolution Scaling을 **0.85**까지 더 낮춰보면 됩니다. (Shadow Distance는 25 미만으로 내리면 캐릭터 근처 그림자가 잘리므로 25~30 유지 권장.)
