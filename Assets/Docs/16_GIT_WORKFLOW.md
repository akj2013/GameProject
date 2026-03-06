좋다. 아래는 **완전히 정리된 최종 버전 `Assets/Docs/16_GIT_WORKFLOW.md` 전체 내용**이다.
이 문서는 다음 목적을 충족하도록 작성했다.

* CLEAN_EXPORT 기반 Git 운영
* Cursor가 Git을 수행하는 구조
* merge 발생 방지
* 저장소 구조 보호
* Unity 프로젝트에서 안전한 Git 사용
* 커밋 규칙 명확화

**그대로 저장해서 사용하면 된다.**

---

# 16_GIT_WORKFLOW.md

# Git 워크플로 — CLEAN_EXPORT 기반 저장소 운영

이 문서는 **WoodLand3D 로컬 Unity 프로젝트**와
**GitHub 저장소 GameProject**를 안전하게 동기화하기 위한 Git 운영 규칙을 설명합니다.

이 저장소는 **전체 Unity 프로젝트 저장소가 아니라 경량 코드/씬/문서 백업 저장소**입니다.

---

# 1. 저장소 목적

GameProject 저장소는 다음 용도로 사용됩니다.

* 스크립트 백업
* 씬 구조 백업
* 프로젝트 문서 관리
* AI 협업 (ChatGPT / Cursor 코드 검토)

이 저장소는 **Unity 프로젝트 전체를 담지 않습니다.**

---

# 2. 저장소 기본 구조

GitHub 저장소에는 다음 구조만 존재합니다.

```
Assets/
 ├ Scripts/
 ├ Scenes/
 └ Docs/

Packages/
ProjectSettings/
```

설명

| 경로              | 내용            |
| --------------- | ------------- |
| Assets/Scripts  | 게임 로직         |
| Assets/Scenes   | Unity 씬       |
| Assets/Docs     | 프로젝트 문서       |
| Packages        | Unity 패키지 목록  |
| ProjectSettings | Unity 프로젝트 설정 |

이 구조는 **커밋 9d9bf6c 이후 기준 구조이며 변경하지 않습니다.**

---

# 3. 저장소에 포함되지 않는 것

다음 파일과 폴더는 **절대 GitHub에 올라가지 않습니다.**

### 대용량 에셋

```
Assets/Art
Assets/Audio
Assets/Models
Assets/Mesh
Assets/Textures
Assets/Materials
Assets/Prefabs
Assets/SOUND
Assets/Legacy
```

### 외부 에셋

```
AssetStoreTools
Plugins
ThirdParty
Resources/External
```

### 바이너리 파일

```
*.fbx
*.png
*.jpg
*.wav
*.mp3
*.ogg
*.psd
*.blend
```

이 파일들은 **WoodLand3D 로컬 프로젝트에만 존재합니다.**

---

# 4. CLEAN_EXPORT 구조

GitHub 저장소에 푸시되는 파일은 **CLEAN_EXPORT 폴더 기준**입니다.

```
CLEAN_EXPORT/

Assets/
 ├ Scripts
 ├ Scenes
 └ Docs

Packages/
ProjectSettings/
```

CLEAN_EXPORT는 **WoodLand3D 프로젝트에서 필요한 파일만 복사한 폴더**입니다.

---

# 5. Export-Clean.ps1 역할

`Export-Clean.ps1` 스크립트는 CLEAN_EXPORT를 생성합니다.

역할

1. 기존 CLEAN_EXPORT 삭제
2. 필요한 폴더만 복사
3. GitHub 업로드용 최소 프로젝트 생성

복사 대상

| WoodLand3D      | CLEAN_EXPORT    |
| --------------- | --------------- |
| Assets/Scripts  | Assets/Scripts  |
| Assets/Scenes   | Assets/Scenes   |
| Assets/Docs     | Assets/Docs     |
| Packages        | Packages        |
| ProjectSettings | ProjectSettings |

---

# 6. 개발 워크스페이스 구조

실제 개발은 **WoodLand3D 워크스페이스**에서 진행됩니다.

```
WoodLand3D
 ├ Assets
 ├ Packages
 ├ ProjectSettings
 ├ CLEAN_EXPORT
 └ Export-Clean.ps1
```

개발 작업은 항상 **WoodLand3D에서 수행합니다.**

GitHub에는 **CLEAN_EXPORT만 푸시합니다.**

