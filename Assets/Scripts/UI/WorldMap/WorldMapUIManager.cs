using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WoodLand3D.WorldMap
{
    public class WorldMapUIManager : MonoBehaviour
    {
        [Header("맵 설정")]
        [SerializeField] private int gridWidth = 5;
        [SerializeField] private int gridHeight = 5;
        [SerializeField] private GameObject tileCellPrefab;
        [SerializeField] private RectTransform gridRoot;

        [Header("아이소메트릭 배치 (다이아몬드 격자)")]
        [Tooltip("한 칸 이동 시 X 변화량. x = (col - row) * xStep")]
        [SerializeField] private float xStep = 40f;
        [Tooltip("한 칸 이동 시 Y 변화량(화면 아래가 음수). y = -(col + row) * yStep")]
        [SerializeField] private float yStep = 24f;
        [Tooltip("true면 그리드 중심이 (0,0). false면 gridOrigin 사용")]
        [SerializeField] private bool autoCenterGrid = true;
        [Tooltip("autoCenterGrid가 false일 때 (0,0) 셀의 위치")]
        [SerializeField] private Vector2 gridOrigin = Vector2.zero;

        [Header("UI 참조")]
        [SerializeField] private TileInfoPanelUI tileInfoPanel;
        [SerializeField] private RectTransform effectLayer;
        [SerializeField] private TMP_FontAsset gainTextFont;

        [Header("타일 스프라이트 (여기에 넣으면 모든 타일에 적용)")]
        [SerializeField] private Sprite spriteEmpty;
        [SerializeField] private Sprite spriteForest;
        [SerializeField] private Sprite spriteHouse;
        [SerializeField] private Sprite spriteMountain;
        [SerializeField] private Sprite spriteLocked;

        private readonly List<TileCellUI> _cells = new List<TileCellUI>();
        private TileCellUI _selectedCell;

        private void Start()
        {
            if (gridRoot == null) gridRoot = GetComponent<RectTransform>();
            BuildMap();
        }

        private void BuildMap()
        {
            if (tileCellPrefab == null || gridRoot == null) return;

            for (int i = gridRoot.childCount - 1; i >= 0; i--)
                Destroy(gridRoot.GetChild(i).gameObject);
            _cells.Clear();

            TileData[] sampleData = GetSampleMapData();
            int count = gridWidth * gridHeight;

            for (int i = 0; i < count; i++)
            {
                GameObject go = Instantiate(tileCellPrefab, gridRoot);
                TileCellUI cell = go.GetComponent<TileCellUI>();
                if (cell != null)
                {
                    cell.SetSpriteSource(this);
                    TileData data = i < sampleData.Length ? sampleData[i] : TileData.Create(TileType.Empty, false, 1, "Empty", 0);
                    cell.Setup(i, data);
                    cell.SetSelected(false);
                    _cells.Add(cell);
                }
            }

            ApplyIsoLayout();
        }

        /// <summary>
        /// GridLayoutGroup 없이 row,col → 아이소 좌표로 직접 배치.
        /// 공식: x = (col - row) * xStep - originX,  y = -(col + row) * yStep - originY
        /// 셀 크기는 인접 격자 간격(stepLength)으로 설정해 정사각형이 겹치지 않게 함.
        /// </summary>
        private void ApplyIsoLayout()
        {
            if (gridRoot == null) return;

            var layout = gridRoot.GetComponent<GridLayoutGroup>();
            if (layout != null)
                layout.enabled = false;

            int w = gridWidth;
            int h = gridHeight;
            int count = gridRoot.childCount;

            float stepLength = Mathf.Sqrt(xStep * xStep + yStep * yStep);
            Vector2 cellSize = new Vector2(stepLength, stepLength);

            float originX;
            float originY;
            if (autoCenterGrid)
            {
                originX = ((w - 1) - (h - 1)) * 0.5f * xStep;
                originY = ((w - 1) + (h - 1)) * 0.5f * yStep;
            }
            else
            {
                originX = gridOrigin.x;
                originY = gridOrigin.y;
            }

            for (int i = 0; i < count; i++)
            {
                int row = i / w;
                int col = i % w;

                float x = (col - row) * xStep - originX;
                float y = -(col + row) * yStep - originY;

                Transform child = gridRoot.GetChild(i);
                var rt = child.GetComponent<RectTransform>();
                if (rt == null) continue;

                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(x, y);
                rt.sizeDelta = cellSize;
            }

            foreach (var cell in _cells)
                cell.RefreshTileImageLayout(cellSize);
        }

        private TileData[] GetSampleMapData()
        {
            int total = gridWidth * gridHeight;
            var data = new TileData[total];
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    int i = y * gridWidth + x;
                    bool edge = x == 0 || x == gridWidth - 1 || y == 0 || y == gridHeight - 1;

                    if (edge && (x + y) % 2 == 0)
                    {
                        data[i] = TileData.Create(TileType.Locked, true, 0, "Locked", 0);
                    }
                    else if (i == 12)
                    {
                        data[i] = TileData.Create(TileType.Forest, false, 1, "Forest Tile", 10, "Wood");
                    }
                    else if (i == 6 || i == 8)
                    {
                        data[i] = TileData.Create(TileType.House, false, 1, "House Tile", 5, "Stone");
                    }
                    else if (i == 16 || i == 18)
                    {
                        data[i] = TileData.Create(TileType.Mountain, false, 1, "Mountain Tile", 20, "Gold");
                    }
                    else
                    {
                        data[i] = TileData.Create(TileType.Empty, false, 1, "Empty Tile", 0);
                    }
                }
            }
            return data;
        }

        public void OnTileClicked(TileCellUI cell)
        {
            if (cell == null) return;

            if (_selectedCell != null)
                _selectedCell.SetSelected(false);

            _selectedCell = cell;
            cell.SetSelected(true);

            if (tileInfoPanel != null)
                tileInfoPanel.ShowTile(cell.Data);

            if (!cell.Data.isLocked && cell.Data.resourceValue > 0)
                ShowGainText(cell, cell.Data.resourceValue, cell.Data.resourceLabel);
        }

        private void ShowGainText(TileCellUI cell, int amount, string label)
        {
            if (effectLayer == null) return;

            RectTransform anchor = cell.GainTextAnchor != null ? cell.GainTextAnchor : cell.GetComponent<RectTransform>();
            if (anchor == null) return;

            StartCoroutine(FloatingGainTextRoutine(anchor, $"+{amount} {label}"));
        }

        private IEnumerator FloatingGainTextRoutine(RectTransform anchor, string text)
        {
            GameObject go = new GameObject("GainText");
            go.transform.SetParent(effectLayer, false);

            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(120f, 40f);
            rt.anchoredPosition = WorldToLocal(anchor.position);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            if (gainTextFont != null) tmp.font = gainTextFont;
            tmp.fontSize = 18f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            float duration = 1f;
            float elapsed = 0f;
            Vector2 startPos = rt.anchoredPosition;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                rt.anchoredPosition = startPos + Vector2.up * (40f * t);
                tmp.alpha = 1f - t;
                yield return null;
            }

            Destroy(go);
        }

        private Vector2 WorldToLocal(Vector3 worldPos)
        {
            if (effectLayer == null) return Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(effectLayer, worldPos, null, out var local);
            return local;
        }

        public Sprite GetSpriteForType(TileType t)
        {
            return t switch
            {
                TileType.Empty => spriteEmpty,
                TileType.Forest => spriteForest,
                TileType.House => spriteHouse,
                TileType.Mountain => spriteMountain,
                TileType.Locked => spriteLocked,
                _ => null
            };
        }
    }
}
