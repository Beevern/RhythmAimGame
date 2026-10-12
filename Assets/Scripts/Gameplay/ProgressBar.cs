using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

//Fills the song progress bar from the level's elapsed time
public class ProgressBar : MonoBehaviour
{
    public GameObject mask;

    //Cached instead of searched for every frame
    private Image fill;
    private VideoPlayer video;

    void Start()
    {
        fill = mask.GetComponent<Image>();
        video = GameObject.FindGameObjectWithTag("VideoPlayer").GetComponent<VideoPlayer>();
    }

    void Update()
    {
        //Video starts after the 3 second countdown
        if (Time.timeSinceLevelLoadAsDouble > 3 && video.length > 0)
        {
            fill.fillAmount = (float)(Time.timeSinceLevelLoadAsDouble / video.length);
        }
    }
}
