using UnityEngine;

public class PlayerAnimTest : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private float speed = 0f;

    private void Reset()
    {
        animator = GetComponent<Animator>();
    }

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Update()
    {
        // 숫자키로 이동 상태 테스트
        if (Input.GetKeyDown(KeyCode.Alpha1))
            speed = 0f;   // Idle

        if (Input.GetKeyDown(KeyCode.Alpha2))
            speed = 1f;   // Walk

        if (Input.GetKeyDown(KeyCode.Alpha3))
            speed = 3f;   // Run

        animator.SetFloat("Speed", speed);

        // 공격 테스트
        if (Input.GetKeyDown(KeyCode.Q))
            animator.SetTrigger("Attack");

        if (Input.GetKeyDown(KeyCode.E))
            animator.SetTrigger("Mine");
    }
}