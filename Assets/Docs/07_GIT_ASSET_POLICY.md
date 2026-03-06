# Git Asset Policy

GitHub에는 다음 파일만 업로드한다.

## 허용

- Scripts
- Scenes
- Prefabs
- ProjectSettings
- Packages
- Docs

## 업로드 금지

Asset Store 에셋

예:

Assets/Conquerror
Assets/PurePoly
Assets/ToolsPack
Assets/TextMesh Pro
Assets/MCPForUnity

## 이유

에셋 라이선스 문제 방지
Git 용량 관리


## Repository Strategy

The public repository contains only:

- Source code
- Project settings
- Documentation

The following are intentionally excluded:

- 3D models
- Textures
- Audio files
- Asset Store content
- Legacy prototype assets

This ensures:

- No license violations
- Smaller repository size
- Clean open-source code structure

## 운영 메모

- 에셋스토어/외부 리소스를 추가할 때는, 먼저 Docs 쪽에 출처와 라이선스를 메모해 두고, Git에는 절대 원본을 올리지 않는다.
- 코드/설정/문서만 포함된 \"클린 리포\"를 유지하고, 실제 아트/사운드는 로컬 또는 별도의 비공개 저장소에서 관리한다.
- 새 팀원이 합류했을 때, 이 문서를 첫 onboarding 자료로 삼아 Git 사용 원칙을 빠르게 공유한다.