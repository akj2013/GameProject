using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace WoodLand3D.WorldMap
{
    /// <summary>
    /// 타일 슬롯 안의 Spark 이미지 하나를
    /// 일정 간격으로 랜덤 위치에서 반짝이게 해주는 스크립트.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class TileSparkleUI : MonoBehaviour
    {
        [Header("반짝이 주기 (초)")]
        [Tooltip("반짝이 사이 최소 대기 시간")]
        public float minDelay = 0.8f;

        [Tooltip("반짝이 사이 최대 대기 시간")]
        public float maxDelay = 2.0f;

        [Header("반짝이 모양")]
        [Tooltip("반짝이가 유지되는 시간")]
        public float sparkleDuration = 0.5f;

        [Tooltip("최소/최대 스케일")]
        public Vector2 sparkleScaleRange = new Vector2(0.6f, 1.0f);

        [Tooltip("알파 값 범위 (0~1)")]
        public float minAlpha = 0f;
        public float maxAlpha = 1f;

        [Header("위치 설정")]
        [Tooltip("시작 시 RectTransform의 현재 위치를 기준 위치로 사용할지 여부")]
        public bool useInitialAnchoredPosition = true;

        [Tooltip("직접 지정할 기준 위치 (useInitialAnchoredPosition 이 false 일 때 사용)")]
        public Vector2 customAnchoredPosition;

        private Image _image;
        private RectTransform _rect;
        private Vector2 _baseAnchoredPosition;

        private void Awake()
        {
            _image = GetComponent<Image>();
            _rect = GetComponent<RectTransform>();

            // 기준 위치 저장
            if (_rect != null)
            {
                _baseAnchoredPosition = _rect.anchoredPosition;
                // 인스펙터에서 바로 현재 값을 보고 수정하기 좋게 초기값을 복사해 둔다.
                if (useInitialAnchoredPosition)
                    customAnchoredPosition = _baseAnchoredPosition;
            }

            // 처음엔 최소 알파로
            SetAlpha(minAlpha);
        }

        private void OnEnable()
        {
            StartCoroutine(SparkleLoop());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }

        private IEnumerator SparkleLoop()
        {
            while (true)
            {
                // 랜덤 대기
                float delay = Random.Range(minDelay, maxDelay);
                yield return new WaitForSeconds(delay);

                // 위치는 항상 고정된 기준 위치를 사용
                if (_rect != null)
                {
                    _rect.anchoredPosition = useInitialAnchoredPosition
                        ? _baseAnchoredPosition
                        : customAnchoredPosition;
                }

                float scale = Random.Range(sparkleScaleRange.x, sparkleScaleRange.y);
                _rect.localScale = new Vector3(scale, scale, 1f);

                // 페이드 인/아웃
                float half = sparkleDuration * 0.5f;
                float t = 0f;

                // 페이드 인
                while (t < half)
                {
                    t += Time.deltaTime;
                    float u = Mathf.Clamp01(t / half);
                    float a = Mathf.Lerp(minAlpha, maxAlpha, u);
                    SetAlpha(a);
                    yield return null;
                }

                // 페이드 아웃
                t = 0f;
                while (t < half)
                {
                    t += Time.deltaTime;
                    float u = 1f - Mathf.Clamp01(t / half);
                    float a = Mathf.Lerp(minAlpha, maxAlpha, u);
                    SetAlpha(a);
                    yield return null;
                }

                SetAlpha(minAlpha);
            }
        }

        private void SetAlpha(float a)
        {
            if (_image == null) return;
            var c = _image.color;
            c.a = a;
            _image.color = c;
        }
    }
}