# Player 애니메이터 연결 상태 점검 리포트

**목적**: Stage_01_Woodland 씬의 Player + ModelRoot + Animator 설정을 점검하고, Idle 한 번 재생 후 멈춤 / Walk·Run 미재생 원인을 정리. GPT 등 다음 작업자 전달용.

**기준 씬**: `Assets/Scenes/Stages/Stage_01_Woodland.unity`  
**기준 컨트롤러**: `Assets/Prefabs/Characters/Player/Animations/AC_Player_Base.controller`

---

## 1. 현재 씬/계층 상태

### 1.1 Player 계층

- **Player** (GameObject, Tag: Player)
  - **Animator**: 연결됨. Controller = `AC_Player_Base`, Avatar = `Meshy_AI_Character_output.fbx` (Humanoid)
  - **Apply Root Motion**: 켜짐 (1)
  - **Update Mode**: Normal (0)
  - **Culling Mode**: Cull Update Transforms (1)
  - 기타: PlayerMover, PlayerAutoHarvest, PlayerInventory, Rigidbody, CapsuleCollider 등
- **ModelRoot** (Player의 자식)
  - 자식: 캐릭터 메쉬/아머처 루트 (Armature 등)
- **Animator 컴포넌트 위치**: **Player** 오브젝트에 있음 (ModelRoot가 아님). ModelRoot는 Animator가 있는 Player의 자식이라, 루트 모션·본 애니메이션 모두 정상 적용 대상임.

### 1.2 정리

- Animator ↔ 모델 계층 구조는 올바름 (Player에 Animator, 그 아래 ModelRoot → 스킨/본).
- Avatar는 캐릭터 FBX(`Meshy_AI_Character_output.fbx`, guid: dce3e3ed5f290e74ca688fd5aaa41b8a)와 연결됨.

---

## 2. 애니메이터 컨트롤러(AC_Player_Base) 상태

### 2.1 파라미터

| 이름   | 타입   | 기본값 |
|--------|--------|--------|
| Speed  | Float  | 0      |
| Attack | Trigger| -      |
| Mine   | Trigger| -      |

### 2.2 레이어·기본 상태

- **Base Layer** 1개만 사용.
- **Default State**: Idle (진입 시 Idle부터 시작).

### 2.3 스테이트와 트랜지션 요약

- **Idle** (기본 상태)
  - 나가는 트랜지션: **1개**
  - 조건: **Speed > 0.1** → **대상: Idle (자기 자신)**
  - 즉, Idle에서 나가서 **Walk로 가는 트랜지션은 없음**.

- **Walk**
  - 들어오는 트랜지션: (다른 스테이트에서 Walk로 들어오는 경로는 있으나, **Idle → Walk 없음**)
  - 나가는 트랜지션:
    - Speed < 0.1 → Idle
    - Speed > 2 → Run

- **Run**
  - 나가는 트랜지션: Speed < 2 → Walk

- **Axe / PickAxe / LeftHook**
  - Any State → 해당 스테이트 (Attack / Mine 트리거 등).
  - 재생 후 Exit Time 등으로 Idle 복귀.

### 2.4 문제점 정리 (컨트롤러)

1. **Idle → Walk 전환이 없음**  
   - Idle에서 나가는 유일한 트랜지션은 “Speed > 0.1 → Idle” (자기 자신)뿐이라, **절대 Walk로 넘어가지 않음**.
   - 따라서 **Walk·Run이 재생되지 않는 직접적인 컨트롤러 원인**은 “Idle에서 Walk로 가는 트랜지션이 없기 때문”으로 보는 것이 맞음.

2. (선택) Run ↔ Walk 전환 조건은 현재 설정만 보면 의도대로 동작 가능 (Speed 0.1 / 2 기준).

---

## 3. 스크립트와 파라미터 연동 상태

### 3.1 PlayerMover.cs

- **이동**: 조이스틱 입력 → `smoothMoveDir`, `smoothSpeedMagnitude`로 실제 속도 계산 후 `transform.position` 갱신.
- **Animator 참조 없음**: `Animator` 컴포넌트를 찾거나 사용하는 코드가 **전혀 없음**.
- **결과**: `Speed` 파라미터가 **한 번도 설정되지 않음** → 계속 **0 (기본값)**.
- 따라서:
  - 컨트롤러에 Idle → Walk 트랜지션이 있어도, 스크립트가 `Speed`를 넣어주지 않으면 Walk로 전환되지 않음.
  - 현재는 “스크립트가 Speed를 안 넣음” + “Idle → Walk 트랜지션 없음” 두 가지가 동시에 있음.

### 3.2 정리

- **Walk/Run이 재생되지 않는 코드 쪽 원인**:  
  **PlayerMover(또는 동등한 이동 스크립트)에서 `Animator.SetFloat("Speed", ...)` 를 호출하지 않음.**

---

## 4. Idle이 “한 번만 재생되고 멈추는” 현상

### 4.1 가능 원인

