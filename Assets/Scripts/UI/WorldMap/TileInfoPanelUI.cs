using UnityEngine;
using TMPro;

namespace WoodLand3D.WorldMap
{
    public class TileInfoPanelUI : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text typeText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text lockedText;
        [SerializeField] private TMP_Text yieldText;
        [SerializeField] private GameObject contentRoot;

        private void Awake()
        {
            if (contentRoot != null) contentRoot.SetActive(false);
        }

        public void ShowTile(TileData data)
        {
            if (contentRoot != null) contentRoot.SetActive(data != null);
            if (data == null) return;

            if (titleText != null) titleText.text = data.displayName;
            if (typeText != null) typeText.text = "Type: " + data.type;
            if (levelText != null) levelText.text = "Level: " + data.level;
            if (lockedText != null) lockedText.text = "Locked: " + (data.isLocked ? "Yes" : "No");
            if (yieldText != null)
                yieldText.text = data.resourceValue > 0 ? $"Yield: {data.resourceValue} {data.resourceLabel}" : "Yield: -";
        }
    }
}
