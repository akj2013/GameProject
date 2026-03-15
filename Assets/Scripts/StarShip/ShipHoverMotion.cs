using UnityEngine;

namespace WoodLand3D.StarShip
{
    /// <summary>
    /// 비행선 Hover 모션.
    /// 기준 위치를 중심으로 상하좌우로 부드럽게 떠 있는 느낌을 만든다.
    /// - 이동 범위와 속도는 인스펙터에서 XYZ 각각 조절 가능.
    /// - 부모 기준으로 움직이게 하고 싶으면 useLocalPosition 을 켜두면 된다.
    /// </summary>
    public class ShipHoverMotion : MonoBehaviour
    {
        [Header("좌표 기준")]
        [Tooltip("로컬 좌표 기준으로 움직일지 여부 (체크 권장: 부모 기준으로 Hover)")]
        public bool useLocalPosition = true;

        [Header("이동 범위 (반경, 유닛 단위)")]
        [Tooltip("각 축으로 기준 위치에서 얼마나 멀리 움직일지 (예: Y = 0.05~0.15 정도)")]
        public Vector3 amplitude = new Vector3(0f, 0.1f, 0f);

        [Header("이동 속도 (초당 사이클 수)")]
        [Tooltip("각 축의 Hover 속도 (1이면 1초에 한 번 위아래 왕복 정도 느낌)")]
        public Vector3 frequency = new Vector3(0f, 0.5f, 0f);

        [Header("위상 보정")]
        [Tooltip("각 축에 추가할 위상(라디안). 여러 대가 있을 때 시작 타이밍을 어긋나게 할 때 사용")]
        public Vector3 phaseOffset = Vector3.zero;

        private Vector3 _basePosition;

        private void Awake()
        {
            CacheBasePosition();
        }

        private void OnEnable()
        {
            CacheBasePosition();
        }

        private void CacheBasePosition()
        {
            _basePosition = useLocalPosition ? transform.localPosition : transform.position;
        }

        private void Update()
        {
            float t = Time.time;

            float offsetX = amplitude.x * Mathf.Sin(2f * Mathf.PI * frequency.x * t + phaseOffset.x);
            float offsetY = amplitude.y * Mathf.Sin(2f * Mathf.PI * frequency.y * t + phaseOffset.y);
            float offsetZ = amplitude.z * Mathf.Sin(2f * Mathf.PI * frequency.z * t + phaseOffset.z);

            Vector3 offset = new Vector3(offsetX, offsetY, offsetZ);

            if (useLocalPosition)
                transform.localPosition = _basePosition + offset;
            else
                transform.position = _basePosition + offset;
        }
    }
}

