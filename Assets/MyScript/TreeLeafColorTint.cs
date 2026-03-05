using UnityEngine;

/// <summary>
/// 나무의 나뭇잎 색상을 인스펙터에서 조정할 수 있게 합니다.
/// MeshRenderer의 _BaseColor를 MaterialPropertyBlock으로 오버라이드합니다.
/// 흰색(1,1,1) = 원본 유지, 다른 색 = 텍스처에 틴트 적용.
/// </summary>
[ExecuteInEditMode]
public class TreeLeafColorTint : MonoBehaviour
{
    [Tooltip("나뭇잎 색상 틴트 (흰색=원본, 녹색=더 푸르게, 노랑/주황=가을 느낌)")]
    public Color leafColorTint = Color.white;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    MeshRenderer _renderer;
    MaterialPropertyBlock _block;

    void OnEnable()
    {
        _renderer = GetComponent<MeshRenderer>();
        _block = new MaterialPropertyBlock();
        ApplyTint();
    }

    void OnValidate()
    {
        if (_block == null) _block = new MaterialPropertyBlock();
        if (_renderer == null) _renderer = GetComponent<MeshRenderer>();
        ApplyTint();
    }

    void ApplyTint()
    {
        if (_renderer == null || _block == null) return;
        _renderer.GetPropertyBlock(_block);
        _block.SetColor(BaseColorId, leafColorTint);
        _renderer.SetPropertyBlock(_block);
    }
}
