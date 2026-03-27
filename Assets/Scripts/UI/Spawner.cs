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
    public static Dictionary<string, float[]> songTimes = new Dictionary<string, float[]>();
    public static Dictionary<string, float[]> heldTimes = new Dictionary<string, float[]>();


    // Start is called before the first frame update
    void Start()
    {
        canvasTrans = GameObject.FindGameObjectWithTag("CrosshairUI").GetComponent<Transform>();
        StartCoroutine(countdown());
        
        
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
        songTimes.Add("We're Only Goonswarm by Srsli", SongSelect2.times[13]);
        songTimes.Add("Dollar For Your Sadness by Unlike Pluto", SongSelect2.times[14]);
        songTimes.Add("Never Know by Swif7", SongSelect2.times[15]);
        songTimes.Add("Dead Or Alive by Stileto", SongSelect2.times[16]);
        songTimes.Add("Hypnotized by DEAMN", SongSelect2.times[17]);
        songTimes.Add("Say Goodbye by Krewella", SongSelect2.times[18]);
        songTimes.Add("Hate You by Jim Yosef X Riell", SongSelect2.times[19]);
        songTimes.Add("Lowlife by Neck Deep", SongSelect2.times[20]);
        songTimes.Add("Sad Song by We The Kings", SongSelect2.times[21]);
        songTimes.Add("Endlessly by The Cab", SongSelect2.times[22]);

        heldTimes.Add("Love is Gone by SLANDER ft. Dylan Matthew (Acoustic)", SongSelect2.heldTimes[11]);
        heldTimes.Add("We're Only Goonswarm by Srsli", SongSelect2.heldTimes[13]);
        heldTimes.Add("Lowlife by Neck Deep", SongSelect2.heldTimes[20]);
        heldTimes.Add("Sad Song by We The Kings", SongSelect2.heldTimes[21]);
        heldTimes.Add("Endlessly by The Cab", SongSelect2.heldTimes[22]);
        
        totalTime = GameObject.FindGameObjectWithTag("VideoPlayer").GetComponent<VideoPlayer>().length;
        totalNotes = songTimes[SongSelect2.theSong].Length + heldTimes[SongSelect2.theSong].Length;

        beat = 0;
        held = 0;
    }

    void Update()
    {
        //For some reason, bloom is not resetting (line 119)
        /* 
        if (beat < songTimes[SongSelect2.theSong].Length-20)
        {
            for (int j = beat; j < beat + 20; j++)
            {
                if (songTimes[SongSelect2.theSong][j] < (float)Time.timeSinceLevelLoadAsDouble + 3.0f)
                { bloom += 0.00025f; }
                else
                { break; }
                Debug.Log(bloom);
            }
        }
        */
        if (beat < songTimes[SongSelect2.theSong].Length && (float)Time.timeSinceLevelLoadAsDouble >= songTimes[SongSelect2.theSong][beat])
        {
            spawnPos = new Vector3(Random.Range(-1.4f - bloom, 1.4f + bloom), Random.Range(-1.4f - bloom, 1.4f - bloom), 0.0f);
            GameObject target = Instantiate(counterPrefab, spawnPos, Quaternion.AngleAxis(90, Vector3.right));
            target.GetComponent<Counter>().isHeld = false;
            beat++;
        }
        if (held < heldTimes[SongSelect2.theSong].Length && (float)Time.timeSinceLevelLoadAsDouble >= heldTimes[SongSelect2.theSong][held] && held%2==0)
        {
            spawnPos = new Vector3(Random.Range(-1.4f - bloom, 1.4f + bloom), Random.Range(-1.4f - bloom, 1.4f + bloom), 0.0f);
            GameObject heldCounter = Instantiate(counterPrefab, spawnPos, Quaternion.AngleAxis(90, Vector3.right));
            counterScript = heldCounter.GetComponent<Counter>();
            counterScript.isHeld = true;
            counterScript.timeTransfer = heldTimes[SongSelect2.theSong][held + 1] - heldTimes[SongSelect2.theSong][held];
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
//enter ="""" & J2 & """," in a random cell in excel after importing Edit Index to add single quotes (remember to CTRL+H to replace with double quotes) and a comma at the end
//https://replit.com/@Bevern/Convert-TimeFrames-to-Seconds#Main.java For converting frames in Davinci resolve to seconds in Unity :)

//When adding a new song: Import vid and coverimage, include in SongSelection scene, Game scene, playMusic script, songSelect2 script, and Spawner script.