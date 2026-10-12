using System.Collections.Generic;

//The instrumental player's chart in duo mode: the level's drum hits (drums.csv), thinned so they're playable.
//Times are converted to the Spawner's clock (seconds since the level loaded when the approach note spawns),
//the same clock notes.csv times are on.
public static class DuoChart
{
    //No two notes closer than this; of hits closer together, the strongest one is kept
    public const float MinGap = 0.25f;

    //Height in the spawn square per drum, so position tells you what you're hitting
    static float HeightFor(string type)
    {
        switch (type)
        {
            case "kick": return 0.15f;
            case "snare": return 0.5f;
            case "hihat": return 0.85f;
            default: return float.NaN;
        }
    }

    //False if the level has no drums.csv (the instrumental player then gets the vocal chart)
    public static bool TryInstrumental(LevelInfo level, out float[] times, out float[] heights)
    {
        times = heights = null;
        if (level == null || level.drumsPath == null) return false;
        List<DrumHits.Hit> hits = DrumHits.Load(level.drumsPath);
        if (hits.Count == 0) return false;

        var kept = new List<DrumHits.Hit>();
        foreach (DrumHits.Hit h in hits)
        {
            if (kept.Count == 0 || h.time - kept[kept.Count - 1].time >= MinGap) kept.Add(h);
            else if (h.strength > kept[kept.Count - 1].strength) kept[kept.Count - 1] = h;
        }

        //Drum times are song time (from when the video starts). The video starts videoDelay after the
        //countdown, and a target pops 3s (the countdown length) after its approach note spawns, so a hit at
        //song time t spawns its note at t + videoDelay.
        times = new float[kept.Count];
        heights = new float[kept.Count];
        for (int i = 0; i < kept.Count; i++)
        {
            times[i] = kept[i].time + level.videoDelay;
            heights[i] = HeightFor(kept[i].type);
        }
        return true;
    }
}
