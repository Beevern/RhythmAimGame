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
    public GameObject pauseUI;
    public TextMeshProUGUI startTimer;
    public GameObject field;
    public static float lifeTime = 2f;
    public static float destroyTimer = 0f;
    public TMP_InputField sensField;
    public bool isCountingDown = false;
    //public float sens = 0.5f;

    private void Start()
    {
        sensField = field.GetComponent<TMP_InputField>();
        sensField.onValueChanged.AddListener(delegate { setSens(); });
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.Escape))
        {

            if (isPaused)
            {
                resume1();
            }
            else
            {
                pause();
            }
        }

        //setSens();

        //Debug.Log(field.GetComponent<InputField>().text);
    }

    public void setSens()
    {
        
        Player.speedH = float.Parse(field.GetComponent<TMP_InputField>().text);
        Player.speedV = float.Parse(field.GetComponent<TMP_InputField>().text);

        field.GetComponent<TMP_InputField>().text = ""+Player.speedV;

        
    }

    public void resume1()
    {
        StartCoroutine(resume2());
        
        Time.timeScale = 0.00000000001f;
        //isPaused = false;
        pauseUI.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        
    }
    IEnumerator resume2()
    {
        GameObject.FindGameObjectWithTag("VideoPlayer").GetComponent<AudioSource>().Pause();
        GameObject.FindGameObjectWithTag("VideoPlayer").GetComponent<VideoPlayer>().Pause();
        Cursor.lockState = CursorLockMode.Locked;
        if (!isCountingDown)
        {
            isCountingDown = true;
            startTimer.text = "3";
            yield return new WaitForSeconds(0.000000000005f);
            startTimer.text = "2";
            yield return new WaitForSeconds(0.000000000005f);
            startTimer.text = "1";
            yield return new WaitForSeconds(0.000000000005f);
            startTimer.text = "Go!";
            yield return new WaitForSeconds(0.000000000005f);
            startTimer.text = "";
            isCountingDown = false;

            GameObject.FindGameObjectWithTag("VideoPlayer").GetComponent<AudioSource>().Play();
            GameObject.FindGameObjectWithTag("VideoPlayer").GetComponent<VideoPlayer>().Play();
            Time.timeScale = 1f;
            isPaused = false;
            pauseUI.SetActive(false);
        }
        /*
        if (isCountingDown)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                StopCoroutine(pause2());
                isPaused = true;
                isCountingDown = false;
            }
        }
        */
        


        
    }

    public void pause()
    {

        isPaused = true;
        pauseUI.SetActive(true);
        Time.timeScale = 0f;
        GameObject.FindGameObjectWithTag("VideoPlayer").GetComponent<AudioSource>().Pause();
        GameObject.FindGameObjectWithTag("VideoPlayer").GetComponent<VideoPlayer>().Pause();
        Cursor.lockState = CursorLockMode.None;
    }



    public void LoadMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu");
    }
    public void QuitGame()
    {
        Application.Quit();
    }
}