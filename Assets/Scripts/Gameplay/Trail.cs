using UnityEngine;

//Streak that travels from a note that just spawned to the next note, arriving as that note spawns.
//Keeping the crosshair on its moving head ("tracking the white line") earns points over time;
//the streak lights up while you're on it.
public class Trail : MonoBehaviour
{
    //Points per second while tracking (before the combo multiplier)
    public const float TrackPointsPerSecond = 150f;
    //How close the aim has to pass to the head, in world units (a note is 0.5 across)
    private const float TrackRadius = 0.3f;

    private Vector3 finalPos;
    private float speed;
    private TrailRenderer[] trails;
    private Transform aim;
    private float pointBank;
    private float glow;

    void Start()
    {
        trails = GetComponentsInChildren<TrailRenderer>();
        foreach (TrailRenderer tr in trails)
        {
            tr.sharedMaterial = Theme.SpriteMaterial;
            tr.time = 0.35f;
            tr.sortingOrder = 5;
        }
        ApplyLook();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) aim = player.transform;

        //Head for whichever counter is due next (the one that spawned us is already at 0)
        Counter next = Counter.NextAfter(null);
        if (next == null)
        {
            //Last note of the song: nothing to travel to
            Destroy(gameObject);
            return;
        }
        finalPos = next.transform.position;
        speed = Vector3.Distance(transform.position, finalPos) / Mathf.Max(next.destroyTimer, 0.01f);
    }

    void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, finalPos, speed * Time.deltaTime);

        if (!Pause.isPaused && Time.deltaTime > 0f)
        {
            bool tracking = IsAimedAt(transform.position);
            RunStats.trailSeconds += Time.deltaTime;
            if (tracking)
            {
                RunStats.trackedSeconds += Time.deltaTime;
                pointBank += TrackPointsPerSecond * (1f + ScoreKeeper.combo * 0.05f) * Time.deltaTime;
                int whole = (int)pointBank;
                if (whole > 0)
                {
                    ScoreKeeper.score += whole;
                    RunStats.trackPoints += whole;
                    pointBank -= whole;
                }
            }
            glow = Mathf.MoveTowards(glow, tracking ? 1f : 0f, Time.deltaTime * 8f);
            ApplyLook();
        }

        if (transform.position == finalPos)
        {
            Destroy(gameObject);
        }
    }

    //Does the crosshair's line of sight pass close to this point?
    private bool IsAimedAt(Vector3 point)
    {
        if (aim == null) return false;
        Vector3 toPoint = point - aim.position;
        float along = Vector3.Dot(toPoint, aim.forward);
        if (along <= 0f) return false;
        return (toPoint - aim.forward * along).sqrMagnitude <= TrackRadius * TrackRadius;
    }

    //White streak normally; blue and a bit thicker while being tracked
    private void ApplyLook()
    {
        Color c = Color.Lerp(Color.white, Theme.Perfect, glow);
        foreach (TrailRenderer tr in trails)
        {
            tr.widthMultiplier = Mathf.Lerp(2.2f, 3.2f, glow);
            tr.startColor = c;
            tr.endColor = c.WithAlpha(0f);
        }
    }
}
