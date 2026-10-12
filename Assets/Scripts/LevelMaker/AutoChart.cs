using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Debug = UnityEngine.Debug;

//Runs Tools/auto_chart.py (Demucs + wav2vec2) on a video's audio and a lyrics .txt, in the background.
//Uses the self-contained Tools folder (Tools/python, Tools/models, Tools/bin/ffmpeg.exe) next to the game,
//copied there by AutoChartBuildHook on build, so it works on any Windows PC with nothing installed.
//The Level Maker setup screen owns the UI.
public class AutoChart
{
    public class Options
    {
        public string video;
        public string lyrics;
        public string outputFolder;
        public string name;            //output files are <name>_notes.csv, <name>_held.csv, ...
        public int wordsPerSubtitle;   //0 = whole lyric line
        public bool lyricVideo;
        public string karaoke;         //"fill", "box" or "plain"
        public bool lyricVideoAudio;   //include the song's audio in the lyric video
        public string title, artist;   //shown at the top of the lyric video ("" = none)
        public float holdMin;          //seconds
        public float offset;           //seconds
        public float fps;              //frame rate of the video (timecodes, EDL and lyric video use it)
    }

    public class Result
    {
        public string error = "";      //empty on success
        public string summary = "";
        public string notes, held, folder;
        public string lyricVideo;      //null when no lyric video was made
        public string drums;           //drum hits CSV (for drum animations)
        public string lyricsJson;      //word timings for lyrics shown in game
    }

    //Written by the worker thread, read on the main thread
    private readonly object gate = new object();
    private string stage = "";
    private string percent = "";
    private string summary = "";
    private string error = "";
    private bool running;
    private bool finished;
    private string notesOut, heldOut, outFolder, videoOut, drumsOut, lyricsOut;
    private Process process;

    //Resolved on the main thread (Unity's Application paths can't be read from the worker thread)
    private string toolsDir, python, script, workRoot, ffmpegPath;

    //File (in StreamingAssets of a build) holding the project's Tools path, written at build time
    //by the AutoChartBuildHook editor script
    public const string ToolsPathFile = "autochart_tools.txt";

    //Where the auto_chart.py + .venv setup lives, or null if it can't be found. Checked in order:
    //  1. <data folder>/../Tools  - the project's Tools folder in the editor, or a Tools folder
    //                               placed next to the game's .exe in a build
    //  2. the path recorded in StreamingAssets/autochart_tools.txt when the build was made, so a
    //     build on the same PC uses the project's Tools without copying the (huge) .venv
    public static string FindToolsDir()
    {
        string local = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Tools"));
        if (IsToolsDir(local)) return local;
        try
        {
            string file = Path.Combine(Application.streamingAssetsPath, ToolsPathFile);
            if (File.Exists(file))
            {
                string recorded = File.ReadAllText(file).Trim();
                if (IsToolsDir(recorded)) return recorded;
            }
        }
        catch (Exception) { }
        return null;
    }

    static bool IsToolsDir(string dir)
    {
        return !string.IsNullOrEmpty(dir) &&
               PythonIn(dir) != null &&
               File.Exists(Path.Combine(dir, "auto_chart.py"));
    }

    //The self-contained Python bundled in Tools/python (works on any PC) if present,
    //otherwise the developer's Tools/.venv (only works on the PC it was made on)
    static string PythonIn(string tools)
    {
        string bundled = Path.Combine(tools, "python", "python.exe");
        if (File.Exists(bundled)) return bundled;
        string venv = Path.Combine(tools, ".venv", "Scripts", "python.exe");
        return File.Exists(venv) ? venv : null;
    }

    public static bool Available => FindToolsDir() != null;

    public bool Running
    {
        get { lock (gate) return running; }
    }

    //Current step for the status line, e.g. "Separating vocals (slowest step)...  42%"
    public string Status
    {
        get
        {
            lock (gate) return stage + (percent.Length > 0 ? "  " + percent : "");
        }
    }

