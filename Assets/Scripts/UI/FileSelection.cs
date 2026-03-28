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

        done.onClick.AddListener(doneFunc);
        vidSelect.onClick.AddListener(selectVid);
        imageSelect.onClick.AddListener(selectImage);
        noteSelect.onClick.AddListener(selectNote);
        heldSelect.onClick.AddListener(selectHeld);
    }

    void doneFunc()
    {
        // 1) Validate name and video
        if (string.IsNullOrWhiteSpace(theName))
        {
            Debug.LogError("Level name is empty.");
            return;
        }
        if (string.IsNullOrEmpty(theVid))
        {
            Debug.LogError("No video selected.");
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
        yield return FileBrowser.WaitForLoadDialog(
            FileBrowser.PickMode.Files, false, Path.Combine(
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
        "Downloads"), "Select a File", "Select");

        if (FileBrowser.Success)
        {
            theVid = FileBrowser.Result[0];

            vidPlayer.url = theVid;                // runtime path is fine[web:76][web:82]
            vidPlayer.Prepare();
            while (!vidPlayer.isPrepared)
                yield return null;

            RenderTexture newTexture = new RenderTexture(
                vidPlayer.texture.width,
                vidPlayer.texture.height,
                1,
                RenderTextureFormat.ARGB32);
            newTexture.Create();

            vidParent.texture = newTexture;
            vidPlayer.targetTexture = newTexture;
            vidPlayer.Play();
        }
    }

    IEnumerator SelectImage()
    {
        yield return FileBrowser.WaitForLoadDialog(
            FileBrowser.PickMode.Files, false, Path.Combine(
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
        "Downloads"), "Select a File", "Select");

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
        // Only allow .csv files
        FileBrowser.SetFilters(true, new FileBrowser.Filter("CSV", ".csv"));
        FileBrowser.SetDefaultFilter(".csv");

        yield return FileBrowser.WaitForLoadDialog(
            FileBrowser.PickMode.Files,
            false,
            Path.Combine(
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
        "Downloads"),
            "Select CSV File",
            "Select"
        );

        if (FileBrowser.Success)
        {
            notePath = FileBrowser.Result[0];

            // TODO: use csvPath (read file, store path, etc.)
        }
    }

    IEnumerator SelectHeld()
    {
        // Only allow .csv files
        FileBrowser.SetFilters(true, new FileBrowser.Filter("CSV", ".csv"));
        FileBrowser.SetDefaultFilter(".csv");

        yield return FileBrowser.WaitForLoadDialog(
            FileBrowser.PickMode.Files,
            false,
            Path.Combine(
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
        "Downloads"),
            "Select CSV File",
            "Select"
        );

        if (FileBrowser.Success)
        {
            heldPath = FileBrowser.Result[0];

            // TODO: use csvPath (read file, store path, etc.)
        }
    }

}