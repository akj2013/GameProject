# Auto Harvest System – Design & Deliverables

**목표**: 수동 도구 전환 없이, 인근 자원에 따라 도구를 자동 표시하고 자동 채집한다.  
**입력**: 이동만 (조이스틱). E키/도구 버튼 없음.

---

## 1. Added scripts

| 경로 | 역할 |
|------|------|
| `Assets/Scripts/Resources/Types/ResourceType.cs` | 자원 종류 enum (Tree, Rock, Ore, Rice, Potato, SweetPotato) |
| `Assets/Scripts/Resources/Types/ToolType.cs` | 도구 종류 enum (None, Axe, Pickaxe, Sickle, Hoe) |
| `Assets/Scripts/Resources/Types/ResourceToolMapping.cs` | ResourceType → ToolType 정적 매핑 |
| `Assets/Scripts/Resources/Nodes/ResourceNode.cs` | 자원 내구도, ApplyDamage, 고갈 시 보상·이벤트 |
| `Assets/Scripts/Gameplay/Inventory/PlayerInventory.cs` | 골드·자원 보유, AddResource (기존 플로우 호환) |
| `Assets/Scripts/Gameplay/Events/ResourceGainedEvents.cs` | 자원 획득 이벤트 허브 (HUD/VFX 구독) |
| `Assets/Scripts/Gameplay/Interaction/PlayerAutoHarvest.cs` | 반경 내 가장 가까운 자원 기준 도구 결정 + 같은 도구 필요 자원 일괄 채집 + 툴 표시 |

---

## 2. Updated / existing scripts

| 스크립트 | 비고 |
|----------|------|
| `PlayerHarvestTest.cs` | E키 수동 채집용 유지. 자동 채집 사용 시 플레이어에서 제거하거나 비활성화하고 `PlayerAutoHarvest`만 사용. |
| `ResourceNode` | 기존 프로젝트에 이미 있다면 해당 버전 사용. 없을 때만 이번에 추가한 `Resources/Nodes/ResourceNode.cs` 사용. |
| `PlayerInventory` / `ResourceGainedEvents` | 동일. 기존 구현이 있으면 그대로 두고, 없을 때만 이번에 추가한 스크립트 사용. |

---

## 3. Closest-resource selection (가장 가까운 자원 선택)

- **입력**: 매 `harvestInterval`(기본 0.4초)마다 `harvestRadius` 내의 **유효한** `ResourceNode`만 수집.
- **유효**: `node.IsValid == true` (미고갈, `_currentHp > 0`).
- **가장 가까운 선택**:  
  `GatherValidNodesInRange()`로 리스트를 만든 뒤, 플레이어 `transform.position` 기준으로 **거리 제곱(sqrMagnitude)**이 가장 작은 노드 하나를 선택.
- **도구 결정**: 그 한 노드의 `Type`으로 `ResourceToolMapping.GetToolForResource(type)`를 호출해 이번 틱의 `ToolType`을 정한다.
- **예**: 나무와 돌이 둘 다 반경 안에 있고, **가장 가까운 것**이 나무면 → 이번 틱은 **Axe**로만 동작.

---

## 4. Tool visibility (툴 표시)

- **규칙**  
  - 유효한 자원이 **하나도 없으면** → `ToolType.None` → **모든 툴 비표시**.  
  - 유효한 자원이 **하나라도 있으면** → 위에서 정한 **가장 가까운 자원**에 맞는 도구 하나만 표시.
- **매핑**  
  - Tree → Axe  
  - Rock, Ore → Pickaxe  
  - Rice → Sickle  
  - Potato, SweetPotato → Hoe  
- **구현**: `PlayerAutoHarvest`의 `SetToolVisibility(ToolType)`에서  
  `toolAxe`, `toolPickaxe`, `toolSickle`, `toolHoe` 중 해당 하나만 `SetActive(true)`, 나머지는 `SetActive(false)`.  
  `ToolType.None`이면 네 개 모두 비활성.
- **플레이스홀더**: 툴은 각각 빈 오브젝트나 간단한 메시로 두고, Inspector에서 `PlayerAutoHarvest`의 Axe/Pickaxe/Sickle/Hoe 필드에 할당.

