using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
public class FileSelection : MonoBehaviour
{
    public Button done;
    public Button vidSelect;
    public Button imageSelect;
    public static VideoClip theVid;
    public static RawImage theImage;
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
        theVid = null;
    }

    void selectImage()
    {
        theImage = null;
    }
}
