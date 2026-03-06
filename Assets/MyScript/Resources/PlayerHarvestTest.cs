using UnityEngine;

namespace WoodLand3D.Core.Resources
{
    /// <summary>
    /// 플레이어 정면 레이캐스트로 ResourceNode에 데미지를 주는 간단한 채집 테스트용 컴포넌트.
    /// 플레이어 오브젝트에 부착하고 E 키로 채집한다.
    /// </summary>
    public class PlayerHarvestTest : MonoBehaviour
    {
        [Header("채집 설정")]
        [SerializeField, Tooltip("레이캐스트 거리")]
        private float rayDistance = 3f;
        [SerializeField, Tooltip("채집 입력 키")]
        private KeyCode harvestKey = KeyCode.E;
        [SerializeField, Tooltip("레이에 감지할 레이어")]
        private LayerMask hitMask = ~0;

        private void Update()
        {
            if (Input.GetKeyDown(harvestKey))
                TryHarvest();
        }

        /// <summary>
        /// 플레이어 정면으로 레이를 쏴 ResourceNode가 있으면 데미지 1을 적용한다.
        /// </summary>
        private void TryHarvest()
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            Vector3 dir = transform.forward;

            if (Physics.Raycast(origin, dir, out var hit, rayDistance, hitMask, QueryTriggerInteraction.Ignore))
            {
                var node = hit.collider.GetComponentInParent<ResourceNode>();
                if (node != null)
                    node.ApplyDamage(1);
            }
        }
    }
}
