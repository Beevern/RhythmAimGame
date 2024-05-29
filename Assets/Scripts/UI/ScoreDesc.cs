using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class ScoreDesc : MonoBehaviour
{
    public float timeToDestroy = 0.5f;
    public static int scoreToAdd;
    // Start is called before the first frame update
    void Start()
    {
        scoreToAdd = Target.scoreToAdd;
        Debug.Log("transferred");
        if (scoreToAdd >= 475)
        {
            GameObject.FindGameObjectWithTag("ScoreDesc").GetComponent<TextMesh>().text = "Perfect";
        }
        else if (scoreToAdd >= 400)
        {
            GameObject.FindGameObjectWithTag("ScoreDesc").GetComponent<TextMesh>().text = "Great";
        }
        else if (scoreToAdd >= 1)
        {
            GameObject.FindGameObjectWithTag("ScoreDesc").GetComponent<TextMesh>().text = "Nice";
        }
        else if (scoreToAdd <= 0)
        {
            GameObject.FindGameObjectWithTag("ScoreDesc").GetComponent<TextMesh>().text = "Too early";
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
