using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class Target : MonoBehaviour
{
    //Instance
    public static int scoreToAdd;
    public static float lifeTime = 0f;
    //Other Objs
    public static GameObject scoreDesc;
    
    // Start is called before the first frame update
    void Start()
    {
        ScoreKeeper.notesGiven++;
        lifeTime = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        lifeTime += Time.deltaTime;
    }

    public static void manualDestroy()
    {
        if ((500 - (int)(lifeTime * 100)) > 0)
        {
            scoreToAdd = (int)((1 + (ScoreKeeper.combo * 0.05)) * (500 - ((lifeTime) * 100)));
            ScoreKeeper.score += scoreToAdd;
        }
        //Debug.Log("Score Updated");
        ScoreKeeper.noteHits++;
        ScoreKeeper.combo++;
        //GameObject textObj = GameObject.FindGameObjectWithTag("ScoreDesc");
        //Debug.Log("I'm destroying the target");
        Destroy(GameObject.FindGameObjectWithTag("Target"));
    }
    public static void naturalDestroy()
    {
        ScoreKeeper.combo = 0;
        //GameObject.FindGameObjectWithTag("ScoreDesc").GetComponent<TextMesh>().text = "Miss";
        Destroy(GameObject.FindGameObjectWithTag("Target"));
    }
}
