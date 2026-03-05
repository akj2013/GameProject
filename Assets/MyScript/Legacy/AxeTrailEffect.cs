using UnityEngine;

/// <summary>
/// 도끼 휘두를 때 궤적 이펙트. Trail Renderer를 붙이고 도끼 색에 맞춰 그라데이션 설정.
/// 도끼 프리팹 루트 또는 날 쪽 자식에 붙이면 됨.
/// </summary>
[RequireComponent(typeof(TrailRenderer))]
public class AxeTrailEffect : MonoBehaviour
{
    [Header("Trail")]
    [Tooltip("궤적 색 (도끼 날 색에 맞춰 설정: 흰/파랑/빨강/검정)")]
    public Color trailColor = Color.white;
    [Tooltip("궤적 지속 시간(초)")]
    [Range(0.05f, 0.5f)]
    public float time = 0.2f;
    [Tooltip("궤적 시작 두께")]
    [Range(0.01f, 0.2f)]
    public float startWidth = 0.08f;
    [Tooltip("궤적 끝 두께 (0이면 사라짐)")]
    [Range(0f, 0.15f)]
    public float endWidth = 0.02f;
    [Tooltip("트레일 머티리얼 (비어 있으면 기본 파티클 Unlit 사용)")]
    public Material trailMaterial;

    [Header("Hit Burst")]
    [Tooltip("무기가 맞을 때 터지는 광원/파티클 이펙트 프리팹 (공용 1개 추천)")]
    public GameObject hitBurstPrefab;
    [Tooltip("히트 이펙트 유지 시간(초) - 이후 자동 파괴")]
    [Range(0.05f, 2f)]
    public float hitBurstLifetime = 0.6f;
    [Tooltip("Trail Color를 히트 이펙트 색으로 사용할지 여부")]
    public bool inheritTrailColor = true;

    TrailRenderer _trail;

    void Awake()
    {
        _trail = GetComponent<TrailRenderer>();
        if (_trail == null) _trail = gameObject.AddComponent<TrailRenderer>();
        ApplySettings();
    }

    void OnValidate()
    {
        if (_trail != null)
            ApplySettings();
    }

    void ApplySettings()
    {
        if (_trail == null) return;

        _trail.time = time;
        _trail.startWidth = startWidth;
        _trail.endWidth = endWidth;
        _trail.autodestruct = false;
        _trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _trail.receiveShadows = false;
        _trail.minVertexDistance = 0.05f;
        _trail.emitting = true;

        if (trailMaterial != null)
            _trail.material = trailMaterial;
        else
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                var mat = new Material(shader);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
                else if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
                _trail.material = mat;
            }
        }

        var g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(trailColor, 0f), new GradientColorKey(trailColor, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        _trail.colorGradient = g;
    }

    /// <summary>무기 숨길 때 궤적 즉시 제거 (다음 휘두를 때 깔끔하게)</summary>
    void OnDisable()
    {
        if (_trail != null)
            _trail.Clear();
    }

    /// <summary>
    /// 외부에서 호출하는 히트 이펙트 재생용 함수.
    /// 나무 / 적 / 바닥 등 맞는 순간의 위치와 노멀을 넘겨주면 됨.
    /// </summary>
    /// <param name="hitPosition">충돌 지점 월드 좌표</param>
    /// <param name="hitNormal">충돌 표면 노멀 (없으면 Vector3.up)</param>
    public void PlayHitBurst(Vector3 hitPosition, Vector3 hitNormal)
    {
        if (hitBurstPrefab == null)
        {
            Debug.LogWarning($"[AxeTrailEffect] PlayHitBurst called but hitBurstPrefab is null on '{name}'.");
            return;
        }

        Debug.Log($"[AxeTrailEffect] PlayHitBurst on '{name}' at pos={hitPosition}, normal={hitNormal}, color={trailColor}, lifetime={hitBurstLifetime}.");

        if (hitNormal == Vector3.zero)
            hitNormal = Vector3.up;

        var rot = Quaternion.LookRotation(hitNormal);
        var go = Instantiate(hitBurstPrefab, hitPosition, rot);

        if (inheritTrailColor)
        {
            // 트레일용 색은 알파가 0일 수 있으므로, 히트 파티클/광원에는 항상 불투명하게 사용
            var color = trailColor;
            color.a = 1f;

            var particleSystems = go.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in particleSystems)
            {
                var main = ps.main;
                main.startColor = color;
            }

            var lights = go.GetComponentsInChildren<Light>(true);
            foreach (var light in lights)
            {
                light.color = color;
            }
        }

        if (hitBurstLifetime > 0f)
            Destroy(go, hitBurstLifetime);
    }
}
