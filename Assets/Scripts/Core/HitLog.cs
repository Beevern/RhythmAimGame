using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

//Records how every note went in a run (perfect / great / good / hit / too early / miss, plus reaction
//time) and appends it to <persistentDataPath>/hitlogs/<level>.csv after each finished run.
//Notes are identified by their chart time (as stored in the level's CSVs, before offset), so the
//Level Editor can show, per note, how often it's missed or hit early across all runs.
public static class HitLog
{
    public const string Perfect = "PERFECT", Great = "GREAT", Good = "GOOD", Hit = "HIT", Early = "EARLY", Miss = "MISS";

    struct Entry
    {
        public float chartTime;
        public bool hold;
        public string result;
        public float reaction;
    }

    static readonly List<Entry> run = new List<Entry>();
    static string song;

    public static string Folder => Path.Combine(Application.persistentDataPath, "hitlogs");

    public static string FileFor(string level)
    {
        string safe = level ?? "level";
        foreach (char c in Path.GetInvalidFileNameChars()) safe = safe.Replace(c, '_');
        return Path.Combine(Folder, safe + ".csv");
    }

    public static void Begin(string level)
    {
        song = level;
        run.Clear();
    }

    //score = the judgement score shown for the hit (0 = miss), like ScoreDesc
    public static void Record(float chartTime, bool hold, int score, float reaction)
    {
        Add(chartTime, hold, ResultFor(score), reaction);
    }

    public static void RecordEarly(float chartTime, bool hold)
    {
        Add(chartTime, hold, Early, 0f);
    }

    static void Add(float chartTime, bool hold, string result, float reaction)
    {
        run.Add(new Entry { chartTime = chartTime, hold = hold, result = result, reaction = reaction });
    }

    public static string ResultFor(int score)
    {
        if (score >= 475) return Perfect;
        if (score >= 400) return Great;
        if (score >= 300) return Good;
        if (score >= 1) return Hit;
        return Miss;
    }

    //Appends this run to the level's log (called when the run finishes)
    public static void Save()
    {
        if (string.IsNullOrEmpty(song) || run.Count == 0) return;
        try
        {
            Directory.CreateDirectory(Folder);
            string path = FileFor(song);
            var sb = new StringBuilder();
            if (!File.Exists(path)) sb.AppendLine("Run,Seconds,Type,Result,ReactionMs");
            string stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            foreach (Entry e in run)
            {
                sb.Append(stamp).Append(',')
                  .Append(e.chartTime.ToString("0.000", CultureInfo.InvariantCulture)).Append(',')
                  .Append(e.hold ? "HOLD" : "TAP").Append(',')
                  .Append(e.result).Append(',')
                  .Append(Mathf.RoundToInt(e.reaction * 1000f)).AppendLine();
            }
            File.AppendAllText(path, sb.ToString());
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Couldn't save hit log: " + ex.Message);
        }
        run.Clear();
    }

    //---------------- Reading (Level Editor) ----------------

    public class NoteStats
    {
        public int plays, perfect, great, good, hit, early, miss;
        public float reactionSum;
        public int reactionCount;
        public float AvgReactionMs => reactionCount > 0 ? reactionSum / reactionCount * 1000f : 0f;
        public float MissRate => plays > 0 ? (float)miss / plays : 0f;
        public float EarlyRate => plays > 0 ? (float)early / plays : 0f;
    }

    //Per-note totals across every logged run, keyed by chart time in milliseconds
    public static Dictionary<long, NoteStats> Load(string level)
    {
        var stats = new Dictionary<long, NoteStats>();
        string path = FileFor(level);
        if (!File.Exists(path)) return stats;
        bool header = true;
        foreach (string line in File.ReadLines(path))
        {
            if (header) { header = false; continue; }
            string[] c = line.Split(',');
            if (c.Length < 5 || !float.TryParse(c[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float t)) continue;
            long key = Key(t);
            if (!stats.TryGetValue(key, out NoteStats s)) stats[key] = s = new NoteStats();
            s.plays++;
            switch (c[3])
            {
                case Perfect: s.perfect++; break;
                case Great: s.great++; break;
                case Good: s.good++; break;
                case Hit: s.hit++; break;
                case Early: s.early++; break;
                default: s.miss++; break;
            }
            if (c[3] != Miss && c[3] != Early && int.TryParse(c[4], out int ms))
            {
                s.reactionSum += ms / 1000f;
                s.reactionCount++;
            }
        }
        return stats;
    }

    public static long Key(float chartTime) => (long)Math.Round(chartTime * 1000.0);

    //Stats for a note, allowing a millisecond either way for rounding
    public static NoteStats Find(Dictionary<long, NoteStats> stats, float chartTime)
    {
        long k = Key(chartTime);
        for (long d = 0; d <= 1; d++)
        {
            if (stats.TryGetValue(k + d, out NoteStats s)) return s;
            if (d > 0 && stats.TryGetValue(k - d, out s)) return s;
        }
        return null;
    }
}
