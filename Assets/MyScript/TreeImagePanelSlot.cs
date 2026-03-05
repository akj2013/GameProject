using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// TreeImagePanel 슬롯. 나무 드롭 시 이미지+타일 리스폰. holdDuration초 길게 누르면 이미지 들어올려서 바깥에 놓으면 슬롯·타일 클리어. 다른 슬롯에 놓으면 그 스테이지에 적용.
/// </summary>
public class TreeImagePanelSlot : MonoBehaviour, IDropHandler, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerUpHandler
{
    [Tooltip("이 패널이 나타내는 스테이지 번호 (1~)")]
    public int stageNumber = 1;

    [Tooltip("해당 스테이지 타일의 TileStageConfig")]
    public TileStageConfig targetTileConfig;

    [Tooltip("나무 이미지를 뿌릴 RawImage (TreeImagePanel의 RawImage)")]
    public RawImage slotRawImage;

    [Tooltip("Image 사용 시 (미니맵 등) 비어 있으면 이 오브젝트의 Image 사용")]
    public Image slotImage;

    /// <summary>다른 슬롯에서 길게 누르고 끌어온 나무 프리팹 (슬롯→슬롯 드롭 시 사용)</summary>
    public static GameObject CurrentDraggingPrefabFromSlot { get; private set; }

    StageTreePanelController _controller;
    float _pointerDownTime;
    bool _isClearDrag;
    GameObject _floatingImage;
    Texture _savedTexture;
    Sprite _savedSprite;
    Coroutine _holdCoroutine;
    GameObject _draggingPrefabFromThisSlot;

    void Awake()
    {
        if (slotRawImage == null)
            slotRawImage = GetComponent<RawImage>();
        if (slotImage == null)
            slotImage = GetComponent<Image>();
        _controller = GetComponentInParent<StageTreePanelController>();
    }

    /// <summary>
    /// 드래그해서 가져온 나무의 이미지를 여기 RawImage에 뿌림
    /// </summary>
    public void SetSlotTexture(Texture texture)
    {
        if (slotRawImage != null && texture != null)
        {
            slotRawImage.texture = texture;
            slotRawImage.color = Color.white;
            slotRawImage.enabled = true;
        }
        else if (slotImage != null && texture is Texture2D tex2d)
        {
            var sprite = Sprite.Create(tex2d, new Rect(0, 0, tex2d.width, tex2d.height), new Vector2(0.5f, 0.5f));
            slotImage.sprite = sprite;
            slotImage.color = Color.white;
            slotImage.enabled = true;
        }
    }

    public void SetSlotSprite(Sprite sprite)
    {
        if (slotImage != null && sprite != null)
        {
            slotImage.sprite = sprite;
            slotImage.color = Color.white;
            slotImage.enabled = true;
        }
    }

    public void SetColor(Color color)
    {
        if (slotRawImage != null) slotRawImage.color = color;
        if (slotImage != null) slotImage.color = color;
    }

    /// <summary>
    /// 슬롯 이미지 비우기 (null 상태로 보이도록). raycastTarget은 유지해서 드롭은 계속 받음.
    /// </summary>
    public void ClearSlot()
    {
        if (slotRawImage != null)
        {
            slotRawImage.texture = null;
            slotRawImage.color = new Color(1, 1, 1, 0);
            slotRawImage.enabled = true;
        }
        if (slotImage != null)
        {
            slotImage.sprite = null;
            slotImage.color = new Color(1, 1, 1, 0);
            slotImage.enabled = true;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _pointerDownTime = Time.time;
        _isClearDrag = false;
        if (_holdCoroutine != null) { StopCoroutine(_holdCoroutine); _holdCoroutine = null; }
        bool hasContent = (slotRawImage != null && slotRawImage.texture != null) || (slotImage != null && slotImage.sprite != null);
        if (hasContent)
            _holdCoroutine = StartCoroutine(WaitThenShowFloatingForSlot(eventData.position));
    }

    IEnumerator WaitThenShowFloatingForSlot(Vector2 downPos)
    {
        float holdDuration = _controller != null ? _controller.holdDurationSeconds : 1f;
        yield return new WaitForSeconds(holdDuration);

        _holdCoroutine = null;
        Texture tex = null;
        if (slotRawImage != null && slotRawImage.texture != null)
        {
            tex = slotRawImage.texture;
            _savedTexture = tex;
            _savedSprite = null;
        }
        else if (slotImage != null && slotImage.sprite != null)
        {
            _savedSprite = slotImage.sprite;
            tex = slotImage.sprite.texture;
            _savedTexture = null;
        }

        _isClearDrag = true;
        _draggingPrefabFromThisSlot = _controller != null ? (_controller.GetTreeForStage(stageNumber) ?? (targetTileConfig != null && targetTileConfig.SpawnableTreePrefabs != null && targetTileConfig.SpawnableTreePrefabs.Count > 0 ? targetTileConfig.SpawnableTreePrefabs[0] : null)) : null;
        CurrentDraggingPrefabFromSlot = _draggingPrefabFromThisSlot;

        var parent = _controller != null ? _controller.transform : transform;
        _floatingImage = StageTreePanelController.CreateFloatingTreeImage(parent, tex, 100f);
        if (_floatingImage != null)
            StageTreePanelController.SetFloatingImagePosition(_floatingImage, Input.mousePosition);

        ClearSlot();
        if (GameManager.Instance != null)
            GameManager.Instance.TriggerSelectingVibration();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_isClearDrag && _floatingImage != null)
            StageTreePanelController.SetFloatingImagePosition(_floatingImage, eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_isClearDrag && _floatingImage != null)
            StageTreePanelController.SetFloatingImagePosition(_floatingImage, eventData.position);
    }

