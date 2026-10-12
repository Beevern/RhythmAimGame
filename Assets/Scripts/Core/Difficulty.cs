using System;
using System.Collections.Generic;
using UnityEngine;

//Star rating for a level, worked out from its chart:
//
//    difficulty = averageNPS + 0.4 * peakNPS + 2 * holdShare
//
//  averageNPS = notes per second between the first and last note (intros/outros don't count)
//  peakNPS    = notes in the busiest 5 second stretch / 5, so short hard sections still count
//  holdShare  = fraction of notes that are holds (tracking + holding is harder than a tap)
//
//Notes = taps + held notes. Current levels land between ~2.8 and ~5.8.
//A level can override this with "difficulty" in its level.json.
public static class Difficulty
{
    private const float PeakWindow = 5f;
    private const float PeakWeight = 0.4f;
    private const float HoldWeight = 2f;

    public static float Calculate(float[] taps, float[] heldPairs)
    {
        var starts = new List<float>(taps);
        for (int i = 0; i + 1 < heldPairs.Length; i += 2) starts.Add(heldPairs[i]);
        int notes = starts.Count;
        if (notes == 0) return 0f;
        starts.Sort();

        float span = Mathf.Max(1f, starts[notes - 1] - starts[0]);
        float average = notes / span;

        int peak = 0;
        for (int i = 0, j = 0; i < notes; i++)
        {
            while (starts[i] - starts[j] > PeakWindow) j++;
            peak = Math.Max(peak, i - j + 1);
        }
        float peakNps = peak / PeakWindow;

        float holdShare = (heldPairs.Length / 2) / (float)notes;
        return average + PeakWeight * peakNps + HoldWeight * holdShare;
    }

    //Colour band for showing a rating
    public static Color ColorFor(float stars)
    {
        if (stars < 3f) return Theme.Great;     //green
        if (stars < 4f) return Theme.Perfect;   //blue
        if (stars < 5f) return Theme.Good;      //yellow
        if (stars < 5.5f) return Theme.Early;   //orange
        return Theme.Miss;                      //red
    }
}
