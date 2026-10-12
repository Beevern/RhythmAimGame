using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//The Level Editor's UI. Build() creates it; the result can be saved into LevelEditor.unity
//(Tools > Rebuild Level Editor UI) so it's visible and editable before Play. At runtime
//LevelEditor finds this component and wires up the behaviour.
//
//Layout is a fixed 1920x1080 panel (top-left origin, y down) scaled to fit the window.
public class LevelEditorView : MonoBehaviour
{
    public const float W = 1920f, H = 1080f;
    public const float MidX = 424f, MidW = 960f;
    public const float RightX = 1404f, RightW = 492f;
    public const float PreviewY = 90f, PreviewH = 540f;
    public const float ZoomBefore = 2f, ZoomAfter = 4f;
    public const int ListRows = 26;
    public const float RowH = 26f;
    public static readonly float[] NudgeSteps = { -0.05f, -0.01f, 0.01f, 0.05f };

    [Header("Top bar")]
    public Button back;
    public Button save;
    public TextMeshProUGUI statusText;

    [Header("Levels")]
    public RectTransform levelListContent;

    [Header("Preview")]
    public RawImage videoImage;
    public RectTransform previewNotes;

    [Header("Transport")]
    public Button play;
    public Button speed;
    public Button sound;
    public TextMeshProUGUI playLabel, speedLabel, soundLabel, timeText;

    [Header("Timelines")]
    public RectTransform fullTimeline;
    public RectTransform playheadLine;
    public RectTransform zoomStrip;
    public RectTransform zoomPlayhead;
    public TimelineInput fullTimelineInput;
    public TimelineInput zoomInput;

    [Header("Selected note")]
    public TextMeshProUGUI selectedText;
    public TMP_InputField timeField, endField, labelField;
    public Button[] nudge;
    public Button snap, addTap, addHold, delete, undo;

    [Header("Level info")]
    public RawImage coverImage;
    public TMP_InputField nameField, offsetField, volumeField;
    public Button replaceCover, replaceVideo, audioBars;
    public TextMeshProUGUI audioBarsLabel;
    public TextMeshProUGUI videoNameText, statsText;

    [Header("Note list")]
    public RectTransform listArea;
    public Slider listScroll;
    public Image[] rowBg;
    public Button[] rowButton;
    public TextMeshProUGUI[] rowIdx, rowTime, rowDelta, rowType, rowWord, rowLog;

    //---------------------------------------------------------------- build

    public static LevelEditorView Build()
    {
        Canvas canvas = UIFactory.OverlayCanvas("LevelEditor UI", 10);
        canvas.GetComponent<CanvasScaler>().screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;
        canvas.gameObject.AddComponent<GraphicRaycaster>();
        canvas.gameObject.AddComponent<NoTheme>();
        LevelEditorView v = canvas.gameObject.AddComponent<LevelEditorView>();

        //Background so the layout reads the same in the Scene view as in game
        UIFactory.FullScreenImage(canvas.transform, Theme.Background);

        var panel = new GameObject("Panel", typeof(RectTransform)).GetComponent<RectTransform>();
        panel.SetParent(canvas.transform, false);
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(W, H);

        v.BuildTopBar(panel);
        v.BuildLevelList(panel);
        v.BuildPreview(panel);
        v.BuildTransport(panel);
        v.BuildTimelines(panel);
        v.BuildEditPanel(panel);
        v.BuildInfoPanel(panel);
        v.BuildNoteList(panel);

        ThemeSprite.TagAll(canvas.transform);
        return v;
    }

    void BuildTopBar(RectTransform p)
    {
        back = MakeButton(p, "BACK", 24, 20, 120, 44, false, out _);
        Label(p, "LEVEL EDITOR", 164, 20, 400, 44, 30, true, Theme.Text, TextAlignmentOptions.Left).characterSpacing = 4;
        statusText = Label(p, "", 700, 20, 980, 44, 20, false, Theme.TextMuted, TextAlignmentOptions.Right);
        save = MakeButton(p, "SAVE", W - 24 - 200, 20, 200, 44, true, out _);
        Box(p, 24, 76, W - 48, 2, Theme.Track);
    }

