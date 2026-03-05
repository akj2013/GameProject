using UnityEngine;

/// <summary>
/// 타겟(플레이어 등)을 SmoothDamp로 부드럽게 따라가는 카메라용 스크립트.
/// 메인 카메라에 붙이고, Target에 따라갈 오브젝트를 넣으면 됩니다.
/// (카메라가 타겟의 자식이면 이 스크립트 대신 부모가 끌고 가므로, 사용 시 카메라를 타겟에서 분리한 뒤 사용하세요.)
/// </summary>
public class CameraFollowSmoothDamp : MonoBehaviour
{
    [Header("따라갈 대상")]
    [Tooltip("플레이어 등 따라갈 오브젝트. 비워두면 Tag가 'Player'인 오브젝트를 자동으로 찾습니다.")]
    [SerializeField] Transform target;

    [Header("카메라 위치 오프셋")]
    [Tooltip("타겟 위치에서 카메라가 있을 상대 위치. 예: (0, 12, -9) = 타겟 뒤 위쪽")]
    [SerializeField] Vector3 offset = new Vector3(0f, 12f, -9f);

    [Header("부드러움")]
    [Tooltip("따라가는 속도. 작을수록 빨리 붙고, 클수록 더 부드럽고 느리게 따라갑니다. (권장: 0.15 ~ 0.4)")]
    [SerializeField, Range(0.05f, 1f)] float smoothTime = 0.25f;

    [Header("회전")]
    [Tooltip("켜면 카메라가 매 프레임 타겟을 바라봅니다. 끄면 현재 회전을 유지합니다.")]
    [SerializeField] bool lookAtTarget = true;

    [Tooltip("바라볼 지점 오프셋 (타겟 위치 기준). 예: (0, 1.5, 0) = 타겟 머리 쪽을 바라봄")]
    [SerializeField] Vector3 lookAtOffset = new Vector3(0f, 1.5f, 0f);

    [Tooltip("lookAtTarget이 켜져 있을 때, 회전을 부드럽게 보간할지 여부")]
    [SerializeField] bool smoothRotation = true;

    [Tooltip("회전 보간 속도 (smoothRotation이 true일 때). 클수록 빨리 타겟을 바라봄")]
    [SerializeField, Range(1f, 20f)] float rotationSpeed = 10f;

    Vector3 _velocity = Vector3.zero;

    // 기본 카메라 세팅(씬 시작 시 값)을 저장해 두었다가, 타일 오버라이드가 끝나면 복구용
    Vector3 _defaultOffset;
    Vector3 _defaultLookAtOffset;
    float _defaultSmoothTime;
    float _defaultRotationSpeed;
    object _overrideOwner;

    void Start()
    {
        if (target == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null)
                target = go.transform;
            else
                Debug.LogWarning("[CameraFollowSmoothDamp] Target이 비어 있고, Tag 'Player' 오브젝트도 없습니다. Inspector에서 Target을 지정하세요.");
        }

        // 시작 시 인스펙터에서 설정된 값을 기본값으로 백업
        _defaultOffset = offset;
        _defaultLookAtOffset = lookAtOffset;
        _defaultSmoothTime = smoothTime;
        _defaultRotationSpeed = rotationSpeed;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 목표 위치 = 타겟 위치 + 오프셋 (월드 기준)
        Vector3 goalPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, goalPosition, ref _velocity, smoothTime);

        if (lookAtTarget)
        {
            Vector3 lookPoint = target.position + lookAtOffset;
            if (smoothRotation)
            {
                Quaternion goalRot = Quaternion.LookRotation(lookPoint - transform.position);
                transform.rotation = Quaternion.Slerp(transform.rotation, goalRot, rotationSpeed * Time.deltaTime);
            }
            else
            {
                transform.LookAt(lookPoint);
            }
        }
    }

    /// <summary>
    /// 특정 존/타일에서 카메라 세팅을 덮어쓸 때 사용. owner는 보통 this(타일 스크립트 자신)를 넘긴다.
    /// 마지막으로 호출한 owner만 유효하며, 나갈 때 ClearOverride(owner)로 복구.
    /// </summary>
    public void ApplyOverride(object owner,
        bool overridePos, Vector3 newOffset,
        bool overrideLookAt, Vector3 newLookAtOffset,
        bool overrideSmooth, float newSmoothTime,
        bool overrideRotSpeed, float newRotSpeed)
    {
        _overrideOwner = owner;

        if (overridePos) offset = newOffset;
        if (overrideLookAt) lookAtOffset = newLookAtOffset;
        if (overrideSmooth) smoothTime = newSmoothTime;
        if (overrideRotSpeed) rotationSpeed = newRotSpeed;
    }

    /// <summary>
    /// owner가 자신일 때만 오버라이드를 해제하고 기본값으로 복구.
    /// </summary>
    public void ClearOverride(object owner)
    {
        if (!ReferenceEquals(_overrideOwner, owner))
            return;

        offset = _defaultOffset;
        lookAtOffset = _defaultLookAtOffset;
        smoothTime = _defaultSmoothTime;
        rotationSpeed = _defaultRotationSpeed;
        _overrideOwner = null;
    }
}
