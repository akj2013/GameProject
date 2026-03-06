using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// TreeHealth의 대체 스크립트. No Leaf Tree를 제거하고, Full Tree가 쓰러지는 애니메이션을 추가합니다.
/// </summary>
public class TreeManager : MonoBehaviour
{
    public GameObject fullTree;

    /// <summary>이 인스턴스를 생성할 때 쓴 나무 프리팹. TileStageConfig가 리젠 시 spawnableTreePrefabs를 채우는 데 사용.</summary>
    [HideInInspector] public GameObject sourcePrefab;

    [Header("Drop")]
    public GameObject logPrefab;   // ⭐ 통나무 프리팹
    public int dropCount = 3;     // ⭐ 떨어지는 통나무 개수 (int로 변경)
    [Tooltip("통나무 1개 판매 가격")]
    public int logPrice = 1;
    [Tooltip("통나무 색상 오버라이드 (알파 0이면 기본색 유지)")]
    public Color logTintColor = new Color(1, 1, 1, 0);
    [Tooltip("통나무 스폰 높이 (지면으로부터의 거리, 최소값)")]
    public float logSpawnHeightMin = 1.2f;
    [Tooltip("통나무 스폰 높이 (지면으로부터의 거리, 최대값)")]
    public float logSpawnHeightMax = 1.8f;
    [Tooltip("통나무 스폰 반경 (나무 중심으로부터의 거리, 최소값)")]
    public float logSpawnRadiusMin = 0.4f;
    [Tooltip("통나무 스폰 반경 (나무 중심으로부터의 거리, 최대값)")]
    public float logSpawnRadiusMax = 0.9f;

    [Header("Drop Popup (DropItemPanelManager 사용)")]
    [Tooltip("통나무 획득 시 DropItemPanel에 아이콘+x2 팝업 표시 여부. 비어 있으면 DropItemPanelManager.Instance 사용")]
    [SerializeField] bool enableDropPopup = true;
    [Tooltip("팝업 표시 담당. 비어 있으면 씬에서 DropItemPanelManager.Instance를 사용")]
    [SerializeField] DropItemPanelManager dropItemPanel;

    [Header("Hit VFX / SFX")]
    [Tooltip("피격 시 재생할 사운드 (옵션)")]
    public AudioClip hitSfx;

    [Header("Shake")]
    [Tooltip("피격 시 흔들리는 지속시간")]
    public float shakeDuration = 0.25f;
    [Tooltip("흔들림 강도(로컬 회전 축 기준, deg)")]
    public float shakeIntensity = 6f;

    [Header("Type (AutoAttack 무기 선택용)")]
    [Tooltip("체크 = 나무(도끼), 체크 해제 = 광석(낫). 기본값은 나무.")]
    [SerializeField] bool isTree = true;

    /// <summary>나무면 true, 광석이면 false. AutoAttack이 도끼/낫 선택에 사용.</summary>
    public bool IsTree => isTree;

    [Header("획득 수량 (기존 Health 대체)")]
    [Tooltip("최대 획득 가능 수량. 이만큼 자원을 획득하면 쓰러짐. 공격당 무기의 자원 획득량만큼 획득")]
    public int maxObtainableQuantity = 10;

