using System;
using UnityEngine;

namespace WoodLand3D.Core.Resources
{
    /// <summary>
    /// 단일 자원 인스턴스(나무, 돌, 광석)를 나타낸다.
    /// HP와 고갈 처리를 담당하며, 리스폰은 TileResourceSpawner가 담당한다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ResourceNode : MonoBehaviour
    {
        [Header("자원 설정")]
        [SerializeField, Tooltip("이 노드의 자원 타입")]
        private ResourceType type = ResourceType.Tree;
        [SerializeField, Tooltip("최대 HP (채집 횟수)")]
        private int maxHp = 3;
        [SerializeField, Tooltip("고갈 후 리스폰까지 대기 시간(초)")]
        private float respawnTime = 30f;

        public ResourceType Type => type;
        public int MaxHp => maxHp;
        public int CurrentHp { get; private set; }
        public float RespawnTime => respawnTime;
        public bool IsDepleted { get; private set; }

        private Action<ResourceNode> _onDepleted;

        /// <summary>
        /// 노드를 지정한 스탯과 고갈 콜백으로 초기화한다. 스포너가 스폰 시 호출한다.
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

        /// <summary>
        /// 채집으로 데미지를 적용한다. HP가 0 이하가 되면 고갈 처리된다.
        /// </summary>
        public void ApplyDamage(int amount)
        {
            if (IsDepleted || amount <= 0)
                return;

            CurrentHp -= amount;
            if (CurrentHp <= 0)
                Deplete();
        }

        private void Deplete()
        {
            if (IsDepleted)
                return;

            IsDepleted = true;
            _onDepleted?.Invoke(this);

            var col = GetComponent<Collider>();
            if (col != null)
                col.enabled = false;
            gameObject.SetActive(false);
        }
    }
}
