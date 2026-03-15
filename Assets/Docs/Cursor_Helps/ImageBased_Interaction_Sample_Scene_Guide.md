# 이미지 기반 상호작용 샘플 씬 가이드

Canvas/UI 기반으로 "배경 이미지 + 클릭 가능한 나무 노드 3개 + 상태 전환 + 반짝이 + +1 팝업" 테스트 씬을 만드는 방법입니다.  
**Raw Image + Texture2D**를 사용하므로 **PNG 파일을 스프라이트로 바꾸지 않고** 그대로 쓸 수 있습니다.

---

## 0. PNG와 스프라이트 차이 (Unity 기준)

| 구분 | PNG 파일 | 스프라이트(Sprite) |
|------|----------|---------------------|
| 의미 | 이미지 파일 포맷 | Unity가 만드는 “2D/UI용 이미지 에셋” |
| 임포트 | Project에 넣으면 **Texture**로 들어옴 (기본값: Texture Type = Default) | PNG 선택 후 **Texture Type = Sprite (2D and UI)**로 바꾸면 생성됨 |
| 쓰는 컴포넌트 | **Raw Image** (`.texture`에 Texture2D 할당) | **Image** (`.sprite`에 Sprite 할당) |
| 장점 | 설정 변경 없이 PNG 그대로 사용 | 9-slice, Fill Amount 등 UI 기능 많음 |

**정리:** PNG만 있을 때는 **Raw Image**에 **Texture2D**(임포트 기본값)를 넣으면 되고, **Image**를 쓰려면 해당 PNG의 Texture Type을 **Sprite**로 바꿔야 합니다. 이 샘플은 **Raw Image + PNG(Default)**로 구성합니다.

---

## 1. 하이어라키 구조 제안

```
Canvas (Screen Space - Overlay)
├── BgImage          ← 배경 이미지 (전체 화면, Raw Image 권장)
├── TreeContainer    ← 빈 오브젝트 (나무 배치용)
│   ├── Tree_1       ← Raw Image + TreeInteractionNode
│   ├── Tree_2
│   └── Tree_3
├── EffectLayer      ← 빈 오브젝트 (반짝이/팝업 생성 위치)
└── EventSystem      ← 기존 또는 자동 생성
```

- **Canvas**: Render Mode = Screen Space - Overlay.
- **BgImage**: **Raw Image** 권장. PNG를 그대로 넣으려면 Texture Type을 Default로 두고 Texture2D로 할당.
- **Tree_1~3**: 각각 **Raw Image** + **TreeInteractionNode** 스크립트. **Raycast Target** 체크로 클릭 수신.
- **EffectLayer**: 반짝이/팝업이 여기 자식으로 생성됨. 스크립트의 **Effect Parent**에 연결.

---

## 2. 씬 생성 및 오브젝트 설정

### Canvas & 배경
1. **File → New Scene** (또는 Basic 2D).
2. **우클릭 → UI → Canvas**. Canvas 생성 시 EventSystem 자동 생성됨.
3. Canvas 선택 → **우클릭 → UI → Raw Image** → 이름을 **BgImage**로 변경.
4. BgImage: **RectTransform**  
   - Anchor: Stretch (우측 상단 stretch 후 Alt+Shift 클릭)  
   - Left=0, Right=0, Top=0, Bottom=0  
   - **Texture**: 배경용 PNG를 프로젝트에 넣은 뒤, 그 에셋을 그대로 할당. (PNG는 **Texture Type = Default**로 두면 Texture2D로 인식됨.)

### 트리 컨테이너 & 나무 노드 3개
5. Canvas 우클릭 → **Create Empty** → 이름 **TreeContainer**.
6. TreeContainer 우클릭 → **UI → Raw Image** → 이름 **Tree_1**.
   - RectTransform: 원하는 크기 (예: Width 120, Height 160), 원하는 위치에 배치.
   - **Raw Image**: Texture에 "풀트리" PNG(Texture2D) 할당. (PNG 임포트는 기본 Default 유지.)
   - **Raycast Target**: 체크 유지 (클릭 받기 위해).
