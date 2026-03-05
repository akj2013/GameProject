using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 통나무(아이템) 종류별 수집 갯수를 표시하는 패널. ItemCollectorController가 생성·갱신합니다.
/// Text_TreeCount에는 갯수만, LogImage에는 해당 나무 프리팹의 RawImage 텍스처를 표시합니다.
/// </summary>
public class ItemCollectorPanel : MonoBehaviour
{
    [Tooltip("갯수 표시 텍스트 (비어 있으면 자식 TextMeshProUGUI 자동 탐색)")]
    public TextMeshProUGUI countText;
    [Tooltip("아이콘 표시용 RawImage (비어 있으면 자식 중 'LogImage' 이름으로 탐색)")]
    public RawImage logImage;

    string itemKey;
    int count;

    void Awake()
    {
        if (countText == null)
            countText = GetComponentInChildren<TextMeshProUGUI>();
        if (logImage == null)
        {
            var rawImages = GetComponentsInChildren<RawImage>(true);
            foreach (var r in rawImages)
            {
                if (r.gameObject.name == "LogImage")
                {
                    logImage = r;
                    break;
                }
            }
            if (logImage == null && rawImages.Length > 0)
                logImage = rawImages[0];
        }
    }

    /// <summary>
    /// 표시할 아이템 키, 갯수, 아이콘 텍스처(나무 프리팹 RawImage)를 설정합니다.
    /// </summary>
    public void SetItem(string key, int newCount, Texture iconTexture = null)
    {
        itemKey = key;
        count = newCount;
        RefreshText();
        if (logImage != null)
            logImage.texture = iconTexture;
    }

    /// <summary>
    /// 갯수만 갱신합니다.
    /// </summary>
    public void SetCount(int newCount)
    {
        count = newCount;
        RefreshText();
    }

    public string ItemKey => itemKey;
    public int Count => count;

    void RefreshText()
    {
        if (countText == null) return;
        countText.text = count.ToString();
    }
}
