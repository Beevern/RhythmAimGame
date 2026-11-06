using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using V = LevelEditorView;

//Polish for the tool screens (Level Editor, Level Maker), added at runtime only so nothing
//saved in the scenes changes: card panels behind each section, an entrance animation,
//and the editor's accent playhead with a "played" fill on the whole-song timeline.
public class ScreenDecor : MonoBehaviour
{
    private const float IntroTime = 0.45f, IntroSlide = 28f;

    private class Intro
    {
        public RectTransform rt;
        public CanvasGroup group;
        public Vector2 home;
        public float delay;
    }

    private readonly List<Intro> intros = new List<Intro>();
    private float t;

    //Editor: the played part of the whole-song timeline follows the playhead
    private RectTransform playhead, playedFill;

    public static ScreenDecor On(GameObject go)
    {
        ScreenDecor fx = go.GetComponent<ScreenDecor>();
        return fx != null ? fx : go.AddComponent<ScreenDecor>();
    }

    //Fades the element in while it slides up into place, starting after `delay` seconds
    public void AddIntro(RectTransform rt, float delay)
    {
        CanvasGroup g = rt.GetComponent<CanvasGroup>();
        if (g == null) g = rt.gameObject.AddComponent<CanvasGroup>();
        g.alpha = 0f;
        intros.Add(new Intro { rt = rt, group = g, home = rt.anchoredPosition, delay = delay });
        rt.anchoredPosition = rt.anchoredPosition - new Vector2(0f, IntroSlide);
    }

    //Rounded panel with a thin accent strip along its top edge (top-left origin, y down)
    public static Image Card(Transform parent, float x, float y, float w, float h)
    {
        Image card = V.Box(parent, x, y, w, h, Theme.Hex("#1A1C22", 0.88f), Theme.RoundedRect);
        card.name = "Card";
        card.type = Image.Type.Sliced;
        card.pixelsPerUnitMultiplier = 1.2f;
        V.Box(card.transform, 14, 0, w - 28, 3, Theme.Accent.WithAlpha(0.85f), Theme.Square).name = "Accent";
        return card;
    }

    //---------------------------------------------------------------- Level Editor

    public static void DecorateEditor(V v)
    {
        RectTransform panel = (RectTransform)v.back.transform.parent;
        if (panel.Find("Decor") != null) return;
        ScreenDecor fx = On(v.gameObject);

        //Let the menu's drifting backdrop show through the editor's flat background
        MenuBackdrop.Create();
        Transform canvas = panel.parent;
        for (int i = 0; i < canvas.childCount; i++)
        {
            Transform c = canvas.GetChild(i);
            if (c == panel) break;
            Image flat = c.GetComponent<Image>();
            if (flat != null && flat.sprite == null) flat.color = Theme.Background.WithAlpha(0.55f);
        }

        //Cards sit behind everything else in the panel
        RectTransform decor = new GameObject("Decor", typeof(RectTransform)).GetComponent<RectTransform>();
        decor.SetParent(panel, false);
        decor.SetAsFirstSibling();
        V.Place(decor, 0, 0, V.W, V.H);

        //Top bar band with an accent underline
        V.Box(decor, 0, 0, V.W, 76, Theme.Hex("#15161B", 0.92f), Theme.Square).name = "Top Bar";
        V.Box(decor, 0, 76, V.W, 2, Theme.Accent.WithAlpha(0.6f), Theme.Square).name = "Top Bar Line";

        //Levels | controls under the preview | level info + notes
        Card(decor, 14, 86, 400, 982);
        Card(decor, 418, 636, 976, 432);
        Card(decor, 1398, 86, 508, 982);

        //Soft outline around the video preview
        Image outline = V.Box(decor, V.MidX - 3, V.PreviewY - 3, V.MidW + 6, V.PreviewH + 6, Theme.Track, Theme.RoundedRect);
        outline.name = "Preview Outline";
        outline.type = Image.Type.Sliced;
        outline.pixelsPerUnitMultiplier = 3f;

        //Accent playheads with a knob, and the played part of the song filled in
        Image played = V.Box(v.fullTimeline, 0, 0, 0, v.fullTimeline.sizeDelta.y, Theme.Accent.WithAlpha(0.14f), Theme.Square);
        played.name = "Played";
        played.transform.SetAsFirstSibling();
        fx.playedFill = played.rectTransform;
        fx.playhead = v.playheadLine;
        Playhead(v.playheadLine, -7);
        Playhead(v.zoomPlayhead, 0);

        fx.AddIntro(panel, 0f);
    }

    private static void Playhead(RectTransform line, float knobY)
    {
        if (line == null) return;
        Image img = line.GetComponent<Image>();
        if (img != null) img.color = Theme.Accent;
        V.Box(line, -5, knobY, 12, 12, Theme.Accent, Theme.Circle).name = "Knob";
    }

    //---------------------------------------------------------------- per frame

    void Update()
    {
        if (playhead != null && playedFill != null)
            playedFill.sizeDelta = new Vector2(Mathf.Max(0f, playhead.anchoredPosition.x), playedFill.sizeDelta.y);

        if (intros.Count == 0) return;
        t += Time.unscaledDeltaTime;
        bool done = true;
        foreach (Intro i in intros)
        {
            float p = Mathf.Clamp01((t - i.delay) / IntroTime);
            if (p < 1f) done = false;
            float e = 1f - (1f - p) * (1f - p) * (1f - p);
            if (i.rt == null) continue;
            i.group.alpha = e;
            i.rt.anchoredPosition = i.home - new Vector2(0f, IntroSlide * (1f - e));
        }
        if (done) intros.Clear();
    }
}
