using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//Modern results screen built on its own canvas: song header, grade + score on the left,
//a radar chart of the run in the middle, stat tiles and judgement breakdown on the right.
//The scene's original results UI is hidden but its GameOver script still runs (it saves stats).
//All layout is in a fixed 1920x1080 panel that is scaled to fit the window, so nothing overlaps.
public class ResultsScreen : MonoBehaviour
{
    private const float W = 1920f, H = 1080f;

    private RectTransform panel;
    private float t;

    //Animated pieces
    private CanvasGroup[] sections;
    private float[] sectionDelays;
    private Vector2[] sectionBase;
    private TextMeshProUGUI scoreValue;
    private int finalScore;
    private GameObject grade;
    private RectTransform gradeRect;
    private Image shockwave;
    private Color gradeColor;
    private GameObject newBest;
    private AudioSource sfx;

    //Timeline (seconds since the screen opened)
    private const float CountStart = 0.2f, CountTime = 1.2f;
    private const float SlamStart = CountStart + CountTime + 0.12f;
    private const float SlamDrop = 0.14f;
    private const float TickInterval = 0.05f;
    private float nextTick = CountStart;
    private float scorePulse;
    private bool slammed;
    private RadarChart radar;
    private RectTransform[] bars;
    private float[] barTargets;

    void Start()
    {
        GameOver results = GetComponent<GameOver>();
        HideOriginal(results);
        Build();
    }

    private static void HideOriginal(GameOver results)
    {
        Canvas original = results.GetComponent<Canvas>();
        if (original == null && results.scoreText != null) original = results.scoreText.canvas.rootCanvas;
        if (original != null) original.enabled = false;
    }

    //---------------- Layout ----------------

