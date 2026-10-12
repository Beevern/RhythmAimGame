using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

//Plays the level's drum hits (drums.csv in the level folder, made by Tools/auto_chart.py)
//as events, in sync with the song. Hook an animation up to it:
//
//    void OnEnable()  { DrumHits.OnHit += Pulse; }
//    void OnDisable() { DrumHits.OnHit -= Pulse; }
//    void Pulse(DrumHits.Hit hit) { if (hit.type == "kick") ...; scale by hit.strength ... }
//
//Times are measured from the moment the video starts, on the same clock the notes use
//(Time.timeSinceLevelLoad), so pausing holds the drums back exactly like the notes.
public class DrumHits : MonoBehaviour
{
    public struct Hit
    {
        public float time;       //seconds into the song
        public string type;      //"kick", "snare" or "hihat" (an estimate from the hit's frequencies)
        public float strength;   //0..1, how hard it hits compared to the rest of the song
    }

    //Fired once per hit, the frame it's heard
    public static event Action<Hit> OnHit;

    //Every hit in the current level, in order (empty if the level has no drums.csv)
    public static IReadOnlyList<Hit> All => hits;

    //Seconds into the song right now (negative before the video starts)
    public static float SongTime => playMusic.VideoStarted ? Time.timeSinceLevelLoad - playMusic.VideoStartTime : -1f;

    private static List<Hit> hits = new List<Hit>();
    private int next;

    void Start()
    {
        hits = new List<Hit>();
        next = 0;
        if (LevelHolder.levels.TryGetValue(SongSelect2.theSong ?? "", out LevelInfo level) && level.drumsPath != null)
        {
            hits = Load(level.drumsPath);
        }
    }

    void Update()
    {
        if (!playMusic.VideoStarted || hits.Count == 0) return;
        float now = SongTime;
        while (next < hits.Count && hits[next].time <= now)
        {
            OnHit?.Invoke(hits[next]);
            next++;
        }
    }

    //drums.csv: header row, then  #, Seconds, Type, Strength
    public static List<Hit> Load(string path)
    {
        var list = new List<Hit>();
        try
        {
            bool header = true;
            foreach (string line in File.ReadLines(path))
            {
                if (header) { header = false; continue; }
                string[] c = line.Split(',');
                if (c.Length < 4) continue;
                if (!float.TryParse(c[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float t)) continue;
                float.TryParse(c[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float s);
                list.Add(new Hit { time = t, type = c[2].Trim(), strength = s });
            }
            list.Sort((a, b) => a.time.CompareTo(b.time));
        }
        catch (Exception e)
        {
            Debug.LogWarning("Couldn't read drum hits " + path + ": " + e.Message);
        }
        return list;
    }
}