    //Starts a run. Returns an error message if it can't start, otherwise null.
    public string Start(Options o)
    {
        if (Running) return "Already running.";
        toolsDir = FindToolsDir();
        if (toolsDir == null) return "Auto-chart tools not found (Tools folder with python/ and auto_chart.py).";
        string ffmpeg = FindFfmpeg(toolsDir);
        if (ffmpeg == null) return "FFmpeg not found (needed to read the video's audio).";
        python = PythonIn(toolsDir);
        script = Path.Combine(toolsDir, "auto_chart.py");
        workRoot = Path.Combine(Application.persistentDataPath, "autochart");

        lock (gate)
        {
            running = true;
            finished = false;
            stage = "Starting...";
            percent = summary = error = "";
        }
        ffmpegPath = ffmpeg;
        new System.Threading.Thread(() => Work(ffmpeg, o)) { IsBackground = true }.Start();
        return null;
    }

    //True once, on the frame after a run ends
    public bool Poll(out Result result)
    {
        result = null;
        lock (gate)
        {
            if (!finished) return false;
            finished = false;
            result = new Result { error = error, summary = summary, notes = notesOut, held = heldOut, folder = outFolder, lyricVideo = videoOut, drums = drumsOut, lyricsJson = lyricsOut };
            return true;
        }
    }

    //Leaving the screen mid-run: don't leave Python running in the background
    public void Cancel()
    {
        try
        {
            if (process != null && !process.HasExited) process.Kill();
        }
        catch (Exception) { }
    }