    [Header("Fall Animation")]
    [Tooltip("나무가 쓰러지는 속도 (각속도, deg/s)\n" +
             "값이 클수록 빠르게 회전하며 쓰러집니다.\n" +
             "예: 30 = 보통 속도, 50 = 빠르게, 10 = 느리게")]
    public float fallAngularVelocity = 30f;
    [Tooltip("나무의 무게 (Rigidbody mass, kg)\n" +
             "값이 클수록 무거워서 관성으로 더 멀리 굴러갑니다.\n" +
             "예: 50 = 가벼움, 100 = 보통, 200 = 무거움")]
    public float treeMass = 100f;
    [Tooltip("선형 저항 / 공기 저항 (Linear Damping)\n" +
             "나무가 앞뒤로 이동할 때 받는 저항입니다.\n" +
             "값이 클수록 빨리 멈춥니다 (0 = 저항 없음, 5 = 빠르게 멈춤, 10 = 매우 빠르게 멈춤)\n" +
             "낮은 값: 나무가 멀리 굴러감, 높은 값: 나무가 빠르게 멈춤")]
    public float linearDamping = 2f;
    [Tooltip("각속도 저항 / 회전 저항 (Angular Damping)\n" +
             "나무가 회전할 때 받는 저항입니다.\n" +
             "값이 클수록 회전이 빨리 멈춥니다 (0 = 저항 없음, 5 = 빠르게 멈춤, 10 = 매우 빠르게 멈춤)\n" +
             "낮은 값: 나무가 계속 회전함, 높은 값: 나무가 빠르게 회전 멈춤")]
    public float angularDamping = 5f;
    [Tooltip("쓰러질 때 앞으로 밀리는 힘 (최소값, Newton)\n" +
             "나무가 쓰러질 때 받는 앞쪽 방향 힘의 최소값입니다.\n" +
             "fallForceMin과 fallForceMax 사이의 랜덤 값이 적용됩니다.\n" +
             "낮은 값: 나무가 가까운 곳에 떨어짐, 높은 값: 나무가 멀리 떨어짐")]
    public float fallForceMin = 50f;
    [Tooltip("쓰러질 때 앞으로 밀리는 힘 (최대값, Newton)\n" +
             "나무가 쓰러질 때 받는 앞쪽 방향 힘의 최대값입니다.\n" +
             "fallForceMin과 fallForceMax 사이의 랜덤 값이 적용됩니다.\n" +
             "낮은 값: 나무가 가까운 곳에 떨어짐, 높은 값: 나무가 멀리 떨어짐")]
    public float fallForceMax = 150f;
    [Tooltip("지면에 닿은 후 사라지기까지의 시간")]
    public float disappearDelayAfterGroundHit = 2f;
    [Tooltip("지면 감지 거리 (나무가 이 거리 내에 있으면 지면에 닿은 것으로 간주)")]
    public float groundCheckDistance = 0.5f;

    [Header("Respawn (자기 자신 리젠)")]
    [Tooltip("Fall 애니메이션 후 비활성화된 뒤, 이 나무를 원상태로 리젠할지 여부")]
    [SerializeField] bool enableRespawn = false;
    [Tooltip("비활성화된 후 원상태로 리젠되기까지의 시간(초)")]
    [SerializeField, Min(0.1f)] float respawnDelay = 10f;
    [Tooltip("리젠 시간에 랜덤 범위 사용 여부")]
    [SerializeField] bool useRespawnDelayRandom = false;
    [Tooltip("리젠 시간 최소값(초). useRespawnDelayRandom이 true일 때만 사용")]
    [SerializeField, Min(0.1f)] float respawnDelayMin = 8f;
    [Tooltip("리젠 시간 최대값(초). useRespawnDelayRandom이 true일 때만 사용")]
    [SerializeField, Min(0.1f)] float respawnDelayMax = 15f;

    [Header("Respawn 시 플레이어 밀어내기")]
    [Tooltip("리젠 시 플레이어가 나무와 겹쳐 있으면 밀어낼 거리(미터). 0이면 밀어내기 비활성")]
    [SerializeField, Min(0f)] float respawnPushDistance = 1.2f;
    [Tooltip("밀어내기에 걸리는 시간(초). 클수록 천천히 밀림")]
    [SerializeField, Min(0.05f)] float respawnPushDuration = 0.35f;
    [Tooltip("겹침 판정 반경(나무 중심부터). 이 거리 안에 플레이어가 있으면 밀어냄")]
    [SerializeField, Min(0.1f)] float respawnOverlapCheckRadius = 1.5f;
    [Tooltip("밀어낸 직후 나무↔플레이어 충돌 무시 시간(초). 짧게 무시하면 밀어낸 뒤 다시 튕기지 않음")]
    [SerializeField, Min(0f)] float respawnIgnoreCollisionDuration = 0.3f;

    [Header("Respawn 애니메이션 (땅에서 솟아오르기)")]
    [Tooltip("리젠 시 땅에서 솟아오르고 살짝 튀었다가 안착하는 연출 사용 여부")]
    [SerializeField] bool enableRespawnAnimation = true;
    [Tooltip("땅에서 올라오는 시간(초)")]
    [SerializeField, Min(0.05f)] float respawnEmergeDuration = 0.4f;
    [Tooltip("시작 시 나무가 묻혀 있는 깊이(로컬 Y 오프셋, 음수). -1.5 = 땅 아래 1.5만큼에서 시작")]
    [SerializeField] float respawnStartOffsetY = -1.5f;
    [Tooltip("땅 위로 솟아오른 뒤 위로 튀어오르는 높이(로컬 Y 추가). 0.3 = 0.3만큼 더 올라갔다가 내려옴")]
    [SerializeField, Min(0f)] float respawnPopHeight = 0.3f;
    [Tooltip("튀어오른 뒤 제자리에 안착하는 시간(초)")]
    [SerializeField, Min(0.05f)] float respawnSettleDuration = 0.25f;

