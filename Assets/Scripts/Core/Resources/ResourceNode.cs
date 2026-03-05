using System;
using UnityEngine;

namespace WoodLand3D.Core.Resources
{
    /// <summary>
    /// Single resource instance (tree, rock, ore).
    /// Handles HP and depletion, but NOT respawn.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ResourceNode : MonoBehaviour
    {
        [SerializeField] private ResourceType type = ResourceType.Tree;
        [SerializeField] private int maxHp = 3;
        [SerializeField] private float respawnTime = 30f;

        public ResourceType Type => type;
        public int MaxHp => maxHp;
        public int CurrentHp { get; private set; }
        public float RespawnTime => respawnTime;
        public bool IsDepleted { get; private set; }

        private Action<ResourceNode> _onDepleted;

        /// <summary>
        /// Initialize the node with custom stats and depletion callback.
        /// </summary>
        public void Initialize(ResourceType resourceType, int hp, float respawnSeconds, Action<ResourceNode> onDepleted)
        {
            type = resourceType;
            maxHp = Mathf.Max(1, hp);
            respawnTime = Mathf.Max(0f, respawnSeconds);
            CurrentHp = maxHp;
            IsDepleted = false;
            _onDepleted = onDepleted;

            gameObject.SetActive(true);
            var col = GetComponent<Collider>();
            if (col != null)
                col.enabled = true;
        }

        public void ApplyDamage(int amount)
        {
            if (IsDepleted)
                return;
            if (amount <= 0)
                return;

            CurrentHp -= amount;
            if (CurrentHp <= 0)
            {
                Deplete();
            }
        }

        private void Deplete()
        {
            if (IsDepleted)
                return;

            IsDepleted = true;

            // Notify spawner/manager first so it can capture position/state.
            _onDepleted?.Invoke(this);

            // Disable visuals & collider.
            var col = GetComponent<Collider>();
            if (col != null)
                col.enabled = false;

            // Optionally disable entire object.
            gameObject.SetActive(false);
        }
    }
}

