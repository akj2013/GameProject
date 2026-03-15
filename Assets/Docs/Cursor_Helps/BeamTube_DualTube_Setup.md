# BeamTube 이중 튜브(Core + Glow) 구성 가이드

## 구조 요약

- **한 오브젝트**에 `BeamTubeMeshRenderer` 붙임.
- 스크립트가 자동으로 **자식 2개** 생성:
  - `GlowTube` — 바깥쪽 원통 메시 (Glow 머티리얼)
  - `CoreTube` — 안쪽 원통 메시 (Core 머티리얼)
- Reveal은 두 메시에 동일하게 `MaterialPropertyBlock`으로 전달됨.

---

## 1. 셰이더

| 셰이더 | 용도 | 프로퍼티 |
|--------|------|----------|
| `WoodLand3D/BeamTube_Core` | 안쪽 본체 | `_BeamColor`, `_Reveal`, `_RevealSoftness` |
| `WoodLand3D/BeamTube_Glow` | 바깥 외곽층 | `_GlowColor`, `_Reveal`, `_RevealSoftness` |

- Core: 카메라 의존 없음, 안정적인 본체.
- Glow: 반투명 외곽층, Core를 가리지 않음 (Queue: Core가 Transparent+1로 나중에 그림).

---

## 2. 머티리얼 만들기

1. **Core 머티리얼**
   - Project에서 우클릭 → Create → Material.
   - Shader를 `WoodLand3D/BeamTube_Core`로 선택.
   - **Beam Color (Core)**: 코어 색 (예: 흰색).
   - Reveal / Reveal Softness는 런타임에 스크립트가 넣으므로 기본값만 두어도 됨.

2. **Glow 머티리얼**
   - Create → Material.
   - Shader를 `WoodLand3D/BeamTube_Glow`로 선택.
   - **Glow Color**: 외곽 색 + 알파 (예: (0.4, 0.6, 1, 0.5)).
   - Reveal / Reveal Softness는 런타임에 동일하게 적용됨.

---

## 3. 에디터에서 오브젝트 설정

1. 빔이 따라갈 **빈 GameObject**에 `BeamTubeMeshRenderer` 추가.
2. **Target Route Line**: `StarShipRouteLine` 곡선 컴포넌트가 있는 오브젝트 연결.
3. **Core Radius** / **Glow Radius**: 안쪽·바깥쪽 반지름 (Glow > Core 권장).
4. **Core Material** / **Glow Material**: 위에서 만든 Core·Glow 머티리얼 드래그.
5. (선택) **Reveal Duration** > 0 이면 해당 초 동안 Reveal 0→1 재생.
6. **Beam Irregularity** 헤더 아래 값으로 단일 빔의 유기적 불규칙성 조절(두께·밝기·연속성·경로 흔들림).
7. Play 후 Hierarchy에서 해당 오브젝트 자식으로 `GlowTube`, `CoreTube`가 생긴 것 확인.

---

## 4. 단일 빔 불규칙성 (Beam Irregularity)

- **BeamTubeMeshRenderer**는 단일 주광선 1개를 “광섬유 튜브”가 아니라 **빛처럼** 보이게 하기 위해, 메시·셰이더 양쪽에 불규칙성을 넣습니다.
- **메시**: 경로 흔들림(path wobble), 길이 방향 두께 변조(width variation). 코어와 글로우는 서로 다른 변조 위상(bias)을 써서 완전히 같은 패턴이 되지 않게 합니다.
- **셰이더**: UV.x(길이) 기준 밝기 변조(intensity variation), 연속성 변조(일부 구간 알파 감쇠, continuity break). Core/Glow 각각 Variation Bias로 위상을 달리 줄 수 있습니다.

### 4.1 추천 기본값 (불규칙성)

- Width Variation Amount: 0.12, Scale: 6
- Intensity Variation Amount: 0.25, Scale: 5
- Continuity Break Amount: 0.15, Scale: 8
- Path Wobble Amount: 0.03, Scale: 5
- Core Variation Bias: 0, Glow Variation Bias: 0.5
- Variation Speed: 0.2 (선택, 약한 시간 변화)

---

## 5. 왜 이중 튜브가 더 안정적인지

