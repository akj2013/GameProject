using UnityEngine;
using System.Collections.Generic;

namespace WoodLand3D.StarShip
{
    /// <summary>
    /// 빔 도착점에서 생성되어 바깥으로 튀어나가며, 수명 동안 이동·페이드 후 사라졌다가
    /// 다시 생성되는 "에너지 파편형" 3D 스파크. 고정 막대가 아님.
    /// </summary>
    [ExecuteInEditMode]
    public class BeamEndSparks : MonoBehaviour
    {
        public const string SparkBaseName = "Spark_";

        [Header("참조")]
        public StarShipRouteLine targetRouteLine;
        [Tooltip("(선택) Reveal 진행도에 따라 스파크 강도 연동.")]
        public BeamTubeMeshRenderer revealSource;

        [Header("스파크 머티리얼")]
        public Material sparkMaterial;

        [Header("스파크 개수")]
        [Range(4, 128)]
        public int sparkCount = 10;

        [Header("수명·속도")]
        [Tooltip("스파크 수명 최소(초).")]
        [Range(0.05f, 5f)]
        public float sparkLifetimeMin = 0.12f;
        [Tooltip("스파크 수명 최대(초).")]
        [Range(0.1f, 8f)]
        public float sparkLifetimeMax = 0.35f;
        [Tooltip("바깥으로 날아가는 속도 최소.")]
        [Range(0.5f, 150f)]
        public float sparkSpeedMin = 2.5f;
        [Tooltip("바깥으로 날아가는 속도 최대.")]
        [Range(1f, 200f)]
        public float sparkSpeedMax = 6f;

        [Header("형태")]
        [Tooltip("스파크 길이 최소.")]
        [Range(0.03f, 50f)]
        public float sparkLengthMin = 0.08f;
        [Tooltip("스파크 길이 최대. 넉넉히 올리면 긴 꼬리 연출 가능.")]
        [Range(0.05f, 50f)]
        public float sparkLengthMax = 0.22f;
        [Tooltip("스파크 굵기.")]
        [Range(0.004f, 0.04f)]
        public float sparkThickness = 0.012f;

        [Header("생성 위치·방향")]
        [Tooltip("도착점에서 이 거리만큼 떨어진 곳에서 생성(0이면 도착점에서).")]
        [Range(0f, 2f)]
        public float sparkSpawnRadius = 0.05f;
        [Tooltip("빔 진행 방향 기준 퍼지는 콘 각도(도). 360이면 전방향.")]
        [Range(1f, 360f)]
        public float sparkConeAngle = 75f;

        [Header("색·강도")]
        public Color sparkColor = new Color(1f, 0.96f, 0.92f, 0.9f);
        [Range(0.3f, 2f)]
        public float sparkIntensity = 1f;

        [Header("동작")]
        public bool followEndPoint = true;

        private struct SparkState
        {
            public Vector3 position;
            public Vector3 velocity;
            public float lifetimeLeft;
            public float maxLifetime;
            public float length;
        }

        private readonly List<Transform> _sparkTransforms = new List<Transform>();
        private readonly List<MeshRenderer> _sparkRenderers = new List<MeshRenderer>();
        private SparkState[] _sparks;
        private int _cachedSparkCount = -1;
        private bool _didInitInPlayMode;
        private MaterialPropertyBlock _propBlock;
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int RevealId = Shader.PropertyToID("_Reveal");
        private static readonly int LifeId = Shader.PropertyToID("_Life");

        private static Mesh _sharedCylinderMesh;

        private void Awake()
        {
            if (Application.isPlaying)
                EnsureSparks();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
                _didInitInPlayMode = false;
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
                DestroyAllSparkChildren();
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying)
            {
                ClearSparkChildrenInEditor();
                return;
            }
            EnsureSparks();
            if (targetRouteLine == null || targetRouteLine.startPoint == null || targetRouteLine.endPoint == null)
                return;
            if (_sparks == null || _sparkTransforms.Count == 0)
                return;

            Vector3 endPos = targetRouteLine.EvaluatePoint(1f);
            Vector3 tangent = targetRouteLine.EvaluateTangent(1f);
            if (tangent.sqrMagnitude < 0.0001f)
                tangent = (targetRouteLine.endPoint.position - targetRouteLine.startPoint.position).normalized;

