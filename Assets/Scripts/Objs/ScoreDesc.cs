using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class ScoreDesc : MonoBehaviour
{
    public float timeToDestroy = 0.3f;
    public int scoreToAdd;
    // Start is called before the first frame update
    void Start()
    {
        //Debug.Log("transferred");
        //Debug.Log(scoreToAdd);
        if (scoreToAdd >= 475)
        {
            this.GetComponent<TextMesh>().text = "Perfect";
        }
        else if (scoreToAdd >= 400)
        {
            this.GetComponent<TextMesh>().text = "Great";
        }
        else if (scoreToAdd >= 300)
        {
            this.GetComponent<TextMesh>().text = "Good";
        }
        else if (scoreToAdd >= 1)
        {
            this.GetComponent<TextMesh>().text = "Hit";
        }
        else if (scoreToAdd <= 0)
        {
            this.GetComponent<TextMesh>().text = "Miss";
        }
    }

    // Update is called once per frame
    void Update()
    {
        timeToDestroy -= Time.deltaTime;
        if (timeToDestroy <=0)
        {

            Destroy(this.gameObject);
        }
        
    }
}
