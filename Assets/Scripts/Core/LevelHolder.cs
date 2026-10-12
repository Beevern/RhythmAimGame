using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

//Finds every level by scanning level folders, so built-in and custom levels work the same way.
//A level is a folder named after the song ("Song by Artist"), containing:
//    video.mp4 (or .webm/.mov/...)   cover.png/.jpg   notes.csv   [held.csv]   [target.png]   [level.json]
//Two places are scanned:
//    Assets/StreamingAssets/Levels/<song>/   built-in levels, copied into every build
//    <persistentDataPath>/custom_levels/<song>/   levels the player made with the Level Maker
//Moving a folder between the two just works.
//level.json is optional: { "name": "...", "offset": 1.0, "videoDelay": 0.35, "difficulty": 4.5 }
//  offset     = seconds added to every note (positive = later), for charts out of sync with the video
//  videoDelay = seconds to hold the video back after the countdown
//  difficulty = star rating to show instead of the calculated one (see Difficulty.cs)
//  chartFps   = frame rate of the timeline the CSV markers were placed on, e.g. 29.97 (default 30)
[DefaultExecutionOrder(-100)]
public class LevelHolder : MonoBehaviour
{
    //Cover shown for levels without a cover image
    public Sprite RandomI;

    public static string[] allSongs = new string[0];
    public static readonly Dictionary<string, float[]> songTimes = new Dictionary<string, float[]>();
    //Held notes are stored as start/end pairs
    public static readonly Dictionary<string, float[]> heldTimes = new Dictionary<string, float[]>();
    public static readonly Dictionary<string, Sprite> songImages = new Dictionary<string, Sprite>();
    public static readonly Dictionary<string, int> songLengths = new Dictionary<string, int>();
    public static readonly Dictionary<string, LevelInfo> levels = new Dictionary<string, LevelInfo>();

    public static string BundledRoot => Path.Combine(Application.streamingAssetsPath, "Levels");

    //CSV timecodes (when a chart has no "Seconds" column) are read at the level's "chartFps"
    //(the frame rate of the timeline the markers were placed on), or this when it isn't set
    public const float ChartFps = 30f;
    private static readonly string[] VideoExtensions = { ".mp4", ".webm", ".mov", ".m4v", ".avi", ".mkv" };
    private static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg" };

    private static Sprite defaultCover;
    //Folders already loaded, so later scans only pick up new levels
    private static readonly HashSet<string> loadedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    //Runs in every scene with the LevelManager prefab; the first time loads everything,
    //afterwards only levels added since (e.g. one just made in the Level Maker)
    void Awake()
    {
        if (RandomI != null) defaultCover = RandomI;
        Rescan();
    }

    public static Sprite DefaultCover => defaultCover;

    //Forget everything and load all level folders again (after the Level Editor saves changes)
    public static void ReloadAll()
    {
        songTimes.Clear();
        heldTimes.Clear();
        songImages.Clear();
        songLengths.Clear();
        levels.Clear();
        loadedFolders.Clear();
        allSongs = new string[0];
        Rescan();
    }

    public static void Rescan()
    {
        bool added = false;
        added |= ScanRoot(BundledRoot, true);
        added |= ScanRoot(CustomLevelManager.LevelsRoot, false);
        if (added)
        {
            var names = new List<string>(levels.Keys);
            names.Sort(StringComparer.OrdinalIgnoreCase);
            allSongs = names.ToArray();
        }
    }

    static bool ScanRoot(string root, bool bundled)
    {
        if (!Directory.Exists(root)) return false;
        bool added = false;
        foreach (string dir in Directory.GetDirectories(root))
        {
            if (loadedFolders.Contains(dir)) continue;
            loadedFolders.Add(dir);
            try
            {
                added |= LoadFolder(dir, bundled);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Couldn't load level folder " + dir + ": " + e.Message);
            }
        }
        return added;
    }

