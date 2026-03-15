using UnityEngine;
using System.Collections.Generic;

namespace WoodLand3D.StarShip
{
    /// <summary>
    /// 빔 경로 전체를 "발생원"으로 하여, 각 지점에서 순간적으로 산란했다가 사라지는 미세 발광 입자층.
    /// 경로를 따라 이동하는 구슬이 아니라, 빔 주변에서 짧게 퍼졌다가 사라지는 광자/별가루 연출.
    /// </summary>
    public class BeamPathParticles : MonoBehaviour
    {
        public const string ParticleBaseName = "PathParticle_";

        [Header("참조")]
        public StarShipRouteLine targetRouteLine;
        [Tooltip("(선택) Reveal 진행도에 따라 발생 구간·가시성 연동.")]
        public BeamTubeMeshRenderer revealSource;

        [Header("입자 머티리얼")]
        public Material particleMaterial;

        [Header("입자 개수·크기")]
        [Range(8, 256)]
        public int particleCount = 64;
        [Range(0.01f, 0.2f)]
        public float particleSizeMin = 0.025f;
        [Range(0.02f, 0.35f)]
        public float particleSizeMax = 0.06f;

        [Header("수명·산란 속도")]
        [Tooltip("입자 수명(초) 최소.")]
        [Range(0.05f, 0.5f)]
        public float particleLifetimeMin = 0.15f;
        [Tooltip("입자 수명(초) 최대.")]
        [Range(0.1f, 1f)]
        public float particleLifetimeMax = 0.4f;
        [Tooltip("바깥으로 퍼지는 속도 최소.")]
        [Range(0.2f, 3f)]
        public float particleSpeedMin = 0.5f;
        [Tooltip("바깥으로 퍼지는 속도 최대.")]
        [Range(0.5f, 5f)]
        public float particleSpeedMax = 1.2f;

        [Header("생성 위치")]
        [Tooltip("빔 축에서 생성 가능한 반경(멀리 나가면 안 됨).")]
        [Range(0f, 0.12f)]
        public float spawnRadius = 0.04f;
        [Tooltip("경로 t 방향 생성 위치 랜덤성.")]
        [Range(0f, 0.5f)]
        public float spawnJitter = 0.1f;

        [Header("색·강도")]
        public Color particleColor = new Color(0.95f, 0.97f, 1f, 0.6f);
        [Range(0.2f, 2f)]
        public float particleIntensity = 0.7f;

        [Header("동작")]
        public bool followRoute = true;

        private struct ParticleSlot
        {
            public Vector3 position;
            public Vector3 velocity;
            public float lifetimeLeft;
            public float maxLifetime;
            public float size;
            public float spawnT;
        }

        private readonly List<Transform> _transforms = new List<Transform>();
        private readonly List<MeshRenderer> _renderers = new List<MeshRenderer>();
        private ParticleSlot[] _slots;
        private int _cachedCount = -1;
        private MaterialPropertyBlock _propBlock;
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int RevealId = Shader.PropertyToID("_Reveal");
        private static readonly int LifeId = Shader.PropertyToID("_Life");

        private static Mesh _sharedSphereMesh;

        private void Awake()
        {
            if (Application.isPlaying)
                EnsureParticles();
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
                DestroyAllParticleChildren();
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying)
                return;

            EnsureParticles();
            if (targetRouteLine == null || targetRouteLine.startPoint == null || targetRouteLine.endPoint == null)
                return;
            if (_slots == null || _transforms == null || _transforms.Count == 0)
                return;

            float reveal = revealSource != null ? revealSource.Reveal01 : 1f;
            float revealSoft = revealSource != null ? Mathf.Max(0.01f, revealSource.revealSoftness) : 0.05f;
            float dt = Time.deltaTime;

            for (int i = 0; i < _slots.Length && i < _transforms.Count; i++)
            {
                if (_transforms[i] == null)
                    continue;

                ref ParticleSlot s = ref _slots[i];

                if (s.maxLifetime <= 0f || s.lifetimeLeft <= 0f)
                {
                    RespawnParticle(i, reveal);
                    s = ref _slots[i];
                }

                s.lifetimeLeft -= dt;
                if (s.lifetimeLeft <= 0f)
                {
                    RespawnParticle(i, reveal);
                    s = ref _slots[i];
                }

                s.position += s.velocity * dt;

                _transforms[i].position = s.position;
                float lifeScale = s.maxLifetime > 0f ? Mathf.Clamp01(s.lifetimeLeft / s.maxLifetime) : 0f;
                _transforms[i].localScale = Vector3.one * Mathf.Max(0.005f, s.size * (0.3f + 0.7f * lifeScale));

                float rev = reveal >= 1f ? 1f : Mathf.Clamp01((reveal - s.spawnT + revealSoft) / revealSoft);
                float life = s.maxLifetime > 0f ? Mathf.Clamp01(s.lifetimeLeft / s.maxLifetime) : 0f;

                if (i < _renderers.Count && _renderers[i] != null && particleMaterial != null)
                {
                    if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
                    _renderers[i].sharedMaterial = particleMaterial;
                    _renderers[i].GetPropertyBlock(_propBlock);
                    _propBlock.SetColor(ColorId, particleColor);
                    _propBlock.SetFloat(IntensityId, particleIntensity * rev);
                    _propBlock.SetFloat(RevealId, rev);
                    _propBlock.SetFloat(LifeId, life);
                    _renderers[i].SetPropertyBlock(_propBlock);
                }
            }
        }

