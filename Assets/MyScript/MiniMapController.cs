using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 미니맵 컨트롤러 - 플레이어가 들어간 스테이지에 로케이션 스프라이트 표시,
/// 스테이지별 나무 존재 여부(활성/비활성)로 패널 색상: 활성 나무 있으면 기본색, 없으면 All Trees Cut Color.
/// </summary>
public class MiniMapController : MonoBehaviour
{
    [Tooltip("미니맵 패널 (미니맵 배경/영역)")]
    public RectTransform miniMapPanel;

    [Header("스테이지 표시")]
    [Tooltip("스테이지별 패널 (미니맵 위 아이콘/영역). 인덱스 순서 = stageTiles, stageImages 순서")]
    public List<RectTransform> stagePanels = new List<RectTransform>();
    [Tooltip("스테이지 타일 Transform 목록 (각 Transform에 TileStageConfig 부착). stagePanels[i], stageTiles[i], stageImages[i]가 한 쌍")]
    public List<Transform> stageTiles = new List<Transform>();
    [Tooltip("스테이지별 TreeImage의 RawImage (StagePanel_x > TreeImagePanel > TreeImage). stagePanels[i], stageTiles[i], stageImages[i]가 한 쌍")]
    public List<RawImage> stageImages = new List<RawImage>();

    [Header("색상")]
    [Tooltip("기본 스테이지 색상 (해당 스테이지에 활성 나무가 있을 때)")]
    public Color defaultColor = Color.white;
    [Tooltip("해당 스테이지에 활성 나무가 없을 때 색상 (전부 비활성/베었음)")]
    public Color allTreesCutColor = new Color(0f, 1f, 0f, 1f);

    [Header("플레이어 위치 (로케이션 마커)")]
    [Tooltip("플레이어가 해당 스테이지에 있을 때 해당 StagePanel_1 등 자식으로 표시할 스프라이트")]
    public Sprite playerLocationSprite;
    [Tooltip("로케이션 스프라이트를 표시할 UI (비어 있으면 런타임 생성, 해당 스테이지 패널 자식으로 생성)")]
    public RectTransform locationMarker;
    [Tooltip("로케이션 마커 스케일 (1 = 24x24 기준). 게임 시작 시 한 번만 적용")]
    public float locationMarkerScale = 1f;
    [Tooltip("플레이어 Transform (비어 있으면 Tag Player / PlayerCollector로 찾음)")]
    public Transform playerTransform;

    float _colorRefreshInterval = 0.2f;
    float _lastColorRefresh;
    List<TileStageConfig> _stageTileConfigsCache = new List<TileStageConfig>();

    void Start()
    {
        if (miniMapPanel == null)
            miniMapPanel = GetComponent<RectTransform>();
        EnsurePlayerRef();
        EnsureLocationMarker();
        CacheStageTileConfigs();
    }

    void CacheStageTileConfigs()
    {
        _stageTileConfigsCache.Clear();
        for (int i = 0; i < stageTiles.Count; i++)
        {
            var t = stageTiles[i];
            _stageTileConfigsCache.Add(t != null ? t.GetComponent<TileStageConfig>() : null);
        }
    }

    void EnsurePlayerRef()
    {
        if (playerTransform != null) return;
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) { playerTransform = go.transform; return; }
        var collector = Object.FindFirstObjectByType<PlayerCollector>();
        if (collector != null) playerTransform = collector.transform;
    }

    void EnsureLocationMarker()
    {
        if (playerLocationSprite == null) return;
        if (locationMarker != null) return;
        // 부모는 나중에 Update에서 현재 스테이지 패널로 설정. 일단 첫 패널에 붙여 생성
        Transform parent = (stagePanels != null && stagePanels.Count > 0 && stagePanels[0] != null)
            ? stagePanels[0] : (miniMapPanel != null ? miniMapPanel : transform);
        var go = new GameObject("MiniMapLocationMarker");
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.localPosition = Vector3.zero;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
        float size = Mathf.Max(1f, 24f * locationMarkerScale);
        rect.sizeDelta = new Vector2(size, size);
        var img = go.AddComponent<Image>();
        img.sprite = playerLocationSprite;
        img.raycastTarget = false;
        locationMarker = rect;
        locationMarker.gameObject.SetActive(false);
    }

    void Update()
    {
        EnsurePlayerRef();
        if (playerTransform == null) return;

        // 1) 플레이어가 들어간 스테이지 인덱스 찾기 → 로케이션 스프라이트를 해당 스테이지 패널에 표시
        int currentStageIndex = GetStageIndexContainingPlayer(playerTransform.position);
        if (locationMarker != null)
        {
            if (currentStageIndex >= 0 && currentStageIndex < stagePanels.Count && stagePanels[currentStageIndex] != null)
            {
                var panel = stagePanels[currentStageIndex];
                if (locationMarker.parent != panel)
                    locationMarker.SetParent(panel, false);
                locationMarker.anchorMin = new Vector2(0.5f, 0.5f);
                locationMarker.anchorMax = new Vector2(0.5f, 0.5f);
                locationMarker.anchoredPosition = Vector2.zero;
                locationMarker.localPosition = Vector3.zero;
                locationMarker.localRotation = Quaternion.identity;
                locationMarker.localScale = Vector3.one;
                locationMarker.gameObject.SetActive(true);
            }
            else
            {
                locationMarker.gameObject.SetActive(false);
            }
        }

        // 2) 주기적으로 스테이지 패널 색상 갱신 (활성 나무 있으면 defaultColor, 없으면 allTreesCutColor)
        if (Time.time - _lastColorRefresh >= _colorRefreshInterval)
        {
            _lastColorRefresh = Time.time;
            RefreshAllStagePanelColors();
        }
    }

    /// <summary>플레이어 월드 위치가 속한 스테이지 인덱스 (stageTiles 순서). 없으면 -1</summary>
    int GetStageIndexContainingPlayer(Vector3 worldPos)
    {
        if (_stageTileConfigsCache.Count != stageTiles.Count)
            CacheStageTileConfigs();
        for (int i = 0; i < _stageTileConfigsCache.Count; i++)
        {
            var tile = _stageTileConfigsCache[i];
            if (tile == null) continue;
            if (tile.GetWorldBounds().Contains(worldPos))
                return i;
        }
        return -1;
    }

    /// <summary>모든 스테이지 패널 색상 갱신 (해당 타일에 활성 나무 있으면 기본색, 없으면 Cut 색)</summary>
    public void RefreshAllStagePanelColors()
    {
        if (_stageTileConfigsCache.Count != stageTiles.Count)
            CacheStageTileConfigs();
        for (int i = 0; i < stagePanels.Count; i++)
        {
            if (stagePanels[i] == null) continue;
            var tile = (i < _stageTileConfigsCache.Count) ? _stageTileConfigsCache[i] : null;
            bool hasActiveTree = tile != null && tile.HasAnyActiveTree();
            RefreshStagePanelColor(i, hasActiveTree);
        }
    }

    /// <summary>
    /// 스테이지 패널 색상 갱신. stageImages[i]의 RawImage 색상 변경. hasActiveTree=true면 defaultColor, false면 allTreesCutColor.
    /// </summary>
    public void RefreshStagePanelColor(int stageIndex, bool hasActiveTree)
    {
        if (stageImages == null || stageIndex < 0 || stageIndex >= stageImages.Count) return;

        var rawImage = stageImages[stageIndex];
        if (rawImage != null)
            rawImage.color = hasActiveTree ? defaultColor : allTreesCutColor;
    }
}
