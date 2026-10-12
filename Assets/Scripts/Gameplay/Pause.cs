using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
public class Pause : MonoBehaviour
{
    public static bool isPaused;
    public Button resumeButton;
    public GameObject pauseUI;
    public TextMeshProUGUI startTimer;
    public GameObject field;
    public TMP_InputField sensField;
    public bool isCountingDown = false;
    private Coroutine countdownRoutine;
    //Song playback, cached instead of searched for on every pause/resume
    private AudioSource songAudio;
    private VideoPlayer songVideo;

    private void Start()
    {
        Button btn = resumeButton.GetComponent<Button>();
        btn.onClick.AddListener(resume);
        GameObject player = GameObject.FindGameObjectWithTag("VideoPlayer");
        songAudio = player.GetComponent<AudioSource>();
        songVideo = player.GetComponent<VideoPlayer>();
    }

    // Update is called once per frame
    void Update()
    {

        //Esc belongs to the crosshair editor while it's open
        if (Input.GetKeyDown(KeyCode.Escape) && !CrosshairEditor.IsOpen && !NoteColorEditor.IsOpen && !DuoGame.Waiting)
        {

            if (isPaused)
            {
                if (isCountingDown)
                {
                    pause();
                }
                else
                {
                    resume();
                }
            }
            else
            {
                pause();
            }
        }

    }

    //Duo mode: pausing and resuming are mirrored on the partner's PC (tellPartner = false when the partner did it)
    public void resume() => resume(true);

    public void resume(bool tellPartner)
    {
        if (tellPartner) NetSession.Send("RESUME");

        pauseUI.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        countdownRoutine = StartCoroutine(countdown());
    }

    IEnumerator countdown()
    {
        if (!isCountingDown)
        {
            isCountingDown = true;
            startTimer.text = "3";
            yield return new WaitForSecondsRealtime(0.5f);
            startTimer.text = "2";
            yield return new WaitForSecondsRealtime(0.5f);
            startTimer.text = "1";
            yield return new WaitForSecondsRealtime(0.5f);
            startTimer.text = "Go!";
            yield return new WaitForSecondsRealtime(0.5f);
            startTimer.text = "";
            isCountingDown = false;

            Time.timeScale = 1f;
            //Only resume the song if it had started (pausing during the opening countdown)
            if (playMusic.VideoStarted)
            {
                songAudio.Play();
                songVideo.Play();
            }
            isPaused = false;
        }
    }

    public void pause() => pause(true);

    public void pause(bool tellPartner)
    {
        if (tellPartner) NetSession.Send("PAUSE");
        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
            countdownRoutine = null;
            isCountingDown = false;
            startTimer.text = "";
        }
        isPaused = true;
        pauseUI.SetActive(true);
        Time.timeScale = 0f;
        songAudio.Pause();
        songVideo.Pause();
        Cursor.lockState = CursorLockMode.None;
    }



    public void LoadMenu()
    {
        Time.timeScale = 1f;
        //Duo mode: leaving the song takes both players back to the lobby
        if (NetSession.Active)
        {
            NetSession.Send("LOBBY");
            MultiplayerLobby.OpenOnLoad = true;
        }
        SceneManager.LoadScene("Menu");
    }
    public void QuitGame()
    {
        Application.Quit();
    }
}