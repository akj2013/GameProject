using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// 아이템 수집 컨트롤러. 실제 드롭된 통나무를 하나의 품목으로 합쳐 갯수를 ItemCollectorPanel에 표시합니다.
/// </summary>
public class ItemCollectorController : MonoBehaviour
{
    public static ItemCollectorController Instance { get; private set; }

    /// <summary>통나무는 나무 종류와 관계없이 동일 품목으로 합쳐서 표시할 때 사용하는 키</summary>
    public const string LogItemKey = "Log";

    [Tooltip("아이템 수집 패널 프리팹 (ItemCollectorPanel)")]
    public GameObject itemCollectorPanelPrefab;
    [Tooltip("패널들을 담을 컨테이너")]
    public Transform container;
    [Tooltip("패널 간 간격")]
    public float panelSpacing = 20f;

    private Dictionary<string, int> itemCounts = new Dictionary<string, int>();
    private Dictionary<string, int> itemPrices = new Dictionary<string, int>();
    private Dictionary<string, ItemCollectorPanel> itemPanels = new Dictionary<string, ItemCollectorPanel>();
    private Dictionary<string, Texture> itemTextures = new Dictionary<string, Texture>();
    private List<string> panelOrder = new List<string>();

    /// <summary>수집 데이터(갯수·종류)가 바뀌었을 때 발생. 인벤토리 등 UI 갱신용.</summary>
    public event Action OnDataChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        if (container == null)
            container = transform;
    }

    /// <summary>
    /// 통나무(아이템) 수집 시 호출. 실제 드롭된 통나무는 모두 동일 품목(Log)으로 합쳐서 갯수만 증가시킵니다.
    /// </summary>
    /// <param name="itemKey">품목 키. 통나무는 ItemCollectorController.LogItemKey 사용 시 하나로 합쳐짐</param>
    /// <param name="iconTexture">통나무(Log) 프리팹의 RawImage 텍스처 (ItemCollectorPanel LogImage에 표시)</param>
    /// <param name="price">통나무 1개당 가격 (0이면 기존/기본값 유지, 정산 시 사용)</param>
    public void AddLogCount(string itemKey, Texture iconTexture = null, int price = 0)
    {
        if (string.IsNullOrEmpty(itemKey)) return;

        if (iconTexture != null)
            itemTextures[itemKey] = iconTexture;
        if (price > 0)
            itemPrices[itemKey] = price;

        int count = GetCount(itemKey) + 1;
        itemCounts[itemKey] = count;

        if (!itemPanels.TryGetValue(itemKey, out ItemCollectorPanel panel))
        {
            Texture tex = itemTextures.TryGetValue(itemKey, out var t) ? t : null;
            panel = CreatePanelForItem(itemKey, count, tex);
            if (panel != null)
                itemPanels[itemKey] = panel;
        }
        else
        {
            panel.SetCount(count);
        }
        OnDataChanged?.Invoke();
    }

    /// <summary>
    /// 지정한 아이템의 수집 갯수를 반환합니다.
    /// </summary>
    public int GetCount(string itemKey)
    {
        return itemCounts.TryGetValue(itemKey, out int c) ? c : 0;
    }

    /// <summary>수집된 통나무 총 갯수 (모든 종류 합계). 스택 UI 등 표시용.</summary>
    public int GetTotalCount()
    {
        int sum = 0;
        foreach (var c in itemCounts.Values) sum += c;
        return sum;
    }

    /// <summary>
    /// 지정한 아이템의 갯수를 설정합니다. (예: 판매 시 PopLog 후 호출)
    /// </summary>
    public void SetCount(string itemKey, int count)
    {
        if (string.IsNullOrEmpty(itemKey)) return;
        count = Mathf.Max(0, count);
        itemCounts[itemKey] = count;

        if (itemPanels.TryGetValue(itemKey, out ItemCollectorPanel panel))
            panel.SetCount(count);
        OnDataChanged?.Invoke();
    }

    /// <summary>
    /// 지정한 아이템의 단가를 반환합니다. (정산 시 사용, 없으면 1)
    /// </summary>
    public int GetPrice(string itemKey)
    {
        return itemPrices.TryGetValue(itemKey, out int p) ? p : 1;
    }

    /// <summary>
    /// 표시 순서대로 아이템 키 목록을 반환합니다. (인벤토리 등에서 동일 순서로 표시할 때 사용)
    /// </summary>
    public List<string> GetOrderedItemKeys()
    {
        return new List<string>(panelOrder);
    }

    /// <summary>
    /// 지정한 아이템의 아이콘 텍스처를 반환합니다. (인벤토리 패널 표시용)
    /// </summary>
    public Texture GetItemTexture(string itemKey)
    {
        return itemTextures.TryGetValue(itemKey, out var t) ? t : null;
    }

    /// <summary>
    /// 필요 자원이 모두 충분할 때만 소모하고 true 반환. 부족하면 false (소모 없음).
    /// 레벨업 등에서 사용.
    /// </summary>
    public bool TryConsumeResources(List<(string itemKey, int count)> required)
    {
        if (required == null) return true;
        foreach (var r in required)
        {
            if (GetCount(r.Item1) < r.Item2)
                return false;
        }
        foreach (var r in required)
            SetCount(r.Item1, GetCount(r.Item1) - r.Item2);
        return true;
    }

    /// <summary>
    /// HOME 버튼 정산: 위에서부터 순서대로 각 패널의 (갯수×가격)만큼 골드 추가, 플로팅 골드 연출 후 해당 패널 제거. 완료 시 onComplete 호출.
    /// </summary>
    public void RunSettlement(Action onComplete)
    {
        StartCoroutine(RunSettlementCoroutine(onComplete));
    }

    IEnumerator RunSettlementCoroutine(Action onComplete)
    {
        var fx = UnityEngine.Object.FindFirstObjectByType<FloatingCoinFx>();
        if (fx == null)
        {
            // 플로팅 없이 즉시 정산
            var snapshot = new List<(string key, ItemCollectorPanel panel, int totalGold)>();
            foreach (string key in panelOrder)
            {
                if (!itemPanels.TryGetValue(key, out var panel) || panel == null) continue;
                int count = GetCount(key);
                if (count <= 0) continue;
                int price = GetPrice(key);
                snapshot.Add((key, panel, count * price));
            }
            foreach (var t in snapshot)
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.AddGold(t.totalGold);
                RemovePanel(t.key);
            }
            onComplete?.Invoke();
            yield break;
        }

        // 스냅샷으로 순서 고정 (위에서부터)
        var list = new List<(string key, ItemCollectorPanel panel, int totalGold)>();
        foreach (string key in panelOrder)
        {
            if (!itemPanels.TryGetValue(key, out var panel) || panel == null) continue;
            int count = GetCount(key);
            if (count <= 0) continue;
            int price = GetPrice(key);
            list.Add((key, panel, count * price));
        }

        if (list.Count == 0)
        {
            onComplete?.Invoke();
            yield break;
        }

        bool done = false;
        int index = 0;

        void Next()
        {
            if (index >= list.Count)
            {
                if (!done) { done = true; onComplete?.Invoke(); }
                return;
            }
            var t = list[index++];
            fx.PlayFromTo(t.panel.GetComponent<RectTransform>(), t.totalGold, () =>
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.AddGold(t.totalGold);
                RemovePanel(t.key);
                Next();
            });
        }

        Next();
        while (!done)
            yield return null;
    }

    /// <summary>
    /// 단일 패널 제거 (정산 완료 후 호출)
    /// </summary>
    public void RemovePanel(string itemKey)
    {
        if (string.IsNullOrEmpty(itemKey)) return;
        if (itemPanels.TryGetValue(itemKey, out var panel) && panel != null && panel.gameObject != null)
            Destroy(panel.gameObject);
        itemPanels.Remove(itemKey);
        itemCounts.Remove(itemKey);
        itemTextures.Remove(itemKey);
        itemPrices.Remove(itemKey);
        panelOrder.Remove(itemKey);
        OnDataChanged?.Invoke();
    }

    ItemCollectorPanel CreatePanelForItem(string itemKey, int count, Texture iconTexture = null)
    {
        if (itemCollectorPanelPrefab == null || container == null) return null;

        GameObject go = Instantiate(itemCollectorPanelPrefab, container);
        RectTransform rect = go.GetComponent<RectTransform>();
        if (rect != null)
        {
            panelOrder.Add(itemKey);
            rect.anchoredPosition = new Vector2(0, -(panelOrder.Count - 1) * (rect.sizeDelta.y + panelSpacing));
        }

        ItemCollectorPanel panel = go.GetComponent<ItemCollectorPanel>();
        if (panel == null)
            panel = go.AddComponent<ItemCollectorPanel>();
        Texture tex = iconTexture ?? (itemTextures.TryGetValue(itemKey, out var t) ? t : null);
        panel.SetItem(itemKey, count, tex);
        return panel;
    }

    /// <summary>
    /// 아이템 수집 패널 추가 (레거시 호환). 해당 아이템 갯수를 설정하고 패널을 표시합니다.
    /// </summary>
    public void AddItemPanel(string itemName, int count)
    {
        if (string.IsNullOrEmpty(itemName)) return;
        itemCounts[itemName] = count;
        Texture tex = itemTextures.TryGetValue(itemName, out var t) ? t : null;
        if (!itemPanels.TryGetValue(itemName, out ItemCollectorPanel panel))
            panel = CreatePanelForItem(itemName, count, tex);
        if (panel != null)
        {
            if (!itemPanels.ContainsKey(itemName))
                itemPanels[itemName] = panel;
            panel.SetCount(count);
        }
        OnDataChanged?.Invoke();
    }

    /// <summary>
    /// 모든 패널 제거
    /// </summary>
    public void ClearAllPanels()
    {
        foreach (var kv in itemPanels)
        {
            if (kv.Value != null && kv.Value.gameObject != null)
                Destroy(kv.Value.gameObject);
        }
        itemPanels.Clear();
        itemCounts.Clear();
        itemTextures.Clear();
        itemPrices.Clear();
        panelOrder.Clear();
        OnDataChanged?.Invoke();
    }
}
