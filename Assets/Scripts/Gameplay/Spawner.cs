using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using TMPro;

//Spawns approach notes (Counters) on the song's chart times and ends the level when the video finishes
public class Spawner : MonoBehaviour
{
    public TextMeshProUGUI startTimer;
    //Prefabs
    public GameObject targetPrefab;
    public GameObject counterPrefab;
    public GameObject heldCounter;
    //Instance Vars
    public int beat = 0;
    public int held = 0;
    public static int totalNotes;
    public static Vector3 spawnPos = new Vector3(0.0f, 0.0f, 0.0f);
    //Half-size of the square notes spawn in
    private const float SpawnRange = 1.4f;
    //Minimum distance between one note and the next (centre to centre): one note width of clear space
    private const float MinSpacing = NoteVisual.Diameter * 2f;
    //Centre of the spawn square: straight ahead of the player's starting view
    private Vector2 spawnCentre;
    private bool hasLastSpawn;
    private Vector2 lastSpawn;

    //This song's chart, looked up once instead of every frame
    private float[] tapTimes;
    //Held notes are stored as start/end pairs
    private float[] heldTimes;
    //End of level: wait for the video to finish instead of cutting off after the last note
    private VideoPlayer vidPlayer;
    private bool videoEnded;
    private float allSpawnedAt = -1f;
    //Offset from level.json, subtracted again to get each note's chart time for the hit log
    private float chartOffset;

    void Start()
    {
        StartCoroutine(countdown());
        //Centre the spawn area on the camera, not the world origin (the camera sits at y = 1.29,
        //so centring on 0,0 used to put notes noticeably below the crosshair)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) spawnCentre = player.transform.position;
        vidPlayer = GameObject.FindGameObjectWithTag("VideoPlayer").GetComponent<VideoPlayer>();
        vidPlayer.loopPointReached += vp => videoEnded = true;

        tapTimes = LevelHolder.songTimes[SongSelect2.theSong];
        heldTimes = LevelHolder.heldTimes[SongSelect2.theSong];
        totalNotes = tapTimes.Length + heldTimes.Length / 2;
        if (LevelHolder.levels.TryGetValue(SongSelect2.theSong, out LevelInfo level)) chartOffset = level.offset;

        beat = 0;
        held = 0;
    }

    void Update()
    {
        float now = (float)Time.timeSinceLevelLoadAsDouble;

        if (beat < tapTimes.Length && now >= tapTimes[beat])
        {
            Counter counter = SpawnCounter();
            counter.isHeld = false;
            counter.chartTime = tapTimes[beat] - chartOffset;
            beat++;
        }
        if (held + 1 < heldTimes.Length && now >= heldTimes[held])
        {
            Counter counter = SpawnCounter();
            counter.isHeld = true;
            counter.timeTransfer = heldTimes[held + 1] - heldTimes[held];
            counter.chartTime = heldTimes[held] - chartOffset;
            held += 2;
        }

        if (beat >= tapTimes.Length && held + 1 >= heldTimes.Length)
        {
            if (allSpawnedAt < 0f) allSpawnedAt = Time.time;
            if (LevelFinished())
            {
                SceneManager.LoadScene("GameOver");
            }
        }
    }

    private Counter SpawnCounter()
    {
        Vector2 p = PickSpawnPoint();
        hasLastSpawn = true;
        lastSpawn = p;
        spawnPos = new Vector3(p.x, p.y, 0.0f);
        GameObject go = Instantiate(counterPrefab, spawnPos, Quaternion.AngleAxis(90, Vector3.right));
        return go.GetComponent<Counter>();
    }

    //Uniform over the spawn square, rejecting points too close to the previous note.
    //Rejection keeps the result uniform over everything that's allowed, so no side is favoured.
    private Vector2 PickSpawnPoint()
    {
        Vector2 best = spawnCentre;
        float bestDist = -1f;
        for (int attempt = 0; attempt < 30; attempt++)
        {
            Vector2 p = spawnCentre + new Vector2(Random.Range(-SpawnRange, SpawnRange), Random.Range(-SpawnRange, SpawnRange));
            if (!hasLastSpawn) return p;
            float d = Vector2.Distance(p, lastSpawn);
            if (d >= MinSpacing) return p;
            //Practically never needed (most of the square is valid), but keep the farthest just in case
            if (d > bestDist)
            {
                bestDist = d;
                best = p;
            }
        }
        return best;
    }

    //All notes are out; finish when the video does (with fallbacks so the level can't hang)
    bool LevelFinished()
    {
        if (videoEnded) return true;
        float sinceLastNote = Time.time - allSpawnedAt;
        //No video at all: just give the last notes time to play out
        bool hasVideo = vidPlayer.clip != null || !string.IsNullOrEmpty(vidPlayer.url);
        if (!hasVideo) return sinceLastNote >= 4f;
        //Video stopped on its own (e.g. end event missed) after the last notes resolved
        if (!Pause.isPaused && sinceLastNote >= 4f && vidPlayer.isPrepared && !vidPlayer.isPlaying) return true;
        //Hard cap: video length past the point the last note spawned
        return vidPlayer.length > 0 && sinceLastNote >= vidPlayer.length;
    }

    IEnumerator countdown()
    {
        startTimer.text = "3";
        yield return new WaitForSeconds(0.5f);
        startTimer.text = "2";
        yield return new WaitForSeconds(0.5f);
        startTimer.text = "1";
        yield return new WaitForSeconds(0.5f);
        startTimer.text = "Go!";
        yield return new WaitForSeconds(0.5f);
        startTimer.text = "";
    }
}
