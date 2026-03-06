# Stage_01_Woodland 테스트 셋업 동작 확인 리포트 02

**목적**: `Stage01_TestScene_Setup_Guide.md` 및 `Stage01_TestScene_Report.md` 기반으로 실제 플레이하여 검증한 결과를 정리. GPT 등 다음 작업자 전달용.

**기준 씬**: `Assets/Scenes/Stages/Stage_01_Woodland.unity`  
**셋업 방법**: Unity 메뉴 **WoodLand3D > Setup Stage_01_Woodland Test Scene** 실행 후 재생.

---

## 1. 잘 된 것 (확인 완료)

| 항목 | 내용 |
|------|------|
| 그리드 | 10x10 타일 런타임 생성, 시작 타일 (5,5) 해제됨. |
| 자원 스폰 | 해제된 타일에서 나무(Tree) 3개, 돌(Rock) 2개 스폰됨. |
| 타일 해제 저장/로드 | 해제한 타일은 PlayerPrefs `TileUnlockData`에 저장됨. 게임 끄고 다시 켜도 해제된 타일은 그대로 유지됨. |
| Unlock UI | 플레이어에 Rigidbody 추가(아래 수정 사항 참고) 후, 잠긴 타일 트리거 진입 시 Unlock 패널 정상 표시. |
| 인벤토리/UI (플레이 중) | 채집 시 ResourceHUD 카운터 갱신, 플로팅 텍스트 등 의도한 대로 동작. |

---

## 2. 잘 안 됐던 것 + 원인 + 수정 내용

### 2.1 E 키 채집이 안 됨 (반응 없음)

- **증상**: 나무/돌 정면에서 E 키 연타해도 채집 안 됨, HUD 0,0,0 유지.
- **원인**  
  - `PlayerHarvestTest` 레이캐스트 시작점: `transform.position + Vector3.up * 0.5f` (+ 이후 수정으로 `+ transform.forward * 0.3f`).  
  - 플레이어 기본 위치 Y=1 이면 레이가 Y=1.5 근처에서 나감.  
  - placeholder 나무는 타일(Y=0) 기준으로 2칸 높이여서 콜라이더가 대략 Y -1~1 구간에 있음.  
  - 따라서 레이가 나무 상단보다 위로 지나가서 **히트하지 않음** (소스 버그가 아니라 레이 높이와 리소스 높이 불일치).
- **수정**  
  - 레이 시작점을 앞으로 뺌: `origin = transform.position + Vector3.up * 0.5f + transform.forward * 0.3f` (`PlayerHarvestTest.cs`).  
  - 추가로 **플레이어 Transform 위치 Y를 조금 낮추니** 채집이 됨. (레이가 리소스 콜라이더 안으로 들어가서 히트.)
- **정리**: 소스 로직은 의도대로임. 플레이어 높이/리소스 높이 설정(게임적 조정) 이슈이며, 나중에 범위 공격·최종 캐릭터 높이 정리할 때 같이 맞추면 됨.

### 2.2 Unlock UI가 안 뜸

- **증상**: 잠긴 타일로 가도 Unlock 패널이 안 나옴. 게임 시작 전에는 버튼/글자 보이다가 재생 시작하면 안 보임.
- **원인**  
  - Unity에서 `OnTriggerEnter`는 **두 콜라이더 중 최소 하나에 Rigidbody가 있어야** 호출됨.  
  - 플레이어: Collider만 있음. 타일: BoxCollider(isTrigger)만 있음 → **트리거 이벤트 자체가 발생하지 않음.**  
  - 시작 시 안 보이는 것은 `UnlockPanelUI` Awake에서 `rootCanvas.enabled = false` 로 숨기는 것이 정상 동작임.
- **수정**  
  - **Player** 오브젝트에 **Rigidbody** 추가.  
  - 이동을 transform으로만 하면 **Is Kinematic** 체크해서 트리거만 받도록 함 (수동 적용).

### 2.3 시작 시 해제 타일이 1개가 아니라 6개

