using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//Pill-shaped on/off switch built from code
public class UISwitch : MonoBehaviour, IPointerClickHandler
{
    public event Action<bool> Changed;

    private bool value;
    private Image track;
    private RectTransform knob;
    private float knobT;

    public bool Value
    {
        get => value;
        set { this.value = value; }
    }

    public static UISwitch Create(Transform parent, bool initial)
    {
        GameObject go = new GameObject("Switch", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        ((RectTransform)go.transform).sizeDelta = new Vector2(52, 28);
        UISwitch sw = go.AddComponent<UISwitch>();

        sw.track = go.AddComponent<Image>();
        sw.track.sprite = Theme.RoundedRect;
        sw.track.type = Image.Type.Sliced;
        sw.track.pixelsPerUnitMultiplier = 1f;
        sw.track.raycastTarget = true;

        Image k = UIFactory.MakeImage(go.transform, Theme.Circle, Color.white, new Vector2(22, 22));
        sw.knob = k.rectTransform;
        sw.value = initial;
        sw.knobT = initial ? 1f : 0f;
        sw.Apply();
        return sw;
    }

    public void OnPointerClick(PointerEventData e)
    {
        value = !value;
        Changed?.Invoke(value);
    }

    void Update()
    {
        knobT = Mathf.MoveTowards(knobT, value ? 1f : 0f, Time.unscaledDeltaTime / 0.12f);
        Apply();
    }

    private void Apply()
    {
        track.color = Color.Lerp(Theme.Track, Theme.Accent, knobT);
        knob.anchoredPosition = new Vector2(Mathf.Lerp(-12f, 12f, knobT), 0f);
    }
}
