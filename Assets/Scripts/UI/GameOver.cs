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
    //Other Scripts


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        participationText.text = "Participation Rate: " + ScoreKeeper.participation + "%";
        scoreText.text = "Score: " + ScoreKeeper.score;
        comboText.text = "Highest Combo: " + ScoreKeeper.highestCombo;
    }
}
