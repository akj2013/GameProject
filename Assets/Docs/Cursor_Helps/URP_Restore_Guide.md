# URP(Render Pipeline) 설정 복구 가이드

Unity 프로젝트에서 **Default Render Pipeline = None** 상태로 인해 URP 머티리얼이 핑크색으로 보일 때, URP를 다시 정상 동작시키기 위한 단계별 가이드입니다.

---

## 현재 상태 판단 (스크린샷 기준)

- **Graphics / Quality**에서 Render Pipeline Asset이 **None** → 프로젝트는 현재 **Built-in** 또는 “파이프라인 미지정” 상태로 동작 중입니다.
- URP 머티리얼(TMP_SDF-URP Lit, Ground - URP 등)은 있지만, **URP를 구동하는 Asset(UniversalRenderPipelineAsset, URP Renderer Asset)이 없거나 연결이 끊긴 상태**로 보입니다.
- **Universal Render Pipeline/Lit** 셰이더가 핑크색인 이유: URP가 활성화되지 않아 해당 셰이더가 로드되지 않기 때문입니다.

---

## 1단계: Universal RP 패키지 설치 여부 확인

### 1-1. Package Manager 열기

1. 상단 메뉴 **Window** 클릭  
2. **Package Manager** 클릭  
3. 창이 열리면 왼쪽 상단 드롭다운이 **Unity Registry** 또는 **In Project**인지 확인

### 1-2. Universal RP 설치 여부 확인

1. 왼쪽 목록에서 **Universal RP** 항목을 찾습니다.  
   - 없으면 상단 검색창에 `universal` 또는 `urp` 입력 후 **Universal RP** 선택  
2. 오른쪽 패널에서  
   - **버전 번호**가 보이고, **Install** 버튼이 없고 **Remove** 또는 **Update**만 있으면 → **이미 설치됨**  
   - **Install** 버튼이 있으면 → **미설치**

### 1-3. 미설치일 때 설치하기

1. **Universal RP** 선택  
2. 오른쪽 아래 **Install** 클릭  
3. 설치가 끝날 때까지 기다린 후 Package Manager 창을 닫아도 됩니다  

> 설치 후에는 아래 2단계로 넘어가서 Pipeline Asset을 만듭니다.

---

## 2단계: URP Pipeline Asset + Renderer Asset 생성 (에셋이 없을 때)

패키지는 있는데 **Render Pipeline Asset 파일이 없을 때** 직접 만드는 방법입니다.

### 2-1. Renderer Asset 먼저 만들기

1. **Project** 창에서 에셋을 둘 폴더로 이동 (예: `Assets/Settings` 또는 `Assets/URP`)  
   - 폴더가 없으면 우클릭 → **Create > Folder**로 `Settings` 또는 `URP` 생성  
2. 해당 폴더 안 빈 공간에서 **우클릭**  
3. **Create** → **Rendering** → **URP Asset (with Universal Renderer)** 선택  
4. 생성된 항목이 **두 개** 생깁니다:  
   - `UniversalRenderPipelineAsset` (이름 예: `UniversalRenderPipelineAsset`)  
   - `UniversalRenderPipelineRenderer` (이름 예: `UniversalRenderPipelineAsset_Renderer`)  
5. **Renderer** 쪽 이름을 구분하기 쉽게 바꿉니다 (예: `URP_Renderer`).  
   - Pipeline Asset 이름도 바꿔도 됩니다 (예: `URP_Asset`).

### 2-2. 생성 후 확인할 파일

- **Pipeline Asset** 1개: Inspector에서 "Script"가 `Universal Render Pipeline Asset`  
- **Renderer Asset** 1개: Inspector에서 "Script"가 `Universal Renderer`  
- Pipeline Asset을 선택했을 때 Inspector **Renderer List**에 위 Renderer가 하나 들어 있어야 합니다. 비어 있으면 그 안에 Renderer 에셋을 드래그로 넣습니다.

### 2-3. Create 메뉴에 URP가 안 보일 때

- **Window > Package Manager**에서 **Universal RP**가 설치되어 있는지 다시 확인  
- 설치 후에도 **Create > Rendering**에 "URP Asset"이 없다면:  
  - Unity 재시작  
  - 또는 **Create > Rendering** 하위에 **URP Renderer**만 보이면:  
    - **URP Renderer**로 Renderer 에셋만 만들고,  
    - **Create > Rendering > Pipeline Asset > Universal Render Pipeline Asset**으로 Asset만 만든 뒤,  
    - Asset의 Inspector **Renderer List**에 방금 만든 Renderer를 할당

---

## 3단계: Project Settings에 URP Asset 연결

### 3-1. Graphics에 연결

1. 상단 메뉴 **Edit** → **Project Settings**  
2. 왼쪽에서 **Graphics** 선택  
3. **Scriptable Render Pipeline Settings** (또는 **Default Render Pipeline**) 항목 찾기  
4. **None**으로 되어 있는 칸을 클릭한 뒤  
   - 2단계에서 만든 **UniversalRenderPipelineAsset** 에셋을 드래그해서 넣거나  
   - 오른쪽 동그라미(⊙)를 눌러서 해당 Asset 선택  
5. 설정 창을 닫아도 자동 저장됩니다.

