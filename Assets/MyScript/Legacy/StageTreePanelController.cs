using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 스테이지 패널 하나로 통합: Content에 나무 리스트(StageTreePanel 프리팹) 표시, 드롭존에 드래그 앤 드롭, 닫기 버튼/외부 클릭으로 패널 비활성화.
/// </summary>
public class StageTreePanelController : MonoBehaviour
{
    [Header("패널 닫기")]
    [Tooltip("닫기 버튼 (예: Btn_Close)")]
    public Button closeButton;
    [Tooltip("비활성화할 패널 루트 (비어 있으면 이 오브젝트). 이 RectTransform 밖을 클릭하면 패널 비활성화")]
    public GameObject panelRoot;

    [Header("길게 누르기")]
    [Tooltip("이 시간(초) 동안 누르고 있으면 나무 드래그 모드 진입. 그 전에는 스크롤만 됨. 기본 1초")]
    public float holdDurationSeconds = 1f;

    [Header("리스트 (Content)")]
    [Tooltip("나무 리스트가 채워질 ScrollView의 Content (수직 스크롤만 사용)")]
    public Transform content;
    [Tooltip("수직 스크롤바 (Scrollbar Vertical). 비어 있으면 ScrollRect 자식에서 찾음")]
    public Scrollbar verticalScrollbar;
    [Tooltip("리스트 한 칸 프리팹 (StageTreePanel 프리팹, TreeListDragItem + RawImage 있음)")]
    public GameObject stageTreePanelPrefab;
    [Tooltip("리스트 항목 사이 간격")]
    public float listSpacing = 10f;
    [Tooltip("리스트 한 칸 크기 (가로, 세로). 겹치지 않도록 적용")]
    public Vector2 listItemSize = new Vector2(100f, 100f);

    [Header("드롭존")]
    [Tooltip("각 스테이지별 드롭존 - TreeImagePanelSlot (스테이지 1,2,... 순)")]
    public List<TreeImagePanelSlot> dropZones = new List<TreeImagePanelSlot>();

    private List<GameObject> cachedTreePrefabs = new List<GameObject>();
    private Dictionary<int, GameObject> selectedTreesByStage = new Dictionary<int, GameObject>();
    /// <summary>사용자가 슬롯에서 바깥으로 빼서 클리어한 스테이지. Element0는 타일에 유지되지만 슬롯은 빈 상태로 표시.</summary>
    private HashSet<int> userClearedStages = new HashSet<int>();

    void Start()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);

        InitializeTreePrefabsCache();
        RefreshTreeList();
        RefreshDropZoneSlotImages();
    }

    void OnEnable()
    {
        RefreshDropZoneSlotImages();
    }

    public void ClosePanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
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

    void InitializeTreePrefabsCache()
    {
        cachedTreePrefabs.Clear();

        GameObject[] resourcesTrees = Resources.LoadAll<GameObject>("Trees");
        foreach (var tree in resourcesTrees)
        {
            if (tree != null && HasTreeComponent(tree) && HasRawImage(tree))
                cachedTreePrefabs.Add(tree);
        }

#if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/MyPrefab" });
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null && HasTreeComponent(prefab) && HasRawImage(prefab) && !cachedTreePrefabs.Contains(prefab))
                cachedTreePrefabs.Add(prefab);
        }
