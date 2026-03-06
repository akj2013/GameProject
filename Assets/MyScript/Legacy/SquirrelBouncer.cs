using UnityEngine;

public class SquirrelBouncer : MonoBehaviour
{
    [Header("이동 설정")]
    public float hopDistance = 0.5f;     // 한 번 점프할 때 평면 이동 거리
    public float hopDuration = 0.4f;     // 한 번 점프에 걸리는 시간
    public float idleDelayMin = 0.1f;    // 점프 사이 최소 대기
    public float idleDelayMax = 0.4f;    // 점프 사이 최대 대기

    [Header("바운스 / 스쿼시 설정")]
    public float hopHeight = 0.25f;      // 점프 높이
    public float squashAmount = 0.2f;    // 착지 시 얼마나 납작해질지 (0.2 = 20% 정도)
    public float squashSpeed = 10f;      // 스쿼시/스트레치 속도

    [Header("지면 레이어")]
    public LayerMask groundLayer = ~0;   // 필요하면 Ground 레이어만 선택

    Vector3 _baseScale;
    Vector3 _baseUp;
    bool _isHopping;

    void Awake()
    {
        _baseScale = transform.localScale;
        _baseUp = Vector3.up;
    }

    void OnEnable()
    {
        if (!_isHopping)
            StartCoroutine(HopLoop());
    }

    System.Collections.IEnumerator HopLoop()
    {
        _isHopping = true;

        while (enabled)
        {
            // 1. 랜덤 방향 (XZ 평면)
            Vector2 dir2D = Random.insideUnitCircle.normalized;
            if (dir2D == Vector2.zero)
                dir2D = Vector2.right;

            Vector3 dir = new Vector3(dir2D.x, 0f, dir2D.y);

            Vector3 startPos = transform.position;
            Vector3 targetPos = startPos + dir * hopDistance;

            // 지면 높이 보정
            if (Physics.Raycast(targetPos + Vector3.up * 5f, Vector3.down, out var hit, 20f, groundLayer))
                targetPos.y = hit.point.y;
            else
                targetPos.y = startPos.y;

            float t = 0f;

            // 2. 점프 모션
            while (t < hopDuration)
            {
                t += Time.deltaTime;
                float normalized = Mathf.Clamp01(t / hopDuration);

                // 위치 보간
                Vector3 pos = Vector3.Lerp(startPos, targetPos, normalized);

                // 위아래 바운스
                float yOffset = Mathf.Sin(normalized * Mathf.PI) * hopHeight;
                pos.y += yOffset;

                transform.position = pos;

                // 3. 스쿼시 & 스트레치
                float squashPhase = (normalized < 0.5f)
                    ? normalized * 2f
                    : (1f - normalized) * 2f;

                float squash = Mathf.Lerp(0f, squashAmount, squashPhase);

                Vector3 targetScale = new Vector3(
                    _baseScale.x * (1f + squash),
                    _baseScale.y * (1f - squash),
                    _baseScale.z * (1f + squash)
                );

                transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * squashSpeed);

                // 진행 방향으로 살짝 회전 (원하면 유지, 싫으면 주석 처리)
                if (dir.sqrMagnitude > 0.001f)
                {
                    Quaternion look = Quaternion.LookRotation(dir, _baseUp);
                    transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * 6f);
                }

                yield return null;
            }

            // 정리
            transform.position = targetPos;
            transform.localScale = _baseScale;

            // 다음 점프까지 랜덤 대기
            float wait = Random.Range(idleDelayMin, idleDelayMax);
            yield return new WaitForSeconds(wait);
        }

        _isHopping = false;
    }
}