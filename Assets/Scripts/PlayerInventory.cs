using UnityEngine;

namespace WoodLand3D.Gameplay
{
    /// <summary>
    /// Minimal player inventory: currently only tracks Gold.
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        [SerializeField] private int startingGold = 200;

        public int Gold { get; private set; }

        private void Awake()
        {
            Gold = startingGold;
        }

        public bool HasGold(int amount)
        {
            return Gold >= amount;
        }

        public void SpendGold(int amount)
        {
            if (amount <= 0)
                return;

            Gold = Mathf.Max(0, Gold - amount);
        }

        public void AddGold(int amount)
        {
            if (amount <= 0)
                return;

            Gold += amount;
        }
    }
}

