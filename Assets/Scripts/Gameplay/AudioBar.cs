using UnityEngine;

//In-game audio visualiser along the bottom of the play area.
//Log-spaced frequency bands across the whole spectrum, mirrored around a centre line,
//smoothed (fast rise, slow fall) with falling peak caps and a bass pulse that brightens everything.
public class AudioBar : MonoBehaviour
{
    public AudioSource audioSource;    // The audio source to visualize
    public GameObject barPrefab;       // No longer used (bars are sprites now); kept so the scene reference stays valid

    private const int BarCount = 64;
    private const float Left = -8.6f, Right = 8.6f;   // World x range (matches the old layout)
    private const float CentreY = -3.9f;               // Bars grow up and down from here
    private const float MaxHalfHeight = 0.85f;
    private const float MinHalfHeight = 0.025f;
    private const float BarFill = 0.62f;               // Bar width as a fraction of the spacing
    private const float PeakHold = 0.35f;              // Seconds a peak cap waits before falling
    private const float PeakFall = 1.2f;               // Peak fall speed (in 0..1 level per second)

    private readonly float[] spectrum = new float[1024];
    private SpriteRenderer[] bars;
    private SpriteRenderer[] peaks;
    private float[] levels;
    private float[] peakLevels;
    private float[] peakTimers;
    private int[] bandLo, bandHi;
    private Color[] colors;
    //Bar centres in this object's local space (it may be the camera, which turns as the player aims)
    private Vector3[] basePos;
    private float bassPulse;

    void Start()
    {
        //Some levels turn the visualiser off (level.json "hideAudioBars")
        if (LevelHolder.levels.TryGetValue(SongSelect2.theSong ?? "", out LevelInfo level) && level.hideAudioBars)
        {
            enabled = false;
            return;
        }
        bars = new SpriteRenderer[BarCount];
        peaks = new SpriteRenderer[BarCount * 2];
        levels = new float[BarCount];
        peakLevels = new float[BarCount];
        peakTimers = new float[BarCount];
        bandLo = new int[BarCount];
        bandHi = new int[BarCount];
        colors = new Color[BarCount];
        basePos = new Vector3[BarCount];

        float spacing = (Right - Left) / BarCount;
        //Usable range: ~23Hz to ~16kHz at 48kHz output with 1024 bins
        const int firstBin = 1, lastBin = 700;
        for (int i = 0; i < BarCount; i++)
        {
            bandLo[i] = Mathf.FloorToInt(firstBin * Mathf.Pow((float)lastBin / firstBin, (float)i / BarCount));
            bandHi[i] = Mathf.Max(bandLo[i] + 1, Mathf.FloorToInt(firstBin * Mathf.Pow((float)lastBin / firstBin, (float)(i + 1) / BarCount)));
            //Blue on the left fading to purple on the right, matching the note colours
            colors[i] = Color.Lerp(Theme.Note, Theme.NextHeld, (float)i / (BarCount - 1));

            //Laid out in world space as before, then kept relative to this object
            basePos[i] = transform.InverseTransformPoint(new Vector3(Left + spacing * (i + 0.5f), CentreY, 0f));
            bars[i] = MakeSprite("Bar", basePos[i], spacing * BarFill, 1);
            peaks[i * 2] = MakeSprite("PeakTop", basePos[i], spacing * BarFill, 2);
            peaks[i * 2 + 1] = MakeSprite("PeakBottom", basePos[i], spacing * BarFill, 2);
        }
    }

    private SpriteRenderer MakeSprite(string name, Vector3 localPos, float width, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = new Vector3(width / 0.04f, 1f, 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        //Theme.Square is 4px at 100ppu = 0.04 units, so x scale above gives the exact width
        sr.sprite = Theme.Square;
        sr.sortingOrder = order;
        return sr;
    }

    void Update()
    {
        if (audioSource == null || bars == null) return;
        float dt = Time.deltaTime;
        audioSource.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        //Bass energy drives a brightness pulse
        float bass = 0f;
        for (int j = 1; j < 8; j++) bass = Mathf.Max(bass, spectrum[j]);
        bassPulse = Mathf.Max(Mathf.Clamp01(Mathf.Sqrt(bass) * 2.2f), bassPulse - dt * 2.5f);

        for (int i = 0; i < BarCount; i++)
        {
            float v = 0f;
            for (int j = bandLo[i]; j < bandHi[i]; j++) v = Mathf.Max(v, spectrum[j]);
            //sqrt compresses the range; highs are much quieter so they get boosted
            float target = Mathf.Clamp01(Mathf.Sqrt(v) * 2.4f * (1f + 2.5f * i / BarCount));

            //Fast rise, slow fall
            levels[i] = target > levels[i]
                ? Mathf.Lerp(levels[i], target, 1f - Mathf.Exp(-35f * dt))
                : Mathf.Max(target, levels[i] - dt * 1.6f);

            //Peak caps hold briefly at the highest level, then fall
            if (levels[i] >= peakLevels[i])
            {
                peakLevels[i] = levels[i];
                peakTimers[i] = PeakHold;
            }
            else if ((peakTimers[i] -= dt) <= 0f)
            {
                peakLevels[i] = Mathf.Max(levels[i], peakLevels[i] - dt * PeakFall);
            }

            float half = Mathf.Lerp(MinHalfHeight, MaxHalfHeight, levels[i]);
            Transform bt = bars[i].transform;
            bt.localScale = new Vector3(bt.localScale.x, half * 2f / 0.04f, 1f);
            bars[i].color = colors[i].WithAlpha(0.22f + 0.25f * levels[i] + 0.2f * bassPulse);

            float peakHalf = Mathf.Lerp(MinHalfHeight, MaxHalfHeight, peakLevels[i]) + 0.03f;
            Color peakColor = Color.white.WithAlpha(0.25f + 0.35f * peakLevels[i]);
            PlacePeak(peaks[i * 2], basePos[i] + Vector3.up * peakHalf, peakColor);
            PlacePeak(peaks[i * 2 + 1], basePos[i] - Vector3.up * peakHalf, peakColor);
        }
    }

    private static void PlacePeak(SpriteRenderer sr, Vector3 localPos, Color c)
    {
        Transform t = sr.transform;
        t.localPosition = localPos;
        t.localScale = new Vector3(t.localScale.x, 0.02f / 0.04f, 1f);
        sr.color = c;
    }
}
