using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.UI;
public class playMusic : MonoBehaviour
{
    //Video
    public VideoClip RandomVid;
    public VideoClip CirclesVid;
    public VideoClip SeasonsKitaVid;
    public VideoClip SeasonsVid;
    public VideoClip MillionDaysVid;
    public VideoClip MeYouVid;
    public VideoClip BrokenGlassVid;
    public VideoClip NevadaVid;
    public VideoClip StayAlignedVid;
    public VideoClip XOVid;
    public VideoClip WhereWeStartedVid;
    public VideoClip DreamsPt2Vid;
    public VideoClip LoveIsGoneVid;
    public VideoClip ThePhoenixVid;
    //Players
    private AudioSource audioSource;
    private VideoPlayer vidPlayer;
    //Textures
    public RenderTexture RandomTexture;
    public RenderTexture CirclesTexture;
    public RenderTexture SeasonsKitaTexture;
    public RenderTexture SeasonsTexture;
    public RenderTexture MillionDaysTexture;
    public RenderTexture MeYouTexture;
    public RenderTexture BrokenGlassTexture;
    public RenderTexture NevadaTexture;
    public RenderTexture StayAlignedTexture;
    public RenderTexture XOTexture;
    public RenderTexture WhereWeStartedTexture;
    public RenderTexture DreamsPt2Texture;
    public RenderTexture LoveIsGoneTexture;
    public RenderTexture ThePhoenixTexture;
    //Other Objects
    public GameObject VideoPlayerParent;
    //Other Scripts
    private ScoreKeeper keeperScript;
    //Instance Vars

    // Start is called before the first frame update
    void Awake()
    {
        StartCoroutine(playVid());
    }
    IEnumerator playVid()
    {
        yield return new WaitForSeconds(0.00f);
        //Initiallizing dictionaries
        //For some reason, taking songNames from SongSelect2 doesn't work, but does in Spawner script
        string[] songNames = {"Circles by EDEN", "Seasons by Rival and Cadmium Piano Cover by Kita Sora", "Seasons by Rival and Cadmium Futuristik and Whogaux Remix",
        "Million Days by SABAI", "Me Plus You by SABAI", "Broken Glass by SABAI", "Nevada by Vicetone", "How to Stay Aligned by Cearul", "XO by EDEN",
        "Where We Started by Lost Sky and Jex", "Dreams Pt II by Lost Sky and Sara Skinner", "Love is Gone by SLANDER ft. Dylan Matthew (Acoustic)", "The Phoenix by Fall Out Boys"};
    VideoClip[] videos = {CirclesVid, SeasonsKitaVid, SeasonsVid, MillionDaysVid, MeYouVid, BrokenGlassVid, NevadaVid, StayAlignedVid, XOVid, WhereWeStartedVid, DreamsPt2Vid, LoveIsGoneVid, ThePhoenixVid };

        RenderTexture[] textures = {CirclesTexture, SeasonsKitaTexture, SeasonsTexture, MillionDaysTexture, MeYouTexture, BrokenGlassTexture, NevadaTexture, StayAlignedTexture,
        XOTexture, WhereWeStartedTexture, DreamsPt2Texture, LoveIsGoneTexture, ThePhoenixTexture};

        Dictionary<string, VideoClip> songVid = new Dictionary<string, VideoClip>();
        Dictionary<string, RenderTexture> songText = new Dictionary<string, RenderTexture>();
        songText.Clear();
        songVid.Clear();
        /*
        for (int i = 0;i<SongSelect2.songNames.Length;i++)
        {
            songText.Add(SongSelect2.songNames[i], textures[i]);
            songVid.Add(SongSelect2.songNames[i], videos[i]);
        }
        */
        
        songVid.Add("Broken Glass by SABAI", BrokenGlassVid);
        songVid.Add("Circles by EDEN", CirclesVid);
        songVid.Add("Dreams Pt II by Lost Sky and Sara Skinner", DreamsPt2Vid);
        songVid.Add("How to Stay Aligned by Cearul", StayAlignedVid);
        songVid.Add("Love is Gone by SLANDER ft. Dylan Matthew (Acoustic)", LoveIsGoneVid);
        songVid.Add("Me Plus You by SABAI", MeYouVid);
        songVid.Add("Million Days by SABAI", MillionDaysVid);
        songVid.Add("Nevada by Vicetone", NevadaVid);
        songVid.Add("Seasons by Rival and Cadmium Futuristik and Whogaux Remix", SeasonsVid);
        songVid.Add("Seasons by Rival and Cadmium Piano Cover by Kita Sora", SeasonsKitaVid);
        songVid.Add("The Phoenix by Fall Out Boys", ThePhoenixVid);
        songVid.Add("Where We Started by Lost Sky and Jex", WhereWeStartedVid);
        songVid.Add("XO by EDEN", XOVid);
        songText.Add("Broken Glass by SABAI", BrokenGlassTexture);
        songText.Add("Circles by EDEN", CirclesTexture);
        songText.Add("Dreams Pt II by Lost Sky and Sara Skinner", DreamsPt2Texture);
        songText.Add("How to Stay Aligned by Cearul", StayAlignedTexture);
        songText.Add("Love is Gone by SLANDER ft. Dylan Matthew (Acoustic)", LoveIsGoneTexture);
        songText.Add("Me Plus You by SABAI", MeYouTexture);
        songText.Add("Million Days by SABAI", MillionDaysTexture);
        songText.Add("Nevada by Vicetone", NevadaTexture);
        songText.Add("Seasons by Rival and Cadmium Futuristik and Whogaux Remix", SeasonsTexture);
        songText.Add("Seasons by Rival and Cadmium Piano Cover by Kita Sora", SeasonsKitaTexture);
        songText.Add("The Phoenix by Fall Out Boys", ThePhoenixTexture);
        songText.Add("Where We Started by Lost Sky and Jex", WhereWeStartedTexture);
        songText.Add("XO by EDEN", XOTexture);
        
        vidPlayer = GameObject.FindGameObjectWithTag("VideoPlayer").GetComponent<VideoPlayer>();
        /*
         * 
        RANDOM
        if (SongSelect2.current == 0)
        {
            SongSelect2.current = (int)Random.Range(1, textures.Length);
        }
        */
        /*
        if (SongSelect2.current == 2)
        {
            vidPlayer.clip = videos[SongSelect2.current];
            vidPlayer.targetTexture = textures[SongSelect2.current];
            player.GetComponent<RawImage>().texture = textures[SongSelect2.current];
        }
        else if (SongSelect2.current == 3)
        {
            yield return new WaitForSeconds(3);
        }
        */
        if (SongSelect2.theSong.Equals("Seasons by Rival and Cadmium Futuristik and Whogaux Remix"))
        {
            yield return new WaitForSeconds(0.35f);
        }
        yield return new WaitForSeconds(3);
        vidPlayer.clip = songVid[SongSelect2.theSong];
        vidPlayer.targetTexture = songText[SongSelect2.theSong];
        VideoPlayerParent.GetComponent<RawImage>().texture = songText[SongSelect2.theSong];

    }
}