    int obtainedSoFar; // 누적 획득 수량 (maxObtainableQuantity 도달 시 쓰러짐)
    int currentState = 0; // 0 = full, 1 = falling

    private Rigidbody fallRigidbody;
    private Coroutine fallCoroutine;
    private Collider[] treeColliders;
    private GameObject playerObject;
    private Vector3 fullTreeInitialLocalPos;
    private Quaternion fullTreeInitialLocalRot;

    const int LayerTree = 3; // Tree 레이어 인덱스

    void Start()
    {
        obtainedSoFar = 0;
        SetState(0);
        
        if (fullTree != null)
        {
            fullTreeInitialLocalPos = fullTree.transform.localPosition;
            fullTreeInitialLocalRot = fullTree.transform.localRotation;
        }
        
        // 나무 오브젝트를 Tree 레이어로 설정 (CameraObstacleFader가 감지하도록)
        SetLayerRecursively(gameObject, LayerTree);
        
        // 나무의 모든 Collider 수집
        treeColliders = GetComponentsInChildren<Collider>(true);
        
        // 플레이어 찾기
        playerObject = GameObject.FindGameObjectWithTag("Player");
    }

    static void SetLayerRecursively(GameObject go, int layer)
    {
        if (go == null) return;
        go.layer = layer;
        for (int i = 0; i < go.transform.childCount; i++)
        {
            Transform child = go.transform.GetChild(i);
            if (child != null && child.gameObject != null)
                SetLayerRecursively(child.gameObject, layer);
        }
    }

    public void Hit(int damage)
    {
        if (currentState == 1) return; // falling 중이면 무시

        if (Application.isMobilePlatform && GameManager.Instance != null)
            GameManager.Instance.TriggerHitVibration();

        PlayHitEffects();

        // 현재 무기의 자원 획득량만큼만 획득 (나무/광석에 따라 WeaponManager에서 조회)
        int gain = 1;
        if (WeaponManager.Instance != null)
        {
            WeaponUpgradeData weaponData = IsTree
                ? WeaponManager.Instance.GetCurrentTreeWeaponData()
                : WeaponManager.Instance.GetCurrentOreWeaponData();
            if (weaponData != null)
                gain = Mathf.Max(1, weaponData.resourceGainPerHit);
        }
        int remaining = maxObtainableQuantity - obtainedSoFar;
        gain = Mathf.Min(gain, remaining);
        obtainedSoFar += gain;

        DropLog(gain);

        bool willDestroy = obtainedSoFar >= maxObtainableQuantity;
        StartCoroutine(ApplyHitAfterDelay(willDestroy));
    }

    IEnumerator ApplyHitAfterDelay(bool willDestroy)
    {
        // 흔들리는 연출이 눈에 보이도록, 쉐이크 시간의 대부분이 지난 뒤에 상태를 바꾼다.
        float delay = Mathf.Max(0.05f, shakeDuration * 0.8f);
        yield return new WaitForSeconds(delay);

        if (willDestroy)
        {
            OnTreeDestroyed?.Invoke();
            StartFallAnimation();
            // 통나무는 타격 시마다 이미 DropLog()로 드롭했으므로 쓰러질 때는 호출하지 않음
        }
    }

