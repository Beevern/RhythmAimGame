using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;

//Shows the level's lyrics (lyrics.json in the level folder, made by Tools/auto_chart.py) during
//the game: the song name + artist at the top, and the current lyric line near the bottom,
//highlighted word by word in the level's style:
//  fill  - each word fills with colour as it's sung
//  box   - a box sits behind the word being sung
//  plain - just the line
//Uses the same clock as the notes and DrumHits, so it pauses with the game.
//Players can turn it off in Settings (LyricsDisplay.Enabled).
public class LyricsDisplay : MonoBehaviour
{
    [Serializable] class Word { public string text; public float start; public float end; }
    [Serializable] class Line { public float start; public float end; public Word[] words; }
    [Serializable] class Lyrics { public string title; public string artist; public string style; public Line[] lines; }

    private const string Pref = "lyrics.show";
    private const float Fade = 0.15f;
    private static readonly string FillColor = "#FFD23F";
    private static readonly string BoxColor = "#3D8BFFCC";

    //Master switch for in-game lyrics. Off = lyrics come from the lyric video instead
    //(Level Maker "LYRICS: Video"). Set true to bring back in-game lyrics: the game shows
    //lyrics.json again, Settings gets its Lyrics switch, and the Level Maker offers "In game".
    public static readonly bool InGame = false;

    public static bool Enabled
    {
        get => PlayerPrefs.GetInt(Pref, 1) == 1;
        set { PlayerPrefs.SetInt(Pref, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    private Lyrics lyrics;
    private Canvas canvas;
    private TextMeshProUGUI title, line;
    private CanvasGroup lineGroup, titleGroup;
    private int current = -1;
    private string shown = "";

    void Start()
    {
        if (!LevelHolder.levels.TryGetValue(SongSelect2.theSong ?? "", out LevelInfo level) || level.lyricsPath == null)
        {
            enabled = false;
            return;
        }
        try
        {
            lyrics = JsonUtility.FromJson<Lyrics>(File.ReadAllText(level.lyricsPath));
        }
        catch (Exception e)
        {
            Debug.LogWarning("Couldn't read lyrics " + level.lyricsPath + ": " + e.Message);
        }
        if (lyrics == null || lyrics.lines == null)
        {
            enabled = false;
            return;
        }
        Build();
    }

    private void Build()
    {
        canvas = UIFactory.OverlayCanvas("Lyrics", 1);
        canvas.GetComponent<UnityEngine.UI.CanvasScaler>().matchWidthOrHeight = 1f;

        //Song name + artist: a small band at the very top, above the HUD
        string header = "";
        if (!string.IsNullOrWhiteSpace(lyrics.title)) header += "<b>" + Escape(lyrics.title) + "</b>";
        if (!string.IsNullOrWhiteSpace(lyrics.artist))
            header += (header.Length > 0 ? "\n" : "") + "<size=70%><color=#B4B8C0>" + Escape(lyrics.artist) + "</color></size>";
        title = MakeText("Title", header, 34, new Vector2(0.5f, 1f), new Vector2(0, -24), new Vector2(1400, 110));
        title.alignment = TextAlignmentOptions.Top;
        titleGroup = title.gameObject.AddComponent<CanvasGroup>();

        //Current lyric line, near the bottom
        line = MakeText("Line", "", 56, new Vector2(0.5f, 0f), new Vector2(0, 120), new Vector2(1700, 90));
        line.fontStyle = FontStyles.Bold;
        line.alignment = TextAlignmentOptions.Bottom;
        lineGroup = line.gameObject.AddComponent<CanvasGroup>();
        lineGroup.alpha = 0f;
        titleGroup.alpha = 0f;
    }

    private TextMeshProUGUI MakeText(string name, string text, float size, Vector2 anchor, Vector2 pos, Vector2 box)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = Theme.Text;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.overflowMode = TextOverflowModes.Overflow;
        //Dark outline so lyrics read over any video
        t.outlineWidth = 0.18f;
        t.outlineColor = new Color32(0, 0, 0, 220);
        RectTransform rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = box;
        return t;
    }

    void Update()
    {
        bool visible = Enabled && !Pause.isPaused && playMusic.VideoStarted;
        canvas.enabled = visible;
        if (!visible) return;

        float now = DrumHits.SongTime;
        titleGroup.alpha = Mathf.Clamp01(now / 0.8f);

        //The line on screen now (lines are in order and don't overlap)
        if (current < 0 || now < lyrics.lines[current].start || now >= lyrics.lines[current].end)
        {
            current = -1;
            for (int i = 0; i < lyrics.lines.Length; i++)
            {
                if (now >= lyrics.lines[i].start && now < lyrics.lines[i].end) { current = i; break; }
            }
        }
        if (current < 0)
        {
            lineGroup.alpha = 0f;
            return;
        }
        Line l = lyrics.lines[current];
        lineGroup.alpha = Mathf.Clamp01(Mathf.Min((now - l.start) / Fade, (l.end - now) / Fade));

        string text = Render(l, now);
        if (text != shown)
        {
            shown = text;
            line.text = text;
        }
    }

    //The line as TMP rich text, highlighted up to `now`
    private string Render(Line l, float now)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < l.words.Length; i++)
        {
            Word w = l.words[i];
            if (i > 0) sb.Append(' ');
            string text = Escape(w.text);
            switch (lyrics.style)
            {
                case "plain":
                    sb.Append(text);
                    break;
                case "box":
                    if (now >= w.start && now < w.end) sb.Append("<mark=").Append(BoxColor).Append(" padding=\"12,12,4,4\">").Append(text).Append("</mark>");
                    else sb.Append(text);
                    break;
                default: //fill: sung letters change colour, sweeping across the word being sung
                    float k = w.end > w.start ? Mathf.Clamp01((now - w.start) / (w.end - w.start)) : (now >= w.start ? 1f : 0f);
                    int n = Mathf.RoundToInt(k * w.text.Length);
                    if (n > 0) sb.Append("<color=").Append(FillColor).Append('>').Append(Escape(w.text.Substring(0, n))).Append("</color>");
                    sb.Append(Escape(w.text.Substring(n)));
                    break;
            }
        }
        return sb.ToString();
    }

    //Lyrics are plain text: stop "<" being read as a rich text tag
    private static string Escape(string s) => s.Replace("<", "<noparse><</noparse>");

    void OnDestroy()
    {
        if (canvas != null) Destroy(canvas.gameObject);
    }
}
