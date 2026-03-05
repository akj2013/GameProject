using UnityEngine;

namespace WoodLand3D.Core.Resources
{
    /// <summary>
    /// Very simple test component to apply damage to ResourceNode in front of the player.
    /// Attach to the player object.
    /// </summary>
    public class PlayerHarvestTest : MonoBehaviour
    {
        [SerializeField] private float rayDistance = 3f;
        [SerializeField] private KeyCode harvestKey = KeyCode.E;
        [SerializeField] private LayerMask hitMask = ~0;

        private void Update()
        {
            if (Input.GetKeyDown(harvestKey))
            {
                TryHarvest();
            }
        }

        private void TryHarvest()
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            Vector3 dir = transform.forward;

            if (Physics.Raycast(origin, dir, out var hit, rayDistance, hitMask, QueryTriggerInteraction.Ignore))
            {
                var node = hit.collider.GetComponentInParent<ResourceNode>();
                if (node != null)
                {
                    node.ApplyDamage(1);
                }
            }
        }
    }
}