- **단일 메시 + 셰이더 비율**: 한 표면에서 NdotV 등으로 core/glow를 나누면 시점에 따라 코어가 얇아지거나, 글로우가 튀어 보이는 문제가 생김.
- **이중 튜브**: 코어는 **실제 안쪽 원통 메시**, 글로우는 **실제 바깥 원통 메시**라서 카메라가 움직여도 구조가 바뀌지 않음. 셰이더는 색과 Reveal만 담당하므로 단순하고 예측 가능함.

---

## 6. 시작점 발사광 (BeamStartGlow, 3D 구체)

- **BeamStartGlow** 컴포넌트로 빔 시작점에 **3D 부피 발사광**(내부 코어 구체 + 바깥 글로우 구체) 추가.
- Quad가 아닌 **실제 구체 메시**라서 카메라 각도에 관계없이 덩어리처럼 보임.

### 5.1 머티리얼

- Create → Material → Shader: **WoodLand3D/BeamStartGlow**
- Color / Intensity / Reveal은 스크립트가 MaterialPropertyBlock으로 넣음. 머티리얼 기본값만 두어도 됨.

### 5.2 에디터 설정

1. 빔 오브젝트(또는 빔과 같은 Route 오브젝트)에 **BeamStartGlow** 추가.
2. **Target Route Line**: 빔과 동일한 `StarShipRouteLine` 연결.
3. **Start Glow Material**: BeamStartGlow 셰이더 머티리얼 할당.
4. (선택) **Reveal Source**: `BeamTubeMeshRenderer` 연결 시 Reveal에 따라 발사광 강도 연동.
5. **Start Glow Core Size** / **Start Glow Outer Size**: 코어 구체·글로우 구체 반지름(스케일).
6. **Start Glow Color** / **Intensity**: 코어 색·강도. **Start Glow Outer Color** / **Outer Intensity**: 바깥 구체 색·강도.
7. **Follow Start Point**: 체크 시 매 프레임 시작점 위치로 이동.

### 5.3 추천 기본값

- Start Glow Core Size: 0.35 / Outer Size: 0.7
- Start Glow Color: (1, 0.98, 1, 0.9), Intensity: 1
- Start Glow Outer Color: (0.7, 0.85, 1, 0.35), Outer Intensity: 0.8

---

## 7. 도착점 임팩트 (BeamEndImpact, 3D)

- **BeamEndImpact** 컴포넌트로 빔 **도착점(타깃)** 에 **3D 부피 임팩트**(에너지 응집/충돌 반응) 추가.
- **ImpactCore**(작은 구) + **ImpactGlow**(바깥 구) + **ImpactRing**(선택, 얇은 3D 링). Quad/빌보드 없음.

### 7.1 머티리얼

- Create → Material → Shader: **WoodLand3D/BeamImpactGlow**
- Color / Intensity / Reveal은 스크립트가 MaterialPropertyBlock으로 넣음.

### 7.2 에디터 설정

1. 빔 오브젝트(또는 같은 Route 오브젝트)에 **BeamEndImpact** 추가.
2. **Target Route Line**: 빔과 동일한 `StarShipRouteLine` 연결.
3. **Impact Material**: BeamImpactGlow 셰이더 머티리얼 할당.
4. (선택) **Reveal Source**: `BeamTubeMeshRenderer` 연결 시 Reveal에 따라 임팩트 강도 연동.
5. **Impact Core Size** / **Impact Glow Size**: 코어·글로우 구 반지름. **Impact Ring Size**: 0이면 링 비표시.
6. **Impact Core/Glow/Ring Color·Intensity**: 각 색·강도.
7. **Follow End Point**: 체크 시 매 프레임 `EvaluatePoint(1)` 위치로 이동. 링은 `EvaluateTangent(1)`로 빔에 수직 정렬.

### 7.3 추천 기본값

- Impact Core Size: 0.3 / Glow Size: 0.6 / Ring Size: 0.5 (0이면 링 끔)
- Impact Core Color: (1, 0.9, 0.95, 0.9), Intensity: 1
- Impact Glow Color: (0.6, 0.75, 1, 0.4), Intensity: 0.85
- Impact Ring Color: (0.8, 0.9, 1, 0.35), Intensity: 0.6

---

## 8. 도착점 스파크 (BeamEndSparks, 파편형 3D)