    /// <summary>
    /// 나무가 쓰러지는 애니메이션 시작
    /// </summary>
    void StartFallAnimation()
    {
        if (currentState == 1) return; // 이미 falling 중이면 무시

        currentState = 1; // falling 상태

        // 플레이어와만 충돌 무시 (바닥과는 충돌 유지)
        IgnorePlayerCollision(true);

        // Full Tree에 Rigidbody 추가하여 물리 시뮬레이션
        if (fullTree != null)
        {
            // Concave MeshCollider 문제 해결: Rigidbody 추가 전에 MeshCollider 처리
            PrepareCollidersForRigidbody(fullTree);

            fallRigidbody = fullTree.GetComponent<Rigidbody>();
            if (fallRigidbody == null)
            {
                fallRigidbody = fullTree.AddComponent<Rigidbody>();
            }

            // Rigidbody 설정 (Inspector에서 조절 가능한 값 사용)
            fallRigidbody.mass = treeMass;
            fallRigidbody.linearDamping = linearDamping;
            fallRigidbody.angularDamping = angularDamping;
            fallRigidbody.useGravity = true;
            fallRigidbody.isKinematic = false;
            
            // Rigidbody의 초기 속도를 0으로 설정 (위로 튀어오르는 문제 방지)
            fallRigidbody.linearVelocity = Vector3.zero;
            fallRigidbody.angularVelocity = Vector3.zero;

            // 랜덤한 방향으로 쓰러지도록 각속도 추가 (위쪽 회전 제거)
            Vector3 randomTorque = new Vector3(
                Random.Range(-fallAngularVelocity, fallAngularVelocity),
                0f, // Y축 회전 제거 (위로 튀어오르는 문제 방지)
                Random.Range(-fallAngularVelocity, fallAngularVelocity)
            );
            fallRigidbody.AddTorque(randomTorque, ForceMode.VelocityChange);

            // 약간의 앞쪽 힘 추가 (더 자연스러운 쓰러짐, Inspector에서 조절 가능)
            // 위쪽 힘은 제거하고 수평 방향만 적용
            Vector3 forwardDir = fullTree.transform.forward;
            forwardDir.y = 0f; // Y축 제거
            forwardDir.Normalize();
            fallRigidbody.AddForce(forwardDir * Random.Range(fallForceMin, fallForceMax), ForceMode.Force);
        }

        // 지면 감지 코루틴 시작
        fallCoroutine = StartCoroutine(CheckGroundAndDisappear());
    }

    /// <summary>
    /// Rigidbody 추가 전에 Collider 준비 (Concave MeshCollider 문제 해결)
    /// CapsuleCollider를 우선 사용하고, 없으면 MeshCollider를 convex로 변경
    /// </summary>
    void PrepareCollidersForRigidbody(GameObject treeObject)
    {
        if (treeObject == null) return;

        // CapsuleCollider가 있는지 먼저 확인
        CapsuleCollider[] capsuleColliders = treeObject.GetComponentsInChildren<CapsuleCollider>(true);
        
        if (capsuleColliders != null && capsuleColliders.Length > 0)
        {
            // CapsuleCollider가 있으면 MeshCollider 비활성화하고 CapsuleCollider 사용
            MeshCollider[] meshColliders = treeObject.GetComponentsInChildren<MeshCollider>(true);
            foreach (var meshCol in meshColliders)
            {
                if (meshCol != null)
                {
                    meshCol.enabled = false; // MeshCollider 비활성화
                }
            }
            
            // CapsuleCollider 활성화
            foreach (var capsuleCol in capsuleColliders)
            {
                if (capsuleCol != null)
                {
                    capsuleCol.enabled = true;
                }
            }
        }
        else
        {
            // CapsuleCollider가 없으면 MeshCollider를 convex로 변경
            MeshCollider[] meshColliders = treeObject.GetComponentsInChildren<MeshCollider>(true);
            
            foreach (var meshCol in meshColliders)
            {
                if (meshCol == null) continue;

                // Concave MeshCollider인 경우 Convex로 변경
                if (!meshCol.convex)
                {
                    meshCol.convex = true;
                    meshCol.enabled = true;
                }
            }
        }
    }

    /// <summary>
    /// 지면에 닿았는지 확인하고, 닿으면 일정 시간 후 사라지도록 처리
    /// </summary>
    IEnumerator CheckGroundAndDisappear()
    {
        if (fullTree == null) yield break;

        float checkInterval = 0.1f; // 0.1초마다 체크
        float groundHitTime = 0f;
        bool hasHitGround = false;

        while (true)
        {
            // 지면 감지 (Raycast 사용)
            RaycastHit hit;
            Vector3 checkPosition = fullTree.transform.position;
            
            if (Physics.Raycast(checkPosition, Vector3.down, out hit, groundCheckDistance))
            {
                // 지면에 닿았음
                if (!hasHitGround)
                {
                    hasHitGround = true;
                    groundHitTime = Time.time;
                }
                else
                {
                    // 지면에 닿은 후 일정 시간이 지나면 비활성화
                    if (Time.time - groundHitTime >= disappearDelayAfterGroundHit)
                    {
                        if (fullTree != null)
                            fullTree.SetActive(false);

                        if (enableRespawn)
                        {
                            // 리젠 사용 시: 이 오브젝트는 활성 유지하고, respawnDelay 후 원상 복구
                            StartCoroutine(RespawnAfterDelay());
                        }
                        else
                        {
                            gameObject.SetActive(false);
                        }
                        yield break;
                    }
                }
            }

            yield return new WaitForSeconds(checkInterval);
        }
    }

