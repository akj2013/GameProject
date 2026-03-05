using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int gold = 0;
    public TextMeshProUGUI goldText;

    [Header("VFX Pooling")]
    private Dictionary<GameObject, Queue<GameObject>> vfxPool = new Dictionary<GameObject, Queue<GameObject>>();
    private Dictionary<GameObject, GameObject> activeVfxInstances = new Dictionary<GameObject, GameObject>();
    private Dictionary<GameObject, float> vfxDurationCache = new Dictionary<GameObject, float>();

    [Header("VFX Prefabs")]
    [Tooltip("도끼 공격 VFX 프리팹")]
    public GameObject axeHitVfxPrefab;

    [Header("Vibration")]
    [Tooltip("나무 선택 시 진동 세기 (0 = 진동 없음, 1.0 = 최대 세기)")]
    [Range(0f, 1f)]
    public float selectingVibrationStrength = 0.1f;
    [Tooltip("나무 공격 시 진동 세기 (0 = 진동 없음, 1.0 = 최대 세기)")]
    [Range(0f, 1f)]
    public float hitVibrationStrength = 0.5f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        RefreshGoldTextRef();
        UpdateUI();
    }

    /// <summary>
    /// goldText가 비어 있으면 GoldPanelLinks에서 가져와 연결 (참조 복구)
    /// </summary>
    void RefreshGoldTextRef()
    {
        if (goldText != null) return;
        var links = GoldPanelLinks.Instance != null ? GoldPanelLinks.Instance : UnityEngine.Object.FindFirstObjectByType<GoldPanelLinks>();
        if (links != null && links.goldText != null)
            goldText = links.goldText;
    }

    public void AddGold(int amount)
    {
        gold += amount;
        UpdateUI();
    }

    public void UpdateUI()
    {
        RefreshGoldTextRef();
        if (goldText != null)
            goldText.text = gold.ToString();
    }

    public bool UseGold(int amount)
    {
        if (gold < amount) return false;

        gold -= amount;
        UpdateUI();
        return true;
    }

    // VFX 풀링 메서드들
    /// <param name="layer">0 이상이면 해당 레이어로 설정 (루트+자식 모두). -1이면 변경 안 함.</param>
    public GameObject GetVfxFromPool(GameObject prefab, Vector3 position, Quaternion rotation, int layer = -1)
    {
        if (prefab == null) return null;

        if (!vfxPool.ContainsKey(prefab))
        {
            vfxPool[prefab] = new Queue<GameObject>();
        }

        GameObject vfx = null;
        Queue<GameObject> pool = vfxPool[prefab];

        // 풀에서 재사용 가능한 VFX 찾기
        while (pool.Count > 0)
        {
            vfx = pool.Dequeue();
            if (vfx != null && !vfx.activeSelf)
            {
                break;
            }
            vfx = null;
        }

        // 풀에 없으면 새로 생성
        if (vfx == null)
        {
            vfx = Instantiate(prefab);
        }

        vfx.transform.position = position;
        vfx.transform.rotation = rotation;
        if (layer >= 0)
            SetLayerRecursively(vfx, layer);
        vfx.SetActive(true);

        RestartParticleSystems(vfx);

        activeVfxInstances[vfx] = prefab;

        return vfx;
    }

    static List<ParticleSystem> s_particleSystemsCache = new List<ParticleSystem>(32);

    /// <summary>풀에서 꺼낸 VFX가 다시 재생되도록 모든 ParticleSystem에 Clear + Play 적용 (할당 없음)</summary>
    public static void RestartParticleSystems(GameObject root)
    {
        if (root == null) return;
        s_particleSystemsCache.Clear();
        root.GetComponentsInChildren<ParticleSystem>(true, s_particleSystemsCache);
        for (int i = 0; i < s_particleSystemsCache.Count; i++)
        {
            var ps = s_particleSystemsCache[i];
            if (ps == null) continue;
            ps.Clear(true);
            ps.Play(true);
        }
    }

    /// <summary>지정 레이어를 해당 오브젝트와 모든 자식에 적용 (VFX 등)</summary>
    public static void SetLayerRecursively(GameObject go, int layer)
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

    public void ReturnVfxToPool(GameObject vfx)
    {
        if (vfx == null || !activeVfxInstances.ContainsKey(vfx)) return;

        GameObject prefab = activeVfxInstances[vfx];
        activeVfxInstances.Remove(vfx);

        vfx.SetActive(false);

        if (!vfxPool.ContainsKey(prefab))
        {
            vfxPool[prefab] = new Queue<GameObject>();
        }

        vfxPool[prefab].Enqueue(vfx);
    }

    public void ReturnVfxToPoolAfter(GameObject vfx, float delay)
    {
        StartCoroutine(ReturnVfxToPoolAfterCoroutine(vfx, delay));
    }

    IEnumerator ReturnVfxToPoolAfterCoroutine(GameObject vfx, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnVfxToPool(vfx);
    }

    public static float CalculateParticleDuration(GameObject vfx)
    {
        if (vfx == null) return 1f;

        var ps = vfx.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            float duration = ps.main.duration;
            float lifetimeMax = 0f;
            var startLife = ps.main.startLifetime;
            try { lifetimeMax = startLife.constantMax; } catch { try { lifetimeMax = startLife.constant; } catch { lifetimeMax = 0.5f; } }
            return duration + lifetimeMax + 0.2f;
        }

#if UNITY_VFX_GRAPH
        var vfxGraph = vfx.GetComponent<UnityEngine.VFX.VisualEffect>();
        if (vfxGraph != null)
        {
            return 2f; // 기본값
        }
#endif

        return 1f;
    }

    /// <summary>프리팹별 재생 시간 캐시. 같은 프리팹은 한 번만 계산해 두고 재사용 (렉 감소).</summary>
    public float GetCachedParticleDuration(GameObject prefab, GameObject vfxInstance)
    {
        if (prefab == null) return 1f;
        if (vfxInstance != null && vfxDurationCache.TryGetValue(prefab, out float cached))
            return cached;
        float d = vfxInstance != null ? CalculateParticleDuration(vfxInstance) : 1f;
        vfxDurationCache[prefab] = d;
        return d;
    }

    public void TriggerHitVibration()
    {
        if (Application.isMobilePlatform && hitVibrationStrength > 0f)
            VibrateAndroid(hitVibrationStrength * 50f);
    }

    public void TriggerSelectingVibration()
    {
        if (Application.isMobilePlatform && selectingVibrationStrength > 0f)
            VibrateAndroid(selectingVibrationStrength * 50f);
    }

    /// <summary>
    /// Android 진동. API 26+ (Android 8+)는 VibrationEffect, 그 미만은 legacy vibrate(long).
    /// </summary>
    static void VibrateAndroid(float durationMs)
    {
        if (Application.platform != RuntimePlatform.Android) return;
        if (durationMs < 1f) return;

        try
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            if (vibrator == null) return;

            long milliseconds = (long)Mathf.Clamp(durationMs, 1f, 500f);

            if (GetAndroidSdkLevel() >= 26)
            {
                AndroidJavaClass vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
                int amplitude = 255;
                AndroidJavaObject effect = vibrationEffectClass.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, amplitude);
                vibrator.Call("vibrate", effect);
            }
            else
            {
                vibrator.Call("vibrate", milliseconds);
            }
        }
        catch (System.Exception) { }
    }

    static int GetAndroidSdkLevel()
    {
        try
        {
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                return version.GetStatic<int>("SDK_INT");
        }
        catch { return 0; }
    }
}