#endif
    }

    bool HasTreeComponent(GameObject prefab)
    {
        return prefab.GetComponent<TreeManager>() != null;
    }

    bool HasRawImage(GameObject prefab)
    {
        return prefab.GetComponentInChildren<RawImage>(true) != null;
    }

    /// <summary>
    /// Content 안에 StageTreePanel 프리팹으로 나무 리스트 생성. 수직 스크롤만 쓰고 간격/크기 적용.
    /// </summary>
    void RefreshTreeList()
    {
        if (content == null || stageTreePanelPrefab == null)
        {
            if (cachedTreePrefabs.Count > 0 && (content == null || stageTreePanelPrefab == null))
                Debug.LogWarning("StageTreePanelController: Content와 Stage Tree Panel Prefab을 할당하세요.");
            return;
        }

        EnsureContentLayout();

        foreach (Transform child in content)
        {
            if (child != null && child.gameObject != null)
                Destroy(child.gameObject);
        }

        foreach (var treePrefab in cachedTreePrefabs)
        {
            if (treePrefab == null) continue;

            GameObject item = Instantiate(stageTreePanelPrefab, content);
            var itemRt = item.GetComponent<RectTransform>();
            if (itemRt != null)
            {
                itemRt.anchorMin = new Vector2(0.5f, 1f);
                itemRt.anchorMax = new Vector2(0.5f, 1f);
                itemRt.pivot = new Vector2(0.5f, 1f);
                itemRt.sizeDelta = listItemSize;
            }
            var le = item.GetComponent<LayoutElement>();
            if (le == null) le = item.AddComponent<LayoutElement>();
            le.minWidth = listItemSize.x;
            le.minHeight = listItemSize.y;
            le.preferredWidth = listItemSize.x;
            le.preferredHeight = listItemSize.y;

            var raw = item.GetComponent<RawImage>() ?? item.GetComponentInChildren<RawImage>(true);
            if (raw != null)
            {
                var treeRaw = treePrefab.GetComponentInChildren<RawImage>(true);
                if (treeRaw != null && treeRaw.texture != null)
                    raw.texture = treeRaw.texture;
            }

            var drag = item.GetComponent<TreeListDragItem>();
            if (drag == null) drag = item.AddComponent<TreeListDragItem>();
            drag.treePrefab = treePrefab;
        }
    }

    void EnsureContentLayout()
    {
        var contentGo = content.gameObject;
        var contentRt = content.GetComponent<RectTransform>();
        if (contentRt != null)
        {
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
        }

        var layout = contentGo.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
            layout = contentGo.AddComponent<VerticalLayoutGroup>();

        layout.spacing = listSpacing;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

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
            var bar = verticalScrollbar != null ? verticalScrollbar : scrollRect.GetComponentInChildren<Scrollbar>();
            if (bar != null)
                scrollRect.verticalScrollbar = bar;
        }
    }

    public void AssignTreeToStage(int stageNumber, GameObject treePrefab)
    {
        if (treePrefab != null && HasTreeComponent(treePrefab))
        {
            selectedTreesByStage[stageNumber] = treePrefab;
            userClearedStages.Remove(stageNumber);
        }
        else
        {
            selectedTreesByStage.Remove(stageNumber);
            userClearedStages.Add(stageNumber);
        }
    }

    /// <summary>
    /// 드래그 앤 드롭으로 슬롯에 나무를 놓았을 때: 스테이지 할당 + TreeImagePanel의 RawImage에 나무 이미지 + 타일 리스폰
    /// </summary>
    public void AssignTreeToStageAndUpdateSlot(int stageNumber, GameObject treePrefab, TreeImagePanelSlot slot)
    {
        if (treePrefab == null) return;
        AssignTreeToStage(stageNumber, treePrefab);

        if (slot != null)
        {
            var treeRaw = treePrefab.GetComponentInChildren<RawImage>(true);
            if (treeRaw != null && treeRaw.texture != null)
                slot.SetSlotTexture(treeRaw.texture);

            if (slot.targetTileConfig != null)
                slot.targetTileConfig.AssignTreeAndReplace(treePrefab);
        }
    }

    public GameObject GetTreeForStage(int stageNumber)
    {
        return selectedTreesByStage.ContainsKey(stageNumber) ? selectedTreesByStage[stageNumber] : null;
    }

    public int GetTreeLogPrice(GameObject treePrefab)
    {
        if (treePrefab == null) return 1;
        var tm = treePrefab.GetComponent<TreeManager>();
        return tm != null ? tm.logPrice : 1;
    }

    /// <summary>
    /// 터치를 따라 움직이는 플로팅 이미지 생성. 나무 프리팹의 RawImage와 동일한 크기.
    /// 호출측에서 OnDrag 시 position 설정, 드롭 시 Destroy.
    /// </summary>
    public static GameObject CreateFloatingTreeImage(Transform parentForCanvas, Texture texture, float sizeIfNoTexture = 100f)
    {
        if (texture == null && parentForCanvas == null) return null;
        var canvas = parentForCanvas != null ? parentForCanvas.GetComponentInParent<Canvas>() : null;
        if (canvas == null) return null;

        float w = sizeIfNoTexture, h = sizeIfNoTexture;
        if (texture != null)
        {
            w = texture.width;
            h = texture.height;
        }

        var go = new GameObject("FloatingTreeImage");
        go.transform.SetParent(canvas.transform, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = Vector2.zero;

        var raw = go.AddComponent<RawImage>();
        raw.texture = texture;
        raw.raycastTarget = false;

        return go;
    }

    /// <summary>
    /// 플로팅 이미지 위치를 스크린 좌표로 설정 (Canvas 스페이스로 변환)
    /// </summary>
    public static void SetFloatingImagePosition(GameObject floatingImage, Vector2 screenPosition)
    {
        if (floatingImage == null) return;
        var rt = floatingImage.GetComponent<RectTransform>();
        if (rt == null) return;
        var canvas = floatingImage.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rt.parent as RectTransform, screenPosition, canvas.worldCamera, out var local);
        rt.anchoredPosition = local;
    }

    /// <summary>
    /// 드롭존 슬롯들의 초기 이미지를 해당 타일 현재 나무로 갱신
    /// </summary>
    public void RefreshDropZoneSlotImages()
    {
        foreach (var slot in dropZones)
        {
            if (slot == null || slot.targetTileConfig == null) continue;
            if (userClearedStages.Contains(slot.stageNumber))
            {
                slot.ClearSlot();
                continue;
            }
            var prefab = GetTreeForStage(slot.stageNumber);
            if (prefab == null && slot.targetTileConfig.SpawnableTreePrefabs != null && slot.targetTileConfig.SpawnableTreePrefabs.Count > 0)
                prefab = slot.targetTileConfig.SpawnableTreePrefabs[0];
            if (prefab != null)
            {
                var raw = prefab.GetComponentInChildren<RawImage>(true);
                if (raw != null && raw.texture != null)
                    slot.SetSlotTexture(raw.texture);
            }
            else
                slot.ClearSlot();
        }
    }
}
