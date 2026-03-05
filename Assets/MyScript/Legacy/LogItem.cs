using UnityEngine;
using System.Collections;
using System;

public class LogItem : MonoBehaviour
{
    [Tooltip("판매 가격 (TreeHealth에서 설정, 없으면 기본 1)")]
    public int price = 1;
    [Tooltip("이 통나무를 생성한 나무 프리팹의 이름")]
    public string sourceTreePrefabName;
    [Tooltip("ItemCollectorPanel LogImage에 표시할 아이콘 (나무 프리팹의 RawImage 텍스처, TreeManager에서 설정)")]
    public Texture iconTexture;
    [Tooltip("DropItemPanel(드롭 시 떠오르는 x2 팝업)에 표시할 아이콘. 비어 있으면 iconTexture 사용. 통나무 이미지를 넣으면 팝업에 통나무 아이콘이 표시됨")]
    public Texture dropPopupIcon;
    /// <summary>수집 패널 구분용. TreeManager가 드롭 시 Drop Log Prefab 이름으로 설정. 비어 있으면 Log로 통합.</summary>
    [Tooltip("수집 패널 구분용. TreeManager가 드롭 시 logPrefab.name으로 설정. 비어 있으면 Log로 통합")]
    public string itemKey;

    [Header("Auto Collect Settings")]
    [Tooltip("지면에 닿은 후 자동 수집까지의 대기 시간 (초, 기본 방식)")]
    public float groundToCollectDelay = 0f;
    [Tooltip("생성 후 강제 수집까지의 최대 시간 (초, 지면에 안 닿고 끼어있는 통나무를 위한 안전장치, 0 = 비활성화)")]
    public float autoCollectDelay = 0f;

    [Header("Collect Sound")]
    [Tooltip("통나무가 플레이어로 날아올 때 재생할 사운드")]
    public AudioClip collectSound;

    const int LayerLog = 7;
    const int LayerLogOnGround = 8;

    private bool isCollected = false;
    private bool isGrounded = false;
    private bool isMovingToStack = false;
    private float spawnTime;
    private Coroutine autoCollectCoroutine;

    Rigidbody rb;
    Collider meshCol;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        meshCol = GetComponentInChildren<Collider>();
        spawnTime = Time.time;
        SetLayerRecursively(gameObject, LayerLog);
        
