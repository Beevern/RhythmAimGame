using System.IO;
using UnityEngine;
using System;
using System.Collections.Generic;

//Persists the list of custom levels (custom_levels.json) and owns their files.
//Each level's video, cover and charts are copied into its own folder under SaveRoot
//(Assets/StreamingAssets/Levels/<level name>/ in the editor), so levels keep working after the
//originals are moved or deleted (and a level folder can be zipped up and shared).
//Runs before LevelHolder so old levels are copied into folders before it scans them
[DefaultExecutionOrder(-200)]
public class CustomLevelManager : MonoBehaviour
{

    public static CustomLevelManager Instance;

    private string savePath;
    public CustomLevelList levelList = new CustomLevelList();

    //Player-made levels in a built game (scanned by LevelHolder)
    public static string LevelsRoot => Path.Combine(Application.persistentDataPath, "custom_levels");

    //Where the Level Maker saves new levels. In the Unity editor they go straight into the
    //project's built-in levels (Assets/StreamingAssets/Levels) so they ship with the game;
    //a built game can't write into its own install folder, so there they go to custom_levels.
#if UNITY_EDITOR
    public static string SaveRoot => LevelHolder.BundledRoot;
#else
    public static string SaveRoot => LevelsRoot;
#endif

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        savePath = Path.Combine(Application.persistentDataPath, "custom_levels.json");
        LoadLevels();
        MigrateExternalFiles();
    }

    public void AddLevel(CustomLevelInfo info)
    {
        StoreFiles(info);
        levelList.levels.Add(info);
        SaveLevels();
        LevelHolder.Rescan();
    }

    void SaveLevels()
    {
        string json = JsonUtility.ToJson(levelList, true);
        File.WriteAllText(savePath, json);
        Debug.Log("Saved custom levels to: " + savePath + "\n" + json);
    }

    void LoadLevels()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            levelList = JsonUtility.FromJson<CustomLevelList>(json);
        }
        else
        {
            levelList = new CustomLevelList();
        }
    }

    //Levels made before files were copied still point at the originals; copy those in once
    void MigrateExternalFiles()
    {
        bool changed = false;
        foreach (CustomLevelInfo info in levelList.levels)
        {
            if (IsStored(info.songFilePath) && IsStored(info.imageFilePath) && IsStored(info.noteFilePath) && IsStored(info.heldFilePath))
                continue;
            StoreFiles(info);
            changed = true;
        }
        if (changed) SaveLevels();
    }

    //Copies the level's files into its folder and points the info at the copies.
    //Missing originals are left as-is (the level loads the same way it did before).
    void StoreFiles(CustomLevelInfo info)
    {
        string folder = FolderFor(info);
        Directory.CreateDirectory(folder);
        info.songFilePath = Store(info.songFilePath, folder, "video");
        info.imageFilePath = Store(info.imageFilePath, folder, "cover");
        info.noteFilePath = Store(info.noteFilePath, folder, "notes");
        info.heldFilePath = Store(info.heldFilePath, folder, "held");
        //Keeps the exact level name even if the folder name had to be changed,
        //and the frame rate the chart's timecodes were placed at
        File.WriteAllText(Path.Combine(folder, "level.json"),
            JsonUtility.ToJson(new LevelName { name = info.levelName, chartFps = info.chartFps }, true));
    }

    [Serializable]
    class LevelName
    {
        public string name;
        public float chartFps;
    }

    static string Store(string source, string folder, string baseName)
    {
        if (string.IsNullOrEmpty(source) || IsStored(source)) return source;
        if (!File.Exists(source))
        {
            Debug.LogWarning("Custom level file not found, keeping original path: " + source);
            return source;
        }
        string dest = Path.Combine(folder, baseName + Path.GetExtension(source).ToLowerInvariant());
        try
        {
            File.Copy(source, dest, true);
            return dest;
        }
        catch (Exception e)
        {
            Debug.LogWarning("Couldn't copy " + source + " into the level folder: " + e.Message);
            return source;
        }
    }

    //Already inside the game's custom level storage?
    static bool IsStored(string path)
    {
        if (string.IsNullOrEmpty(path)) return true;
        try
        {
            string full = Path.GetFullPath(path);
            foreach (string r in new[] { LevelsRoot, LevelHolder.BundledRoot })
            {
                string root = Path.GetFullPath(r) + Path.DirectorySeparatorChar;
                if (full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
        catch (Exception)
        {
            //Not a usable file path; leave it alone
            return true;
        }
    }

    //One folder per level, named after it; a different level with the same name gets " (2)" etc.
    string FolderFor(CustomLevelInfo info)
    {
        string safe = info.levelName ?? "Level";
        foreach (char c in Path.GetInvalidFileNameChars()) safe = safe.Replace(c, '_');
        safe = safe.Trim().TrimEnd('.');
        if (safe.Length == 0) safe = "Level";

        string folder = Path.Combine(SaveRoot, safe);
        for (int n = 2; FolderTakenByOtherLevel(folder, info); n++)
        {
            folder = Path.Combine(SaveRoot, safe + " (" + n + ")");
        }
        return folder;
    }

    bool FolderTakenByOtherLevel(string folder, CustomLevelInfo info)
    {
        string prefix = Path.GetFullPath(folder) + Path.DirectorySeparatorChar;
        //Never overwrite an existing level folder (e.g. a built-in level with the same name)
        //unless it already belongs to this level
        bool ours = !string.IsNullOrEmpty(info.songFilePath) &&
                    Path.GetFullPath(info.songFilePath).StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        if (Directory.Exists(folder) && !ours) return true;
        foreach (CustomLevelInfo other in levelList.levels)
        {
            if (other == info) continue;
            string p = other.songFilePath;
            if (string.IsNullOrEmpty(p)) continue;
            try
            {
                if (Path.GetFullPath(p).StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
            }
            catch (Exception) { }
        }
        return false;
    }
}
[Serializable]
public class CustomLevelInfo
{
    public string levelName;     // Whatever you want to show in UI
    public string songFilePath;  // Video (copied into the level's folder)
    public string imageFilePath; // Cover image (copied into the level's folder)
    public string noteFilePath;  // Notes CSV (copied into the level's folder)
    public string heldFilePath;  // Held notes CSV (copied into the level's folder)
    public float chartFps;       // Frame rate of the video the chart was made against (0 = 30)
}

[Serializable]
public class CustomLevelList
{
    public List<CustomLevelInfo> levels = new List<CustomLevelInfo>();
}
