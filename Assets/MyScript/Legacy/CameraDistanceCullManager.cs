using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 메인 카메라와의 거리를 주기적으로 체크하여, 등록된 오브젝트(나무/타일)를
/// 거리 구간에 따라 활성/비활성 처리하는 매니저.
/// 씬에 하나만 두고, 각 나무/타일에는 CameraDistanceCuller를 붙입니다.
/// </summary>
public class CameraDistanceCullManager : MonoBehaviour
{
    public static CameraDistanceCullManager Instance { get; private set; }

    [Header("카메라")]
    [Tooltip("거리 측정에 사용할 카메라. 비워두면 Camera.main 사용")]
    [SerializeField] Camera targetCamera;

    [Header("체크 주기")]
    [Tooltip("거리 체크 간격(초). 0.2 = 0.2초마다 체크 (권장: 0.15~0.3)")]
    [SerializeField, Range(0.05f, 1f)] float checkInterval = 0.2f;

    [Header("디버그 (선택)")]
    [Tooltip("켜두면 콘솔에 등록된 컬러 수/활성 수를 주기적으로 출력")]
    [SerializeField] bool logStats = false;

    readonly List<CameraDistanceCuller> _cullers = new List<CameraDistanceCuller>();
    float _nextCheckTime;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        _cullers.Clear();
    }

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
        _nextCheckTime = Time.time + checkInterval;

        // TileStageConfig 없는 타일(Road_02 등)처럼 비활성으로 시작하는 오브젝트도 수집
        var allCullers = FindObjectsByType<CameraDistanceCuller>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in allCullers)
            Register(c);
    }

    void Update()
    {
        if (Time.time < _nextCheckTime) return;
        _nextCheckTime = Time.time + checkInterval;

        DoDistanceCheck();
    }

    /// <summary>
    /// 컬러 등록. CameraDistanceCuller가 Awake/OnEnable에서 호출합니다.
    /// </summary>
    public void Register(CameraDistanceCuller culler)
    {
        if (culler != null && !_cullers.Contains(culler))
            _cullers.Add(culler);
    }

    /// <summary>
    /// 컬러 해제. CameraDistanceCuller가 OnDestroy에서 호출합니다.
    /// </summary>
    public void Unregister(CameraDistanceCuller culler)
    {
        if (culler != null)
            _cullers.Remove(culler);
    }

    void DoDistanceCheck()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
        if (targetCamera == null) return;

        Vector3 camPos = targetCamera.transform.position;
        int activeCount = 0;
        Transform playerTransform = null;

        for (int i = _cullers.Count - 1; i >= 0; i--)
        {
            var c = _cullers[i];
            if (c == null)
            {
                _cullers.RemoveAt(i);
                continue;
            }

            float distance = Vector3.Distance(camPos, c.GetPositionForDistance());

            if (c.IsVisible)
            {
                if (distance > c.CullDistance)
                {
                    if (c.UncullIfPlayerWithin > 0f)
                    {
                        if (playerTransform == null)
                        {
                            var go = GameObject.FindGameObjectWithTag("Player");
                            playerTransform = go != null ? go.transform : null;
                        }
                        if (playerTransform != null && Vector3.Distance(playerTransform.position, c.GetPositionForDistance()) < c.UncullIfPlayerWithin)
                        {
                            activeCount++;
                            continue;
                        }
                    }
                    c.ApplyCullState(false);
                }
                else
                    activeCount++;
            }
            else
            {
                bool shouldShow = distance < c.EnableDistance;
                if (!shouldShow && c.UncullIfPlayerWithin > 0f)
                {
                    if (playerTransform == null)
                    {
                        var go = GameObject.FindGameObjectWithTag("Player");
                        playerTransform = go != null ? go.transform : null;
                    }
                    if (playerTransform != null && Vector3.Distance(playerTransform.position, c.GetPositionForDistance()) < c.UncullIfPlayerWithin)
                        shouldShow = true;
                }
                if (shouldShow)
                {
                    c.ApplyCullState(true);
                    activeCount++;
                }
            }
        }

        if (logStats)
            Debug.Log($"[CameraDistanceCullManager] Cullers: {_cullers.Count}, Visible: {activeCount}");
    }
}
