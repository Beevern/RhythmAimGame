using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
public class Spawner : MonoBehaviour
{
    //Other GameObjects
    private Transform canvasTrans;
    private GameObject counterObject;
    private Counter counterScript; 
    public TextMeshProUGUI startTimer;
    //Prefabs
    public GameObject targetPrefab;
    public GameObject counterPrefab;
    public GameObject heldCounter;
    //Other Scripts
    //Instance Vars
    public float[] beatArr;
    public float[] heldArr;
    public int beat = 0;
    public int held = 0;
    public static int totalNotes;
    public static double totalTime;
    public static Vector3 spawnPos = new Vector3(0.0f, 0.0f, 0.0f);
    public float spawnCooldown = 0.5f;
    public float timeFromSpawn = 0f;
    public float bloom = 0;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("persistentDataPath = " + Application.persistentDataPath);
        canvasTrans = GameObject.FindGameObjectWithTag("CrosshairUI").GetComponent<Transform>();
        StartCoroutine(countdown());
        totalTime = GameObject.FindGameObjectWithTag("VideoPlayer").GetComponent<VideoPlayer>().length;
        totalNotes = LevelHolder.songTimes[SongSelect2.theSong].Length + LevelHolder.heldTimes[SongSelect2.theSong].Length;

        beat = 0;
        held = 0;
    }

    void Update()
    {
        //For some reason, bloom is not resetting (line 119)
        /* 
        if (beat < LevelHolder.songTimes[SongSelect2.theSong].Length-20)
        {
            for (int j = beat; j < beat + 20; j++)
            {
                if (LevelHolder.songTimes[SongSelect2.theSong][j] < (float)Time.timeSinceLevelLoadAsDouble + 3.0f)
                { bloom += 0.00025f; }
                else
                { break; }
                Debug.Log(bloom);
            }
        }
        */
        if (beat < LevelHolder.songTimes[SongSelect2.theSong].Length && (float)Time.timeSinceLevelLoadAsDouble >= LevelHolder.songTimes[SongSelect2.theSong][beat])
        {
            spawnPos = new Vector3(Random.Range(-1.4f - bloom, 1.4f + bloom), Random.Range(-1.4f - bloom, 1.4f - bloom), 0.0f);
            GameObject target = Instantiate(counterPrefab, spawnPos, Quaternion.AngleAxis(90, Vector3.right));
            target.GetComponent<Counter>().isHeld = false;
            beat++;
        }
        if (held < LevelHolder.heldTimes[SongSelect2.theSong].Length && (float)Time.timeSinceLevelLoadAsDouble >= LevelHolder.heldTimes[SongSelect2.theSong][held] && held%2==0)
        {
            spawnPos = new Vector3(Random.Range(-1.4f - bloom, 1.4f + bloom), Random.Range(-1.4f - bloom, 1.4f + bloom), 0.0f);
            GameObject heldCounter = Instantiate(counterPrefab, spawnPos, Quaternion.AngleAxis(90, Vector3.right));
            counterScript = heldCounter.GetComponent<Counter>();
            counterScript.isHeld = true;
            counterScript.timeTransfer = LevelHolder.heldTimes[SongSelect2.theSong][held + 1] - LevelHolder.heldTimes[SongSelect2.theSong][held];
            held += 2;
        }
        bloom = 0;
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