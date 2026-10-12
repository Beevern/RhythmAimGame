using TMPro;
using UnityEngine;

//Pops a text in (scale + fade) every time its contents change. Used for the countdown.
[RequireComponent(typeof(TMP_Text))]
public class TextPop : MonoBehaviour
{
    public float fromScale = 1.5f;
    public float duration = 0.25f;

    private TMP_Text text;
    private string last;
    private float t = 1f;

    void Awake()
    {
        text = GetComponent<TMP_Text>();
        last = text.text;
        //Pop straight away if it already has something to show
        if (!string.IsNullOrEmpty(last)) t = 0f;
    }

    void LateUpdate()
    {
        if (text.text != last)
        {
            last = text.text;
            if (!string.IsNullOrEmpty(last)) t = 0f;
        }
        t = Mathf.Min(1f, t + Time.unscaledDeltaTime / duration);
        float e = 1f - (1f - t) * (1f - t) * (1f - t);
        transform.localScale = Vector3.one * Mathf.Lerp(fromScale, 1f, e);
        text.alpha = Mathf.Lerp(0f, 1f, Mathf.Clamp01(t * 2f));
    }
}
