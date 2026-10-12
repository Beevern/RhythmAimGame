using TMPro;
using UnityEngine;
using UnityEngine.UI;

//Turns the SettingsPanel prefab into a compact, opaque card centred on a dark scrim,
//with a soft scale/fade in whenever it opens. Elements are found by their prefab names.
public class SettingsCard : MonoBehaviour
{
    private const float CardW = 440f, CardH = 440f;
    private const float Pad = 24f;
    private const float OpenTime = 0.18f;

    private CanvasGroup group;
    private TMP_FontAsset labelFont;
    private RectTransform card;
    private float t;

    public static void Apply(Settings settings)
    {
        GameObject panel = settings.settingsMenu;
        if (panel == null || panel.GetComponent<SettingsCard>() != null) return;
        CreatePairedControls(settings);
        panel.AddComponent<SettingsCard>().Layout(settings);
    }

    //Gives sensitivity a slider and volume a text box by cloning the existing (already themed) controls.
    //Runs before Settings.Start, which wires both pairs together.
    //True in the Game scene (the settings panel is opened from the pause menu)
    private static bool InLevel => FindFirstObjectByType<ScoreKeeper>(FindObjectsInactive.Include) != null;

    private static void CreatePairedControls(Settings settings)
    {
        if (settings.sensSlider == null && settings.volSlider != null)
        {
            GameObject go = Instantiate(settings.volSlider.gameObject, settings.volSlider.transform.parent);
            go.name = "SensSlider";
            settings.sensSlider = go.GetComponent<Slider>();
            settings.sensSlider.onValueChanged = new Slider.SliderEvent();
        }
        if (settings.volField == null && settings.field != null)
        {
            GameObject go = Instantiate(settings.field, settings.field.transform.parent);
            go.name = "VolField";
            TMP_InputField f = go.GetComponent<TMP_InputField>();
            f.onValueChanged = new TMP_InputField.OnChangeEvent();
            f.onEndEdit = new TMP_InputField.SubmitEvent();
            f.onSubmit = new TMP_InputField.SubmitEvent();
            if (f.placeholder is TMP_Text ph) ph.text = "";
            settings.volField = f;
        }
        //Inside a level only: a second slider + box for this level's volume
        if (InLevel && settings.levelVolSlider == null && settings.volSlider != null && settings.volField != null)
        {
            GameObject sl = Instantiate(settings.volSlider.gameObject, settings.volSlider.transform.parent);
            sl.name = "LevelVolSlider";
            settings.levelVolSlider = sl.GetComponent<Slider>();
            settings.levelVolSlider.onValueChanged = new Slider.SliderEvent();
            GameObject fo = Instantiate(settings.volField.gameObject, settings.volField.transform.parent);
            fo.name = "LevelVolField";
            TMP_InputField lf = fo.GetComponent<TMP_InputField>();
            lf.onValueChanged = new TMP_InputField.OnChangeEvent();
            lf.onEndEdit = new TMP_InputField.SubmitEvent();
            lf.onSubmit = new TMP_InputField.SubmitEvent();
            settings.levelVolField = lf;
        }
        //Value boxes are short numbers, so centre them
        foreach (GameObject f in new[] { settings.field, settings.volField != null ? settings.volField.gameObject : null,
                                         settings.levelVolField != null ? settings.levelVolField.gameObject : null })
        {
            if (f == null) continue;
            TMP_InputField input = f.GetComponent<TMP_InputField>();
            if (input.textComponent != null) input.textComponent.alignment = TextAlignmentOptions.Center;
            if (input.placeholder is TMP_Text ph) { ph.text = ""; }
        }
    }

