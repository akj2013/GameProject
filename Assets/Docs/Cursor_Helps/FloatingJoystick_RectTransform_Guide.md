# FloatingJoystick UI — RectTransform 설정 가이드

Unity에서 **Anchor(앵커)** 설정에 따라 Inspector에 보이는 필드가 달라집니다.  
이 문서는 **stretch / center** 차이와 **Left·Top·Right·Bottom** vs **Pos·Width·Height** 관계를 정리한 뒤,  
FloatingJoystickRoot와 그 자식들(JoystickBackground, JoystickHandle)의 **권장 Rect 설정**을 단계별로 설명합니다.

---

## 1. Anchor에 따라 Inspector에 나오는 필드

### 1) Anchor가 "한 점"일 때 (Center 등)

- **Anchor Min = Max** (예: Min 0.5, 0.5 / Max 0.5, 0.5)  
  → 앵커가 부모 안의 **한 점**에 고정된 상태입니다.
- Inspector에는 다음이 나옵니다:
  - **Pos X, Pos Y** (그리고 Pos Z)  
    → 스크립트의 `RectTransform.anchoredPosition` 에 대응합니다.  
    앵커 점으로부터 **피벗이 얼마나 떨어져 있는지** 오프셋입니다.
  - **Width, Height**  
    → 스크립트의 `RectTransform.sizeDelta` (x, y)에 대응합니다.  
    앵커가 한 점일 때는 **요소의 가로·세로 크기**를 직접 지정하는 값입니다.
- **Left, Top, Right, Bottom** 은 이 모드에서는 **나오지 않습니다**.

### 2) Anchor가 "늘어남(Stretch)"일 때

- **Anchor Min ≠ Max** (예: Min 0,0 / Max 1,1)  
  → 부모의 **영역에 맞춰 늘어나는** 모드입니다.
- Inspector에는 다음이 나옵니다:
  - **Left, Top, Right, Bottom**  
    → 부모 Rect의 **왼쪽/위/오른쪽/아래 가장자리**로부터,  
    이 UI 요소의 **해당 가장자리까지의 거리(픽셀)** 입니다.  
    즉, “여백”처럼 쓰입니다.
  - 이 모드에서 실제 크기와 위치는  
    부모 크기와 Left/Top/Right/Bottom 값으로 **계산**되며,  
    내부적으로는 여전히 `anchoredPosition`, `sizeDelta` 등으로 표현됩니다.
- **Pos X, Pos Y, Width, Height** 는 Inspector에서 **숨겨지고**,  
  대신 **Left, Top, Right, Bottom** 만 보이게 됩니다.

정리:

| Anchor 형태       | Inspector에 보이는 것   | 스크립트에서 주로 다루는 것   |
|-------------------|-------------------------|-------------------------------|
| 한 점 (Center 등) | Pos X, Pos Y, Width, Height | `anchoredPosition`, `sizeDelta` |
| Stretch           | Left, Top, Right, Bottom     | (같은 속성이지만 값이 자동 계산됨) |

---

## 2. FloatingJoystickRoot — 권장 설정

**역할:**  
화면 **전체**를 덮는 투명 터치 영역. 여기 붙은 `FloatingJoystickUI`가 터치/드래그를 받습니다.  
“조이스틱이 나타날 위치”는 스크립트가 **JoystickBackground** 의 `anchoredPosition`으로 옮기므로,  
Root는 “화면 전체 한 장”으로만 쓰면 됩니다.

- **Anchor**
  - **Stretch**  
    - Min X: 0, Min Y: 0  
    - Max X: 1, Max Y: 1  
  - 즉, 부모(Canvas)를 전부 채우는 형태입니다.
- **Inspector에 나오는 필드 (Stretch이므로)**
  - **Left: 0**
  - **Top: 0**
  - **Right: 0**
  - **Bottom: 0**
  - 이렇게 하면 여백 없이 **Canvas와 같은 크기**가 됩니다.
- **Pivot:** 0.5, 0.5 (가운데) — 크게 상관 없음.
- **참고:**  
  지금처럼 Left 350, Right 350, Top 175, Bottom 175 로 두면  
  화면 중앙 일부만 터치 영역이 됩니다.  
  “아무 곳이나 눌러도 조이스틱이 뜨게” 하려면 **0, 0, 0, 0** 으로 두는 것이 좋습니다.

---

## 3. JoystickBackground — 권장 설정

**역할:**  
조이스틱 **배경 원**이 그려지는 오브젝트.  
스크립트가 여기의 `anchoredPosition`을 터치 위치로 바꿔서 “그 위치에 조이스틱이 생긴다”고 보면 됩니다.

