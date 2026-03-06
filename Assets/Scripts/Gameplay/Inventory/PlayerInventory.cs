using System;
using UnityEngine;
using WoodLand3D.Resources.Types;

namespace WoodLand3D.Gameplay.Inventory
{
    /// <summary>
    /// 플레이어의 자원 인벤토리를 관리하는 스크립트.
    /// 골드(타일 언락 비용)와 각종 자원(나무, 돌, 광석)의 획득·사용·조회를 담당한다.
    /// 리소스는 무제한 보관 가능하며, 변경 시 이벤트를 발생시킨다.
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        [Header("골드 설정")]
        [SerializeField, Tooltip("게임 시작 시 지급되는 골드")]
        private int startingGold = 200;

        public int Gold { get; private set; }

        /// <summary> 골드 변경 시 (변경 후 총량) </summary>
        public event Action<int> OnGoldChanged;

        /// <summary> 자원 변경 시 (자원 타입, 변경 후 총량, 이번 증감량) </summary>
        public event Action<ResourceType, int, int> OnResourceChanged;

        private readonly System.Collections.Generic.Dictionary<ResourceType, int> _resources = new System.Collections.Generic.Dictionary<ResourceType, int>();

        private void Awake()
        {
            Gold = startingGold;
            OnGoldChanged?.Invoke(Gold);
        }

        public bool HasGold(int amount) => Gold >= amount;

        public void SpendGold(int amount)
        {
            if (amount <= 0) return;
            int prev = Gold;
            Gold = Mathf.Max(0, Gold - amount);
            if (Gold != prev) OnGoldChanged?.Invoke(Gold);
        }

        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            Gold += amount;
            OnGoldChanged?.Invoke(Gold);
        }

        public int GetResourceAmount(ResourceType type) => _resources.TryGetValue(type, out int v) ? v : 0;

        public void AddResource(ResourceType type, int amount)
        {
            if (amount <= 0) return;
            int prev = GetResourceAmount(type);
            int next = prev + amount;
            _resources[type] = next;
            OnResourceChanged?.Invoke(type, next, amount);
        }

        public bool HasResource(ResourceType type, int amount) => GetResourceAmount(type) >= amount;

        public bool SpendResource(ResourceType type, int amount)
        {
            if (amount <= 0) return false;
            int prev = GetResourceAmount(type);
            if (prev < amount) return false;
            int next = prev - amount;
            _resources[type] = next;
            OnResourceChanged?.Invoke(type, next, -amount);
            return true;
        }
    }
}
