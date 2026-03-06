using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WoodLand3D.Tiles.Runtime;
using WoodLand3D.Gameplay.Inventory;
using WoodLand3D.Resources.Types;
using WoodLand3D.Resources.Nodes;
using WoodLand3D.Core.Events;

namespace WoodLand3D.Resources.Spawn
{
    /// <summary>
    /// 한 타일에 대한 자원 스폰·고갈·리스폰·즉시 보상을 담당한다.
    /// </summary>
    public class TileResourceSpawner : MonoBehaviour
    {
        [Serializable]
        public class ResourcePrefabEntry
        {
            public ResourceType type;
            public GameObject prefab;
        }

        [Header("참조")]
        [SerializeField, Tooltip("소속 타일 컨트롤러(비어 있으면 같은 오브젝트에서 찾음)")]
        private TileController tileController;
        [SerializeField, Tooltip("스폰된 자원의 부모 Transform")]
        private Transform spawnRoot;

        [Header("프리팹")]
        [SerializeField] private List<ResourcePrefabEntry> prefabs = new List<ResourcePrefabEntry>();

        [Header("스폰 설정")]
        [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
        [SerializeField] private int defaultCountTrees = 3;
        [SerializeField] private int defaultCountRocks = 2;
        [SerializeField] private Vector2 randomOffsetRange = new Vector2(4f, 4f);
        [SerializeField] private int defaultHp = 3;
        [SerializeField] private float defaultRespawnTime = 30f;

        [Header("보상(고갈 시 즉시 지급)")]
        [SerializeField] private int rewardTree = 3;
        [SerializeField] private int rewardRock = 2;
        [SerializeField] private int rewardOre = 1;

        private readonly Dictionary<ResourceType, GameObject> _prefabLookup = new Dictionary<ResourceType, GameObject>();
        private bool _initialSpawnDone;

        private void Awake()
        {
            if (tileController == null) tileController = GetComponent<TileController>();
            if (spawnRoot == null) spawnRoot = transform;
            BuildPrefabLookup();
        }

        public void OnTileUnlocked()
        {
            if (_initialSpawnDone) return;
            _initialSpawnDone = true;
            for (int i = 0; i < defaultCountTrees; i++) Spawn(ResourceType.Tree, GetSpawnPosition(ResourceType.Tree, i));
            for (int i = 0; i < defaultCountRocks; i++) Spawn(ResourceType.Rock, GetSpawnPosition(ResourceType.Rock, i));
        }

        private Vector3 GetSpawnPosition(ResourceType type, int index)
        {
            var typedPoints = new List<Transform>();
            foreach (var t in spawnPoints)
            {
                if (t == null) continue;
                var sp = t.GetComponent<ResourceSpawnPoint>();
                if (sp != null && sp.Type == type) typedPoints.Add(sp.SpawnTransform);
            }
            if (typedPoints.Count > 0)
            {
                var chosen = typedPoints[index % typedPoints.Count];
                if (chosen != null) return chosen.position;
            }
            if (spawnPoints.Count > 0)
            {
                var any = spawnPoints[index % spawnPoints.Count];
                if (any != null) return any.position;
            }
            Vector3 center = spawnRoot != null ? spawnRoot.position : transform.position;
            float offsetX = UnityEngine.Random.Range(-randomOffsetRange.x, randomOffsetRange.x);
            float offsetZ = UnityEngine.Random.Range(-randomOffsetRange.y, randomOffsetRange.y);
            return new Vector3(center.x + offsetX, center.y, center.z + offsetZ);
        }

        public void Spawn(ResourceType type, Vector3 position)
        {
            if (tileController != null && !tileController.IsUnlocked) return;
            if (!_prefabLookup.TryGetValue(type, out var prefab) || prefab == null)
            {
                Debug.LogWarning($"TileResourceSpawner: 타입 {type}에 대한 프리팹이 없습니다. ({name})");
                return;
            }
            GameObject instance = Instantiate(prefab, position, Quaternion.identity, spawnRoot);
            var node = instance.GetComponent<ResourceNode>();
            if (node == null)
            {
                try { node = instance.AddComponent<ResourceNode>(); }
                catch (Exception e)
                {
                    Debug.LogWarning($"TileResourceSpawner: ResourceNode 추가 실패 '{instance.name}'. {e.Message}");
                    return;
                }
            }
            if (node != null) node.Initialize(type, defaultHp, defaultRespawnTime, HandleDepleted);
        }

        private void HandleDepleted(ResourceNode node)
        {
            if (node == null) return;
            int amount = GetRewardAmount(node.Type);
            if (amount > 0)
            {
                var inventory = UnityEngine.Object.FindFirstObjectByType<PlayerInventory>();
                if (inventory != null)
                {
                    inventory.AddResource(node.Type, amount);
                    ResourceGainedEvents.Raise(node.Type, amount, node.transform.position, inventory.transform);
                }
            }
            var type = node.Type;
            var pos = node.transform.position;
            float delay = node.RespawnTime;
            StartCoroutine(RespawnRoutine(type, pos, delay));
        }

        private int GetRewardAmount(ResourceType type)
        {
            switch (type) { case ResourceType.Tree: return rewardTree; case ResourceType.Rock: return rewardRock; case ResourceType.Ore: return rewardOre; default: return 1; }
        }

        private IEnumerator RespawnRoutine(ResourceType type, Vector3 position, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (tileController != null && !tileController.IsUnlocked) yield break;
            Spawn(type, position);
        }

        private void BuildPrefabLookup()
        {
            _prefabLookup.Clear();
            foreach (var entry in prefabs)
            {
                if (entry == null || entry.prefab == null) continue;
                if (_prefabLookup.ContainsKey(entry.type)) continue;
                _prefabLookup[entry.type] = entry.prefab;
            }
        }
    }
}
