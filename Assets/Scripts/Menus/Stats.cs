using UnityEngine;
using UnityEngine.UI;
using TMPro;

//Per-song stats panel on Song Select
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

    //Song the texts currently show; they're only rebuilt when this changes (PlayerPrefs reads are slow)
    private string shownSong;

    void Start()
    {
        resetBtn.GetComponent<Button>().onClick.AddListener(resetStats);
    }

    void Update()
    {
        if (SongSelect2.theSong != shownSong)
        {
            Refresh();
        }
    }

    void Refresh()
    {
        string song = SongSelect2.theSong;
        shownSong = song;
        int timesPlayed = PlayerPrefs.GetInt(song + "timesPlayed", 0);
        timesPlayedT.text = "Times Played: " + timesPlayed;
        highScoreT.text = "High Score: " + PlayerPrefs.GetInt(song + "highScore", 0);
        highComboT.text = "Highest Combo: " + PlayerPrefs.GetInt(song + "highCombo", 0);
        if (timesPlayed > 0)
        {
            avgScoreT.text = "Average Score: " + PlayerPrefs.GetInt(song + "totalScore", 0) / timesPlayed;
            avgPartiT.text = "Average Participation: " + (int)PlayerPrefs.GetFloat(song + "totalParti", 0) / timesPlayed + "%";
        }
        else
        {
            avgScoreT.text = "Average Score: 0";
            avgPartiT.text = "Average Participation: 0%";
        }
    }

    //Resets the selected song only
    void resetStats()
    {
        string song = SongSelect2.theSong;
        PlayerPrefs.SetInt(song + "highScore", 0);
        PlayerPrefs.SetInt(song + "highCombo", 0);
        PlayerPrefs.SetInt(song + "totalScore", 0);
        //totalParti is stored as a float everywhere else, so reset it as one
        PlayerPrefs.SetFloat(song + "totalParti", 0);
        PlayerPrefs.SetInt(song + "timesPlayed", 0);
        PlayerPrefs.SetInt(song + "gamesPlayed", 0);
        PlayerPrefs.Save();
        Refresh();
    }
}