    /// <summary>
    /// Fall 후 비활성화된 뒤 respawnDelay 초가 지나면 이 나무 자신을 원상태로 리젠.
    /// </summary>
    IEnumerator RespawnAfterDelay()
    {
        float delay = respawnDelay;
        if (useRespawnDelayRandom)
            delay = Random.Range(Mathf.Min(respawnDelayMin, respawnDelayMax), Mathf.Max(respawnDelayMin, respawnDelayMax));

        yield return new WaitForSeconds(delay);

        Respawn();
    }

    /// <summary>
    /// 플레이어와의 충돌 무시/복구
    /// </summary>
    void IgnorePlayerCollision(bool ignore)
    {
        if (playerObject == null) { playerObject = GameObject.FindGameObjectWithTag("Player"); }
        if (playerObject == null) return;

        Collider[] playerColliders = playerObject.GetComponentsInChildren<Collider>();
        if (treeColliders == null || treeColliders.Length == 0) { treeColliders = GetComponentsInChildren<Collider>(true); }

        foreach (var treeCol in treeColliders)
        {
            if (treeCol == null) continue;

            foreach (var playerCol in playerColliders)
            {
                if (playerCol == null) continue;
                Physics.IgnoreCollision(treeCol, playerCol, ignore);
            }
        }
    }

    /// <summary>
    /// 통나무(또는 임의 오브젝트)와 플레이어 간 충돌을 무시합니다. 통나무는 바닥 등과만 충돌합니다.
    /// </summary>
    void SetIgnoreCollisionWithPlayer(GameObject other, bool ignore = true)
    {
        if (other == null) return;
        if (playerObject == null) { playerObject = GameObject.FindGameObjectWithTag("Player"); }
        if (playerObject == null) return;

        Collider[] playerColliders = playerObject.GetComponentsInChildren<Collider>();
        Collider[] otherColliders = other.GetComponentsInChildren<Collider>();

        foreach (var c in otherColliders)
        {
            if (c == null) continue;
            foreach (var p in playerColliders)
            {
                if (p == null) continue;
                Physics.IgnoreCollision(c, p, ignore);
            }
        }
    }

    void SetState(int state)
    {
        currentState = state;

        if (state == 0) // Full Tree
        {
            if (fullTree != null) fullTree.SetActive(true);
        }
        else if (state == 1) // Falling (Full Tree는 계속 활성화, Rigidbody로 쓰러짐)
        {
            // Full Tree는 활성화 상태 유지 (쓰러지는 애니메이션)
        }

        // IsFullyGrown 플래그 (외부 호환성 유지)
        IsFullyGrown = (state == 0);
    }

    public System.Action OnTreeDestroyed;

    /// <summary>
    /// Home 버튼/스테이지 리스폰 시 호출. 쓰러진(비활성) 나무를 초기 상태로 복구 후 활성화.
    /// enableRespawnAnimation이 켜져 있으면 땅에서 솟아오르는 연출 후 플레이어 밀어내기.
    /// </summary>
    public void Respawn()
    {
        if (fallCoroutine != null)
        {
            StopCoroutine(fallCoroutine);
            fallCoroutine = null;
        }
        if (fullTree != null)
        {
            var rb = fullTree.GetComponent<Rigidbody>();
            if (rb != null)
                Destroy(rb);
            fullTree.transform.localRotation = fullTreeInitialLocalRot;
            if (enableRespawnAnimation)
            {
                fullTree.transform.localPosition = fullTreeInitialLocalPos + Vector3.up * respawnStartOffsetY;
            }
            else
            {
                fullTree.transform.localPosition = fullTreeInitialLocalPos;
            }
            fullTree.SetActive(true);
        }
        IgnorePlayerCollision(false);
        obtainedSoFar = 0;
        currentState = 0;
        SetState(0);
        if (fullTree != null)
            fullTree.SetActive(true);
        gameObject.SetActive(true);

        if (enableRespawnAnimation && fullTree != null)
            StartCoroutine(RespawnEmergeAndSettle());
        else
            TryPushPlayerIfOverlapping();
    }

    /// <summary>
    /// 리젠 시 땅에서 솟아오르고, 살짝 위로 튀었다가 제자리에 안착하는 애니메이션.
    /// </summary>
    IEnumerator RespawnEmergeAndSettle()
    {
        Transform treeT = fullTree.transform;
        Vector3 restPos = fullTreeInitialLocalPos;
        Vector3 startPos = restPos + Vector3.up * respawnStartOffsetY;
        Vector3 popTopPos = restPos + Vector3.up * respawnPopHeight;

        // 1) 땅에서 올라오기 (startPos -> restPos)
        float elapsed = 0f;
        while (elapsed < respawnEmergeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / respawnEmergeDuration);
            t = t * t * (3f - 2f * t);
            treeT.localPosition = Vector3.Lerp(startPos, restPos, t);
            yield return null;
        }
        treeT.localPosition = restPos;