            if (Application.isPlaying && !_didInitInPlayMode)
            {
                _didInitInPlayMode = true;
                for (int i = 0; i < _sparks.Length; i++)
                    RespawnSpark(i, endPos, tangent);
            }

            float reveal = revealSource != null ? revealSource.Reveal01 : 1f;
            float dt = Application.isPlaying ? Time.deltaTime : 0.02f;

            for (int i = 0; i < _sparks.Length && i < _sparkTransforms.Count; i++)
            {
                Transform t = _sparkTransforms[i];
                if (t == null) continue;

                ref SparkState s = ref _sparks[i];

                if (s.maxLifetime <= 0f || s.lifetimeLeft <= 0f)
                {
                    RespawnSpark(i, endPos, tangent);
                    s = ref _sparks[i];
                }

                s.lifetimeLeft -= dt;
                if (s.lifetimeLeft <= 0f)
                {
                    RespawnSpark(i, endPos, tangent);
                    s = ref _sparks[i];
                }

                s.position += s.velocity * dt;

                t.position = s.position;
                if (s.velocity.sqrMagnitude > 0.0001f)
                    t.rotation = Quaternion.LookRotation(s.velocity);
                t.localScale = new Vector3(sparkThickness, sparkThickness, Mathf.Max(0.01f, s.length));

                float life = s.maxLifetime > 0f ? Mathf.Clamp01(s.lifetimeLeft / s.maxLifetime) : 0f;
                if (_sparkRenderers.Count > i && sparkMaterial != null)
                {
                    if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
                    _sparkRenderers[i].sharedMaterial = sparkMaterial;
                    _sparkRenderers[i].GetPropertyBlock(_propBlock);
                    _propBlock.SetColor(ColorId, sparkColor);
                    _propBlock.SetFloat(IntensityId, sparkIntensity);
                    _propBlock.SetFloat(RevealId, reveal);
                    _propBlock.SetFloat(LifeId, life);
                    _sparkRenderers[i].SetPropertyBlock(_propBlock);
                }
            }
        }

        private void RespawnSpark(int index, Vector3 endPos, Vector3 tangent)
        {
            Vector3 dir = RandomDirectionInCone(tangent, sparkConeAngle);
            float spawnDist = Mathf.Max(0f, sparkSpawnRadius);
            Vector3 pos = endPos + dir * spawnDist;
            float speed = Mathf.Lerp(sparkSpeedMin, sparkSpeedMax, Random.value);
            Vector3 velocity = dir * speed;
            float maxLt = Mathf.Lerp(sparkLifetimeMin, sparkLifetimeMax, Random.value);
            float length = Mathf.Lerp(sparkLengthMin, sparkLengthMax, Random.value);

            if (_sparks == null || index < 0 || index >= _sparks.Length) return;

            _sparks[index] = new SparkState
            {
                position = pos,
                velocity = velocity,
                lifetimeLeft = maxLt,
                maxLifetime = maxLt,
                length = length
            };
        }

        private static Vector3 RandomDirectionInCone(Vector3 coneAxis, float coneAngleDeg)
        {
            if (coneAngleDeg >= 359f)
                return Random.insideUnitSphere.normalized;
            float coneRad = Mathf.Clamp(coneAngleDeg, 1f, 358f) * 0.5f * Mathf.Deg2Rad;
            float u = Random.Range(0f, 1f);
            float v = Random.Range(0f, Mathf.PI * 2f);
            float r = Mathf.Sin(coneRad * Mathf.Sqrt(u));
            float h = Mathf.Cos(coneRad * Mathf.Sqrt(u));
            Vector3 local = new Vector3(r * Mathf.Cos(v), r * Mathf.Sin(v), h);
            if (Mathf.Abs(coneAxis.z - 1f) < 0.001f)
                return local.normalized;
            return Quaternion.FromToRotation(Vector3.forward, coneAxis) * local.normalized;
        }

