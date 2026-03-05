using System.Collections.Generic;
using UnityEngine;

namespace WoodLand3D.Tiles
{
    /// <summary>
    /// Spawns and manages a square grid of tiles.
    /// </summary>
    public class SquareGridManager : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private TileController tilePrefab;
        [SerializeField] private int width = 10;
        [SerializeField] private int height = 10;
        [SerializeField] private float tileSize = 10f;
        [SerializeField] private Transform tilesRoot;
        [SerializeField] private Vector2Int startUnlockedPos = new Vector2Int(5, 5);

        private readonly Dictionary<Vector2Int, TileController> _tiles = new Dictionary<Vector2Int, TileController>();

        public int Width => width;
        public int Height => height;
        public float TileSize => tileSize;
        public Vector2Int StartUnlockedPos => startUnlockedPos;

        private void Start()
        {
            BuildGrid();

            // Notify unlock system that grid is ready.
            if (TileUnlockSystem.Instance != null)
            {
                TileUnlockSystem.Instance.OnGridReady(this);
            }
        }

        private void BuildGrid()
        {
            if (tilePrefab == null)
            {
                Debug.LogError("SquareGridManager: tilePrefab is not assigned.");
                return;
            }

            Transform parent = tilesRoot != null ? tilesRoot : transform;
            _tiles.Clear();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var gridPos = new Vector2Int(x, y);
                    Vector3 worldPos = GridToWorld(gridPos);

                    TileController tile = Instantiate(tilePrefab, worldPos, Quaternion.identity, parent);
                    tile.name = $"Tile_{x}_{y}";

                    bool initiallyUnlocked = gridPos == startUnlockedPos;
                    tile.Initialize(gridPos, initiallyUnlocked);

                    _tiles[gridPos] = tile;
                }
            }
        }

        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            float worldX = (gridPos.x - (width - 1) / 2f) * tileSize;
            float worldZ = (gridPos.y - (height - 1) / 2f) * tileSize;
            return new Vector3(worldX, 0f, worldZ);
        }

        public bool TryGetTile(Vector2Int pos, out TileController tile)
        {
            return _tiles.TryGetValue(pos, out tile);
        }

        public bool IsInBounds(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
        }

        public IEnumerable<TileController> GetNeighbors4(Vector2Int pos)
        {
            // Up, Down, Left, Right
            Vector2Int[] offsets =
            {
                new Vector2Int(0, 1),
                new Vector2Int(0, -1),
                new Vector2Int(-1, 0),
                new Vector2Int(1, 0)
            };

            foreach (var offset in offsets)
            {
                Vector2Int neighborPos = pos + offset;
                if (IsInBounds(neighborPos) && _tiles.TryGetValue(neighborPos, out var neighbor))
                {
                    yield return neighbor;
                }
            }
        }
    }
}