    void FinishClearDrag(Vector2 screenPos)
    {
        if (_floatingImage != null)
        {
            Destroy(_floatingImage);
            _floatingImage = null;
        }
        var droppedOnSlot = RaycastSlotAt(screenPos);
        if (droppedOnSlot != null && droppedOnSlot == this)
        {
            if (_savedTexture != null)
                SetSlotTexture(_savedTexture);
            else if (_savedSprite != null)
                SetSlotSprite(_savedSprite);
            CurrentDraggingPrefabFromSlot = null;
        }
        else if (droppedOnSlot == null)
        {
            // 바깥에 놓음: 슬롯 비우고, 해당 타일의 spawnable 비우기 + 나무 제거
            ClearSlot();
            if (targetTileConfig != null)
            {
                targetTileConfig.ClearTileCompletely();
                targetTileConfig.RespawnTreesOnTileGrounds(syncFromExistingTrees: false);
            }
            if (_controller != null)
                _controller.AssignTreeToStage(stageNumber, null);
            CurrentDraggingPrefabFromSlot = null;
        }
        else
        {
            // 다른 스테이지 슬롯에 놓음: 원래 슬롯은 spawnable 비우기 + 나무 제거, 놓은 슬롯에 나무 적용
            ClearSlot();
            if (targetTileConfig != null)
            {
                targetTileConfig.ClearTileCompletely();
                targetTileConfig.RespawnTreesOnTileGrounds(syncFromExistingTrees: false);
            }
            if (_controller != null)
            {
                _controller.AssignTreeToStage(stageNumber, null);
                var prefab = _draggingPrefabFromThisSlot;
                if (prefab != null)
                    _controller.AssignTreeToStageAndUpdateSlot(droppedOnSlot.stageNumber, prefab, droppedOnSlot);
            }
            CurrentDraggingPrefabFromSlot = null;
        }

        _draggingPrefabFromThisSlot = null;
        _savedTexture = null;
        _savedSprite = null;
        _isClearDrag = false;
    }

    static TreeImagePanelSlot RaycastSlotAt(Vector2 screenPos)
    {
        var results = new System.Collections.Generic.List<RaycastResult>();
        var eventData = new PointerEventData(EventSystem.current) { position = screenPos };
        EventSystem.current.RaycastAll(eventData, results);
        foreach (var r in results)
        {
            var slot = r.gameObject.GetComponentInParent<TreeImagePanelSlot>();
            if (slot != null) return slot;
        }
        return null;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isClearDrag) return;
        FinishClearDrag(eventData.position);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // 터치를 뗐을 때 한 번은 반드시 처리 (OnEndDrag가 안 불리는 터치 환경 대비). 플로팅 제거 + 드롭 로직은 FinishClearDrag에서 함.
        if (_isClearDrag && _floatingImage != null)
            FinishClearDrag(eventData.position);
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject prefab = TreeListDragItem.CurrentDraggingPrefab;
        bool fromSlot = false;
        if (prefab == null)
        {
            prefab = CurrentDraggingPrefabFromSlot;
            fromSlot = prefab != null;
        }
        if (prefab == null) return;

        // 슬롯→슬롯 드롭은 소스 슬롯의 FinishClearDrag에서만 적용. 여기서 또 적용하면 ReplaceTreesInTile이 두 번 호출되어 나무가 64+64로 쌓임.
        if (fromSlot)
        {
            CurrentDraggingPrefabFromSlot = null;
            return;
        }

        if (_controller != null)
            _controller.AssignTreeToStageAndUpdateSlot(stageNumber, prefab, this);

        CurrentDraggingPrefabFromSlot = null;
    }
}
