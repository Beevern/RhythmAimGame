using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//Crosshair settings panel: live preview on the left, controls on the right.
//Opened from the settings card in the menu and in the pause menu. Built on its own canvas
//(fixed 1920x1080 layout scaled to fit), so it never collides with scene layouts.
public class CrosshairEditor : MonoBehaviour
{
    private const float CardW = 1040f, CardH = 700f;
    private const float RowH = 46f, RowsTop = 100f;
    private const float LabelX = 460f, ControlX = 660f, ControlW = 280f, ValueX = 952f;

    private static CrosshairEditor instance;
    public static bool IsOpen => instance != null;

    private RectTransform card;
    private float openT;
    //Rows that only matter when their feature is switched on
    private readonly List<CanvasGroup> outlineRows = new List<CanvasGroup>();
    private readonly List<CanvasGroup> dotRows = new List<CanvasGroup>();
    private readonly List<CanvasGroup> lineRows = new List<CanvasGroup>();
    //Refreshes every control from the current settings (used by Reset)
    private readonly List<Action> refreshers = new List<Action>();
    private readonly List<Image> swatchRings = new List<Image>();

    public static void Open()
    {
        if (instance != null) return;
        Canvas canvas = UIFactory.OverlayCanvas("CrosshairEditor", 150);
        canvas.GetComponent<CanvasScaler>().screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;
        canvas.gameObject.AddComponent<GraphicRaycaster>();
        instance = canvas.gameObject.AddComponent<CrosshairEditor>();
        instance.Build();
    }

    private void Build()
    {
        //Scrim blocks clicks to whatever is behind
        Image scrim = UIFactory.FullScreenImage(transform, new Color(0f, 0f, 0f, 0.7f));
        scrim.raycastTarget = true;

        Image cardImg = UIFactory.MakeImage(transform, Theme.RoundedRect, Theme.Hex("#1D1F26"), new Vector2(CardW, CardH));
        cardImg.type = Image.Type.Sliced;
        cardImg.pixelsPerUnitMultiplier = 1.2f;
        cardImg.raycastTarget = true;
        card = cardImg.rectTransform;

        Image strip = Box(card, 14, 0, CardW - 28, 3, Theme.Accent);
        strip.sprite = Theme.Square;

        TextMeshProUGUI title = Label(card, "CROSSHAIR", 40, 30, 400, 44, 32, true, Theme.Text, TextAlignmentOptions.Left);
        title.characterSpacing = 4;

        BuildPreview();
        BuildControls();

        //Footer
        MakeButton("RESET TO DEFAULT", 40, CardH - 84, 380, 52, false, () =>
        {
            CrosshairSettings.ResetToDefaults();
            foreach (Action r in refreshers) r();
        });
        MakeButton("DONE", CardW - 40 - 220, CardH - 84, 220, 52, true, Close);
    }

    private void BuildPreview()
    {
        //Split background (dark | light) so the crosshair can be judged against both
        const float x = 40, y = 100, size = 380;
        RectMask2D mask = new GameObject("Preview", typeof(RectTransform)).AddComponent<RectMask2D>();
        RectTransform box = mask.rectTransform;
        box.SetParent(card, false);
        Place(box, x, y, size, size);
        Image dark = UIFactory.MakeImage(box, Theme.Square, Theme.Hex("#121317"), Vector2.zero);
        dark.rectTransform.anchorMin = Vector2.zero;
        dark.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        dark.rectTransform.offsetMin = dark.rectTransform.offsetMax = Vector2.zero;
        Image light = UIFactory.MakeImage(box, Theme.Square, Theme.Hex("#A8AEB8"), Vector2.zero);
        light.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        light.rectTransform.anchorMax = Vector2.one;
        light.rectTransform.offsetMin = light.rectTransform.offsetMax = Vector2.zero;

        //Shown at 2x so small sizes are easy to see; true size is in the corner
        CrosshairView zoomed = CrosshairView.Create(box);
        zoomed.transform.localScale = Vector3.one * 2f;
        CrosshairView actual = CrosshairView.Create(box);
        RectTransform at = (RectTransform)actual.transform;
        at.anchorMin = at.anchorMax = new Vector2(0f, 0f);
        at.anchoredPosition = new Vector2(48, 48);

        Label(card, "PREVIEW  2×", x, y + size + 10, 200, 24, 16, false, Theme.TextMuted, TextAlignmentOptions.Left).characterSpacing = 4;
        Label(card, "ACTUAL SIZE BOTTOM-LEFT", x + size - 260, y + size + 10, 260, 24, 16, false, Theme.TextMuted, TextAlignmentOptions.Right).characterSpacing = 2;
    }

