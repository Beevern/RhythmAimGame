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
    public VideoClip GoonswarmVid;
    public VideoClip DollarSadnessVid;
    public VideoClip NeverKnowVid;
    public VideoClip DeadOrAliveVid;
    public VideoClip HypnotizedVid;
    public VideoClip SayGoodbyeVid;
    public VideoClip HateYouVid;
    public VideoClip LowlifeVid;
    public VideoClip SadSongVid;
    public VideoClip EndlesslyVid;
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
    public RenderTexture GoonswarmTexture;
    public RenderTexture DollarSadnessTexture;
    public RenderTexture NeverKnowTexture;
    public RenderTexture DeadOrAliveTexture;
    public RenderTexture HypnotizedTexture;
    public RenderTexture SayGoodbyeTexture;
    public RenderTexture HateYouTexture;
    public RenderTexture LowlifeTexture;
    public RenderTexture SadSongTexture;
    public RenderTexture EndlesslyTexture;
    //Other Objects
    public GameObject VideoPlayerParent;
    //Other Scripts
    private ScoreKeeper keeperScript;
    //Instance Vars
    public static double volume;
    // Start is called before the first frame update
    void Awake()
    {
        StartCoroutine(playVid());
    }
    IEnumerator playVid()
    {
        yield return new WaitForSeconds(0.00f);
        //Initiallizing dictionaries

        VideoClip[] videos = { CirclesVid, SeasonsKitaVid, SeasonsVid, MillionDaysVid, MeYouVid, BrokenGlassVid, NevadaVid, 
        StayAlignedVid, XOVid, WhereWeStartedVid, DreamsPt2Vid, LoveIsGoneVid, ThePhoenixVid, GoonswarmVid, 
        DollarSadnessVid, NeverKnowVid, DeadOrAliveVid, HypnotizedVid, SayGoodbyeVid, HateYouVid, LowlifeVid, SadSongVid,
        EndlesslyVid};

        RenderTexture[] textures = {CirclesTexture, SeasonsKitaTexture, SeasonsTexture, MillionDaysTexture, MeYouTexture, 
        BrokenGlassTexture, NevadaTexture, StayAlignedTexture, XOTexture, WhereWeStartedTexture, DreamsPt2Texture, 
        LoveIsGoneTexture, ThePhoenixTexture, GoonswarmTexture, DollarSadnessTexture, NeverKnowTexture, DeadOrAliveTexture,
        HypnotizedTexture, SayGoodbyeTexture, HateYouTexture, LowlifeTexture, SadSongTexture, EndlesslyTexture};

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

        songVid.Add("Circles by EDEN", CirclesVid);
        songVid.Add("Seasons by Rival and Cadmium Piano Cover by Kita Sora", SeasonsKitaVid);
        songVid.Add("Seasons by Rival and Cadmium Futuristik and Whogaux Remix", SeasonsVid);
        songVid.Add("Million Days by SABAI", MillionDaysVid);
        songVid.Add("Me Plus You by SABAI", MeYouVid);
        songVid.Add("Broken Glass by SABAI", BrokenGlassVid);
        songVid.Add("Nevada by Vicetone", NevadaVid);
        songVid.Add("How to Stay Aligned by Cearul", StayAlignedVid);
        songVid.Add("XO by EDEN", XOVid);
        songVid.Add("Where We Started by Lost Sky and Jex", WhereWeStartedVid);
        songVid.Add("Dreams Pt II by Lost Sky and Sara Skinner", DreamsPt2Vid);
        songVid.Add("Love is Gone by SLANDER ft. Dylan Matthew (Acoustic)", LoveIsGoneVid);
        songVid.Add("The Phoenix by Fall Out Boy", ThePhoenixVid);
        songVid.Add("We're Only Goonswarm by Srsli", GoonswarmVid);
        songVid.Add("Dollar For Your Sadness by Unlike Pluto", DollarSadnessVid);
        songVid.Add("Never Know by Swif7", NeverKnowVid);
        songVid.Add("Dead Or Alive by Stileto", DeadOrAliveVid);
        songVid.Add("Hypnotized by DEAMN", HypnotizedVid);
        songVid.Add("Say Goodbye by Krewella", SayGoodbyeVid);
        songVid.Add("Hate You by Jim Yosef X Riell", HateYouVid);
        songVid.Add("Lowlife by Neck Deep", LowlifeVid);
        songVid.Add("Sad Song by We The Kings", SadSongVid);
        songVid.Add("Endlessly by The Cab", EndlesslyVid);

        songText.Add("Circles by EDEN", CirclesTexture);
        songText.Add("Seasons by Rival and Cadmium Piano Cover by Kita Sora", SeasonsKitaTexture);
        songText.Add("Seasons by Rival and Cadmium Futuristik and Whogaux Remix", SeasonsTexture);
        songText.Add("Million Days by SABAI", MillionDaysTexture);
        songText.Add("Me Plus You by SABAI", MeYouTexture);
        songText.Add("Broken Glass by SABAI", BrokenGlassTexture);
        songText.Add("Nevada by Vicetone", NevadaTexture);
        songText.Add("How to Stay Aligned by Cearul", StayAlignedTexture);
        songText.Add("XO by EDEN", XOTexture);
        songText.Add("Where We Started by Lost Sky and Jex", WhereWeStartedTexture);
        songText.Add("Dreams Pt II by Lost Sky and Sara Skinner", DreamsPt2Texture);
        songText.Add("Love is Gone by SLANDER ft. Dylan Matthew (Acoustic)", LoveIsGoneTexture);
        songText.Add("The Phoenix by Fall Out Boy", ThePhoenixTexture);
        songText.Add("We're Only Goonswarm by Srsli", GoonswarmTexture);
        songText.Add("Dollar For Your Sadness by Unlike Pluto", DollarSadnessTexture);
        songText.Add("Never Know by Swif7", NeverKnowTexture);
        songText.Add("Dead Or Alive by Stileto", DeadOrAliveTexture);
        songText.Add("Hypnotized by DEAMN", HypnotizedTexture);
        songText.Add("Say Goodbye by Krewella", SayGoodbyeTexture);
        songText.Add("Hate You by Jim Yosef X Riell", HateYouTexture);
        songText.Add("Lowlife by Neck Deep", LowlifeTexture);
        songText.Add("Sad Song by We The Kings", SadSongTexture);
        songText.Add("Endlessly by The Cab", EndlesslyTexture);

        vidPlayer = GameObject.FindGameObjectWithTag("VideoPlayer").GetComponent<VideoPlayer>();
        /*
        RANDOM
        if (SongSelect2.current == 0)
        {
            SongSelect2.current = (int)Random.Range(1, textures.Length);
        }
        */
        if (SongSelect2.theSong.Equals("Seasons by Rival and Cadmium Futuristik and Whogaux Remix"))
        {
            yield return new WaitForSeconds(0.35f);
        }
        yield return new WaitForSeconds(3f);
        vidPlayer.clip = songVid[SongSelect2.theSong];
        vidPlayer.targetTexture = songText[SongSelect2.theSong];
        VideoPlayerParent.GetComponent<RawImage>().texture = songText[SongSelect2.theSong];
        yield return new WaitForSeconds((float)vidPlayer.clip.length);
        SceneManager.LoadScene("GameOver");
    }
}