    private void Build()
    {
        Canvas canvas = UIFactory.OverlayCanvas("ResultsScreen", 10);
        canvas.GetComponent<CanvasScaler>().screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;
        canvas.gameObject.AddComponent<GraphicRaycaster>();
        sfx = Sfx.Source(canvas.gameObject);

        GameObject root = new GameObject("Panel", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        panel = (RectTransform)root.transform;
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(W, H);

        string song = SongSelect2.theSong ?? "";
        finalScore = ScoreKeeper.score;

        RectTransform header = Section("Header");
        RectTransform left = Section("Left");
        RectTransform middle = Section("Radar");
        RectTransform right = Section("Stats");
        RectTransform buttons = Section("Buttons");
        sectionDelays = new[] { 0f, 0.1f, 0.2f, 0.3f, 0.5f };

        BuildHeader(header, song);
        BuildLeft(left);
        BuildRadar(middle);
        BuildStats(right);
        BuildButtons(buttons);

        sections = new[] { header, left, middle, right, buttons }.ToCanvasGroups();
        sectionBase = new Vector2[sections.Length];
        for (int i = 0; i < sections.Length; i++)
        {
            sectionBase[i] = ((RectTransform)sections[i].transform).anchoredPosition;
            sections[i].alpha = 0f;
        }
        Update();
    }

    private RectTransform Section(string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(panel, false);
        RectTransform rt = (RectTransform)go.transform;
        UIFactory.Stretch(rt);
        go.AddComponent<CanvasGroup>();
        return rt;
    }

    private void BuildHeader(RectTransform s, string song)
    {
        if (LevelHolder.songImages.TryGetValue(song, out Sprite cover) && cover != null)
        {
            Image img = Box(s, "Cover", 72, 64, 160, 160, Color.white, cover);
            img.preserveAspect = true;
        }

        LevelNames.Split(song, out string artist, out string title);

        TextMeshProUGUI caption = Label(s, "RESULTS", 256, 74, 1100, 30, 20, false, Theme.TextMuted, TextAlignmentOptions.Left);
        caption.characterSpacing = 8;
        TextMeshProUGUI titleText = Label(s, title, 256, 104, 1100, 64, 52, true, Theme.Text, TextAlignmentOptions.Left);
        titleText.enableAutoSizing = true;
        titleText.fontSizeMin = 28;
        titleText.fontSizeMax = 52;
        titleText.overflowMode = TextOverflowModes.Ellipsis;
        Label(s, artist, 256, 172, 1100, 40, 28, false, Theme.TextMuted, TextAlignmentOptions.Left);

        Box(s, "Divider", 72, 256, W - 144, 2, Theme.Track);
    }

    private void BuildLeft(RectTransform s)
    {
        double p = ScoreKeeper.participation;
        string letter;
        Color color;
        if (p >= 100 && RunStats.judgements[RunStats.MissIdx] == 0) { letter = "SS"; color = Theme.Good; }
        else if (p >= 95) { letter = "S"; color = Theme.Good; }
        else if (p >= 90) { letter = "A"; color = Theme.Great; }
        else if (p >= 80) { letter = "B"; color = Theme.Perfect; }
        else if (p >= 70) { letter = "C"; color = Theme.Accent; }
        else { letter = "D"; color = Theme.Miss; }

        TextMeshProUGUI g = Label(s, letter, 72, 300, 520, 330, 300, true, color, TextAlignmentOptions.Center);
        g.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        g.rectTransform.anchoredPosition = new Vector2(72 + 260, -(300 + 165));
        grade = g.gameObject;
        gradeRect = g.rectTransform;
        gradeColor = color;
        grade.SetActive(false);

        //Ring that bursts out from the grade when it lands
        shockwave = UIFactory.MakeImage(s, Theme.Ring, Color.clear, Vector2.zero);
        shockwave.rectTransform.anchorMin = shockwave.rectTransform.anchorMax = new Vector2(0f, 1f);
        shockwave.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        shockwave.rectTransform.anchoredPosition = gradeRect.anchoredPosition;

        TextMeshProUGUI cap = Label(s, "SCORE", 72, 650, 520, 30, 22, false, Theme.TextMuted, TextAlignmentOptions.Center);
        cap.characterSpacing = 8;
        scoreValue = Label(s, "0", 72, 684, 520, 90, 80, true, Theme.Text, TextAlignmentOptions.Center);
        //Centre pivot so the tick pulse grows from the middle
        scoreValue.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        scoreValue.rectTransform.anchoredPosition = new Vector2(72 + 260, -(684 + 45));

        if (finalScore > RunStats.previousHighScore && finalScore > 0)
        {
            Image badge = Box(s, "NewBest", 72 + 260 - 110, 792, 220, 44, Theme.Accent, Theme.RoundedRect);
            badge.type = Image.Type.Sliced;
            badge.pixelsPerUnitMultiplier = 1.4f;
            TextMeshProUGUI txt = Label(badge.rectTransform, "NEW BEST", 0, 0, 220, 44, 22, true, Theme.Background, TextAlignmentOptions.Center);
            txt.characterSpacing = 6;
            badge.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            badge.rectTransform.anchoredPosition = new Vector2(72 + 260, -(792 + 22));
            newBest = badge.gameObject;
            newBest.SetActive(false);
        }
        else
        {
            Label(s, "BEST  " + Mathf.Max(RunStats.previousHighScore, finalScore).ToString("N0"), 72, 796, 520, 36, 22, false, Theme.TextMuted, TextAlignmentOptions.Center);
        }
    }

    private void BuildRadar(RectTransform s)
    {
        const float cx = 960, cy = 600, radius = 200;
        int hits = RunStats.reactionTimes.Count;
        float avg = RunStats.AverageReaction;
        float spread = RunStats.ReactionSpread;
        bool hasHolds = RunStats.holdRatios.Count > 0;
        int total = Spawner.totalNotes;

        //Each axis: score 0..1, value text, name
        float reactionScore = hits > 0 ? Mathf.Clamp01((1f - avg) / 0.85f) : 0f;
        float consistencyScore = hits > 1 ? Mathf.Clamp01(1f - spread / 0.4f) : (hits == 1 ? 1f : 0f);
        //Hit quality: points earned before the combo bonus, out of 500 per note.
        //(Comparing the real score to a full-combo maximum mostly measured combo length instead.)
        float scoreRatio = total > 0 ? Mathf.Clamp01(RunStats.baseScore / (500f * total)) : 0f;
        (float value, string text, string name)[] axes =
        {
            (RunStats.Accuracy, Pct(RunStats.Accuracy), "ACCURACY"),
            (reactionScore, hits > 0 ? Ms(avg) : "—", "REACTION"),
            (consistencyScore, hits > 1 ? "±" + Ms(spread) : "—", "CONSISTENCY"),
            (hasHolds ? RunStats.HoldAccuracy : 0f, hasHolds ? Pct(RunStats.HoldAccuracy) : "—", "HOLD"),
            (scoreRatio, Pct(scoreRatio), "SCORE"),
            ((float)ScoreKeeper.participation / 100f, ScoreKeeper.participation + "%", "PARTICIPATION"),
        };

        GameObject go = new GameObject("Radar", typeof(RectTransform));
        go.transform.SetParent(s, false);
        radar = go.AddComponent<RadarChart>();
        radar.raycastTarget = false;
        radar.fillColor = Theme.Perfect.WithAlpha(0.3f);
        radar.lineColor = Theme.Perfect;
        radar.values = new float[axes.Length];
        for (int i = 0; i < axes.Length; i++) radar.values[i] = axes[i].value;
        Place(radar.rectTransform, cx - radius, cy - radius, radius * 2, radius * 2);
        radar.SetProgress(0f);

        for (int i = 0; i < axes.Length; i++)
        {
            Vector2 dir = radar.AxisDirection(i);
            float lx = cx + dir.x * (radius + 62), ly = cy - dir.y * (radius + 62);
            string txt = "<b>" + axes[i].text + "</b>\n<size=18><color=#8E94A0>" + axes[i].name + "</color></size>";
            TextMeshProUGUI label = Label(s, txt, lx - 110, ly - 32, 220, 64, 30, false, Theme.Text, TextAlignmentOptions.Center);
            label.lineSpacing = -10;
        }
    }

    private void BuildStats(RectTransform s)
    {
        const float x0 = 1316, colW = 258, rowH = 104, gap = 16;
        int hits = RunStats.reactionTimes.Count;
        bool hasHolds = RunStats.holdRatios.Count > 0;
        (string name, string value)[] tiles =
        {
            ("ACCURACY", Pct(RunStats.Accuracy)),
            ("PARTICIPATION", ScoreKeeper.participation + "%"),
            ("AVG REACTION", hits > 0 ? Ms(RunStats.AverageReaction) : "—"),
            ("MAX COMBO", ScoreKeeper.highestCombo + "x"),
            ("HOLD ACCURACY", hasHolds ? Pct(RunStats.HoldAccuracy) : "—"),
            ("NOTES HIT", ScoreKeeper.noteHits + " / " + ScoreKeeper.notesGiven),
        };
        for (int i = 0; i < tiles.Length; i++)
        {
            float x = x0 + (i % 2) * (colW + gap);
            float y = 300 + (i / 2) * (rowH + gap);
            Image bg = Box(s, "Tile", x, y, colW, rowH, Theme.Surface, Theme.RoundedRect);
            bg.type = Image.Type.Sliced;
            bg.pixelsPerUnitMultiplier = 1.4f;
            TextMeshProUGUI name = Label(s, tiles[i].name, x + 20, y + 16, colW - 40, 24, 17, false, Theme.TextMuted, TextAlignmentOptions.Left);
            name.characterSpacing = 4;
            TextMeshProUGUI val = Label(s, tiles[i].value, x + 20, y + 44, colW - 40, 48, 38, true, Theme.Text, TextAlignmentOptions.Left);
            val.enableAutoSizing = true;
            val.fontSizeMin = 20;
            val.fontSizeMax = 38;
        }

        TextMeshProUGUI head = Label(s, "SCORE DISTRIBUTION", x0, 670, 532, 26, 18, false, Theme.TextMuted, TextAlignmentOptions.Left);
        head.characterSpacing = 6;

        string[] names = { "PERFECT", "GREAT", "GOOD", "HIT", "TOO EARLY", "MISS" };
        Color[] colors = { Theme.Perfect, Theme.Great, Theme.Good, Theme.Hit, Theme.Early, Theme.Miss };
        int max = 1;
        foreach (int j in RunStats.judgements) max = Mathf.Max(max, j);
        bars = new RectTransform[names.Length];
        barTargets = new float[names.Length];
        const float barX = x0 + 150, barW = 290;
        for (int i = 0; i < names.Length; i++)
        {
            float y = 706 + i * 40;
            Label(s, names[i], x0, y, 140, 36, 22, true, colors[i], TextAlignmentOptions.Left);
            Image track = Box(s, "Track", barX, y + 13, barW, 10, Theme.Track, Theme.RoundedRect);
            track.type = Image.Type.Sliced;
            track.pixelsPerUnitMultiplier = 3f;
            Image fill = Box(s, "Fill", barX, y + 13, 0, 10, colors[i], Theme.RoundedRect);
            fill.type = Image.Type.Sliced;
            fill.pixelsPerUnitMultiplier = 3f;
            bars[i] = fill.rectTransform;
            barTargets[i] = barW * RunStats.judgements[i] / max;
            Label(s, RunStats.judgements[i].ToString(), barX + barW + 12, y, 1848 - (barX + barW + 12), 36, 24, true, Theme.Text, TextAlignmentOptions.Right);
        }
    }

    private void BuildButtons(RectTransform s)
    {
        Button("RETRY", "Game", 580, true, s);
        Button("SONG SELECT", "SongSelect", 840, false, s);
        Button("MENU", "Menu", 1100, false, s);
    }

    private void Button(string text, string scene, float x, bool primary, RectTransform parent)
    {
        Image bg = Box(parent, text, x, 976, 240, 64, Color.white, Theme.RoundedRect);
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
        b.onClick.AddListener(() =>
        {
            ScoreKeeper.combo = 0;
            SceneManager.LoadScene(scene);
        });
        bg.gameObject.AddComponent<ButtonFeedback>();
        TextMeshProUGUI label = Label(bg.rectTransform, text, 0, 0, 240, 64, 24, true, primary ? Theme.Background : Theme.Text, TextAlignmentOptions.Center);
        label.characterSpacing = 4;
    }

    //---------------- Animation ----------------

    void Update()
    {
        if (sections == null) return;
        t += Time.unscaledDeltaTime;

        //Sections fade and rise in, staggered
        for (int i = 0; i < sections.Length; i++)
        {
            float k = Ease(Mathf.Clamp01((t - sectionDelays[i]) / 0.4f));
            sections[i].alpha = k;
            ((RectTransform)sections[i].transform).anchoredPosition = sectionBase[i] + new Vector2(0, -24f * (1f - k));
        }

        AnimateScore();
        AnimateGrade();
        if (newBest != null && t >= SlamStart + SlamDrop + 0.3f && !newBest.activeSelf)
        {
            newBest.SetActive(true);
        }

        radar.SetProgress(Ease(Mathf.Clamp01((t - 0.3f) / 0.8f)));

        for (int i = 0; i < bars.Length; i++)
        {
            float k = Ease(Mathf.Clamp01((t - 0.5f - i * 0.06f) / 0.6f));
            bars[i].sizeDelta = new Vector2(barTargets[i] * k, bars[i].sizeDelta.y);
        }
    }

    //Score counts up with a tick sound; each tick pulses the number bigger, rising in pitch to the end
    private void AnimateScore()
    {
        float p = Mathf.Clamp01((t - CountStart) / CountTime);
        scoreValue.text = Mathf.RoundToInt(finalScore * Ease(p)).ToString("N0");

        if (finalScore > 0 && t >= nextTick && t < CountStart + CountTime)
        {
            nextTick += TickInterval;
            scorePulse = 1f;
            sfx.pitch = 0.85f + 0.6f * p;
            sfx.PlayOneShot(Sfx.Tick, 0.5f);
        }
        scorePulse = Mathf.Max(0f, scorePulse - Time.unscaledDeltaTime / 0.09f);
        float s = 1f + 0.09f * scorePulse * scorePulse;
        scoreValue.rectTransform.localScale = new Vector3(s, s, 1f);
    }

    //Grade drops in from huge, slams down with a squash, screen shake, shockwave and thump
    private void AnimateGrade()
    {
        if (t < SlamStart) return;
        if (!grade.activeSelf) grade.SetActive(true);

        float since = t - SlamStart;
        TextMeshProUGUI text = grade.GetComponent<TextMeshProUGUI>();
        if (since < SlamDrop)
        {
            //Falling towards the screen: accelerates (ease-in), slight tilt that straightens out
            float k = since / SlamDrop;
            float e = k * k;
            float scale = Mathf.Lerp(3.2f, 1f, e);
            gradeRect.localScale = new Vector3(scale, scale, 1f);
            gradeRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-12f, 0f, e));
            text.alpha = Mathf.Clamp01(k * 2f);
            return;
        }