        // 2) 위로 살짝 튀어오르기 (restPos -> popTopPos)
        elapsed = 0f;
        float popDuration = respawnSettleDuration * 0.5f;
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / popDuration);
            t = t * t;
            treeT.localPosition = Vector3.Lerp(restPos, popTopPos, t);
            yield return null;
        }
        treeT.localPosition = popTopPos;

        // 3) 제자리에 안착 (popTopPos -> restPos)
        elapsed = 0f;
        while (elapsed < respawnSettleDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / respawnSettleDuration);
            t = 1f - (1f - t) * (1f - t);
            treeT.localPosition = Vector3.Lerp(popTopPos, restPos, t);
            yield return null;
        }
        treeT.localPosition = restPos;

        TryPushPlayerIfOverlapping();
    }

    /// <summary>
    /// 리젠 직후 플레이어가 나무 위치와 겹쳐 있으면 짧은 시간에 걸쳐 부드럽게 밀어냄.
    /// </summary>
    void TryPushPlayerIfOverlapping()
    {
        if (respawnPushDistance <= 0f) return;
        if (playerObject == null) { playerObject = GameObject.FindGameObjectWithTag("Player"); }
        if (playerObject == null) return;

        Vector3 treeCenter = transform.position;
        Vector3 playerPos = playerObject.transform.position;
        Vector3 toPlayer = playerPos - treeCenter;
        toPlayer.y = 0f;
        float distXZ = toPlayer.magnitude;
        if (distXZ <= 0.01f) return;
        if (distXZ > respawnOverlapCheckRadius) return;

        Vector3 pushDir = toPlayer.normalized;
        IgnorePlayerCollision(true);
        StartCoroutine(PushPlayerOverTime(pushDir));
    }

    /// <summary>
    /// respawnPushDuration 동안 매 프레임 조금씩 밀어서 자연스럽게 밀리는 느낌을 줌.
    /// </summary>
    IEnumerator PushPlayerOverTime(Vector3 pushDir)
    {
        float duration = Mathf.Max(0.05f, respawnPushDuration);
        float elapsed = 0f;
        Vector3 startPos = playerObject.transform.position;
        var cc = playerObject.GetComponent<CharacterController>();
        Rigidbody rb = (cc == null) ? playerObject.GetComponent<Rigidbody>() : null;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            float currentDist = respawnPushDistance * t;
            Vector3 targetPos = startPos + pushDir * currentDist;
            targetPos.y = playerObject.transform.position.y;

            if (cc != null)
            {
                Vector3 delta = targetPos - playerObject.transform.position;
                delta.y = 0f;
                if (delta.sqrMagnitude > 0.0001f)
                    cc.Move(delta);
            }
            else if (rb != null && !rb.isKinematic)
            {
                Vector3 pos = rb.position;
                pos.x = targetPos.x;
                pos.z = targetPos.z;
                rb.MovePosition(pos);
            }

            yield return null;
        }

        if (respawnIgnoreCollisionDuration > 0f)
            StartCoroutine(RespawnRestoreCollisionAfterDelay(respawnIgnoreCollisionDuration));
        else
            IgnorePlayerCollision(false);
    }

    IEnumerator RespawnRestoreCollisionAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        IgnorePlayerCollision(false);
    }

    /// <param name="count">이번 타격으로 드롭할 통나무 갯수 (무기 자원 획득량 또는 남은 수량)</param>
    void DropLog(int count)
    {
        count = Mathf.Max(0, count);
        if (count == 0) return;

        if (enableDropPopup)
        {
            var panel = dropItemPanel != null ? dropItemPanel : DropItemPanelManager.Instance;
            if (panel != null)
            {
                Texture icon = GetDropPopupIcon();
                panel.ShowDrop(GetHitEffectSpawnPosition(), icon, count);
            }
        }

        // Full Tree의 위치를 기준으로 통나무 스폰
        Vector3 spawnBase = fullTree != null ? fullTree.transform.position : transform.position;

        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(0f, 360f);
            Vector3 lateralDir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

            Vector3 spawnPos =
                spawnBase +
                lateralDir * Random.Range(logSpawnRadiusMin, logSpawnRadiusMax) +
                Vector3.up * Random.Range(logSpawnHeightMin, logSpawnHeightMax);

            Quaternion rot = Quaternion.Euler(
                Random.Range(0f, 360f),
                Random.Range(0f, 360f),
                Random.Range(0f, 360f)
            );

            GameObject log = Instantiate(logPrefab, spawnPos, rot);
            SetIgnoreCollisionWithPlayer(log, true); // 통나무 ↔ 플레이어 충돌 없음 (바닥 등과만 충돌)

            var logItem = log.GetComponent<LogItem>();
            if (logItem != null)
            {
                logItem.price = logPrice;
                string prefabName = gameObject.name;
                if (prefabName.EndsWith("(Clone)"))
                    prefabName = prefabName.Substring(0, prefabName.Length - 7);
                logItem.sourceTreePrefabName = prefabName;
                // 수집 키: 프리팹의 LogItem.itemKey가 비어 있지 않으면 그대로 사용(인벤토리·업그레이드와 동일 키), 비어 있으면 프리팹 이름
                var prefabLogItem = logPrefab.GetComponent<LogItem>();
                if (prefabLogItem != null && !string.IsNullOrEmpty(prefabLogItem.itemKey))
                    logItem.itemKey = prefabLogItem.itemKey;
                else
                    logItem.itemKey = logPrefab.name;
                // 아이콘은 Log Item 프리팹에 설정된 Icon Texture·Drop Popup Icon만 사용 (런타임 덮어쓰기 안 함)
            }

            if (logTintColor.a > 0.01f)
            {
                foreach (var r in log.GetComponentsInChildren<Renderer>())
                {
                    if (r.sharedMaterial == null) continue;
                    var mat = r.material;
                    
                    // 여러 Material 속성 이름 시도 (URP, Built-in, Shader Graph 등)
                    bool colorApplied = false;
                    
                    // URP Shader Graph 속성들
                    if (mat.HasProperty("_BaseColor"))
                    {
                        mat.SetColor("_BaseColor", logTintColor);
                        colorApplied = true;
                    }
                    // Built-in Standard Shader
                    else if (mat.HasProperty("_Color"))
                    {
                        mat.SetColor("_Color", logTintColor);
                        colorApplied = true;
                    }
                    // 다른 가능한 속성 이름들
                    else if (mat.HasProperty("_MainColor"))
                    {
                        mat.SetColor("_MainColor", logTintColor);
                        colorApplied = true;
                    }
                    else if (mat.HasProperty("_TintColor"))
                    {
                        mat.SetColor("_TintColor", logTintColor);
                        colorApplied = true;
                    }
                    else if (mat.HasProperty("_AlbedoColor"))
                    {
                        mat.SetColor("_AlbedoColor", logTintColor);
                        colorApplied = true;
                    }
                    
                    // 색상이 적용되었고 텍스처를 흰색으로 교체하려는 경우
                    if (colorApplied)
                    {
                        // 텍스처를 흰색으로 교체 (선택사항)
                        if (mat.HasProperty("_BaseMap"))
                        {
                            mat.SetTexture("_BaseMap", Texture2D.whiteTexture);
                        }
                        else if (mat.HasProperty("_MainTex"))
                        {
                            mat.SetTexture("_MainTex", Texture2D.whiteTexture);
                        }
                        else if (mat.HasProperty("_Albedo_Texture"))
                        {
                            mat.SetTexture("_Albedo_Texture", Texture2D.whiteTexture);
                        }
                    }
                    else
                    {
                        // 속성을 찾지 못한 경우 디버그 로그
                        Debug.LogWarning($"[TreeManager] 통나무 Material에 색상 속성을 찾을 수 없습니다. Material: {mat.name}, Shader: {mat.shader.name}");
                    }
                }
            }

            Rigidbody rb = log.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 randomDir = (lateralDir * Random.Range(0.6f, 1.5f) +
                                     Vector3.up * Random.Range(0.8f, 1.6f) +
                                     Random.insideUnitSphere * 0.2f).normalized;

                rb.AddForce(randomDir * Random.Range(2f, 4f), ForceMode.Impulse);
            }
        }
    }

    /// <summary>
    /// DropItemPanel 팝업에 쓸 아이콘. 통나무 프리팹(LogItem)의 dropPopupIcon → iconTexture 순으로 사용, 없으면 나무 RawImage.
    /// </summary>
    Texture GetDropPopupIcon()
    {
        if (logPrefab != null)
        {
            var logItem = logPrefab.GetComponent<LogItem>();
            if (logItem != null)
            {
                if (logItem.dropPopupIcon != null) return logItem.dropPopupIcon;
                if (logItem.iconTexture != null) return logItem.iconTexture;
            }
        }
        var treeRaw = GetComponentInChildren<RawImage>(true);
        return (treeRaw != null && treeRaw.texture != null) ? treeRaw.texture : null;
    }

    // 외부에서 검사할 수 있는 상태 플래그
    public bool IsFullyGrown { get; private set; } = true;

    // 공격 가능한지 여부
    public bool IsAttackable => currentState == 0;
    /// <summary>현재까지 획득한 수량. Max 도달 시 쓰러짐.</summary>
    public int ObtainedSoFar => obtainedSoFar;
    /// <summary>남은 획득 가능 수량. AutoAttack에서 이번 타격으로 쓰러질지 판단할 때 사용.</summary>
    public int CurrentHealth => Mathf.Max(0, maxObtainableQuantity - obtainedSoFar);
    public bool IsStump => false; // 밑둥 기능 제거

    GameObject GetActiveModel()
    {
        if (currentState == 0 && fullTree != null) return fullTree;
        return null;
    }

    // ---- VFX / SFX / Shake 구현 ----
    void PlayHitEffects()
    {
        // SFX 재생
        if (hitSfx != null)
        {
            Vector3 pos = GetHitEffectSpawnPosition();
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayClipAtPoint(hitSfx, pos, SoundManager.SoundCategory.Hit);
            }
            else
            {
                GameObject audioGo = new GameObject("TempHitAudio");
                audioGo.transform.position = pos;
                var src = audioGo.AddComponent<AudioSource>();
                src.clip = hitSfx;
                src.spatialBlend = 0f;
                src.playOnAwake = false;
                src.volume = 1f;
                src.Play();
                Destroy(audioGo, (hitSfx.length > 0f) ? hitSfx.length + 0.2f : 2f);
            }
        }

        // 트리 모델에 Animator가 있으면 Shake 트리거 시도
        GameObject activeModel = GetActiveModel();
        if (activeModel != null)
        {
            var anim = activeModel.GetComponent<Animator>();
            if (anim != null && anim.HasState(0, Animator.StringToHash("Shake")))
            {
                anim.SetTrigger("Shake");
            }
        }

        // 코드 기반 흔들림 (모델 루트)
        StartCoroutine(ShakeRoutine());
    }

    Vector3 GetHitEffectSpawnPosition()
    {
        Collider col = GetComponentInChildren<Collider>();
        if (col != null)
            return col.ClosestPoint(transform.position) + Vector3.up * 1.0f;

        GameObject active = GetActiveModel();
        if (active != null)
            return active.transform.position + Vector3.up * 1.2f;

        return transform.position + Vector3.up * 1.2f;
    }

    IEnumerator ShakeRoutine()
    {
        GameObject model = GetActiveModel();
        if (model == null) yield break;

        Transform t = model.transform;
        Quaternion originalRot = t.localRotation;
        Vector3 originalPos = t.localPosition;

        float elapsed = 0f;

        float posIntensity = Mathf.Max(0.1f, shakeIntensity * 0.02f);
        float rotIntensity = shakeIntensity;

        while (elapsed < shakeDuration)
        {
            float damper = 1f - (elapsed / shakeDuration);

            Vector3 shakeEuler = new Vector3(
                (Mathf.PerlinNoise(Time.time * 20f, 0f) - 0.5f) * 2f,
                (Mathf.PerlinNoise(0f, Time.time * 20f) - 0.5f) * 2f,
                (Mathf.PerlinNoise(Time.time * 12f, Time.time * 12f) - 0.5f) * 2f
            ) * (rotIntensity * 0.15f * damper);

            Vector3 shakePos = new Vector3(
                (Mathf.PerlinNoise(Time.time * 25f, 0f) - 0.5f) * 2f,
                (Mathf.PerlinNoise(0f, Time.time * 25f) - 0.5f) * 2f,
                (Mathf.PerlinNoise(Time.time * 18f, Time.time * 18f) - 0.5f) * 2f
            ) * (posIntensity * damper);

            t.localRotation = originalRot * Quaternion.Euler(shakeEuler);
            t.localPosition = originalPos + shakePos;

            elapsed += Time.deltaTime;
            yield return null;
        }

        t.localRotation = originalRot;
        t.localPosition = originalPos;
    }
}
