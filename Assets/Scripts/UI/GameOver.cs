using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class GameOver : MonoBehaviour
{
    //Text
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI participationText;
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI songText;
    //Other Scripts

    //Vars
    // Start is called before the first frame update
    void Start()
    {
        participationText.text = "Participation Rate: " + ScoreKeeper.participation + "%";
        scoreText.text = "Score: " + ScoreKeeper.score;
        comboText.text = "Highest Combo: " + ScoreKeeper.highestCombo;
        songText.text = "Song: " + SongSelect2.theSong;

        if (ScoreKeeper.score > PlayerPrefs.GetInt(SongSelect2.theSong + "highScore", 0))
        {
            PlayerPrefs.SetInt(SongSelect2.theSong + "highScore", ScoreKeeper.score);
        }
        if (ScoreKeeper.highestCombo > PlayerPrefs.GetInt(SongSelect2.theSong + "highCombo", 0))
        {
            PlayerPrefs.SetInt(SongSelect2.theSong + "highCombo", ScoreKeeper.highestCombo);
        }
        PlayerPrefs.SetInt(SongSelect2.theSong + "totalScore", ScoreKeeper.score+PlayerPrefs.GetInt(SongSelect2.theSong + "totalScore"));
        PlayerPrefs.SetFloat(SongSelect2.theSong + "totalParti", (float) ScoreKeeper.participation+PlayerPrefs.GetFloat(SongSelect2.theSong + "totalParti"));
        PlayerPrefs.SetInt(SongSelect2.theSong + "timesPlayed", PlayerPrefs.GetInt(SongSelect2.theSong + "timesPlayed", 0)+1);
        /*
        Debug.Log(SongSelect2.theSong);
        Debug.Log(PlayerPrefs.GetInt(SongSelect2.theSong + "timesPlayed", 0));
        Debug.Log(PlayerPrefs.GetFloat(SongSelect2.theSong + "totalParti"));
        Debug.Log(PlayerPrefs.GetInt(SongSelect2.theSong + "totalScore"));
        */
    }
}
