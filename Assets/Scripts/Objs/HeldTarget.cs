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
    //Other Objs

    // Start is called before the first frame update
    void Start()
    {
        ScoreKeeper.notesGiven++;
        lifeTime = 0f;
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
            //GameObject scoreDesc1 = Instantiate(scoreDesc, new Vector2(this.transform.position.x + 0.15f, this.transform.position.y + 0.005f), Quaternion.identity);
            Destroy(this.gameObject);
        }
    }
}