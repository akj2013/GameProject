# LOD 확인 결과 & LOD Group 만드는 방법

## 1. 확인 결과

### 1-1. Maximum LOD Level (Quality)

| 품질 레벨 | maximumLODLevel | 비고 |
|-----------|-----------------|------|
| Very Low  | 0               | LOD 비활성(항상 최고 디테일만) |
| Low       | 0               | LOD 비활성 |
| **Medium**| **1**           | **LOD 1까지 사용 가능** (현재 사용 중인 품질) |
| High 등   | 0               | LOD 비활성 |

- **현재 사용 품질:** `m_CurrentQuality: 2` → **Medium**.  
  Medium은 이미 **Maximum LOD Level = 1** 이라서, LOD Group이 있으면 LOD1을 쓸 수 있습니다.
- 다른 품질에서도 LOD를 쓰려면 Edit → Project Settings → Quality → 해당 품질 → **Maximum LOD Level** 을 **1** 또는 **2**로 올리면 됩니다.

### 1-2. 나무·바닥 프리팹에 LOD Group 여부

- **나무 프리팹** (MyPrefab, Pandazole, EDGE, BOKI 등): **LOD Group 컴포넌트 없음.**
- **타일/바닥 프리팹** (TileGround, TileHex 등): **LOD Group 없음.**

즉, **LOD Level을 “선택”할 오브젝트가 없어서**, Maximum LOD Level을 1로 해도 효과가 없습니다.  
**나무·바닥 등에 LOD Group을 추가**해야 합니다.

---

## 2. Maximum LOD Level 바꾸는 방법

1. **Edit → Project Settings → Quality** 열기.
2. 왼쪽에서 **사용할 품질**(예: Medium, Low) 선택.
3. **Level of Detail** 섹션 찾기.
4. **Maximum LOD Level**
   - `0` = LOD 0만 사용 (항상 최고 디테일).
   - `1` = LOD 0, 1 사용 (멀면 1단계 낮은 디테일).
   - `2` = LOD 0, 1, 2 사용.

Medium은 이미 1이므로, **나무/바닥에 LOD Group을 만든 뒤** 그대로 두면 됩니다.

---

## 3. LOD Group 만드는 방법

LOD는 **거리에 따라** 다른 메쉬를 보여줍니다.  
- **LOD 0:** 가까울 때 (전체 디테일).  
- **LOD 1, 2:** 멀어질수록 단순한 메쉬(또는 빌보드).

### 방법 A: 지금 메쉬 하나만 있을 때 (LOD 0만 넣기)

나중에 LOD1 메쉬를 만들 수 있을 때까지, **현재 메쉬를 LOD 0으로만** 넣어 두는 방법입니다.

1. **프리팹 열기**  
   Project에서 나무(또는 바닥) 프리팹 더블클릭 → Prefab Mode 진입.
2. **루트 오브젝트 선택**  
   Hierarchy에서 최상위(루트) GameObject 선택.
3. **LOD Group 추가**  
   Inspector 하단 **Add Component** → 검색창에 **LOD Group** 입력 → **LOD Group** 추가.
4. **LOD 0에 현재 렌더러 넣기**  
   - LOD Group 컴포넌트에 **LOD 0** 슬롯이 있음.  
   - 현재 나무/바닥을 그리는 **MeshRenderer**가 붙은 오브젝트를 **LOD 0**에 넣어야 함.  
     - 그 오브젝트가 **루트 자식**이면: Hierarchy에서 해당 자식 오브젝트를 **LOD 0** 슬롯에 **드래그**하거나, LOD Group의 **LOD 0** 옆 **Renderers** 리스트에서 **+** 로 그 오브젝트의 Renderer 추가.  
     - 루트 자체에 MeshRenderer가 있으면: 루트를 LOD 0에 넣기.  
   - **LOD 0 비율:** 보통 0.5~1.0 (예: 0.6 = 화면 높이의 60% 이상일 때 LOD 0).
5. **저장**  
   Prefab Mode 나가서 저장 (Ctrl+S 또는 상단 Save).

이렇게 하면 **동작은 지금과 같고**, 나중에 단순 메쉬(LOD 1)를 만들어서 같은 LOD Group에 추가하면 됩니다.

