using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using SimpleFileBrowser;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//Level Maker setup screen, split in two halves:
//  left  - visuals: level name, video preview, cover image
//  right - chart: tap / held note lists, and auto-charting from lyrics with its settings
//Built on its own canvas (fixed 1920x1080 layout scaled to fit). The scene's own controls
//(pickers, name field, previews, back / proceed) are moved into the layout, so FileSelection
//and SwitchScene keep working exactly as before.
public class LevelSetupScreen : MonoBehaviour
{
    private const float Margin = 48f, PanelTop = 112f, PanelW = 896f, PanelH = 920f, Pad = 32f;
    private const float Inner = PanelW - 2 * Pad;
    private const float RowH = 44f, LabelW = 230f;
    private const int MaxListLines = 600;

    private static LevelSetupScreen instance;

    private FileSelection sel;
    private RectTransform root;

    //Visuals
    private TextMeshProUGUI videoName, coverName, videoEmpty, coverEmpty;
    private AspectRatioFitter videoFit, coverFit;

    //Note lists
    private TextMeshProUGUI tapHeader, heldHeader, tapList, heldList;
    private string shownNotes = "\0", shownHeld = "\0", shownVid = "\0", shownImage = "\0";

    //Auto-chart
    private readonly AutoChart runner = new AutoChart();
    private TextMeshProUGUI lyricsLabel, outputLabel, wordsValue, wordsHint, status, runLabel;
    private Button runButton, showFilesButton;
    private Image fillChoice, boxChoice, plainChoice, gameChoice, videoChoice, offChoice;
    private string lastFolder;

    //The video the player picked (charted from), and the lyric video that replaced it, if any
    private string sourceVideo, generatedVideo;
    private string SourceVideo => !string.IsNullOrEmpty(sourceVideo) ? sourceVideo : FileSelection.theVid;
    public static string ChartVideo => instance != null ? instance.SourceVideo : null;

    //Auto-chart settings (remembered between sessions)
    private static string lyricsPath;
    private string outputFolder;
    private int words;
    private string lyricsMode;  //"game", "video" or "off"
    private bool lyricAudio;   //song audio in the lyric video
    private bool lyricTitle;   //song name + artist at the top of the lyric video
    private string karaoke;
    private float holdMin;
    private int offsetMs;

    //Volume of the picked video's preview, separate from the game volume
    private float previewVolume;

    public static void Attach(FileSelection selection)
    {
        Canvas canvas = UIFactory.OverlayCanvas("LevelSetup", 5);
        canvas.GetComponent<CanvasScaler>().screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;
        canvas.gameObject.AddComponent<GraphicRaycaster>();
        instance = canvas.gameObject.AddComponent<LevelSetupScreen>();
        instance.sel = selection;
        instance.LoadSettings();
        instance.Build();
    }

    //Shown in the status line, e.g. when Proceed is pressed without a name
    public static void ShowMessage(string text, bool error)
    {
        if (instance != null) instance.SetStatus(text, error ? Theme.Miss : Theme.TextMuted);
    }

    //---------------- Settings ----------------

    private void LoadSettings()
    {
        outputFolder = PlayerPrefs.GetString("autochart.output", "");
        if (outputFolder == "" || !Directory.Exists(outputFolder)) outputFolder = FileSelection.DownloadsFolder();
        words = PlayerPrefs.GetInt("autochart.words", 1);
        lyricsMode = PlayerPrefs.GetString("autochart.lyrics", LyricsDisplay.InGame ? "game" : "video");
        if (!LyricsDisplay.InGame && lyricsMode == "game") lyricsMode = "video";
        lyricAudio = PlayerPrefs.GetInt("autochart.videoAudio", 1) == 1;
        lyricTitle = PlayerPrefs.GetInt("autochart.videoTitle", 1) == 1;
        karaoke = PlayerPrefs.GetString("autochart.karaoke", "fill");
        holdMin = PlayerPrefs.GetFloat("autochart.holdMin", 1f);
        offsetMs = PlayerPrefs.GetInt("autochart.offsetMs", 0);
        previewVolume = PlayerPrefs.GetFloat("levelmaker.previewVolume", 0.5f);
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetString("autochart.output", outputFolder);
        PlayerPrefs.SetInt("autochart.words", words);
        PlayerPrefs.SetString("autochart.lyrics", lyricsMode);
        PlayerPrefs.SetInt("autochart.videoAudio", lyricAudio ? 1 : 0);
        PlayerPrefs.SetInt("autochart.videoTitle", lyricTitle ? 1 : 0);
        PlayerPrefs.SetString("autochart.karaoke", karaoke);
        PlayerPrefs.SetFloat("autochart.holdMin", holdMin);
        PlayerPrefs.SetInt("autochart.offsetMs", offsetMs);
        PlayerPrefs.SetFloat("levelmaker.previewVolume", previewVolume);
        PlayerPrefs.Save();
    }

