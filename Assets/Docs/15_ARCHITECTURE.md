# 15_ARCHITECTURE.md

이 문서는 프로젝트의 전체 아키텍처(구조)를 정리한다.  
목표는 다음과 같다.

- 기능 추가/수정 시 어디를 건드려야 하는지 빠르게 찾기
- 스크립트가 20개를 넘어가도 혼란이 없도록 역할을 명확히 하기
- AI 협업(Cursor 등)에서 “무엇을 어디에 추가/수정할지” 지시를 명확히 하기
- 스크립트가 과도하게 커지지 않도록(500라인 제한) 초기에 분리 기준을 정의하기


---

## 1. 핵심 설계 원칙

### 1.1 단일 책임 원칙 (Single Responsibility)
- 하나의 스크립트는 “한 가지 역할”만 가진다.
- 기능이 커지면 스크립트를 분리한다.
- 한 스크립트가 500라인을 넘어가면 분리 대상이다.

### 1.2 데이터 흐름은 이벤트 중심
- “자원 획득”, “타일 언락” 같은 핵심 사건은 이벤트로 흘린다.
- UI/VFX/사운드가 게임 로직을 직접 참조하지 않도록 한다.
- 예: ResourceGainedEvent → HUD/플로팅 텍스트/VFX가 구독하여 반응

### 1.3 인스펙터 친화 설계
- 모든 SerializeField에는 Tooltip을 붙인다.
- Header/Space 등을 활용해 인스펙터 그룹을 명확히 한다.
- 널 참조가 발생할 가능성이 있는 부분은 실행 시 경고 로그 + 안전하게 무시한다.

### 1.4 “동작 우선” → “확장”
- 프로토타입 단계에서는 단순하게 동작하게 만든다.
- 이후 기능별로 강화(폴리싱/최적화/데이터화)한다.


---

## 2. 현재 구현된 게임 루프(Core Loop)

현재 프로토타입에서 동작하는 루프는 다음과 같다.

1) 그리드 생성(10x10)  
2) 시작 타일 1개만 언락 상태  
3) 언락된 타일에 자원 스폰  
4) 플레이어가 E키로 자원 채집  
5) 자원 고갈 → 사라짐 → 일정 시간 후 리젠  
6) 잠긴 타일 근처 접근 → 언락 UI 표시  
7) 언락 버튼 → 구름 제거 → 타일 언락 → 자원 스폰  
8) 언락 상태는 저장되며 재실행해도 유지됨(현재는 PlayerPrefs)

이 문서는 위 루프가 어떤 스크립트/시스템으로 구성되는지를 설명한다.


---

## 3. 폴더 구조 제안(권장)

현재 Assets/Scripts 구조가 확장될 것을 고려하여, 아래처럼 기능별 폴더로 정리하는 것을 권장한다.  
(파일/폴더명은 영어로 유지)

- Assets/Scripts
  - Core
    - Events
    - Save
    - Utilities
  - Gameplay
    - Player
    - Interaction
  - Tiles
    - Grid
    - Unlock
  - Resources
    - Nodes
    - Spawn
    - Types
  - UI
    - HUD
    - Panels
    - WorldUI
  - CameraSystems
  - NPC
  - Economy

※ “현재 존재하는 폴더/네임스페이스”를 최대한 유지하면서, 점진적으로 이동한다.


---

## 4. 시스템별 설명

### 4.1 Tiles System (Grid / Tile / Unlock)

#### 목적
- 월드가 타일 단위로 구성된다.
- 잠긴 타일은 구름으로 가려지고, 언락하면 해제된다.
- 인접 타일 규칙(상하좌우)을 기반으로 확장된다.

#### 주요 컴포넌트(예시)
- SquareGridManager
  - 그리드를 생성한다.
  - 타일 좌표(x,y) ↔ 타일 오브젝트를 관리/조회한다.
  - 인접 타일 조회(Up/Down/Left/Right)를 제공한다.
- TileController
  - 타일 하나의 상태(locked/unlocked)를 관리한다.
  - 구름 비주얼 On/Off, 하이라이트(스케일 업) 등을 처리한다.
  - 플레이어 트리거를 감지하여 언락 UI 요청을 보낸다.
