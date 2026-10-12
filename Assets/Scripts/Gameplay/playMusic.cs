using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

//Plays the level's video (from its level folder) behind the game.
//The video is loaded during the 3 second countdown and started exactly when it ends,
//so note timing doesn't depend on how long the video takes to load.
public class playMusic : MonoBehaviour
{
    //Players
    private VideoPlayer vidPlayer;
    private RenderTexture renderTexture;
    //Other Objects
    public GameObject VideoPlayerParent;
    //Instance Vars
    public static double volume;
    //True once the song video has actually started (Pause shouldn't start it early)
    public static bool VideoStarted { get; private set; }

    //Notes are charted against the video starting this long after the level loads
    private const float CountdownSeconds = 3f;

    void Awake()
    {
        VideoStarted = false;
        //Set before Settings.Start applies the volume, so the level's own volume is included
        Settings.LevelVolume = Settings.CombinedLevelVolume(SongSelect2.theSong);
        StartCoroutine(playVid());
    }

    IEnumerator playVid()
    {
        vidPlayer = GameObject.FindGameObjectWithTag("VideoPlayer").GetComponent<VideoPlayer>();
        if (!LevelHolder.levels.TryGetValue(SongSelect2.theSong ?? "", out LevelInfo level) || level.videoPath == null)
        {
            Debug.LogWarning("No video for level: " + SongSelect2.theSong);
            yield break;
        }

        //Play from the level folder; sound goes through the scene's AudioSource (volume setting, pause)
        AudioSource audio = vidPlayer.GetComponent<AudioSource>();
        vidPlayer.Stop();
        vidPlayer.playOnAwake = false;
        vidPlayer.source = VideoSource.Url;
        vidPlayer.url = level.videoPath;
        if (audio != null)
        {
            vidPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            vidPlayer.controlledAudioTrackCount = 1;
            vidPlayer.EnableAudioTrack(0, true);
            vidPlayer.SetTargetAudioSource(0, audio);
            audio.volume = PlayerPrefs.GetFloat("volume", 0.5f) * Settings.LevelVolume;
        }
        vidPlayer.Prepare();
        while (!vidPlayer.isPrepared)
            yield return null;

        //Render into a texture sized to this video, shown by the background RawImage
        renderTexture = new RenderTexture((int)vidPlayer.width, (int)vidPlayer.height, 0);
        renderTexture.Create();
        vidPlayer.targetTexture = renderTexture;
        VideoPlayerParent.GetComponent<RawImage>().texture = renderTexture;

        //Start on the beat: after the countdown, plus any per-level delay (level.json "videoDelay").
        //Scaled time, so pausing during the countdown holds the video back too.
        float startAt = CountdownSeconds + level.videoDelay;
        while (Time.timeSinceLevelLoad < startAt || Pause.isPaused)
            yield return null;
        vidPlayer.Play();
        VideoStarted = true;
    }

    void OnDestroy()
    {
        Settings.LevelVolume = 1f;
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }
}
