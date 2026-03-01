using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
public class Player : MonoBehaviour
{
    //Variables
    //Instance
    public static float accuracy;
    public static bool holding;
    public float holdTime;
    private double holdScore;
    private GameObject heldHit;
    private GameObject hitTarget;
    private Vector2 heldPos;
    //Mouse Look
    public static float speedH = 40f;
    public static float speedV = 40f;
    public float yaw = 0.0f;
    public float pitch = 0.0f;
    //Score
    private int shotsFired = 0;
    private int shotsHit = 0;
    public static int scoreToAdd;
    //Text
    public TextMeshProUGUI accuracyText;
    //Objs
    public GameObject scoreDesc;
    public GameObject trailPrefab;
    // Start is called before the first frame update
    void Start()
    {
        Pause.isPaused = false;
        //Mouse Lock
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        if (!Pause.isPaused)
        {
            mouseLook();
            shoot();
            hold();
            //showText();
        }
        
        
    }
    //Accuracy text: shots hit over shots fired NOT USED ANYMORE!
    private void showText()
    {
        accuracy = Mathf.Round(((float)shotsHit / shotsFired) * 100);
        accuracyText.text = "Accuracy: " + accuracy + "%";
    }
    //Shoot for targets
    private void shoot()
    {
        //Mouse input
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = new Ray();
            ray.origin = GameObject.FindGameObjectWithTag("Player").transform.position;
            ray.direction = GameObject.FindGameObjectWithTag("Player").transform.forward;
            Debug.DrawRay(ray.origin, ray.direction * 75f, Color.red, 3f);
            RaycastHit hit;
            shotsFired++;
            //Debug.Log(shotsFired);
            if (Physics.Raycast(ray, out hit, 75.0f))
            {
                GameObject targetHit = hit.collider.gameObject;
                if (hit.transform.tag == "Target")
                {
                    shotsHit++;
                    //Debug.Log("I'm destroying the Target");
                    Target.manualDestroy();
                    scoreToAdd = (int)(500 - ((Target.lifeTime) * 100));
                    ScoreKeeper.score += (int)((1 + (ScoreKeeper.combo * 0.05)) * scoreToAdd);
                    
                    //Instantiate(trailPrefab, transform.position, Quaternion.identity);
                    //Debug.Log(scoreToAdd);
                    GameObject scoreDesc1 = Instantiate(scoreDesc, new Vector2(targetHit.transform.position.x+0.15f, targetHit.transform.position.y+0.005f), Quaternion.identity);
                }
                //If hit a counter while trying to hit a target, destroy the counter. (Maybe remove?)
                /*
                if (hit.transform.tag == "Counter")
                {
                    targetHit = hit.collider.gameObject;
                    scoreToAdd = 0;
                    //Debug.Log(scoreToAdd);
                    
                    GameObject scoreDesc1 = Instantiate(scoreDesc, new Vector2(targetHit.transform.position.x + 0.15f, targetHit.transform.position.y + 0.005f), Quaternion.identity);
                    Destroy(targetHit);
                    ScoreKeeper.combo = 0;
                }
                */
            }
        }
    }
    //Right click for helds
    private void hold()
    {
        if (Input.GetMouseButtonDown(1) && !holding)
        {
            Ray ray = new Ray();
            ray.origin = GameObject.FindGameObjectWithTag("Player").transform.position;
            ray.direction = GameObject.FindGameObjectWithTag("Player").transform.forward;

            //Debug.DrawRay(ray.origin, ray.direction * 75f, Color.red, 3f);
            RaycastHit hit;
            shotsFired++;
            if (Physics.Raycast(ray, out hit, 75.0f))
            {
                heldHit = hit.collider.gameObject;
                heldPos = new Vector2(heldHit.transform.position.x, heldHit.transform.position.y + 0.005f);
                if (hit.transform.tag == "HeldTarget")
                {
                    holding = true;
                    holdTime = 0;
                    ScoreKeeper.combo++;
                }
                //If hit a target while trying to hit a held
                /*
                if (hit.transform.tag == "Counter")
                {
                    holdTime = 0;
                    scoreToAdd = 0;
                    GameObject heldHit = hit.collider.gameObject;
                    GameObject scoreDesc1 = Instantiate(scoreDesc, new Vector2(heldHit.transform.position.x + 0.15f, heldHit.transform.position.y + 0.005f), Quaternion.identity);
                    Destroy(heldHit);
                    ScoreKeeper.combo = 0;
                }
                */
            }
        }
        if (heldHit == null && Input.GetMouseButton(1) && holding)
        {
            holdTime -= 5*Time.deltaTime;
        }
        else if ((heldHit == null || !Input.GetMouseButton(1)) && holding)
        {
            holding = false;
            holdScore = (double)(holdTime / HeldTarget.destroyTimer) * 500;
            scoreToAdd = (int)holdScore;
            if (scoreToAdd>0)
            {
                ScoreKeeper.score += scoreToAdd;
            }
            GameObject scoreDesc1 = Instantiate(scoreDesc, heldPos, Quaternion.identity);
            Destroy(heldHit);
        }
        if (holding && heldHit != null && Input.GetMouseButton(1))
        {
            holdTime += Time.deltaTime;
        }
    }

    private void mouseLook()
    {
        //Mouse Look
        yaw += speedH*0.01f * Input.GetAxis("Mouse X");
        pitch -= speedV*0.01f * Input.GetAxis("Mouse Y");
        transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);
    }
}
