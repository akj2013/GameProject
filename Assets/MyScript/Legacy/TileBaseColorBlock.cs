using UnityEngine;

/// <summary>
/// 타일마다 MaterialPropertyBlock으로 _BaseColor만 설정합니다.
/// 머티리얼은 1개(Environment_Mat)로 두고 Draw Call을 늘리지 않습니다.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class TileBaseColorBlock : MonoBehaviour
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    [Header("타일 색상")]
    [Tooltip("이 타일에 적용할 색. Environment_Mat(Simple Lit)의 Base Color를 PropertyBlock으로 덮어씁니다.")]
    [SerializeField] Color tileColor = new Color(0.9f, 0.85f, 0.75f, 1f); // 기본: 연한 베이지

    MaterialPropertyBlock _block;
    Renderer _renderer;

    /// <summary>현재 설정된 타일 색</summary>
    public Color TileColor => tileColor;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _block = new MaterialPropertyBlock();
    }

    void Start()
    {
        ApplyColor();
    }

    /// <summary>현재 tileColor로 PropertyBlock 적용 (Start에서 자동 호출)</summary>
    public void ApplyColor()
    {
        if (_renderer == null) _renderer = GetComponent<Renderer>();
        if (_renderer == null) return;

        if (_block == null) _block = new MaterialPropertyBlock();
        _renderer.GetPropertyBlock(_block);
        _block.SetColor(BaseColorId, tileColor);
        _renderer.SetPropertyBlock(_block);
    }

    /// <summary>런타임에 색만 바꿀 때 호출. 머티리얼은 그대로 1개 유지.</summary>
    public void SetColor(Color color)
    {
        tileColor = color;
        ApplyColor();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying) return;
        ApplyColor();
    }
#endif
}