        if (!slammed)
        {
            slammed = true;
            sfx.pitch = 1f;
            sfx.PlayOneShot(Sfx.Slam, 1f);
        }

        //Impact: squash wide and short, then spring back
        float a = since - SlamDrop;
        float spring = Mathf.Exp(-a * 9f) * Mathf.Cos(a * 28f);
        gradeRect.localScale = new Vector3(1f + 0.18f * spring, 1f - 0.14f * spring, 1f);
        gradeRect.localRotation = Quaternion.identity;
        text.alpha = 1f;

        //Screen shake that dies away
        float shake = 16f * Mathf.Exp(-a * 10f);
        panel.anchoredPosition = shake > 0.3f
            ? new Vector2(UnityEngine.Random.Range(-shake, shake), UnityEngine.Random.Range(-shake, shake))
            : Vector2.zero;

        //Shockwave ring in the grade's colour
        float w = Mathf.Clamp01(a / 0.5f);
        float we = Ease(w);
        shockwave.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(260f, 760f, we);
        shockwave.color = gradeColor.WithAlpha(0.7f * (1f - w));
    }

    private static float Ease(float x) => 1f - (1f - x) * (1f - x) * (1f - x);

    //---------------- Helpers ----------------

    private static string Pct(float v) => Mathf.RoundToInt(v * 100f) + "%";
    private static string Ms(float seconds) => Mathf.RoundToInt(seconds * 1000f) + "ms";

    //Position a rect by its top-left corner in panel pixels (y grows downward)
    private static void Place(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
    }

    private static Image Box(Transform parent, string name, float x, float y, float w, float h, Color color, Sprite sprite = null)
    {
        Image img = UIFactory.MakeImage(parent, sprite, color, Vector2.zero);
        img.name = name;
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
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
        Place(t.rectTransform, x, y, w, h);
        return t;
    }
}

static class ResultsScreenExtensions
{
    public static CanvasGroup[] ToCanvasGroups(this RectTransform[] rts)
    {
        CanvasGroup[] groups = new CanvasGroup[rts.Length];
        for (int i = 0; i < rts.Length; i++) groups[i] = rts[i].GetComponent<CanvasGroup>();
        return groups;
    }
}