---

# 7. Cursor 기반 Git 운영

이 프로젝트는 **Cursor가 Git 작업을 수행하는 방식**을 사용합니다.

개발자는 직접 Git 명령을 실행하지 않습니다.

Cursor가 수행하는 작업

* git add
* git commit
* git push
* git pull

하지만 **엄격한 규칙이 있습니다.**

---

# 8. Git 병합 방지 규칙

이 저장소는 **단일 브랜치 운영**을 사용합니다.

```
main
```

규칙

* 브랜치 생성 금지
* merge 금지
* rebase 금지

Cursor는 반드시 다음 순서로 Git 작업을 수행합니다.

```
git pull origin main
git add .
git commit -m "message"
git push origin main
```

---

# 9. 절대 사용하면 안 되는 Git 명령

Cursor는 다음 명령을 **절대 실행하면 안 됩니다.**

```
git merge
git rebase
git pull --rebase
git cherry-pick
git reset --hard
git clean -fd
git push --force
```

이 명령들은 **히스토리를 깨뜨리거나 충돌을 발생시킬 수 있습니다.**

---

# 10. Fast-Forward 전용 Pull 설정

merge commit 발생을 방지하기 위해 다음 설정을 사용합니다.

```
git config pull.ff only
```

이 설정을 사용하면 **merge commit이 생성되지 않습니다.**

---

# 11. 커밋 메시지 규칙

커밋 메시지는 다음 형식을 사용합니다.

```
type: summary
```

예시

```
feat: add resource spawn system
fix: correct tile unlock logic
refactor: reorganize script namespaces
docs: update architecture document
overwrite: sync Unity structure
```

타입

| 타입        | 의미        |
| --------- | --------- |
| feat      | 기능 추가     |
| fix       | 버그 수정     |
| refactor  | 구조 변경     |
| docs      | 문서 수정     |
| overwrite | 구조 전체 동기화 |

---

# 12. 표준 작업 절차

개발 흐름

## 1. WoodLand3D에서 개발

```
씬 수정
스크립트 수정
문서 수정
```

---

## 2. CLEAN_EXPORT 생성

PowerShell

```
.\Export-Clean.ps1
```

---

## 3. Cursor에게 Git 작업 요청

Cursor는 다음 순서로 실행합니다.

```
cd CLEAN_EXPORT
git pull origin main
git status
git add .
git commit -m "message"
git push origin main
```

---

# 13. 커밋 전 확인 규칙

Cursor는 커밋 전에 반드시 다음을 확인합니다.

```
git status
```

다음 파일이 보이면 **커밋을 중단합니다.**

```
*.fbx
*.png
*.jpg
*.wav
*.mp3
*.ogg
*.psd
*.blend
```

또는

```
Assets/Art
Assets/Audio
Assets/Models
Assets/Textures
Assets/Materials
Assets/Prefabs
```

---

# 14. 저장소 기준 커밋

커밋 `9d9bf6c` 이후 저장소 기준은 다음과 같습니다.

```
Assets/
 ├ Scripts
 ├ Scenes
 └ Docs

Packages
ProjectSettings
```

이 구조는 **프로젝트 기준 구조이며 변경하지 않습니다.**

---

# 15. 요약

개발자는 다음 작업만 수행합니다.

```
1. WoodLand3D에서 개발
2. Export-Clean.ps1 실행
3. Cursor에게 Git push 요청
```

Cursor는 다음 작업을 수행합니다.

```
cd CLEAN_EXPORT
git pull origin main
git status
git add .
git commit
git push origin main
```

---

# 마지막 규칙

GameProject 저장소는

**Unity 프로젝트의 “경량 코드 + 씬 + 문서 백업 저장소”입니다.**

에셋, 바이너리, 외부 리소스는 **절대 포함하지 않습니다.**

---

## 추천 Cursor 사용 문장

Git 작업을 요청할 때 다음 문장을 함께 전달합니다.

```
Follow Assets/Docs/16_GIT_WORKFLOW.md strictly.
Run Export-Clean.ps1 first, then commit and push from CLEAN_EXPORT only.
Do not use merge, rebase, or force push.
```

---

이 문서를 기준으로 하면

* Git merge 거의 안 생김
* 저장소 구조 깨질 가능성 없음
* Cursor가 Git 작업해도 안전
* Unity 프로젝트와 GitHub 역할 분리 유지

된다.

---

