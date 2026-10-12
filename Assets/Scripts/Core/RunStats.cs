using System.Collections.Generic;
using UnityEngine;

//Per-run performance data collected during play and shown on the results screen
public static class RunStats
{
    public static int shotsFired;
    public static int shotsHit;
    //Points earned before the combo multiplier (max 500 per note)
    public static int baseScore;
    //Seconds between a target appearing and it being hit
    public static readonly List<float> reactionTimes = new List<float>();
    //0..1, how much of each held note was held
    public static readonly List<float> holdRatios = new List<float>();
    //Perfect, Great, Good, Hit, Too Early, Miss
    public const int PerfectIdx = 0, GreatIdx = 1, GoodIdx = 2, HitIdx = 3, EarlyIdx = 4, MissIdx = 5;
    public static readonly int[] judgements = new int[6];
    //High score for the song before this run (GameOver overwrites the saved one)
    public static int previousHighScore;
    //Seconds the crosshair stayed on the white trail, out of seconds a trail was on screen
    public static float trackedSeconds;
    public static float trailSeconds;
    public static int trackPoints;

    public static void Reset(string song)
    {
        shotsFired = 0;
        shotsHit = 0;
        baseScore = 0;
        reactionTimes.Clear();
        holdRatios.Clear();
        System.Array.Clear(judgements, 0, judgements.Length);
        previousHighScore = PlayerPrefs.GetInt(song + "highScore", 0);
        trackedSeconds = 0f;
        trailSeconds = 0f;
        trackPoints = 0;
        HitLog.Begin(song);
    }

    public static void AddJudgement(int score)
    {
        if (score >= 475) judgements[PerfectIdx]++;
        else if (score >= 400) judgements[GreatIdx]++;
        else if (score >= 300) judgements[GoodIdx]++;
        else if (score >= 1) judgements[HitIdx]++;
        else judgements[MissIdx]++;
    }

    //Shots that hit something, out of all clicks
    public static float Accuracy => shotsFired > 0 ? (float)shotsHit / shotsFired : 0f;

    public static float AverageReaction
    {
        get
        {
            if (reactionTimes.Count == 0) return 0f;
            float sum = 0f;
            foreach (float r in reactionTimes) sum += r;
            return sum / reactionTimes.Count;
        }
    }

    //Standard deviation of reaction times; lower = steadier timing
    public static float ReactionSpread
    {
        get
        {
            if (reactionTimes.Count < 2) return 0f;
            float mean = AverageReaction, sq = 0f;
            foreach (float r in reactionTimes) sq += (r - mean) * (r - mean);
            return Mathf.Sqrt(sq / reactionTimes.Count);
        }
    }

    public static float HoldAccuracy
    {
        get
        {
            if (holdRatios.Count == 0) return 0f;
            float sum = 0f;
            foreach (float h in holdRatios) sum += h;
            return sum / holdRatios.Count;
        }
    }
}
