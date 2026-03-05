using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 무기 전환 패널 컨트롤러
/// </summary>
public class WeaponSwitchPanelController : MonoBehaviour
{
    [Tooltip("무기 전환 버튼")]
    public Button btnWeaponSwitch;
    [Tooltip("무기 전환 텍스트")]
    public TextMeshProUGUI textWeaponSwitch;

    void Start()
    {
        if (btnWeaponSwitch != null)
        {
            btnWeaponSwitch.onClick.AddListener(OnWeaponSwitchClicked);
        }
    }

    void OnWeaponSwitchClicked()
    {
        // 무기 전환 로직 (WeaponManager와 연동)
        var weaponManager = FindFirstObjectByType<WeaponManager>();
        if (weaponManager != null)
        {
            weaponManager.SwitchWeapon();
            UpdateWeaponText();
        }
    }

    void UpdateWeaponText()
    {
        if (textWeaponSwitch != null)
        {
            var weaponManager = FindFirstObjectByType<WeaponManager>();
            if (weaponManager != null)
            {
                textWeaponSwitch.text = weaponManager.GetCurrentWeaponName();
            }
        }
    }
}
