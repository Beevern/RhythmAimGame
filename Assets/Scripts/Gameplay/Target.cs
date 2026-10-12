using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Target : MonoBehaviour
{
    //Instance
    public float lifeTime = 0f;
    //Chart time of this note (hit log)
    public float chartTime;
    //Other Objs
    public GameObject scoreDesc;
    // Start is called before the first frame update
    void Start()
    {
        ScoreKeeper.notesGiven++;
        lifeTime = 0f;
        //Custom target art comes from target.png in the level's folder
        NoteVisual.Attach(gameObject, NoteVisual.Kind.Target, LevelHolder.TargetSprite(SongSelect2.theSong));
    }

    // Update is called once per frame
    void Update()
    {
        lifeTime += Time.deltaTime;
        
    }

    //Destroy if hit by player
    public static void manualDestroy()
    {
        ScoreKeeper.noteHits++;
        ScoreKeeper.combo++;
        //Why not "this"?
        Destroy(GameObject.FindGameObjectWithTag("Target"));
    }
    public void naturalDestroy()
    {
        ScoreKeeper.combo = 0;
        HitLog.Record(chartTime, false, 0, 0f);
        Vector3 pos = GameObject.FindGameObjectWithTag("Target").GetComponent<Transform>().position;
        GameObject scoreDesc1 = Instantiate(scoreDesc, pos, Quaternion.identity);
        scoreDesc1.GetComponent<ScoreDesc>().scoreToAdd = 0;
        //Why not "this"?
        Destroy(GameObject.FindGameObjectWithTag("Target"));
    }

}