- TileUnlockSystem
  - 언락 규칙(인접 타일 필요 등)을 판단한다.
  - 비용 계산/결제(현재는 Gold 기반)를 수행한다.
  - UI를 열고 닫는 흐름을 제어한다.
  - 언락 데이터를 저장/로드한다.

#### 데이터
- 타일 크기: 10
- 그리드 크기: 10x10
- 언락 데이터: 좌표 목록(예: (5,5), (5,6) …)

#### 주의사항
- 저장 로드시 “이미 언락된 타일에 자원이 중복 스폰”되지 않도록 보호 로직이 필요할 수 있다.
- TileController는 “시각/트리거” 중심, Unlock 로직은 TileUnlockSystem에 집중한다.


---

### 4.2 Resources System (Spawn / Harvest / Respawn)

#### 목적
- 언락된 타일 위에 자원이 생성된다(나무/바위/광석 등).
- 플레이어가 상호작용(E키)로 자원을 채집한다.
- 자원이 고갈되면 사라지고, 일정 시간 후 재생성된다.

#### 주요 컴포넌트(예시)
- TileResourceSpawner
  - 타일 언락 시 해당 타일에 자원을 스폰한다.
  - 스폰 포인트/랜덤 위치 배치 규칙을 가진다.
  - 자원 리젠(코루틴/타이머)을 관리한다.
- ResourceNode
  - 자원 오브젝트(나무/돌 등)의 “채집 가능 상태”를 관리한다.
  - HP/내구도 감소, 고갈 처리, 리젠 트리거를 제공한다.
- PlayerHarvest(또는 현재 사용 중인 PlayerHarvestTest)
  - 플레이어 입력(E키)을 받아 자원을 채집한다.
  - 범위 체크/레이캐스트/트리거 체크 등 “채집 행위”를 담당한다.

#### 현재 프로토타입에서 확인된 동작
- 타일 언락 직후 자원 생성
- 플레이어가 가까이 가서 E키로 채집
- 자원 제거 후 일정 시간 뒤 리젠

#### 향후 확장
- 자원 타입(ResourceType) 통일
- 자원 획득량/리젠 시간 데이터화(ScriptableObject)
- 자원 연출(VFX, SFX) 이벤트 기반 연결


---

### 4.3 Player System (Movement / Inventory / Interaction)

#### 목적
- 플레이어 이동과 상호작용(채집, 타일 언락 접근)을 담당한다.
- 인벤토리는 “캐주얼 특성”상 무제한이며 즉시 획득형으로 간다.

#### 주요 컴포넌트(예시)
- PlayerMovement / PlayerController
  - 플레이어 이동 처리
  - (현재는 고정 이동속도, 이후 이동수단은 별도 시스템으로)
- PlayerInventory
  - 현재: Gold 기반(언락 비용)
  - 확장: 자원 기반 무제한 인벤토리(Dictionary<ResourceType, int>)
  - 이벤트: OnGoldChanged, OnResourceChanged 등

#### 주의사항
- “드랍 아이템을 줍는 시스템” 대신 “즉시 획득 + HUD 반영 + 흡수 VFX” 방식으로 구현한다.


---

### 4.4 UI System (Unlock Panel / HUD / World Feedback)

#### 목적
- 언락 패널 UI를 제공한다.
- 현재 자원/재화 상태를 HUD로 보여준다.
- 채집 시 즉각 피드백(+3, 아이콘 흡수 등)을 제공한다.

#### 주요 컴포넌트(예시)
- UnlockPanelUI
  - 잠긴 타일 접근 시 표시
  - 비용/언락 버튼/닫기 버튼
- ResourceHUD
  - 상단/좌측 등 고정 위치에 자원 카운터 표시
  - 최근 획득 3~4개 리스트 표시
- FloatingTextWorld
  - 자원 채집 순간 “+3 Wood” 같은 텍스트를 월드 공간에 표시
- ResourcePickupVFX
  - 자원 주변에서 작은 아이콘/오브젝트가 튀어나와 플레이어에게 빨려 들어가는 연출