    private void BuildControls()
    {
        int row = 0;
        ColorRow(row++);
        SliderRow(row++, "OPACITY", 0f, 1f, false, () => CrosshairSettings.opacity, v => CrosshairSettings.opacity = v, Pct);

        SwitchRow(row++, "OUTLINE", () => CrosshairSettings.outline, v => CrosshairSettings.outline = v);
        outlineRows.Add(SliderRow(row++, "OUTLINE THICKNESS", 1, 3, true, () => CrosshairSettings.outlineThickness, v => CrosshairSettings.outlineThickness = (int)v, Px));
        outlineRows.Add(SliderRow(row++, "OUTLINE OPACITY", 0f, 1f, false, () => CrosshairSettings.outlineOpacity, v => CrosshairSettings.outlineOpacity = v, Pct));

        SwitchRow(row++, "CENTER DOT", () => CrosshairSettings.dot, v => CrosshairSettings.dot = v);
        dotRows.Add(SliderRow(row++, "DOT SIZE", 1, 10, true, () => CrosshairSettings.dotSize, v => CrosshairSettings.dotSize = (int)v, Px));

        SwitchRow(row++, "INNER LINES", () => CrosshairSettings.lines, v => CrosshairSettings.lines = v);
        lineRows.Add(SliderRow(row++, "LINE LENGTH", 1, 20, true, () => CrosshairSettings.lineLength, v => CrosshairSettings.lineLength = (int)v, Px));
        lineRows.Add(SliderRow(row++, "LINE THICKNESS", 1, 10, true, () => CrosshairSettings.lineThickness, v => CrosshairSettings.lineThickness = (int)v, Px));
        lineRows.Add(SliderRow(row++, "LINE GAP", 0, 20, true, () => CrosshairSettings.lineGap, v => CrosshairSettings.lineGap = (int)v, Px));

        refreshers.Add(UpdateDependentRows);
        UpdateDependentRows();
    }

    //---------------- Rows ----------------

