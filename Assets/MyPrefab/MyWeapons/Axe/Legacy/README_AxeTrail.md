# 도끼 궤적 이펙트 (AxeTrailEffect)

## 사용법

1. **도끼 프리팹** (Axe01_White / Blue / Red / Black)을 연다.
2. **루트 오브젝트** 또는 **날이 붙은 자식**에 **AxeTrailEffect** 컴포넌트 추가.
   - Add Component → **AxeTrailEffect** (Trail Renderer가 없으면 자동 추가됨).
3. **Trail Color**를 도끼 색에 맞게 설정:
   - White → 흰색 (1, 1, 1)
   - Blue → 파랑 (0.2, 0.5, 1)
   - Red → 빨강 (1, 0.2, 0.2)
   - Black → 짙은 회색 (0.2, 0.2, 0.2)
4. (선택) **Trail Material**에 Unlit 계열 머티리얼 할당.  
   비워 두면 스크립트가 기본 쉐이더로 머티리얼을 만들어 씀.

## 옵션

- **Time**: 궤적이 남는 시간 (0.2초 권장).
- **Start Width / End Width**: 궤적 두께 (앞쪽/뒤쪽).

무기를 숨길 때(OnDisable) 궤적이 자동으로 Clear 되므로, 다음에 휘두를 때 깔끔하게 나옵니다.
