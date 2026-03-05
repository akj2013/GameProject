using UnityEngine;

/// <summary>
/// 자원 프리팹(젬 등)에 부착하여 ItemCollectorController용 수집 키를 지정.
/// LogItem이 없는 프리팹에서 레벨업 비용/인벤토리 키로 사용.
/// </summary>
public class ResourceItemKey : MonoBehaviour
{
    [Tooltip("ItemCollectorController에서 사용하는 아이템 키 (예: JemRoot)")]
    public string itemKey = "";

    public string GetCollectorKey() => string.IsNullOrEmpty(itemKey) ? gameObject.name : itemKey;
}
