using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 레벨업 필요 자원 한 항목 (프리팹/갯수). 인스펙터 리스트용.
/// </summary>
[System.Serializable]
public class LevelUpRequirement
{
    [Tooltip("자원 프리팹 (LogItem 또는 ResourceItemKey 있는 오브젝트. 아이콘/키 추출용)")]
    public GameObject prefab;
    [Tooltip("필요 갯수")]
    public int count = 1;
}

/// <summary>
/// 무기별 업그레이드 데이터. 각 무기 오브젝트에 부착.
/// 1. 무기레벨 2. 자원 획득량 3. 레벨업 필요 자원 4. 최초 1레벨 무기 여부 5. 해금 여부 6. 벌목/채광 구분
/// </summary>
public class WeaponUpgradeData : MonoBehaviour
{
    [Header("레벨")]
    [Tooltip("무기 레벨 (1, 2, 3 ...)")]
    public int weaponLevel = 1;

    [Header("자원 획득")]
    [Tooltip("공격당 획득하는 자원 갯수 (1, 2, 3 ...)")]
    public int resourceGainPerHit = 1;

    [Header("레벨업 필요 자원")]
    [Tooltip("다음 레벨로 올릴 때 필요한 자원 (프리팹/갯수). 이 무기가 최종 레벨이면 비워둠")]
    public List<LevelUpRequirement> levelUpRequirements = new List<LevelUpRequirement>();

    [Header("초기/해금")]
    [Tooltip("체크 = 최초 1레벨 무기 (레벨업 없이 사용 가능). 해금 초기값이 true가 됨")]
    public bool isInitialLevel1Weapon = false;
    [Tooltip("체크 = 레벨업 완료로 해금됨. 최초 1레벨 무기는 초기값 true")]
    public bool isUnlocked = false;

    [Header("용도")]
    [Tooltip("체크 = 벌목용 무기 (나무)")]
    public bool isLumberWeapon = true;
    [Tooltip("체크 = 채광용 무기 (광석)")]
    public bool isMiningWeapon = false;

    void Awake()
    {
        if (isInitialLevel1Weapon && !isUnlocked)
            isUnlocked = true;
    }

    /// <summary>레벨업 필요 자원을 (itemKey, count) 리스트로 반환. 프리팹에서 키 추출.</summary>
    public List<(string itemKey, int count)> GetLevelUpRequirementKeys()
    {
        var list = new List<(string, int)>();
        foreach (var req in levelUpRequirements)
        {
            if (req.prefab == null || req.count <= 0) continue;
            string key = GetItemKeyFromPrefab(req.prefab);
            if (!string.IsNullOrEmpty(key))
                list.Add((key, req.count));
        }
        return list;
    }

    /// <summary>프리팹에서 수집 키 추출 (LogItem → GetCollectorKey, ResourceItemKey → itemKey, 없으면 name)</summary>
    public static string GetItemKeyFromPrefab(GameObject prefab)
    {
        if (prefab == null) return null;
        var logItem = prefab.GetComponent<LogItem>();
        if (logItem != null)
            return logItem.GetCollectorKey();
        var res = prefab.GetComponent<ResourceItemKey>();
        if (res != null)
            return res.GetCollectorKey();
        return prefab.name;
    }

    /// <summary>프리팹에서 아이콘 텍스처 추출 (RawImage 또는 LogItem)</summary>
    public static Texture GetIconFromPrefab(GameObject prefab)
    {
        if (prefab == null) return null;
        var raw = prefab.GetComponentInChildren<UnityEngine.UI.RawImage>(true);
        if (raw != null && raw.texture != null) return raw.texture;
        var logItem = prefab.GetComponent<LogItem>();
        if (logItem != null) return logItem.iconTexture ?? logItem.GetCollectorIcon();
        return null;
    }
}
