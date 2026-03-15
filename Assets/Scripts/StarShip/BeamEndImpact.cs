using UnityEngine;

namespace WoodLand3D.StarShip
{
    /// <summary>
    /// 빔 도착점(타깃/채집점)에 3D 부피를 가진 임팩트(에너지 응집/충돌 반응)를 표시합니다.
    /// ImpactCore(작은 구) + ImpactGlow(바깥 구) + 선택적 ImpactRing(얇은 3D 링). 전부 실제 메시 기반.
    /// </summary>
    [ExecuteInEditMode]
    public class BeamEndImpact : MonoBehaviour
    {
        public const string CoreChildName = "ImpactCore";
        public const string GlowChildName = "ImpactGlow";
        public const string RingChildName = "ImpactRing";

        [Header("참조")]
        [Tooltip("빔 경로. 도착점 위치·방향에 사용.")]
        public StarShipRouteLine targetRouteLine;

        [Tooltip("(선택) 연결 시 Reveal 진행도에 따라 임팩트 강도 연동.")]
        public BeamTubeMeshRenderer revealSource;

        [Header("임팩트 머티리얼")]
        [Tooltip("코어·글로우·링 공통 셰이더 머티리얼 (BeamImpactGlow 셰이더).")]
        public Material impactMaterial;

        [Header("3D 임팩트 크기")]
        [Range(0.05f, 2f)]
        public float impactCoreSize = 0.3f;
        [Range(0.1f, 3f)]
        public float impactGlowSize = 0.6f;
        [Tooltip("0이면 링 비표시.")]
        [Range(0f, 2f)]
        public float impactRingSize = 0.5f;

        [Header("임팩트 색·강도")]
        public Color impactCoreColor = new Color(1f, 0.9f, 0.95f, 0.9f);
        [Range(0f, 2f)]
        public float impactCoreIntensity = 1f;
        public Color impactGlowColor = new Color(0.6f, 0.75f, 1f, 0.4f);
        [Range(0f, 2f)]
        public float impactGlowIntensity = 0.85f;
        public Color impactRingColor = new Color(0.8f, 0.9f, 1f, 0.35f);
        [Range(0f, 2f)]
        public float impactRingIntensity = 0.6f;

        [Tooltip("true면 매 프레임 도착점을 따라갑니다.")]
        public bool followEndPoint = true;

        private Transform _coreRoot;
        private Transform _glowRoot;
        private Transform _ringRoot;
        private MeshFilter _coreFilter;
        private MeshFilter _glowFilter;
        private MeshFilter _ringFilter;
        private MeshRenderer _coreRenderer;
        private MeshRenderer _glowRenderer;
        private MeshRenderer _ringRenderer;
        private MaterialPropertyBlock _corePropBlock;
        private MaterialPropertyBlock _glowPropBlock;
        private MaterialPropertyBlock _ringPropBlock;
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int RevealId = Shader.PropertyToID("_Reveal");

        private static Mesh _sharedSphereMesh;
        private static Mesh _sharedRingMesh;

        private void Awake()
        {
            EnsureImpactMeshes();
        }

        private void LateUpdate()
        {
            EnsureImpactMeshes();
            if (followEndPoint && targetRouteLine != null && targetRouteLine.startPoint != null && targetRouteLine.endPoint != null)
            {
                Vector3 endPos = targetRouteLine.EvaluatePoint(1f);
                Vector3 tangent = targetRouteLine.EvaluateTangent(1f);
                if (_coreRoot != null) _coreRoot.position = endPos;
                if (_glowRoot != null) _glowRoot.position = endPos;
                if (_ringRoot != null)
                {
                    _ringRoot.position = endPos;
                    if (tangent.sqrMagnitude > 0.0001f)
                        _ringRoot.rotation = Quaternion.FromToRotation(Vector3.up, tangent.normalized);
                }
            }

            if (_coreRoot != null)
                _coreRoot.localScale = Vector3.one * Mathf.Max(0.01f, impactCoreSize);
            if (_glowRoot != null)
                _glowRoot.localScale = Vector3.one * Mathf.Max(0.01f, impactGlowSize);
            if (_ringRoot != null)
            {
                float ringScale = Mathf.Max(0f, impactRingSize);
                _ringRoot.gameObject.SetActive(ringScale > 0.01f);
                if (ringScale > 0.01f)
                    _ringRoot.localScale = Vector3.one * ringScale;
            }

            float reveal = revealSource != null ? revealSource.Reveal01 : 1f;

            if (impactMaterial != null && _coreRenderer != null)
            {
                _coreRenderer.sharedMaterial = impactMaterial;
                if (_corePropBlock == null) _corePropBlock = new MaterialPropertyBlock();
                _coreRenderer.GetPropertyBlock(_corePropBlock);
                _corePropBlock.SetColor(ColorId, impactCoreColor);
                _corePropBlock.SetFloat(IntensityId, impactCoreIntensity);
                _corePropBlock.SetFloat(RevealId, reveal);
                _coreRenderer.SetPropertyBlock(_corePropBlock);
            }
            if (impactMaterial != null && _glowRenderer != null)
            {
                _glowRenderer.sharedMaterial = impactMaterial;
                if (_glowPropBlock == null) _glowPropBlock = new MaterialPropertyBlock();
                _glowRenderer.GetPropertyBlock(_glowPropBlock);
                _glowPropBlock.SetColor(ColorId, impactGlowColor);
                _glowPropBlock.SetFloat(IntensityId, impactGlowIntensity);
                _glowPropBlock.SetFloat(RevealId, reveal);
                _glowRenderer.SetPropertyBlock(_glowPropBlock);
            }
            if (impactMaterial != null && _ringRenderer != null && impactRingSize > 0.01f)
            {
                _ringRenderer.sharedMaterial = impactMaterial;
                if (_ringPropBlock == null) _ringPropBlock = new MaterialPropertyBlock();
                _ringRenderer.GetPropertyBlock(_ringPropBlock);
                _ringPropBlock.SetColor(ColorId, impactRingColor);
                _ringPropBlock.SetFloat(IntensityId, impactRingIntensity);
                _ringPropBlock.SetFloat(RevealId, reveal);
                _ringRenderer.SetPropertyBlock(_ringPropBlock);
            }
        }