    private RectTransform Row(int index, string label, out CanvasGroup group)
    {
        RectTransform rt = new GameObject(label, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(card, false);
        Place(rt, 0, RowsTop + index * RowH, CardW, RowH);
        group = rt.gameObject.AddComponent<CanvasGroup>();
        TextMeshProUGUI t = Label(rt, label, LabelX, 0, ControlX - LabelX - 10, RowH, 16, false, Theme.TextMuted, TextAlignmentOptions.Left);
        t.characterSpacing = 2;
        return rt;
    }

    private CanvasGroup SliderRow(int index, string label, float min, float max, bool whole,
        Func<float> get, Action<float> set, Func<float, string> format)
    {
        RectTransform row = Row(index, label, out CanvasGroup group);
        Slider slider = UIFactory.MakeSlider(row, new Vector2(ControlW, 20), min, max, whole, get());
        Place((RectTransform)slider.transform, ControlX, (RowH - 20) / 2, ControlW, 20);
        TextMeshProUGUI value = Label(row, format(get()), ValueX, 0, 48, RowH, 18, true, Theme.Text, TextAlignmentOptions.Right);
        slider.onValueChanged.AddListener(v =>
        {
            set(v);
            value.text = format(v);
            CrosshairSettings.NotifyChanged();
        });
        refreshers.Add(() =>
        {
            slider.SetValueWithoutNotify(get());
            value.text = format(get());
        });
        return group;
    }

    private void SwitchRow(int index, string label, Func<bool> get, Action<bool> set)
    {
        RectTransform row = Row(index, label, out _);
        UISwitch sw = UISwitch.Create(row, get());
        Place((RectTransform)sw.transform, ControlX, (RowH - 28) / 2, 52, 28);
        sw.Changed += v =>
        {
            set(v);
            UpdateDependentRows();
            CrosshairSettings.NotifyChanged();
        };
        refreshers.Add(() => sw.Value = get());
    }

    private void ColorRow(int index)
    {
        RectTransform row = Row(index, "COLOR", out _);
        const float size = 30, gap = 8;
        for (int i = 0; i < CrosshairSettings.Presets.Length; i++)
        {
            Color c = CrosshairSettings.Presets[i];
            float x = ControlX + i * (size + gap);
            //Selection ring sits behind the swatch
            Image ring = Box(row, x - 4, (RowH - size) / 2 - 4, size + 8, size + 8, Color.white);
            ring.sprite = Theme.Ring;
            swatchRings.Add(ring);
            Image swatch = Box(row, x, (RowH - size) / 2, size, size, c);
            swatch.sprite = Theme.Circle;
            swatch.raycastTarget = true;
            Button b = swatch.gameObject.AddComponent<Button>();
            b.targetGraphic = swatch;
            b.onClick.AddListener(() =>
            {
                CrosshairSettings.color = c;
                UpdateSwatches();
                CrosshairSettings.NotifyChanged();
            });
            swatch.gameObject.AddComponent<ButtonFeedback>();
        }
        refreshers.Add(UpdateSwatches);
        UpdateSwatches();
    }

    private void UpdateSwatches()
    {
        for (int i = 0; i < swatchRings.Count; i++)
        {
            bool selected = ColorsMatch(CrosshairSettings.Presets[i], CrosshairSettings.color);
            swatchRings[i].enabled = selected;
        }
    }

    private static bool ColorsMatch(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 0.01f && Mathf.Abs(a.g - b.g) < 0.01f && Mathf.Abs(a.b - b.b) < 0.01f;
    }

    //Fade out and lock rows whose feature is switched off
    private void UpdateDependentRows()
    {
        SetRows(outlineRows, CrosshairSettings.outline);
        SetRows(dotRows, CrosshairSettings.dot);
        SetRows(lineRows, CrosshairSettings.lines);
    }

    private static void SetRows(List<CanvasGroup> rows, bool on)
    {
        foreach (CanvasGroup g in rows)
        {
            g.alpha = on ? 1f : 0.35f;
            g.interactable = on;
            g.blocksRaycasts = on;
        }
    }

    private static string Pct(float v) => Mathf.RoundToInt(v * 100f) + "%";
    private static string Px(float v) => Mathf.RoundToInt(v).ToString();

    //---------------- Open / close ----------------

    private void Close()
    {
        CrosshairSettings.Save();
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void Update()
    {
        openT = Mathf.Min(1f, openT + Time.unscaledDeltaTime / 0.18f);
        float e = 1f - (1f - openT) * (1f - openT) * (1f - openT);
        card.localScale = Vector3.one * Mathf.Lerp(0.95f, 1f, e);
        if (Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    //---------------- Helpers (card space: top-left origin, y down) ----------------

    private static void Place(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
    }

    private static Image Box(Transform parent, float x, float y, float w, float h, Color color)
    {
        Image img = UIFactory.MakeImage(parent, null, color, Vector2.zero);
        Place(img.rectTransform, x, y, w, h);
        return img;
    }

    private static TextMeshProUGUI Label(Transform parent, string text, float x, float y, float w, float h,
        float size, bool bold, Color color, TextAlignmentOptions align)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        t.color = color;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        Place(t.rectTransform, x, y, w, h);
        return t;
    }

    private void MakeButton(string text, float x, float y, float w, float h, bool primary, Action onClick)
    {
        Image bg = Box(card, x, y, w, h, Color.white);
        bg.sprite = Theme.RoundedRect;
        bg.type = Image.Type.Sliced;
        bg.pixelsPerUnitMultiplier = 1.4f;
        bg.raycastTarget = true;
        Button b = bg.gameObject.AddComponent<Button>();
        ColorBlock cb = ColorBlock.defaultColorBlock;
        cb.normalColor = primary ? Theme.Accent : Theme.Surface;
        cb.highlightedColor = primary ? Theme.Accent.WithAlpha(0.85f) : Theme.SurfaceHover;
        cb.pressedColor = Theme.AccentPressed;
        cb.selectedColor = cb.normalColor;
        cb.fadeDuration = 0.08f;
        b.colors = cb;
        b.onClick.AddListener(() => onClick());
        bg.gameObject.AddComponent<ButtonFeedback>();
        TextMeshProUGUI label = Label(bg.rectTransform, text, 0, 0, w, h, 20, true, primary ? Theme.Background : Theme.Text, TextAlignmentOptions.Center);
        label.characterSpacing = 4;
    }
}
