using UnityEngine;

namespace WoodLand3D.Gameplay.Interaction
{
    /// <summary>
    /// 레거시 E키 수동 채집용. 자동 채집을 쓰는 경우 PlayerAutoHarvest를 사용하고 이 컴포넌트는 제거하거나 비활성화한다.
    /// </summary>
    public class PlayerHarvestTest : MonoBehaviour
    {
        [Header("수동 채집 (선택)")]
        [SerializeField, Tooltip("채집 키")]
        private KeyCode harvestKey = KeyCode.E;

        [SerializeField, Tooltip("레이 거리")]
        private float rayDistance = 2f;

        private void Update()
        {
            if (!Input.GetKeyDown(harvestKey)) return;

            var origin = transform.position + Vector3.up * 0.5f + transform.forward * 0.3f;
            if (!Physics.Raycast(origin, transform.forward, out var hit, rayDistance, ~0, QueryTriggerInteraction.Ignore))
                return;

            var node = hit.collider.GetComponent<Resources.Nodes.ResourceNode>();
            if (node != null && node.IsValid)
                node.ApplyDamage(1);
        }
    }
}
