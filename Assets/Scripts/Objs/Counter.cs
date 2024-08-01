using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Counter : MonoBehaviour
{
    //Prefabs
    public GameObject heldPrefab;
    public GameObject targetPrefab;
    public GameObject trailPrefab;
    //Other Scripts
    //Objects
    //Material
    public Material notiPri;
    public Material noti;
    public Material heldNoti;
    //Instance
    public bool isPri = false;
    public float destroyTimer = 3f;
    public bool isHeld = false;
    //private int notesInSecond = 0;
    private Vector3 spawnPos = new Vector3(0.0f, 0.0f, 0.0f);
    // Start is called before the first frame update
    void Start()
    {
        this.transform.localScale = new Vector3((0.5f / 3) * (3.0f - destroyTimer), 0.0f, (3.0f - destroyTimer) * (0.5f / 3));
        spawnPos = Spawner.spawnPos;
        if(isHeld)
        {
            this.gameObject.GetComponent<Renderer>().material = heldNoti;
        }
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
            if (!isHeld)
            {
                if (isPri)
                {
                    this.GetComponent<Renderer>().material = notiPri;
                    this.transform.Translate(Vector3.forward * Time.deltaTime * 0.001f);
                }
                else
                {
                    this.GetComponent<Renderer>().material = noti;
                }
            }
            

            this.transform.localScale = new Vector3((0.5f / 3) * (3.0f - destroyTimer), 0.001f, (3.0f - destroyTimer) * (0.5f / 3));
            destroyTimer -= Time.deltaTime;
            //Debug.Log(destroyTimer);
            if (destroyTimer <= 0)
            {
                if (isHeld)
                {
                    //Debug.Log("Spawn a target");
                    if (GameObject.FindGameObjectWithTag("Target") != null)
                    {
                        //Debug.Log("I'm destroying the Target");
                        Target.naturalDestroy();
                    }
                    Instantiate(heldPrefab, transform.position, Quaternion.identity);
                    Instantiate(trailPrefab, transform.position, Quaternion.identity);
                    ScoreKeeper.bloom += 0.01f;
                    Destroy(this.gameObject);
                }
                else
                {
                    if (GameObject.FindGameObjectWithTag("Target") != null)
                    {
                        //Debug.Log("I'm destroying the Target");
                        Target.naturalDestroy();
                    }
                    Instantiate(targetPrefab, transform.position, Quaternion.identity);
                    Instantiate(trailPrefab, transform.position, Quaternion.identity);
                    ScoreKeeper.bloom += 0.01f;
                    Destroy(this.gameObject);
                }
                
            }
            
        }
        
        
    }
}