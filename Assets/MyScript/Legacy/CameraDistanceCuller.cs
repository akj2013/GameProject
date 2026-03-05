using UnityEngine;

/// <summary>
/// 이 오브젝트를 메인 카메라와의 거리에 따라 활성/비활성(또는 렌더러만 끄기)으로
/// 컬링합니다. CameraDistanceCullManager가 0.2초마다 거리를 체크하고,
/// 이 컴포넌트의 설정(Cull Distance / Enable Distance)에 따라 표시 여부를 결정합니다.
///
/// 사용법:
/// - 나무: 이 스크립트를 나무 프리팹 루트에 붙이고, Cull Mode = Whole Game Object.
/// - 타일(지면만 숨기고 자식 나무는 그대로): 타일 루트에 붙이고, Cull Mode = Renderers Only (Self + Children).
/// - 타일 전체(지면+자식 한꺼번에): 타일 루트에 붙이고, Cull Mode = Whole Game Object.
/// </summary>
[RequireComponent(typeof(Transform))]
public class CameraDistanceCuller : MonoBehaviour
{
    /// <summary>
    /// 컬링 방식.
    /// WholeGameObject: 거리 밖이면 GameObject.SetActive(false). 나무처럼 단일 오브젝트에 적합.
    /// RenderersOnlySelf: 이 오브젝트의 MeshRenderer/SkinnedMeshRenderer만 끔. 자식은 유지.
    /// RenderersOnlySelfAndChildren: 자신 + 자식의 모든 Renderer만 끔. 타일 지면+장식만 숨기고 자식 나무는 유지할 때 사용.
    /// </summary>
    public enum CullMode
    {
        [Tooltip("거리 밖이면 전체 오브젝트 비활성화. 나무 등 단일 오브젝트에 적합")]
        WholeGameObject,

        [Tooltip("거리 밖이면 이 오브젝트의 렌더러만 비활성화. 자식은 그대로")]
        RenderersOnlySelf,

        [Tooltip("거리 밖이면 자신+자식의 모든 렌더러만 비활성화. 타일 지면만 숨기고 자식 나무는 유지할 때")]
        RenderersOnlySelfAndChildren
    }

    [Header("거리 설정")]
    [Tooltip("카메라와의 거리가 이 값보다 크면 컬링(숨김). 단위: 유닛")]
    [SerializeField, Min(1f)] float cullDistance = 50f;

    [Tooltip("한번 숨겨진 뒤, 카메라가 이 거리 안으로 들어오면 다시 표시. cullDistance보다 작게 두면 깜빡임 방지(히스테리시스)")]
    [SerializeField, Min(1f)] float enableDistance = 40f;

    [Tooltip("타일처럼 루트에 렌더러가 없고 자식만 있을 때 켜세요. 거리를 Transform이 아닌 자식 렌더러 Bounds 중심으로 계산합니다.")]
    [SerializeField] bool useBoundsCenterForDistance = false;

    [Header("컬링 방식")]
    [Tooltip("Whole Game Object: 오브젝트 전체 끔. Renderers Only: 메시만 끄고 오브젝트/자식은 유지")]
    [SerializeField] CullMode cullMode = CullMode.WholeGameObject;

    [Header("초기 상태")]
    [Tooltip("체크 시 시작 시점에 거리 체크 한 번 하고, 멀면 처음부터 숨김")]
    [SerializeField] bool cullOnStart = true;

    [Header("플레이어 근처 예외")]
    [Tooltip("0이 아니면: 플레이어가 이 거리(유닛) 안에 있으면 컬링하지 않음. 나무/광석 등 공격 대상에 넣으면 카메라가 멀어도 플레이어가 가까우면 계속 공격 가능.")]
    [SerializeField, Min(0f)] float uncullIfPlayerWithin = 0f;

    bool _renderersVisible = true;
    Renderer[] _cachedRenderers;
    Vector3 _cachedBoundsCenter;
    bool _cachedBoundsCenterValid;

    /// <summary>현재 컬 거리(밖이면 숨김)</summary>
    public float CullDistance => cullDistance;

