using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 무기 레벨업 전용 UI. 해금된 다음 레벨 무기의 필요 자원을 스크롤에 표시하고, Btn_LevelUp으로 해금·장착·자원 소모.
/// </summary>
public class UpgradeUI : MonoBehaviour
{
    [Tooltip("닫을 패널 루트 (비어 있으면 이 오브젝트)")]
    public GameObject panel;
    [Tooltip("닫기 버튼 (Btn_Close)")]
    public Button closeButton;
    [Header("무기 레벨업")]
    [Tooltip("스크롤뷰 Content (자원 행들이 채워짐)")]
    public Transform content;
    [Tooltip("자원 한 줄 프리팹 (WeaponUpgradePanelRow + ItemImage, Text_ItemCount)")]
    public GameObject weaponUpgradePanelRowPrefab;
    [Tooltip("레벨업 버튼 (필요 자원 모두 소유 시에만 활성화, 부족 시 비활성·눌린 상태)")]
    public Button btnLevelUp;
    [Tooltip("Content 그리드: 한 줄(셀) 크기")]
    public Vector2 cellSize = new Vector2(200f, 60f);
    [Tooltip("Content 그리드: 셀 사이 간격")]
    public Vector2 spacing = new Vector2(10f, 10f);
    [Header("사운드")]
    public AudioClip buttonClickSound;

    bool _zoneIsLumber;
    bool _zoneIsMining;
    GameObject _nextWeaponGo; // 현재 표시 중인 "다음 레벨" 무기 (레벨업 시 해금할 대상)
    List<(string key, int count)> _currentRequirements = new List<(string, int)>();

