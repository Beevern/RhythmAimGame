using System;
using UnityEngine;

//Crosshair customisation (FPS-style): colour, outline, centre dot and the four inner lines.
//Sizes are in pixels at 1080p. Saved in PlayerPrefs so they persist between levels and restarts.
public static class CrosshairSettings
{
    public static readonly Color[] Presets =
    {
        Theme.Hex("#00FF26"), //green (the game's original)
        Theme.Hex("#FFFFFF"),
        Theme.Hex("#FFE600"),
        Theme.Hex("#00F0FF"),
        Theme.Hex("#FF66AB"),
        Theme.Hex("#FF3B3B"),
        Theme.Hex("#B57CFF"),
    };

    public static Color color;
    public static float opacity;

    public static bool outline;
    public static int outlineThickness;
    public static float outlineOpacity;

    public static bool dot;
    public static int dotSize;

    public static bool lines;
    public static int lineLength;
    public static int lineThickness;
    public static int lineGap;

    //Fired whenever a value changes, so the in-game crosshair and preview redraw live
    public static event Action Changed;

    static CrosshairSettings()
    {
        Load();
    }

    public static void ResetToDefaults()
    {
        color = Presets[0];
        opacity = 1f;
        outline = true;
        outlineThickness = 1;
        outlineOpacity = 0.6f;
        dot = true;
        dotSize = 2;
        lines = true;
        lineLength = 6;
        lineThickness = 2;
        lineGap = 3;
        NotifyChanged();
    }

    public static void Load()
    {
        ResetToDefaults();
        if (ColorUtility.TryParseHtmlString("#" + PlayerPrefs.GetString("xh_color", ColorUtility.ToHtmlStringRGB(color)), out Color c)) color = c;
        opacity = PlayerPrefs.GetFloat("xh_opacity", opacity);
        outline = PlayerPrefs.GetInt("xh_outline", outline ? 1 : 0) == 1;
        outlineThickness = PlayerPrefs.GetInt("xh_outlineThickness", outlineThickness);
        outlineOpacity = PlayerPrefs.GetFloat("xh_outlineOpacity", outlineOpacity);
        dot = PlayerPrefs.GetInt("xh_dot", dot ? 1 : 0) == 1;
        dotSize = PlayerPrefs.GetInt("xh_dotSize", dotSize);
        lines = PlayerPrefs.GetInt("xh_lines", lines ? 1 : 0) == 1;
        lineLength = PlayerPrefs.GetInt("xh_lineLength", lineLength);
        lineThickness = PlayerPrefs.GetInt("xh_lineThickness", lineThickness);
        lineGap = PlayerPrefs.GetInt("xh_lineGap", lineGap);
        NotifyChanged();
    }

    public static void Save()
    {
        PlayerPrefs.SetString("xh_color", ColorUtility.ToHtmlStringRGB(color));
        PlayerPrefs.SetFloat("xh_opacity", opacity);
        PlayerPrefs.SetInt("xh_outline", outline ? 1 : 0);
        PlayerPrefs.SetInt("xh_outlineThickness", outlineThickness);
        PlayerPrefs.SetFloat("xh_outlineOpacity", outlineOpacity);
        PlayerPrefs.SetInt("xh_dot", dot ? 1 : 0);
        PlayerPrefs.SetInt("xh_dotSize", dotSize);
        PlayerPrefs.SetInt("xh_lines", lines ? 1 : 0);
        PlayerPrefs.SetInt("xh_lineLength", lineLength);
        PlayerPrefs.SetInt("xh_lineThickness", lineThickness);
        PlayerPrefs.SetInt("xh_lineGap", lineGap);
        PlayerPrefs.Save();
    }

    public static void NotifyChanged()
    {
        Changed?.Invoke();
    }
}
