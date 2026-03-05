using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    public enum SoundCategory
    {
        Hit,
        Collect,
        UI,
        Background
    }

    [Header("Volume Settings")]
    [Tooltip("무기(듀얼/싱글)가 나무를 때릴 때 사운드 볼륨")]
    [Range(0f, 1f)]
    public float hitVolume = 1f;
    [Tooltip("통나무가 플레이어로 날아올 때 수집 사운드 볼륨")]
    [Range(0f, 1f)]
    public float collectVolume = 1f;
    [Tooltip("UpgradePanel 버튼 클릭 사운드 볼륨")]
    [Range(0f, 1f)]
    public float upgradeBtnVolume = 1f;
    [Tooltip("정산 시 플로팅 골드 생성 사운드 볼륨")]
    [Range(0f, 1f)]
    public float floatingGoldVolume = 1f;
    [Range(0f, 1f)]
    public float backgroundVolume = 1f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public float GetVolume(SoundCategory category)
    {
        switch (category)
        {
            case SoundCategory.Hit:
                return hitVolume;
            case SoundCategory.Collect:
                return collectVolume;
            case SoundCategory.UI:
                return upgradeBtnVolume;
            case SoundCategory.Background:
                return backgroundVolume;
            default:
                return 1f;
        }
    }

    public void PlayClipAtPoint(AudioClip clip, Vector3 position, SoundCategory category)
    {
        if (clip == null) return;

        GameObject audioGo = new GameObject("TempAudio");
        audioGo.transform.position = position;
        AudioSource src = audioGo.AddComponent<AudioSource>();
        src.clip = clip;
        src.spatialBlend = 0f; // 2D
        src.playOnAwake = false;
        src.volume = GetVolume(category);
        src.Play();
        Destroy(audioGo, clip.length + 0.2f);
    }
}