    void BuildLevelList(RectTransform p)
    {
        Label(p, "LEVELS", 24, 90, 380, 28, 18, false, Theme.TextMuted, TextAlignmentOptions.Left).characterSpacing = 6;
        var view = new GameObject("Levels", typeof(RectTransform)).GetComponent<RectTransform>();
        view.SetParent(p, false);
        Place(view, 24, 124, 380, 932);
        view.gameObject.AddComponent<RectMask2D>();
        Image catcher = view.gameObject.AddComponent<Image>();
        catcher.color = Color.clear;   //lets the scroll wheel work over gaps between rows

        levelListContent = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
        levelListContent.SetParent(view, false);
        levelListContent.anchorMin = new Vector2(0, 1);
        levelListContent.anchorMax = new Vector2(1, 1);
        levelListContent.pivot = new Vector2(0.5f, 1);

        ScrollRect sr = view.gameObject.AddComponent<ScrollRect>();
        sr.content = levelListContent;
        sr.horizontal = false;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 30f;
    }

    void BuildPreview(RectTransform p)
    {
        Image frame = Box(p, MidX, PreviewY, MidW, PreviewH, Color.black);
        frame.name = "Preview";
        frame.gameObject.AddComponent<RectMask2D>();
        videoImage = new GameObject("Video", typeof(RectTransform)).AddComponent<RawImage>();
        videoImage.rectTransform.SetParent(frame.transform, false);
        videoImage.color = new Color(0.55f, 0.55f, 0.55f, 1f);   //dimmed like in game
        videoImage.raycastTarget = false;
        UIFactory.Stretch(videoImage.rectTransform);

        previewNotes = new GameObject("Notes", typeof(RectTransform)).GetComponent<RectTransform>();
        previewNotes.SetParent(frame.transform, false);
        UIFactory.Stretch(previewNotes);
    }

    void BuildTransport(RectTransform p)
    {
        float y = PreviewY + PreviewH + 12;
        play = MakeButton(p, "▶ PLAY", MidX, y, 140, 44, true, out playLabel);
        timeText = Label(p, "0:00.000", MidX + 156, y, 360, 44, 22, true, Theme.Text, TextAlignmentOptions.Left);
        speed = MakeButton(p, "1x SPEED", MidX + MidW - 330, y, 150, 44, false, out speedLabel);
        sound = MakeButton(p, "HIT SOUNDS ON", MidX + MidW - 170, y, 170, 44, false, out soundLabel);
    }

    void BuildTimelines(RectTransform p)
    {
        float y = PreviewY + PreviewH + 68;
        Label(p, "WHOLE SONG", MidX, y, 200, 20, 14, false, Theme.TextMuted, TextAlignmentOptions.Left).characterSpacing = 4;
        Image full = Box(p, MidX, y + 22, MidW, 44, Theme.Field, Theme.Square);
        full.name = "Whole Song";
        full.raycastTarget = true;
        fullTimeline = full.rectTransform;
        fullTimelineInput = full.gameObject.AddComponent<TimelineInput>();
        playheadLine = Box(fullTimeline, 0, 0, 2, 44, Color.white, Theme.Square).rectTransform;
        playheadLine.name = "Playhead";

        float zy = y + 80;
        Label(p, "ZOOM  ·  click a note to select, drag to move, click empty space to seek", MidX, zy, MidW, 20, 14, false, Theme.TextMuted, TextAlignmentOptions.Left).characterSpacing = 2;
        Image zoom = Box(p, MidX, zy + 22, MidW, 96, Theme.Field, Theme.Square);
        zoom.name = "Zoom";
        zoom.raycastTarget = true;
        zoom.gameObject.AddComponent<RectMask2D>();
        zoomStrip = zoom.rectTransform;
        zoomInput = zoom.gameObject.AddComponent<TimelineInput>();
        //One faint line per second
        for (int i = 0; i <= ZoomBefore + ZoomAfter; i++)
            Box(zoomStrip, i * MidW / (ZoomBefore + ZoomAfter), 0, 1, 96, Color.white.WithAlpha(0.06f), Theme.Square);
        zoomPlayhead = Box(zoomStrip, ZoomBefore / (ZoomBefore + ZoomAfter) * MidW - 1, 0, 2, 96, Color.white, Theme.Square).rectTransform;
        zoomPlayhead.name = "Playhead";
    }