### 3-2. Quality에 연결

1. **Edit** → **Project Settings**  
2. 왼쪽에서 **Quality** 선택  
3. 오른쪽 **Quality Levels**에서 **현재 사용 중인 레벨**(초록색 체크된 행) 클릭  
4. 해당 레벨의 **Render Pipeline Asset** (또는 **Render Pipeline Asset** 필드) 찾기  
5. 여기도 **None**이면 2단계에서 만든 **같은 UniversalRenderPipelineAsset**을 할당  
   - 드래그하거나 ⊙로 선택  
6. 다른 Quality 레벨도 URP로 쓸 예정이면 같은 방식으로 같은 Asset 할당

### 3-3. 연결 후 확인

- **Graphics**와 **Quality** 둘 다 같은 URP Asset이 들어가 있어야 합니다.  
- 씬을 다시 열거나 Play 모드에 들어가면 URP가 적용된 상태로 렌더링됩니다.

---

## 4단계: 핑크 머티리얼이 안 사라질 때 추가 조치

연결까지 했는데 여전히 **Universal Render Pipeline/Lit**이 핑크색이면 아래를 순서대로 시도합니다.

### 4-1. 씬/프로젝트 새로고침

1. **File > Save Project** (선택)  
2. **File > New Scene**으로 임시 씬 열었다가 다시 원래 씬 열기  
   - 또는 **Window > General > Scene**에서 씬 탭 더블클릭으로 재로드  
3. **Play** 한 번 했다가 멈춰 보기  

→ 대부분 이 단계에서 URP가 로드되며 핑크가 사라집니다.

### 4-2. 셰이더가 여전히 깨진 머티리얼만 고치기

1. **Project**에서 해당 머티리얼 선택  
2. **Inspector**에서 **Shader**가 `Universal Render Pipeline/Lit` (또는 URP/Lit)인지 확인  
3. Shader 드롭다운을 한 번 열었다가 **다시 Universal Render Pipeline > Lit** 선택  
   - 리스트에 없으면 URP가 아직 로드되지 않은 것이므로 1~3단계 재확인  
4. 머티리얼이 여러 개면 **여러 개 선택** 후 Inspector에서 Shader만 다시 **URP > Lit**으로 맞춤  

### 4-3. Built-in → URP 머티리얼 일괄 변환 (선택)

- 예전에 Built-in 머티리얼을 많이 쓰고 있었다면:  
  1. **Edit > Render Pipeline > Universal Render Pipeline > Upgrade Project Materials to Universal Render Pipeline Materials**  
  2. 경고/확인 창에서 **Proceed**  
  - 이 메뉴는 Built-in 머티리얼을 URP용으로 바꿔 줍니다. 이미 URP 머티리얼만 있다면 필수는 아닙니다.

### 4-4. Unity 재시작

- 위를 다 해도 핑크가 남으면 **Unity 에디터를 완전히 종료했다가 다시 프로젝트 열기**  
- 재시작 후 1~3단계 설정이 그대로인지 한 번 더 확인

---

## 5단계: 현재 프로젝트가 Built-in인지 URP인지 판단

**지금 보이는 스크린샷 기준:**

- **Project Settings > Graphics**의 **Default Render Pipeline** = **None**  
- **Quality**의 **Render Pipeline Asset** = **None**  

→ 이 상태면 **Built-in**이거나 “파이프라인 미지정”으로 동작 중입니다. URP Asset이 할당되지 않았기 때문입니다.

**복구 후 (URP 정상 상태):**

- **Graphics**와 **Quality**에 **Universal Render Pipeline Asset**이 할당됨  
- Play 모드나 씬 뷰에서 URP Lit 머티리얼이 핑크가 아닌 색으로 보임  
- **Edit > Project Settings > Graphics**에서 해당 필드에 에셋 이름이 보이면 URP로 동작 중이라고 보면 됩니다.

---

## 요약

| 항목 | 내용 |
|------|------|
| **가장 가능성 높은 원인** | URP 패키지는 있지만 **Render Pipeline Asset(및 Renderer)이 없거나**, **Graphics/Quality에 연결되지 않아** URP가 활성화되지 않은 상태. |
| **가장 빠른 복구 방법** | 1) Package Manager에서 **Universal RP** 설치 확인 → 2) **Create > Rendering > URP Asset (with Universal Renderer)** 로 Asset + Renderer 생성 → 3) **Project Settings > Graphics**와 **Quality**에 그 Asset 할당 → 4) 씬 새로고침 또는 Play 한 번. |
| **복구 후 확인 체크리스트** | □ Graphics의 Default Render Pipeline에 URP Asset 할당됨 □ Quality의 Render Pipeline Asset에 URP Asset 할당됨 □ URP Lit 머티리얼이 핑크가 아님 □ Play 모드에서 라이팅/쉐도우가 기대대로 보임 |

---

## 참고

- 모바일 3D 탑다운/쿼터뷰 프로토타입에서는 URP를 복구한 뒤 타일/머티리얼/라이팅 작업을 계속하는 것을 권장합니다.  
- Standard 셰이더로 임시 버티기보다, 이 가이드대로 URP를 다시 연결해 두는 것이 이후 작업에 유리합니다.