        /// <summary>
        /// 에디터 전용: 하이어라키에 남아 있는 Spark_xx 자식 전부 제거. 플레이 중이 아닐 때만 호출.
        /// </summary>
        private void ClearSparkChildrenInEditor()
        {
            if (Application.isPlaying) return;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child != null && child.name.StartsWith(SparkBaseName, System.StringComparison.Ordinal))
                    DestroyImmediate(child.gameObject);
            }
            _sparkTransforms.Clear();
            _sparkRenderers.Clear();
            _cachedSparkCount = -1;
        }

        private void EnsureSparks()
        {
            if (!Application.isPlaying) return;

            int n = Mathf.Clamp(sparkCount, 4, 128);
            bool countChanged = _cachedSparkCount != n;
            _cachedSparkCount = n;

            if (countChanged)
            {
                _sparks = new SparkState[n];
                if (targetRouteLine != null && targetRouteLine.startPoint != null && targetRouteLine.endPoint != null)
                {
                    Vector3 endPos = targetRouteLine.EvaluatePoint(1f);
                    Vector3 tangent = targetRouteLine.EvaluateTangent(1f);
                    if (tangent.sqrMagnitude < 0.0001f)
                        tangent = (targetRouteLine.endPoint.position - targetRouteLine.startPoint.position).normalized;
                    for (int i = 0; i < n; i++)
                        RespawnSpark(i, endPos, tangent);
                }
                else
                {
                    for (int i = 0; i < n; i++)
                        RespawnSpark(i, Vector3.zero, Vector3.forward);
                }
            }

            Mesh cylinderMesh = GetOrCreateCylinderMesh();

            while (_sparkTransforms.Count < n)
            {
                int idx = _sparkTransforms.Count;
                var go = new GameObject(SparkBaseName + idx.ToString("00"));
                go.transform.SetParent(transform, false);
                var mf = go.AddComponent<MeshFilter>();
                mf.sharedMesh = cylinderMesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                if (sparkMaterial != null) mr.sharedMaterial = sparkMaterial;
                _sparkTransforms.Add(go.transform);
                _sparkRenderers.Add(mr);
            }

            while (_sparkTransforms.Count > n)
            {
                int last = _sparkTransforms.Count - 1;
                if (_sparkTransforms[last] != null)
                {
                    if (Application.isPlaying) Destroy(_sparkTransforms[last].gameObject);
                    else DestroyImmediate(_sparkTransforms[last].gameObject);
                }
                _sparkTransforms.RemoveAt(last);
                _sparkRenderers.RemoveAt(last);
            }
        }

        private static Mesh GetOrCreateCylinderMesh()
        {
            if (_sharedCylinderMesh != null) return _sharedCylinderMesh;
            _sharedCylinderMesh = BuildCylinderMesh();
            return _sharedCylinderMesh;
        }

        private static Mesh BuildCylinderMesh()
        {
            const int seg = 8;
            float r = 0.5f;
            float h = 0.5f;
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            for (int i = 0; i <= seg; i++)
            {
                float t = i / (float)seg * Mathf.PI * 2f;
                float cx = Mathf.Cos(t) * r;
                float cy = Mathf.Sin(t) * r;
                Vector3 n = new Vector3(cx, cy, 0).normalized;
                verts.Add(new Vector3(cx, cy, -h));
                verts.Add(new Vector3(cx, cy, h));
                normals.Add(n);
                normals.Add(n);
                uvs.Add(new Vector2(i / (float)seg, 0));
                uvs.Add(new Vector2(i / (float)seg, 1));
            }
            for (int i = 0; i < seg; i++)
            {
                int a = i * 2, b = i * 2 + 1, c = (i + 1) * 2, d = (i + 1) * 2 + 1;
                tris.Add(a); tris.Add(c); tris.Add(b);
                tris.Add(b); tris.Add(c); tris.Add(d);
            }
            var m = new Mesh();
            m.name = "BeamImpactSparkCylinder";
            m.SetVertices(verts);
            m.SetNormals(normals);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            return m;
        }

        private void DestroyAllSparkChildren()
        {
            for (int i = _sparkTransforms.Count - 1; i >= 0; i--)
            {
                if (_sparkTransforms[i] != null)
                {
                    if (Application.isPlaying)
                        Destroy(_sparkTransforms[i].gameObject);
                    else
                        DestroyImmediate(_sparkTransforms[i].gameObject);
                }
            }
            _sparkTransforms.Clear();
            _sparkRenderers.Clear();
        }

        private void OnDestroy()
        {
            DestroyAllSparkChildren();
        }
    }
}
