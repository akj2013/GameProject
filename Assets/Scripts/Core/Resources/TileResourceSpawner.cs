using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WoodLand3D.Tiles;

namespace WoodLand3D.Core.Resources
{
    /// <summary>
    /// Spawns and manages resources for a single tile.
    /// </summary>
    public class TileResourceSpawner : MonoBehaviour
    {
        [Serializable]
        public class ResourcePrefabEntry
        {
            public ResourceType type;
            public GameObject prefab;
        }

        [Header("References")]
        [SerializeField] private TileController tileController;
        [SerializeField] private Transform spawnRoot;

        [Header("Prefabs")]
        [SerializeField] private List<ResourcePrefabEntry> prefabs = new List<ResourcePrefabEntry>();

        [Header("Spawn Settings")]
        [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
        [SerializeField] private int defaultCountTrees = 3;
        [SerializeField] private int defaultCountRocks = 2;
        [SerializeField] private Vector2 randomOffsetRange = new Vector2(4f, 4f);
        [SerializeField] private int defaultHp = 3;
        [SerializeField] private float defaultRespawnTime = 30f;

        private readonly Dictionary<ResourceType, GameObject> _prefabLookup = new Dictionary<ResourceType, GameObject>();
        private bool _initialSpawnDone;

        private void Awake()
        {
            if (tileController == null)
                tileController = GetComponent<TileController>();

            if (spawnRoot == null)
                spawnRoot = transform;

            BuildPrefabLookup();
        }

        private void BuildPrefabLookup()
        {
            _prefabLookup.Clear();
            foreach (var entry in prefabs)
            {
                if (entry == null || entry.prefab == null)
                    continue;

                if (_prefabLookup.ContainsKey(entry.type))
                    continue;

                _prefabLookup[entry.type] = entry.prefab;
            }
        }

        /// <summary>
        /// Called by TileController when the tile becomes unlocked for the first time.
        /// </summary>
        public void OnTileUnlocked()
        {
            if (_initialSpawnDone)
                return;

            _initialSpawnDone = true;

            // Basic initial distribution: a few trees and rocks.
            for (int i = 0; i < defaultCountTrees; i++)
            {
                Vector3 pos = GetSpawnPosition(ResourceType.Tree, i);
                Spawn(ResourceType.Tree, pos);
            }

            for (int i = 0; i < defaultCountRocks; i++)
            {
                Vector3 pos = GetSpawnPosition(ResourceType.Rock, i);
                Spawn(ResourceType.Rock, pos);
            }
        }

        private Vector3 GetSpawnPosition(ResourceType type, int index)
        {
            // Prefer explicit spawn points that match the type if provided.
            foreach (var t in spawnPoints)
            {
                if (t == null)
                    continue;

                var sp = t.GetComponent<ResourceSpawnPoint>();
                if (sp != null && sp.Type == type)
                    return sp.SpawnTransform.position;
            }

            // Otherwise, just pick any spawn point by index.
            if (spawnPoints.Count > 0)
            {
                var any = spawnPoints[index % spawnPoints.Count];
                if (any != null)
                    return any.position;
            }

            // Fallback: random position inside the tile area around this spawner.
            Vector3 center = spawnRoot != null ? spawnRoot.position : transform.position;
            float offsetX = UnityEngine.Random.Range(-randomOffsetRange.x, randomOffsetRange.x);
            float offsetZ = UnityEngine.Random.Range(-randomOffsetRange.y, randomOffsetRange.y);
            return new Vector3(center.x + offsetX, center.y, center.z + offsetZ);
        }

        public void Spawn(ResourceType type, Vector3 position)
        {
            if (tileController != null && !tileController.IsUnlocked)
                return;

            if (!_prefabLookup.TryGetValue(type, out var prefab) || prefab == null)
            {
                Debug.LogWarning($"TileResourceSpawner: No prefab assigned for resource type {type} on tile {name}.");
                return;
            }

            GameObject instance = Instantiate(prefab, position, Quaternion.identity, spawnRoot);
            var node = instance.GetComponent<ResourceNode>();
            if (node == null)
            {
                try
                {
                    node = instance.AddComponent<ResourceNode>();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"TileResourceSpawner: Failed to add ResourceNode to '{instance.name}'. " +
                                     $"Make sure the root has a Collider. Exception: {e.Message}");
                    return;
                }
            }

            if (node != null)
            {
                node.Initialize(type, defaultHp, defaultRespawnTime, HandleDepleted);
            }
        }

        private void HandleDepleted(ResourceNode node)
        {
            if (node == null)
                return;

            // Capture data needed for respawn.
            var type = node.Type;
            var pos = node.transform.position;
            float delay = node.RespawnTime;

            StartCoroutine(RespawnRoutine(type, pos, delay));
        }

        private IEnumerator RespawnRoutine(ResourceType type, Vector3 position, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (tileController != null && !tileController.IsUnlocked)
                yield break;

            Spawn(type, position);
        }
    }
}