    void BuildEditPanel(RectTransform p)
    {
        float y = PreviewY + PreviewH + 270;
        selectedText = Label(p, "No note selected", MidX, y, 300, 40, 20, true, Theme.Text, TextAlignmentOptions.Left);
        Label(p, "TIME", MidX + 300, y - 2, 60, 40, 14, false, Theme.TextMuted, TextAlignmentOptions.Left);
        timeField = MakeInput(p, MidX + 350, y, 120, 40, "");
        nudge = new Button[NudgeSteps.Length];
        float bx = MidX + 482;
        for (int i = 0; i < NudgeSteps.Length; i++)
        {
            float d = NudgeSteps[i];
            nudge[i] = MakeButton(p, (d > 0 ? "+" : "−") + Mathf.RoundToInt(Mathf.Abs(d) * 1000) + "ms", bx, y, 82, 40, false, out _);
            bx += 88;
        }
        snap = MakeButton(p, "TO PLAYHEAD", bx, y, 130, 40, false, out _);

        float y2 = y + 52;
        addTap = MakeButton(p, "+ TAP (N)", MidX, y2, 140, 40, true, out _);
        addHold = MakeButton(p, "+ HOLD (H)", MidX + 148, y2, 150, 40, true, out _);
        Label(p, "HOLD END", MidX + 312, y2 - 2, 90, 40, 14, false, Theme.TextMuted, TextAlignmentOptions.Left);
        endField = MakeInput(p, MidX + 392, y2, 110, 40, "");
        Label(p, "WORD", MidX + 514, y2 - 2, 60, 40, 14, false, Theme.TextMuted, TextAlignmentOptions.Left);
        labelField = MakeInput(p, MidX + 566, y2, 170, 40, "");
        delete = MakeButton(p, "DELETE", MidX + MidW - 210, y2, 100, 40, false, out _);
        undo = MakeButton(p, "UNDO", MidX + MidW - 102, y2, 102, 40, false, out _);

        Label(p, "Space/K play · J/L ±2s · N tap · H hold · Del delete · ←/→ nudge 10ms (Shift 50ms) · ↑/↓ prev/next · Ctrl+Z undo · Ctrl+S save",
            MidX, y2 + 52, MidW, 24, 14, false, Theme.TextMuted, TextAlignmentOptions.Left);
    }

    void BuildInfoPanel(RectTransform p)
    {
        coverImage = new GameObject("Cover", typeof(RectTransform)).AddComponent<RawImage>();
        coverImage.rectTransform.SetParent(p, false);
        Place(coverImage.rectTransform, RightX, 90, 150, 150);
        coverImage.color = Theme.Surface;
        coverImage.raycastTarget = false;

        float x = RightX + 166, w = RightW - 166;
        Label(p, "NAME  (Song by Artist)", x, 86, w, 20, 13, false, Theme.TextMuted, TextAlignmentOptions.Left);
        nameField = MakeInput(p, x, 106, w, 40, "");
        replaceCover = MakeButton(p, "REPLACE COVER", x, 154, w / 2 - 4, 38, false, out _);
        replaceVideo = MakeButton(p, "REPLACE VIDEO", x + w / 2 + 4, 154, w / 2 - 4, 38, false, out _);
        videoNameText = Label(p, "", x, 198, w, 22, 14, false, Theme.TextMuted, TextAlignmentOptions.Left);
        videoNameText.overflowMode = TextOverflowModes.Ellipsis;

        Label(p, "OFFSET (s)", RightX, 250, 90, 36, 13, false, Theme.TextMuted, TextAlignmentOptions.Left);
        offsetField = MakeInput(p, RightX + 78, 250, 86, 36, "");
        Label(p, "VOLUME %", RightX + 176, 250, 80, 36, 13, false, Theme.TextMuted, TextAlignmentOptions.Left);
        volumeField = MakeInput(p, RightX + 246, 250, 70, 36, "");
        audioBars = MakeButton(p, "AUDIO BARS ON", RightX + 326, 250, RightW - 326, 36, false, out audioBarsLabel);
        //Taps / holds / difficulty, on the NOTES header line
        statsText = Label(p, "", RightX + 150, 300, RightW - 150, 24, 15, false, Theme.Text, TextAlignmentOptions.Right);
    }

