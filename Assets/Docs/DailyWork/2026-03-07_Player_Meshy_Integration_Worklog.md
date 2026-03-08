# 2026-03-07 작업일지 — Meshy 플레이어 적용 및 자동 채집 애니메이션 연동

---

## 1. 오늘 작업 목표

- Meshy로 남성 기본 바디 제작
- Unity에 FBX/애니메이션/텍스처 적용
- Animator Controller 연결 및 이동/공격/채집 애니메이션 테스트
- Auto Harvest를 "애니 1회 = 피해 1회 = 획득 1회" 구조로 수정

---

## 2. 오늘 실제 진행한 작업

- 컨셉 이미지 생성 및 Meshy 투입용 기본 바디 정리
- 초기 기본 바디에서 허리끈/옷깃이 손에 딸려오는 리깅 문제 확인
- 기본 복장을 단순화한 재시도 버전으로 변경
- Meshy 리메시(약 10k tris, 사각형 면) 진행
- 리깅 테스트 및 런 애니메이션 확인
- 텍스처 생성 및 Unity 임포트
- Animator Controller에 Idle / Walk / Run / Axe / PickAxe 연결
- Walk/Run 정상 작동 확인
- Attack/Mine은 수동 트리거로 정상, 실제 자동 채집 코드에서 트리거 누락 확인
- PlayerMover Animator 연동 및 AutoHarvest 애니메이션 종료 폴백 로직 반영
- 기존 틱 기반 피해 구조가 애니보다 먼저 자원을 파괴하는 문제 확인
- 애니메이션 이벤트 기반 피해 구조로 리팩터링 방향 확정
- OnHarvestHitEvent / OnHarvestAnimationFinished 기반 구조로 수정 반영
- 최종적으로 자동 채집/도구 전환/애니메이션 연동 정상 동작 확인

---

## 3. 발생했던 문제와 해결

### 리깅 시 양손 공격에서 옷깃/소매가 손에 끌려감

- **원인**: 기본 바디 의상 구조가 자동 리깅에 불리함
- **해결**: 허리끈 돌출 제거, 소매 단순화, A포즈 강화, 손/발 단순화한 기본 바디로 재시도

### Idle 한 번만 재생되고 멈춤

- **원인**: Idle Loop 설정 필요
- **해결**: Idle 클립 루프 설정 점검

### Walk/Run이 안 나옴

- **원인**: Animator에 Idle→Walk 전이 누락 + PlayerMover에서 Speed 파라미터 미설정
- **해결**: Animator 트랜지션 보완, PlayerMover에서 `Animator.SetFloat("Speed", ...)` 반영

### Attack/Mine 애니가 실제 자동 채집 중 안 나옴

- **원인**: 코드에서 Attack/Mine Trigger 호출 누락
- **해결**: 자동 채집 코드 흐름에서 Animator 트리거 연결

### 애니메이션 1회가 끝나기 전에 나무가 먼저 쓰러짐

- **원인**: harvestInterval 기반 피해 적용이 애니보다 먼저 반복 실행됨
- **해결**: 피해/획득/VFX/UI를 애니메이션 이벤트 시점에만 적용하는 구조로 변경

---

## 4. 오늘 기준 확정된 설계/규칙

- 기본 바디는 "최종 의상"이 아니라 "리깅 친화형 베이스 바디"로 간다
- 의상은 기본 바디 위에 별도 파츠 메쉬로 얹는 구조로 간다
- 이동은 코드 기반, Animator는 Root Motion OFF 기준
- 자동 채집은 이동 입력만 받고, 도구 스왑/공격/채광/수확은 자동 처리
- 최종 채집 구조는 "애니 1회 = 피해 1회 = 획득 1회"
- Attack = 도끼 계열, Mine = 곡괭이 계열 트리거로 유지

---

## 5. 오늘 산출물 / 반영 파일

- Meshy 기본 바디 FBX 및 애니메이션 FBX
- AC_Player_Base Animator Controller 연결
- PlayerMover Animator 연동 수정
- PlayerAutoHarvest 애니메이션 이벤트 기반 구조 수정
- Player_Animator_Setup_Report.md
- AutoHarvest_Design_Deliverables.md

---

## 6. 다음 작업 우선순위

- Axe / PickAxe 애니메이션 이벤트 위치 최종 조정
- 나무 / 광석 / 작물별 타격 이벤트 시점 통일
- 획득 UI / 자원 튀어나오기 / VFX를 OnHarvestHitEvent에 묶기
- 여성 기본 바디 제작
- 헤어 파츠 제작
- 농부 / 광부 / 사무라이 / 임금 의상 파츠 제작

---

## 7. 짧은 회고

- 오늘 가장 큰 성과는 Meshy 캐릭터를 실제 Player에 적용하고, 이동/채집/애니메이션 구조를 한 단계 현실적인 게임 구조로 바꾼 점
- 실패한 시도(과한 기본복 디테일, 틱 기반 피해)도 왜 안 되는지 원인을 확인했으므로 의미 있는 진전

---

## 8. 관련 문서

- [06_CHARACTER_DESIGN.md](../06_CHARACTER_DESIGN.md)
- [Cursor_Helps/AutoHarvest_Design_Deliverables.md](../Cursor_Helps/AutoHarvest_Design_Deliverables.md)
- [Cursor_Helps/Player_Animator_Setup_Report.md](../Cursor_Helps/Player_Animator_Setup_Report.md)
