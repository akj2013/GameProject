using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 나무/광석 공격용 무기 표시. 무기 목록·현재 장착·교체(레벨업) 지원.
/// </summary>
public class WeaponManager : MonoBehaviour
{
    public static WeaponManager Instance;

    [Header("나무 공격용 무기 (도끼)")]
    [Tooltip("나무 벨 때 표시할 무기. WeaponUpgradeData 필수")]
    public GameObject treeWeapon;
    [Tooltip("벌목용 무기 후보 (레벨 순). 비어 있으면 treeWeapon만 사용")]
    public List<GameObject> treeWeaponOptions = new List<GameObject>();

    [Header("광석 공격용 무기 (낫)")]
    [Tooltip("광석 캘 때 표시할 무기. WeaponUpgradeData 필수")]
    public GameObject oreWeapon;
    [Tooltip("채광용 무기 후보 (레벨 순). 비어 있으면 oreWeapon만 사용")]
    public List<GameObject> oreWeaponOptions = new List<GameObject>();

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
        HideAllWeapons();
    }

    void Start()
    {
        if (treeWeapon == null && treeWeaponOptions.Count > 0)
        {
            foreach (var go in treeWeaponOptions)
            {
                var d = go != null ? go.GetComponent<WeaponUpgradeData>() : null;
                if (d != null && d.isUnlocked) { SetEquippedTreeWeapon(go); break; }
            }
        }
        if (oreWeapon == null && oreWeaponOptions.Count > 0)
        {
            foreach (var go in oreWeaponOptions)
            {
                var d = go != null ? go.GetComponent<WeaponUpgradeData>() : null;
                if (d != null && d.isUnlocked) { SetEquippedOreWeapon(go); break; }
            }
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>나무 공격 시 도끼만 보이게</summary>
    public void ShowTreeWeapon()
    {
        if (treeWeapon != null) treeWeapon.SetActive(true);
        if (oreWeapon != null) oreWeapon.SetActive(false);
    }

    /// <summary>광석 공격 시 낫만 보이게</summary>
    public void ShowOreWeapon()
    {
        if (treeWeapon != null) treeWeapon.SetActive(false);
        if (oreWeapon != null) oreWeapon.SetActive(true);
    }

    /// <summary>평상시·공격 종료 후 둘 다 숨김</summary>
    public void HideAllWeapons()
    {
        if (treeWeapon != null) treeWeapon.SetActive(false);
        if (oreWeapon != null) oreWeapon.SetActive(false);
    }

    /// <summary>현재 장착된 나무 무기의 WeaponUpgradeData (TreeManager 자원 획득량 등)</summary>
    public WeaponUpgradeData GetCurrentTreeWeaponData()
    {
        return treeWeapon != null ? treeWeapon.GetComponent<WeaponUpgradeData>() : null;
    }

    /// <summary>현재 장착된 광석 무기의 WeaponUpgradeData</summary>
    public WeaponUpgradeData GetCurrentOreWeaponData()
    {
        return oreWeapon != null ? oreWeapon.GetComponent<WeaponUpgradeData>() : null;
    }

    /// <summary>장착 나무 무기 교체 (레벨업 시 호출)</summary>
    public void SetEquippedTreeWeapon(GameObject weaponGo)
    {
        if (treeWeapon != null) treeWeapon.SetActive(false);
        treeWeapon = weaponGo;
        if (treeWeapon != null) treeWeapon.SetActive(true);
    }

    /// <summary>장착 광석 무기 교체 (레벨업 시 호출)</summary>
    public void SetEquippedOreWeapon(GameObject weaponGo)
    {
        if (oreWeapon != null) oreWeapon.SetActive(false);
        oreWeapon = weaponGo;
        if (oreWeapon != null) oreWeapon.SetActive(true);
    }

    /// <summary>벌목용 무기 후보 (옵션 없으면 treeWeapon 한 개 리스트)</summary>
    public List<GameObject> GetTreeWeaponOptions()
    {
        if (treeWeaponOptions != null && treeWeaponOptions.Count > 0)
            return treeWeaponOptions;
        var list = new List<GameObject>();
        if (treeWeapon != null) list.Add(treeWeapon);
        return list;
    }

    /// <summary>채광용 무기 후보 (옵션 없으면 oreWeapon 한 개 리스트)</summary>
    public List<GameObject> GetOreWeaponOptions()
    {
        if (oreWeaponOptions != null && oreWeaponOptions.Count > 0)
            return oreWeaponOptions;
        var list = new List<GameObject>();
        if (oreWeapon != null) list.Add(oreWeapon);
        return list;
    }

    /// <summary>UI 호환용.</summary>
    public void SwitchWeapon() { }

    /// <summary>UI 호환용.</summary>
    public string GetCurrentWeaponName() => "나무/광석 (자동)";
}