        private void RespawnParticle(int index, float reveal)
        {
            if (targetRouteLine == null || targetRouteLine.startPoint == null || targetRouteLine.endPoint == null)
                return;
            if (_slots == null || index < 0 || index >= _slots.Length)
                return;

            float t = reveal >= 1f ? Random.value : Random.Range(0f, Mathf.Clamp01(reveal + 0.05f));
            t = Mathf.Clamp01(t + (Random.value - 0.5f) * spawnJitter);

            Vector3 center = targetRouteLine.EvaluatePoint(t);
            Vector3 tangent = targetRouteLine.EvaluateTangent(t);
            if (tangent.sqrMagnitude < 0.0001f)
                tangent = (targetRouteLine.endPoint.position - targetRouteLine.startPoint.position).normalized;

            Vector3 right = Vector3.Cross(tangent, Vector3.up).normalized;
            if (right.sqrMagnitude < 0.01f)
                right = Vector3.Cross(tangent, Vector3.forward).normalized;
            Vector3 up = Vector3.Cross(right, tangent).normalized;

            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 outward = (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)).normalized;
            float radius = Random.Range(0f, Mathf.Max(0.001f, spawnRadius));
            Vector3 pos = center + outward * radius;
            float speed = Mathf.Lerp(particleSpeedMin, particleSpeedMax, Random.value);
            Vector3 velocity = outward * speed;

            float maxLt = Mathf.Lerp(particleLifetimeMin, particleLifetimeMax, Random.value);
            float size = Mathf.Lerp(particleSizeMin, particleSizeMax, Random.value);

            _slots[index] = new ParticleSlot
            {
                position = pos,
                velocity = velocity,
                lifetimeLeft = maxLt,
                maxLifetime = maxLt,
                size = size,
                spawnT = t
            };
        }

        private void EnsureParticles()
        {
            if (!Application.isPlaying) return;

            int n = Mathf.Clamp(particleCount, 8, 256);
            bool countChanged = _cachedCount != n;
            _cachedCount = n;

            if (countChanged)
            {
                _slots = new ParticleSlot[n];
                float reveal = revealSource != null ? revealSource.Reveal01 : 1f;
                for (int i = 0; i < n; i++)
                    RespawnParticle(i, reveal);
            }

            Mesh sphereMesh = GetOrCreateSphereMesh();

            while (_transforms.Count < n)
            {
                int idx = _transforms.Count;
                var go = new GameObject(ParticleBaseName + idx.ToString("000"));
                go.transform.SetParent(transform, false);
                var mf = go.AddComponent<MeshFilter>();
                mf.sharedMesh = sphereMesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                if (particleMaterial != null) mr.sharedMaterial = particleMaterial;
                _transforms.Add(go.transform);
                _renderers.Add(mr);
            }

            while (_transforms.Count > n)
            {
                int last = _transforms.Count - 1;
                if (_transforms[last] != null)
                {
                    if (Application.isPlaying) Destroy(_transforms[last].gameObject);
                    else DestroyImmediate(_transforms[last].gameObject);
                }
                _transforms.RemoveAt(last);
                _renderers.RemoveAt(last);
            }
        }

        private void DestroyAllParticleChildren()
        {
            if (Application.isPlaying) return;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child != null && child.name.StartsWith(ParticleBaseName, System.StringComparison.Ordinal))
                    DestroyImmediate(child.gameObject);
            }
            _transforms.Clear();
            _renderers.Clear();
            _cachedCount = -1;
        }

        private static Mesh GetOrCreateSphereMesh()
        {
            if (_sharedSphereMesh != null) return _sharedSphereMesh;
            _sharedSphereMesh = BuildSphereMesh();
            return _sharedSphereMesh;
        }

        private static Mesh BuildSphereMesh()
        {
            const int seg = 8;
            const int ring = 6;
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            for (int r = 0; r <= ring; r++)
            {
                float v = r / (float)ring * Mathf.PI;
                float y = Mathf.Cos(v);
                float ringR = Mathf.Sin(v);
                for (int s = 0; s <= seg; s++)
                {
                    float u = s / (float)seg * Mathf.PI * 2f;
                    float x = Mathf.Cos(u) * ringR;
                    float z = Mathf.Sin(u) * ringR;
                    verts.Add(new Vector3(x * 0.5f, y * 0.5f, z * 0.5f));
                    normals.Add(new Vector3(x, y, z).normalized);
                    uvs.Add(new Vector2(s / (float)seg, r / (float)ring));
                }
            }

            for (int r = 0; r < ring; r++)
            for (int s = 0; s < seg; s++)
            {
                int a = r * (seg + 1) + s;
                int b = a + 1;
                int c = a + (seg + 1);
                int d = c + 1;
                tris.Add(a); tris.Add(c); tris.Add(b);
                tris.Add(b); tris.Add(c); tris.Add(d);
            }

            var m = new Mesh();
            m.name = "BeamPathParticleSphere";
            m.SetVertices(verts);
            m.SetNormals(normals);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            return m;
        }

        private void OnDestroy()
        {
            for (int i = _transforms.Count - 1; i >= 0; i--)
            {
                if (_transforms[i] != null)
                {
                    if (Application.isPlaying) Destroy(_transforms[i].gameObject);
                    else DestroyImmediate(_transforms[i].gameObject);
                }
            }
            _transforms.Clear();
            _renderers.Clear();
        }
    }
}
