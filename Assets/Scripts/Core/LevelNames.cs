using System;
using UnityEngine;

//Level names are "Song by Artist" (e.g. "Circles by EDEN").
//"Artist - Song" is understood too.
public static class LevelNames
{
    public static void Split(string name, out string artist, out string title)
    {
        name = name ?? "";
        int by = name.LastIndexOf(" by ", StringComparison.OrdinalIgnoreCase);
        if (by > 0)
        {
            title = name.Substring(0, by).Trim();
            artist = name.Substring(by + 4).Trim();
            return;
        }
        int dash = name.IndexOf(" - ", StringComparison.Ordinal);
        if (dash > 0)
        {
            artist = name.Substring(0, dash).Trim();
            title = name.Substring(dash + 3).Trim();
            return;
        }
        artist = "";
        title = name.Trim();
    }

    public static string Artist(string name)
    {
        Split(name, out string artist, out _);
        return artist;
    }

    public static string Title(string name)
    {
        Split(name, out _, out string title);
        return title;
    }

    public static string Compose(string artist, string title)
    {
        artist = (artist ?? "").Trim();
        title = (title ?? "").Trim();
        return artist.Length > 0 ? title + " by " + artist : title;
    }

    //Stats are saved under the level's name; when a level is renamed they move to the new name
    private static readonly string[] IntStats = { "highScore", "highCombo", "totalScore", "timesPlayed", "gamesPlayed" };

    //If the old name has more plays than the new one (e.g. a level was renamed, played, then renamed
    //back), its stats replace the new name's; otherwise only missing stats are filled in.
    public static void MoveStats(string oldName, string newName)
    {
        if (string.IsNullOrEmpty(oldName) || oldName == newName) return;
        bool overwrite = PlayerPrefs.GetInt(oldName + "timesPlayed", 0) > PlayerPrefs.GetInt(newName + "timesPlayed", 0);
        bool moved = false;
        foreach (string key in IntStats)
        {
            if (PlayerPrefs.HasKey(oldName + key) && (overwrite || !PlayerPrefs.HasKey(newName + key)))
            {
                PlayerPrefs.SetInt(newName + key, PlayerPrefs.GetInt(oldName + key));
                moved = true;
            }
        }
        if (PlayerPrefs.HasKey(oldName + "totalParti") && (overwrite || !PlayerPrefs.HasKey(newName + "totalParti")))
        {
            PlayerPrefs.SetFloat(newName + "totalParti", PlayerPrefs.GetFloat(oldName + "totalParti"));
            moved = true;
        }
        if (moved) PlayerPrefs.Save();
    }
}
