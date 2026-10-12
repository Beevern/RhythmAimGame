using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class SwitchScene : MonoBehaviour

{
    // Start is called before the first frame update
    public Button playButton;
    public string sceneName;
    //Audio
    public AudioSource source;
    public AudioClip hover;
    public AudioClip press;
    void Start()
    {
        source = GameObject.FindGameObjectWithTag("Background").GetComponent<AudioSource>();
        Button btn = playButton.GetComponent<Button>();
        btn.onClick.AddListener(TaskOnClick);
    }

    void TaskOnClick()
    {
        //Duo mode: Song Select's play button starts the song on both PCs (host only)
        if (sceneName == "Game" && NetSession.Active)
        {
            if (!NetSession.IsHost) return;
            NetSession.StartSong(SongSelect2.theSong);
        }
        SceneManager.LoadScene(sceneName);
        source.PlayOneShot(press);
        ScoreKeeper.combo = 0;
    }
    void OnMouseOver()
    {
        //If your mouse hovers over the GameObject with the script attached, output this message
        source.PlayOneShot(hover);
    }
}
