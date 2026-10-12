using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//Song select carousel: left/right through the (searched, sorted) song list, Enter to play
public class SongSelect2 : MonoBehaviour
{
    //Buttons
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

    //Covers/title are only reassigned when the selection or list changes
    private bool dirty = true;
    private Image centreImg, leftImg, rightImg;

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
        //The scene's dropdown has Song / Artist / Length; add Difficulty as the 4th option
        if (sortDrop.options.Count < 4)
        {
            sortDrop.options.Add(new TMP_Dropdown.OptionData("Difficulty"));
            sortDrop.RefreshShownValue();
        }
        searchBar = searchObj.GetComponent<TMP_InputField>();
        centreImg = image.GetComponent<Image>();
        leftImg = leftImage.GetComponent<Image>();
        rightImg = rightImage.GetComponent<Image>();
        sort(sortDrop);
        sortDrop.onValueChanged.AddListener(delegate { sort(sortDrop); });
        searchBar.onValueChanged.AddListener(delegate { search(); });
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow)) goRight();
        if (Input.GetKeyDown(KeyCode.LeftArrow)) goLeft();
        if (Input.GetKeyDown(KeyCode.Return) && songNames.Length > 0)
        {
            SceneManager.LoadScene("Game");
        }
        if (dirty) Refresh();
    }

    void Refresh()
    {
        dirty = false;
        int n = songNames.Length;
        if (n == 0) return;
        current = Mathf.Clamp(current, 0, n - 1);
        centreImg.sprite = Cover(songNames[current]);
        leftImg.sprite = Cover(songNames[(current - 1 + n) % n]);
        rightImg.sprite = Cover(songNames[(current + 1) % n]);
        songName.text = songNames[current];
        theSong = songNames[current];
    }

    static Sprite Cover(string song)
    {
        return LevelHolder.songImages.TryGetValue(song, out Sprite s) ? s : null;
    }

    //Filters all songs by the search text, keeping the chosen sort order
    void search()
    {
        string query = searchBar.text ?? "";
        songNames = LevelHolder.allSongs
            .Where(s => s.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        sort(sortDrop);
    }

    void sort(TMP_Dropdown drop)
    {
        switch (drop.value)
        {
            //Song title alphabetically (just the title part, ignoring the artist)
            case 0:
                songNames = songNames.OrderBy(LevelNames.Title, StringComparer.OrdinalIgnoreCase).ThenBy(s => s).ToArray();
                break;
            //Artist alphabetically, then song
            case 1:
                songNames = songNames.OrderBy(Artist).ThenBy(s => s).ToArray();
                break;
            //Length (note count) ascending
            case 2:
                songNames = songNames
                    .OrderBy(s => LevelHolder.songLengths.TryGetValue(s, out int len) ? len : 0)
                    .ThenBy(s => s)
                    .ToArray();
                break;
            //Difficulty (star rating) easiest first
            case 3:
                songNames = songNames
                    .OrderBy(s => LevelHolder.levels.TryGetValue(s, out LevelInfo info) ? info.difficulty : 0f)
                    .ThenBy(s => s)
                    .ToArray();
                break;
        }
        current = 0;
        dirty = true;
    }

    //"EDEN - Circles" -> "EDEN" (older "Circles by EDEN" names work too)
    static string Artist(string song) => LevelNames.Artist(song);

    void goRight()
    {
        if (songNames.Length == 0) return;
        current = (current + 1) % songNames.Length;
        dirty = true;
    }

    void goLeft()
    {
        if (songNames.Length == 0) return;
        current = (current - 1 + songNames.Length) % songNames.Length;
        dirty = true;
    }
}
