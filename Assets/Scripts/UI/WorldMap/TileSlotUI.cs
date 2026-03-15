using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace WoodLand3D.WorldMap
{
    /// <summary>
    /// 씬에 고정된 타일 슬롯 하나.
    /// - Inspector 에서 타일 스프라이트를 넣으면 화면에 바로 표시된다.
    /// - 런타임 자동 생성이 아니라, 월드맵을 수작업으로 조립하는 "작업대용 슬롯" 이다.
    /// - 선택 하이라이트는 SelectionFrame 스프라이트로만 표시한다.
    /// </summary>
    public class TileSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
    {
        [Header("타일 표시")]
        [Tooltip("이 슬롯에 넣을 타일 이미지. 바꾸면 즉시 반영된다.")]
        [SerializeField] private Sprite tileSprite;

        [Tooltip("실제 타일 이미지를 표시하는 Image")]
        [SerializeField] private Image tileImageDisplay;

        [Header("선택 프레임 (슬롯 기준)")]
        [Tooltip("선택 시 보이는 프레임 스프라이트 Image (슬롯 전체를 감싸는 UI)")]
        [SerializeField] private Image selectionFrameImage;

        [Header("격자 테두리 (항상 표시)")]
        [Tooltip("슬롯 격자선 색상")]
        [SerializeField] private Color borderColor = new Color(1f, 1f, 1f, 0.2f);

        [Tooltip("격자선 두께 (Outline effectDistance.x,y 절대값)")]
        [SerializeField] private float borderThickness = 1f;

        [Tooltip("격자 테두리를 그릴 Image (슬롯 배경용)")]
        [SerializeField] private Image borderDisplay;

        [Header("슬롯 메타 정보 (저장/로드 확장용)")]
        [SerializeField] private int slotIndex = -1;
        [SerializeField] private int row = -1;
        [SerializeField] private int col = -1;

        [Header("부유 애니메이션 (플레이 중)")]
        [Tooltip("게임 실행 중 슬롯이 살짝 떠다니는 효과를 줄지 여부")]
        [SerializeField] private bool floatOnPlay = true;

        [Tooltip("X,Y 방향 진폭 (픽셀 단위)")]
        [SerializeField] private Vector2 floatAmplitude = new Vector2(4f, 6f);

        [Tooltip("전체 속도 배율")]
        [SerializeField] private float floatSpeed = 1f;

        [Tooltip("애니메이션 시작 위상 오프셋 (라디안). 슬롯마다 랜덤 위상이 추가됨")]
        [SerializeField] private float floatPhaseOffset = 0f;

        // 내부 상태 ----------------------------------------------------------
        private RectTransform _rectTransform;
        private Vector2 _baseAnchoredPosition;
        private float _phase;
        private bool _isDragging;
        private Vector2 _dragOffsetLocal;
        private Outline _borderOutline;

        /// <summary>현재 선택된 슬롯(보드 전체에서 1개 유지)</summary>
        private static TileSlotUI _currentSelected;

        /// <summary>현재 슬롯이 선택 상태인지 여부</summary>
        private bool _isSelected;

        // 외부에서 읽기 좋은 프로퍼티들 ------------------------------------
        public Sprite TileSprite
        {
            get => tileSprite;
            set
            {
                tileSprite = value;
                RefreshDisplay();
            }
        }

        public int SlotIndex => slotIndex;
        public int Row => row;
        public int Col => col;

        // Unity 생명주기 ----------------------------------------------------

        private void OnValidate()
        {
            // 에디터에서 값이 바뀌었을 때도 UI를 갱신
            RefreshDisplay();
            CacheBasePosition();
            ApplySelectionVisual();
        }

        private void Reset()
        {
            RefreshDisplay();
            CacheBasePosition();
            SetSelected(false, false);
        }

        private void Awake()
        {
            EnsureBorderOutline();
            SetSelected(false, false);
            CacheBasePosition();
        }

        private void OnEnable()
        {
            EnsureBorderOutline();
            ApplySelectionVisual();
            CacheBasePosition();
        }

        /// <summary>
        /// Inspector 에서 스프라이트를 바꿨을 때 타일 이미지를 갱신한다.
        /// </summary>
        public void RefreshDisplay()
        {
            // 본 타일 이미지
            if (tileImageDisplay != null)
            {
                tileImageDisplay.sprite = tileSprite;
                tileImageDisplay.enabled = tileSprite != null;
                tileImageDisplay.color = Color.white;
            }

            // 선택 프레임은 프레임용 스프라이트를 아티스트가 지정하는 구조라
            // 여기서는 상태(선택/비선택)에 따라 On/Off 만 제어한다.

            // 격자 테두리 기본 설정
            if (borderDisplay != null)
            {
                borderDisplay.raycastTarget = false;
                borderDisplay.enabled = true;
                EnsureBorderOutline();
                ApplySelectionVisual();
            }
        }

        /// <summary>
        /// borderDisplay 에 Outline 컴포넌트가 연결되어 있는지 보장한다.
        /// </summary>
        private void EnsureBorderOutline()
        {
            if (borderDisplay == null)
                return;

            if (_borderOutline == null)
            {
                _borderOutline = borderDisplay.GetComponent<Outline>();
                if (_borderOutline == null)
                    _borderOutline = borderDisplay.gameObject.AddComponent<Outline>();
            }
        }

        /// <summary>
        /// 이 슬롯을 선택/해제한다.
        /// 다른 슬롯과의 관계(전역 1개 선택)를 함께 처리한다.
        /// </summary>
        private void SetSelected(bool selected, bool asCurrent = true)
        {
            // 선택 전의 전역 선택 상태를 저장해 두면
            // 새 슬롯을 선택할 때 "이전 슬롯 → 현재 슬롯" 연결선을 그릴 수 있다.
            TileSlotUI previousSelected = _currentSelected;

            _isSelected = selected;
            ApplySelectionVisual();

            if (selected && asCurrent)
            {
                // 이전 선택 슬롯이 있으면 해제
                if (_currentSelected != null && _currentSelected != this)
                    _currentSelected.SetSelected(false, false);

                _currentSelected = this;

                // 이전에 다른 슬롯이 선택되어 있었다면, 두 슬롯을 연결하는 라인을 보여준다.
                if (previousSelected != null && previousSelected != this && TileConnectionManager.Instance != null)
                {
                    TileConnectionManager.Instance.ShowConnection(previousSelected, this);
                }
            }
            else if (!selected && _currentSelected == this && asCurrent)
            {
                _currentSelected = null;
            }
        }

        /// <summary>
        /// 보드의 빈 공간을 눌렀을 때 등,
        /// 외부에서 전역 선택을 모두 해제하고 싶을 때 호출하는 정적 메서드.
        /// </summary>
        public static void ClearGlobalSelection()
        {
            if (_currentSelected != null)
            {
                _currentSelected.SetSelected(false, false);
                _currentSelected = null;
            }
        }

        /// <summary>
        /// 선택/비선택 상태에 따라 SelectionFrame, Border 등을 갱신한다.
        /// hover, locked, preview 같은 상태도 이 메서드를 확장하면 된다.
        /// </summary>
        private void ApplySelectionVisual()
        {
            // 격자선은 항상 동일한 색/두께로 유지
            if (borderDisplay != null)
            {
                borderDisplay.color = borderColor;
                EnsureBorderOutline();
                if (_borderOutline != null)
                {
                    _borderOutline.effectColor = Color.white;
                    _borderOutline.effectDistance = new Vector2(borderThickness, borderThickness);
                }
            }

            // 선택 프레임 On/Off (실제 실루엣이 아니라 슬롯 기준 프레임)
            if (selectionFrameImage != null)
            {
                selectionFrameImage.enabled = _isSelected;

                if (_isSelected)
                {
                    // 프레임이 항상 타일 위에 보이도록 맨 마지막 자식으로 이동
                    selectionFrameImage.transform.SetAsLastSibling();
                }
            }
        }

        /// <summary>
        /// 현재 위치를 부유 애니메이션의 기준점으로 캐시한다.
        /// </summary>
        private void CacheBasePosition()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            if (_rectTransform == null)
                return;

            _baseAnchoredPosition = _rectTransform.anchoredPosition;

            // 슬롯마다 위상이 조금씩 다르게 되도록 랜덤 위상 추가
            _phase = floatPhaseOffset + Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            if (!Application.isPlaying || !floatOnPlay)
                return;

            if (_isDragging)
                return;

            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            if (_rectTransform == null)
                return;

            float t = Time.time * floatSpeed;

            // X 는 sin, Y 는 cos 를 약간 다른 속도로 써서 상하좌우로 부드럽게 움직이게 함
            float offsetX = Mathf.Sin(t + _phase) * floatAmplitude.x;
            float offsetY = Mathf.Cos(t * 0.8f + _phase) * floatAmplitude.y;

            Vector2 pos = _baseAnchoredPosition + new Vector2(offsetX, offsetY);

            // 픽셀 그리드에 정렬해서 소수점 좌표로 인한 번짐/깨짐을 줄임
            pos.x = Mathf.Round(pos.x);
            pos.y = Mathf.Round(pos.y);

            _rectTransform.anchoredPosition = pos;
        }

        // 마우스/터치 입력 ----------------------------------------------------

        public void OnPointerDown(PointerEventData eventData)
        {
            // 클릭 시 슬롯을 맨 앞으로 올려서 다른 슬롯 위에 보이게 함
            transform.SetAsLastSibling();

            // 이미 선택된 슬롯을 다시 누르면 선택 해제,
            // 선택되지 않은 슬롯을 누르면 선택으로 전환.
            SetSelected(!_isSelected);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            if (_rectTransform == null)
                return;

            var parentRt = _rectTransform.parent as RectTransform;
            if (parentRt == null)
                return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRt,
                    eventData.position,
                    eventData.pressEventCamera,
                    out var localPoint))
            {
                _dragOffsetLocal = localPoint - _rectTransform.anchoredPosition;
                _isDragging = true;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging || _rectTransform == null)
                return;

            var parentRt = _rectTransform.parent as RectTransform;
            if (parentRt == null)
                return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRt,
                    eventData.position,
                    eventData.pressEventCamera,
                    out var localPoint))
            {
                _rectTransform.anchoredPosition = localPoint - _dragOffsetLocal;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            if (_rectTransform == null)
                return;

            _isDragging = false;

            // 드롭한 위치를 기준으로 다시 부유하도록 기준 위치를 갱신
            _baseAnchoredPosition = _rectTransform.anchoredPosition;
        }

        /// <summary>
        /// 에디터에서 슬롯 인덱스/행/열 설정 (보드 생성 시 호출).
        /// 이후 저장/로드 로직에서 이 정보를 활용할 수 있다.
        /// </summary>
        public void SetSlotIndex(int index, int rowCount, int colCount)
        {
            slotIndex = index;
            row = index / colCount;
            col = index % colCount;
        }
    }
}

