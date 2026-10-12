using UnityEngine;
using UnityEngine.EventSystems;

//Subtle scale response on hover/press. Uses unscaled time so it works in the pause menu.
public class ButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public float hoverScale = 1.03f;
    public float pressScale = 0.97f;

    private Vector3 baseScale;
    private bool hovered;
    private bool pressed;

    void Awake()
    {
        baseScale = transform.localScale;
    }

    void OnDisable()
    {
        hovered = pressed = false;
        transform.localScale = baseScale;
    }

    void Update()
    {
        float target = pressed ? pressScale : hovered ? hoverScale : 1f;
        Vector3 goal = baseScale * target;
        transform.localScale = Vector3.Lerp(transform.localScale, goal, 1f - Mathf.Exp(-20f * Time.unscaledDeltaTime));
    }

    public void OnPointerEnter(PointerEventData e) { hovered = true; }
    public void OnPointerExit(PointerEventData e) { hovered = false; pressed = false; }
    public void OnPointerDown(PointerEventData e) { pressed = true; }
    public void OnPointerUp(PointerEventData e) { pressed = false; }
}
