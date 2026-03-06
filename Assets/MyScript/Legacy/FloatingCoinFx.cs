using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

/// <summary>
/// 정산 시 ItemCollectorPanel에서 위로 살짝 떠오른 뒤 GoldPanel로 날아가는 플로팅 골드 연출.
/// </summary>
public class FloatingCoinFx : MonoBehaviour
{
    [Tooltip("금화 아이콘 RectTransform (GoldUIStyler에서 자동 설정)")]
    public RectTransform goldCoinIconRect;
    [Tooltip("오버레이 Canvas (GoldUIStyler에서 자동 설정)")]
    public Canvas overlayCanvas;

    [Header("정산 플로팅")]
    [Tooltip("위로 떠오르는 높이 (픽셀)")]
    public float floatUpOffset = 40f;
    [Tooltip("떠오르는 시간")]
    public float floatUpDuration = 0.15f;
    [Tooltip("GoldPanel로 날아가는 시간")]
    public float flyToGoldDuration = 0.35f;
    [Tooltip("플로팅 골드 생성 시 재생할 사운드")]
    public AudioClip floatingGoldSound;

    void Start()
    {
        EnsureRefs();
    }

    void EnsureRefs()
    {
        if (goldCoinIconRect == null)
        {
            var styler = FindFirstObjectByType<GoldUIStyler>();
            if (styler != null)
                goldCoinIconRect = styler.GetGoldCoinIconRect();
        }
        if (overlayCanvas == null)
            overlayCanvas = FindFirstObjectByType<Canvas>();
    }

    /// <summary>
    /// startRect 위치에서 금화를 생성해 위로 살짝 떠오른 뒤 GoldPanel로 날아가고, 완료 시 onComplete 호출.
    /// </summary>
    public void PlayFromTo(RectTransform startRect, int goldAmount, Action onComplete)
    {
        if (startRect == null) { onComplete?.Invoke(); return; }
        EnsureRefs();
        if (goldCoinIconRect == null) { onComplete?.Invoke(); return; }

        StartCoroutine(PlayFromToCoroutine(startRect, goldAmount, onComplete));
    }

    IEnumerator PlayFromToCoroutine(RectTransform startRect, int goldAmount, Action onComplete)
    {
        // 금화가 그려질 캔버스 = 골드 아이콘이 있는 캔버스 (같은 캔버스에 넣어야 확실히 보임)
        Canvas targetCanvas = goldCoinIconRect != null ? goldCoinIconRect.GetComponentInParent<Canvas>() : overlayCanvas;
        if (targetCanvas == null) targetCanvas = overlayCanvas;
        if (targetCanvas == null) { onComplete?.Invoke(); yield break; }

        RectTransform targetRect = targetCanvas.GetComponent<RectTransform>();
        if (targetRect == null) { onComplete?.Invoke(); yield break; }

        Canvas startCanvas = startRect.GetComponentInParent<Canvas>();
        Camera cam = startCanvas != null ? startCanvas.worldCamera : targetCanvas.worldCamera;
        Vector2 screenStart = RectTransformUtility.WorldToScreenPoint(cam, startRect.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRect, screenStart, targetCanvas.worldCamera, out Vector2 localStart);

        // 스프라이트가 없으면 Image는 그려지지 않음 → 골드 아이콘 스프라이트 또는 기본 텍스처 사용
        Sprite coinSprite = null;
        float scale = 1f;
        if (GoldPanelLinks.Instance != null)
        {
            coinSprite = GoldPanelLinks.Instance.floatingCoinSprite;
            scale = GoldPanelLinks.Instance.floatingCoinScale;
        }
        if (coinSprite == null && goldCoinIconRect != null)
        {
            var iconImg = goldCoinIconRect.GetComponentInChildren<UnityEngine.UI.Image>(true);
            if (iconImg != null && iconImg.sprite != null)
                coinSprite = iconImg.sprite;
        }
        if (coinSprite == null)
            coinSprite = CreateFallbackCoinSprite();

        GameObject coinGo = new GameObject("FloatingCoin");
        coinGo.transform.SetParent(targetCanvas.transform, false);
        coinGo.transform.SetAsLastSibling(); // 다른 UI 위에 그리기
        RectTransform coinRect = coinGo.AddComponent<RectTransform>();
        float size = 48f * scale;
        coinRect.sizeDelta = new Vector2(size, size);
        coinRect.anchoredPosition = localStart;
        coinRect.anchorMin = coinRect.anchorMax = new Vector2(0.5f, 0.5f);
        coinRect.pivot = new Vector2(0.5f, 0.5f);

        Image img = coinGo.AddComponent<Image>();
        img.sprite = coinSprite;
        img.raycastTarget = false;
        img.color = new Color(1f, 0.85f, 0.2f);

        // 플로팅 골드 사운드 재생 (GameManager의 floatingGoldVolume 사용)
        PlayFloatingGoldSound();

        // 1) 위로 살짝 떠오르기
        Vector2 upPos = localStart + new Vector2(0f, floatUpOffset);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / floatUpDuration;
            if (t > 1f) t = 1f;
            coinRect.anchoredPosition = Vector2.Lerp(localStart, upPos, t);
            yield return null;
        }