7. **Tree_1**에 **TreeInteractionNode** 스크립트 추가 (Add Component).
8. **Tree_2**, **Tree_3**도 동일하게 만들거나 Tree_1을 복제 후 위치만 변경.

### 이펙트 레이어
9. Canvas 우클릭 → **Create Empty** → 이름 **EffectLayer**.  
   - RectTransform은 Stretch 전체 화면이어도 되고, 기본값이어도 됨.

---

## 3. Inspector에서 연결할 것 (TreeInteractionNode)

각 **Tree_1, Tree_2, Tree_3**의 **Tree Interaction Node (Script)**에서:

| 필드 | 할당 |
|------|------|
| **State Full** | 풀트리 PNG(Texture2D). Project에서 PNG 선택 시 Texture Type이 Default면 그대로 드래그 가능. |
| **State Damaged** | 훼손 상태 PNG(Texture2D). |
| **State Stump** | 밑둥 상태 PNG(Texture2D). |
| **Sparkle Texture** | (선택) 반짝이용 텍스처. 비우면 런타임에 원형으로 생성. |
| **Effect Parent** | **EffectLayer**의 RectTransform (드래그로 연결) |
| **Popup Font** | (선택) TextMeshPro 폰트 에셋. 비우면 기본 UI Text 사용 |
| **Sparkle Color** | 반짝이 색 (기본 흰색) |
| **Sparkle Duration** | 반짝이 지속 시간 (기본 0.4초) |
| **Popup Duration** | +1 팝업 지속 시간 (기본 0.8초) |
| **Popup Move Y** | 팝업이 위로 올라가는 거리 (픽셀, 기본 60) |

**필수**: State Full / State Damaged / State Stump(각 PNG), **Effect Parent** (EffectLayer).

---

## 4. 스크립트 위치

- `Assets/Scripts/UI/TreeInteractionNode.cs`

**Raw Image + Texture2D** 기준으로 동작합니다.  
- **클릭** → 상태가 풀트리 → 훼손 → 밑둥 → 풀트리 … 순환.  
- 클릭 시 **반짝이** 이펙트가 나무 위치에 잠깐 표시되고 사라짐.  
- **+1** 텍스트가 위로 올라가며 페이드 아웃 후 제거.

---

## 5. 상태 이미지 3종 준비 (PNG 그대로 사용)

- **풀트리**: 온전한 나무 PNG.
- **훼손**: 일부 잘리거나 손상된 나무 PNG.
- **밑둥**: 나무 밑둥만 PNG.

프로젝트에 PNG를 넣고 **Texture Type은 Default(또는 Texture2D)**로 두면 됩니다. Sprite로 바꿀 필요 없음.

---

## 6. TextMeshPro 사용 시

- **Window → TextMeshPro → Import TMP Essential Resources** (최초 1회).
- **Popup Font**에 **LiberationSans SDF** 등 TMP 폰트 에셋을 넣으면 "+1"이 TMP로 표시됩니다.
- 비워 두면 Unity UI **Text**로 표시됩니다.

---

## 7. 테스트 순서

1. 배경 PNG 1장, 나무 상태 PNG 3장을 프로젝트에 넣기 (임포트 설정은 Default 유지).
2. 위 하이어라키대로 Canvas / BgImage(Raw Image) / TreeContainer / Tree_1~3(Raw Image) / EffectLayer 생성.
3. Tree_1~3에 **TreeInteractionNode** 붙이고, Inspector에서 상태 텍스처 3개(PNG) + Effect Parent 연결.
4. Play → 나무를 클릭하면 상태 변경 + 반짝이 + "+1" 팝업 확인.

리젠, 저장, 복잡한 애니메이션은 이 샘플에 포함하지 않았습니다. "이미지 기반 클릭만으로도 손맛이 나는지" 빠르게 검증하는 용도입니다.