    void BuildNoteList(RectTransform p)
    {
        float y = 300;
        Label(p, "NOTES", RightX, y, 200, 24, 18, false, Theme.TextMuted, TextAlignmentOptions.Left).characterSpacing = 6;
        y += 30;
        HeaderCell(p, "#", RightX + 6, y, 44);
        HeaderCell(p, "TIME", RightX + 50, y, 90);
        HeaderCell(p, "Δ NEXT", RightX + 144, y, 80);
        HeaderCell(p, "TYPE", RightX + 228, y, 70);
        HeaderCell(p, "WORD", RightX + 300, y, 100);
        HeaderCell(p, "MISS", RightX + 404, y, 64);
        y += 26;

        Image area = Box(p, RightX, y, RightW - 20, ListRows * RowH, Theme.Field, Theme.Square);
        area.name = "Note List";
        area.raycastTarget = true;
        listArea = area.rectTransform;

        rowBg = new Image[ListRows];
        rowButton = new Button[ListRows];
        rowIdx = new TextMeshProUGUI[ListRows];
        rowTime = new TextMeshProUGUI[ListRows];
        rowDelta = new TextMeshProUGUI[ListRows];
        rowType = new TextMeshProUGUI[ListRows];
        rowWord = new TextMeshProUGUI[ListRows];
        rowLog = new TextMeshProUGUI[ListRows];
        for (int i = 0; i < ListRows; i++)
        {
            Image bg = Box(listArea, 0, i * RowH, RightW - 20, RowH, Color.clear, Theme.Square);
            bg.name = "Row " + (i + 1);
            bg.raycastTarget = true;
            Button b = bg.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            rowBg[i] = bg;
            rowButton[i] = b;
            rowIdx[i] = Label(bg.transform, "", 6, 0, 44, RowH, 15, false, Theme.TextMuted, TextAlignmentOptions.Left);
            rowTime[i] = Label(bg.transform, "", 50, 0, 90, RowH, 15, false, Theme.Text, TextAlignmentOptions.Left);
            rowDelta[i] = Label(bg.transform, "", 144, 0, 80, RowH, 15, false, Theme.Text, TextAlignmentOptions.Left);
            rowType[i] = Label(bg.transform, "", 228, 0, 70, RowH, 15, false, Theme.Text, TextAlignmentOptions.Left);
            rowWord[i] = Label(bg.transform, "", 300, 0, 100, RowH, 15, false, Theme.TextMuted, TextAlignmentOptions.Left);
            rowWord[i].overflowMode = TextOverflowModes.Ellipsis;
            //Hit log: how often this note is missed across all runs
            rowLog[i] = Label(bg.transform, "", 404, 0, 64, RowH, 15, false, Theme.TextMuted, TextAlignmentOptions.Left);
        }

        //Vertical scrollbar to the right of the list (a slider turned on its side)
        listScroll = UIFactory.MakeSlider(p, new Vector2(ListRows * RowH, 14), 0, 1, true, 0);
        listScroll.name = "Note List Scroll";
        RectTransform st = (RectTransform)listScroll.transform;
        st.anchorMin = st.anchorMax = new Vector2(0, 1);
        st.pivot = new Vector2(0, 1);
        st.localRotation = Quaternion.Euler(0, 0, -90);
        st.anchoredPosition = new Vector2(RightX + RightW - 2, -y);
    }