        // 2) GoldPanel로 날아가기
        Vector2 screenEnd = RectTransformUtility.WorldToScreenPoint(cam, goldCoinIconRect.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRect, screenEnd, targetCanvas.worldCamera, out Vector2 localEnd);

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / flyToGoldDuration;
            if (t > 1f) t = 1f;
            coinRect.anchoredPosition = Vector2.Lerp(upPos, localEnd, t);
            yield return null;
        }

        Destroy(coinGo);
        onComplete?.Invoke();
    }

    static Sprite _fallbackCoinSprite;

    /// <summary>스프라이트가 없을 때 보이는 기본 금화용 스프라이트 생성 (한 번만 생성해 재사용)</summary>
    static Sprite CreateFallbackCoinSprite()
    {
        if (_fallbackCoinSprite != null) return _fallbackCoinSprite;
        const int w = 64;
        const int h = 64;
        var tex = new Texture2D(w, h);
        Color gold = new Color(1f, 0.85f, 0.2f, 1f);
        Color dark = new Color(0.9f, 0.7f, 0.1f, 1f);
        float cx = w * 0.5f, cy = h * 0.5f, r = (w * 0.5f) - 2f;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx, dy = y - cy;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                tex.SetPixel(x, y, d <= r ? (d <= r - 2 ? gold : dark) : Color.clear);
            }
        tex.Apply();
        _fallbackCoinSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        return _fallbackCoinSprite;
    }

    void PlayFloatingGoldSound()
    {
        if (floatingGoldSound == null) return;
        if (SoundManager.Instance != null)
        {
            // floatingGoldVolume은 SoundManager에 직접 접근
            float volume = SoundManager.Instance.floatingGoldVolume;
            if (volume <= 0f) return;
            GameObject audioGo = new GameObject("FloatingGoldSound");
            audioGo.transform.position = Vector3.zero;
            AudioSource src = audioGo.AddComponent<AudioSource>();
            src.clip = floatingGoldSound;
            src.spatialBlend = 0f;
            src.playOnAwake = false;
            src.volume = volume;
            src.Play();
            Destroy(audioGo, floatingGoldSound.length + 0.2f);
        }
        else
        {
            // SoundManager 없을 때 폴백
            GameObject audioGo = new GameObject("FloatingGoldSound");
            audioGo.transform.position = Vector3.zero;
            AudioSource src = audioGo.AddComponent<AudioSource>();
            src.clip = floatingGoldSound;
            src.spatialBlend = 0f;
            src.playOnAwake = false;
            src.volume = 1f;
            src.Play();
            Destroy(audioGo, floatingGoldSound.length + 0.2f);
        }
    }
}
