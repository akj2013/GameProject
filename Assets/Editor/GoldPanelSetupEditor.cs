using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// GoldPanel에 GoldPanelLinks 추가 및 참조 연결.
/// CoinIcon 배경 추가로 체커보드 숨김.
/// </summary>
public static class GoldPanelSetupEditor
{
    [MenuItem("Tools/WoodLand3D/Gold Panel 연결 (씬에 등록)")]
    public static void SetupGoldPanelLinks()
    {
        var goldPanel = GameObject.Find("GoldPanel");
        if (goldPanel == null)
        {
            Debug.LogWarning("GoldPanel을 찾을 수 없습니다.");
            return;
        }

        var links = goldPanel.GetComponent<GoldPanelLinks>();
        if (links == null) links = goldPanel.AddComponent<GoldPanelLinks>();

        var content = goldPanel.transform.Find("GoldContent");
        if (content == null)
        {
            Debug.LogWarning("GoldContent를 찾을 수 없습니다.");
            return;
        }

        // CoinIcon 찾기
        var coinIcon = content.Find("CoinIcon");
        RectTransform coinIconRect = null;
        if (coinIcon != null)
        {
            coinIconRect = coinIcon.GetComponent<RectTransform>();
            AddCoinIconBackground(coinIcon); // 체커보드 숨김용 배경
        }

        // 골드 숫자 텍스트 찾기
        TextMeshProUGUI goldText = null;
        foreach (Transform child in content)
        {
            if (child.name == "CoinIcon") continue;
            var tmp = child.GetComponent<TextMeshProUGUI>();
            if (tmp != null) { goldText = tmp; break; }
        }

        links.goldText = goldText;
        links.coinIconRect = coinIconRect;

        // 골드 숫자 오버플로우 방지 (사이즈는 사용자 설정 유지)
        if (goldText != null)
        {
            goldText.enableAutoSizing = true;
            goldText.fontSizeMin = 14;
            goldText.fontSizeMax = Mathf.Max(14, goldText.fontSize);
            goldText.overflowMode = TextOverflowModes.Truncate;
        }

        // GameManager, FloatingCoinFx에 연결
        var gm = Object.FindFirstObjectByType<GameManager>();
        if (gm != null) gm.goldText = goldText;

        var fx = Object.FindFirstObjectByType<FloatingCoinFx>();
        if (fx != null) fx.goldCoinIconRect = coinIconRect;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Gold Panel 연결 완료. 씬을 저장하세요 (Ctrl+S)");
    }

    static void AddCoinIconBackground(Transform coinIcon)
    {
        if (coinIcon.Find("CoinImage") != null) return;

        var oldBg = coinIcon.Find("CoinIconBg");
        if (oldBg != null) Object.DestroyImmediate(oldBg.gameObject);

        var parentImg = coinIcon.GetComponent<Image>();
        if (parentImg == null) return;

        // 배경을 먼저 그리게 하려면: 부모=배경, 자식=금화
        var sprite = parentImg.sprite;
        parentImg.sprite = null;
        parentImg.color = new Color(0.08f, 0.06f, 0.04f, 1f);
        parentImg.raycastTarget = false;

        var coinGo = new GameObject("CoinImage");
        coinGo.transform.SetParent(coinIcon, false);
        var rect = coinGo.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        var img = coinGo.AddComponent<Image>();
        img.sprite = sprite;
        img.color = Color.white;
        img.preserveAspect = true;
        img.raycastTarget = false;
    }
}
