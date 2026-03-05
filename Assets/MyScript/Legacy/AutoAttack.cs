using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>타격 대상(나무)에 재생할 VFX 설정. 포지션/회전/스케일 오프셋 적용.</summary>
[System.Serializable]
public class AttackVfxConfig
{
    [Tooltip("재생할 VFX 프리팹 (비어 있으면 재생 안 함)")]
    public GameObject prefab;
    [Tooltip("나무 기준 로컬 위치 오프셋 (월드 Up 방향 등)")]
    public Vector3 positionOffset = new Vector3(0f, 1.2f, 0f);
    [Tooltip("나무 기준 로컬 회전 오프셋 (오일러 각도)")]
    public Vector3 rotationOffsetEuler = Vector3.zero;
    [Tooltip("VFX 스케일")]
    public float scale = 1f;
}

public class AutoAttack : MonoBehaviour
{
    public static AutoAttack Instance { get; private set; }

    private Animator animator;

    float attackTimer = 0f;
    /// <summary>한 번 공격이 시작되면 애니 종료 시까지 새 공격/무기 토글 방지 (깜빡임·한 번 더 공격 방지)</summary>
    bool attackInProgress = false;
    /// <summary>다음 공격 1회 스킵. 마지막 타격 직후 한 번 더 휘두르는 것 방지.</summary>
    bool skipNextAttack = false;

    [Header("Attack Settings")]
    [SerializeField] float baseAttackCooldown = 1.2f;
    [SerializeField] float attackSpeed = 1f;
    // 애니메이터 AttackSpeed 파라미터에 넣어줄 부드럽게 보간된 속도 값
    float animSpeed = 1f;
    [Tooltip("한 번 휘두르는 동안 공격 불가 시간(초). 반드시 공격 클립 길이 이상으로 설정. 짧으면 한 번 휘두르는데 데미지가 여러 번 들어감")]
    [SerializeField] float singleAttackAnimDuration = 0.9f;

    [Header("Raycast / Cone")]
    [SerializeField] float attackRange = 2.2f;
    [Tooltip("나무·광석이 올 수 있는 레이어 (Tree 선택). Default도 항상 함께 검사함.")]
    [SerializeField] LayerMask treeLayer;
    [SerializeField, Range(0f, 180f)] float coneHalfAngle = 35f;
    [SerializeField, Range(16, 128)] int maxOverlapResults = 64; // 주변 콜라이더 많으면 작을수록 타겟 누락됨
    [SerializeField] bool ignoreVerticalForAngle = true; // 높이 무시하고 수평 각도로 체크할지
    [Tooltip("나무/광석 탐색 주기(초). 작을수록 반응 빠름, 클수록 CPU 절약. 0.08 = 약 60fps에서 5프레임마다")]
    [SerializeField, Range(0.03f, 0.2f)] float detectTreeInterval = 0.08f;

    TreeManager targetTreeManager;
    TreeManager targetOreTreeManager; // Is Tree 체크 해제된 타겟 → 낫
    float _nextDetectTreeTime;
    float _lastAttackStartTime = -999f; // 한 번 휘두른 뒤 최소 간격 강제용

    [SerializeField] Transform characterModel;

    // 재사용 가능한 배열 (GC 방지)
    Collider[] overlapResults;
    List<TreeManager> treesInRangeCache = new List<TreeManager>(32);
    HashSet<TreeManager> treesInRangeSet = new HashSet<TreeManager>();
    List<TreeManager> oresInRangeCache = new List<TreeManager>(32);
    HashSet<TreeManager> oresInRangeSet = new HashSet<TreeManager>();

    [Header("Anim State Names")]
    [Tooltip("나무/광석 공통 사용 양손 공격 스테이트 이름")]
    [SerializeField] string singleAnimState = "Attack2H01";

    [Header("VFX (타격 시 범위 내 모든 나무 위치에 재생)")]
    [Tooltip("듀얼 무기 오른손 타격 시 VFX")]
    [SerializeField] AttackVfxConfig vfxDualRight = new AttackVfxConfig();
    [Tooltip("듀얼 무기 왼손 타격 시 VFX")]
    [SerializeField] AttackVfxConfig vfxDualLeft = new AttackVfxConfig();
    [Tooltip("싱글(오른손) 공격 시 VFX")]
    [SerializeField] AttackVfxConfig vfxSingleRight = new AttackVfxConfig();

    float EffectiveAttackRange => attackRange;

