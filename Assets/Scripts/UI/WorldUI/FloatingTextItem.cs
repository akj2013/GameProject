using UnityEngine;

namespace WoodLand3D.UI.WorldUI
{
    /// <summary>
    /// 월드 스페이스 플로팅 텍스트 한 개를 담당한다.
    /// 위로 이동하며 페이드 아웃한 뒤 자기 자신을 파괴한다. 프리팹에 부착하고 매니저가 Setup으로 텍스트를 설정한다.
    /// </summary>
    public class FloatingTextItem : MonoBehaviour
    {
        [Header("애니메이션")]
        [SerializeField, Tooltip("표시 시간(초)")]
        private float duration = 1f;
        [SerializeField, Tooltip("위로 이동하는 높이")]
        private float moveUpAmount = 1.5f;

        private Vector3 _startPos;
        private float _elapsed;

#if UNITY_EDITOR || true
        private void Reset()
        {
            duration = 1f;
            moveUpAmount = 1.5f;
        }
#endif

        /// <summary>
        /// 표시할 텍스트와 시작 월드 위치를 설정한다. 매니저가 스폰 직후 호출한다.
        /// </summary>
        public void Setup(string text, Vector3 worldPosition)
        {
            _startPos = worldPosition;
            _elapsed = 0f;
            transform.position = worldPosition;

            var tmp = GetComponentInChildren<TMPro.TMP_Text>(true);
            if (tmp != null)
                tmp.text = text;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / duration);

            transform.position = _startPos + Vector3.up * (moveUpAmount * t);

            var tmp = GetComponentInChildren<TMPro.TMP_Text>(true);
            if (tmp != null)
            {
                Color c = tmp.color;
                c.a = 1f - t;
                tmp.color = c;
            }

            if (_elapsed >= duration)
                Destroy(gameObject);
        }
    }
}
