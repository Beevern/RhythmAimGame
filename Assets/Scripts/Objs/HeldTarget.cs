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
        naturalDestroy();
        lifeTime += Time.deltaTime;
        this.transform.localScale = new Vector3((0.5f / destroyTimer) * (destroyTimer - lifeTime), 0.001f, (destroyTimer - lifeTime) * (0.5f / destroyTimer));
        //this.transform.localScale = new Vector3((0.5f / 3) * (destroyTimer - lifeTime), 0.001f, (destroyTimer - lifeTime) * (0.5f / 3));
    }

    public void naturalDestroy()
    {
        if(lifeTime>destroyTimer)
        {
            Destroy(this.gameObject);
        }
    }
}