    /// <summary>공격속도 (쿨타임·애니 배속). UpgradeUI 등에서 표시/업그레이드용.</summary>
    public float AttackSpeed => attackSpeed;

    /// <summary>공격속도 업그레이드 (+0.2). UpgradeUI에서 호출.</summary>
    public void UpgradeAttackSpeed()
    {
        attackSpeed += 0.2f;
    }

    // 레이어 무시하고 전부 검사 후 TreeManager만 필터 (Jem_36 등 레이어/태그 이슈 회피)
    const int AllLayers = -1;

    void Awake()
    {
        if (Instance != null && Instance != this)
            return;
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        // 초기 애니 속도 세팅
        animSpeed = Mathf.Clamp(attackSpeed, 1f, 1.5f);
        if (animator != null)
            animator.SetFloat("AttackSpeed", animSpeed);

        overlapResults = new Collider[Mathf.Max(4, maxOverlapResults)];
    }

    void Update()
    {
        if (Time.time >= _nextDetectTreeTime)
        {
            _nextDetectTreeTime = Time.time + detectTreeInterval;
            DetectTarget();
        }

        attackTimer += Time.deltaTime;

        float realCooldown = baseAttackCooldown / attackSpeed;

        // 공격속도에 따른 애니메이션 배속을 1.0~1.5 사이에서 부드럽게 보간
        float normalized = Mathf.Clamp01((attackSpeed - 1f) / 2f); // attackSpeed 1~3 → 0~1
        float targetAnimSpeed = Mathf.Lerp(1f, 1.5f, normalized);
        animSpeed = Mathf.Lerp(animSpeed, targetAnimSpeed, Time.deltaTime * 10f);
        if (animator != null)
            animator.SetFloat("AttackSpeed", animSpeed);

        bool hasTarget = targetTreeManager != null || targetOreTreeManager != null;
        float timeSinceLastAttack = Time.time - _lastAttackStartTime;
        bool attackIntervalOk = timeSinceLastAttack >= singleAttackAnimDuration;
        if (!attackInProgress && hasTarget && attackTimer >= realCooldown && attackIntervalOk && animator != null)
        {
            if (skipNextAttack)
            {
                skipNextAttack = false;
                return;
            }
            if (!IsCurrentTargetStillValid())
                return;
            attackInProgress = true;
            _lastAttackStartTime = Time.time;
            if (targetTreeManager != null)
            {
                if (WeaponManager.Instance != null) WeaponManager.Instance.ShowTreeWeapon();
                animator.CrossFade(singleAnimState, 0.01f);
            }
            else if (targetOreTreeManager != null)
            {
                if (WeaponManager.Instance != null) WeaponManager.Instance.ShowOreWeapon();
                animator.CrossFade(singleAnimState, 0.01f);
            }
            attackTimer = 0f;
            StartCoroutine(HideWeaponAfterAttack());
        }
    }

    IEnumerator HideWeaponAfterAttack()
    {
        yield return new WaitForSeconds(singleAttackAnimDuration);
        attackInProgress = false;
        if (WeaponManager.Instance != null)
            WeaponManager.Instance.HideAllWeapons();
    }

