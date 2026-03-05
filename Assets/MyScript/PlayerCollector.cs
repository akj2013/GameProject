using UnityEngine;

public class PlayerCollector : MonoBehaviour
{
    void OnTriggerStay(Collider other)
    {
        LogItem log = other.GetComponentInParent<LogItem>();
        if (log == null) return;
        if (!log.CanCollect()) return;
        if (log.CanBeCollected == false) return; // 이미 수집 중이면 무시

        CollectLog(log);
    }

    /// <summary>통나무가 캐릭터로 날아와 사라지고, ItemCollectorPanel에 Drop Log Prefab별 패널에 갯수 반영</summary>
    void CollectLog(LogItem log)
    {
        string key = log.GetCollectorKey();
        Texture tex = log.GetCollectorIcon();
        int price = log.price;
        log.FlyToPlayer(transform, () =>
        {
            if (ItemCollectorController.Instance != null)
                ItemCollectorController.Instance.AddLogCount(key, tex, price);
        });
    }

    /// <summary>HOME 버튼 등에서 미회수 통나무를 즉시 전부 회수 (날아오는 연출 없이 갯수만 반영).</summary>
    public void CollectAllCollectibleLogsNow()
    {
        var allLogs = Object.FindObjectsByType<LogItem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var log in allLogs)
        {
            if (log == null || !log.CanBeCollected) continue;
            AddLogImmediate(log);
        }
    }

    /// <summary>즉시 회수: ItemCollectorPanel에만 반영하고 통나무 오브젝트 제거 (HOME 즉시 회수용)</summary>
    void AddLogImmediate(LogItem log)
    {
        if (log == null) return;
        string key = log.GetCollectorKey();
        Texture tex = log.GetCollectorIcon();
        int price = log.price;
        if (ItemCollectorController.Instance != null)
            ItemCollectorController.Instance.AddLogCount(key, tex, price);
        Destroy(log.gameObject);
    }

    /// <summary>스택 미사용 — 항상 null 반환 (호환용)</summary>
    public LogItem PopLog()
    {
        return null;
    }

    /// <summary>현재 수집된 통나무 총 갯수 (ItemCollectorController 기준)</summary>
    public int LogCount()
    {
        return ItemCollectorController.Instance != null ? ItemCollectorController.Instance.GetTotalCount() : 0;
    }

    /// <summary>
    /// 통나무 수집 시 호출. 통나무는 모두 Log 품목으로 합쳐서 표시됩니다. texture는 통나무 프리팹 RawImage.
    /// </summary>
    public void AddLogCount(string itemKey, Texture iconTexture = null, int price = 0)
    {
        if (ItemCollectorController.Instance != null && !string.IsNullOrEmpty(itemKey))
            ItemCollectorController.Instance.AddLogCount(itemKey, iconTexture, price);
    }

}
