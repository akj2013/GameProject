using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 이미지 기반 나무 노드: 클릭 시 상태 이미지 순환 + 반짝이 이펙트 + "+1" 팝업.
/// Raw Image + Texture2D 사용 → PNG 파일을 임포트 설정 변경 없이(Default) 그대로 할당 가능.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class TreeInteractionNode : MonoBehaviour, IPointerClickHandler
{
    [Header("나무 상태 텍스처 (순서: 풀트리 → 훼손 → 밑둥). PNG를 Default로 임포트 후 여기에 할당")]
    [SerializeField] private Texture2D stateFull;
    [SerializeField] private Texture2D stateDamaged;
    [SerializeField] private Texture2D stateStump;

    [Header("이펙트")]
    [SerializeField, Tooltip("반짝이용 텍스처. 비어 있으면 런타임에 원형으로 생성")]
    private Texture2D sparkleTexture;
    [SerializeField, Tooltip("이펙트/팝업이 생성될 부모 (보통 Canvas 자식 빈 오브젝트)")]
    private RectTransform effectParent;
    [SerializeField, Tooltip("+1 팝업에 쓸 TMP 폰트. 비어 있으면 Unity UI Text 사용")]
    private TMP_FontAsset popupFont;
    [SerializeField] private Color sparkleColor = Color.white;
    [SerializeField] private float sparkleDuration = 0.4f;
    [SerializeField] private float popupDuration = 0.8f;
    [SerializeField] private float popupMoveY = 60f;

    private RawImage _rawImage;
    private int _state;
    private RectTransform _rectTransform;

    private void Awake()
    {
        _rawImage = GetComponent<RawImage>();
        _rectTransform = GetComponent<RectTransform>();
        if (effectParent == null)
            effectParent = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
        ApplyState();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        CycleState();
        ShowSparkle();
        ShowPopup();
    }

    private void CycleState()
    {
        _state = (_state + 1) % 3;
        ApplyState();
    }

    private void ApplyState()
    {
        Texture2D tex = _state switch { 0 => stateFull, 1 => stateDamaged, _ => stateStump };
        if (tex != null && _rawImage != null)
            _rawImage.texture = tex;
    }

    private void ShowSparkle()
    {
        if (effectParent == null) return;
        StartCoroutine(SparkleRoutine());
    }

    private IEnumerator SparkleRoutine()
    {
        var go = new GameObject("Sparkle");
        go.transform.SetParent(effectParent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(80f, 80f);
        rt.anchoredPosition = WorldToEffectLocal(transform.position);

        var raw = go.AddComponent<RawImage>();
        raw.color = sparkleColor;
        raw.texture = sparkleTexture != null ? sparkleTexture : CreateSimpleCircleTexture();
        raw.raycastTarget = false;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / sparkleDuration;
            float scale = Mathf.Lerp(0.4f, 1.2f, t);
            float alpha = 1f - t;
            rt.localScale = Vector3.one * scale;
            raw.color = new Color(sparkleColor.r, sparkleColor.g, sparkleColor.b, alpha);
            yield return null;
        }

        Destroy(go);
    }

    private void ShowPopup()
    {
        if (effectParent == null) return;
        StartCoroutine(PopupRoutine());
    }

    private IEnumerator PopupRoutine()
    {
        var go = new GameObject("PopupText");
        go.transform.SetParent(effectParent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(120f, 50f);
        rt.anchoredPosition = WorldToEffectLocal(transform.position) + Vector2.up * 30f;

        if (popupFont != null)
        {
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = "+1";
            tmp.font = popupFont;
            tmp.fontSize = 32f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            float t = 0f;
            Vector2 startPos = rt.anchoredPosition;
            while (t < 1f)
            {
                t += Time.deltaTime / popupDuration;
                rt.anchoredPosition = startPos + Vector2.up * (popupMoveY * t);
                tmp.alpha = 1f - t;
                yield return null;
            }
        }
        else
        {
            var txt = go.AddComponent<Text>();
            txt.text = "+1";
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 28;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.raycastTarget = false;

            float t = 0f;
            Vector2 startPos = rt.anchoredPosition;
            Color c = txt.color;
            while (t < 1f)
            {
                t += Time.deltaTime / popupDuration;
                rt.anchoredPosition = startPos + Vector2.up * (popupMoveY * t);
                c.a = 1f - t;
                txt.color = c;
                yield return null;
            }
        }

        Destroy(go);
    }

    private Vector2 WorldToEffectLocal(Vector3 worldPos)
    {
        if (effectParent == null) return Vector2.zero;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            effectParent, worldPos, null, out var local);
        return local;
    }

    private static Texture2D CreateSimpleCircleTexture()
    {
        int size = 64;
        var tex = new Texture2D(size, size);
        float r = size * 0.5f;
        float r2 = r * r;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = x - r + 0.5f;
            float dy = y - r + 0.5f;
            float d2 = dx * dx + dy * dy;
            float a = d2 <= r2 ? 1f : 0f;
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        return tex;
    }
}