    void DetectTarget()
    {
        if (characterModel == null) return;

        float range = EffectiveAttackRange;
        Vector3 origin = characterModel.position + Vector3.up * 1f;
        Vector3 forward = characterModel.forward;
        float maxSqrRange = range * range;

#if UNITY_EDITOR
        Debug.DrawRay(origin, forward * range, Color.red);
        Vector3 leftDir = Quaternion.AngleAxis(-coneHalfAngle, Vector3.up) * forward;
        Vector3 rightDir = Quaternion.AngleAxis(coneHalfAngle, Vector3.up) * forward;
        Debug.DrawRay(origin, leftDir * range, Color.yellow);
        Debug.DrawRay(origin, rightDir * range, Color.yellow);
#endif

        TreeManager foundTree = null;
        TreeManager foundOre = null;

        int hitCount = Physics.OverlapSphereNonAlloc(origin, range, overlapResults, AllLayers);
        for (int i = 0; i < hitCount; i++)
        {
            Collider col = overlapResults[i];
            if (col == null) continue;
            TreeManager tm = col.GetComponentInParent<TreeManager>() ?? col.GetComponent<TreeManager>();
            if (tm == null || !tm.IsAttackable) continue;

            Vector3 closestPoint;
            var meshCol = col as MeshCollider;
            if (meshCol != null && !meshCol.convex)
                closestPoint = col.bounds.ClosestPoint(origin);
            else
                closestPoint = col.ClosestPoint(origin);

            Vector3 toPoint = closestPoint - origin;
            if (ignoreVerticalForAngle) toPoint.y = 0f;
            if (toPoint.sqrMagnitude <= 0.0001f) continue;

            float angle = Vector3.Angle(forward, toPoint.normalized);
            if (angle > coneHalfAngle || toPoint.sqrMagnitude > maxSqrRange) continue;

            if (!tm.IsTree) { if (foundOre == null) foundOre = tm; }
            else { if (foundTree == null) foundTree = tm; }
        }

        if (foundOre != null)
        {
            if (targetOreTreeManager != foundOre)
            {
                if (targetTreeManager != null)
                {
                    targetTreeManager.OnTreeDestroyed -= StopAttack;
                    targetTreeManager = null;
                }
                targetOreTreeManager = foundOre;
                attackTimer = 0f;
            }
            return;
        }

        if (foundTree != null)
        {
            if (targetTreeManager != foundTree)
            {
                if (targetTreeManager != null)
                    targetTreeManager.OnTreeDestroyed -= StopAttack;
                targetTreeManager = foundTree;
                attackTimer = 0f;
                if (targetTreeManager != null)
                    targetTreeManager.OnTreeDestroyed += StopAttack;
            }
            targetOreTreeManager = null;
            return;
        }

        // 이번 프레임 overlap에 안 잡혀도, 이미 타겟이 사거리 안이면 유지 (OverlapSphere 개수 제한으로 누락 방지)
        if (targetOreTreeManager != null)
        {
            bool attackable = targetOreTreeManager.IsAttackable;
            bool inRange = IsInRangeAndCone(targetOreTreeManager.gameObject, origin, forward, range, maxSqrRange);
            if (attackable && inRange)
            {
                return; // 유지 (로그 스팸 방지로 생략)
            }
        }
        if (targetTreeManager != null && targetTreeManager.IsAttackable && IsInRangeAndCone(targetTreeManager.gameObject, origin, forward, range, maxSqrRange))
        {
            return;
        }

        StopAttack();
    }

    bool IsInRangeAndCone(GameObject go, Vector3 origin, Vector3 forward, float range, float maxSqrRange)
    {
        if (go == null) return false;
        var col = go.GetComponentInChildren<Collider>();
        if (col == null) return false;
        Vector3 closestPoint = col.ClosestPoint(origin);
        Vector3 toPoint = closestPoint - origin;
        if (ignoreVerticalForAngle) toPoint.y = 0f;
        if (toPoint.sqrMagnitude <= 0.0001f || toPoint.sqrMagnitude > maxSqrRange) return false;
        return Vector3.Angle(forward, toPoint.normalized) <= coneHalfAngle;
    }

    /// <summary>공격 넣기 직전에 타겟이 아직 유효한지 검사. 없거나 사거리 밖이면 false.</summary>
    bool IsCurrentTargetStillValid()
    {
        if (characterModel == null) return false;
        float range = EffectiveAttackRange;
        Vector3 origin = characterModel.position + Vector3.up * 1f;
        Vector3 forward = characterModel.forward;
        float maxSqrRange = range * range;

        if (targetTreeManager != null)
            return targetTreeManager.IsAttackable && IsInRangeAndCone(targetTreeManager.gameObject, origin, forward, range, maxSqrRange);
        if (targetOreTreeManager != null)
            return targetOreTreeManager.IsAttackable && IsInRangeAndCone(targetOreTreeManager.gameObject, origin, forward, range, maxSqrRange);
        return false;
    }


    /// <summary>애니메이션 이벤트에서 호출 (싱글 무기용)</summary>
    public void DealDamage()
    {
        Debug.Log("[AutoAttack] DealDamage() called (Animation Event).");
        DealDamageInternal(dualStrike: false);
        TryPlayAxeHitBurst();
    }

