using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class HeldTarget : MonoBehaviour
{
    //Instance
    public float lifeTime = 0f;
    public float destroyTimer;
    //Set by Player when right-clicked; a held note that runs out without ever being grabbed is a miss
    public bool grabbed;
    //Chart time of this note (hit log)
    public float chartTime;

    // Start is called before the first frame update
    void Start()
    {
        ScoreKeeper.notesGiven++;
        lifeTime = 0f;
        NoteVisual.Attach(gameObject, NoteVisual.Kind.Held);
    }

    // Update is called once per frame
    void Update()
    {
        naturalDestroy();
        lifeTime += Time.deltaTime;
        this.transform.localScale = new Vector3((0.5f / destroyTimer) * (destroyTimer - lifeTime), (destroyTimer - lifeTime) * (0.5f / destroyTimer), 0.001f);
    }

    public void naturalDestroy()
    {
        if(lifeTime>=destroyTimer)
        {
            if (!grabbed)
            {
                //Missed entirely: same as letting a tap note expire
                ScoreKeeper.combo = 0;
                HitLog.Record(chartTime, true, 0, 0f);
                Player player = FindFirstObjectByType<Player>();
                if (player != null && player.scoreDesc != null)
                {
                    //scoreToAdd defaults to 0, which shows "MISS" and records the miss
                    Instantiate(player.scoreDesc, transform.position, Quaternion.identity);
                }
            }
            Destroy(this.gameObject);
        }
    }
}