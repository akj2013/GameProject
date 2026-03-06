using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WoodLand3D.Gameplay;
using WoodLand3D.UI;
using WoodLand3D.CameraSystems;

namespace WoodLand3D.Tiles
{
    /// <summary>
    /// 타일 언락 규칙, 비용 계산, 언락 UI 표시, 저장/로드를 담당한다.
    /// 그리드 준비 후 해제된 타일 목록을 PlayerPrefs에 저장하고, 플레이어가 타일 트리거에 들어오면 언락 패널을 띄운다.
    /// </summary>
    public class TileUnlockSystem : MonoBehaviour
    {
        private const string SaveKey = "TileUnlockData";

        [Serializable]
        private class Vector2IntSerializable
        {
            public int x;
            public int y;
            public Vector2IntSerializable() { }
            public Vector2IntSerializable(Vector2Int v) { x = v.x; y = v.y; }
            public Vector2Int ToVector2Int() => new Vector2Int(x, y);
        }

        [Serializable]
        private class TileUnlockSaveData
        {
            public List<Vector2IntSerializable> positions = new List<Vector2IntSerializable>();
        }

        public static TileUnlockSystem Instance { get; private set; }

        [Header("참조")]
        [SerializeField, Tooltip("타일 그리드 매니저")]
        private SquareGridManager grid;
        [SerializeField, Tooltip("플레이어 인벤토리(골드 소비용)")]
        private PlayerInventory inventory;
        [SerializeField, Tooltip("언락 패널 UI")]
        private UnlockPanelUI ui;
        [SerializeField, Tooltip("카메라 포커스용")]
        private CameraFollow cameraFollow;

        private readonly HashSet<Vector2Int> _unlockedPositions = new HashSet<Vector2Int>();
        private bool _loaded;
        private TileController _currentTileForUI;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// 그리드가 구축된 뒤 호출된다. 저장 데이터 로드 후 타일에 적용한다.
        /// </summary>
        public void OnGridReady(SquareGridManager gridManager)
        {
            grid = gridManager;
            EnsureInventoryAndUi();
            EnsureCamera();
            LoadStateAndApply();
        }

        private void EnsureInventoryAndUi()
        {
            if (inventory == null)
                inventory = UnityEngine.Object.FindFirstObjectByType<PlayerInventory>();
            if (ui == null)
                ui = UnityEngine.Object.FindFirstObjectByType<UnlockPanelUI>(FindObjectsInactive.Include);
        }

        private void EnsureCamera()
        {
            if (cameraFollow == null)
                cameraFollow = UnityEngine.Object.FindFirstObjectByType<CameraFollow>();
        }

