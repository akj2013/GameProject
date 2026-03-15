using UnityEngine;
using UnityEngine.EventSystems;

namespace WoodLand3D.WorldMap
{
    /// <summary>
    /// 씬에 고정된 "격자형 타일 배치 보드"의 루트.
    /// 런타임에 맵을 자동 생성하지 않으며, 미리 배치된 TileSlot 자식들을 담는 컨테이너 역할만 한다.
    /// 슬롯 생성/배치는 에디터 메뉴 "WoodLand3D > World Map Board > Create Board And Generate Slots"에서 수행.
    /// </summary>
    /// <summary>
    /// 씬에 고정된 "격자형 타일 배치 보드"의 루트.
    /// - 런타임에 맵을 자동 생성하지 않는다.
    /// - 미리 배치된 TileSlot 자식들을 담는 컨테이너 역할만 한다.
    /// - 보드의 빈 공간을 클릭했을 때 선택을 해제하기 위해 IPointerDownHandler 를 구현한다.
    /// </summary>
    public class WorldMapBoard : MonoBehaviour, IPointerDownHandler
    {
        [Header("격자 설정 (에디터에서 슬롯 생성 시 사용)")]
        [Tooltip("격자 열 수")]
        [SerializeField] private int gridWidth = 5;
        [Tooltip("격자 행 수")]
        [SerializeField] private int gridHeight = 5;
        [Tooltip("한 칸 이동 시 X 변화량. x = (col - row) * xStep")]
        [SerializeField] private float xStep = 40f;
        [Tooltip("한 칸 이동 시 Y 변화량. y = -(col + row) * yStep")]
        [SerializeField] private float yStep = 24f;
        [Tooltip("true면 그리드 중심이 (0,0)")]
        [SerializeField] private bool autoCenterGrid = true;
        [Tooltip("autoCenterGrid가 false일 때 (0,0) 셀 위치")]
        [SerializeField] private Vector2 gridOrigin = Vector2.zero;

        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;
        public float XStep => xStep;
        public float YStep => yStep;
        public bool AutoCenterGrid => autoCenterGrid;
        public Vector2 GridOrigin => gridOrigin;

        /// <summary>
        /// 인덱스로 TileSlotUI 자식 찾기.
        /// </summary>
        public TileSlotUI GetSlot(int index)
        {
            if (index < 0 || index >= transform.childCount)
                return null;
            return transform.GetChild(index).GetComponent<TileSlotUI>();
        }

        /// <summary>
        /// 행/열로 TileSlotUI 찾기.
        /// </summary>
        public TileSlotUI GetSlot(int row, int col)
        {
            int w = gridWidth;
            if (row < 0 || row >= gridHeight || col < 0 || col >= w)
                return null;
            return GetSlot(row * w + col);
        }

        /// <summary>
        /// 보드의 빈 공간을 클릭했을 때 호출된다.
        /// - EventSystem + GraphicRaycaster 가 있는 Canvas 하에서,
        ///   WorldMapBoard 에 투명한 Image 가 붙어 있으면 그 영역이 "빈 공간" 역할을 한다.
        /// - 이 때 현재 선택된 타일 슬롯의 SelectionFrame 을 모두 끈다.
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            TileSlotUI.ClearGlobalSelection();
        }
    }
}
