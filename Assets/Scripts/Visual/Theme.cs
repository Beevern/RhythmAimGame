using System;
using System.Collections.Generic;
using UnityEngine;

//Central palette + procedurally generated sprites/fonts for the minimal look.
//Everything is generated at runtime so no art assets need to be hand-made.
public static class Theme
{
    //Palette
    public static readonly Color Background = Hex("#101115");
    public static readonly Color BackgroundTop = Hex("#181A20");
    public static readonly Color Surface = Hex("#1D1F26", 0.92f);
    public static readonly Color SurfaceHover = Hex("#2A2D36", 0.95f);
    public static readonly Color Field = Hex("#0B0C0F", 0.85f);
    public static readonly Color Track = new Color(1f, 1f, 1f, 0.12f);
    public static readonly Color Overlay = new Color(0f, 0f, 0f, 0.78f);
    public static readonly Color Text = Hex("#F2F3F5");
    public static readonly Color TextMuted = Hex("#8E94A0");
    public static readonly Color Accent = Hex("#FF66AB");
    public static readonly Color AccentPressed = Hex("#D94F90");
    //Note colours (player-customisable, see NoteColors): the target ring, approach discs further
    //down the queue, the next note to appear, held notes, and the next held note
    public static Color Note => NoteColors.Get(NoteColors.Kind.Target);
    public static Color UpcomingNote => NoteColors.Get(NoteColors.Kind.Upcoming);
    public static Color NextNote => NoteColors.Get(NoteColors.Kind.Next);
    public static Color HeldNote => NoteColors.Get(NoteColors.Kind.Held);
    public static Color NextHeld => NoteColors.Get(NoteColors.Kind.NextHeld);

    //Judgement colours
    public static readonly Color Perfect = Hex("#66CCFF");
    public static readonly Color Great = Hex("#8CE36B");
    public static readonly Color Good = Hex("#FFD15C");
    public static readonly Color Hit = Hex("#C9CDD4");
    public static readonly Color Early = Hex("#FF9F43");
    public static readonly Color Miss = Hex("#FF5A5F");

    //Score thresholds mirror ScoreDesc
    public static Color JudgementColor(int score)
    {
        if (score >= 475) return Perfect;
        if (score >= 400) return Great;
        if (score >= 300) return Good;
        if (score >= 1) return Hit;
        return Miss;
    }

    public static string JudgementLabel(int score)
    {
        if (score >= 475) return "PERFECT";
        if (score >= 400) return "GREAT";
        if (score >= 300) return "GOOD";
        if (score >= 1) return "HIT";
        return "MISS";
    }

    public static Color Hex(string hex, float alpha = 1f)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        c.a = alpha;
        return c;
    }

    public static Color WithAlpha(this Color c, float a)
    {
        c.a = a;
        return c;
    }

    //---------------- Sprites ----------------
    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    //9-sliced rounded rectangle for UI panels/buttons
    public static Sprite RoundedRect => Cached("rounded", () =>
    {
        const int size = 64, radius = 14;
        Texture2D tex = MakeTexture(size, size, (x, y) =>
        {
            float px = Mathf.Abs(x - size * 0.5f) - (size * 0.5f - radius);
            float py = Mathf.Abs(y - size * 0.5f) - (size * 0.5f - radius);
            float outside = new Vector2(Mathf.Max(px, 0), Mathf.Max(py, 0)).magnitude;
            float inside = Mathf.Min(Mathf.Max(px, py), 0);
            return Mathf.Clamp01(0.5f - (outside + inside - radius));
        });
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(radius + 2, radius + 2, radius + 2, radius + 2));
    });

    //Plain white square (for filled progress bars)
    public static Sprite Square => Cached("square", () =>
    {
        Texture2D tex = MakeTexture(4, 4, (x, y) => 1f);
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
    });

    //Anti-aliased disc. World size is 1 unit across.
    public static Sprite Circle => Cached("circle", () => Disc(256, 0f));

    //Ring with thickness given as a fraction of the radius. World size is 1 unit across.
    public static Sprite Ring => Cached("ring", () => Disc(256, 0.88f));
    public static Sprite ThinRing => Cached("thinring", () => Disc(256, 0.94f));

    //Soft blurred blob for backgrounds
    public static Sprite SoftBlob => Cached("blob", () =>
    {
        const int size = 128;
        Texture2D tex = MakeTexture(size, size, (x, y) =>
        {
            float d = new Vector2(x - size * 0.5f, y - size * 0.5f).magnitude / (size * 0.5f);
            float a = Mathf.Clamp01(1f - d);
            return a * a * (3f - 2f * a);
        });
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    });

    //Vertical gradient, white at top fading to transparent at bottom
    public static Sprite VerticalGradient => Cached("vgrad", () =>
    {
        Texture2D tex = MakeTexture(2, 64, (x, y) => y / 63f);
        tex.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(tex, new Rect(0, 0, 2, 64), new Vector2(0.5f, 0.5f), 100f);
    });

    private static Sprite Disc(int size, float innerFraction)
    {
        float r = size * 0.5f - 1f;
        float inner = r * innerFraction;
        Texture2D tex = MakeTexture(size, size, (x, y) =>
        {
            float d = new Vector2(x - size * 0.5f, y - size * 0.5f).magnitude;
            float a = Mathf.Clamp01(r - d + 0.5f);
            if (innerFraction > 0f) a *= Mathf.Clamp01(d - inner + 0.5f);
            return a;
        });
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Texture2D MakeTexture(int w, int h, Func<float, float, float> alphaAt)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Color32[] px = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                //Sample at pixel centres
                byte a = (byte)(Mathf.Clamp01(alphaAt(x + 0.5f, y + 0.5f)) * 255f);
                px[y * w + x] = new Color32(255, 255, 255, a);
            }
        }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        return tex;
    }

    private static Sprite Cached(string key, Func<Sprite> make)
    {
        if (!cache.TryGetValue(key, out Sprite s) || s == null)
        {
            s = make();
            s.name = "Theme_" + key;
            cache[key] = s;
        }
        return s;
    }

    //Built-in sprite material (Sprites/Default), grabbed from a SpriteRenderer so it's always in the build
    private static Material spriteMaterial;
    public static Material SpriteMaterial
    {
        get
        {
            if (spriteMaterial == null)
            {
                GameObject tmp = new GameObject("tmp");
                spriteMaterial = tmp.AddComponent<SpriteRenderer>().sharedMaterial;
                UnityEngine.Object.Destroy(tmp);
            }
            return spriteMaterial;
        }
    }

}