        private void LoadStateAndApply()
        {
            if (_loaded || grid == null)
                return;

            _loaded = true;
            _unlockedPositions.Clear();

            if (PlayerPrefs.HasKey(SaveKey))
            {
                string json = PlayerPrefs.GetString(SaveKey, string.Empty);
                if (!string.IsNullOrEmpty(json))
                {
                    try
                    {
                        var data = JsonUtility.FromJson<TileUnlockSaveData>(json);
                        if (data != null && data.positions != null)
                        {
                            foreach (var p in data.positions)
                                _unlockedPositions.Add(p.ToVector2Int());
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"TileUnlockSystem: 저장 데이터 파싱 실패: {e.Message}");
                    }
                }
            }

            _unlockedPositions.Add(grid.StartUnlockedPos);

            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    var pos = new Vector2Int(x, y);
                    if (grid.TryGetTile(pos, out var tile))
                    {
                        bool unlocked = _unlockedPositions.Contains(pos);
                        tile.SetUnlocked(unlocked, playFx: false);
                    }
                }
            }
        }

        private void SaveState()
        {
            var data = new TileUnlockSaveData
            {
                positions = _unlockedPositions.Select(p => new Vector2IntSerializable(p)).ToList()
            };
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 플레이어가 타일 트리거에 들어왔을 때 호출된다. 언락 UI를 표시한다.
        /// </summary>
        public void HandleTileTriggerEnter(TileController tile)
        {
            ShowUnlockUI(tile);
        }

        /// <summary>
        /// 플레이어가 타일 트리거에서 나갔을 때 호출된다. 해당 타일용 UI면 숨긴다.
        /// </summary>
        public void HandleTileTriggerExit(TileController tile)
        {
            if (_currentTileForUI == tile)
                HideUI();
        }

        /// <summary>
        /// 지정한 타일에 대한 언락 패널을 띄운다. 비용·가능 여부를 계산해 UI에 전달한다.
        /// </summary>
        public void ShowUnlockUI(TileController tile)
        {
            EnsureInventoryAndUi();
            EnsureCamera();

            if (grid == null)
            {
                Debug.LogWarning("TileUnlockSystem: 그리드 참조가 없습니다.");
                return;
            }

            HideUI();
            _currentTileForUI = tile;

            if (tile.IsUnlocked)
            {
                HideUI();
                return;
            }

            int cost = ComputeUnlockCost(tile.GridPos);
            bool hasAdjacent = HasUnlockedNeighbor(tile.GridPos);
            bool canUnlockByRule = hasAdjacent || tile.GridPos == grid.StartUnlockedPos;

            string message;
            bool canUnlock = false;

            if (!canUnlockByRule)
                message = "Cannot unlock: need at least one unlocked neighbor.";
            else
            {
                if (inventory != null && inventory.HasGold(cost))
                {
                    message = $"Cost: {cost} Gold";
                    canUnlock = true;
                }
                else
                    message = $"Need {cost} Gold (not enough).";
            }

            if (ui != null)
                ui.Show(tile, cost, canUnlock, message, () => TryUnlock(tile, cost));
            else
                Debug.LogWarning("TileUnlockSystem: UnlockPanelUI 참조가 없습니다.");

            if (cameraFollow != null)
                cameraFollow.FocusOnTile(tile.transform.position);
        }

        /// <summary>
        /// 언락 패널을 숨기고 카메라 포커스를 해제한다.
        /// </summary>
        public void HideUI()
        {
            _currentTileForUI = null;
            if (ui != null)
                ui.Hide();
            if (cameraFollow != null)
                cameraFollow.ClearFocus();
        }

        private int ComputeUnlockCost(Vector2Int pos)
        {
            if (grid == null)
                return 0;
            Vector2Int start = grid.StartUnlockedPos;
            int distance = Mathf.Abs(pos.x - start.x) + Mathf.Abs(pos.y - start.y);
            return 50 + distance * 10;
        }

        private bool HasUnlockedNeighbor(Vector2Int pos)
        {
            if (grid == null)
                return false;
            foreach (var neighbor in grid.GetNeighbors4(pos))
            {
                if (neighbor.IsUnlocked)
                    return true;
            }
            return false;
        }

        private void TryUnlock(TileController tile, int cost)
        {
            if (tile == null)
                return;
            if (grid == null)
            {
                Debug.LogWarning("TileUnlockSystem: TryUnlock에서 그리드 참조가 없습니다.");
                return;
            }
            if (inventory == null)
            {
                Debug.LogWarning("TileUnlockSystem: TryUnlock에서 인벤토리 참조가 없습니다.");
                return;
            }

            if (tile.IsUnlocked)
                return;

            bool canByRule = HasUnlockedNeighbor(tile.GridPos) || tile.GridPos == grid.StartUnlockedPos;
            if (!canByRule)
            {
                ShowUnlockUI(tile);
                return;
            }
            if (!inventory.HasGold(cost))
            {
                ShowUnlockUI(tile);
                return;
            }

            inventory.SpendGold(cost);
            tile.SetUnlocked(true, playFx: true);
            _unlockedPositions.Add(tile.GridPos);
            SaveState();
            HideUI();
        }
    }
}
