using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 4프레임 스프라이트 시트를 RawImage에서 루프 재생 (예: 나무 바람 흔들림).
/// - 스프라이트 시트: 한 텍스처에 가로로 4프레임 → UV Rect로 프레임 전환.
/// - 또는 프레임별 텍스처 4개를 배열로 할당.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class SpriteSheetLoop : MonoBehaviour
{
    public enum Mode
    {
        /// <summary>한 장의 텍스처에 가로로 4프레임 (1행 N열)</summary>
        SingleTextureRow,
        /// <summary>프레임마다 텍스처 4개 (배열)</summary>
        SeparateTextures
    }

    [Header("재생 방식")]
    [SerializeField] private Mode mode = Mode.SingleTextureRow;

    [Header("스프라이트 시트 (SingleTextureRow일 때)")]
    [SerializeField, Tooltip("가로로 4프레임이 나란히 있는 텍스처")]
    private Texture2D spriteSheetTexture;
    [SerializeField] private int frameCount = 4;
    [SerializeField] private bool horizontalRow = true;

    [Header("프레임별 텍스처 (SeparateTextures일 때)")]
    [SerializeField] private Texture2D[] frameTextures = new Texture2D[4];

    [Header("재생")]
    [SerializeField] private float framesPerSecond = 6f;
    [SerializeField] private bool playOnEnable = true;

    private RawImage _rawImage;
    private float _timer;
    private int _currentIndex;

    private void Awake()
    {
        _rawImage = GetComponent<RawImage>();
    }

    private void OnEnable()
    {
        _timer = 0f;
        _currentIndex = 0;
        if (playOnEnable)
            ApplyFrame(0);
    }

    private void Update()
    {
        if (frameCount <= 0) return;

        float interval = 1f / Mathf.Max(0.1f, framesPerSecond);
        _timer += Time.deltaTime;
        while (_timer >= interval)
        {
            _timer -= interval;
            _currentIndex = (_currentIndex + 1) % frameCount;
            ApplyFrame(_currentIndex);
        }
    }

    private void ApplyFrame(int index)
    {
        if (_rawImage == null) return;

        if (mode == Mode.SingleTextureRow && spriteSheetTexture != null)
        {
            _rawImage.texture = spriteSheetTexture;
            int count = Mathf.Max(1, frameCount);
            float w = 1f / count;
            if (horizontalRow)
                _rawImage.uvRect = new Rect(index * w, 0f, w, 1f);
            else
                _rawImage.uvRect = new Rect(0f, 1f - (index + 1) * w, 1f, w);
        }
        else if (mode == Mode.SeparateTextures && frameTextures != null && index < frameTextures.Length && frameTextures[index] != null)
        {
            _rawImage.texture = frameTextures[index];
            _rawImage.uvRect = new Rect(0f, 0f, 1f, 1f);
        }
    }

    /// <summary>재생 속도 설정 (FPS).</summary>
    public void SetFramesPerSecond(float fps)
    {
        framesPerSecond = Mathf.Max(0.1f, fps);
    }
}