    private void Layout(Settings settings)
    {
        //Sizes are in units of the project's 800-wide reference canvas
        float k = UIFactory.Unit(this) * 1920f / 800f;
        //Font of the panel's own labels ("Volume", "Sensitivity"), reused for the rows added here
        RectTransform volLabel = Find("VolText");
        TMP_Text sample = volLabel != null ? volLabel.GetComponent<TMP_Text>() : null;
        labelFont = sample != null ? sample.font : null;
        //One extra row inside a level (Level volume)
        float extra = settings.levelVolSlider != null ? 50f : 0f;
        //The Lyrics row only exists while in-game lyrics are on (LyricsDisplay.InGame)
        float cardH = CardH + extra - (LyricsDisplay.InGame ? 0f : 50f);
        float left = -CardW / 2 + Pad, right = CardW / 2 - Pad, top = cardH / 2;

        //Darker, solid scrim behind everything
        Image scrim = GetComponent<Image>();
        if (scrim != null)
        {
            scrim.sprite = null;
            scrim.color = new Color(0f, 0f, 0f, 0.85f);
        }

        //Opaque card drawn behind the controls
        Image cardImg = UIFactory.MakeImage(transform, Theme.RoundedRect, Theme.Hex("#1D1F26"), Vector2.zero);
        cardImg.name = "Card";
        cardImg.type = Image.Type.Sliced;
        cardImg.pixelsPerUnitMultiplier = 14f / (12f * UIFactory.Unit(cardImg));
        cardImg.raycastTarget = true;
        cardImg.transform.SetAsFirstSibling();
        card = cardImg.rectTransform;
        Pin(card, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(CardW, cardH), k);

        //Accent strip along the top edge of the card
        Image strip = UIFactory.MakeImage(card, Theme.Square, Theme.Accent, Vector2.zero);
        strip.rectTransform.anchorMin = new Vector2(0, 1);
        strip.rectTransform.anchorMax = new Vector2(1, 1);
        strip.rectTransform.pivot = new Vector2(0.5f, 1);
        strip.rectTransform.sizeDelta = new Vector2(-2 * 12f * k, 3f * k);
        strip.rectTransform.anchoredPosition = Vector2.zero;

        Text("Title", new Vector2(0, 0.5f), new Vector2(left, top - 36), new Vector2(300, 40), 30, true, Theme.Text, TextAlignmentOptions.Left, k);

        RectTransform close = Find("CloseSettings");
        if (close != null)
        {
            Pin(close, new Vector2(0.5f, 0.5f), new Vector2(right - 16, top - 36), new Vector2(32, 32), k);
            TextMeshProUGUI x = close.GetComponentInChildren<TextMeshProUGUI>(true);
            if (x != null) Style(x, 18, true, Theme.Text, TextAlignmentOptions.Center, k);
        }

        Image divider = UIFactory.MakeImage(transform, Theme.Square, Theme.Track, Vector2.zero);
        Pin(divider.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, top - 66), new Vector2(CardW - 2 * Pad, 1), k);

        //Rows: label | slider | value box
        const float boxW = 64f, gap = 12f, labelW = 120f;
        float sliderRight = right - boxW - gap;
        float sliderW = sliderRight - (left + labelW + gap);
        float row1 = top - 106, row2 = top - 156 - extra;
        Text("VolText", new Vector2(0, 0.5f), new Vector2(left, row1), new Vector2(labelW, 32), 20, false, Theme.Text, TextAlignmentOptions.Left, k);
        if (settings.volSlider != null) Pin((RectTransform)settings.volSlider.transform, new Vector2(1, 0.5f), new Vector2(sliderRight, row1), new Vector2(sliderW, 16), k);
        if (settings.volField != null) Pin((RectTransform)settings.volField.transform, new Vector2(1, 0.5f), new Vector2(right, row1), new Vector2(boxW, 30), k);
        Text("SensText", new Vector2(0, 0.5f), new Vector2(left, row2), new Vector2(labelW, 32), 20, false, Theme.Text, TextAlignmentOptions.Left, k);
        if (settings.sensSlider != null) Pin((RectTransform)settings.sensSlider.transform, new Vector2(1, 0.5f), new Vector2(sliderRight, row2), new Vector2(sliderW, 16), k);
        if (settings.field != null) Pin((RectTransform)settings.field.transform, new Vector2(1, 0.5f), new Vector2(right, row2), new Vector2(boxW, 30), k);

        //Level volume row (in a level only), between Volume and Sensitivity
        if (settings.levelVolSlider != null)
        {
            float rowL = top - 156;
            TextMeshProUGUI lvText = new GameObject("LevelVolText", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            lvText.text = "Level volume";
            MatchFont(lvText);
            Pin(lvText.rectTransform, new Vector2(0, 0.5f), new Vector2(left, rowL), new Vector2(labelW, 32), k);
            Style(lvText, 18, false, Theme.Text, TextAlignmentOptions.Left, k);
            lvText.raycastTarget = false;
            Pin((RectTransform)settings.levelVolSlider.transform, new Vector2(1, 0.5f), new Vector2(sliderRight, rowL), new Vector2(sliderW, 16), k);
            if (settings.levelVolField != null) Pin((RectTransform)settings.levelVolField.transform, new Vector2(1, 0.5f), new Vector2(right, rowL), new Vector2(boxW, 30), k);
        }

        //Rows that open the full editors
        OpenerRow("Crosshair", top - 206 - extra, left, right, labelW, k, CrosshairEditor.Open);
        OpenerRow("Note colours", top - 256 - extra, left, right, labelW, k, NoteColorEditor.Open);
        //Lyrics + song name shown during levels that have them (see LyricsDisplay)
        if (LyricsDisplay.InGame)
            SwitchRow("Lyrics", top - 306 - extra, left, right, labelW, k, LyricsDisplay.Enabled, v => LyricsDisplay.Enabled = v);

        //"Work in progress" note becomes a quiet footer
        Text("Text (TMP)", new Vector2(0.5f, 0.5f), new Vector2(0, -cardH / 2 + 28), new Vector2(CardW - 2 * Pad, 24), 13, false, Theme.TextMuted, TextAlignmentOptions.Center, k);

        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
    }

