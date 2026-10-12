using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//Small UI builders for the duo screens (lobby, in-game partner HUD, results card).
//Positions are in a 1920x1080 canvas (UIFactory.OverlayCanvas), measured from the parent's centre.
public static class DuoUI
{
    public static Canvas Canvas(string name, int order)
    {
        Canvas canvas = UIFactory.OverlayCanvas(name, order);
        canvas.GetComponent<CanvasScaler>().screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;
        canvas.gameObject.AddComponent<GraphicRaycaster>();
        canvas.gameObject.AddComponent<NoTheme>();
        return canvas;
    }

    public static RectTransform Place(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    public static Image Panel(Transform parent, float x, float y, float w, float h, Color color)
    {
        Image img = UIFactory.MakeImage(parent, Theme.RoundedRect, color, Vector2.zero);
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 1.4f;
        Place(img.rectTransform, x, y, w, h);
        return img;
    }

    public static TextMeshProUGUI Label(Transform parent, string text, float x, float y, float w, float h,
        float size, Color color, bool bold = false, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        t.color = color;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.raycastTarget = false;
        Place(t.rectTransform, x, y, w, h);
        return t;
    }

    public static Button Button(Transform parent, string text, float x, float y, float w, float h, bool primary, Action onClick)
    {
        Image bg = Panel(parent, x, y, w, h, Color.white);
        bg.name = text;
        bg.raycastTarget = true;
        Button b = bg.gameObject.AddComponent<Button>();
        ColorBlock cb = ColorBlock.defaultColorBlock;
        cb.normalColor = primary ? Theme.Accent : Theme.SurfaceHover;
        cb.highlightedColor = primary ? Theme.Accent.WithAlpha(0.85f) : Theme.Track.WithAlpha(0.3f);
        cb.pressedColor = Theme.AccentPressed;
        cb.selectedColor = cb.normalColor;
        cb.disabledColor = Theme.Track;
        cb.fadeDuration = 0.08f;
        b.colors = cb;
        b.onClick.AddListener(() => onClick());
        bg.gameObject.AddComponent<ButtonFeedback>();
        TextMeshProUGUI label = Label(bg.rectTransform, text, 0, 0, w, h, 22, primary ? Theme.Background : Theme.Text, true);
        label.characterSpacing = 4;
        return b;
    }

    //Single-line text box
    public static TMP_InputField Input(Transform parent, string placeholder, string value, float x, float y, float w, float h)
    {
        //Built inactive so the input field never enables without its text components
        GameObject go = new GameObject("Input", typeof(RectTransform));
        go.SetActive(false);
        go.transform.SetParent(parent, false);
        Image bg = go.AddComponent<Image>();
        bg.sprite = Theme.RoundedRect;
        bg.type = Image.Type.Sliced;
        bg.pixelsPerUnitMultiplier = 1.4f;
        bg.color = Theme.Field;
        Place(bg.rectTransform, x, y, w, h);

        RectTransform area = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
        area.SetParent(go.transform, false);
        UIFactory.Stretch(area);
        area.offsetMin = new Vector2(18, 6);
        area.offsetMax = new Vector2(-18, -6);

        TextMeshProUGUI ph = Label(area, placeholder, 0, 0, 0, 0, 26, Theme.TextMuted, false, TextAlignmentOptions.MidlineLeft);
        ph.textWrappingMode = TextWrappingModes.NoWrap;
        ph.fontStyle = FontStyles.Italic;
        UIFactory.Stretch(ph.rectTransform);
        TextMeshProUGUI text = Label(area, "", 0, 0, 0, 0, 26, Theme.Text, false, TextAlignmentOptions.MidlineLeft);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        UIFactory.Stretch(text.rectTransform);

        TMP_InputField field = go.AddComponent<TMP_InputField>();
        field.textViewport = area;
        field.textComponent = text;
        field.placeholder = ph;
        field.targetGraphic = bg;
        field.lineType = TMP_InputField.LineType.SingleLine;
        field.caretColor = Theme.Accent;
        field.customCaretColor = true;
        field.selectionColor = Theme.Accent.WithAlpha(0.35f);
        go.SetActive(true);
        field.text = value ?? "";
        return field;
    }

    public static string Thousands(int n) => n.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
}
