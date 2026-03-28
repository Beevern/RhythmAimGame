using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class CsvParser
{
    // columnIndex: 0-based (J = 9)
    public static float[] ParseTimeColumn(string csvPath, int columnIndex = 10, int fps = 30)
    {
        var result = new List<float>();

        if (!File.Exists(csvPath))
        {
            Debug.LogError("CSV not found: " + csvPath);
            return result.ToArray();
        }
         bool isFirstRow = true;

        foreach (var line in File.ReadLines(csvPath))
        {

            if (isFirstRow)
            {
                isFirstRow = false;   // skip header
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
                continue;

            // naive CSV split; OK for your marker file
            var cells = line.Split(',');

            if (cells.Length <= columnIndex)
                continue;

            string timeStr = cells[columnIndex].Trim();
            if (string.IsNullOrEmpty(timeStr))
                continue;

            if (TryParseTimecode(timeStr, fps, out float seconds))
                result.Add(seconds);
        }

        //Debug.Log(result.ToString());
        //Debug.Log(result.Count);
        return result.ToArray();
    }

    // Expects "hh:mm:ss:ff"
    static bool TryParseTimecode(string tc, int fps, out float seconds)
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

        seconds = (hh-1) * 3600f + mm * 60f + ss + (ff / (float)fps);
        return true;
    }
}