    /// <summary>범위(콘) 내 공격 가능한 모든 나무를 반환. 캐시 리스트에 채워서 반환.</summary>
    List<TreeManager> GetTreesInRange()
    {
        treesInRangeCache.Clear();
        treesInRangeSet.Clear();
        if (characterModel == null) return treesInRangeCache;

        float range = EffectiveAttackRange;
        Vector3 origin = characterModel.position + Vector3.up * 1f;
        Vector3 forward = characterModel.forward;
        float maxSqrRange = range * range;

        int hitCount = Physics.OverlapSphereNonAlloc(origin, range, overlapResults, AllLayers);
        for (int i = 0; i < hitCount; i++)
        {
            Collider col = overlapResults[i];
            if (col == null) continue;
            TreeManager tm = col.GetComponentInParent<TreeManager>() ?? col.GetComponent<TreeManager>();
            if (tm == null || !tm.IsAttackable || !tm.IsTree) continue;

            Vector3 closestPoint;
            var meshCol = col as MeshCollider;
            if (meshCol != null && !meshCol.convex)
                closestPoint = col.bounds.ClosestPoint(origin);
            else
                closestPoint = col.ClosestPoint(origin);

            Vector3 toPoint = closestPoint - origin;
            if (ignoreVerticalForAngle) toPoint.y = 0f;
            if (toPoint.sqrMagnitude <= 0.0001f) continue;

            float angle = Vector3.Angle(forward, toPoint.normalized);
            if (angle <= coneHalfAngle && toPoint.sqrMagnitude <= maxSqrRange && treesInRangeSet.Add(tm))
                treesInRangeCache.Add(tm);
        }
        return treesInRangeCache;
    }

    void DealDamageInternal(bool dualStrike = false, bool isRightHand = true)
    {
        int damage = PlayerStats.Instance != null ? PlayerStats.Instance.attackDamage : 1;

        if (targetTreeManager != null)
        {
            AttackVfxConfig vfxConfig = vfxSingleRight;
            List<TreeManager> trees = GetTreesInRange();
            bool playVfx = vfxConfig != null && vfxConfig.prefab != null;
            int treeGain = 1;
            if (WeaponManager.Instance != null)
            {
                var w = WeaponManager.Instance.GetCurrentTreeWeaponData();
                if (w != null) treeGain = Mathf.Max(1, w.resourceGainPerHit);
            }
            for (int i = 0; i < trees.Count; i++)
            {
                TreeManager tm = trees[i];
                if (tm == null) continue;
                if (tm.CurrentHealth <= treeGain)
                    skipNextAttack = true;
                tm.Hit(damage);
                if (playVfx)
                    SpawnVfxOnTree(tm, vfxConfig);
            }
            if (skipNextAttack)
                StopAttack();
        }
        else if (targetOreTreeManager != null)
        {
            List<TreeManager> ores = GetOresInRange();
            bool playVfx = vfxSingleRight != null && vfxSingleRight.prefab != null;
            int oreGain = 1;
            if (WeaponManager.Instance != null)
            {
                var w = WeaponManager.Instance.GetCurrentOreWeaponData();
                if (w != null) oreGain = Mathf.Max(1, w.resourceGainPerHit);
            }
            for (int i = 0; i < ores.Count; i++)
            {
                TreeManager tm = ores[i];
                if (tm == null) continue;
                if (tm.CurrentHealth <= oreGain)
                    skipNextAttack = true;
                tm.Hit(damage);
                if (playVfx)
                    SpawnVfxOnTree(tm, vfxSingleRight);
            }
            if (skipNextAttack)
                StopAttack();
        }
    }

    /// <summary>
    /// AutoAttack이 데미지를 넣은 직후, 현재 사용 중인 무기의 AxeTrailEffect를 찾아
    /// 무기 위치(도끼 날 근처)에서 히트 버스트 이펙트를 재생한다.
    /// 타겟 유무와 관계없이, 데미지 이벤트가 발생하면 항상 한 번 재생하도록 단순화.
    /// </summary>
    void TryPlayAxeHitBurst()
    {
        Debug.Log("[AutoAttack] TryPlayAxeHitBurst() start.");
        if (WeaponManager.Instance == null)
        {
            Debug.LogWarning("[AutoAttack] WeaponManager.Instance is null. Cannot play hit burst.");
            return;
        }

        // 나무를 공격 중이면 treeWeapon, 광석이면 oreWeapon을 사용
        // 타겟 유무 상관 없이, 현재 무기를 기준으로 이펙트 재생
        GameObject weapon = null;

        // 나무 공격용 무기가 있으면 우선 사용
        if (WeaponManager.Instance.treeWeapon != null)
        {
            Debug.Log("[AutoAttack] Using treeWeapon for hit burst.");
            weapon = WeaponManager.Instance.treeWeapon;
        }

        // 광석 공격 중이거나 나무 무기가 없고 광석 무기가 있으면 그쪽 사용
        if (WeaponManager.Instance.oreWeapon != null && targetOreTreeManager != null)
        {
            Debug.Log("[AutoAttack] Overriding with oreWeapon for hit burst.");
            weapon = WeaponManager.Instance.oreWeapon;
        }

        if (weapon == null)
        {
            Debug.LogWarning("[AutoAttack] weapon GameObject is null. Cannot find AxeTrailEffect for hit burst.");
            return;
        }

        var axeTrail = weapon.GetComponentInChildren<AxeTrailEffect>();
        if (axeTrail == null)
        {
            Debug.LogWarning($"[AutoAttack] AxeTrailEffect not found under weapon '{weapon.name}'.");
            return;
        }

        // 지금은 정확한 충돌 지점 대신, 무기(도끼) 위치 기준으로만 터뜨린다.
        Vector3 hitPosition = axeTrail.transform.position;
        Vector3 hitNormal = Vector3.up;
        Debug.Log($"[AutoAttack] Calling AxeTrailEffect.PlayHitBurst at pos={hitPosition}, normal={hitNormal}.");
        axeTrail.PlayHitBurst(hitPosition, hitNormal);
    }

