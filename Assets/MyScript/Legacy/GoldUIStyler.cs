using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// GOLD UI를 반투명 패널 위에 깔끔하게 표시하고, 게임 분위기에 맞게 스타일을 적용합니다.
/// GameManager의 goldText를 감싸는 패널을 생성하고 스타일을 적용합니다.
/// </summary>
[RequireComponent(typeof(GameManager))]
[ExecuteAlways]
public class GoldUIStyler : MonoBehaviour
{
    public GameManager gameManager;
    [Header("패널 설정")]
    [Tooltip("패널 반투명도 (0~1)")]
    [Range(0.1f, 0.9f)]
    public float panelAlpha = 0.75f;
    [Tooltip("패널 배경 색 (다크 톤 권장)")]
    public Color panelColor = new Color(0.08f, 0.06f, 0.04f, 1f);
    [Tooltip("패널 크기 - 텍스트 대비 여백")]
    public Vector2 panelPadding = new Vector2(100f, 40f);
    [Tooltip("패널 테두리 색 (금화 톤)")]
    public Color borderColor = new Color(1f, 0.85f, 0.4f, 1f);
    [Tooltip("패널 테두리 두께")]
    public float borderWidth = 4f;

    [Header("금화 아이콘")]
    [Tooltip("금화 더미 스프라이트 (비어있으면 Resources/UI/GoldCoinPile 로드)")]
    public Sprite coinSprite;
    [Tooltip("금화 아이콘 크기")]
    public float coinSize = 288f;

    [Header("텍스트 스타일")]
    [Tooltip("폰트 크기")]
    public int fontSize = 120;
    [Tooltip("텍스트 색 - 골드/앰버 톤")]
    public Color textColor = new Color(1f, 0.85f, 0.45f, 1f);
    [Tooltip("텍스트 그림자 색")]
    public Color shadowColor = new Color(0.15f, 0.12f, 0.08f, 0.8f);
    public Vector2 shadowOffset = new Vector2(2f, -2f);

    void OnEnable()
    {
        // 게임 시작 시 GoldPanel 건드리지 않음. 참조는 GoldPanelLinks 또는 Inspector에서 등록.
    }

    /// <summary>
    /// 씬에 있는 GoldPanel에 스타일만 적용. 새 패널은 생성하지 않음.
    /// GoldPanel 내 숫자 텍스트를 GameManager.goldText에 자동 연결.
    /// </summary>
    public void ApplyStylesToExistingPanel()
    {
        var existingPanel = FindGoldPanelInScene();
        if (existingPanel == null) return;

        // GoldPanel 내 골드 숫자 텍스트 찾아서 GameManager에 연결
        var goldNumText = FindGoldNumberText(existingPanel);
        if (goldNumText != null)
        {
            gameManager.goldText = goldNumText;
            gameManager.UpdateUI(); // 0으로 초기화 표시
        }

        SimplifyCoinIcon(existingPanel);
        ApplyStyles(existingPanel.transform, goldNumText ?? gameManager.goldText);
        ApplyPanelBorder(existingPanel);

        var fx = FindFirstObjectByType<FloatingCoinFx>();
        if (fx != null)
        {
            fx.goldCoinIconRect = GetGoldCoinIconRect();
            fx.overlayCanvas = existingPanel.GetComponentInParent<Canvas>();
        }
    }

    Transform FindGoldPanelInScene()
    {
        var goldPanel = GameObject.Find("GoldPanel");
        return goldPanel != null ? goldPanel.transform : null;
    }

    TextMeshProUGUI FindGoldNumberText(Transform panel)
    {
        var content = panel.Find("GoldContent");
        if (content == null) return null;
        foreach (Transform child in content)
        {
            if (child.name == "CoinIcon") continue;
            var tmp = child.GetComponent<TextMeshProUGUI>();
            if (tmp != null) return tmp;
        }
        return null;
    }

    /// <summary>
    /// CoinIcon에서 Mask/CoinImage 자식 제거 → 단일 Image만 유지
    /// (배경+CoinImage 구조는 체커보드 방지용이므로 건드리지 않음)
    /// </summary>
    void SimplifyCoinIcon(Transform panel)
    {
        var coinIcon = panel.Find("GoldContent/CoinIcon");
        if (coinIcon == null) return;

        var coinImage = coinIcon.Find("CoinImage");
        if (coinImage != null)
        {
            var parentImg = coinIcon.GetComponent<Image>();
            // 부모에 스프라이트 없음 = 배경+CoinImage 구조 (체커보드 방지) → 건드리지 않음
            if (parentImg != null && parentImg.sprite == null) return;

            var childImg = coinImage.GetComponent<Image>();
            if (childImg != null && parentImg != null)
            {
                parentImg.sprite = childImg.sprite;
                parentImg.color = Color.white;
                parentImg.preserveAspect = true;
            }
            if (Application.isPlaying)
                Object.Destroy(coinImage.gameObject);
            else
                Object.DestroyImmediate(coinImage.gameObject);
        }
        var mask = coinIcon.GetComponent<Mask>();
        if (mask != null)
        {
            if (Application.isPlaying)
                Object.Destroy(mask);
            else
                Object.DestroyImmediate(mask);
        }
    }

    public RectTransform GetGoldPanelRect()
    {
        if (gameManager?.goldText == null) return null;
        var parent = gameManager.goldText.transform.parent;
        while (parent != null)
        {
            if (parent.name == "GoldPanel")
                return parent.GetComponent<RectTransform>();
            parent = parent.parent;
        }
        return null;
    }

    public RectTransform GetGoldCoinIconRect()
    {
        if (gameManager?.goldText == null) return null;
        var panel = GetGoldPanelRect();
        if (panel == null) return null;
        var coinIcon = panel.Find("GoldContent/CoinIcon");
        return coinIcon != null ? coinIcon.GetComponent<RectTransform>() : panel;
    }

    Transform FindGoldPanel(RectTransform rect)
    {
        var p = rect.parent;
        while (p != null)
        {
            if (p.name == "GoldPanel") return p;
            p = p.parent;
        }
        return null;
    }

    void ApplyPanelBorder(Transform panelTransform)
    {
        var outline = panelTransform.GetComponent<Outline>();
        if (outline == null) outline = panelTransform.gameObject.AddComponent<Outline>();
        outline.effectColor = borderColor;
        outline.effectDistance = new Vector2(borderWidth, borderWidth);
    }

    void ApplyStyles(Transform panelTransform, TextMeshProUGUI tmp)
    {
        if (tmp == null) return;

        tmp.fontSize = fontSize;
        tmp.color = textColor;
        tmp.fontStyle = FontStyles.Bold;

        // Outline으로 가독성 향상
        tmp.outlineWidth = 0.2f;
        tmp.outlineColor = shadowColor;

        tmp.alignment = TextAlignmentOptions.Center;
        tmp.margin = new Vector4(12, 6, 12, 6);

        tmp.raycastTarget = false;
    }
}
