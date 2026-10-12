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
    private float shownScore;
    //Last values written to the HUD, so text (and its garbage) is only rebuilt when something changes
    private int lastShownScore = -1;
    private double lastParticipation = -1;
    private int lastCombo = -1;
    private bool lastHadScore;
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
        //Was never reset, so results showed the best combo from any earlier run
        highestCombo = 0;
        RunStats.Reset(SongSelect2.theSong);
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
        //Score rolls up towards the real value
        shownScore = Mathf.Lerp(shownScore, score, 1f - Mathf.Exp(-12f * Time.deltaTime));
        if (Mathf.Abs(score - shownScore) < 1f) shownScore = score;
        int rounded = Mathf.RoundToInt(shownScore);
        if (rounded != lastShownScore)
        {
            lastShownScore = rounded;
            scoreText.text = "Score: " + rounded;
        }
        if (participation != lastParticipation)
        {
            lastParticipation = participation;
            participationText.text = "Participation: " + participation + "%";
        }
        if (combo > highestCombo)
        {
            highestCombo = combo;
        }
        bool hasScore = score >= 1;
        if (combo != lastCombo || hasScore != lastHadScore)
        {
            lastCombo = combo;
            lastHadScore = hasScore;
            //Combo only shows from 3 up; between 1 and 2 the previous text stays (as before)
            if (combo > 2 && hasScore) comboText.text = "x" + combo;
            else if (combo <= 0) comboText.text = "";
        }
        if (bloom > 0f && Pause.isPaused==false)
        {
            //Frame-rate independent (was 0.02/60 per frame, i.e. 0.02 per second at 60fps)
            bloom -= 0.02f * Time.deltaTime;
        }
    }
}
