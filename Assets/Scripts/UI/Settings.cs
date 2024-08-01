using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Video;
public class Settings : MonoBehaviour
{
    //Objects
    public Button settingsButton;
    public Button closeSettings;
    public TMP_InputField sensField;
    public GameObject field;
    public Slider volSlider;
    public GameObject settingsMenu;
    //Instance 
    public bool isSettings=false;
    //Audio
    public AudioSource source;
    public AudioClip hover;
    public AudioClip press;

    void Start()
    {
        //VideoPlayer videoPlayer = GameObject.FindGameObjectWithTag("VideoPlayer").GetComponent<VideoPlayer>();
        //source = GameObject.FindGameObjectWithTag("VideoPlayer").GetComponent<AudioSource>();
        //videoPlayer.AudioSource = source;
        Button openBtn = settingsButton.GetComponent<Button>();
        openBtn.onClick.AddListener(OpenCloseSettings);
        Button closeBtn = closeSettings.GetComponent<Button>();
        closeBtn.onClick.AddListener(OpenCloseSettings);
        sensField = field.GetComponent<TMP_InputField>();
        sensField.onValueChanged.AddListener(delegate { setSens(); });
        volSlider.onValueChanged.AddListener(delegate { setVol(); });
        volSlider.value = PlayerPrefs.GetFloat("volume", 0.5f);
        field.GetComponent<TMP_InputField>().text = "" + PlayerPrefs.GetFloat("xSen", 50);
    }

    void setVol()
    {
        PlayerPrefs.SetFloat("volume", volSlider.value);
        source.volume = PlayerPrefs.GetFloat("volume", 0.5f);
    }

    public void setSens()
    {

        
        PlayerPrefs.SetFloat("xSen", float.Parse(field.GetComponent<TMP_InputField>().text));
        PlayerPrefs.SetFloat("ySen", float.Parse(field.GetComponent<TMP_InputField>().text));
        Player.speedH = PlayerPrefs.GetFloat("xSen", 50);
        Player.speedV = PlayerPrefs.GetFloat("ySen", 50);
    }
    public void OpenCloseSettings()
    {
        //Debug.Log("Opening Settings");
        if (!isSettings)
        {
            settingsMenu.SetActive(true);
            isSettings = true;
        }
        else
        {
            settingsMenu.SetActive(false);
            isSettings = false;
        }
    }
    void OnMouseOver()
    {
        //If your mouse hovers over the GameObject with the script attached, output this message
        //source.PlayOneShot(hover);
    }


}
