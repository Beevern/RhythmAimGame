using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using TMPro;
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
    public Slider timeSlider;
    public Button PlayPauseBtn;
    public Button right;
    public Button left;
    public TextMeshProUGUI playPauseText;
    public TextMeshProUGUI markerT;
    //Instances
    public float timeNow;
    ArrayList times = new ArrayList();
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
    }

    // Update is called once per frame
    void Update()
    {
        placeMarker();
        if (!Input.GetMouseButton(0))
        {
            timeSlider.value = (float)vidPlayer.time;
        }
        
        //Must multiply inside then round, then divide so its rounded
        timeT.text = "Time: "+Mathf.Round((float)vidPlayer.time*100)/100;

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
            timeNow = Mathf.Round((float)vidPlayer.time * 100) / 100;
            if (times.Contains(timeNow))
            {
                //Debug.Log("Removed: " + timeNow);
                times.Remove(timeNow);
                //Destroy()
                displayMarkers();
            }
            else
            {
                //Debug.Log("Added: " + timeNow);
                times.Add(timeNow);
                GameObject marker1 = Instantiate(marker, needle.transform.position, Quaternion.identity);
                marker1.transform.SetParent(box.transform);
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
            needle.transform.position = new Vector2((timeSlider.value/timeSlider.maxValue)*TimeBox.boxLength/2, needle.GetComponent<RectTransform>().anchoredPosition.y+27);
        }
    }

    void PlayPause()
    {
        if (vidPlayer.isPaused)
        {
            vidPlayer.Play();
            //playPauseText.text = "Pause";

        }
        else
        {
            vidPlayer.Pause();
            //playPauseText.text = "Play";
        }
    }

    void goRight()
    {
        vidPlayer.time -= 0.01;
        foreach (GameObject markerText in GameObject.FindGameObjectsWithTag("MarkerT"))
        {
            //markerText.GetComponent<TMP_Text>().text
        }
    }

    void goLeft()
    {
        vidPlayer.time -= 0.01;
    }
}
