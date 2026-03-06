using UnityEngine;

namespace WoodLand3D.CameraSystems
{
    /// <summary>
    /// 카메라의 부드러운 추적 및 타일 포커스 기능을 담당한다.
    /// target이 있으면 평소에는 target을 따라가고, FocusOnTile 호출 시 해당 위치로 이동한다.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Header("추적 대상")]
        [SerializeField, Tooltip("평소에 따라갈 대상(예: 플레이어)")]
        private Transform target;
        [SerializeField, Tooltip("이동 보간에 사용할 부드러운 시간")]
        private float smoothTime = 0.25f;

        private Vector3 _velocity;
        private Vector3? _focusPoint;

        private void LateUpdate()
        {
            Vector3 desired = transform.position;

            if (_focusPoint.HasValue)
            {
                var p = _focusPoint.Value;
                desired = new Vector3(p.x, transform.position.y, p.z);
            }
            else if (target != null)
            {
                desired = new Vector3(target.position.x, transform.position.y, target.position.z);
            }

            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime);
        }

        /// <summary>
        /// 카메라가 지정한 월드 위치(타일)를 부드럽게 바라보도록 한다.
        /// </summary>
        public void FocusOnTile(Vector3 tilePos)
        {
            _focusPoint = tilePos;
        }

        /// <summary>
        /// 포커스를 해제하여 다시 target 추적 모드로 돌아간다.
        /// </summary>
        public void ClearFocus()
        {
            _focusPoint = null;
        }
    }
}
