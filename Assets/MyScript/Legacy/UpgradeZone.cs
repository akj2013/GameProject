using UnityEngine;

/// <summary>
/// 콜라이더 범위 내 플레이어 진입 시 UpgradePanel 활성화, 이탈 또는 닫기 버튼 시 비활성화.
/// 벌목/채광 구분 체크박스로 해당 타입 무기 레벨업만 표시.
/// </summary>
public class UpgradeZone : MonoBehaviour
{
    [Tooltip("열릴 업그레이드 패널 (UpgradePanel)")]
    public GameObject upgradeUI;

    [Header("업그레이드 타입")]
    [Tooltip("체크 = 벌목용 무기 레벨업 표시")]
    public bool isLumberUpgrade = true;
    [Tooltip("체크 = 채광용 무기 레벨업 표시")]
    public bool isMiningUpgrade = false;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var ui = upgradeUI != null ? upgradeUI.GetComponent<UpgradeUI>() : null;
        if (ui != null)
            ui.SetZoneType(isLumberUpgrade, isMiningUpgrade);

        if (upgradeUI != null)
        {
            upgradeUI.SetActive(true);
            upgradeUI.transform.SetAsLastSibling();
            if (ui != null)
                ui.RefreshWeaponUpgradeContent();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && upgradeUI != null)
            upgradeUI.SetActive(false);
    }
}
