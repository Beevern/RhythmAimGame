using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

//Reads a level chart CSV (one row per note/marker, first row is the header).
//Picks the time column by header name, so CSVs from different tools all work:
//  1. "Seconds"   - exact time in seconds (auto_chart.py and converted built-in levels)
//  2. "Record In" - timecode hh:mm:ss:ff on a timeline starting at 01:00:00:00 (DaVinci Resolve)
//  3. otherwise column K (index 10) as a timecode, like the original marker CSVs
public static class CsvParser
{
    //fps is the frame rate of the timeline the markers were placed on (29.97 for most YouTube videos)
    public static float[] ParseTimeColumn(string csvPath, int columnIndex = 10, float fps = 30f)
    {
        return ParseNotes(csvPath, out _, columnIndex, fps).ToArray();
    }

    //Like ParseTimeColumn, plus each note's label from a "Word" or "Name" column ("" if none)
    public static List<float> ParseNotes(string csvPath, out List<string> labels, int columnIndex = 10, float fps = 30f)
    {
        var result = new List<float>();
        labels = new List<string>();

        if (!File.Exists(csvPath))
        {
            Debug.LogError("CSV not found: " + csvPath);
            return result;
        }

        int secondsCol = -1, timecodeCol = columnIndex, labelCol = -1;
        bool isFirstRow = true;

        foreach (var line in File.ReadLines(csvPath))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // naive CSV split; fine for marker files (no quoted commas in the columns we read)
            var cells = line.Split(',');

            if (isFirstRow)
            {
                isFirstRow = false;
                for (int i = 0; i < cells.Length; i++)
                {
                    string h = cells[i].Trim().Trim('"');
                    if (h.Equals("Seconds", StringComparison.OrdinalIgnoreCase)) secondsCol = i;
                    else if (h.Equals("Record In", StringComparison.OrdinalIgnoreCase)) timecodeCol = i;
                    else if (labelCol < 0 && (h.Equals("Word", StringComparison.OrdinalIgnoreCase) || h.Equals("Name", StringComparison.OrdinalIgnoreCase))) labelCol = i;
                }
                continue;
            }

            if (secondsCol >= 0 && secondsCol < cells.Length &&
                float.TryParse(cells[secondsCol].Trim().Trim('"'), NumberStyles.Float, CultureInfo.InvariantCulture, out float s))
            {
                result.Add(s);
                labels.Add(Label(cells, labelCol));
                continue;
            }

            if (cells.Length <= timecodeCol)
                continue;

            string timeStr = cells[timecodeCol].Trim().Trim('"');
            if (string.IsNullOrEmpty(timeStr))
                continue;

            if (TryParseTimecode(timeStr, fps, out float seconds))
            {
                result.Add(seconds);
                labels.Add(Label(cells, labelCol));
            }
        }

        return result;
    }

    static string Label(string[] cells, int col)
    {
        return col >= 0 && col < cells.Length ? cells[col].Trim().Trim('"') : "";
    }

    //Snaps a measured frame rate (e.g. Unity's 29.97003) to the standard rate it stands for
    public static float StandardFps(float fps)
    {
        float[] rates = { 24000f / 1001f, 24f, 25f, 30000f / 1001f, 30f, 48f, 50f, 60000f / 1001f, 60f };
        foreach (float r in rates)
            if (Mathf.Abs(fps - r) < 0.02f) return r;
        return fps > 1f ? fps : 30f;
    }

    // Expects "hh:mm:ss:ff" on a timeline that starts at 01:00:00:00.
    // Non-drop timecode counts whole frames at the rounded rate (30 for 29.97),
    // so the frame number is converted back to real time at the true rate.
    static bool TryParseTimecode(string tc, float fps, out float seconds)
    {
        seconds = 0f;
        var parts = tc.Split(':');
        if (parts.Length != 4)
        {
            Debug.LogWarning("Bad timecode format: " + tc);
            return false;
        }

        if (!int.TryParse(parts[0], out int hh)) return false;
        if (!int.TryParse(parts[1], out int mm)) return false;
        if (!int.TryParse(parts[2], out int ss)) return false;
        if (!int.TryParse(parts[3], out int ff)) return false;

        int nominal = Mathf.RoundToInt(fps);
        long frames = ((hh - 1) * 3600L + mm * 60L + ss) * nominal + ff;
        seconds = (float)(frames / (double)fps);
        return true;
    }
}
