using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

//After a Windows build, copies the self-contained auto-chart tools (Tools/auto_chart.py, python/,
//models/, bin/) next to the game's .exe, so the build folder can be zipped and sent to anyone and the
//Level Maker's auto-chart works with nothing else installed.
//Files already in the build folder with the same size and date are skipped, so only the first build
//copies the full ~6.5 GB.
public class AutoChartBuildHook : IPostprocessBuildWithReport
{
    public int callbackOrder => 0;

    static readonly string[] Folders = { "python", "models", "bin" };
    static readonly string[] Files = { "auto_chart.py", "requirements.txt" };

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.StandaloneWindows && report.summary.platform != BuildTarget.StandaloneWindows64)
            return;

        string src = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Tools"));
        if (!File.Exists(Path.Combine(src, "auto_chart.py")) || !File.Exists(Path.Combine(src, "python", "python.exe")))
        {
            Debug.LogWarning("AutoChartBuildHook: Tools/python (self-contained auto-chart) not found in the project; " +
                             "auto-chart won't work in this build on other PCs.");
            return;
        }

        string dst = Path.Combine(Path.GetDirectoryName(report.summary.outputPath), "Tools");
        int copied = 0, skipped = 0;
        try
        {
            foreach (string f in Files)
            {
                string s = Path.Combine(src, f);
                if (File.Exists(s)) CopyIfChanged(s, Path.Combine(dst, f), ref copied, ref skipped);
            }
            for (int i = 0; i < Folders.Length; i++)
            {
                string from = Path.Combine(src, Folders[i]);
                if (!Directory.Exists(from)) continue;
                string[] all = Directory.GetFiles(from, "*", SearchOption.AllDirectories);
                for (int n = 0; n < all.Length; n++)
                {
                    if (n % 200 == 0 && EditorUtility.DisplayCancelableProgressBar("Copying auto-chart tools into the build",
                            Folders[i] + "  (" + n + " / " + all.Length + ")", (float)n / all.Length))
                    {
                        Debug.LogWarning("AutoChartBuildHook: copy cancelled; the build's Tools folder is incomplete.");
                        return;
                    }
                    string rel = all[n].Substring(from.Length).TrimStart('\\', '/');
                    //Python's own bytecode caches are rebuilt on the other PC
                    if (rel.Contains("__pycache__")) continue;
                    CopyIfChanged(all[n], Path.Combine(dst, Folders[i], rel), ref copied, ref skipped);
                }
            }
            //Older builds stored this PC's Tools path here; not needed when Tools sits next to the .exe
            string exe = report.summary.outputPath;
            string pathFile = Path.Combine(Path.GetDirectoryName(exe), Path.GetFileNameWithoutExtension(exe) + "_Data", "StreamingAssets", "autochart_tools.txt");
            if (File.Exists(pathFile)) File.Delete(pathFile);
        }
        catch (Exception e)
        {
            Debug.LogError("AutoChartBuildHook: copying Tools failed: " + e.Message);
            return;
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
        Debug.Log($"AutoChartBuildHook: auto-chart tools ready in {dst} ({copied} files copied, {skipped} already up to date).");
    }

    static void CopyIfChanged(string from, string to, ref int copied, ref int skipped)
    {
        var a = new FileInfo(from);
        var b = new FileInfo(to);
        if (b.Exists && b.Length == a.Length && b.LastWriteTimeUtc == a.LastWriteTimeUtc)
        {
            skipped++;
            return;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(to));
        File.Copy(from, to, true);
        File.SetLastWriteTimeUtc(to, a.LastWriteTimeUtc);
        copied++;
    }
}
