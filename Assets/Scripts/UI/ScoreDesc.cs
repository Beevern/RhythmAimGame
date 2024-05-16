using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreDesc : MonoBehaviour
{
    public float timeToDestroy = 0.5f;
    private int scoreToAdd;
    // Start is called before the first frame update
    void Start()
    {
        scoreToAdd = Target.scoreToAdd;
        Debug.Log("transferred");
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