- **Anchor**
  - **Center (한 점)**  
    - Min X: 0.5, Min Y: 0.5  
    - Max X: 0.5, Max Y: 0.5  
  - 부모(FloatingJoystickRoot)의 **중심**을 기준점으로 두는 형태입니다.
- **Inspector에 나오는 필드 (한 점이므로)**
  - **Pos X: 0, Pos Y: 0, Pos Z: 0**  
    → 에디터에서의 기본 위치.  
    런타임에는 스크립트가 `joystickRoot.anchoredPosition = (터치 좌표)` 로 덮어씁니다.
  - **Width: 160, Height: 160**  
    → 배경 원의 크기. 원 스프라이트를 쓰면 이 크기의 정사각형 안에 원이 들어갑니다.
- **Pivot:** 0.5, 0.5  
  → 조이스틱이 **중심** 기준으로 배치되도록 합니다.

---

## 4. JoystickHandle — 권장 설정

**역할:**  
손가락/마우스로 드래그할 **핸들(작은 원)**.  
스크립트가 `handle.anchoredPosition`을 **JoystickBackground 기준 로컬 좌표**로 계속 갱신합니다.  
그래서 Handle의 기준점(앵커·피벗)은 **배경의 중심**에 맞춰 두는 것이 맞습니다.

- **Anchor**
  - **Center (한 점)**  
    - Min X: 0.5, Min Y: 0.5  
    - Max X: 0.5, Max Y: 0.5  
  - 부모(JoystickBackground)의 **중심**에 앵커를 둡니다.
- **Inspector에 나오는 필드 (한 점이므로)**
  - **Pos X: 0, Pos Y: 0, Pos Z: 0**  
    → 처음에는 배경 한가운데.  
    드래그 시 스크립트가 이 값을 반지름 안에서 (x, y) 로 바꿉니다.
  - **Width: 80, Height: 80**  
    → 핸들 크기. 배경보다 작게 두면 됩니다.
- **Pivot:** 0.5, 0.5  
  → 핸들 자신도 중심 기준이라, 배경 중심과 겹쳐서 “중심에서 밀어내는” 느낌이 됩니다.

---

## 5. 한눈에 보는 권장값 요약

| 오브젝트            | Anchor (Min / Max) | Inspector 필드 (권장)              | 비고 |
|---------------------|--------------------|------------------------------------|------|
| FloatingJoystickRoot| 0,0 / 1,1 (Stretch)| Left 0, Top 0, Right 0, Bottom 0  | 화면 전체 터치 영역 |
| JoystickBackground  | 0.5,0.5 / 0.5,0.5 (Center) | Pos 0,0, Width 160, Height 160 | 위치는 스크립트가 터치 위치로 설정 |
| JoystickHandle      | 0.5,0.5 / 0.5,0.5 (Center) | Pos 0,0, Width 80, Height 80   | 위치는 스크립트가 드래그에 따라 설정 |

---

## 6. 스크립트와의 대응

- `FloatingJoystickUI` 는  
  - **joystickRoot** = JoystickBackground 의 `RectTransform`  
  - **handle** = JoystickHandle 의 `RectTransform`  
  을 참조합니다.
- 터치 시:  
  `joystickRoot.anchoredPosition = (Canvas 기준 터치 위치의 로컬 좌표)`  
  → 그래서 JoystickBackground는 **Center + Pos 0,0** 이면,  
  터치한 점이 배경의 중심이 됩니다.
- 드래그 시:  
  `handle.anchoredPosition = (JoystickBackground 기준 로컬 좌표, 반지름 안으로 클램프)`  
  → JoystickHandle은 **부모 기준 Center, Pivot Center** 이어야  
  “배경 중심에서 밀어내는” 좌표계와 맞습니다.

---

## 7. 문제 해결

- **조이스틱이 화면 일부에서만 반응함**  
  → FloatingJoystickRoot 가 Stretch인데 Left/Top/Right/Bottom 이 0이 아님.  
  → **0, 0, 0, 0** 으로 바꿔서 화면 전체가 터치 영역인지 확인.
- **핸들이 배경 밖으로 나가거나 비뚤게 움직임**  
  → JoystickHandle 의 부모가 **JoystickBackground** 인지,  
  Anchor/Pivot 이 **Center(0.5, 0.5)** 인지 확인.  
  → `FloatingJoystickUI` 의 **Radius** 값을 JoystickBackground 크기(예: 160)의 절반(80) 이하로 두면, 핸들이 배경 안에 들어갑니다.

이렇게 설정하면 FloatingJoystickRoot와 그 자식들의 Rect를 stretch/center와 Left·Top·Right·Bottom / Pos·Width·Height 관계까지 포함해 일관되게 맞출 수 있습니다.
