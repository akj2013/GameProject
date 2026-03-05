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
    /// Handles tile unlock rules, costs, UI and save/load.
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
            public Vector2IntSerializable(Vector2Int v)
            {
                x = v.x;
                y = v.y;
            }

            public Vector2Int ToVector2Int() => new Vector2Int(x, y);
        }

        [Serializable]
        private class TileUnlockSaveData
        {
            public List<Vector2IntSerializable> positions = new List<Vector2IntSerializable>();
        }

        public static TileUnlockSystem Instance { get; private set; }

        [Header("References")]
        [SerializeField] private SquareGridManager grid;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private UnlockPanelUI ui;
        [SerializeField] private CameraFollow cameraFollow;

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
        /// Called by SquareGridManager after it has built the grid.
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
                inventory = FindObjectOfType<PlayerInventory>();
            if (ui == null)
                ui = FindObjectOfType<UnlockPanelUI>(includeInactive: true);
        }

        private void EnsureCamera()
        {
            if (cameraFollow == null)
                cameraFollow = FindObjectOfType<CameraFollow>();
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
                            {
                                _unlockedPositions.Add(p.ToVector2Int());
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"TileUnlockSystem: Failed to parse save data: {e.Message}");
                    }
                }
            }

            // Ensure start tile is always unlocked.
            _unlockedPositions.Add(grid.StartUnlockedPos);

            // Apply to tiles.
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
                positions = _unlockedPositions
                    .Select(p => new Vector2IntSerializable(p))
                    .ToList()
            };

            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
        }

        // Called by TileController when player enters tile trigger.
        public void HandleTileTriggerEnter(TileController tile)
        {
            ShowUnlockUI(tile);
        }

        // Called by TileController when player exits tile trigger.
        public void HandleTileTriggerExit(TileController tile)
        {
            if (_currentTileForUI == tile)
            {
                HideUI();
            }
        }

        public void ShowUnlockUI(TileController tile)
        {
            EnsureInventoryAndUi();
            EnsureCamera();

            if (grid == null)
            {
                Debug.LogWarning("TileUnlockSystem: Grid reference is missing.");
                return;
            }

            // Close any previous UI before opening a new one.
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
            {
                message = "Cannot unlock: need at least one unlocked neighbor.";
            }
            else
            {
                if (inventory != null && inventory.HasGold(cost))
                {
                    message = $"Cost: {cost} Gold";
                    canUnlock = true;
                }
                else
                {
                    message = $"Need {cost} Gold (not enough).";
                }
            }

            if (ui != null)
            {
                ui.Show(tile, cost, canUnlock, message, () => TryUnlock(tile, cost));
            }
            else
            {
                Debug.LogWarning("TileUnlockSystem: UnlockPanelUI reference is missing.");
            }

            // Optional camera focus toward this tile.
            if (cameraFollow != null)
            {
                cameraFollow.FocusOnTile(tile.transform.position);
            }
        }

        public void HideUI()
        {
            _currentTileForUI = null;
            if (ui != null)
            {
                ui.Hide();
            }
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
                Debug.LogWarning("TileUnlockSystem: Grid reference is missing in TryUnlock.");
                return;
            }
            if (inventory == null)
            {
                Debug.LogWarning("TileUnlockSystem: Inventory reference is missing in TryUnlock.");
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

