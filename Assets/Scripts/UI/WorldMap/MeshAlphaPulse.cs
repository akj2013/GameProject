using UnityEngine;

namespace WoodLand3D.WorldMap
{
    /// <summary>
    /// MeshRenderer(Material)의 알파 값을 주기적으로 변화시켜
    /// 서서히 나타났다 사라지는 느낌을 만드는 스크립트.
    /// CloudFar 같은 Quad에 붙여서 사용한다.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class MeshAlphaPulse : MonoBehaviour
    {
        [Header("알파 값 범위 (0~1)")]
        public float minAlpha = 0.3f;
        public float maxAlpha = 1.0f;

        [Header("펄스 속도")]
        [Tooltip("알파가 최소→최대→최소로 한 바퀴 도는 데 걸리는 시간(초)")]
        public float pulseDuration = 1.5f;

        [Tooltip("여러 개가 동시에 붙어 있을 때, 시작 위상을 랜덤으로 섞을지 여부")]
        public bool randomStartPhase = true;

        private Renderer _renderer;
        private Material _materialInstance;
        private Color _baseColor;
        private float _timeOffset;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            if (_renderer != null)
            {
                // 다른 오브젝트와 머티리얼을 공유하지 않도록 인스턴스 생성
                _materialInstance = _renderer.material;
                _baseColor = _materialInstance.color;
            }

            if (randomStartPhase)
            {
                _timeOffset = Random.value * Mathf.Max(0.01f, pulseDuration);
            }
        }

        private void OnDestroy()
        {
            if (_materialInstance != null)
            {
                // 런타임에서 만든 머티리얼 인스턴스 정리
                Destroy(_materialInstance);
            }
        }

        private void Update()
        {
            if (_materialInstance == null)
                return;

            if (pulseDuration <= 0.0001f)
                return;

            // 0~1 구간을 기준으로 0→1→0으로 왕복하는 값을 만든다.
            float t = ((Time.time + _timeOffset) / pulseDuration) % 1f;
            float pingPong = t < 0.5f ? (t * 2f) : (1f - (t - 0.5f) * 2f);

            float a = Mathf.Lerp(minAlpha, maxAlpha, pingPong);

            var c = _baseColor;
            c.a = a;
            _materialInstance.color = c;
        }
    }
}