        // 생성 후 일정 시간이 지나도 수집되지 않으면 강제 수집 (안전장치)
        if (autoCollectDelay > 0f)
        {
            autoCollectCoroutine = StartCoroutine(ForceCollectAfterDelay());
        }
    }
    
    void OnDestroy()
    {
        // 코루틴 정리
        if (autoCollectCoroutine != null)
        {
            StopCoroutine(autoCollectCoroutine);
            autoCollectCoroutine = null;
        }
    }
    
    /// <summary>
    /// 생성 후 일정 시간이 지나도 수집되지 않으면 강제로 수집 (안전장치)
    /// </summary>
    IEnumerator ForceCollectAfterDelay()
    {
        yield return new WaitForSeconds(autoCollectDelay);
        
        // 이미 수집 중이거나 수집 완료되었으면 무시
        if (!isMovingToStack && !isCollected)
        {
            // 지면 충돌 여부와 관계없이 강제로 수집 시작
            StartCoroutine(AutoMoveToStackRoot());
        }
    }

    void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.CompareTag("Ground"))
        {
            SetGrounded();
        }
        else
        {
            // 맵 바닥이 "Ground" 태그가 없어도, 바닥/지형과 충돌하면 줍기 가능
            if (col.contactCount > 0)
            {
                var contact = col.GetContact(0);
                if (Vector3.Dot(contact.normal, Vector3.up) > 0.5f) // 위를 향한 표면 = 바닥
                    SetGrounded();
            }
        }
    }

    void SetGrounded()
    {
        if (isGrounded) return;
        isGrounded = true;
        SetLayerRecursively(gameObject, LayerLogOnGround);
        
        // 땅에 닿으면 대기 시간 후 자동으로 플레이어(캐릭터)로 날아가 사라짐
        StartCoroutine(AutoMoveToStackRoot());
    }

    static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        for (int i = 0; i < go.transform.childCount; i++)
            SetLayerRecursively(go.transform.GetChild(i).gameObject, layer);
    }

    public bool CanCollect()
    {
        // 더 이상 사용하지 않음 (자동 수집으로 변경)
        return false;
    }

    /// <summary>아직 수집/이동 중이 아니어서 수집 가능한지 (HOME 버튼 즉시 회수용)</summary>
    public bool CanBeCollected => !isCollected && !isMovingToStack;

    /// <summary>ItemCollectorController 패널 키. Drop Log Prefab별로 패널이 나뉨. 비어 있으면 Log.</summary>
    public string GetCollectorKey() => string.IsNullOrEmpty(itemKey) ? ItemCollectorController.LogItemKey : itemKey;
    /// <summary>패널/팝업용 아이콘. Log Item에 설정된 Drop Popup Icon 또는 Icon Texture만 사용.</summary>
    public Texture GetCollectorIcon() => dropPopupIcon != null ? dropPopupIcon : iconTexture;

    /// <summary>캐릭터(플레이어) 위치로 날아온 뒤 사라지고 onArrived 콜백 호출. 스택에는 쌓지 않음.</summary>
    public void FlyToPlayer(Transform playerTarget, Action onArrived)
    {
        if (isCollected) return;
        isCollected = true;
        isMovingToStack = true;

        if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }
        if (meshCol != null) meshCol.enabled = false;

        // 수집 사운드 재생 (GameManager의 collectVolume 사용)
        PlayCollectSound();

        StartCoroutine(FlyToPlayerAnimation(playerTarget, onArrived));
    }

    void PlayCollectSound()
    {
        if (collectSound == null) return;
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayClipAtPoint(collectSound, transform.position, SoundManager.SoundCategory.Collect);
        }
        else
        {
            // SoundManager 없을 때 폴백
            GameObject audioGo = new GameObject("CollectSound");
            audioGo.transform.position = transform.position;
            AudioSource src = audioGo.AddComponent<AudioSource>();
            src.clip = collectSound;
            src.spatialBlend = 0f;
            src.playOnAwake = false;
            src.volume = 1f;
            src.Play();
            Destroy(audioGo, collectSound.length + 0.2f);
        }
    }

    IEnumerator FlyToPlayerAnimation(Transform playerTarget, Action onArrived)
    {
        if (playerTarget == null) { isMovingToStack = false; yield break; }
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float duration = 0.35f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            if (t > 1f) t = 1f;
            transform.position = Vector3.Lerp(startPos, playerTarget.position + Vector3.up * 0.5f, t);
            transform.rotation = Quaternion.Slerp(startRot, Quaternion.identity, t);
            yield return null;
        }

        isMovingToStack = false;
        onArrived?.Invoke();
        Destroy(gameObject);
    }

    IEnumerator AutoMoveToStackRoot()
    {
        // 이미 이동 중이면 무시
        if (isMovingToStack || isCollected) yield break;

        // 지면에 닿은 후 대기 시간 (기본 방식)
        // 단, 강제 수집(ForceCollectAfterDelay)에서 호출된 경우는 이미 대기 시간이 지났으므로 즉시 수집
        if (isGrounded && groundToCollectDelay > 0f)
        {
            yield return new WaitForSeconds(groundToCollectDelay);
        }

        // 다시 한 번 체크 (대기 중에 이미 수집되었을 수 있음)
        if (isMovingToStack || isCollected) yield break;

        isMovingToStack = true;
        isCollected = true;
        
        // 강제 수집 코루틴 정리 (이미 수집 중이므로 더 이상 필요 없음)
        if (autoCollectCoroutine != null)
        {
            StopCoroutine(autoCollectCoroutine);
            autoCollectCoroutine = null;
        }

        // 물리 비활성화
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        if (meshCol != null)
            meshCol.enabled = false;

        // 수집 사운드 재생 (GameManager의 collectVolume 사용)
        PlayCollectSound();

        // 플레이어(캐릭터) 찾기 — 그 위치로 날아간 뒤 사라짐
        PlayerCollector collector = FindFirstObjectByType<PlayerCollector>();
        if (collector == null)
        {
            isMovingToStack = false;
            isCollected = false;
            yield break;
        }

        Transform playerT = collector.transform;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 velocity = Vector3.zero;
        Vector3 targetOffset = Vector3.up * 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Vector3 targetPos = playerT.position + targetOffset;
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, 0.15f);
            transform.rotation = Quaternion.Slerp(startRot, Quaternion.identity, elapsed / duration);
            yield return null;
        }

        if (collector != null)
            collector.AddLogCount(GetCollectorKey(), GetCollectorIcon(), price);
        Destroy(gameObject);
    }

}
