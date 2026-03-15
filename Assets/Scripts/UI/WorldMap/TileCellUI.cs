using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WoodLand3D.WorldMap
{
    [RequireComponent(typeof(Button))]
    public class TileCellUI : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private Image tileImage;
        [SerializeField] private GameObject selectionHighlight;
        [SerializeField] private GameObject lockIcon;
        [SerializeField] private RectTransform gainTextAnchor;

        [Header("타일 이미지 (셀 중앙 기준, 공통 규칙)")]
        [Tooltip("미세 보정만 사용. 기본 (0,0) 권장")]
        [SerializeField] private Vector2 tileImageOffset = Vector2.zero;
        [Tooltip("1 = 셀 크기와 동일. 공통 규칙에서는 1 권장")]
        [SerializeField] private float tileImageScale = 1f;
        [Tooltip("켜면 스프라이트 비율(가로:세로)을 유지. 1536x1024 같은 비정사각형 스프라이트에 사용")]
        [SerializeField] private bool preserveSpriteAspectRatio = false;
        [Tooltip("비율 유지 시 기준: 셀 너비에 맞출지, 셀 높이에 맞출지")]
        [SerializeField] private FitMode aspectFitMode = FitMode.FitByHeight;

        public enum FitMode
        {
            [Tooltip("셀 너비에 맞추고, 높이는 비율에 따라 계산 (가로 꽉 참)")]
            FitByWidth,
            [Tooltip("셀 높이에 맞추고, 너비는 비율에 따라 계산 (세로 꽉 참). 1536x1024처럼 가로가 긴 스프라이트에 적합")]
            FitByHeight
        }

        [Header("타일 이미지 (비어 있으면 placeholder 색상 사용)")]
        [SerializeField] private Sprite spriteEmpty;
        [SerializeField] private Sprite spriteForest;
        [SerializeField] private Sprite spriteHouse;
        [SerializeField] private Sprite spriteMountain;
        [SerializeField] private Sprite spriteLocked;

        private Button _button;
        private TileData _data;
        private int _gridIndex;
        private WorldMapUIManager _spriteSource;

        public TileData Data => _data;
        public int GridIndex => _gridIndex;
        public RectTransform GainTextAnchor => gainTextAnchor != null ? gainTextAnchor : GetComponent<RectTransform>();

        public void SetSpriteSource(WorldMapUIManager manager)
        {
            _spriteSource = manager;
        }

        private void Awake()
        {
            EnsureRaycastTarget();
            _button = GetComponent<Button>();
            if (_button != null)
            {
                if (_button.targetGraphic == null)
                    _button.targetGraphic = GetComponent<Graphic>();
                _button.onClick.AddListener(OnClick);
            }
        }

        private void EnsureRaycastTarget()
        {
            var graphic = GetComponent<Graphic>();
            if (graphic == null)
            {
                var img = gameObject.AddComponent<Image>();
                img.color = new Color(1f, 1f, 1f, 0f);
                img.raycastTarget = true;
            }
            else if (!graphic.raycastTarget)
            {
                graphic.raycastTarget = true;
            }
        }

        public void Setup(int index, TileData data)
        {
            _gridIndex = index;
            _data = data;
            ApplyVisuals();
        }

        public void RefreshTileImageLayout(Vector2 cellSize)
        {
            if (tileImage == null) return;
            var rt = tileImage.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = tileImageOffset;

            Vector2 sizeDelta;
            if (preserveSpriteAspectRatio && tileImage.sprite != null)
            {
                float w = tileImage.sprite.rect.width;
                float h = tileImage.sprite.rect.height;
                if (w <= 0f || h <= 0f)
                {
                    sizeDelta = cellSize * tileImageScale;
                }
                else
                {
                    float baseSize = aspectFitMode == FitMode.FitByHeight ? cellSize.y : cellSize.x;
                    float scale = baseSize * tileImageScale;
                    if (aspectFitMode == FitMode.FitByHeight)
                        sizeDelta = new Vector2(scale * (w / h), scale);
                    else
                        sizeDelta = new Vector2(scale, scale * (h / w));
                }
            }
            else
            {
                sizeDelta = cellSize * tileImageScale;
            }

            rt.sizeDelta = sizeDelta;
        }

        public void SetSelected(bool selected)
        {
            if (selectionHighlight != null)
                selectionHighlight.SetActive(selected);
        }

        private void ApplyVisuals()
        {
            if (_data == null) return;

            if (tileImage != null)
            {
                Sprite s = GetSpriteForType(_data.type);
                if (s != null)
                {
                    tileImage.sprite = s;
                    tileImage.color = Color.white;
                }
                else
                {
                    tileImage.sprite = null;
                    tileImage.color = GetColorForType(_data.type);
                }
            }

            if (lockIcon != null)
                lockIcon.SetActive(_data.isLocked);
        }

        private Sprite GetSpriteForType(TileType t)
        {
            if (_spriteSource != null)
            {
                Sprite fromManager = _spriteSource.GetSpriteForType(t);
                if (fromManager != null) return fromManager;
            }
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

        private static Color GetColorForType(TileType t)
        {
            return t switch
            {
                TileType.Empty => new Color(0.7f, 0.95f, 0.7f),
                TileType.Forest => new Color(0.2f, 0.6f, 0.2f),
                TileType.House => new Color(0.85f, 0.75f, 0.4f),
                TileType.Mountain => new Color(0.5f, 0.5f, 0.55f),
                TileType.Locked => new Color(0.25f, 0.2f, 0.2f),
                _ => Color.gray
            };
        }

        private void OnClick()
        {
            WorldMapUIManager manager = GetComponentInParent<WorldMapUIManager>();
            if (manager != null)
                manager.OnTileClicked(this);
        }
    }
}
