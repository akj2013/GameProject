using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FloatingJoystickProxy : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Tooltip("Canvas containing the joystick UI")]
    [SerializeField] Canvas canvas;

    [Tooltip("Root RectTransform of the joystick (Background GameObject)")]
    [SerializeField] RectTransform joystickRoot;

    [Tooltip("Optional: a background Image to enable/disable visuals")]
    [SerializeField] Graphic backgroundGraphic;

    EventSystem m_EventSystem;

    void Awake()
    {
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        m_EventSystem = EventSystem.current;

        if (joystickRoot != null)
            joystickRoot.gameObject.SetActive(false);
    }

    // Convert screen point to world point in referenceRect. Returns true when conversion succeeded.
    bool ScreenPointToWorldPointInReference(Vector2 screenPos, Camera cam, out Vector3 world)
    {
        world = Vector3.zero;
        if (canvas == null || joystickRoot == null) return false;

        RectTransform referenceRect = joystickRoot.parent as RectTransform;
        if (referenceRect == null)
            referenceRect = canvas.transform as RectTransform;

        return RectTransformUtility.ScreenPointToWorldPointInRectangle(referenceRect, screenPos, cam, out world);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (joystickRoot == null) return;

        // Place joystick pivot exactly at the click world position so the click is at joystick center.
        if (ScreenPointToWorldPointInReference(eventData.position, eventData.pressEventCamera, out Vector3 worldPos))
        {
            // ???? -> ???? -> ???? ???? ?????? ??????? ???
            joystickRoot.position = worldPos;
            joystickRoot.gameObject.SetActive(true);

            if (backgroundGraphic != null) backgroundGraphic.enabled = true;
        }

        // forward original eventData to joystick so its internal state stays valid
        ExecuteEvents.ExecuteHierarchy(joystickRoot.gameObject, eventData, ExecuteEvents.pointerDownHandler);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (joystickRoot == null) return;

        // forward original eventData to joystick
        ExecuteEvents.ExecuteHierarchy(joystickRoot.gameObject, eventData, ExecuteEvents.dragHandler);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (joystickRoot == null) return;

        // forward original eventData to joystick
        ExecuteEvents.ExecuteHierarchy(joystickRoot.gameObject, eventData, ExecuteEvents.pointerUpHandler);

        // hide joystick
        joystickRoot.gameObject.SetActive(false);
        if (backgroundGraphic != null) backgroundGraphic.enabled = false;
    }
}
