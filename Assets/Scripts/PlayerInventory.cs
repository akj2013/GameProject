using System;
using System.Collections.Generic;
using UnityEngine;
using WoodLand3D.Core.Resources;

namespace WoodLand3D.Gameplay
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

        private readonly Dictionary<ResourceType, int> _resources = new Dictionary<ResourceType, int>();

        private void Awake()
        {
            Gold = startingGold;
            OnGoldChanged?.Invoke(Gold);
        }

        /// <summary>
        /// 보유 골드가 지정한 양 이상인지 확인한다.
        /// </summary>
        public bool HasGold(int amount)
        {
            return Gold >= amount;
        }

        /// <summary>
        /// 골드를 소비한다. 타일 언락 등에 사용.
        /// </summary>
        public void SpendGold(int amount)
        {
            if (amount <= 0)
                return;

            int prev = Gold;
            Gold = Mathf.Max(0, Gold - amount);
            if (Gold != prev)
                OnGoldChanged?.Invoke(Gold);
        }

        /// <summary>
        /// 골드를 추가한다.
        /// </summary>
        public void AddGold(int amount)
        {
            if (amount <= 0)
                return;

            Gold += amount;
            OnGoldChanged?.Invoke(Gold);
        }

        /// <summary>
        /// 지정한 자원 타입의 현재 보유량을 반환한다.
        /// </summary>
        public int GetResourceAmount(ResourceType type)
        {
            return _resources.TryGetValue(type, out int v) ? v : 0;
        }

        /// <summary>
        /// 플레이어에게 자원을 추가한다. 채집 시 호출된다.
        /// </summary>
        public void AddResource(ResourceType type, int amount)
        {
            if (amount <= 0)
                return;

            int prev = GetResourceAmount(type);
            int next = prev + amount;
            _resources[type] = next;
            OnResourceChanged?.Invoke(type, next, amount);
        }

        /// <summary>
        /// 지정한 자원이 지정한 양 이상인지 확인한다.
        /// </summary>
        public bool HasResource(ResourceType type, int amount)
        {
            return GetResourceAmount(type) >= amount;
        }

        /// <summary>
        /// 자원을 소비한다. 성공 시 true.
        /// </summary>
        public bool SpendResource(ResourceType type, int amount)
        {
            if (amount <= 0)
                return false;

            int prev = GetResourceAmount(type);
            if (prev < amount)
                return false;

            int next = prev - amount;
            _resources[type] = next;
            OnResourceChanged?.Invoke(type, next, -amount);
            return true;
        }
    }
}
