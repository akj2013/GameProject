using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// 리스트 항목: 짧게 누르고 드래그 = 스크롤. holdDuration초 길게 누르면 플로팅 이미지가 바로 뜨고 터치 따라 이동 → TreeImagePanel에 놓으면 적용.
/// </summary>
public class TreeListDragItem : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public GameObject treePrefab;

    public static GameObject CurrentDraggingPrefab { get; private set; }

    ScrollRect _scrollRect;
    StageTreePanelController _controller;
    float _pointerDownTime;
    bool _isTreeDrag;
    GameObject _floatingImage;
    Coroutine _holdCoroutine;

    void Awake()
    {
        _scrollRect = GetComponentInParent<ScrollRect>();
        _controller = GetComponentInParent<StageTreePanelController>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _pointerDownTime = Time.time;
        _isTreeDrag = false;
        if (_holdCoroutine != null) { StopCoroutine(_holdCoroutine); _holdCoroutine = null; }
        _holdCoroutine = StartCoroutine(WaitThenShowFloating());
    }

    IEnumerator WaitThenShowFloating()
    {
        float holdDuration = _controller != null ? _controller.holdDurationSeconds : 1f;
        yield return new WaitForSeconds(holdDuration);

        _holdCoroutine = null;
        _isTreeDrag = true;
        CurrentDraggingPrefab = treePrefab;

        var treeRaw = treePrefab != null ? treePrefab.GetComponentInChildren<RawImage>(true) : null;
        Texture tex = treeRaw != null ? treeRaw.texture : null;
        var parent = _controller != null ? _controller.transform : transform;
        _floatingImage = StageTreePanelController.CreateFloatingTreeImage(parent, tex, 100f);
        if (_floatingImage != null)
            StageTreePanelController.SetFloatingImagePosition(_floatingImage, Input.mousePosition);

        if (GameManager.Instance != null)
            GameManager.Instance.TriggerSelectingVibration();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_holdCoroutine != null) { StopCoroutine(_holdCoroutine); _holdCoroutine = null; }
        if (_isTreeDrag && _floatingImage != null)
            FinishTreeDrag(eventData.position);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_isTreeDrag && _floatingImage != null)
            StageTreePanelController.SetFloatingImagePosition(_floatingImage, eventData.position);
        else if (!_isTreeDrag && _scrollRect != null)
            ExecuteEvents.Execute(_scrollRect.gameObject, eventData, ExecuteEvents.beginDragHandler);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_isTreeDrag && _floatingImage != null)
            StageTreePanelController.SetFloatingImagePosition(_floatingImage, eventData.position);
        else if (_scrollRect != null)
            ExecuteEvents.Execute(_scrollRect.gameObject, eventData, ExecuteEvents.dragHandler);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_isTreeDrag)
            FinishTreeDrag(eventData.position);
        else if (_scrollRect != null)
            ExecuteEvents.Execute(_scrollRect.gameObject, eventData, ExecuteEvents.endDragHandler);
    }

    void FinishTreeDrag(Vector2 screenPos)
    {
        if (_floatingImage != null)
        {
            Destroy(_floatingImage);
            _floatingImage = null;
        }
        var slot = RaycastDropSlot(screenPos);
        if (slot != null && _controller != null)
            _controller.AssignTreeToStageAndUpdateSlot(slot.stageNumber, treePrefab, slot);
        StartCoroutine(ClearDraggingPrefabAfterDrop());
    }

    TreeImagePanelSlot RaycastDropSlot(Vector2 screenPos)
    {
        var results = new System.Collections.Generic.List<RaycastResult>();
        var eventData = new PointerEventData(EventSystem.current) { position = screenPos };
        EventSystem.current.RaycastAll(eventData, results);
        foreach (var r in results)
        {
            var slot = r.gameObject.GetComponent<TreeImagePanelSlot>();
            if (slot != null) return slot;
        }
        return null;
    }

    IEnumerator ClearDraggingPrefabAfterDrop()
    {
        yield return null;
        CurrentDraggingPrefab = null;
    }
}
