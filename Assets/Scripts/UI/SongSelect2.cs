using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class SongSelect2 : MonoBehaviour
{
    //Buttons
    //public Button play;
    public Button right;
    public Button left;
    //Images
    public GameObject image;
    public Sprite RandomI;
    public Sprite circlesI;
    public Sprite seasonsKitaI;
    public Sprite seasonsI;
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
    //Text
    public TextMeshProUGUI songName;
    // Other
    public static int current = 0;
    private string[] songNames = { "Random", "Circles by EDEN", "Seasons by Rival x Cadmium Piano Cover by Kita Sora", "Seasons by Rival x Cadmium Futuristik & Whogaux Remix", 
        "Million Days by SABAI", "Me Plus You by SABAI", "Broken Glass by SABAI", "Nevada by Vicetone", "How To Stay Aligned a EVE Online Parody by Cearul", "XO by EDEN", 
        "Where We Started by Lost Sky x Jex", "Dreams Pt II by Lost Sky X Sara Skinner", "Love is Gone by SLANDER ft. Dylan Matthew (Acoustic)", "The Phoenix by Fall Out Boys"};
    // Start is called before the first frame update
    void Start()
    {
        current = 0;
        right.GetComponent<Button>().onClick.AddListener(goRight);
        left.GetComponent<Button>().onClick.AddListener(goLeft);
    }

    // Update is called once per frame
    void Update()
    {
        Sprite[] images = { RandomI, circlesI, seasonsKitaI, seasonsI, MillionDaysI, MeYouI, BrokenGlassI, NevadaI, StayAlignedI, XOI, WhereWeStartedI, DreamsPt2I, LoveIsGoneI, ThePhoenixI};
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
        image.GetComponent<Image>().sprite = images[current];
        songName.text = songNames[current];
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
