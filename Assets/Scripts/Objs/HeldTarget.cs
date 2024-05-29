using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class HeldTarget : MonoBehaviour
{
    //Instance
    public static int heldScoreToAdd;
    public static float lifeTime = 0f;
    public static float destroyTimer;
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
        //this.transform.localScale = new Vector3((0.5f / 3) * (3.0f - destroyTimer), 0.001f, (3.0f - destroyTimer) * (0.5f / 3));
    }

    public static void manualDestroy()
    {
        //Debug.Log("Score Updated");
        ScoreKeeper.noteHits++;
        ScoreKeeper.combo++;
        //GameObject textObj = GameObject.FindGameObjectWithTag("ScoreDesc");
        //Debug.Log("I'm destroying the target");
        Destroy(GameObject.FindGameObjectWithTag("HeldTarget"));
    }
}