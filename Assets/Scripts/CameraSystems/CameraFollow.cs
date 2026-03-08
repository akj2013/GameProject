using UnityEngine;

namespace WoodLand3D.CameraSystems
{
    /// <summary>
    /// 카메라의 부드러운 추적 및 타일 포커스 기능을 담당한다.
    /// 위치는 target + offset 기준, 회전은 fixedRotation으로 고정 가능.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Header("추적 대상")]
        [SerializeField, Tooltip("평소에 따라갈 대상(예: 플레이어)")]
        private Transform target;

        [Header("위치")]
        [SerializeField, Tooltip("target 기준 상대 오프셋 (월드 방향). Y=높이, Z=뒤쪽 거리")]
        private Vector3 offset = new Vector3(0f, 8f, -6f);

        [SerializeField, Tooltip("이동 보간에 사용할 부드러운 시간")]
        private float smoothTime = 0.25f;

        [Header("회전")]
        [SerializeField, Tooltip("true면 매 프레임 fixedRotation으로 회전 고정")]
        private bool useFixedRotation = true;

        [SerializeField, Tooltip("고정할 오일러 각도 (X=위아래, Y=좌우, Z=기울기)")]
        private Vector3 fixedRotation = new Vector3(55f, 0f, 0f);

        private Vector3 _velocity;
        private Vector3? _focusPoint;

        private void LateUpdate()
        {
            if (useFixedRotation)
                transform.rotation = Quaternion.Euler(fixedRotation);

            Vector3 desired;
            if (_focusPoint.HasValue)
                desired = _focusPoint.Value + offset;
            else if (target != null)
                desired = target.position + offset;
            else
                return;

            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime);
        }

        public void FocusOnTile(Vector3 tilePos)
        {
            _focusPoint = tilePos;
        }

        public void ClearFocus()
        {
            _focusPoint = null;
        }
    }
}

/*
 * [설명]
 * 1. 왜 기존 방식에서 Main Camera Transform 값이 되돌아갔는지
 *    - LateUpdate()에서 desired = (target.x, transform.y, target.z) 형태로 목표 위치를 정한 뒤
 *      SmoothDamp로 매 프레임 위치를 덮어썼기 때문. Inspector에서 X/Z를 바꿔도 다음 프레임에
 *      target 기준으로 다시 계산되어 원래대로 돌아갔음.
 *
 * 2. 앞으로 카메라 구도를 어떤 값을 조절해서 바꾸면 되는지
 *    - offset: target(플레이어) 기준 카메라 위치. Y 올리면 위에서 내려다보는 각도, Z를 음수로 크게 하면 더 뒤에서 봄.
 *    - fixedRotation: 카메라가 바라보는 방향. X=55면 위에서 55도 내려다보는 탑다운/쿼터뷰.
 *    - smoothTime: 따라가는 속도. 작을수록 빠르게 붙음.
 *    - useFixedRotation: 끄면 씬/애니에서 준 회전이 유지되며, 켜면 매 프레임 fixedRotation으로 고정.
 */
