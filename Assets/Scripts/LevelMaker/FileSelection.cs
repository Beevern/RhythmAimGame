using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using SimpleFileBrowser;
using System.IO;

public class FileSelection : MonoBehaviour
{
    public Button done;
    public Button vidSelect;
    public Button imageSelect;
    public Button noteSelect;
    public Button heldSelect;


    public static string theVid;
    public static string theImage;
    public static string theName;
    public static string notePath;
    public static string heldPath;

    public VideoPlayer vidPlayer;
    public RawImage vidParent;
    public RawImage imageDisplayer;
    public TMP_InputField nameField;

    void Start()
    {
        nameField.onValueChanged.AddListener(delegate { setName(); });
        //Black preview until a video is picked (the scene's placeholder texture is gone)
        if (vidParent.texture == null) vidParent.texture = Texture2D.blackTexture;
        if (imageDisplayer.texture == null) imageDisplayer.texture = Texture2D.blackTexture;

        done.onClick.AddListener(doneFunc);
        vidSelect.onClick.AddListener(selectVid);
        imageSelect.onClick.AddListener(selectImage);
        noteSelect.onClick.AddListener(selectNote);
        heldSelect.onClick.AddListener(selectHeld);
        //Two-panel layout (visuals | chart + auto-chart); moves the controls above into it
        LevelSetupScreen.Attach(this);
    }

    void doneFunc()
    {
        // 1) Validate name and video
        if (string.IsNullOrWhiteSpace(theName))
        {
            Debug.LogError("Level name is empty.");
            LevelSetupScreen.ShowMessage("Give the level a name first.", true);
            return;
        }
        if (string.IsNullOrEmpty(theVid))
        {
            Debug.LogError("No video selected.");
            LevelSetupScreen.ShowMessage("Select a video first.", true);
            return;
        }

        // 2) Create a CustomLevelInfo from what the user chose
        
        CustomLevelInfo info = new CustomLevelInfo
        {
            levelName = theName,
            songFilePath = theVid,      // using the video path as the song/video source
            imageFilePath = theImage,
            noteFilePath = notePath,          // you can fill this later when you have note files
            heldFilePath = heldPath,
            chartFps = ChartFps,
        };

        // 3) Add to persistent manager
        CustomLevelManager.Instance.AddLevel(info);
        
        Debug.Log("Custom level registered: " + theName +
                  "\nVideo: " + theVid +
                  "\nImage: " + theImage);

        // 4) Go to Menu
        SceneManager.LoadScene("Menu");
    }

    void setName()
    {
        theName = nameField.text;
    }

    void selectVid()
    {
        StartCoroutine(SelectVid());
    }

    void selectImage()
    {
        StartCoroutine(SelectImage());
    }

    void selectNote()
    {
        StartCoroutine(SelectNote());
    }

    void selectHeld()
    {
        StartCoroutine(SelectHeld());
    }

    IEnumerator SelectVid()
    {
        yield return PickFile("Select Video", new FileBrowser.Filter("Videos", ".mp4", ".mov", ".webm", ".avi", ".mkv", ".m4v"));

        if (FileBrowser.Success)
        {
            LoadVideo(FileBrowser.Result[0]);
        }
    }

    //Frame rate of each video previewed, so charts know what timeline rate their timecodes use
    public static readonly Dictionary<string, float> VideoFps = new Dictionary<string, float>();

    //Frame rate of the video the chart was made against (the one you'd put in DaVinci Resolve):
    //the picked video, even when an auto-chart lyric video has replaced it as the level video
    public static float ChartFps
    {
        get
        {
            string v = LevelSetupScreen.ChartVideo ?? theVid;
            return v != null && VideoFps.TryGetValue(v, out float fps) ? fps : LevelHolder.ChartFps;
        }
    }

    //Makes `path` the level's video and shows it in the preview (also used by auto-chart's lyric video)
    public void LoadVideo(string path)
    {
        theVid = path;
        StopCoroutine(nameof(PreviewVideo));
        StartCoroutine(nameof(PreviewVideo), path);
    }

    IEnumerator PreviewVideo(string path)
    {
        vidPlayer.Stop();
        vidPlayer.source = UnityEngine.Video.VideoSource.Url;
        vidPlayer.url = path;
        vidPlayer.Prepare();
        while (!vidPlayer.isPrepared)
            yield return null;
        VideoFps[path] = CsvParser.StandardFps(vidPlayer.frameRate);

        RenderTexture newTexture = new RenderTexture(
            vidPlayer.texture.width,
            vidPlayer.texture.height,
            1,
            RenderTextureFormat.ARGB32);
        newTexture.Create();

        //Free the previous video's texture
        if (vidParent.texture is RenderTexture old && old != newTexture) old.Release();
        vidParent.texture = newTexture;
        vidPlayer.targetTexture = newTexture;
        vidPlayer.Play();
    }

    IEnumerator SelectImage()
    {
        yield return PickFile("Select Cover Image", new FileBrowser.Filter("Images", ".png", ".jpg", ".jpeg"));

        if (FileBrowser.Success)
        {
            theImage = FileBrowser.Result[0];

            byte[] fileData = File.ReadAllBytes(theImage);
            Texture2D texture = new Texture2D(2, 2);
            if (texture.LoadImage(fileData))
            {
                imageDisplayer.texture = texture;
            }
        }
    }

    IEnumerator SelectNote()
    {
        yield return PickFile("Select Notes CSV", new FileBrowser.Filter("CSV", ".csv"));

        if (FileBrowser.Success)
        {
            notePath = FileBrowser.Result[0];

            // TODO: use csvPath (read file, store path, etc.)
        }
    }

    IEnumerator SelectHeld()
    {
        yield return PickFile("Select Held Notes CSV", new FileBrowser.Filter("CSV", ".csv"));

        if (FileBrowser.Success)
        {
            heldPath = FileBrowser.Result[0];

            // TODO: use csvPath (read file, store path, etc.)
        }
    }

    //Opens the file picker in the user's Downloads folder, showing only the given file type
    //(each picker sets its own filter, so a CSV filter no longer carries over to the video/image pickers)
    IEnumerator PickFile(string title, FileBrowser.Filter filter)
    {
        FileBrowser.SetFilters(true, filter);
        FileBrowser.SetDefaultFilter(filter.extensions[0]);
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, DownloadsFolder(), null, title, "Select");
    }

    //The real Downloads folder: asks Windows first (it may have been moved or redirected),
    //then falls back to <user>/Downloads, then the user folder itself
    public static string DownloadsFolder()
    {
        string home = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        try
        {
            if (SHGetKnownFolderPath(new System.Guid("374DE290-123F-4565-9164-39C4925E467B"), 0, System.IntPtr.Zero, out System.IntPtr ptr) == 0)
            {
                string path = System.Runtime.InteropServices.Marshal.PtrToStringUni(ptr);
                System.Runtime.InteropServices.Marshal.FreeCoTaskMem(ptr);
                if (Directory.Exists(path)) return path;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Couldn't look up the Downloads folder: " + e.Message);
        }
#endif
        string fallback = Path.Combine(home, "Downloads");
        return Directory.Exists(fallback) ? fallback : home;
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    static extern int SHGetKnownFolderPath(System.Guid rfid, uint flags, System.IntPtr token, out System.IntPtr path);
#endif
}
