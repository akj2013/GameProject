using UnityEngine;

namespace WoodLand3D.StarShip
{
    /// <summary>
    /// StarShipRouteLine 곡선을 따라가는 이중 튜브 메시(Core + Glow)를 생성·갱신합니다.
    /// 코어 = 안쪽 원통(본체), 글로우 = 바깥쪽 원통(외곽층). 카메라 의존 없이 구조적으로 분리합니다.
    /// </summary>
    public class BeamTubeMeshRenderer : MonoBehaviour
    {
        public const string CoreTubeName = "CoreTube";
        public const string GlowTubeName = "GlowTube";

        [Header("참조")]
        [Tooltip("곡선 경로. EvaluatePoint / EvaluateTangent 사용.")]
        public StarShipRouteLine targetRouteLine;

        [Header("튜브 메시")]
        [Tooltip("길이 방향 샘플 수 (링 개수 - 1).")]
        [Range(2, 128)]
        public int lengthSegments = 24;

        [Tooltip("한 링(원형 단면)의 버텍스 수.")]
        [Range(3, 64)]
        public int radialSegments = 12;

        [Tooltip("코어(안쪽) 튜브 반지름.")]
        [Min(0.001f)]
        public float coreRadius = 0.08f;

        [Tooltip("글로우(바깥) 튜브 반지름. 코어보다 커야 바깥층으로 보입니다.")]
        [Min(0.001f)]
        public float glowRadius = 0.15f;

        [Header("머티리얼")]
        public Material coreMaterial;
        public Material glowMaterial;

        [Header("동작")]
        [Tooltip("true면 매 프레임 메시 갱신 (곡선/시작·끝점 변경 반영).")]
        public bool updateEveryFrame = true;

        [Header("Reveal")]
        [Tooltip("0이면 사용 안 함. 양수면 해당 초 동안 Reveal 0→1 재생.")]
        public float revealDuration = 0f;
        public float revealSoftness = 0.05f;

        [Header("Beam Irregularity (단일 빔 유기화)")]
        [Tooltip("길이 방향 두께 변조 정도.")]
        [Range(0f, 4f)]
        public float widthVariationAmount = 0.12f;
        [Tooltip("두께 변동 주파수(클수록 잦음).")]
        [Range(0.5f, 250f)]
        public float widthVariationScale = 6f;
        [Tooltip("길이 방향 밝기 변조 정도.")]
        [Range(0f, 6f)]
        public float intensityVariationAmount = 0.25f;
        [Tooltip("밝기 변동 주파수.")]
        [Range(0.5f, 200f)]
        public float intensityVariationScale = 5f;
        [Tooltip("연속성 약화(일부 구간 알파 감쇠) 정도.")]
        [Range(0f, 5f)]
        public float continuityBreakAmount = 0.15f;
        [Tooltip("연속성 변동 주파수.")]
        [Range(1f, 250f)]
        public float continuityBreakScale = 8f;
        [Tooltip("경로 흔들림 정도.")]
        [Range(0f, 1f)]
        public float pathWobbleAmount = 0.03f;
        [Tooltip("경로 흔들림 주파수.")]
        [Range(1f, 200f)]
        public float pathWobbleScale = 5f;
        [Tooltip("코어 변조 위상 오프셋(글로우와 다르게).")]
        [Range(0f, 1f)]
        public float coreVariationBias = 0f;
        [Tooltip("글로우 변조 위상 오프셋.")]
        [Range(0f, 1f)]
        public float glowVariationBias = 0.5f;
        [Tooltip("(선택) 변조의 약한 시간 변화 속도.")]
        [Range(0f, 15f)]
        public float variationSpeed = 0.2f;

        private Mesh _coreMesh;
        private Mesh _glowMesh;
        private MeshFilter _coreFilter;
        private MeshFilter _glowFilter;
        private MeshRenderer _coreRenderer;
        private MeshRenderer _glowRenderer;

        private readonly System.Collections.Generic.List<Vector3> _vertices = new System.Collections.Generic.List<Vector3>();
        private readonly System.Collections.Generic.List<Vector3> _normals = new System.Collections.Generic.List<Vector3>();
        private readonly System.Collections.Generic.List<Vector2> _uvs = new System.Collections.Generic.List<Vector2>();
        private readonly System.Collections.Generic.List<int> _triangles = new System.Collections.Generic.List<int>();

        private int _cachedLengthSegments = -1;
        private int _cachedRadialSegments = -1;
        private float _reveal01;
        private MaterialPropertyBlock _propBlock;

