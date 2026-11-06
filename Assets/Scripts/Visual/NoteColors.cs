using System;
using UnityEngine;

//Player-chosen note colours (Settings > Note colours). Saved in PlayerPrefs.
//Theme.Note / UpcomingNote / NextNote / HeldNote / NextHeld read from here.
public static class NoteColors
{
    public enum Kind { Target, Upcoming, Next, Held, NextHeld }

    public static readonly string[] Labels = { "TARGET RING", "UPCOMING NOTES", "NEXT NOTE", "HOLD NOTES", "NEXT HOLD" };

    //Defaults stay off the judgement colours (cyan/green/yellow/orange/red) so colour on screen
    //always means "how did I hit"; the next note stands out by being brighter, not by hue
    public static readonly Color[] Defaults =
    {
        Hex("#FFFFFF"),   //target ring: white
        Hex("#FF66AB"),   //upcoming: pink
        Hex("#FFE3F1"),   //next: near-white pink
        Hex("#B07CFF"),   //hold: lavender
        Hex("#E2CCFF"),   //next hold: pale lavender
    };

    //Swatches offered for every slot
    public static readonly Color[] Palette =
    {
        Hex("#FFFFFF"), Hex("#FF3B3B"), Hex("#FF9F43"), Hex("#FFD23F"), Hex("#8CE36B"), Hex("#00F0B5"),
        Hex("#4FD8FF"), Hex("#3D8BFF"), Hex("#A855F7"), Hex("#FF66AB"),
        Hex("#FFE3F1"), Hex("#B07CFF"), Hex("#E2CCFF"),
    };

    private static readonly Color[] current = new Color[Defaults.Length];
    public static event Action Changed;

    static NoteColors()
    {
        for (int i = 0; i < current.Length; i++)
        {
            current[i] = Defaults[i];
            string saved = PlayerPrefs.GetString(Key((Kind)i), "");
            if (saved.Length > 0 && ColorUtility.TryParseHtmlString("#" + saved, out Color c)) current[i] = c;
        }
    }

    public static Color Get(Kind k) => current[(int)k];

    public static void Set(Kind k, Color c)
    {
        current[(int)k] = c;
        PlayerPrefs.SetString(Key(k), ColorUtility.ToHtmlStringRGB(c));
        Changed?.Invoke();
    }

    public static void ResetToDefaults()
    {
        for (int i = 0; i < current.Length; i++)
        {
            current[i] = Defaults[i];
            PlayerPrefs.DeleteKey(Key((Kind)i));
        }
        Changed?.Invoke();
    }

    static string Key(Kind k) => "noteColor_" + k;

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }
}