- **BeamEndSparks**는 도착점에 붙어 있는 고정 막대가 아니라, **도착점에서 생성 → 바깥으로 짧게 날아감 → 수명에 따라 페이드 아웃 → 사라지면 다시 생성**되는 **에너지 파편형** 3D 스파크입니다.
- Quad/빌보드 없이 **실제 3D 실린더 메시**만 사용. 카메라 의존 셰이더 없음.

### 8.1 머티리얼

- Create → Material → Shader: **WoodLand3D/BeamImpactSpark**
- Color / Intensity / Reveal / Life는 스크립트가 MaterialPropertyBlock으로 매 프레임 설정. 머티리얼 기본값만 넣어 두면 됨.

### 8.2 에디터 설정

1. 빔 오브젝트(또는 같은 Route 오브젝트)에 **BeamEndSparks** 추가.
2. **Target Route Line**: 빔과 동일한 `StarShipRouteLine` 연결.
3. **Spark Material**: BeamImpactSpark 셰이더 머티리얼 할당.
4. (선택) **Reveal Source**: `BeamTubeMeshRenderer` 연결 시 Reveal에 따라 스파크 강도 연동.
5. **Spark Count**: 4~24. **Spark Lifetime Min/Max**, **Spark Speed Min/Max**, **Spark Length Min/Max**, **Spark Thickness**, **Spark Spawn Radius**, **Spark Cone Angle**로 동작·형태 조절.
6. **Follow End Point**: 체크 시 도착점이 라우트 끝을 따라 이동하며, 스파크는 매 프레임 해당 위치 근처에서 재생성·이동.

### 8.3 추천 기본값 (파편형: 튀어나가며 사라지는 스파크)

- Spark Count: 10
- Spark Lifetime Min: 0.12 / Max: 0.35
- Spark Speed Min: 2.5 / Max: 6
- Spark Length Min: 0.08 / Max: 0.22
- Spark Thickness: 0.012
- Spark Spawn Radius: 0.05
- Spark Cone Angle: 75
- Spark Color: (1, 0.96, 0.92, 0.9), Intensity: 1

---

## 9. 경로 전체 광자층 (BeamPathParticles)

- **BeamPathParticles**는 메인 빔을 대체하지 않고, **빔 길 전체를 발생원**으로 하는 미세 발광 입자층입니다.
- 입자는 **경로를 따라 이동하는 구슬이 아니라**, 경로의 각 지점에서 순간적으로 생성되어 **코어 주변 360°로 짧게 산란했다가 수명에 따라 사라지는** 연출입니다. Reveal 구간까지만 생성·표시됩니다.

### 9.1 머티리얼

- Create → Material → Shader: **WoodLand3D/BeamPathParticle**
- Color / Intensity / Reveal / Life는 스크립트가 MaterialPropertyBlock으로 설정. **Softness**: 중심에서 바깥으로 부드럽게 사라지는 정도.

### 9.2 에디터 설정

1. 빔 오브젝트(BeamTube와 같은 부모 또는 BeamTube 자식)에 **BeamPathParticles** 추가.
2. **Target Route Line**: 빔과 동일한 `StarShipRouteLine` 연결.
3. **Particle Material**: BeamPathParticle 셰이더 머티리얼 할당.
4. (선택) **Reveal Source**: `BeamTubeMeshRenderer` 연결 시 Reveal이 차오른 구간까지만 입자 생성·가시성 연동.
5. **Particle Count**, **Particle Lifetime Min/Max**, **Particle Speed Min/Max**, **Particle Size Min/Max**, **Spawn Radius**, **Spawn Jitter**로 밀도·수명·산란 속도·크기·생성 범위 조절.
6. **Follow Route**: 체크 시 매 프레임 경로 기준 갱신.

### 9.3 추천 기본값 (산란형)

- Particle Count: 64
- Particle Lifetime Min: 0.15 / Max: 0.4
- Particle Speed Min: 0.5 / Max: 1.2
- Particle Size Min: 0.025 / Max: 0.06
- Spawn Radius: 0.04
- Spawn Jitter: 0.1
- Particle Color: (0.95, 0.97, 1, 0.6), Intensity: 0.7
- Softness(머티리얼): 1.2
