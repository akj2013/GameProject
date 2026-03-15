# BeamTube 메시 빔 시스템 설정 가이드

기존 VFX strip 빔(`VFX_HarvestBeam`, `StarShipBeamVFXBridge`)과 별개로, **튜브 메시 기반** 빔 본체를 쓰기 위한 설정 순서입니다.

---

## 1. 새로 추가된 파일 목록

| 파일 | 설명 |
|------|------|
| `Assets/Scripts/StarShip/BeamTubeMeshRenderer.cs` | 곡선 샘플링 + 튜브 메시 생성/갱신, Reveal 선택 옵션 |
| `Assets/Art/Shader/BeamTube_Unlit.shader` | URP Unlit 셰이더 (Reveal, BeamColor 등 프로퍼티) |
| `Assets/Docs/BeamTube_Setup_Guide.md` | 본 가이드 |

- 머티리얼 `M_BeamTube`는 Unity 에디터에서 **새로 생성**합니다 (아래 4번 참고).

---

## 2. BeamTubeMeshRenderer.cs 역할 요약

- **targetRouteLine**: `StarShipRouteLine` 참조 (기존 MainBeam_01 등).
- **lengthSegments**: 길이 방향 링 수 (곡선 샘플 수).
- **radialSegments**: 한 링의 원주 버텍스 수.
- **beamRadius**: 튜브 반지름 (실제 스케일).
- **updateEveryFrame**: true면 매 프레임 메시 갱신.
- **revealDuration**: 0이면 미사용. 양수면 해당 초 동안 Reveal 0→1 재생 (선택).

메시는 재사용하며, GC 할당을 줄이기 위해 리스트/메시를 한 번 할당 후 Clear/Set만 합니다.

---

## 3. 셰이더 / 머티리얼 구성

### 셰이더 (이미 생성됨)

- **경로**: `Assets/Art/Shader/BeamTube_Unlit.shader`
- **Shader 이름**: `WoodLand3D/BeamTube_Unlit`
- **프로퍼티**: BeamColor(코어), GlowColor(글로우), Reveal, RevealSoftness, BeamWidth(코어 폭), GlowWidth(글로우 바깥 폭), FlowSpeed(예약).

### 머티리얼 M_BeamTube 만들기

1. Project 창에서 우클릭 → **Create → Material**.
2. 이름을 `M_BeamTube`로 지정.
3. Inspector에서 **Shader**를 `WoodLand3D/BeamTube_Unlit`로 선택.
4. **Beam Color** 등 원하는 값으로 설정 (Reveal은 1로 두면 전부 표시).

---

## 4. Unity 에디터에서 연결하는 순서

1. **빈 GameObject 생성** (예: `BeamTube`).
2. **BeamTubeMeshRenderer** 컴포넌트 추가 (Add Component → BeamTubeMeshRenderer).
3. **Target Route Line**에 기존 `StarShipRouteLine`이 붙어 있는 오브젝트 할당 (예: MainBeam_01).
4. **Mesh Renderer**의 **Materials**에 `M_BeamTube` 할당 (위에서 만든 머티리얼).
5. **Beam Radius**, **Length Segments**, **Radial Segments** 등 원하는 값으로 조정.
6. (선택) **Reveal Duration**을 0.35 등으로 두면 재생 시 빔이 시작→끝으로 차오르는 연출.
7. **Play** 후 곡선을 따라가는 튜브가 보이는지 확인.

---

## 5. 현재 버전 한계 / 다음 확장 포인트

- **한계**
  - 본체만 구현. 시작점/끝점 스파크, 서페이스 스파크 등 미포함.
  - 셰이더는 Unlit. Flow / Glow / CoreWidth 등 프로퍼티는 준비만 되어 있음.
  - Shader Graph 버전(SG_BeamTube)은 없음. 필요 시 동일 프로퍼티로 새 Shader Graph 생성 후 머티리얼만 교체 가능.

- **확장 포인트**
  - Reveal: 이미 UV.x 기준으로 동작. Reveal Duration으로 자동 재생 가능.
  - Flow: UV 스크롤 또는 FlowSpeed 기반 오프셋 추가.
  - Glow/Core: UV.y 또는 원주 거리 기준으로 코어/글로우 분리.
  - 시작/끝 VFX: 별도 파티클/이펙트로 추가.
