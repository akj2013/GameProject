# WoodLand3D 코드 스타일 가이드

이 프로젝트에서 사용하는 C# / Unity 스크립트 작성 규칙입니다.

---

## 1. 네임스페이스 규칙

- **WoodLand3D.Gameplay** – 플레이어 인벤토리 등 게임플레이 핵심
- **WoodLand3D.Tiles** – 타일 그리드, 언락, 타일 컨트롤러
- **WoodLand3D.UI** – UI 패널, HUD, 플로팅 텍스트 등
- **WoodLand3D.CameraSystems** – 카메라 추적/포커스
- **WoodLand3D.Core.Resources** – 자원 타입, 노드, 스폰, 이벤트

기존 네임스페이스 구조를 유지하며, 새 스크립트는 위와 같은 역할별 네임스페이스에 맞춥니다.

---

## 2. 스크립트 길이 제한

- **한 스크립트 최대 500라인**을 원칙으로 합니다.
- 500라인을 초과하면 기능별로 클래스를 분리합니다.
  - 예: ResourceSystem → ResourceNode, ResourceSpawner, ResourceInventory

---

## 3. SerializeField 사용 규칙

- Inspector에서 할당할 변수는 `[SerializeField]`를 사용합니다.
- **모든 SerializeField에는 `[Tooltip("한글 설명")]`을 함께 붙입니다.**

예시:

```csharp
[SerializeField, Tooltip("플레이어의 이동 속도")]
private float moveSpeed = 5f;

[SerializeField, Tooltip("자원 채집 시 획득량")]
private int resourceGainAmount = 3;
```

---

## 4. Tooltip 사용 규칙

- SerializeField 변수의 용도를 한 문장으로 설명합니다.
- 툴팁만으로도 Inspector에서 의미를 파악할 수 있게 작성합니다.

---

## 5. 주석 작성 규칙

### 클래스 상단

- **반드시** `<summary>` 한글 주석으로 스크립트의 역할을 설명합니다.

```csharp
/// <summary>
/// 플레이어의 자원 인벤토리를 관리하는 스크립트.
/// 골드와 각종 자원의 획득, 사용, 조회 기능을 담당한다.
/// </summary>
public class PlayerInventory : MonoBehaviour
```

### public 메서드

- 역할이 명확하지 않을 경우 `<summary>` 한글 주석을 추가합니다.

```csharp
/// <summary>
/// 플레이어에게 자원을 추가한다.
/// </summary>
public void AddResource(ResourceType type, int amount)
```

### private 메서드

- 복잡한 로직일 경우 한 줄 한글 주석으로 목적을 적습니다.

---

## 6. 스크립트 구조 순서

다음 순서로 블록을 정리합니다.

1. **Summary** – 클래스 상단 `<summary>` 주석  
2. **Namespace** – namespace 선언  
3. **Using** – using 문  
4. **Fields** – [Header] + SerializeField / private 필드  
5. **Unity Events** – Awake, OnEnable, Start, Update, OnDisable 등  
6. **Public Methods** – 외부에서 호출하는 API  
7. **Private Methods** – 내부 로직  
8. **Events** – event 선언이 별도 블록일 경우

---

## 7. Inspector 친화적 코드

- 관련 필드는 `[Header("한글 라벨")]`로 그룹 짓습니다.

예시:

```csharp
[Header("플레이어 설정")]
[SerializeField, Tooltip("플레이어 이동 속도")]
private float moveSpeed;

[Header("자원 설정")]
[SerializeField, Tooltip("나무 채집 시 기본 획득량")]
private int treeGain = 3;
```

---

## 8. 이벤트 사용 규칙

- C# event는 `On~Changed`, `On~Gained` 등 의미가 드러나는 이름을 사용합니다.
- 정적 이벤트 허브는 `Raise(...)` 메서드로만 발동하고, 외부에서는 구독만 하도록 합니다.
- 이벤트 파라미터는 (필요 시) XML 주석으로 설명합니다.

---

## 9. 기존 기능 보존

리팩터링 시 다음은 **동작과 시그니처를 변경하지 않습니다**.

- 타일 시스템 (타일 언락, 그리드, 저장/로드)
- 리스폰 시스템
- 플레이어 채집 시스템 (E키 채집)
- Unity Inspector에 연결된 참조 (필드 이름 변경 금지)

**구조 정리 + 주석·Tooltip·Header 추가**만 수행합니다.

---

## 10. 요약 체크리스트

| 항목 | 적용 |
|------|------|
| 클래스 상단 한글 summary | 필수 |
| SerializeField + Tooltip | 필수 |
| public 메서드 summary | 역할이 불명확할 때 |
| [Header]로 필드 그룹화 | 권장 |
| 스크립트 500라인 이하 | 초과 시 분리 |
| 네임스페이스/클래스명 변경 | 하지 않음 |
