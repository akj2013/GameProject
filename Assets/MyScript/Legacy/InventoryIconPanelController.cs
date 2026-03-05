using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 아이콘 패널. Btn_InventoryIcon 클릭 시 InventoryPanel을 활성화합니다.
/// </summary>
public class InventoryIconPanelController : MonoBehaviour
{
    [Tooltip("인벤토리 아이콘 버튼 (Btn_InventoryIcon)")]
    public Button btnInventoryIcon;
    [Tooltip("열릴 인벤토리 패널 (InventoryPanel)")]
    public GameObject inventoryPanel;

    void Start()
    {
        if (btnInventoryIcon != null && inventoryPanel != null)
        {
            btnInventoryIcon.onClick.AddListener(OpenInventory);
        }
    }

    public void OpenInventory()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(true);
    }
}
