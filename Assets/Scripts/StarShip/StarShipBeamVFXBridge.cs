using UnityEngine;
using UnityEngine.VFX;

namespace WoodLand3D.StarShip
{
    /// <summary>
    /// StarShipRouteLine 곡선을 샘플링하여 1픽셀 높이 Texture2D에 저장하고 VisualEffect에 전달하는 브리지.
    /// VFX Graph에서 UV 기반으로 곡선 포인트/접선을 샘플링해 빔 본체·버스트 파티클을 구현할 수 있다.
    /// </summary>
    public class StarShipBeamVFXBridge : MonoBehaviour
    {
        [Header("참조")]
        public StarShipRouteLine targetRouteLine;
        public VisualEffect targetVFX;

        [Tooltip("빔 시작점으로 쓸 비행선 Transform. 비어 있으면 곡선 t=0을 사용.")]
        public Transform starShipTransform;

        [Header("샘플링")]
        [Tooltip("곡선을 나눌 샘플 수 (2~128). VFX의 Strip Particle Count와 같거나 작게 맞출 것.")]
        public int sampleCount = 16;

        [Header("빔 파라미터")]
        public float beamWidth = 1f;
        public float glowWidth = 1.4f;
        public Color beamColor = Color.white;
        public Color glowColor = Color.cyan;

        [Header("동작")]
        [Tooltip("true면 매 프레임 VFX에 갱신 전달.")]
        public bool updateEveryFrame = true;

        [Header("Reveal")]
        [Tooltip("빔이 시작점에서 끝점까지 차오르는 데 걸리는 시간(초). 0 이하면 즉시 전부 표시.")]
        public float revealDuration = 0.35f;
        [Tooltip("Reveal 경계 부드러움 (Shader Reveal Softness).")]
        public float revealSoftness = 0.05f;

        private Texture2D _pointTexture;
        private Texture2D _tangentTexture;
        private float _reveal01 = 0f;

        private void Start()
        {
            _reveal01 = 0f;
            PushToVFX();
        }

        private void LateUpdate()
        {
            if (revealDuration > 0f)
                _reveal01 += Time.deltaTime / revealDuration;
            else
                _reveal01 = 1f;
            _reveal01 = Mathf.Clamp01(_reveal01);

            if (updateEveryFrame)
                PushToVFX();
        }

        private void OnDestroy()
        {
            if (_pointTexture != null)
            {
                Destroy(_pointTexture);
                _pointTexture = null;
            }
            if (_tangentTexture != null)
            {
                Destroy(_tangentTexture);
                _tangentTexture = null;
            }
        }

        /// <summary>
        /// 곡선을 샘플링해 텍스처에 채운 뒤 targetVFX에 전달.
        /// </summary>
        public void PushToVFX()
        {
            if (targetRouteLine == null || targetVFX == null)
                return;
            if (targetRouteLine.startPoint == null || targetRouteLine.endPoint == null)
                return;

            int count = Mathf.Clamp(sampleCount, 2, 128);

            // 필요 시 텍스처 생성/재생성
            if (_pointTexture == null || _pointTexture.width != count)
            {
                if (_pointTexture != null) Destroy(_pointTexture);
                _pointTexture = CreateCurveTexture(count);
            }
            if (_tangentTexture == null || _tangentTexture.width != count)
            {
                if (_tangentTexture != null) Destroy(_tangentTexture);
                _tangentTexture = CreateCurveTexture(count);
            }

            // PointTexture / TangentTexture 는 월드 좌표 기준으로 VFX에 전달됨
            Vector3 beamStartWorld = starShipTransform != null
                ? starShipTransform.position
                : targetRouteLine.EvaluatePoint(0f);

            for (int i = 0; i < count; i++)
            {
                float t = (count == 1) ? 0f : i / (float)(count - 1);
                Vector3 pos;
                if (i == 0)
                    pos = beamStartWorld;
                else
                    pos = targetRouteLine.EvaluatePoint(t);

                Vector3 tangent = targetRouteLine.EvaluateTangent(t);

                _pointTexture.SetPixel(i, 0, new Color(pos.x, pos.y, pos.z, 1f));
                _tangentTexture.SetPixel(i, 0, new Color(tangent.x, tangent.y, tangent.z, 1f));
            }

            _pointTexture.Apply(false);
            _tangentTexture.Apply(false);

            targetVFX.SetTexture("PointTexture", _pointTexture);
            targetVFX.SetTexture("TangentTexture", _tangentTexture);
            targetVFX.SetInt("SampleCount", count);
            targetVFX.SetFloat("BeamWidth", beamWidth);
            targetVFX.SetFloat("GlowWidth", glowWidth);
            targetVFX.SetVector4("BeamColor", beamColor);
            targetVFX.SetVector4("GlowColor", glowColor);
            targetVFX.SetVector3("BeamStart", beamStartWorld);
            targetVFX.SetVector3("BeamEnd", targetRouteLine.EvaluatePoint(1f));
            targetVFX.SetFloat("Reveal", _reveal01);
            targetVFX.SetFloat("RevealSoftness", revealSoftness);
        }

        private static Texture2D CreateCurveTexture(int width)
        {
            var tex = new Texture2D(width, 1, TextureFormat.RGBAFloat, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }
    }
}

// Inspector에서 조절: Reveal 섹션 → Reveal Duration(차오르는 시간), Reveal Softness(경계 부드러움)