#### UI 원칙
- UI는 “로직을 직접 호출하지 않고 이벤트를 구독”해서 반응하도록 구성한다.


---

### 4.5 Save System

#### 목적
- 언락된 타일 상태가 재실행 후에도 유지된다.
- 프로토타입에서는 PlayerPrefs 기반으로 간단히 처리한다.

#### 현재 상태
- TileUnlockData가 PlayerPrefs로 저장되는 형태(레지스트리)

#### 향후 확장
- 저장 데이터가 커지면 JSON 파일(persistentDataPath) 방식으로 전환 고려
- Stage 단위 저장/로드(지역별 맵 분리) 설계 필요


---

### 4.6 Camera System

#### 목적
- 모바일 가로 화면(Landscape)에서 넓은 풍경을 보여주는 카메라
- 플레이어를 따라가되, 필요 시 타일/랜드마크 포커싱 가능

#### 주의사항
- UI를 닫은 뒤 카메라가 플레이어 추적 모드로 정상 복귀하는지 체크 필요
- 향후: 줌/바운드/핀치 줌 추가 예정


---

## 5. 이벤트 설계(권장)

프로젝트 확장 시 “UI/사운드/이펙트”와 “게임 로직” 분리를 위해 이벤트를 사용한다.

예시 이벤트
- OnTileUnlocked(tileCoord)
- OnResourceGained(resourceType, amount, worldPos)
- OnResourceDepleted(node)
- OnInventoryChanged(resourceType, newValue)

이벤트를 사용하면:
- 로직은 로직만 담당
- UI/VFX/SFX는 구독해서 연출만 담당


---

## 6. 스크립트 분리 기준(500라인 제한)

다음 기준에 해당하면 분리한다.

- 한 클래스가 서로 다른 시스템(Tiles + UI + Save 등)을 동시에 처리
- 입력 처리 + 데이터 처리 + UI 업데이트가 한 스크립트에 섞임
- 500라인을 넘거나 넘을 가능성이 높은 경우

분리 예시
- TileUnlockSystem
  - UnlockRuleEvaluator(규칙 판단)
  - TileUnlockSaveService(저장/로드)
  - TileUnlockUIController(UI 표시)


---

## 7. “Stage(지역별 맵)” 확장 대비 구조

월드는 “지역 단위 Stage”로 분리한다.
- Korea Stage
- Japan Stage
- China Stage
- USA Stage

Stage 시스템에서 필요한 것
- Stage별 자원 테이블
- Stage별 특산물(무역/교환)
- Stage별 언락 데이터 분리
- Stage 이동/복귀(1맵 회귀 플레이의 의미 유지)

Stage가 늘어나도 초기 자원이 죽지 않도록, 경제 시스템은 “물물교환 기반”으로 설계한다.
(자세한 내용은 12_ECONOMY_TRADE_SYSTEM.md 참고)


---

## 8. 현재 상태 체크리스트(프로토타입)

현재까지 구현되어 정상 동작하는 항목(사용자 확인 기준)
- 그리드 생성(10x10)
- 시작 타일 1개 언락
- 언락 타일 자원 스폰
- E키로 자원 채집
- 자원 제거/리젠
- 잠긴 타일 접근 시 언락 UI 표시
- 언락 버튼으로 구름 제거 + 자원 스폰
- 재실행 후 언락 상태 유지(세이브 정상)


---

## 9. 다음 구현(권장 우선순위)

1) 자원 즉시 획득 피드백 시스템
- ResourceGainedEvent
- FloatingTextWorld
- ResourcePickupVFX
- ResourceHUD
- PlayerInventory 무제한 자원 인벤토리

2) NPC Trader(물물교환)
- 고정 교환 비율
- 지역별 교환 테이블 확장

3) 농사 시스템(논/밭 + 농부 NPC)
- 기존 자원 시스템 재사용


---

## 10. 문서 업데이트 규칙

- 시스템이 추가되면 이 문서의 “시스템별 설명”에 반드시 반영한다.
- 스크립트가 분리되면 “폴더 구조/분리 기준”에 업데이트한다.
- 큰 변경을 하면 DEVLOG.md에도 기록한다.