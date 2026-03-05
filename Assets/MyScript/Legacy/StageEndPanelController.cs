using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 스테이지 종료 패널 컨트롤러
/// </summary>
public class StageEndPanelController : MonoBehaviour
{
    [Header("버튼")]
    [Tooltip("스테이지 종료 확인 버튼 (누르면 순간이동 + 스테이지 나무 리스폰)")]
    public Button btnStageEnd;

    [Header("플레이어 홈 이동")]
    [Tooltip("홈으로 이동시킬 플레이어 Transform")]
    public Transform playerTransform;
    [Tooltip("홈 위치 (버튼 클릭 시 플레이어를 여기로 이동)")]
    public Vector3 homePosition = new Vector3(9.9f, 0.2f, 4.8f);

    [Header("나무 리스폰")]
    [Tooltip("스테이지별 나무 할당 참조 (비어 있으면 씬에서 찾음). 리스폰 시 사용")]
    public StageTreePanelController stageTreePanelController;

    [Header("홈 이동 연출")]
    [Tooltip("플레이어 하위 SF_Rainbow (비어 있으면 플레이어 자식 중 이름 'SF_Rainbow'로 찾음)")]
    public GameObject sfRainbow;

    void Start()
    {
        if (btnStageEnd != null)
            btnStageEnd.onClick.AddListener(OnStageEndButtonClicked);
    }

    void OnStageEndButtonClicked()
    {
        Transform player = GetPlayerTransform();
        if (player == null)
        {
            RunSettlementThenHome(null);
            return;
        }

        // 0. 플레이어 아래 SF_Rainbow 활성화 → 5초 후 비활성화 (홈 이동 후 비활성화는 하지 않음)
        GameObject rainbow = sfRainbow != null ? sfRainbow : FindChildByName(player, "SF_Rainbow");
        if (rainbow != null)
        {
            rainbow.SetActive(true);
            StartCoroutine(DeactivateSfRainbowAfter(rainbow, 5f));
        }

        // 1. 회수되지 않은 통나무가 있으면 곧바로 회수
        var collector = player.GetComponent<PlayerCollector>();
        if (collector == null) collector = player.GetComponentInChildren<PlayerCollector>();
        if (collector != null)
            collector.CollectAllCollectibleLogsNow();

        // 2. 정산 로직 시작 → 완료 시 3·4·5
        RunSettlementThenHome(rainbow);
    }

    void RunSettlementThenHome(GameObject sfRainbowObj)
    {
        if (ItemCollectorController.Instance != null)
        {
            ItemCollectorController.Instance.RunSettlement(() => OnSettlementComplete());
            return;
        }
        OnSettlementComplete();
    }

    IEnumerator DeactivateSfRainbowAfter(GameObject rainbow, float seconds)
    {
        if (rainbow == null || seconds <= 0f) yield break;
        yield return new WaitForSeconds(seconds);
        if (rainbow != null)
            rainbow.SetActive(false);
    }

    void OnSettlementComplete()
    {
        Transform target = GetPlayerTransform();

        // 3. 플레이어 홈 이동
        if (target != null)
            TeleportTo(target, homePosition);

        // 4. SF_Rainbow 비활성화는 하지 않음 (5초 후 코루틴에서만 비활성화)

        // 5. 나무 리스폰
        RespawnAllStageTrees();

        // 미니맵 색상 즉시 갱신 (나무 있음/전부 베임 반영)
        var miniMap = Object.FindFirstObjectByType<MiniMapController>();
        if (miniMap != null)
            miniMap.RefreshAllStagePanelColors();
    }

    Transform GetPlayerTransform()
    {
        if (playerTransform != null) return playerTransform;
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null) return playerGo.transform;
        var collector = Object.FindFirstObjectByType<PlayerCollector>();
        return collector != null ? collector.transform : null;
    }

    static GameObject FindChildByName(Transform parent, string name)
    {
        if (parent == null || string.IsNullOrEmpty(name)) return null;
        for (int i = 0; i < parent.childCount; i++)
        {
            var t = parent.GetChild(i);
            if (t.name == name) return t.gameObject;
            var found = FindChildByName(t, name);
            if (found != null) return found;
        }
        return null;
    }

    /// <summary>
    /// CharacterController / Rigidbody가 있어도 확실히 순간이동되도록 처리
    /// </summary>
    static void TeleportTo(Transform target, Vector3 worldPosition)
    {
        if (target == null) return;

        // CharacterController가 있으면 잠시 끄지 않으면 position이 적용되지 않음 (루트/자식/부모 모두 검사)
        var cc = target.GetComponent<CharacterController>();
        if (cc == null) cc = target.GetComponentInChildren<CharacterController>();
        if (cc == null) cc = target.GetComponentInParent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            target.position = worldPosition;
            cc.enabled = true;
            return;
        }

        // Rigidbody만 있는 경우 속도 초기화 후 이동
        var rb = target.GetComponent<Rigidbody>();
        if (rb == null) rb = target.GetComponentInChildren<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        target.position = worldPosition;
    }

    /// <summary>모든 스테이지 타일에 대해 TileStageConfig 리젠 호출. 스테이지 패널에서 나무 바꿀 때와 동일한 처리(기존 나무 제거 + 나무 스폰 설정으로 생성).</summary>
    void RespawnAllStageTrees()
    {
        var tiles = Object.FindObjectsByType<TileStageConfig>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var tile in tiles)
        {
            if (tile == null) continue;
            tile.RespawnTreesOnTileGrounds();
        }
    }

    /// <summary>
    /// 통나무의 가격을 가져옵니다 (TreeManager에서)
    /// </summary>
    public int GetLogPrice(GameObject treePrefab)
    {
        if (treePrefab == null) return 1;

        var treeManager = treePrefab.GetComponent<TreeManager>();
        if (treeManager != null)
        {
            return treeManager.logPrice;
        }

        return 1;
    }

    /// <summary>
    /// 통나무 아이템에서 가격 가져오기
    /// </summary>
    public int GetLogPriceFromItem(LogItem logItem)
    {
        if (logItem != null)
        {
            return logItem.price;
        }
        return 1;
    }
}