        /// <summary> Reveal 진행도 0~1. Reveal 미사용 시 1. 시작점 발사광 등 연동용. </summary>
        public float Reveal01 => revealDuration > 0f ? _reveal01 : 1f;
        private static readonly int RevealId = Shader.PropertyToID("_Reveal");
        private static readonly int RevealSoftnessId = Shader.PropertyToID("_RevealSoftness");
        private static readonly int IntensityVariationAmountId = Shader.PropertyToID("_IntensityVariationAmount");
        private static readonly int IntensityVariationScaleId = Shader.PropertyToID("_IntensityVariationScale");
        private static readonly int ContinuityBreakAmountId = Shader.PropertyToID("_ContinuityBreakAmount");
        private static readonly int ContinuityBreakScaleId = Shader.PropertyToID("_ContinuityBreakScale");
        private static readonly int VariationBiasId = Shader.PropertyToID("_VariationBias");
        private static readonly int VariationSpeedId = Shader.PropertyToID("_VariationSpeed");

        private void Awake()
        {
            EnsureChildTubes();
        }

        private void Start()
        {
            RebuildTube();
            ApplyPropertyBlocks();
        }

        private void LateUpdate()
        {
            if (updateEveryFrame)
                RebuildTube();
            UpdateReveal();
        }

        private void UpdateReveal()
        {
            if (revealDuration > 0f)
            {
                _reveal01 += Time.deltaTime / revealDuration;
                _reveal01 = Mathf.Clamp01(_reveal01);
            }
            ApplyPropertyBlocks();
        }

        private void ApplyPropertyBlocks()
        {
            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
            _propBlock.SetFloat(RevealId, Reveal01);
            _propBlock.SetFloat(RevealSoftnessId, revealSoftness);
            _propBlock.SetFloat(IntensityVariationAmountId, intensityVariationAmount);
            _propBlock.SetFloat(IntensityVariationScaleId, intensityVariationScale);
            _propBlock.SetFloat(ContinuityBreakAmountId, continuityBreakAmount);
            _propBlock.SetFloat(ContinuityBreakScaleId, continuityBreakScale);
            _propBlock.SetFloat(VariationSpeedId, variationSpeed);

            _propBlock.SetFloat(VariationBiasId, coreVariationBias);
            if (_coreRenderer != null) _coreRenderer.SetPropertyBlock(_propBlock);

            _propBlock.SetFloat(VariationBiasId, glowVariationBias);
            if (_glowRenderer != null) _glowRenderer.SetPropertyBlock(_propBlock);
        }

        private void OnDestroy()
        {
            if (_coreMesh != null) { Destroy(_coreMesh); _coreMesh = null; }
            if (_glowMesh != null) { Destroy(_glowMesh); _glowMesh = null; }
        }

        /// <summary>
        /// CoreTube / GlowTube 자식을 생성하거나 찾아 MeshFilter/MeshRenderer를 연결합니다.
        /// </summary>
        private void EnsureChildTubes()
        {
            Transform coreT = transform.Find(CoreTubeName);
            if (coreT == null)
            {
                var coreGo = new GameObject(CoreTubeName);
                coreGo.transform.SetParent(transform, false);
                coreT = coreGo.transform;
            }
            _coreFilter = coreT.GetComponent<MeshFilter>();
            if (_coreFilter == null) _coreFilter = coreT.gameObject.AddComponent<MeshFilter>();
            _coreRenderer = coreT.GetComponent<MeshRenderer>();
            if (_coreRenderer == null) _coreRenderer = coreT.gameObject.AddComponent<MeshRenderer>();
            if (coreMaterial != null) _coreRenderer.sharedMaterial = coreMaterial;

            Transform glowT = transform.Find(GlowTubeName);
            if (glowT == null)
            {
                var glowGo = new GameObject(GlowTubeName);
                glowGo.transform.SetParent(transform, false);
                glowT = glowGo.transform;
            }
            _glowFilter = glowT.GetComponent<MeshFilter>();
            if (_glowFilter == null) _glowFilter = glowT.gameObject.AddComponent<MeshFilter>();
            _glowRenderer = glowT.GetComponent<MeshRenderer>();
            if (_glowRenderer == null) _glowRenderer = glowT.gameObject.AddComponent<MeshRenderer>();
            if (glowMaterial != null) _glowRenderer.sharedMaterial = glowMaterial;

            // Glow를 먼저 그리기 위해 자식 순서: GlowTube 먼저, CoreTube 나중 (Core가 앞에 보이도록)
            if (glowT.GetSiblingIndex() > coreT.GetSiblingIndex())
                glowT.SetAsFirstSibling();
        }

