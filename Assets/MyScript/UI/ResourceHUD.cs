using System.Collections.Generic;
using UnityEngine;
using WoodLand3D.Core.Resources;
using WoodLand3D.Gameplay;
using TMPro;

namespace WoodLand3D.UI
{
    /// <summary>
    /// 자원 보유량 카운터와 최근 획득 목록을 표시하는 HUD.
    /// PlayerInventory와 ResourceGained 이벤트를 구독하며, Inspector에서 인벤토리·TMP 필드를 할당한다.
    /// </summary>
    public class ResourceHUD : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField, Tooltip("플레이어 인벤토리(비어 있으면 씬에서 자동 탐색)")]
        private PlayerInventory inventory;
        [SerializeField, Tooltip("카운터 라벨 부모(선택). 비어 있어도 동작함")]
        private Transform counterRoot;
        [SerializeField, Tooltip("나무 보유량 표시 텍스트")]
        private TMP_Text treeCounterText;
        [SerializeField, Tooltip("돌 보유량 표시 텍스트")]
        private TMP_Text rockCounterText;
        [SerializeField, Tooltip("광석 보유량 표시 텍스트")]
        private TMP_Text oreCounterText;

        [Header("최근 획득")]
        [SerializeField, Tooltip("표시할 최대 개수. 최신이 위에 오도록 표시")]
        private int maxRecentEntries = 4;
        [SerializeField, Tooltip("최근 획득 항목을 표시할 TMP 텍스트 배열")]
        private TMP_Text[] recentGainTexts = new TMP_Text[0];

        private readonly Queue<string> _recentGains = new Queue<string>();

        private void OnEnable()
        {
            if (inventory == null)
                inventory = UnityEngine.Object.FindFirstObjectByType<PlayerInventory>();

            if (inventory != null)
            {
                inventory.OnResourceChanged += OnResourceChanged;
                RefreshAllCounters();
            }
            else
                Debug.LogWarning("ResourceHUD: PlayerInventory를 찾을 수 없습니다. 카운터가 갱신되지 않습니다.");

            ResourceGainedEvents.OnResourceGained += OnResourceGained;
        }

        private void OnDisable()
        {
            if (inventory != null)
                inventory.OnResourceChanged -= OnResourceChanged;
            ResourceGainedEvents.OnResourceGained -= OnResourceGained;
        }

        private void OnResourceChanged(ResourceType type, int newTotal, int delta)
        {
            RefreshCounter(type, newTotal);
        }

        private void OnResourceGained(ResourceType type, int amount, Vector3 worldPosition, Transform playerTransform)
        {
            PushRecentGain(type, amount);
        }

        /// <summary>
        /// 지정한 자원 타입의 카운터 텍스트를 갱신한다.
        /// </summary>
        public void RefreshCounter(ResourceType type, int total)
        {
            TMP_Text text = GetCounterTextFor(type);
            if (text != null)
                text.text = total.ToString();
        }

        /// <summary>
        /// 모든 자원 카운터를 인벤토리 기준으로 한 번에 갱신한다.
        /// </summary>
        public void RefreshAllCounters()
        {
            if (inventory == null)
                return;
            RefreshCounter(ResourceType.Tree, inventory.GetResourceAmount(ResourceType.Tree));
            RefreshCounter(ResourceType.Rock, inventory.GetResourceAmount(ResourceType.Rock));
            RefreshCounter(ResourceType.Ore, inventory.GetResourceAmount(ResourceType.Ore));
        }

        /// <summary>
        /// 최근 획득 목록에 항목을 추가하고 표시를 갱신한다.
        /// </summary>
        public void PushRecentGain(ResourceType type, int amount)
        {
            string entry = $"+{amount} {type}";
            _recentGains.Enqueue(entry);
            while (_recentGains.Count > maxRecentEntries)
                _recentGains.Dequeue();
            RefreshRecentGainsDisplay();
        }

        private void RefreshRecentGainsDisplay()
        {
            var list = new List<string>(_recentGains);
            list.Reverse();
            for (int i = 0; i < recentGainTexts.Length; i++)
            {
                if (recentGainTexts[i] != null)
                    recentGainTexts[i].text = i < list.Count ? list[i] : "";
            }
        }

        private TMP_Text GetCounterTextFor(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Tree: return treeCounterText;
                case ResourceType.Rock: return rockCounterText;
                case ResourceType.Ore: return oreCounterText;
                default: return null;
            }
        }
    }
}
