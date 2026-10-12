using TMPro;
using UnityEngine;
using UnityEngine.UI;

//Main menu art: a "living note" behind the title that fills like an approach note and bursts on
//every beat, plus a strip of audio bars along the bottom. The bars follow whatever audio is playing;
//with no music they ripple out from each beat instead. Drawn on its own canvas behind the menu UI.
public class MenuLogo : MonoBehaviour
{
    private const float BeatInterval = 1.6f;
    private const int BarCount = 56;
    private const float BarMaxHeight = 150f;

    private RectTransform canvasRect;
    private Canvas canvas;
    private TMP_Text title;

    private RectTransform note;
    private RectTransform glow;
    private RectTransform fill;
    private RectTransform rim;
    private Image burst;
    private Image flash;
    private float beatTime;
    //1 right after a beat, decaying to 0
    private float beatEnergy;

    private RectTransform[] bars;
    private float[] levels;
    private readonly float[] spectrum = new float[512];

    public static void Create()
    {
        if (FindFirstObjectByType<MenuLogo>() != null) return;
        Canvas canvas = UIFactory.OverlayCanvas("MenuLogo", -50);
        MenuLogo logo = canvas.gameObject.AddComponent<MenuLogo>();
        logo.canvas = canvas;
        logo.canvasRect = (RectTransform)canvas.transform;
        logo.Build();
    }

    private void Build()
    {
        //The menu's title text (the settings panel has its own "Title", so skip that one)
        foreach (TMP_Text text in FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
        {
            if (text.name == "Title" && text.GetComponentInParent<SettingsCard>(true) == null) title = text;
        }

        //Living note
        note = new GameObject("Note", typeof(RectTransform)).GetComponent<RectTransform>();
        note.SetParent(transform, false);
        glow = UIFactory.MakeImage(note, Theme.SoftBlob, Theme.Note.WithAlpha(0.14f), Vector2.one).rectTransform;
        fill = UIFactory.MakeImage(note, Theme.Circle, Theme.Note.WithAlpha(0.28f), Vector2.one).rectTransform;
        rim = UIFactory.MakeImage(note, Theme.Ring, Color.white.WithAlpha(0.55f), Vector2.one).rectTransform;
        flash = UIFactory.MakeImage(note, Theme.Circle, Color.clear, Vector2.one);
        burst = UIFactory.MakeImage(note, Theme.Ring, Color.clear, Vector2.one);
        //Children are sized as multiples of the note, which is scaled to the title
        foreach (RectTransform child in note)
        {
            child.anchorMin = child.anchorMax = new Vector2(0.5f, 0.5f);
        }

        //Audio bars along the bottom edge
        bars = new RectTransform[BarCount];
        levels = new float[BarCount];
        for (int i = 0; i < BarCount; i++)
        {
            Image bar = UIFactory.MakeImage(transform, Theme.Square, Color.white.WithAlpha(0.13f), Vector2.zero);
            RectTransform rt = bar.rectTransform;
            rt.anchorMin = new Vector2((float)i / BarCount, 0f);
            rt.anchorMax = new Vector2((float)(i + 1) / BarCount, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(-6f, 4f);
            bars[i] = rt;
        }
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        beatTime += dt;
        if (beatTime >= BeatInterval)
        {
            beatTime -= BeatInterval;
            beatEnergy = 1f;
        }
        beatEnergy = Mathf.Max(0f, beatEnergy - dt * 2.2f);

        UpdateNote();
        UpdateBars(dt);
    }

    private void UpdateNote()
    {
        //Follow the title (it lives on a different canvas), sized to its text height
        float diameter = 260f;
        if (title != null && title.gameObject.activeInHierarchy)
        {
            Bounds b = title.textBounds;
            Vector3 centreWorld = title.transform.TransformPoint(b.center);
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, centreWorld);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local))
            {
                note.anchoredPosition = local;
            }
            float textHeightPx = b.size.y * title.transform.lossyScale.y;
            diameter = Mathf.Clamp(textHeightPx / canvas.scaleFactor * 2.6f, 180f, 440f);
        }
        else
        {
            note.anchoredPosition = new Vector2(0f, 160f);
        }

        //Approach disc fills the rim over each beat, then bursts
        float p = beatTime / BeatInterval;
        float punch = 1f + 0.06f * beatEnergy * beatEnergy;
        fill.sizeDelta = Vector2.one * diameter * p;
        rim.sizeDelta = Vector2.one * diameter * punch;
        glow.sizeDelta = Vector2.one * diameter * 2.3f;

        burst.rectTransform.sizeDelta = Vector2.one * diameter * Mathf.Lerp(1f, 1.55f, 1f - beatEnergy * beatEnergy);
        burst.color = Theme.Perfect.WithAlpha(0.75f * beatEnergy * beatEnergy);
        flash.rectTransform.sizeDelta = Vector2.one * diameter;
        flash.color = Color.white.WithAlpha(0.22f * Mathf.Pow(beatEnergy, 4f));
        rim.GetComponent<Image>().color = Color.white.WithAlpha(0.55f + 0.35f * beatEnergy);
    }

    private void UpdateBars(float dt)
    {
        AudioListener.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);
        float total = 0f;
        for (int i = 0; i < spectrum.Length; i++) total += spectrum[i];
        bool hasAudio = total > 0.0005f;
        float t = Time.unscaledTime;

        for (int i = 0; i < BarCount; i++)
        {
            float target;
            if (hasAudio)
            {
                //Log-spaced frequency bands, boosted towards the highs so they're visible
                int lo = Mathf.FloorToInt(Mathf.Pow(spectrum.Length, (float)i / BarCount));
                int hi = Mathf.Max(lo + 1, Mathf.FloorToInt(Mathf.Pow(spectrum.Length, (float)(i + 1) / BarCount)));
                float v = 0f;
                for (int j = lo; j < hi && j < spectrum.Length; j++) v = Mathf.Max(v, spectrum[j]);
                target = Mathf.Clamp01(Mathf.Sqrt(v) * 2.5f * (1f + 2f * i / BarCount));
            }
            else
            {
                //Idle: a soft wave plus a ripple that spreads out from the centre on each beat
                float fromCentre = Mathf.Abs(i - (BarCount - 1) * 0.5f) / (BarCount * 0.5f);
                float ripple = Mathf.Max(0f, 1f - Mathf.Abs(fromCentre - (1f - beatEnergy)) * 4f) * beatEnergy;
                float wave = 0.5f + 0.5f * Mathf.Sin(i * 0.38f + t * 1.6f);
                target = 0.05f + 0.07f * wave + 0.35f * ripple;
            }
            //Fast rise, slow fall
            levels[i] = target > levels[i] ? Mathf.Lerp(levels[i], target, 1f - Mathf.Exp(-30f * dt)) : Mathf.Max(target, levels[i] - dt * 0.9f);
            bars[i].sizeDelta = new Vector2(-6f, 4f + levels[i] * BarMaxHeight);
        }
    }
}
