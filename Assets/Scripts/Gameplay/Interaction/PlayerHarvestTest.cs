using UnityEngine;
using WoodLand3D.Resources.Nodes;

namespace WoodLand3D.Gameplay.Interaction
{
    /// <summary>
    /// 플레이어 정면 레이캐스트로 ResourceNode에 데미지를 주는 채집 컴포넌트. E 키로 채집.
    /// </summary>
    public class PlayerHarvestTest : MonoBehaviour
    {
        [Header("채집 설정")]
        [SerializeField, Tooltip("레이캐스트 거리")] private float rayDistance = 3f;
        [SerializeField, Tooltip("채집 입력 키")] private KeyCode harvestKey = KeyCode.E;
        [SerializeField, Tooltip("레이에 감지할 레이어")] private LayerMask hitMask = ~0;

        private void Update()
        {
            if (Input.GetKeyDown(harvestKey)) TryHarvest();
        }

        private void TryHarvest()
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f + transform.forward * 0.3f;
            Vector3 dir = transform.forward;
            if (Physics.Raycast(origin, dir, out var hit, rayDistance, hitMask, QueryTriggerInteraction.Ignore))
            {
                var node = hit.collider.GetComponentInParent<ResourceNode>();
                if (node != null) node.ApplyDamage(1);
            }
        }
    }
}
