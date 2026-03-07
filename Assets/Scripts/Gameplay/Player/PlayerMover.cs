using UnityEngine;
using WoodLand3D.UI.Controls;

namespace WoodLand3D.Gameplay.Player
{
    /// <summary>
    /// 플로팅 조이스틱 입력을 이용해 플레이어를 카메라 기준으로 이동·회전시키는 간단한 이동 컴포넌트.
    /// Animator의 Speed 파라미터를 갱신하며, Attack/Mine 트리거는 public 메서드로 외부 호출 가능.
    /// </summary>
    /// <remarks>
    /// AC_Player_Base 컨트롤러 기대 파라미터:
    /// - Float "Speed": 0=Idle, 약 0.1~2=Walk, 2 이상=Run (트랜지션 임계값에 맞춤)
    /// - Trigger "Attack": 도끼 등 공격 애니메이션
    /// - Trigger "Mine": 곡괭이 채광 애니메이션
    /// 이동은 transform 기반이므로 Animator에서 Apply Root Motion은 OFF로 두는 것을 권장.
    /// </remarks>
    [RequireComponent(typeof(Transform))]
    public class PlayerMover : MonoBehaviour
    {
        // AC_Player_Base 컨트롤러 파라미터 이름 (고정)
        private static readonly int SpeedParam = Animator.StringToHash("Speed");
        private const string AttackTriggerName = "Attack";
        private const string MineTriggerName = "Mine";

        [Header("이동 설정")]
        [SerializeField, Tooltip("최대 이동 속도 (풀 조이스틱 시 유닛/초)")]
        private float maxMoveSpeed = 4f;

        [SerializeField, Tooltip("회전 보간 속도 (클수록 빠르게 향함)")]
        private float rotationSpeed = 10f;

        [SerializeField, Tooltip("방향 스무딩 (클수록 반응 빠름)")]
        private float directionSmooth = 10f;

        [SerializeField, Tooltip("속도 스무딩 (클수록 가속/감속 빠름)")]
        private float speedSmooth = 10f;

        [Header("입력 소스")]
        [SerializeField, Tooltip("플로팅 조이스틱 UI 참조")]
        private FloatingJoystickUI joystick;

        [Header("참조 (선택)")]
        [SerializeField, Tooltip("이동 기준이 될 카메라 (비어 있으면 Camera.main)")]
        private Transform cameraTransform;

        // TODO 수동 설정: 이동을 코드(transform)로만 할 경우, Animator 컴포넌트에서 Apply Root Motion = OFF 로 설정 필요.
        [SerializeField, Tooltip("애니메이터 (비어 있으면 같은 오브젝트에서 GetComponent)")]
        private Animator animator;

        private Vector3 smoothMoveDir;
        private float smoothSpeedMagnitude;

        private void Awake()
        {
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            if (joystick == null)
                joystick = FindFirstObjectByType<FloatingJoystickUI>();

            if (animator == null)
                animator = GetComponent<Animator>();
        }

        private void Update()
        {
            if (joystick == null || cameraTransform == null)
            {
                if (animator != null)
                    animator.SetFloat(SpeedParam, 0f);
                return;
            }

            Vector2 input = joystick.Input;
            float inputMagnitude = Mathf.Clamp01(input.magnitude);

            // 카메라 기준 평면 방향 (Y축 기울기 무시)
            Vector3 camForward = cameraTransform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            Vector3 camRight = cameraTransform.right;
            camRight.y = 0f;
            camRight.Normalize();

            Vector3 targetDir = input.sqrMagnitude > 0.0001f
                ? (camForward * input.y + camRight * input.x).normalized
                : Vector3.zero;

            smoothMoveDir = Vector3.Lerp(
                smoothMoveDir,
                targetDir,
                directionSmooth * Time.deltaTime
            );

            smoothSpeedMagnitude = Mathf.Lerp(
                smoothSpeedMagnitude,
                inputMagnitude,
                speedSmooth * Time.deltaTime
            );

            float currentSpeed = smoothSpeedMagnitude * maxMoveSpeed;
            transform.position += smoothMoveDir * (currentSpeed * Time.deltaTime);

            if (animator != null)
                animator.SetFloat(SpeedParam, currentSpeed);

            // 실제 이동 방향으로 부드럽게 회전
            if (smoothMoveDir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(smoothMoveDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRot,
                    rotationSpeed * Time.deltaTime
                );
            }
        }

        /// <summary>
        /// 공격 애니메이션 재생 (AC_Player_Base Trigger "Attack").
        /// 채집/전투 등에서 호출.
        /// </summary>
        public void PlayAttackAnimation()
        {
            if (animator != null)
                animator.SetTrigger(AttackTriggerName);
        }

        /// <summary>
        /// 채광 애니메이션 재생 (AC_Player_Base Trigger "Mine").
        /// 채집 시 곡괭이 등에서 호출.
        /// </summary>
        public void PlayMineAnimation()
        {
            if (animator != null)
                animator.SetTrigger(MineTriggerName);
        }
    }
}

/*
 * ---------- Unity 수동 작업 목록 (코드에서 건드리지 않음) ----------
 * 1. AC_Player_Base: Idle -> Walk 트랜지션 추가 (조건: Speed > 0.1). 기존 Idle -> Idle 잘못된 트랜지션 수정.
 * 2. Animator (Player): Apply Root Motion = OFF 로 설정 (이동은 PlayerMover가 담당).
 * 3. Idle 클립: Meshy_AI_Animation_Idle_02_frame_rate_60.fbx -> Animation 탭에서 해당 클립 Loop Time 체크.
 * 4. (선택) Player 인스펙터에서 Animator 참조가 비어 있으면 Awake에서 같은 오브젝트 GetComponent로 채움.
 */

