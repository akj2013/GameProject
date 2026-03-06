using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 타일 해제 스크립트. Box Collider(Trigger)에 플레이어가 들어오면 해제 필요 자원을 투입하고,
/// 충족 시 LockedTile 비활성화, UnlockedTile/Resources/Environment 순서로 활성화 연출을 재생합니다.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class TileUnlock : MonoBehaviour
{
    [Header("해제 필요 자원")]
    [Tooltip("타일 해제에 필요한 자원 종류별 총량 (예: LogRoot 30개, JemRoot 30개). 프리팹과 필요 갯수를 지정")]
    public List<TileUnlockRequirement> requiredResources = new List<TileUnlockRequirement>();

    [Header("해제 대상 (동일 Tile 내)")]
    [Tooltip("해제 전 타일 오브젝트. 해제 시 아래로 내려가며 비활성화")]
    public GameObject lockedTileRoot;
    [Tooltip("해제 후 타일 오브젝트. 해제 시 아래에서 올라오며 활성화")]
    public GameObject unlockedTileRoot;
    [Tooltip("나무/광석 등 채집 자원의 부모. 해제 시 자식들이 올라오며 활성화")]
    public Transform resourcesRoot;
    [Tooltip("울타리/꽃 등 환경 오브젝트의 부모. 해제 시 자식들이 빠르게 올라오며 활성화")]
    public Transform environmentRoot;

    [Header("프로그래스바")]
    [Tooltip("필요 자원 투입 진행도 (0~1). Image면 fillAmount, Slider면 value 사용")]
    public GameObject progressBarObject;
    [Tooltip("ProgressBar가 Image 타입이면 체크. Slider면 해제. SpriteRenderer 기반이면 둘 다 해제")]
    public bool progressBarIsImage = true;
    [Tooltip("SpriteRenderer 기반 프로그래스바: 채워지는 부분 오브젝트 (ProgressBar_Full). 비어 있으면 progressBarObject 자식에서 'ProgressBar_Full' 이름으로 찾음")]
    public GameObject progressBarFullSprite;

    [Header("자원 상태 캔버스")]
    [Tooltip("충족/필요 자원을 표시하는 캔버스 (해제 대상 타일에 카펫처럼 배치). 해제 시 페이드 아웃")]
    public Canvas resourceStatusCanvas;
    [Tooltip("자원 한 줄 패널 프리팹 (TileUnlockResourceRow + ItemImage, Text_ItemCount). 캔버스 안에 동적 생성")]
    public GameObject resourceStatusPanelPrefab;
    [Tooltip("자원 패널들이 채워질 부모 (캔버스 하위). 비어 있으면 캔버스의 첫 번째 자식 또는 캔버스 자신")]
    public Transform resourceStatusContent;

    [Header("자원 투입 연출")]
    [Tooltip("자원이 날아가는 시작 위치 (플레이어의 PickupTrigger 등). 비어 있으면 트리거 진입한 플레이어 transform 사용")]
    public Transform playerPickupTrigger;
    [Tooltip("자원이 날아가는 데 걸리는 시간(초)")]
    public float flyDuration = 0.8f;
    [Tooltip("포물선 궤적의 최대 높이(미터). 클수록 더 높이 뜀")]
    public float flyArcHeight = 2f;
    [Tooltip("자원별 시작 위치 랜덤 반경(미터). 0이면 겹침")]
    public float flyStartRandomRadius = 0.35f;
    [Tooltip("포물선 높이 랜덤 배율 (최소~최대). 예: 0.75~1.25")]
    public Vector2 flyArcHeightRandom = new Vector2(0.75f, 1.25f);
    [Tooltip("시간에 따른 가중치: 이 시간(초)까지는 startBatch/startCount 사용, 이후 서서히 endBatch/endCount로 변화")]
    public float timeToMaxWeightSeconds = 10f;
    [Tooltip("가중치 적용 전: 한 번에 날리는 자원 개수(시각)")]
    public int startBatchSize = 1;
    [Tooltip("가중치 적용 전: 한 번에 올라가는 충족 갯수")]
    public int startCountIncrement = 1;
    [Tooltip("가중치 적용 후: 한 번에 날리는 자원 개수(시각)")]
    public int endBatchSize = 5;
    [Tooltip("가중치 적용 후: 한 번에 올라가는 충족 갯수")]
    public int endCountIncrement = 20;
    [Tooltip("같은 자원 타입에서 다음 배치를 날리기까지 대기 시간(초)")]
    public float batchIntervalSeconds = 0.4f;

    [Header("해제 연출")]
    [Tooltip("프로그래스바·자원상태 캔버스 페이드 아웃 시간(초)")]
    public float fadeOutDuration = 0.5f;
    [Tooltip("LockedTile이 아래로 내려가며 사라지는 시간(초)")]
    public float lockedMoveDownDuration = 0.4f;
    [Tooltip("LockedTile이 내려가는 Y 오프셋(로컬, 미터). 예: -3")]
    public float lockedMoveDownOffsetY = -3f;
    [Tooltip("UnlockedTile이 아래에서 올라오며 나타나는 시간(초)")]
    public float unlockedMoveUpDuration = 0.5f;
    [Tooltip("UnlockedTile 시작 시 로컬 Y 오프셋(땅 아래). 예: -1.5")]
    public float unlockedStartOffsetY = -1.5f;
    [Tooltip("Resources 자식들이 올라오며 나타나는 시간(초)")]
    public float resourcesMoveUpDuration = 0.6f;
    [Tooltip("Resources 시작 시 로컬 Y 오프셋")]
    public float resourcesStartOffsetY = -1.2f;
    [Tooltip("Environment 자식들이 올라오며 나타나는 시간(초). 더 빠르게")]
    public float environmentMoveUpDuration = 0.35f;
    [Tooltip("Environment 시작 시 로컬 Y 오프셋")]
    public float environmentStartOffsetY = -1f;

    [Header("타일 연결 및 UnlockVisual")]
    [Tooltip("이 타일을 해금해 줄 직전 타일. 비어 있으면 첫 타일로 간주")]
    public TileUnlock previousTile;
    [Tooltip("이 타일이 해금될 때 다음에 해금 가능해질 타일들 (복수 가능)")]
    public List<TileUnlock> nextTiles = new List<TileUnlock>();
    [Tooltip("다음 타일 UnlockVisual이 위에서 내려오는 애니 시간(초)")]
    public float nextUnlockVisualDropDuration = 0.4f;
    [Tooltip("다음 타일 UnlockVisual이 떨어지기 시작하는 위쪽 오프셋(로컬 Y, 미터)")]
    public float nextUnlockVisualDropOffsetY = 2f;

    // 런타임: 종류별 투입된 갯수 (실제로 타일에 도착해 반영된 값)
    Dictionary<string, int> contributed = new Dictionary<string, int>();
    // 종류별 필요 갯수 (키 → 필요량)
    Dictionary<string, int> requiredByKey = new Dictionary<string, int>();
    // 종류별 프리팹 (키 → 프리팹, 아이콘/비주얼용)
    Dictionary<string, GameObject> prefabByKey = new Dictionary<string, GameObject>();
    // UI 행 (키 → TileUnlockResourceRow)
    Dictionary<string, TileUnlockResourceRow> rowByKey = new Dictionary<string, TileUnlockResourceRow>();
    // 아직 비행 중인 자원(Reserve) 갯수. ItemCollector에서는 아직 빠지지 않았지만, 추가 배치에서는 중복 계산하지 않기 위해 사용.
    Dictionary<string, int> reservedByKey = new Dictionary<string, int>();

    bool isUnlocked;
    // 해금 연출 코루틴이 이미 실행 중인지 여부 (중복 실행 방지용)
    bool isUnlocking;
    bool playerInsideTrigger;
    Transform currentPlayerTransform;
    Coroutine contributingCoroutine;
    float contributeStartTime;

    BoxCollider boxCollider;

    /// <summary>이 타일이 이미 해금되었는지 (직전 타일 체크용).</summary>
    public bool IsUnlocked => isUnlocked;

    /// <summary>해제 필요 자원이 하나도 설정되어 있지 않으면 true (직전 타일 해금 시 함께 해금되는 타일 판별용).</summary>
    public bool HasNoRequiredResources => requiredByKey.Count == 0;

    void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null && !boxCollider.isTrigger)
            boxCollider.isTrigger = true;

        foreach (var req in requiredResources)
        {
            if (req.prefab == null || req.requiredCount <= 0) continue;
            string key = WeaponUpgradeData.GetItemKeyFromPrefab(req.prefab);
            if (string.IsNullOrEmpty(key)) continue;
            requiredByKey[key] = req.requiredCount;
            prefabByKey[key] = req.prefab;
            contributed[key] = 0;
            reservedByKey[key] = 0;
        }
    }

    void Start()
    {
        Transform content = resourceStatusContent != null ? resourceStatusContent : (resourceStatusCanvas != null ? resourceStatusCanvas.transform : null);
        if (resourceStatusCanvas != null && resourceStatusPanelPrefab != null && content != null)
        {
            foreach (var kv in requiredByKey)
            {
                string key = kv.Key;
                int required = kv.Value;
                GameObject go = Instantiate(resourceStatusPanelPrefab, content);
                var row = go.GetComponentInChildren<TileUnlockResourceRow>(true);
                if (row != null)
                {
                    rowByKey[key] = row;
                    Texture icon = WeaponUpgradeData.GetIconFromPrefab(prefabByKey[key]);
                    row.Set(0, required, icon);
                }
            }
        }

        UpdateProgressBar();
        if (resourceStatusCanvas != null) resourceStatusCanvas.gameObject.SetActive(true);
        if (progressBarObject != null) progressBarObject.SetActive(true);

        InitializeTileVisualAtGameStart();

        // UnlockVisual: 직전 타일이 아직 lock이면 비활성 유지, 해금됐으면 활성
        if (gameObject.name.Contains("Level0_0_0_0") || gameObject.name.Contains("Level1_0_1_-1"))
            Debug.Log($"[TileUnlock] {gameObject.name} Start: isUnlocked={isUnlocked}, previousTile={(previousTile != null ? previousTile.gameObject.name : "null")}, previousTile==this={previousTile == this}");
        RefreshUnlockVisual();
    }

    /// <summary>
    /// 게임 시작 시 타일의 초기 잠금/해금 상태에 따라
    /// LockedTile / UnlockedTile / Resources / Environments 활성 상태를 정리합니다.
    /// - previousTile이 null 이거나 자기 자신을 가리키면 "이미 해금된 타일"로 간주
    /// - 그 외에는 "해금되지 않은 타일"로 보고 LockedTile만 켭니다.
    /// </summary>
    void InitializeTileVisualAtGameStart()
    {
        if (!Application.isPlaying)
            return;

        bool isStartTile = (previousTile == null || previousTile == this);

        if (isStartTile)
        {
            // 시작 타일은 처음부터 해금된 것으로 간주
            isUnlocked = true;

            if (lockedTileRoot != null)
                lockedTileRoot.SetActive(false);

            if (unlockedTileRoot != null)
                unlockedTileRoot.SetActive(true);

            if (resourcesRoot != null)
                resourcesRoot.gameObject.SetActive(true);

            if (environmentRoot != null)
                environmentRoot.gameObject.SetActive(true);
        }
        else
        {
            // 해금되지 않은 타일: LockedTile만 활성, 나머지는 모두 비활성
            if (lockedTileRoot != null)
                lockedTileRoot.SetActive(true);

            if (unlockedTileRoot != null)
                unlockedTileRoot.SetActive(false);

            if (resourcesRoot != null)
                resourcesRoot.gameObject.SetActive(false);

            if (environmentRoot != null)
                environmentRoot.gameObject.SetActive(false);
        }
    }

    /// <summary>직전 타일 해금 여부에 따라 UnlockVisual 활성/비활성.</summary>
    public void RefreshUnlockVisual()
    {
        GameObject visual = GetUnlockVisual();
        if (visual == null) return;
        
        bool shouldLog = gameObject.name.Contains("Level0_0_0_0") || gameObject.name.Contains("Level1_0_1_-1");
        
        if (shouldLog)
            Debug.Log($"[TileUnlock] {gameObject.name} RefreshUnlockVisual: isUnlocked={isUnlocked}, previousTile={(previousTile != null ? previousTile.gameObject.name : "null")}, previousTile==this={previousTile == this}, visual.activeSelf={visual.activeSelf}");
        
        // 이미 해금되었거나, previousTile이 자기 자신이면 UnlockVisual 비활성 (해금 완료 상태)
        if (isUnlocked || previousTile == this)
        {
            if (shouldLog)
                Debug.Log($"[TileUnlock] {gameObject.name} RefreshUnlockVisual: Setting visual to FALSE (isUnlocked={isUnlocked}, previousTile==this={previousTile == this})");
            visual.SetActive(false);
            return;
        }
        
        // previousTile이 null이면 아직 직전 타일 연결 안 됨 → UnlockVisual 비활성
        if (previousTile == null)
        {
            if (shouldLog)
                Debug.Log($"[TileUnlock] {gameObject.name} RefreshUnlockVisual: previousTile is null, setting visual to FALSE");
            visual.SetActive(false);
            return;
        }
        
        // 직전 타일이 실제로 해금되었을 때만 UnlockVisual 활성.
        // 단, 직전 타일이 "시작 타일"(previousTile==자기자신)이면 첫 번째 해금 가능 타일이므로 활성화.
        bool prevActuallyUnlocked = previousTile.IsUnlocked
            || (previousTile.previousTile == previousTile); // 시작 타일 다음 = 처음부터 UnlockVisual 표시
        if (shouldLog)
            Debug.Log($"[TileUnlock] {gameObject.name} RefreshUnlockVisual: prevActuallyUnlocked={prevActuallyUnlocked} (IsUnlocked={previousTile.IsUnlocked}, isStartTileNext={previousTile.previousTile == previousTile}), setting visual to {prevActuallyUnlocked}");
        visual.SetActive(prevActuallyUnlocked);
    }

    /// <summary>이 타일의 UnlockVisual 오브젝트 (자식 'UnlockVisual'에서 자동 탐색).</summary>
    public GameObject GetUnlockVisual()
    {
        Transform t = transform.Find("UnlockVisual");
        return t != null ? t.gameObject : null;
    }

    /// <summary>자원 투입 없이 즉시 해금 연출을 재생. (직전 타일 해금 시 함께 해금되는 타일용)</summary>
    public void UnlockImmediately()
    {
        if (isUnlocked || isUnlocking) return;
        isUnlocking = true;
        StartCoroutine(UnlockSequenceCoroutine());
    }

    /// <summary>모든 필요 자원이 충족되었는지 계산.</summary>
    bool AreAllRequirementsSatisfied()
    {
        foreach (var kv in requiredByKey)
        {
            int cur = contributed.TryGetValue(kv.Key, out int c) ? c : 0;
            if (cur < kv.Value)
                return false;
        }
        return true;
    }

    /// <summary>
    /// 자원 투입(또는 비행)이 끝난 뒤 호출: 필요 자원이 모두 찼으면, 플레이어 위치와 상관없이 해금 시도.
    /// ContributingCoroutine과 FlyResourceAndApply 양쪽에서 사용.
    /// </summary>
    void TryAutoUnlockAfterContribution()
    {
        // 이미 해금되었거나 해금 연출 중이면 아무 것도 하지 않음
        if (isUnlocked || isUnlocking)
            return;

        if (!AreAllRequirementsSatisfied())
            return;

        isUnlocking = true;
        contributingCoroutine = null;
        StartCoroutine(UnlockSequenceCoroutine());
    }

    /// <summary>해당 타일이 해금된 것으로 간주되는지 (직전 타일 체크용). previousTile==자기자신이거나 실제 해금됐으면 true.</summary>
    public static bool IsConsideredUnlocked(TileUnlock tile)
    {
        if (tile == null) return true;
        if (tile.IsUnlocked) return true;
        if (tile.previousTile == tile) return true; // 자기 자신을 가리키면 시작 타일 → 해금된 것으로 간주
        if (tile.previousTile != null && tile.previousTile.previousTile == tile.previousTile) return true; // 직전 타일이 시작 타일
        return false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (isUnlocked) return;

        bool shouldLog = gameObject.name.Contains("Level0_0_0_0") || gameObject.name.Contains("Level1_0_1_-1");
        
        if (shouldLog)
            Debug.Log($"[TileUnlock] {gameObject.name} OnTriggerEnter: previousTile={(previousTile != null ? previousTile.gameObject.name : "null")}, previousTile==this={previousTile == this}, previousTile.IsUnlocked={(previousTile != null ? previousTile.IsUnlocked.ToString() : "N/A")}, IsConsideredUnlocked(previousTile)={IsConsideredUnlocked(previousTile)}");
        
        // 직전 타일이 "해금된 것으로 간주"되지 않으면 진행 불가 (RefreshUnlockVisual과 동일한 기준)
        if (previousTile != null && !IsConsideredUnlocked(previousTile))
        {
            if (shouldLog)
                Debug.Log($"[TileUnlock] {gameObject.name} OnTriggerEnter: BLOCKED - previous tile not considered unlocked");
            return;
        }

        if (shouldLog)
            Debug.Log($"[TileUnlock] {gameObject.name} OnTriggerEnter: Passed previous tile check");
        playerInsideTrigger = true;
        currentPlayerTransform = other.transform;
        if (playerPickupTrigger == null && currentPlayerTransform != null)
        {
            var t = currentPlayerTransform.Find("PickupTrigger");
            if (t != null) playerPickupTrigger = t;
        }

        bool hasInventory = HasAnyRequiredInInventory();
        if (shouldLog)
            Debug.Log($"[TileUnlock] {gameObject.name} OnTriggerEnter: HasAnyRequiredInInventory={hasInventory}");
        if (!hasInventory) return;

        if (contributingCoroutine == null)
        {
            if (shouldLog)
                Debug.Log($"[TileUnlock] {gameObject.name} OnTriggerEnter: Starting ContributingCoroutine");
            contributeStartTime = Time.time;
            contributingCoroutine = StartCoroutine(ContributingCoroutine());
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInsideTrigger = false;
    }

    bool HasAnyRequiredInInventory()
    {
        var collector = ItemCollectorController.Instance;
        if (collector == null) return false;
        foreach (var kv in requiredByKey)
        {
            int have = collector.GetCount(kv.Key);
            int cont = contributed.TryGetValue(kv.Key, out int c) ? c : 0;
            if (have > 0 && cont < kv.Value) return true;
        }
        return false;
    }

    IEnumerator ContributingCoroutine()
    {
        var collector = ItemCollectorController.Instance;
        if (collector == null) { contributingCoroutine = null; yield break; }

        while (true)
        {
            // 이번 틱에 투입할 자원 종류별 배치 정보 (여러 종류 동시 진행)
            var batches = new List<(string key, GameObject prefab, int visualCount, int amountPerVisual, int remainder)>();
            foreach (var kv in requiredByKey)
            {
                string key = kv.Key;
                int required = kv.Value;
                int cur = contributed.TryGetValue(key, out int c) ? c : 0;
                if (cur >= required) continue;

                int rawHave = collector.GetCount(key);
                int reserved = reservedByKey.TryGetValue(key, out int r) ? r : 0;
                int available = Mathf.Max(0, rawHave - reserved);
                if (available <= 0) continue;

                float elapsed = Time.time - contributeStartTime;
                float t = Mathf.Clamp01(elapsed / timeToMaxWeightSeconds);
                int batchSize = Mathf.RoundToInt(Mathf.Lerp(startBatchSize, endBatchSize, t));
                int countIncrement = Mathf.RoundToInt(Mathf.Lerp((float)startCountIncrement, (float)endCountIncrement, t));
                batchSize = Mathf.Max(1, batchSize);
                countIncrement = Mathf.Max(1, countIncrement);

                int need = required - cur;
                int canTake = Mathf.Min(available, need, countIncrement);
                if (canTake <= 0) continue;

                int visualCount = Mathf.Min(batchSize, canTake);
                int amountPerVisual = canTake / visualCount;
                int remainder = canTake % visualCount;
                GameObject prefab = prefabByKey.TryGetValue(key, out var p) ? p : null;
                if (prefab != null)
                {
                    batches.Add((key, prefab, visualCount, amountPerVisual, remainder));
                    reservedByKey[key] = reserved + canTake; // 이번 틱에 비행 예정인 자원만큼 예약
                }
            }

            Vector3 startPos = GetFlyStartPosition();
            Vector3 endPos = GetFlyEndPosition();
            if (batches.Count > 0 && startPos == endPos) { contributingCoroutine = null; yield break; }

            foreach (var b in batches)
            {
                for (int i = 0; i < b.visualCount; i++)
                {
                    int add = b.amountPerVisual + (i < b.remainder ? 1 : 0);
                    if (add <= 0) continue;
                    // 시작 위치·포물선은 FlyResourceAndApply 내부에서 개별 랜덤 적용
                    StartCoroutine(FlyResourceAndApply(b.prefab, b.key, add, startPos, endPos));
                }
            }

            if (batches.Count > 0)
            {
                for (int i = 0; i < batches.Count; i++)
                {
                    int visualCount = batches[i].visualCount;
                    for (int k = 0; k < visualCount - 1; k++)
                        yield return new WaitForSeconds(batchIntervalSeconds / Mathf.Max(1, visualCount));
                }
                yield return new WaitForSeconds(batchIntervalSeconds);
            }
            else
            {
                bool allSatisfied = true;
                foreach (var kv in requiredByKey)
                {
                    int cur = contributed.TryGetValue(kv.Key, out int c) ? c : 0;
                    if (cur < kv.Value) { allSatisfied = false; break; }
                }
                if (allSatisfied)
                {
                    // 플레이어가 트리거 안에 있든 없든, 필요 자원이 모두 찼으면 해금 시도
                    TryAutoUnlockAfterContribution();
                    yield break;
                }
                if (!playerInsideTrigger)
                {
                    contributingCoroutine = null;
                    yield break;
                }
                yield return new WaitForSeconds(0.15f);
            }
        }
    }

    IEnumerator FlyResourceAndApply(GameObject prefab, string key, int amount, Vector3 startPos, Vector3 endPos)
    {
        // 자원마다 시작 위치·포물선 높이를 살짝 랜덤하게 해서 겹침 감소
        Vector3 randomStart = startPos;
        if (flyStartRandomRadius > 0f)
            randomStart += new Vector3(
                Random.Range(-flyStartRandomRadius, flyStartRandomRadius),
                Random.Range(-flyStartRandomRadius * 0.5f, flyStartRandomRadius),
                Random.Range(-flyStartRandomRadius, flyStartRandomRadius));
        float height = flyArcHeight * Mathf.Lerp(flyArcHeightRandom.x, flyArcHeightRandom.y, Random.value);

        GameObject fly = Instantiate(prefab, randomStart, Quaternion.identity);
        // 비행 중 자원이 캐릭터와 겹쳐서 캐릭터가 밀려 올라가는 것 방지: 콜라이더 전부 비활성화
        foreach (var col in fly.GetComponentsInChildren<Collider>(true))
            col.enabled = false;

        float elapsed = 0f;
        while (elapsed < flyDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flyDuration;
            fly.transform.position = Parabola(randomStart, endPos, height, t);
            fly.transform.Rotate(Vector3.up, 180f * Time.deltaTime);
            yield return null;
        }

        if (fly != null) Destroy(fly);

        var collector = ItemCollectorController.Instance;
        if (collector == null) yield break;
        int reserved = reservedByKey.TryGetValue(key, out int r) ? r : 0;
        int applyAmount = Mathf.Min(amount, reserved);
        if (applyAmount <= 0) yield break;

        reservedByKey[key] = reserved - applyAmount;

        int have = collector.GetCount(key);
        int cur = contributed.TryGetValue(key, out int c) ? c : 0;
        int required = requiredByKey.TryGetValue(key, out int req) ? req : 0;
        int need = Mathf.Max(0, required - cur);
        // 충족값이 필요량을 넘지 않도록 캡. 인벤에서 빼는 양도 실제로 반영한 만큼만
        int add = Mathf.Min(applyAmount, have, need);
        if (add <= 0) yield break;

        contributed[key] = cur + add;
        collector.SetCount(key, have - add);

        if (rowByKey.TryGetValue(key, out var row))
            row.Set(contributed[key], requiredByKey[key], WeaponUpgradeData.GetIconFromPrefab(prefabByKey[key]));
        UpdateProgressBar();

        // 개별 자원 투입이 반영된 뒤에도, 모든 필요 자원이 찼다면 해금 시도
        TryAutoUnlockAfterContribution();
    }

    static Vector3 Parabola(Vector3 start, Vector3 end, float height, float t)
    {
        Vector3 linear = Vector3.Lerp(start, end, t);
        float y = height * 4f * t * (1f - t);
        return linear + Vector3.up * y;
    }

    Vector3 GetFlyStartPosition()
    {
        Vector3 basePos;
        if (playerPickupTrigger != null) basePos = playerPickupTrigger.position;
        else if (currentPlayerTransform != null) basePos = currentPlayerTransform.position + Vector3.up * 1f;
        else basePos = transform.position + Vector3.up * 1f;
        // 캐릭터와 겹치지 않도록 앞쪽·위쪽 오프셋 (콜라이더로 밀리지 않게)
        if (currentPlayerTransform != null)
            return basePos + currentPlayerTransform.forward * 0.5f + Vector3.up * 0.4f;
        return basePos + Vector3.up * 0.4f;
    }

    Vector3 GetFlyEndPosition()
    {
        if (unlockedTileRoot != null)
        {
            var r = unlockedTileRoot.GetComponent<Renderer>();
            if (r != null)
                return r.bounds.center - Vector3.up * (r.bounds.extents.y);
            return unlockedTileRoot.transform.position - Vector3.up * 0.5f;
        }
        return transform.position;
    }

    void UpdateProgressBar()
    {
        if (progressBarObject == null) return;
        int totalRequired = 0, totalContributed = 0;
        foreach (var kv in requiredByKey)
        {
            totalRequired += kv.Value;
            totalContributed += contributed.TryGetValue(kv.Key, out int c) ? c : 0;
        }
        float fill = totalRequired > 0 ? Mathf.Clamp01((float)totalContributed / totalRequired) : 0f;

        // SpriteRenderer 기반 프로그래스바 (ProgressBar_Full의 scale 조절)
        GameObject fullSpriteObj = progressBarFullSprite;
        if (fullSpriteObj == null && progressBarObject != null)
        {
            Transform found = progressBarObject.transform.Find("ProgressBar_Full");
            if (found != null) fullSpriteObj = found.gameObject;
        }
        if (fullSpriteObj != null)
        {
            SpriteRenderer sr = fullSpriteObj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                // 방법 1: Transform scale 조절 (간단)
                Vector3 scale = fullSpriteObj.transform.localScale;
                scale.x = fill;
                fullSpriteObj.transform.localScale = scale;
            }
            else
            {
                // 방법 2: Image fillAmount 사용 (더 자연스러움)
                var img = fullSpriteObj.GetComponent<Image>();
                if (img != null) img.fillAmount = fill;
            }
            return;
        }

        // 기존 UI 방식 (Image 또는 Slider)
        if (progressBarIsImage)
        {
            var img = progressBarObject.GetComponent<Image>();
            if (img != null) img.fillAmount = fill;
        }
        else
        {
            var slider = progressBarObject.GetComponent<Slider>();
            if (slider != null) slider.value = fill;
        }
    }

    IEnumerator UnlockSequenceCoroutine()
    {
        isUnlocked = true;

        if (resourceStatusCanvas != null)
        {
            var group = resourceStatusCanvas.GetComponent<CanvasGroup>();
            if (group == null) group = resourceStatusCanvas.gameObject.AddComponent<CanvasGroup>();
            float t = 1f;
            while (t > 0f)
            {
                t -= Time.deltaTime / fadeOutDuration;
                group.alpha = Mathf.Max(0f, t);
                yield return null;
            }
            resourceStatusCanvas.gameObject.SetActive(false);
        }
        if (progressBarObject != null)
        {
            var group = progressBarObject.GetComponent<CanvasGroup>();
            if (group == null) group = progressBarObject.AddComponent<CanvasGroup>();
            float t = 1f;
            while (t > 0f)
            {
                t -= Time.deltaTime / fadeOutDuration;
                group.alpha = Mathf.Max(0f, t);
                yield return null;
            }
            progressBarObject.SetActive(false);
        }

        if (lockedTileRoot != null)
        {
            Vector3 startPos = lockedTileRoot.transform.localPosition;
            Vector3 endPos = startPos + Vector3.up * lockedMoveDownOffsetY;
            float elapsed = 0f;
            while (elapsed < lockedMoveDownDuration)
            {
                elapsed += Time.deltaTime;
                lockedTileRoot.transform.localPosition = Vector3.Lerp(startPos, endPos, elapsed / lockedMoveDownDuration);
                yield return null;
            }
            lockedTileRoot.SetActive(false);
        }

        if (unlockedTileRoot != null)
        {
            // 참조가 비어 있으면 LockedTile과 같은 부모 아래 "UnlockedTile" 이름으로 찾기
            GameObject toUnlock = unlockedTileRoot;
            if (toUnlock == null && lockedTileRoot != null)
            {
                Transform siblingParent = lockedTileRoot.transform.parent;
                if (siblingParent != null)
                {
                    Transform found = siblingParent.Find("UnlockedTile");
                    if (found != null) toUnlock = found.gameObject;
                }
            }
            if (toUnlock != null)
            {
                // 부모가 꺼져 있으면 보이지 않으므로, 이 오브젝트까지 부모 체인 활성화
                Transform walk = toUnlock.transform;
                while (walk != null && walk != transform)
                {
                    walk.gameObject.SetActive(true);
                    walk = walk.parent;
                }
                toUnlock.SetActive(true);
                Vector3 startPos = toUnlock.transform.localPosition + Vector3.up * unlockedStartOffsetY;
                Vector3 endPos = toUnlock.transform.localPosition;
                toUnlock.transform.localPosition = startPos;
                float elapsed = 0f;
                while (elapsed < unlockedMoveUpDuration)
                {
                    elapsed += Time.deltaTime;
                    toUnlock.transform.localPosition = Vector3.Lerp(startPos, endPos, elapsed / unlockedMoveUpDuration);
                    yield return null;
                }
            }
        }

        if (resourcesRoot != null)
        {
            // 부모 자체를 비활성로 두고 시작하는 경우를 위해, 해제 시점에 먼저 활성화
            resourcesRoot.gameObject.SetActive(true);

            List<Transform> children = new List<Transform>();
            List<Vector3> targetLocalPositions = new List<Vector3>();
            foreach (Transform c in resourcesRoot)
            {
                children.Add(c);
                targetLocalPositions.Add(c.localPosition);
            }
            for (int i = 0; i < children.Count; i++)
            {
                children[i].gameObject.SetActive(true);
                children[i].localPosition = targetLocalPositions[i] + Vector3.up * resourcesStartOffsetY;
            }
            float elapsed = 0f;
            while (elapsed < resourcesMoveUpDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / resourcesMoveUpDuration;
                for (int i = 0; i < children.Count; i++)
                    children[i].localPosition = Vector3.Lerp(targetLocalPositions[i] + Vector3.up * resourcesStartOffsetY, targetLocalPositions[i], t);
                yield return null;
            }
        }

        if (environmentRoot != null)
        {
            // 마찬가지로, 환경 루트가 비활성 상태라도 해제 시점에 켜준다
            environmentRoot.gameObject.SetActive(true);

            List<Transform> children = new List<Transform>();
            List<Vector3> targetLocalPositions = new List<Vector3>();
            foreach (Transform c in environmentRoot)
            {
                children.Add(c);
                targetLocalPositions.Add(c.localPosition);
            }
            for (int i = 0; i < children.Count; i++)
            {
                children[i].gameObject.SetActive(true);
                children[i].localPosition = targetLocalPositions[i] + Vector3.up * environmentStartOffsetY;
            }
            float elapsed = 0f;
            while (elapsed < environmentMoveUpDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / environmentMoveUpDuration;
                for (int i = 0; i < children.Count; i++)
                    children[i].localPosition = Vector3.Lerp(targetLocalPositions[i] + Vector3.up * environmentStartOffsetY, targetLocalPositions[i], t);
                yield return null;
            }
        }

        // 자신의 UnlockVisual 비활성화 (해금되었으므로 더 이상 필요 없음)
        GameObject myVisual = GetUnlockVisual();
        if (myVisual != null)
            myVisual.SetActive(false);

        // 다음 타일들의 UnlockVisual 활성화 후 위에서 아래로 내려오는 애니 (각 타일은 자식 'UnlockVisual' 사용)
        if (nextTiles != null && nextTiles.Count > 0)
        {
            float dur = Mathf.Max(0.01f, nextUnlockVisualDropDuration);
            float offsetY = nextUnlockVisualDropOffsetY;
            foreach (TileUnlock next in nextTiles)
            {
                if (next == null) continue;
                
                // 다음 타일의 RefreshUnlockVisual 호출 (previousTile 체크 후 활성화)
                // 이 시점에서 이 타일(this)이 해금되었으므로, next의 previousTile이 this라면 활성화됨
                next.RefreshUnlockVisual();
                
                GameObject nextVisual = next.GetUnlockVisual();
                if (nextVisual == null) continue;
                
                // RefreshUnlockVisual에서 이미 활성화했을 수도 있지만, 애니메이션을 위해 다시 확인
                if (!nextVisual.activeSelf)
                    nextVisual.SetActive(true);
                    
                Transform tr = nextVisual.transform;
                Vector3 endPos = tr.localPosition;
                Vector3 startPos = endPos + Vector3.up * offsetY;
                tr.localPosition = startPos;
                float elapsed = 0f;
                while (elapsed < dur)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / dur);
                    tr.localPosition = Vector3.Lerp(startPos, endPos, t);
                    yield return null;
                }
                tr.localPosition = endPos;

                // Previous Tile이 자기 자신이 아닌 다른 타일이고, 해제 필요 자원이 없으면 → 직전 타일(this) 해금 순간 함께 해금
                if (next.previousTile == this && next.HasNoRequiredResources && !next.IsUnlocked)
                    next.UnlockImmediately();
            }
        }

        // nextTiles에 포함되지 않았지만 previousTile이 this인 모든 타일도 체크 (자원 없이 함께 해금되는 타일)
        TileUnlock[] allTiles = Object.FindObjectsByType<TileUnlock>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (TileUnlock tile in allTiles)
        {
            if (tile == null || tile == this) continue;
            if (tile.previousTile == this && tile.HasNoRequiredResources && !tile.IsUnlocked)
            {
                // nextTiles에 포함되지 않은 타일도 해금 처리
                tile.UnlockImmediately();
            }
        }
    }
}
