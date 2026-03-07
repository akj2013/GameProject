using UnityEngine;
using WoodLand3D.Resources.Types;

namespace WoodLand3D.Gameplay.Inventory
{
    /// <summary>
    /// 플레이어의 골드·자원 인벤토리를 관리한다. 채집 보상 및 언락 비용에 사용.
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        [Header("초기값")]
        [SerializeField, Tooltip("시작 골드")]
        private int startingGold = 500;

        private int _gold;
        private System.Collections.Generic.Dictionary<ResourceType, int> _resources = new System.Collections.Generic.Dictionary<ResourceType, int>();

        /// <summary>현재 골드.</summary>
        public int Gold => _gold;

        /// <summary>자원 수량 변경 시 (타입, 변경 후 총량, 이번 증분).</summary>
        public System.Action<ResourceType, int, int> OnResourceChanged;

        private void Awake()
        {
            _gold = startingGold;
            foreach (ResourceType t in System.Enum.GetValues(typeof(ResourceType)))
                _resources[t] = 0;
        }

        /// <summary>
        /// 자원을 추가한다. OnResourceChanged 발생.
        /// </summary>
        public void AddResource(ResourceType type, int amount)
        {
            if (amount <= 0) return;
            int oldTotal = GetResourceAmount(type);
            _resources[type] = oldTotal + amount;
            OnResourceChanged?.Invoke(type, _resources[type], amount);
        }

        /// <summary>
        /// 해당 자원 현재 수량.
        /// </summary>
        public int GetCount(ResourceType type) => GetResourceAmount(type);

        /// <summary>
        /// 해당 자원 현재 수량. (HUD 등에서 사용)
        /// </summary>
        public int GetResourceAmount(ResourceType type) => _resources.TryGetValue(type, out var v) ? v : 0;

        /// <summary>
        /// 지정 골드 이상 보유 여부.
        /// </summary>
        public bool HasGold(int amount) => amount >= 0 && _gold >= amount;

        /// <summary>
        /// 골드 사용 (언락 등). 성공 시 true.
        /// </summary>
        public bool SpendGold(int amount)
        {
            if (amount <= 0 || _gold < amount) return false;
            _gold -= amount;
            return true;
        }
    }
}
