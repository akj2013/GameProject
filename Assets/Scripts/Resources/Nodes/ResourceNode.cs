using System;
using UnityEngine;
using WoodLand3D.Resources.Types;

namespace WoodLand3D.Resources.Nodes
{
    /// <summary>
    /// 자원 오브젝트(나무/돌 등)의 채집 가능 상태를 관리한다.
    /// 내구도 감소, 고갈 시 보상 지급 또는 콜백(스포너 연동) 처리.
    /// </summary>
    public class ResourceNode : MonoBehaviour
    {
        [Header("자원 설정")]
        [SerializeField, Tooltip("자원 종류 (툴 매핑에 사용)")]
        private ResourceType type = ResourceType.Tree;

        [SerializeField, Tooltip("최대 내구도 (0 이하가 되면 고갈)")]
        private int maxHp = 3;

        [SerializeField, Tooltip("고갈 시 지급할 수량 (콜백 미사용 시)")]
        private int rewardAmount = 1;

        private int _currentHp;
        private bool _depleted;
        private float _respawnTime;
        private Action<ResourceNode> _onDepleted;

        /// <summary>현재 자원 종류.</summary>
        public ResourceType Type => type;

        /// <summary>아직 채집 가능한지.</summary>
        public bool IsValid => !_depleted && _currentHp > 0;

        /// <summary>스포너에서 설정한 리스폰 대기 시간(초).</summary>
        public float RespawnTime => _respawnTime;

        private void Awake()
        {
            if (_onDepleted == null)
                _currentHp = maxHp;
        }

        /// <summary>
        /// 스포너가 인스턴스 스폰 후 호출. 타입·내구도·리스폰 시간·고갈 콜백 설정.
        /// </summary>
        public void Initialize(ResourceType t, int hp, float respawnTime, Action<ResourceNode> onDepleted)
        {
            type = t;
            maxHp = hp;
            _currentHp = hp;
            _respawnTime = respawnTime;
            _onDepleted = onDepleted;
            _depleted = false;
        }

        /// <summary>
        /// 해당 수만큼 피해를 적용한다. 고갈 시 콜백 또는 보상·이벤트 후 비활성화.
        /// </summary>
        public void ApplyDamage(int amount)
        {
            if (_depleted || amount <= 0) return;

            _currentHp = Mathf.Max(0, _currentHp - amount);
            if (_currentHp > 0) return;

            _depleted = true;
            if (_onDepleted != null)
            {
                _onDepleted(this);
                gameObject.SetActive(false);
            }
            else
            {
                DeliverReward();
            }
        }

        private void DeliverReward()
        {
            var inventory = UnityEngine.Object.FindFirstObjectByType<Gameplay.Inventory.PlayerInventory>();
            if (inventory != null)
                inventory.AddResource(type, rewardAmount);

            WoodLand3D.Core.Events.ResourceGainedEvents.Raise(type, rewardAmount, transform.position, inventory != null ? inventory.transform : null);

            gameObject.SetActive(false);
        }
    }
}
