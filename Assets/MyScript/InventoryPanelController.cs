using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 인벤토리 패널: Btn_Close 클릭 또는 패널 외부 클릭 시 닫힘.
/// ScrollView Content에 ItemCollectorController의 자원 종류·갯수를 ItemCollectorPanel로 순서대로 표시합니다.
/// </summary>
public class InventoryPanelController : MonoBehaviour
{
    [Header("패널 닫기")]
    [Tooltip("닫기 버튼 (Btn_Close)")]
    public Button closeButton;
    [Tooltip("비활성화할 패널 루트. 이 RectTransform 밖을 클릭하면 패널 비활성화. 비어 있으면 이 오브젝트")]
    public GameObject panelRoot;

    [Header("인벤토리 리스트")]
    [Tooltip("스크롤뷰의 Content (ItemCollectorPanel들이 채워질 부모)")]
    public Transform content;
    [Tooltip("ItemCollectorPanel 프리팹 (ItemCollectorController와 동일한 프리팹 권장)")]
    public GameObject itemCollectorPanelPrefab;
    [Tooltip("가로 열 개수 (X열 배치)")]
    public int columns = 3;
    [Tooltip("한 칸(셀) 크기 (가로, 세로). GridLayoutGroup에 사용")]
    public Vector2 cellSize = new Vector2(100f, 100f);
    [Tooltip("항목 사이 간격 (가로, 세로)")]
    public Vector2 spacing = new Vector2(10f, 10f);

    [Header("참조")]
    [Tooltip("수집 데이터 소스. 비어 있으면 ItemCollectorController.Instance 사용")]
    public ItemCollectorController itemCollector;

    void Start()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);

        if (itemCollector == null)
            itemCollector = ItemCollectorController.Instance;

        if (itemCollector != null)
            itemCollector.OnDataChanged += OnCollectorDataChanged;
    }

    void OnDestroy()
    {
        if (itemCollector != null)
            itemCollector.OnDataChanged -= OnCollectorDataChanged;
    }

    void OnCollectorDataChanged()
    {
        if (panelRoot != null && panelRoot.activeSelf)
            RefreshInventoryContent();
    }

    void OnEnable()
    {
        RefreshInventoryContent();
    }

    void Update()
    {
        if (panelRoot == null || !panelRoot.activeSelf) return;
        if (!Input.GetMouseButtonDown(0)) return;

        var rt = panelRoot.GetComponent<RectTransform>();
        if (rt == null) return;

        var canvas = panelRoot.GetComponentInParent<Canvas>();
        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
        if (!RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition, cam))
            ClosePanel();
    }

    public void ClosePanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    /// <summary>
    /// ItemCollectorController의 자원을 순서대로 Content에 ItemCollectorPanel로 채웁니다.
    /// </summary>
    void RefreshInventoryContent()
    {
        if (content == null || itemCollectorPanelPrefab == null)
            return;

        if (itemCollector == null)
            itemCollector = ItemCollectorController.Instance;
        if (itemCollector == null)
            return;

        EnsureContentLayout();

        foreach (Transform child in content)
        {
            if (child != null && child.gameObject != null)
                Destroy(child.gameObject);
        }

        List<string> orderedKeys = itemCollector.GetOrderedItemKeys();
        for (int i = 0; i < orderedKeys.Count; i++)
        {
            string key = orderedKeys[i];
            int count = itemCollector.GetCount(key);
            if (count <= 0) continue;

            GameObject go = Instantiate(itemCollectorPanelPrefab, content);
            ItemCollectorPanel panel = go.GetComponent<ItemCollectorPanel>();
            if (panel == null)
                panel = go.AddComponent<ItemCollectorPanel>();

            Texture tex = itemCollector.GetItemTexture(key);
            panel.SetItem(key, count, tex);
        }
    }

    void EnsureContentLayout()
    {
        if (content == null) return;

        var contentGo = content.gameObject;
        var contentRt = content.GetComponent<RectTransform>();
        if (contentRt != null)
        {
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
        }

        var vertical = contentGo.GetComponent<VerticalLayoutGroup>();
        if (vertical != null)
            Destroy(vertical);

        var grid = contentGo.GetComponent<GridLayoutGroup>();
        if (grid == null)
            grid = contentGo.AddComponent<GridLayoutGroup>();

        int col = Mathf.Max(1, columns);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = col;
        grid.cellSize = cellSize;
        grid.spacing = spacing;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;

        var fitter = contentGo.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = contentGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scrollRect = content.GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
        {
            scrollRect.vertical = true;
            scrollRect.horizontal = false;
        }
    }
}
