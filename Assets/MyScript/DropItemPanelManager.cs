using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// DropItemPanel에 붙여서 사용. 채집 시 나무 위치에 "아이콘 + x2" 팝업을 동적으로 생성해 떠오르게 합니다.
/// 여러 나무를 동시에 채집해도 각각 DropItem이 생성되어 독립적으로 떠오릅니다.
/// </summary>
public class DropItemPanelManager : MonoBehaviour
{
    public static DropItemPanelManager Instance { get; private set; }

    [Header("Drop Item 프리팹")]
    [Tooltip("Instantiate할 DropItem 프리팹. RectTransform + RawImage + 자식 Text_Drop(TMP_Text) 필요")]
    [SerializeField] GameObject dropItemPrefab;

    [Header("애니메이션")]
    [Tooltip("팝업이 위로 떠오르는 시간(초)")]
    [SerializeField, Min(0.1f)] float floatDuration = 1.2f;
    [Tooltip("위로 올라가는 높이(anchoredPosition Y 추가량, 픽셀)")]
    [SerializeField] float floatHeight = 80f;
    [Tooltip("페이드 아웃 시작 시점(0~1). 0.5 = 절반 올라간 뒤부터 페이드")]
    [SerializeField, Range(0f, 1f)] float fadeStartNormalized = 0.4f;
    [Tooltip("페이드 아웃에 걸리는 시간(초)")]
    [SerializeField, Min(0.01f)] float fadeDuration = 0.5f;

    RectTransform _panelRect;
    Canvas _canvas;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[DropItemPanelManager] Instance가 이미 있습니다. 중복 제거 권장.", this);
            return;
        }
        Instance = this;
        _panelRect = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// 월드 위치에 드롭 팝업 하나를 생성해 떠오르게 합니다. 여러 번 호출하면 각각 독립적으로 표시됩니다.
    /// </summary>
    /// <param name="worldPosition">나무(또는 채집 지점) 월드 위치</param>
    /// <param name="iconTexture">팝업에 표시할 아이콘 텍스처 (나무 RawImage 등)</param>
    /// <param name="count">표시할 개수 (예: 3 → "x3")</param>
    public void ShowDrop(Vector3 worldPosition, Texture iconTexture, int count)
    {
        if (dropItemPrefab == null)
        {
            Debug.LogWarning("[DropItemPanelManager] Drop Item 프리팹이 할당되지 않았습니다.", this);
            return;
        }
        if (_panelRect == null) _panelRect = GetComponent<RectTransform>();
        if (_canvas == null) _canvas = GetComponentInParent<Canvas>();

        GameObject go = Instantiate(dropItemPrefab, _panelRect);
        go.SetActive(true);
        RectTransform root = go.GetComponent<RectTransform>();
        if (root == null)
        {
            Destroy(go);
            return;
        }

        // 월드 → 스크린 → 패널 로컬 좌표
        Camera cam = (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay && _canvas.worldCamera != null)
            ? _canvas.worldCamera : Camera.main;
        if (_canvas != null && _canvas.renderMode == RenderMode.WorldSpace)
            Debug.LogWarning("[DropItemPanelManager] 이 패널은 Screen Space 캔버스 아래에 두어야 게임 화면에 보입니다.", _canvas);

        Vector2 screenPoint = cam.WorldToScreenPoint(worldPosition);
        RectTransform parentRT = root.parent as RectTransform;
        if (parentRT != null)
        {
            Camera camForRect = (_canvas != null && _canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : cam;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRT, screenPoint, camForRect, out Vector2 localPoint))
                root.anchoredPosition = localPoint;
        }

        RawImage popupRaw = root.GetComponentInChildren<RawImage>(true);
        if (popupRaw != null && iconTexture != null)
            popupRaw.texture = iconTexture;

        TMP_Text textComp = root.GetComponentInChildren<TMP_Text>(true);
        if (textComp != null)
            textComp.text = "x" + count;

        root.SetAsLastSibling();
        StartCoroutine(FloatUpAndFadeOutThenDestroy(root, worldPosition));
    }

    /// <summary>
    /// 월드 좌표를 현재 카메라 기준으로 패널 로컬 좌표로 변환 (캐릭터/카메라 이동 시 재조정용).
    /// DropItem의 부모가 패널이므로, 변환 대상은 패널(_panelRect) 기준이어야 함.
    /// </summary>
    bool WorldToPanelLocal(Vector3 worldPos, out Vector2 localPoint)
    {
        localPoint = Vector2.zero;
        if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
        if (_panelRect == null) _panelRect = GetComponent<RectTransform>();
        if (_panelRect == null) return false;

        Camera cam = (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay && _canvas.worldCamera != null)
            ? _canvas.worldCamera : Camera.main;
        Vector2 screenPoint = cam.WorldToScreenPoint(worldPos);
        Camera camForRect = (_canvas != null && _canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : cam;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(_panelRect, screenPoint, camForRect, out localPoint);
    }

    IEnumerator FloatUpAndFadeOutThenDestroy(RectTransform root, Vector3 worldPosition)
    {
        float duration = Mathf.Max(0.1f, floatDuration);
        float fadeStart = duration * Mathf.Clamp01(fadeStartNormalized);
        float fadeDur = Mathf.Max(0.01f, fadeDuration);
        Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // 캐릭터/카메라 이동에 따라 월드 위치를 매 프레임 스크린 좌표로 재계산 후, 떠오르는 오프셋 추가
            if (WorldToPanelLocal(worldPosition, out Vector2 baseLocal))
                root.anchoredPosition = baseLocal + new Vector2(0f, floatHeight * t);

            float alpha = 1f;
            if (elapsed >= fadeStart && fadeDur > 0f)
            {
                float fadeT = (elapsed - fadeStart) / fadeDur;
                alpha = 1f - Mathf.Clamp01(fadeT);
            }
            foreach (var g in graphics)
            {
                if (g != null)
                {
                    Color c = g.color;
                    c.a = alpha;
                    g.color = c;
                }
            }
            yield return null;
        }

        Destroy(root.gameObject);
    }
}
