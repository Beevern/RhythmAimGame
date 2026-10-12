using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using System.IO;
using TMPro;
using SimpleFileBrowser;
public class LevelMaker : MonoBehaviour
{
    //Prefabs
    public GameObject marker;
    public GameObject box;
    public GameObject needle;
    //UI Objects
    public Transform canvas;
    public TextMeshProUGUI timeT;
    public VideoPlayer vidPlayer;
    public RawImage vidParent;
    public Slider timeSlider;
    public Button PlayPauseBtn;
    public Button right;
    public Button left;
    public Button done;
    public TextMeshProUGUI playPauseText;
    public TextMeshProUGUI markerT;
    //Instances
    public static float timeNow;
    public ArrayList times = new ArrayList();
    // Start is called before the first frame update
    void Start()
    {
        playPauseText.text = "Pause";
        timeSlider.onValueChanged.AddListener(delegate { setVidTime(); });
        timeSlider.maxValue = (float) vidPlayer.length;
        Button playpause = PlayPauseBtn.GetComponent<Button>();
        playpause.onClick.AddListener(PlayPause);
        Button rightBtn = right.GetComponent<Button>();
        rightBtn.onClick.AddListener(goRight);
        Button leftBtn = left.GetComponent<Button>();
        leftBtn.onClick.AddListener(goLeft);
        Button doneBtn = done.GetComponent<Button>();
        doneBtn.onClick.AddListener(doneFunc);

        StartCoroutine(PlayVid());
    }

    // Update is called once per frame
    void Update()
    {
        timeNow = Mathf.Round((float)vidPlayer.time * 100) / 100;
        placeMarker();
        if (!Input.GetMouseButton(0))
        {
            timeSlider.value = (float)vidPlayer.time;
        }
        
        //Must multiply inside then round, then divide so its rounded
        timeT.text = "Time: "+timeNow;

        if (vidPlayer.isPlaying)
        {
            playPauseText.text = "Pause";
        }
        else
        {
            playPauseText.text = "Play";
        }
    }

    void placeMarker()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            
            if (times.Contains(timeNow))
            {
                //Debug.Log("Removed: " + timeNow);
                times.Remove(timeNow);
                foreach (GameObject mark in GameObject.FindGameObjectsWithTag("Marker"))
                {
                    if (Marker.On(mark).myTime == timeNow)
                    {
                        Destroy(mark);
                        Debug.Log("Marker Destroyed");
                    }
                }
                displayMarkers();
            }
            else
            {
                //Debug.Log("Added: " + timeNow);
                times.Add(timeNow);
                GameObject marker1 = Instantiate(marker, needle.transform.position, Quaternion.identity);
                marker1.transform.SetParent(box.transform);
                Marker.On(marker1).myTime = timeNow;
                displayMarkers();
            }
        }
    }

    void displayMarkers()
    {
        foreach (GameObject marker in GameObject.FindGameObjectsWithTag("MarkerT"))
        {
            Destroy(marker);
        }
        for (int i = 0;i<times.Count;i++)
        {
            TextMeshProUGUI thisMark = Instantiate(markerT, new Vector3(-840, 500-30*i, 0), Quaternion.identity);
            thisMark.GetComponent<TMP_Text>().text = "" + times[i];
            thisMark.transform.SetParent(canvas, false);
        }
    }
    void setVidTime()
    {
        if (Input.GetMouseButton(0))
        {
            //vidPlayer.Pause();
            vidPlayer.time = timeSlider.value;
            //The problem with this is when I move the end (forward) it works fine (the box catches up) but when I move to the start (backwards) the box doesn't slide back.
            //The needle's x position stays at a stupid high number
            //It also doesn't appear to be dependant on where the current needle is, just set the time. So if the level is already halfway through, it'll still move the box as if it was at the start
            //eg 88624, 82687, the first being after some time has passed and we click the end, the second is clicking to end at the start.
            //                                                                                        v82619
            needle.transform.position = new Vector2((timeSlider.value/timeSlider.maxValue)*(TimeBox.boxLength/2), needle.GetComponent<RectTransform>().anchoredPosition.y+27);
        }
    }

    void doneFunc()
    {
        LevelSaver theScript = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<LevelSaver>();
        foreach (GameObject marker in GameObject.FindGameObjectsWithTag("Marker"))
        {
            theScript.dataList.Add(Marker.On(marker).myTime);
        }
        //theScript.SaveFiles(new string[]{FileSelection.theVid, FileSelection.theImage});
        theScript.SaveArrayListData();
    }

    void PlayPause()
    {
        if (vidPlayer.isPaused)
        {
            vidPlayer.Play();

        }
        else
        {
            vidPlayer.Pause();
        }
    }

    void goRight()
    {
        //vidPlayer.time -= 0.01;
        GameObject closestRight = null;
        double diff = double.MaxValue;
        double closestDiff = double.MaxValue;
        foreach (GameObject marker in GameObject.FindGameObjectsWithTag("Marker"))
        {
            diff = Marker.On(marker).myTime - timeNow;
            //Debug.Log("Current: " + diff);
            if ((diff<closestDiff||closestDiff<0) && diff>0)
            {
                closestRight = marker;
                closestDiff = diff;
                //Debug.Log("Changed:" + closestDiff);
            }
        }
        //No marker to the right
        if (closestRight == null) return;
        needle.transform.position = closestRight.transform.position;
        vidPlayer.time = Marker.On(closestRight).myTime;
    }

    void goLeft()
    {
        GameObject closestLeft = null;
        double diff = double.MaxValue;
        double closestDiff = double.MaxValue;
        foreach (GameObject marker in GameObject.FindGameObjectsWithTag("Marker"))
        {
            diff = timeNow - Marker.On(marker).myTime;
            //Debug.Log("Current: " + diff);
            if ((diff < closestDiff || closestDiff < 0) && diff > 0)
            {
                closestLeft = marker;
                closestDiff = diff;
                //Debug.Log("Changed:" + closestDiff);
            }
        }
        //No marker to the left
        if (closestLeft == null) return;
        needle.transform.position = closestLeft.transform.position;
        vidPlayer.time = Marker.On(closestLeft).myTime;
    }

    IEnumerator PlayVid()
    {
        Debug.Log(FileSelection.theVid);
        vidParent.texture = Texture2D.blackTexture;
        vidPlayer.source = VideoSource.Url;
        vidPlayer.url = FileSelection.theVid;
        vidPlayer.Prepare();
        //Wait for it to actually load (yielding the bool only waited one frame)
        while (!vidPlayer.isPrepared)
            yield return null;
        timeSlider.maxValue = (float)vidPlayer.length;
        RenderTexture newTexture = new RenderTexture(vidPlayer.texture.width, vidPlayer.texture.height, 1, RenderTextureFormat.ARGB32);
        newTexture.Create();
        vidParent.texture = newTexture;
        vidPlayer.targetTexture = newTexture;
    }

    
}