- **증상**: 처음 기대는 시작 타일 1개만 해제인데, 재생 시 6칸이 이미 해제된 상태.
- **원인**  
  - 이전 플레이에서 해제한 타일이 **PlayerPrefs** 키 `TileUnlockData`에 저장되어 있음.  
  - 재실행 시 `TileUnlockSystem`이 해당 키를 로드해 그대로 적용함.
- **수정**  
  - 초기화가 필요하면: Unity **Edit → Clear All** (PlayerPrefs 전체 삭제) 또는 `PlayerPrefs.DeleteKey("TileUnlockData")` 로 삭제.

### 2.4 리소스(나무/돌) 콜라이더를 트리거로 켜야 하나?

- **결론**: **아니오. 트리거면 안 됨.**  
  - 채집 레이가 `QueryTriggerInteraction.Ignore` 로 동작함.  
  - 리소스 BoxCollider를 **Is Trigger = true** 로 두면 레이가 통과해서 채집이 안 됨.  
  - **Is Trigger = false** 유지가 맞음 (현재 리소스 프리팹 설정 유지).

---

## 3. 소스/설계상 그렇게 되어 있는 것 (버그 아님)

| 항목 | 설명 |
|------|------|
| 채집량 초기화 | 게임 끄고 다시 켜면 나무/돌 개수가 0,0으로 돌아감. **인벤토리(골드·자원)를 저장/로드하는 코드가 현재 없음.** 타일 해제만 PlayerPrefs에 저장됨. 따라서 “저장이 안 돼서 초기화되는 것”이 맞고, “저장하려 했는데 버그”가 아님. 채집량 유지하려면 별도 인벤토리 저장/로드 구현 필요. |
| 레이 vs 리소스 높이 | 레이 높이와 placeholder 리소스 높이 불일치는 **설정/배치 이슈**이며, 코드 버그가 아님. |

---

## 4. 아직 수정/구현 필요한 것

| 우선순위 | 항목 | 내용 |
|----------|------|------|
| (선택) | 인벤토리 저장/로드 | 골드·나무·돌·광석을 세션 간 유지하려면 PlayerPrefs 또는 세이브 파일 등으로 저장/로드 추가 필요. 현재는 미구현. |
| (나중) | 플레이어 높이 vs 레이 | 최종 캐릭터/리소스 높이 정해지면, 플레이어 Y 위치 및 레이 오프셋(또는 범위 공격 로직) 재조정. |
| (나중) | 아트/프리팹 교체 | `Stage01_TestScene_Report.md` 섹션 8 참고: 타일·자원·잠금 클라우드·플레이어·UI·ResourcePickupVFX 등 placeholder를 실제 리소스로 교체. |

---

## 5. 수정된 파일 (이번 검증 과정에서)

- **`Assets/Scripts/Gameplay/Interaction/PlayerHarvestTest.cs`**  
  - 레이 시작점: `transform.position + Vector3.up * 0.5f` → `+ transform.forward * 0.3f` 추가.  
  - (플레이어 Y 위치는 씬/프리팹에서 수동 조정.)

---

## 6. GPT/다음 작업자에게 전달 요약

- **동작하는 것**: 그리드, 시작 타일 해제, 자원 스폰, 타일 해제 저장/로드, Unlock UI(Rigidbody 추가 후), 채집 시 HUD/플로팅 텍스트.
- **수동으로 한 수정**: Player에 Rigidbody(필요 시 Kinematic), 플레이어 Y 위치 약간 하향 조정.
- **코드 수정**: `PlayerHarvestTest` 레이 origin에 `forward * 0.3f` 추가.
- **의도된 동작**: 리소스 콜라이더는 non-trigger 유지, 인벤토리는 현재 미저장(재시작 시 0으로 초기화).
- **추가로 할 일**: 인벤토리 저장 원하면 별도 구현, 이후 레이/범위 공격·아트 교체 시 높이/비주얼 정리.

---

**참고 문서**  
- 셋업 절차: `Stage01_TestScene_Setup_Guide.md`  
- 초기 리포트(계층/참조/권장 사항): `Stage01_TestScene_Report.md`
