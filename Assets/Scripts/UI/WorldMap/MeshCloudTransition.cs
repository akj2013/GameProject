using UnityEngine;

namespace WoodLand3D.WorldMap
{
    /// <summary>
    /// 구름 메쉬(CloudFarLeft 등)용 원방향 전환 스크립트.
    /// 1) 알파: 지속시간 동안 최초 알파 → 최종 알파 (원상복귀 없음)
    /// 2) 위치: 지속시간 동안 최초 좌표 → 최종 좌표 (원상복귀 없음)
    /// 활성화 시 한 번만 재생된다.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class MeshCloudTransition : MonoBehaviour
    {
        [Header("알파 전환")]
        [Tooltip("알파 전환 사용 여부")]
        public bool enableAlphaTransition = true;

        [Tooltip("시작 알파 (0~1)")]
        [Range(0f, 1f)]
        public float startAlpha = 1f;

        [Tooltip("끝 알파 (0~1)")]
        [Range(0f, 1f)]
        public float endAlpha = 0f;

        [Tooltip("알파가 start → end 로 바뀌는 데 걸리는 시간(초)")]
        public float alphaDuration = 2f;

        [Header("위치 전환")]
        [Tooltip("위치 전환 사용 여부")]
        public bool enablePositionTransition = true;

        [Tooltip("좌표를 로컬 기준으로 할지, 월드 기준으로 할지")]
        public bool useLocalPosition = true;

        [Tooltip("시작 좌표")]
        public Vector3 startPosition;

        [Tooltip("끝 좌표")]
        public Vector3 endPosition;

        [Tooltip("위치가 start → end 로 바뀌는 데 걸리는 시간(초)")]
        public float positionDuration = 2f;

        private Renderer _renderer;
        private Material _materialInstance;
        private Color _baseColor;
        private Transform _transform;
        private float _startTime;
        private bool _running;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _transform = transform;

            if (_renderer != null)
            {
                _materialInstance = _renderer.material;
                _baseColor = _materialInstance.color;
            }
        }

        private void OnEnable()
        {
            _startTime = Time.time;
            _running = true;

            // 시작 시점 값 적용
            if (enableAlphaTransition && _materialInstance != null)
            {
                var c = _baseColor;
                c.a = startAlpha;
                _materialInstance.color = c;
            }

            if (enablePositionTransition && _transform != null)
            {
                if (useLocalPosition)
                    _transform.localPosition = startPosition;
                else
                    _transform.position = startPosition;
            }
        }

        private void OnDestroy()
        {
            if (_materialInstance != null)
                Destroy(_materialInstance);
        }

        private void Update()
        {
            if (!_running)
                return;

            float elapsed = Time.time - _startTime;

            if (enableAlphaTransition && _materialInstance != null && alphaDuration > 0.0001f)
            {
                float t = Mathf.Clamp01(elapsed / alphaDuration);
                float a = Mathf.Lerp(startAlpha, endAlpha, t);
                var c = _baseColor;
                c.a = a;
                _materialInstance.color = c;
            }

            if (enablePositionTransition && _transform != null && positionDuration > 0.0001f)
            {
                float t = Mathf.Clamp01(elapsed / positionDuration);
                Vector3 pos = Vector3.Lerp(startPosition, endPosition, t);
                if (useLocalPosition)
                    _transform.localPosition = pos;
                else
                    _transform.position = pos;
            }

            // 두 전환이 모두 끝나면 업데이트 중단
            bool alphaDone = !enableAlphaTransition || elapsed >= alphaDuration;
            bool positionDone = !enablePositionTransition || elapsed >= positionDuration;
            if (alphaDone && positionDone)
                _running = false;
        }

        /// <summary>
        /// 현재 Transform 위치를 startPosition 또는 endPosition 으로 잡을 때 호출.
        /// 인스펙터에서 "현재 위치를 시작으로" 버튼 등으로 연동 가능.
        /// </summary>
        public void SetStartPositionToCurrent()
        {
            startPosition = useLocalPosition ? transform.localPosition : transform.position;
        }

        public void SetEndPositionToCurrent()
        {
            endPosition = useLocalPosition ? transform.localPosition : transform.position;
        }
    }
}
