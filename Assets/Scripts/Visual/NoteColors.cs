using System;
using UnityEngine;

//Player-chosen note colours (Settings > Note colours). Saved in PlayerPrefs.
//Theme.Note / UpcomingNote / NextNote / HeldNote / NextHeld read from here.
public static class NoteColors
{
    public enum Kind { Target, Upcoming, Next, Held, NextHeld }

    public static readonly string[] Labels = { "TARGET RING", "UPCOMING NOTES", "NEXT NOTE", "HOLD NOTES", "NEXT HOLD" };

    //Defaults are the game's original note colours
    public static readonly Color[] Defaults =
    {
        Hex("#3D8BFF"),   //target ring: blue
        Hex("#FF66AB"),   //upcoming: pink
        Hex("#FF3B3B"),   //next: red
        Hex("#FFD23F"),   //hold: yellow
        Hex("#A855F7"),   //next hold: purple
    };

    //Swatches offered for every slot
    public static readonly Color[] Palette =
    {
        Hex("#FFFFFF"), Hex("#FF3B3B"), Hex("#FF9F43"), Hex("#FFD23F"), Hex("#8CE36B"), Hex("#00F0B5"),
        Hex("#4FD8FF"), Hex("#3D8BFF"), Hex("#A855F7"), Hex("#FF66AB"),
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
