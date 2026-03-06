using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 업그레이드 패널 한 줄: 자원 아이콘 + "소유갯수 / 필요갯수". 소유 >= 필요 시 노란색, 미달 시 빨간색.
/// ItemImage, Text_ItemCount는 인스펙터 미지정 시 자식에서 이름으로 자동 탐색.
/// </summary>
public class WeaponUpgradePanelRow : MonoBehaviour
{
    [Tooltip("자원 아이콘 (프리팹 RawImage). 비어 있으면 자식 중 'ItemImage' 이름으로 탐색")]
    public RawImage itemImage;
    [Tooltip("갯수 텍스트 (예: 0 / 30). 비어 있으면 자식 중 'Text_ItemCount' 이름으로 탐색")]
    public TextMeshProUGUI textItemCount;

    static readonly Color ColorEnough = Color.yellow;
    static readonly Color ColorNotEnough = Color.red;

    void Awake()
    {
        // 이 행(Clone 한 개) 안에서만 검색. root는 Canvas라 다른 UI를 잡을 수 있음
        Transform searchRoot = transform;
        if (itemImage == null)
        {
            var raws = searchRoot.GetComponentsInChildren<RawImage>(true);
            foreach (var r in raws)
            {
                if (r.gameObject.name == "ItemImage") { itemImage = r; break; }
            }
            if (itemImage == null && raws.Length > 0) itemImage = raws[0];
        }
        if (textItemCount == null)
        {
            var texts = searchRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in texts)
            {
                if (t.gameObject.name == "Text_ItemCount") { textItemCount = t; break; }
            }
            if (textItemCount == null && texts.Length > 0) textItemCount = texts[0];
        }
    }

    public void Set(int current, int required, Texture icon)
    {
        if (textItemCount != null)
        {
            textItemCount.text = $"{current} / {required}";
            textItemCount.color = current >= required ? ColorEnough : ColorNotEnough;
        }
        if (itemImage != null)
            itemImage.texture = icon;
    }
}
