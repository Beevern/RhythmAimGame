using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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
        mask.GetComponent<Image>().fillAmount = (float)Spawner.timeSinceLoad / (Spawner.totalTime + 3.0f);
        //mask.GetComponent<Image>().fillAmount = (float)ScoreKeeper.notesGiven / Spawner.totalNotes;
    }
}