    //---------------- Layout ----------------

    private void Build()
    {
        root = new GameObject("Layout", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(transform, false);
        root.sizeDelta = new Vector2(1920f, 1080f);

        Transform sceneCanvas = sel.nameField.transform.parent;
        Transform oldTitle = sceneCanvas.Find("Title");
        if (oldTitle != null) oldTitle.gameObject.SetActive(false);

        BuildHeader();
        BuildVisuals(Panel(Margin));
        BuildChart(Panel(1920f - Margin - PanelW));
    }

    private void BuildHeader()
    {
        SwitchScene back = sel.GetComponent<SwitchScene>();
        if (back != null && back.playButton != null)
            Adopt(back.playButton, root, Margin, 36, 150, 48, "BACK", false);

        TextMeshProUGUI title = Label(root, "LEVEL MAKER", Margin + 150 + 28, 36, 600, 48, 34, true, Theme.Text, TextAlignmentOptions.Left);
        title.characterSpacing = 6;

        Adopt(sel.done, root, 1920f - Margin - 240, 36, 240, 48, "SAVE LEVEL", true);
    }

    private RectTransform Panel(float x)
    {
        Image bg = Box(root, x, PanelTop, PanelW, PanelH, Theme.Hex("#1D1F26", 0.94f));
        bg.sprite = Theme.RoundedRect;
        bg.type = Image.Type.Sliced;
        bg.pixelsPerUnitMultiplier = 1.2f;
        Image strip = Box(bg.rectTransform, 14, 0, PanelW - 28, 3, Theme.Accent);
        strip.sprite = Theme.Square;
        return bg.rectTransform;
    }

    private void BuildVisuals(RectTransform p)
    {
        SectionTitle(p, "VISUALS", 28);

        //Level name
        SmallLabel(p, "LEVEL NAME", Pad, 84);
        RectTransform name = (RectTransform)sel.nameField.transform;
        name.SetParent(p, false);
        Place(name, Pad, 110, Inner, 56);
        if (sel.nameField.textComponent != null)
        {
            sel.nameField.textComponent.fontSize = 24;
            sel.nameField.textComponent.alignment = TextAlignmentOptions.MidlineLeft;
        }
        if (sel.nameField.placeholder is TextMeshProUGUI ph)
        {
            ph.text = "Song Name by Artist Name";
            ph.fontSize = 24;
            ph.fontStyle = FontStyles.Italic;
            ph.alignment = TextAlignmentOptions.MidlineLeft;
            ph.textWrappingMode = TextWrappingModes.NoWrap;
        }

        //Video: 16:9 frame, the preview keeps the video's own aspect inside it
        SmallLabel(p, "VIDEO", Pad, 190);
        videoName = Label(p, "", Pad + 120, 190, Inner - 120, 24, 16, false, Theme.TextMuted, TextAlignmentOptions.Right);
        videoName.overflowMode = TextOverflowModes.Ellipsis;
        RectTransform videoFrame = Frame(p, Pad + (Inner - 768) / 2, 216, 768, 432);
        videoFit = Fit(sel.vidParent, videoFrame);
        videoEmpty = Label(videoFrame, "No video selected", 0, 0, 768, 432, 20, false, Theme.TextMuted, TextAlignmentOptions.Center);
        Adopt(sel.vidSelect, p, Pad, 660, 360, 44, "SELECT VIDEO", false);

        //Preview volume beside the picker
        TextMeshProUGUI volLabel = Label(p, "PREVIEW VOLUME", Pad + 384, 660, 170, 44, 15, true, Theme.TextMuted, TextAlignmentOptions.Left);
        volLabel.characterSpacing = 3;
        Slider vol = UIFactory.MakeSlider(p, new Vector2(204, 20), 0f, 1f, false, previewVolume);
        Place((RectTransform)vol.transform, Pad + 560, 672, 204, 20);
        TextMeshProUGUI volValue = Label(p, Pct(previewVolume), Pad + Inner - 60, 660, 60, 44, 18, true, Theme.Text, TextAlignmentOptions.Right);
        vol.onValueChanged.AddListener(v =>
        {
            previewVolume = v;
            volValue.text = Pct(v);
            ApplyPreviewVolume();
            SaveSettings();
        });

        //Cover image: square frame with picker and hints beside it
        SmallLabel(p, "COVER IMAGE", Pad, 728);
        RectTransform coverFrame = Frame(p, Pad, 754, 134, 134);
        coverFit = Fit(sel.imageDisplayer, coverFrame);
        coverEmpty = Label(coverFrame, "None", 0, 0, 134, 134, 16, false, Theme.TextMuted, TextAlignmentOptions.Center);
        float cx = Pad + 134 + 24;
        Adopt(sel.imageSelect, p, cx, 754, 260, 44, "SELECT IMAGE", false);
        coverName = Label(p, "", cx, 808, Inner - 158, 24, 16, false, Theme.TextMuted, TextAlignmentOptions.Left);
        coverName.overflowMode = TextOverflowModes.Ellipsis;
        Label(p, "Shown in Song Select. Square images look best (1024 × 1024).", cx, 838, Inner - 158, 24, 16, false, Theme.TextMuted, TextAlignmentOptions.Left);
    }

    private void BuildChart(RectTransform p)
    {
        SectionTitle(p, "CHART", 28);

        //Note lists side by side
        const float colW = (Inner - 16) / 2, listY = 112, listH = 168;
        float heldX = Pad + colW + 16;
        tapHeader = SmallLabel(p, "TAP NOTES", Pad, 84);
        heldHeader = SmallLabel(p, "HELD NOTES", heldX, 84);
        tapList = ScrollList(p, Pad, listY, colW, listH);
        heldList = ScrollList(p, heldX, listY, colW, listH);
        Adopt(sel.noteSelect, p, Pad, listY + listH + 12, colW, 44, "LOAD TAP CSV", false);
        Adopt(sel.heldSelect, p, heldX, listY + listH + 12, colW, 44, "LOAD HELD CSV", false);

        Image divider = Box(p, Pad, 356, Inner, 1, Theme.Track);
        divider.sprite = Theme.Square;

        //Auto-chart
        SectionTitle(p, "AUTO-CHART FROM LYRICS AND VIDEO", 374);
        Label(p, "Pick the video and a lyrics .txt, then press Auto-chart. Takes about a minute.",
            Pad, 406, Inner, 26, 16, false, Theme.TextMuted, TextAlignmentOptions.Left);
        RectTransform rows = new GameObject("AutoChart", typeof(RectTransform)).GetComponent<RectTransform>();
        rows.SetParent(p, false);
        Place(rows, 0, 0, PanelW, PanelH);
        CanvasGroup autoGroup = rows.gameObject.AddComponent<CanvasGroup>();

        const float y0 = 442;
        float controlX = Pad + LabelW;
        float controlW = Inner - LabelW;
        const float browseW = 130;

        //Lyrics file
        Row(rows, 0, y0, "LYRICS (.TXT)");
        lyricsLabel = PathLabel(rows, controlX, y0, controlW - browseW - 12);
        MakeButton(rows, "BROWSE", Pad + Inner - browseW, y0 + 4, browseW, RowH - 8, false, () => StartCoroutine(PickLyrics(false)));

        //Output folder
        float y = y0 + RowH;
        Row(rows, 1, y0, "OUTPUT FOLDER");
        outputLabel = PathLabel(rows, controlX, y, controlW - browseW - 12);
        MakeButton(rows, "BROWSE", Pad + Inner - browseW, y + 4, browseW, RowH - 8, false, () => StartCoroutine(PickOutputFolder()));

        //Words per subtitle: - N +
        y += RowH;
        Row(rows, 2, y0, "WORDS PER SUBTITLE");
        MakeButton(rows, "−", controlX, y + 6, 40, 32, false, () => SetWords(words - 1));
        wordsValue = Label(rows, "", controlX + 44, y, 48, RowH, 20, true, Theme.Text, TextAlignmentOptions.Center);
        MakeButton(rows, "+", controlX + 96, y + 6, 40, 32, false, () => SetWords(words + 1));
        wordsHint = Label(rows, "", controlX + 152, y, controlW - 152, RowH, 16, false, Theme.TextMuted, TextAlignmentOptions.Left);

        //Where the lyrics show: drawn by the game, baked into a lyric video, or not at all
        y += RowH;
        Row(rows, 3, y0, "LYRICS");
        //"In game" only while in-game lyrics are on (LyricsDisplay.InGame)
        float lx = controlX;
        gameChoice = MakeButton(rows, "IN GAME", lx, y + 6, 120, 32, false, () => SetLyricsMode("game")).GetComponent<Image>();
        gameChoice.gameObject.SetActive(LyricsDisplay.InGame);
        if (LyricsDisplay.InGame) lx += 126;
        videoChoice = MakeButton(rows, "VIDEO", lx, y + 6, 120, 32, false, () => SetLyricsMode("video")).GetComponent<Image>();
        offChoice = MakeButton(rows, "OFF", lx + 126, y + 6, 120, 32, false, () => SetLyricsMode("off")).GetComponent<Image>();

        //Highlight style (in game and in the video)
        y += RowH;
        Row(rows, 4, y0, "LYRIC STYLE");
        fillChoice = MakeButton(rows, "FILL", controlX, y + 6, 100, 32, false, () => SetKaraoke("fill")).GetComponent<Image>();
        boxChoice = MakeButton(rows, "BOX", controlX + 106, y + 6, 100, 32, false, () => SetKaraoke("box")).GetComponent<Image>();
        plainChoice = MakeButton(rows, "PLAIN", controlX + 212, y + 6, 100, 32, false, () => SetKaraoke("plain")).GetComponent<Image>();

        //Extras: song audio (lyric video only), song name + artist at the top
        y += RowH;
        Row(rows, 5, y0, "EXTRAS");
        RectTransform extras = new GameObject("Extras", typeof(RectTransform)).GetComponent<RectTransform>();
        extras.SetParent(rows, false);
        Place(extras, controlX, y, controlW, RowH);
        Toggle(extras, 0, "Video audio", lyricAudio, v => lyricAudio = v);
        Toggle(extras, 196, "Name & artist", lyricTitle, v => lyricTitle = v);
        Label(extras, "from level name", 420, 0, controlW - 420, RowH, 14, false, Theme.TextMuted, TextAlignmentOptions.Left);

        //Hold length
        y += RowH;
        Row(rows, 6, y0, "HOLD NOTES FROM");
        SliderRow(rows, controlX, y, controlW, 0.5f, 3f, false, holdMin,
            v => holdMin = Mathf.Round(v * 10f) / 10f,
            () => holdMin.ToString("0.0", CultureInfo.InvariantCulture) + " s");

        //Timing offset
        y += RowH;
        Row(rows, 7, y0, "TIMING OFFSET");
        SliderRow(rows, controlX, y, controlW, -200, 200, true, offsetMs,
            v => offsetMs = Mathf.RoundToInt(v),
            () => (offsetMs > 0 ? "+" : "") + offsetMs + " ms");

        //Status + run
        y += RowH + 4;
        status = Label(p, "", Pad, y, Inner - 160, 30, 17, false, Theme.TextMuted, TextAlignmentOptions.Left);
        status.overflowMode = TextOverflowModes.Ellipsis;
        showFilesButton = MakeButton(rows, "SHOW FILES", Pad + Inner - 150, y - 2, 150, 34, false, ShowFiles);
        showFilesButton.gameObject.SetActive(false);

        runButton = MakeButton(rows, "AUTO-CHART", Pad, PanelH - Pad - 56, Inner, 56, true, OnRun);
        runLabel = runButton.GetComponentInChildren<TextMeshProUGUI>();

        SetWords(words);
        RefreshStyle();
        RefreshPaths();

        if (!AutoChart.Available)
        {
            //Tool isn't installed (e.g. a standalone build): show the section, but locked
            autoGroup.alpha = 0.4f;
            autoGroup.interactable = false;
            autoGroup.blocksRaycasts = false;
            SetStatus("Auto-chart tools not found: the Tools folder must be next to the game (it is copied there when you build).", Theme.Miss);
        }
    }

    //---------------- Auto-chart ----------------

    private void SetWords(int n)
    {
        words = Mathf.Clamp(n, 0, 12);
        wordsValue.text = words == 0 ? "ALL" : words.ToString();
        wordsHint.text = words == 0 ? "whole lyric line at a time" : words == 1 ? "one word at a time" : words + " words at a time";
        SaveSettings();
    }

    private void SetKaraoke(string k)
    {
        karaoke = k;
        RefreshStyle();
        SaveSettings();
    }

    private void RefreshStyle()
    {
        Choice(fillChoice, karaoke == "fill");
        Choice(boxChoice, karaoke == "box");
        Choice(plainChoice, karaoke == "plain");
        Choice(gameChoice, lyricsMode == "game");
        Choice(videoChoice, lyricsMode == "video");
        Choice(offChoice, lyricsMode == "off");
    }

    private void SetLyricsMode(string mode)
    {
        lyricsMode = mode;
        RefreshStyle();
        SaveSettings();
    }

    //Switch with a label to its right
    private void Toggle(Transform parent, float x, string label, bool initial, Action<bool> set)
    {
        UISwitch sw = UISwitch.Create(parent, initial);
        Place((RectTransform)sw.transform, x, (RowH - 28) / 2, 52, 28);
        sw.Changed += v =>
        {
            set(v);
            SaveSettings();
        };
        Label(parent, label, x + 62, 0, 130, RowH, 17, false, Theme.Text, TextAlignmentOptions.Left);
    }

    //"Song Name by Artist Name" -> ("Song Name", "Artist Name"); "Artist - Song" works too
    private static void SplitLevelName(string name, out string title, out string artist)
    {
        LevelNames.Split(name, out artist, out title);
    }

    private static void Choice(Image img, bool selected)
    {
        Button b = img.GetComponent<Button>();
        ColorBlock cb = b.colors;
        cb.normalColor = selected ? Theme.Accent : Theme.Surface;
        cb.highlightedColor = selected ? Theme.Accent.WithAlpha(0.85f) : Theme.SurfaceHover;
        cb.selectedColor = cb.normalColor;
        b.colors = cb;
        img.GetComponentInChildren<TextMeshProUGUI>().color = selected ? Theme.Background : Theme.Text;
    }

    private void RefreshPaths()
    {
        bool haveLyrics = !string.IsNullOrEmpty(lyricsPath) && File.Exists(lyricsPath);
        lyricsLabel.text = haveLyrics ? Path.GetFileName(lyricsPath) : "None selected";
        lyricsLabel.color = haveLyrics ? Theme.Text : Theme.TextMuted;
        outputLabel.text = outputFolder;
        outputLabel.color = Theme.Text;
    }

    private IEnumerator PickLyrics(bool thenRun)
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Lyrics", ".txt"));
        FileBrowser.SetDefaultFilter(".txt");
        string start = !string.IsNullOrEmpty(lyricsPath) ? Path.GetDirectoryName(lyricsPath) : FileSelection.DownloadsFolder();
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, start, null, "Select Lyrics (.txt)", "Select");
        if (!FileBrowser.Success) yield break;
        lyricsPath = FileBrowser.Result[0];
        RefreshPaths();
        if (thenRun) OnRun();
    }

    private IEnumerator PickOutputFolder()
    {
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Folders, false, outputFolder, null, "Select Output Folder", "Select");
        if (!FileBrowser.Success) yield break;
        outputFolder = FileBrowser.Result[0];
        RefreshPaths();
        SaveSettings();
    }

    private void OnRun()
    {
        if (runner.Running) return;
        //Chart from the video the player picked, even after a lyric video has replaced it
        string video = SourceVideo;
        if (string.IsNullOrEmpty(video) || !File.Exists(video))
        {
            SetStatus("Select the video first (its audio is what gets charted).", Theme.Miss);
            return;
        }
        if (string.IsNullOrEmpty(lyricsPath) || !File.Exists(lyricsPath))
        {
            StartCoroutine(PickLyrics(true));
            return;
        }

        //Outputs are named after the level, or the video if the level has no name yet
        string levelName = string.IsNullOrWhiteSpace(FileSelection.theName)
            ? Path.GetFileNameWithoutExtension(video) : FileSelection.theName;
        string name = AutoChart.SafeFileName(levelName);
        string title = "", artist = "";
        if (lyricTitle) SplitLevelName(levelName, out title, out artist);
        string err = runner.Start(new AutoChart.Options
        {
            video = video,
            lyrics = lyricsPath,
            outputFolder = outputFolder,
            name = name,
            wordsPerSubtitle = words,
            lyricVideo = lyricsMode == "video",
            karaoke = karaoke,
            lyricVideoAudio = lyricAudio,
            title = title,
            artist = artist,
            holdMin = holdMin,
            fps = FileSelection.ChartFps,
            offset = offsetMs / 1000f,
        });
        if (err != null)
        {
            SetStatus(err, Theme.Miss);
            return;
        }
        runButton.interactable = false;
        showFilesButton.gameObject.SetActive(false);
    }

    private void ShowFiles()
    {
        if (string.IsNullOrEmpty(lastFolder) || !Directory.Exists(lastFolder)) return;
        try
        {
            System.Diagnostics.Process.Start("explorer.exe", "\"" + lastFolder + "\"");
        }
        catch (Exception e)
        {
            Debug.LogWarning("Couldn't open folder: " + e.Message);
        }
    }

    private void SetStatus(string text, Color color)
    {
        status.text = text;
        status.color = color;
    }

    //---------------- Per frame ----------------

    void Update()
    {
        if (runner.Running)
        {
            float dots = Mathf.Repeat(Time.unscaledTime * 2f, 4f);
            runLabel.text = "WORKING" + new string('.', (int)dots);
            runLabel.color = Theme.Text;      //the disabled button is dark, so the usual dark label would vanish
            SetStatus(runner.Status, Theme.TextMuted);
        }
        if (runner.Poll(out AutoChart.Result r))
        {
            runButton.interactable = true;
            runLabel.text = "AUTO-CHART";
            runLabel.color = Theme.Background;
            if (r.error.Length > 0)
            {
                SetStatus(r.error, Theme.Miss);
            }
            else
            {
                //Hand the generated charts to the level setup, as if picked by hand
                FileSelection.notePath = r.notes;
                FileSelection.heldPath = File.Exists(r.held) && CsvParser.ParseTimeColumn(r.held, 10, FileSelection.ChartFps).Length > 0 ? r.held : null;
                FileSelection.drumsPath = File.Exists(r.drums) ? r.drums : null;
                //In-game lyrics: the level gets lyrics.json (video mode bakes them into the video instead)
                FileSelection.lyricsPath = lyricsMode == "game" && File.Exists(r.lyricsJson) ? r.lyricsJson : null;
                lastFolder = r.folder;
                showFilesButton.gameObject.SetActive(true);
                string done = "Done: " + r.summary;

                //The lyric video becomes the level's video. The level plays the video's
                //sound, so only swap when the lyric video has the song in it.
                if (!string.IsNullOrEmpty(r.lyricVideo) && File.Exists(r.lyricVideo))
                {
                    if (lyricAudio)
                    {
                        sourceVideo = SourceVideo;
                        generatedVideo = r.lyricVideo;
                        sel.LoadVideo(r.lyricVideo);
                        done += ". Lyric video set as the level video";
                    }
                    else
                    {
                        done += ". Lyric video has no audio, so the original video stays";
                    }
                }
                SetStatus(done, Theme.Great);
            }
        }

        //The pickers live in FileSelection; follow whatever they last chose
        if (FileSelection.notePath != shownNotes)
        {
            shownNotes = FileSelection.notePath;
            ShowTaps(shownNotes);
        }
        if (FileSelection.heldPath != shownHeld)
        {
            shownHeld = FileSelection.heldPath;
            ShowHeld(shownHeld);
        }
        if (FileSelection.theVid != shownVid)
        {
            shownVid = FileSelection.theVid;
            //Picked by hand (not our lyric video): that's the new source to chart from
            if (shownVid != generatedVideo)
            {
                sourceVideo = shownVid;
                FileSelection.drumsPath = null;   //drum hits belonged to the previous video
                FileSelection.lyricsPath = null;  //so did the lyrics
            }
            bool has = !string.IsNullOrEmpty(shownVid);
            videoName.text = has ? Path.GetFileName(shownVid) : "";
            videoEmpty.gameObject.SetActive(!has);
        }
        if (FileSelection.theImage != shownImage)
        {
            shownImage = FileSelection.theImage;
            bool has = !string.IsNullOrEmpty(shownImage);
            coverName.text = has ? Path.GetFileName(shownImage) : "No image selected";
            coverEmpty.gameObject.SetActive(!has);
        }
        KeepAspect(videoFit, sel.vidParent);
        KeepAspect(coverFit, sel.imageDisplayer);
        //A newly picked video starts with its own track volumes, so keep re-applying
        ApplyPreviewVolume();
    }

    //The scene's player sends audio straight to the device (Direct mode), so it ignores the
    //game volume; set its track volumes instead. Handles an AudioSource output too.
    private void ApplyPreviewVolume()
    {
        UnityEngine.Video.VideoPlayer vp = sel.vidPlayer;
        if (vp == null) return;
        if (vp.audioOutputMode == UnityEngine.Video.VideoAudioOutputMode.AudioSource)
        {
            for (ushort i = 0; i < vp.controlledAudioTrackCount; i++)
            {
                AudioSource src = vp.GetTargetAudioSource(i);
                if (src != null) src.volume = previewVolume;
            }
        }
        else
        {
            ushort tracks = (ushort)Mathf.Max(1, (int)vp.audioTrackCount);
            for (ushort i = 0; i < tracks; i++)
            {
                if (!Mathf.Approximately(vp.GetDirectAudioVolume(i), previewVolume)) vp.SetDirectAudioVolume(i, previewVolume);
            }
        }
    }

    private static string Pct(float v) => Mathf.RoundToInt(v * 100f) + "%";

    private static void KeepAspect(AspectRatioFitter fit, RawImage img)
    {
        Texture t = img.texture;
        if (t != null && t.width > 4 && t.height > 4) fit.aspectRatio = (float)t.width / t.height;
    }

    void OnDestroy()
    {
        runner.Cancel();
        if (instance == this) instance = null;
    }

    //---------------- Note lists ----------------

    private void ShowTaps(string path)
    {
        float[] t = LoadTimes(path);
        tapHeader.text = "TAP NOTES" + (t.Length > 0 ? "  ·  " + t.Length : "");
        if (t.Length == 0)
        {
            tapList.text = Empty(path, "tap");
            return;
        }
        var sb = new StringBuilder();
        int shown = Mathf.Min(t.Length, MaxListLines);
        for (int i = 0; i < shown; i++)
            sb.Append("<color=#8E94A0>").Append((i + 1).ToString().PadLeft(3)).Append("</color>   ").Append(FormatTime(t[i])).Append('\n');
        if (t.Length > shown) sb.Append("<color=#8E94A0>… ").Append(t.Length - shown).Append(" more</color>");
        tapList.text = sb.ToString();
    }

    private void ShowHeld(string path)
    {
        float[] t = LoadTimes(path);
        int pairs = t.Length / 2;
        heldHeader.text = "HELD NOTES" + (pairs > 0 ? "  ·  " + pairs : "");
        if (t.Length == 0)
        {
            heldList.text = Empty(path, "held");
            return;
        }
        var sb = new StringBuilder();
        int shown = Mathf.Min(pairs, MaxListLines);
        for (int i = 0; i < shown; i++)
        {
            float a = t[2 * i], b = t[2 * i + 1];
            sb.Append("<color=#8E94A0>").Append((i + 1).ToString().PadLeft(3)).Append("</color>   ")
              .Append(FormatTime(a)).Append("  →  ").Append(FormatTime(b))
              .Append("<color=#8E94A0>   ").Append((b - a).ToString("0.0", CultureInfo.InvariantCulture)).Append("s</color>\n");
        }
        if (pairs > shown) sb.Append("<color=#8E94A0>… ").Append(pairs - shown).Append(" more</color>\n");
        //Held notes are start/end pairs; a leftover marker means one is missing its end
        if (t.Length % 2 == 1) sb.Append("<color=#FF5A5F>Odd number of held markers: the last one has no end.</color>");
        heldList.text = sb.ToString();
    }

    private static float[] LoadTimes(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return new float[0];
        try
        {
            //Same columns and frame rate LevelHolder uses
            return CsvParser.ParseTimeColumn(path, 10, FileSelection.ChartFps);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Couldn't read " + path + ": " + e.Message);
            return new float[0];
        }
    }

    private static string Empty(string path, string kind)
    {
        if (!string.IsNullOrEmpty(path)) return "<color=#FF5A5F>No times found in " + Path.GetFileName(path) + "</color>";
        return "<color=#8E94A0>No " + kind + " notes yet.\nLoad a CSV, or auto-chart below.</color>";
    }

    private static string FormatTime(float s)
    {
        int m = Mathf.FloorToInt(s / 60f);
        return m + ":" + (s - m * 60f).ToString("00.00", CultureInfo.InvariantCulture);
    }

    //---------------- Building blocks (top-left origin, y down) ----------------

    private static void Place(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    private static Image Box(Transform parent, float x, float y, float w, float h, Color color)
    {
        Image img = UIFactory.MakeImage(parent, null, color, Vector2.zero);
        Place(img.rectTransform, x, y, w, h);
        return img;
    }

    private static TextMeshProUGUI Label(Transform parent, string text, float x, float y, float w, float h,
        float size, bool bold, Color color, TextAlignmentOptions align)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        t.color = color;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        Place(t.rectTransform, x, y, w, h);
        return t;
    }

    private static void SectionTitle(Transform parent, string text, float y)
    {
        Label(parent, text, Pad, y, Inner, 32, 22, true, Theme.Text, TextAlignmentOptions.Left).characterSpacing = 4;
    }

    private static TextMeshProUGUI SmallLabel(Transform parent, string text, float x, float y)
    {
        TextMeshProUGUI t = Label(parent, text, x, y, 400, 24, 15, true, Theme.TextMuted, TextAlignmentOptions.Left);
        t.characterSpacing = 3;
        return t;
    }

    private static void Row(Transform parent, int index, float y0, string label)
    {
        TextMeshProUGUI t = Label(parent, label, Pad, y0 + index * RowH, LabelW - 12, RowH, 16, false, Theme.TextMuted, TextAlignmentOptions.Left);
        t.characterSpacing = 2;
    }

    private static TextMeshProUGUI PathLabel(Transform parent, float x, float y, float w)
    {
        TextMeshProUGUI t = Label(parent, "", x, y, w, RowH, 17, false, Theme.Text, TextAlignmentOptions.Left);
        t.overflowMode = TextOverflowModes.Ellipsis;
        return t;
    }

    private void SliderRow(Transform parent, float x, float y, float w, float min, float max, bool whole, float value,
        Action<float> set, Func<string> format)
    {
        const float valueW = 90;
        Slider slider = UIFactory.MakeSlider(parent, new Vector2(w - valueW - 16, 20), min, max, whole, value);
        Place((RectTransform)slider.transform, x, y + (RowH - 20) / 2, w - valueW - 16, 20);
        TextMeshProUGUI v = Label(parent, format(), x + w - valueW, y, valueW, RowH, 18, true, Theme.Text, TextAlignmentOptions.Right);
        slider.onValueChanged.AddListener(f =>
        {
            set(f);
            v.text = format();
            SaveSettings();
        });
    }

    //Dark frame that clips whatever preview sits inside it
    private static RectTransform Frame(Transform parent, float x, float y, float w, float h)
    {
        Image bg = Box(parent, x, y, w, h, Color.black);
        bg.gameObject.AddComponent<RectMask2D>();
        return bg.rectTransform;
    }

    //Moves a scene preview into a frame, keeping its aspect ratio
    private static AspectRatioFitter Fit(RawImage img, RectTransform frame)
    {
        RectTransform rt = img.rectTransform;
        rt.SetParent(frame, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;
        AspectRatioFitter fit = img.GetComponent<AspectRatioFitter>();
        if (fit == null) fit = img.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = 16f / 9f;
        return fit;
    }

    //Moves one of the scene's buttons into the layout and restyles its label
    private static void Adopt(Button b, Transform parent, float x, float y, float w, float h, string text, bool primary)
    {
        RectTransform rt = (RectTransform)b.transform;
        rt.SetParent(parent, false);
        Place(rt, x, y, w, h);
        Style(b, primary);
        TextMeshProUGUI label = b.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            label.text = text;
            label.enableAutoSizing = false;
            label.fontSize = 18;
            label.fontStyle = FontStyles.Bold;
            label.characterSpacing = 4;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.color = primary ? Theme.Background : Theme.Text;
        }
    }

    private static Button MakeButton(Transform parent, string text, float x, float y, float w, float h, bool primary, Action onClick)
    {
        Image bg = Box(parent, x, y, w, h, Color.white);
        bg.sprite = Theme.RoundedRect;
        bg.type = Image.Type.Sliced;
        bg.pixelsPerUnitMultiplier = 1.4f;
        bg.raycastTarget = true;
        Button b = bg.gameObject.AddComponent<Button>();
        b.targetGraphic = bg;
        Style(b, primary);
        b.onClick.AddListener(() => onClick());
        bg.gameObject.AddComponent<ButtonFeedback>();
        TextMeshProUGUI label = Label(bg.rectTransform, text, 0, 0, w, h, 18, true, primary ? Theme.Background : Theme.Text, TextAlignmentOptions.Center);
        label.characterSpacing = 4;
        return b;
    }

    private static void Style(Button b, bool primary)
    {
        ColorBlock cb = ColorBlock.defaultColorBlock;
        cb.normalColor = primary ? Theme.Accent : Theme.Hex("#2A2D36");
        cb.highlightedColor = primary ? Theme.Accent.WithAlpha(0.85f) : Theme.Hex("#363A45");
        cb.pressedColor = Theme.AccentPressed;
        cb.selectedColor = cb.normalColor;
        cb.disabledColor = Theme.Surface.WithAlpha(0.6f);
        cb.fadeDuration = 0.08f;
        b.colors = cb;
    }

    //Scrollable text list on a dark field
    private static TextMeshProUGUI ScrollList(Transform parent, float x, float y, float w, float h)
    {
        Image bg = Box(parent, x, y, w, h, Theme.Field);
        bg.sprite = Theme.RoundedRect;
        bg.type = Image.Type.Sliced;
        bg.pixelsPerUnitMultiplier = 1.4f;
        bg.raycastTarget = true;          //so the wheel/drag scrolls anywhere on the list

        RectTransform viewport = new GameObject("Viewport", typeof(RectTransform)).GetComponent<RectTransform>();
        viewport.SetParent(bg.rectTransform, false);
        UIFactory.Stretch(viewport);
        viewport.offsetMin = new Vector2(16, 10);
        viewport.offsetMax = new Vector2(-16, -10);
        viewport.gameObject.AddComponent<RectMask2D>();

        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport, false);
        RectTransform crt = (RectTransform)content.transform;
        crt.anchorMin = new Vector2(0f, 1f);
        crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot = new Vector2(0.5f, 1f);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = Vector2.zero;
        TextMeshProUGUI t = content.AddComponent<TextMeshProUGUI>();
        t.fontSize = 18;
        t.color = Theme.Text;
        t.alignment = TextAlignmentOptions.TopLeft;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.lineSpacing = 8;
        t.raycastTarget = false;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = bg.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = crt;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;
        return t;
    }
}
