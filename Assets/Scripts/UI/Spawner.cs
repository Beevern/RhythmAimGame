using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
public class Spawner : MonoBehaviour
{
    //Other GameObjects
    private Transform canvasTrans;
    private GameObject counterObject;
    public TextMeshProUGUI startTimer;
    //Prefabs
    public GameObject targetPrefab;
    public GameObject counterPrefab;
    public GameObject heldPrefab;
    //Other Scripts
    //Instance Vars
    public static int totalNotes;
    public static float totalTime;
    public static Vector3 spawnPos = new Vector3(0.0f, 0.0f, 0.0f);
    public float spawnCooldown = 0.5f;
    public float timeFromSpawn = 0f;
    public float bloom = 0;
    public static float timeSinceLoad;
    public static Dictionary<string, float[]> songTimes = new Dictionary<string, float[]>();

    // Start is called before the first frame update
    void Start()
    {
        canvasTrans = GameObject.FindGameObjectWithTag("CrosshairUI").GetComponent<Transform>();
        //spawnPos = new Vector3(Random.Range(-1.4f - ScoreKeeper.bloom, 1.4f + ScoreKeeper.bloom), Random.Range(-1.4f - ScoreKeeper.bloom, 1.4f + ScoreKeeper.bloom), 0.0f);
        StartCoroutine(start());
        timeSinceLoad = 0;

    }
    void Update()
    {
        if (!Pause.isPaused)
        {
            timeSinceLoad += Time.deltaTime;
        }
        //Debug.Log(timeSinceLoad);
    }
    IEnumerator start()
    {
        yield return new WaitForSeconds(0.00f);
        StartCoroutine(countdown());
        songTimes.Clear();
        /*
        for (int i = 0;i<SongSelect2.allSongs.Length;i++)
        {
            songTimes.Add(SongSelect2.allSongs[i], SongSelect2.times[i]);
        }
        */
        songTimes.Add("Circles by EDEN", SongSelect2.times[0]);
        songTimes.Add("Seasons by Rival and Cadmium Piano Cover by Kita Sora", SongSelect2.times[1]);
        songTimes.Add("Seasons by Rival and Cadmium Futuristik and Whogaux Remix", SongSelect2.times[2]);
        songTimes.Add("Million Days by SABAI", SongSelect2.times[3]);
        songTimes.Add("Me Plus You by SABAI", SongSelect2.times[4]);
        songTimes.Add("Broken Glass by SABAI", SongSelect2.times[5]);
        songTimes.Add("Nevada by Vicetone", SongSelect2.times[6]);
        songTimes.Add("How to Stay Aligned by Cearul", SongSelect2.times[7]);
        songTimes.Add("XO by EDEN", SongSelect2.times[8]);
        songTimes.Add("Where We Started by Lost Sky and Jex", SongSelect2.times[9]);
        songTimes.Add("Dreams Pt II by Lost Sky and Sara Skinner", SongSelect2.times[10]);
        songTimes.Add("Love is Gone by SLANDER ft. Dylan Matthew (Acoustic)", SongSelect2.times[11]);
        songTimes.Add("The Phoenix by Fall Out Boy", SongSelect2.times[12]);
        StartCoroutine(spawnCounter(songTimes[SongSelect2.theSong]));
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
    IEnumerator spawnCounter(float[] spawnTimes)
    {
        totalTime = spawnTimes[spawnTimes.Length - 1];
        totalNotes = spawnTimes.Length;
        for (int i = 0; i < spawnTimes.Length; i++)
        {
            yield return new WaitUntil(() => (float)Time.timeSinceLevelLoadAsDouble >= spawnTimes[i]);
            //Debug.Log(spawnTimes[i]);

            for (int j = i; j < spawnTimes.Length; j++)
            {
                if (spawnTimes[j] < (float)timeSinceLoad + 3.0f)
                { bloom += 0.025f; }
                else
                { break; }
            }
            //Debug.Log(bloom);
            spawnPos = new Vector3(Random.Range(-1.4f - bloom, 1.4f + bloom), Random.Range(-1.4f - bloom, 1.4f + bloom), 0.0f);
            GameObject target = Instantiate(counterPrefab, spawnPos, Quaternion.AngleAxis(90, Vector3.right));
            bloom = 0;
        }
        yield return new WaitForSeconds(5);
        SceneManager.LoadScene("GameOver");
    }

    IEnumerator spawnHeld(float[] spawnTimes)
    {
        for (int i = 0; i < spawnTimes.Length; i += 2)
        {
            yield return new WaitUntil(() => (float)Time.timeSinceLevelLoadAsDouble >= spawnTimes[i]);
            //Debug.Log(spawnTimes[i]);

            for (int j = i; j < spawnTimes.Length; j++)
            {
                if (spawnTimes[j] < (float)Time.timeSinceLevelLoadAsDouble + 3.0f)
                { bloom += 0.025f; }
                else
                { break; }
            }
            //Debug.Log(bloom);
            spawnPos = new Vector3(Random.Range(-1.4f - bloom, 1.4f + bloom), Random.Range(-1.4f - bloom, 1.4f + bloom), 0.0f);
            GameObject heldTarget = Instantiate(heldPrefab, spawnPos, Quaternion.AngleAxis(90, Vector3.right));
            HeldTarget.destroyTimer = spawnTimes[i] - spawnTimes[i + 1];
            bloom = 0;
        }
    }
}
//enter ="'"&J2&"'"&"," in a random cell in excel after importing Edit Index to add single quotes (remember to CTRL+H to replace with double quotes) and a comma at the end
//https://replit.com/@Bevern/Convert-TimeFrames-to-Seconds#Main.java For converting frames in Davinci resolve to seconds in Unity :)

//When adding a new song: Import vid and coverimage, include in SongSelection scene, Game scene, playMusic script, songSelect2 script, and Spawner script.