1. **Idle 클립이 루프가 아님**
   - Idle 모션: `Meshy_AI_Animation_Idle_02_frame_rate_60.fbx` (guid: 139cf74d75ce39f4e974d023ce03c8c3)
   - 해당 FBX 메타: `clipAnimations: []`, `animationWrapMode: 0` (Default)
   - **클립이 Loop로 설정되어 있지 않으면** 한 사이클 재생 후 마지막 프레임에서 멈춘 것처럼 보일 수 있음.
   - **확인 방법**:  
     FBX 선택 → Inspector → Animation 탭에서 해당 클립 선택 → **Loop Time** 체크 여부 확인.

2. **Write Defaults**
   - AC_Player_Base에서는 Idle 스테이트에 `m_WriteDefaultValues: 1`로 되어 있음.  
     일반적으로 Idle 한 번 재생 후 멈추는 현상의 주원인은 아니고, 우선은 **클립 루프**를 의심하는 것이 좋음.

### 4.2 정리

- **Idle 한 번만 재생되고 멈춤** → 우선 **Idle 클립의 Loop Time** 설정 확인 권장.
- 추가로, 같은 캐릭터/같은 Avatar를 쓰는 다른 클립(Walk, Run)이 있다면, 해당 FBX들도 Animation 탭에서 루프 설정 확인하는 것이 좋음.

---

## 5. Apply Root Motion 관련

- 현재 Animator에서 **Apply Root Motion = On**.
- 이동은 **PlayerMover**가 `transform.position`으로 직접 제어 중.
- Root Motion을 쓰지 않고 코드로만 이동할 경우, **Apply Root Motion을 끄는 것**이 일반적임.  
  (켜 두면 Idle/Walk/Run 클립에 root motion이 있을 때 위치가 이중으로 움직일 수 있음.)
- **권장**: 코드 기반 이동만 사용한다면 **Apply Root Motion = Off** 로 두고, 이동은 전부 PlayerMover로만 처리.

---

## 6. GPT/다음 작업자 전달용 요약

| 구분 | 현재 상태 | 문제/조치 |
|------|-----------|------------|
| **씬 계층** | Player → ModelRoot → 메쉬/아머처, Animator는 Player에 있음 | 구조상 문제 없음. |
| **Avatar/Controller** | AC_Player_Base, Humanoid Avatar 연결됨 | 연결됨. |
| **Idle → Walk** | Idle에서 나가는 트랜지션은 “Speed > 0.1 → Idle”만 존재 | **Idle → Walk 트랜지션 추가 필요** (조건 예: Speed > 0.1 또는 >= 0.1). |
| **Speed 파라미터** | PlayerMover에서 Animator를 참조하지 않음 | **이동 스크립트에서 `Animator.SetFloat("Speed", 현재속도)` 호출 필요.** (현재 속도는 `smoothSpeedMagnitude * maxMoveSpeed` 등으로 계산 가능.) |
| **Idle 한 번 재생 후 멈춤** | Idle 클립 FBX: clipAnimations 비어 있음, animationWrapMode 0 | **Idle 클립 Loop Time 여부 확인** (FBX Animation 탭). 루프 꺼져 있으면 켜기. |
| **Apply Root Motion** | On | 코드 이동만 쓸 경우 **Off 권장.** |

---

## 7. 수정 시 권장 순서

1. **AC_Player_Base 컨트롤러**  
   - Idle 스테이트에서 **Walk**로 가는 트랜지션 추가: 조건 **Speed > 0.1** (또는 >= 0.1).

2. **PlayerMover(또는 전용 애니메이션 브릿지)**  
   - 같은 오브젝트(또는 ModelRoot 상위)의 `Animator`를 참조하여,  
     매 프레임(또는 이동 시)에 **`Animator.SetFloat("Speed", currentSpeed)`** 호출.  
     - currentSpeed 예: `smoothSpeedMagnitude * maxMoveSpeed`  
     - 달리기 구분이 있으면 구간에 따라 0 / walkSpeed / runSpeed 등으로 나누어 넣을 수 있음.

3. **Idle FBX**  
   - `Meshy_AI_Animation_Idle_02_frame_rate_60.fbx` → Animation 탭에서 해당 클립 **Loop Time** 체크.

4. **(선택)**  
   - Apply Root Motion 끄기 (코드 이동만 사용할 경우).

위 순서대로 적용하면, Idle 루프 재생 + 이동 시 Walk/Run 전환까지 연결된 상태를 기대할 수 있음.

---

**참고 파일**
- 씬: `Assets/Scenes/Stages/Stage_01_Woodland.unity`
- 컨트롤러: `Assets/Prefabs/Characters/Player/Animations/AC_Player_Base.controller`
- 이동 스크립트: `Assets/Scripts/Gameplay/Player/PlayerMover.cs`
- Idle FBX: `Assets/Prefabs/Characters/Player/Animations/Meshy_AI_Animation_Idle_02_frame_rate_60.fbx`
