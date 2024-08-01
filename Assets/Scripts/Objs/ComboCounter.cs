using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class ComboCounter : MonoBehaviour
{
    //Other Objects
    public TextMeshProUGUI comboCounter;
    //Instance Vars
    public int oldCombo;
    
    //Other Scripts
    private ScoreKeeper keeperScript;
    // Start is called before the first frame update
    void Start()
    {
        RectTransform rt = GetComponent<RectTransform>();
        keeperScript = GameObject.FindGameObjectWithTag("Overviewer").GetComponent<ScoreKeeper>();
    }

    // Update is called once per frame
    void Update()
    {
        if (ScoreKeeper.combo > oldCombo)
        {
            //Debug.Log("Better Combo");
            oldCombo = ScoreKeeper.combo;
            comboCounter.fontSize = comboCounter.fontSize * 1.08f;
            
        }
        if (comboCounter.fontSize >= 50)
        {
            comboCounter.fontSize = 50;
        }
        if (comboCounter.fontSize >= 30)
        {
            comboCounter.fontSize -= 0.05f;
        }
        
        if (ScoreKeeper.combo == 0)
        {
            comboCounter.fontSize = 30;
            oldCombo = 0;
        }
    }
    
}
