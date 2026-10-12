using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
    //Paired controls (created by SettingsCard so each setting has a slider + a text box)
    public Slider sensSlider;
    public TMP_InputField volField;
    //Per-level volume (only created by SettingsCard inside a level, not on the main menu)
    public Slider levelVolSlider;
    public TMP_InputField levelVolField;
    //Instance
    public bool isSettings=false;
    public static float vol;
    //Volume multiplier for the level being played: level.json "volume" x the player's own
    //"Level volume" for that song. Set by playMusic while a level is playing, 1 elsewhere.
    public static float LevelVolume = 1f;

    //The player's own volume for a song (0..2, 1 = unchanged), saved per song
    public static float PlayerLevelVolume(string song) => PlayerPrefs.GetFloat(song + "levelVolume", 1f);

    //level.json volume x the player's level volume
    public static float CombinedLevelVolume(string song)
    {
        float baseVol = LevelHolder.levels.TryGetValue(song ?? "", out LevelInfo info) ? info.volume : 1f;
        return baseVol * PlayerLevelVolume(song);
    }
    //Sensitivity range for the slider; the text box accepts anything in the wider range
    public const float SensSliderMin = 1f, SensSliderMax = 200f;
    public const float SensMin = 0.1f, SensMax = 1000f;
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

        //Sensitivity: slider and text box drive each other
        sensField.contentType = TMP_InputField.ContentType.DecimalNumber;
        sensField.onValueChanged.AddListener(text =>
        {
            //While typing, only apply valid numbers and leave the text alone
            if (TryParse(text, out float v)) SetSens(v, false);
        });
        sensField.onEndEdit.AddListener(text => SetSens(Sens, true));
        if (sensSlider != null)
        {
            sensSlider.minValue = SensSliderMin;
            sensSlider.maxValue = SensSliderMax;
            sensSlider.wholeNumbers = false;
            sensSlider.onValueChanged.AddListener(v => SetSens(Mathf.Round(v * 2f) / 2f, true));
        }

        //Volume: slider is 0-1, text box shows 0-100
        volSlider.onValueChanged.AddListener(v => SetVol(v, true));
        if (volField != null)
        {
            volField.contentType = TMP_InputField.ContentType.IntegerNumber;
            volField.characterLimit = 3;
            volField.onValueChanged.AddListener(text =>
            {
                if (TryParse(text, out float v)) SetVol(v / 100f, false);
            });
            volField.onEndEdit.AddListener(text => SetVol(vol, true));
        }

        //Level volume: slider is 0-2 (100% = unchanged), text box shows 0-200
        if (levelVolSlider != null)
        {
            levelVolSlider.minValue = 0f;
            levelVolSlider.maxValue = 2f;
            levelVolSlider.wholeNumbers = false;
            levelVolSlider.onValueChanged.AddListener(v => SetLevelVol(v, true));
        }
        if (levelVolField != null)
        {
            levelVolField.contentType = TMP_InputField.ContentType.IntegerNumber;
            levelVolField.characterLimit = 3;
            levelVolField.onValueChanged.AddListener(text =>
            {
                if (TryParse(text, out float v)) SetLevelVol(v / 100f, false);
            });
            levelVolField.onEndEdit.AddListener(text => SetLevelVol(PlayerLevelVolume(SongSelect2.theSong), true));
        }
        if (levelVolSlider != null || levelVolField != null)
            SetLevelVol(PlayerLevelVolume(SongSelect2.theSong), true);

        //Load saved values (PlayerPrefs persists across scenes and restarts)
        SetVol(PlayerPrefs.GetFloat("volume", 0.5f), true);
        SetSens(PlayerPrefs.GetFloat("xSen", 50), true);
    }

    public static float Sens => PlayerPrefs.GetFloat("xSen", 50);

    void SetVol(float v, bool updateField)
    {
        vol = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat("volume", vol);
        if (source != null) source.volume = vol * LevelVolume;
        volSlider.SetValueWithoutNotify(vol);
        if (updateField && volField != null) volField.SetTextWithoutNotify(Mathf.RoundToInt(vol * 100f).ToString());
    }

    void SetLevelVol(float v, bool updateField)
    {
        string song = SongSelect2.theSong ?? "";
        v = Mathf.Clamp(v, 0f, 2f);
        PlayerPrefs.SetFloat(song + "levelVolume", v);
        LevelVolume = CombinedLevelVolume(song);
        if (source != null) source.volume = vol * LevelVolume;
        if (levelVolSlider != null) levelVolSlider.SetValueWithoutNotify(v);
        if (updateField && levelVolField != null) levelVolField.SetTextWithoutNotify(Mathf.RoundToInt(v * 100f).ToString());
    }

    void SetSens(float v, bool updateField)
    {
        v = Mathf.Clamp(v, SensMin, SensMax);
        PlayerPrefs.SetFloat("xSen", v);
        PlayerPrefs.SetFloat("ySen", v);
        Player.speedH = v;
        Player.speedV = v;
        if (sensSlider != null) sensSlider.SetValueWithoutNotify(Mathf.Clamp(v, SensSliderMin, SensSliderMax));
        if (updateField) sensField.SetTextWithoutNotify(v.ToString("0.##", CultureInfo.InvariantCulture));
    }

    static bool TryParse(string text, out float v)
    {
        return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out v) && v >= 0f;
    }

    void setVol()
    {
        SetVol(volSlider.value, true);
    }

    //Kept for any inspector events still pointing here
    public void setSens()
    {
        if (TryParse(sensField.text, out float v)) SetSens(v, false);
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
            //Write to disk now rather than only on quit
            PlayerPrefs.Save();
        }
    }

    void OnDestroy()
    {
        PlayerPrefs.Save();
    }

    void OnMouseOver()
    {
        //If your mouse hovers over the GameObject with the script attached, output this message
        //source.PlayOneShot(hover);
    }


}
