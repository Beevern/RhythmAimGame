using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using SimpleFileBrowser;
using System.IO;
public class FileSelection : MonoBehaviour
{
    public Button done;
    public Button vidSelect;
    public Button imageSelect;
    public static string theVid;
    public static string theImage;
    public VideoPlayer vidPlayer;
    public RawImage vidParent;
    public RawImage imageDisplayer;
    // Start is called before the first frame update
    void Start()
    {
        Button doneBtn = done.GetComponent<Button>();
        doneBtn.onClick.AddListener(doneFunc);
        Button vidSel = vidSelect.GetComponent<Button>();
        vidSel.onClick.AddListener(selectVid);
        Button imageSel = imageSelect.GetComponent<Button>();
        imageSel.onClick.AddListener(selectImage);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void doneFunc()
    {
        SceneManager.LoadScene("LevelMaker");
    }


    void selectVid()
    {
        StartCoroutine(SelectVid());
    }
    void selectImage()
    {
        StartCoroutine(SelectImage());
    }
    IEnumerator SelectVid()
    {
        // Wait for the user to pick a file
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, null, "Select a File", "Select");

        // Check if the user selected a file
        if (FileBrowser.Success)
        {
            theVid = FileBrowser.Result[0];
            //Debug.Log("Selected file: " + theVid);
            vidPlayer.url = theVid;
            vidPlayer.Prepare();
            yield return vidPlayer.isPrepared;
            RenderTexture newTexture = new RenderTexture(vidPlayer.texture.width, vidPlayer.texture.height, 1, RenderTextureFormat.ARGB32);
            newTexture.Create();
            vidParent.texture = newTexture;
            vidPlayer.targetTexture = newTexture;
            vidPlayer.Play();
        }
    }

    IEnumerator SelectImage()
    {
        // Wait for the user to pick a file
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, null, "Select a File", "Select");

        // Check if the user selected a file
        if (FileBrowser.Success)
        {
            string theImage = FileBrowser.Result[0];
            //Debug.Log("Selected file: " + theImage);
            byte[] fileData = File.ReadAllBytes(theImage);
            Texture2D texture = new Texture2D(2, 2); // The size is overwritten when loading the image
            if (texture.LoadImage(fileData))
            {
                // Assign the texture to the RawImage component
                imageDisplayer.texture = texture;
                imageDisplayer.SetNativeSize(); // Optional: set the size of the RawImage to match the texture size
                Debug.Log("Image loaded and displayed from: " + theImage);
            }
        }
    }
}