    /// <summary>이 거리 안이면 다시 표시</summary>
    public float EnableDistance => enableDistance;

    /// <summary>플레이어가 이 거리 안에 있으면 컬링하지 않음. 0이면 미사용.</summary>
    public float UncullIfPlayerWithin => uncullIfPlayerWithin;

    /// <summary>현재 보이는 상태인지. WholeGameObject면 activeSelf, RenderersOnly면 렌더러 상태</summary>
    public bool IsVisible
    {
        get
        {
            if (cullMode == CullMode.WholeGameObject)
                return gameObject.activeSelf;
            return _renderersVisible;
        }
    }

    void Awake()
    {
        if (cullMode != CullMode.WholeGameObject)
            CacheRenderers();
        // 매니저가 이미 있으면 미리 등록 (비활성 오브젝트는 OnEnable이 안 불리므로 Start에서 매니저가 한 번에 수집함)
        if (CameraDistanceCullManager.Instance != null)
            CameraDistanceCullManager.Instance.Register(this);
    }

    void OnEnable()
    {
        if (CameraDistanceCullManager.Instance != null)
            CameraDistanceCullManager.Instance.Register(this);
    }

    // OnDisable에서 Unregister 하지 않음. WholeGameObject 모드에서 SetActive(false) 시
    // OnDisable이 호출되는데, 여기서 빼면 매니저가 다시 활성화할 수 없음. OnDestroy에서만 해제.

    void OnDestroy()
    {
        if (CameraDistanceCullManager.Instance != null)
            CameraDistanceCullManager.Instance.Unregister(this);
    }

    void Start()
    {
        if (!cullOnStart) return;
        if (CameraDistanceCullManager.Instance == null) return;

        var cam = Camera.main;
        if (cam == null) return;

        float distance = Vector3.Distance(cam.transform.position, GetPositionForDistance());
        if (distance > cullDistance)
            ApplyCullState(false);
    }

    /// <summary>
    /// 매니저가 거리 계산에 사용할 월드 위치. 타일은 useBoundsCenterForDistance 시 Bounds 중심 사용.
    /// 비활성 오브젝트(TileStageConfig 없는 Road_02 등)도 includeInactive로 렌더러 수집해 계산.
    /// </summary>
    public Vector3 GetPositionForDistance()
    {
        if (!useBoundsCenterForDistance)
            return transform.position;

        var renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers != null && renderers.Length > 0)
        {
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    b.Encapsulate(renderers[i].bounds);
            }
            if (b.size.sqrMagnitude > 0.0001f)
            {
                _cachedBoundsCenter = b.center;
                _cachedBoundsCenterValid = true;
                return _cachedBoundsCenter;
            }
        }

        return _cachedBoundsCenterValid ? _cachedBoundsCenter : transform.position;
    }

    void CacheRenderers()
    {
        if (cullMode == CullMode.RenderersOnlySelf)
            _cachedRenderers = GetComponents<Renderer>();
        else if (cullMode == CullMode.RenderersOnlySelfAndChildren)
            _cachedRenderers = GetComponentsInChildren<Renderer>(true);
    }

    /// <summary>
    /// 매니저가 거리 체크 후 호출. 보일지 숨길지 적용합니다.
    /// </summary>
    public void ApplyCullState(bool visible)
    {
        if (cullMode == CullMode.WholeGameObject)
        {
            if (gameObject.activeSelf != visible)
                gameObject.SetActive(visible);
            return;
        }

        if (_cachedRenderers == null || _cachedRenderers.Length == 0)
            CacheRenderers();

        if (_cachedRenderers != null)
        {
            for (int i = 0; i < _cachedRenderers.Length; i++)
            {
                if (_cachedRenderers[i] != null)
                    _cachedRenderers[i].enabled = visible;
            }
        }

        _renderersVisible = visible;
    }

    void OnValidate()
    {
        if (enableDistance >= cullDistance)
            enableDistance = Mathf.Max(1f, cullDistance - 5f);
    }
}
