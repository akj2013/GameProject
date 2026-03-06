using System.Collections;
using UnityEngine;
using WoodLand3D.Core.Resources;

namespace WoodLand3D.UI
{
    /// <summary>
    /// 자원 획득 시 작은 비주얼을 스폰해 플레이어 쪽으로 날아가며 사라지게 하는 VFX 매니저.
    /// 프리팹·타겟이 없으면 경고만 남기고 스킵하며, 게임플레이에는 영향을 주지 않는다.
    /// </summary>
    public class ResourcePickupVFX : MonoBehaviour
    {
        [Header("프리팹(선택)")]
        [SerializeField, Tooltip("획득당 스폰할 작은 비주얼. 비어 있으면 이펙트 미표시")]
        private GameObject pickupVisualPrefab;

        [Header("타겟")]
        [SerializeField, Tooltip("비주얼이 날아갈 대상(보통 플레이어). 비어 있으면 PlayerInventory가 붙은 오브젝트를 찾음")]
        private Transform targetTransform;

        [Header("동작")]
        [SerializeField, Tooltip("획득당 최대 스폰 개수")]
        private int maxVisualsPerGain = 3;
        [SerializeField, Tooltip("처음 위로 튀어 오르는 높이")]
        private float popUpHeight = 0.5f;
        [SerializeField, Tooltip("타겟까지 이동하는 시간(초)")]
        private float moveDuration = 0.5f;
        [SerializeField, Tooltip("최대 대기 시간 후 강제 파괴(초)")]
        private float timeout = 2f;

        private void OnEnable()
        {
            ResourceGainedEvents.OnResourceGained += OnResourceGained;
        }

        private void OnDisable()
        {
            ResourceGainedEvents.OnResourceGained -= OnResourceGained;
        }

        private void OnResourceGained(ResourceType type, int amount, Vector3 worldPosition, Transform playerTransform)
        {
            if (pickupVisualPrefab == null)
                return;

            Transform target = playerTransform != null ? playerTransform : targetTransform;
            if (target == null)
            {
                var inv = UnityEngine.Object.FindFirstObjectByType<WoodLand3D.Gameplay.PlayerInventory>();
                if (inv != null)
                    target = inv.transform;
            }

            if (target == null)
            {
                Debug.LogWarning("ResourcePickupVFX: 타겟(플레이어) Transform을 찾을 수 없습니다. VFX를 건너뜁니다.");
                return;
            }

            int count = Mathf.Clamp(amount, 1, maxVisualsPerGain);
            for (int i = 0; i < count; i++)
            {
                Vector3 offset = Random.insideUnitSphere * 0.3f;
                offset.y = 0f;
                StartCoroutine(RunPickupVisual(worldPosition + offset, target));
            }
        }

        private IEnumerator RunPickupVisual(Vector3 startPos, Transform target)
        {
            GameObject instance = Instantiate(pickupVisualPrefab, startPos, Quaternion.identity);
            float elapsed = 0f;

            while (elapsed < timeout && instance != null && target != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / moveDuration);

                float upPhase = Mathf.Min(1f, t * 2f);
                Vector3 upOffset = Vector3.up * (popUpHeight * Mathf.Sin(upPhase * Mathf.PI));
                Vector3 toTarget = target.position + Vector3.up * 0.5f - startPos;
                instance.transform.position = startPos + upOffset + toTarget * t;

                if (t >= 1f)
                    break;
                yield return null;
            }

            if (instance != null)
                Destroy(instance);
        }
    }
}
