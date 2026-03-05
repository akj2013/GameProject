using UnityEngine;

namespace WoodLand3D.Core.Resources
{
    /// <summary>
    /// Optional helper component to mark specific spawn positions on a tile.
    /// </summary>
    public class ResourceSpawnPoint : MonoBehaviour
    {
        [SerializeField] private ResourceType type = ResourceType.Tree;
        [SerializeField] private Transform spawnTransform;

        public ResourceType Type => type;
        public Transform SpawnTransform => spawnTransform != null ? spawnTransform : transform;
    }
}

