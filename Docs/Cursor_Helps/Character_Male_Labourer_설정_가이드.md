# Character_Male_Labourer 설정 가이드

## 1. DeformationSystem vs Geometry가 따로 있는 이유

- **DeformationSystem**: **뼈대(리그)** 만 있는 계층입니다. Hip, Spine, Arm, Leg 등 본(Transform)들이 여기 있고, 애니메이션이 이 본들을 움직입니다.
- **Geometry**: **메시(몸통/옷 등)** 가 있는 계층입니다. 자식 `geo`에 **SkinnedMeshRenderer**가 있고, 이 메시가 DeformationSystem의 본들을 “참조”해서 따라 움직입니다.

즉, **애니메이션은 DeformationSystem을 돌리고 → Geometry의 메시가 그 본에 맞춰 늘어나는 구조**라서 둘이 분리되어 있습니다. Geometry에는 본이 없고, DeformationSystem에는 메시가 없습니다.

---

## 2. 어디에 뭘 넣어야 하는지

### Animator (애니메이터)

- **넣는 위치: Character_Male_Labourer 루트** (DeformationSystem과 Geometry의 부모인 최상위 오브젝트).
- **Geometry에는 넣지 마세요.** Geometry는 메시만 있어서 Animator를 두어도 본을 움직일 수 없습니다. Animator는 **본 계층(DeformationSystem)을 가진 오브젝트**에 있어야 합니다.
- **설정 내용:**
  - **Avatar**: 이 캐릭터 FBX(Character_Male_Labourer)에서 만든 Avatar 사용 (보통 FBX 임포트 시 자동 생성).
  - **Controller**: **New Animator Override Controller** 지정.

현재 **Character_Male_Labourer 프리팹에는 Animator 컴포넌트가 없습니다.**  
→ 프리팹 루트(Character_Male_Labourer)에 **Animator 컴포넌트를 추가**하고, 위처럼 Avatar + Override Controller를 넣어야 합니다.

### Animation Relay 스크립트

- **넣는 위치:** 애니메이션 이벤트가 호출되는 쪽. 보통 **Animator가 붙어 있는 오브젝트 = Character_Male_Labourer 루트**에 두면 됩니다.
- **설정:** `autoAttack` 필드에 **Player에 붙어 있는 AutoAttack 컴포넌트**를 드래그해서 연결 (씬에서 Player 하위로 이 캐릭터가 들어가 있다면, 그 Player의 AutoAttack).

---

## 3. 오버라이드 원본 컨트롤러 파라미터 확인 결과

**New Animator Override Controller**의 원본 컨트롤러는 **TopDownAnimator.controller** 입니다.

해당 컨트롤러에 있는 파라미터는 아래와 같고, **CharacterLocomotion이 사용하는 이름과 동일합니다.**

| 파라미터 이름         | 타입  | CharacterLocomotion 사용 여부 |
|----------------------|-------|--------------------------------|
| **Forward**          | Float | ✅ `forwardAnimationVar` 기본값 |
| **Strafe**           | Float | ✅ `strafeAnimationVar` 기본값 |
| **MoveSpeedMultiplier** | Float | ✅ 코드에서 직접 `SetFloat` |
| Attack               | Trigger | (AutoAttack 등에서 사용) |
| AttackSpeed          | Float | (공격 속도) |

따라서 **오버라이드/원본 컨트롤러 쪽 파라미터는 수정할 필요 없습니다.**  
애니메이션이 안 나오는 이유는 파라미터가 아니라 **Animator를 넣은 위치** 또는 **CharacterLocomotion에서 Animator 참조를 안 하는 것** 때문입니다.

---

## 4. 씬(Player) 쪽 설정

- **CharacterLocomotion (Player에 붙어 있음)**  
  - **Animator:** 씬에서 **Character_Male_Labourer 인스턴스 루트에 추가한 그 Animator**를 드래그해서 연결.  
  - **Character Visual:** 캐릭터가 이동 방향으로만 돌아가게 하려면, **Character_Male_Labourer 루트 Transform** 또는 **Geometry Transform**을 지정 (카메라는 회전하지 않게 하려면 루트/Geometry 중 하나만 회전하도록).

---

## 5. 요약 체크리스트

1. **Character_Male_Labourer 프리팹 루트**에 **Animator** 추가 → Avatar + **New Animator Override Controller** 지정.
2. **Geometry에는 Animator 넣지 않기** (Override만 Geometry에 “설정했다”면, 그건 잘못된 위치; 루트로 옮기기).
3. **Animation Relay**는 Character_Male_Labourer 루트에 두고, **autoAttack**에 Player의 AutoAttack 연결.
4. **씬에서 Player의 CharacterLocomotion** → **Animator** 필드에 위에서 만든 Animator 할당.
5. **Character Visual**은 캐릭터 루트 또는 Geometry로 설정해 두기.

이렇게 하면 이동/공격 애니메이션과 릴레이가 정상 동작합니다.

---

## 6. 캡슐은 화면 중앙인데 캐릭터만 밀려 나갈 때 (루트 모션)

**증상:** 캡슐 콜라이더/플레이어 중심은 화면 중앙에 있는데, 캐릭터 비주얼만 앞이나 옆으로 나가 보임.

**원인:** 캐릭터에 붙인 **Animator**에서 **Apply Root Motion**이 켜져 있으면, 애니메이션이 “루트(캐릭터 루트 Transform)”를 움직입니다. 실제 이동은 **CharacterController**(Player)가 담당하는데, 루트 모션은 **Character_Male_Labourer** 루트만 움직이기 때문에, 콜라이더(Player)와 비주얼(캐릭터)이 어긋납니다.

**해결:**

1. **Character_Male_Labourer** 루트(Animator가 붙어 있는 오브젝트) 선택.
2. **Animator** 컴포넌트에서 **Apply Root Motion** 체크 **해제**.
3. 저장 후 플레이해서, 캐릭터가 콜라이더/화면 중앙과 같이 움직이는지 확인.

이 프로젝트는 **CharacterController**로 이동을 처리하므로, 애니메이션은 **루트 모션 없이** 본/메시만 움직이도록 두는 것이 맞습니다.
