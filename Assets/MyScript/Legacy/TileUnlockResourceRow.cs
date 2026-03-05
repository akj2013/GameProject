using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 타일 해제 자원 상태 캔버스 안의 한 줄: 자원 아이콘 + "충족 / 필요" 텍스트.
/// 인스펙터에서 ItemImage(RawImage), Text_ItemCount(TextMeshProUGUI) 지정하거나, 비어 있으면 자식에서 이름으로 탐색.
/// </summary>
public class TileUnlockResourceRow : MonoBehaviour
{
    [Tooltip("자원 아이콘. 비어 있으면 자식 중 'ItemImage' 이름으로 탐색")]
    public RawImage itemImage;
    [Tooltip("충족/필요 텍스트 (예: 10 / 30). 비어 있으면 자식 중 'Text_ItemCount' 이름으로 탐색")]
    public TextMeshProUGUI textItemCount;

    void Awake()
    {
        if (itemImage == null)
        {
            var raws = GetComponentsInChildren<RawImage>(true);
            foreach (var r in raws)
            {
                if (r.gameObject.name == "ItemImage") { itemImage = r; break; }
            }
            if (itemImage == null && raws.Length > 0) itemImage = raws[0];
        }
        if (textItemCount == null)
        {
            var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in texts)
            {
                if (t.gameObject.name == "Text_ItemCount") { textItemCount = t; break; }
            }
            if (textItemCount == null && texts.Length > 0) textItemCount = texts[0];
        }
    }

    /// <summary>충족 갯수, 필요 갯수, 아이콘 텍스처 설정</summary>
    public void Set(int current, int required, Texture icon)
    {
        if (textItemCount != null)
            textItemCount.text = $"{current} / {required}";
        if (itemImage != null && icon != null)
            itemImage.texture = icon;
    }
}
