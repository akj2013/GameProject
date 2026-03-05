# Tile Expansion System

맵은 사각 타일 기반으로 구성된다.

## 타일 상태

Locked
- 구름으로 가려짐

Unlocked
- 플레이 가능

## 해제 방법

플레이어가 타일 근처에 가면
해제 UI가 나타난다.

해제 비용:

- 금화
- 자원

## 해제 과정

1. 플레이어 접근
2. 해제 UI 표시
3. 비용 확인
4. 해제 버튼 클릭
5. 구름 애니메이션
6. 타일 활성화

## 타일 구조

Tile
 ├ Locked
 │   └ Cloud
 ├ Unlocked
 │   ├ Resources
 │   └ Buildings
 └ Trigger