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
        //With the averages, it's taking the current one, (eg 20k), adding (1/timesPlayed) of the previous total
        PlayerPrefs.SetInt(SongSelect2.theSong + "totalScore", PlayerPrefs.GetInt(SongSelect2.theSong + "totalScore"));
        PlayerPrefs.SetFloat(SongSelect2.theSong + "totalParti", PlayerPrefs.GetFloat(SongSelect2.theSong + "totalParti"));
        PlayerPrefs.SetInt(SongSelect2.theSong + "timesPlayed", PlayerPrefs.GetInt(SongSelect2.theSong + "timesPlayed", 0)+1);

        Debug.Log(PlayerPrefs.GetInt(SongSelect2.theSong + "timesPlayed", 0));
        Debug.Log((ScoreKeeper.participation + PlayerPrefs.GetFloat(SongSelect2.theSong + "totalParti"))/2);
        Debug.Log((ScoreKeeper.score + PlayerPrefs.GetInt(SongSelect2.theSong + "totalScore"))/2);
    }
}
