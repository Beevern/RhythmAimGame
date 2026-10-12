using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

//One note in the Level Editor. Times are chart times in seconds (as stored in the CSVs,
//before the level's offset is applied).
public class EditNote
{
    public float time;
    public bool hold;
    public float end;      //hold release time (holds only)
    public string label = "";

    public EditNote Clone() => (EditNote)MemberwiseClone();
}

//A level folder loaded for editing, plus everything needed to write it back.
public class EditableLevel
{
    public string folder;
    public string name;
    public string originalName;
    public readonly List<EditNote> notes = new List<EditNote>();

    //level.json settings (kept as-is unless edited)
    public float offset;
    public float videoDelay;
    public float difficultyOverride;
    public float chartFps;
    public float volume = 1f;
    public bool hideAudioBars;
    public List<string> oldNames = new List<string>();

    public string videoPath;
    public string coverPath;
    //Replacement files picked in the editor; copied into the folder on Save
    public string newVideoSource;
    public string newCoverSource;

    //Where the preview should play the video from (the replacement, if one was picked)
    public string PreviewVideo => newVideoSource ?? videoPath;
    public string PreviewCover => newCoverSource ?? coverPath;

    [Serializable]
    class LevelJson
    {
        public string name;
        public float offset;
        public float videoDelay;
        public float difficulty;
        public float chartFps;
        public string[] oldNames;
        public float volume;
        public bool hideAudioBars;
    }

    public static EditableLevel Load(string folder)
    {
        var lv = new EditableLevel { folder = folder };
        var json = new LevelJson();
        string jsonPath = Path.Combine(folder, "level.json");
        if (File.Exists(jsonPath)) JsonUtility.FromJsonOverwrite(File.ReadAllText(jsonPath), json);

        lv.name = string.IsNullOrWhiteSpace(json.name) ? Path.GetFileName(folder) : json.name.Trim();
        lv.originalName = lv.name;
        lv.offset = json.offset;
        lv.videoDelay = json.videoDelay;
        lv.difficultyOverride = json.difficulty;
        lv.chartFps = json.chartFps;
        lv.volume = json.volume > 0f ? json.volume : 1f;
        lv.hideAudioBars = json.hideAudioBars;
        if (json.oldNames != null) lv.oldNames.AddRange(json.oldNames);
        lv.videoPath = FindFile(folder, "video", ".mp4", ".webm", ".mov", ".m4v", ".avi", ".mkv");
        lv.coverPath = FindFile(folder, "cover", ".png", ".jpg", ".jpeg");

        float fps = json.chartFps > 0f ? json.chartFps : LevelHolder.ChartFps;
        string notesPath = Path.Combine(folder, "notes.csv");
        if (File.Exists(notesPath))
        {
            List<float> t = CsvParser.ParseNotes(notesPath, out List<string> labels, 10, fps);
            for (int i = 0; i < t.Count; i++) lv.notes.Add(new EditNote { time = t[i], label = labels[i] });
        }
        string heldPath = Path.Combine(folder, "held.csv");
        if (File.Exists(heldPath))
        {
            List<float> t = CsvParser.ParseNotes(heldPath, out List<string> labels, 10, fps);
            for (int i = 0; i + 1 < t.Count; i += 2)
                lv.notes.Add(new EditNote { time = t[i], end = t[i + 1], hold = true, label = labels[i] });
        }
        lv.Sort();
        return lv;
    }

    public void Sort()
    {
        notes.Sort((a, b) => a.time.CompareTo(b.time));
    }

    public int TapCount { get { int n = 0; foreach (var x in notes) if (!x.hold) n++; return n; } }
    public int HoldCount => notes.Count - TapCount;

    public float Difficulty()
    {
        if (difficultyOverride > 0f) return difficultyOverride;
        var taps = new List<float>();
        var held = new List<float>();
        foreach (var n in notes)
        {
            if (n.hold) { held.Add(n.time); held.Add(n.end); }
            else taps.Add(n.time);
        }
        return global::Difficulty.Calculate(taps.ToArray(), held.ToArray());
    }

    //---------------- Saving ----------------

