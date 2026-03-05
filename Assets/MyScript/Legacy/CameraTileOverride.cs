using UnityEngine;

/// <summary>
/// 플레이어가 이 타일 트리거 안으로 들어오면
/// 메인 카메라의 CameraFollowSmoothDamp 오프셋들을 일시적으로 덮어쓴다.
/// 나가면 CameraFollowSmoothDamp가 기억한 기본값으로 복구.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CameraTileOverride : MonoBehaviour
{
    [Header("카메라 찾기 설정")]
    [Tooltip("비워두면 씬의 MainCamera에서 CameraFollowSmoothDamp를 자동으로 찾습니다.")]
    public CameraFollowSmoothDamp cameraFollow;

    [Header("Override 설정")]
    [Tooltip("위치 오프셋을 덮어쓸지 여부")]
    public bool overridePositionOffset = true;
    [Tooltip("새 위치 오프셋 (CameraFollowSmoothDamp.offset에 적용)")]
    public Vector3 positionOffset = new Vector3(0f, 12f, -9f);

    [Tooltip("회전(바라보는 지점) 오프셋을 덮어쓸지 여부")]
    public bool overrideLookAtOffset = false;
    [Tooltip("새 회전 오프셋 (CameraFollowSmoothDamp.lookAtOffset에 적용)")]
    public Vector3 lookAtOffset = new Vector3(0f, 1.5f, 0f);

    [Header("추가 옵션 (선택)")]
    [Tooltip("이 존에 들어왔을 때 smoothTime을 덮어쓸지 여부")]
    public bool overrideSmoothTime = false;
    public float smoothTime = 0.25f;

    [Tooltip("이 존에 들어왔을 때 rotationSpeed를 덮어쓸지 여부")]
    public bool overrideRotationSpeed = false;
    public float rotationSpeed = 10f;

    void Reset()
    {
        // 자동으로 트리거로 설정
        var col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    void Awake()
    {
        if (cameraFollow == null && Camera.main != null)
            cameraFollow = Camera.main.GetComponent<CameraFollowSmoothDamp>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (cameraFollow == null) return;

        // 카메라에 이 타일 기준 오버라이드 적용
        cameraFollow.ApplyOverride(
            this,
            overridePositionOffset, positionOffset,
            overrideLookAtOffset, lookAtOffset,
            overrideSmoothTime, smoothTime,
            overrideRotationSpeed, rotationSpeed
        );
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (cameraFollow == null) return;

        // 이 타일이 마지막으로 적용한 오버라이드라면 기본값으로 복구
        cameraFollow.ClearOverride(this);
    }
}

