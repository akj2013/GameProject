using System.Collections.Generic;
using UnityEngine;

namespace WoodLand3D.Tiles
{
    /// <summary>
    /// 사각형 타일 그리드를 생성하고 관리한다.
    /// 타일 프리팹을 배치하며, 그리드 좌표·이웃 조회·월드 좌표 변환을 제공한다.
    /// </summary>
    public class SquareGridManager : MonoBehaviour
    {
        [Header("그리드 설정")]
        [SerializeField, Tooltip("타일로 사용할 프리팹")]
        private TileController tilePrefab;
        [SerializeField, Tooltip("그리드 가로 칸 수")]
        private int width = 10;
        [SerializeField, Tooltip("그리드 세로 칸 수")]
        private int height = 10;
        [SerializeField, Tooltip("한 타일의 월드 크기")]
        private float tileSize = 10f;
        [SerializeField, Tooltip("타일이 생성될 부모 Transform (비어 있으면 이 오브젝트)")]
        private Transform tilesRoot;
        [SerializeField, Tooltip("처음 해제된 타일의 그리드 좌표")]
        private Vector2Int startUnlockedPos = new Vector2Int(5, 5);

        private readonly Dictionary<Vector2Int, TileController> _tiles = new Dictionary<Vector2Int, TileController>();

        public int Width => width;
        public int Height => height;
        public float TileSize => tileSize;
        public Vector2Int StartUnlockedPos => startUnlockedPos;

        private void Start()
        {
            BuildGrid();
            if (TileUnlockSystem.Instance != null)
                TileUnlockSystem.Instance.OnGridReady(this);
        }

        private void BuildGrid()
        {
            if (tilePrefab == null)
            {
                Debug.LogError("SquareGridManager: tilePrefab이 할당되지 않았습니다.");
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

        /// <summary>
        /// 그리드 좌표를 월드 좌표로 변환한다.
        /// </summary>
        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            float worldX = (gridPos.x - (width - 1) / 2f) * tileSize;
            float worldZ = (gridPos.y - (height - 1) / 2f) * tileSize;
            return new Vector3(worldX, 0f, worldZ);
        }

        /// <summary>
        /// 지정한 그리드 좌표의 타일을 가져온다.
        /// </summary>
        public bool TryGetTile(Vector2Int pos, out TileController tile)
        {
            return _tiles.TryGetValue(pos, out tile);
        }

        /// <summary>
        /// 그리드 범위 안인지 확인한다.
        /// </summary>
        public bool IsInBounds(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
        }

        /// <summary>
        /// 상하좌우 이웃 타일을 순서대로 반환한다.
        /// </summary>
        public IEnumerable<TileController> GetNeighbors4(Vector2Int pos)
        {
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
                    yield return neighbor;
            }
        }
    }
}
