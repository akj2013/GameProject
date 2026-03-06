using UnityEngine;
using WoodLand3D.UI.Controls;

namespace WoodLand3D.Gameplay.Player
{
    /// <summary>
    /// 플로팅 조이스틱 입력을 이용해 플레이어를 카메라 기준으로 이동·회전시키는 간단한 이동 컴포넌트.
    /// 키보드 입력은 사용하지 않으며, 기존 E 키 채집 스크립트와는 독립적으로 동작한다.
    /// </summary>
    [RequireComponent(typeof(Transform))]
    public class PlayerMover : MonoBehaviour
    {
        [Header("이동 설정")]
        [SerializeField, Tooltip("이동 속도 (유닛/초)")]
        private float moveSpeed = 4f;

        [SerializeField, Tooltip("회전 속도 (보간 계수)")]
        private float rotationSpeed = 10f;

        [Header("입력 소스")]
        [SerializeField, Tooltip("플로팅 조이스틱 UI 참조")]
        private FloatingJoystickUI joystick;

        [Header("참조 (선택)")]
        [SerializeField, Tooltip("이동 기준이 될 카메라 (비어 있으면 Camera.main)")]
        private Transform cameraTransform;

        private void Awake()
        {
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            if (joystick == null)
                joystick = FindObjectOfType<FloatingJoystickUI>();
        }

        private void Update()
        {
            if (joystick == null || cameraTransform == null)
                return;

            Vector2 input = joystick.Input;
            if (input == Vector2.zero)
                return;

            // 카메라 기준 평면 방향 계산 (Y축 기울기는 무시)
            Vector3 camForward = cameraTransform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            Vector3 camRight = cameraTransform.right;
            camRight.y = 0f;
            camRight.Normalize();

            Vector3 moveDir = camForward * input.y + camRight * input.x;
            if (moveDir.sqrMagnitude < 0.0001f)
                return;

            moveDir.Normalize();

            // 위치 이동 (Rigidbody가 있다면 IsKinematic=true 인 상태에서 transform 이동)
            transform.position += moveDir * (moveSpeed * Time.deltaTime);

            // 이동 방향을 향하도록 부드럽게 회전
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }
}