    static void HeaderCell(Transform p, string text, float x, float y, float w)
    {
        Label(p, text, x, y, w, 24, 13, false, Theme.TextMuted, TextAlignmentOptions.Left).characterSpacing = 3;
    }

    //---------------------------------------------------------------- helpers (also used at runtime)

    public static void Place(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
    }

    public static Image Box(Transform parent, float x, float y, float w, float h, Color color, Sprite sprite = null)
    {
        Image img = UIFactory.MakeImage(parent, sprite, color, Vector2.zero);
        Place(img.rectTransform, x, y, w, h);
        return img;
    }

    public static TextMeshProUGUI Label(Transform parent, string text, float x, float y, float w, float h,
        float size, bool bold, Color color, TextAlignmentOptions align)
    {
        var go = new GameObject(string.IsNullOrEmpty(text) ? "Text" : text, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
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

    public static ColorBlock Colors(Color normal, Color hover)
    {
        ColorBlock cb = ColorBlock.defaultColorBlock;
        cb.normalColor = normal;
        cb.highlightedColor = hover;
        cb.pressedColor = Theme.AccentPressed;
        cb.selectedColor = normal;
        cb.disabledColor = normal.WithAlpha(0.4f);
        cb.fadeDuration = 0.08f;
        return cb;
    }

    public static Button MakeButton(Transform parent, string text, float x, float y, float w, float h, bool primary, out TextMeshProUGUI label)
    {
        Image bg = Box(parent, x, y, w, h, Color.white, Theme.RoundedRect);
        bg.name = text;
        bg.type = Image.Type.Sliced;
        bg.pixelsPerUnitMultiplier = 1.6f;
        bg.raycastTarget = true;
        var b = bg.gameObject.AddComponent<Button>();
        b.colors = Colors(primary ? Theme.Accent : Theme.Surface, primary ? Theme.Accent.WithAlpha(0.85f) : Theme.SurfaceHover);
        bg.gameObject.AddComponent<ButtonFeedback>();
        label = Label(bg.transform, text, 0, 0, w, h, Mathf.Min(18, h * 0.42f), true, primary ? Theme.Background : Theme.Text, TextAlignmentOptions.Center);
        label.characterSpacing = 2;
        return b;
    }

    public static TMP_InputField MakeInput(Transform parent, float x, float y, float w, float h, string text)
    {
        Image bg = Box(parent, x, y, w, h, Color.white, Theme.RoundedRect);
        bg.name = "Input";
        bg.type = Image.Type.Sliced;
        bg.pixelsPerUnitMultiplier = 1.6f;
        bg.raycastTarget = true;

        var area = new GameObject("Text Area", typeof(RectTransform)).GetComponent<RectTransform>();
        area.SetParent(bg.transform, false);
        area.anchorMin = Vector2.zero;
        area.anchorMax = Vector2.one;
        area.offsetMin = new Vector2(10, 2);
        area.offsetMax = new Vector2(-10, -2);
        area.gameObject.AddComponent<RectMask2D>();

        TextMeshProUGUI t = Label(area, "", 0, 0, 10, 10, Mathf.Min(19, h * 0.48f), false, Theme.Text, TextAlignmentOptions.Left);
        UIFactory.Stretch(t.rectTransform);
        t.alignment = TextAlignmentOptions.MidlineLeft;

        //Add the input field while inactive so it's fully wired before it first enables
        bg.gameObject.SetActive(false);
        var f = bg.gameObject.AddComponent<TMP_InputField>();
        f.textViewport = area;
        f.textComponent = t;
        f.colors = Colors(Theme.Field, Theme.Field.WithAlpha(1f));
        f.caretColor = Theme.Accent;
        f.customCaretColor = true;
        f.selectionColor = Theme.Accent.WithAlpha(0.35f);
        f.text = text;
        bg.gameObject.SetActive(true);
        return f;
    }
}