    public static string SafeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        return name.Trim();
    }

    //---------------- Worker thread ----------------

    private void Work(string ffmpeg, Options o)
    {
        try
        {
            //Audio and the vocal stem live in a work folder per video (the stem is cached there,
            //so re-running after fixing the lyrics skips the slow separation step)
            string work = Path.Combine(workRoot, SafeFileName(Path.GetFileNameWithoutExtension(o.video)));
            Directory.CreateDirectory(work);
            string audio = Path.Combine(work, "song.wav");

            outFolder = o.outputFolder;
            Directory.CreateDirectory(outFolder);
            notesOut = Path.Combine(outFolder, o.name + "_notes.csv");
            heldOut = Path.Combine(outFolder, o.name + "_held.csv");
            videoOut = o.lyricVideo ? Path.Combine(outFolder, o.name + "_lyrics.mp4") : null;
            drumsOut = Path.Combine(outFolder, o.name + "_drums.csv");
            lyricsOut = Path.Combine(outFolder, o.name + "_lyrics.json");

            if (!File.Exists(audio))
            {
                Stage("Extracting audio...");
                int code = Run(ffmpeg, $"-y -loglevel error -i \"{o.video}\" -vn -ac 2 -ar 44100 \"{audio}\"", work);
                if (code != 0) throw new Exception("FFmpeg couldn't read the video's audio.");
            }

            Stage("Loading models...");
            CultureInfo inv = CultureInfo.InvariantCulture;
            string args = $"-u \"{script}\" \"{audio}\" \"{o.lyrics}\" -o \"{notesOut}\"" +
                          $" --words {o.wordsPerSubtitle} --karaoke {o.karaoke}" +
                          $" --hold-min {o.holdMin.ToString("0.###", inv)} --offset {o.offset.ToString("0.###", inv)}" +
                          (o.fps > 0f ? " --fps " + o.fps.ToString("0.#####", inv) : "") +
                          (o.lyricVideo ? "" : " --no-video") +
                          (o.lyricVideoAudio ? "" : " --no-audio") +
                          (string.IsNullOrWhiteSpace(o.title) ? "" : " --title " + Quote(o.title)) +
                          (string.IsNullOrWhiteSpace(o.artist) ? "" : " --artist " + Quote(o.artist));
            int rc = Run(python, args, toolsDir);
            if (rc != 0 || !File.Exists(notesOut))
            {
                string why;
                lock (gate) why = error;
                throw new Exception(why.Length > 0 ? why : "Auto-chart failed (exit code " + rc + ").");
            }
            lock (gate) error = "";
        }
        catch (Exception e)
        {
            lock (gate) error = e.Message;
            Debug.LogWarning("Auto-chart: " + e);
        }
        finally
        {
            lock (gate)
            {
                running = false;
                finished = true;
            }
        }
    }

    //A command-line argument in quotes (inner double quotes would end it early)
    private static string Quote(string s) => "\"" + s.Trim().Replace("\"", "'") + "\"";

    private void Stage(string s)
    {
        lock (gate)
        {
            stage = s;
            percent = "";
        }
    }

    //Runs a process, feeding its output into the status line. Returns the exit code.
    private int Run(string exe, string args, string workingDir)
    {
        var psi = new ProcessStartInfo(exe, args)
        {
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        psi.EnvironmentVariables["PYTHONUNBUFFERED"] = "1";
        psi.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
        //The script renders the lyric video with this ffmpeg (it may not be on PATH)
        if (ffmpegPath != null) psi.EnvironmentVariables["FFMPEG"] = ffmpegPath;
        //Self-contained tools: bundled ffmpeg first on PATH, and the AI models loaded from
        //Tools/models with no downloads, so it works on a PC that has never run it before
        string bin = Path.Combine(toolsDir, "bin");
        if (Directory.Exists(bin))
            psi.EnvironmentVariables["PATH"] = bin + Path.PathSeparator + psi.EnvironmentVariables["PATH"];
        string models = Path.Combine(toolsDir, "models");
        if (Directory.Exists(models))
        {
            psi.EnvironmentVariables["HF_HOME"] = Path.Combine(models, "huggingface");
            psi.EnvironmentVariables["TORCH_HOME"] = Path.Combine(models, "torch");
            psi.EnvironmentVariables["HF_HUB_OFFLINE"] = "1";
            psi.EnvironmentVariables["TRANSFORMERS_OFFLINE"] = "1";
        }

        using (Process p = Process.Start(psi))
        {
            process = p;
            var errReader = new System.Threading.Thread(() => Pump(p.StandardError, true)) { IsBackground = true };
            errReader.Start();
            Pump(p.StandardOutput, false);
            errReader.Join();
            p.WaitForExit();
            process = null;
            return p.ExitCode;
        }
    }

    private static readonly Regex Percent = new Regex(@"(\d{1,3})%\|");

    //Reads a stream splitting on \r as well as \n, so tqdm progress bars update live
    private void Pump(StreamReader reader, bool isErr)
    {
        var line = new StringBuilder();
        int ch;
        while ((ch = reader.Read()) >= 0)
        {
            if (ch == '\r' || ch == '\n')
            {
                if (line.Length > 0) HandleLine(line.ToString(), isErr);
                line.Clear();
            }
            else
            {
                line.Append((char)ch);
            }
        }
        if (line.Length > 0) HandleLine(line.ToString(), isErr);
    }

    private void HandleLine(string l, bool isErr)
    {
        Debug.Log("[auto_chart] " + l);
        lock (gate)
        {
            Match m = Percent.Match(l);
            if (m.Success)
            {
                percent = m.Groups[1].Value + "%";
                return;
            }
            if (l.StartsWith("Separating vocals")) { stage = "Separating vocals (slowest step)..."; percent = ""; }
            else if (l.StartsWith("Using cached")) { stage = "Using cached vocals..."; percent = ""; }
            else if (l.StartsWith("Aligning")) { stage = l.TrimEnd('.') + "..."; percent = ""; }
            else if (l.StartsWith("Rendering lyric video")) { stage = "Rendering lyric video..."; percent = ""; }
            else if (Regex.IsMatch(l, @"^\d+ words ->")) summary = l.Substring(l.IndexOf("->") + 3);
            //The script exits with a plain message for bad input; Python errors end with "SomeError: ..."
            else if (l.StartsWith("Song not found") || l.StartsWith("Lyrics not found") || l.StartsWith("No words found"))
                error = l;
            else if (isErr && Regex.IsMatch(l, @"^\w+(Error|Exception): ")) error = l;
        }
    }

    //---------------- FFmpeg lookup ----------------

    //Tools/bin/ffmpeg.exe (bundled) first, then PATH, then WinGet installs
    private static string FindFfmpeg(string tools)
    {
        string bundled = tools != null ? Path.Combine(tools, "bin", "ffmpeg.exe") : null;
        if (bundled != null && File.Exists(bundled)) return bundled;
        string path = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (string dir in path.Split(Path.PathSeparator))
        {
            try
            {
                string exe = Path.Combine(dir.Trim(), "ffmpeg.exe");
                if (File.Exists(exe)) return exe;
            }
            catch (Exception) { }
        }
        //WinGet installs (e.g. the one that comes with yt-dlp) when PATH isn't inherited
        string winget = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WinGet", "Packages");
        if (Directory.Exists(winget))
        {
            foreach (string exe in Directory.GetFiles(winget, "ffmpeg.exe", SearchOption.AllDirectories))
                return exe;
        }
        return null;
    }
}