    void Awake()
    {
        if (panel == null) panel = gameObject;
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseUI);
        if (btnLevelUp != null)
            btnLevelUp.onClick.AddListener(OnLevelUpClick);
    }

    public void SetZoneType(bool isLumber, bool isMining)
    {
        _zoneIsLumber = isLumber;
        _zoneIsMining = isMining;
    }

    public void CloseUI()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    /// <summary>스크롤에 "다음 레벨 무기" 필요 자원 채우고, Btn_LevelUp 상호작용 갱신</summary>
    public void RefreshWeaponUpgradeContent()
    {
        _nextWeaponGo = null;
        _currentRequirements.Clear();

        if (content == null || weaponUpgradePanelRowPrefab == null)
        {
            SetLevelUpInteractable(false);
            return;
        }

        bool forLumber = _zoneIsLumber && !_zoneIsMining ? true : (_zoneIsMining && !_zoneIsLumber ? false : _zoneIsLumber);
        List<GameObject> options = forLumber
            ? (WeaponManager.Instance != null ? WeaponManager.Instance.GetTreeWeaponOptions() : null)
            : (WeaponManager.Instance != null ? WeaponManager.Instance.GetOreWeaponOptions() : null);

        if (options == null || options.Count == 0)
        {
            ClearContent();
            SetLevelUpInteractable(false);
            return;
        }

        WeaponUpgradeData currentData = forLumber
            ? WeaponManager.Instance.GetCurrentTreeWeaponData()
            : WeaponManager.Instance.GetCurrentOreWeaponData();
        int currentLevel = currentData != null ? currentData.weaponLevel : 0;

        GameObject nextWeapon = null;
        WeaponUpgradeData nextData = null;
        foreach (var go in options)
        {
            var d = go != null ? go.GetComponent<WeaponUpgradeData>() : null;
            if (d == null || d.weaponLevel != currentLevel + 1 || d.isUnlocked) continue;
            nextWeapon = go;
            nextData = d;
            break;
        }

        if (nextWeapon == null || nextData == null)
        {
            ClearContent();
            SetLevelUpInteractable(false);
            return;
        }

        _nextWeaponGo = nextWeapon;
        _currentRequirements = nextData.GetLevelUpRequirementKeys();

        if (_currentRequirements.Count == 0)
        {
            ClearContent();
            SetLevelUpInteractable(false);
            return;
        }

        ClearContent();
        EnsureContentLayout();

        var collector = ItemCollectorController.Instance;
        bool allEnough = true;
        foreach (var req in _currentRequirements)
        {
            int have = collector != null ? collector.GetCount(req.key) : 0;
            if (have < req.count) allEnough = false;

            // 같은 키는 인벤토리/수집과 동일한 아이콘 사용 (목재·채광 무기에서 Jem_36 등 동일 이미지)
            Texture icon = collector != null ? collector.GetItemTexture(req.key) : null;
            if (icon == null)
            {
                GameObject prefabForIcon = null;
                foreach (var r in nextData.levelUpRequirements)
                {
                    if (r.prefab == null) continue;
                    string k = WeaponUpgradeData.GetItemKeyFromPrefab(r.prefab);
                    if (k == req.key) { prefabForIcon = r.prefab; break; }
                }
                icon = prefabForIcon != null ? WeaponUpgradeData.GetIconFromPrefab(prefabForIcon) : null;
            }

            GameObject row = Instantiate(weaponUpgradePanelRowPrefab, content);
            var rowScript = row.GetComponentInChildren<WeaponUpgradePanelRow>(true);
            if (rowScript == null)
                rowScript = row.AddComponent<WeaponUpgradePanelRow>();
            if (rowScript != null)
                rowScript.Set(have, req.count, icon);
        }

        SetLevelUpInteractable(allEnough);
    }

    void EnsureContentLayout()
    {
        if (content == null) return;
        var contentGo = content.gameObject;
        var contentRt = content.GetComponent<RectTransform>();
        if (contentRt != null)
        {
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
        }
        var vertical = contentGo.GetComponent<VerticalLayoutGroup>();
        if (vertical != null) Destroy(vertical);
        var grid = contentGo.GetComponent<GridLayoutGroup>();
        if (grid == null) grid = contentGo.AddComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 1;
        grid.cellSize = cellSize;
        grid.spacing = spacing;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Vertical;
        var fitter = contentGo.GetComponent<ContentSizeFitter>();
        if (fitter == null) fitter = contentGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scrollRect = content.GetComponentInParent<ScrollRect>();
        if (scrollRect != null) { scrollRect.vertical = true; scrollRect.horizontal = false; }
    }

    void ClearContent()
    {
        if (content == null) return;
        foreach (Transform t in content)
        {
            if (t != null && t.gameObject != null)
                Destroy(t.gameObject);
        }
    }

    /// <summary>필요 자원이 모두 충분할 때만 버튼 활성화. 부족 시 비활성 + 회색 표시.</summary>
    void SetLevelUpInteractable(bool canUpgrade)
    {
        if (btnLevelUp == null) return;
        btnLevelUp.interactable = canUpgrade;
        if (btnLevelUp.targetGraphic != null)
            btnLevelUp.targetGraphic.color = canUpgrade ? Color.white : new Color(0.55f, 0.55f, 0.55f, 0.9f);
    }

    void OnLevelUpClick()
    {
        if (_nextWeaponGo == null || _currentRequirements == null || _currentRequirements.Count == 0)
            return;

        var collector = ItemCollectorController.Instance;
        if (collector == null) return;
        if (!collector.TryConsumeResources(_currentRequirements))
            return;

        var nextData = _nextWeaponGo.GetComponent<WeaponUpgradeData>();
        if (nextData != null)
            nextData.isUnlocked = true;

        bool forLumber = _zoneIsLumber && !_zoneIsMining ? true : (_zoneIsMining && !_zoneIsLumber ? false : _zoneIsLumber);
        if (WeaponManager.Instance != null)
        {
            if (forLumber)
                WeaponManager.Instance.SetEquippedTreeWeapon(_nextWeaponGo);
            else
                WeaponManager.Instance.SetEquippedOreWeapon(_nextWeaponGo);
        }

        PlayButtonClickSound();
        RefreshWeaponUpgradeContent();
    }

    void PlayButtonClickSound()
    {
        if (buttonClickSound != null && SoundManager.Instance != null)
            SoundManager.Instance.PlayClipAtPoint(buttonClickSound, Vector3.zero, SoundManager.SoundCategory.UI);
    }

    void OnEnable()
    {
        RefreshWeaponUpgradeContent();
    }
}
