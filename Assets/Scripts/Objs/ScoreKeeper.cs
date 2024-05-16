using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class ScoreKeeper : MonoBehaviour
{
    //Instance Vars
    public static string levelSelected;
    //Score variables
    public static double participation;
    public static int noteHits;
    public static int notesGiven;
    public static int score;
    public static int combo;
    public static int highestCombo;
    public static int targetCount;
    public static float bloom;
    //Text
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI participationText;
    public TextMeshProUGUI comboText;
    // Start is called before the first frame update
    void Start()
    {
        comboText.text = "";
        score = 0;
        combo = 0;
        bloom = 0;
        notesGiven = 0;
        noteHits = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (combo <0)
        {
            combo = 0;
        }
        if (notesGiven == 0)
        {
            participation = 0;
        }
        else
        {
            participation = (int) (((double) noteHits / notesGiven) * 100);
        }
        scoreText.text = "Score: " + score;
        participationText.text = "Participation: " + participation + "%";
        if (combo > highestCombo)
        {
            highestCombo = combo;
        }
        if (combo > 0 && score >= 1)
        {
            comboText.text = "x" + combo;
        }
        else if (combo <= 0)
        {
            comboText.text = "";
        }
        if (bloom > 0f && Pause.isPaused==false)
        {
            bloom -= (0.02f/60f);
            //Debug.Log(bloom);
        }
    }
}
