# Git 워크플로: CLEAN_EXPORT와 GameProject 연동

이 문서는 **CLEAN_EXPORT** 폴더를 사용해 GitHub **GameProject** 저장소에 안전하게 푸시하는 방법을 설명합니다.

---

## 1. CLEAN_EXPORT가 존재하는 이유

GameProject 저장소는 **전체 유니티 프로젝트**를 담는 용도가 아닙니다.

- **목적**
  - 스크립트 백업
  - 씬 백업
  - 문서 백업
  - AI 협업(ChatGPT / Cursor가 코드·문서 검토)

- **올리지 않는 것**
  - Asset Store 에셋
  - 메시/FBX, 텍스처, 오디오
  - 기타 외부 리소스·대용량 바이너리

이런 것들은 **로컬 워크스페이스(WoodLand3D)** 에만 두고,  
**CLEAN_EXPORT** 에는 스크립트·씬·문서·프로젝트 설정·패키지 목록만 넣어서 GameProject에 푸시합니다.

---

## 2. 어떤 폴더가 내보내지는가

CLEAN_EXPORT에는 **아래만** 복사됩니다.

| 복사 대상 (WoodLand3D) | CLEAN_EXPORT 내 경로 |
|------------------------|----------------------|
| Assets/MyScript (Legacy 제외) | Assets/Scripts |
| Assets/MyScript/Resources | Assets/Scripts/Core/Resources |
| Assets/MyScript/UI | Assets/Scripts/UI |
| Assets/MyScenes (Legacy 제외) | Assets/Scenes |
| Assets/Docs | Assets/Docs |
| Packages | Packages |
| ProjectSettings | ProjectSettings |

---

## 3. 어떤 폴더·파일이 제외되는가

CLEAN_EXPORT로 **복사하지 않으며**, 실수로 커밋되지 않도록 무시합니다.

- **폴더**: Assets/Audio, Assets/SOUND, Assets/Legacy, Assets/Models, Assets/Mesh, Assets/Textures, Assets/Materials, Assets/Prefabs, AssetStoreTools, Plugins, ThirdParty, Resources/External
- **확장자**: *.fbx, *.png, *.jpg, *.wav, *.mp3, *.ogg 등 (꼭 필요하지 않은 바이너리)

---

## 4. GitHub에 안전하게 푸시하는 방법

### 4.1 일상적인 워크플로

1. **WoodLand3D 워크스페이스에서 개발**  
   - 씬, 스크립트, 문서 등 모두 여기서 수정합니다.

2. **CLEAN_EXPORT 갱신**  
   - 프로젝트 루트에서 PowerShell로 실행:
   ```powershell
   .\Export-Clean.ps1
   ```
   - 위에서 정한 “복사 대상”만 CLEAN_EXPORT로 다시 복사됩니다.

3. **CLEAN_EXPORT에서 커밋·푸시**  
   - 터미널에서:
   ```powershell
   cd CLEAN_EXPORT
   git add .
   git status   # 올라갈 파일 확인
   git commit -m "메시지"
   git push origin main
   ```

### 4.2 주의사항

- **푸시는 반드시 CLEAN_EXPORT 폴더 안에서만** 합니다.  
  WoodLand3D 루트에서 `git push` 하면 안 됩니다 (원격이 GameProject라면 CLEAN_EXPORT 전용이어야 함).
- Export-Clean.ps1 실행 후 **반드시 CLEAN_EXPORT로 이동해서** add/commit/push 합니다.

---

## 5. 요약

| 단계 | 할 일 |
|------|--------|
| 1 | WoodLand3D에서 개발 |
| 2 | `.\Export-Clean.ps1` 실행 |
| 3 | `cd CLEAN_EXPORT` 후 `git add .` → `git commit` → `git push origin main` |

이렇게 하면 GameProject에는 스크립트·씬·문서·설정만 올라가고, 에셋·바이너리는 올라가지 않습니다.
