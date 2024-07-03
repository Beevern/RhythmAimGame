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
        openBtn.onClick.AddListener(OpenSettings);
        Button closeBtn = closeSettings.GetComponent<Button>();
        closeBtn.onClick.AddListener(CloseSettings);
        sensField = field.GetComponent<TMP_InputField>();
        sensField.onValueChanged.AddListener(delegate { setSens(); });
        volSlider.onValueChanged.AddListener(delegate { setVol(); });
    }

    void setVol()
    {
       //source.volume = volSlider.value;
    }

    public void setSens()
    {

        Player.speedH = float.Parse(field.GetComponent<TMP_InputField>().text);
        Player.speedV = float.Parse(field.GetComponent<TMP_InputField>().text);

        field.GetComponent<TMP_InputField>().text = "" + Player.speedV;
    }
    public void OpenSettings()
    {
        //Debug.Log("Opening Settings");
        settingsMenu.SetActive(true);
        isSettings = true;
    }
    public void CloseSettings()
    {
        //Debug.Log("Closing Settings");
        settingsMenu.SetActive(false);
        isSettings = false;
    }
    void OnMouseOver()
    {
        //If your mouse hovers over the GameObject with the script attached, output this message
        //source.PlayOneShot(hover);
    }


}
