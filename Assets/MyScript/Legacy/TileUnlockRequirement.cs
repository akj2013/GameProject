using UnityEngine;

/// <summary>
/// 타일 해제에 필요한 자원 한 종류 (프리팹 기준, 인스펙터 리스트용).
/// 키/아이콘은 WeaponUpgradeData.GetItemKeyFromPrefab / GetIconFromPrefab 사용.
/// </summary>
[System.Serializable]
public class TileUnlockRequirement
{
    [Tooltip("자원 프리팹 (LogRoot, JemRoot 등. LogItem 또는 ResourceItemKey로 수집 키 결정, 아이콘은 프리팹 RawImage/LogItem 사용)")]
    public GameObject prefab;
    [Tooltip("해제에 필요한 총 갯수 (예: 30)")]
    public int requiredCount = 30;
}