        /// <summary>
        /// 곡선을 샘플링해 코어/글로우 두 개의 튜브 메시를 생성·갱신합니다.
        /// </summary>
        public void RebuildTube()
        {
            if (targetRouteLine == null)
                return;
            if (targetRouteLine.startPoint == null || targetRouteLine.endPoint == null)
                return;

            EnsureChildTubes();
            if (coreMaterial != null) _coreRenderer.sharedMaterial = coreMaterial;
            if (glowMaterial != null) _glowRenderer.sharedMaterial = glowMaterial;

            int lenSeg = Mathf.Clamp(lengthSegments, 2, 128);
            int radSeg = Mathf.Clamp(radialSegments, 3, 64);
            bool sizeChanged = (_cachedLengthSegments != lenSeg || _cachedRadialSegments != radSeg);
            _cachedLengthSegments = lenSeg;
            _cachedRadialSegments = radSeg;

            float coreR = Mathf.Max(0.001f, coreRadius);
            float glowR = Mathf.Max(coreR + 0.001f, glowRadius);

            // Core 메시 (불규칙성 적용)
            BuildTubeData(coreR, lenSeg, radSeg, true);
            if (_coreMesh == null) _coreMesh = new Mesh();
            else if (sizeChanged) _coreMesh.Clear();
            ApplyTubeToMesh(_coreMesh);
            _coreFilter.sharedMesh = _coreMesh;

            // Glow 메시 (같은 곡선, 더 큰 반지름, 별도 변조 위상)
            BuildTubeData(glowR, lenSeg, radSeg, false);
            if (_glowMesh == null) _glowMesh = new Mesh();
            else if (sizeChanged) _glowMesh.Clear();
            ApplyTubeToMesh(_glowMesh);
            _glowFilter.sharedMesh = _glowMesh;
        }

        private void BuildTubeData(float radius, int lenSeg, int radSeg, bool isCore)
        {
            _vertices.Clear();
            _normals.Clear();
            _uvs.Clear();
            _triangles.Clear();

            int ringCount = lenSeg + 1;
            Vector3 tangentPrev = Vector3.forward;
            Vector3 upPrev = Vector3.up;
            float bias = isCore ? coreVariationBias : glowVariationBias;
            float time = Application.isPlaying ? Time.time * variationSpeed : 0f;

            for (int ring = 0; ring < ringCount; ring++)
            {
                float t = (ringCount == 1) ? 0f : ring / (float)lenSeg;
                Vector3 center = targetRouteLine.EvaluatePoint(t);
                Vector3 tangent = targetRouteLine.EvaluateTangent(t);
                if (tangent.sqrMagnitude < 0.0001f)
                    tangent = tangentPrev;
                tangent.Normalize();

                Vector3 right = Vector3.Cross(tangent, upPrev);
                if (right.sqrMagnitude < 0.0001f)
                    right = Vector3.Cross(tangent, Vector3.up);
                if (right.sqrMagnitude < 0.0001f)
                    right = Vector3.right;
                right.Normalize();

                Vector3 up = Vector3.Cross(right, tangent);
                up.Normalize();
                upPrev = up;

                // 경로 흔들림: 접선에 수직 방향으로 약한 오프셋
                float wobbleX = 0f;
                float wobbleY = 0f;
                if (pathWobbleAmount > 0.0001f && pathWobbleScale > 0f)
                {
                    float sx = t * pathWobbleScale + bias * 10f;
                    float sy = t * pathWobbleScale + bias * 10f + 5f;
                    wobbleX = (Mathf.PerlinNoise(sx, time) * 2f - 1f) * pathWobbleAmount;
                    wobbleY = (Mathf.PerlinNoise(sy, time) * 2f - 1f) * pathWobbleAmount;
                }
                center += right * wobbleX + up * wobbleY;

                // 길이 방향 두께 변조
                float radiusMult = 1f;
                if (widthVariationAmount > 0.0001f && widthVariationScale > 0f)
                {
                    float n = Mathf.PerlinNoise(t * widthVariationScale + bias * 7f, 0f);
                    radiusMult = 1f + (n * 2f - 1f) * widthVariationAmount;
                    radiusMult = Mathf.Max(0.3f, radiusMult);
                }
                float ringRadius = radius * radiusMult;

                float uLength = t;
                for (int r = 0; r < radSeg; r++)
                {
                    float angle = 2f * Mathf.PI * r / radSeg;
                    Vector3 offset = (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * ringRadius;
                    Vector3 pos = center + offset;
                    Vector3 normal = offset.normalized;

                    _vertices.Add(pos);
                    _normals.Add(normal);
                    _uvs.Add(new Vector2(uLength, (float)r / radSeg));
                }
                tangentPrev = tangent;
            }

            for (int ring = 0; ring < lenSeg; ring++)
            {
                int baseA = ring * radSeg;
                int baseB = (ring + 1) * radSeg;
                for (int r = 0; r < radSeg; r++)
                {
                    int nextR = (r + 1) % radSeg;
                    int a0 = baseA + r, a1 = baseA + nextR, b0 = baseB + r, b1 = baseB + nextR;
                    _triangles.Add(a0); _triangles.Add(b0); _triangles.Add(a1);
                    _triangles.Add(a1); _triangles.Add(b0); _triangles.Add(b1);
                }
            }
        }

        private void ApplyTubeToMesh(Mesh mesh)
        {
            mesh.SetVertices(_vertices);
            mesh.SetNormals(_normals);
            mesh.SetUVs(0, _uvs);
            mesh.SetTriangles(_triangles, 0);
            mesh.RecalculateBounds();
        }
    }
}
