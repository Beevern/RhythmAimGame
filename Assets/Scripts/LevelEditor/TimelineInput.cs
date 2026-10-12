using System;
using UnityEngine;
using UnityEngine.EventSystems;

//Reports clicks and drags on a timeline strip as an x position (in the strip's local units)
public class TimelineInput : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    //(x, pressed this event, dragging)
    public Action<float, bool, bool> onPress;

    float LocalX(PointerEventData e)
    {
        RectTransform rt = (RectTransform)transform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, e.pressEventCamera, out Vector2 p);
        return Mathf.Clamp(p.x - rt.rect.xMin, 0f, rt.rect.width);
    }

    public void OnPointerDown(PointerEventData e) => onPress?.Invoke(LocalX(e), true, false);
    public void OnDrag(PointerEventData e) => onPress?.Invoke(LocalX(e), false, true);
    public void OnPointerUp(PointerEventData e) => onPress?.Invoke(LocalX(e), false, false);
}
