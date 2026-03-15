using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace WoodLand3D.WorldMap
{
    /// <summary>
    /// 월드맵 UI 전체에 대한 입력 관리.
    /// - 마우스/터치 클릭이 발생했을 때,
    ///   어떤 TileSlot 도 클릭되지 않았다면 현재 선택된 타일을 해제한다.
    /// - 즉, "빈 공간을 눌렀다"는 효과를 전역적으로 처리한다.
    /// </summary>
    public class WorldMapInput : MonoBehaviour
    {
        private PointerEventData _pointerEventData;
        private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>(16);

        private void Awake()
        {
            if (EventSystem.current == null)
            {
                Debug.LogWarning("WorldMapInput: EventSystem.current 가 없음. UI 클릭 입력을 받을 수 없습니다.");
            }
        }

        private void Update()
        {
            // 좌클릭/터치 시작 시점만 처리
            if (!Input.GetMouseButtonDown(0))
                return;

            if (EventSystem.current == null)
                return;

            // 현재 마우스 위치 기준으로 UI Raycast 실행
            if (_pointerEventData == null)
                _pointerEventData = new PointerEventData(EventSystem.current);

            _pointerEventData.position = Input.mousePosition;
            _raycastResults.Clear();
            EventSystem.current.RaycastAll(_pointerEventData, _raycastResults);

            // Raycast 결과 중에 TileSlotUI 를 가진 오브젝트가 하나라도 있으면
            // 해당 슬롯의 OnPointerDown 이 선택/해제를 처리하므로 여기서는 아무 것도 하지 않는다.
            foreach (var hit in _raycastResults)
            {
                if (hit.gameObject == null)
                    continue;

                if (hit.gameObject.GetComponentInParent<TileSlotUI>() != null)
                    return;
            }

            // 어떤 TileSlot 도 클릭되지 않았다면,
            // 현재 선택된 타일을 해제하고 연결선도 숨긴다.
            TileSlotUI.ClearGlobalSelection();
        }
    }
}