    //Use the settings panel's label font (set before Style() so sizes apply to the right font)
    private void MatchFont(TMP_Text t)
    {
        if (labelFont == null) return;
        t.font = labelFont;
        t.fontSharedMaterial = labelFont.material;
    }

    //Label on the left, CUSTOMIZE button on the right that opens an editor panel
    private void OpenerRow(string label, float y, float left, float right, float labelW, float k, UnityEngine.Events.UnityAction open)
    {
        TextMeshProUGUI text = new GameObject(label + "Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        text.text = label;
        MatchFont(text);
        Pin(text.rectTransform, new Vector2(0, 0.5f), new Vector2(left, y), new Vector2(labelW + 40, 32), k);
        Style(text, 20, false, Theme.Text, TextAlignmentOptions.Left, k);
        text.raycastTarget = false;

        Image bg = UIFactory.MakeImage(transform, Theme.RoundedRect, Color.white, Vector2.zero);
        bg.name = label + "Button";
        bg.type = Image.Type.Sliced;
        bg.pixelsPerUnitMultiplier = 14f / (8f * UIFactory.Unit(bg));
        bg.raycastTarget = true;
        Pin(bg.rectTransform, new Vector2(1, 0.5f), new Vector2(right, y), new Vector2(150, 30), k);
        Button button = bg.gameObject.AddComponent<Button>();
        ColorBlock cb = ColorBlock.defaultColorBlock;
        cb.normalColor = Theme.Hex("#2A2D36");
        cb.highlightedColor = Theme.Hex("#363A45");
        cb.pressedColor = Theme.AccentPressed;
        cb.selectedColor = cb.normalColor;
        cb.fadeDuration = 0.08f;
        button.colors = cb;
        button.onClick.AddListener(open);
        bg.gameObject.AddComponent<ButtonFeedback>();
        TextMeshProUGUI btnText = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        btnText.rectTransform.SetParent(bg.rectTransform, false);
        UIFactory.Stretch(btnText.rectTransform);
        btnText.text = "CUSTOMIZE";
        MatchFont(btnText);
        Style(btnText, 14, true, Theme.Text, TextAlignmentOptions.Center, k);
        btnText.characterSpacing = 3;
        btnText.raycastTarget = false;
    }

    //Label on the left, on/off switch on the right
    private void SwitchRow(string label, float y, float left, float right, float labelW, float k, bool initial, System.Action<bool> changed)
    {
        TextMeshProUGUI text = new GameObject(label + "Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        text.text = label;
        MatchFont(text);
        Pin(text.rectTransform, new Vector2(0, 0.5f), new Vector2(left, y), new Vector2(labelW + 40, 32), k);
        Style(text, 20, false, Theme.Text, TextAlignmentOptions.Left, k);
        text.raycastTarget = false;

        UISwitch sw = UISwitch.Create(transform, initial);
        Pin((RectTransform)sw.transform, new Vector2(1, 0.5f), new Vector2(right, y), new Vector2(52, 28), k);
        sw.Changed += changed;
    }

    void OnEnable()
    {
        t = 0f;
    }

    void Update()
    {
        if (card == null) return;
        t = Mathf.Min(1f, t + Time.unscaledDeltaTime / OpenTime);
        float e = 1f - (1f - t) * (1f - t) * (1f - t);
        group.alpha = e;
        card.localScale = Vector3.one * Mathf.Lerp(0.95f, 1f, e);
    }

    private RectTransform Find(string name)
    {
        Transform tr = transform.Find(name);
        return tr != null ? (RectTransform)tr : null;
    }

    //Re-parent onto the card so everything scales with it during the open animation
    private void Pin(RectTransform rt, Vector2 pivot, Vector2 pos, Vector2 size, float k)
    {
        if (card != null && rt != card) rt.SetParent(card, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = pivot;
        rt.anchoredPosition = pos * k;
        rt.sizeDelta = size * k;
    }

    private void Text(string name, Vector2 pivot, Vector2 pos, Vector2 size, float fontSize, bool bold, Color color, TextAlignmentOptions align, float k)
    {
        RectTransform rt = Find(name);
        if (rt == null) return;
        Pin(rt, pivot, pos, size, k);
        TextMeshProUGUI tmp = rt.GetComponent<TextMeshProUGUI>();
        if (tmp != null) Style(tmp, fontSize, bold, color, align, k);
    }

    private static void Style(TextMeshProUGUI tmp, float fontSize, bool bold, Color color, TextAlignmentOptions align, float k)
    {
        tmp.enableAutoSizing = false;
        tmp.fontSize = fontSize * k;
        tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.color = color;
        tmp.alignment = align;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
    }
}
