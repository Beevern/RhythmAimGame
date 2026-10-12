using System.Collections.Generic;
using TMPro;
using UnityEngine;

//The game's fonts (LiberationSans, Bubblegum) don't have symbols like ★ ▶ ❚ ✓ ← →, so TextMeshPro
//showed a box (□) and logged a warning. This adds Windows' own symbol fonts as global fallbacks at
//startup, so any missing character is borrowed from them, in every font, in the editor and in builds.
public static class GlyphFallback
{
    //Segoe UI Symbol covers stars, shapes and arrows; Segoe UI covers general punctuation
    static readonly string[] Families = { "Segoe UI Symbol", "Segoe UI" };
    static readonly List<TMP_FontAsset> added = new List<TMP_FontAsset>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        if (added.Count > 0) return;
        List<TMP_FontAsset> list = TMP_Settings.fallbackFontAssets;
        if (list == null)
        {
            list = new List<TMP_FontAsset>();
            TMP_Settings.fallbackFontAssets = list;
        }
        foreach (string family in Families)
        {
            TMP_FontAsset font = null;
            try
            {
                font = TMP_FontAsset.CreateFontAsset(family, "Regular");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("GlyphFallback: couldn't load " + family + ": " + e.Message);
            }
            if (font == null) continue;
            font.name = family + " (fallback)";
            list.Add(font);
            added.Add(font);
        }
        //These live only for this run: take them back out of TMP's settings asset when Play stops,
        //so the editor never tries to save runtime-made fonts into it
        Application.quitting += () =>
        {
            foreach (TMP_FontAsset f in added) TMP_Settings.fallbackFontAssets?.Remove(f);
            added.Clear();
        };
    }
}
