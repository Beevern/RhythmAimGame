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
    //public float sens = 0.5f;
    // Update is called once per frame
    void Update()
    {
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            
            if (isPaused)
            {
                resume();
            }
            else
            {
                pause();
            }
        }
        
        setSens();
        
        Debug.Log(field.GetComponent<InputField>().text);
    }
    
    public void setSens()
    {
        Player.speedH = float.Parse(field.GetComponent<InputField>().text);
        Player.speedV = float.Parse(field.GetComponent<InputField>().text);
        
        field.GetComponent<InputField>().text = ""+Player.speedV;

    }
    
    public void resume()
    {
        /*
        startTimer.text = "3";
        yield return new WaitForSeconds(0.5f);
        startTimer.text = "2";
        yield return new WaitForSeconds(0.5f);
        startTimer.text = "1";
        yield return new WaitForSeconds(0.5f);
        startTimer.text = "Go!";
        yield return new WaitForSeconds(0.5f);
        startTimer.text = "";
        */
        Time.timeScale = 1f;
        isPaused = false;
        pauseUI.SetActive(false);
        GameObject.FindGameObjectWithTag("VideoPlayer").GetComponent<AudioSource>().Play();
        GameObject.FindGameObjectWithTag("VideoPlayer").GetComponent<VideoPlayer>().Play();

        Cursor.lockState = CursorLockMode.Locked;
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
