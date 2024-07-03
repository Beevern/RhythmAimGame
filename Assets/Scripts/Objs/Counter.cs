using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Counter : MonoBehaviour
{
    //Prefabs
    public GameObject targetPrefab;
    //Other Scripts
    //Objects
    //Material
    public Material notiPri;
    public Material noti;
    //Instance
    private bool isPri = false;
    private float destroyTimer = 3f;
    //private int notesInSecond = 0;
    private Vector3 spawnPos = new Vector3(0.0f, 0.0f, 0.0f);
    // Start is called before the first frame update
    void Start()
    {
        this.transform.localScale = new Vector3((0.5f / 3) * (3.0f - destroyTimer), 0.0f, (3.0f - destroyTimer) * (0.5f / 3));
        spawnPos = Spawner.spawnPos;
    }

    // Update is called once per frame
    void Update()
    {
        if (!Pause.isPaused)
        {
            isPri = true;
            foreach (GameObject obj in GameObject.FindGameObjectsWithTag("Counter"))
            {
                if (this.destroyTimer > obj.GetComponent<Counter>().destroyTimer)
                {
                    isPri = false;
                }
            }
            if (isPri)
            {
                this.GetComponent<Renderer>().material = notiPri;
                this.transform.Translate(Vector3.forward * Time.deltaTime * 0.001f);
            }
            else
            {
                this.GetComponent<Renderer>().material = noti;
            }

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
                Instantiate(targetPrefab, transform.position, Quaternion.AngleAxis(90, Vector3.right));
                /*
                else
                {
                    if (GameObject.FindGameObjectWithTag("Counter") != null)
                    {
                        //Debug.Log("I'm destroying the Target");
                        Destroy(GameObject.FindGameObjectWithTag("Counter"));
                    }
                    Instantiate(heldPrefab, transform.position, Quaternion.AngleAxis(90, Vector3.right));
                }
                */
                ScoreKeeper.bloom += 0.01f;
                Destroy(this.gameObject);
            }
            
        }
        
        
    }
}