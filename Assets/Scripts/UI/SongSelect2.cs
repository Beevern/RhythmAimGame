using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
public class SongSelect2 : MonoBehaviour
{
    //Buttons
    //public Button play;
    public Button right;
    public Button left;
    //Images
    public GameObject image;
    public GameObject leftImage;
    public GameObject rightImage;
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
    public String[] songNames;
    // Start is called before the first frame update
    void Awake()
    {
        songNames = (string[])LevelHolder.allSongs.Clone();
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
        searchBar.onValueChanged.AddListener(delegate { search(); });
    }

    // Update is called once per frame
    void Update()
    {
        
        //Debug.Log(theSong);
        if (Input.GetKeyDown(KeyCode.RightArrow))
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
        if (Input.GetKeyDown(KeyCode.LeftArrow))
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
        if (Input.GetKeyDown(KeyCode.Return))
        {
            SceneManager.LoadScene("Game");
        }
        if (songNames.Length > current && songNames.Length > 0)
        {
            image.GetComponent<Image>().sprite = LevelHolder.songImages[songNames[current]];
            
            if (current-1 <0)
            {
                leftImage.GetComponent<Image>().sprite = LevelHolder.songImages[songNames[songNames.Length-1]];
            }
            else
            {
                leftImage.GetComponent<Image>().sprite = LevelHolder.songImages[songNames[current - 1]];
            }
            if (current + 1 == songNames.Length)
            {
                rightImage.GetComponent<Image>().sprite = LevelHolder.songImages[songNames[0]];
            }
            else
            {
                rightImage.GetComponent<Image>().sprite = LevelHolder.songImages[songNames[current + 1]];
            }
            
            songName.text = songNames[current];
            theSong = songNames[current];
        }
    }
    void search()
    {
        if (searchBar.text.Equals("") || searchBar.text == null)
        {
            songNames = (string[])LevelHolder.allSongs.Clone();
        }
        List<string> results = new List<string>();
        for (int i = 0; i < LevelHolder.allSongs.Length; i++)
        {
            if (LevelHolder.allSongs[i].ToLower().IndexOf(searchBar.text.ToLower()) != -1)
            {
                results.Add(LevelHolder.allSongs[i]);
            }
        }
        songNames = new string[results.Count];
        for (int i = 0; i < results.Count; i++)
        {
            songNames[i] = results[i];
            //Debug.Log(songNames[i]);
        }
        current = 0;

    }
    void sort(TMP_Dropdown drop)
    {
        //Song Alphabetically
        if (drop.value == 0)
        {
            Array.Sort(songNames);
            current = 0;
            //Debug.Log(songNames);
        }
        //Artist Alphabetically
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
        //Length Ascending
        else if (drop.value == 2)
        {
            int[] lengths = new int[LevelHolder.times.Length];
            for (int i = 0; i < LevelHolder.times.Length; i++)
            {
                lengths[i] = LevelHolder.times[i].Length;// + LevelHolder.heldTimesArray[i].Length;
            }
            Array.Sort(lengths);
            //Debug.Log(lengths);
            for (int i = 0; i < LevelHolder.allSongs.Length; i++)
            {
                //Debug.Log(times.Length);
                foreach (string key in LevelHolder.songLengths.Keys)
                {
                    //Debug.Log(LevelHolder.songLengths[key]);
                    //Debug.Log(times[i]);
                    if (LevelHolder.songLengths[key] == lengths[i])
                    {
                        songNames[i] = key;
                        //Debug.Log(songNames[i]);
                    }
                }
            }
            current = 0;
        }
    }
    void goRight()
    {
        if (songNames.Length > 0)
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
    }

    void goLeft()
    {
        if (songNames.Length > 0)
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

}