    /// <summary>범위(콘) 내 TreeManager.IsTree == false 인 목록 (광석). 무기 선택은 TreeManager 체크로만 구분.</summary>
    List<TreeManager> GetOresInRange()
    {
        oresInRangeCache.Clear();
        oresInRangeSet.Clear();
        if (characterModel == null) return oresInRangeCache;

        float range = EffectiveAttackRange;
        Vector3 origin = characterModel.position + Vector3.up * 1f;
        Vector3 forward = characterModel.forward;
        float maxSqrRange = range * range;

        int hitCount = Physics.OverlapSphereNonAlloc(origin, range, overlapResults, AllLayers);
        for (int i = 0; i < hitCount; i++)
        {
            Collider col = overlapResults[i];
            if (col == null) continue;
            TreeManager tm = col.GetComponentInParent<TreeManager>() ?? col.GetComponent<TreeManager>();
            if (tm == null || !tm.IsAttackable || tm.IsTree) continue;

            Vector3 closestPoint;
            var meshCol = col as MeshCollider;
            if (meshCol != null && !meshCol.convex)
                closestPoint = col.bounds.ClosestPoint(origin);
            else
                closestPoint = col.ClosestPoint(origin);

            Vector3 toPoint = closestPoint - origin;
            if (ignoreVerticalForAngle) toPoint.y = 0f;
            if (toPoint.sqrMagnitude <= 0.0001f) continue;

            float angle = Vector3.Angle(forward, toPoint.normalized);
            if (angle <= coneHalfAngle && toPoint.sqrMagnitude <= maxSqrRange && oresInRangeSet.Add(tm))
                oresInRangeCache.Add(tm);
        }
        return oresInRangeCache;
    }

    /// <summary>타격 순간의 나무 위치를 기준으로 VFX 재생. 풀에서 꺼내 쓰고 재생 시간 뒤 풀에 반환.</summary>
    void SpawnVfxOnTree(TreeManager treeManager, AttackVfxConfig config)
    {
        if (config == null || config.prefab == null || GameManager.Instance == null || treeManager == null) return;
        Transform t = treeManager.transform;
        if (t == null) return;

        try
        {
            int vfxLayer = LayerMask.NameToLayer("TransparentFX");
            if (vfxLayer < 0) vfxLayer = -1;
            Vector3 pos = t.TransformPoint(config.positionOffset);
            Quaternion rot = t.rotation * Quaternion.Euler(config.rotationOffsetEuler);
            GameObject vfx = GameManager.Instance.GetVfxFromPool(config.prefab, pos, rot, vfxLayer);
            if (vfx != null)
            {
                vfx.transform.localScale = Vector3.one * config.scale;
                float duration = GameManager.Instance.GetCachedParticleDuration(config.prefab, vfx);
                if (duration > 0f)
                    GameManager.Instance.ReturnVfxToPoolAfter(vfx, duration);
                else
                    GameManager.Instance.ReturnVfxToPoolAfter(vfx, 2f);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[AutoAttack] SpawnVfxOnTree failed: {e.Message}");
        }
    }

    void StopAttack()
    {
        if (targetTreeManager != null)
        {
            targetTreeManager.OnTreeDestroyed -= StopAttack;
            targetTreeManager = null;
        }
        targetOreTreeManager = null;
        attackTimer = 0f;
        // 무기는 HideWeaponAfterAttack에서만 숨김 (공격 중 타겟 잃어도 깜빡임 방지)
        if (animator != null)
            animator.ResetTrigger("Attack");
    }
}
