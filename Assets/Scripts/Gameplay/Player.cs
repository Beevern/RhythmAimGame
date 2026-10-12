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
        //Saved sensitivity (also kept in sync by Settings)
        speedH = PlayerPrefs.GetFloat("xSen", speedH);
        speedV = PlayerPrefs.GetFloat("ySen", speedV);
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
            RaycastHit hit;
            shotsFired++;
            RunStats.shotsFired++;
            if (Physics.Raycast(ray, out hit, 75.0f))
            {
                
                if (hit.transform.tag == "Target")
                {
                    shotsHit++;
                    GameObject targetHit = hit.collider.gameObject;
                    Target.manualDestroy();
                    RunStats.shotsHit++;
                    RunStats.reactionTimes.Add(targetHit.GetComponent<Target>().lifeTime);
                    scoreToAdd = (int)(500 - ((targetHit.GetComponent<Target>().lifeTime) * 100));
                    ScoreKeeper.score += (int)((1 + (ScoreKeeper.combo * 0.05)) * scoreToAdd);
                    RunStats.baseScore += Mathf.Max(0, scoreToAdd);
                    
                    HitFx.Burst(targetHit.transform.position, scoreToAdd);
                    HitLog.Record(targetHit.GetComponent<Target>().chartTime, false, scoreToAdd, targetHit.GetComponent<Target>().lifeTime);
                    GameObject scoreDesc1 = Instantiate(scoreDesc, targetHit.transform.position, Quaternion.identity);
                    scoreDesc1.GetComponent<ScoreDesc>().scoreToAdd = this.scoreToAdd;
                }
                else if (hit.transform.tag == "Counter")
                {
                    TryHitEarly(hit.collider.GetComponent<Counter>(), false);
                }
            }
        }
    }

    //Hitting the next note's approach disc just before its target appears (Counter.EarlyWindow):
    //  - within Counter.GraceWindow (tap notes): counts as a PERFECT hit, as if hit on time
    //  - otherwise: "TOO EARLY", flat 50 points, combo left exactly as it is, and it is not
    //    counted as a hit for participation
    //Either way the note is used up. Taps need left click and held notes right click, like their targets.
    private bool TryHitEarly(Counter counter, bool rightClick)
    {
        if (counter == null || !counter.CanHitEarly || counter.isHeld != rightClick) return false;
        Vector3 pos = counter.transform.position;
        ScoreKeeper.notesGiven++;
        RunStats.shotsHit++;
        ScoreDesc desc = Instantiate(scoreDesc, pos, Quaternion.identity).GetComponent<ScoreDesc>();
        if (!counter.isHeld && counter.destroyTimer <= Counter.GraceWindow)
        {
            const int perfect = 500;
            ScoreKeeper.noteHits++;
            ScoreKeeper.combo++;
            ScoreKeeper.score += (int)((1 + (ScoreKeeper.combo * 0.05)) * perfect);
            RunStats.reactionTimes.Add(0f);
            RunStats.baseScore += perfect;
            HitFx.Burst(pos, perfect);
            desc.scoreToAdd = perfect;
            HitLog.Record(counter.chartTime, false, perfect, 0f);
        }
        else
        {
            ScoreKeeper.score += Counter.EarlyPoints;
            RunStats.baseScore += Counter.EarlyPoints;
            HitFx.Early(pos);
            desc.scoreToAdd = Counter.EarlyPoints;
            desc.early = true;
            HitLog.RecordEarly(counter.chartTime, counter.isHeld);
        }
        counter.ConsumeEarly();
        return true;
    }
    //Right click for helds
    private void hold()
    {
        if (Input.GetMouseButtonDown(1) && !holding)
        {
            Ray ray = new Ray();
            ray.origin = playerTransform.position;
            ray.direction = playerTransform.forward;

            RaycastHit hit;
            shotsFired++;
            RunStats.shotsFired++;
            if (Physics.Raycast(ray, out hit, 75.0f))
            {
                heldHit = hit.collider.gameObject;
                heldPos = heldHit.transform.position;
                if (hit.transform.tag == "Counter")
                {
                    TryHitEarly(hit.collider.GetComponent<Counter>(), true);
                }
                else if (hit.transform.tag == "HeldTarget")
                {
                    GameObject scoreDesc1 = Instantiate(scoreDesc, heldPos, Quaternion.identity);
                    scoreDesc1.GetComponent<ScoreDesc>().scoreToAdd = (int)(500 - (heldHit.GetComponent<HeldTarget>().lifeTime * 100));
                    HitFx.Burst(heldPos, scoreDesc1.GetComponent<ScoreDesc>().scoreToAdd);
                    heldTargetMaxTime = heldHit.GetComponent<HeldTarget>().destroyTimer;
                    HitLog.Record(heldHit.GetComponent<HeldTarget>().chartTime, true, scoreDesc1.GetComponent<ScoreDesc>().scoreToAdd, heldHit.GetComponent<HeldTarget>().lifeTime);
                    RunStats.shotsHit++;
                    RunStats.reactionTimes.Add(heldHit.GetComponent<HeldTarget>().lifeTime);
                    holding = true;
                    holdTime = 0;
                    //Any grab counts as a hit and builds combo; a weak release later never breaks it
                    heldHit.GetComponent<HeldTarget>().grabbed = true;
                    ScoreKeeper.noteHits++;
                    ScoreKeeper.combo++;
                }
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
            RunStats.holdRatios.Add(Mathf.Clamp01(holdTime / heldTargetMaxTime));
            scoreToAdd = (int)holdScore;
            //Makes sure we don't go negative incase the player holds right click wayyyyy too long
            if (scoreToAdd>0)
            {
                ScoreKeeper.score += (int)((1 + (ScoreKeeper.combo * 0.05)) * scoreToAdd);
                RunStats.baseScore += Mathf.Min(scoreToAdd, 500);
            }
            GameObject scoreDesc1 = Instantiate(scoreDesc, heldPos, Quaternion.identity);
            scoreDesc1.GetComponent<ScoreDesc>().scoreToAdd = this.scoreToAdd;
            HitFx.Burst(heldPos, scoreToAdd);
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
