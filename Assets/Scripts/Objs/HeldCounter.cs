using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeldCounter : MonoBehaviour
{
    //Prefabs
    public GameObject heldPrefab;
    //Other Scripts
    //Objects
    //Instance
    private float destroyTimer = 3f;
    //private int notesInSecond = 0;
    private Vector3 spawnPos = new Vector3(0.0f, 0.0f, 0.0f);
    // Start is called before the first frame update
    void Start()
    {
        this.transform.localScale = new Vector3((0.5f / 3) * (3.0f - destroyTimer), 0.001f, (3.0f - destroyTimer) * (0.5f / 3));
        spawnPos = Spawner.spawnPos;
    }

    // Update is called once per frame
    void Update()
    {
        if (!Pause.isPaused)
        {

            this.transform.localScale = new Vector3((0.5f / 3) * (3.0f - destroyTimer), 0.001f, (3.0f - destroyTimer) * (0.5f / 3));
            destroyTimer -= Time.deltaTime;
            if (destroyTimer <= 0)
            {

                //Debug.Log("Spawn a target");

                //Target.lifeTime = 0;
                if (GameObject.FindGameObjectWithTag("Target") != null)
                {
                    //Debug.Log("I'm destroying the Target");
                    Target.naturalDestroy();
                }
                Instantiate(heldPrefab, transform.position, Quaternion.AngleAxis(90, Vector3.right));
                ScoreKeeper.bloom += 0.01f;
                Destroy(this.gameObject);
            }

        }


    }
}