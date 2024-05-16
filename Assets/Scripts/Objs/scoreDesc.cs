using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class scoreDesc : MonoBehaviour
{
    public int myScoreToAdd;
    private double destroyTimer = 0.5;
    public TextMeshProUGUI myText;
    // Start is called before the first frame update
    void Start()
    {
        myScoreToAdd = Target.scoreToAdd;
    }

    // Update is called once per frame
    void Update()
    {
        destroyTimer -= Time.deltaTime;
        if (destroyTimer<=0)
        {
            Destroy(this.gameObject);
        }
        if (myScoreToAdd > 450)
        { myText.text = "Perfect!"; }
        else if (myScoreToAdd>350){ myText.text = "Great"; }
        else { myText.text = "Nice"; }

        //transform.LookAt(GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>());
        //this.transform.localScale = new Vector3(, , 0.1);
    }
}
