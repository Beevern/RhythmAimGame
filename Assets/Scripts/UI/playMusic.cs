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

        VideoClip[] videos = { RandomVid, CirclesVid, SeasonsKitaVid, SeasonsVid, MillionDaysVid, MeYouVid, BrokenGlassVid, NevadaVid, StayAlignedVid, XOVid, WhereWeStartedVid, DreamsPt2Vid, LoveIsGoneVid, ThePhoenixVid};

        RenderTexture[] textures = { RandomTexture, CirclesTexture, SeasonsKitaTexture, SeasonsTexture, MillionDaysTexture, MeYouTexture, BrokenGlassTexture, NevadaTexture, StayAlignedTexture, 
            XOTexture, WhereWeStartedTexture, DreamsPt2Texture, LoveIsGoneTexture, ThePhoenixTexture};
        vidPlayer = GameObject.FindGameObjectWithTag("VideoPlayer").GetComponent<VideoPlayer>();
        if (SongSelect2.current == 0)
        {
            SongSelect2.current = (int)Random.Range(1, textures.Length);
        }
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
        if (SongSelect2.current == 3)
        {
            yield return new WaitForSeconds(0.35f);
        }
        yield return new WaitForSeconds(3);
        vidPlayer.clip = videos[SongSelect2.current];
        vidPlayer.targetTexture = textures[SongSelect2.current];
        VideoPlayerParent.GetComponent<RawImage>().texture = textures[SongSelect2.current];

    }
}
