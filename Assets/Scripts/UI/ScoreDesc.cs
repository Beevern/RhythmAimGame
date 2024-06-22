using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class ScoreDesc : MonoBehaviour
{
    public float timeToDestroy = 0.5f;
    public int scoreToAdd;
    // Start is called before the first frame update
    void Start()
    {
        scoreToAdd = Player.scoreToAdd;
        //Debug.Log("transferred");
        Debug.Log(scoreToAdd);
        if (scoreToAdd >= 475)
        {
            this.GetComponent<TextMesh>().text = "Perfect";
        }
        else if (scoreToAdd >= 400)
        {
            this.GetComponent<TextMesh>().text = "Great";
        }
        else if (scoreToAdd >= 1)
        {
            this.GetComponent<TextMesh>().text = "Nice";
        }
        else if (scoreToAdd <= 0)
        {
            this.GetComponent<TextMesh>().text = "Too early";
        }
        Destroy(GameObject.FindGameObjectWithTag("Target"));
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
