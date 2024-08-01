using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
public class ProgressBar : MonoBehaviour
{
    public GameObject mask;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        GetCurrentFill();
    }

    void GetCurrentFill()
    {
        if (Time.timeSinceLevelLoadAsDouble>3)
        {
            mask.GetComponent<Image>().fillAmount = (float)Time.timeSinceLevelLoadAsDouble / (float)(GameObject.FindGameObjectWithTag("VideoPlayer").GetComponent<VideoPlayer>().length);
            //mask.GetComponent<Image>().fillAmount = (float)ScoreKeeper.notesGiven / Spawner.totalNotes;
        }

    }
}
