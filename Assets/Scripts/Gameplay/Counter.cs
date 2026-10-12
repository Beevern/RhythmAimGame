using System.Collections.Generic;
using UnityEngine;

//Approach note ("noti"): counts down from 3 seconds, then spawns a Target or HeldTarget in its place.
public class Counter : MonoBehaviour
{
    //Prefabs
    public GameObject heldPrefab;
    public GameObject targetPrefab;
    public GameObject trailPrefab;
    //Material (unused since notes are drawn by NoteVisual; kept so prefab references stay valid)
    public Material notiPri;
    public Material noti;
    public Material heldNoti;
    //Instance
    public float destroyTimer = 3f;
    public bool isHeld = false;
    public float timeTransfer = 0;
    //This note's time in the level's chart (before offset); identifies it in the hit log
    public float chartTime;
    public Vector3 spawnPos = new Vector3(0.0f, 0.0f, 0.0f);

    //Last stretch before the target appears, during which the counter itself can be hit "too early"
    public const float EarlyWindow = 0.2f;
    public const int EarlyPoints = 50;
    //Within this much of the target appearing, hitting the counter counts as a perfect hit instead
    public const float GraceWindow = 0.1f;
    private bool consumed;

    //Only the next note (the red one) can be hit early
    public bool CanHitEarly => !consumed && destroyTimer > 0f && destroyTimer <= EarlyWindow && IsPriority;

    //Hit before its target appeared: the note is used up and never spawns a target
    public void ConsumeEarly()
    {
        consumed = true;
        Destroy(gameObject);
    }

    //Every live counter, so nothing has to search the scene by tag each frame
    public static readonly List<Counter> Active = new List<Counter>();
    private static int priorityFrame = -1;
    private static float lowestTimer;

    //True for the counter(s) that will spawn next
    public bool IsPriority
    {
        get
        {
            //Work out the lowest timer once per frame, shared by every counter
            if (priorityFrame != Time.frameCount)
            {
                priorityFrame = Time.frameCount;
                lowestTimer = float.MaxValue;
                foreach (Counter c in Active) lowestTimer = Mathf.Min(lowestTimer, c.destroyTimer);
            }
            return destroyTimer <= lowestTimer;
        }
    }

    //The next counter due to spawn after this one, or null
    public static Counter NextAfter(Counter current)
    {
        Counter next = null;
        foreach (Counter c in Active)
        {
            if (c == current || c.destroyTimer <= 0f) continue;
            if (next == null || c.destroyTimer < next.destroyTimer) next = c;
        }
        return next;
    }

    void OnEnable() { Active.Add(this); }
    void OnDisable() { Active.Remove(this); }

    void Start()
    {
        UpdateScale();
        spawnPos = Spawner.spawnPos;
        NoteVisual.Attach(gameObject, NoteVisual.Kind.Approach);
    }

    void Update()
    {
        if (Pause.isPaused || consumed) return;

        //Make bigger as time goes down (scale drives the collider; NoteVisual draws the note)
        UpdateScale();
        destroyTimer -= Time.deltaTime;

        //Spawn either a target or held target
        if (destroyTimer <= 0)
        {
            //Only one target can be up at a time: an unhit one counts as a miss
            GameObject target = GameObject.FindGameObjectWithTag("Target");
            if (target != null)
            {
                target.GetComponent<Target>().naturalDestroy();
            }
            if (isHeld)
            {
                GameObject heldTarget = Instantiate(heldPrefab, transform.position, Quaternion.identity);
                heldTarget.GetComponent<HeldTarget>().destroyTimer = timeTransfer;
                heldTarget.GetComponent<HeldTarget>().chartTime = chartTime;
            }
            else
            {
                Instantiate(targetPrefab, transform.position, Quaternion.identity).GetComponent<Target>().chartTime = chartTime;
            }
            Instantiate(trailPrefab, transform.position, Quaternion.identity);
            ScoreKeeper.bloom += 0.01f;
            Destroy(gameObject);
        }
    }

    private void UpdateScale()
    {
        float s = (0.5f / 3f) * (3.0f - destroyTimer);
        transform.localScale = new Vector3(s, 0.001f, s);
    }
}
