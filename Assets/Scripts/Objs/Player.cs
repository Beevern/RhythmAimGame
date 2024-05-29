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
    //Mouse Look
    public static float speedH = 0.5f;
    public static float speedV = 0.5f;
    public float yaw = 0.0f;
    public float pitch = 0.0f;
    //Score
    private int shotsFired = 0;
    private int shotsHit = 0;
    //Text
    public TextMeshProUGUI accuracyText;
    //Objs
    public GameObject scoreDesc;
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
            //showText();
        }
        
        
    }
    private void showText()
    {
        accuracy = Mathf.Round(((float)shotsHit / shotsFired) * 100);
        accuracyText.text = "Accuracy: " + accuracy + "%";
    }
    private void shoot()
    {
        //Mouse input
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
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
                if (hit.transform.tag == "Target")
                {
                    shotsHit++;
                    Debug.Log("I'm destroying the Target");
                    Target.manualDestroy();
                    GameObject targetHit = hit.collider.gameObject;
                    GameObject scoreDesc1 = Instantiate(scoreDesc, new Vector2(targetHit.transform.position.x+0.15f, targetHit.transform.position.y+0.005f), Quaternion.identity);
                }
                if (hit.transform.tag == "Counter")
                {
                    Destroy(hit.collider.gameObject);
                    GameObject targetHit = hit.collider.gameObject;
                    GameObject scoreDesc1 = Instantiate(scoreDesc, new Vector2(targetHit.transform.position.x + 0.15f, targetHit.transform.position.y + 0.005f), Quaternion.identity);
                    ScoreDesc.scoreToAdd = 0;
                }
            }
        }
    }

    private void hold()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = new Ray();
            ray.origin = GameObject.FindGameObjectWithTag("Player").transform.position;
            ray.direction = GameObject.FindGameObjectWithTag("Player").transform.forward;
            //Debug.DrawRay(ray.origin, ray.direction * 75f, Color.red, 3f);
            RaycastHit hit;
            shotsFired++;
            if (Physics.Raycast(ray, out hit, 75.0f))
            {
                if (hit.transform.tag == "HeldTarget")
                {
                    while (Input.GetMouseButtonDown(1) && GameObject.FindGameObjectWithTag("HeldTarget") != null)
                    {
                        ScoreKeeper.score += 30;
                    }

                }
            }
        }
    }

    private void mouseLook()
    {
        //Mouse Look
        yaw += speedH * Input.GetAxis("Mouse X");
        pitch -= speedV * Input.GetAxis("Mouse Y");
        transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);
    }
}
