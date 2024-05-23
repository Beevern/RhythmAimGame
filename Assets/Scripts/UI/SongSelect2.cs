using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
public class SongSelect2 : MonoBehaviour
{
    //Buttons
    //public Button play;
    public Button right;
    public Button left;
    //Images
    public GameObject image;
    public Sprite RandomI;
    public Sprite CirclesI;
    public Sprite SeasonsKitaI;
    public Sprite SeasonsI;
    public Sprite MillionDaysI;
    public Sprite MeYouI;
    public Sprite BrokenGlassI;
    public Sprite NevadaI;
    public Sprite StayAlignedI;
    public Sprite XOI;
    public Sprite WhereWeStartedI;
    public Sprite DreamsPt2I;
    public Sprite LoveIsGoneI;
    public Sprite ThePhoenixI;
    //Other UI
    public TextMeshProUGUI songName;
    public TMP_Dropdown sortDrop;
    public TMP_InputField searchBar;
    //GameObjects
    public GameObject dropDownObj;
    public GameObject searchObj;
    // Other
    public static int current = 0;
    public static string theSong;
    public static string[] songNames = {"Circles by EDEN", "Seasons by Rival and Cadmium Piano Cover by Kita Sora", "Seasons by Rival and Cadmium Futuristik and Whogaux Remix", 
        "Million Days by SABAI", "Me Plus You by SABAI", "Broken Glass by SABAI", "Nevada by Vicetone", "How to Stay Aligned by Cearul", "XO by EDEN",
        "Where We Started by Lost Sky and Jex", "Dreams Pt II by Lost Sky and Sara Skinner", "Love is Gone by SLANDER ft. Dylan Matthew (Acoustic)", "The Phoenix by Fall Out Boys"};
    public int[] lengths = { Spawner.times[0].Length, Spawner.times[1].Length, Spawner.times[2].Length, Spawner.times[3].Length, Spawner.times[4].Length, Spawner.times[5].Length, Spawner.times[6].Length, 
    Spawner.times[7].Length, Spawner.times[8].Length, Spawner.times[9].Length, Spawner.times[10].Length, Spawner.times[11].Length, Spawner.times[12].Length };
    public static Dictionary<string, Sprite> songImages = new Dictionary<string, Sprite>();
    public Dictionary<string, int> songLengths = new Dictionary<string, int>();
    // Start is called before the first frame update
    void Awake()
    {
        //Initializing Dictionaries
        /*
        songImages.Clear();
        Sprite[] images = { CirclesI, SeasonsKitaI, SeasonsI, MillionDaysI, MeYouI, BrokenGlassI, NevadaI, StayAlignedI, XOI, WhereWeStartedI, DreamsPt2I, LoveIsGoneI, ThePhoenixI};
        for (int i = 0; i < SongSelect2.songNames.Length; i++)
        {
            songImages.Add(SongSelect2.songNames[i], images[i]);
            //songLengths.Add(songNames[i], lengths[i]);
        }
        */
    }