    //Writes charts, level.json and any replaced files. Renames the folder if the name changed.
    //Returns null on success, or an error message.
    public string Save()
    {
        try
        {
            Sort();
            string newName = (name ?? "").Trim();
            if (newName.Length == 0) return "The level needs a name.";

            if (newName != originalName)
            {
                string target = Path.Combine(Path.GetDirectoryName(folder), SafeFolderName(newName));
                if (!string.Equals(Path.GetFullPath(target), Path.GetFullPath(folder), StringComparison.OrdinalIgnoreCase))
                {
                    if (Directory.Exists(target)) return "A level folder called \"" + Path.GetFileName(target) + "\" already exists.";
                    Directory.Move(folder, target);
                    MoveMeta(folder, target);
                    if (videoPath != null) videoPath = Path.Combine(target, Path.GetFileName(videoPath));
                    if (coverPath != null) coverPath = Path.Combine(target, Path.GetFileName(coverPath));
                    folder = target;
                }
                if (!oldNames.Contains(originalName)) oldNames.Add(originalName);
                LevelNames.MoveStats(originalName, newName);
                string oldLog = HitLog.FileFor(originalName), newLog = HitLog.FileFor(newName);
                if (File.Exists(oldLog) && !File.Exists(newLog)) File.Move(oldLog, newLog);
                originalName = newName;
            }

            if (newVideoSource != null)
            {
                videoPath = ReplaceFile(newVideoSource, "video", ".mp4", ".webm", ".mov", ".m4v", ".avi", ".mkv");
                newVideoSource = null;
            }
            if (newCoverSource != null)
            {
                coverPath = ReplaceFile(newCoverSource, "cover", ".png", ".jpg", ".jpeg");
                newCoverSource = null;
            }

            WriteChart(Path.Combine(folder, "notes.csv"), false);
            string heldPath = Path.Combine(folder, "held.csv");
            if (HoldCount > 0) WriteChart(heldPath, true);
            else if (File.Exists(heldPath)) File.Delete(heldPath);

            var json = new LevelJson
            {
                name = newName,
                offset = offset,
                videoDelay = videoDelay,
                difficulty = difficultyOverride,
                chartFps = chartFps,
                volume = Mathf.Approximately(volume, 1f) ? 0f : volume,
                hideAudioBars = hideAudioBars,
                oldNames = oldNames.Count > 0 ? oldNames.ToArray() : null,
            };
            File.WriteAllText(Path.Combine(folder, "level.json"), JsonUtility.ToJson(json, true));
            return null;
        }
        catch (Exception e)
        {
            Debug.LogWarning("Level Editor save failed: " + e);
            return "Save failed: " + e.Message;
        }
    }

    //Same layout as the converted/auto-charted CSVs: exact seconds plus a timecode for Resolve
    void WriteChart(string path, bool holds)
    {
        var lines = new List<string> { "#,Word,Seconds,Aligned,Snapped,Confidence,Duration,Edge,,,Record In" };
        int i = 1;
        foreach (EditNote n in notes)
        {
            if (n.hold != holds) continue;
            if (holds)
            {
                lines.Add(Row(i++, n.label, n.time, "start"));
                lines.Add(Row(i++, n.label, n.end, "end"));
            }
            else
            {
                lines.Add(Row(i++, n.label, n.time, ""));
            }
        }
        File.WriteAllLines(path, lines);
    }

    static string Row(int index, string label, float seconds, string edge)
    {
        string word = (label ?? "").Replace(",", " ").Replace("\"", "");
        return index + "," + word + "," + seconds.ToString("R", CultureInfo.InvariantCulture) + ",,,,," + edge + ",,," + Timecode(seconds);
    }

    //hh:mm:ss:ff at 30 fps on a timeline starting at 01:00:00:00 (only used by external tools)
    static string Timecode(float seconds)
    {
        int frames = Mathf.Max(0, Mathf.RoundToInt(seconds * 30f));
        int ff = frames % 30, s = frames / 30;
        return $"{1 + s / 3600:00}:{s / 60 % 60:00}:{s % 60:00}:{ff:00}";
    }

    string ReplaceFile(string source, string baseName, params string[] exts)
    {
        string dest = Path.Combine(folder, baseName + Path.GetExtension(source).ToLowerInvariant());
        //Remove the old one (it may have a different extension)
        string old = FindFile(folder, baseName, exts);
        if (old != null && !string.Equals(old, dest, StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(old);
            if (File.Exists(old + ".meta")) File.Delete(old + ".meta");
        }
        File.Copy(source, dest, true);
        return dest;
    }

    //Unity keeps a .meta next to every folder inside Assets; move it along with the folder
    static void MoveMeta(string from, string to)
    {
        if (File.Exists(from + ".meta") && !File.Exists(to + ".meta")) File.Move(from + ".meta", to + ".meta");
    }

    public static string SafeFolderName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        name = name.Trim().TrimEnd('.');
        return name.Length > 0 ? name : "Level";
    }

    static string FindFile(string dir, string baseName, params string[] exts)
    {
        if (!Directory.Exists(dir)) return null;
        foreach (string f in Directory.GetFiles(dir))
        {
            if (!Path.GetFileNameWithoutExtension(f).Equals(baseName, StringComparison.OrdinalIgnoreCase)) continue;
            if (Array.IndexOf(exts, Path.GetExtension(f).ToLowerInvariant()) >= 0) return f;
        }
        return null;
    }
}