        private void OnDestroy()
        {
            if (_coreRoot != null)
            {
                if (Application.isPlaying) Destroy(_coreRoot.gameObject);
                else DestroyImmediate(_coreRoot.gameObject);
                _coreRoot = null;
            }
            if (_glowRoot != null)
            {
                if (Application.isPlaying) Destroy(_glowRoot.gameObject);
                else DestroyImmediate(_glowRoot.gameObject);
                _glowRoot = null;
            }
            if (_ringRoot != null)
            {
                if (Application.isPlaying) Destroy(_ringRoot.gameObject);
                else DestroyImmediate(_ringRoot.gameObject);
                _ringRoot = null;
            }
        }

        private void EnsureImpactMeshes()
        {
            if (_coreRoot != null && _glowRoot != null && _ringRoot != null)
                return;

            Mesh sphereMesh = GetOrCreateSphereMesh();
            Mesh ringMesh = GetOrCreateRingMesh();

            if (_coreRoot == null)
            {
                Transform ex = transform.Find(CoreChildName);
                if (ex != null)
                {
                    _coreRoot = ex;
                    _coreFilter = _coreRoot.GetComponent<MeshFilter>();
                    _coreRenderer = _coreRoot.GetComponent<MeshRenderer>();
                    if (_coreFilter != null && sphereMesh != null) _coreFilter.sharedMesh = sphereMesh;
                }
                else
                {
                    var go = new GameObject(CoreChildName);
                    go.transform.SetParent(transform, false);
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localRotation = Quaternion.identity;
                    go.transform.localScale = Vector3.one * Mathf.Max(0.01f, impactCoreSize);
                    _coreRoot = go.transform;
                    _coreFilter = go.AddComponent<MeshFilter>();
                    _coreFilter.sharedMesh = sphereMesh;
                    _coreRenderer = go.AddComponent<MeshRenderer>();
                    _coreRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    _coreRenderer.receiveShadows = false;
                    if (impactMaterial != null) _coreRenderer.sharedMaterial = impactMaterial;
                }
            }

            if (_glowRoot == null)
            {
                Transform ex = transform.Find(GlowChildName);
                if (ex != null)
                {
                    _glowRoot = ex;
                    _glowFilter = _glowRoot.GetComponent<MeshFilter>();
                    _glowRenderer = _glowRoot.GetComponent<MeshRenderer>();
                    if (_glowFilter != null && sphereMesh != null) _glowFilter.sharedMesh = sphereMesh;
                }
                else
                {
                    var go = new GameObject(GlowChildName);
                    go.transform.SetParent(transform, false);
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localRotation = Quaternion.identity;
                    go.transform.localScale = Vector3.one * Mathf.Max(0.01f, impactGlowSize);
                    _glowRoot = go.transform;
                    _glowFilter = go.AddComponent<MeshFilter>();
                    _glowFilter.sharedMesh = sphereMesh;
                    _glowRenderer = go.AddComponent<MeshRenderer>();
                    _glowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    _glowRenderer.receiveShadows = false;
                    if (impactMaterial != null) _glowRenderer.sharedMaterial = impactMaterial;
                }
            }

            if (_ringRoot == null)
            {
                Transform ex = transform.Find(RingChildName);
                if (ex != null)
                {
                    _ringRoot = ex;
                    _ringFilter = _ringRoot.GetComponent<MeshFilter>();
                    _ringRenderer = _ringRoot.GetComponent<MeshRenderer>();
                    if (_ringFilter != null && ringMesh != null) _ringFilter.sharedMesh = ringMesh;
                }
                else
                {
                    var go = new GameObject(RingChildName);
                    go.transform.SetParent(transform, false);
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localRotation = Quaternion.identity;
                    go.transform.localScale = Vector3.one * Mathf.Max(0.01f, impactRingSize);
                    _ringRoot = go.transform;
                    _ringFilter = go.AddComponent<MeshFilter>();
                    _ringFilter.sharedMesh = ringMesh;
                    _ringRenderer = go.AddComponent<MeshRenderer>();
                    _ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    _ringRenderer.receiveShadows = false;
                    if (impactMaterial != null) _ringRenderer.sharedMaterial = impactMaterial;
                }
            }

            if (_glowRoot.GetSiblingIndex() > _coreRoot.GetSiblingIndex())
                _glowRoot.SetAsFirstSibling();
            if (_ringRoot.GetSiblingIndex() > _glowRoot.GetSiblingIndex())
                _ringRoot.SetAsFirstSibling();
        }