    void Start()
    {
        current = 0;
        right.GetComponent<Button>().onClick.AddListener(goRight);
        left.GetComponent<Button>().onClick.AddListener(goLeft);
        sortDrop = dropDownObj.GetComponent<TMP_Dropdown>();
        searchBar = searchObj.GetComponent<TMP_InputField>();
        sort(sortDrop);
        sortDrop.onValueChanged.AddListener(delegate { sort(sortDrop); });
        //searchBar.onValueChanged.AddListener(delegate {  search(); });
        //Initializing Dictionaries

        songImages.Add("Broken Glass by SABAI", BrokenGlassI);
        songImages.Add("Circles by EDEN", CirclesI);
        songImages.Add("Dreams Pt II by Lost Sky and Sara Skinner", DreamsPt2I);
        songImages.Add("How to Stay Aligned by Cearul", StayAlignedI);
        songImages.Add("Love is Gone by SLANDER ft. Dylan Matthew (Acoustic)", LoveIsGoneI);
        songImages.Add("Me Plus You by SABAI", MeYouI);
        songImages.Add("Million Days by SABAI", MillionDaysI);
        songImages.Add("Nevada by Vicetone", NevadaI);
        songImages.Add("Seasons by Rival and Cadmium Futuristik and Whogaux Remix", SeasonsI);
        songImages.Add("Seasons by Rival and Cadmium Piano Cover by Kita Sora", SeasonsKitaI);
        songImages.Add("The Phoenix by Fall Out Boys", ThePhoenixI);
        songImages.Add("Where We Started by Lost Sky and Jex", WhereWeStartedI);
        songImages.Add("XO by EDEN", XOI);
        /*
        songLengths.Add("Broken Glass by SABAI", Spawner.times.Length);
        songLengths.Add("Circles by EDEN", Spawner.times.Length);
        songLengths.Add("Dreams Pt II by Lost Sky and Sara Skinner", Spawner.times.Length);
        songLengths.Add("How to Stay Aligned by Cearul", Spawner.times.Length);
        songLengths.Add("Love is Gone by SLANDER ft. Dylan Matthew (Acoustic)", Spawner.times.Length);
        songLengths.Add("Me Plus You by SABAI", Spawner.times.Length);
        songLengths.Add("Million Days by SABAI", Spawner.times.Length);
        songLengths.Add("Nevada by Vicetone", Spawner.times.Length);
        songLengths.Add("Seasons by Rival and Cadmium Futuristik and Whogaux Remix", Spawner.times.Length);
        songLengths.Add("Seasons by Rival and Cadmium Piano Cover by Kita Sora", Spawner.times.Length);
        songLengths.Add("The Phoenix by Fall Out Boys", Spawner.times.Length);
        songLengths.Add("Where We Started by Lost Sky and Jex", Spawner.times.Length);
        songLengths.Add("XO by EDEN", Spawner.times.Length);
        */
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            if (current >= songNames.Length-1)
            {
                current = 0;
            }
            else
            {
                current++;
            }
            
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            if (current <= 0)
            {
                current = songNames.Length-1;
            }
            else
            {
                current--;
            }
            
        }
        image.GetComponent<Image>().sprite = songImages[songNames[current]];
        songName.text = songNames[current];
        theSong = songNames[current];
    }
    void search()
    {
        List<string> results = new List<string>();
        for (int i = 0;i<songNames.Length;i++)
        {
            if (songNames[i].IndexOf(searchBar.text)==-1)
            {
                results.Add(songNames[i]);
            }
        }
        songNames = new string[results.Count];
        for (int i = 0;i<results.Count;i++)
        {
            songNames[i] = results[i];
            Debug.Log(songNames[i]);
        }
        
    }
    void sort(TMP_Dropdown drop)
    {
        if (drop.value == 0)
        {
            Array.Sort(songNames);
            current = 0;
            //Debug.Log(songNames);
        }
        else if (drop.value == 1)
        {
            Array.Sort(songNames);
            string[] artname = new string[songNames.Length];
            string[] artistSong = new string[songNames.Length];
            for (int i = 0; i < songNames.Length; i++)
            {
                artname[i] = songNames[i].Substring(songNames[i].IndexOf(" by ") + 4);
                artistSong[i] = songNames[i].Substring(songNames[i].IndexOf(" by ") + 4) + songNames[i].Substring(0, songNames[i].IndexOf(" by ") + 4);
                //Debug.Log(artistSong[i]);
                //Debug.Log(artname[i]);
            }
            string[] list = new string[songNames.Length];

            Array.Sort(artistSong);
            Array.Sort(artname);

            for (int i = 0; i < songNames.Length; i++)
            {
                //Eg. Circles by EDEN -> EDENCircles by 
                songNames[i] = artistSong[i].Substring(artname[i].Length) + artname[i];
                //Debug.Log(songNames[i]);
            }
            current = 0;
        }
        else if (drop.value == 2)
        {
            Array.Sort(lengths);
            //Debug.Log(lengths);
            for (int i = 0;i<songNames.Length;i++)
            {
                Debug.Log(lengths.Length);
                foreach (string key in songLengths.Keys)
                {
                    Debug.Log(songLengths[key]);
                    Debug.Log(lengths[i]);
                    if (songLengths[key] == lengths[i])
                    {
                        songNames[i] = key;
                        Debug.Log(songNames[i]);
                    }
                }
            }
            current = 0;
        }
    }
    void goRight()
    {
        if (current >= songNames.Length - 1)
        {
            current = 0;
        }
        else
        {
            current++;
        }
    }

    void goLeft()
    {
        if (current <= 0)
        {
            current = songNames.Length - 1;
        }
        else
        {
            current--;
        }
    }

}
