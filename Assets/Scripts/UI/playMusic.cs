using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.UI;
public class playMusic : MonoBehaviour
{
    //Players
    private AudioSource audioSource;
    private VideoPlayer vidPlayer;
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

        vidPlayer = GameObject.FindGameObjectWithTag("VideoPlayer").GetComponent<VideoPlayer>();
        /*
        RANDOM
        if (SongSelect2.current == 0)
        {
            SongSelect2.current = (int)Random.Range(1, LevelHolder.textures.Length);
        }
        */
        if (SongSelect2.theSong.Equals("Seasons by Rival and Cadmium Futuristik and Whogaux Remix"))
        {
            yield return new WaitForSeconds(0.35f);
        }
        yield return new WaitForSeconds(3f);
        // existing: built-in video
        if (LevelHolder.songVid.ContainsKey(SongSelect2.theSong) && LevelHolder.songVid[SongSelect2.theSong] != null)
        {
            vidPlayer.source = VideoSource.VideoClip;
            vidPlayer.clip = LevelHolder.songVid[SongSelect2.theSong];
            vidPlayer.targetTexture = LevelHolder.songText[SongSelect2.theSong];
        }
        else
        {
            // custom level: find its info
            var info = CustomLevelManager.Instance.levelList.levels
                         .Find(l => l.levelName == SongSelect2.theSong);

            if (info != null && !string.IsNullOrEmpty(info.songFilePath))
            {
                vidPlayer.source = VideoSource.Url;
                vidPlayer.url = info.songFilePath;
            }
        }
        //vidPlayer.clip = LevelHolder.songVid[SongSelect2.theSong];
        vidPlayer.targetTexture = LevelHolder.songText[SongSelect2.theSong];
        VideoPlayerParent.GetComponent<RawImage>().texture = LevelHolder.songText[SongSelect2.theSong];
        //yield return new WaitForSeconds((float)vidPlayer.clip.length);
        //SceneManager.LoadScene("GameOver");
    }
}