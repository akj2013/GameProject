using UnityEngine;

namespace WoodLand3D.StarShip
{
    /// <summary>
    /// 빔 시작점(비행선 쪽)에 3D 부피를 가진 발사광(muzzle / launch glow)을 표시합니다.
    /// 내부 밝은 코어 구체 + 바깥 반투명 글로우 구체로, 카메라 각도에 관계없이 덩어리처럼 보입니다.
    /// </summary>
    [ExecuteInEditMode]
    public class BeamStartGlow : MonoBehaviour
    {
        public const string CoreChildName = "StartGlowCore";
        public const string GlowChildName = "StartGlowOuter";

        [Header("참조")]
        [Tooltip("빔 경로. 시작점 위치에 사용.")]
        public StarShipRouteLine targetRouteLine;

        [Tooltip("(선택) 연결 시 Reveal 진행도에 따라 발사광 강도 연동.")]
        public BeamTubeMeshRenderer revealSource;

        [Header("발사광 머티리얼")]
        [Tooltip("코어·글로우 공통 셰이더 머티리얼 (BeamStartGlow 셰이더).")]
        public Material startGlowMaterial;

        [Header("3D 발사광 크기")]
        [Tooltip("안쪽 밝은 코어 구체 반지름(스케일).")]
        [Range(0.05f, 2f)]
        public float startGlowCoreSize = 0.35f;

        [Tooltip("바깥 반투명 글로우 구체 반지름(스케일).")]
        [Range(0.1f, 3f)]
        public float startGlowOuterSize = 0.7f;

        [Header("발사광 색·강도")]
        public Color startGlowColor = new Color(1f, 0.98f, 1f, 0.9f);
        [Range(0f, 2f)]
        public float startGlowIntensity = 1f;

        public Color startGlowOuterColor = new Color(0.7f, 0.85f, 1f, 0.35f);
        [Range(0f, 2f)]
        public float startGlowOuterIntensity = 0.8f;

        [Tooltip("true면 매 프레임 시작점을 따라갑니다.")]
        public bool followStartPoint = true;

        private Transform _coreRoot;
        private Transform _glowRoot;
        private MeshFilter _coreFilter;
        private MeshFilter _glowFilter;
        private MeshRenderer _coreRenderer;
        private MeshRenderer _glowRenderer;
        private MaterialPropertyBlock _corePropBlock;
        private MaterialPropertyBlock _glowPropBlock;
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int RevealId = Shader.PropertyToID("_Reveal");

        private static Mesh _sharedSphereMesh;

        private void Awake()
        {
            EnsureGlowSpheres();
        }