    static bool LoadFolder(string dir, bool bundled)
    {
        LevelJson settings = new LevelJson();
        string jsonPath = Path.Combine(dir, "level.json");
        if (File.Exists(jsonPath)) JsonUtility.FromJsonOverwrite(File.ReadAllText(jsonPath), settings);

        string name = string.IsNullOrWhiteSpace(settings.name) ? Path.GetFileName(dir) : settings.name.Trim();
        string notes = Path.Combine(dir, "notes.csv");
        if (!File.Exists(notes))
        {
            Debug.LogWarning("Level folder has no notes.csv, skipping: " + dir);
            return false;
        }
        if (levels.ContainsKey(name))
        {
            Debug.LogWarning("Two levels are called \"" + name + "\"; keeping the first. Skipped: " + dir);
            return false;
        }

        LevelInfo info = new LevelInfo
        {
            name = name,
            folder = dir,
            bundled = bundled,
            videoPath = FindFile(dir, "video", VideoExtensions),
            coverPath = FindFile(dir, "cover", ImageExtensions),
            targetPath = FindFile(dir, "target", ImageExtensions),
            offset = settings.offset,
            videoDelay = settings.videoDelay,
        };

        float fps = settings.chartFps > 0f ? settings.chartFps : ChartFps;
        float[] taps = Offset(CsvParser.ParseTimeColumn(notes, 10, fps), info.offset);
        string heldPath = Path.Combine(dir, "held.csv");
        float[] held = File.Exists(heldPath) ? Offset(CsvParser.ParseTimeColumn(heldPath, 10, fps), info.offset) : new float[0];

        info.difficulty = settings.difficulty > 0f ? settings.difficulty : Difficulty.Calculate(taps, held);
        info.volume = settings.volume > 0f ? settings.volume : 1f;
        info.hideAudioBars = settings.hideAudioBars;

        if (settings.oldNames != null)
            foreach (string old in settings.oldNames) LevelNames.MoveStats(old, name);

        levels[name] = info;
        songTimes[name] = taps;
        heldTimes[name] = held;
        //Note count includes held notes (held times are start/end pairs)
        songLengths[name] = taps.Length + held.Length / 2;
        songImages[name] = info.coverPath != null ? LoadSprite(info.coverPath) : defaultCover;
        return true;
    }

    //Custom target art for a song (target.png in its folder), or null for the default circle
    public static Sprite TargetSprite(string song)
    {
        if (song == null || !levels.TryGetValue(song, out LevelInfo info) || info.targetPath == null) return null;
        if (info.targetSprite == null) info.targetSprite = LoadSprite(info.targetPath);
        return info.targetSprite;
    }

    static float[] Offset(float[] chart, float offset)
    {
        if (offset == 0f) return chart;
        for (int i = 0; i < chart.Length; i++) chart[i] += offset;
        return chart;
    }

    //First file named <baseName>.<one of exts> (case-insensitive), or null
    static string FindFile(string dir, string baseName, string[] exts)
    {
        foreach (string f in Directory.GetFiles(dir))
        {
            if (!Path.GetFileNameWithoutExtension(f).Equals(baseName, StringComparison.OrdinalIgnoreCase)) continue;
            string ext = Path.GetExtension(f).ToLowerInvariant();
            if (Array.IndexOf(exts, ext) >= 0) return f;
        }
        return null;
    }

    static Sprite LoadSprite(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        Texture2D tex = new Texture2D(2, 2);
        if (!tex.LoadImage(bytes))
            return defaultCover;
        tex.name = Path.GetFileName(path);
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }

    [Serializable]
    class LevelJson
    {
        public string name;
        public float offset;
        public float videoDelay;
        public float difficulty;
        public float chartFps;
        //Names this level had before (stats are moved over to the current name)
        public string[] oldNames;
        //Song volume for this level, 1 = normal (0/missing also means normal)
        public float volume;
        //Turn off the in-game audio visualiser bars for this level
        public bool hideAudioBars;
    }
}

public class LevelInfo
{
    public string name;
    public string folder;
    public bool bundled;
    public string videoPath;
    public string coverPath;
    public string targetPath;
    public Sprite targetSprite;
    public float offset;
    public float videoDelay;
    //Star rating shown on Song Select
    public float difficulty;
    //Song volume multiplier (level.json "volume"), 1 = normal
    public float volume = 1f;
    //level.json "hideAudioBars"
    public bool hideAudioBars;
}
//Tool pipeline (for vocals): yt to mp4 (4k vid downloader)
// -> Level Maker: pick the video, then AUTO-CHART FROM LYRICS AND VIDEO (or chart by hand in DaVinci Resolve and export the markers CSV)
// -> Done saves the level to custom_levels/<name>/
//To ship a level with the game, copy its folder into Assets/StreamingAssets/Levels/.
