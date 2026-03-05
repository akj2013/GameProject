# 현재 씬 CPU → 60 FPS 근접 제안

현재 CPU 31~34ms(약 30 FPS)를 16ms 이하로 줄이기 위한 수정 제안입니다.  
우선순위 순으로 정리했습니다.

---

## 1. 스크립트 (매 프레임 비용 줄이기)

### 1-1. AutoAttack — 나무 탐색 빈도 줄이기 (효과 큼)

**현재:** `Update()`에서 **매 프레임** `DetectTree()` 호출  
→ `Physics.OverlapSphereNonAlloc` + hit마다 `GetComponentInParent` / `GetComponent` / `ClosestPoint` 실행.

**제안:**
- 나무 탐색을 **매 프레임이 아니라 2~3프레임마다** 또는 **0.05~0.1초 간격**으로만 실행.
- 예: `if ((Time.frameCount % 2) == 0) DetectTree();` 또는 `_nextDetectTime`으로 주기 체크.
- 타겟이 없을 때는 주기를 더 길게(0.15초 등) 두어도 됨.

### 1-2. AutoAttack — Debug.DrawRay 제거/조건부

**현재:** 매 프레임 `Debug.DrawRay` 3회 호출 (공격 범위 시각화).

**제안:**
- **빌드에서는 비활성화.** `#if UNITY_EDITOR` 안에서만 호출하거나, 빌드 전 제거.
- 디버그용이면 `[Conditional("UNITY_EDITOR")]` 또는 `#if !UNITY_EDITOR`로 감싸기.

### 1-3. StackUI — 값 바뀔 때만 UI 갱신

**현재:** `Update()`에서 매 프레임 `PlayerStats.Instance.attackDamage` 읽어서 슬라이더/텍스트 갱신.

**제안:**
- `int _lastAttackDamage` 캐시. `attackDamage != _lastAttackDamage`일 때만 슬라이더/텍스트 설정 후 `_lastAttackDamage` 갱신.
- 또는 `PlayerStats`에서 공격력 변경 시 이벤트를 쏘고, StackUI는 그 이벤트에서만 갱신.

### 1-4. MiniMapController — GetComponent 캐시

**현재:** `GetStageIndexContainingPlayer()`에서 매 프레임 `stageTiles[i].GetComponent<TileStageConfig>()` 호출.

**제안:**
- `TileStageConfig[]` 또는 `List<TileStageConfig>` 캐시 배열을 한 번 채워 두고, 인덱스만 사용.
- Start/초기화 시 `stageTiles` 순서대로 `GetComponent<TileStageConfig>()` 한 번만 호출해 캐시.

### 1-5. StageTreePanelController — RectTransform/Canvas 캐시

**현재:** 패널이 켜져 있을 때 `Update()`에서 매번 `GetComponent<RectTransform>()`, `GetComponentInParent<Canvas>()` 호출.

**제안:**
- `RectTransform` / `Canvas`를 Awake나 패널 열 때 한 번만 가져와서 캐시하고, Update에서는 캐시만 사용.

### 1-6. CameraDistanceCullManager — 거리 비교 최소 최적화

**현재:** `Vector3.Distance(camPos, ...)` 사용 (제곱근 계산).

**제안:**
- 비교만 할 때는 `(camPos - pos).sqrMagnitude` vs `cullDistance * cullDistance`로 비교해 sqrt 제거. (이미 0.2초 간격이라 효과는 작을 수 있음.)

---

## 2. 렌더링 (CPU 쪽 부담 줄이기)

### 2-1. 그림자 거리 줄이기

**현재:** Quality에 따라 shadowDistance 15~150 등 다양.

**제안:**
- **모바일/타겟 기기**에서 쓰는 Quality 레벨의 **Shadow Distance**를 20~40 정도로 제한.
- Edit → Project Settings → Quality → 해당 레벨 → Shadow Distance 감소.
- 그림자 계산/캐스팅 CPU 비용이 줄어듦.

### 2-2. URP 에셋에서 그림자/캐스케이드

- URP Renderer/Forward Renderer에서 **Cascaded Shadow Map** 단계 수 줄이기(예: 1~2단).
- 가능하면 **Hard Shadows**만 사용(모바일).

### 2-3. 실시간 라이트 개수

- **실시간 그림자를 쓰는 방향광 1개**만 두고, 나머지는 실시간 그림자 끄기.
- 필요하면 라이트맵으로 보완.

### 2-4. 환경 오브젝트 Static + 배칭

- 나무 제외 타일/지면/구조물 **Static** 체크 → 스태틱 배칭 기대.
- PandaMat 등 **단일 머티리얼** 유지해 SetPass/배칭 유리하게 (이미 진행 중이면 유지).

---

## 3. 물리

### 3-1. 레이어 충돌 매트릭스

- Edit → Project Settings → Physics → Layer Collision Matrix.
- **나무–나무**, **로그–로그** 등 서로 충돌 불필요한 조합은 체크 해제해 불필요한 충돌 검사 제거.

### 3-2. OverlapSphere 범위/레이어

- AutoAttack의 `attackRange` / `EffectiveAttackRange`가 필요 이상으로 크지 않은지 확인.
- `treeLayer`에 **나무 콜라이더만** 들어가 있도록 레이어 설정 유지.

---

## 4. 기타

### 4-1. 빌드 시 로그 비활성화

- `Debug.Log` / `Debug.LogWarning`은 빌드에서도 호출되면 부담이 됨.
- 중요한 것만 남기고, 나머지는 `#if UNITY_EDITOR` 또는 로그 레벨로 빌드에서 제거.

### 4-2. CameraDistanceCullManager 체크 주기

- 현재 `checkInterval = 0.2f`면 이미 부담이 적음.
- 기기 사양이 낮으면 0.25~0.3초로 늘려도 됨.

### 4-3. TreeManager — 플레이어 참조 캐시

- `playerObject = GameObject.FindGameObjectWithTag("Player")`가 여러 곳에서 null일 때만 호출됨.
- 가능하면 **한 번 찾은 뒤** 계속 재사용하고, 씬 전환 시에만 초기화.

---

## 적용 순서 제안

1. **AutoAttack** 나무 탐색 주기 조절 + Debug.DrawRay 제거/EDITOR 전용 (즉시 체감 가능).
2. **StackUI** 값 변경 시에만 갱신.
3. **MiniMapController** TileStageConfig 캐시.
4. **Quality / URP** Shadow Distance·캐스케이드 축소.
5. **Physics** 레이어 매트릭스 정리.
6. 나머지(StageTreePanel 캐시, 거리 sqrMagnitude, 로그 정리)는 여유 있을 때 적용.

이 순서로 적용하면 CPU 31~34ms에서 20ms 대, 목표 16ms 근처까지 단계적으로 줄여볼 수 있습니다.
