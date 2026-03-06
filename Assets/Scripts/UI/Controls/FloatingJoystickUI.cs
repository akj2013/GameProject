using UnityEngine;
using UnityEngine.EventSystems;

namespace WoodLand3D.UI.Controls
{
    /// <summary>
    /// 화면 아무 곳이나 터치/클릭했을 때 해당 위치에 플로팅 조이스틱을 띄우고,
    /// 드래그 방향을 -1~1 범위의 정규화된 입력(Vector2)으로 노출하는 UI 컴포넌트.
    /// 전체 화면을 덮는 투명 이미지(터치 영역)에 부착해 사용한다.
    /// </summary>
    public class FloatingJoystickUI : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("참조")]
        [SerializeField, Tooltip("최상위 Canvas의 RectTransform")]
        private RectTransform canvasRect;

        [SerializeField, Tooltip("조이스틱 전체(배경+핸들) 루트")]
        private RectTransform joystickRoot;

        [SerializeField, Tooltip("조이스틱 핸들 이미지 RectTransform")]
        private RectTransform handle;

        [Header("설정")]
        [SerializeField, Tooltip("핸들이 이동할 수 있는 반지름 (UI 픽셀 단위)")]
        private float radius = 80f;

        /// <summary>
        /// 현재 조이스틱 입력. X=좌우, Y=앞뒤. -1~1 범위.
        /// 입력이 없으면 (0,0).
        /// </summary>
        public Vector2 Input => _input;

        private Vector2 _input;
        private int _activePointerId = -1;

        private void Awake()
        {
            if (canvasRect == null)
                canvasRect = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();

            if (joystickRoot != null)
                joystickRoot.gameObject.SetActive(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // 이미 다른 손가락이 사용 중이면 무시
            if (_activePointerId != -1)
                return;

            _activePointerId = eventData.pointerId;
            _input = Vector2.zero;

            if (canvasRect == null || joystickRoot == null || handle == null)
                return;

            // 캔버스 로컬 좌표로 변환 후, 그 위치로 조이스틱 루트를 이동
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    eventData.position,
                    eventData.pressEventCamera,
                    out var localPos))
            {
                joystickRoot.anchoredPosition = localPos;
            }

            joystickRoot.gameObject.SetActive(true);
            handle.anchoredPosition = Vector2.zero;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != _activePointerId)
                return;

            if (canvasRect == null || joystickRoot == null || handle == null)
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    joystickRoot,
                    eventData.position,
                    eventData.pressEventCamera,
                    out var localPos))
            {
                return;
            }

            // 중심(0,0)을 기준으로 한 상대 위치를 반지름 안으로 클램프
            Vector2 clamped = Vector2.ClampMagnitude(localPos, radius);
            handle.anchoredPosition = clamped;

            // -1~1 범위로 정규화
            _input = clamped / Mathf.Max(radius, 0.0001f);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _activePointerId)
                return;

            _activePointerId = -1;
            _input = Vector2.zero;

            if (handle != null)
                handle.anchoredPosition = Vector2.zero;
            if (joystickRoot != null)
                joystickRoot.gameObject.SetActive(false);
        }
    }
}