### 방법 B: LOD 1까지 넣기 (단순 메쉬가 있을 때)

**LOD 1용 메쉬**가 이미 있거나 만들 수 있을 때:

1. **방법 A**처럼 루트에 **LOD Group** 추가하고, **LOD 0**에 기존 디테일 메쉬(Renderer) 넣기.
2. **LOD 1** 슬롯에 **단순화된 메쉬**가 들어간 오브젝트의 **Renderer** 넣기.  
   - 단순 메쉬는 3D 툴(Blender 등)에서 폴리곤 줄이거나, Unity에서 **ProBuilder** / **Simplify** 등으로 만들 수 있음.  
   - 또는 같은 메쉬를 그대로 넣고 **LOD 1 비율만 작게** 두면, “멀 때만 이걸 쓴다”는 구조만 먼저 잡을 수 있음.
3. **비율 설정**  
   - LOD 0: 예) 0.4 (가까움).  
   - LOD 1: 예) 0.15~0.2 (멀음).  
   - LOD 2: 0.05 등 (아주 멀음).  
   퍼센트는 “화면에서 이 오브젝트가 차지하는 높이 비율” 기준이라, 값이 작을수록 더 멀리서도 그 LOD가 씀.

### 방법 C: 여러 자식 메쉬가 있는 나무

나무가 **여러 자식**에 MeshRenderer가 있는 경우(줄기+잎 등):

1. **루트**에 **LOD Group** 추가.
2. **LOD 0**에는 **현재 보이는 모든 Renderer**를 넣어야 함.  
   - LOD Group은 “Renderer 리스트”를 단위로 바꿈.  
   - 줄기 Renderer + 잎 Renderer 둘 다 LOD 0에 넣기 (같은 LOD 0에 여러 개 추가 가능).
3. LOD 1을 쓸 때는 **단순화된 메쉬 하나**만 쓰는 게 보통이라, LOD 1용 자식 오브젝트 하나 만들고 그 Renderer만 LOD 1에 넣습니다.

---

## 4. LOD 비율(Threshold) 간단 설명

- **LOD 0 = 0.5** → 화면 높이의 50% 이상 차지할 때 LOD 0 (가장 디테일).  
- **LOD 1 = 0.2** → 20% 이상일 때 LOD 1.  
- **LOD 2 = 0.05** → 5% 이상일 때 LOD 2 (멀리서).

Quality의 **LOD Bias**가 크면 “더 가까이 있어도 낮은 LOD로 빨리 전환”됩니다.  
Medium은 이미 **LOD Bias 0.5**라서, LOD 전환이 적당히 일찍 일어나도록 되어 있습니다.

---

## 5. 바닥/타일은?

- 바닥 타일은 **평평하고 가까이** 있으므로 LOD 효과가 작을 수 있음.  
- **나무**처럼 멀리서 많이 보이는 오브젝트에 LOD Group을 먼저 넣는 것을 권장.  
- 타일도 “매우 멀리 있는 타일”을 단순 메쉬로 줄이고 싶다면, 같은 방식으로 LOD Group 추가 후 LOD 1에 단순 메쉬 넣으면 됨.

---

## 6. 요약

| 항목 | 상태 | 할 일 |
|------|------|--------|
| Quality Maximum LOD Level | Medium = 1 (이미 사용 가능) | 다른 품질 쓸 때만 1 이상으로 설정 |
| 나무/바닥 프리팹 | LOD Group 없음 | 루트에 LOD Group 추가, LOD 0에 현재 메쉬 넣기 |
| LOD 1 메쉬 | 없음 | 나중에 단순 메쉬 만들면 LOD 1 슬롯에 추가 |

**지금 바로:** 자주 쓰는 나무 프리팹 1~2종만 골라서, 루트에 **LOD Group** 추가하고 **LOD 0에 기존 메쉬**만 넣어 두면, 구조는 갖춰집니다.  
이후 단순 메쉬를 만들면 **LOD 1**에 넣어서 멀리 있는 나무는 가벼운 메쉬로 그리게 할 수 있습니다.