        private static Mesh GetOrCreateSphereMesh()
        {
            if (_sharedSphereMesh != null) return _sharedSphereMesh;
            _sharedSphereMesh = BuildSphereMesh();
            return _sharedSphereMesh;
        }

        private static Mesh GetOrCreateRingMesh()
        {
            if (_sharedRingMesh != null) return _sharedRingMesh;
            _sharedRingMesh = BuildRingMesh();
            return _sharedRingMesh;
        }

        private static Mesh BuildSphereMesh()
        {
            const int segLat = 16;
            const int segLon = 16;
            var verts = new System.Collections.Generic.List<Vector3>();
            var normals = new System.Collections.Generic.List<Vector3>();
            var uvs = new System.Collections.Generic.List<Vector2>();
            var tris = new System.Collections.Generic.List<int>();

            for (int lat = 0; lat <= segLat; lat++)
            {
                float v = lat / (float)segLat;
                float phi = v * Mathf.PI;
                float y = Mathf.Cos(phi);
                float r = Mathf.Sin(phi);
                for (int lon = 0; lon <= segLon; lon++)
                {
                    float u = lon / (float)segLon;
                    float theta = u * Mathf.PI * 2f;
                    float x = r * Mathf.Cos(theta);
                    float z = r * Mathf.Sin(theta);
                    verts.Add(new Vector3(x, y, z));
                    normals.Add(new Vector3(x, y, z).normalized);
                    uvs.Add(new Vector2(u, v));
                }
            }
            for (int lat = 0; lat < segLat; lat++)
            {
                int rowA = lat * (segLon + 1);
                int rowB = rowA + segLon + 1;
                for (int lon = 0; lon < segLon; lon++)
                {
                    int a = rowA + lon, b = rowA + lon + 1, c = rowB + lon, d = rowB + lon + 1;
                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(b); tris.Add(c); tris.Add(d);
                }
            }
            var m = new Mesh();
            m.name = "BeamImpactSphere";
            m.SetVertices(verts);
            m.SetNormals(normals);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            return m;
        }

        /// <summary>
        /// 단위 반지름의 얇은 링 메시 (XZ 평면, Y up = 노멀). 도착점에서 빔에 수직으로 세움.
        /// </summary>
        private static Mesh BuildRingMesh()
        {
            const int seg = 32;
            float inner = 0.7f;
            float outer = 1f;
            var verts = new System.Collections.Generic.List<Vector3>();
            var normals = new System.Collections.Generic.List<Vector3>();
            var uvs = new System.Collections.Generic.List<Vector2>();
            var tris = new System.Collections.Generic.List<int>();
            Vector3 up = Vector3.up;

            for (int i = 0; i <= seg; i++)
            {
                float t = i / (float)seg * Mathf.PI * 2f;
                float cx = Mathf.Cos(t);
                float cz = Mathf.Sin(t);
                verts.Add(new Vector3(inner * cx, 0, inner * cz));
                verts.Add(new Vector3(outer * cx, 0, outer * cz));
                normals.Add(up);
                normals.Add(up);
                uvs.Add(new Vector2(i / (float)seg, 0));
                uvs.Add(new Vector2(i / (float)seg, 1));
            }
            for (int i = 0; i < seg; i++)
            {
                int a = i * 2;
                int b = i * 2 + 1;
                int c = (i + 1) * 2;
                int d = (i + 1) * 2 + 1;
                tris.Add(a); tris.Add(c); tris.Add(b);
                tris.Add(b); tris.Add(c); tris.Add(d);
            }
            var m = new Mesh();
            m.name = "BeamImpactRing";
            m.SetVertices(verts);
            m.SetNormals(normals);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            return m;
        }
    }
}
