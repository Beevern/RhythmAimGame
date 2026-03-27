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
    private Transform playerTransform;
    private float heldTargetMaxTime;
    //Mouse Look
    public static float speedH = 40f;
    public static float speedV = 40f;
    public float yaw = 0.0f;
    public float pitch = 0.0f;
    //Score
    private int shotsFired = 0;
    private int shotsHit = 0;
    public int scoreToAdd;
    //Text
    public TextMeshProUGUI accuracyText;
    //Objs
    public GameObject scoreDesc;
    public GameObject trailPrefab;
    // Start is called before the first frame update
    void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
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
    //Accuracy text: shots hit over shots fired (NOT USED ANYMORE!)
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
            ray.origin = playerTransform.position;
            ray.direction = playerTransform.forward;
            Debug.DrawRay(ray.origin, ray.direction * 75f, Color.red, 3f);
            RaycastHit hit;
            shotsFired++;
            //Debug.Log(shotsFired);
            if (Physics.Raycast(ray, out hit, 75.0f))
            {
                
                if (hit.transform.tag == "Target")
                {
                    shotsHit++;
                    //Debug.Log("I'm destroying the Target");
                    GameObject targetHit = hit.collider.gameObject;
                    Target.manualDestroy();
                    scoreToAdd = (int)(500 - ((targetHit.GetComponent<Target>().lifeTime) * 100));
                    ScoreKeeper.score += (int)((1 + (ScoreKeeper.combo * 0.05)) * scoreToAdd);
                    
                    //Instantiate(trailPrefab, transform.position, Quaternion.identity);
                    //Debug.Log(scoreToAdd);
                    GameObject scoreDesc1 = Instantiate(scoreDesc, new Vector2(targetHit.transform.position.x+0.15f, targetHit.transform.position.y+0.005f), Quaternion.identity);
                    scoreDesc1.GetComponent<ScoreDesc>().scoreToAdd = this.scoreToAdd;
                }
                //If hit a counter while trying to hit a target, destroy the counter. (Maybe remove? | Currently removed)
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
            ray.origin = playerTransform.position;
            ray.direction = playerTransform.forward;

            //Debug.DrawRay(ray.origin, ray.direction * 75f, Color.red, 3f);
            RaycastHit hit;
            shotsFired++;
            if (Physics.Raycast(ray, out hit, 75.0f))
            {
                heldHit = hit.collider.gameObject;
                heldPos = new Vector2(heldHit.transform.position.x, heldHit.transform.position.y + 0.005f);
                if (hit.transform.tag == "HeldTarget")
                {
                    GameObject scoreDesc1 = Instantiate(scoreDesc, heldPos, Quaternion.identity);
                    scoreDesc1.GetComponent<ScoreDesc>().scoreToAdd = (int)(500 - (heldHit.GetComponent<HeldTarget>().lifeTime * 100));
                    heldTargetMaxTime = heldHit.GetComponent<HeldTarget>().destroyTimer;
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
        //if the held target has disappeared but still holding right click, reduce holdTime which will reduce the points to add
        if (heldHit == null && Input.GetMouseButton(1) && holding)
        {
            //Penalty multiplier for overholding
            int overshootPen = 1;
            holdTime -= overshootPen*Time.deltaTime;
        }
        //Held target has disappeared and player has stopped holding right click
        //OR the held target still exists but player has stopped holding right click
        //In the case that the target is gone but we are still holding right click, the first if statement covers that
        else if ((heldHit == null || !Input.GetMouseButton(1)) && holding)
        {
            holding = false;
            holdScore = (double)(holdTime / heldTargetMaxTime) * 500;
            scoreToAdd = (int)holdScore;
            //Makes sure we don't go negative incase the player holds right click wayyyyy too long
            if (scoreToAdd>0) { ScoreKeeper.score += (int)((1 + (ScoreKeeper.combo * 0.05)) * scoreToAdd); }
            GameObject scoreDesc1 = Instantiate(scoreDesc, heldPos, Quaternion.identity);
            scoreDesc1.GetComponent<ScoreDesc>().scoreToAdd = this.scoreToAdd;
            Destroy(heldHit);
        }
        //Player is holding right click while target is present. Add holdTime which will increase the points to add
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
