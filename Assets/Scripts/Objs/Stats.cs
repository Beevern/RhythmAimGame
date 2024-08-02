using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class Stats : MonoBehaviour
{
    //Vars
    public Button resetBtn;
    //Text Objs
    public TextMeshProUGUI timesPlayedT;
    public TextMeshProUGUI highScoreT;
    public TextMeshProUGUI highComboT;
    public TextMeshProUGUI avgScoreT;
    public TextMeshProUGUI avgPartiT;

    // Start is called before the first frame update

    void Start()
    {
        //RESET STATS HERE!!! COMMENT OUT FOR NO RESET, LEAVE IN FOR RESET
        Button btn = resetBtn.GetComponent<Button>();
        btn.onClick.AddListener(resetStats);
        
        
    }
    void Update()
    {
        timesPlayedT.text = "Times Played: "+PlayerPrefs.GetInt(SongSelect2.theSong + "timesPlayed", 0);
        highScoreT.text = "High Score: "+PlayerPrefs.GetInt(SongSelect2.theSong + "highScore", 0);
        highComboT.text = "Highest Combo: "+PlayerPrefs.GetInt(SongSelect2.theSong + "highCombo", 0);
        if (PlayerPrefs.GetInt(SongSelect2.theSong + "timesPlayed", 1)>0)
        {
            avgScoreT.text = "Average Score: " + PlayerPrefs.GetInt(SongSelect2.theSong + "totalScore", 0) / PlayerPrefs.GetInt(SongSelect2.theSong + "timesPlayed", 1);
            avgPartiT.text = "Average Participation: " + (int)PlayerPrefs.GetFloat(SongSelect2.theSong + "totalParti", 0) / PlayerPrefs.GetInt(SongSelect2.theSong + "timesPlayed", 1) + "%";
        }
        else
        {
            avgScoreT.text = "Average Score: 0";
            avgPartiT.text = "Average Participation: 0%";
        }
    }

    void resetStats()
    {
        /* FOR ALL SONGS
        for (int i = 0; i < SongSelect2.allSongs.Length; i++)
        {
            PlayerPrefs.SetInt(SongSelect2.allSongs[i] + "highScore", 0);
            PlayerPrefs.SetInt(SongSelect2.allSongs[i] + "highCombo", 0);
            PlayerPrefs.SetInt(SongSelect2.allSongs[i] + "totalScore", 0);
            PlayerPrefs.SetInt(SongSelect2.allSongs[i] + "totalParti", 0);
            PlayerPrefs.SetInt(SongSelect2.allSongs[i] + "timesPlayed", 0);
            PlayerPrefs.SetInt(SongSelect2.allSongs[i] + "gamesPlayed", 0);
        }
        */
        //FOR ONE SONG
        PlayerPrefs.SetInt(SongSelect2.theSong + "highScore", 0);
        PlayerPrefs.SetInt(SongSelect2.theSong + "highCombo", 0);
        PlayerPrefs.SetInt(SongSelect2.theSong + "totalScore", 0);
        PlayerPrefs.SetInt(SongSelect2.theSong + "totalParti", 0);
        PlayerPrefs.SetInt(SongSelect2.theSong + "timesPlayed", 0);
        PlayerPrefs.SetInt(SongSelect2.theSong + "gamesPlayed", 0);
    }
}
