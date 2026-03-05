# DropItemPanel 설정 가이드

채집 시 나무 위치에 "아이콘 + x2" 팝업이 떠오르도록 하는 UI 패널입니다. **DropItemPanelManager**가 제어하며, **DropItem**을 동적으로 생성해 여러 나무를 동시에 채집해도 각각 독립적으로 표시됩니다.

## 1. 씬 구성

- **Canvas** (Screen Space - Overlay 또는 Screen Space - Camera 권장)
  - **DropItemPanel** (빈 GameObject 또는 Panel)
    - 여기에 **DropItemPanelManager** 스크립트를 붙입니다.
    - DropItem 프리팹 인스턴스들이 런타임에 이 Transform 아래에 생성됩니다.

## 2. DropItemPanelManager 설정

| 필드 | 설명 |
|------|------|
| **Drop Item Prefab** | Instantiate할 DropItem 프리팹. 아래 구조를 만든 뒤 프리팹으로 저장해 할당합니다. |
| **Float Duration** | 팝업이 위로 떠오르는 시간(초). 기본 1.2 |
| **Float Height** | 위로 올라가는 높이(픽셀). 기본 80 (스크린 스페이스 기준) |
| **Fade Start Normalized** | 페이드 아웃 시작 시점 0~1. 기본 0.4 |
| **Fade Duration** | 페이드 아웃 시간(초). 기본 0.5 |

## 3. DropItem 프리팹 구조

프리팹 루트에 **RectTransform**이 있어야 하고, 아래 자식 구성을 권장합니다.

```
DropItem (프리팹 루트)
├── RectTransform (Anchor: Center-Center, 크기 예: 80x80)
├── RawImage (아이콘용, 텍스처는 런타임에 설정됨)
└── Text_Drop (TMP_Text, "x2" 등 개수 표시)
```

- **루트**: RectTransform만 있으면 됨. Anchor/Pivot은 Center–Center 권장.
- **RawImage**: 자식 중 하나에 RawImage가 있으면 그 텍스처를 나무 아이콘으로 채웁니다.
- **TMP_Text**: 자식 중 TMP_Text가 있으면 "x" + 개수로 설정됩니다. 이름은 달라도 됨.

프리팹을 만든 뒤 **DropItemPanelManager**의 **Drop Item Prefab** 슬롯에 할당합니다.

## 4. TreeManager 연동

- TreeManager에는 **Drop Popup** 헤더 아래에 **Enable Drop Popup**, **Drop Item Panel** 필드만 있습니다.
- **Drop Item Panel**을 비워 두면 **DropItemPanelManager.Instance**를 사용합니다 (씬에 하나만 있으면 자동 연결).
- 여러 패널을 쓰려면 나무별로 원하는 **DropItemPanelManager**를 할당하면 됩니다.

## 5. 동작 요약

- 나무가 쓰러질 때 `TreeManager`가 `DropItemPanelManager.ShowDrop(월드위치, 아이콘텍스처, 개수)`를 호출합니다.
- 매 호출마다 DropItem 프리팹이 **DropItemPanel** 자식으로 하나씩 생성되고, 해당 위치에 표시된 뒤 위로 떠오르며 페이드 아웃 후 제거됩니다.
- 여러 나무를 연속으로 채집하면 각각 별도 DropItem이 생성되어 동시에 떠오릅니다.
