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
    //0..1, kicked to 1 on every combo increase and eased back down
    private float punch;

    // Update is called once per frame
    void Update()
    {
        if (ScoreKeeper.combo > oldCombo)
        {
            oldCombo = ScoreKeeper.combo;
            punch = 1f;
        }
        if (ScoreKeeper.combo == 0)
        {
            oldCombo = 0;
        }
        //Scale the transform instead of the font size so the layout never reflows
        punch = Mathf.MoveTowards(punch, 0f, Time.deltaTime * 6f);
        float s = 1f + 0.18f * punch * punch;
        comboCounter.rectTransform.localScale = new Vector3(s, s, 1f);
    }

}