---

## 5. Multi-target harvesting (한 틱에 여러 자원 채집)

- **한 틱 동작**  
  1. 반경 내 유효한 `ResourceNode` 목록 수집.  
  2. 그중 **가장 가까운** 노드로 이번 틱의 `ToolType` 결정.  
  3. **같은 ToolType**이 필요한 노드만 필터링.  
  4. 그 노드들에 대해 각각 `ApplyDamage(damagePerTick)` 호출.
- **다른 도구 자원은 무시**  
  - 이번 틱에 “가장 가까운 자원 = 나무”면 Axe만 사용 → **나무만** 피해 적용, 돌/광석은 이번 틱에서 건드리지 않음.  
  - 나무가 다 없어지면 다음 틱에 다시 수집·가장 가까운 선택 → 그때 돌이 가장 가까우면 Pickaxe로 전환되어 돌만 채집.
- **예시**  
  - 반경 안에 나무 2, 돌 1 → 가장 가까운 게 나무 → Axe 표시, 나무 2개만 `ApplyDamage`, 돌 1개는 무시.  
  - 다음 틱에서 나무가 없어졌으면 → 가장 가까운 건 돌 → Pickaxe 표시, 돌만 채집.

---

## 6. Test checklist (mixed Tree/Rock range)

플레이 모드에서 아래를 확인하면 된다.

| # | 확인 항목 | 기대 결과 |
|---|-----------|-----------|
| 1 | **나무만 반경 안** | 도구는 도끼만 보임. 나무만 피해 들어가고, 고갈 시 인벤토리·HUD·플로팅 텍스트/VFX 정상. |
| 2 | **돌만 반경 안** | 곡괭이만 보임. 돌만 채집, 보상·이벤트 정상. |
| 3 | **나무와 돌 동시에 반경 안, 나무가 더 가까움** | 도끼만 보임. 나무만 피해, 돌은 그 틱에 무시. 나무 없어지면 다음 틱부터 곡괭이로 돌만 채집. |
| 4 | **나무와 돌 동시에 반경 안, 돌이 더 가까움** | 곡괭이만 보임. 돌만 피해, 나무는 그 틱에 무시. |
| 5 | **자원이 반경 밖** | 어떤 도구도 보이지 않음. 채집 없음. |
| 6 | **기존 보상 플로우** | ResourceNode 고갈 → PlayerInventory 증가, ResourceGainedEvents 발동 → HUD/플로팅 텍스트/ResourcePickupVFX 등 기존대로 동작. |

---

## 7. Scene setup (요약)

- **Player**  
  - `PlayerAutoHarvest` 추가.  
  - `Harvest Radius` (예: 2.5), `Damage Per Tick`, `Harvest Interval` 설정.  
  - 툴 플레이스홀더 4개(Axe, Pickaxe, Sickle, Hoe)를 자식으로 두고 각각 `Tool Axe` … `Tool Hoe`에 할당.  
  - 자동 채집만 쓸 경우 `PlayerHarvestTest`는 제거하거나 비활성화.
- **자원**  
  - 각 자원 오브젝트에 `ResourceNode` (Type, Max Hp, Reward Amount) 설정.  
  - 기존처럼 Collider는 non-trigger 유지 (레이/오버랩 정책에 맞춤).
- **이벤트/인벤토리**  
  - 씬에 `PlayerInventory`(플레이어에), `ResourceGainedEvents`(씬에 1개) 있으면 보상·HUD·VFX 연동 유지.

---

## 8. Design rules 요약

- **ToolType** 유지: None, Axe, Pickaxe, Sickle, Hoe.  
- **수동 도구 전환 버튼 없음.**  
- **평상시**: 도구 안 보임. **유효 자원이 반경 안에 있을 때만** 해당 도구 하나 표시.  
- **자동 채집**: 반경 내 가장 가까운 유효 자원으로 도구 결정 → 같은 도구 필요한 자원만 그 틱에 피해 적용.  
- **기존 보상 플로우** 유지: ResourceNode 고갈 → PlayerInventory + ResourceGainedEvents → HUD / 플로팅 텍스트 / VFX.