        private void LateUpdate()
        {
            EnsureGlowSpheres();
            if (followStartPoint && targetRouteLine != null && targetRouteLine.startPoint != null && targetRouteLine.endPoint != null)
            {
                Vector3 startPos = targetRouteLine.EvaluatePoint(0f);
                if (_coreRoot != null) _coreRoot.position = startPos;
                if (_glowRoot != null) _glowRoot.position = startPos;
            }

            if (_coreRoot != null)
                _coreRoot.localScale = Vector3.one * Mathf.Max(0.01f, startGlowCoreSize);
            if (_glowRoot != null)
                _glowRoot.localScale = Vector3.one * Mathf.Max(0.01f, startGlowOuterSize);

            float reveal = revealSource != null ? revealSource.Reveal01 : 1f;

            if (startGlowMaterial != null && _coreRenderer != null)
            {
                _coreRenderer.sharedMaterial = startGlowMaterial;
                if (_corePropBlock == null) _corePropBlock = new MaterialPropertyBlock();
                _coreRenderer.GetPropertyBlock(_corePropBlock);
                _corePropBlock.SetColor(ColorId, startGlowColor);
                _corePropBlock.SetFloat(IntensityId, startGlowIntensity);
                _corePropBlock.SetFloat(RevealId, reveal);
                _coreRenderer.SetPropertyBlock(_corePropBlock);
            }
            if (startGlowMaterial != null && _glowRenderer != null)
            {
                _glowRenderer.sharedMaterial = startGlowMaterial;
                if (_glowPropBlock == null) _glowPropBlock = new MaterialPropertyBlock();
                _glowRenderer.GetPropertyBlock(_glowPropBlock);
                _glowPropBlock.SetColor(ColorId, startGlowOuterColor);
                _glowPropBlock.SetFloat(IntensityId, startGlowOuterIntensity);
                _glowPropBlock.SetFloat(RevealId, reveal);
                _glowRenderer.SetPropertyBlock(_glowPropBlock);
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
        }

        private void EnsureGlowSpheres()
        {
            if (_coreRoot != null && _glowRoot != null)
                return;

            Mesh sphereMesh = GetOrCreateSphereMesh();

            Transform coreExisting = transform.Find(CoreChildName);
            if (coreExisting != null)
            {
                _coreRoot = coreExisting;
                _coreFilter = _coreRoot.GetComponent<MeshFilter>();
                _coreRenderer = _coreRoot.GetComponent<MeshRenderer>();
                if (_coreFilter != null && sphereMesh != null) _coreFilter.sharedMesh = sphereMesh;
            }
            else
            {
                var coreGo = new GameObject(CoreChildName);
                coreGo.transform.SetParent(transform, false);
                coreGo.transform.localPosition = Vector3.zero;
                coreGo.transform.localRotation = Quaternion.identity;
                coreGo.transform.localScale = Vector3.one * Mathf.Max(0.01f, startGlowCoreSize);
                _coreRoot = coreGo.transform;
                _coreFilter = coreGo.AddComponent<MeshFilter>();
                _coreFilter.sharedMesh = sphereMesh;
                _coreRenderer = coreGo.AddComponent<MeshRenderer>();
                _coreRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _coreRenderer.receiveShadows = false;
                if (startGlowMaterial != null) _coreRenderer.sharedMaterial = startGlowMaterial;
            }

            Transform glowExisting = transform.Find(GlowChildName);
            if (glowExisting != null)
            {
                _glowRoot = glowExisting;
                _glowFilter = _glowRoot.GetComponent<MeshFilter>();
                _glowRenderer = _glowRoot.GetComponent<MeshRenderer>();
                if (_glowFilter != null && sphereMesh != null) _glowFilter.sharedMesh = sphereMesh;
            }
            else
            {
                var glowGo = new GameObject(GlowChildName);
                glowGo.transform.SetParent(transform, false);
                glowGo.transform.localPosition = Vector3.zero;
                glowGo.transform.localRotation = Quaternion.identity;
                glowGo.transform.localScale = Vector3.one * Mathf.Max(0.01f, startGlowOuterSize);
                _glowRoot = glowGo.transform;
                _glowFilter = glowGo.AddComponent<MeshFilter>();
                _glowFilter.sharedMesh = sphereMesh;
                _glowRenderer = glowGo.AddComponent<MeshRenderer>();
                _glowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _glowRenderer.receiveShadows = false;
                if (startGlowMaterial != null) _glowRenderer.sharedMaterial = startGlowMaterial;
            }

            if (_glowRoot.GetSiblingIndex() > _coreRoot.GetSiblingIndex())
                _glowRoot.SetAsFirstSibling();
        }

        private static Mesh GetOrCreateSphereMesh()
        {
            if (_sharedSphereMesh != null)
                return _sharedSphereMesh;
            _sharedSphereMesh = BuildSphereMesh();
            return _sharedSphereMesh;
        }

        /// <summary>
        /// 단위 반지름 구체 메시 (라디안 세그먼트).
        /// </summary>
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
                    int a = rowA + lon;
                    int b = rowA + lon + 1;
                    int c = rowB + lon;
                    int d = rowB + lon + 1;
                    tris.Add(a); tris.Add(c); tris.Add(b);
                    tris.Add(b); tris.Add(c); tris.Add(d);
                }
            }

            var m = new Mesh();
            m.name = "BeamStartGlowSphere";
            m.SetVertices(verts);
            m.SetNormals(normals);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            return m;
        }
    }
}
